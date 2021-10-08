using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour, IPortal, IActivate
{
    [Header("Pair")]
    [SerializeField] private Portal SecondPortal;
    [SerializeField] private IPortal PairPortal => SecondPortal;
    [SerializeField] private bool isActive = true;
    public bool Activated { get => isActive; }
    private ICube prevCube;

    private Animator _anim;

    public Transform PortalTransform => transform;

    public void SetPair(Portal portal)
    {
        SecondPortal = portal;
    }

    public void OnCubeEntered(ICube cube)
    {
        if (!isActive)
            return;
        if(PairPortal != null)
        {
            if (prevCube == null && PairPortal.Activated)
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
        cube.CubeRig.velocity *= -0.2f;
        Vector3 CubeImpulse = Vector3.up * 5;
        Vector3 CubeAngular = new Vector3(GameManagement.RandomOne(), GameManagement.RandomOne(), GameManagement.RandomOne()) * 5;
        cube.AddImpulse(CubeImpulse, CubeAngular);
        ClearPrevCube();
    }

    public void Teleport(ICube cube)
    {
        StartCoroutine(TeleportCour(cube));
    }
    private IEnumerator TeleportCour(ICube cube)
    {
        cube.CubeTransform.parent = PairPortal.PortalTransform;
        yield return new WaitForFixedUpdate();
        Vector3 DeltaPosition = (cube.CubeTransform.localPosition);
        cube.CubeTransform.parent = transform;


        prevCube = cube;
        cube.CubeRig.velocity = (cube.CubeRig.velocity.magnitude + GameManagement.MainData.AddVelocityOnExitPortal)
        * (transform.forward + Vector3.up * 0.25f) * GameManagement.MainData.SaveVelocityOnExitPortal;
        cube.CubeTransform.localPosition = DeltaPosition;
        yield return new WaitForFixedUpdate();
        prevCube.SetNullParent();
        cube.OnEnterPortal();
        yield break;
    }

    private void OnTeleported()
    {
        ClearPrevCube();
        PairPortal.ClearPrevCube();
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
            if (cube != prevCube)
            {
                OnCubeEntered(cube);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if(other.tag == "Cube")
        {
            ICube cube = other.GetComponent<ICube>();
            if(cube == prevCube)
            {
                OnTeleported();
            }
        }
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
