using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BetterArchitect
{
    [HarmonyPatch(typeof(GUIUtility), nameof(GUIUtility.RotateAroundPivot))]
    public static class GUIUtility_RotateAroundPivot_Patch
    {
        private static readonly Func<Vector2, Vector2> unclip;

        static GUIUtility_RotateAroundPivot_Patch()
        {
            try
            {
                var method = AccessTools.Method(AccessTools.TypeByName("UnityEngine.GUIClip"), "Unclip", new[] { typeof(Vector2) });
                if (method != null)
                {
                    unclip = AccessTools.MethodDelegate<Func<Vector2, Vector2>>(method);
                }
            }
            catch (Exception)
            {
                unclip = null;
            }
        }

        public static void Prefix(ref Vector2 pivotPoint)
        {
            if (unclip == null)
            {
                return;
            }

            var scale = Prefs.UIScale;
            if (scale == 1f)
            {
                return;
            }

            var matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            var origin = unclip(Vector2.zero);
            GUI.matrix = matrix;

            pivotPoint += origin * (scale - 1f);
        }
    }
}
