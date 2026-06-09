using System;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Gui;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace GlamourChecker.Core;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public unsafe class TooltipManager : IDisposable {
    private readonly Configuration _config;
    private readonly ModelScanner _modelScanner;
    private readonly InventoryWatcher _inventoryWatcher;

    public TooltipManager(Configuration config, ModelScanner modelScanner, InventoryWatcher inventoryWatcher) {
        _config = config;
        _modelScanner = modelScanner;
        _inventoryWatcher = inventoryWatcher;
        Services.GameInteropProvider.InitializeFromAttributes(this);
        Services.AddonLifecycle.RegisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PostRequestedUpdate, "ItemDetail", OnItemDetailUpdate);
    }

    private void OnItemDetailUpdate(Dalamud.Game.Addon.Lifecycle.AddonEvent type, Dalamud.Game.Addon.Lifecycle.AddonArgTypes.AddonArgs args) {
        if (!_config.ShowInTooltips) return;

        _inventoryWatcher.ScanDresserAndArmoire();

        var hoveredItemId = Services.GameGui.HoveredItem;
        if (hoveredItemId == 0) return;

        var actualItemId = hoveredItemId % 500000;
        var modelId = _modelScanner.GetModelId((uint)actualItemId);
        if (modelId == 0) return;

        bool hasModel = _config.HasModel(modelId);
        
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null || !addon->IsVisible) return;
        
        var categoryNode = addon->GetTextNodeById(35); // Item Category (e.g. "Necklace", "Ring")
        if (categoryNode != null) {
            var currentText = categoryNode->NodeText.ToString();
            if (!currentText.Contains("[Modelo:")) {
                string extraText = hasModel ? Loc.Localize("Tooltip_Stored", "  [Modelo: Guardado]") : Loc.Localize("Tooltip_NotStored", "  [Modelo: Nao Guardado]");
                categoryNode->NodeText.SetString(currentText + extraText);
            }
        }
    }

    public void Dispose() {
        Services.AddonLifecycle.UnregisterListener(OnItemDetailUpdate);
    }
}
