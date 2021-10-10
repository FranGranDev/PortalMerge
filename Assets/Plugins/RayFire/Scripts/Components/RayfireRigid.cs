using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RayFire
{
    [SelectionBase]
    [DisallowMultipleComponent]
    [AddComponentMenu ("RayFire/Rayfire Rigid")]
    [HelpURL ("http://rayfirestudios.com/unity-online-help/unity-rigid-component/")]
    public class RayfireRigid : MonoBehaviour
    {
        public enum InitType
        {
            ByMethod = 0,
            AtStart  = 1
        }

        [Space (2)]
        public InitType initialization = InitType.ByMethod;

        [Header ("  Main")]
        [Space (3)]
        
        [Tooltip ("Defines behaviour of object during simulation.")]
        public SimType simulationType = SimType.Dynamic;
        [Space (2)]
        public ObjectType objectType = ObjectType.Mesh;
        [Space (2)]
        public DemolitionType demolitionType = DemolitionType.None;
        
        [Header ("  Simulation")]
        [Space (3)]
        
        public RFPhysic     physics    = new RFPhysic();
        [Space (2)]
        public RFActivation activation = new RFActivation();
        
        [Header ("  Demolition")]
        [Space (3)]
        
        public RFLimitations         limitations         = new RFLimitations();
        [Space (2)]
        public RFDemolitionMesh      meshDemolition      = new RFDemolitionMesh();
        [Space (2)]
        public RFDemolitionCluster   clusterDemolition   = new RFDemolitionCluster();
        [Space (2)]
        public RFReferenceDemolition referenceDemolition = new RFReferenceDemolition();
        [Space (2)]
        public RFSurface             materials           = new RFSurface();
        [Space (2)]
        public RFDamage              damage              = new RFDamage();
        
        [Header ("  Common")]
        [Space (3)]
        
        public RFFade                fading              = new RFFade();
        [Space (2)]
        public RFReset               reset               = new RFReset();

        [Header ("  Info")]

        /// /////////////////////////////////////////////////////////
        /// Hidden
        /// /////////////////////////////////////////////////////////
        
        [HideInInspector] public bool initialized;
        [HideInInspector] public Mesh[] meshes;
        [HideInInspector] public Vector3[] pivots;
        [HideInInspector] public RFMesh[] rfMeshes;
        [HideInInspector] public List<RFDictionary> subIds;
        [HideInInspector] public List<RayfireRigid> fragments;
        [HideInInspector] public Quaternion cacheRotation; // NOTE. Should be public, otherwise rotation error on demolition.

        [HideInInspector] public Transform transForm;
        [HideInInspector] public Transform rootChild;
        [HideInInspector] public Transform rootParent;
        
        [HideInInspector] public MeshFilter          meshFilter;
        [HideInInspector] public MeshRenderer        meshRenderer;
        [HideInInspector] public SkinnedMeshRenderer skinnedMeshRend;
        [HideInInspector] public List<RayfireDebris> debrisList;
        [HideInInspector] public List<RayfireDust>   dustList;
        [HideInInspector] public RayfireRestriction  restriction;
        [HideInInspector] public RayfireSound        sound;
        
        /// /////////////////////////////////////////////////////////
        /// Events
        /// /////////////////////////////////////////////////////////
        
        public RFDemolitionEvent demolitionEvent = new RFDemolitionEvent();
        public RFActivationEvent activationEvent = new RFActivationEvent();
        public RFRestrictionEvent restrictionEvent = new RFRestrictionEvent();

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////
        
        // Awake
        void Awake()
        {
            // Awake Mesh input
            if (objectType == ObjectType.Mesh && 
                demolitionType == DemolitionType.Runtime && 
                meshDemolition.meshInput == RFDemolitionMesh.MeshInputType.AtStart)
                MeshInput();
            
            // Initialize at start
            if (initialization == InitType.AtStart)
            {
                Initialize();
                
                //AwakeMethods();
                //StartMethods();
            }
        }

        // Activation
        void OnEnable()
        {
            if (gameObject.activeSelf == true && initialized == true)
            {
               // StopAllCoroutines();
               // StartAllCoroutines();
            }
        }

        // Awake ops
        void AwakeMethods()
        {
            // Create RayFire manager if not created
            RayfireMan.RayFireManInit();

            // Set components for mesh / skinned mesh / clusters
            SetComponentsBasic();
            
            // Set particles
            RFParticles.SetParticleComponents(this);
            
            // Init mesh root.
            if (SetRootMesh() == true)
                return;
            
            // Check for user mistakes
            RFLimitations.Checks(this);
            
            // Set components for mesh / skinned mesh / clusters
            SetComponentsPhysics();

            // Initialization Mesh input
            if (meshDemolition.meshInput == RFDemolitionMesh.MeshInputType.AtInitialization)
                MeshInput();
            
            // Precache meshes at awake
            AwakePrecache(); 
            
            // Prefragment object at awake
            AwakePrefragment();
        }
        
        // Start ops
        void StartMethods()
        {
            // Skinned mesh FIXME
            if (objectType == ObjectType.SkinnedMesh)
            {
                // Reset rigid data
                Default();

                // Check for demolition state every frame
                if (demolitionType != DemolitionType.None)
                    StartCoroutine (limitations.DemolishableCor(this));
                
                initialized = true;
            }

            // Excluded from simulation
            if (physics.exclude == true)
                return;
            
            // Set Start variables
            SetObjectType();
            
            // Start all coroutines
            StartAllCoroutines();
            
            // Object initialized
            initialized = true;
        }

        // Initialize 
        public void Initialize()
        {
            if (initialized == false)
            {
                AwakeMethods();
                StartMethods();
                
                // Init sound
                RFSound.InitializationSound(sound, limitations.bboxSize);
            }

            // TODO add reinit for already initialized objects in case of property change
            else
            {
               
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Awake ops
        /// /////////////////////////////////////////////////////////

        // Init mesh root. Copy Rigid component for children with mesh
        bool SetRootMesh()
        {
            if (objectType == ObjectType.MeshRoot)
            {
                // Stop if already initiated
                if (limitations.demolished == true || physics.exclude == true)
                    return true;
                
                // Get children
                List<Transform> children = new List<Transform>();
                for (int i = 0; i < transform.childCount; i++)
                    children.Add (transform.GetChild (i));
                
                // Add Rigid to child with mesh
                fragments = new List<RayfireRigid>();
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i].GetComponent<MeshFilter>() != null)
                    {
                        // Get rigid  // TODO check if fragment already has Rigid, Reinit in this case.
                        RayfireRigid childRigid = children[i].gameObject.GetComponent<RayfireRigid>();
                        if (childRigid == null)
                            childRigid = children[i].gameObject.AddComponent<RayfireRigid>();
                        fragments.Add (childRigid);
                        
                        // Copy parent properties
                        CopyPropertiesTo (childRigid);
                        
                        // Init
                        childRigid.Initialize();
                    }
                }

                // Copy components
                RayfireShatter.CopyRootMeshShatter (this, fragments);
                RFParticles.CopyRootMeshParticles(this, fragments);
                RFSound.CopyRootMeshSound(this, fragments);
                
                // TODO Setup as clusters root children with transform only

                // Check for Unyielding component
                RayfireUnyielding[] unyArray =  transform.GetComponents<RayfireUnyielding>();
                for (int i = 0; i < unyArray.Length; i++)
                    unyArray[i].SetUnyByOverlap(this);

                // Turn off demolition and physics
                demolitionType  = DemolitionType.None;
                physics.exclude = true;
                return true;
            }

            return false;
        }
        
        // Define basic components
        public void SetComponentsBasic()
        {
            // Set shatter component
            meshDemolition.scrShatter = meshDemolition.useShatter == true 
                ? GetComponent<RayfireShatter>() 
                : null;
            
            // Other
            transForm       = GetComponent<Transform>();
            meshFilter      = GetComponent<MeshFilter>();
            meshRenderer    = GetComponent<MeshRenderer>();
            skinnedMeshRend = GetComponent<SkinnedMeshRenderer>();
            restriction     = GetComponent<RayfireRestriction>();
            
            // Set sound
            sound = GetComponent<RayfireSound>();
            if (sound != null)
            {
                sound.rigid = this;
                sound.Initialize();
            }

            // Add missing mesh renderer
            if (meshFilter != null && meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();

            // Init reset lists
            if (reset.action == RFReset.PostDemolitionType.DeactivateToReset)
                limitations.descendants = new List<RayfireRigid>();
        }
        
        // Define components
        void SetComponentsPhysics()
        {
            // Excluded from simulation
            if (physics.exclude == true)
                return;
            
            // Physics components
            physics.rigidBody = GetComponent<Rigidbody>();
            physics.meshCollider = GetComponent<Collider>();

            // Mesh Set collider
            if (objectType == ObjectType.Mesh)
                RFPhysic.SetMeshCollider (this);

            // Cluster check
            if (objectType == ObjectType.NestedCluster || objectType == ObjectType.ConnectedCluster)
                Clusterize();
            
            // Rigid body
            if (simulationType != SimType.Static && physics.rigidBody == null)
            {
                physics.rigidBody = gameObject.AddComponent<Rigidbody>();
                physics.rigidBody.collisionDetectionMode = RayfireMan.inst.collisionDetection;
            }
        }

        // Clusterize
        void Clusterize()
        {
            // TODO skip if minor nested cluster
            if (objectType == ObjectType.NestedCluster)
                if (clusterDemolition.cluster.id > 1)
                    return;
            
            // Fail check
            if (RFDemolitionCluster.Clusterize (this) == true)
                return;
            
            // Fail
            physics.exclude = true;
            Debug.Log ("RayFire Rigid: " + name + " has no children with mesh. Object Excluded from simulation.", gameObject);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Start ops
        /// /////////////////////////////////////////////////////////
        
        // Set Start variables
        void SetObjectType ()
        {
            if (objectType == ObjectType.Mesh ||
                objectType == ObjectType.NestedCluster ||
                objectType == ObjectType.ConnectedCluster)
            {
                // Reset rigid data
                Default();

                // Set physics properties
                SetPhysics();
            }
        }
        
        // Reset rigid data
        public void Default()
        {
            // Reset
            limitations.Reset();
            meshDemolition.Reset();
            if (clusterDemolition != null)
                clusterDemolition.Reset();
            
            limitations.birthTime = Time.time + Random.Range (0f, 0.3f);

            // Birth position for activation check
            physics.initScale    = transForm.localScale;
            physics.initPosition = transForm.position;
            physics.initRotation = transForm.rotation;
            
            // Set bound and size
            RFLimitations.SetBound(this);
        }
        
        // Set physics properties
        void SetPhysics()
        {
            // Excluded from sim
            if (physics.exclude == true)
                return;
            
            // MeshCollider physic material preset. Set new or take from parent 
            RFPhysic.SetColliderMaterial (this);

            // Set physical simulation type. Important. Should after collider material define
            RFPhysic.SetSimulationType (this);

            // Do not set convex, mass, drag for static
            if (simulationType == SimType.Static)
                return;
            
            // Convex collider meshCollider. After SetSimulation Type to turn off convex for kinematic
            RFPhysic.SetColliderConvex (this);
            
            // Set density. After collider defined
            RFPhysic.SetDensity (this);

            // Set drag properties
            RFPhysic.SetDrag (this);

            // Set material solidity and destructible
            physics.solidity = physics.Solidity;
            physics.destructible = physics.Destructible;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Coroutines
        /// /////////////////////////////////////////////////////////
        
        // Start all coroutines
        public void StartAllCoroutines()
        {
            // Stop if static
            if (simulationType == SimType.Static)
                return;
            
            // Inactive
            if (gameObject.activeSelf == false)
                return;
            
            // Prevent physics cors
            if (physics.exclude == true)
                return;
            
            // Check for demolition state every frame
            if (demolitionType != DemolitionType.None)
                StartCoroutine (limitations.DemolishableCor(this));
           
            // Activation by velocity\offset coroutines
            if (simulationType == SimType.Inactive || simulationType == SimType.Kinematic)
            {
                // TODO skip not activatable uny objects
                // if (activation.unyielding == true && activation.activatable == fading)
   
                if (activation.byVelocity > 0)
                    StartCoroutine (activation.ActivationVelocityCor(this));
                if (activation.byOffset > 0)
                    //RayfireMan.inst.AddActivationOffset (this);
                    StartCoroutine (activation.ActivationOffsetCor(this));
            }
            
            // Init inactive every frame update coroutine
            if (simulationType == SimType.Inactive)
                //RayfireMan.inst.AddInactive (this);
                StartCoroutine (activation.InactiveCor(this));
            
            // Cache physics data for fragments 
            StartCoroutine (physics.PhysicsDataCor(this));

            // Init restriction check
            RayfireRestriction.InitRestriction (this);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Demolition types
        /// /////////////////////////////////////////////////////////
        
        // Awake Mesh input // TODO add checks in case has input mesh but mesh input is off
        public void MeshInput()
        {
            if (objectType == ObjectType.Mesh)
            {
                // Set components for mesh / skinned mesh / clusters
                SetComponentsBasic();
                
                // Timestamp
                //float t1 = Time.realtimeSinceStartup;
                
                // Input
                RFFragment.InputMesh (this);
                
                // Timestamp
                //float t2 = Time.realtimeSinceStartup;
                    
                //Debug.Log (gameObject.name +  " Input time: " + (t2 - t1));
            }
        }
        
        // Precache meshes at awake
        void AwakePrecache()
        {
            if (demolitionType == DemolitionType.AwakePrecache && objectType == ObjectType.Mesh)
                RFDemolitionMesh.CacheInstant(this);
        }
        
        // Predefine fragments
        void AwakePrefragment()
        {
            if (demolitionType == DemolitionType.AwakePrefragment && objectType == ObjectType.Mesh)
            {
                // Cache meshes
                RFDemolitionMesh.CacheInstant(this);

                // Predefine fragments
                Prefragment();
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Collision
        /// /////////////////////////////////////////////////////////

        // Collision check
        void OnCollisionEnter (Collision collision)
        {
            // TODO check if it is better to check state or collisions str
            
            // Demolish object check
            if (DemolitionState() == false) 
                return;
            
            // Check if collision demolition passed
            if (CollisionDemolition (collision) == true)
               limitations.demolitionShould = true;
        }
        
        // Check if collision demolition passed
        bool CollisionDemolition (Collision collision)
        {
            // Collision with kinematic object. Uses collision.impulse
            if (collision.rigidbody != null && collision.rigidbody.isKinematic == true)
            {
                if (collision.impulse.magnitude > physics.solidity * limitations.solidity * RayfireMan.inst.globalSolidity * 7f) // TODO fix
                {
                    limitations.contactPoint = collision.contacts[0];
                    limitations.contactVector3 = collision.contacts[0].point;
                    limitations.contactNormal = collision.contacts[0].normal;
                    return true;
                }
            }

            // Collision force checks. Uses relativeVelocity
            float collisionMagnitude = collision.relativeVelocity.magnitude;
            for (int i = 0; i < collision.contacts.Length; i++)
            {
                // Set contact point
                limitations.contactPoint = collision.contacts[i];
                limitations.contactVector3 = collision.contacts[i].point;
                limitations.contactNormal = collision.contacts[i].normal;
                
                // Demolish if collision high enough
                if (collisionMagnitude > physics.solidity * limitations.solidity * RayfireMan.inst.globalSolidity)
                    return true;
                
                // Collect damage by collision
                if (damage.enable == true && damage.collect == true)
                    if (ApplyDamage (collisionMagnitude * damage.multiplier, limitations.contactVector3) == true)
                        return true;
            }

            return false;
        }

        /// /////////////////////////////////////////////////////////
        /// Demolition
        /// /////////////////////////////////////////////////////////

         // Demolition available state
        public bool State ()
        {
            // Object already demolished
            if (limitations.demolished == true)
                return false;

            // Object already passed demolition state and demolishing is in progress
            if (meshDemolition.runtimeCaching.inProgress == true)
                return false;
            
            // Bad mesh check
            if (meshDemolition.badMesh > RayfireMan.inst.advancedDemolitionProperties.badMeshTry)
                return false;

            // Max amount check
            if (RayfireMan.MaxAmountCheck == false)
                return false;
            
            // Depth level check
            if (limitations.depth > 0 && limitations.currentDepth >= limitations.depth)
                return false;

            // Min Size check. Min Size should be considered and size is less than
            if (limitations.bboxSize < limitations.size)
                return false;
            
            // Safe frame
            if (Time.time - limitations.birthTime < limitations.time)
                return false;
            
            // Static objects can not be demolished
            if (simulationType == SimType.Static)
                return false;
            
            // Fading
            if (fading.state == 2)
                return false;

            return true;
        }
        
        // Check if object should be demolished
        public bool DemolitionState ()
        {
            // No demolition allowed
            if (demolitionType == DemolitionType.None)
                return false;
            
            // Non destructible material
            if (physics.destructible == false)
                return false;

            // Visibility check
            if (limitations.visible == true)
            {
                if (meshRenderer != null && meshRenderer.isVisible == false)
                    return false;
                if (skinnedMeshRend != null && skinnedMeshRend.isVisible == false)
                    return false;
            } 
            
            // Demolition available check
            if (State() == false)
                return false;

            // Per frame time check
            if (RayfireMan.inst.timeQuota > 0 && RayfireMan.inst.maxTimeThisFrame > RayfireMan.inst.timeQuota)
                return false;

            return true;
        }
        
        // Demolish object
        public void Demolish()
        {
            // Profiler.BeginSample ("Demolition");
            // Debug.Log (limitations.demolitionShould);
            
            // Initialize if not
            if (initialized == false)
                Initialize();

            // Timestamp
            float t1 = Time.realtimeSinceStartup;
            
            // Restore position and rotation to prevent high collision offset
            transForm.position = physics.position;
            transForm.rotation = physics.rotation;

            // Demolish mesh or cluster to reference
            if (RFReferenceDemolition.DemolishReference(this) == false)
                return;

            // Demolish mesh and create fragments. Stop if runtime caching or no meshes/fragments were created
            if (RFDemolitionMesh.DemolishMesh(this) == false)
                return;
            
            /* EXPERIMENTAL
            // TODO Clusterize
            bool clusterize = true;
            if (clusterize == true && objectType == ObjectType.Mesh && demolitionType == DemolitionType.Runtime)
            {

                foreach (var frag in fragments)
                {
                    Destroy (frag.physics.rigidBody);
                    Destroy (frag);
                }
                
                RayfireRigid scr = this.rootChild.gameObject.AddComponent<RayfireRigid>();
                this.CopyPropertiesTo (scr);
                scr.demolitionType = DemolitionType.Runtime;
                scr.objectType     = ObjectType.ConnectedCluster;
                
                scr.limitations.contactPoint   = this.limitations.contactPoint;
                scr.limitations.contactNormal  = this.limitations.contactNormal;
                scr.limitations.contactVector3 = this.limitations.contactVector3;

                scr.physics.velocity = this.physics.velocity;
                
                scr.clusterDemolition.cluster  = new RFCluster();
                scr.Initialize();
                
                scr.physics.rigidBody.velocity   = this.physics.velocity;
                scr.limitations.demolitionShould = true;
                //scr.Demolish();
                RayfireMan.DestroyFragment (this, rootParent);
                return;
            }
            */
            
            
            // Demolish cluster to children nodes 
            if (RFDemolitionCluster.DemolishCluster(this) == true)
                return;

            // Check fragments and proceed TODO separate flow for connected cls demolition
            if (limitations.demolished == false)
            {
                limitations.demolitionShould = false;
                demolitionType = DemolitionType.None;
                return;
            }

            // Connectivity check
            activation.CheckConnectivity();
            
            // Fragments initialisation
            InitMeshFragments();
            
            // Sum total demolition time
            RayfireMan.inst.maxTimeThisFrame += Time.realtimeSinceStartup - t1;
            
            // Init particles
            RFParticles.InitDemolitionParticles(this);

            // Init sound
            RFSound.DemolitionSound(sound, limitations.bboxSize);

            // Event
            demolitionEvent.InvokeLocalEvent (this);
            RFDemolitionEvent.InvokeGlobalEvent (this);
            
            // Destroy demolished object
            RayfireMan.DestroyFragment (this, rootParent);
            
            // Timestamp
            // float t2 = Time.realtimeSinceStartup;
            // Debug.Log (t2 - t1);
            // Profiler.EndSample();
        }
        
        /// /////////////////////////////////////////////////////////
        /// Fragments
        /// /////////////////////////////////////////////////////////
        
        // Copy rigid properties from parent to fragments
        public void CopyPropertiesTo (RayfireRigid toScr)
        {
            // Object type
            toScr.objectType = objectType;
            if (objectType == ObjectType.MeshRoot || objectType == ObjectType.SkinnedMesh)
                toScr.objectType = ObjectType.Mesh;

            // Sim type
            toScr.simulationType = simulationType;
            if (objectType != ObjectType.MeshRoot)
                if (simulationType == SimType.Static)
                    toScr.simulationType = SimType.Dynamic;

            // Demolition type
            toScr.demolitionType = demolitionType;
            if (objectType != ObjectType.MeshRoot)
                if (demolitionType != DemolitionType.None)
                    toScr.demolitionType = DemolitionType.Runtime;
            if (demolitionType == DemolitionType.ReferenceDemolition)
                toScr.demolitionType = DemolitionType.None;
            
            // Copy physics
            toScr.physics.CopyFrom (physics);
            if (objectType != ObjectType.MeshRoot)
                if (simulationType == SimType.Sleeping || simulationType == SimType.Kinematic)
                    toScr.simulationType = SimType.Dynamic;
                
            toScr.activation.CopyFrom (activation);
            toScr.limitations.CopyFrom (limitations);
            toScr.meshDemolition.CopyFrom (meshDemolition);
            toScr.clusterDemolition.CopyFrom (clusterDemolition);

            // Copy reference demolition props
            if (objectType == ObjectType.MeshRoot)
                toScr.referenceDemolition.CopyFrom (referenceDemolition);
            
            toScr.materials.CopyFrom (materials);
            toScr.damage.CopyFrom (damage);
            toScr.fading.CopyFrom (fading);
            toScr.reset.CopyFrom (this);
            
            // Copy restriction
            if (restriction != null)
            {
                toScr.restriction = toScr.gameObject.AddComponent<RayfireRestriction>();
                toScr.restriction.CopyFrom (restriction);
            }
        }
        
        // Fragments initialisation
        public void InitMeshFragments()
        {
            // No fragments
            if (HasFragments == false)
                return;
            
            // Set velocity
            RFPhysic.SetFragmentsVelocity (this);
            
            // Sum total new fragments amount
            RayfireMan.inst.advancedDemolitionProperties.currentAmount += fragments.Count;
            
            // Set ancestor and descendants 
            if (reset.mesh == RFReset.MeshResetType.ReuseInputMesh)
            {
                RFLimitations.SetAncestor (this);
                RFLimitations.SetDescendants (this);
            }

            // Fading. move to fragment
            if (fading.onDemolition == true)
                fading.DemolitionFade (fragments);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Manual methods
        /// /////////////////////////////////////////////////////////
        
        // Predefine fragments
        void Prefragment()
        {
            // Delete existing
            DeleteFragments();

            // Create fragments from cache
            fragments = RFDemolitionMesh.CreateFragments(this);
                
            // Stop
            if (HasFragments == false)
            {
                demolitionType = DemolitionType.None;
                return;
            }
            
            // Set physics properties
            for (int i = 0; i < fragments.Count; i++)
            {
                fragments[i].SetComponentsBasic();
                //fragments[i].SetParticleComponents();
                fragments[i].SetComponentsPhysics();
                fragments[i].SetObjectType();
            }
            
            // Deactivate fragments root
            if (rootChild != null)
                rootChild.gameObject.SetActive (false);
        }

        // Clear cache info
        public void DeleteCache()
        {
            meshes   = null;
            pivots   = null;
            rfMeshes = null;
            subIds   = new List<RFDictionary>();
        }
        
        // Delete fragments
        public void DeleteFragments()
        {
            // Destroy root
            if (rootChild != null)
            {
                if (Application.isPlaying == true)
                    Destroy (rootChild.gameObject);
                else
                    DestroyImmediate (rootChild.gameObject);

                // Clear ref
                rootChild = null;
            }

            // Clear array
            fragments = null;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Blade
        /// /////////////////////////////////////////////////////////

        // Add new slice plane
        public void AddSlicePlane (Vector3[] slicePlane)
        {
            // Not even amount of slice data
            if (slicePlane.Length % 2 == 1)
                return;

            // Add slice plane data
            limitations.slicePlanes.AddRange (slicePlane);
        }
        
        // Slice object
        public void Slice()
        {
            if (objectType == ObjectType.Mesh || objectType == ObjectType.SkinnedMesh)
                RFDemolitionMesh.SliceMesh(this);
            else if (objectType == ObjectType.ConnectedCluster)
                RFDemolitionCluster.SliceConnectedCluster (this);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Caching
        /// /////////////////////////////////////////////////////////
        
        // Caching into meshes over several frames
        public void CacheFrames()
        {
            StartCoroutine (meshDemolition.RuntimeCachingCor(this));
        }

        /// /////////////////////////////////////////////////////////
        /// Public methods
        /// /////////////////////////////////////////////////////////

        // Apply damage
        public bool ApplyDamage (float damageValue, Vector3 damagePoint, float damageRadius = 0f)
        {
            return RFDamage.ApplyDamage (this, damageValue, damagePoint, damageRadius);
        }
        
        // Activate inactive object
        public void Activate()
        {
            RFActivation.Activate (this);
        }
        
        // Fade this object
        public void Fade()
        {
            RFFade.Fade (this);
        }
        
        // Reset object
        public void ResetRigid()
        {
            RFReset.ResetRigid (this);
        }

        /// /////////////////////////////////////////////////////////
        /// Other
        /// /////////////////////////////////////////////////////////
        
        // Destroy
        public void DestroyCollider(Collider col) { Destroy (col); }
        public void DestroyObject(GameObject go) { Destroy (go); }
        public void DestroyRigid(RayfireRigid rigid) { Destroy (rigid); }
        public void DestroyRb(Rigidbody rb) { Destroy (rb); }

        /// /////////////////////////////////////////////////////////
        /// Getters
        /// /////////////////////////////////////////////////////////
        
        // Fragments/Meshes/RFMeshes check
        public bool HasFragments { get { return fragments != null && fragments.Count > 0; } }
        public bool HasMeshes { get { return meshes != null && meshes.Length > 0; } }
        public bool HasRfMeshes { get { return rfMeshes != null && rfMeshes.Length > 0; } }
        public bool HasDebris { get { return debrisList != null && debrisList.Count > 0; } }
        public bool HasDust { get { return dustList != null && dustList.Count > 0; } }
    }
}


// Explosivness. Slightly explodes fragments on demolition 
// Activation by continuity by weight
// Unyielding range
// man/awake amount diff because contact bias
// separate slice half, input for frag next frame
// awake cache slower at first demolition, faster if reused. check diff between precache and first demolition, move in awake expensive ops
// Peeling or surface fragmentation (custom point cloud), + not activated
// Replace Uhyielding component with Physic component to change any property

