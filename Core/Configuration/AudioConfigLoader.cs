namespace MoonBark.AudioSystem.Core.Configuration;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Loads and resolves JSON-backed audio configuration documents.
/// Supports both absolute resource paths (res://) and relative file paths.
/// YAML loading has been removed â€” config files should use JSON format.
/// </summary>
public static readonly class AudioConfigLoader
    private const string RelativeAudioConfigPath = "Assets/Audio/audio_config.json";

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
            candidateRoots.Add(Path.GetFullPath(startDirectory));
        }

        candidateRoots.Add(AppContext.BaseDirectory);
        candidateRoots.Add(Environment.CurrentDirectory);

        foreach (string root in candidateRoots)
        {
            string? current = Path.GetFullPath(root);
            while (!string.IsNullOrWhiteSpace(current))
            {
                string candidate = Path.Combine(current, RelativeAudioConfigPath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                DirectoryInfo? parent = Directory.GetParent(current);
                if (parent is null)
                {
                    break;
                }

                current = parent.FullName;
            }
        }

        throw new FileNotFoundException(
            $"Audio config file not found. Expected to locate {RelativeAudioConfigPath} from {startDirectory ?? AppContext.BaseDirectory}.");
    }

    /// <summary>
    /// Loads and deserializes an audio configuration document from JSON.
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

        string json = File.ReadAllText(resolvedPath);

        // Handle BOM
        if (json.Length > 0 && json[0] == '\uFEFF')
        {
            json = json[1..];
        }

        AudioConfigDocument? config = JsonSerializer.Deserialize<AudioConfigDocument>(json, JsonOptions);
        if (config is null)
        {
            throw new InvalidOperationException($"Audio config at {resolvedPath} is missing required mappings.");
        }

        return config;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Resolves the absolute audio root path for a configuration document.
    /// </summary>
    /// <param name="config">The audio configuration document.</param>
    /// <param name="configPath">An optional explicit config path.</param>
    /// <returns>The absolute audio root path.</returns>
    public static string ResolveAudioRootPath(AudioConfigDocument config, string? configPath = null)
    {
        string resolvedConfigPath = configPath ?? ResolveConfigPath();
        return ResolveConfigRootPath(config.AudioRootPath, resolvedConfigPath);
    }

    /// <summary>
    /// Resolves the root path from a configuration object. If the root path is relative,
    /// it is resolved against the directory containing the config file.
    /// </summary>
    /// <param name="rootPath">The root path string from the config (may be relative or absolute).</param>
    /// <param name="configFilePath">The full path to the config file (used to resolve relative roots).</param>
    /// <returns>The absolute resolved root path.</returns>
    public static string ResolveConfigRootPath(string rootPath, string configFilePath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return Path.GetDirectoryName(configFilePath) ?? AppContext.BaseDirectory;
        }

        if (Path.IsPathRooted(rootPath))
        {
            return Path.GetFullPath(rootPath);
        }

        string configDirectory = Path.GetDirectoryName(configFilePath) ?? AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(configDirectory, NormalizeConfigPath(rootPath)));
    }

    /// <summary>
    /// Builds a collection containing absolute paths for all configured audio entries.
    /// Supports res:// resource paths (returned as-is) and relative file paths
    /// (resolved against the audio root path from the config).
    /// </summary>
    /// <param name="config">The source configuration.</param>
    /// <param name="relativePaths">The relative path collection to expand.</param>
    /// <param name="configPath">An optional explicit config path.</param>
    /// <returns>A collection with absolute resolved paths.</returns>
    public static AudioPathCollection BuildAbsolutePathMap(
        AudioConfigDocument config,
        AudioPathCollection relativePaths,
        string? configPath = null)
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

            // res:// and user:// paths are Godot resource paths â€” return as-is
            if (IsGodotResourcePath(relativePath))
            {
                collection.Add(cueId, relativePath);
                continue;
            }

            string absolutePath = Path.IsPathRooted(relativePath)
                ? Path.GetFullPath(relativePath)
                : Path.GetFullPath(Path.Combine(audioRootPath, NormalizeConfigPath(relativePath)));

            collection.Add(cueId, absolutePath);
        }

        return collection;
    }

    private static bool IsGodotResourcePath(string path)
    {
        return path.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeConfigPath(string path)
    {
        return path.Replace('/', Path.DirectorySeparatorChar);
    }
}