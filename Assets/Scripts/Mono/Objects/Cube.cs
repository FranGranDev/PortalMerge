using System.Collections;
using TMPro;
using UnityEngine;

public class Cube : MonoBehaviour, ICube
{
    [Header("Settings")]
    [SerializeField] private int _number;
    [HideInInspector] public int Number { get => _number; private set => _number = value; }
    public enum DestroyType {Laser, Fall, Water, Bullet}
    private string DestroyTypeToSoundID(DestroyType type)
    {
        switch (type)
        {
            case DestroyType.Fall:
                return "destroy_fall";
            case DestroyType.Laser:
                return "destroy_laser";
            case DestroyType.Water:
                return "destroy_water";
            case DestroyType.Bullet:
                return "destroy_bullet";
            default:
                return "destroy_fall";
        }
        
    }

    [Header("Physics")]
    private float DragSpeed;
    private float DragAcceleration;
    private float MinDistanceToMove = 0.5f;
    private float CubeHeightOnMove = 0.5f;

    #region Callbacks
    public delegate void OnCubeAction(ICube cube);
    public OnCubeAction OnCubeDestroyed;
    public OnCubeAction OnCubeEnterPortal;
    public OnCubeAction OnCubeExitPortal;
    public OnCubeAction OnCubeLeaveGround;
    public delegate void OnCubeMergeAction(ICube cube1, ICube cube2);
    public OnCubeMergeAction OnCubesMerge;
    public OnCubeMergeAction OnCubesFailedMerge;


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
    public void SubscribeForFailedMerge(OnCubeMergeAction action, bool Unsubscribe = false)
    {
        if (Unsubscribe)
        {
            OnCubesFailedMerge -= action;
        }
        else
        {
            OnCubesFailedMerge += action;
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
    private ParticleSystem Aura;
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI[] TextNumber;


    //States
    public bool AfterPortal { get; private set; }
    public bool NoTelepor { get; private set; }
    public bool AfterMerge { get; private set; }
    public bool NoInput { get; private set; }
    private Color CurrantColor;
    public Color CubeColor { get => CurrantColor; }
    public Vector3 StartScale { get; private set; }
    public float AnimationScale;
    public bool inAir { get; private set; }
    public bool isMoving { get; private set; }
    public bool isPortalMoving { get; private set; }
    public bool isOutOfZone { get; private set; }
    public bool isOnPlatform { get; private set; }
    private Transform PrevParent;
    public ICube PrevCube { get; set; }
    private BarrierMove PrevPlatform;
    public bool isNull => Equals(null);
    public bool isDestroyed { get; private set; }

    #region Action
    public void TryMerge(ICube other)
    {
        if (Number == other.Number)
        {
            if (PrevCube == null && other.PrevCube == null)
            {
                PrevCube = other;
                other.PrevCube = this;
                OnCubesMerge?.Invoke(this, other); //OnCubesMerge(ICube cube1, ICube cube2) in GameManager
            }
        }
        else
        {
            OnCubesFailedMerge?.Invoke(this, other);
        }

    }
    private void MakeCopy() => StaticCube.MakeCopy(this);
    public void OnEnterPortal()
    {
        isPortalMoving = true;

        MakeCopy();

        OnCubeEnterPortal?.Invoke(this);

        _animator.SetTrigger(ANIM_ENTER);

        DestroyCubeAura();
    }
    public void OnFailedEnterPortal()
    {
        OnCubeLeaveGround?.Invoke(this);
    }
    public void OnExitPortal()
    {
        OnCubeExitPortal?.Invoke(this);

        _animator.SetTrigger(ANIM_EXIT);

        OnTeleportWait();

        SoundManagment.PlaySound("portal", transform);
    }
    public void OnExitPortalMoveEnd()
    {
        isPortalMoving = false;
        SetNullParent();
    }
    public void OnEnterTrap()
    {
        OnCubeLeaveGround?.Invoke(this);

        if (OnLeaveZoneCoroutine != null)
        {
            StopCoroutine(OnLeaveZoneCoroutine);
        }
        OnLeaveZoneCoroutine = StartCoroutine(OnLeaveZoneCour());

        DestroyCubeAura();
        

        SoundManagment.PlaySound("twirl_punch", transform);   
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
    public void StartFinalMerge()
    {
        StartCoroutine(FinalMergeCour());
    }
    private IEnumerator FinalMergeCour()
    {
        while (CubeRig.velocity.magnitude > 5f)
        {
            yield return new WaitForFixedUpdate();
        }
        CubeRig.velocity *= 0.75f;
        CubeRig.useGravity = false;
        StartCoroutine(FinalParticleCour());
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

        SoundManagment.PlaySound(DestroyTypeToSoundID(DestroyType.Water), transform);

        OnCubeDestroyed?.Invoke(this);
        _animator.Play(ANIM_DIE);
        Destroy(gameObject, 1f);
        DestroyCubeAura();
    }
    public void DestroyCube(DestroyType type = DestroyType.Fall)
    {
        if (isDestroyed || GameManagement.isGameWin)
            return;
        isDestroyed = true;
        CreateDestroyParticle();

        OnCubeDestroyed?.Invoke(this);
        Destroy(gameObject, Time.fixedDeltaTime);
        DestroyCubeAura();

        SoundManagment.PlaySound(DestroyTypeToSoundID(type), transform);
    }
    public void ForceDestroy()
    {
        isDestroyed = true;
        DestroyCubeAura();
        Destroy(gameObject);
    }

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
        SoundManagment.PlaySound("merge", transform);
        float Delay = GameManagement.MainData.VerticalForceOnMerge * 0.03f;

        NoInput = true;
        AfterMerge = true;
        yield return new WaitForSeconds(Delay);
        NoInput = false;
        yield return new WaitForSeconds(Delay);
        AfterMerge = false;
        yield break;
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

        CreateAuraParticle();
    }
    public void Drag(Vector3 Point)
    {
        if (inAir)
            return;
        Point += Vector3.up * transform.localScale.y * CubeHeightOnMove;
        Vector3 Direction = (Point - transform.position);
        if (Direction.magnitude > MinDistanceToMove)
        {
            Vector3 NewSpeed = Direction.normalized * DragSpeed * 50;
            float DragY = (NewSpeed.y > 0) ? 0 : NewSpeed.y;
            NewSpeed = new Vector3(NewSpeed.x, inAir ? CubeRig.velocity.y : DragY, NewSpeed.z);
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
    public void SetImpulse(Vector3 Impulse, Vector3 Angular)
    {
        _rig.velocity = Impulse;
        _rig.angularVelocity = Angular;
    }
    private void CheckBelowScene()
    {
        if(InputManagement.Active.BelowGameZone(transform))
        {
            DestroyCube(DestroyType.Fall);
        }
    }
    private void OnEnterGround()
    {
        
        if(OnEnterGroundCoroutine == null)
        {
            OnEnterGroundCoroutine = StartCoroutine(OnEnterGroundCour());
        }
    }
    private Coroutine OnEnterGroundCoroutine;
    private IEnumerator OnEnterGroundCour()
    {
        yield return new WaitForSeconds(0.25f);
        if (!Physics.Raycast(transform.position, Vector3.down, 3, 1 << 8)) //есть ли под нами земля?
        {
            inAir = true;
            OnEnterGroundCoroutine = null;
            yield break;
        }
        inAir = false;
        OnEnterGroundCoroutine = null;
        yield break;
    }
    private void OnLeaveGround()
    {
        if(OnEnterGroundCoroutine != null)
        {
            StopCoroutine(OnEnterGroundCoroutine);
            OnEnterGroundCoroutine = null;
        }
        if (OnLeaveGroundCoroutine == null)
        {
            OnLeaveGroundCoroutine = StartCoroutine(OnLeaveGroundCour());
        }
    }
    private Coroutine OnLeaveGroundCoroutine;
    private IEnumerator OnLeaveGroundCour()
    {
        yield return new WaitForSeconds(0.05f);
        inAir = true;
        if (Physics.Raycast(transform.position, Vector3.down, 3, 1 << 8)) //есть ли под нами земля?
        {
            inAir = false;
            OnLeaveGroundCoroutine = null;
            yield break;
        }
        if (isMoving)
        {
            CubeRig.velocity = new Vector3(CubeRig.velocity.x * 0.5f, CubeRig.velocity.y, CubeRig.velocity.z * 0.5f);
            isMoving = false;
        }
        CubeRig.useGravity = true;
        OnCubeLeaveGround?.Invoke(this);
        OnLeaveGroundCoroutine = null;
        yield break;
    }

    private void SetScale()
    {
        if(_animator.enabled)
        {
            transform.localScale = StartScale * AnimationScale;
        }
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
    private IEnumerator FinalParticleCour()
    {
        Vector3 Offset = new Vector3(0f, 0, 0.1f);
        GameObject particle = Instantiate(GameManagement.MainData.RadialShine, transform.position, Quaternion.identity, PrevParent);
        Vector3 Scale = particle.transform.localScale;
        Material material = particle.GetComponent<MeshRenderer>().material;
        Color TargetColor = material.color;
        material.color = new Color(1, 1, 1, 0);
        particle.transform.localScale = Vector3.zero;
        while (!isNull)
        {
            particle.transform.position = transform.position + Vector3.down * 2;
            particle.transform.Rotate(Vector3.up, 10 * Time.fixedDeltaTime);
            particle.transform.localScale = Vector3.Lerp(particle.transform.localScale, Scale, 0.025f);
            material.color = Color.Lerp(material.color, TargetColor, 0.02f);
            yield return new WaitForFixedUpdate();
        }

        yield break;
    }
    private void CreateAuraParticle()
    {
        StartCoroutine(AuraParticleCour());
    }
    private Coroutine AuraParticleCoroutine;
    private IEnumerator AuraParticleCour()
    {

        ParticleSystem CubeAura = Instantiate(GameManagement.MainData.CubeAura, transform.position, Quaternion.identity, PrevParent);
        Aura = CubeAura;
        CubeAura.transform.localScale = Vector3.zero;
        var main = CubeAura.main;
        main.startColor = CurrantColor;
        while (!isNull && CubeAura != null && isMoving)
        {
            RaycastHit hit;
            Ray ray = new Ray(transform.position, Vector3.down);
            Vector3 Point = transform.position - Vector3.down * transform.localScale.y;
            if (Physics.Raycast(ray, out hit, 10, 1 << 8))
            {
                Point = new Vector3(transform.position.x, hit.point.y + 0.1f, transform.position.z);
            }
            CubeAura.transform.position = Point;
            CubeAura.transform.localScale = Vector3.Lerp(CubeAura.transform.localScale, Vector3.one, 0.1f);
            yield return new WaitForFixedUpdate();
        }

        while (!isNull && CubeAura != null && CubeAura.transform.localScale.magnitude > 0.1f)
        {
            RaycastHit hit;
            Ray ray = new Ray(transform.position, Vector3.down);
            Vector3 Point = transform.position - Vector3.down * transform.localScale.y;
            if (Physics.Raycast(ray, out hit, 10, 1 << 8))
            {
                Point = new Vector3(transform.position.x, hit.point.y + 0.1f, transform.position.z);
            }
            CubeAura.transform.position = Point;
            CubeAura.transform.localScale = Vector3.Lerp(CubeAura.transform.localScale, Vector3.zero, 0.1f);
            yield return new WaitForFixedUpdate();
        }
        if (CubeAura != null)
        {
            Destroy(CubeAura.gameObject);
        }
        yield break;
    }
    public void DestroyCubeAura()
    {
        if (Aura != null)
        {
            Destroy(Aura);
        }
    }
    #endregion
    #region Init
    private void SetComponents()
    {
        StartScale = transform.localScale;
        AnimationScale = 1;
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
        inAir = true;
    }
    #endregion


    private void OnTriggerStay(Collider other)
    {
        switch (other.tag)
        {
            case "Man":
                OnEnterGround();
                break;
            case "Ground":
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
            case "Platform":
                CheckBelowScene();
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
        InitCube();
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

        SetScale();
    }
}

public interface ICube
{
    bool isNull { get; }

    bool isDestroyed { get; }

    bool NoTelepor { get; }

    int Number { get; }

    bool AfterPortal { get; }

    bool AfterMerge { get; }

    bool NoInput { get; }

    bool isPortalMoving { get; }

    bool isMoving { get; }

    bool inAir { get; }

    bool isOnPlatform { get; }

    Color CubeColor { get; }

    Transform CubeTransform { get; }

    GameObject CubeObject { get; }

    Rigidbody CubeRig { get; }

    Collider CubeCol { get; }

    Vector3 StartScale { get; }

    void SetNullParent();

    void Take();

    void Drag(Vector3 Point);

    void Throw();

    void ForceDestroy();

    void DestroyCube(Cube.DestroyType type = Cube.DestroyType.Fall);

    void DestroyCubeAura();

    void OnEnterPortal();

    void OnFailedEnterPortal();

    void OnExitPortal();

    void OnExitPortalMoveEnd();

    void OnTeleportWait();

    void OnEnterTrap();


    void EnterPlatform(BarrierMove platform);

    void ExitPlatform();

    void StartFinalMerge();

    void CreateMergeParticle();

    void CreateDestroyParticle();

    void AddImpulse(Vector3 Impulse);

    void AddImpulse(Vector3 Impulse, Vector3 Angular);

    void SetImpulse(Vector3 Impulse, Vector3 Angular);

    void InitCube();

    void InitCube(int num);

    void InitCube(int num, Vector3 Impulse, Vector3 Angular, bool AfterMerge = false);

    ICube PrevCube { get; set; }

    void SubscribeForDestroyed(Cube.OnCubeAction action, bool Unsubscribe = false);

    void SubscribeForEnterPortal(Cube.OnCubeAction action, bool Unsubscribe = false);

    void SubscribeForFailedMerge(Cube.OnCubeMergeAction action, bool Unsubscribe = false);

    void SubscribeForLeaveGround(Cube.OnCubeAction action, bool Unsubscribe = false);

    void SubscribeForMerge(Cube.OnCubeMergeAction action, bool Unsubscribe = false);

    void SubscribeForExitPortal(Cube.OnCubeAction action, bool Unsubscribe = false);

}