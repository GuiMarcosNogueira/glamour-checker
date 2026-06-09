using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GlamourChecker.Windows;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class ConfigWindow(Configuration config) : Window("GlamourChecker Config"), IDisposable {
    public override void Draw() {
        if (ImGui.Checkbox(Loc.Localize("Config_ShowTooltips", "Mostrar informacoes nos Tooltips"), ref config.ShowInTooltips)) {
            config.Save();
        }

        ImGui.Spacing();
        
        var pluginDir = Services.PluginInterface.AssemblyLocation.Directory?.FullName;
        if (pluginDir != null) {
            var langs = Loc.GetAvailableLanguages(pluginDir);
            int currentIndex = langs.IndexOf(config.PluginLanguage);
            if (currentIndex == -1) currentIndex = 0;

            string[] langArray = langs.ToArray();
            if (ImGui.Combo(Loc.Localize("Config_Language", "Idioma / Language"), ref currentIndex, langArray, langArray.Length)) {
                config.PluginLanguage = langArray[currentIndex];
                config.Save();

                string activeLang = config.PluginLanguage == "default" ? Services.PluginInterface.UiLanguage : config.PluginLanguage;
                Loc.Setup(pluginDir, activeLang);
            }
        }
    }

    public void Dispose() { }
}
