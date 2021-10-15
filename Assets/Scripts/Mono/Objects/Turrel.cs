using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turrel : MonoBehaviour, IActivate
{
    [Range(0.5f, 2f)]
    [SerializeField] private float AnimationSpeed;
    [Range(0f, 25f)]
    [SerializeField] private float BulletSpeed;
    [Range(0f, 25f)]
    [SerializeField] private float BulletDistance;
    [Range(0f, 5)]
    [SerializeField] private float ReloadTime;
    private bool Reloaded;
    private bool Reloading;
    [Range(0f, 2f)]
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
    [SerializeField] private Material IdleZone;
    [SerializeField] private Material AngryZone;
    [SerializeField] private MeshRenderer Field42;
    [SerializeField] private MeshRenderer Field90;
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
        Field42.gameObject.SetActive(false);
        Field90.gameObject.SetActive(false);
        switch (FieldOfView)
        {
            case ViewField.Field42:
                CurrantMax = MaxField42;
                Radius = (transform.position - CurrantMax.position).magnitude;
                Field42.gameObject.SetActive(true);
                break;
            case ViewField.Field90:
                CurrantMax = MaxField90;
                Radius = (transform.position - CurrantMax.position).magnitude;
                Field90.gameObject.SetActive(true);
                break;
        }
        if (_anim == null) _anim = transform.GetComponentInChildren<Animator>();
        _anim.SetFloat("Speed", AnimationSpeed);
    }
    private void ChangeMaterial()
    {
        switch (FieldOfView)
        {
            case ViewField.Field42:
                if(Enemy == null)
                {
                    Field42.material = IdleZone;
                }
                else
                {
                    Field42.material = AngryZone;
                }
                break;
            case ViewField.Field90:
                if (Enemy == null)
                {
                    Field90.material = IdleZone;
                }
                else
                {
                    Field90.material = AngryZone;
                }
                break;
        }
        
    }
    private void ActivateZone()
    {
        switch (FieldOfView)
        {
            case ViewField.Field42:
                Field42.gameObject.SetActive(Activated);
                break;
            case ViewField.Field90:
                Field90.gameObject.SetActive(Activated);
                break;
        }
    }

    private void CheckForCube()
    {
        Enemy = null;
        if (!Activated)
        {
            return;
        }
        Collider[] colliders = Physics.OverlapSphere(transform.position, Radius, 1 << 9);
        foreach (Collider col in colliders)
        {
            if (InFieldOfView(col.transform) && col.tag == "Cube")
            {
                Enemy = col.GetComponent<ICube>();
                break;
            }
        }
        ChangeMaterial();
    }
    private void ActionExecute()
    {
        ActivateZone();

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
            RaycastHit[] colliders = Physics.RaycastAll(FirePoint[0].position, FirePoint[0].forward, Radius, 1 << 9);
            foreach(RaycastHit hit in colliders)
            {
                if(hit.transform == Enemy.CubeTransform)
                {
                    if (Reloaded && !Reloading)
                    {
                        Fire();
                    }
                    else if (!Reloading)
                    {
                        Reload();
                    }
                }
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
    }
    public void MakeFire()
    {
        for (int i = 0; i < FirePoint.Length; i++)
        {
            ParticleSystem partilce = Instantiate(GameManagement.MainData.TurrelFire, FirePoint[i].position, FirePoint[i].rotation, null);

            Bullet bullet = Instantiate(GameManagement.MainData.Bullet, FirePoint[i].position, FirePoint[i].rotation, null).GetComponent<Bullet>();
            bullet.Fire(Main.transform.forward * BulletSpeed, BulletDistance, Enemy);
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
