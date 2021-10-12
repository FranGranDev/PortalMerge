using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireUnyielding))]
    public class RayfireUnyieldingEditor : Editor
    {
        private RayfireUnyielding uny;
        private Vector3 centerWorldPos;
        private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();
        static Color wireColor = new Color (0.58f, 0.77f, 1f);
        
        // Draw gizmo
        [DrawGizmo (GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmosSelected (RayfireUnyielding targ, GizmoType gizmoType)
        {
            if (targ.enabled && targ.showGizmo == true)
            {
                Gizmos.color  = wireColor;
                Gizmos.matrix = targ.transform.localToWorldMatrix;
                Gizmos.DrawWireCube (targ.centerPosition, targ.size);
            }
        }

        // Show center move handle
        private void OnSceneGUI()
        {
            // Get shatter
            uny = target as RayfireUnyielding;
            if (uny == null)
                return;

            if (uny.enabled && uny.showGizmo == true)
            {
                Transform transform      = uny.transform;
                centerWorldPos = transform.TransformPoint (uny.centerPosition);

                // Point3 handle
                if (uny.showCenter == true)
                {
                    EditorGUI.BeginChangeCheck();
                    centerWorldPos = Handles.PositionHandle (centerWorldPos, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck() == true)
                        Undo.RecordObject (uny, "Center Move");
                    uny.centerPosition = transform.InverseTransformPoint (centerWorldPos);
                }

                Handles.matrix = uny.transform.localToWorldMatrix;
                m_BoundsHandle.wireframeColor = wireColor;
                m_BoundsHandle.center         = uny.centerPosition;
                m_BoundsHandle.size           = uny.size;

                // draw the handle
                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject (uny, "Change Bounds");
                    uny.size = m_BoundsHandle.size;
                }
            }
        }
        
        // Inspector
        public override void OnInspectorGUI()
        {
            // Get shatter
            uny = target as RayfireUnyielding;
            if (uny == null)
                return;
            
            GUILayout.Space (5);
             
            // Label
            // GUILayout.Label ("  Preview", EditorStyles.boldLabel);

            // Show center toggle
            EditorGUI.BeginChangeCheck();
            uny.showGizmo = GUILayout.Toggle (uny.showGizmo, " Show Gizmo ", "Button", GUILayout.Height (22));
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
            
            GUILayout.Space (2);
            
            // Preview section Begin
            GUILayout.BeginHorizontal();

            // Show center toggle
            EditorGUI.BeginChangeCheck();
            uny.showCenter = GUILayout.Toggle (uny.showCenter, "Show Center", "Button", GUILayout.Height (22));
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
            
            // Reset center
            if (GUILayout.Button ("   Reset   ", GUILayout.Height (22)))
            {
                foreach (var targ in targets)
                    if (targ as RayfireUnyielding != null)
                        (targ as RayfireUnyielding).centerPosition = Vector3.zero;
                SceneView.RepaintAll();
            }

            // Preview section End
            EditorGUILayout.EndHorizontal();
            
            // Space
            GUILayout.Space (5);
            
            // Draw script UI
            DrawDefaultInspector();
            
        }
    }
}