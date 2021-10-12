using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RayFire
{ 
    [Serializable]
    public class RFActivation
    {
        [Header ("  Activation")]
        [Space (3)]
        
        [Tooltip("Inactive object will be activated when it's velocity will be higher than By Velocity value when pushed by other dynamic objects.")]
        public float byVelocity;
        
        [Space(1)]
        [Tooltip("Inactive object will be activated if will be pushed from it's original position farther than By Offset value.")]
        public float byOffset;

        [Space(1)]
        [Tooltip("Inactive object will be activated if will get total damage higher than this value.")]
        public float byDamage;
        
        [Space(1)]
        [Tooltip("Inactive object will be activated by overlapping with object with RayFire Activator component.")]
        public bool byActivator;
        
        [Space(1)]
        [Tooltip("Inactive object will be activated when it will be shot by RayFireGun component.")]
        public bool byImpact;
        
        [Space(1)]
        [Tooltip("Inactive object will be activated by Connectivity component if it will not be connected with Unyielding zone.")] 
        public bool byConnectivity;
       
        [Header("  Connectivity")]
        [Space(3)]

        public bool unyielding;    
        [Space (1)]
        public bool activatable; 
        
        // Hidden
        [NonSerialized] public bool activated; 
        [NonSerialized] public RayfireConnectivity connect;
          
        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////

        // Constructor
        public RFActivation()
        {
            byVelocity     = 0f;
            byOffset       = 0f;
            byDamage       = 0f;
            byActivator    = false;
            byImpact       = false;
            byConnectivity = false;
            unyielding     = false;
            activatable    = false;
            activated      = false;
            Reset();
        }
        
        // Copy from
        public void CopyFrom(RFActivation act)
        {
            byActivator    = act.byActivator;
            byImpact       = act.byImpact;
            byVelocity     = act.byVelocity;
            byOffset       = act.byOffset;
            byDamage       = act.byDamage;
            byConnectivity = act.byConnectivity;
            unyielding     = act.unyielding;
            activatable    = act.activatable;
        }

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////

        // Turn of all activation properties
        public void Reset()
        {
            activated = false;
        }
        
        // Connectivity check
        public void CheckConnectivity()
        {
            if (byConnectivity == true && connect != null)
            {
                connect.checkNeed = true;
                connect = null;
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Coroutines
        /// /////////////////////////////////////////////////////////
        
        // Check velocity for activation
        public IEnumerator ActivationVelocityCor (RayfireRigid scr)
        {
            while (scr.activation.activated == false && scr.activation.byVelocity > 0)
            {
                if (scr.physics.rigidBody.velocity.magnitude > byVelocity)
                    scr.Activate();
                yield return null;
            }
        }

        // Check offset for activation
        public IEnumerator ActivationOffsetCor (RayfireRigid scr)
        {
            while (scr.activation.activated == false && scr.activation.byOffset > 0)
            {
                if (Vector3.Distance (scr.transForm.position, scr.physics.initPosition) > scr.activation.byOffset)
                    scr.Activate();
                yield return null;
            }
        }
        
        // Exclude from simulation, move under ground, destroy
        public IEnumerator InactiveCor (RayfireRigid scr)
        {
            //scr.transForm.hasChanged = false;
            while (scr.simulationType == SimType.Inactive)
            {
                //if (scr.transForm.hasChanged == true)
                {
                    scr.physics.rigidBody.velocity        = Vector3.zero;
                    scr.physics.rigidBody.angularVelocity = Vector3.zero;
                }
                yield return null;
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Static
        /// /////////////////////////////////////////////////////////
                
        // Activate inactive object
        public static void Activate (RayfireRigid scr)
        {
            // Stop if excluded
            if (scr.physics.exclude == true)
                return;

            // Skip not activatable unyielding objects
            if (scr.activation.activatable == false && scr.activation.unyielding == true)
                return;

            // Initialize if not
            if (scr.initialized == false)
                scr.Initialize();
            
            // Turn convex if kinematik activation
            if (scr.simulationType == SimType.Kinematic)
            {
                MeshCollider meshCollider = scr.physics.meshCollider as MeshCollider;
                if (meshCollider != null)
                    meshCollider.convex = true;

                // Swap with animated object
                if (scr.physics.rec == true)
                {
                    // Set dynamic before copy
                    scr.simulationType = SimType.Dynamic;
                    scr.physics.rigidBody.isKinematic = false;
                    scr.physics.rigidBody.useGravity  = scr.physics.useGravity;
                    
                    // Create copy
                    GameObject inst = UnityEngine.Object.Instantiate (scr.gameObject);
                    inst.transform.position = scr.transForm.position;
                    inst.transform.rotation = scr.transForm.rotation;

                    // Save velocity
                    Rigidbody rBody = inst.GetComponent<Rigidbody>();
                    if (rBody != null)
                    { 
                        rBody.velocity = scr.physics.rigidBody.velocity;
                        rBody.angularVelocity = scr.physics.rigidBody.angularVelocity; 
                    }
                    
                    // Activate and init rigid
                    scr.gameObject.SetActive (false);
                }
            }

            // Connectivity check
            scr.activation.CheckConnectivity();

            // Set state
            scr.activation.activated = true;
            
            // Set props
            scr.simulationType = SimType.Dynamic;
            scr.physics.rigidBody.isKinematic = false;
            scr.physics.rigidBody.useGravity = scr.physics.useGravity;
            
            // Fade on activation
            if (scr.fading.onActivation == true)
            {
                // Size check
                if (scr.fading.sizeFilter > 0 && scr.fading.sizeFilter > scr.limitations.bboxSize)
                    scr.Fade();
                else
                    scr.Fade();
            }
            
            // Init particles on activation
            RFParticles.InitActivationParticles(scr);

            // Init sound
            RFSound.ActivationSound(scr.sound, scr.limitations.bboxSize);
            
            // Event
            scr.activationEvent.InvokeLocalEvent (scr);
            RFActivationEvent.InvokeGlobalEvent (scr);

            // Add initial rotation if still TODO put in ui
            if (scr.physics.rigidBody.angularVelocity == Vector3.zero)
            {
                float val = 1.0f;
                scr.physics.rigidBody.angularVelocity = new Vector3 (
                    Random.Range (-val, val), Random.Range (-val, val), Random.Range (-val, val));
            }
        }
        
        // Get meshes and create colliders 
        public static void SetUnyielding (RayfireRigid scr)
        {
            if (scr.simulationType == SimType.Inactive || scr.simulationType == SimType.Kinematic)
            {
                RayfireUnyielding[] unyArray =  scr.GetComponents<RayfireUnyielding>();
                for (int i = 0; i < unyArray.Length; i++)
                    unyArray[i].SetUnyByOverlap(scr);
            }
        }
    }
}