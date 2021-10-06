using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(LevelManagement))]
public class LevelManagementEditor : Editor
{
    private SerializedProperty editorMode;
    private LevelManagement _levelManagement;
    private int _prevIndex;
    private bool _EditorChooseLevel;
    private ReorderableList listLvl;

    private void Awake()
    {
        _levelManagement = target as LevelManagement;
    }

    private void OnEnable()
    {
        editorMode = serializedObject.FindProperty("editorMode");
        listLvl = new ReorderableList(serializedObject, serializedObject.FindProperty("Levels"), true, true, true, true);
        listLvl.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            var element = listLvl.serializedProperty.GetArrayElementAtIndex(index);

            EditorGUI.IntField(new Rect(rect.x, rect.y, 30, EditorGUIUtility.singleLineHeight), index + 1);

           if( GUI.Button(new Rect(rect.x + 36, rect.y, 50, EditorGUIUtility.singleLineHeight), new GUIContent("Select")))
            {
                _levelManagement.SelectLevel(index);
            }
            
            EditorGUI.PropertyField(
                new Rect(rect.x + 90, rect.y, 200, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("LevelPrefab"), GUIContent.none);


            if (GUI.Button(new Rect(rect.x + 300, rect.y, 50, EditorGUIUtility.singleLineHeight), new GUIContent("Clear")))
            {
                _levelManagement.clearListAtIndex(index);
            }

            if (GUI.Button(new Rect(rect.x + 347, rect.y, 50, EditorGUIUtility.singleLineHeight), new GUIContent("Delete")))
            {
                //element.DeleteCommand();
                _levelManagement.Levels.RemoveAt(index);
            }

        };

        listLvl.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Levels");
        };
    }

    public override void OnInspectorGUI()
    {
        editorMode.boolValue = GUILayout.Toggle(editorMode.boolValue, new GUIContent("Editor Mode"), GUILayout.Width(100), GUILayout.Height(20));
        _levelManagement.editorMode = editorMode.boolValue;
        serializedObject.ApplyModifiedProperties();
        if (editorMode.boolValue) DrawSelectedLevel();

        serializedObject.Update();
        listLvl.DoLayoutList();
        serializedObject.ApplyModifiedProperties();

        if (GUILayout.Button("Clear Player Prefs", GUILayout.Width(200), GUILayout.Height(20)))
            PlayerPrefs.DeleteAll();
    }

    private void DrawSelectedLevel()
    {

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            var index = EditorGUILayout.IntField(
                "Current Level",
                _levelManagement.CurrentLevelIndex + 1);
            if (EditorGUI.EndChangeCheck())
            {
                _levelManagement.SelectLevel(index - 1);
            }


            if (GUILayout.Button("<<", GUILayout.Width(30), GUILayout.Height(20)))
            {
                _levelManagement.PrevLevel();
            }

            if (GUILayout.Button(">>", GUILayout.Width(30), GUILayout.Height(20)))
            {
                _levelManagement.NextLevel();
            }


            EditorGUILayout.EndHorizontal();
    }

    /* private void DrawLevels()
     {
         DrawSelectedLevel();
         DrawLevelsList();
     }

     private void DrawSelectedLevel()
     {

         EditorGUILayout.BeginHorizontal();

         _EditorChooseLevel = EditorGUILayout.Toggle("Choose level from editor", _EditorChooseLevel);

         EditorGUILayout.EndHorizontal();

         if (_EditorChooseLevel)
         {

             EditorGUILayout.BeginHorizontal();
             EditorGUI.BeginChangeCheck();
             var index = EditorGUILayout.IntField(
                 "Current Index",
                 _levelManagement.CurrentLevelIndex + 1);
             if (EditorGUI.EndChangeCheck())
             {
                 _levelManagement.SelectLevel(index - 1);
             }


                 if (GUILayout.Button("<<", GUILayout.Width(30), GUILayout.Height(20)))
             {
                 _levelManagement.PrevLevel();
             }

             if (GUILayout.Button(">>", GUILayout.Width(30), GUILayout.Height(20)))
             {
                 _levelManagement.NextLevel();
             }

             if (index != _prevIndex)
             {

                 *//* _levelManagement.SelectLevel(index);*//*
                 _prevIndex = index;
             }

             EditorGUILayout.EndHorizontal();
         }



 }

     private void DrawLevelsList()
     {
         EditorGUILayout.BeginHorizontal("box");

         GUILayout.Label("Levels");

         if (GUILayout.Button("Add", GUILayout.Width(40), GUILayout.Height(25)))
         {
             _levelManagement.Levels.Add(new Level());
         }

         EditorGUILayout.EndHorizontal();


         if (_levelManagement.Levels.Count > 0)
         {


             for (int i = 0; i < _levelManagement.Levels.Count; i++)
             {
                 Level level = _levelManagement.Levels[i];

                 EditorGUILayout.BeginVertical("box");

                 level.LevelPrefab = (GameObject)EditorGUILayout.ObjectField(
                     new GUIContent("Level Prefab"),
                     _levelManagement.Levels[i].LevelPrefab,
                     typeof(GameObject));

                 level.SkyboxMaterial = (Material)EditorGUILayout.ObjectField(
                     new GUIContent("Skybox"),
                     _levelManagement.Levels[i].SkyboxMaterial,
                     typeof(Material));

                 EditorGUILayout.BeginHorizontal();

                 if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(25)))
                 {
                     _levelManagement.SelectLevel(i);
                 }

                 GUILayout.FlexibleSpace();

                 if (GUILayout.Button("Remove", GUILayout.Width(55), GUILayout.Height(25)))
                 {
                     _levelManagement.Levels.Remove(level);
                 }

                 if (GUILayout.Button("Clear", GUILayout.Width(50), GUILayout.Height(25)))
                 {
                     _levelManagement.Levels.RemoveAt(i);
                     _levelManagement.Levels.Insert(i, new Level());
                 }

                 EditorGUILayout.EndHorizontal();
                 EditorGUILayout.EndVertical();
             }
         }
     }

     private void SetDirty(GameObject gameObject)
     {
         EditorUtility.SetDirty(gameObject);
         EditorSceneManager.MarkSceneDirty(gameObject.scene);
     }*/
}
