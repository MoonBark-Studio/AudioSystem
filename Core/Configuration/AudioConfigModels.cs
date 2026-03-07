namespace AudioSystem.Core.Configuration;

using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

/// <summary>
/// Represents the root audio configuration document loaded from YAML.
/// </summary>
public sealed class AudioConfigDocument
{
    /// <summary>
    /// Gets or sets the source pack metadata for the configured audio assets.
    /// </summary>
    public AudioSourceConfig SourcePack { get; set; } = new();

    /// <summary>
    /// Gets or sets the root path containing the configured audio assets.
    /// </summary>
    public string AudioRootPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cue mappings used for YAML deserialization.
    /// </summary>
    [YamlMember(Alias = "cues")]
    public Dictionary<string, string> CuesDict { get; set; } = new();

    /// <summary>
    /// Gets or sets the music mappings used for YAML deserialization.
    /// </summary>
    [YamlMember(Alias = "music")]
    public Dictionary<string, string> MusicDict { get; set; } = new();

    /// <summary>
    /// Gets or sets the ambient mappings used for YAML deserialization.
    /// </summary>
    [YamlMember(Alias = "ambient")]
    public Dictionary<string, string> AmbientDict { get; set; } = new();

    /// <summary>
    /// Gets the type-safe cue mappings.
    /// </summary>
    public AudioPathCollection Cues => AudioPathCollection.FromDictionary(CuesDict);

    /// <summary>
    /// Gets the type-safe music mappings.
    /// </summary>
    public AudioPathCollection Music => AudioPathCollection.FromDictionary(MusicDict);

    /// <summary>
    /// Gets the type-safe ambient mappings.
    /// </summary>
    public AudioPathCollection Ambient => AudioPathCollection.FromDictionary(AmbientDict);
}

/// <summary>
/// Represents a case-insensitive collection of audio cue identifiers and their paths.
/// </summary>
public sealed class AudioPathCollection
{
    private readonly Dictionary<string, string> _paths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets all cue identifiers in the collection.
    /// </summary>
    public IReadOnlyCollection<string> CueIds => _paths.Keys;

    /// <summary>
    /// Gets the number of mappings in the collection.
    /// </summary>
    public int Count => _paths.Count;

    /// <summary>
    /// Adds or updates a cue path mapping.
    /// </summary>
    /// <param name="cueId">The cue identifier.</param>
    /// <param name="filePath">The configured file path.</param>
    public void Add(string cueId, string filePath)
    {
        if (string.IsNullOrWhiteSpace(cueId))
        {
            throw new ArgumentException("Cue ID cannot be null or whitespace.", nameof(cueId));
        }

        _paths[cueId] = filePath ?? string.Empty;
    }

    /// <summary>
    /// Tries to resolve the configured path for a cue identifier.
    /// </summary>
    /// <param name="cueId">The cue identifier.</param>
    /// <param name="filePath">The resolved file path when found.</param>
    /// <returns><see langword="true"/> when a mapping exists; otherwise <see langword="false"/>.</returns>
    public bool TryGetPath(string cueId, out string filePath)
    {
        return _paths.TryGetValue(cueId, out filePath!);
    }

    /// <summary>
    /// Checks whether the collection contains a cue identifier.
    /// </summary>
    /// <param name="cueId">The cue identifier to check.</param>
    /// <returns><see langword="true"/> when the cue exists; otherwise <see langword="false"/>.</returns>
    public bool ContainsCue(string cueId)
    {
        return _paths.ContainsKey(cueId);
    }

    /// <summary>
    /// Removes a cue mapping.
    /// </summary>
    /// <param name="cueId">The cue identifier to remove.</param>
    /// <returns><see langword="true"/> if the cue was removed; otherwise <see langword="false"/>.</returns>
    public bool Remove(string cueId)
    {
        return _paths.Remove(cueId);
    }

    /// <summary>
    /// Clears all mappings from the collection.
    /// </summary>
    public void Clear()
    {
        _paths.Clear();
    }

    /// <summary>
    /// Creates a copy of the internal mapping dictionary.
    /// </summary>
    /// <returns>A new dictionary containing the configured mappings.</returns>
    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>(_paths, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a path collection from an existing dictionary.
    /// </summary>
    /// <param name="dictionary">The source dictionary.</param>
    /// <returns>A populated <see cref="AudioPathCollection"/>.</returns>
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

/// <summary>
/// Represents metadata describing the source and licensing of an audio pack.
/// </summary>
public sealed class AudioSourceConfig
{
    /// <summary>
    /// Gets or sets the source pack display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source URL for the pack.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the license name.
    /// </summary>
    public string LicenseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the short license summary.
    /// </summary>
    public string LicenseSummary { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets implementation notes for the pack.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}
