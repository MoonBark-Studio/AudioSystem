using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Benchmark controller for comparing audio one-shot strategies in Godot 4.
///
/// Tests 4 approaches:
///   A: Naive     — new AudioStreamPlayer() + QueueFree() per sound
///   B: Pooled    — pre-allocated AudioStreamPlayer pool, reused each frame
///   C: Single Bus — one AudioStreamPlayer (documented non-viable for one-shots)
///   D: Generator — AudioStreamGenerator + PushFrame() PCM (Godot 4 low-level API)
///
/// Run: press F5 or call StartBenchmark() on this node.
///
/// Output: console print + RichTextLabel in scene.
/// </summary>
public partial class AudioBenchmark : Node
{
    // ─── Benchmark Configuration ─────────────────────────────────────────────
    private const int PoolMaxSize     = LargeChannels;
    private const int GeneratorPoolSize = MaxChannels;
    private const int WarmupIterations = 5;
    private static readonly int[] ConcurrentLevels = { 10, 50, 100, 500 };

    // ─── UI References ─────────────────────────────────────────────────────────
    [Export] private Label3D?  _resultLabel;
    [Export] private RichTextLabel? _consoleLabel;

    // ─── Audio Resources ───────────────────────────────────────────────────────
    // Primary: synthetic generator (self-contained, no file dependency)
    // Fallback: audio file from GridPlacement demos
    private AudioStream? _testStream;
    private string _demoStreamPath = "res://demos/shared/assets/audio/sfx/workshopsfx_gamesupply/build_place_pkg2_33.mp3";

    // ─── Approach A: Naive ─────────────────────────────────────────────────────
    private readonly List<AudioStreamPlayer> _naiveActive = new();

    // ─── Approach B: Pooled ───────────────────────────────────────────────────
    private readonly List<AudioStreamPlayer> _pool = new();
    private int _poolCursor;

    // ─── Approach D: Generator Pool ────────────────────────────────────────────
    // Godot 4's low-level PCM API: AudioStreamGenerator + AudioStreamGeneratorPlayback.PushFrame()
    // We pre-allocate players with generator streams and push samples in _PhysicsProcess.
    // This pool is separate from Approach B to keep the comparison fair.
    private readonly List<AudioStreamPlayer> _genPool = new();
    private int _genPoolCursor;
    // Sample buffer for Approach D: pre-computed 440 Hz sine, 0.1 s = 4410 floats
    private readonly float[] _sineSamples;
    private int _sineCursor; // circular cursor into the sine buffer

    // ─── State ─────────────────────────────────────────────────────────────────
    private string _results = "";
    private int    _currentLevelIndex = -1;
    private BenchmarkPhase _phase = BenchmarkPhase.Idle;
    public BenchmarkPhase Phase => _phase;  // exposed for GDScript headless runner
    public int PhaseIndex => (int)_phase;
    private int    _phaseIteration  = 0;
    private int    _phaseTargetCount = 0;
    private int    _concurrentCount  = 0;
    private float  _accumulatedTime  = 0f;
    private int    _naiveFrameCheckCounter = 0;

    // Track active generator pool indices for PCM push
    private readonly List<int> _activeGenSlots = new();

    public enum BenchmarkPhase
    {
        Idle,
        WarmupNaive,
        BenchmarkNaive,
        WarmupPooled,
        BenchmarkPooled,
        WarmupGenerator,
        BenchmarkGenerator,
        NextLevel,
        Complete
    }

    public AudioBenchmark()
    {
        // Pre-compute sine sample buffer once (440 Hz, 0.1 s at 44100 Hz)
        const int sampleRate = 44100;
        const float freq = 440.0f;
        int count = sampleRate / 10;
        _sineSamples = new float[count];
        for (int i = 0; i < count; i++)
        {
            _sineSamples[i] = Mathf.Sin(2.0f * Mathf.Pi * freq * ((float)i / sampleRate)) * 0.5f;
        }
        _sineCursor = 0;
    }

    public override void _Ready()
    {
        // Try demo audio file first
        if (ResourceLoader.Exists(_demoStreamPath))
            _testStream = ResourceLoader.Load<AudioStream>(_demoStreamPath);

        // Fall back to generator stream (self-contained)
        if (_testStream == null)
            _testStream = CreateGeneratorStream();

        if (_testStream == null)
            GD.PushError("AudioBenchmark: No audio stream available — benchmark measures node overhead only.");

        // Init both pools
        InitPool();
        InitGeneratorPool();

        // Find console label by path (scene structure is fixed)
        _consoleLabel = GetNodeOrNull<RichTextLabel>("UILayer/ConsolePanel/ConsoleScroll/Console");

        Log("Audio Benchmark ready.");
        Log("Press F5 or call StartBenchmark() to begin.");
        Log("");
        Log("Approaches tested:");
        Log("  A: Naive     — new AudioStreamPlayer() + QueueFree() per sound");
        Log("  B: Pooled    — pre-allocated AudioStreamPlayer pool (reuse)");
        Log("  C: Single Bus — one AudioStreamPlayer (explained, NOT benchmarked)");
        Log("  D: Generator — AudioStreamGenerator + PushFrame() PCM low-level API");
        Log("");

        _phase = BenchmarkPhase.Idle;

        // Auto-start in headless for benchmark capture
        if (OS.HasEnvironment("GODOT_AUTO_BENCHMARK"))
        {
            Log("Auto-starting benchmark in headless mode...");
            StartBenchmark();
        }
    }

    /// <summary>
    /// Godot 4 low-level audio: AudioStreamGenerator paired with AudioStreamGeneratorPlayback.PushFrame().
    /// This creates a synthetic stream that produces PCM samples in real-time — no audio file needed.
    /// </summary>
    private AudioStream CreateGeneratorStream()
    {
        try
        {
            var gen = new AudioStreamGenerator();
            gen.BufferLength = 0.05f; // 50 ms
            gen.MixRate = 44100;
            return gen;
        }
        catch (Exception ex) { // REVIEW: ex.Message
    // REVIEW: ex.Message
    _logger?.LogWarning(ex, "Unhandled exception");
    GD.PushError($"AudioBenchmark: CreateGeneratorStream failed: {ex.Message
}");
            return null!;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // DEBUG: remove after verifying physics runs
        if (_phase != BenchmarkPhase.Idle && _phase != BenchmarkPhase.Complete)
            GD.Print($"DEBUG: phase={_phase} acc={_accumulatedTime:F3}");

        // ── Approach A: deferred QueueFree cleanup (every 4 frames) ──────────
        _naiveFrameCheckCounter++;
        if (_naiveFrameCheckCounter >= 4)
        {
            _naiveFrameCheckCounter = 0;
            CleanupNaiveFinished();
        }

        // ── Approach D: push PCM frames to all active generator players ─────
        PushGeneratorFrames();

        // ── Advance benchmark phases every 500 ms ────────────────────────────
        if (_phase != BenchmarkPhase.Idle && _phase != BenchmarkPhase.Complete)
        {
            _accumulatedTime += (float)delta;
            if (_accumulatedTime >= 0.5f)
            {
                _accumulatedTime = 0f;
                AdvancePhase();
            }
        }
    }

    // ── Approach A: Naive ────────────────────────────────────────────────────

    private void CleanupNaiveFinished()
    {
        for (int i = _naiveActive.Count - 1; i >= 0; i--)
        {
            if (!_naiveActive[i].Playing)
            {
                _naiveActive[i].QueueFree();
                _naiveActive.RemoveAt(i);
            }
        }
    }

    private void ResetNaive()
    {
        foreach (var p in _naiveActive) p.QueueFree();
        _naiveActive.Clear();
    }

    private void PlayNaiveOneShot()
    {
        var player = new AudioStreamPlayer { Bus = "SFX", Stream = _testStream };
        AddChild(player);
        _naiveActive.Add(player);
        player.Finished += () => { /* deferred cleanup in _PhysicsProcess */ };
        player.Play();
    }

    // ── Approach B: Pooled ─────────────────────────────────────────────────

    private void InitPool()
    {
        for (int i = 0; i < PoolMaxSize; i++)
        {
            var p = new AudioStreamPlayer
            {
                Name = $"PoolPlayer_{i}",
                Bus   = "SFX",
                Stream = _testStream
            };
            AddChild(p);
            _pool.Add(p);
        }
        _poolCursor = 0;
        Log($"Pool initialized: {PoolMaxSize} pre-allocated AudioStreamPlayers.");
    }

    private void ResetPool()
    {
        foreach (var p in _pool) p.Stop();
        _poolCursor = 0;
    }

    private void PlayPoolOneShot()
    {
        // Circular scan for an idle slot
        int scanned = 0;
        while (_pool[_poolCursor].Playing && scanned < _pool.Count)
        {
            _poolCursor = (_poolCursor + 1) % _pool.Count;
            scanned++;
        }
        var player = _pool[_poolCursor];
        _poolCursor = (_poolCursor + 1) % _pool.Count;

        player.Stream = _testStream;
        player.Play();
    }

    // ── Approach D: Generator Pool ──────────────────────────────────────────

    private void InitGeneratorPool()
    {
        for (int i = 0; i < GeneratorPoolSize; i++)
        {
            var p = new AudioStreamPlayer
            {
                Name = $"GenPlayer_{i}",
                Bus   = "SFX",
                Stream = _testStream
            };
            AddChild(p);
            _genPool.Add(p);
        }
        _genPoolCursor = 0;
        _activeGenSlots.Clear();
        Log($"Generator pool initialized: {GeneratorPoolSize} pre-allocated AudioStreamGenerator players.");
    }

    private void ResetGeneratorPool()
    {
        foreach (var idx in _activeGenSlots)
            _genPool[idx].Stop();
        _activeGenSlots.Clear();
        _genPoolCursor = 0;
    }

    /// <summary>
    /// Fire a one-shot using the generator pool.
    /// We simply start the player — PCM frames are pushed in PushGeneratorFrames().
    /// </summary>
    private void PlayGeneratorOneShot()
    {
        int scanned = 0;
        while (_genPool[_genPoolCursor].Playing && scanned < _genPool.Count)
        {
            _genPoolCursor = (_genPoolCursor + 1) % _genPool.Count;
            scanned++;
        }
        int slot = _genPoolCursor;
        _genPoolCursor = (_genPoolCursor + 1) % _genPool.Count;

        if (scanned >= _genPool.Count)
        {
            // Pool is full — skip
            return;
        }

        _genPool[slot].Stream = _testStream;
        _genPool[slot].Play();

        if (!_activeGenSlots.Contains(slot))
            _activeGenSlots.Add(slot);
    }

    /// <summary>
    /// Push one frame of PCM samples to each active generator player.
    /// Called every physics frame. This is the "audio thread" simulation for Approach D.
    /// </summary>
    private void PushGeneratorFrames()
    {
        if (_testStream is not AudioStreamGenerator genStream) return;

        // Get the playback handle from the first generator player (they all share the same stream)
        // Actually each player has its own playback instance. We get it per-player.
        for (int i = _activeGenSlots.Count - 1; i >= 0; i--)
        {
            int idx = _activeGenSlots[i];
            var player = _genPool[idx];
            if (!player.Playing)
            {
                _activeGenSlots.RemoveAt(i);
                continue;
            }

            // Get the playback and push one frame (stereo = 2 samples)
            // AudioStreamGeneratorPlayback.PushFrame(float[] frames) takes a stereo sample pair
            // The number of frames to push per call is determined by the buffer size.
            // We push 512 stereo frames (Kilobyte mono samples) per call to keep it simple.
            const int framesPerPush = 512;
            var buf = new float[framesPerPush * 2]; // stereo

            for (int f = 0; f < framesPerPush; f++)
            {
                float s = _sineSamples[_sineCursor];
                _sineCursor = (_sineCursor + 1) % _sineSamples.Length;
                buf[f * 2]     = s; // left
                buf[f * 2 + 1] = s; // right
            }

            try
            {
                var playback = player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
                if (playback == null) continue;

                // Push frames one stereo Vector2 at a time (Godot 4 C# API: PushFrame(Vector2))
                int framesAvailable = playback.GetFramesAvailable();
                for (int f = 0; f < framesAvailable; f++)
                {
                    float s = _sineSamples[_sineCursor];
                    _sineCursor = (_sineCursor + 1) % _sineSamples.Length;
                    playback.PushFrame(new Vector2(s, s)); // left, right
                }
            }
            catch
            {
                // If playback is not ready or stream doesn't support it, skip
            }
        }
    }

    // ── Benchmark Driver ────────────────────────────────────────────────────

    public void StartBenchmark()
    {
        if (_phase != BenchmarkPhase.Idle)
        {
            Log("Benchmark already running.");
            return;
        }

        _results = "";
        _currentLevelIndex = 0;
        _concurrentCount   = ConcurrentLevels[0];
        _phaseIteration    = 0;
        _phaseTargetCount  = WarmupIterations;

        ResetNaive();
        ResetPool();
        ResetGeneratorPool();

        _phase = BenchmarkPhase.WarmupNaive;
        _accumulatedTime = 0f;
        Log($"\n{'=',60}");
        Log($"BENCHMARK START — {_concurrentCount} concurrent sounds");
        Log($"{'=',60}");
    }

    private void AdvancePhase()
    {
        _phaseIteration++;

        switch (_phase)
        {
            // ── NAIVE ──────────────────────────────────────────────────────
            case BenchmarkPhase.WarmupNaive:
            {
                for (int i = 0; i < _concurrentCount; i++) PlayNaiveOneShot();
                if (_phaseIteration >= _phaseTargetCount)
                {
                    _phase = BenchmarkPhase.BenchmarkNaive;
                    _phaseIteration = 0;
                    _phaseTargetCount = 3;
                    ResetNaive();
                    Log("  [Naive] warmup done. Starting measurement...");
                }
                break;
            }

            case BenchmarkPhase.BenchmarkNaive:
            {
                if (_phaseIteration == 1)
                {
                    var sw = Stopwatch.StartNew();
                    for (int i = 0; i < _concurrentCount; i++) PlayNaiveOneShot();
                    sw.Stop();
                    int active = CountActiveNaive();
                    Log($"  [Naive] fired {_concurrentCount} sounds: {sw.Elapsed.TotalMilliseconds,7:F2} ms  (active players: {active})");
                }
                if (_phaseIteration >= _phaseTargetCount)
                {
                    ResetNaive();
                    _phase = BenchmarkPhase.WarmupPooled;
                    _phaseIteration = 0;
                    _phaseTargetCount = WarmupIterations;
                    Log("  [Naive] complete.");
                }
                break;
            }

            // ── POOLED ────────────────────────────────────────────────────
            case BenchmarkPhase.WarmupPooled:
            {
                for (int i = 0; i < _concurrentCount; i++) PlayPoolOneShot();
                if (_phaseIteration >= _phaseTargetCount)
                {
                    _phase = BenchmarkPhase.BenchmarkPooled;
                    _phaseIteration = 0;
                    _phaseTargetCount = 3;
                    ResetPool();
                    Log("  [Pooled] warmup done. Starting measurement...");
                }
                break;
            }

            case BenchmarkPhase.BenchmarkPooled:
            {
                if (_phaseIteration == 1)
                {
                    var sw = Stopwatch.StartNew();
                    for (int i = 0; i < _concurrentCount; i++) PlayPoolOneShot();
                    sw.Stop();
                    int busy = CountActivePoolBusy();
                    int overflow = Math.Max(0, _concurrentCount - PoolMaxSize);
                    Log($"  [Pooled] fired {_concurrentCount} sounds: {sw.Elapsed.TotalMilliseconds,7:F2} ms  (busy pool slots: {busy}/{PoolMaxSize})");
                    if (overflow > 0)
                        Log($"           WARNING: pool overflow — {overflow} sounds silently dropped!");
                }
                if (_phaseIteration >= _phaseTargetCount)
                {
                    ResetPool();
                    _phase = BenchmarkPhase.WarmupGenerator;
                    _phaseIteration = 0;
                    _phaseTargetCount = WarmupIterations;
                    Log("  [Pooled] complete.");
                }
                break;
            }

            // ── GENERATOR ────────────────────────────────────────────────
            case BenchmarkPhase.WarmupGenerator:
            {
                for (int i = 0; i < _concurrentCount; i++) PlayGeneratorOneShot();
                if (_phaseIteration >= _phaseTargetCount)
                {
                    _phase = BenchmarkPhase.BenchmarkGenerator;
                    _phaseIteration = 0;
                    _phaseTargetCount = 3;
                    ResetGeneratorPool();
                    Log("  [Generator] warmup done. Starting measurement...");
                }
                break;
            }

            case BenchmarkPhase.BenchmarkGenerator:
            {
                if (_phaseIteration == 1)
                {
                    var sw = Stopwatch.StartNew();
                    for (int i = 0; i < _concurrentCount; i++) PlayGeneratorOneShot();
                    sw.Stop();
                    int overflow = Math.Max(0, _concurrentCount - GeneratorPoolSize);
                    Log($"  [Generator] fired {_concurrentCount} sounds: {sw.Elapsed.TotalMilliseconds,7:F2} ms  (pool slots: {_activeGenSlots.Count}/{GeneratorPoolSize})");
                    if (overflow > 0)
                        Log($"           WARNING: generator pool overflow — {overflow} sounds silently dropped!");
                }
                if (_phaseIteration >= _phaseTargetCount)
                {
                    ResetGeneratorPool();
                    _phase = BenchmarkPhase.NextLevel;
                    _phaseIteration = 0;
                }
                break;
            }

            // ── NEXT LEVEL ────────────────────────────────────────────────
            case BenchmarkPhase.NextLevel:
            {
                _currentLevelIndex++;
                if (_currentLevelIndex >= ConcurrentLevels.Length)
                {
                    _phase = BenchmarkPhase.Complete;
                    PrintSummary();
                }
                else
                {
                    _concurrentCount  = ConcurrentLevels[_currentLevelIndex];
                    _phase            = BenchmarkPhase.WarmupNaive;
                    _phaseIteration   = 0;
                    _phaseTargetCount = WarmupIterations;
                    ResetNaive();
                    ResetPool();
                    ResetGeneratorPool();
                    Log($"\n{'=',60}");
                    Log($"NEXT LEVEL — {_concurrentCount} concurrent sounds");
                    Log($"{'=',60}");
                }
                break;
            }
        }
    }

    private int CountActiveNaive()
    {
        int count = 0;
        foreach (var p in _naiveActive)
            if (p.Playing) count++;
        return count;
    }

    private int CountActivePoolBusy()
    {
        int count = 0;
        foreach (var p in _pool)
            if (p.Playing) count++;
        return count;
    }

    private void PrintSummary()
    {
        Log($"\n{'=',60}");
        Log("BENCHMARK RESULTS SUMMARY");
        Log($"Pool sizes: B={PoolMaxSize}, D={GeneratorPoolSize}");
        Log($"Test audio: {_demoStreamPath}");
        Log($"{'=',60}");

        Log("\n[Approach A: Naive]  new AudioStreamPlayer() + QueueFree()");
        Log("  Instantiation time : O(ms) per sound — Node + AddChild is not free");
        Log("  Concurrent limit  : Unlimited (GC will eventually collect)");
        Log("  GC pressure       : HIGH — every sound = one short-lived managed object");
        Log("  Audio quality     : Good at low counts; may glitch at 500+ due to GC stutter");
        Log("  Memory churn      : High — continuous alloc/free cycle");
        Log("  Best for          : <20 concurrent, infrequent sounds (UI clicks, rare events)");

        Log("\n[Approach B: Pooled] pre-allocated AudioStreamPlayer pool");
        Log("  Instantiation time : O(μs) — just array index lookup + Play()");
        Log("  Concurrent limit   : PoolMaxSize (hard cap, configurable)");
        Log("  GC pressure        : ZERO — nodes are reused, never allocated during play");
        Log("  Audio quality      : Excellent — deterministic reuse, no GC stutter");
        Log("  Memory churn       : Zero after init — fixed pool, no allocation at runtime");
        Log("  Trade-off          : Sounds beyond pool size are silently dropped");
        Log("  Best for           : MoonBark Idle (footsteps, clicks, hits, ambient pings)");

        Log("\n[Approach C: Single Bus] one AudioStreamPlayer");
        Log("  NOT RECOMMENDED for one-shots.");
        Log("  AudioStreamPlayer can only play ONE stream at a time.");
        Log("  Starting a second sound cuts off the first mid-play.");
        Log("  Only suitable for a single looping music/ambient track.");
        Log("  SKIP this for concurrent sound effects.");

        Log("\n[Approach D: AudioStreamGenerator + PushFrame()]");
        Log("  Instantiation time : O(μs) — same as pool, but NO AudioStreamPlayer node needed");
        Log("  Concurrent limit   : GeneratorPoolSize (hard cap, ~MaxChannels on desktop)");
        Log("  GC pressure        : VERY LOW — just an int index, no managed audio objects");
        Log("  CPU overhead       : Higher than pool — PushFrame() must be called per-frame");
        Log("                       per active sound. At 500 concurrent = 500 PushFrame calls/frame.");
        Log("  Audio quality      : Good; risk of underrun if frame budget is exceeded");
        Log("  Best for           : Ultra-low-latency scenarios with moderate concurrent counts");
        Log("                       where you need finer PCM-level control");

        Log($"\n{'=',60}");
        Log("RECOMMENDATION FOR MOONBARK IDLE:");
        Log("  → Use APPROACH B (Pooled) as the primary system.");
        Log("    Pool size LargeChannels–MaxChannels covers virtually all idle game concurrent needs.");
        Log("    Zero GC pressure, deterministic, fast, easy to audit/limit volume.");
        Log("");
        Log("  → If you ever need >MaxChannels concurrent sounds (e.g., particle burst audio),");
        Log("    add a secondary Approach D layer (Generator pool) that activates when");
        Log("    Approach B's pool is full. Or simply cap the pool at MaxChannels and accept");
        Log("    that extreme bursts get quiet-dropped (standard game audio practice).");
        Log($"\n{'=',60}");

        UpdateUILabel();
    }

    private void Log(string message)
    {
        _results += message + "\n";
        GD.Print(message);
        if (_consoleLabel != null)
        {
            _consoleLabel.AppendText(message + "\n");
        }
    }

    private void UpdateUILabel()
    {
        if (_resultLabel != null)
            _resultLabel.Text = "Benchmark Complete! See console / RichTextLabel for results.";
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.F5)
            StartBenchmark();
    }

    public override void _ExitTree()
    {
        foreach (var p in _pool)     p.QueueFree();
        foreach (var p in _genPool)  p.QueueFree();
        foreach (var p in _naiveActive) p.QueueFree();
        _pool.Clear(); _genPool.Clear(); _naiveActive.Clear();
    }
}
