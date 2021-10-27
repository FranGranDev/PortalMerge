using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwistPaddle : MonoBehaviour
{
    [SerializeField] private Twist TwistParent;
    private Transform Normal;
    private bool NoAction;
    private ICube prevCube;

    private void OnCubeEntered(ICube cube)
    {
        if(!NoAction && prevCube != cube)
        {
            Vector3 Direction = (-Normal.up + Vector3.up * 0.25f).normalized;
            cube.AddImpulse(Direction * TwistParent.Impulse());
            cube.OnEnterTrap();
            TwistParent.ReduseSpeed(0.5f);

            prevCube = cube;
        }
    }
    private void OnManEntered(IMan man)
    {
        if (!NoAction)
        {
            Vector3 Direction = (-Normal.up + Vector3.up * 0.25f).normalized;
            man.AddImpulse(Direction * TwistParent.Impulse());
            man.TurnRagdollOn();

            //StartCoroutine(Delay(man.ManCollider));
        }
    }
    private void Init()
    {
        if (TwistParent == null)
        {
            TwistParent = transform.parent.parent.GetComponent<Twist>();
        }
        Normal = transform.GetChild(0);
    }

    private void Start()
    {
        Init();
    }
   
    private void OnTriggerEnter(Collider collider)
    {
        if(collider.tag == "Cube")
        {
            ICube cube = collider.GetComponent<ICube>();
            OnCubeEntered(cube);
        }
    }
    private void OnTriggerExit(Collider collider)
    {
        if (collider.tag == "Cube")
        {
            ICube cube = collider.GetComponent<ICube>();
            if(cube == prevCube)
            {
                prevCube = null;
            }
        }
    }
}
