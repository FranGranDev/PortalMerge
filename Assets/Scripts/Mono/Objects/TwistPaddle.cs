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

            StartCoroutine(Delay(cube));
        }
    }
    private IEnumerator Delay(ICube cube)
    {
        NoAction = true;
        Physics.IgnoreCollision(cube.CubeCol, _collider, true);
        yield return new WaitForSeconds(0.25f);
        NoAction = false;

        if(!cube.isNull)
        {
            Physics.IgnoreCollision(cube.CubeCol, _collider, false);
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
    }
}
