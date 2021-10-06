using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DataGameMain", menuName = "Data/DataGameMain")]
public class DataGameMain : ScriptableObject
{

    [Header("Prefabs")]
    public GameObject Cube;
    [Header("Colors")]
    public Color[] CubeColor;
    public Color GetCubeColor(int num)
    {
        if(num > CubeColor.Length - 1 || num < 0)
        {
            return Color.black;
        }
        return CubeColor[num];
    }
}
