using HarmonyLib;
using RimWorld;

namespace BetterArchitect
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.DebugSetAllProjectsFinished))]
    public static class ResearchManager_DebugSetAllProjectsFinished_Patch
    {
        public static void Postfix()
        {
            ArchitectCategoryTab_DesignationTabOnGUI_Patch.InvalidateResearchSensitiveCaches();
        }
    }
}
