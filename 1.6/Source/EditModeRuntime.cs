using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BetterArchitect
{
    public static class EditModeRuntime
    {
        private static bool hasDefaultParentChildrenCache;
        private static readonly Dictionary<string, List<string>> defaultParentChildren = new Dictionary<string, List<string>>();
        private static readonly Dictionary<string, Dictionary<string, int>> runtimeChildOrderByParent = new Dictionary<string, Dictionary<string, int>>();
        private static readonly Dictionary<string, DesignationCategoryDef> categoryProxyDefs = new Dictionary<string, DesignationCategoryDef>();
        private static HashSet<DesignationCategoryDef> cachedParents;
        private static bool hasCachedParents;

        public static void Initialize()
        {
            hasDefaultParentChildrenCache = false;
            hasCachedParents = false;
            EditModeSelectionOverrides.ClearSelectionCache();
            DesignatorSearchCache.Invalidate();
            RebuildDefaultCaches();
        }

        public static void InvalidateAllCaches()
        {
            runtimeChildOrderByParent.Clear();
            hasDefaultParentChildrenCache = false;
            hasCachedParents = false;
            EditModeSelectionOverrides.ClearSelectionCache();
            DesignatorSearchCache.Invalidate();
            RebuildDefaultCaches();
        }

        public static IReadOnlyList<string> GetChildrenForParent(string parentDefName)
        {
            EnsureDefaultParentCaches();
            var defaults = defaultParentChildren.TryGetValue(parentDefName, out var list)
                ? new List<string>(list)
                : new List<string>();

            BetterArchitectSettings.parentOverrides.TryGetValue(parentDefName, out var parentOverride);
            if (parentOverride == null)
            {
                return defaults;
            }

            if (parentOverride.replaceDefaultChildren)
            {
                return parentOverride.childCategoryIds.ToList();
            }

            foreach (var childId in parentOverride.childCategoryIds)
            {
                if (!defaults.Contains(childId))
                {
                    defaults.Add(childId);
                }
            }

            return defaults;
        }

        public static void SetChildrenForParent(string parentDefName, List<string> childIds, bool replaceDefaults = true)
        {
            var parentOverride = BetterArchitectSettings.GetOrCreateParentOverride(parentDefName);
            parentOverride.replaceDefaultChildren = replaceDefaults;
            parentOverride.childCategoryIds = childIds == null
                ? new List<string>()
                : childIds.ToList();
            InvalidateAllCaches();
            BetterArchitectSettings.Save();
        }

        public static bool IsDefaultChild(string parentDefName, string childId)
        {
            EnsureDefaultParentCaches();
            return defaultParentChildren.TryGetValue(parentDefName, out var list) && list.Contains(childId);
        }

        public static CategoryOverride GetCategoryOverride(string categoryId)
        {
            if (BetterArchitectSettings.categoryOverrides.TryGetValue(categoryId, out var entry))
            {
                return entry;
            }

            return null;
        }

        public static string GetCategoryLabel(string categoryId)
        {
            var def = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(categoryId);
            return def?.LabelCap.ToString() ?? categoryId;
        }

        public static void SetRuntimeParentOrder(string parentDefName, List<string> orderedChildIds)
        {
            var orderMap = new Dictionary<string, int>();
            for (var i = 0; i < orderedChildIds.Count; i++)
            {
                var id = orderedChildIds[i];
                if (orderMap.ContainsKey(id))
                {
                    continue;
                }

                orderMap[id] = i;
            }

            runtimeChildOrderByParent[parentDefName] = orderMap;
        }

        public static int GetRuntimeChildOrder(string parentDefName, string childCategoryId, int fallbackOrder)
        {
            if (runtimeChildOrderByParent.TryGetValue(parentDefName, out var map) &&
                map.TryGetValue(childCategoryId, out var index))
            {
                return index;
            }

            return fallbackOrder;
        }

        public static DesignationCategoryDef GetOrCreateCategoryProxy(string parentDefName, string categoryId, int fallbackOrder)
        {
            var proxyKey = parentDefName + "::" + categoryId;
            if (!categoryProxyDefs.TryGetValue(proxyKey, out var proxy))
            {
                proxy = new DesignationCategoryDef { defName = categoryId };
                categoryProxyDefs[proxyKey] = proxy;
            }

            var sourceDef = DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(categoryId);
            var orderIndex = GetRuntimeChildOrder(parentDefName, categoryId, fallbackOrder);
            var sortOrder = 100000 - orderIndex;

            proxy.label = GetCategoryLabel(categoryId);
            proxy.order = sortOrder;

            if (sourceDef?.specialDesignatorClasses != null)
            {
                proxy.specialDesignatorClasses = sourceDef.specialDesignatorClasses.ToList();
            }
            else
            {
                proxy.specialDesignatorClasses = new List<System.Type>();
            }

            return proxy;
        }

        public static HashSet<DesignationCategoryDef> GetParents()
        {
            EnsureDefaultParentCaches();
            if (!hasCachedParents || cachedParents == null)
            {
                cachedParents = DefDatabase<DesignationCategoryDef>.AllDefsListForReading
                    .Where(d => !defaultParentChildren.TryGetValue(d.defName, out var children) || children.Count > 0)
                    .OrderBy(d => d.order)
                    .ThenBy(d => d.LabelCap.ToString())
                    .ToHashSet();
                hasCachedParents = true;
            }

            return cachedParents;
        }

        private static void EnsureDefaultParentCaches()
        {
            if (!hasDefaultParentChildrenCache)
            {
                RebuildDefaultCaches();
            }
        }

        private static void RebuildDefaultCaches()
        {
            defaultParentChildren.Clear();
            categoryProxyDefs.Clear();
            cachedParents = null;
            hasCachedParents = false;

            foreach (var parent in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
            {
                defaultParentChildren[parent.defName] = new List<string>();
            }

            foreach (var category in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
            {
                var nested = category.GetModExtension<NestedCategoryExtension>();
                if (nested?.parentCategory == null)
                {
                    continue;
                }

                var parentDefName = nested.parentCategory.defName;
                if (!defaultParentChildren.TryGetValue(parentDefName, out var list))
                {
                    list = new List<string>();
                    defaultParentChildren[parentDefName] = list;
                }

                if (!list.Contains(category.defName))
                {
                    list.Add(category.defName);
                }
            }

            foreach (var key in defaultParentChildren.Keys.ToList())
            {
                defaultParentChildren[key] = defaultParentChildren[key]
                    .Distinct()
                    .OrderByDescending(child => DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(child).order)
                    .ThenBy(GetCategoryLabel)
                    .ToList();
            }

            hasDefaultParentChildrenCache = true;
        }
    }
}
