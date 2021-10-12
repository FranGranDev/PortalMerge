using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RayFire
{
    [AddComponentMenu ("RayFire/Rayfire Bomb")]
    [HelpURL ("http://rayfirestudios.com/unity-online-help/unity-bomb-component/")]
    public class RayfireBomb : MonoBehaviour
    {
        // Activation Type
        public enum RangeType
        {
            Spherical = 0
        }

        // Strength fade Type
        public enum FadeType
        {
            Linear      = 0,
            Exponential = 1,
            None        = 2
        }

        // Projectiles class
        [Serializable]
        public class Projectile
        {
            public Vector3      positionPivot;
            public Vector3      positionClosest;
            public float        fade;
            public Rigidbody    rb;
            public RayfireRigid scrRigid;
            public Quaternion   rotation;
        }
        
        [Header ("  Range")]
        [Space (3)]
        
        public RangeType rangeType = RangeType.Spherical;
        [Space (1)]
        public FadeType fadeType = FadeType.Linear;
        [Space (1)]
        public float    range    = 5f;

        [Header ("  Impulse")]
        [Space (3)]
        
        [Range (0f, 10f)] public float strength = 1f;
        [Space (1)]
        [Range (0,  100)] public int  variation       = 50;
        [Space (1)]
        [Range (0,  90)]  public int  chaos           = 30;
        [Space (1)]
        public                   bool forceByMass     = true;
        [Space (1)]
        public                   bool affectKinematic = false;

        [Header ("  Detonation")]
        [Space (3)]
        
        public float heightOffset = 0f;
        [Space (1)]
        public float delay = 0f;

        [Header ("  Damage")]
        [Space (3)]
        
        public bool applyDamage = true;
        [Space (1)]
        public float damageValue = 1f;

        [Header ("  Audio")]
        [Space (3)]

        public bool play = false;
        [Space (1)]
        [Range (0.01f, 1f)] public float     volume = 1f;
        [Space (1)]
        public AudioClip clip;

        // Event
        public RFExplosionEvent explosionEvent = new RFExplosionEvent();
        
        // Hidden
        [HideInInspector] public Transform        transForm;
        [HideInInspector] public Vector3          bombPosition;
        [HideInInspector] public Vector3          explPosition;
        [HideInInspector] public Collider[]       colliders   = null;
        [HideInInspector] public List<Projectile> projectiles = new List<Projectile>();
        [HideInInspector] public List<Rigidbody>  rigidbodies = new List<Rigidbody>();
        [HideInInspector] public bool             showGizmo   = true;
        [HideInInspector] public int              mask        = -1;
        [HideInInspector] public string           tagFilter   = "Untagged";
        
        /// /////////////////////////////////////////////////////////
        /// Common
        /// /////////////////////////////////////////////////////////

        // Awake
        void Awake()
        {
            // Cache variables
            DefineComponents();

            // Clear
            ClearLists();
        }
        
        // Cache variables
        void DefineComponents()
        {
            // Cache transform
            transForm = GetComponent<Transform>();
        }

        // Copy properties from another Rigs
        public void CopyFrom (RayfireBomb scr)
        {
            rangeType       = scr.rangeType;
            fadeType        = scr.fadeType;
            range           = scr.range;
            strength        = scr.strength;
            variation       = scr.variation;
            chaos           = scr.chaos;
            forceByMass     = scr.forceByMass;
            affectKinematic = scr.affectKinematic;
            heightOffset    = scr.heightOffset;
            delay           = scr.delay;
            applyDamage     = scr.applyDamage;
            damageValue     = scr.damageValue;
            clip            = scr.clip;
            volume          = scr.volume;
        }

        /// /////////////////////////////////////////////////////////
        /// Explode
        /// /////////////////////////////////////////////////////////

        // Explode bomb
        public void Explode (float delayLoc)
        {
            if (delayLoc == 0)
                Explode();
            else if (delayLoc > 0)
                StartCoroutine (ExplodeCor());
        }

        // Init delay before explode
        IEnumerator ExplodeCor()
        {
            // Wait delay time
            yield return new WaitForSeconds (delay);

            // Explode
            Explode();
        }

        // Explode bomb
        void Explode()
        {
            // Set bomb and explosion positions
            SetPositions();

            // Setup collider, projectiles and rigidbodies
            if (Setup() == false)
                return;
            
            // Recollect projectiles if damage with demolition.
            if (SetRigidDamage() == true)
                if (Setup() == false)
                    return;

            // Apply explosion force
            SetForce();
            
            // Event
            explosionEvent.InvokeLocalEvent (this);
            RFExplosionEvent.InvokeGlobalEvent (this);

            // Explosion Sound
            PlayAudio();

            // Clear lists in runtime
            if (Application.isEditor == false)
                ClearLists();
        }

        // Explosion Sound
        void PlayAudio()
        {
            if (play == true && clip != null)
            {
                // Fix volume
                if (volume < 0)
                    volume = 1f;

                // TODO Set volume bu range

                // Play clip
                AudioSource.PlayClipAtPoint (clip, transform.position, volume);
            }
        }

        // Setup collider, projectiles and rigidbodies
        bool Setup()
        {
            // Clear all lists
            ClearLists();

            // Set colliders by range type
            SetColliders();

            // Set rigidbodies by colliders
            SetProjectiles();

            // Nothing to explode
            if (projectiles.Count == 0)
                return false;

            return true;
        }

        // Restore exploded objects transformation
        public void Restore()
        {
            foreach (Projectile projectile in projectiles)
            {
                if (projectile.rb != null)
                {
                    projectile.rb.velocity           = Vector3.zero;
                    projectile.rb.angularVelocity    = Vector3.zero;
                    projectile.rb.transform.position = projectile.positionPivot;
                    projectile.rb.transform.rotation = projectile.rotation;
                }
            }
        }

        // Reset all lists
        void ClearLists()
        {
            colliders = null;
            rigidbodies.Clear();
            projectiles.Clear();
        }

        /// /////////////////////////////////////////////////////////
        /// Setups
        /// /////////////////////////////////////////////////////////

        // Set bomb and explosion positions
        void SetPositions()
        {
            // Set initial bomb and explosion positions
            bombPosition = transform.position;
            explPosition = transform.position;

            // Use custom point as bomb position and explosion position
            //if (customPoint != null) {
            //    bombPosition = customPoint;
            //    explPosition = customPoint;
            //}

            // Consider height offset
            if (heightOffset != 0)
                explPosition.y = bombPosition.y + heightOffset;
        }

        // Set colliders by range type
        void SetColliders()
        {
            if (rangeType == RangeType.Spherical)
                colliders = Physics.OverlapSphere (bombPosition, range, mask);

            //else if (rangeType == RangeType.Cylindrical)
            //    colliders = Physics.OverlapSphere(bombPosition, range * 2, mask);
        }

        // Set projectiles by colliders
        void SetProjectiles()
        {
            projectiles.Clear();

            // Collect all rigid bodies in range
            foreach (Collider col in colliders)
            {
                // Tag filter
                if (tagFilter != "Untagged" && col.gameObject.CompareTag (tagFilter) == false)
                    continue;
                
                // Get attached rigid body
                Rigidbody rb = col.attachedRigidbody;

                // No rb
                if (rb == null)
                    continue;

                // Create projectile if rigid body new. Could be several colliders on one object.
                if (rigidbodies.Contains (rb) == false)
                {
                    Projectile projectile = new Projectile();
                    projectile.rb = rb;

                    // Transform
                    projectile.positionPivot = rb.transform.position;
                    projectile.rotation      = rb.transform.rotation;

                    // Get position of closest point to explosion position
                    projectile.positionClosest = col.bounds.ClosestPoint (transForm.position);
  
                    // Get fade multiplier by range and distance
                    projectile.fade = Fade (transForm.position, projectile.positionClosest);

                    // Check for Rigid script
                    projectile.scrRigid = projectile.rb.GetComponent<RayfireRigid>();

                    // Collect projectile
                    projectiles.Add (projectile);

                    // Remember rigid body
                    rigidbodies.Add (rb);
                }
            }

            // do not collect kinematic
            // collect rigid kinematik if can be activated
        }

        // Set RayFire Rigid refs for projectiles
        bool SetRigidDamage()
        {
            // Recollect state for new fragments after demolition
            bool recollectState = false;

            // Apply damage to rigid and demolish first
            if (applyDamage == true && damageValue > 0)
            {
                foreach (Projectile projectile in projectiles)
                {
                    // Rigid exist and damage enabled
                    if (projectile.scrRigid != null && projectile.scrRigid.damage.enable == true)
                    {
                        // Apply damage and demolish TODO set damage position to objects
                        if (projectile.scrRigid.ApplyDamage (damageValue * projectile.fade, transform.position) == true)
                            recollectState = true;
                    }
                }
            }

            return recollectState;
        }

        // Apply explosion force, vector and rotation to projectiles
        void SetForce()
        {
            // Set same random state
            Random.InitState (1);

            // Get str for each object by expl type with variation
            foreach (Projectile projectile in projectiles)
            {
                // Set forceMode by mass state
                ForceMode forceMode = ForceMode.Impulse;
                if (forceByMass == false)
                    forceMode = ForceMode.VelocityChange;

                // Get explosion vector from explosion position to projectile center of mass
                Vector3 vector = Vector (projectile);

                // Affect Kinematic
                SetKinematic (projectile);

                // Get local velocity strength
                float strVar  = strength * variation / 100f + strength;
                float str     = Random.Range (strength, strVar);
                float strMult = projectile.fade * str * 10f;

                // Apply force
                projectile.rb.AddForce (vector * strMult, forceMode);

                // Get local rotation strength 
                Vector3 rot = new Vector3 (Random.Range (-chaos, chaos), Random.Range (-chaos, chaos), Random.Range (-chaos, chaos));

                // Set rotation impulse
                projectile.rb.angularVelocity = rot;
            }
        }

        // Explode kinematik objects
        void SetKinematic (Projectile projectile)
        {
            if (affectKinematic == true && projectile.fade > 0 && projectile.rb.isKinematic == true)
            {
                // Convert kinematic to dynamic via rigid script
                if (projectile.scrRigid != null)
                {
                    projectile.scrRigid.Activate();
                }

                // Convert regular kinematik to dynamic
                else
                {
                    projectile.rb.isKinematic = false;

                    // TODO Set mass

                    // Set convex
                    MeshCollider meshCol = projectile.rb.gameObject.GetComponent<MeshCollider>();
                    if (meshCol != null && meshCol.convex == false)
                        meshCol.convex = true;
                }
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Support
        /// /////////////////////////////////////////////////////////

        // Fade multiplier
        float Fade (Vector3 bombPos, Vector3 fragPos)
        {
            // Get rate by fade type
            float fade = 1f;

            // Linear or Exponential fade
            if (fadeType == FadeType.Linear)
                fade = 1f - Vector3.Distance (bombPos, fragPos) / range;

            // Exponential fade
            else if (fadeType == FadeType.Exponential)
            {
                fade =  1f - Vector3.Distance (bombPos, fragPos) / range;
                fade *= fade;
            }

            // Cap fade
            if (fade < 0)
                fade = 0;

            return fade;
        }

        // Get explosion vector from explosion position to projectile center of mass
        Vector3 Vector (Projectile projectile)
        {
            Vector3 vector = Vector3.up;

            // Spherical range
            if (rangeType == RangeType.Spherical)
                vector = Vector3.Normalize (projectile.positionPivot - explPosition);

            // Cylindrical range
            //else if (rangeType == RangeType.Cylindrical)
            //{
            //    Vector3 lineDir = transForm.InverseTransformDirection(Vector3.up);
            //    lineDir = Vector3.up;
            //    lineDir.Normalize();
            //    var vec = projectile.positionPivot - explPosition;
            //    var dot = Vector3.Dot(vec, lineDir);
            //    Vector3 nearestPointOnLine = explPosition + lineDir * dot;
            //    vector = Vector3.Normalize(projectile.positionPivot - nearestPointOnLine);
            //}

            return vector;
        }
    }
}