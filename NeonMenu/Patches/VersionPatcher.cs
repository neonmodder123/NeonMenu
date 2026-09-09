using HarmonyLib;

namespace NeonMenu.Patches;

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public static class VersionShowerPatch
{
    [HarmonyPostfix]
    private static void Patch(VersionShower __instance)
    {
        try
        {
            if (__instance.text == null) return;

            __instance.text.text = $"<color=#FFBFA3>NeonMenu {NeonMenuPlugin.PluginVersion}</color> - {__instance.text.text}";
        }
        catch (System.Exception ex)
        {
            NeonMenuPlugin.Logger.LogError($"exception in VersionPatcher: {ex}");
        }
    }
}