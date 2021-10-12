using UnityEngine;
using UnityEditor;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireSound))]
    public class RayfireSoundEditor : Editor
    {
        // Target
        RayfireSound sound;
 
       
        public override void OnInspectorGUI()
        {
            // Get target
            sound = target as RayfireSound;
            if (sound == null)
                return;

            // Initialize
            if (Application.isPlaying == true)
            {
                GUILayout.Space (8);
                
                if (GUILayout.Button ("Initialization Sound", GUILayout.Height (25)))
                    foreach (var targ in targets)
                        if (targ as RayfireSound != null)
                            RFSound.InitializationSound(targ as RayfireSound, 0f);
                if (GUILayout.Button ("Activation Sound", GUILayout.Height (25)))
                    foreach (var targ in targets)
                        if (targ as RayfireSound != null)
                            RFSound.ActivationSound(targ as RayfireSound, 0f);
                if (GUILayout.Button ("Demolition Sound", GUILayout.Height (25)))
                    foreach (var targ in targets)
                        if (targ as RayfireSound != null)
                            RFSound.DemolitionSound(targ as RayfireSound, 0f);
                
                // Info
                GUILayout.Label ("Info", EditorStyles.boldLabel);
                
                // Get volume
                GUILayout.Label ("  Volume: " + RFSound.GeVolume(sound, 0f));
                
                GUILayout.Space (5);
            }
            
            // Draw script UI
            DrawDefaultInspector();
        }
    }
}