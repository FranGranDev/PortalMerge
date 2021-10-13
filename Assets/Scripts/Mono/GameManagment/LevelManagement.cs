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
    const string PREFS_KEY_LAST_INDEX = "LastLevelIndex";


    public bool editorMode;
    public int CurrentLevelCount => PlayerPrefs.GetInt(PREFS_KEY_LEVEL_ID, 0) + 1;
    public int CurrentLevelIndex;
    public List<Level> Levels = new List<Level>();

    public void Start()
    {
#if UNITY_EDITOR
#else
            editorMode = false;
#endif
        if (!editorMode)
        {
            SelectLevel(PlayerPrefs.GetInt(PREFS_KEY_LAST_INDEX, 0), false);
        }
    }

    public void StartGame()
    {
        //SendStart();
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

        if (level.LevelPrefab)
        {
            SetLevelParams(level);
            CurrentLevelIndex = levelIndex;
        }
    }

    public void NextLevel() 
    {
        SelectLevel(CurrentLevelIndex + 1);

        if(!editorMode)
        {
            PlayerPrefs.SetInt(PREFS_KEY_LEVEL_ID, (PlayerPrefs.GetInt(PREFS_KEY_LEVEL_ID, 0) + 1));
            GameManagement.Active.StartGame();
        }
    }

    public void PrevLevel() =>
        SelectLevel(CurrentLevelIndex - 1);
    private int GetCorrectedIndex(int levelIndex)
    {
        if (editorMode)
        {
            return levelIndex > Levels.Count - 1 || levelIndex <= 0 ? 0 : levelIndex;
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
                //Dont work when Android FIX
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
            }  
        }

        if (level.SkyboxMaterial)
        {
            RenderSettings.skybox = level.SkyboxMaterial;
        }


    }

    private void ClearChilds()
    {
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
        PlayerPrefs.SetInt(PREFS_KEY_LAST_INDEX, CurrentLevelIndex);
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
