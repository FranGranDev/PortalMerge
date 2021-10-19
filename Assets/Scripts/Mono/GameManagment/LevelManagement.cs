using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using DG.Tweening;
//using GameAnalyticsSDK;

public class LevelManagement : MonoBehaviour
{
    #region Singletone
    public static LevelManagement Default { get; private set; }
    public LevelManagement() => Default = this;
    #endregion

    const string PREFS_KEY_LEVEL_ID = "CurrentLevelCount";


    public bool editorMode;
    public int CurrentLevelIndex { get => PlayerPrefs.GetInt(PREFS_KEY_LEVEL_ID, 0); private set => PlayerPrefs.SetInt(PREFS_KEY_LEVEL_ID, value); }
    public List<Level> Levels = new List<Level>();
    private bool GameInitialised;

    public void Start()
    {
#if UNITY_EDITOR
#else
            editorMode = false;
#endif
        
        if (!editorMode)
        {
            SelectLevel(CurrentLevelIndex);
        }
    }

    private void LevelInit()
    {

    }

    private void InitGame()
    {
        if(!GameInitialised)
        {
            GameManagement.Active.Init();
            GameInitialised = true;
        }
    }
    public void StartGame()
    {
        InitGame();
        GameManagement.Active.StartGame();
    }
    public void RestartLevel()
    {
        SelectLevel(CurrentLevelIndex, false);
    }

    public void clearListAtIndex(int levelIndex)
    {
        Levels[levelIndex].LevelPrefab = null;
    }

    public void SelectLevel(int levelIndex, bool indexCheck = true)
    {
        if (indexCheck)
            levelIndex = GetCorrectedIndex(levelIndex);

        if (Levels[levelIndex].LevelPrefab == null)
        {
            Debug.Log("<color=red>There is no prefab attached!</color>");
            return;
        }

        var level = Levels[levelIndex];

        if (level.LevelPrefab != null)
        {
            SetLevelParams(level);
            
            CurrentLevelIndex = levelIndex;

            //Debug.Log("Set: " + CurrentLevelIndex);
        }
    }

    public void NextLevel() 
    {
        CurrentLevelIndex++;
        SelectLevel(CurrentLevelIndex);
    }

    public void PrevLevel()
    {
        CurrentLevelIndex--;
        SelectLevel(CurrentLevelIndex);
    }
        
    private int GetCorrectedIndex(int levelIndex)
    {
        if (editorMode)
        {
            if (levelIndex > Levels.Count - 1)
            {
                return 0;
            }
            else if (levelIndex < 0)
            {
                return Levels.Count - 1;
            }
            else
            {
                return levelIndex;
            }
        }  
        else
        {
            int levelId = PlayerPrefs.GetInt(PREFS_KEY_LEVEL_ID, 0);
            if (levelId > Levels.Count - 1) 
            {
                if (Levels.Count > 1)
                {
                    levelId = UnityEngine.Random.Range(0, Levels.Count - 1);
                    
                    //Вроде все правильно, но каждый раз юнити крашилась, не знаю почему, пока так

                    //while (levelId == CurrentLevelIndex)
                    //{
                    //    levelId = UnityEngine.Random.Range(0, Levels.Count - 1);
                    //}

                    return levelId;
                }
                else
                {
                    return UnityEngine.Random.Range(0, Levels.Count - 1);
                }
            }
            return levelId;
        }
    }
    private void SetLevelParams(Level level)
    {
        if (level.LevelPrefab)
        {
            ClearChilds();
            if (Application.isEditor)
            {
                if (Application.isPlaying)
                {
                    Instantiate(level.LevelPrefab, transform);
                }
                else
                {
                    PrefabUtility.InstantiatePrefab(level.LevelPrefab, transform);
                }
            }
            else
            {
                Instantiate(level.LevelPrefab, transform);
                InitGame();
            }

            
        }

        if (level.SkyboxMaterial)
        {
            RenderSettings.skybox = level.SkyboxMaterial;
        }
    }

    private void ClearChilds()
    {
        GameInitialised = false;

        DOTween.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject destroyObject = transform.GetChild(i).gameObject;

            DestroyImmediate(destroyObject);
        }

        Transform rayFireMan = GameObject.Find("RayFireMan")?.transform;
        if (rayFireMan != null)
        {
            foreach (Transform t in rayFireMan)
            {
                if (t.gameObject.name != "Pool_Fragments" && t.gameObject.name != "Pool_Particles")
                    Destroy(t.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        
    }
    /*
    #region Analitics Events

    public void SendStart()
    {
        string content = (PlayerPrefs.GetInt(PREFS_KEY_LEVEL_ID, 0) + 1).ToString();
        if (!editorMode) 
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, content);
    }

    public void SendRestart()
    {
        string content = (PlayerPrefs.GetInt(PREFS_KEY_LEVEL_ID, 0) + 1).ToString();
        if (!editorMode) GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, content);
        //Debug.Log("Analitics Restart " + content);
    }

    public void SendComplete()
    {
        string content = (PlayerPrefs.GetInt(PREFS_KEY_LEVEL_ID, 0) + 1).ToString();
        if (!editorMode) GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, content);
        //Debug.Log("Analitics Complete " + content);
    }

    #endregion
    */
}

[System.Serializable]
public class Level
{
    public GameObject LevelPrefab;
    public Material SkyboxMaterial; 
}
