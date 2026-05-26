using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterArchitect
{
    public class ParentOverride : IExposable
    {
        public string parentDefName;
        public bool replaceDefaultChildren;
        public List<string> childCategoryIds = new List<string>();

        public void ExposeData()
        {
            Scribe_Values.Look(ref parentDefName, "parentDefName");
            Scribe_Values.Look(ref replaceDefaultChildren, "replaceDefaultChildren", false);
            Scribe_Collections.Look(ref childCategoryIds, "childCategoryIds", LookMode.Value);
            if (childCategoryIds == null)
            {
                childCategoryIds = new List<string>();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                childCategoryIds.RemoveAll(defName =>
                    defName.NullOrEmpty() || DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(defName) == null);
            }
        }
    }

    public class CategoryOverride : IExposable
    {
        public string categoryId;
        public bool hasOrderOverride;
        public int orderOverride;
        public bool replaceDefaultSpecials;
        public bool replaceDefaultBuildables;
        public List<string> specialClassNames = new List<string>();
        public List<string> specialClassOrder = new List<string>();
        public List<string> removedSpecialClassNames = new List<string>();
        public List<string> buildableDefNames = new List<string>();
        public List<string> removedBuildableDefNames = new List<string>();

        public bool HasModifications =>
            replaceDefaultBuildables ||
            replaceDefaultSpecials ||
            hasOrderOverride ||
            buildableDefNames.Count > 0 ||
            specialClassNames.Count > 0 ||
            specialClassOrder.Count > 0 ||
            removedSpecialClassNames.Count > 0 ||
            removedBuildableDefNames.Count > 0;

        public void ExposeData()
        {
            Scribe_Values.Look(ref categoryId, "categoryId");
            Scribe_Values.Look(ref hasOrderOverride, "hasOrderOverride", false);
            Scribe_Values.Look(ref orderOverride, "orderOverride", 0);
            Scribe_Values.Look(ref replaceDefaultSpecials, "replaceDefaultSpecials", false);
            Scribe_Values.Look(ref replaceDefaultBuildables, "replaceDefaultBuildables", false);
            Scribe_Collections.Look(ref specialClassNames, "specialClassNames", LookMode.Value);
            Scribe_Collections.Look(ref specialClassOrder, "specialClassOrder", LookMode.Value);
            Scribe_Collections.Look(ref removedSpecialClassNames, "removedSpecialClassNames", LookMode.Value);
            Scribe_Collections.Look(ref buildableDefNames, "buildableDefNames", LookMode.Value);
            Scribe_Collections.Look(ref removedBuildableDefNames, "removedBuildableDefNames", LookMode.Value);

            if (specialClassNames == null)
            {
                specialClassNames = new List<string>();
            }
            if (specialClassOrder == null)
            {
                specialClassOrder = new List<string>();
            }
            if (removedSpecialClassNames == null)
            {
                removedSpecialClassNames = new List<string>();
            }
            if (buildableDefNames == null)
            {
                buildableDefNames = new List<string>();
            }
            if (removedBuildableDefNames == null)
            {
                removedBuildableDefNames = new List<string>();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                buildableDefNames.RemoveAll(defName =>
                    defName.NullOrEmpty() || DefDatabase<BuildableDef>.GetNamedSilentFail(defName) == null);
                removedBuildableDefNames.RemoveAll(defName =>
                    defName.NullOrEmpty() || DefDatabase<BuildableDef>.GetNamedSilentFail(defName) == null);
                CleanupSpecialDesignatorOverrides();
            }
        }

        public void CleanupSpecialDesignatorOverrides()
        {
            specialClassNames = DistinctValidClassNames(specialClassNames);
            specialClassOrder = DistinctValidClassNames(specialClassOrder);
            removedSpecialClassNames = DistinctValidClassNames(removedSpecialClassNames);

            if (categoryId.NullOrEmpty())
            {
                return;
            }

            var defaultSpecials = GetDefaultSpecialClassNames(categoryId);
            if (defaultSpecials.Count == 0)
            {
                specialClassOrder.Clear();
                removedSpecialClassNames.Clear();
                return;
            }

            var defaultSet = new HashSet<string>(defaultSpecials);
            removedSpecialClassNames.RemoveAll(className => !defaultSet.Contains(className));
            specialClassOrder.RemoveAll(className => removedSpecialClassNames.Contains(className));

            MigrateReplaceDefaultSpecials(defaultSpecials, defaultSet);
            RemoveDefaultOrderNoOp(defaultSpecials);
        }

        private void MigrateReplaceDefaultSpecials(List<string> defaultSpecials, HashSet<string> defaultSet)
        {
            if (!replaceDefaultSpecials || specialClassNames.Count == 0)
            {
                return;
            }

            var persistedSet = new HashSet<string>(specialClassNames);
            if (!specialClassNames.Any(className => defaultSet.Contains(className)))
            {
                return;
            }

            for (var i = 0; i < defaultSpecials.Count; i++)
            {
                var className = defaultSpecials[i];
                if (!persistedSet.Contains(className) && !removedSpecialClassNames.Contains(className))
                {
                    removedSpecialClassNames.Add(className);
                }
            }

            specialClassOrder = specialClassOrder.Count > 0
                ? MergeClassOrder(specialClassOrder, specialClassNames)
                : MergeClassOrder(specialClassNames);
            specialClassNames = specialClassNames.Where(className => !defaultSet.Contains(className)).ToList();
            specialClassOrder.RemoveAll(className => removedSpecialClassNames.Contains(className));
            replaceDefaultSpecials = false;
        }

        private void RemoveDefaultOrderNoOp(List<string> defaultSpecials)
        {
            if (specialClassOrder.Count == 0)
            {
                return;
            }

            var removedSet = new HashSet<string>(removedSpecialClassNames);
            var effectiveDefaults = defaultSpecials
                .Where(className => !removedSet.Contains(className))
                .ToList();
            if (specialClassOrder.Count == effectiveDefaults.Count && specialClassOrder.SequenceEqual(effectiveDefaults))
            {
                specialClassOrder.Clear();
            }
        }

        private static List<string> DistinctValidClassNames(List<string> classNames)
        {
            var result = new List<string>();
            if (classNames == null)
            {
                return result;
            }

            for (var i = 0; i < classNames.Count; i++)
            {
                var className = classNames[i];
                if (className.NullOrEmpty() || result.Contains(className) || !CanResolveDesignatorType(className))
                {
                    continue;
                }

                result.Add(className);
            }

            return result;
        }

        private static bool CanResolveDesignatorType(string className)
        {
            var type = AccessTools.TypeByName(className);
            return type != null && typeof(Designator).IsAssignableFrom(type) && !type.IsAbstract;
        }

        private static List<string> GetDefaultSpecialClassNames(string categoryId)
        {
            var def = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(categoryId);
            if (def?.specialDesignatorClasses == null)
            {
                return new List<string>();
            }

            return def.specialDesignatorClasses
                .Where(type => type != null && typeof(Designator).IsAssignableFrom(type) && !type.IsAbstract)
                .Select(type => type.FullName)
                .Where(className => !className.NullOrEmpty())
                .Distinct()
                .ToList();
        }

        private static List<string> MergeClassOrder(params List<string>[] classNameLists)
        {
            var result = new List<string>();
            for (var i = 0; i < classNameLists.Length; i++)
            {
                var classNameList = classNameLists[i];
                if (classNameList == null)
                {
                    continue;
                }

                for (var j = 0; j < classNameList.Count; j++)
                {
                    var className = classNameList[j];
                    if (!className.NullOrEmpty() && !result.Contains(className))
                    {
                        result.Add(className);
                    }
                }
            }

            return result;
        }
    }
}
