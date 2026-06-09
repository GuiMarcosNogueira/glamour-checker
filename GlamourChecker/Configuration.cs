using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

using System.Diagnostics.CodeAnalysis;

namespace GlamourChecker;

[ExcludeFromCodeCoverage]
[Serializable]
public class Configuration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    [JsonProperty] public bool ShowInTooltips = true;
    [JsonProperty] public bool HideGearsetItems { get; set; } = true;
    [JsonProperty] public string PluginLanguage { get; set; } = "default";
    [JsonProperty] public HashSet<ulong> DresserModelIds { get; set; } = new();
    [JsonProperty] public HashSet<ulong> ArmoireModelIds { get; set; } = new();
    
    [JsonProperty] public Dictionary<ulong, List<uint>> DresserItemsByModel { get; set; } = new();
    [JsonProperty] public Dictionary<ulong, List<uint>> ArmoireItemsByModel { get; set; } = new();
    [JsonProperty] public Dictionary<ulong, bool> DresserSharedModels { get; set; } = new();
    
    // Kept for backward compatibility when loading old config
    [JsonProperty] public HashSet<ulong> StoredModelIds { get; set; } = new();

    public bool HasModel(ulong modelId) {
        return DresserModelIds.Contains(modelId) || ArmoireModelIds.Contains(modelId) || StoredModelIds.Contains(modelId);
    }

    public void Save() {
        if (Services.PluginInterface != null) {
            Services.PluginInterface.SavePluginConfig(this);
        }
    }
}
