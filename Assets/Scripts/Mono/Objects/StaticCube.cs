using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StaticCube : MonoBehaviour
{
    public static void MakeCopy(ICube cube)
    {
        StaticCube Copy = Instantiate(GameManagement.MainData.CubeCopy, cube.CubeTransform.position, cube.CubeTransform.rotation, null).GetComponent<StaticCube>();
        Copy.SetView(cube.Number, cube);
    }

    private int Number;
    public float AnimationScale;
    private Vector3 StartScale;
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
    private void SetMaterial()
    {
        Material material = GameManagement.MainData.GetCubeMaterial(Mathf.RoundToInt(Mathf.Log(Number, 2) - 1));
        _meshRenderer.material = material;
    }
    private void SetNumbers()
    {
        for (int i = 0; i < TextNumber.Length; i++)
        {
            TextNumber[i].text = Number.ToString();
        }
    }

    public void SetView(int num, ICube cube = null)
    {
        if(cube != null)
        {
            StartScale = cube.StartScale;
        }

        Number = num;

        SetComponents();
        SetMaterial();
        SetNumbers();
    }

    public void DestroyCopy()
    {
        Destroy(gameObject);
    }

    private void Awake()
    {
        AnimationScale = 1f;
        StartScale = transform.localScale;
    }
    private void FixedUpdate()
    {
        transform.localScale = StartScale * AnimationScale;
    }
}
