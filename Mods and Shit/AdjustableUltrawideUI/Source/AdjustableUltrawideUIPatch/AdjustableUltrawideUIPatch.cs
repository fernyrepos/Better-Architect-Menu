using System.Reflection;
using HarmonyLib;
using Verse;

namespace sfdsfdfsfsdfs
{
    [StaticConstructorOnStartup]
    public class AdjustableUltrawideUIPatch
    {
        static AdjustableUltrawideUIPatch()
        {
            var harmony = new Harmony("BetterArchitect.ExtensionMethods");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }

        [HarmonyPatch(typeof(BetterArchitect.ExtensionMethods), "get_uiWidth")]
        public static class BetterArchitect_ExtensionMethods_get_uiWidth
        {
            [HarmonyPrefix]
            public static bool Prefix(ref float __result)
            {
                __result =  UI.screenWidth * UltrawideUI.UltrawideUI.UIWidth;
                return false;
            }
        }

        [HarmonyPatch(typeof(BetterArchitect.ExtensionMethods), "get_leftUIEdge")]
        public static class BetterArchitect_ExtensionMethods_get_leftUIEdge
        {
            [HarmonyPrefix]
            public static bool Prefix(ref float __result)
            {
                __result = UI.screenWidth * UltrawideUI.UltrawideUI.LeftMultiplier;
                return false;
            }
        }
    }
}
