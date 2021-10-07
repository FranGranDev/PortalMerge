using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour, IPortal, IActivate
{
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
        if (prevCube == null)
        {
            PairPortal.Teleport(cube);
        }
    }

    public void Teleport(ICube cube)
    {
        cube.CubeTransform.parent = PairPortal.PortalTransform;
        Vector3 DeltaPosition = (cube.CubeTransform.localPosition);
        cube.CubeTransform.parent = transform;
        

        prevCube = cube;
        cube.CubeRig.velocity = cube.CubeRig.velocity.magnitude * transform.up;
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

    public void Activate(bool on = true)
    {
        Debug.Log("Portal: " + on);
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
