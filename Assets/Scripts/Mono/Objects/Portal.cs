using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour, IPortal, IActivate
{
    [Header("Pair")]
    [SerializeField] private Portal SecondPortal;
    [SerializeField] private IPortal PairPortal => SecondPortal;
    private IPortal PrevPortal;
    [SerializeField] private bool isActive = true;
    public bool Activated { get => isActive; }
    private ICube prevCube;
    private Coroutine TeleportCoroutine;

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
            if ((prevCube == null || cube != prevCube) && !cube.NoTelepor && PairPortal.Activated)
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
        if(TeleportCoroutine == null)
        {
            TeleportCoroutine = StartCoroutine(TeleportCour(cube));
        }
        
    }
    private IEnumerator TeleportCour(ICube cube)
    {
        yield return new WaitForFixedUpdate();
        if (!cube.AfterPortal)
        {
            cube.OnEnterPortal();
            prevCube = cube;
            float PrevCubeVelocity = cube.CubeRig.velocity.magnitude;
            cube.CubeRig.velocity *= 0.25f;
            yield return new WaitForSeconds(GameManagement.MainData.TeleportTime);
            cube.CubeRig.velocity = (transform.forward + Vector3.up).normalized * (PrevCubeVelocity *
            GameManagement.MainData.SaveVelocityOnExitPortal + GameManagement.MainData.AddVelocityOnExitPortal);
            Vector3 CubeAngular = GameManagement.MainData.AddRotationOnExitPortal * new Vector3(GameManagement.RandomOne(), GameManagement.RandomOne(), GameManagement.RandomOne());
            cube.CubeRig.angularVelocity += CubeAngular;
            cube.CubeTransform.position = transform.position + Vector3.up * 3f;
            prevCube.SetNullParent();
            prevCube.OnExitPortal();


            OnTeleported(prevCube);
        }
            
        
        TeleportCoroutine = null;
        yield break;
    }

    private void OnTeleported(ICube cube)
    {
        if(TeleportCoroutine != null)
        {
            StopCoroutine(TeleportCoroutine);
        }
        TeleportCoroutine = StartCoroutine(OnTeleportedCour(cube));
    }
    private IEnumerator OnTeleportedCour(ICube cube)
    {
        float Lenght = (transform.position - cube.CubeTransform.position).magnitude;
        while (!cube.isNull && (transform.position - cube.CubeTransform.position).magnitude < Lenght + 0.1f)
        {
            yield return new WaitForFixedUpdate();
        }
        ClearPrevCube();
        PairPortal.ClearPrevCube();
        

        TeleportCoroutine = null;
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
    }
    private void Init()
    {
        if (PairPortal != null)
        {
            PairPortal.SetPair(this);
        }
        TryGetComponent<Animator>(out _anim);

        Activate(isActive);
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
