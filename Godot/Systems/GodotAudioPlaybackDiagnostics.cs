using Godot;
using MoonBark.AudioSystem.Core.Diagnostics;

namespace MoonBark.AudioSystem.Godot.Systems;

/// <summary>
/// Deduped Godot console logging for missing cues, failed loads, and failed playback.
/// </summary>
internal sealed class GodotAudioPlaybackDiagnostics
{
    private readonly HashSet<string> _loggedKeys = new(StringComparer.OrdinalIgnoreCase);

    public void Report(
        AudioPlaybackFailureKind kind,
        string category,
        string cueId,
        string detail,
        bool logEveryTime = false)
    {
        string message = AudioPlaybackLog.Format(kind, category, cueId, detail);
        string key = $"{kind}|{category}|{cueId}|{detail}";
        if (!logEveryTime && !_loggedKeys.Add(key))
            return;

        GD.PrintErr(message);
    }

    public void ClearLoggedKeys() => _loggedKeys.Clear();
}
