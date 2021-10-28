#if UNITY_EDITOR
using UnityEngine;
using System.Reflection;
using System.Collections;
using System.IO;
using UnityEditor;
using System;

[System.Serializable]
public class ScreenshotResolution
{
    public bool MakeShot;

    public int width;
    public int height;
}

public class ScreenshotMaker : MonoBehaviour
{
    public enum KeyToShot
    {
        RightMouseButton,
        F
    }

    public KeyToShot keyToShot;
    public ScreenshotResolution[] resolutionsToShot;

    private void Awake()
    {
        if(!IsPreparedToShot())
        {
            Debug.LogError("<color=red> Screenshots will not save. Please, set 'MakeShot' to true at least at the one of resoutions. </color>");
        }
        else
        {
            InitializeDirectories();
        }
    }

    private bool IsPreparedToShot()
    {
        for (int i = 0; i < resolutionsToShot.Length; i++)
        {
            if (resolutionsToShot[i].MakeShot)
                return true;
        }

        return false;
    }

    private void InitializeDirectories()
    {
        for (int i = 0; i < resolutionsToShot.Length; i++)
        {
            if(resolutionsToShot[i].MakeShot)
                CreateDirectoriesIfNotExists(resolutionsToShot[i].width, resolutionsToShot[i].height);
        }
    }

    private void CreateDirectoriesIfNotExists(int width, int height)
    {
        if (!Directory.Exists("Assets/ScreenshotMaker"))
        {
            Directory.CreateDirectory("Assets/ScreenshotMaker");
            Debug.Log($"<color=red> Directory with path 'Assets/ScreenshotMaker' was not found and created automatically. </color>");
        }
        if (!Directory.Exists("Assets/ScreenshotMaker/Screenshots"))
        {
            Directory.CreateDirectory($"Assets/ScreenshotMaker/Screenshots");
            Debug.Log($"<color=red> Directory with path 'Assets/ScreenshotMaker/Screenshots' was not found and created automatically. </color>");
        }
        if (!Directory.Exists($"Assets/ScreenshotMaker/Screenshots/{width}x{height}"))
        {
            Directory.CreateDirectory($"Assets/ScreenshotMaker/Screenshots/{width}x{height}");
            Debug.Log($"<color=red> Directory with path 'Assets/ScreenshotMaker/Screenshots{width}x{height}' was not found and created automatically. </color>");
        }
    }

    private void Update()
	{
        switch (keyToShot)
        {
            case KeyToShot.RightMouseButton:
                if (Input.GetMouseButtonDown(1))
                {
                    StartCoroutine(MakeScreenShot());
                }
                break;
            case KeyToShot.F:
                if (Input.GetKeyDown(KeyCode.F))
                {
                    StartCoroutine(MakeScreenShot());
                }
                break;
        }
    }
    
    private IEnumerator MakeScreenShot()
    {
        Time.timeScale = 0.1f;

        for (int i = 0; i < resolutionsToShot.Length; i++)
        {
            if (resolutionsToShot[i].MakeShot)
            {
                yield return new WaitForEndOfFrame();

                GameViewUtils.SetSize(resolutionsToShot[i].width, resolutionsToShot[i].height);

                yield return new WaitForEndOfFrame();

                Texture2D shot = GetScreenShot();
                byte[] shotData = shot.EncodeToPNG();
                Destroy(shot);

                string savePath = GetNumberedSavePath(resolutionsToShot[i].width, resolutionsToShot[i].height);

                File.WriteAllBytes(savePath, shotData);
            }
        }

        Time.timeScale = 1;
    }

    private Texture2D GetScreenShot()
    {
        int width = Screen.width;
        int height = Screen.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        return tex;
    }

    private string GetNumberedSavePath(int width, int height)
    {
        System.Text.StringBuilder savePath = new System.Text.StringBuilder();
        savePath.Append("Assets/ScreenshotMaker/Screenshots/");
        savePath.Append($"{width}x{height}/");

        string[] existingShots = Directory.GetFiles(savePath.ToString());
        int currentShotNumber = 0;
        int lastShotNumber = 0;
        for (int j = 0; j < existingShots.Length; j++)
        {
            string shotNumber = existingShots[j].Split('/')[4].Split('_')[0];

            currentShotNumber = int.Parse(shotNumber);
            if (currentShotNumber > lastShotNumber)
                lastShotNumber = currentShotNumber;
        }

        lastShotNumber++;

        savePath.Append($"{lastShotNumber.ToString("00")}_{width}x{height}.png");

        return savePath.ToString();
    }

    [ContextMenu("Clear ALL Screenshots")]
    public void ClearAllScreenShots()
    {
        DirectoryInfo directoryOfScreenShots = new DirectoryInfo("Assets/ScreenshotMaker/Screenshots/");
        DirectoryInfo[] shotFolders = directoryOfScreenShots.GetDirectories();
        for (int i = 0; i < shotFolders.Length; i++)
        {
            FileInfo[] shots = shotFolders[i].GetFiles();
            for (int j = 0; j < shots.Length; j++)
            {
                shots[j].Delete();
            }
        }
        AssetDatabase.Refresh();
    }
}

public class GameViewUtils
{
    static object gameViewSizesInstance;
    static MethodInfo getGroup;

    static GameViewUtils()
    {
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instanceProp = singleType.GetProperty("instance");
        getGroup = sizesType.GetMethod("GetGroup");
        gameViewSizesInstance = instanceProp.GetValue(null, null);
    }

    public enum GameViewSizeType
    {
        AspectRatio, FixedResolution
    }

    public static void SetSize(int width, int height)
    {
        string currentPlatformName = EditorUserBuildSettings.activeBuildTarget.ToString();
        GameViewSizeGroupType currentPlatformSizeGroup = (GameViewSizeGroupType)System.Enum.Parse(typeof(GameViewSizeGroupType), currentPlatformName);
        int sizeIndex = FindSize(currentPlatformSizeGroup, width, height);

        Debug.Log($"Sizeindex of screen {width}x{height} = " + sizeIndex);
        //Debug.LogError("There should be check for platform, but there is not.");
        if (sizeIndex == -1)
        {
            //AddCustomSize(GameViewSizeType.FixedResolution, currentPlatformSizeGroup, width, height, $"{width}x{height}");
            Debug.LogError($"{width}x{height} resolution was not found in resolutions list of GameView.");
            return;
            //sizeIndex = FindSize(currentPlatformSizeGroup, width, height);
        }

        var gvWndType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var gvWnd = EditorWindow.GetWindow(gvWndType);
        var SizeSelectionCallback = gvWndType.GetMethod("SizeSelectionCallback",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        SizeSelectionCallback.Invoke(gvWnd, new object[] { sizeIndex, null });
    }

    public static int FindSize(GameViewSizeGroupType sizeGroupType, int width, int height)
    {
        var group = GetGroup(sizeGroupType);
        var groupType = group.GetType();
        var getBuiltinCount = groupType.GetMethod("GetBuiltinCount");
        var getCustomCount = groupType.GetMethod("GetCustomCount");
        int sizesCount = (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);
        var getGameViewSize = groupType.GetMethod("GetGameViewSize");
        var gvsType = getGameViewSize.ReturnType;
        var widthProp = gvsType.GetProperty("width");
        var heightProp = gvsType.GetProperty("height");
        var indexValue = new object[1];
        for (int i = 0; i < sizesCount; i++)
        {
            indexValue[0] = i;
            var size = getGameViewSize.Invoke(group, indexValue);
            int sizeWidth = (int)widthProp.GetValue(size, null);
            int sizeHeight = (int)heightProp.GetValue(size, null);
            if (sizeWidth == width && sizeHeight == height)
                return i;
        }
        return -1;
    }

    static object GetGroup(GameViewSizeGroupType type)
    {
        return getGroup.Invoke(gameViewSizesInstance, new object[] { (int)type });
    }

    public static void AddCustomSize(GameViewSizeType viewSizeType, GameViewSizeGroupType sizeGroupType, int width, int height, string text)
    {
        // goal:
        // var group = ScriptableSingleton<GameViewSizes>.instance.GetGroup(sizeGroupType);
        // group.AddCustomSize(new GameViewSize(viewSizeType, width, height, text);

        var asm = typeof(Editor).Assembly;
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instanceProp = singleType.GetProperty("instance");
        var getGroup = sizesType.GetMethod("GetGroup");
        var instance = instanceProp.GetValue(null, null);
        var group = getGroup.Invoke(instance, new object[] { (int)sizeGroupType });
        var addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize"); // or group.GetType().
        var gvsType = asm.GetType("UnityEditor.GameViewSize");

        var types = new Type[] { typeof(GameViewSizeType), typeof(int), typeof(int), typeof(string) };
        // in original 
        // var types = new Type[] { typeof(int), typeof(int), typeof(int), typeof(string) };
        // throws exception too

        var ctor = gvsType.GetConstructor(types);

        // throws exception here
        var newSize = ctor.Invoke(new object[] { viewSizeType, width, height, text });
        // in original
        // var newSize = ctor.Invoke(new object[] { (int)viewSizeType, width, height, text });
        // throws exception too

        addCustomSize.Invoke(group, new object[] { newSize });

        sizesType.GetMethod("SaveToHDD").Invoke(gameViewSizesInstance, null);
    }

}
#endif