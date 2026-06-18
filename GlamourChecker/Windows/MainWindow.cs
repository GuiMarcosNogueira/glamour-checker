using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.Game;
using GlamourChecker.Core;
using GlamourChecker.ViewModels;
using System.Diagnostics.CodeAnalysis;

namespace GlamourChecker.Windows;

[ExcludeFromCodeCoverage]
public class MainWindow : Window, IDisposable
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel) : base(Loc.Localize("Window_Title", "GlamourChecker"), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 450),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        _viewModel = viewModel;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.Button(Loc.Localize("Button_Scan", "Ler Dresser/Armoire (Precisa estar aberto)")))
        {
            _viewModel.ScanDresserAndArmoire();
        }

        ImGui.Spacing();

        int categoryIndex = _viewModel.SelectedCategoryIndex;
        if (ImGui.Combo("##CategoryCombo", ref categoryIndex, _viewModel.Categories, _viewModel.Categories.Length))
        {
            _viewModel.SelectedCategoryIndex = categoryIndex;
        }

        bool hideGearsetItems = _viewModel.HideGearsetItems;
        if (ImGui.Checkbox(Loc.Localize("Checkbox_HideGearset", "Hide items registered to gear sets."), ref hideGearsetItems))
        {
            _viewModel.HideGearsetItems = hideGearsetItems;
        }

        ImGui.Spacing();

        string searchQuery = _viewModel.SearchQuery;
        if (ImGui.InputText(Loc.Localize("Placeholder_Search", "Buscar Item"), ref searchQuery, 100))
        {
            _viewModel.SearchQuery = searchQuery;
        }

        ImGui.Spacing();

        var newAppearances = _viewModel.NewAppearances;

        if (ImGui.BeginTabBar("GlamourTabs"))
        {
            if (ImGui.BeginTabItem(Loc.Localize("Tab_NewAppearances", "AparÃªncias Novas (NÃ£o Guardadas)")))
            {
                DrawItemList(newAppearances);
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), string.Format(Loc.Localize("Footer_TotalNew", "Total: {0} aparÃªncias Ãºnicas ({1} itens)"), newAppearances.Select(x => x.ModelId).Distinct().Count(), newAppearances.Count));
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem(Loc.Localize("Tab_Duplicates", "Duplicatas no ArmÃ¡rio")))
            {
                DrawDuplicatesList();
                ImGui.EndTabItem();
            }

            if (_viewModel.Config.ShowIgnoredLists)
            {
                if (ImGui.BeginTabItem(Loc.Localize("Tab_IgnoredItems", "Itens Ignorados")))
                {
                    DrawIgnoredItemsList();
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawIgnoredItemsList()
    {
        if (ImGui.BeginChild("IgnoredList", new Vector2(0, -40), true))
        {
            if (ImGui.CollapsingHeader(Loc.Localize("Config_IgnoredNew", "Ignored Items (New Appearances)")))
            {
                var groupedBySlot = _viewModel.IgnoredNewAppearances
                    .GroupBy(x =>
                    {
                        var sheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(x.ItemId);
                        return sheet.HasValue ? ItemCategoryHelper.GetEquipSlotGroup(sheet.Value.EquipSlotCategory.RowId) : Loc.Localize("SlotGroup_Other", "Other");
                    })
                    .OrderBy(g => ItemCategoryHelper.GetEquipSlotSortOrder(g.Key));

                foreach (var slotGroup in groupedBySlot)
                {
                    ImGui.Spacing();
                    ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.25f, 0.25f, 0.25f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.TextDisabled, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                    ImGui.Selectable($"◆ {slotGroup.Key}", true, ImGuiSelectableFlags.Disabled);
                    ImGui.PopStyleColor(2);
                    ImGui.Separator();

                    var groupedNew = slotGroup.GroupBy(x => x.ModelId == 0 ? (ulong.MaxValue - x.ItemId) : x.ModelId);
                    foreach (var group in groupedNew)
                    {
                        var firstItem = group.First();
                        var firstSheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(firstItem.ItemId);
                        if (firstSheet.HasValue)
                        {
                            if (group.Count() > 1)
                            {
                                if (ImGui.TreeNodeEx(string.Format(Loc.Localize("Format_ModelOf", "Modelo de: {0} ({1} itens)"), firstSheet.Value.Name, group.Count()), ImGuiTreeNodeFlags.DefaultOpen))
                                {
                                    foreach (var item in group)
                                    {
                                        DrawIgnoredSingleItem(item.ItemId, false);
                                    }
                                    ImGui.TreePop();
                                }
                            }
                            else
                            {
                                DrawIgnoredSingleItem(firstItem.ItemId, false);
                            }
                        }
                    }
                }
            }

            ImGui.Spacing();

            if (ImGui.CollapsingHeader(Loc.Localize("Config_IgnoredDup", "Ignored Items (Duplicate Appearances)")))
            {
                var groupedBySlot = _viewModel.IgnoredDuplicates
                    .GroupBy(x =>
                    {
                        var firstId = x.ItemIds.First();
                        var sheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(firstId);
                        return sheet.HasValue ? ItemCategoryHelper.GetEquipSlotGroup(sheet.Value.EquipSlotCategory.RowId) : Loc.Localize("SlotGroup_Other", "Other");
                    })
                    .OrderBy(g => ItemCategoryHelper.GetEquipSlotSortOrder(g.Key));

                foreach (var slotGroup in groupedBySlot)
                {
                    ImGui.Spacing();
                    ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.25f, 0.25f, 0.25f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.TextDisabled, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                    ImGui.Selectable($"◆ {slotGroup.Key}", true, ImGuiSelectableFlags.Disabled);
                    ImGui.PopStyleColor(2);
                    ImGui.Separator();

                    foreach (var group in slotGroup)
                    {
                        var firstItemSheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(group.ItemIds.First());
                        if (firstItemSheet.HasValue)
                        {
                            if (ImGui.TreeNodeEx(string.Format(Loc.Localize("Format_ModelOfDuplicates", "Modelo de: {0} ({1} itens duplicados)"), firstItemSheet.Value.Name, group.ItemIds.Count), ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                foreach (var itemId in group.ItemIds)
                                {
                                    DrawIgnoredSingleItem(itemId, true);
                                }
                                ImGui.TreePop();
                            }
                        }
                    }
                }
            }
            ImGui.EndChild();
        }
    }

    private void DrawIgnoredSingleItem(uint itemId, bool isDuplicate)
    {
        var itemSheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(itemId);
        if (itemSheet.HasValue)
        {
            var icon = Services.TextureProvider.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(itemSheet.Value.Icon));
            if (icon != null)
            {
                ImGui.Image(icon.GetWrapOrEmpty().Handle, new Vector2(24, 24));
                DrawItemTooltip(itemId, _viewModel.IsDyeable(itemId), icon.GetWrapOrDefault());
                ImGui.SameLine();
            }
            ImGui.Text($"{itemSheet.Value.Name}");
            DrawItemTooltip(itemId, _viewModel.IsDyeable(itemId), icon?.GetWrapOrDefault());

            if (ImGui.BeginPopupContextItem($"ContextIgnored_{itemId}"))
            {
                if (ImGui.Selectable(Loc.Localize("Menu_CopyName", "Copiar Nome do Item")))
                {
                    ImGui.SetClipboardText(itemSheet.Value.Name.ToString());
                }
                if (ImGui.Selectable(Loc.Localize("Menu_TryOn", "Try On")))
                {
                    unsafe
                    {
                        FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentTryon.TryOn(0, itemId, 0, 0, 0, false);
                    }
                }
                if (ImGui.Selectable(Loc.Localize("Config_Remove", "Remove")))
                {
                    if (isDuplicate)
                    {
                        _viewModel.Config.IgnoredDuplicateItemIds.Remove(itemId);
                    }
                    else
                    {
                        _viewModel.Config.IgnoredItemIds.Remove(itemId);
                    }
                    _viewModel.Config.Save();
                    _viewModel.RefreshLists();
                }
                ImGui.EndPopup();
            }

            ImGui.SameLine();
            if (ImGui.Button($"{Loc.Localize("Config_Remove", "Remove")}##ignored_{itemId}"))
            {
                if (isDuplicate)
                {
                    _viewModel.Config.IgnoredDuplicateItemIds.Remove(itemId);
                }
                else
                {
                    _viewModel.Config.IgnoredItemIds.Remove(itemId);
                }
                _viewModel.Config.Save();
                _viewModel.RefreshLists();
            }
        }
    }

    private void DrawDuplicatesList()
    {
        var filteredDuplicates = _viewModel.Duplicates;

        if (filteredDuplicates.Count == 0)
        {
            ImGui.Text(Loc.Localize("Message_NoDuplicates", "Nenhuma aparÃªncia duplicada encontrada no Dresser/Armoire!"));
            return;
        }

        if (ImGui.BeginChild("DuplicatesList", new Vector2(0, -40), true))
        {
            var groupedBySlot = filteredDuplicates
                .GroupBy(x =>
                {
                    var firstId = x.ItemIds.First();
                    var sheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(firstId);
                    return sheet.HasValue ? ItemCategoryHelper.GetEquipSlotGroup(sheet.Value.EquipSlotCategory.RowId) : Loc.Localize("SlotGroup_Other", "Other");
                })
                .OrderBy(g => ItemCategoryHelper.GetEquipSlotSortOrder(g.Key));

            foreach (var slotGroup in groupedBySlot)
            {
                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.25f, 0.25f, 0.25f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.TextDisabled, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                ImGui.Selectable($"◆ {slotGroup.Key}", true, ImGuiSelectableFlags.Disabled);
                ImGui.PopStyleColor(2);
                ImGui.Separator();

                foreach (var group in slotGroup)
                {
                    var firstItemSheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(group.ItemIds.First());
                    if (firstItemSheet.HasValue)
                    {
                        if (ImGui.TreeNodeEx(string.Format(Loc.Localize("Format_ModelOfDuplicates", "Modelo de: {0} ({1} itens duplicados)"), firstItemSheet.Value.Name, group.ItemIds.Count), ImGuiTreeNodeFlags.DefaultOpen))
                        {
                            bool isFirst = true;
                            int dupIndex = 0;
                            foreach (var itemId in group.ItemIds)
                            {
                                var itemSheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(itemId);
                                if (itemSheet.HasValue)
                                {
                                    var icon = Services.TextureProvider.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(itemSheet.Value.Icon));
                                    if (icon != null)
                                    {
                                        ImGui.Image(icon.GetWrapOrEmpty().Handle, new Vector2(24, 24));
                                        DrawItemTooltip(itemId, _viewModel.IsDyeable(itemId), icon.GetWrapOrDefault());
                                        ImGui.SameLine();
                                    }
                                    if (isFirst)
                                    {
                                        ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), $"{itemSheet.Value.Name} (Recomendado/Manter)");
                                        DrawItemTooltip(itemId, _viewModel.IsDyeable(itemId), icon?.GetWrapOrDefault());
                                        isFirst = false;
                                    }
                                    else
                                    {
                                        ImGui.Text($"{itemSheet.Value.Name}");
                                        DrawItemTooltip(itemId, _viewModel.IsDyeable(itemId), icon?.GetWrapOrDefault());
                                    }

                                    if (ImGui.BeginPopupContextItem($"ContextDuplicate_{itemId}_{dupIndex++}"))
                                    {
                                        if (ImGui.Selectable(Loc.Localize("Menu_CopyName", "Copiar Nome do Item")))
                                        {
                                            ImGui.SetClipboardText(itemSheet.Value.Name.ToString());
                                        }
                                        if (ImGui.Selectable(Loc.Localize("Menu_TryOn", "Try On")))
                                        {
                                            unsafe
                                            {
                                                FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentTryon.TryOn(0, itemId, 0, 0, 0, false);
                                            }
                                        }
                                        if (ImGui.Selectable(Loc.Localize("Menu_IgnoreDuplicate", "Ignorar como Duplicata")))
                                        {
                                            _viewModel.IgnoreDuplicateItem(itemId);
                                        }
                                        ImGui.EndPopup();
                                    }
                                }
                            }
                            ImGui.TreePop();
                        }
                    }
                }
            }
            ImGui.EndChild();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), string.Format(Loc.Localize("Footer_TotalDuplicates", "Total: {0} aparÃªncias duplicadas ({1} itens no armÃ¡rio)"), filteredDuplicates.Count, filteredDuplicates.Sum(g => g.ItemIds.Count)));
    }

    private string GetCategoryName(InventoryType type)
    {
        return _viewModel.GetCategoryName(type);
    }

    private void DrawItemList(List<Core.InventoryItemInfo> items)
    {
        if (items.Count == 0)
        {
            ImGui.Text(Loc.Localize("Message_NoNewAppearances", "Nenhum item com aparÃªncia nova encontrado nesta categoria!"));
            return;
        }

        if (ImGui.BeginChild("ItemList", new Vector2(0, -40), true))
        {
            var groupedBySlot = items
                .GroupBy(x =>
                {
                    var sheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(x.ItemId);
                    return sheet.HasValue ? ItemCategoryHelper.GetEquipSlotGroup(sheet.Value.EquipSlotCategory.RowId) : Loc.Localize("SlotGroup_Other", "Other");
                })
                .OrderBy(g => ItemCategoryHelper.GetEquipSlotSortOrder(g.Key));

            int index = 0;

            foreach (var slotGroup in groupedBySlot)
            {
                ImGui.Spacing();
                ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.25f, 0.25f, 0.25f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.TextDisabled, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                ImGui.Selectable($"◆ {slotGroup.Key}", true, ImGuiSelectableFlags.Disabled);
                ImGui.PopStyleColor(2);
                ImGui.Separator();

                var groupedItems = slotGroup.GroupBy(x => x.ModelId == 0 ? (ulong.MaxValue - x.ItemId) : x.ModelId);

                foreach (var group in groupedItems)
                {
                    var firstItem = group.First();
                    var firstSheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(firstItem.ItemId);

                    if (firstSheet.HasValue)
                    {
                        bool isGroup = group.Count() > 1;

                        if (isGroup)
                        {
                            if (ImGui.TreeNodeEx(string.Format(Loc.Localize("Format_ModelOf", "Modelo de: {0} ({1} itens)"), firstSheet.Value.Name, group.Count()), ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                foreach (var item in group)
                                {
                                    DrawSingleItem(item, index++);
                                }
                                ImGui.TreePop();
                            }
                        }
                        else
                        {
                            DrawSingleItem(firstItem, index++);
                        }
                    }
                }
            }
            ImGui.EndChild();
        }
    }

    private void DrawSingleItem(Core.InventoryItemInfo itemInfo, int uniqueIndex)
    {
        var itemSheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(itemInfo.ItemId);
        if (itemSheet.HasValue)
        {
            var icon = Services.TextureProvider.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(itemSheet.Value.Icon));
            if (icon != null)
            {
                ImGui.Image(icon.GetWrapOrEmpty().Handle, new Vector2(24, 24));
                DrawItemTooltip(itemInfo.ItemId, _viewModel.IsDyeable(itemInfo.ItemId), icon.GetWrapOrDefault());
                ImGui.SameLine();
            }
            ImGui.Text(string.Format(Loc.Localize("Format_ItemInContainer", "{0} (Em: {1})"), itemSheet.Value.Name, GetCategoryName(itemInfo.ContainerType)));
            DrawItemTooltip(itemInfo.ItemId, _viewModel.IsDyeable(itemInfo.ItemId), icon?.GetWrapOrDefault());

            if (ImGui.BeginPopupContextItem($"ContextMenu_{itemInfo.ItemId}_{uniqueIndex}"))
            {
                if (ImGui.Selectable(Loc.Localize("Menu_CopyName", "Copiar Nome do Item")))
                {
                    ImGui.SetClipboardText(itemSheet.Value.Name.ToString());
                }
                if (ImGui.Selectable(Loc.Localize("Menu_TryOn", "Try On")))
                {
                    unsafe
                    {
                        FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentTryon.TryOn(0, itemInfo.ItemId, 0, 0, 0, false);
                    }
                }
                if (ImGui.Selectable(Loc.Localize("Menu_IgnoreNewAppearance", "Ignorar como Nova AparÃªncia")))
                {
                    _viewModel.IgnoreNewAppearanceItem(itemInfo.ItemId);
                }
                ImGui.EndPopup();
            }

            if (itemInfo.IsDyeableUpgrade)
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(1.0f, 0.84f, 0.0f, 1.0f), Loc.Localize("Tag_DyeableUpgrade", "[Upgrade TingÃ­vel]"));
            }
        }
    }

    private void DrawItemTooltip(uint itemId, bool isDyeable, Dalamud.Interface.Textures.TextureWraps.IDalamudTextureWrap? icon)
    {
        if (!ImGui.IsItemHovered()) return;

        var itemSheet = Services.DataManager.GetExcelSheet<Lumina.Excel.Sheets.Item>()?.GetRowOrDefault(itemId);
        if (!itemSheet.HasValue) return;

        ImGui.BeginTooltip();

        if (icon != null)
        {
            ImGui.Image(icon.Handle, new Vector2(32, 32));
            ImGui.SameLine();
        }

        // Title and Slot
        ImGui.BeginGroup();
        ImGui.Text(itemSheet.Value.Name.ToString());
        string slot = itemSheet.Value.EquipSlotCategory.RowId switch
        {
            1 => "One-Handed Weapon",
            2 => "Off Hand",
            3 => "Head",
            4 => "Body",
            5 => "Hands",
            6 => "Waist",
            7 => "Legs",
            8 => "Feet",
            9 => "Earrings",
            10 => "Necklace",
            11 => "Bracelets",
            12 => "Ring",
            13 => "Two-Handed Weapon",
            14 => "One-Handed Weapon",
            15 => "Body/Head",
            16 => "Body/Head/Hands/Legs/Feet",
            17 => "Soul Crystal",
            18 => "Legs/Feet",
            19 => "Two-Handed Weapon",
            20 => "Body/Hands/Legs/Feet",
            21 => "Body/Legs/Feet",
            33 => "Fishing Tackle",
            _ => "Equipment"
        };
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), slot);
        ImGui.EndGroup();

        ImGui.Separator();

        // Stats
        ImGui.Text($"ITEM LEVEL {itemSheet.Value.LevelItem.RowId}");

        string classes = itemSheet.Value.ClassJobCategory.Value.Name.ToString();
        if (string.IsNullOrEmpty(classes)) classes = "All Classes";
        ImGui.TextColored(new Vector4(0.5f, 1.0f, 0.5f, 1.0f), classes); // Light green

        ImGui.Text($"Lv. {itemSheet.Value.LevelEquip}");

        // Dyeable
        if (isDyeable)
        {
            ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.8f, 1.0f), Loc.Localize("Tooltip_Dyeable", "Dyeable: Yes"));
        }
        else
        {
            ImGui.TextColored(new Vector4(0.8f, 0.4f, 0.4f, 1.0f), Loc.Localize("Tooltip_NotDyeable", "Dyeable: No"));
        }

        var outfitName = _viewModel.GetOutfitName(itemId);
        if (outfitName != null)
        {
            ImGui.TextColored(new Vector4(0.8f, 0.5f, 1.0f, 1.0f), string.Format(Loc.Localize("Tooltip_PartOfOutfit", "Part of Outfit: {0}"), outfitName));
        }

        ImGui.EndTooltip();
    }
}

