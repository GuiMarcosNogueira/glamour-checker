using System.IO;
using GlamourChecker;
using Xunit;

namespace GlamourChecker.Tests;

public class LocTests
{
    [Fact]
    public void Setup_LoadsEnglishByDefault()
    {

        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "GlamourCheckerLocTests");
        var locDir = Path.Combine(tempDir, "loc");
        Directory.CreateDirectory(locDir);
        File.WriteAllText(Path.Combine(locDir, "en.json"), "{ \"TestKey\": \"TestValueEN\" }");

        // Act
        Loc.Setup(tempDir, "en");

        // Assert
        Assert.Equal("TestValueEN", Loc.Localize("TestKey", "Fallback"));

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void Setup_LoadsRequestedLanguage()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), "GlamourCheckerLocTests2");
        var locDir = Path.Combine(tempDir, "loc");
        Directory.CreateDirectory(locDir);
        File.WriteAllText(Path.Combine(locDir, "en.json"), "{ \"TestKey\": \"TestValueEN\" }");
        File.WriteAllText(Path.Combine(locDir, "pt.json"), "{ \"TestKey\": \"TestValuePT\" }");

        Loc.Setup(tempDir, "pt");

        Assert.Equal("TestValuePT", Loc.Localize("TestKey", "Fallback"));

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void Localize_ReturnsFallback_WhenKeyNotFound()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), "GlamourCheckerLocTests3");
        var locDir = Path.Combine(tempDir, "loc");
        Directory.CreateDirectory(locDir);
        File.WriteAllText(Path.Combine(locDir, "en.json"), "{ }");

        Loc.Setup(tempDir, "en");

        Assert.Equal("MyFallback", Loc.Localize("MissingKey", "MyFallback"));

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void Localize_ReturnsFallbackFromEnglish_WhenMissingInOtherLang()
    {

        var tempDir = Path.Combine(Path.GetTempPath(), "GlamourCheckerLocTests4");
        var locDir = Path.Combine(tempDir, "loc");
        Directory.CreateDirectory(locDir);
        File.WriteAllText(Path.Combine(locDir, "en.json"), "{ \"EnglishOnlyKey\": \"EnglishValue\" }");
        File.WriteAllText(Path.Combine(locDir, "pt.json"), "{ \"OtherKey\": \"OtherValue\" }");

        Loc.Setup(tempDir, "pt");

        Assert.Equal("EnglishValue", Loc.Localize("EnglishOnlyKey", "Fallback"));

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void GetAvailableLanguages_ReturnsValidList()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GlamourCheckerLocTests5");
        var locDir = Path.Combine(tempDir, "loc");
        Directory.CreateDirectory(locDir);
        File.WriteAllText(Path.Combine(locDir, "en.json"), "{ }");
        File.WriteAllText(Path.Combine(locDir, "pt-BR.json"), "{ }");

        var langs = Loc.GetAvailableLanguages(tempDir);

        Assert.Contains("default", langs);
        Assert.Contains("en", langs);
        Assert.Contains("pt-BR", langs);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void LoadLanguage_CatchesException_OnInvalidJson()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "GlamourCheckerLocTests6");
        var locDir = Path.Combine(tempDir, "loc");
        Directory.CreateDirectory(locDir);

        File.WriteAllText(Path.Combine(locDir, "pt.json"), "{ invalid json }");

        Loc.Setup(tempDir, "en");
        Loc.Setup(tempDir, "pt"); // Should catch exception silently

        // Assert: No exception thrown, and returns english keys or keys loaded before
        Assert.Equal("Fallback", Loc.Localize("SomeKey", "Fallback"));

        Directory.Delete(tempDir, true);
    }
}
