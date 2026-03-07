namespace AudioSystem.Core.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Loads and resolves YAML-backed audio configuration documents.
/// </summary>
public static class AudioConfigLoader
{
    private const string RelativeAudioConfigPath = "Assets/Audio/audio_config.yaml";
    private const string LocalAudioConfigPath = "Assets/Audio/audio_config.yaml";

    /// <summary>
    /// Resolves the audio configuration path by walking up from the supplied or default roots.
    /// </summary>
    /// <param name="startDirectory">An optional starting directory.</param>
    /// <returns>The resolved configuration file path.</returns>
    public static string ResolveConfigPath(string? startDirectory = null)
    {
        List<string> candidateRoots = new();

        if (!string.IsNullOrWhiteSpace(startDirectory))
        {
            candidateRoots.Add(startDirectory);
        }
        else
        {
            candidateRoots.Add(AppContext.BaseDirectory);
            candidateRoots.Add(Environment.CurrentDirectory);
        }

        foreach (string root in candidateRoots)
        {
            string localCandidate = Path.Combine(Path.GetFullPath(root), LocalAudioConfigPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(localCandidate))
            {
                return localCandidate;
            }

            string? current = Path.GetFullPath(root);
            while (!string.IsNullOrWhiteSpace(current))
            {
                string candidate = Path.Combine(current, RelativeAudioConfigPath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                DirectoryInfo? parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }
        }

        throw new FileNotFoundException($"Audio config file not found. Expected to locate {RelativeAudioConfigPath} from {startDirectory ?? AppContext.BaseDirectory}.");
    }

    /// <summary>
    /// Loads and deserializes an audio configuration document.
    /// </summary>
    /// <param name="configPath">An optional explicit config path.</param>
    /// <returns>The parsed audio configuration document.</returns>
    public static AudioConfigDocument Load(string? configPath = null)
    {
        string resolvedPath = configPath ?? ResolveConfigPath();
        if (!File.Exists(resolvedPath))
        {
            throw new FileNotFoundException($"Audio config file not found: {resolvedPath}", resolvedPath);
        }

        string yaml = File.ReadAllText(resolvedPath);
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        AudioConfigDocument? config = deserializer.Deserialize<AudioConfigDocument>(yaml);
        if (config is null || string.IsNullOrWhiteSpace(config.AudioRootPath))
        {
            throw new InvalidOperationException($"Audio config at {resolvedPath} is missing required mappings.");
        }

        return config;
    }

    /// <summary>
    /// Resolves the absolute audio root path for a configuration document.
    /// </summary>
    /// <param name="config">The audio configuration document.</param>
    /// <param name="configPath">An optional explicit config path.</param>
    /// <returns>The absolute audio root path.</returns>
    public static string ResolveAudioRootPath(AudioConfigDocument config, string? configPath = null)
    {
        if (Path.IsPathRooted(config.AudioRootPath))
        {
            return Path.GetFullPath(config.AudioRootPath);
        }

        string resolvedConfigPath = configPath ?? ResolveConfigPath();
        string configDirectory = Path.GetDirectoryName(resolvedConfigPath) ?? AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(configDirectory, NormalizeConfigPath(config.AudioRootPath)));
    }

    /// <summary>
    /// Builds a collection containing absolute paths for all configured audio entries.
    /// </summary>
    /// <param name="config">The source configuration.</param>
    /// <param name="relativePaths">The relative path collection to expand.</param>
    /// <param name="configPath">An optional explicit config path.</param>
    /// <returns>A collection with absolute resolved paths.</returns>
    public static AudioPathCollection BuildAbsolutePathMap(AudioConfigDocument config, AudioPathCollection relativePaths, string? configPath = null)
    {
        string audioRootPath = ResolveAudioRootPath(config, configPath);
        AudioPathCollection collection = new();

        foreach (string cueId in relativePaths.CueIds)
        {
            if (!relativePaths.TryGetPath(cueId, out string? relativePath))
            {
                collection.Add(cueId, string.Empty);
                continue;
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                collection.Add(cueId, string.Empty);
                continue;
            }

            string absolutePath = Path.IsPathRooted(relativePath)
                ? Path.GetFullPath(relativePath)
                : Path.GetFullPath(Path.Combine(audioRootPath, NormalizeConfigPath(relativePath)));

            collection.Add(cueId, absolutePath);
        }

        return collection;
    }

    /// <summary>
    /// Normalizes a configuration path to the current platform separator.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    public static string NormalizeConfigPath(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar);
    }
}
