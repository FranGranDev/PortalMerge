using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TwistPaddle : MonoBehaviour
{
    [SerializeField] private Twist TwistParent;
    private MeshCollider _collider;
    private Transform Normal;
    private bool NoAction;

    private void OnCubeEntered(ICube cube)
    {
        if(!NoAction)
        {
            Vector3 Direction = (-Normal.up + Vector3.up * 0.25f).normalized;
            cube.AddImpulse(Direction * TwistParent.Impulse());
            cube.OnEnterTrap();

            StartCoroutine(Delay(cube.CubeCol));
        }
    }
    private void OnManEntered(IMan man)
    {
        if (!NoAction)
        {
            Vector3 Direction = (-Normal.up + Vector3.up * 0.25f).normalized;
            man.AddImpulse(Direction * TwistParent.Impulse());
            man.TurnRagdollOn();

            StartCoroutine(Delay(man.ManCollider));
        }
    }
    private IEnumerator Delay(Collider Col)
    {
        NoAction = true;
        Physics.IgnoreCollision(Col, _collider, true);
        yield return new WaitForSeconds(0.25f);
        NoAction = false;

        if(Col != null)
        {
            Physics.IgnoreCollision(Col, _collider, false);
        }
        
        yield break;
    }
    private void Init()
    {
        if (TwistParent == null)
        {
            TwistParent = transform.parent.parent.GetComponent<Twist>();
        }
        Normal = transform.GetChild(0);
        _collider = GetComponent<MeshCollider>();
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
        else if (collider.tag == "Man")
        {
            IMan man = collider.GetComponent<IMan>();
            OnManEntered(man);
        }
    }
}
