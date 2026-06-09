using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using GlamourChecker.Windows;
using GlamourChecker.Core;
using Dalamud.Game.Command;

namespace GlamourChecker;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public class Plugin : IDalamudPlugin {
    public const string CommandName = "/glamourchecker";

    public Configuration Configuration;

    public readonly WindowSystem WindowSystem = new("GlamourChecker");
    public MainWindow MainWindow;
    public ConfigWindow ConfigWindow;

    public ModelScanner ModelScanner { get; private set; }
    public InventoryWatcher InventoryWatcher { get; private set; }
    public TooltipManager TooltipManager { get; private set; }
    public GlamourLogic GlamourLogic { get; private set; }

    public Plugin(IDalamudPluginInterface pluginInterface) {
        pluginInterface.Create<Services>();

        this.Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        var pluginDir = Services.PluginInterface.AssemblyLocation.Directory?.FullName;
        if (pluginDir != null) {
            string language = this.Configuration.PluginLanguage == "default" ? Services.PluginInterface.UiLanguage : this.Configuration.PluginLanguage;
            Loc.Setup(pluginDir, language);
        }

        var memoryProvider = new GameMemoryProvider();

        this.ModelScanner = new ModelScanner();
        this.InventoryWatcher = new InventoryWatcher(this.ModelScanner, this.Configuration, memoryProvider);
        this.GlamourLogic = new GlamourLogic(this.InventoryWatcher, this.Configuration, memoryProvider);
        this.TooltipManager = new TooltipManager(this.Configuration, this.ModelScanner, this.InventoryWatcher);

        _viewModel = new GlamourChecker.ViewModels.MainWindowViewModel(
            this.GlamourLogic, 
            this.InventoryWatcher, 
            this.Configuration, 
            id => {
                var item = Services.DataManager?.GetExcelSheet<Lumina.Excel.Sheets.Item>()?.GetRowOrDefault(id);
                if (item == null) return null;
                return (item.Value.Name.ToString(), item.Value.EquipSlotCategory.RowId);
            }
        );

        this.MainWindow = new MainWindow(_viewModel);
        this.ConfigWindow = new ConfigWindow(this.Configuration);
        this.ConfigWindow.OnLanguageChanged = () => {
            _viewModel.ReloadCategories();
            this.MainWindow.WindowName = Loc.Localize("Window_Title", "GlamourChecker");
            this.ConfigWindow.WindowName = Loc.Localize("Window_Config_Title", "GlamourChecker Config");
        };
        this.WindowSystem.AddWindow(this.MainWindow);
        this.WindowSystem.AddWindow(this.ConfigWindow);

        Services.PluginInterface.UiBuilder.Draw += this.DrawUi;
        Services.PluginInterface.UiBuilder.OpenMainUi += this.ToggleMainUi;
        Services.PluginInterface.UiBuilder.OpenConfigUi += this.ToggleConfigUi;
        Services.Framework.Update += this.Framework_Update;

        Services.CommandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand) {
            HelpMessage = "Opens the GlamourChecker UI. Use '/glamourchecker scan' to manually scan the armoire/dresser if open."
        });
    }

    private int _updateThrottle = 0;
    private GlamourChecker.ViewModels.MainWindowViewModel _viewModel;

    private void Framework_Update(Dalamud.Plugin.Services.IFramework framework) {
        _updateThrottle++;
        if (_updateThrottle >= 30) {
            _updateThrottle = 0;
            if (this.InventoryWatcher.CheckDresserChanges()) {
                this.InventoryWatcher.ScanDresserAndArmoire();
                _viewModel?.RefreshLists();
            }
        }
    }

    public void Dispose() {
        Services.CommandManager.RemoveHandler(CommandName);

        this.Configuration.Save();
        this.TooltipManager.Dispose();

        this.WindowSystem.RemoveAllWindows();
        this.MainWindow.Dispose();
        this.ConfigWindow.Dispose();

        Services.PluginInterface.UiBuilder.Draw -= this.DrawUi;
        Services.PluginInterface.UiBuilder.OpenMainUi -= this.ToggleMainUi;
        Services.PluginInterface.UiBuilder.OpenConfigUi -= this.ToggleConfigUi;
        Services.Framework.Update -= this.Framework_Update;
    }

    private void DrawUi() => this.WindowSystem.Draw();
    private void ToggleMainUi() => this.MainWindow.Toggle();
    private void ToggleConfigUi() => this.ConfigWindow.Toggle();

    private void OnCommand(string command, string args) {
        if (args is "settings" or "config") {
            this.ToggleConfigUi();
        } else if (args is "scan") {
            this.InventoryWatcher.ScanDresserAndArmoire();
            Services.PluginLog.Info("GlamourChecker: Manually scanned Dresser and Armoire.");
        } else {
            this.ToggleMainUi();
        }
    }
}
