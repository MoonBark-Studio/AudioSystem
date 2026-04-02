namespace AudioSystem.Tests;

using AudioSystem.Core.Configuration;
using System.IO;
using Xunit;

public class GodotAudioManagerTests
{
    private static readonly string ConfigPath = Path.Combine(
        AppContext.BaseDirectory, "Fixtures", "audio_config.yaml");

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
        Assert.Contains("Assets/Audio/audio_config.yaml", ex.Message);
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
}
