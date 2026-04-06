using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HDPlus.Patches;

namespace HDPlus;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }
    public static ConfigEntry<int> ResValue { get; private set; } = null!;
    public static ConfigEntry<int> UWResValue { get; private set; } = null!;
    public static ConfigEntry<bool> UWEnabled { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        ResValue = Config.Bind("Resolution","Index:",0,"Presets. 0=860x520 (vanilla), 1=1280x720, 2=1920x1080, 3=2560x1440, 4=3840x2160");
        UWEnabled = Config.Bind("Ultrawide","Enabled",false,"Enable 21:9 ultrawide support.");
        UWResValue = Config.Bind("Ultrawide","Index",0,"Ultrawide presets. 0=860x520 (vanilla), 1=2560x1080, 2=3440x1440"
    );

        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);
        Harmony.PatchAll(typeof(ResolutionPatch));
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

}
