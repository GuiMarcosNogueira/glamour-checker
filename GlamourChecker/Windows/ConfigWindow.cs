using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GlamourChecker.Windows;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class ConfigWindow(Configuration config) : Window("GlamourChecker Config"), IDisposable
{
    public Action? OnLanguageChanged;
    public Action? OnDumpDuplicates;

    public override void Draw()
    {
        if (ImGui.Checkbox(Loc.Localize("Config_ShowTooltips", "Mostrar informacoes nos Tooltips"), ref config.ShowInTooltips))
        {
            config.Save();
        }
        if (ImGui.Checkbox(Loc.Localize("Config_AutoOpen", "Auto-open window when Dresser/Armoire is accessed"), ref config.AutoOpenWindow))
        {
            config.Save();
        }

        ImGui.Spacing();

        if (ImGui.Button("Dump Duplicates to Desktop (Developer)"))
        {
            OnDumpDuplicates?.Invoke();
        }

        ImGui.Spacing();

        var pluginDir = Services.PluginInterface.AssemblyLocation.Directory?.FullName;
        if (pluginDir != null)
        {
            var langs = Loc.GetAvailableLanguages(pluginDir);
            int currentIndex = langs.IndexOf(config.PluginLanguage);
            if (currentIndex == -1) currentIndex = 0;

            // Formata o dropdown para ficar amigavel, ex: "default (en)" ou "default (pt-BR)"
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

    public void Dispose() { }
}
