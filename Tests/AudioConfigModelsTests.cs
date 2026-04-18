namespace MoonBark.AudioSystem.Tests;

using MoonBark.AudioSystem.Core.Configuration;
using Xunit;

public class AudioPathCollectionTests
{
    [Fact]
    public void Add_ValidCueAndPath_AddsSuccessfully()
    {
        var collection = new AudioPathCollection();

        collection.Add("test_cue", "path/to/sound.ogg");

        Assert.Equal(1, collection.Count);
        Assert.True(collection.ContainsCue("test_cue"));
    }

    [Fact]
    public void Add_WithNullCueId_ThrowsArgumentException()
    {
        var collection = new AudioPathCollection();

        Assert.Throws<ArgumentException>(() => collection.Add(null!, "path/to/sound.ogg"));
    }

    [Fact]
    public void Add_WithEmptyCueId_ThrowsArgumentException()
    {
        var collection = new AudioPathCollection();

        Assert.Throws<ArgumentException>(() => collection.Add("", "path/to/sound.ogg"));
    }

    [Fact]
    public void Add_WithWhitespaceCueId_ThrowsArgumentException()
    {
        var collection = new AudioPathCollection();

        Assert.Throws<ArgumentException>(() => collection.Add("   ", "path/to/sound.ogg"));
    }

    [Fact]
    public void Add_WithNullPath_AddsEmptyString()
    {
        var collection = new AudioPathCollection();

        collection.Add("test_cue", null!);

        Assert.True(collection.TryGetPath("test_cue", out string? path));
        Assert.Equal(string.Empty, path);
    }

    [Fact]
    public void Add_DuplicateCueId_UpdatesPath()
    {
        var collection = new AudioPathCollection();

        collection.Add("test_cue", "first/path.ogg");
        collection.Add("test_cue", "second/path.ogg");

        Assert.Equal(1, collection.Count);
        Assert.True(collection.TryGetPath("test_cue", out string? path));
        Assert.Equal("second/path.ogg", path);
    }

    [Fact]
    public void Remove_ExistingCue_ReturnsTrue()
    {
        var collection = new AudioPathCollection();
        collection.Add("test_cue", "path.ogg");

        bool result = collection.Remove("test_cue");

        Assert.True(result);
        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void Remove_NonExistentCue_ReturnsFalse()
    {
        var collection = new AudioPathCollection();

        bool result = collection.Remove("nonexistent");

        Assert.False(result);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        var collection = new AudioPathCollection();
        collection.Add("cue1", "path1.ogg");
        collection.Add("cue2", "path2.ogg");

        collection.Clear();

        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void ContainsCue_ExistingCue_ReturnsTrue()
    {
        var collection = new AudioPathCollection();
        collection.Add("test_cue", "path.ogg");

        Assert.True(collection.ContainsCue("test_cue"));
    }

    [Fact]
    public void ContainsCue_NonExistentCue_ReturnsFalse()
    {
        var collection = new AudioPathCollection();

        Assert.False(collection.ContainsCue("nonexistent"));
    }

    [Fact]
    public void ContainsCue_CaseInsensitive_ReturnsTrue()
    {
        var collection = new AudioPathCollection();
        collection.Add("TestCue", "path.ogg");

        Assert.True(collection.ContainsCue("TESTCUE"));
        Assert.True(collection.ContainsCue("testcue"));
        Assert.True(collection.ContainsCue("TestCue"));
    }

    [Fact]
    public void TryGetPath_ExistingCue_ReturnsTrueAndPath()
    {
        var collection = new AudioPathCollection();
        collection.Add("test_cue", "my/path.ogg");

        bool result = collection.TryGetPath("test_cue", out string? path);

        Assert.True(result);
        Assert.Equal("my/path.ogg", path);
    }

    [Fact]
    public void TryGetPath_NonExistentCue_ReturnsFalse()
    {
        var collection = new AudioPathCollection();

        bool result = collection.TryGetPath("nonexistent", out string? path);

        Assert.False(result);
        Assert.Null(path);
    }

    [Fact]
    public void FromDictionary_ValidDictionary_CreatesPopulatedCollection()
    {
        var dictionary = new Dictionary<string, string>
        {
            ["cue1"] = "path1.ogg",
            ["cue2"] = "path2.ogg"
        };

        AudioPathCollection collection = AudioPathCollection.FromDictionary(dictionary);

        Assert.Equal(2, collection.Count);
        Assert.True(collection.ContainsCue("cue1"));
        Assert.True(collection.ContainsCue("cue2"));
    }

    [Fact]
    public void FromDictionary_EmptyDictionary_CreatesEmptyCollection()
    {
        var dictionary = new Dictionary<string, string>();

        AudioPathCollection collection = AudioPathCollection.FromDictionary(dictionary);

        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void ToDictionary_CreatesIndependentCopy()
    {
        var collection = new AudioPathCollection();
        collection.Add("cue1", "path1.ogg");
        collection.Add("cue2", "path2.ogg");

        Dictionary<string, string> dict = collection.ToDictionary();

        Assert.Equal(2, dict.Count);
        dict["cue1"] = "modified.ogg";
        Assert.Equal("path1.ogg", collection.ToDictionary()["cue1"]);
    }
}

public class AudioSourceConfigTests
{
    [Fact]
    public void DefaultConstructor_HasEmptyValues()
    {
        var config = new AudioSourceConfig();

        Assert.Equal(string.Empty, config.Name);
        Assert.Equal(string.Empty, config.SourceUrl);
        Assert.Equal(string.Empty, config.LicenseName);
        Assert.Equal(string.Empty, config.LicenseSummary);
        Assert.Equal(string.Empty, config.Notes);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        var config = new AudioSourceConfig
        {
            Name = "Test Pack",
            SourceUrl = "https://example.com/audio",
            LicenseName = "MIT",
            LicenseSummary = "Free to use",
            Notes = "Version 1.0"
        };

        Assert.Equal("Test Pack", config.Name);
        Assert.Equal("https://example.com/audio", config.SourceUrl);
        Assert.Equal("MIT", config.LicenseName);
        Assert.Equal("Free to use", config.LicenseSummary);
        Assert.Equal("Version 1.0", config.Notes);
    }
}

public class AudioConfigDocumentTests
{
    [Fact]
    public void DefaultConstructor_HasEmptyCollections()
    {
        var doc = new AudioConfigDocument();

        Assert.NotNull(doc.CuesDict);
        Assert.NotNull(doc.MusicDict);
        Assert.NotNull(doc.AmbientDict);
        Assert.Equal(0, doc.Cues.Count);
        Assert.Equal(0, doc.Music.Count);
        Assert.Equal(0, doc.Ambient.Count);
    }

    [Fact]
    public void Cues_ReturnsAudioPathCollection_FromDictionary()
    {
        var doc = new AudioConfigDocument
        {
            CuesDict = new Dictionary<string, string>
            {
                ["test_cue"] = "path.ogg"
            }
        };

        Assert.Equal(1, doc.Cues.Count);
        Assert.True(doc.Cues.ContainsCue("test_cue"));
    }

    [Fact]
    public void Cues_ReturnsNewCollectionEachAccess()
    {
        var doc = new AudioConfigDocument
        {
            CuesDict = new Dictionary<string, string>
            {
                ["cue1"] = "path1.ogg"
            }
        };

        var collection1 = doc.Cues;
        var collection2 = doc.Cues;

        Assert.NotSame(collection1, collection2);
        Assert.Equal(collection1.Count, collection2.Count);
    }

    [Fact]
    public void CuesDict_ModificationsVisibleThroughCues()
    {
        var doc = new AudioConfigDocument
        {
            CuesDict = new Dictionary<string, string>
            {
                ["cue1"] = "path1.ogg"
            }
        };

        doc.CuesDict["cue2"] = "path2.ogg";

        Assert.Equal(2, doc.Cues.Count);
        Assert.True(doc.Cues.ContainsCue("cue2"));
    }

    [Fact]
    public void Music_ReturnsAudioPathCollection_FromMusicDict()
    {
        var doc = new AudioConfigDocument
        {
            MusicDict = new Dictionary<string, string>
            {
                ["bgm"] = "music.ogg"
            }
        };

        Assert.Equal(1, doc.Music.Count);
        Assert.True(doc.Music.ContainsCue("bgm"));
    }

    [Fact]
    public void Ambient_ReturnsAudioPathCollection_FromAmbientDict()
    {
        var doc = new AudioConfigDocument
        {
            AmbientDict = new Dictionary<string, string>
            {
                ["rain"] = "ambient/rain.ogg"
            }
        };

        Assert.Equal(1, doc.Ambient.Count);
        Assert.True(doc.Ambient.ContainsCue("rain"));
    }
}
