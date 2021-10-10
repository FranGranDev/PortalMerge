using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireGun))]
    public class RayfireGunEditor : Editor
    {
        // Draw gizmo
        [DrawGizmo (GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmosSelected (RayfireGun gun, GizmoType gizmoType)
        {
            // Ray
            if (gun.showRay == true)
            {
                Gizmos.DrawRay (gun.transform.position, gun.ShootVector * gun.maxDistance);
            }

            // Hit
            if (gun.showHit == true)
            {
                RaycastHit hit;
                bool       hitState = Physics.Raycast (gun.transform.position, gun.ShootVector, out hit, gun.maxDistance, gun.mask);
                if (hitState == true)
                {

                    // TODO COLOR BY IMPACT STR

                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere (hit.point, gun.radius);
                }
            }
        }

        private void OnSceneGUI()
        {
            // var gun = target as RayfireGun;
            // Show ray
            //// Draw handles
            //EditorGUI.BeginChangeCheck();
            //bomb.range = Handles.RadiusHandle(transform.rotation, transform.position, bomb.range);
            //if (EditorGUI.EndChangeCheck() == true)
            //{
            //    Undo.RecordObject(bomb, "Change Range");
            //}
        }

        // Inspector editing
        public override void OnInspectorGUI()
        {
            // Get target
            RayfireGun gun = target as RayfireGun;
            if (gun == null)
                return;

            // Space
            GUILayout.Space (8);

            // Begin
            GUILayout.BeginHorizontal();

            // Start Shooting
            if (GUILayout.Toggle (gun.shooting, "Start Shooting", "Button", GUILayout.Height (25)) == true)
            {
                gun.StartShooting();
            }
            else
            {
                gun.StopShooting();
            }

            // End
            EditorGUILayout.EndHorizontal();

            // Space
            GUILayout.Space (1);

            // Begin
            GUILayout.BeginHorizontal();

            // Shoot
            if (GUILayout.Button ("Single Shot", GUILayout.Height (22)))
            {
                foreach (var targ in targets)
                    (targ as RayfireGun).Shoot();
            }

            // Shoot
            if (GUILayout.Button ("    Burst   ", GUILayout.Height (22)))
            {
                foreach (var targ in targets)
                    (targ as RayfireGun).Burst();
            }
            
            EditorGUILayout.EndHorizontal();

            
            GUILayout.Space (1);

            GUILayout.BeginHorizontal();

            // Show ray and hit
            EditorGUI.BeginChangeCheck();
            gun.showRay = GUILayout.Toggle (gun.showRay, "Show Ray", "Button");
            gun.showHit = GUILayout.Toggle (gun.showHit, "Show Hit", "Button");
            if (EditorGUI.EndChangeCheck())
                    SceneView.RepaintAll();
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space (3);

            // Draw script UI
            DrawDefaultInspector();
            
            GUILayout.Space (3);
            
            GUILayout.Label ("  Filters", EditorStyles.boldLabel);

            // Tag filter
            gun.tagFilter = EditorGUILayout.TagField ("Tag", gun.tagFilter);

            // Layer mask
            List<string> layerNames = new List<string>();
            for (int i = 0; i <= 31; i++)
                layerNames.Add (i + ". " + LayerMask.LayerToName (i));
            gun.mask = EditorGUILayout.MaskField ("Layer", gun.mask, layerNames.ToArray());
        }
    }
}