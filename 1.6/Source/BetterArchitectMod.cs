using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterArchitect
{
    public class BetterArchitectMod : Mod
    {
        public static BetterArchitectSettings settings;

        // For simplicity, all patch categories should use the same identifier as the mod they categorize
        private List<String> PatchCategories = new List<string>()
        {
            "OskarPotocki.VFE.Medieval2"
        };

        public BetterArchitectMod(ModContentPack content) : base(content)
        {
            // Queue init
            LongEventHandler.QueueLongEvent(Init, "BA.LoadingLabel", true, null);
            ApplyHarmonyPatches();
        }

        public void Init()
        {
            settings = GetSettings<BetterArchitectSettings>();
            BetterArchitectSettings.mod = this;
            VefHiddenDesignatorCache.Initialize();
            EditModeRuntime.Initialize();
        }

        /// <summary>
        /// Applies all Harmony patches. Only patches for active mods will be applied, so long as they are properly categorized and the patch category is included in PatchCategories
        /// </summary>
        private void ApplyHarmonyPatches()
        {
            Harmony harmonyInstance = new Harmony("BetterArchitectMod");

            foreach (string category in PatchCategories)
            {
                bool modIsActive = ModLister.GetActiveModWithIdentifier(category) != null;
                if (modIsActive)
                    harmonyInstance.PatchCategory(category);
            }

            harmonyInstance.PatchAllUncategorized();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Label("BA.MenuHeight".Translate() + ": " + Mathf.RoundToInt(BetterArchitectSettings.menuHeight));
            BetterArchitectSettings.menuHeight = listingStandard.Slider(BetterArchitectSettings.menuHeight, 250f, 600f);
            listingStandard.Gap();
            listingStandard.Label("BA.BackgroundAlpha".Translate() + ": " + BetterArchitectSettings.backgroundAlpha.ToStringPercent());
            BetterArchitectSettings.backgroundAlpha = listingStandard.Slider(BetterArchitectSettings.backgroundAlpha, 0f, 1.0f);
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("BA.HideOnSelection".Translate(), ref BetterArchitectSettings.hideOnSelection, "BA.HideOnSelectionTooltip".Translate());
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("BA.RememberSubcategory".Translate(), ref BetterArchitectSettings.rememberSubcategory, "BA.RememberSubcategoryTooltip".Translate());

            listingStandard.Gap();
            listingStandard.CheckboxLabeled("BA.UseSpecialFloorsTab".Translate(), ref BetterArchitectSettings.useSpecialFloorsTab, "BA.UseSpecialFloorsTabTooltip".Translate());

            listingStandard.GapLine();
            listingStandard.Label("BA.SettingsIntro".Translate());
            if (listingStandard.ButtonText("BA.OpenParentVisibility".Translate()))
            {
                Find.WindowStack.Add(new EditModeParentVisibilityWindow());
            }
            listingStandard.Gap();
            if (listingStandard.ButtonText("BA.ResetAll".Translate()))
            {
                BetterArchitectSettings.ResetAllEditModeOverrides();
                if (MainButtonDefOf.Architect.TabWindow is MainTabWindow_Architect architectWindow)
                {
                    architectWindow.CacheDesPanels();
                }
                Messages.Message("BA.ResetAllComplete".Translate(), MessageTypeDefOf.NeutralEvent, false);
            }

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => Content.Name;
    }
}
