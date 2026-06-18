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
}
