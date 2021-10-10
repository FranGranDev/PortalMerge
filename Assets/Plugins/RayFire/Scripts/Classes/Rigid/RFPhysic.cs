using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;


namespace RayFire
{
    /*[Serializable]
    public class RFImpulse
    {
        public bool previewLinear;
        public bool enableLinear;
        public Vector3 linearVelocity;
        public float linearVelocityVariation;
        public int angularVelocityDivergence;
        
        public bool enableAngular;
        public Vector3 angularVelocity;
        public int angularVelocityVariation;
    }*/
    
    [Serializable]
    public class RFPhysic
    {
        [Header("  Physic Material")]
        [Space(3)]
        
        [Tooltip("Material preset with predefined density, friction, elasticity and solidity. Can be edited in Rayfire Man component.")]
        public MaterialType materialType;
        
        [Space(2)]
        [Tooltip("Allows to define own Physic Material.")]
        public PhysicMaterial material;
        
        [Header("  Mass")]
        [Space(3)]
        
        public MassType massBy;
        [Space (2)]
        [Range(0.1f, 100f)] public float mass;
        
        [Header ("  Other")]
        [Space(3)]
        
        public RFColliderType colliderType;
        [Space (2)]
        public bool useGravity;
        
        [Header ("  Fragments")]
        [Space(3)]

        [Tooltip("Multiplier for demolished fragments velocity.")]
        [Range(0, 5f)] public float dampening;
        
        // Hidden
        [HideInInspector] public Rigidbody rigidBody;
        [HideInInspector] public Collider meshCollider;
        [HideInInspector] public List<Collider> clusterColliders;
         
        [NonSerialized] public bool rec;
        [NonSerialized] public bool exclude;
        [NonSerialized] public bool destructible;
        [NonSerialized] public int  solidity;
        
        [NonSerialized] public Quaternion rotation;
        [NonSerialized] public Vector3    position;
        [NonSerialized] public Vector3    velocity;
        
        [NonSerialized] public Vector3    initScale;
        [NonSerialized] public Vector3    initPosition;
        [NonSerialized] public Quaternion initRotation;

        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////

        // Constructor
        public RFPhysic()
        {
            materialType = MaterialType.Concrete;
            material     = null;
            massBy       = MassType.MaterialDensity;
            mass         = 1f;
            colliderType = RFColliderType.Mesh;
            useGravity   = true;
            dampening    = 0.8f;
            solidity          = 1;
            Reset();
            
            rotation     = Quaternion.identity;
            position     = Vector3.zero;
            velocity     = Vector3.zero;
            
            initScale    = Vector3.one;
            initPosition = Vector3.zero;
            initRotation = Quaternion.identity;
        }

        // Copy from
        public void CopyFrom(RFPhysic physics)
        {
            materialType       = physics.materialType;
            material           = physics.material;
            massBy             = physics.massBy;
            mass               = physics.mass;
            colliderType       = physics.colliderType;
            useGravity         = physics.useGravity;
            dampening          = physics.dampening;

            Reset();
        }
        
        // Reset
        public void Reset()
        {
            rec     = false;
            exclude      = false;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Simulation Type
        /// /////////////////////////////////////////////////////////
        
        // Set simulation type properties
        public static void SetSimulationType(RayfireRigid scr)
        {
            // Dynamic
            if (scr.simulationType == SimType.Dynamic)
                SetDynamic(scr);

            // Sleeping 
            else if (scr.simulationType == SimType.Sleeping)
                SetSleeping(scr);

            // Inactive
            else if (scr.simulationType == SimType.Inactive)
                SetInactive(scr);

            // Kinematic
            else if (scr.simulationType == SimType.Kinematic)
                SetKinematic(scr);
        }

        // Set as dynamic
        static void SetDynamic(RayfireRigid scr)
        {
            // Set dynamic rigid body properties
            scr.physics.rigidBody.isKinematic = false;
            scr.physics.rigidBody.useGravity = scr.physics.useGravity;
        }

        // Set as sleeping
        static void SetSleeping(RayfireRigid scr)
        {
            // Set dynamic rigid body properties
            scr.physics.rigidBody.isKinematic   = false;
            scr.physics.rigidBody.useGravity    = scr.physics.useGravity;
            scr.physics.rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
            // Set sleep
            scr.physics.rigidBody.Sleep();
        }

        // Set as inactive
        static void SetInactive(RayfireRigid scr)
        {
            scr.physics.rigidBody.isKinematic = false;
            scr.physics.rigidBody.useGravity = false;

            // Set sleep
            scr.physics.rigidBody.Sleep();
        }

        // Set as Kinematic
        static void SetKinematic(RayfireRigid scr)
        {
            scr.physics.rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            scr.physics.rigidBody.isKinematic = true;
            scr.physics.rigidBody.useGravity = scr.physics.useGravity;
        }

        /// /////////////////////////////////////////////////////////
        /// Rigid body
        /// /////////////////////////////////////////////////////////
        
        // Set density. After collider defined.
        public static void SetDensity(RayfireRigid scr)
        {
            // Do not set mass for kinematik
            if (scr.physics.rigidBody.isKinematic == true)
                return;
                
            // Default mass from inspector
            float m = scr.physics.mass;
            
            // Get mass by density
            if (scr.objectType == ObjectType.Mesh && scr.physics.massBy == MassType.MaterialDensity)
            {
                scr.physics.rigidBody.SetDensity(RayfireMan.inst.materialPresets.Density(scr.physics.materialType));
                m = scr.physics.rigidBody.mass;
            }

            // Sec cluster mass by shards
            else if (scr.objectType == ObjectType.ConnectedCluster || scr.objectType == ObjectType.NestedCluster)
            {
                // Collect main cluster shards
                m = 0.1f;
                float density = RayfireMan.inst.materialPresets.Density (scr.physics.materialType);
                for (int i = 0; i < scr.clusterDemolition.cluster.shards.Count; i++)
                    m += scr.clusterDemolition.cluster.shards[i].sz * density;
                
                // Collect minor cluster shards
                if (scr.objectType == ObjectType.NestedCluster)
                {
                    if (scr.clusterDemolition.cluster.HasChildClusters)
                        for (int c = 0; c < scr.clusterDemolition.cluster.childClusters.Count; c++)
                            m += scr.clusterDemolition.cluster.childClusters[c].bound.size.magnitude * density;
                }
            }
            
            // Check for minimum mass
            if (RayfireMan.inst.minimumMass > 0)
                if (m < RayfireMan.inst.minimumMass)
                    m = RayfireMan.inst.minimumMass;
            
            // Check for maximum mass
            if (RayfireMan.inst.maximumMass > 0)
                if (m > RayfireMan.inst.maximumMass)
                    m = RayfireMan.inst.maximumMass;
            
            // Update mass in inspector
            scr.physics.rigidBody.mass = m;
        }
        
        // Set drag properties
        public static void SetDrag(RayfireRigid scr)
        {
            scr.physics.rigidBody.drag        = (RayfireMan.inst.materialPresets.Drag(scr.physics.materialType));
            scr.physics.rigidBody.angularDrag = (RayfireMan.inst.materialPresets.AngularDrag(scr.physics.materialType));
        }

        // Set velocity
        public static void SetFragmentsVelocity (RayfireRigid scr)
        {
            // TODO different for clusters, get rigid bodys center of mass
            
            // Current velocity
            if (scr.meshDemolition.runtimeCaching.wasUsed == true && scr.meshDemolition.runtimeCaching.skipFirstDemolition == false)
            {
                for (int i = 0; i < scr.fragments.Count; i++)
                    if (scr.fragments[i] != null)
                        scr.fragments[i].physics.rigidBody.velocity = scr.physics.rigidBody.GetPointVelocity (scr.fragments[i].transForm.position) * scr.physics.dampening;
            }

            // Previous frame velocity
            else
            {
                Vector3 baseVelocity = scr.physics.velocity * scr.physics.dampening;
                for (int i = 0; i < scr.fragments.Count; i++)
                    if (scr.fragments[i].physics.rigidBody != null)
                        scr.fragments[i].physics.rigidBody.velocity = baseVelocity;
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Mesh Collider
        /// /////////////////////////////////////////////////////////
        
        // Set fragments collider
        public static void SetFragmentMeshCollider(RayfireRigid scr, Mesh mesh)
        {
            // Custom collider
            scr.physics.colliderType = scr.meshDemolition.properties.colliderType;
            if (scr.meshDemolition.properties.sizeFilter > 0)
                if (mesh.bounds.size.magnitude < scr.meshDemolition.properties.sizeFilter)
                    scr.physics.colliderType = RFColliderType.None;
            
            // Skip collider
            SetMeshCollider (scr, mesh);
        }
        
        // Set fragments collider
        public static void SetMeshCollider (RayfireRigid scr, Mesh mesh = null)
        {
            // Skip collider
            if (scr.physics.colliderType == RFColliderType.None)
                return;
            
            // Discard collider if just trigger
            if (scr.physics.meshCollider != null && scr.physics.meshCollider.isTrigger == true)
                scr.physics.meshCollider = null;

            // No collider. Add own // TODO set non convex shape for collider
            if (scr.physics.meshCollider == null)
            {
                // Mesh collider
                if (scr.physics.colliderType == RFColliderType.Mesh)
                {
                    // Add Mesh collider
                    MeshCollider mCol = scr.gameObject.AddComponent<MeshCollider>();
                    
                    // Set mesh
                    if (mesh != null)
                        mCol.sharedMesh = mesh;

                    // Set convex
                    if (scr.simulationType != SimType.Static)
                        mCol.convex = true;
                    scr.physics.meshCollider = mCol;
                }
                    
                // Box / Sphere collider
                else if (scr.physics.colliderType == RFColliderType.Box)
                    scr.physics.meshCollider = scr.gameObject.AddComponent<BoxCollider>();
                else if (scr.physics.colliderType == RFColliderType.Sphere)
                    scr.physics.meshCollider = scr.gameObject.AddComponent<SphereCollider>();
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Cluster Colliders
        /// /////////////////////////////////////////////////////////
        
        // Create mesh colliders for every input mesh TODO input cluster to control all nest roots for correct colliders
        public static bool SetClusterCollidersByShards (RayfireRigid scr)
        {
            // Check colliders list
            scr.physics.CollidersRemoveNull (scr);

            // Already clusterized
            if (scr.physics.HasClusterColliders == true)
                return true;
            
            // Colliders list
            if (scr.physics.clusterColliders == null)
                scr.physics.clusterColliders = new List<Collider>();
            
            // Connected/Nested colliders
            if (scr.objectType == ObjectType.ConnectedCluster)
                SetShardColliders (scr, scr.clusterDemolition.cluster);
            else if (scr.objectType == ObjectType.NestedCluster)
                SetDeepShardColliders (scr, scr.clusterDemolition.cluster);
            
            return true;
        }

        // Check children for mesh or cluster root until all children will not be checked
        static void SetShardColliders (RayfireRigid scr, RFCluster cluster)
        {
            MeshCollider meshCol;
            for (int i = 0; i < cluster.shards.Count; i++)
            {
                // Get mesh filter and collider TODO set collider by type
                meshCol = cluster.shards[i].tm.GetComponent<MeshCollider>();
                if (meshCol == null)
                {
                    MeshFilter mf      = cluster.shards[i].tm.GetComponent<MeshFilter>();
                    meshCol            = mf.gameObject.AddComponent<MeshCollider>();
                    meshCol.sharedMesh = mf.sharedMesh;
                }
                meshCol.convex = true;
                
                // Set shard collider and collect
                cluster.shards[i].col = meshCol;
                scr.physics.clusterColliders.Add (meshCol);
            }
        }
        
        // Check children for mesh or cluster root until all children will not be checked
        static void SetDeepShardColliders (RayfireRigid scr, RFCluster cluster)
        {
            // Set shard colliders
            SetShardColliders (scr, cluster);

            // Set child cluster colliders
            if (cluster.HasChildClusters == true)
                for (int i = 0; i < cluster.childClusters.Count; i++)
                    SetDeepShardColliders (scr, cluster.childClusters[i]);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Cluster Colliders
        /// /////////////////////////////////////////////////////////   
        
        // Set cluster colliders by shards
        public static void CollectClusterColliders (RayfireRigid scr, RFCluster cluster)
        {
            // Reset original cluster colliders list
            if (scr.physics.clusterColliders == null)
                scr.physics.clusterColliders = new List<Collider>();
            else
                scr.physics.clusterColliders.Clear();
            
            // Collect all shards colliders
            CollectDeepColliders (scr, cluster);
        }
        
        // Check children for mesh or cluster root until all children will not be checked
        static void CollectDeepColliders (RayfireRigid scr, RFCluster cluster)
        {
            // Collect shards colliders
            for (int i = 0; i < cluster.shards.Count; i++)
                scr.physics.clusterColliders.Add (cluster.shards[i].col);

            // Set child cluster colliders
            if (scr.objectType == ObjectType.NestedCluster)
                if (cluster.HasChildClusters == true)
                    for (int i = 0; i < cluster.childClusters.Count; i++)
                        CollectDeepColliders (scr, cluster.childClusters[i]);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Collider properties
        /// /////////////////////////////////////////////////////////       
         
        // Set collider material
        public static void SetColliderMaterial(RayfireRigid scr)
        {
            // Set physics material if not defined by user
            if (scr.physics.material == null)
                scr.physics.material = scr.physics.PhysMaterial;
            
            // Set mesh collider material
            if (scr.physics.meshCollider != null)
            {
                scr.physics.meshCollider.sharedMaterial = scr.physics.material;
                
                // Set debris material properties
                if (scr.HasDebris == true)
                    for (int i = 0; i < scr.debrisList.Count; i++)
                        scr.debrisList[i].collision.SetMaterialProps (scr.debrisList[i]);
                
                return;
            }
            
            // Set cluster colliders material
            if (scr.physics.HasClusterColliders == true)
                for (int i = 0; i < scr.physics.clusterColliders.Count; i++)
                    scr.physics.clusterColliders[i].sharedMaterial = scr.physics.material;
        }
        
        // Set collider convex state
        public static void SetColliderConvex(RayfireRigid scr)
        {
            if (scr.physics.meshCollider != null)
            {
                // Not Mesh collider
                if (scr.physics.meshCollider is MeshCollider == false)
                    return;
                
                // Turn on convex for non kinematik
                MeshCollider mCol = (MeshCollider)scr.physics.meshCollider;
                if (scr.physics.rigidBody.isKinematic == false)
                    mCol.convex = true;
            }
        }
        
        // EDITOR clear colliders
        public static void DestroyColliders(RayfireRigid scr)
        {
            if (scr.physics.HasClusterColliders == true)
                for (int i = scr.physics.clusterColliders.Count - 1; i >= 0; i--)
                    if (scr.physics.clusterColliders[i] != null)
                        Object.DestroyImmediate (scr.physics.clusterColliders[i], true);
            scr.physics.clusterColliders.Clear();
        }

        // Null check and remove
        public void CollidersRemoveNull(RayfireRigid scr)
        {
            if (scr.physics.HasClusterColliders == true)
                for (int i = scr.physics.clusterColliders.Count - 1; i >= 0; i--)
                    if (scr.physics.clusterColliders[i] == null)
                        scr.physics.clusterColliders.RemoveAt (i);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Coroutines
        /// /////////////////////////////////////////////////////////
        
        // Cache physics data for fragments 
        public IEnumerator PhysicsDataCor (RayfireRigid scr)
        {
            velocity = scr.physics.rigidBody.velocity;
            position = scr.transForm.position;
            rotation = scr.transForm.rotation;
            
            while (exclude == false)
            {
                if (scr.transForm.hasChanged == true)
                {
                    velocity = scr.physics.rigidBody.velocity;
                    position = scr.transForm.position;
                    rotation = scr.transForm.rotation;
                    scr.transForm.hasChanged = false;
                }

                yield return null;
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Getters
        /// /////////////////////////////////////////////////////////
        
        // Get Destructible state
        public bool HasClusterColliders
        {
            get
            {
                if (clusterColliders != null && clusterColliders.Count > 0)
                    return true;
                return false;
            }
        }
        
        // Get Destructible state
        public bool Destructible
        {
            get { return RayfireMan.inst.materialPresets.Destructible(materialType); }
        }

        // Get physic material
        public int Solidity
        {
            get { return RayfireMan.inst.materialPresets.Solidity(materialType); }
        }

        // Get physic material
        PhysicMaterial PhysMaterial
        {
            get
            {
                // Return predefine material
                if (material != null)
                    return material;

                // Crete new material
                return RFMaterialPresets.Material(materialType);
            }
        }
    }
}