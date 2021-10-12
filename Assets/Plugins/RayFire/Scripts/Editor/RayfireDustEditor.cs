using UnityEngine;
using UnityEditor;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireDust))]
    public class RayfireDustEditor : Editor
    {
        // Target
        RayfireDust dust = null;

        public override void OnInspectorGUI()
        {
            // Get target
            dust = target as RayfireDust;
            if (dust == null)
                return;
            
            // Space
            GUILayout.Space (8);
            
            if (Application.isPlaying == true)
            {
                if (GUILayout.Button ("Emit", GUILayout.Height (25)))
                        foreach (var targ in targets)
                            if (targ as RayfireDust != null)
                                (targ as RayfireDust).Emit();
            }

            // Draw script UI
            DrawDefaultInspector();
        }
    }
}