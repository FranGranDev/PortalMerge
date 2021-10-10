using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireBlade))]
    public class RayfireBladeEditor : Editor
    {

        RayfireBlade blade;
        List<string> layerNames;
        
        // Inspector editing
        public override void OnInspectorGUI()
        {
            // Get target
            blade = target as RayfireBlade;
            if (blade == null)
                return;

            // Slice Target
            if (Application.isPlaying == true)
            {
                // Precache
                if (GUILayout.Button (" Slice Target ", GUILayout.Height (25)))
                {
                    foreach (var bl in targets)
                        if (bl as RayfireBlade != null)
                            (bl as RayfireBlade).SliceTarget();
                }
            }

            // Space
            GUILayout.Space (3);

            // Draw script UI
            DrawDefaultInspector();

            // Space
            GUILayout.Space (3);

            // Label
            GUILayout.Label ("  Filters", EditorStyles.boldLabel);

            // Tag filter
            blade.tagFilter = EditorGUILayout.TagField ("Tag", blade.tagFilter);

            // Layer mask
            if (layerNames == null)
                layerNames = new List<string>();
            layerNames.Clear();
            for (int i = 0; i <= 31; i++)
                layerNames.Add (i + ". " + LayerMask.LayerToName (i));
            blade.mask = EditorGUILayout.MaskField ("Layer", blade.mask, layerNames.ToArray());
        }
    }
}