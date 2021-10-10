using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RayFire;

public class BreakableObject : MonoBehaviour, IBreakable
{
    [Header("Settings")]
    [Range(0.1f, 10)]
    [SerializeField] private float Mass = 1;
    [Range(3, 20)]
    [SerializeField] private int PartAmount = 10;
    private bool Destroyed;

    [Header("Components (Not necessary)")]
    [SerializeField] private Material InnerMaterial;
    [SerializeField] private Collider[] _colliders;
    [SerializeField] private Rigidbody _rig;
    [SerializeField] private RayfireRigid _rayfire;

    private void Init()
    {
        if (_rig == null) _rig = GetComponent<Rigidbody>();
        if (_rayfire == null) _rayfire = GetComponent<RayfireRigid>();
        if (_colliders == null) _colliders = GetComponents<Collider>();
        if(InnerMaterial != null) _rayfire.materials.innerMaterial = Instantiate(InnerMaterial);

        _rayfire.initialization = RayfireRigid.InitType.AtStart;
        _rayfire.simulationType = SimType.Kinematic;
        _rayfire.demolitionType = DemolitionType.Runtime;
        _rayfire.meshDemolition.amount = PartAmount;
        _rayfire.meshDemolition.meshInput = RFDemolitionMesh.MeshInputType.AtStart;

        _rayfire.physics.mass = Mass;
        _rayfire.physics.colliderType = RFColliderType.Sphere;
        _rayfire.physics.useGravity = true;
    }

    private void Start()
    {
        Init();
    }

    public void Demolish()
    {
        if (Destroyed)
            return;
        foreach(Collider col in _colliders)
        {
            col.enabled = false;
        }
        Destroyed = true;
        _rayfire.Demolish();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.tag == "Cube")
        {
            Demolish();
        }
    }
}

public interface IBreakable
{
    void Demolish();
}
