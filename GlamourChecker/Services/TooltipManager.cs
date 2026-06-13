using System;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Gui;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.System.Memory;

namespace GlamourChecker.Core;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public unsafe class TooltipManager : IDisposable
{
    private readonly Configuration _config;
    private readonly ModelScanner _modelScanner;
    private readonly InventoryWatcher _inventoryWatcher;
    private const int CustomNodeId = 32613;

    private AtkUnitBase* _currentAddon = null;

    public TooltipManager(Configuration config, ModelScanner modelScanner, InventoryWatcher inventoryWatcher)
    {
        _config = config;
        _modelScanner = modelScanner;
        _inventoryWatcher = inventoryWatcher;
        Services.GameInteropProvider.InitializeFromAttributes(this);
        Services.AddonLifecycle.RegisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PreRequestedUpdate, "ItemDetail", OnItemDetailPreUpdate);
        Services.AddonLifecycle.RegisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PostRequestedUpdate, "ItemDetail", OnItemDetailUpdate);
    }

    public static unsafe void RestoreToNormal(AtkUnitBase* itemTooltip)
    {
        for (var i = 0; i < itemTooltip->UldManager.NodeListCount; i++)
        {
            var n = itemTooltip->UldManager.NodeList[i];
            if (n->NodeId != CustomNodeId || !n->IsVisible())
                continue;

            n->ToggleVisibility(false);

            var insertNode = itemTooltip->GetNodeById(2);
            if (insertNode == null) return;

            float shrinkAmount = n->Height + 4;
            itemTooltip->WindowNode->AtkResNode.SetHeight((ushort)(itemTooltip->WindowNode->AtkResNode.Height - shrinkAmount));
            itemTooltip->WindowNode->Component->UldManager.RootNode->SetHeight(itemTooltip->WindowNode->AtkResNode.Height);
            itemTooltip->WindowNode->Component->UldManager.RootNode->PrevSiblingNode->SetHeight(itemTooltip->WindowNode->AtkResNode.Height);
            itemTooltip->RootNode->SetHeight(itemTooltip->WindowNode->AtkResNode.Height);

            // We do not restore the width here anymore.
            // FFXIV's layout engine does not natively shrink the window width if we expanded it.
            // Instead, we let it be, and we calculate the exact perfect width dynamically in DrawTooltip
            // based on the native content (which FFXIV correctly resizes for every item).

            insertNode->SetYFloat(insertNode->Y - shrinkAmount);
            break;
        }
    }

    private void OnItemDetailPreUpdate(Dalamud.Game.Addon.Lifecycle.AddonEvent type, Dalamud.Game.Addon.Lifecycle.AddonArgTypes.AddonArgs args)
    {
        if (!_config.ShowInTooltips) return;
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null || !addon->IsVisible) return;

        RestoreToNormal(addon);
    }

    private void OnItemDetailUpdate(Dalamud.Game.Addon.Lifecycle.AddonEvent type, Dalamud.Game.Addon.Lifecycle.AddonArgTypes.AddonArgs args)
    {
        if (!_config.ShowInTooltips) return;
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null || !addon->IsVisible) return;

        DrawTooltip(addon);
    }

    private float GetYRelativeToParent(AtkResNode* node, AtkResNode* targetParent)
    {
        float y = node->Y;
        var parent = node->ParentNode;
        while (parent != null && parent != targetParent)
        {
            y += parent->Y;
            parent = parent->ParentNode;
        }
        return y;
    }

    private void DrawTooltip(AtkUnitBase* addon)
    {
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

        _inventoryWatcher.ScanDresserAndArmoire();

        try
        {
            string stateText = "";
            ushort colorCode = 14;

            if (hasExactItem)
            {
                stateText = Loc.Localize("Tooltip_State_ItemStored", "Guardado");
                colorCode = 43;
            }
            else if (_config.IgnoredItemIds.Contains((uint)actualItemId) || _config.IgnoredDuplicateItemIds.Contains((uint)actualItemId))
            {
                stateText = Loc.Localize("Tooltip_State_Ignored", "Ignorado");
                colorCode = 4;
            }
            else if (hasModel || hasSharedModel)
            {
                colorCode = 66;
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
                colorCode = 14;
            }

            var seBuilder = new SeStringBuilder()
                .AddText("Glamour Checker: ")
                .AddUiForeground(colorCode)
                .AddText(stateText)
                .AddUiForegroundOff();

            var bytes = seBuilder.Build().Encode();
            var nullTerminated = new byte[bytes.Length + 1];
            Array.Copy(bytes, nullTerminated, bytes.Length);

            AtkTextNode* customNode = null;
            for (var i = 0; i < addon->UldManager.NodeListCount; i++)
            {
                var node = addon->UldManager.NodeList[i];
                if (node == null || node->NodeId != CustomNodeId)
                    continue;
                customNode = (AtkTextNode*)node;
                break;
            }

            var insertNode = addon->GetNodeById(2);
            if (insertNode == null) return;

            if (customNode == null)
            {
                var baseNode = addon->GetTextNodeById(44);
                if (baseNode == null) return;

                customNode = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
                customNode->AtkResNode.Type = NodeType.Text;
                customNode->AtkResNode.NodeId = CustomNodeId;
                customNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
                customNode->AtkResNode.X = 16;
                customNode->AtkResNode.Width = (ushort)(addon->WindowNode->AtkResNode.Width - 32);
                customNode->AtkResNode.Color = baseNode->AtkResNode.Color;
                customNode->TextColor = baseNode->TextColor;
                customNode->EdgeColor = baseNode->EdgeColor;
                customNode->LineSpacing = 18;
                customNode->FontSize = 12;
                customNode->TextFlags = baseNode->TextFlags | TextFlags.MultiLine | TextFlags.AutoAdjustNodeSize;

                var prev = insertNode->PrevSiblingNode;
                customNode->AtkResNode.ParentNode = insertNode->ParentNode;
                insertNode->PrevSiblingNode = (AtkResNode*)customNode;
                if (prev != null)
                    prev->NextSiblingNode = (AtkResNode*)customNode;

                customNode->AtkResNode.PrevSiblingNode = prev;
                customNode->AtkResNode.NextSiblingNode = insertNode;
                addon->UldManager.UpdateDrawNodeList();
            }

            customNode->AtkResNode.ToggleVisibility(true);

            fixed (byte* ptr = nullTerminated)
            {
                customNode->SetText(ptr);
            }

            // AutoAdjustNodeSize must not be limited by a fixed width initially so it can measure its true width
            customNode->ResizeNodeForCurrentText();

            // Calculate the TRUE native width of the tooltip using the divider line (insertNode).
            // FFXIV natively resizes this line to perfectly match the right edge of the content (like "Total" and "ITEM LEVEL").
            // By adding the left margin (insertNode->X) as the right margin, we get the absolute perfect native window width.
            ushort nativeWidth = (ushort)(insertNode->Width + (insertNode->X * 2));
            if (nativeWidth < 100) nativeWidth = addon->WindowNode->AtkResNode.Width; // Safe fallback

            ushort textWidthNeeded = (ushort)(customNode->AtkResNode.Width + 32);
            ushort newWindowWidth = Math.Max(nativeWidth, textWidthNeeded);

            addon->WindowNode->SetWidth(newWindowWidth);
            addon->WindowNode->AtkResNode.SetWidth(newWindowWidth);
            addon->WindowNode->Component->UldManager.RootNode->SetWidth(newWindowWidth);
            addon->WindowNode->Component->UldManager.RootNode->PrevSiblingNode->SetWidth(newWindowWidth);
            addon->RootNode->SetWidth(newWindowWidth);

            // By placing our node at `WindowHeight - 10`, we push the text down 2 pixels from the native elements.
            // We then expand the window by `Height + 4` to make room for our text and preserve the bottom padding.
            customNode->AtkResNode.SetYFloat(addon->WindowNode->AtkResNode.Height - 10);

            float expandAmount = customNode->AtkResNode.Height + 4;
            ushort newWindowHeight = (ushort)(addon->WindowNode->AtkResNode.Height + expandAmount);

            addon->WindowNode->SetHeight(newWindowHeight);
            addon->WindowNode->AtkResNode.SetHeight(newWindowHeight);
            addon->WindowNode->Component->UldManager.RootNode->SetHeight(newWindowHeight);
            addon->WindowNode->Component->UldManager.RootNode->PrevSiblingNode->SetHeight(newWindowHeight);
            addon->RootNode->SetHeight(newWindowHeight);

            insertNode->SetYFloat(insertNode->Y + expandAmount);
        }
        catch (Exception)
        {
        }
    }

    private void Cleanup()
    {
        unsafe
        {
            var atkUnitBase = (AtkUnitBase*)Services.GameGui.GetAddonByName("ItemDetail", 1).Address;
            if (atkUnitBase == null)
                return;

            for (var n = 0; n < atkUnitBase->UldManager.NodeListCount; n++)
            {
                var node = atkUnitBase->UldManager.NodeList[n];
                if (node == null || node->NodeId != CustomNodeId)
                    continue;

                if (node->ParentNode != null && node->ParentNode->ChildNode == node)
                    node->ParentNode->ChildNode = node->PrevSiblingNode;
                if (node->PrevSiblingNode != null)
                    node->PrevSiblingNode->NextSiblingNode = node->NextSiblingNode;
                if (node->NextSiblingNode != null)
                    node->NextSiblingNode->PrevSiblingNode = node->PrevSiblingNode;

                atkUnitBase->UldManager.UpdateDrawNodeList();
                node->Destroy(true);
                break;
            }
        }
    }

    public void Dispose()
    {
        Services.AddonLifecycle.UnregisterListener(OnItemDetailPreUpdate);
        Services.AddonLifecycle.UnregisterListener(OnItemDetailUpdate);
        Cleanup();
    }
}
