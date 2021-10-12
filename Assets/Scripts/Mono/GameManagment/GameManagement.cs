using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagement : MonoBehaviour
{
    public static GameManagement Active { get; private set; }
    [SerializeField] private DataGameMain _data;
    [SerializeField] private Transform _level;
    [SerializeField] private LevelManagement levelManagement;
    private Transform GetLevelTransform => _level.GetChild(0);
    public static DataGameMain MainData {get => Active._data; }
    [SerializeField]private List<ICube> Cubes;
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
    private int TargetNum;
    public static bool isGameStarted;

    #region Callbacks
    public delegate void OnGameAction();
    public static OnGameAction OnGameWin;
    public static OnGameAction OnGameFailed;
    public static OnGameAction OnGameStarted;

    #endregion

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
            SubscribeForCube(cube);
            cube.InitCube();
            TargetNum += cube.Number;
        }
        if(!isPower2(TargetNum))
        {
            Debug.Log("На сцене должно быть кубиков кратно степени двойки!!!");
        }
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
            Destroy(cube.CubeObject);
        }
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
        int CubeSum = cube1.Number + cube2.Number;
        Vector3 CubePosition = (cube1.CubeTransform.position + cube2.CubeTransform.position) / 2;
        Vector3 CubeImpulse = (cube1.CubeRig.velocity + cube2.CubeRig.velocity) * MainData.SpeedSumOnMerge;
        CubeImpulse += Vector3.up * MainData.VerticalForceOnMerge;
        Vector3 CubeAngular = MainData.RotationOnMerge * new Vector3(RandomOne(), RandomOne(), RandomOne());
        DestroyCube(cube1);
        DestroyCube(cube2);

        ICube newCube = Instantiate(MainData.Cube, CubePosition, cube1.CubeTransform.rotation, GetLevelTransform).GetComponent<ICube>();
        newCube.InitCube(CubeSum, CubeImpulse, CubeAngular);
        newCube.CreateMergeParticle();
        Cubes.Add(newCube);
        SubscribeForCube(newCube);
        LastCube = newCube;

        if (newCube.Number == TargetNum)
        {
            GameWin();
        }
    }

    public void StartGame()
    {
        isGameStarted = true;

        GetAllCubesOnScene();
        OnGameStarted?.Invoke();
    }
    private void GameFailed()
    {
        if (!isGameStarted)
            return;
        isGameStarted = false;
        OnGameFailed?.Invoke();
    }
    private void GameWin()
    {
        if (!isGameStarted)
            return;
        isGameStarted = false;
        OnGameWin?.Invoke();
    }

    private void Awake()
    {
        Active = this;
    }
    private void Start()
    {
        
    }
}
