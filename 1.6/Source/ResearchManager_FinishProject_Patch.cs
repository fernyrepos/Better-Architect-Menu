using HarmonyLib;
using RimWorld;

namespace BetterArchitect
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class ResearchManager_FinishProject_Patch
    {
        public static void Postfix()
        {
            ArchitectCategoryTab_DesignationTabOnGUI_Patch.InvalidateResearchSensitiveCaches();
        }
    }
}
