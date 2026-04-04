namespace AudioSystem.Tests;

using AudioSystem.Core.Configuration;
using System.IO;
using Xunit;

public class AudioConfigLoaderTests
{
    private static readonly string FixturePath = Path.Combine(
        AppContext.BaseDirectory, "Fixtures", "audio_config.json");

    [Fact]
    public void Load_ValidConfig_ReturnsDocument()
    {
        // Arrange & Act
        AudioConfigDocument config = AudioConfigLoader.Load(FixturePath);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("Test Audio Pack", config.SourcePack.Name);
        Assert.Equal("test_fixtures", config.AudioRootPath);
    }

    [Fact]
    public void Load_ValidConfig_ParsesCues()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(FixturePath);

        // Assert
        Assert.Equal(6, config.Cues.Count);
        Assert.True(config.Cues.ContainsCue("footstep_stone"));
        Assert.True(config.Cues.ContainsCue("coin_pickup"));
    }

    [Fact]
    public void Load_ValidConfig_ParsesMusic()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(FixturePath);

        // Assert
        Assert.Equal(3, config.Music.Count);
        Assert.True(config.Music.ContainsCue("main_theme"));
        Assert.True(config.Music.ContainsCue("peaceful_morning"));
    }

    [Fact]
    public void Load_ValidConfig_ParsesAmbient()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(FixturePath);

        // Assert
        Assert.Equal(3, config.Ambient.Count);
        Assert.True(config.Ambient.ContainsCue("forest_day"));
        Assert.True(config.Ambient.ContainsCue("rain_heavy"));
    }

    [Fact]
    public void Load_ValidConfig_CaseInsensitiveLookup()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(FixturePath);

        // Assert — AudioPathCollection is case-insensitive
        Assert.True(config.Cues.ContainsCue("FOOTSTEP_STONE"));
        Assert.True(config.Cues.ContainsCue("Footstep_Stone"));
    }

    [Fact]
    public void Load_ValidConfig_TryGetPathReturnsCorrectRelativePaths()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(FixturePath);

        // Assert
        Assert.True(config.Cues.TryGetPath("footstep_stone", out string? path));
        Assert.Equal("sfx/footstep_stone.ogg", path);
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() =>
            AudioConfigLoader.Load("non_existent_path.json"));
    }

    [Fact]
    public void ResolveAudioRootPath_RelativeRoot_ResolvedAgainstConfigDir()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(FixturePath);

        // Act
        string rootPath = AudioConfigLoader.ResolveAudioRootPath(config, FixturePath);

        // Assert
        string expected = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(FixturePath)!, "test_fixtures"));
        Assert.Equal(expected, rootPath);
    }

    [Fact]
    public void BuildAbsolutePathMap_RelativePaths_ExpandedToAbsolute()
    {
        // Arrange
        AudioConfigDocument config = AudioConfigLoader.Load(FixturePath);

        // Act
        AudioPathCollection absoluteCues = AudioConfigLoader.BuildAbsolutePathMap(
            config, config.Cues, FixturePath);

        // Assert
        Assert.True(absoluteCues.TryGetPath("footstep_stone", out string? absPath));
        Assert.True(Path.IsPathRooted(absPath!), "Expected absolute path");
    }

    [Fact]
    public void BuildAbsolutePathMap_ResPaths_ReturnedAsIs()
    {
        // Arrange
        AudioConfigDocument config = new()
        {
            AudioRootPath = string.Empty,
            CuesDict = new Dictionary<string, string>
            {
                ["test_cue"] = "res://assets/audio/sfx/test.ogg"
            }
        };

        // Act
        AudioPathCollection result = AudioConfigLoader.BuildAbsolutePathMap(
            config, config.Cues, FixturePath);

        // Assert
        Assert.True(result.TryGetPath("test_cue", out string? path));
        Assert.Equal("res://assets/audio/sfx/test.ogg", path);
    }
}
