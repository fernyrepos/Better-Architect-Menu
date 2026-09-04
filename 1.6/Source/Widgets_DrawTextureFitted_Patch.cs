using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BetterArchitect
{
    [HarmonyPatch(typeof(Widgets), nameof(Widgets.DrawTextureFitted), new Type[]
    {
        typeof(Rect), typeof(Texture), typeof(float), typeof(Vector2), typeof(Rect), typeof(float), typeof(Material), typeof(float)
    })]
    public static class Widgets_DrawTextureFitted_Patch
    {
        public static bool Prefix(Rect outerRect, Texture tex, float scale, Vector2 texProportions, Rect texCoords, float angle, Material mat, float alpha)
        {
            if (angle == 0f || Mathf.Abs(Mathf.DeltaAngle(angle, 180f)) > 0.01f)
            {
                return true;
            }
            if (Event.current.type != EventType.Repaint)
            {
                return false;
            }

            var rect = new Rect(0f, 0f, texProportions.x, texProportions.y);
            var fit = (rect.width / rect.height < outerRect.width / outerRect.height)
                ? outerRect.height / rect.height
                : outerRect.width / rect.width;
            fit *= scale;
            rect.width *= fit;
            rect.height *= fit;
            rect.x = outerRect.x + outerRect.width / 2f - rect.width / 2f;
            rect.y = outerRect.y + outerRect.height / 2f - rect.height / 2f;

            var halfTurnCoords = new Rect(texCoords.x + texCoords.width, texCoords.y + texCoords.height, -texCoords.width, -texCoords.height);
            var oldColor = GUI.color;
            if (!Mathf.Approximately(alpha, 1f))
            {
                var faded = GUI.color;
                faded.a *= alpha;
                GUI.color = faded;
            }
            GenUI.DrawTextureWithMaterial(rect, tex, mat, halfTurnCoords);
            GUI.color = oldColor;
            return false;
        }
    }
}
