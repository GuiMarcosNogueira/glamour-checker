using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using System.Diagnostics.CodeAnalysis;

namespace GlamourChecker;

[ExcludeFromCodeCoverage]
public class Services
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; set; } = null!;
    [PluginService] public static IDataManager DataManager { get; set; } = null!;
    [PluginService] public static IClientState ClientState { get; set; } = null!;
    [PluginService] public static IGameGui GameGui { get; set; } = null!;
    [PluginService] public static IPluginLog PluginLog { get; set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; set; } = null!;
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; set; } = null!;
    [PluginService] public static IFramework Framework { get; set; } = null!;
}
