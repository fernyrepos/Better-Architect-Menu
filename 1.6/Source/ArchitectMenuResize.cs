using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BetterArchitect
{
    public static class ArchitectMenuResize
    {
        private const float GripSize = 18f;
        private static readonly Color IdleColor = new Color(1f, 1f, 1f, 0.45f);

        private static bool dragging;
        private static int draggedColumns;
        private static Rect gripRect;

        public static int ResolveColumns(int maxColumns)
        {
            var chosen = BetterArchitectSettings.gridColumns;
            return chosen <= 0 ? maxColumns : Mathf.Clamp(chosen, 1, maxColumns);
        }

        public static void HandleInput(Rect menuRect, float leftWidth, float ordersWidth, float columnWidth, int maxColumns)
        {
            gripRect = new Rect(menuRect.xMax - GripSize, menuRect.yMax - GripSize, GripSize, GripSize);
            var ev = Event.current;

            if (!dragging)
            {
                if (ev.type != EventType.MouseDown || !Mouse.IsOver(gripRect)) return;
                if (ev.button == 1)
                {
                    BetterArchitectSettings.gridColumns = 0;
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    BetterArchitectSettings.Save();
                }
                else if (ev.button == 0)
                {
                    dragging = true;
                    draggedColumns = ResolveColumns(maxColumns);
                }
                ev.Use();
                return;
            }

            if (ev.rawType == EventType.MouseUp || !Input.GetMouseButton(0))
            {
                dragging = false;
                BetterArchitectSettings.Save();
                if (ev.rawType == EventType.MouseUp) ev.Use();
                return;
            }

            var columns = Mathf.RoundToInt((ev.mousePosition.x - menuRect.x - leftWidth - ordersWidth - 16f) / columnWidth);
            columns = Mathf.Clamp(columns, 1, maxColumns);
            if (columns != draggedColumns)
            {
                draggedColumns = columns;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            }
            BetterArchitectSettings.gridColumns = columns >= maxColumns ? 0 : columns;
            if (ev.type == EventType.MouseDrag) ev.Use();
        }

        public static void Draw()
        {
            if (gripRect.width <= 0f) return;
            var hovered = Mouse.IsOver(gripRect);

            if (Event.current.type == EventType.Repaint)
            {
                var color = hovered || dragging ? Color.white : IdleColor;
                for (int i = 1; i <= 3; i++)
                {
                    var offset = i * 5f + 1f;
                    Widgets.DrawLine(
                        new Vector2(gripRect.xMax - offset, gripRect.yMax),
                        new Vector2(gripRect.xMax, gripRect.yMax - offset),
                        color,
                        1f);
                }
            }

            if (hovered && !dragging) TooltipHandler.TipRegion(gripRect, "BA.ResizeWidthTip".Translate());
        }
    }
}
