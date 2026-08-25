using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BetterArchitect
{
    [HotSwappable]
    public static class DebugUiOrderOverlay
    {
        private const float BadgeHeight = 14f;
        private static readonly Color BadgeBackgroundColor = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color BadgeTextColor = new Color(1f, 0.85f, 0.35f);

        public static void DrawBadges(List<Designator> designators, Vector2 origin, int perRow, float gizmoSize, float gizmoSpacing, float rowHeight)
        {
            if (Event.current.type != EventType.Repaint) return;

            var oldFont = Text.Font;
            var oldAnchor = Text.Anchor;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            for (int i = 0; i < designators.Count; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                var badgeRect = new Rect(
                    origin.x + col * (gizmoSize + gizmoSpacing),
                    origin.y + row * rowHeight + gizmoSize - BadgeHeight,
                    gizmoSize,
                    BadgeHeight);
                Widgets.DrawBoxSolid(badgeRect, BadgeBackgroundColor);
                GUI.color = BadgeTextColor;
                Widgets.Label(badgeRect, designators[i].Order.ToString("0.##"));
                GUI.color = Color.white;
            }

            Text.Font = oldFont;
            Text.Anchor = oldAnchor;
        }
    }
}
