using Lumina.Excel.Sheets;
using Xunit;
using Xunit.Abstractions;

namespace GlamourChecker.Tests;

public class LuminaTester
{
    private readonly ITestOutputHelper _output;

    public LuminaTester(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DumpItemProperties()
    {
        _output.WriteLine("Item Properties:");
        foreach (var p in typeof(Item).GetProperties())
        {
            if (p.Name.Contains("BaseParam"))
            {
                _output.WriteLine($"{p.Name} : {p.PropertyType.Name}");
            }
        }

        _output.WriteLine("ClassJob Properties:");
        foreach (var p in typeof(ClassJob).GetProperties())
        {
            if (p.Name.Contains("Modifier") || p.Name.Contains("Role"))
            {
                _output.WriteLine($"{p.Name} : {p.PropertyType.Name}");
            }
        }
    }
}
