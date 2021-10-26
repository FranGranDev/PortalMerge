using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour, IPortal, IActivate
{
    [Header("Settings")]
    [SerializeField] private Transform ExitPoint;
    [SerializeField] private AnimationCurve HorizontalMove;
    [SerializeField] private AnimationCurve VerticalMove;
    [SerializeField] private float Height;
    [SerializeField] private float TimeMove;
    [Header("Pair")]
    [SerializeField] private Portal SecondPortal;
    [SerializeField] private IPortal PairPortal => SecondPortal;
    private IPortal PrevPortal;
    [SerializeField] private bool isActive = true;
    public bool Activated { get => isActive; }
    private ICube prevCube;
    private Dictionary<ICube, Coroutine> TeleportCoroutine = new Dictionary<ICube, Coroutine>();

    private ISound Sound;
    private Animator _anim;

    public Transform PortalTransform => transform;

    public void SetPair(Portal portal)
    {
        PrevPortal = portal;
    }

    public void OnCubeEntered(ICube cube)
    {
        if (!isActive)
            return;
        if(PairPortal != null)
        {
            if ((prevCube == null || cube != prevCube) && PairPortal.Activated)
            {
                PairPortal.Teleport(cube);
            }
        }
        else
        {
            DontLetCube(cube);
            Debug.Log("Нет ссылки на парный портал!");
        }
        
    }

    private void DontLetCube(ICube cube)
    {
        cube.OnFailedEnterPortal();
        cube.CubeRig.velocity *= -0.5f;
        Vector3 CubeImpulse = Vector3.up * 5;
        Vector3 CubeAngular = new Vector3(GameManagement.RandomOne(), GameManagement.RandomOne(), GameManagement.RandomOne()) * 5;
        cube.AddImpulse(CubeImpulse, CubeAngular);
        ClearPrevCube();
    }

    public void Teleport(ICube cube)
    {
        if(!TeleportCoroutine.ContainsKey(cube))
        {
            TeleportCoroutine[cube] = StartCoroutine(TeleportCour(cube));
        }
    }
    private IEnumerator TeleportCour(ICube cube)
    {
        if (!cube.AfterPortal)
        {
            yield return new WaitForSeconds(GameManagement.MainData.TeleportTime);
            cube.SubscribeForFailedMerge(StopTeleport);
            cube.OnEnterPortal();
            prevCube = cube;
            yield return new WaitForSeconds(GameManagement.MainData.TeleportTime);
            cube.CubeTransform.position = transform.position;

            prevCube.OnExitPortal();

            float CurrantTime = 0;
            Vector3 OffsetY = Vector3.zero;
            Vector3 CurrantPoint = transform.position;
            Vector3 PrevPoint = CurrantPoint;
            Vector3 EndPoint = ExitPoint.position;
            while (CurrantTime < TimeMove * 0.95f)
            {
                if(cube.isNull || !cube.isPortalMoving)
                {
                    yield break;
                }
                OffsetY = Vector3.up * Height * (VerticalMove.Evaluate(CurrantTime / TimeMove));
                CurrantPoint = Vector3.Lerp(transform.position, EndPoint, HorizontalMove.Evaluate(CurrantTime / TimeMove));
                cube.CubeRig.position = CurrantPoint + OffsetY;
                cube.CubeRig.velocity = (CurrantPoint - PrevPoint) / Time.fixedDeltaTime;
                PrevPoint = CurrantPoint;
                CurrantTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            cube.SubscribeForFailedMerge(StopTeleport, true);
            cube.OnExitPortalMoveEnd();

            ClearPrevCube();
            PairPortal.ClearPrevCube();
        }
            
        
        TeleportCoroutine.Remove(cube);
        yield break;
    }

    public void StopTeleport(ICube cube, ICube other)
    {
        StartCoroutine(StopTeleportCour(cube));
    }
    private IEnumerator StopTeleportCour(ICube cube)
    {
        if (TeleportCoroutine.ContainsKey(cube))
        {
            StopCoroutine(TeleportCoroutine[cube]);
            TeleportCoroutine.Remove(cube);
        }
        if (cube == prevCube)
        {
            cube.SubscribeForFailedMerge(StopTeleport, true);
            prevCube.OnExitPortalMoveEnd();
            

            ClearPrevCube();
            PairPortal.ClearPrevCube();
        }

        
        yield break;
    }

    public void ClearPrevCube()
    {
        prevCube = null;
    }



    public void Activate(bool on = true)
    {
        isActive = on;

        _anim?.SetBool("Active", on);

        ClearPrevCube();
        if(Sound != null)
        {
            Sound.Mute(!on);
        }
    }
    private void Init()
    {
        if (PairPortal != null)
        {
            PairPortal.SetPair(this);
        }
        TryGetComponent<Animator>(out _anim);

        Activate(isActive);

        TryGetComponent(out Sound);
        if (Sound != null)
        {
            Sound.Init(Activated);
        }
    }

    private void Start()
    {
        Init();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Cube")
        {
            ICube cube = other.GetComponent<ICube>();
            OnCubeEntered(cube);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        //if(other.tag == "Cube")
        //{
        //    ICube cube = other.GetComponent<ICube>();
        //    if(cube == prevCube)
        //    {
        //        OnTeleported(cube);
        //    }
        //}
    }
}
public interface IPortal
{
    Transform PortalTransform { get; }

    void OnCubeEntered(ICube cube);

    void Teleport(ICube cube);

    bool Activated { get; }

    void ClearPrevCube();

    void SetPair(Portal portal);
}
