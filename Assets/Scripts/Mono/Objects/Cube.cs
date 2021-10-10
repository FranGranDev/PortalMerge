using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Cube : MonoBehaviour, ICube
{
    [Header("Settings")]
    [SerializeField]private int _number;
    public int Number { get => _number; private set => _number = value; }
    private Color CurrantColor;
    public Color CubeColor { get => CurrantColor; }

    [Header("Physics")]
    private float DragSpeed;
    private float DragAcceleration;
    private float MinDistanceToMove = 0.5f;
    private float CubeHeightOnMove = 0.5f;
    private bool OffGravityOnTake;

    #region Callbacks
    public delegate void OnCubeAction(ICube cube);
    public OnCubeAction OnCubeDestroyed;
    public OnCubeAction OnCubeEnterPortal;
    public OnCubeAction OnCubeLeaveGround;
    public delegate void OnCubeMergeAction(ICube cube1, ICube cube2);
    public OnCubeMergeAction OnCubesMerge;

    public void SubscribeForDestroyed(OnCubeAction action, bool Unsubscribe = false)
    {
        if(Unsubscribe)
        {
            OnCubeDestroyed -= action;
        }
        else
        {
            OnCubeDestroyed += action;
        }

    }
    public void SubscribeForEnterPortal(OnCubeAction action, bool Unsubscribe = false)
    {
        if (Unsubscribe)
        {
            OnCubeEnterPortal -= action;
        }
        else
        {
            OnCubeEnterPortal += action;
        }
    }
    public void SubscribeForLeaveGround(OnCubeAction action, bool Unsubscribe = false)
    {
        if (Unsubscribe)
        {
            OnCubeLeaveGround -= action;
        }
        else
        {
            OnCubeLeaveGround += action;
        }
    }
    public void SubscribeForMerge(OnCubeMergeAction action, bool Unsubscribe = false)
    {
        if(Unsubscribe)
        {
            OnCubesMerge -= action;
        }
        else
        {
            OnCubesMerge += action;
        }
        
    }

    #endregion

    [Header("Components")]
    [SerializeField] private Rigidbody _rig;
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private Collider _collider;
    private Material _material;
    public Collider CubeCol { get => _collider; }
    public Transform CubeTransform { get => transform; }
    public GameObject CubeObject { get => gameObject; }
    public Rigidbody CubeRig { get => _rig; }
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI[] TextNumber;


    [Header("States")]
    [SerializeField] private bool inAir;
    [SerializeField] private bool isMoving;
    private Transform PrevParent;
    public ICube PrevCube { get; private set; }
    public bool isNull => Equals(null);


    #region Action
    public void TryMerge(ICube other)
    { 
        if(Number == other.Number)
        {
            if (other.PrevCube == null)
            {
                PrevCube = this;
                OnCubesMerge?.Invoke(this, other); //OnCubesMerge(ICube cube1, ICube cube2) in GameManager
            }
        }
        
    }
    public void OnEnterPortal()
    {
        OnCubeEnterPortal?.Invoke(this);
    }
    public void OnEnterTrap()
    {
        OnCubeLeaveGround?.Invoke(this);
    }
    public void SetNullParent()
    {
        transform.parent = PrevParent;
    }
    public void DestroyCube()
    {
        CreateDestroyParticle();

        OnCubeDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
    #endregion
    #region Particle
    public void CreateDestroyParticle()
    {
        ParticleSystem partilce = Instantiate(GameManagement.MainData.CubeDestroyed, transform.position, transform.rotation);
        ParticleSystemRenderer partilceRender = partilce.GetComponent<ParticleSystemRenderer>();
        partilceRender.material.color = CurrantColor;

    }
    public void CreateMergeParticle()
    {
        ParticleSystem partilce = Instantiate(GameManagement.MainData.CubesMerge, transform.position, transform.rotation, transform);
        ParticleSystemRenderer partilceRender = partilce.GetComponent<ParticleSystemRenderer>();
        partilceRender.material.color = CurrantColor;

    }
    #endregion
    #region Movement
    public void Take()
    {
        isMoving = true;

        if (OffGravityOnTake)
        {
            _rig.useGravity = false;
        }
    }
    public void Drag(Vector3 Point)
    {
        Point += Vector3.up * transform.localScale.y * CubeHeightOnMove;
        Vector3 Direction = (Point - transform.position);
        if (Direction.magnitude > MinDistanceToMove)
        {
            float AirRatio = inAir && OffGravityOnTake ? 1f : 0f;
            Vector3 NewSpeed = Direction.normalized * DragSpeed / (AirRatio + 1) * 50 + Physics.gravity * AirRatio;
            _rig.velocity = Vector3.Lerp(_rig.velocity, NewSpeed, DragAcceleration * 10 * Time.deltaTime);
        }
        else
        {
            Vector3 NewSpeed = Physics.gravity;
            _rig.velocity = Vector3.Lerp(_rig.velocity, NewSpeed, 25 * Time.deltaTime);
        }
    }
    public void Throw()
    {
        isMoving = false;

        if(OffGravityOnTake)
        {
            _rig.useGravity = true;
        }
    }
    public void AddImpulse(Vector3 Impulse)
    {
        _rig.velocity += Impulse;
    }
    public void AddImpulse(Vector3 Impulse, Vector3 Angular)
    {
        _rig.velocity += Impulse;
        _rig.angularVelocity += Angular;
    }
    private void OnEnterGround()
    {
        inAir = false;
        if(OnLeaveGroundCoroutine != null)
        {
            StopCoroutine(OnLeaveGroundCoroutine);
        }
    }
    private void OnLeaveGround()
    {
        inAir = true;
        OnLeaveGroundCoroutine = StartCoroutine(OnLeaveGroundCour());
    }
    private Coroutine OnLeaveGroundCoroutine;
    private IEnumerator OnLeaveGroundCour()
    {
        yield return new WaitForSeconds(0.025f);
        if (Physics.Raycast(transform.position, Vector3.down, 3, 1 << 8)) //���� �� ��� ���� �����?
            yield break;
        if(isMoving)
        {
            CubeRig.velocity = new Vector3(CubeRig.velocity.x * 0.5f, CubeRig.velocity.y - 5, CubeRig.velocity.z * 0.5f);
            CubeRig.useGravity = true;
            isMoving = false;
        }
        OnCubeLeaveGround?.Invoke(this);
        OnLeaveGroundCoroutine = null;
        yield break;
    }
    #endregion
    #region Init
    private void SetComponents()
    {
        PrevParent = transform.parent;

        if (_rig == null)
        {
            _rig = GetComponent<Rigidbody>();
        }
        if(_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }
        if(_material == null)
        {
            _material = _meshRenderer.material;
        }
    }
    private void SetView()
    {
        CurrantColor = GameManagement.MainData.GetCubeColor(Mathf.RoundToInt(Mathf.Log(Number, 2) - 1));
        _material.color = CurrantColor;
    }
    private void ApplySettings()
    {
        DragSpeed = GameManagement.MainData.CubeDragSpeed;
        DragAcceleration = GameManagement.MainData.CubeDragAcceleration;
        MinDistanceToMove = GameManagement.MainData.MinDistanceToMove;
        OffGravityOnTake = GameManagement.MainData.OffGravityOnTake;
        CubeHeightOnMove = GameManagement.MainData.CubeHeightOnMove;
    }
    private void SetNumbers()
    {
        for(int i = 0; i < TextNumber.Length; i++)
        {
            TextNumber[i].text = Number.ToString();
        }
    }

    public void InitCube()
    {
        SetComponents();
        SetView();
        ApplySettings();
        SetNumbers();
    }
    public void InitCube(int num)
    {
        Number = num;
        InitCube();
    }
    public void InitCube(int num, Vector3 Impulse, Vector3 Angular)
    {
        Number = num;
        InitCube();

        AddImpulse(Impulse, Angular);
    }
    #endregion
    private void OnTriggerStay(Collider other)
    {
        switch (other.tag)
        {
            case "Ground":
                OnEnterGround();
                break;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        switch(other.tag)
        {
            case "Cube":
                ICube cube = other.GetComponent<ICube>();
                if(cube != null)
                {
                    TryMerge(cube);
                }
                break;
            case "Ground":
                OnEnterGround();
                break;
            case "Death":
                DestroyCube();
                break;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        switch (other.tag)
        {
            case "Ground":
                OnLeaveGround();
                break;

        }
    }

    private void Awake()
    {

    }
    private void Start()
    {
        
    }

}

public interface ICube
{
    bool isNull { get; }

    int Number { get; }

    Color CubeColor { get; }

    Transform CubeTransform { get; }

    GameObject CubeObject { get; }

    Rigidbody CubeRig { get; }

    Collider CubeCol { get; }

    void SetNullParent();
    void Take();

    void Drag(Vector3 Point);

    void Throw();

    void DestroyCube();

    void OnEnterPortal();

    void OnEnterTrap();

    void CreateMergeParticle();

    void CreateDestroyParticle();

    void AddImpulse(Vector3 Impulse);

    void AddImpulse(Vector3 Impulse, Vector3 Angular);

    void InitCube();

    void InitCube(int num);

    void InitCube(int num, Vector3 Impulse, Vector3 Angular);

    ICube PrevCube { get; }

    void SubscribeForDestroyed(Cube.OnCubeAction action, bool Unsubscribe = false);

    void SubscribeForEnterPortal(Cube.OnCubeAction action, bool Unsubscribe = false);

    void SubscribeForLeaveGround(Cube.OnCubeAction action, bool Unsubscribe = false);

    void SubscribeForMerge(Cube.OnCubeMergeAction action, bool Unsubscribe = false);

}