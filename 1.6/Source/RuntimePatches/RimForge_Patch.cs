using System.Reflection;
using HarmonyLib;

namespace BetterArchitect.RuntimePatches
{
    /// <summary>
    /// RimForge creates its own "RimForge" architect category after the defs are loaded and moves all its buildings into it,
    /// overwriting the categories set through XML patches. Skip that step so Better Architect Menu can sort them.
    /// </summary>
    [HarmonyPatchCategory("co.uk.epicguru.rimforge")]
    [HarmonyPatch]
    public static class RimForge_Patch
    {
        private static MethodBase Target => AccessTools.Method("RimForge.Patches.Patch_DefOfHelper_RebindAllDefOfs:Postfix");

        [HarmonyPrepare]
        public static bool Prepare() => Target != null;

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod() => Target;

        [HarmonyPrefix]
        public static bool Prefix() => false;
    }
}
