using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterArchitect
{
    [HarmonyPatch(typeof(ArchitectCategoryTab), nameof(ArchitectCategoryTab.CacheSearchState))]
    public static class ArchitectCategoryTab_CacheSearchState_Patch
    {
        public static void Postfix(ArchitectCategoryTab __instance)
        {
            if (!__instance.quickSearchFilter.Active || __instance.AnySearchMatches)
            {
                return;
            }
            var nestedCategories = DefDatabase<DesignationCategoryDef>.AllDefsListForReading
                .Where(d => d.GetModExtension<NestedCategoryExtension>()?.parentCategory == __instance.def).ToList();
            
            foreach (var nestedCategory in nestedCategories)
            {
                foreach (var designator in VefHiddenDesignatorCache.FilterDesignators(nestedCategory.ResolvedAllowedDesignators))
                {
                    if (__instance.quickSearchFilter.Matches(designator.Label))
                    {
                        __instance.anySearchMatches = true;
                        return;
                    }
                }
            }
        }
    }
}