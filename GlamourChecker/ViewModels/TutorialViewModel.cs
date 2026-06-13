using System;

namespace GlamourChecker.ViewModels;

public class TutorialViewModel
{
    private readonly Configuration _config;

    public int CurrentPage { get; private set; } = 1;
    public int TotalPages { get; } = 4;
    public bool IsFinished { get; private set; } = false;

    public TutorialViewModel(Configuration config)
    {
        _config = config;
    }

    public void NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
    }

    public void PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    public void FinishTutorial()
    {
        _config.HasSeenTutorial = true;
        _config.Save();
        IsFinished = true;
    }

    public void Reset()
    {
        CurrentPage = 1;
        IsFinished = false;
    }
}
