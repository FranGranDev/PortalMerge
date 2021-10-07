using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagement : MonoBehaviour
{
    private static GameManagement active;
    [SerializeField] private DataGameMain _data;
    [SerializeField] private Transform _level;
    private Transform GetLevelTransform => _level.GetChild(0);
    public static DataGameMain MainData {get => active._data; }
    [SerializeField]private List<ICube> Cubes;

    public static float RandomOne()
    {
        return Random.Range(0, 2) == 0 ? 1 : -1 * (Random.Range(0.5f, 1f));
    }
    public static float RandomOne(float from)
    {
        return Random.Range(0, 2) == 0 ? 1 : -1 * (Random.Range(from, 1f));
    }

    private void GetAllCubesOnScene()
    {
        Cubes = new List<ICube>();

        GameObject[] obj = GameObject.FindGameObjectsWithTag("Cube");
        for(int i = 0; i < obj.Length; i++)
        {
            ICube cube = obj[i].GetComponent<ICube>();
            Cubes.Add(cube);
            SubscribeForCube(cube);
            cube.InitCube();
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

    private void OnCubeDestroyed(ICube cube)
    {
        UnsubscribeForCube(cube);
        if(Cubes.Exists(item => item == cube))
        {
            //Some action
            Cubes.Remove(cube);
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
        cube1.DestroyCube();
        cube2.DestroyCube();

        ICube newCube = Instantiate(MainData.Cube, CubePosition, cube1.CubeTransform.rotation, GetLevelTransform).GetComponent<ICube>();
        newCube.InitCube(CubeSum, CubeImpulse, CubeAngular);
        Cubes.Add(newCube);

        SubscribeForCube(newCube);
    }


    private void Awake()
    {
        active = this;
        GetAllCubesOnScene();
    }
    private void Start()
    {
        
    }
}
