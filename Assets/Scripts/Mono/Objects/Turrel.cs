using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turrel : MonoBehaviour, IActivate
{
    [Range(0f, 1)]
    [SerializeField] private float RotationSpeed;
    [SerializeField] private bool Activated;
    [SerializeField] private bool MoveTo;
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
            Vector3 Direction = (Enemy.CubeTransform.position - transform.position).normalized;
            Direction = new Vector3(Direction.x, 0, Direction.z);
            Main.transform.forward = Vector3.Lerp(Main.transform.forward, Direction, RotationSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 Direction = transform.forward;
            Direction = new Vector3(Direction.x, 0, Direction.z);
            Main.transform.forward = Vector3.Lerp(Main.transform.forward, Direction, RotationSpeed * Time.deltaTime);
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
