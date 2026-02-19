using System.Collections.Generic;
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
        public List<string> buildableDefNames = new List<string>();
        public List<string> removedBuildableDefNames = new List<string>();

        public bool HasModifications =>
            replaceDefaultBuildables ||
            replaceDefaultSpecials ||
            hasOrderOverride ||
            buildableDefNames.Count > 0 ||
            specialClassNames.Count > 0 ||
            removedBuildableDefNames.Count > 0;

        public void ExposeData()
        {
            Scribe_Values.Look(ref categoryId, "categoryId");
            Scribe_Values.Look(ref hasOrderOverride, "hasOrderOverride", false);
            Scribe_Values.Look(ref orderOverride, "orderOverride", 0);
            Scribe_Values.Look(ref replaceDefaultSpecials, "replaceDefaultSpecials", false);
            Scribe_Values.Look(ref replaceDefaultBuildables, "replaceDefaultBuildables", false);
            Scribe_Collections.Look(ref specialClassNames, "specialClassNames", LookMode.Value);
            Scribe_Collections.Look(ref buildableDefNames, "buildableDefNames", LookMode.Value);
            Scribe_Collections.Look(ref removedBuildableDefNames, "removedBuildableDefNames", LookMode.Value);

            if (specialClassNames == null)
            {
                specialClassNames = new List<string>();
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
            }
        }
    }
}
