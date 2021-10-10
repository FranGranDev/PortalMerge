using UnityEngine;
using UnityEditor;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireRecorder))]
    public class RayfireRecorderEditor : Editor
    {
        // Target
        RayfireRecorder recorder = null;
        string          rec      = "Recording: ";

        public override void OnInspectorGUI()
        {
            // Get target
            recorder = target as RayfireRecorder;

            GUILayout.Space (8);

            // Begin
            GUILayout.BeginHorizontal();

            // Record
            if (recorder.mode == RayfireRecorder.AnimatorType.Record)
            {
                if (Application.isPlaying == true)
                {
                    if (recorder.recorder == false)
                        if (GUILayout.Button ("Start record", GUILayout.Height (25)))
                            recorder.StartRecord();
                    if (recorder.recorder == true)
                        if (GUILayout.Button ("Stop record", GUILayout.Height (25)))
                            recorder.StopRecord();
                }
            }

            // Play
            if (recorder.mode == RayfireRecorder.AnimatorType.Play)
                if (Application.isPlaying == true)
                    if (GUILayout.Button ("Start Play", GUILayout.Height (25)))
                        recorder.StartPlay();

            // End
            EditorGUILayout.EndHorizontal();

            GUILayout.Space (3);

            // Recording
            if (recorder.mode == RayfireRecorder.AnimatorType.Record)
                if (Application.isPlaying == true)
                    if (recorder.recorder == true)
                        GUILayout.Label (rec + (int)recorder.recordedTime + "/" + recorder.duration);

            GUILayout.Space (3);

            // Draw script UI
            DrawDefaultInspector();
            ;
        }
    }
}