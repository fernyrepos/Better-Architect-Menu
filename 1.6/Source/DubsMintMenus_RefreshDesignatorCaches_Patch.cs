using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterArchitect
{
    [StaticConstructorOnStartup]
    public static class DubsMintMenus_RefreshDesignatorCaches_Patch
    {
        private static System.Type mainTabWindowMayaMenuType;
        private static MainTabWindow_Architect cachedArchitectTab;
        private static List<ArchitectCategoryTab> hiddenCategoriesBackup;
        private static List<DesignationCategoryDef> temporarilyAddedCategories;
        static DubsMintMenus_RefreshDesignatorCaches_Patch()
        {
            if (ArchitectCategoryTab_DesignationTabOnGUI_Patch.DubsMintMenusActive)
            {
                var harmony = new Harmony("BetterArchitectMod.DubsMintMenusPatch");
                harmony.Patch(TargetMethod(), new HarmonyMethod(typeof(DubsMintMenus_RefreshDesignatorCaches_Patch).GetMethod(nameof(Prefix))), new HarmonyMethod(typeof(DubsMintMenus_RefreshDesignatorCaches_Patch).GetMethod(nameof(Postfix))));
            }
        }

        public static MethodBase TargetMethod()
        {
            mainTabWindowMayaMenuType = AccessTools.TypeByName("DubsMintMenus.MainTabWindow_MayaMenu");
            if (mainTabWindowMayaMenuType == null)
            {
                return null;
            }

            var method = AccessTools.Method(mainTabWindowMayaMenuType, "RefreshDesignatorCaches");
            return method;
        }

        public static void Prefix()
        {
            try
            {
                cachedArchitectTab = MainButtonDefOf.Architect?.TabWindow as MainTabWindow_Architect;
                if (cachedArchitectTab == null)
                {
                    return;
                }
                temporarilyAddedCategories = new List<DesignationCategoryDef>();
                hiddenCategoriesBackup = new List<ArchitectCategoryTab>();
                var allCategories = DefDatabase<DesignationCategoryDef>.AllDefsListForReading;
                var visibleCategories = allCategories.Where(def => def.GetModExtension<NestedCategoryExtension>()?.parentCategory == null).ToList();
                var hiddenCategories = allCategories.Where(def => def.GetModExtension<NestedCategoryExtension>()?.parentCategory != null).ToList();
                foreach (var hiddenCategory in hiddenCategories)
                {
                    if (!cachedArchitectTab.desPanelsCached.Any(x => x.def == hiddenCategory))
                    {
                        var hiddenCategoryTab = new ArchitectCategoryTab(hiddenCategory, cachedArchitectTab.quickSearchWidget.filter);
                        cachedArchitectTab.desPanelsCached.Add(hiddenCategoryTab);
                        temporarilyAddedCategories.Add(hiddenCategory);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"BetterArchitect: Error in DubsMintMenus_RefreshDesignatorCaches_Prefix: {ex}");
            }
        }

        public static void Postfix()
        {
            try
            {
                if (cachedArchitectTab != null && temporarilyAddedCategories != null)
                {
                    cachedArchitectTab.desPanelsCached.RemoveAll(x => temporarilyAddedCategories.Contains(x.def));
                }
                temporarilyAddedCategories = null;
                hiddenCategoriesBackup = null;
                cachedArchitectTab = null;
            }
            catch (Exception ex)
            {
                Log.Warning($"BetterArchitect: Error in DubsMintMenus_RefreshDesignatorCaches_Postfix: {ex}");
            }
        }
    }
}
