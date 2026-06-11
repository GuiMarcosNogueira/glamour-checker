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

        bool hasExactItem = _config.HasExactItem((uint)actualItemId, modelId);
        bool hasModel = _config.HasModel(modelId);
        bool hasSharedModel = false;
        uint storedReplacementItemId = 0;

        if (!hasExactItem)
        {
            if (hasModel)
            {
                storedReplacementItemId = _config.GetStoredItemIdForModel(modelId, 0);
            }
            else
            {
                var sharedModelId = _modelScanner.GetSharedModelId((uint)actualItemId);
                if (_config.DresserSharedModelScores.ContainsKey(sharedModelId) || _config.DresserSharedModels.ContainsKey(sharedModelId))
                {
                    hasSharedModel = true;
                    storedReplacementItemId = _config.GetStoredItemIdForModel(0, sharedModelId);
                }
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
                if (currentText != null && !currentText.Contains("[Item:") && !currentText.Contains("[Modelo:") && !currentText.Contains("[Aparência:") && !currentText.Contains("[Appearance:") && !currentText.Contains("[Não Guardado]") && !currentText.Contains("[Not Stored]"))
                {
                    string stateText = "";
                    ushort colorCode = 14; // Default Red

                    if (hasExactItem)
                    {
                        stateText = Loc.Localize("Tooltip_State_ItemStored", "Item: Guardado");
                        colorCode = 43; // Green
                    }
                    else if (hasModel || hasSharedModel)
                    {
                        colorCode = 66; // Yellow/Orange
                        string? replacementName = null;
                        if (storedReplacementItemId != 0)
                        {
                            var itemRow = Services.DataManager?.GetExcelSheet<Lumina.Excel.Sheets.Item>()?.GetRowOrDefault(storedReplacementItemId);
                            if (itemRow.HasValue)
                            {
                                replacementName = itemRow.Value.Name.ExtractText();
                            }
                        }

                        if (!string.IsNullOrEmpty(replacementName))
                        {
                            stateText = string.Format(Loc.Localize("Tooltip_State_AppearanceStored", "Aparência: {0}"), replacementName);
                        }
                        else
                        {
                            stateText = Loc.Localize("Tooltip_State_AppearanceFallback", "Aparência: Guardada");
                        }
                    }
                    else
                    {
                        stateText = Loc.Localize("Tooltip_State_NotStored", "Não Guardado");
                        colorCode = 14; // Red
                    }

                    var seBuilder = new SeStringBuilder()
                        .AddText(currentText)
                        .AddText("  [")
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
