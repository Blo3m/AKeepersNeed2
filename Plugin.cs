using AKeepersNeed2.Core;
using BepInEx;
using BepInEx.Logging;

namespace AKeepersNeed2;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        ModConfig.Init(Config);
        ModuleRegistry.EnableAll();
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! Press "
                       + $"{ModConfig.MenuHotkey.Value} in-game to open the mod menu.");
    }

    private void Update()
    {
        ModuleRegistry.Tick();
    }

    private void OnDestroy()
    {
        ModuleRegistry.DisableAll();
    }
}
