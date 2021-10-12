using UnityEngine;
using UnityEditor;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireCombine))]
    public class RayfireCombineEditor : Editor
    {
        RayfireCombine combine = null;
        
        public override void OnInspectorGUI()
        {
            // Get shatter
            combine = target as RayfireCombine;
            if (combine == null)
                return;

            // Space
            GUILayout.Space (8);

            // Combine 
            if (GUILayout.Button ("Combine", GUILayout.Height (25)))
                combine.Combine();

            // Space
            GUILayout.Space (3);
            
            // Draw script UI
            DrawDefaultInspector();
            
            // Space
            GUILayout.Space (3);
            
            GUILayout.Label ("  Export", EditorStyles.boldLabel);
            
            
            // Export 
            if (GUILayout.Button ("Export Mesh", GUILayout.Height (25)))
            {
                MeshFilter mf = combine.GetComponent<MeshFilter>();
                RFMeshAsset.SaveMesh (mf, combine.name);
            }
            
            // Space
            GUILayout.Space (5);
        }
    }
}