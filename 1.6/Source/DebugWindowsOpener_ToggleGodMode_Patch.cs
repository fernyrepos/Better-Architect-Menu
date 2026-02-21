using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterArchitect
{
    [HarmonyPatch(typeof(DebugWindowsOpener), "ToggleGodMode")]
    public static class DebugWindowsOpener_ToggleGodMode_Patch
    {
        public static void Postfix()
        {
            ArchitectCategoryTab_DesignationTabOnGUI_Patch.Reset();

            if (MainButtonDefOf.Architect.TabWindow is MainTabWindow_Architect architectWindow)
            {
                architectWindow.CacheDesPanels();
            }
        }
    }
}
