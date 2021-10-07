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
    [Range(0, 0.6f)] //Если куб будет слишком высоко, то код будет сличтать, что он в воздухе и отпускать его
    public float CubeHeightOnMove;
    [Range(0, 1f)]
    public float MinDistanceToMove;
    public bool OffGravityOnTake;
    [Header("Физика камеры")]
    public bool LockSideMove;
    [Range(1, 100)]
    public float CameraFollowSpeed;
    [Range(1, 25)]
    public float CameraFriction;
    [Header("Настройка активаторов")]
    public float DelayTime;
    [Header("Префабы")]
    public GameObject Cube;
    [Header("Цвета")]
    [SerializeField ]private Color[] CubeColor;
    public Color GetCubeColor(int num)
    {
        if(num > CubeColor.Length - 1 || num < 0)
        {
            return Color.black;
        }
        return CubeColor[num];
    }
}
