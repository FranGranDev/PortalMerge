using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody _rig;

    public void Fire(Vector3 Impulse)
    {
        if (_rig == null) _rig = GetComponent<Rigidbody>();
        _rig.velocity = Impulse;

        StartCoroutine(WaitDisable());
    }
    private IEnumerator WaitDisable()
    {
        yield return new WaitForSeconds(10);
        Destroy(gameObject);
        yield break;
    }

    private void OnCubeEntered(ICube cube)
    {
        if (cube.isDestroyed)
            return;
        cube.DestroyCube();
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Cube")
        {
            ICube cube = other.GetComponent<ICube>();
            OnCubeEntered(cube);
        }
    }

    void Start()
    {
        
    }
}
