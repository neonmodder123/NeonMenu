using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace NeonMenu;

[BepInAutoPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("Among Us.exe")]
public partial class NeonMenuPlugin : BasePlugin
{
    public const string PluginGuid = "io.neonmodder123.NeonMenu";
    public const string PluginName = "NeonMenu";
    public const string PluginVersion = "v1.0.1";
    public Harmony Harmony { get; } = new(PluginGuid);
    public static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(PluginName);
    
    public override void Load()
    {
        Harmony.PatchAll();
        Logger.LogInfo($"{PluginName} loaded!");
    }
}
