using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turrel : MonoBehaviour, IActivate
{
    [Range(0f, 10f)]
    [SerializeField] private float BulletSpeed;
    [Range(0f, 1)]
    [SerializeField] private float ReloadTime;
    private bool Reloaded;
    private bool Reloading;
    [Range(0f, 1)]
    [SerializeField] private float RotationSpeed;
    [SerializeField] private bool Activated;
    private bool MoveTo;
    public void TurnMove(bool on)
    {
        MoveTo = on;
    }
    public enum ViewField { Field42, Field90};
    [SerializeField] private ViewField FieldOfView;
    [Header("Component")]
    [SerializeField] private Transform Main;
    [SerializeField] private Animator _anim;
    [SerializeField] private Transform MaxField42;
    [SerializeField] private Transform MaxField90;
    [SerializeField] private Transform[] FirePoint;

    [Header("States")]
    private ICube Enemy;
    private Transform CurrantMax;
    private float Radius;
    private Coroutine MoveToCoroutine;

    private bool InFieldOfView(Transform obj)
    {
        Vector3 Dir = (obj.position - transform.position).normalized;
        Vector3 MaxDir = (CurrantMax.position - transform.position).normalized;
        return (Vector3.Dot(Dir, transform.forward) > Vector3.Dot(MaxDir, transform.forward));
    }

    public void Activate(bool on)
    {
        Activated = on;
    }

    private void Init()
    {
        switch (FieldOfView)
        {
            case ViewField.Field42:
                CurrantMax = MaxField42;
                
                Radius = (transform.position - CurrantMax.position).magnitude;
                break;
            case ViewField.Field90:
                CurrantMax = MaxField90;
                Radius = (transform.position - CurrantMax.position).magnitude;
                break;
        }
        if (_anim == null) _anim = transform.GetComponentInChildren<Animator>();
    }
    private void CheckForCube()
    {
        if (!Activated)
        {
            return;
        }
        Collider[] colliders = Physics.OverlapSphere(transform.position, Radius, 1 << 9);
        Enemy = null;
        foreach (Collider col in colliders)
        {
            if (InFieldOfView(col.transform) && col.tag == "Cube")
            {
                Enemy = col.GetComponent<ICube>();
                break;
            }
        }
    }
    private void ActionExecute()
    {
        if(!Activated)
        {
            _anim.SetBool("Activated", false);
            return;
        }
        if(MoveTo && Enemy == null)
        {
            MoveTo = false;
            if(MoveToCoroutine != null)
            {
                StopCoroutine(MoveToCoroutine);
            }
            Reloaded = false;
        }
        if(!MoveTo && Enemy != null && MoveToCoroutine == null)
        {
            MoveToCoroutine = StartCoroutine(ActivateDelayed());
        }

        _anim.SetBool("Activated", Enemy != null);
        if (MoveTo)
        {
            Vector3 Direction = (Enemy.CubeTransform.position - transform.position).normalized;
            Direction = new Vector3(Direction.x, 0, Direction.z);
            Main.transform.forward = Vector3.Lerp(Main.transform.forward, Direction, RotationSpeed * 2 * Time.deltaTime);
        }
        else
        {
            Vector3 Direction = transform.forward;
            Direction = new Vector3(Direction.x, 0, Direction.z);
            Main.transform.forward = Vector3.Lerp(Main.transform.forward, Direction, RotationSpeed * 2 * Time.deltaTime);
        }

        if (MoveTo && Enemy != null)
        {
            Vector3 FireDirection = (transform.position - Enemy.CubeTransform.position).normalized;
            if (Reloaded && !Reloading)
            {
                if (Vector3.Dot(FireDirection, Main.transform.forward) < 0.01f)
                {
                    Fire();
                }
            }
            else if (!Reloading)
            {
                Reload();
            }
        }
    }
    private IEnumerator ActivateDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        MoveTo = true;
        MoveToCoroutine = null;
        yield break;
    }

    private void Fire()
    {
        Reloaded = false;

        _anim.SetTrigger("Fire");
        for(int i = 0; i < FirePoint.Length; i++)
        {
            ParticleSystem partilce = Instantiate(GameManagement.MainData.TurrelFire, FirePoint[i].position, FirePoint[i].rotation, null);

            Bullet bullet = Instantiate(GameManagement.MainData.Bullet, FirePoint[i].position, FirePoint[i].rotation, null).GetComponent<Bullet>();
            bullet.Fire(Main.transform.forward * BulletSpeed);
        }
        
    }
    private void Reload()
    {
        StartCoroutine(ReloadCour());
    }
    private IEnumerator ReloadCour()
    {
        Reloading = true;
        yield return new WaitForSeconds(ReloadTime);
        Reloaded = true;
        Reloading = false;
        yield break;
    }

    private void Start()
    {
        Init();
    }
    private void FixedUpdate()
    {
        CheckForCube();
        ActionExecute();
    }
}
