using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Reinit at script change, creates second Manager on Manual prefragment

namespace RayFire
{
    [DisallowMultipleComponent]
    [AddComponentMenu ("RayFire/Rayfire Man")]
    [HelpURL ("http://rayfirestudios.com/unity-online-help/unity-man-component/")]
    public class RayfireMan : MonoBehaviour
    {
        public static RayfireMan inst;
        
        [Header ("  Gravity")]
        [Space (3)]

        public bool setGravity = false;
        [Space (2)]
        [Range (0f, 1f)] public float multiplier = 1f;

        [Header ("  Physics")]
        [Space (3)]

        public CollisionDetectionMode collisionDetection = CollisionDetectionMode.ContinuousDynamic;
        
        [Header ("  Materials")]
        [Space (3)]

        [Range (0f, 1f)] public float minimumMass = 0.1f;
        [Space (2)]
        [Range (0f, 400f)] public float maximumMass = 400f;
        [Space (2)]
        public RFMaterialPresets materialPresets = new RFMaterialPresets();

        [Header ("  Demolition")]
        [Space (3)]

        [Range (0f, 5f)] public float globalSolidity = 1f;
        [Space (2)]
        [Tooltip ("Maximum time in milliseconds per frame allowed to be used for demolitions. Off if 0.")]
        [Range (0f, 0.1f)] public float timeQuota = 0.033f;
        [Space (2)]
        public RFManDemolition advancedDemolitionProperties = new RFManDemolition();

        [Header ("  Pooling")]
        [Space (3)]

        public RFPoolingFragment fragments = new RFPoolingFragment();
        [Space (2)]
        public RFPoolingParticles particles = new RFPoolingParticles();

        [Header ("  About")]
        [Space (3)]
        
        public static int buildMajor = 1;
        public static int buildMinor = 25;

        // Hidden
        [HideInInspector] public Transform transForm;
        [HideInInspector] public List<string> layers;
        [HideInInspector] public float maxTimeThisFrame = 0f;
        
        /// /////////////////////////////////////////////////////////
        /// Common
        /// /////////////////////////////////////////////////////////

        // Awake
        void Awake()
        {
            // Set static instance
            SetInstance();
        }

        // Update
        void LateUpdate()
        {
            maxTimeThisFrame = 0f;
        }

        // Set instance
        void SetInstance()
        {
            // Set new static instance
            if (inst == null)
            {
                inst = this;
            }

            // Static instance not defined
            if (inst != null)
            {
                // Instance is this mono
                if (inst == this)
                {
                    // Set vars
                    SetVariables();

                    // Start pooling objects for fragments
                    StartPooling();
                }

                // Instance is not this mono
                if (inst != this)
                {
                    Destroy (gameObject);
                }
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////

        // Set vars
        void SetVariables()
        {
            // Get components
            transForm = GetComponent<Transform>();

            // Reset amount
            advancedDemolitionProperties.currentAmount = 0;

            // Set gravity
            SetGravity();

            // Set Physic Materials if needed
            materialPresets.SetMaterials();

            // Set all layers and tags
            SetLayers();
        }

        // Set gravity
        void SetGravity()
        {
            if (setGravity == true)
            {
                Physics.gravity = -9.81f * multiplier * Vector3.up;
            }
        }

        // Create RayFire manager if not created
        public static void RayFireManInit()
        {
            if (inst == null)
            {
                GameObject rfMan = new GameObject ("RayFireMan");
                inst = rfMan.AddComponent<RayfireMan>();
            }

            EditorCreate();
        }

        // Set instance ops for editor creation
        static void EditorCreate()
        {
            if (Application.isPlaying == false)
            {
                inst.SetInstance();
            }
        }

        // Set list of layers and tags
        void SetLayers()
        {
            // Set layers list
            layers = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName (i);
                if (layerName.Length > 0)
                    layers.Add (layerName);
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Pooling
        /// /////////////////////////////////////////////////////////

        // Enable objects pooling for fragments                
        void StartPooling()
        {
            // Create pool root
            fragments.CreatePoolRoot (transform);

            // Create pool instance
            fragments.CreateInstance (transform);

            // Pooling. Mot in editor
            if (Application.isPlaying == true && fragments.enable == true)
                StartCoroutine (fragments.StartPoolingCor (transForm));

            // Create pool root
            particles.CreatePoolRoot (transform);

            // Create pool instance
            particles.CreateInstance (transform);

            // Pooling. Mot in editor
            if (Application.isPlaying == true && particles.enable == true)
                StartCoroutine (particles.StartPoolingCor (transForm));
        }

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////

        // Check if fragment is the last child in root and delete root as well
        public static void DestroyFragment (RayfireRigid scr, Transform tm, float time = 0f)
        {
            // Decrement total amount.
            if (Application.isPlaying == true)
                inst.advancedDemolitionProperties.currentAmount--;

            // Deactivate
            scr.gameObject.SetActive (false);

            // Destroy mesh
            if (scr.reset.action == RFReset.PostDemolitionType.DestroyWithDelay)
            {
                DestroyOp (scr, tm, time);
            }

            // Destroy not connected child clusters in any case
            else if (scr.reset.action == RFReset.PostDemolitionType.DeactivateToReset)
                if (scr.objectType == ObjectType.ConnectedCluster && scr.clusterDemolition.cluster.id > 1)
                    DestroyOp (scr, tm, time);
        }

        // Check if fragment is the last child in root and delete root as well
        public static void DestroyGo (GameObject go)
        {
            Destroy (go);
        }

        // Check if fragment is the last child in root and delete root as well
        static void DestroyOp (RayfireRigid scr, Transform tm, float time = 0f)
        {
            // Set delay
            if (time == 0)
                time = scr.reset.destroyDelay;
            
            // Object is going to be destroyed. Timer is on
            scr.reset.toBeDestroyed = true;

            // Destroy object
            if (time <= 0f)
                Destroy (scr.gameObject);
            else
                Destroy (scr.gameObject, time);

            // Destroy root
            if (tm != null && tm.childCount == 0)
                Destroy (tm.gameObject);
        }

        // Max fragments amount check
        public static bool MaxAmountCheck
        {
            get { return inst.advancedDemolitionProperties.currentAmount < inst.advancedDemolitionProperties.maximumAmount; }
        }

        /// /////////////////////////////////////////////////////////
        /// Inactive
        /// /////////////////////////////////////////////////////////    
        
        List<RayfireRigid> activationOffset = new List<RayfireRigid>();
        List<RayfireRigid> inactive = new List<RayfireRigid>();
        //static Vector3 zero = Vector3.zero;
        
        public void AddInactive(RayfireRigid scr)
        {
            int amount = inactive.Count;
            inactive.Add (scr);
            if (amount == 0)
                StartCoroutine (InactiveCor());
        }
        
        // Exclude from simulation, move under ground, destroy
        IEnumerator InactiveCor ()
        {
            while (inactive.Count > 0)
            {
                for (int i = inactive.Count - 1; i >= 0; i--)
                {
                    if (inactive[i].simulationType == SimType.Inactive)
                    {
                        //Debug.Log (inactive[i].name);
                        // if (inactive[i].transForm.hasChanged == true)
                        {
                            inactive[i].physics.rigidBody.velocity        = Vector3.zero;
                            inactive[i].physics.rigidBody.angularVelocity = Vector3.zero;
                        }
                    }
                    else
                    {
                        inactive.RemoveAt (i);
                    }
                }
                yield return null;
            }
        }

        public void AddActivationOffset(RayfireRigid scr)
        {
            int amount = activationOffset.Count;
            activationOffset.Add (scr);
            if (amount == 0)
                StartCoroutine (ActivationOffsetCor());
        }
        
        // Check offset for activation
        IEnumerator ActivationOffsetCor ()
        {
            while (activationOffset.Count > 0)
            {
                for (int i = activationOffset.Count - 1; i >= 0; i--)
                {
                    if (activationOffset[i].activation.activated == false)
                    {
                        // if (inactive[i].transForm.hasChanged == true)
                        {
                            if (Vector3.Distance (activationOffset[i].transForm.position, 
                                activationOffset[i].physics.initPosition) > activationOffset[i].activation.byOffset)
                                activationOffset[i].Activate();
                        }
                    }
                    else
                    {
                        activationOffset.RemoveAt (i);
                    }
                }
                yield return null;
            }
        }
    }
}

