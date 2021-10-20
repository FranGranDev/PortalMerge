using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Cube : MonoBehaviour, ICube
{
    [Header("Settings")]
    [SerializeField] private int _number;
    [HideInInspector] public int Number { get => _number; private set => _number = value; }
    public bool AfterPortal { get; private set; }
    public bool NoTelepor { get; private set; }
    private bool AfterMerge;
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
    public OnCubeAction OnCubeExitPortal;
    public OnCubeAction OnCubeLeaveGround;
    public delegate void OnCubeMergeAction(ICube cube1, ICube cube2);
    public OnCubeMergeAction OnCubesMerge;

    public void SubscribeForDestroyed(OnCubeAction action, bool Unsubscribe = false)
    {
        if (Unsubscribe)
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
    public void SubscribeForExitPortal(OnCubeAction action, bool Unsubscribe = false)
    {
        if (Unsubscribe)
        {
            OnCubeExitPortal -= action;
        }
        else
        {
            OnCubeExitPortal += action;
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
        if (Unsubscribe)
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
    [SerializeField] private Animator _animator;
    private const string ANIM_ENTER = "EnterPortal";
    private const string ANIM_EXIT = "ExitPortal";
    private const string ANIM_DIE = "EnterWater";
    private Material _material;
    public Collider CubeCol { get => _collider; }
    public Transform CubeTransform { get => transform; }
    public GameObject CubeObject { get => gameObject; }
    public Rigidbody CubeRig { get => _rig; }
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI[] TextNumber;


    //States
    public bool inAir { get; private set; }
    public bool isMoving { get; private set; }
    public bool isOutOfZone { get; private set; }
    public bool isOnPlatform { get; private set; }
    private Transform PrevParent;
    public ICube PrevCube { get; private set; }
    private BarrierMove PrevPlatform;
    public bool isNull => Equals(null);
    public bool isDestroyed { get; private set; }

    #region Action
    public void TryMerge(ICube other)
    {
        if (Number == other.Number)
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
        MakeCopy();

        OnCubeEnterPortal?.Invoke(this);

        _animator.SetTrigger(ANIM_ENTER);
    }
    private void MakeCopy() => StaticCube.MakeCopy(this);
    public void OnFailedEnterPortal()
    {
        OnCubeLeaveGround?.Invoke(this);
    }
    public void OnExitPortal()
    {
        OnCubeExitPortal?.Invoke(this);

        _animator.SetTrigger(ANIM_EXIT);

        OnTeleportWait();
    }
    public void OnEnterTrap()
    {
        OnCubeLeaveGround?.Invoke(this);

        if (OnLeaveZoneCoroutine != null)
        {
            StopCoroutine(OnLeaveZoneCoroutine);
        }
        OnLeaveZoneCoroutine = StartCoroutine(OnLeaveZoneCour());
    }
    private Coroutine OnLeaveZoneCoroutine;
    private IEnumerator OnLeaveZoneCour()
    {
        float time = 5f;
        while (time > 0)
        {
            if (InputManagement.Active.OutOfGameZone(transform))
            {
                CameraMovement.active.FollowCubeDie(this);
                break;
            }

            time -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        OnLeaveZoneCoroutine = null;
        yield break;
    }

    public void EnterPlatform(BarrierMove platform)
    {
        PrevPlatform = platform;
        isOnPlatform = true;

        transform.SetParent(platform.transform, true);
        CubeRig.isKinematic = true;
        _animator.enabled = false;
    }

    public void ExitPlatform()
    {
        if (PrevPlatform != null)
        {
            PrevPlatform.OnExitPlatform(this);
            AddImpulse(PrevPlatform._rig.velocity);
            PrevPlatform = null;
        }

        isOnPlatform = false;

        transform.SetParent(PrevParent);
        CubeRig.isKinematic = false;
        _animator.enabled = true;
    }

    public void SetNullParent()
    {
        transform.parent = PrevParent;
    }
    public void OnWaterEntered()
    {
        if (isDestroyed)
            return;
        isDestroyed = true;
        CreateWaterParticle();

        OnCubeDestroyed?.Invoke(this);
        _animator.Play(ANIM_DIE);
    }
    public void DestroyCube()
    {
        if (isDestroyed || GameManagement.isGameWin)
            return;
        isDestroyed = true;
        CreateDestroyParticle();

        OnCubeDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
    public void ForceDestroy() => Destroy(gameObject);

    private Coroutine OnTeleportWaitCoroutine;
    public void OnTeleportWait()
    {
        if (OnTeleportWaitCoroutine != null)
        {
            StopCoroutine(OnTeleportWaitCoroutine);
        }
        OnTeleportWaitCoroutine = StartCoroutine(OnTeleportWaitCour());
    }
    private IEnumerator OnTeleportWaitCour()
    {
        AfterPortal = true;
        yield return new WaitForSeconds(0.3f);
        AfterPortal = false;
        OnTeleportWaitCoroutine = null;
        yield break;
    }

    private void DelayTeleport()
    {
        StartCoroutine(DelayTeleportCour());
    }
    private IEnumerator DelayTeleportCour()
    {
        NoTelepor = true;
        yield return new WaitForSeconds(0.1f);
        NoTelepor = false;
        yield break;
    }

    private IEnumerator AfterMergeCour()
    {
        AfterMerge = true;
        yield return new WaitForSeconds(0.5f);
        AfterMerge = false;
        yield break;
    }

    #endregion
    #region Particle
    public void CreateWaterParticle()
    {
        ParticleSystem partilce = Instantiate(GameManagement.MainData.CubeWater, transform.position, Quaternion.identity);
        partilce.transform.localScale = Vector3.one * GameManagement.MainData.WaterParticleSize;
    }
    public void CreateDestroyParticle()
    {
        ParticleSystem partilce = Instantiate(GameManagement.MainData.CubeDestroyed, transform.position, transform.rotation);
        ParticleSystemRenderer partilceRender = partilce.GetComponent<ParticleSystemRenderer>();
        partilceRender.material.color = CurrantColor;
        partilce.transform.localScale = transform.localScale * GameManagement.MainData.DestroyParticleSize;
    }
    public void CreateMergeParticle()
    {
        ParticleSystem partilce = Instantiate(GameManagement.MainData.CubesMerge, transform.position, transform.rotation, transform);
        ParticleSystemRenderer partilceRender = partilce.GetComponent<ParticleSystemRenderer>();
        partilceRender.material.color = CurrantColor;
        partilce.transform.localScale = transform.localScale * GameManagement.MainData.MergeParticleSize;

    }
    #endregion
    #region Movement
    public void Take()
    {
        isMoving = true;

        if (isOnPlatform)
        {
            ExitPlatform();
        }
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
            NewSpeed = new Vector3(NewSpeed.x, AfterMerge ? _rig.velocity.y : NewSpeed.y, NewSpeed.z);
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

        if (OffGravityOnTake)
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
        if (OnLeaveGroundCoroutine != null)
        {
            StopCoroutine(OnLeaveGroundCoroutine);
        }
    }
    private void OnLeaveGround()
    {
        OnLeaveGroundCoroutine = StartCoroutine(OnLeaveGroundCour());
    }
    private Coroutine OnLeaveGroundCoroutine;
    private IEnumerator OnLeaveGroundCour()
    {
        yield return new WaitForSeconds(0.025f);
        if (Physics.Raycast(transform.position, Vector3.down, 5, 1 << 8)) //есть ли под нами земля?
        {
            inAir = false;
            yield break;
        }
        inAir = true;
        if (isMoving)
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
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }
    }
    private void SetView()
    {
        Material material = GameManagement.MainData.GetCubeMaterial(Mathf.RoundToInt(Mathf.Log(Number, 2) - 1));
        CurrantColor = material.color;
        _meshRenderer.material = material;
    }
    private void ApplySettings()
    {
        DragSpeed = GameManagement.MainData.CubeDragSpeed;
        DragAcceleration = GameManagement.MainData.CubeDragAcceleration;
        MinDistanceToMove = GameManagement.MainData.MinDistanceToMove;
        OffGravityOnTake = GameManagement.MainData.OffGravityOnTake;
        CubeHeightOnMove = GameManagement.MainData.CubeHeightOnMove;
    }
    public void SetNumbers()
    {
        for (int i = 0; i < TextNumber.Length; i++)
        {
            TextNumber[i].text = _number.ToString();
        }
    }

    private void ClearCallbacks()
    {
        OnCubeDestroyed = null;
        OnCubeEnterPortal = null;
        OnCubeLeaveGround = null;
        OnCubesMerge = null;
    }
    public void InitCube()
    {
        ClearCallbacks();

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
    public void InitCube(int num, Vector3 Impulse, Vector3 Angular, bool AfterPortal = false)
    {
        Number = num;
        InitCube();

        AddImpulse(Impulse, Angular);
        if (AfterPortal)
        {
            DelayTeleport();
        }
        StartCoroutine(AfterMergeCour());
    }
    #endregion

    private void OnTriggerStay(Collider other)
    {
        switch (other.tag)
        {
            case "Ground":
                OnEnterGround();
                break;
            case "Man":
                OnEnterGround();
                break;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        switch (other.tag)
        {
            case "Cube":
                ICube cube = other.GetComponent<ICube>();
                if (cube != null)
                {
                    TryMerge(cube);
                }
                break;
            case "Ground":
                OnEnterGround();
                break;
            case "Death":
                OnWaterEntered();
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
        ClearCallbacks();
    }
    private void Start()
    {

    }

    private void Update()
    {

    }
    private void FixedUpdate()
    {
        if (!isOutOfZone && InputManagement.Active.OutOfGameZone(transform))
        {
            isOutOfZone = true;
            CameraMovement.active.FollowCubeDie(this);
        }
        else if (isOutOfZone && !InputManagement.Active.OutOfGameZone(transform))
        {
            isOutOfZone = false;
        }

    }
}

public interface ICube
{
    bool isNull { get; }

    bool isDestroyed { get; }

    bool NoTelepor { get; }

    int Number { get; }

    bool AfterPortal { get; }

    bool isMoving { get; }

    bool isOnPlatform { get; }

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

    void OnFailedEnterPortal();

    void OnExitPortal();

    void OnTeleportWait();

    void OnEnterTrap();


    void EnterPlatform(BarrierMove platform);

    void ExitPlatform();


    void CreateMergeParticle();

    void CreateDestroyParticle();

    void AddImpulse(Vector3 Impulse);

    void AddImpulse(Vector3 Impulse, Vector3 Angular);

    void InitCube();

    void InitCube(int num);

    void InitCube(int num, Vector3 Impulse, Vector3 Angular, bool AfterMerge = false);

    ICube PrevCube { get; }

    void SubscribeForDestroyed(Cube.OnCubeAction action, bool Unsubscribe = false);

    void SubscribeForEnterPortal(Cube.OnCubeAction action, bool Unsubscribe = false);

    void SubscribeForLeaveGround(Cube.OnCubeAction action, bool Unsubscribe = false);

    void SubscribeForMerge(Cube.OnCubeMergeAction action, bool Unsubscribe = false);

    void SubscribeForExitPortal(Cube.OnCubeAction action, bool Unsubscribe = false);

}