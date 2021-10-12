using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireActivator))]
    public class RayfireActivatorEditor : Editor
    {
        private RayfireActivator activator;
        private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();
        static Color wireColor = new Color (0.58f, 0.77f, 1f);

        // Draw gizmo
        [DrawGizmo (GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmosSelected (RayfireActivator targ, GizmoType gizmoType)
        {
            // Gizmo preview
            if (targ.enabled && targ.showGizmo == true)
            {
                // Gizmo properties
                Gizmos.color  = wireColor;
                Gizmos.matrix = targ.transform.localToWorldMatrix;

                // Box gizmo
                if (targ.gizmoType == RayfireActivator.GizmoType.Box)
                {
                    Gizmos.DrawWireCube (Vector3.zero, targ.boxSize);
                }

                // Sphere gizmo
                if (targ.gizmoType == RayfireActivator.GizmoType.Sphere)
                {
                    // Vars
                    int   size   = 45;
                    float rate   = 0f;
                    float scale  = 1f / size;
                    float radius = targ.sphereRadius;

                    Vector3 previousPoint = Vector3.zero;
                    Vector3 nextPoint     = Vector3.zero;

                    // Draw top eye
                    rate            = 0f;
                    nextPoint.y     = 0f;
                    previousPoint.y = 0f;
                    previousPoint.x = radius * Mathf.Cos (rate);
                    previousPoint.z = radius * Mathf.Sin (rate);
                    for (int i = 0; i < size; i++)
                    {
                        rate        += 2.0f * Mathf.PI * scale;
                        nextPoint.x =  radius * Mathf.Cos (rate);
                        nextPoint.z =  radius * Mathf.Sin (rate);
                        Gizmos.DrawLine (previousPoint, nextPoint);
                        previousPoint = nextPoint;
                    }

                    // Draw top eye
                    rate            = 0f;
                    nextPoint.x     = 0f;
                    previousPoint.x = 0f;
                    previousPoint.y = radius * Mathf.Cos (rate);
                    previousPoint.z = radius * Mathf.Sin (rate);
                    for (int i = 0; i < size; i++)
                    {
                        rate        += 2.0f * Mathf.PI * scale;
                        nextPoint.y =  radius * Mathf.Cos (rate);
                        nextPoint.z =  radius * Mathf.Sin (rate);
                        Gizmos.DrawLine (previousPoint, nextPoint);
                        previousPoint = nextPoint;
                    }

                    // Draw top eye
                    rate            = 0f;
                    nextPoint.z     = 0f;
                    previousPoint.z = 0f;
                    previousPoint.y = radius * Mathf.Cos (rate);
                    previousPoint.x = radius * Mathf.Sin (rate);
                    for (int i = 0; i < size; i++)
                    {
                        rate        += 2.0f * Mathf.PI * scale;
                        nextPoint.y =  radius * Mathf.Cos (rate);
                        nextPoint.x =  radius * Mathf.Sin (rate);
                        Gizmos.DrawLine (previousPoint, nextPoint);
                        previousPoint = nextPoint;
                    }

                    // Selectable sphere
                    float sphereSize = radius * 0.07f;
                    if (sphereSize < 0.1f)
                        sphereSize = 0.1f;
                    Gizmos.color = new Color (1.0f, 0.60f, 0f);
                    Gizmos.DrawSphere (new Vector3 (0f,      radius,  0f),      sphereSize);
                    Gizmos.DrawSphere (new Vector3 (0f,      -radius, 0f),      sphereSize);
                    Gizmos.DrawSphere (new Vector3 (radius,  0f,      0f),      sphereSize);
                    Gizmos.DrawSphere (new Vector3 (-radius, 0f,      0f),      sphereSize);
                    Gizmos.DrawSphere (new Vector3 (0f,      0f,      radius),  sphereSize);
                    Gizmos.DrawSphere (new Vector3 (0f,      0f,      -radius), sphereSize);
                }
            }
        }
        
        // Sphere gizmo radius
        private void OnSceneGUI()
        {
            activator = target as RayfireActivator;
            if (activator == null)
                return;

            if (activator.enabled == true && activator.showGizmo == true)
            {
                if (activator.gizmoType == RayfireActivator.GizmoType.Sphere)
                {
                    var transform = activator.transform;

                    // Draw handles
                    EditorGUI.BeginChangeCheck();
                    activator.sphereRadius = Handles.RadiusHandle (transform.rotation, transform.position, activator.sphereRadius, true);
                    if (EditorGUI.EndChangeCheck() == true)
                    {
                        // TODO change sphere collider size
                        
                        Undo.RecordObject (activator, "Change Radius");
                    }
                }

                if (activator.gizmoType == RayfireActivator.GizmoType.Box)
                {
                    Handles.matrix = activator.transform.localToWorldMatrix;
                    m_BoundsHandle.wireframeColor = wireColor;
                    m_BoundsHandle.center = Vector3.zero;
                    m_BoundsHandle.size   = activator.boxSize;

                    // draw the handle
                    EditorGUI.BeginChangeCheck();
                    m_BoundsHandle.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject (activator, "Change Bounds");
                        activator.boxSize = m_BoundsHandle.size;
                    }
                }
            }
        }

        // Inspector
        public override void OnInspectorGUI()
        {
            // Get target
            activator = target as RayfireActivator;
            if (activator == null)
                return;
            
            // Space
            GUILayout.Space (8);
            
            // Show center toggle
            EditorGUI.BeginChangeCheck();
            activator.showGizmo = GUILayout.Toggle (activator.showGizmo, " Show Gizmo ", "Button", GUILayout.Height (22));
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
            
            // Buttons
            if (Application.isPlaying == true)
            {
                // Begin
                GUILayout.BeginHorizontal();

                // Cache buttons
                if (GUILayout.Button ("   Start   ", GUILayout.Height (25)))
                    foreach (var targ in targets)
                        if (targ as RayfireActivator != null)
                            (targ as RayfireActivator).TriggerAnimation();
                if (GUILayout.Button ("    Stop    ", GUILayout.Height (25)))
                    foreach (var targ in targets)
                        if (targ as RayfireActivator != null)
                            (targ as RayfireActivator).StopAnimation();
                if (GUILayout.Button ("Reset", GUILayout.Height (25)))
                    foreach (var targ in targets)
                        if (targ as RayfireActivator != null)
                            (targ as RayfireActivator).ResetAnimation();

                // End
                EditorGUILayout.EndHorizontal();
            }

            // Space
            GUILayout.Space (1);

            // Begin
            GUILayout.BeginHorizontal();

            // Cache buttons
            if (GUILayout.Button ("Add Position", GUILayout.Height (22)))
                activator.AddPosition (activator.transform.position);
            if (GUILayout.Button ("Remove Last", GUILayout.Height (22)))
                if (activator.positionList.Count > 0)
                    activator.positionList.RemoveAt (activator.positionList.Count - 1);
            if (GUILayout.Button ("Clear All", GUILayout.Height (22)))
                activator.positionList.Clear();

            // End
            EditorGUILayout.EndHorizontal();

            // Space
            GUILayout.Space (3);

            // Positions info
            if (activator.positionList != null && activator.positionList.Count > 0)
            {
                GUILayout.Label ("Positions : " + activator.positionList.Count);

                // Space
                GUILayout.Space (2);
            }
            
            // Space
            GUILayout.Space (8);

            // Draw script UI
            DrawDefaultInspector();
        }
    }
}