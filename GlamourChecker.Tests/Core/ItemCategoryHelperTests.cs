using Xunit;
using GlamourChecker.Core;

namespace GlamourChecker.Tests.Core;

public class ItemCategoryHelperTests
{
    [Theory]
    [InlineData(1, "Main Hand")]
    [InlineData(13, "Main Hand")]
    [InlineData(2, "Off Hand")]
    [InlineData(3, "Head")]
    [InlineData(4, "Body")]
    [InlineData(15, "Body")]
    [InlineData(5, "Hands")]
    [InlineData(7, "Legs")]
    [InlineData(8, "Feet")]
    [InlineData(9, "Ears")]
    [InlineData(10, "Neck")]
    [InlineData(11, "Wrists")]
    [InlineData(12, "Fingers")]
    [InlineData(99, "Other")]
    public void GetEquipSlotGroup_ReturnsExpectedGroup(uint rowId, string expectedGroupDefaultEn)
    {
        // Act
        var result = ItemCategoryHelper.GetEquipSlotGroup(rowId);

        // Assert
        // Since Loc.Localize is used, we'll verify it returns a non-null string.
        // If Loc is not initialized in the test, it returns the default English string.
        Assert.Equal(expectedGroupDefaultEn, result);
    }

    [Theory]
    [InlineData("Main Hand", 1)]
    [InlineData("Off Hand", 2)]
    [InlineData("Head", 3)]
    [InlineData("Body", 4)]
    [InlineData("Hands", 5)]
    [InlineData("Legs", 6)]
    [InlineData("Feet", 7)]
    [InlineData("Ears", 8)]
    [InlineData("Neck", 9)]
    [InlineData("Wrists", 10)]
    [InlineData("Fingers", 11)]
    [InlineData("Unknown Group", 99)]
    public void GetEquipSlotSortOrder_ReturnsExpectedOrder(string slotGroupDefaultEn, int expectedOrder)
    {
        // Act
        var result = ItemCategoryHelper.GetEquipSlotSortOrder(slotGroupDefaultEn);

        // Assert
        Assert.Equal(expectedOrder, result);
    }

    [Fact]
    public void GroupInventoryItems_ShouldGroupAndSortCorrectly()
    {
        // Arrange
        var items = new List<InventoryItemInfo>
        {
            new InventoryItemInfo { ItemId = 1, ModelId = 10 }, // Hand (Sort 5)
            new InventoryItemInfo { ItemId = 2, ModelId = 10 }, // Hand (Sort 5) - Same model
            new InventoryItemInfo { ItemId = 3, ModelId = 20 }, // Head (Sort 3)
            new InventoryItemInfo { ItemId = 4, ModelId = 0 }   // Unknown model -> Should use ulong.Max - ItemId
        };

        // Mock sheet lookup:
        // ItemId 1, 2 -> Category 5 (Hands)
        // ItemId 3, 4 -> Category 3 (Head)
        System.Func<uint, (string Name, uint Category, uint LevelItem)?> lookup = id =>
        {
            if (id == 1 || id == 2) return ("HandItem", 5, 100);
            if (id == 3 || id == 4) return ("HeadItem", 3, 100);
            return null;
        };

        // Act
        var grouped = ItemCategoryHelper.GroupInventoryItems(items, lookup).ToList();

        // Assert
        Assert.Equal(2, grouped.Count);

        // First group should be Head (Sort order 3 < 5)
        Assert.Equal("Head", grouped[0].Name); // Fallback english from Loc
        Assert.Equal(2, grouped[0].Items.Count()); // 2 distinct models (20 and 0 -> Max-4)

        // Second group should be Hands (Sort order 5)
        Assert.Equal("Hands", grouped[1].Name);
        Assert.Single(grouped[1].Items); // Both have ModelId 10
        Assert.Equal(2, grouped[1].Items.First().Count());
    }
}
