using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace GlamourChecker.Windows;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class TutorialWindow : Window, IDisposable
{
    private readonly Configuration _config;
    private int _currentStep = 1;

    public TutorialWindow(Configuration config) : base(Loc.Localize("Tutorial_Title", "Welcome to Glamour Checker!"), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)
    {
        _config = config;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(450, 260),
            MaximumSize = new Vector2(450, 260)
        };
    }

    public override void Draw()
    {
        ImGui.TextWrapped(Loc.Localize("Tutorial_Step1", "This plugin helps you identify which items in your inventory are missing from your Glamour Dresser and Armoire."));
        ImGui.Spacing();

        if (_currentStep >= 2)
        {
            ImGui.TextWrapped(Loc.Localize("Tutorial_Step2", "To get started, you MUST physically open the Glamour Dresser or Armoire in the game."));
            ImGui.Spacing();
        }

        if (_currentStep >= 3)
        {
            ImGui.TextWrapped(Loc.Localize("Tutorial_Step3", "Once opened, click the \"Read Dresser/Armoire\" button in the main window to scan your currently stored items."));
            ImGui.Spacing();
        }

        if (_currentStep >= 4)
        {
            ImGui.TextWrapped(Loc.Localize("Tutorial_Step4", "You can then see which items are \"New Appearances\" (not stored) and which are \"Duplicates\" (already stored)."));
            ImGui.Spacing();
        }

        ImGui.SetCursorPosY(220);

        if (_currentStep < 4)
        {
            if (ImGui.Button(Loc.Localize("Tutorial_Btn_Next", "Next")))
            {
                _currentStep++;
            }
        }
        else
        {
            if (ImGui.Button(Loc.Localize("Tutorial_Btn_Finish", "Got it, finish tutorial!")))
            {
                _config.HasSeenTutorial = true;
                _config.Save();
                IsOpen = false;
            }
        }
    }

    public void Dispose() { }
}
