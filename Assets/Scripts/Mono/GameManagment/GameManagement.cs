using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagement : MonoBehaviour
{
    public static GameManagement Active { get; private set; }
    public GameManagement() => Active = this;

    [SerializeField] private DataGameMain _data;
    [SerializeField] private Transform _level;
    public Transform LevelTransform { get => _level; }
    [SerializeField] private LevelManagement levelManagement;
    public Transform GetLevelTransform => _level.GetChild(0);
    public static DataGameMain MainData {get => Active._data; }
    [SerializeField]private List<ICube> Cubes;
    [SerializeField] private List<ICollected> Gems;
    public static ICube LastCube;
    private bool isPower2(int num)
    {
        if (num > 1)
        {
            return (Mathf.Round(Mathf.Log(num, 2)) == Mathf.Log(num, 2));
        }
        else
        {
            return false;
        }
    }
    [Header("Game States")]
    [SerializeField] private int TargetNum;
    [SerializeField] private int GemCollected;
    [SerializeField] private int MaxGems;
    [SerializeField] public static bool isGameStarted;
    [SerializeField] public static bool isGameWin;
    [Header("Components")]
    [SerializeField] private StaticCube RenderCube;
    #region Callbacks
    public delegate void OnGameAction();
    public static OnGameAction OnGameWin;
    public static OnGameAction OnGameFailed;
    public static OnGameAction OnGameStarted;
    
    #endregion

    public static Vector3 RandomVector()
    {
        return new Vector3(RandomOne(), RandomOne(), RandomOne());
    }
    public static float RandomOne()
    {
        return Random.Range(0, 2) == 0 ? 1 : -1 * (Random.Range(0.5f, 1f));
    }
    public static float RandomOne(float from)
    {
        return Random.Range(0, 2) == 0 ? 1 : -1 * (Random.Range(from, 1f));
    }

    public void Init()
    {
        GetAllCubesOnScene();
        GetAllGemsOnScene();
    }

    public void ShowCubesNumbers()
    {
        GameObject[] obj = GameObject.FindGameObjectsWithTag("Cube");
        for (int i = 0; i < obj.Length; i++)
        {
            ICube cube = obj[i].GetComponent<ICube>();
            cube.InitCube();
        }
    }

    private void GetAllCubesOnScene()
    {
        Cubes = new List<ICube>();
        TargetNum = 0;
        GameObject[] obj = GameObject.FindGameObjectsWithTag("Cube");
        for(int i = 0; i < obj.Length; i++)
        {
            ICube cube = obj[i].GetComponent<ICube>();
            Cubes.Add(cube);
            cube.InitCube();
            

            SubscribeForCube(cube);
            TargetNum += cube.Number;
        }

        RenderCube.SetView(TargetNum);
    }
    private void GetAllGemsOnScene()
    {
        Gems = new List<ICollected>();
        MaxGems = 0;
        GemCollected = 0;

        GameObject[] obj = GameObject.FindGameObjectsWithTag("Gem");
        for (int i = 0; i < obj.Length; i++)
        {
            ICollected gem = obj[i].GetComponent<ICollected>();
            gem.Init();
            Gems.Add(gem);
            

            SubscribeForGem(gem);
            MaxGems++;
        }
    }

    private void SubscribeForGem(ICollected gem)
    {
        gem.SubscribeFor(OnGemCollected);

        gem.SubscribeFor(UIManager.Active.OnGemCollected);
    }
    private void UnsubscribeForGem(ICollected gem)
    {
        gem.SubscribeFor(OnGemCollected, true);
    }

    private void SubscribeForCube(ICube cube)
    {
        cube.SubscribeForMerge(OnCubesMerge);
        cube.SubscribeForEnterPortal(OnCubeEnterPortal);
        cube.SubscribeForDestroyed(OnCubeDestroyed);

        if(MainData.MoveToCubeOnEnterPortal)
        {
            CameraMovement.active.SubcribeToCube(cube);
        }
    }
    private void UnsubscribeForCube(ICube cube)
    {
        cube.SubscribeForMerge(OnCubesMerge, true);
        cube.SubscribeForEnterPortal(OnCubeEnterPortal, true);
        cube.SubscribeForDestroyed(OnCubeDestroyed, true);

        if (MainData.MoveToCubeOnEnterPortal)
        {
            CameraMovement.active.SubcribeToCube(cube, true);
        }
    }

    private void DestroyCube(ICube cube)
    {
        UnsubscribeForCube(cube);
        if (Cubes.Exists(item => item == cube))
        {
            Cubes.Remove(cube);
        }
        cube.ForceDestroy();
    }
    private void OnCubeDestroyed(ICube cube)
    {
        UnsubscribeForCube(cube);
        if(Cubes.Exists(item => item == cube))
        {
            Cubes.Remove(cube);
            GameFailed();
        }
    }
    private void OnCubeEnterPortal(ICube cube)
    {

    }

    private void OnCubesMerge(ICube cube1, ICube cube2)
    {
        //StartCoroutine(OnCubesMergeCour(cube1, cube2));
        //return;
        int CubeSum = cube1.Number + cube2.Number;
        Vector3 CubePosition = (cube1.CubeTransform.position + cube2.CubeTransform.position) / 2;
        Vector3 CubeImpulse = (cube1.CubeRig.velocity + cube2.CubeRig.velocity) * MainData.SpeedSumOnMerge;
        CubeImpulse += Vector3.up * MainData.VerticalForceOnMerge;
        Vector3 CubeAngular = MainData.RotationOnMerge * RandomVector();

        ICube newCube = Instantiate(MainData.Cube, CubePosition, cube1.CubeTransform.rotation, GetLevelTransform).GetComponent<ICube>();
        newCube.InitCube(CubeSum, CubeImpulse, CubeAngular, cube1.AfterPortal || cube2.AfterPortal);
        newCube.CreateMergeParticle();
        Cubes.Add(newCube);
        SubscribeForCube(newCube);
        LastCube = newCube;

        InputManagement.Active.SubscribeForCube(newCube);
        if (newCube.Number == TargetNum)
        {
            Vector3 WinCubeImpulse = (cube1.CubeRig.velocity + cube2.CubeRig.velocity) * MainData.SpeedSumOnMerge;
            WinCubeImpulse += Vector3.up * MainData.VerticalForceOnFinalMerge;
            Vector3 WinCubeAngular = MainData.RotationOnMerge * RandomVector();
            newCube.SetImpulse(WinCubeImpulse, WinCubeAngular);
            newCube.StartFinalMerge();

            GameWin();
        }

        DestroyCube(cube1);
        DestroyCube(cube2);
    }
    private IEnumerator OnCubesMergeCour(ICube cube1, ICube cube2)
    {
        int CubeSum = cube1.Number + cube2.Number;


        cube1.AddImpulse(Vector3.up * MainData.VerticalForceOnMerge * 1.5f);
        cube2.AddImpulse(Vector3.up * MainData.VerticalForceOnMerge * 1.5f);
        yield return new WaitForSeconds(0.5f);

        Vector3 CubePosition = (cube1.CubeTransform.position + cube2.CubeTransform.position) / 2;
        Vector3 CubeImpulse = (cube1.CubeRig.velocity + cube2.CubeRig.velocity) / 2;
       
        Vector3 CubeAngular = MainData.RotationOnMerge * RandomVector();

        ICube newCube = Instantiate(MainData.Cube, CubePosition, cube1.CubeTransform.rotation, GetLevelTransform).GetComponent<ICube>();
        newCube.InitCube(CubeSum, CubeImpulse, CubeAngular, cube1.AfterPortal || cube2.AfterPortal);
        newCube.CreateMergeParticle();
        Cubes.Add(newCube);
        SubscribeForCube(newCube);
        LastCube = newCube;

        InputManagement.Active.SubscribeForCube(newCube);
        if (newCube.Number == TargetNum)
        {
            Vector3 WinCubeImpulse = (cube1.CubeRig.velocity + cube2.CubeRig.velocity) * MainData.SpeedSumOnMerge;
            WinCubeImpulse += Vector3.up * MainData.VerticalForceOnFinalMerge;
            Vector3 WinCubeAngular = MainData.RotationOnMerge * RandomVector();
            newCube.SetImpulse(WinCubeImpulse, WinCubeAngular);
            newCube.StartFinalMerge();

            GameWin();
        }

        DestroyCube(cube1);
        DestroyCube(cube2);
        yield break;
    }

    private void OnGemCollected(ICollected gem)
    {
        UnsubscribeForGem(gem);
        if(Gems.Exists(item => item == gem))
        {
            GemCollected++;
            Gems.Remove(gem);
        }

    }

    public void StartGame()
    {
        isGameStarted = true;
        isGameWin = false;

        OnGameStarted?.Invoke();
    }
    private void GameFailed()
    {
        if (!isGameStarted)
            return;
        isGameStarted = false;
        isGameWin = false;
        OnGameFailed?.Invoke();

        SoundManagment.PlaySound("lose", null, 0.5f);
    }
    private void GameWin()
    {
        if (!isGameStarted)
            return;
        isGameStarted = false;
        isGameWin = true;
        OnGameWin?.Invoke();

        SoundManagment.PlaySound("win");
    }

    private void Awake()
    {
        Active = this;
    }
    private void Start()
    {
        //Init();
    }
}
