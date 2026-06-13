using System.Collections.Generic;
using GlamourChecker;
using Xunit;

namespace GlamourChecker.Tests;

public class ConfigurationTests
{
    [Fact]
    public void HasModel_ReturnsTrue_WhenInDresser()
    {
        var config = new Configuration();
        config.DresserModelIds.Add(12345ul);
        Assert.True(config.HasModel(12345ul));
    }

    [Fact]
    public void HasModel_ReturnsTrue_WhenInArmoire()
    {
        var config = new Configuration();
        config.ArmoireModelIds.Add(54321ul);
        Assert.True(config.HasModel(54321ul));
    }

    [Fact]
    public void HasModel_ReturnsFalse_WhenNotInDresserOrArmoire()
    {
        var config = new Configuration();
        Assert.False(config.HasModel(12345ul));
    }

    [Fact]
    public void Save_WorksWhenPluginInterfaceIsNull()
    {
        var config = new Configuration();
        config.Save(); // Should not throw
    }

    [Fact]
    public void HasExactItem_ReturnsTrue_WhenItemMatchesModel()
    {
        var config = new Configuration();
        config.DresserItemsByModel[100] = new List<uint> { 555 };

        Assert.True(config.HasExactItem(555, 100));
        Assert.False(config.HasExactItem(556, 100));
        Assert.False(config.HasExactItem(555, 101));
    }

    [Fact]
    public void GetStoredItemIdForModel_ReturnsCorrectId()
    {
        var config = new Configuration();
        config.DresserItemsByModel[100] = new List<uint> { 555, 556 };
        config.ArmoireItemsBySharedModel[200] = new List<uint> { 777 };

        Assert.Equal(555u, config.GetStoredItemIdForModel(100, 0));
        Assert.Equal(777u, config.GetStoredItemIdForModel(0, 200));
        Assert.Equal(0u, config.GetStoredItemIdForModel(999, 999));
    }

    [Fact]
    public void HasSeenTutorial_DefaultIsFalse()
    {
        var config = new Configuration();
        Assert.False(config.HasSeenTutorial);

        config.HasSeenTutorial = true;
        Assert.True(config.HasSeenTutorial);
    }
}
