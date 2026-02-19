using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterArchitect
{
    public class EditModeParentVisibilityWindow : Window
    {
        private Vector2 scrollPosition;
        private readonly List<DesignationCategoryDef> parentCategories;

        public override Vector2 InitialSize => new Vector2(760f, 620f);

        public EditModeParentVisibilityWindow()
        {
            forcePause = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            parentCategories = EditModeRuntime.GetParents().OrderBy(d => d.LabelCap.ToString()).ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0f, 0f, inRect.width, 28f), "BA.SkipParentCategoriesTitle".Translate());
            Widgets.Label(new Rect(0f, 30f, inRect.width, 50f), "BA.SkipParentCategoriesDesc".Translate());

            var outRect = new Rect(0f, 86f, inRect.width, inRect.height - 140f);
            Widgets.DrawBoxSolid(outRect, new Color(0f, 0f, 0f, 0.15f));
            Widgets.DrawBox(outRect, 1);

            var viewRect = new Rect(0f, 0f, outRect.width - 16f, parentCategories.Count * 30f + 8f);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            var rowListing = new Listing_Standard();
            rowListing.Begin(new Rect(4f, 4f, viewRect.width - 8f, viewRect.height - 8f));
            for (var i = 0; i < parentCategories.Count; i++)
            {
                var parentDef = parentCategories[i];
                var isEnabled = !BetterArchitectSettings.ShouldSkipParentCategory(parentDef.defName);
                var previous = isEnabled;
                rowListing.CheckboxLabeled(parentDef.LabelCap + " (" + parentDef.defName + ")", ref isEnabled);

                if (isEnabled == previous)
                {
                    continue;
                }

                SetParentCategorySkipped(parentDef.defName, !isEnabled);
                BetterArchitectSettings.Save();
                RefreshArchitectTabs();
            }
            rowListing.End();

            Widgets.EndScrollView();

            var footerY = inRect.height - 36f;
            if (Widgets.ButtonText(new Rect(0f, footerY, 210f, 30f), "BA.ResetAll".Translate()))
            {
                BetterArchitectSettings.ResetAllEditModeOverrides();
                RefreshArchitectTabs();
                Messages.Message("BA.ResetAllComplete".Translate(), MessageTypeDefOf.NeutralEvent, false);
                Close();
            }

            if (Widgets.ButtonText(new Rect(inRect.width - 120f, footerY, 120f, 30f), "BA.Close".Translate()))
            {
                Close();
            }
        }

        private static void SetParentCategorySkipped(string defName, bool shouldSkip)
        {
            if (shouldSkip)
            {
                if (!BetterArchitectSettings.skippedParentCategoryIds.Contains(defName))
                {
                    BetterArchitectSettings.skippedParentCategoryIds.Add(defName);
                }
            }
            else
            {
                BetterArchitectSettings.skippedParentCategoryIds.Remove(defName);
            }
        }

        private static void RefreshArchitectTabs()
        {
            var architectWindow = MainButtonDefOf.Architect.TabWindow as MainTabWindow_Architect;
            architectWindow?.CacheDesPanels();
        }
    }
}
