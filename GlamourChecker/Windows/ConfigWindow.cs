using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility.Raii;

namespace GlamourChecker.Windows;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class ConfigWindow(Configuration config) : Window("GlamourChecker Config"), IDisposable
{
    public Action? OnLanguageChanged;

    public override void Draw()
    {
        if (ImGui.BeginTabBar("ConfigTabBar"))
        {
            if (ImGui.BeginTabItem(Loc.Localize("ConfigTab_Settings", "Settings")))
            {
                DrawSettingsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.Localize("ConfigTab_Features", "Features & Help")))
            {
                DrawFeaturesTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawSettingsTab()
    {
        ImGui.Spacing();
        if (ImGui.Checkbox(Loc.Localize("Config_ShowTooltips", "Mostrar informacoes nos Tooltips"), ref config.ShowInTooltips))
        {
            config.Save();
        }
        if (ImGui.Checkbox(Loc.Localize("Config_AutoOpen", "Auto-open window when Dresser/Armoire is accessed"), ref config.AutoOpenWindow))
        {
            config.Save();
        }

        if (ImGui.Checkbox(Loc.Localize("Config_ShowIgnoredLists", "Enable Ignored Items Tab in main window"), ref config.ShowIgnoredLists))
        {
            config.Save();
        }

        ImGui.Spacing();

        var pluginDir = Services.PluginInterface.AssemblyLocation.Directory?.FullName;
        if (pluginDir != null)
        {
            var langs = Loc.GetAvailableLanguages(pluginDir);
            int currentIndex = langs.IndexOf(config.PluginLanguage);
            if (currentIndex == -1) currentIndex = 0;

            string[] langArray = new string[langs.Count];
            for (int i = 0; i < langs.Count; i++)
            {
                if (langs[i] == "default")
                    langArray[i] = $"default ({Services.PluginInterface.UiLanguage})";
                else
                    langArray[i] = langs[i];
            }

            if (ImGui.Combo(Loc.Localize("Config_Language", "Idioma / Language"), ref currentIndex, langArray, langArray.Length))
            {
                config.PluginLanguage = langs[currentIndex];
                config.Save();

                string activeLang = config.PluginLanguage == "default" ? Services.PluginInterface.UiLanguage : config.PluginLanguage;
                Loc.Setup(pluginDir, activeLang);
                OnLanguageChanged?.Invoke();
            }
        }
    }

    private void DrawFeaturesTab()
    {
        ImGui.Spacing();

        using var child = ImRaii.Child("FeaturesListChild", new Vector2(0, 0), true);
        if (child)
        {
            if (ImGui.CollapsingHeader(Loc.Localize("Feature_Tooltips_Title", "Item Tooltips")))
            {
                ImGui.TextWrapped(Loc.Localize("Feature_Tooltips_Desc", "The plugin adds a visual indicator to item tooltips in your inventory, letting you know at a glance if you have stored the item or its appearance in your Dresser/Armoire."));
            }

            if (ImGui.CollapsingHeader(Loc.Localize("Feature_NewApps_Title", "New Appearances Tab")))
            {
                ImGui.TextWrapped(Loc.Localize("Feature_NewApps_Desc", "Scans your inventory for items whose visual model and dye are not yet stored. It is extremely strict and requires the exact match, catering to perfectionist collectors."));
            }

            if (ImGui.CollapsingHeader(Loc.Localize("Feature_Duplicates_Title", "Duplicates Tab")))
            {
                ImGui.TextWrapped(Loc.Localize("Feature_Duplicates_Desc", "Groups items that share the exact same 3D base mesh in your Dresser. It ignores minor texture differences to aggressively help you free up 800 slots."));
            }

            if (ImGui.CollapsingHeader(Loc.Localize("Feature_UpgradeAdvisor_Title", "Dresser Upgrade Advisor")))
            {
                ImGui.TextWrapped(Loc.Localize("Feature_UpgradeAdvisor_Desc", "When you change jobs in a Sanctuary, the plugin checks if any stored gear in your Dresser/Armoire is a smart upgrade (based on Item Level and Stat Priorities) over your equipped gear, notifying you in chat."));
            }

            if (ImGui.CollapsingHeader(Loc.Localize("Feature_Ignore_Title", "Absolute Player Control (Ignore List)")))
            {
                ImGui.TextWrapped(Loc.Localize("Feature_Ignore_Desc", "You can Right-Click any item in the plugin to 'Try On' or Ignore it. Ignored items won't show up in suggestions anymore. You can restore them from the 'Ignored Items' tab (enable it in Settings)."));
            }
        }
    }

    public void Dispose() { }
}
