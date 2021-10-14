using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turrel : MonoBehaviour, IActivate
{
    [SerializeField] private bool Activated;
    [SerializeField] private bool Ready;
    public enum ViewField { Field42, Field90};
    [SerializeField] private ViewField FieldOfView;
    [Header("Component")]
    [SerializeField] private Animator _anim;
    [SerializeField] private Transform MaxField42;
    [SerializeField] private Transform MaxField90;

    [Header("States")]
    private ICube Enemy;
    private Transform CurrantMax;
    private float Radius;

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
        _anim.SetBool("Activated", Enemy != null);
        if (Enemy != null)
        {
            
        }
        else
        {

        }
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
