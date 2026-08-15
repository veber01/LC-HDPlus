using HarmonyLib;
using Unity.Netcode;

namespace HDPlus.Patches
{
    internal class RPCLag
    {
        [HarmonyPatch(typeof(NetworkManager), "Awake")]
        [HarmonyPostfix]
        private static void RPCLagFix(NetworkManager __instance)
        {
            __instance.LogLevel = LogLevel.Error;
        }
    }
}