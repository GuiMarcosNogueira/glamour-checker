using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace GlamourChecker;

public static class Loc
{
    private static Dictionary<string, string> _strings = new();
    private static Dictionary<string, string> _fallbackStrings = new();

    public static void Setup(string pluginDirectory, string language)
    {
        string locPath = Path.Combine(pluginDirectory, "loc");

        _fallbackStrings.Clear();
        _strings.Clear();

        LoadLanguage(locPath, "en", _fallbackStrings);

        if (language != "en")
        {
            LoadLanguage(locPath, language, _strings);
        }
    }

    public static int DictionaryCount => _strings.Count;
    public static int FallbackCount => _fallbackStrings.Count;
    public static string LastError = "";

    private static void LoadLanguage(string locPath, string langCode, Dictionary<string, string> targetDict)
    {
        try
        {
            string filePath = Path.Combine(locPath, $"{langCode}.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        targetDict[kvp.Key] = kvp.Value;
                    }
                }
                LastError += $"[Loaded {langCode} ({targetDict.Count} keys)] ";
            }
            else
            {
                LastError += $"[File not found: {filePath}] ";
            }
        }
        catch (Exception ex)
        {
            LastError += $"[Error {langCode}: {ex.Message}] ";
        }
    }

    public static string Localize(string key, string fallbackText)
    {
        if (_strings.TryGetValue(key, out string? text) && text != null)
        {
            return text;
        }
        if (_fallbackStrings.TryGetValue(key, out string? fbText) && fbText != null)
        {
            return fbText;
        }
        return fallbackText;
    }

    public static List<string> GetAvailableLanguages(string pluginDirectory)
    {
        var list = new List<string>() { "default", "en" };
        try
        {
            string locPath = Path.Combine(pluginDirectory, "loc");
            if (Directory.Exists(locPath))
            {
                foreach (var file in Directory.GetFiles(locPath, "*.json"))
                {
                    var lang = Path.GetFileNameWithoutExtension(file);
                    if (lang != "en" && !list.Contains(lang))
                    {
                        list.Add(lang);
                    }
                }
            }
        }
        catch { }
        return list;
    }
}
