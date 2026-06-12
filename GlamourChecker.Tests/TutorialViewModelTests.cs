using System;
using GlamourChecker;
using GlamourChecker.ViewModels;
using Xunit;

namespace GlamourChecker.Tests;

public class TutorialViewModelTests
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        var config = new Configuration();
        var vm = new TutorialViewModel(config);

        Assert.Equal(1, vm.CurrentPage);
        Assert.Equal(4, vm.TotalPages);
        Assert.False(vm.IsFinished);
    }

    [Fact]
    public void NextPage_IncrementsPage_UntilTotalPages()
    {
        var config = new Configuration();
        var vm = new TutorialViewModel(config);

        vm.NextPage();
        Assert.Equal(2, vm.CurrentPage);

        vm.NextPage();
        vm.NextPage();
        Assert.Equal(4, vm.CurrentPage);

        // Should not go beyond TotalPages
        vm.NextPage();
        Assert.Equal(4, vm.CurrentPage);
    }

    [Fact]
    public void PreviousPage_DecrementsPage_UntilPageOne()
    {
        var config = new Configuration();
        var vm = new TutorialViewModel(config);

        vm.NextPage();
        vm.NextPage();
        Assert.Equal(3, vm.CurrentPage);

        vm.PreviousPage();
        Assert.Equal(2, vm.CurrentPage);

        vm.PreviousPage();
        Assert.Equal(1, vm.CurrentPage);

        // Should not go below 1
        vm.PreviousPage();
        Assert.Equal(1, vm.CurrentPage);
    }

    [Fact]
    public void FinishTutorial_UpdatesConfigAndState()
    {
        var config = new Configuration();
        var vm = new TutorialViewModel(config);

        vm.FinishTutorial();

        Assert.True(config.HasSeenTutorial);
        Assert.True(vm.IsFinished);
    }

    [Fact]
    public void Reset_ResetsState()
    {
        var config = new Configuration();
        var vm = new TutorialViewModel(config);

        vm.NextPage();
        vm.FinishTutorial();

        vm.Reset();

        Assert.Equal(1, vm.CurrentPage);
        Assert.False(vm.IsFinished);
    }
}
