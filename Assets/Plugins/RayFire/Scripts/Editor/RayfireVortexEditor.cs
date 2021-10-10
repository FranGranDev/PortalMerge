using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireVortex))]
    public class RayfireVortexEditor : Editor
    {

        // Draw gizmo
        [DrawGizmo (GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmosSelected (RayfireVortex vortex, GizmoType gizmoType)
        {
            if (vortex.showGizmo)
            {
                // Vars
                Vector3 previousPoint = Vector3.zero;
                Vector3 nextPoint     = Vector3.zero;
                Color   wireColor     = new Color (0.58f, 0.77f, 1f);

                // Gizmo properties
                Gizmos.color  = wireColor;
                Gizmos.matrix = vortex.transform.localToWorldMatrix;

                // Gizmo center line
                Gizmos.DrawLine (vortex.topAnchor, vortex.bottomAnchor);

                // Draw main circles
                DrawCircle (vortex.topAnchor,    vortex.topRadius,    previousPoint, nextPoint);
                DrawCircle (vortex.bottomAnchor, vortex.bottomRadius, previousPoint, nextPoint);

                // Draw main eyes circles
                DrawCircle (vortex.topAnchor,    vortex.topRadius * vortex.eye,    previousPoint, nextPoint);
                DrawCircle (vortex.bottomAnchor, vortex.bottomRadius * vortex.eye, previousPoint, nextPoint);

                // Draw additional circles
                //if (vortex.circles > 2)
                //{
                //    float step = 1f / (vortex.circles - 1);
                //    for (int i = 1; i < vortex.circles - 1; i++)
                //    {
                //        Vector3 midPoint = Vector3.Lerp(vortex.bottomAnchor, vortex.topAnchor, step *i);
                //        float rad = Mathf.Lerp(vortex.bottomRadius, vortex.topRadius, step * i);
                //        DrawCircle(midPoint, rad);
                //        DrawCircle(midPoint, (vortex.topRadius + vortex.bottomRadius) / 2f * vortex.eye);
                //    }
                //}

                // Selectable sphere
                float sphereSize = (vortex.topRadius + vortex.bottomRadius) * 0.03f;
                if (sphereSize < 0.1f)
                    sphereSize = 0.1f;
                Gizmos.color = new Color (1.0f, 0.60f, 0f);
                Gizmos.DrawSphere (new Vector3 (vortex.bottomRadius,  0f, 0f),                   sphereSize);
                Gizmos.DrawSphere (new Vector3 (-vortex.bottomRadius, 0f, 0f),                   sphereSize);
                Gizmos.DrawSphere (new Vector3 (0f,                   0f, vortex.bottomRadius),  sphereSize);
                Gizmos.DrawSphere (new Vector3 (0f,                   0f, -vortex.bottomRadius), sphereSize);

                Gizmos.DrawSphere (new Vector3 (vortex.topRadius,  0f, 0f) + vortex.topAnchor,                sphereSize);
                Gizmos.DrawSphere (new Vector3 (-vortex.topRadius, 0f, 0f) + vortex.topAnchor,                sphereSize);
                Gizmos.DrawSphere (new Vector3 (0f,                0f, vortex.topRadius) + vortex.topAnchor,  sphereSize);
                Gizmos.DrawSphere (new Vector3 (0f,                0f, -vortex.topRadius) + vortex.topAnchor, sphereSize);

                //// Draw circle gizmo
                //void DrawHelix()
                //{
                //    float detalization = 200f;
                //    // Starting position from bottom to top on vortex axis
                //    Vector3 bottomStartPos = vortex.bottomAnchor;
                //    Vector3 vectorToTop = vortex.topAnchor - vortex.bottomAnchor;
                //    Vector3 vectorToTopStep = vectorToTop / detalization;
                //    float swirlNow = 0f;
                //    float swirlRate = 0.1f;
                //    float heightRateNow = 0f;
                //    previousPoint = bottomStartPos;
                //    nextPoint = Vector3.zero;
                //    float heightRateStep = 1f / detalization;
                //    while (heightRateNow < 1f)
                //    {
                //        // Next swirl rate
                //        swirlNow += swirlRate;

                //        // Increase current rate for lerp
                //        heightRateNow += heightRateStep;

                //        // Get average radius by height
                //        float radius = Mathf.Lerp(vortex.bottomRadius, vortex.topRadius, heightRateNow);

                //        // Get next point on vortex axis
                //        bottomStartPos += vectorToTopStep;

                //        // Get local helix point
                //        Vector3 point = Vector3.zero;
                //        point.x = Mathf.Cos(swirlNow) * radius;
                //        point.z = Mathf.Sin(swirlNow) * radius;

                //        // Get final vortex point
                //        point += bottomStartPos;

                //        // Gizmos.DrawWireSphere(point, 0.1f);
                //        Gizmos.DrawLine(point, previousPoint);
                //        // Gizmos.DrawWireSphere(point, 0.1f);
                //        previousPoint = point;
                //    }
                //}
            }
        }


        // Draw circle gizmo
        static void DrawCircle (Vector3 point, float radius, Vector3 previousPoint, Vector3 nextPoint)
        {
            // Draw top eye
            const int size  = 45;
            float     rate  = 0f;
            float     scale = 1f / size;
            nextPoint.y     = point.y;
            previousPoint.y = point.y;
            previousPoint.x = radius * Mathf.Cos (rate) + point.x;
            previousPoint.z = radius * Mathf.Sin (rate) + point.z;
            for (int i = 0; i < size; i++)
            {
                rate        += 2.0f * Mathf.PI * scale;
                nextPoint.x =  radius * Mathf.Cos (rate) + point.x;
                nextPoint.z =  radius * Mathf.Sin (rate) + point.z;

                Gizmos.DrawLine (previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }

        [DrawGizmo (GizmoType.Selected | GizmoType.NonSelected)]
        void OnSceneGUI()
        {
            var vortex = target as RayfireVortex;
            if (vortex.showGizmo == true)
            {
                Transform transForm = vortex.transform;

                // Start check for changes and record undo
                EditorGUI.BeginChangeCheck();

                // Top Bottom circles
                Handles.DrawWireDisc (transForm.TransformPoint (vortex.topAnchor),    transForm.up, vortex.topRadius);
                Handles.DrawWireDisc (transForm.TransformPoint (vortex.bottomAnchor), transForm.up, vortex.bottomRadius);

                // Top Bottom radius handles
                vortex.topRadius    = Handles.RadiusHandle (transForm.rotation, transForm.TransformPoint (vortex.topAnchor),    vortex.topRadius,    true);
                vortex.bottomRadius = Handles.RadiusHandle (transForm.rotation, transForm.TransformPoint (vortex.bottomAnchor), vortex.bottomRadius, true);
                if (EditorGUI.EndChangeCheck() == true)
                {
                    Undo.RecordObject (vortex, "Change Gizmo");
                }

                // Top point handle
                if (vortex.topHandle == true)
                {
                    vortex.topAnchor = transForm.InverseTransformPoint (Handles.PositionHandle (transForm.TransformPoint (vortex.topAnchor), transForm.rotation));
                    if (vortex.topAnchor.x > 20)
                        vortex.topAnchor.x = 20;
                    else if (vortex.topAnchor.z > 20)
                        vortex.topAnchor.z = 20;
                    if (vortex.topAnchor.x < -20)
                        vortex.topAnchor.x = -20;
                    else if (vortex.topAnchor.z < -20)
                        vortex.topAnchor.z = -20;
                }
            }
        }

        // Inspector editing
        public override void OnInspectorGUI()
        {
            // Get target
            RayfireVortex vortex = target as RayfireVortex;

            // Space
            GUILayout.Space (8);

            // Fragmentation section Begin
            GUILayout.BeginHorizontal();

            // Show gizmo
            vortex.showGizmo = GUILayout.Toggle (vortex.showGizmo, "Show Gizmo", "Button");

            // Show gizmo
            vortex.topHandle = GUILayout.Toggle (vortex.topHandle, "Top Handle", "Button");

            // Fragmentation section End
            EditorGUILayout.EndHorizontal();

            // Space
            GUILayout.Space (3);

            // Draw script UI
            DrawDefaultInspector();

            // Space
            GUILayout.Space (5);

            // Label
            GUILayout.Label ("Filters", EditorStyles.boldLabel);

            // Tag filter
            vortex.tagFilter = EditorGUILayout.TagField ("Tag", vortex.tagFilter);

            // Layer mask
            List<string> layerNames = new List<string>();
            for (int i = 0; i <= 31; i++)
                layerNames.Add (i + ". " + LayerMask.LayerToName (i));
            vortex.mask = EditorGUILayout.MaskField ("Layer", vortex.mask, layerNames.ToArray());

        }
    }
}