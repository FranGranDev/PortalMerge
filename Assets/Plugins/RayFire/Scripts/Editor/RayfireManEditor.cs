using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireMan))]
    public class RayfireManEditor : Editor
    {
        Texture2D logo;
        Texture2D icon;

        // Sphere gizmo radius
        private void OnSceneGUI()
        {
            // Set icon
            if (icon == null)
            {
                icon = EditorGUIUtility.IconContent ("Assets/RayFire/Info/Logo/logo_small.png").image as Texture2D;
                Type         editorGuiUtilityType = typeof(EditorGUIUtility);
                BindingFlags bindingFlags         = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;
                object[]     args                 = new object[] {(target as RayfireMan).gameObject, icon};
                editorGuiUtilityType.InvokeMember ("SetIconForObject", bindingFlags, null, null, args);
            }
        }

        // Inspector editing
        public override void OnInspectorGUI()
        {
            // Get target
            RayfireMan man = target as RayfireMan;

            // Set new static instance
            if (RayfireMan.inst == null)
                RayfireMan.inst = man;

            // Draw script UI
            DrawDefaultInspector();

            // Info
            GUILayout.Label ("  Info:", EditorStyles.boldLabel);

            // Pool
            if (Application.isPlaying == true)
            {
                if (man.fragments.poolList.Count > 0)
                    GUILayout.Label ("Pool amount: " + man.fragments.poolList.Count);
                
                if (man.advancedDemolitionProperties.currentAmount > 0)
                    GUILayout.Label ("Fragments: " + man.advancedDemolitionProperties.currentAmount + "/" + man.advancedDemolitionProperties.maximumAmount);
            }

            // Space
            GUILayout.Space (5);

            // About
            GUILayout.Label ("  About", EditorStyles.boldLabel);

            // Version
            GUILayout.Label ("Plugin build: " + RayfireMan.buildMajor + '.' + RayfireMan.buildMinor.ToString ("D2"));

            // Logo TODO remove if component removed
            if (logo == null)
                logo = (Texture2D)AssetDatabase.LoadAssetAtPath ("Assets/RayFire/Info/Logo/logo_small.png", typeof(Texture2D));
            if (logo != null)
                GUILayout.Box (logo, GUILayout.Width ((int)EditorGUIUtility.currentViewWidth - 19f), GUILayout.Height (64));

            // Begin
            GUILayout.BeginHorizontal();

            // Changelog check
            if (GUILayout.Button ("     Changelog     ", GUILayout.Height (20)))
                Application.OpenURL ("https://assetstore.unity.com/packages/tools/game-toolkits/rayfire-for-unity-148690#releases");
            
            // End
            EditorGUILayout.EndHorizontal();

            // Space
            GUILayout.Space (3);
        }
    }
}