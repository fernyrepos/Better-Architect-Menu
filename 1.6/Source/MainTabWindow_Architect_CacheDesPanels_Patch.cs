using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterArchitect
{
    [HarmonyPatch(typeof(MainTabWindow_Architect), "CacheDesPanels")]
    public static class MainTabWindow_Architect_CacheDesPanels_Patch
    {
        public static void Postfix(MainTabWindow_Architect __instance)
        {
            var visibleCategories = DefDatabase<DesignationCategoryDef>.AllDefsListForReading.Where(def => def.GetModExtension<NestedCategoryExtension>()?.parentCategory == null).ToHashSet();
            var removedSelected = false;

            for (var i = __instance.desPanelsCached.Count - 1; i >= 0; i--)
            {
                var tab = __instance.desPanelsCached[i];
                var hiddenByNestedRule = !visibleCategories.Contains(tab.def);
                var hiddenByUserSetting = BetterArchitectSettings.ShouldSkipParentCategory(tab.def.defName);

                if (!hiddenByNestedRule && !hiddenByUserSetting)
                {
                    continue;
                }

                if (__instance.selectedDesPanel == tab)
                {
                    removedSelected = true;
                }

                __instance.desPanelsCached.RemoveAt(i);
            }

            if (removedSelected)
            {
                __instance.selectedDesPanel = null;
            }
        }
    }
}
