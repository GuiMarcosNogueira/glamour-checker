using GlamourChecker.Core;
using Xunit;

namespace GlamourChecker.Tests;

public class ModelScannerTests {
    [Fact]
    public void GetModelId_ReturnsZero_WhenItemNotFound() {
        var scanner = new ModelScanner(_ => null);
        var id = scanner.GetModelId(999);
        Assert.Equal(0ul, id);
    }
    
    [Fact]
    public void GetModelId_ReturnsCachedValue_OnSecondCall() {
        int callCount = 0;
        var scanner = new ModelScanner(_ => {
            callCount++;
            return null;
        });
        
        scanner.GetModelId(123);
        scanner.GetModelId(123);
        
        Assert.Equal(1, callCount); // Second call should hit cache
    }
    
    [Fact]
    public void GetModelId_UsesModelMain_WhenArmor() {
        var scanner = new ModelScanner(_ => new ItemModelData {
            ModelMain = 12345ul,
            EquipSlotCategory = 3, // Head
            DyeCount = 0
        });
        
        var id = scanner.GetModelId(1);
        var expected = 12345ul | (3ul << 48);
        Assert.Equal(expected, id);
    }

    [Fact]
    public void GetModelId_RespectsColor_ForAllItems() {
        var scanner = new ModelScanner(id => {
            if (id == 1) return new ItemModelData { ModelMain = 0x0000_1000_0000_0000 | 0x01, EquipSlotCategory = 3, DyeCount = 1 }; // Dyeable but has color 1000
            if (id == 2) return new ItemModelData { ModelMain = 0x0000_2000_0000_0000 | 0x01, EquipSlotCategory = 3, DyeCount = 1 }; // Same item but color 2000
            return null;
        });

        var id1 = scanner.GetModelId(1);
        var id2 = scanner.GetModelId(2);

        Assert.NotEqual(id1, id2); // Colors should make exact models different
    }

    [Fact]
    public void GetModelId_UsesModelSub_WhenWeapon() {
        var scanner = new ModelScanner(_ => new ItemModelData {
            ModelMain = 11111ul,
            ModelSub = 22222ul,
            EquipSlotCategory = 1, // Main hand
            DyeCount = 0
        });
        
        var id = scanner.GetModelId(1);
        var subSignature = 22222ul;
        var visualSignature = 11111ul ^ ((subSignature << 13) | (subSignature >> 51));
        var expected = visualSignature | (1ul << 48);
        Assert.Equal(expected, id);
    }
    
    [Fact]
    public void GetModelId_UsesModelMain_WhenWeaponWithoutModelSub() {
        var scanner = new ModelScanner(_ => new ItemModelData {
            ModelMain = 11111ul,
            ModelSub = 0,
            EquipSlotCategory = 1, // Main hand
            DyeCount = 0
        });
        
        var id = scanner.GetModelId(1);
        var expected = 11111ul | (1ul << 48);
        Assert.Equal(expected, id);
    }
    
    [Fact]
    public void GetModelId_ReturnsZero_WhenCategoryIsZero() {
        var scanner = new ModelScanner(_ => new ItemModelData {
            ModelMain = 11111ul,
            EquipSlotCategory = 0, // Not gear
        });
        
        var id = scanner.GetModelId(1);
        Assert.Equal(0ul, id);
    }
    
    [Fact]
    public void GetModelId_UsesModelSub_WhenNonDyeableWeapon() {
        var scanner = new ModelScanner(_ => new ItemModelData {
            ModelMain = 11111ul,
            ModelSub = 22222ul,
            EquipSlotCategory = 1, 
            DyeCount = 0
        });
        
        var id = scanner.GetModelId(1);
        var subSignature = 22222ul;
        var visualSignature = 11111ul ^ ((subSignature << 13) | (subSignature >> 51));
        var expected = visualSignature | (1ul << 48);
        Assert.Equal(expected, id);
    }
}
