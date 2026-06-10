using System;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Gui;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace GlamourChecker.Core;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public unsafe class TooltipManager : IDisposable
{
    private readonly Configuration _config;
    private readonly ModelScanner _modelScanner;
    private readonly InventoryWatcher _inventoryWatcher;

    public TooltipManager(Configuration config, ModelScanner modelScanner, InventoryWatcher inventoryWatcher)
    {
        _config = config;
        _modelScanner = modelScanner;
        _inventoryWatcher = inventoryWatcher;
        Services.GameInteropProvider.InitializeFromAttributes(this);
        Services.AddonLifecycle.RegisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PostRequestedUpdate, "ItemDetail", OnItemDetailUpdate);
    }

    private void OnItemDetailUpdate(Dalamud.Game.Addon.Lifecycle.AddonEvent type, Dalamud.Game.Addon.Lifecycle.AddonArgTypes.AddonArgs args)
    {
        if (!_config.ShowInTooltips) return;

        _inventoryWatcher.ScanDresserAndArmoire();

        var hoveredItemId = Services.GameGui.HoveredItem;
        if (hoveredItemId == 0) return;

        var actualItemId = hoveredItemId % 500000;
        var modelId = _modelScanner.GetModelId((uint)actualItemId);
        if (modelId == 0) return;

        bool hasModel = _config.HasModel(modelId);

        if (!hasModel)
        {
            var sharedModelId = _modelScanner.GetSharedModelId((uint)actualItemId);
            if (_config.DresserSharedModelScores.ContainsKey(sharedModelId) || _config.DresserSharedModels.ContainsKey(sharedModelId))
            {
                hasModel = true;
            }
        }

        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null || !addon->IsVisible) return;

        var categoryNode = addon->GetTextNodeById(35); // Item Category (e.g. "Necklace", "Ring")
        if (categoryNode != null)
        {
            try
            {
                var currentText = categoryNode->NodeText.ToString();
                if (currentText != null && !currentText.Contains("[Modelo:"))
                {
                    string stateText = hasModel ? Loc.Localize("Tooltip_Stored_State", "Guardado") : Loc.Localize("Tooltip_NotStored_State", "Nao Guardado");

                    ushort colorCode = hasModel ? (ushort)43 : (ushort)14; // 43 = Green, 14 = Light Red

                    var seBuilder = new SeStringBuilder()
                        .AddText(currentText)
                        .AddText("  [Modelo: ")
                        .AddUiForeground(colorCode)
                        .AddText(stateText)
                        .AddUiForegroundOff()
                        .AddText("]");

                    var bytes = seBuilder.Build().Encode();
                    var nullTerminated = new byte[bytes.Length + 1];
                    Array.Copy(bytes, nullTerminated, bytes.Length);

                    fixed (byte* ptr = nullTerminated)
                    {
                        categoryNode->NodeText.SetString(ptr);
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors
            }
        }
    }

    public void Dispose()
    {
        Services.AddonLifecycle.UnregisterListener(OnItemDetailUpdate);
    }
}
