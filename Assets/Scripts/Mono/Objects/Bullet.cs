using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Range(0, 0.1f)]
    [SerializeField] private float Follow;
    private float ImpulseMagn;
    private ICube Enemy;

    [Header("Components")]
    [SerializeField] private Rigidbody _rig;

    public void Fire(Vector3 Impulse, float Distance, ICube cube)
    {
        if (_rig == null) _rig = GetComponent<Rigidbody>();
        _rig.velocity = Impulse;
        ImpulseMagn = Impulse.magnitude;

        Enemy = cube;
        StartCoroutine(WaitDisable(Distance));
    }
    private IEnumerator WaitDisable(float Distance)
    {
        float Dist = 0;
        Vector3 prev = transform.position;
        while(Dist < Distance)
        {
            Dist += (transform.position - prev).magnitude;
            prev = transform.position;
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

    private void FixedUpdate()
    {
        if(Enemy != null && !Enemy.isNull)
        {
            Vector3 Dir = (Enemy.CubeTransform.position - transform.position).normalized;
            _rig.velocity = Vector3.Lerp(_rig.velocity, ImpulseMagn * Dir, Follow);
        }
    }
}
