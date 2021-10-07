using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour, IPortal, IActivate
{
    [Header("Pair")]
    [SerializeField] private Portal SecondPortal;
    [SerializeField] private IPortal PairPortal => SecondPortal;
    private ICube prevCube;

    public Transform PortalTransform => transform;

    public void SetPair(Portal portal)
    {
        SecondPortal = portal;
    }

    public void OnCubeEntered(ICube cube)
    {
        if(PairPortal != null)
        {
            if (prevCube == null)
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
        cube.ClearPrevPortal();
    }

    public void Teleport(ICube cube)
    {
        cube.CubeTransform.parent = PairPortal.PortalTransform;
        Vector3 DeltaPosition = (cube.CubeTransform.localPosition);
        cube.CubeTransform.parent = transform;
        

        prevCube = cube;
        cube.CubeRig.velocity = (cube.CubeRig.velocity.magnitude + GameManagement.MainData.AddVelocityOnExitPortal)
        * (transform.forward + Vector3.up * 0.25f) * GameManagement.MainData.SaveVelocityOnExitPortal;
        cube.CubeTransform.localPosition = DeltaPosition;

        prevCube.SetNullParent();
    }

    private void OnTeleported()
    {
        prevCube.ClearPrevPortal();


        ClearPrevCube();
        PairPortal.ClearPrevCube();
    }

    public void ClearPrevCube()
    {
        prevCube = null;
    }

    public void Activate(bool on = true)
    {
        Debug.Log("Portal: " + on);
    }

    private void Awake()
    {
        if(PairPortal != null)
        {
            PairPortal.SetPair(this);
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

    void ClearPrevCube();

    void SetPair(Portal portal);
}
