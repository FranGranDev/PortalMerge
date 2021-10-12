using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireWind))]
    public class RayfireWindEditor : Editor
    {
        // Draw gizmo
        [DrawGizmo (GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmosSelected (RayfireWind wind, GizmoType gizmoType)
        {
            // Vars
            int     stepX;
            int     stepZ;
            float   windStr;
            float   x,  y,  z;
            Vector3 p1, p2, p3, p4, p5, p6, p7, p8, p10, p11, to;
            Vector3 h1;
            Vector3 vector;
            Vector3 localPos;
            float   perlinVal;
            Color   color = Color.red;
            color.b = 0.0f;
            Color wireColor = new Color (0.58f, 0.77f, 1f);
            Color sphCol    = new Color (1.0f,  0.60f, 0f);

            // Gizmo preview
            if (wind.showGizmo == true)
            {
                // Offsets
                x = wind.gizmoSize.x / 2f;
                y = wind.gizmoSize.y;
                z = wind.gizmoSize.z / 2f;

                // Get points
                p1 = new Vector3 (-x, 0, -z);
                p2 = new Vector3 (-x, 0, +z);
                p3 = new Vector3 (+x, 0, -z);
                p4 = new Vector3 (+x, 0, +z);
                p5 = new Vector3 (-x, y, -z);
                p6 = new Vector3 (-x, y, +z);
                p7 = new Vector3 (+x, y, -z);
                p8 = new Vector3 (+x, y, +z);

                p10 = new Vector3 (-x, 0, 0);
                p11 = new Vector3 (+x, 0, 0);
                to  = new Vector3 (+0, 0, z);

                // Gizmo properties
                Gizmos.color  = wireColor;
                Gizmos.matrix = wind.transform.localToWorldMatrix;

                // Gizmo Lines
                Gizmos.DrawLine (p1, p2);
                Gizmos.DrawLine (p3, p4);
                Gizmos.DrawLine (p5, p6);
                Gizmos.DrawLine (p7, p8);
                Gizmos.DrawLine (p1, p5);
                Gizmos.DrawLine (p2, p6);
                Gizmos.DrawLine (p3, p7);
                Gizmos.DrawLine (p4, p8);
                Gizmos.DrawLine (p1, p3);
                Gizmos.DrawLine (p2, p4);
                Gizmos.DrawLine (p5, p7);
                Gizmos.DrawLine (p6, p8);

                // Arrow
                Gizmos.DrawLine (p1,  Vector3.zero);
                Gizmos.DrawLine (p3,  Vector3.zero);
                Gizmos.DrawLine (p10, to);
                Gizmos.DrawLine (p11, to);

                // Selectable sphere
                float sphereSize = (x + y + z) * 0.02f;
                if (sphereSize < 0.1f)
                    sphereSize = 0.1f;
                float ySph = y / 2f;
                Gizmos.color = sphCol;
                Gizmos.DrawSphere (new Vector3 (x,  ySph, 0f), sphereSize);
                Gizmos.DrawSphere (new Vector3 (-x, ySph, 0f), sphereSize);
                Gizmos.DrawSphere (new Vector3 (0f, ySph, z),  sphereSize);
                Gizmos.DrawSphere (new Vector3 (0f, ySph, -z), sphereSize);

                // Force preview
                if (wind.showNoise == true)
                {
                    // Preview rate
                    stepX = (int)(wind.gizmoSize.x / wind.previewDensity);
                    stepZ = (int)(wind.gizmoSize.z / wind.previewDensity);

                    // Create preview helpers
                    for (int xx = -(stepX / 2); xx < stepX / 2 + 1; xx++)
                    {
                        for (int zz = -(stepZ / 2); zz < stepZ / 2 + 1; zz++)
                        {
                            // Local position
                            localPos   = Vector3.zero;
                            localPos.x = xx * wind.previewDensity;
                            localPos.z = zz * wind.previewDensity;
                            localPos.y = 0.2f;

                            // Get perlin value for local position
                            perlinVal = wind.PerlinFixedLocal (localPos);

                            // Get final strength for local position by min and max str
                            windStr = wind.WindStrength (perlinVal);

                            // Get vector for current point
                            vector = wind.GetVectorLocal (localPos) * wind.previewSize;

                            // Set color
                            if (windStr >= 0)
                            {
                                color.r = perlinVal;
                                color.g = 1f - perlinVal;
                                color.b = 0f;
                            }
                            else
                            {
                                color.r = 0f;
                                color.g = perlinVal;
                                color.b = 1f - perlinVal;
                            }

                            Gizmos.color = color;

                            // Sphere preview
                            Gizmos.DrawWireSphere (localPos, windStr * 0.1f * wind.previewSize);

                            // Get vector end point
                            h1 = localPos + vector * windStr;

                            // Draw line
                            Gizmos.DrawLine (localPos, h1);
                        }
                    }
                }
            }
        }

        // Inspector editing
        public override void OnInspectorGUI()
        {
            // Get target
            RayfireWind wind = target as RayfireWind;

            // Space
            GUILayout.Space (8);

            // Fragmentation section Begin
            GUILayout.BeginHorizontal();

            // Show gizmo
            wind.showGizmo = GUILayout.Toggle (wind.showGizmo, "Show Gizmo ", "Button");

            // Show noise 
            wind.showNoise = GUILayout.Toggle (wind.showNoise, "Show Noise ", "Button");

            // Fragmentation section End
            EditorGUILayout.EndHorizontal();

            // Space
            GUILayout.Space (3);

            // Draw script UI
            DrawDefaultInspector();

            // Space
            GUILayout.Space (3);

            // Label
            GUILayout.Label ("Filters", EditorStyles.boldLabel);

            // Tag filter
            wind.tagFilter = EditorGUILayout.TagField ("Tag", wind.tagFilter);

            // Layer mask
            List<string> layerNames = new List<string>();
            for (int i = 0; i <= 31; i++)
                layerNames.Add (i + ". " + LayerMask.LayerToName (i));
            wind.mask = EditorGUILayout.MaskField ("Layer", wind.mask, layerNames.ToArray());
        }
    }
}