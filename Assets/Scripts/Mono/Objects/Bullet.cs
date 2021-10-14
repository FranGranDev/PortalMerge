using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody _rig;

    public void Fire(Vector3 Impulse, float Distance)
    {
        if (_rig == null) _rig = GetComponent<Rigidbody>();
        _rig.velocity = Impulse;

        StartCoroutine(WaitDisable(Distance));
    }
    private IEnumerator WaitDisable(float Distance)
    {
        Vector3 StartPoint = transform.position;
        while((StartPoint - transform.position).magnitude < Distance)
        {
            yield return new WaitForFixedUpdate();
        }
        DestroyBullet();
        yield break;
    }

    private void DestroyBullet()
    {
        ParticleSystem partilce = Instantiate(GameManagement.MainData.BulletDestroy, transform.position, transform.rotation, null);

        Destroy(gameObject);
    }

    private void OnCubeEntered(ICube cube)
    {
        if (cube.isDestroyed)
            return;
        cube.DestroyCube();
        DestroyBullet();
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
