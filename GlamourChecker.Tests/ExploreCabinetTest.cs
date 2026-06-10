using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace GlamourChecker.Tests;

public class ExploreMirageManagerTest
{
    private readonly ITestOutputHelper _output;

    public ExploreMirageManagerTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DumpMirageManager()
    {
        var type = typeof(MirageManager);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var f in fields)
        {
            _output.WriteLine($"{f.FieldType.Name} {f.Name}");
        }
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var p in props)
        {
            _output.WriteLine($"{p.PropertyType.Name} {p.Name}");
        }
    }
}
