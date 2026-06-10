using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace GlamourChecker.Core;

public class VisualDictionary
{
    // Map from ItemId to VisualGroupId
    private readonly Dictionary<uint, uint> _visualMap = new();

    // Offset to ensure visual group IDs don't collide with native Model IDs.
    // We set the 48th bit to 1, since ModelId is usually ulong.
    private const ulong VisualGroupOffset = 0x1000000000000;

    public VisualDictionary()
    {
        if (!FeatureFlags.EnableVisualDictionary) return;
        LoadDictionary(null);
    }

    public VisualDictionary(Stream stream)
    {
        LoadDictionary(stream);
    }

    private void LoadDictionary(Stream? input)
    {
        try
        {
            if (input != null)
            {
                ProcessStream(input);
                return;
            }

            string resourceName = "GlamourChecker.Resources.SharedModels.json";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            ProcessStream(stream!);
        }
        catch (Exception ex)
        {
            Services.PluginLog?.Error($"Failed to load VisualDictionary: {ex.Message}");
        }
    }

    private void ProcessStream(Stream stream)
    {
        using StreamReader reader = new(stream);
        string json = reader.ReadToEnd();
        var map = JsonSerializer.Deserialize<Dictionary<string, uint>>(json);

        if (map == null) return;

        PopulateMap(map);
        Services.PluginLog?.Info($"Loaded {_visualMap.Count} shared visual mappings.");
    }

    private void PopulateMap(Dictionary<string, uint> map)
    {
        foreach (var kvp in map)
        {
            if (uint.TryParse(kvp.Key, out uint itemId))
            {
                _visualMap[itemId] = kvp.Value;
            }
        }
    }

    public bool TryGetVisualGroup(uint itemId, out ulong visualGroupId)
    {
        if (_visualMap.TryGetValue(itemId, out var groupId))
        {
            visualGroupId = VisualGroupOffset | groupId;
            return true;
        }
        visualGroupId = 0;
        return false;
    }
}
