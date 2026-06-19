using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BetterArchitect
{
    public static class EditModeSelectionOverrides
    {
        private static readonly Dictionary<string, DesignatorCategoryData> byDefNameBuffer = new Dictionary<string, DesignatorCategoryData>();
        private static readonly HashSet<string> currentParentVisibleChildIds = new HashSet<string>();
        private static readonly Dictionary<string, List<DesignatorCategoryData>> cachedRowsByParent = new Dictionary<string, List<DesignatorCategoryData>>();
        private static readonly Dictionary<string, HashSet<string>> cachedVisibleChildIdsByParent = new Dictionary<string, HashSet<string>>();
        private static readonly Dictionary<string, int> cachedRowsFrameByParent = new Dictionary<string, int>();
        private const int SelectionCacheLifetimeFrames = 300;

        public static bool IsCurrentParentChildVisible(string categoryDefName)
        {
            return currentParentVisibleChildIds.Contains(categoryDefName);
        }

        public static void ClearSelectionCache()
        {
            byDefNameBuffer.Clear();
            currentParentVisibleChildIds.Clear();
            cachedRowsByParent.Clear();
            cachedVisibleChildIdsByParent.Clear();
            cachedRowsFrameByParent.Clear();
            ArchitectCategoryTab_DesignationTabOnGUI_Patch.InvalidateDesignatorDataCache();
        }

        public static void Apply(DesignationCategoryDef mainCat, List<DesignatorCategoryData> designatorDataList)
        {
            if (TryApplyCachedRows(mainCat, designatorDataList))
            {
                return;
            }

            byDefNameBuffer.Clear();
            currentParentVisibleChildIds.Clear();
            for (var i = 0; i < designatorDataList.Count; i++)
            {
                var row = designatorDataList[i];
                var key = row.def.defName;
                if (!byDefNameBuffer.ContainsKey(key))
                {
                    byDefNameBuffer[key] = row;
                }
            }

            BetterArchitectSettings.parentOverrides.TryGetValue(mainCat.defName, out var parentOverride);
            var childIds = new List<string>(EditModeRuntime.GetChildrenForParent(mainCat.defName));
            for (var i = 0; i < childIds.Count; i++)
            {
                var childId = childIds[i];
                if (!EditModeRuntime.IsDefaultChild(mainCat.defName, childId) ||
                    (BetterArchitectSettings.categoryOverrides.TryGetValue(childId, out var co) && co.HasModifications))
                {
                    currentParentVisibleChildIds.Add(childId);
                }
            }
            EditModeRuntime.SetRuntimeParentOrder(mainCat.defName, childIds);

            List<DesignatorCategoryData> result;
            if (parentOverride == null)
            {
                result = new List<DesignatorCategoryData>(designatorDataList.Count);
                for (var i = 0; i < designatorDataList.Count; i++)
                {
                    var d = designatorDataList[i];
                    var built = BuildDataRow(mainCat.defName, d.def, d.def == mainCat, d.allDesignators, d.def.defName, 0, false);
                    result.Add(built);
                }
            }
            else
            {
                result = new List<DesignatorCategoryData>();
                for (var i = 0; i < childIds.Count; i++)
                {
                    var childId = childIds[i];
                    var existing = byDefNameBuffer.TryGetValue(childId, out var current) ? current : null;

                    if (existing == null &&
                        !currentParentVisibleChildIds.Contains(childId) &&
                        EditModeRuntime.IsDefaultChild(mainCat.defName, childId))
                    {
                        var co = EditModeRuntime.GetCategoryOverride(childId);
                        if (co == null || !co.HasModifications)
                        {
                            continue;
                        }
                    }

                    var sourceDef = existing != null ? existing.def : DefDatabase<DesignationCategoryDef>.GetNamedSilentFail(childId);

                    var sourceDesignators = existing != null ? existing.allDesignators : GetDefaultDesignatorsFor(sourceDef);
                    var built = BuildDataRow(mainCat.defName, sourceDef, false, sourceDesignators, childId, i, true);
                    result.Add(built);
                }

                if (!result.Any(d => d.def.defName == mainCat.defName))
                {
                    var mainExisting = byDefNameBuffer.TryGetValue(mainCat.defName, out var currentMain) ? currentMain : null;
                    var mainDesignators = mainExisting != null ? mainExisting.allDesignators : GetDefaultDesignatorsFor(mainCat);
                    var builtMain = BuildDataRow(mainCat.defName, mainCat, true, mainDesignators, mainCat.defName, 9999, false);
                    result.Add(builtMain);
                }
            }

            designatorDataList.Clear();
            designatorDataList.AddRange(result);
            UpdateSelectionCache(mainCat.defName, result);
        }

        private static bool TryApplyCachedRows(DesignationCategoryDef mainCat, List<DesignatorCategoryData> designatorDataList)
        {
            if (!cachedRowsByParent.TryGetValue(mainCat.defName, out var cachedRows))
            {
                return false;
            }

            currentParentVisibleChildIds.Clear();
            if (cachedVisibleChildIdsByParent.TryGetValue(mainCat.defName, out var cachedVisibleChildIds))
            {
                foreach (var childId in cachedVisibleChildIds)
                {
                    currentParentVisibleChildIds.Add(childId);
                }
            }

            designatorDataList.Clear();
            designatorDataList.AddRange(cachedRows);
            return true;
        }

        private static void UpdateSelectionCache(string parentDefName, List<DesignatorCategoryData> rows)
        {
            cachedRowsByParent[parentDefName] = rows.ToList();
            var visibleChildIds = new HashSet<string>();
            foreach (var childId in currentParentVisibleChildIds)
            {
                visibleChildIds.Add(childId);
            }

            cachedVisibleChildIdsByParent[parentDefName] = visibleChildIds;
            cachedRowsFrameByParent[parentDefName] = Time.frameCount;
        }

        private static DesignatorCategoryData BuildDataRow(
            string parentDefName,
            DesignationCategoryDef sourceDef,
            bool isMainCategory,
            List<Designator> sourceAllDesignators,
            string categoryId,
            int fallbackOrder,
            bool useProxyDef)
        {
            var rowDef = sourceDef;
            if (useProxyDef)
            {
                rowDef = EditModeRuntime.GetOrCreateCategoryProxy(parentDefName, categoryId, fallbackOrder);
            }

            var all = BuildEffectiveDesignators(categoryId, sourceAllDesignators);
            var separated = ArchitectCategoryTab_DesignationTabOnGUI_Patch.SeparateDesignatorsByType(all, rowDef);
            return new DesignatorCategoryData(rowDef, isMainCategory, all, separated.buildables, separated.orders);
        }

        private static List<Designator> GetDefaultDesignatorsFor(DesignationCategoryDef def)
        {
            if (def == null || def.ResolvedAllowedDesignators == null)
            {
                return new List<Designator>();
            }

            return def.ResolvedAllowedDesignators.Where(d => d.Visible).ToList();
        }

        private static List<Designator> BuildEffectiveDesignators(string categoryId, List<Designator> defaults)
        {
            var entry = EditModeRuntime.GetCategoryOverride(categoryId);
            if (entry == null)
            {
                return defaults.ToList();
            }

            var defaultBuildables = new List<Designator>();
            var defaultSpecials = new List<Designator>();
            for (var i = 0; i < defaults.Count; i++)
            {
                var defName = GetBuildableDefName(defaults[i]);
                if (defName.NullOrEmpty())
                {
                    defaultSpecials.Add(defaults[i]);
                }
                else
                {
                    defaultBuildables.Add(defaults[i]);
                }
            }

            var buildables = entry.replaceDefaultBuildables ? new List<Designator>() : defaultBuildables.ToList();
            var specials = entry.replaceDefaultSpecials ? new List<Designator>() : defaultSpecials.ToList();

            if (entry.removedBuildableDefNames.Count > 0)
            {
                var removed = new HashSet<string>(entry.removedBuildableDefNames);
                buildables = buildables.Where(d => !removed.Contains(GetBuildableDefName(d))).ToList();
            }

            if (entry.buildableDefNames.Count > 0)
            {
                var currentByDefName = new Dictionary<string, Designator>();
                for (var i = 0; i < buildables.Count; i++)
                {
                    var key = GetBuildableDefName(buildables[i]);
                    if (!key.NullOrEmpty() && !currentByDefName.ContainsKey(key))
                    {
                        currentByDefName[key] = buildables[i];
                    }
                }
                for (var i = 0; i < defaultBuildables.Count; i++)
                {
                    var key = GetBuildableDefName(defaultBuildables[i]);
                    if (!key.NullOrEmpty() && !currentByDefName.ContainsKey(key))
                    {
                        currentByDefName[key] = defaultBuildables[i];
                    }
                }

                foreach (var defName in entry.buildableDefNames)
                {
                    if (currentByDefName.TryGetValue(defName, out var existing))
                    {
                        if (!buildables.Contains(existing))
                        {
                            buildables.Add(existing);
                        }
                        continue;
                    }

                    var created = CreateBuildableDesignator(defName);
                    if (created == null)
                    {
                        continue;
                    }
                    buildables.Add(created);
                    currentByDefName[defName] = created;
                }

                var order = entry.buildableDefNames
                    .Select((name, index) => new { name, index })
                    .ToDictionary(x => x.name, x => x.index);

                buildables.Sort((a, b) =>
                {
                    var keyA = GetBuildableDefName(a);
                    var keyB = GetBuildableDefName(b);
                    var orderA = order.TryGetValue(keyA, out var idxA) ? idxA : int.MaxValue;
                    var orderB = order.TryGetValue(keyB, out var idxB) ? idxB : int.MaxValue;
                    var cmp = orderA.CompareTo(orderB);
                    if (cmp != 0)
                    {
                        return cmp;
                    }
                    return string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), StringComparison.OrdinalIgnoreCase);
                });
            }

            if (entry.specialClassNames.Count > 0)
            {
                var currentByClass = new Dictionary<string, Designator>();
                for (var i = 0; i < specials.Count; i++)
                {
                    var d = specials[i];
                    var key = d.GetType().FullName;
                    if (!key.NullOrEmpty() && !currentByClass.ContainsKey(key))
                    {
                        currentByClass[key] = d;
                    }
                }

                foreach (var className in entry.specialClassNames)
                {
                    if (currentByClass.TryGetValue(className, out var existing))
                    {
                        if (!specials.Contains(existing))
                        {
                            specials.Add(existing);
                        }
                        continue;
                    }

                    var created = CreateSpecialDesignator(className);
                    if (created == null)
                    {
                        continue;
                    }
                    specials.Add(created);
                    currentByClass[className] = created;
                }

            }

            if (entry.removedSpecialClassNames.Count > 0)
            {
                var removed = new HashSet<string>(entry.removedSpecialClassNames);
                specials = specials.Where(d => !removed.Contains(d.GetType().FullName)).ToList();
            }

            ApplySpecialClassOrder(specials, entry);

            var mixed = BuildMixedDesignatorList(defaults, buildables, specials);
            if (BetterArchitectSettings.editMode)
            {
                return mixed;
            }

            return mixed.Where(d => d.Visible).ToList();
        }

        private static List<Designator> BuildMixedDesignatorList(List<Designator> defaults, List<Designator> buildables, List<Designator> specials)
        {
            var result = new List<Designator>(buildables.Count + specials.Count);
            var buildableQueue = new Queue<Designator>(buildables);
            var specialQueue = new Queue<Designator>(specials);

            for (var i = 0; i < defaults.Count; i++)
            {
                var slot = defaults[i];
                var isBuildableSlot = !GetBuildableDefName(slot).NullOrEmpty();
                if (isBuildableSlot)
                {
                    if (buildableQueue.Count > 0)
                    {
                        result.Add(buildableQueue.Dequeue());
                    }
                }
                else if (specialQueue.Count > 0)
                {
                    result.Add(specialQueue.Dequeue());
                }
            }

            while (buildableQueue.Count > 0)
            {
                result.Add(buildableQueue.Dequeue());
            }

            while (specialQueue.Count > 0)
            {
                result.Add(specialQueue.Dequeue());
            }

            return result;
        }

        private static void ApplySpecialClassOrder(List<Designator> specials, CategoryOverride entry)
        {
            var classOrder = BuildClassOrder(entry);
            if (classOrder.Count == 0 || specials.Count <= 1)
            {
                return;
            }

            var byClass = new Dictionary<string, Queue<Designator>>();
            for (var i = 0; i < specials.Count; i++)
            {
                var className = specials[i].GetType().FullName;
                if (className.NullOrEmpty())
                {
                    continue;
                }

                if (!byClass.TryGetValue(className, out var queue))
                {
                    queue = new Queue<Designator>();
                    byClass[className] = queue;
                }

                queue.Enqueue(specials[i]);
            }

            var ordered = new List<Designator>(specials.Count);
            var consumed = new HashSet<Designator>();
            for (var i = 0; i < classOrder.Count; i++)
            {
                var className = classOrder[i];
                if (byClass.TryGetValue(className, out var queue) && queue.Count > 0)
                {
                    var designator = queue.Dequeue();
                    ordered.Add(designator);
                    consumed.Add(designator);
                }
            }

            for (var i = 0; i < specials.Count; i++)
            {
                var designator = specials[i];
                if (!consumed.Contains(designator))
                {
                    ordered.Add(designator);
                }
            }

            specials.Clear();
            specials.AddRange(ordered);
        }

        private static List<string> BuildClassOrder(CategoryOverride entry)
        {
            var result = new List<string>();
            var sourceOrder = entry.specialClassOrder != null && entry.specialClassOrder.Count > 0 ? entry.specialClassOrder : entry.specialClassNames;
            if (sourceOrder != null)
            {
                for (var i = 0; i < sourceOrder.Count; i++)
                {
                    AddClassOrder(result, sourceOrder[i]);
                }
            }

            if (entry.specialClassNames == null)
            {
                return result;
            }

            for (var i = 0; i < entry.specialClassNames.Count; i++)
            {
                AddClassOrder(result, entry.specialClassNames[i]);
            }

            return result;
        }

        private static void AddClassOrder(List<string> classOrder, string className)
        {
            if (!className.NullOrEmpty() && !classOrder.Contains(className))
            {
                classOrder.Add(className);
            }
        }

        private static Designator CreateBuildableDesignator(string defName)
        {
            var def = DefDatabase<BuildableDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return null;
            }
            return new Designator_Build(def);
        }

        private static Designator CreateSpecialDesignator(string className)
        {
            var type = AccessTools.TypeByName(className);
            if (type == null)
            {
                return null;
            }

            return Activator.CreateInstance(type) as Designator;
        }

        private static string GetBuildableDefName(Designator d)
        {
            if (d is Designator_Build db)
            {
                return db.PlacingDef != null ? db.PlacingDef.defName : null;
            }

            if (d is Designator_Dropdown dd)
            {
                var nested = dd.Elements.OfType<Designator_Build>().FirstOrDefault();
                return nested?.PlacingDef?.defName;
            }

            return null;
        }
    }
}
