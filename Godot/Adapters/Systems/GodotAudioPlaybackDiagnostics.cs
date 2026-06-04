using MoonBark.AudioSystem.Core.Diagnostics;
using MoonBark.Framework.Logging;

namespace MoonBark.AudioSystem.Godot.Adapters.Systems;

internal sealed class GodotAudioPlaybackDiagnostics
{
    private readonly HashSet<string> _loggedKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger? _logger;

    internal GodotAudioPlaybackDiagnostics(ILogger? logger)
    {
        _logger = logger;
    }

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

        switch (kind)
        {
            case AudioPlaybackFailureKind.PoolExhausted:
                _logger?.Info(message);
                break;
            case AudioPlaybackFailureKind.CueNotRegistered:
            case AudioPlaybackFailureKind.InvalidCueId:
                _logger?.Debug(message);
                break;
            default:
                _logger?.Warning(message);
                break;
        }
    }

    public void ClearLoggedKeys() => _loggedKeys.Clear();
}