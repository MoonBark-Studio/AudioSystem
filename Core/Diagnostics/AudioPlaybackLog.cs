namespace MoonBark.AudioSystem.Core.Diagnostics;

/// <summary>Failure reasons for audio cue resolution, load, and playback.</summary>
public enum AudioPlaybackFailureKind
{
    InvalidCueId,
    CueNotRegistered,
    AssetNotFound,
    LoadFailed,
    PlayerNotReady,
    PoolExhausted,
    PlayRejected,
}

/// <summary>
/// Engine-agnostic log message formatting for audio playback diagnostics.
/// Godot (and other hosts) print these via their native logger.
/// </summary>
public static class AudioPlaybackLog
{
    public static string Format(AudioPlaybackFailureKind kind, string category, string cueId, string detail)
    {
        string safeCue = string.IsNullOrWhiteSpace(cueId) ? "(empty)" : cueId;
        string safeCategory = string.IsNullOrWhiteSpace(category) ? "audio" : category;
        return kind switch
        {
            AudioPlaybackFailureKind.InvalidCueId =>
                $"[AudioSystem] Cannot play {safeCategory}: cue id is missing or whitespace.",
            AudioPlaybackFailureKind.CueNotRegistered =>
                $"[AudioSystem] Cannot play {safeCategory} cue '{safeCue}': not registered in audio_config.",
            AudioPlaybackFailureKind.AssetNotFound =>
                $"[AudioSystem] Cannot play {safeCategory} cue '{safeCue}': {detail}",
            AudioPlaybackFailureKind.LoadFailed =>
                $"[AudioSystem] Failed to load {safeCategory} cue '{safeCue}': {detail}",
            AudioPlaybackFailureKind.PlayerNotReady =>
                $"[AudioSystem] Cannot play {safeCategory} cue '{safeCue}': {detail}",
            AudioPlaybackFailureKind.PoolExhausted =>
                $"[AudioSystem] Cannot play {safeCategory} cue '{safeCue}': {detail}",
            AudioPlaybackFailureKind.PlayRejected =>
                $"[AudioSystem] Failed to play {safeCategory} cue '{safeCue}': {detail}",
            _ => $"[AudioSystem] Audio failure ({kind}) for {safeCategory} cue '{safeCue}': {detail}",
        };
    }

    public static string AssetNotFoundDetail(string path) =>
        $"audio file not found at '{path}'.";

    public static string LoadReturnedNullDetail(string path) =>
        $"loader returned null for '{path}'.";

    public static string PoolExhaustedDetail(int poolSize) =>
        $"one-shot pool is full ({poolSize} slots); sound was dropped.";
}
