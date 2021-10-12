using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Man : MonoBehaviour, IMan
{
    [Header("Settings")]
    [SerializeField] private float LieDelay;
    [Header("States")]
    private bool isRagdolled;
    private bool inObject;
    [Header("Components")]
    [SerializeField] private Transform Main;
    private Vector3 MainStartPos;
    private Quaternion MainStartRot;
    [SerializeField] private Rigidbody _rig;
    [SerializeField] private Animator _animator;
    private const string STAND_UP = "StandUp";
    [SerializeField] private Collider _collider;
    private List<Collider> RagdollColliders;
    private List<Rigidbody> RagdollRigibodies;
    private Coroutine RagdollOnCoroutine;
    private Coroutine WakeUpCoroutine;

    public Rigidbody ManRigidbody { get => _rig; }
    public Transform ManTransform { get => transform; }
    public Collider ManCollider{ get => _collider;}

    private void Init()
    {
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_collider == null) _collider = GetComponent<Collider>();
        if (_rig == null) _rig = GetComponent<Rigidbody>();

        MainStartPos = Main.localPosition;
        MainStartRot = Main.transform.localRotation;

        GetRagdollParts();
    }
    private void GetRagdollParts()
    {
        RagdollColliders = new List<Collider>();
        Collider[] collider = GetComponentsInChildren<Collider>();
        foreach(Collider col in collider)
        {
            if(col.gameObject != gameObject)
            {
                col.isTrigger = true;
                RagdollColliders.Add(col);
            }
        }
        RagdollRigibodies = new List<Rigidbody>();
        Rigidbody[] Rigs = GetComponentsInChildren<Rigidbody>();
        foreach(Rigidbody rig in Rigs)
        {
            if(rig.gameObject != gameObject)
            {
                rig.isKinematic = true;
                RagdollRigibodies.Add(rig);
            }
        }
    }

    private void TurnRagdoll(bool on)
    {
        isRagdolled = on;
        _animator.enabled = !on;
        _collider.isTrigger = on;
        if(on)
        {
            foreach(Collider col in RagdollColliders)
            {
                col.isTrigger = false;
            }
            foreach(Rigidbody rig in RagdollRigibodies)
            {
                rig.velocity = Vector3.zero;
                rig.angularVelocity = Vector3.zero;
                rig.isKinematic = false;
            }
        }
        else
        {
            foreach (Collider col in RagdollColliders)
            {
                col.isTrigger = true;
            }
            foreach (Rigidbody rig in RagdollRigibodies)
            {
                rig.isKinematic = true;
                rig.velocity = _rig.velocity;
            }
        }
    }

    public void TurnRagdollOff()
    {
        if (RagdollOnCoroutine != null)
        {
            StopCoroutine(RagdollOnCoroutine);
        }

        TurnRagdoll(false);

        _rig.useGravity = true;
        _rig.angularVelocity = Vector3.zero;
        _rig.velocity = Vector3.zero;
        transform.position = Main.transform.position;
        transform.forward = Main.transform.up;
        transform.rotation = new Quaternion(0, transform.rotation.y, 0, transform.rotation.w);

        Main.transform.localPosition = MainStartPos;
        Main.transform.localRotation = MainStartRot;
        _animator.Play(STAND_UP, 0, 0);
    }
    public void TurnRagdollOn()
    {
        TurnRagdoll(true);

        _rig.useGravity = false;
        _rig.angularVelocity = Vector3.zero;
        _rig.velocity = Vector3.zero;
        if (RagdollOnCoroutine != null)
        {
            StopCoroutine(RagdollOnCoroutine);
        }
        RagdollOnCoroutine = StartCoroutine(RagdollOnDelay());
    }

    private IEnumerator RagdollOnDelay()
    {
        yield return new WaitForSeconds(LieDelay);
        while(inObject)
        {
            yield return new WaitForFixedUpdate();
        }
        TurnRagdollOff();
        yield break;
    }

    public void AddImpulse(Vector3 Impulse)
    {
        if(isRagdolled)
        {

        }
        else
        {
            _rig.velocity += Impulse;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Cube")
        {
            TurnRagdollOn();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Cube")
        {
            inObject = false;
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Cube")
        {
            inObject = true;
        }
    }
    private void Start()
    {
        Init();
    }
}
public interface IMan
{
    Transform ManTransform { get; }

    Collider ManCollider { get; }

    Rigidbody ManRigidbody { get; }

    void AddImpulse(Vector3 Impulse);

    void TurnRagdollOff();
    void TurnRagdollOn();
}

