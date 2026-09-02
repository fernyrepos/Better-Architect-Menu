using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterArchitect
{
    [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
    public static class ResearchManager_FinishProject_Patch
    {
        public static void Postfix(ResearchProjectDef proj)
        {
            MysteryUnlockTracker.MarkUnlockedBy(proj);
            ArchitectCategoryTab_DesignationTabOnGUI_Patch.InvalidateResearchSensitiveCaches();
        }
    }
}
