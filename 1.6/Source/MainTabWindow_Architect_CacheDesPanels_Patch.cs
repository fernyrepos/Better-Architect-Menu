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
            var visibleCategories = DefDatabase<DesignationCategoryDef>.AllDefsListForReading.Where(def => def.GetModExtension<NestedCategoryExtension>()?.parentCategory == null).ToList();
            __instance.desPanelsCached.RemoveAll(x => !visibleCategories.Contains(x.def));
        }
    }
}
