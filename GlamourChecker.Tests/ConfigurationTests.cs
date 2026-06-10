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
}
