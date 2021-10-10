using UnityEngine;
using UnityEditor;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireSnapshot))]
    public class RayfireSnapshotEditor : Editor
    {
        // Target
        RayfireSnapshot snap = null;

        public override void OnInspectorGUI()
        {
            // Get target
            snap = target as RayfireSnapshot;

            GUILayout.Space (8);

            // Save
            if (snap.transform.childCount > 0)
                if (GUILayout.Button ("Snapshot", GUILayout.Height (25)))
                    snap.Snapshot();

            // Load
            if (snap.snapshotAsset != null)
                if (GUILayout.Button ("Load", GUILayout.Height (25)))
                    snap.Load();

            // Draw script UI
            DrawDefaultInspector();
            ;
        }
    }
}