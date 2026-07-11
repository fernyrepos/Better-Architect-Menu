using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterArchitect
{
    // Reflects VEF.Buildings.HiddenDesignatorsDef so Better Architect can honor it without a
    // compile-time dependency on Vanilla Expanded Framework.
    public static class VefHiddenDesignatorCache
    {
        private static bool initialized;
        private static readonly HashSet<BuildableDef> hiddenBuildables = new HashSet<BuildableDef>();
        private static readonly HashSet<string> hiddenBuildableDefNames = new HashSet<string>();

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            BuildCache();
        }

        public static void Invalidate()
        {
            initialized = false;
            hiddenBuildables.Clear();
            hiddenBuildableDefNames.Clear();
        }

        private static void BuildCache()
        {
            hiddenBuildables.Clear();
            hiddenBuildableDefNames.Clear();

            try
            {
                var hiddenDefType = AccessTools.TypeByName("VEF.Buildings.HiddenDesignatorsDef");
                if (hiddenDefType == null)
                {
                    return;
                }

                var hiddenDesignatorsField = AccessTools.Field(hiddenDefType, "hiddenDesignators");
                if (hiddenDesignatorsField == null)
                {
                    Log.Warning("BetterArchitect: found VEF.Buildings.HiddenDesignatorsDef but could not reflect its 'hiddenDesignators' field.");
                    return;
                }

                var databaseType = typeof(DefDatabase<>).MakeGenericType(hiddenDefType);
                var allDefsProperty = AccessTools.Property(databaseType, "AllDefsListForReading")
                    ?? AccessTools.Property(databaseType, "AllDefs");
                if (allDefsProperty == null)
                {
                    Log.Warning("BetterArchitect: could not reflect DefDatabase<HiddenDesignatorsDef> defs list.");
                    return;
                }

                if (!(allDefsProperty.GetValue(null) is IEnumerable allHiddenDefs))
                {
                    return;
                }

                foreach (var hiddenDef in allHiddenDefs)
                {
                    if (!(hiddenDesignatorsField.GetValue(hiddenDef) is IEnumerable entries))
                    {
                        continue;
                    }

                    foreach (var entry in entries)
                    {
                        if (entry is BuildableDef buildable)
                        {
                            hiddenBuildables.Add(buildable);
                            if (!buildable.defName.NullOrEmpty())
                            {
                                hiddenBuildableDefNames.Add(buildable.defName);
                            }
                        }
                    }
                }

                if (Prefs.DevMode && hiddenBuildables.Count > 0)
                {
                    Log.Message($"BetterArchitect: cached {hiddenBuildables.Count} VEF hidden designator buildable(s).");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"BetterArchitect: failed to read VEF hidden designators: {ex}");
            }
        }

        public static bool IsHidden(BuildableDef def)
        {
            if (def == null)
            {
                return false;
            }

            if (hiddenBuildables.Contains(def))
            {
                return true;
            }

            return !def.defName.NullOrEmpty() && hiddenBuildableDefNames.Contains(def.defName);
        }

        public static bool IsHidden(string defName)
        {
            return !defName.NullOrEmpty() && hiddenBuildableDefNames.Contains(defName);
        }

        public static bool ShouldHide(Designator designator)
        {
            switch (designator)
            {
                case null:
                    return false;
                case Designator_Build build:
                    return IsHidden(build.PlacingDef);
                case Designator_Dropdown dropdown:
                    var placeElements = dropdown.Elements.OfType<Designator_Place>().ToList();
                    return placeElements.Count > 0 && placeElements.All(e => IsHidden(e.PlacingDef));
                case Designator_Place place:
                    return IsHidden(place.PlacingDef);
                default:
                    return false;
            }
        }

        public static IEnumerable<Designator> FilterDesignators(IEnumerable<Designator> source)
        {
            if (source == null)
            {
                yield break;
            }

            foreach (var designator in source)
            {
                if (!ShouldHide(designator))
                {
                    yield return designator;
                }
            }
        }

        public static List<Designator> FilterDesignatorsToList(IEnumerable<Designator> source)
        {
            return FilterDesignators(source).ToList();
        }
    }
}
