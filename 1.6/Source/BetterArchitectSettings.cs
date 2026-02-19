using System.Collections.Generic;
using System.Linq;
using Verse;

namespace BetterArchitect
{

    public class BetterArchitectSettings : ModSettings
    {
        public static float menuHeight = 330;
        public static bool hideOnSelection = false;
        public static bool rememberSubcategory = false;
        public static bool useSpecialFloorsTab = true;
        public static float backgroundAlpha = 0.42f;
        public static Dictionary<string, SortSettings> sortSettingsPerCategory = new Dictionary<string, SortSettings>();
        public static Dictionary<string, bool> groupByTechLevelPerCategory = new Dictionary<string, bool>();
        public static bool editMode;
        public static int editModeSchemaVersion = 1;
        public static Dictionary<string, ParentOverride> parentOverrides = new Dictionary<string, ParentOverride>();
        public static Dictionary<string, CategoryOverride> categoryOverrides = new Dictionary<string, CategoryOverride>();
        public static List<string> skippedParentCategoryIds = new List<string>();
        private static HashSet<string> _skippedParentCategoryIdsCache;

        public static BetterArchitectMod mod;
        public static void Save()
        {
            _skippedParentCategoryIdsCache = null;
            mod.GetSettings<BetterArchitectSettings>().Write();
            EditModeRuntime.InvalidateAllCaches();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref menuHeight, "menuHeight", 285);
            Scribe_Values.Look(ref hideOnSelection, "hideOnSelection", false);
            Scribe_Values.Look(ref rememberSubcategory, "rememberSubcategory", false);
            Scribe_Values.Look(ref backgroundAlpha, "backgroundAlpha", 0.15f);
            Scribe_Values.Look(ref useSpecialFloorsTab, "useSpecialFloorsTab", true);

            Scribe_Collections.Look(ref sortSettingsPerCategory, "sortSettingsPerCategory", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref groupByTechLevelPerCategory, "groupByTechLevelPerCategory", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref editMode, "editMode", false);
            Scribe_Values.Look(ref editModeSchemaVersion, "editModeSchemaVersion", 1);
            Scribe_Collections.Look(ref parentOverrides, "parentOverrides", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref categoryOverrides, "categoryOverrides", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref skippedParentCategoryIds, "skippedParentCategoryIds", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                sortSettingsPerCategory ??= new Dictionary<string, SortSettings>();
                groupByTechLevelPerCategory ??= new Dictionary<string, bool>();
                parentOverrides ??= new Dictionary<string, ParentOverride>();
                categoryOverrides ??= new Dictionary<string, CategoryOverride>();
                skippedParentCategoryIds ??= new List<string>();
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                skippedParentCategoryIds = skippedParentCategoryIds
                    .Where(id => !id.NullOrEmpty() && DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(id) != null)
                    .ToList();
                _skippedParentCategoryIdsCache = null;
            }
            base.ExposeData();
        }

        public static ParentOverride GetOrCreateParentOverride(string parentDefName)
        {
            if (parentOverrides.TryGetValue(parentDefName, out var entry))
            {
                return entry;
            }

            entry = new ParentOverride { parentDefName = parentDefName };
            parentOverrides.Add(parentDefName, entry);
            return entry;
        }

        public static CategoryOverride GetOrCreateCategoryOverride(string categoryId)
        {
            if (categoryOverrides.TryGetValue(categoryId, out var entry))
            {
                return entry;
            }

            entry = new CategoryOverride { categoryId = categoryId };
            categoryOverrides.Add(categoryId, entry);
            return entry;
        }

        public static void ResetAllEditModeOverrides()
        {
            editMode = false;
            parentOverrides.Clear();
            categoryOverrides.Clear();
            skippedParentCategoryIds.Clear();
            Save();
        }

        public static bool ShouldSkipParentCategory(string parentDefName)
        {
            _skippedParentCategoryIdsCache ??= new HashSet<string>(skippedParentCategoryIds);
            return _skippedParentCategoryIdsCache.Contains(parentDefName);
        }
    }
}
