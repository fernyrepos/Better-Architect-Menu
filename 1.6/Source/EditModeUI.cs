using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BetterArchitect
{
    public enum BamDragSection { None, CategoryList, DesignatorGrid, OrdersPanel }

    public static class BamDragDrop
    {
        public static BamDragSection ActiveSection = BamDragSection.None;
        public static bool IsDragging => ActiveSection != BamDragSection.None;

        // Designator drag fields
        public static int SourceIndex;
        public static Designator DraggedDesignator;
        public static DesignationCategoryDef SourceCategory;

        // Category drag fields
        public static DesignationCategoryDef DraggedCategory;
        public static DesignationCategoryDef MainCategory;

        // Pending (for 5px drag threshold)
        public static bool PendingDragStart;
        public static BamDragSection PendingSection;
        public static int PendingIndex;
        public static Designator PendingDesignator;
        public static DesignationCategoryDef PendingCategory;
        public static DesignationCategoryDef PendingMainCategory;

        // Shared
        public static Vector2 DragStartMousePos;
        public static int HoverTargetIndex = -1;

        public static void Clear()
        {
            ActiveSection = BamDragSection.None;
            PendingDragStart = false;
            PendingSection = BamDragSection.None;
            PendingIndex = -1;
            PendingDesignator = null;
            PendingCategory = null;
            PendingMainCategory = null;
            SourceIndex = -1;
            DraggedDesignator = null;
            SourceCategory = null;
            DraggedCategory = null;
            MainCategory = null;
            DragStartMousePos = Vector2.zero;
            HoverTargetIndex = -1;
        }
    }

    public static class CategoryEditControlsDrawer
    {
        public static void DrawCategoryEditControls(Rect leftRect, DesignationCategoryDef mainCat, DesignationCategoryDef selectedCategory)
        {
            var toolbarHeight = ArchitectCategoryTab_DesignationTabOnGUI_Patch.EditToolbarHeight;
            var toolbarRect = new Rect(leftRect.x + 4f, leftRect.yMax + 2f, leftRect.width - 8f, toolbarHeight);

            var parentId = mainCat.defName;
            var children = EditModeRuntime.GetChildrenForParent(parentId).ToList();
            var selectedId = selectedCategory != null ? selectedCategory.defName : null;
            BetterArchitectSettings.parentOverrides.TryGetValue(parentId, out var parentOverride);
            var replaceDefaults = parentOverride != null && parentOverride.replaceDefaultChildren;

            var buttonW = toolbarRect.height;
            var plusRect = new Rect(toolbarRect.x, toolbarRect.y, buttonW, buttonW);
            var minusRect = new Rect(plusRect.xMax + 3f, toolbarRect.y, buttonW, buttonW);
            var resetCategoriesRect = new Rect(minusRect.xMax + 3f, toolbarRect.y, buttonW, buttonW);
            var resetDesignatorsRect = new Rect(resetCategoriesRect.xMax + 3f, toolbarRect.y, buttonW, buttonW);

            if (DrawIconButton(plusRect, TexButton.Plus)) OpenAddCategoryMenu(parentId, children);

            var canModifySelected = !selectedId.NullOrEmpty() && children.Contains(selectedId);
            if (DrawIconButton(minusRect, TexButton.Delete) && canModifySelected)
            {
                var updated = replaceDefaults ? children.ToList() : EditModeRuntime.GetChildrenForParent(parentId).ToList();
                updated.Remove(selectedId);
                EditModeRuntime.SetChildrenForParent(parentId, updated, true);
            }
            if (DrawIconButton(resetCategoriesRect, Assets.RestoreIcon)) EditModeRuntime.SetChildrenForParent(parentId, new List<string>(), false);
            if (DrawIconButton(resetDesignatorsRect, Assets.RestoreIcon) && !selectedId.NullOrEmpty()) ResetDesignatorsForCategory(selectedId);

            TooltipHandler.TipRegion(plusRect, "BA.TooltipAddCategoryToParent".Translate());
            TooltipHandler.TipRegion(minusRect, "BA.TooltipRemoveSelectedChildCategory".Translate());
            TooltipHandler.TipRegion(resetCategoriesRect, "BA.TooltipResetCategoriesForParent".Translate());
            TooltipHandler.TipRegion(resetDesignatorsRect, "BA.TooltipResetDesignatorsForSelectedCategory".Translate());
        }

        private static bool DrawIconButton(Rect rect, Texture2D icon)
        {
            var iconRect = rect.ContractedBy(3f);
            return Widgets.ButtonImage(iconRect, icon);
        }

        private static void OpenAddCategoryMenu(string parentId, List<string> currentChildren)
        {
            var options = new List<FloatMenuOption>();
            var currentChildrenSet = new HashSet<string>(currentChildren);
            var parentCategoryIds = new HashSet<string>(EditModeRuntime.GetParents().Select(d => d.defName));
            foreach (var def in DefDatabase<DesignationCategoryDef>.AllDefsListForReading
                         .Where(d => d.defName != parentId &&
                                     !currentChildrenSet.Contains(d.defName) &&
                                     !parentCategoryIds.Contains(d.defName))
                         .OrderBy(d => d.LabelCap.ToString()))
            {
                var defName = def.defName;
                options.Add(new FloatMenuOption("BA.CategoryOptionFormat".Translate(def.LabelCap, defName), delegate
                {
                    var updated = EditModeRuntime.GetChildrenForParent(parentId).ToList();
                    if (!updated.Contains(defName))
                    {
                        updated.Add(defName);
                        EditModeRuntime.SetChildrenForParent(parentId, updated);
                    }
                }));
            }

            if (!options.Any()) options.Add(new FloatMenuOption("BA.NoCategoriesAvailable".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        internal static void MoveString(List<string> list, string value, int direction)
        {
            if (value.NullOrEmpty()) return;
            var idx = list.IndexOf(value);
            if (idx < 0) return;
            var target = idx + direction;
            if (target < 0 || target >= list.Count) return;
            var tmp = list[idx];
            list[idx] = list[target];
            list[target] = tmp;
        }

        internal static void MoveStringToIndex(List<string> list, string value, int targetIndex)
        {
            var idx = list.IndexOf(value);
            if (idx < 0 || idx == targetIndex) return;
            targetIndex = Mathf.Clamp(targetIndex, 0, list.Count - 1);
            list.RemoveAt(idx);
            list.Insert(targetIndex, value);
        }

        internal static void ResetDesignatorsForCategory(string categoryId)
        {
            var entry = BetterArchitectSettings.GetOrCreateCategoryOverride(categoryId);
            entry.replaceDefaultBuildables = false;
            entry.replaceDefaultSpecials = false;
            entry.buildableDefNames.Clear();
            entry.specialClassNames.Clear();
            entry.removedBuildableDefNames.Clear();
            BetterArchitectSettings.Save();
        }

        public static void TryHandleCategoryDrag(
            Rect outRect,
            float rowWidth,
            List<DesignationCategoryDef> displayCategories,
            DesignationCategoryDef mainCat,
            Vector2 scroll)
        {
            const float rowHeight = 36f;
            const float itemHeight = 41f; // 36 + 5 spacing
            var evt = Event.current;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                for (int i = 0; i < displayCategories.Count; i++)
                {
                    var rowOnScreen = new Rect(outRect.x, outRect.y + i * itemHeight - scroll.y, rowWidth, rowHeight);
                    if (rowOnScreen.yMax < outRect.y || rowOnScreen.y > outRect.yMax) continue;
                    if (!rowOnScreen.Contains(evt.mousePosition)) continue;

                    BamDragDrop.PendingDragStart = true;
                    BamDragDrop.PendingSection = BamDragSection.CategoryList;
                    BamDragDrop.PendingIndex = i;
                    BamDragDrop.PendingCategory = displayCategories[i];
                    BamDragDrop.PendingMainCategory = mainCat;
                    BamDragDrop.DragStartMousePos = evt.mousePosition;
                    return;
                }
            }

            if (evt.type == EventType.MouseDrag && evt.button == 0)
            {
                if (BamDragDrop.PendingDragStart &&
                    BamDragDrop.PendingSection == BamDragSection.CategoryList &&
                    BamDragDrop.PendingMainCategory == mainCat)
                {
                    if (Vector2.Distance(evt.mousePosition, BamDragDrop.DragStartMousePos) > 5f)
                    {
                        BamDragDrop.ActiveSection = BamDragSection.CategoryList;
                        BamDragDrop.SourceIndex = BamDragDrop.PendingIndex;
                        BamDragDrop.DraggedCategory = BamDragDrop.PendingCategory;
                        BamDragDrop.MainCategory = mainCat;
                        BamDragDrop.PendingDragStart = false;
                        evt.Use();
                    }
                }
                else if (BamDragDrop.IsDragging &&
                         BamDragDrop.ActiveSection == BamDragSection.CategoryList &&
                         BamDragDrop.MainCategory == mainCat)
                {
                    BamDragDrop.HoverTargetIndex = GetCategoryIndexAtMouse(outRect, rowWidth, displayCategories, scroll, evt.mousePosition);
                    evt.Use();
                }
            }

            if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                if (BamDragDrop.PendingDragStart && BamDragDrop.PendingSection == BamDragSection.CategoryList)
                {
                    BamDragDrop.Clear();
                    return;
                }

                if (BamDragDrop.IsDragging &&
                    BamDragDrop.ActiveSection == BamDragSection.CategoryList &&
                    BamDragDrop.MainCategory == mainCat)
                {
                    var dropIndex = GetCategoryIndexAtMouse(outRect, rowWidth, displayCategories, scroll, evt.mousePosition);
                    if (dropIndex >= 0 && dropIndex != BamDragDrop.SourceIndex && BamDragDrop.DraggedCategory != null)
                    {
                        var children = EditModeRuntime.GetChildrenForParent(mainCat.defName).ToList();
                        var draggedId = BamDragDrop.DraggedCategory.defName;
                        foreach (var cat in displayCategories)
                        {
                            if (!children.Contains(cat.defName)) children.Add(cat.defName);
                        }
                        if (!children.Contains(draggedId)) children.Add(draggedId);
                        var targetId = displayCategories[dropIndex].defName;
                        var targetChildIndex = children.IndexOf(targetId);
                        if (targetChildIndex < 0) targetChildIndex = dropIndex;
                        MoveStringToIndex(children, draggedId, targetChildIndex);
                        EditModeRuntime.SetChildrenForParent(mainCat.defName, children, true);
                    }
                    BamDragDrop.Clear();
                    evt.Use();
                }
            }
        }

        private static int GetCategoryIndexAtMouse(
            Rect outRect, float rowWidth, List<DesignationCategoryDef> displayCategories, Vector2 scroll, Vector2 mousePos)
        {
            const float rowHeight = 36f;
            const float itemHeight = 41f;
            for (int i = 0; i < displayCategories.Count; i++)
            {
                var rowOnScreen = new Rect(outRect.x, outRect.y + i * itemHeight - scroll.y, rowWidth, rowHeight);
                if (rowOnScreen.Contains(mousePos)) return i;
            }
            return -1;
        }

        public static void DrawCategoryDropHighlight(
            Rect outRect, float rowWidth, List<DesignationCategoryDef> displayCategories, Vector2 scroll)
        {
            if (!BamDragDrop.IsDragging || BamDragDrop.ActiveSection != BamDragSection.CategoryList) return;
            if (BamDragDrop.HoverTargetIndex < 0 || BamDragDrop.HoverTargetIndex >= displayCategories.Count) return;

            const float rowHeight = 36f;
            const float itemHeight = 41f;
            var i = BamDragDrop.HoverTargetIndex;
            var rowOnScreen = new Rect(outRect.x, outRect.y + i * itemHeight - scroll.y, rowWidth, rowHeight);
            if (rowOnScreen.yMax < outRect.y || rowOnScreen.y > outRect.yMax) return;

            GUI.color = new Color(1f, 1f, 0f, 0.5f);
            Widgets.DrawBox(rowOnScreen, 2);
            GUI.color = Color.white;
        }

        public static void DrawCategoryGhost()
        {
            if (!BamDragDrop.IsDragging || BamDragDrop.ActiveSection != BamDragSection.CategoryList) return;
            if (BamDragDrop.DraggedCategory == null) return;
            if (Event.current.type != EventType.Repaint) return;

            var mousePos = Event.current.mousePosition;
            const float ghostHeight = 36f;
            const float ghostWidth = 180f;
            var ghostRect = new Rect(mousePos.x + 12f, mousePos.y - ghostHeight / 2f, ghostWidth, ghostHeight);

            GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            GUI.DrawTexture(ghostRect, Texture2D.whiteTexture);
            GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            Widgets.DrawBox(ghostRect, 1);

            var icon = ArchitectIcons.Resources.FindArchitectTabCategoryIcon(BamDragDrop.DraggedCategory.defName);
            if (icon != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.8f);
                Widgets.DrawTextureFitted(new Rect(ghostRect.x + 4f, ghostRect.y + 8f, 20f, 20f), icon, 1f);
            }

            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(ghostRect.x + 28f, ghostRect.y, ghostRect.width - 32f, ghostRect.height), BamDragDrop.DraggedCategory.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
    }

    public static class Bam_InlineDesignatorEditor
    {
        public static void TryHandleOverlayInput(Rect rect, List<Designator> designators, Vector2 scroll, bool twoColumns, DesignationCategoryDef category)
        {
            var section = twoColumns ? BamDragSection.OrdersPanel : BamDragSection.DesignatorGrid;
            TryHandleDesignatorDrag(rect, designators, scroll, twoColumns, category, section);

            if (BamDragDrop.IsDragging || BamDragDrop.PendingDragStart) return;

            if (Event.current.type != EventType.MouseDown)
            {
                return;
            }

            const float gizmoSize = 75f;
            const float gizmoSpacing = 5f;
            const float rowHeight = gizmoSize + gizmoSpacing + 5f;
            var perRow = twoColumns ? 2 : Mathf.Max(1, Mathf.FloorToInt((rect.width - 16f) / (gizmoSize + gizmoSpacing)));

            for (int i = 0; i < designators.Count; i++)
            {
                var row = i / perRow;
                var col = i % perRow;
                var gizmoRect = new Rect(rect.x + col * (gizmoSize + gizmoSpacing), rect.y + row * rowHeight - scroll.y, gizmoSize, gizmoSize);
                if (gizmoRect.yMax < rect.y || gizmoRect.y > rect.yMax) continue;

                if (HandleOverlayClick(gizmoRect, designators, i, category))
                {
                    Event.current.Use();
                    return;
                }
            }

            if (!(twoColumns && ArchitectCategoryTab_DesignationTabOnGUI_Patch.IsSpecialCategory(category)))
            {
                var plusIndex = designators.Count;
                var plusRow = plusIndex / perRow;
                var plusCol = plusIndex % perRow;
                var plusRect = new Rect(rect.x + plusCol * (gizmoSize + gizmoSpacing) + 25f, rect.y + plusRow * rowHeight - scroll.y + 25f, 24f, 24f);
                if (plusRect.yMax >= rect.y && plusRect.y <= rect.yMax && Mouse.IsOver(plusRect))
                {
                    OpenAddDesignatorMenu(category);
                    Event.current.Use();
                }
            }
        }

        public static void TryHandleDesignatorDrag(
            Rect rect,
            List<Designator> designators,
            Vector2 scroll,
            bool twoColumns,
            DesignationCategoryDef category,
            BamDragSection section)
        {
            const float gizmoSize = 75f;
            const float gizmoSpacing = 5f;
            const float rowHeight = gizmoSize + gizmoSpacing + 5f;
            var perRow = twoColumns ? 2 : Mathf.Max(1, Mathf.FloorToInt((rect.width - 16f) / (gizmoSize + gizmoSpacing)));
            var evt = Event.current;

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                for (int i = 0; i < designators.Count; i++)
                {
                    var row = i / perRow;
                    var col = i % perRow;
                    var gizmoRect = new Rect(
                        rect.x + col * (gizmoSize + gizmoSpacing),
                        rect.y + row * rowHeight - scroll.y,
                        gizmoSize, gizmoSize);

                    if (gizmoRect.yMax < rect.y || gizmoRect.y > rect.yMax) continue;
                    if (!gizmoRect.Contains(evt.mousePosition)) continue;

                    // Exclude the minus button area
                    var minusRect = new Rect(gizmoRect.xMax - 14f, gizmoRect.y + 2f, 12f, 12f);
                    var minusButtonRect = minusRect.ExpandedBy(2f);
                    var minusExtraHeight = minusButtonRect.height * 0.5f;
                    minusButtonRect.y -= minusExtraHeight * 0.5f;
                    minusButtonRect.height += minusExtraHeight;
                    if (minusButtonRect.Contains(evt.mousePosition)) break;

                    BamDragDrop.PendingDragStart = true;
                    BamDragDrop.PendingSection = section;
                    BamDragDrop.PendingIndex = i;
                    BamDragDrop.PendingDesignator = designators[i];
                    BamDragDrop.PendingCategory = category;
                    BamDragDrop.DragStartMousePos = evt.mousePosition;
                    return;
                }
            }

            if (evt.type == EventType.MouseDrag && evt.button == 0)
            {
                if (BamDragDrop.PendingDragStart &&
                    BamDragDrop.PendingSection == section &&
                    BamDragDrop.PendingCategory == category)
                {
                    if (Vector2.Distance(evt.mousePosition, BamDragDrop.DragStartMousePos) > 5f)
                    {
                        BamDragDrop.ActiveSection = section;
                        BamDragDrop.SourceIndex = BamDragDrop.PendingIndex;
                        BamDragDrop.DraggedDesignator = BamDragDrop.PendingDesignator;
                        BamDragDrop.SourceCategory = category;
                        BamDragDrop.PendingDragStart = false;
                        evt.Use();
                    }
                }
                else if (BamDragDrop.IsDragging &&
                         BamDragDrop.ActiveSection == section &&
                         BamDragDrop.SourceCategory == category)
                {
                    BamDragDrop.HoverTargetIndex = GetDesignatorIndexAtMouse(rect, designators, scroll, twoColumns, evt.mousePosition);
                    evt.Use();
                }
            }

            if (evt.type == EventType.MouseUp && evt.button == 0)
            {
                if (BamDragDrop.PendingDragStart && BamDragDrop.PendingSection == section)
                {
                    BamDragDrop.Clear();
                    return;
                }

                if (BamDragDrop.IsDragging &&
                    BamDragDrop.ActiveSection == section &&
                    BamDragDrop.SourceCategory == category)
                {
                    var dropIndex = GetDesignatorIndexAtMouse(rect, designators, scroll, twoColumns, evt.mousePosition);
                    if (dropIndex >= 0 && dropIndex != BamDragDrop.SourceIndex && BamDragDrop.DraggedDesignator != null)
                    {
                        MoveDesignatorToIndex(category, designators, BamDragDrop.DraggedDesignator, dropIndex);
                    }
                    BamDragDrop.Clear();
                    evt.Use();
                }
            }
        }

        private static int GetDesignatorIndexAtMouse(
            Rect rect, List<Designator> designators, Vector2 scroll, bool twoColumns, Vector2 mousePos)
        {
            const float gizmoSize = 75f;
            const float gizmoSpacing = 5f;
            const float rowHeight = gizmoSize + gizmoSpacing + 5f;
            var perRow = twoColumns ? 2 : Mathf.Max(1, Mathf.FloorToInt((rect.width - 16f) / (gizmoSize + gizmoSpacing)));

            for (int i = 0; i < designators.Count; i++)
            {
                var row = i / perRow;
                var col = i % perRow;
                var gizmoRect = new Rect(
                    rect.x + col * (gizmoSize + gizmoSpacing),
                    rect.y + row * rowHeight - scroll.y,
                    gizmoSize, gizmoSize);
                if (gizmoRect.Contains(mousePos)) return i;
            }
            return -1;
        }

        public static void DrawGridOverlay(Rect rect, List<Designator> designators, Vector2 scroll, bool twoColumns, DesignationCategoryDef category)
        {
            const float gizmoSize = 75f;
            const float gizmoSpacing = 5f;
            const float rowHeight = gizmoSize + gizmoSpacing + 5f;

            var perRow = twoColumns ? 2 : Mathf.Max(1, Mathf.FloorToInt((rect.width - 16f) / (gizmoSize + gizmoSpacing)));

            for (int i = 0; i < designators.Count; i++)
            {
                var row = i / perRow;
                var col = i % perRow;
                var gizmoRect = new Rect(rect.x + col * (gizmoSize + gizmoSpacing), rect.y + row * rowHeight - scroll.y, gizmoSize, gizmoSize);
                if (gizmoRect.yMax < rect.y || gizmoRect.y > rect.yMax) continue;
                DrawButtonsForDesignator(gizmoRect, designators, i, category);
            }

            if (!(twoColumns && ArchitectCategoryTab_DesignationTabOnGUI_Patch.IsSpecialCategory(category)))
            {
                var plusIndex = designators.Count;
                var plusRow = plusIndex / perRow;
                var plusCol = plusIndex % perRow;
                var plusRect = new Rect(rect.x + plusCol * (gizmoSize + gizmoSpacing) + 25f, rect.y + plusRow * rowHeight - scroll.y + 25f, 24f, 24f);
                if (plusRect.yMax >= rect.y && plusRect.y <= rect.yMax)
                {
                    if (Widgets.ButtonImage(plusRect, TexButton.Plus)) OpenAddDesignatorMenu(category);
                    TooltipHandler.TipRegion(plusRect, "BA.TooltipAddBuildableOrSpecialDesignator".Translate());
                }
            }

            var activeSection = twoColumns ? BamDragSection.OrdersPanel : BamDragSection.DesignatorGrid;
            DrawDesignatorDropHighlight(rect, designators, scroll, twoColumns, activeSection);
            DrawDesignatorGhost(activeSection);
        }

        private static void DrawDesignatorDropHighlight(
            Rect rect, List<Designator> designators, Vector2 scroll, bool twoColumns, BamDragSection section)
        {
            if (!BamDragDrop.IsDragging || BamDragDrop.ActiveSection != section) return;
            if (BamDragDrop.HoverTargetIndex < 0 || BamDragDrop.HoverTargetIndex >= designators.Count) return;

            const float gizmoSize = 75f;
            const float gizmoSpacing = 5f;
            const float rowHeight = gizmoSize + gizmoSpacing + 5f;
            var perRow = twoColumns ? 2 : Mathf.Max(1, Mathf.FloorToInt((rect.width - 16f) / (gizmoSize + gizmoSpacing)));

            var i = BamDragDrop.HoverTargetIndex;
            var row = i / perRow;
            var col = i % perRow;
            var gizmoRect = new Rect(
                rect.x + col * (gizmoSize + gizmoSpacing),
                rect.y + row * rowHeight - scroll.y,
                gizmoSize, gizmoSize);

            if (gizmoRect.yMax < rect.y || gizmoRect.y > rect.yMax) return;

            GUI.color = new Color(1f, 1f, 0f, 0.5f);
            Widgets.DrawBox(gizmoRect, 2);
            GUI.color = Color.white;
        }

        private static void DrawDesignatorGhost(BamDragSection section)
        {
            if (!BamDragDrop.IsDragging || BamDragDrop.ActiveSection != section) return;
            if (BamDragDrop.DraggedDesignator == null) return;
            if (Event.current.type != EventType.Repaint) return;

            var mousePos = Event.current.mousePosition;
            const float ghostSize = 75f;
            var ghostRect = new Rect(mousePos.x - ghostSize / 2f, mousePos.y - ghostSize / 2f, ghostSize, ghostSize);

            GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
            GUI.DrawTexture(ghostRect, Texture2D.whiteTexture);

            var icon = BamDragDrop.DraggedDesignator.icon;
            if (icon != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.7f);
                Widgets.DrawTextureFitted(ghostRect.ContractedBy(8f), icon, 1f);
            }

            GUI.color = Color.white;
        }

        private static void DrawButtonsForDesignator(Rect gizmoRect, List<Designator> designators, int index, DesignationCategoryDef category)
        {
            var designator = designators[index];

            var minusRect = new Rect(gizmoRect.xMax - 14f, gizmoRect.y + 2f, 12f, 12f);
            var minusButtonRect = minusRect.ExpandedBy(2f);
            var minusExtraHeight = minusButtonRect.height * 0.5f;
            minusButtonRect.y -= minusExtraHeight * 0.5f;
            minusButtonRect.height += minusExtraHeight;

            if (Widgets.ButtonImage(minusButtonRect, TexButton.Minus)) RemoveDesignator(category, designators, designator);

            TooltipHandler.TipRegion(minusButtonRect, "BA.TooltipRemoveDesignator".Translate());
        }

        private static bool HandleOverlayClick(Rect gizmoRect, List<Designator> designators, int index, DesignationCategoryDef category)
        {
            var designator = designators[index];
            var minusRect = new Rect(gizmoRect.xMax - 14f, gizmoRect.y + 2f, 12f, 12f);
            var minusButtonRect = minusRect.ExpandedBy(2f);
            var minusExtraHeight = minusButtonRect.height * 0.5f;
            minusButtonRect.y -= minusExtraHeight * 0.5f;
            minusButtonRect.height += minusExtraHeight;

            if (Mouse.IsOver(minusButtonRect))
            {
                RemoveDesignator(category, designators, designator);
                return true;
            }

            return false;
        }

        private static void RemoveDesignator(DesignationCategoryDef category, List<Designator> currentDesignators, Designator target)
        {
            var entry = BetterArchitectSettings.GetOrCreateCategoryOverride(category.defName);
            var buildableDefName = GetBuildableDefName(target);

            if (!buildableDefName.NullOrEmpty())
            {
                if (entry.replaceDefaultBuildables) entry.buildableDefNames.Remove(buildableDefName);
                else if (!entry.removedBuildableDefNames.Contains(buildableDefName)) entry.removedBuildableDefNames.Add(buildableDefName);
                entry.buildableDefNames.Remove(buildableDefName);
            }
            else
            {
                var className = target.GetType().FullName;
                if (!entry.replaceDefaultSpecials)
                {
                    entry.replaceDefaultSpecials = true;
                    entry.specialClassNames = BuildSpecialClassSeedList(currentDesignators, entry);
                }
                entry.specialClassNames.Remove(className);
            }

            BetterArchitectSettings.Save();
        }

        private static void MoveDesignator(DesignationCategoryDef category, List<Designator> currentDesignators, Designator target, int dir)
        {
            var entry = BetterArchitectSettings.GetOrCreateCategoryOverride(category.defName);
            var buildableDefName = GetBuildableDefName(target);

            if (!buildableDefName.NullOrEmpty())
            {
                EnsureBuildableOrderSeeded(entry, currentDesignators);
                if (!entry.buildableDefNames.Contains(buildableDefName)) entry.buildableDefNames.Add(buildableDefName);
                CategoryEditControlsDrawer.MoveString(entry.buildableDefNames, buildableDefName, dir);
                entry.removedBuildableDefNames.Remove(buildableDefName);
            }
            else
            {
                var className = target.GetType().FullName;
                if (!entry.replaceDefaultSpecials)
                {
                    entry.replaceDefaultSpecials = true;
                    entry.specialClassNames = BuildSpecialClassSeedList(currentDesignators, entry);
                }
                if (!entry.specialClassNames.Contains(className)) entry.specialClassNames.Add(className);
                CategoryEditControlsDrawer.MoveString(entry.specialClassNames, className, dir);
            }

            BetterArchitectSettings.Save();
        }

        private static void MoveDesignatorToIndex(DesignationCategoryDef category, List<Designator> currentDesignators, Designator target, int targetIndex)
        {
            var entry = BetterArchitectSettings.GetOrCreateCategoryOverride(category.defName);
            var buildableDefName = GetBuildableDefName(target);

            if (!buildableDefName.NullOrEmpty())
            {
                EnsureBuildableOrderSeeded(entry, currentDesignators);
                if (!entry.buildableDefNames.Contains(buildableDefName))
                    entry.buildableDefNames.Add(buildableDefName);
                entry.removedBuildableDefNames.Remove(buildableDefName);
                CategoryEditControlsDrawer.MoveStringToIndex(entry.buildableDefNames, buildableDefName, targetIndex);
            }
            else
            {
                var className = target.GetType().FullName;
                if (!entry.replaceDefaultSpecials)
                {
                    entry.replaceDefaultSpecials = true;
                    entry.specialClassNames = BuildSpecialClassSeedList(currentDesignators, entry);
                }
                if (!entry.specialClassNames.Contains(className))
                    entry.specialClassNames.Add(className);
                CategoryEditControlsDrawer.MoveStringToIndex(entry.specialClassNames, className, targetIndex);
            }

            BetterArchitectSettings.Save();
        }

        private static void EnsureBuildableOrderSeeded(CategoryOverride entry, List<Designator> currentDesignators)
        {
            var orderedVisible = currentDesignators
                .Select(GetBuildableDefName)
                .ToList();

            if (entry.buildableDefNames == null || entry.buildableDefNames.Count == 0)
            {
                entry.buildableDefNames = orderedVisible;
                return;
            }

            var merged = new List<string>(orderedVisible);
            foreach (var existing in entry.buildableDefNames)
            {
                if (!merged.Contains(existing))
                {
                    merged.Add(existing);
                }
            }

            entry.buildableDefNames = merged;
        }

        private static void OpenAddDesignatorMenu(DesignationCategoryDef category)
        {
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("BA.AddBuildable".Translate(), delegate
                {
                    Find.WindowStack.Add(new DesignatorSearchWindow(
                        "BA.AddBuildableTitle".Translate(),
                        DesignatorSearchMode.Buildable,
                        delegate(string value) { AddBuildableToCurrentCategory(value, category); }));
                }),
                new FloatMenuOption("BA.AddSpecialDesignator".Translate(), delegate
                {
                    Find.WindowStack.Add(new DesignatorSearchWindow(
                        "BA.AddSpecialDesignatorTitle".Translate(),
                        DesignatorSearchMode.Special,
                        delegate(string value) { AddSpecialToCurrentCategory(value, category); }));
                })
            };
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void AddBuildableToCurrentCategory(string defName, DesignationCategoryDef category)
        {
            var entry = BetterArchitectSettings.GetOrCreateCategoryOverride(category.defName);
            if (!entry.buildableDefNames.Contains(defName)) entry.buildableDefNames.Add(defName);
            entry.removedBuildableDefNames.Remove(defName);
            BetterArchitectSettings.Save();
        }

        private static void AddSpecialToCurrentCategory(string className, DesignationCategoryDef category)
        {
            var entry = BetterArchitectSettings.GetOrCreateCategoryOverride(category.defName);
            if (!entry.specialClassNames.Contains(className)) entry.specialClassNames.Add(className);
            BetterArchitectSettings.Save();
        }

        private static string GetBuildableDefName(Designator d)
        {
            if (d is Designator_Build db) return db.PlacingDef != null ? db.PlacingDef.defName : null;
            if (d is Designator_Dropdown dd)
            {
                var nested = dd.Elements.OfType<Designator_Build>().FirstOrDefault();
                return nested != null && nested.PlacingDef != null ? nested.PlacingDef.defName : null;
            }
            return null;
        }

        private static List<string> BuildSpecialClassSeedList(List<Designator> currentDesignators, CategoryOverride entry)
        {
            var result = new List<string>();

            // Seed from the current display order to preserve relative positions of all
            // designators (including user-added ones that appear before defaults in the grid).
            for (int i = 0; i < currentDesignators.Count; i++)
            {
                var designator = currentDesignators[i];
                if (!GetBuildableDefName(designator).NullOrEmpty())
                {
                    continue;
                }

                AddUniqueClassName(result, designator.GetType().FullName);
            }

            // Append any tracked class names not currently visible (e.g. hidden designators).
            for (int i = 0; i < entry.specialClassNames.Count; i++)
            {
                AddUniqueClassName(result, entry.specialClassNames[i]);
            }

            return result;
        }

        private static void AddUniqueClassName(List<string> classNames, string className)
        {
            if (classNames.Contains(className))
            {
                return;
            }

            classNames.Add(className);
        }
    }

    public enum DesignatorSearchMode
    {
        Buildable,
        Special
    }

    public class DesignatorChoice
    {
        public string key;
        public string label;
        public string secondary;
        public string searchText;
    }

    public static class DesignatorSearchCache
    {
        private static readonly List<DesignatorChoice> buildables = new List<DesignatorChoice>();
        private static readonly List<DesignatorChoice> specials = new List<DesignatorChoice>();
        private static bool hasBuiltCache;

        public static IReadOnlyList<DesignatorChoice> GetChoices(DesignatorSearchMode mode)
        {
            if (!hasBuiltCache)
            {
                Rebuild();
            }

            return mode == DesignatorSearchMode.Buildable ? buildables : specials;
        }

        public static void Rebuild()
        {
            buildables.Clear();
            specials.Clear();

            foreach (var def in DefDatabase<BuildableDef>.AllDefsListForReading)
            {
                if (!def.BuildableByPlayer)
                {
                    continue;
                }

                var label = def.LabelCap.ToString();
                if (label.NullOrEmpty())
                {
                    label = def.defName;
                }

                buildables.Add(new DesignatorChoice
                {
                    key = def.defName,
                    label = label,
                    secondary = def.defName,
                    searchText = (label + " " + def.defName).ToLowerInvariant()
                });
            }

            var classNames = new HashSet<string>();
            foreach (var cat in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
            {
                if (cat.specialDesignatorClasses != null)
                {
                    foreach (var type in cat.specialDesignatorClasses)
                    {
                        if (type != null && !type.FullName.NullOrEmpty())
                        {
                            classNames.Add(type.FullName);
                        }
                    }
                }

                if (cat.ResolvedAllowedDesignators != null)
                {
                    foreach (var d in cat.ResolvedAllowedDesignators)
                    {
                        if (d is Designator_Build)
                        {
                            continue;
                        }

                        var fullName = d != null ? d.GetType().FullName : null;
                        if (!fullName.NullOrEmpty())
                        {
                            classNames.Add(fullName);
                        }
                    }
                }
            }

            foreach (var entry in BetterArchitectSettings.categoryOverrides.Values)
            {
                foreach (var className in entry.specialClassNames)
                {
                    if (!className.NullOrEmpty())
                    {
                        classNames.Add(className);
                    }
                }
            }

            foreach (var className in classNames)
            {
                var simpleName = className.Split('.').Last();
                specials.Add(new DesignatorChoice
                {
                    key = className,
                    label = simpleName,
                    secondary = className,
                    searchText = (simpleName + " " + className).ToLowerInvariant()
                });
            }

            buildables.Sort(delegate (DesignatorChoice a, DesignatorChoice b)
            {
                var cmp = string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
                return string.Compare(a.secondary, b.secondary, StringComparison.OrdinalIgnoreCase);
            });

            specials.Sort(delegate (DesignatorChoice a, DesignatorChoice b)
            {
                var cmp = string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase);
                if (cmp != 0) return cmp;
                return string.Compare(a.secondary, b.secondary, StringComparison.OrdinalIgnoreCase);
            });

            hasBuiltCache = true;
        }

        public static void Invalidate()
        {
            hasBuiltCache = false;
        }
    }

    public class DesignatorSearchWindow : Window
    {
        private readonly Action<string> onPick;
        private readonly string title;
        private readonly DesignatorSearchMode mode;

        private string query = "";
        private string lastQuery = null;
        private readonly List<DesignatorChoice> filtered = new List<DesignatorChoice>();
        private Vector2 scrollPosition;
        private bool focusSearchOnOpen = true;

        private const float RowHeight = 30f;
        private const float FooterHeight = 30f;
        private const float SearchHeight = 30f;
        private const float HeaderHeight = 28f;
        private const string SearchFieldControlName = "BAMEditMode_DesignatorSearchField";

        public override Vector2 InitialSize { get { return new Vector2(760f, 620f); } }

        public DesignatorSearchWindow(string title, DesignatorSearchMode mode, Action<string> onPick)
        {
            this.title = title;
            this.mode = mode;
            this.onPick = onPick;
            forcePause = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0f, 0f, inRect.width, HeaderHeight), title);

            GUI.SetNextControlName(SearchFieldControlName);
            query = Widgets.TextField(new Rect(0f, HeaderHeight + 4f, inRect.width, SearchHeight - 4f), query);
            if (focusSearchOnOpen && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl(SearchFieldControlName);
                focusSearchOnOpen = false;
            }
            var listRect = new Rect(0f, HeaderHeight + SearchHeight + 4f, inRect.width, inRect.height - HeaderHeight - SearchHeight - FooterHeight - 8f);
            var footerRect = new Rect(0f, listRect.yMax + 4f, inRect.width, FooterHeight);

            DrawChoiceList(listRect);

            var sourceCount = DesignatorSearchCache.GetChoices(mode).Count;
            Widgets.Label(new Rect(0f, footerRect.y + 6f, footerRect.width - 120f, 24f), "BA.SearchShowing".Translate(filtered.Count, sourceCount));
            if (Widgets.ButtonText(new Rect(footerRect.width - 100f, footerRect.y + 2f, 100f, 24f), "BA.Close".Translate()))
            {
                Close();
            }
        }

        private void DrawChoiceList(Rect outRect)
        {
            RefreshFilterIfNeeded();

            var viewRect = new Rect(0f, 0f, outRect.width - 16f, filtered.Count * RowHeight);
            Widgets.DrawBoxSolid(outRect, new Color(0f, 0f, 0f, 0.2f));
            Widgets.DrawBox(outRect, 1);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            var start = Mathf.Max(0, Mathf.FloorToInt(scrollPosition.y / RowHeight));
            var visible = Mathf.CeilToInt(outRect.height / RowHeight) + 2;
            var end = Mathf.Min(filtered.Count, start + visible);

            for (int i = start; i < end; i++)
            {
                var rowRect = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                DrawChoiceRow(rowRect, filtered[i], query);
            }

            Widgets.EndScrollView();
        }

        private void DrawChoiceRow(Rect rowRect, DesignatorChoice choice, string filterQuery)
        {
            Widgets.DrawHighlightIfMouseover(rowRect);
            if (Widgets.ButtonInvisible(rowRect))
            {
                if (onPick != null && !choice.key.NullOrEmpty())
                {
                    onPick(choice.key);
                }
                Close();
                return;
            }

            var labelRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, rowRect.width * 0.45f, rowRect.height - 4f);
            var secondaryRect = new Rect(rowRect.x + rowRect.width * 0.45f + 12f, rowRect.y + 2f, rowRect.width * 0.55f - 16f, rowRect.height - 4f);
            DrawHighlightedLabel(labelRect, choice.label, filterQuery, Color.white);
            DrawHighlightedLabel(secondaryRect, choice.secondary, filterQuery, Color.gray);
        }

        private void RefreshFilterIfNeeded()
        {
            var normalized = query.NullOrEmpty() ? "" : query.Trim().ToLowerInvariant();
            if (normalized == lastQuery)
            {
                return;
            }

            lastQuery = normalized;
            filtered.Clear();
            var source = DesignatorSearchCache.GetChoices(mode);
            if (normalized.NullOrEmpty())
            {
                filtered.AddRange(source);
                return;
            }

            foreach (var choice in source)
            {
                if (choice.searchText.Contains(normalized))
                {
                    filtered.Add(choice);
                }
            }
        }

        private static void DrawHighlightedLabel(Rect rect, string text, string filterQuery, Color baseColor)
        {
            if (text.NullOrEmpty())
            {
                return;
            }

            if (filterQuery.NullOrEmpty())
            {
                GUI.color = baseColor;
                Widgets.Label(rect, text);
                GUI.color = Color.white;
                return;
            }

            var idx = text.IndexOf(filterQuery, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                GUI.color = baseColor;
                Widgets.Label(rect, text);
                GUI.color = Color.white;
                return;
            }

            var before = text.Substring(0, idx);
            var match = text.Substring(idx, filterQuery.Length > text.Length - idx ? text.Length - idx : filterQuery.Length);
            var after = text.Substring(idx + match.Length);

            var beforeSize = Text.CalcSize(before).x;
            var matchSize = Text.CalcSize(match).x;

            GUI.color = baseColor;
            Widgets.Label(rect, before);

            var matchRect = new Rect(rect.x + beforeSize, rect.y + 2f, matchSize, rect.height - 4f);
            Widgets.DrawBoxSolid(matchRect, new Color(1f, 0.92f, 0.35f, 0.35f));
            GUI.color = Color.white;
            Widgets.Label(new Rect(rect.x + beforeSize, rect.y, rect.width - beforeSize, rect.height), match);

            GUI.color = baseColor;
            Widgets.Label(new Rect(rect.x + beforeSize + matchSize, rect.y, rect.width - beforeSize - matchSize, rect.height), after);
            GUI.color = Color.white;
        }
    }
}
