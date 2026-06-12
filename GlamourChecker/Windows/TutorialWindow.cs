using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using GlamourChecker.ViewModels;

namespace GlamourChecker.Windows;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class TutorialWindow : Window, IDisposable
{
    private readonly TutorialViewModel _viewModel;

    public TutorialWindow(TutorialViewModel viewModel) : base(Loc.Localize("Tutorial_Title", "Welcome to Glamour Checker!"), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)
    {
        _viewModel = viewModel;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 340),
            MaximumSize = new Vector2(500, 340)
        };
    }

    public override void Draw()
    {
        string pageText = Loc.Localize($"Tutorial_Page{_viewModel.CurrentPage}_Text", "Loading...");

        ImGui.TextWrapped(pageText);

        ImGui.SetCursorPosY(300);

        if (_viewModel.CurrentPage > 1)
        {
            if (ImGui.Button(Loc.Localize("Tutorial_Btn_Prev", "Previous")))
            {
                _viewModel.PreviousPage();
            }
            ImGui.SameLine();
        }

        if (_viewModel.CurrentPage < _viewModel.TotalPages)
        {
            if (ImGui.Button(Loc.Localize("Tutorial_Btn_Next", "Next")))
            {
                _viewModel.NextPage();
            }
        }
        else
        {
            if (ImGui.Button(Loc.Localize("Tutorial_Btn_Finish", "Got it, finish tutorial!")))
            {
                _viewModel.FinishTutorial();
                IsOpen = false;
            }
        }
    }

    public void Dispose() { }
}
