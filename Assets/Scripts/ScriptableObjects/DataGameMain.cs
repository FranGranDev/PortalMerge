using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DataGameMain", menuName = "Data/DataGameMain")]
public class DataGameMain : ScriptableObject
{
    [Header("Физика куба")]
    [Range(0, 1f)]
    public float SpeedSumOnMerge;
    [Range(0, 10f)]
    public float VerticalForceOnMerge;
    [Range(0, 10f)]
    public float RotationOnMerge;
    [Range(0.1f, 1f)]
    public float CubeDragSpeed;
    [Range(0.1f, 1f)]
    public float CubeDragAcceleration;
    [Range(0.1f, 1f)]
    public float CubeFriction;
    [Range(0, 0.6f)] 
    public float CubeHeightOnMove; //Если куб будет слишком высоко, то код будет сличтать, что он в воздухе и отпускать его, поэтому ограничение 0.6
    [Range(0, 1f)]
    public float MinDistanceToMove;
    public bool OffGravityOnTake;
    [Header("Физика камеры")]
    public bool LockSideMove;
    [Range(1, 100f)]
    public float CameraFollowSpeed;
    [Range(1, 25f)]
    public float CameraFriction;
    [Header("Настройка камеры")]
    public bool MoveToCubeOnEnterPortal;
    public bool CameraFollowCube;
    [Range(0, 150f)]
    public float FollowCubeDeadZone;
    [Range(0, 10f)]
    public float FollowCubeSpeed;
    [Range(0.1f, 1f)]
    public float MoveToPortalSpeed;
    [Header("Настройка телепортов")]
    [Range(0, 1f)]
    public float SaveVelocityOnExitPortal;
    [Range(0, 5f)]
    public float AddVelocityOnExitPortal;
    [Header("Настройка ловушек")]
    public float DestroyDelay;
    [Range(0.1f, 1f)]
    public float ObstacleAcceleration;
    [Header("Настройка активаторов")]
    public float DelayTime;
    [Header("Партиклы")]
    [Range(0.25f, 2f)]
    public float DestroyParticleSize;
    [Range(0.25f, 2f)]
    public float MergeParticleSize;

    public ParticleSystem CubeDestroyed;
    public ParticleSystem CubesMerge;
    [Header("Префабы")]
    public GameObject Cube;
    [Header("Цвета")]
    [SerializeField] private Color[] CubeColor;
    public Color GetCubeColor(int num)
    {
        if(num > CubeColor.Length - 1 || num < 0)
        {
            return Color.black;
        }
        return CubeColor[num];
    }
}
