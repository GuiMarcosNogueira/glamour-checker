using System;
using Xunit;
using FFXIVClientStructs.FFXIV.Client.Game;
using Xunit.Abstractions;

namespace GlamourChecker.Tests;

public class StructSizeTest {
    private readonly ITestOutputHelper _output;

    public StructSizeTest(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public unsafe void CheckMirageManager() {
        _output.WriteLine($"MirageManager size: {sizeof(MirageManager)}");
        MirageManager* manager = null;
        // Since we can't instantiate it, we just want to know the size of the array.
        // But PrismBoxItemIds is a fixed buffer. Let's see its length.
        // Unfortunately fixed buffer lengths aren't directly available without an instance.
        // We can just dump reflection info!
        var type = typeof(MirageManager);
        var field = type.GetField("PrismBoxItemIds");
        if (field != null) {
            _output.WriteLine($"PrismBoxItemIds Field Type: {field.FieldType}");
        }
    }
}
