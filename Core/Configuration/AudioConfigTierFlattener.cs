namespace MoonBark.AudioSystem.Core.Configuration;

using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// Flattens nested tier cue trees (active_interactions, ambient_automation, etc.) into flat cue id maps.
/// </summary>
public static class AudioConfigTierFlattener
{
    public static void MergeTierPathsInto(
        Dictionary<string, string> cues,
        Dictionary<string, string> ambient,
        JsonElement tiersRoot)
    {
        if (tiersRoot.ValueKind != JsonValueKind.Object)
            return;

        foreach (JsonProperty tier in tiersRoot.EnumerateObject())
        {
            if (tier.Value.ValueKind != JsonValueKind.Object)
                continue;

            foreach (JsonProperty group in tier.Value.EnumerateObject())
            {
                if (group.Name.Equals("description", StringComparison.OrdinalIgnoreCase))
                    continue;

                bool toAmbient = group.Name.Contains("loop", StringComparison.OrdinalIgnoreCase);
                Walk(group.Value, cues, ambient, toAmbient);
            }
        }
    }

    private static void Walk(JsonElement node, Dictionary<string, string> cues, Dictionary<string, string> ambient, bool toAmbient)
    {
        switch (node.ValueKind)
        {
            case JsonValueKind.String:
                return;
            case JsonValueKind.Object:
                foreach (JsonProperty prop in node.EnumerateObject())
                {
                    if (prop.Name.Equals("description", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        string? path = prop.Value.GetString();
                        if (string.IsNullOrWhiteSpace(path) || !LooksLikeAudioPath(path))
                            continue;

                        if (toAmbient || path.Contains("/ambient/", StringComparison.OrdinalIgnoreCase))
                            ambient[prop.Name] = path;
                        else
                            cues[prop.Name] = path;
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        Walk(prop.Value, cues, ambient, toAmbient);
                    }
                }

                break;
        }
    }

    private static bool LooksLikeAudioPath(string path) =>
        path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase);
}
