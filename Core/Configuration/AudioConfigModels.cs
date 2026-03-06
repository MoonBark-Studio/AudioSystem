namespace AudioSystem.Core.Configuration;

using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

public sealed class AudioConfigDocument
{
    public AudioSourceConfig SourcePack { get; set; } = new();
    public string AudioRootPath { get; set; } = string.Empty;

    [YamlMember(Alias = "cues")]
    public Dictionary<string, string> CuesDict { get; set; } = new();

    [YamlMember(Alias = "music")]
    public Dictionary<string, string> MusicDict { get; set; } = new();

    [YamlMember(Alias = "ambient")]
    public Dictionary<string, string> AmbientDict { get; set; } = new();

    public AudioPathCollection Cues => AudioPathCollection.FromDictionary(CuesDict);
    public AudioPathCollection Music => AudioPathCollection.FromDictionary(MusicDict);
    public AudioPathCollection Ambient => AudioPathCollection.FromDictionary(AmbientDict);
}

public sealed class AudioPathCollection
{
    private readonly Dictionary<string, string> _paths = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> CueIds => _paths.Keys;
    public int Count => _paths.Count;

    public void Add(string cueId, string filePath)
    {
        if (string.IsNullOrWhiteSpace(cueId))
        {
            throw new ArgumentException("Cue ID cannot be null or whitespace.", nameof(cueId));
        }

        _paths[cueId] = filePath ?? string.Empty;
    }

    public bool TryGetPath(string cueId, out string filePath)
    {
        return _paths.TryGetValue(cueId, out filePath!);
    }

    public bool ContainsCue(string cueId)
    {
        return _paths.ContainsKey(cueId);
    }

    public bool Remove(string cueId)
    {
        return _paths.Remove(cueId);
    }

    public void Clear()
    {
        _paths.Clear();
    }

    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>(_paths, StringComparer.OrdinalIgnoreCase);
    }

    public static AudioPathCollection FromDictionary(Dictionary<string, string> dictionary)
    {
        AudioPathCollection collection = new();
        foreach (KeyValuePair<string, string> pair in dictionary)
        {
            collection.Add(pair.Key, pair.Value);
        }

        return collection;
    }
}

public sealed class AudioSourceConfig
{
    public string Name { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string LicenseName { get; set; } = string.Empty;
    public string LicenseSummary { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
