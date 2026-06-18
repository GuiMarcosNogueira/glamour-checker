using System;

namespace GlamourChecker.Core;

public static class ItemCategoryHelper
{
    public static string GetEquipSlotGroup(uint rowId)
    {
        return rowId switch
        {
            1 or 13 or 14 or 19 or 33 => Loc.Localize("SlotGroup_MainHand", "Main Hand"),
            2 => Loc.Localize("SlotGroup_OffHand", "Off Hand"),
            3 => Loc.Localize("SlotGroup_Head", "Head"),
            4 or 15 or 16 or 20 or 21 => Loc.Localize("SlotGroup_Body", "Body"),
            5 => Loc.Localize("SlotGroup_Hands", "Hands"),
            7 or 18 => Loc.Localize("SlotGroup_Legs", "Legs"),
            8 => Loc.Localize("SlotGroup_Feet", "Feet"),
            9 => Loc.Localize("SlotGroup_Ears", "Ears"),
            10 => Loc.Localize("SlotGroup_Neck", "Neck"),
            11 => Loc.Localize("SlotGroup_Wrists", "Wrists"),
            12 => Loc.Localize("SlotGroup_Fingers", "Fingers"),
            _ => Loc.Localize("SlotGroup_Other", "Other")
        };
    }

    public static int GetEquipSlotSortOrder(string slotGroup)
    {
        if (slotGroup == Loc.Localize("SlotGroup_MainHand", "Main Hand")) return 1;
        if (slotGroup == Loc.Localize("SlotGroup_OffHand", "Off Hand")) return 2;
        if (slotGroup == Loc.Localize("SlotGroup_Head", "Head")) return 3;
        if (slotGroup == Loc.Localize("SlotGroup_Body", "Body")) return 4;
        if (slotGroup == Loc.Localize("SlotGroup_Hands", "Hands")) return 5;
        if (slotGroup == Loc.Localize("SlotGroup_Legs", "Legs")) return 6;
        if (slotGroup == Loc.Localize("SlotGroup_Feet", "Feet")) return 7;
        if (slotGroup == Loc.Localize("SlotGroup_Ears", "Ears")) return 8;
        if (slotGroup == Loc.Localize("SlotGroup_Neck", "Neck")) return 9;
        if (slotGroup == Loc.Localize("SlotGroup_Wrists", "Wrists")) return 10;
        if (slotGroup == Loc.Localize("SlotGroup_Fingers", "Fingers")) return 11;
        return 99;
    }
}
