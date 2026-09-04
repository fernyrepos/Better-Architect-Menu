using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BetterArchitect
{
    [HotSwappable]
    public static class MysteryUnlockRuntime
    {
        private const float RevealDuration = 0.9f;
        private const float CoverDuration = 0.22f;
        private const int RevealAllThreshold = 10;
        private const float RevealAllButtonWidth = 116f;

        private static readonly Color TileColor = new Color(0.09f, 0.09f, 0.13f, 0.92f);
        private static readonly Color SilhouetteColor = new Color(0.02f, 0.02f, 0.04f, 0.95f);
        private static readonly Color AccentColor = new Color(1f, 1f, 1f);
        private static readonly Color ScrimColor = new Color(0f, 0f, 0f, 0.72f);

        private static readonly Dictionary<string, float> activeReveals = new Dictionary<string, float>();
        private static float revealsUntil;

        public static bool Active => BetterArchitectSettings.mysteryUnlocks &&
                                     !BetterArchitectSettings.editMode &&
                                     !DebugSettings.godMode &&
                                     (MysteryUnlockTracker.HasPending || Time.realtimeSinceStartup < revealsUntil);

        public static void Reset()
        {
            activeReveals.Clear();
            revealsUntil = 0f;
        }

        public static bool DrawInsteadOfGizmo(Vector2 topLeft, float size, Designator designator, GizmoRenderParms parms)
        {
            var key = KeyFor(designator);
            if (key == null) return false;

            var rect = new Rect(topLeft.x, topLeft.y, size, size);
            if (activeReveals.TryGetValue(key, out var startTime))
            {
                var elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed >= RevealDuration)
                {
                    activeReveals.Remove(key);
                    return false;
                }

                if (elapsed < CoverDuration)
                {
                    DrawWindup(rect, designator, elapsed / CoverDuration);
                }
                else
                {
                    DrawReveal(rect, topLeft, size, designator, parms,
                        Mathf.Clamp01((elapsed - CoverDuration) / (RevealDuration - CoverDuration)));
                }
                return true;
            }

            if (!MysteryUnlockTracker.HasPending || !IsHidden(designator)) return false;

            DrawMysteryBox(rect, designator, key);
            return true;
        }

        public static void DrawRevealAllButton(Rect headerRect, List<Designator> designators)
        {
            if (!MysteryUnlockTracker.HasPending || designators == null) return;

            var hiddenCount = 0;
            for (var i = 0; i < designators.Count; i++)
            {
                if (IsHidden(designators[i])) hiddenCount++;
            }
            if (hiddenCount <= RevealAllThreshold) return;

            var available = headerRect.width - 108f;
            if (available < 70f) return;

            var buttonRect = new Rect(headerRect.x + 4f, headerRect.y + 2f, Mathf.Min(RevealAllButtonWidth, available), 24f);
            var oldFont = Text.Font;
            Text.Font = GameFont.Tiny;
            if (Widgets.ButtonText(buttonRect, "BA.RevealAll".Translate(hiddenCount)))
            {
                RevealAll(designators);
            }
            Text.Font = oldFont;
        }

        private static void RevealAll(List<Designator> designators)
        {
            var revealedAny = false;
            for (var i = 0; i < designators.Count; i++)
            {
                if (!IsHidden(designators[i])) continue;
                ClearPendingFor(designators[i]);
                revealedAny = true;
            }

            if (!revealedAny) return;

            activeReveals.Clear();
            revealsUntil = 0f;
            DefsOf.BA_DiscoverDesignator?.PlayOneShotOnCamera();
        }

        private static void DrawMysteryBox(Rect rect, Designator designator, string key)
        {
            var hovered = Mouse.IsOver(rect);

            GUI.DrawTexture(rect, Command.BGTex);
            Widgets.DrawBoxSolid(rect, TileColor);
            DrawSilhouette(rect, designator);

            var oldFont = Text.Font;
            var oldAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            if (hovered)
            {
                Widgets.DrawBoxSolid(rect, ScrimColor);
                Text.Font = GameFont.Tiny;
                GUI.color = AccentColor;
                Widgets.Label(rect.ContractedBy(5f), "BA.ClickToReveal".Translate());
            }
            else
            {
                Text.Font = GameFont.Small;
                GUI.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.85f);
                Widgets.Label(rect, "BA.NewUnlockBadge".Translate());
            }
            GUI.color = Color.white;
            Text.Font = oldFont;
            Text.Anchor = oldAnchor;

            GUI.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, hovered ? 0.95f : 0.45f);
            Widgets.DrawBox(rect, 2);
            GUI.color = Color.white;

            if (hovered)
            {
                MouseoverSounds.DoRegion(rect);
            }
            if (Widgets.ButtonInvisible(rect))
            {
                Reveal(designator, key);
            }
        }

        private static void DrawWindup(Rect rect, Designator designator, float progress)
        {
            GUI.DrawTexture(rect, Command.BGTex);
            Widgets.DrawBoxSolid(rect, TileColor);
            DrawSilhouette(rect, designator);
            Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, progress * progress * 0.85f));
        }

        private static void DrawReveal(Rect rect, Vector2 topLeft, float size, Designator designator, GizmoRenderParms parms, float t)
        {
            designator.GizmoOnGUI(topLeft, size, parms);

            var icon = BuildableOf(designator)?.uiIcon;
            if (icon != null)
            {
                var pop = Mathf.Clamp01(t / 0.45f);
                if (pop < 1f)
                {
                    var buildable = BuildableOf(designator);
                    var half = size * Mathf.Lerp(0.55f, 1f, EaseOutCubic(pop)) * 0.5f;
                    var tint = buildable.uiIconColor;
                    GUI.color = new Color(tint.r, tint.g, tint.b, 1f - pop);
                    Widgets.DrawTextureFitted(new Rect(rect.center.x - half, rect.center.y - half, half * 2f, half * 2f), icon, 0.9f);
                    GUI.color = Color.white;
                }
            }

            Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - t * 5f) * 0.85f));

            GUI.color = new Color(1f, 1f, 1f, 1f - t);
            Widgets.DrawBox(rect, 2);
            GUI.color = Color.white;
        }

        private static void DrawSilhouette(Rect rect, Designator designator)
        {
            var icon = BuildableOf(designator)?.uiIcon;
            if (icon == null) return;

            GUI.color = SilhouetteColor;
            Widgets.DrawTextureFitted(rect.ContractedBy(10f), icon, 0.9f);
            GUI.color = Color.white;
        }

        private static void ClearPendingFor(Designator designator)
        {
            if (designator is Designator_Build build)
            {
                MysteryUnlockTracker.ClearPending(build.PlacingDef?.defName);
            }
            else if (designator is Designator_Dropdown dropdown)
            {
                var elements = dropdown.Elements;
                for (var i = 0; i < elements.Count; i++)
                {
                    if (elements[i] is Designator_Build element)
                    {
                        MysteryUnlockTracker.ClearPending(element.PlacingDef?.defName);
                    }
                }
            }
        }

        private static void Reveal(Designator designator, string key)
        {
            ClearPendingFor(designator);

            var now = Time.realtimeSinceStartup;
            if (now >= revealsUntil)
            {
                activeReveals.Clear();
            }
            activeReveals[key] = now;
            revealsUntil = now + RevealDuration;
            DefsOf.BA_DiscoverDesignator?.PlayOneShotOnCamera();
        }

        private static bool IsHidden(Designator designator)
        {
            if (designator is Designator_Build build)
            {
                return MysteryUnlockTracker.IsPending(build.PlacingDef?.defName);
            }

            if (designator is Designator_Dropdown dropdown)
            {
                var elements = dropdown.Elements;
                var buildCount = 0;
                var pendingCount = 0;
                for (var i = 0; i < elements.Count; i++)
                {
                    if (elements[i] is Designator_Build element)
                    {
                        buildCount++;
                        if (MysteryUnlockTracker.IsPending(element.PlacingDef?.defName))
                        {
                            pendingCount++;
                        }
                    }
                }

                if (pendingCount == 0) return false;
                if (pendingCount == buildCount) return true;

                for (var i = 0; i < elements.Count; i++)
                {
                    if (elements[i] is Designator_Build element)
                    {
                        MysteryUnlockTracker.ClearPending(element.PlacingDef?.defName);
                    }
                }
            }

            return false;
        }

        private static string KeyFor(Designator designator)
        {
            if (designator is Designator_Build build)
            {
                return build.PlacingDef?.defName;
            }

            if (designator is Designator_Dropdown dropdown)
            {
                var elements = dropdown.Elements;
                for (var i = 0; i < elements.Count; i++)
                {
                    if (elements[i] is Designator_Build element)
                    {
                        return element.PlacingDef?.defName;
                    }
                }
            }

            return null;
        }

        private static BuildableDef BuildableOf(Designator designator)
        {
            if (designator is Designator_Build build) return build.PlacingDef;
            if (designator is Designator_Dropdown dropdown)
            {
                var elements = dropdown.Elements;
                for (var i = 0; i < elements.Count; i++)
                {
                    if (elements[i] is Designator_Build element)
                    {
                        return element.PlacingDef;
                    }
                }
            }
            return null;
        }

        private static float EaseOutCubic(float t)
        {
            var p = 1f - t;
            return 1f - p * p * p;
        }
    }
}
