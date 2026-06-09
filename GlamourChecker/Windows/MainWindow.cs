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
public class MainWindow : Window, IDisposable {
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel) : base(Loc.Localize("Window_Title", "GlamourChecker"), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse) {
        SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(400, 450),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        _viewModel = viewModel;
    }

    public void Dispose() { }

    public override void Draw() {
        if (ImGui.Button(Loc.Localize("Button_Scan", "Ler Dresser/Armoire (Precisa estar aberto)"))) {
            _viewModel.ScanDresserAndArmoire();
        }

        ImGui.Spacing();

        int categoryIndex = _viewModel.SelectedCategoryIndex;
        if (ImGui.Combo("##CategoryCombo", ref categoryIndex, _viewModel.Categories, _viewModel.Categories.Length)) {
            _viewModel.SelectedCategoryIndex = categoryIndex;
        }

        bool hideGearsetItems = _viewModel.HideGearsetItems;
        if (ImGui.Checkbox(Loc.Localize("Checkbox_HideGearset", "Hide items registered to gear sets."), ref hideGearsetItems)) {
            _viewModel.HideGearsetItems = hideGearsetItems;
        }

        ImGui.Spacing();

        string searchQuery = _viewModel.SearchQuery;
        if (ImGui.InputText(Loc.Localize("Placeholder_Search", "Buscar Item"), ref searchQuery, 100)) {
            _viewModel.SearchQuery = searchQuery;
        }

        ImGui.Spacing();

        var newAppearances = _viewModel.NewAppearances;

        if (ImGui.BeginTabBar("GlamourTabs")) {
            if (ImGui.BeginTabItem(Loc.Localize("Tab_NewAppearances", "Aparências Novas (Não Guardadas)"))) {
                DrawItemList(newAppearances);
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), string.Format(Loc.Localize("Footer_TotalNew", "Total: {0} aparências únicas ({1} itens)"), newAppearances.Select(x => x.ModelId).Distinct().Count(), newAppearances.Count));
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem(Loc.Localize("Tab_Duplicates", "Duplicatas no Armário"))) {
                DrawDuplicatesList();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawDuplicatesList() {
        var filteredDuplicates = _viewModel.Duplicates;

        if (filteredDuplicates.Count == 0) {
            ImGui.Text(Loc.Localize("Message_NoDuplicates", "Nenhuma aparência duplicada encontrada no Dresser/Armoire!"));
            return;
        }

        if (ImGui.BeginChild("DuplicatesList", new Vector2(0, -40), true)) {
            foreach (var group in filteredDuplicates) {
                var firstItemSheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(group.ItemIds.First());
                if (firstItemSheet.HasValue) {
                    if (ImGui.TreeNodeEx(string.Format(Loc.Localize("Format_ModelOfDuplicates", "Modelo de: {0} ({1} itens duplicados)"), firstItemSheet.Value.Name, group.ItemIds.Count), ImGuiTreeNodeFlags.DefaultOpen)) {
                        foreach (var itemId in group.ItemIds) {
                            var itemSheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(itemId);
                            if (itemSheet.HasValue) {
                                var icon = Services.TextureProvider.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(itemSheet.Value.Icon));
                                if (icon != null) {
                                    ImGui.Image(icon.GetWrapOrEmpty().Handle, new Vector2(24, 24));
                                    ImGui.SameLine();
                                }
                                ImGui.Text($"{itemSheet.Value.Name}");
                            }
                        }
                        ImGui.TreePop();
                    }
                }
            }
            ImGui.EndChild();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), string.Format(Loc.Localize("Footer_TotalDuplicates", "Total: {0} aparências duplicadas ({1} itens no armário)"), filteredDuplicates.Count, filteredDuplicates.Sum(g => g.ItemIds.Count)));
    }

    private string GetCategoryName(InventoryType type) {
        return _viewModel.GetCategoryName(type);
    }

    private void DrawItemList(List<Core.InventoryItemInfo> items) {
        if (items.Count == 0) {
            ImGui.Text(Loc.Localize("Message_NoNewAppearances", "Nenhum item com aparência nova encontrado nesta categoria!"));
            return;
        }

        if (ImGui.BeginChild("ItemList", new Vector2(0, -40), true)) {
            var groupedItems = items.GroupBy(x => x.ModelId);
            
            foreach (var group in groupedItems) {
                var firstItem = group.First();
                var firstSheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(firstItem.ItemId);
                
                if (firstSheet.HasValue) {
                    bool isGroup = group.Count() > 1;
                    
                    if (isGroup) {
                        if (ImGui.TreeNodeEx(string.Format(Loc.Localize("Format_ModelOf", "Modelo de: {0} ({1} itens)"), firstSheet.Value.Name, group.Count()), ImGuiTreeNodeFlags.DefaultOpen)) {
                            foreach (var item in group) {
                                DrawSingleItem(item);
                            }
                            ImGui.TreePop();
                        }
                    } else {
                        DrawSingleItem(firstItem);
                    }
                }
            }
            ImGui.EndChild();
        }
    }

    private void DrawSingleItem(Core.InventoryItemInfo itemInfo) {
        var itemSheet = Services.DataManager.GetExcelSheet<Item>()?.GetRowOrDefault(itemInfo.ItemId);
        if (itemSheet.HasValue) {
            var icon = Services.TextureProvider.GetFromGameIcon(new Dalamud.Interface.Textures.GameIconLookup(itemSheet.Value.Icon));
            if (icon != null) {
                ImGui.Image(icon.GetWrapOrEmpty().Handle, new Vector2(24, 24));
                ImGui.SameLine();
            }
            ImGui.Text(string.Format(Loc.Localize("Format_ItemInContainer", "{0} (Em: {1})"), itemSheet.Value.Name, GetCategoryName(itemInfo.ContainerType)));
        }
    }
}
