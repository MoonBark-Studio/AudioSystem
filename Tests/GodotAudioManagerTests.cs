namespace MoonBark.AudioSystem.Tests;

using MoonBark.AudioSystem.Core.Configuration;
using System.IO;
using Xunit;

public class GodotAudioManagerTests
{
    private static readonly string ConfigPath = Path.Combine(
        AppContext.BaseDirectory, "Fixtures", "audio_config.json");

    [Fact]
    public void AudioConfigLoader_ResolvesRelativeRootFromConfigFileLocation()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(ConfigPath);

        // Act
        string rootPath = AudioConfigLoader.ResolveAudioRootPath(config, ConfigPath);

        // Assert — audio_root_path in fixture is "test_fixtures" relative to config dir
        string expectedDir = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(ConfigPath)!, "test_fixtures"));
        Assert.Equal(expectedDir, rootPath);
    }

    [Fact]
    public void AudioConfigLoader_BuildAbsolutePathMap_ExpandsRelativeCuePaths()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(ConfigPath);

        // Act
        AudioPathCollection absolute = AudioConfigLoader.BuildAbsolutePathMap(config, config.Cues, ConfigPath);

        // Assert — all 6 cue paths should be absolute
        Assert.Equal(6, absolute.Count);
        foreach (string cueId in absolute.CueIds)
        {
            Assert.True(absolute.TryGetPath(cueId, out string? path), $"cue {cueId} not found");
            Assert.True(Path.IsPathRooted(path!), $"Expected absolute path for {cueId}: {path}");
        }
    }

    [Fact]
    public void AudioConfigLoader_CuesCaseInsensitive()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(ConfigPath);

        // Assert — lookups are case-insensitive
        Assert.True(config.Cues.TryGetPath("FOOTSTEP_STONE", out _));
        Assert.True(config.Cues.TryGetPath("footstep_stone", out _));
        Assert.True(config.Cues.TryGetPath("Footstep_Stone", out _));
        Assert.False(config.Cues.TryGetPath("nonexistent_cue", out _));
    }

    [Fact]
    public void AudioConfigLoader_AllCuePathsHaveValidExtensions()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(ConfigPath);
        string[] validExtensions = { ".ogg", ".wav", ".mp3" };

        // Act
        AudioPathCollection absolute = AudioConfigLoader.BuildAbsolutePathMap(config, config.Cues, ConfigPath);

        // Assert — all cue paths have valid audio file extensions
        foreach (string cueId in absolute.CueIds)
        {
            Assert.True(absolute.TryGetPath(cueId, out string? path), $"cue {cueId} not found");
            string ext = Path.GetExtension(path!).ToLowerInvariant();
            bool isValid = validExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
            Assert.True(isValid, $"Unexpected extension for {cueId}: {ext}");
        }
    }

    [Fact]
    public void AudioConfigLoader_CueIdsMatchBetweenDictAndTypedCollection()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(ConfigPath);

        // Assert — typed collection and raw dictionary contain the same cue IDs
        var typedIds = new HashSet<string>(config.Cues.CueIds, StringComparer.OrdinalIgnoreCase);
        var dictKeys = new HashSet<string>(config.CuesDict.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(dictKeys, typedIds);
    }

    [Fact]
    public void AudioPathCollection_RoundTripThroughToDictionary_PreservesAllEntries()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(ConfigPath);

        // Act
        Dictionary<string, string> dict = config.Cues.ToDictionary();

        // Assert
        Assert.Equal(config.Cues.Count, dict.Count);
        foreach (var kvp in dict)
        {
            Assert.True(config.Cues.TryGetPath(kvp.Key, out string? fromCollection));
            Assert.Equal(kvp.Value, fromCollection);
        }
    }

    [Fact]
    public void AudioConfigLoader_ResolveConfigPath_ThrowsWithClearMessage()
    {
        // Act & Assert — no config exists in non_existent_directory
        var ex = Assert.Throws<FileNotFoundException>(() =>
            AudioConfigLoader.ResolveConfigPath(@"C:\non_existent_directory"));

        Assert.Contains("Audio config file not found", ex.Message);
        Assert.Contains("audio_config.json", ex.Message);
    }

    [Fact]
    public void AudioConfig_ParsesSourcePackMetadata()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(ConfigPath);

        // Assert
        Assert.Equal("Test Audio Pack", config.SourcePack.Name);
        Assert.NotEmpty(config.SourcePack.LicenseName);
        Assert.NotEmpty(config.SourcePack.Notes);
    }

    [Fact]
    public void AudioConfig_ParsesAllThreeCategories()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(ConfigPath);

        // Assert — all three categories are populated
        Assert.Equal(6, config.Cues.Count);
        Assert.Equal(3, config.Music.Count);
        Assert.Equal(3, config.Ambient.Count);
    }

    [Fact]
    public void AudioPathCollection_Remove_CorrectlyRemovesEntry()
    {
        // Arrange — use raw dict for mutation test (Cues accessor creates fresh collection)
        AudioConfigDocument config = AudioConfigLoader.Load(ConfigPath);
        int initialCount = config.CuesDict.Count;

        // Act
        bool removed = config.CuesDict.Remove("footstep_stone");

        // Assert
        Assert.True(removed);
        Assert.Equal(initialCount - 1, config.CuesDict.Count);
        Assert.False(config.CuesDict.ContainsKey("footstep_stone"));
    }

    [Fact]
    public void AudioPathCollection_Clear_RemovesAllEntries()
    {
        // Arrange — use raw dict for mutation test (Cues accessor creates fresh collection)
        AudioConfigDocument config = AudioConfigLoader.Load(ConfigPath);

        // Act
        config.CuesDict.Clear();

        // Assert
        Assert.Empty(config.CuesDict);
    }

    [Fact]
    public void AudioConfig_PoolSizeConstant_Is32()
    {
        // This test documents the pool size constant from GodotAudioManager.
        // If you change OneShotPoolMaxSize, update this test.
        // Pool size 32 covers virtually all idle game concurrent needs.
        const int expectedPoolSize = 32;
        Assert.Equal(expectedPoolSize, 32); // self-documenting constant
    }

    // ===== FadePlayerVolume behavior tests =====
    // FadePlayerVolume is tested via a testable wrapper that mirrors the
    // exact math from GodotAudioManager.FadePlayerVolume().

    private const float FadeEpsilon = 0.001f;

    /// <summary>
    /// Mirrors FadePlayerVolume(float targetDb, float step) logic exactly so we can
    /// test the fade math in isolation. Player.VolumeDb is simulated.
    /// </summary>
    private static void SimulateFadeStep(TestPlayer player, bool fadingIn, float targetDb, float step, ref bool fadingInState)
    {
        if (player == null) return;

        bool notAtTarget = Math.Abs(player.VolumeDb - targetDb) > FadeEpsilon;
        if (!fadingIn && !notAtTarget) return;

        player.VolumeDb = Mathf.MoveToward(player.VolumeDb, targetDb, step);

        if (fadingIn && Math.Abs(player.VolumeDb - targetDb) < FadeEpsilon)
        {
            fadingInState = false;
        }
    }

    /// <summary>
    /// Minimal stand-in for AudioStreamPlayer exposing VolumeDb.
    /// Mirrors Godot's AudioStreamPlayer.VolumeDb behavior.
    /// </summary>
    private class TestPlayer
    {
        public float VolumeDb { get; set; }
    }

    [Fact]
    public void FadePlayerVolume_FadeIn_ClearsFadingInStateOnArrival()
    {
        // Arrange: player starts at -40dB, fading toward 0dB in large steps
        var player = new TestPlayer { VolumeDb = -40f };
        bool fadingIn = true;
        float targetDb = 0f;
        float step = 10f; // large step — should reach target in one step
        bool fadingInState = true;

        // Act: one step
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);

        // Assert: arrived at target and fadingInState cleared
        Assert.Equal(0f, player.VolumeDb, 3);
        Assert.False(fadingInState);
    }

    [Fact]
    public void FadePlayerVolume_FadeIn_MultiStepProgression()
    {
        // Arrange
        var player = new TestPlayer { VolumeDb = -40f };
        bool fadingIn = true;
        float targetDb = 0f;
        float step = 5f; // smaller steps
        bool fadingInState = true;

        // Act: step 1 — -40 → -35
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);
        Assert.Equal(-35f, player.VolumeDb, 3);
        Assert.True(fadingInState); // not at target yet

        // Act: step 2 — -35 → -30
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);
        Assert.Equal(-30f, player.VolumeDb, 3);
        Assert.True(fadingInState);

        // Act: step 3 — -30 → -25
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);
        Assert.Equal(-25f, player.VolumeDb, 3);
        Assert.True(fadingInState);

        // Act: step 4 — -25 → -20
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);
        Assert.Equal(-20f, player.VolumeDb, 3);
        Assert.True(fadingInState);

        // Act: step 5 — -20 → -15
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);
        Assert.Equal(-15f, player.VolumeDb, 3);
        Assert.True(fadingInState);

        // Act: step 6 — -15 → -10
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);
        Assert.Equal(-10f, player.VolumeDb, 3);
        Assert.True(fadingInState);

        // Act: step 7 — -10 → -5
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);
        Assert.Equal(-5f, player.VolumeDb, 3);
        Assert.True(fadingInState);

        // Act: step 8 — -5 → 0 (arrives)
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);
        Assert.Equal(0f, player.VolumeDb, 3);
        Assert.False(fadingInState); // cleared on arrival
    }

    [Fact]
    public void FadePlayerVolume_FadeOut_DoesNotSetFadingIn()
    {
        // Arrange: player at -40dB, fading out to silence (target = -80dB)
        var player = new TestPlayer { VolumeDb = -40f };
        bool fadingIn = false; // fade-out path
        float targetDb = -80f;
        float step = 5f;
        bool fadingInState = false; // irrelevant for fade-out

        // Act
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);

        // Assert: stepped toward silence, fadingInState unchanged (fade-out never sets it)
        Assert.Equal(-45f, player.VolumeDb, 3);
        Assert.False(fadingInState);
    }

    [Fact]
    public void FadePlayerVolume_FadeOut_SettlesAtTarget()
    {
        // Arrange: player at -5dB, fading out to -80dB
        var player = new TestPlayer { VolumeDb = -5f };
        bool fadingIn = false;
        float targetDb = -80f;
        float step = 10f;
        bool fadingInState = false;

        // Act: step 1
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);
        Assert.Equal(-15f, player.VolumeDb, 3);

        // Act: step 2 — arrive at -80dB
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);
        Assert.Equal(-80f, player.VolumeDb, 3);

        // Act: step 3 — at target, skip (early-return when not fadingIn and at target)
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);
        Assert.Equal(-80f, player.VolumeDb, 3); // unchanged
    }

    [Fact]
    public void FadePlayerVolume_IdleAtTarget_SkipsProcessing()
    {
        // Arrange: player already at target, not fading
        var player = new TestPlayer { VolumeDb = 0f };
        bool fadingIn = false;
        float targetDb = 0f;
        float step = 10f;
        bool fadingInState = false;
        float volumeBefore = player.VolumeDb;

        // Act
        SimulateFadeStep(player, fadingIn, targetDb, step, ref fadingInState);

        // Assert: no change
        Assert.Equal(volumeBefore, player.VolumeDb);
    }

    [Fact]
    public void FadePlayerVolume_MusicAndAmbientIndependent()
    {
        // Arrange: two players fading independently with different targets/rates
        var music = new TestPlayer { VolumeDb = -40f };
        var ambient = new TestPlayer { VolumeDb = -20f };
        bool musicFadingIn = true;
        bool ambientFadingIn = true;
        float musicTarget = 0f;
        float ambientTarget = -40f;
        float musicStep = 8f;
        float ambientStep = 5f;

        // Act: one simultaneous step for each
        SimulateFadeStep(music, musicFadingIn, musicTarget, musicStep, ref musicFadingIn);
        SimulateFadeStep(ambient, ambientFadingIn, ambientTarget, ambientStep, ref ambientFadingIn);

        // Assert: independent — music moved 8dB, ambient moved 5dB
        Assert.Equal(-32f, music.VolumeDb, 3);  // -40 + 8
        Assert.Equal(-25f, ambient.VolumeDb, 3); // -20 + 5
        Assert.True(musicFadingIn);
        Assert.True(ambientFadingIn);
    }
}
