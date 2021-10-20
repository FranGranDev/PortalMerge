using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StaticCube : MonoBehaviour
{
    public static void MakeCopy(ICube cube)
    {
        StaticCube Copy = Instantiate(GameManagement.MainData.CubeCopy, cube.CubeTransform.position, cube.CubeTransform.rotation, null).GetComponent<StaticCube>();
        Copy.SetView(cube.Number);
    }

    private int Number;
    [Header("Components")]
    [SerializeField] private MeshRenderer _meshRenderer;
    private Material _material;
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI[] TextNumber;
    private Animator _anim;

    private void SetComponents()
    {
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }
        if (_material == null)
        {
            _material = _meshRenderer.material;
        }
    }
    private void SetColor()
    {
        _material.color = GameManagement.MainData.GetCubeColor(Mathf.RoundToInt(Mathf.Log(Number, 2) - 1));
    }
    private void SetNumbers()
    {
        for (int i = 0; i < TextNumber.Length; i++)
        {
            TextNumber[i].text = Number.ToString();
        }
    }

    public void SetView(int num)
    {
        Number = num;

        SetComponents();
        SetColor();
        SetNumbers();
    }

    public void DestroyCopy()
    {
        Destroy(gameObject);
    }
}
