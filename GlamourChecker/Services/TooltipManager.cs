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
        if (categoryNode != null && !categoryNode->NodeText.IsEmpty) {
            try {
                var bytesSpan = new ReadOnlySpan<byte>(categoryNode->NodeText.StringPtr, (int)categoryNode->NodeText.BufUsed - 1);
                var parsedString = SeString.Parse(bytesSpan.ToArray());
                
                if (!parsedString.TextValue.Contains("[Modelo:")) {
                    string stateText = hasModel ? Loc.Localize("Tooltip_Stored_State", "Guardado") : Loc.Localize("Tooltip_NotStored_State", "Nao Guardado");
                    ushort colorCode = hasModel ? (ushort)43 : (ushort)14; // 43 = Green, 14 = Light Red
                    
                    parsedString.Payloads.Add(new TextPayload("  [Modelo: "));
                    parsedString.Payloads.Add(new UIForegroundPayload(colorCode));
                    parsedString.Payloads.Add(new TextPayload(stateText));
                    parsedString.Payloads.Add(UIForegroundPayload.UIForegroundOff);
                    parsedString.Payloads.Add(new TextPayload("]"));
                    
                    var newBytes = parsedString.Encode();
                    var nullTerminated = new byte[newBytes.Length + 1];
                    Array.Copy(newBytes, nullTerminated, newBytes.Length);
                    
                    fixed (byte* ptr = nullTerminated) {
                        categoryNode->NodeText.SetString(ptr);
                    }
                }
            } catch (Exception) {
                // Ignore errors to prevent hiding the tooltip completely
            }
        }
    }

    public void Dispose() {
        Services.AddonLifecycle.UnregisterListener(OnItemDetailUpdate);
    }
}
