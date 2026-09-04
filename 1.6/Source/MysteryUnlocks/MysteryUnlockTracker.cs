using System.Collections.Generic;
using Verse;

namespace BetterArchitect
{
    public class MysteryUnlockTracker : GameComponent
    {
        private static readonly HashSet<string> pending = new HashSet<string>();
        private List<string> pendingForSave = new List<string>();

        public static bool HasPending;

        private static bool acceptsMarks;

        public MysteryUnlockTracker(Game game)
        {
            pending.Clear();
            HasPending = false;
            acceptsMarks = false;
        }

        public static bool IsPending(string defName)
        {
            return defName != null && pending.Contains(defName);
        }

        public static void ClearPending(string defName)
        {
            if (defName == null) return;
            if (pending.Remove(defName))
            {
                HasPending = pending.Count > 0;
            }
        }

        public static void ClearAll()
        {
            pending.Clear();
            HasPending = false;
        }

        public static void MarkPending(BuildableDef def)
        {
            if (!acceptsMarks) return;
            if (def == null || def.designationCategory == null || !def.canGenerateDefaultDesignator) return;
            if (VefHiddenDesignatorCache.IsHidden(def)) return;
            if (def.researchPrerequisites != null)
            {
                for (var i = 0; i < def.researchPrerequisites.Count; i++)
                {
                    if (!def.researchPrerequisites[i].IsFinished) return;
                }
            }

            pending.Add(def.defName);
            HasPending = true;
        }

        public static void MarkAllAvailable()
        {
            var things = DefDatabase<ThingDef>.AllDefsListForReading;
            for (var i = 0; i < things.Count; i++)
            {
                MarkPending(things[i]);
            }

            var terrains = DefDatabase<TerrainDef>.AllDefsListForReading;
            for (var i = 0; i < terrains.Count; i++)
            {
                MarkPending(terrains[i]);
            }
        }

        public override void StartedNewGame()
        {
            pending.Clear();
            HasPending = false;
            acceptsMarks = true;
        }

        public override void LoadedGame()
        {
            acceptsMarks = true;
        }

        public static void MarkUnlockedBy(ResearchProjectDef project)
        {
            if (project == null) return;

            var unlocked = project.UnlockedDefs;
            for (var i = 0; i < unlocked.Count; i++)
            {
                if (unlocked[i] is BuildableDef buildable)
                {
                    MarkPending(buildable);
                }
            }
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                pendingForSave = new List<string>(pending);
            }

            Scribe_Collections.Look(ref pendingForSave, "baMysteryUnlocks", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                pending.Clear();
                if (pendingForSave != null)
                {
                    foreach (var defName in pendingForSave)
                    {
                        if (!defName.NullOrEmpty())
                        {
                            pending.Add(defName);
                        }
                    }
                }
                HasPending = pending.Count > 0;
            }
        }
    }
}
