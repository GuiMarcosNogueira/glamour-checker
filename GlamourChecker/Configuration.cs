using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

using System.Diagnostics.CodeAnalysis;

namespace GlamourChecker;

[ExcludeFromCodeCoverage]
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    [JsonProperty] public bool ShowInTooltips = true;
    [JsonProperty] public bool AutoOpenWindow = false;
    [JsonProperty] public bool HideGearsetItems { get; set; } = true;
    [JsonProperty] public string PluginLanguage { get; set; } = "default";
    [JsonProperty] public HashSet<ulong> DresserModelIds { get; set; } = new();
    [JsonProperty] public HashSet<ulong> ArmoireModelIds { get; set; } = new();

    [JsonProperty] public Dictionary<ulong, List<uint>> DresserItemsByModel { get; set; } = new();
    [JsonProperty] public Dictionary<ulong, List<uint>> ArmoireItemsByModel { get; set; } = new();
    [JsonProperty] public Dictionary<ulong, List<uint>> DresserItemsBySharedModel { get; set; } = new();
    [JsonProperty] public Dictionary<ulong, List<uint>> ArmoireItemsBySharedModel { get; set; } = new();
    [JsonProperty] public Dictionary<ulong, bool> DresserSharedModels { get; set; } = new();
    [JsonProperty] public Dictionary<ulong, int> DresserSharedModelScores { get; set; } = new();
    [JsonProperty] public Dictionary<ulong, int> DresserVisualGroupScores { get; set; } = new();

    [JsonProperty] public HashSet<uint> IgnoredItemIds { get; set; } = new();
    [JsonProperty] public HashSet<uint> IgnoredDuplicateItemIds { get; set; } = new();

    // Kept for backward compatibility when loading old config
    [JsonProperty] public HashSet<ulong> StoredModelIds { get; set; } = new();

    public bool HasModel(ulong modelId)
    {
        return DresserModelIds.Contains(modelId) || ArmoireModelIds.Contains(modelId) || StoredModelIds.Contains(modelId);
    }

    public bool HasExactItem(uint itemId, ulong modelId)
    {
        if (DresserItemsByModel != null && DresserItemsByModel.TryGetValue(modelId, out var dresserList) && dresserList.Contains(itemId)) return true;
        if (ArmoireItemsByModel != null && ArmoireItemsByModel.TryGetValue(modelId, out var armoireList) && armoireList.Contains(itemId)) return true;
        return false;
    }

    public uint GetStoredItemIdForModel(ulong modelId, ulong sharedModelId)
    {
        if (modelId != 0)
        {
            if (DresserItemsByModel != null && DresserItemsByModel.TryGetValue(modelId, out var dresserExact) && dresserExact.Count > 0) return dresserExact[0];
            if (ArmoireItemsByModel != null && ArmoireItemsByModel.TryGetValue(modelId, out var armoireExact) && armoireExact.Count > 0) return armoireExact[0];
        }

        if (sharedModelId != 0)
        {
            if (DresserItemsBySharedModel != null && DresserItemsBySharedModel.TryGetValue(sharedModelId, out var dresserShared) && dresserShared.Count > 0) return dresserShared[0];
            if (ArmoireItemsBySharedModel != null && ArmoireItemsBySharedModel.TryGetValue(sharedModelId, out var armoireShared) && armoireShared.Count > 0) return armoireShared[0];
        }

        return 0;
    }

    public void Save()
    {
        if (Services.PluginInterface != null)
        {
            Services.PluginInterface.SavePluginConfig(this);
        }
    }
}
