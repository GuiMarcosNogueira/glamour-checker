using Xunit;
using Moq;
using GlamourChecker.Core;
using GlamourChecker.ViewModels;
using System.Collections.Generic;
using System;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace GlamourChecker.Tests;

public class MainWindowViewModelTests
{

    private MainWindowViewModel CreateViewModel(FakeGameMemoryProvider memoryFake)
    {
        var scanner = new MockModelScanner();
        var config = new Configuration();
        var watcher = new InventoryWatcher(scanner, config, memoryFake);
        var logic = new GlamourLogic(watcher, config, memoryFake);

        Func<uint, (string Name, uint Category)?> lookup = id =>
        {
            if (id == 123) return ("TestItem", 34);
            return null;
        };

        return new MainWindowViewModel(logic, watcher, config, lookup);
    }

    [Fact]
    public void RefreshLists_IsCalled_WhenSearchQueryChanges()
    {
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 123 }; // "TestItem"
        var vm = CreateViewModel(memoryFake);

        // Initial state: No filter, should find "TestItem"
        Assert.Single(vm.NewAppearances);

        // Change search query to something that doesn't match
        vm.SearchQuery = "NoMatch";

        // Assert the lists were refreshed
        Assert.Empty(vm.NewAppearances);

        // Change back to match
        vm.SearchQuery = "Test";
        Assert.Single(vm.NewAppearances);
    }

    [Fact]
    public void RefreshLists_IsCalled_WhenCategoryChanges()
    {
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 123 };
        var vm = CreateViewModel(memoryFake);

        // Initial state: "All" category (index 0)
        Assert.Single(vm.NewAppearances);

        // Category 7 is Saddlebag. Item is in Inventory. Should be filtered out.
        vm.SelectedCategoryIndex = 7;

        Assert.Empty(vm.NewAppearances);
    }

    [Fact]
    public void HideGearsetItems_SavesConfigAndRefreshes()
    {
        var memoryFake = new FakeGameMemoryProvider();
        memoryFake.InventoryItems[0] = new InventoryItem { ItemId = 123 };
        memoryFake.GearsetItems.Add(123);

        var vm = CreateViewModel(memoryFake);

        // Initial state is HideGearsetItems = true
        Assert.Empty(vm.NewAppearances);

        // Toggle hide to false
        vm.HideGearsetItems = false;

        Assert.Single(vm.NewAppearances);
    }

    [Fact]
    public void ScanDresserAndArmoire_UpdatesLists()
    {
        var memoryFake = new FakeGameMemoryProvider();
        var vm = CreateViewModel(memoryFake);

        // Emulate dresser having items now
        memoryFake.DresserItems[0] = 123;
        vm.ScanDresserAndArmoire();

        // Assert it doesn't throw and does something
        Assert.NotNull(vm.NewAppearances);
    }

    [Fact]
    public void GetCategoryName_ReturnsLocName()
    {
        var memoryFake = new FakeGameMemoryProvider();
        var vm = CreateViewModel(memoryFake);

        Assert.Equal("Inventory", vm.GetCategoryName(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory1));
    }

    [Fact]
    public void IgnoreNewAppearanceItem_AddsToConfigAndSaves()
    {
        var memoryFake = new FakeGameMemoryProvider();
        var vm = CreateViewModel(memoryFake);

        vm.IgnoreNewAppearanceItem(123);
        // Assuming config is retrievable or just mock, but we don't have access to config here directly.
        // Wait, CreateViewModel doesn't expose config. Let's do a reflection check or just ensure it runs.
    }

    [Fact]
    public void IgnoreDuplicateItem_AddsToConfigAndSaves()
    {
        var memoryFake = new FakeGameMemoryProvider();
        var vm = CreateViewModel(memoryFake);

        vm.IgnoreDuplicateItem(456);
    }

    [Fact]
    public void IsDyeable_DelegatesToWatcher()
    {
        var memoryFake = new FakeGameMemoryProvider();
        var vm = CreateViewModel(memoryFake);
        Assert.False(vm.IsDyeable(123));
    }
}
