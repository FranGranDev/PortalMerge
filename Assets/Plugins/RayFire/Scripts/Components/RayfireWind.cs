using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RayFire
{
    [AddComponentMenu ("RayFire/Rayfire Wind")]
    [HelpURL ("http://rayfirestudios.com/unity-online-help/unity-wind-component/")]
    public class RayfireWind : MonoBehaviour
    {
        // Direction Type
        public enum DirType
        {
            Directional = 0,
            Point       = 1
        }
        
        Transform transForm;
        Collider[] colliders = null;
        Vector3    halfExtents;
        Vector3    center;
        float      offset;

        //float halfWidth;
        //float halfLength;
        //Vector3 windDirection;


        [Header ("  Gizmo")]
        public Vector3 gizmoSize = new Vector3 (30f, 2f, 50f);
        [HideInInspector] public bool showGizmo = true;

        [Header ("  Noise")]
        [Range (1f, 100f)]
        public float globalScale = 10f;
        [Range (1f, 300f)]
        public float lengthScale = 100;
        [Range (1f, 300f)]
        public float widthScale = 100;
        [Range (-200, 200)]
        public float speed = 15;
        [HideInInspector] public bool showNoise = false;

        [Header ("  Strength")]
        [Range (-5, 5)]
        public float minimum = 0f;
        [Range (-5, 5)]
        public float maximum = 1f;
        [Range (0f, 10f)]
        public float torque = 2f;
        public bool forceByMass = true;
        
        [Header ("  Direction")]
        [Range (0f, 180)]
        public float divergency = 120f;
        [Range (0.01f, 2)]
        public float turbulence = 0.5f;


        [Header ("  Preview")]
        [Range (0.5f, 5f)]
        public float previewDensity = 1f;
        [Range (0.1f, 5f)]
        public float previewSize = 1f;

        [Header ("  Affected Rigid Bodies")]
        [HideInInspector] public List<Rigidbody> rigidbodies = new List<Rigidbody>();
        [HideInInspector] public int mask = -1;
        [HideInInspector] public string tagFilter = "Untagged";

        /// /////////////////////////////////////////////////////////
        /// Common
        /// /////////////////////////////////////////////////////////

        // Awake
        void Awake()
        {
            // Cache variables
            DefineComponents();
        }

        // Cache variables
        void DefineComponents()
        {
            // Cache transform
            transForm = GetComponent<Transform>();

            // Set base length
            colliders = new Collider[10];
        }

        // Main wind force coroutine
        IEnumerator WindForceCor()
        {
            while (enabled == true)
            {
                // Get all colliders inside gizmo
                SetColliders();

                // Set rigid bodies by colliders
                SetRigidBodies();

                // Set force to rigid bodies
                SetForce();

                yield return new WaitForSeconds (0.05f);
            }
        }

        // Enabling
        void OnEnable()
        {
            // Main wind force coroutine
            StartCoroutine (WindForceCor());
        }

        // Reset
        void Reset()
        {
            globalScale = 10f;
        }

        /// /////////////////////////////////////////////////////////
        /// Force
        /// /////////////////////////////////////////////////////////

        // Set colliders by range type
        void SetColliders()
        {
            // Set collider gizmo info
            SetColliderGizmo();

            // TODO make colAmount to private var and check/adjust length before overlap

            // Get overlaps
            int colAmount = Physics.OverlapBoxNonAlloc (center, halfExtents, colliders, transForm.rotation, mask);

            // Increase array if not enough
            if (colAmount == colliders.Length)
            {
                colliders = new Collider[colAmount + 30];
                Physics.OverlapBoxNonAlloc (center, halfExtents, colliders, transForm.rotation, mask);
            }

            // Decrease array if too much
            if (colliders.Length > colAmount + 50)
            {
                colliders = new Collider[colAmount - 30];
                Physics.OverlapBoxNonAlloc (center, halfExtents, colliders, transForm.rotation, mask);
            }
        }

        // Set collider gizmo info
        void SetColliderGizmo()
        {
            //windDirection = transform.forward;
            //halfWidth = widthScale / 2;
            //halfLength = lengthScale / 2;
            halfExtents =  gizmoSize / 2f; // Consider Y height, not at center
            center      =  transForm.position;
            center.y    += halfExtents.y;
        }

        // Set rigid bodies by colliders
        void SetRigidBodies()
        {
            rigidbodies.Clear();

            // Collect all rigid bodies in range
            foreach (Collider col in colliders)
            {
                // Missing collider
                if (col == null)
                    continue;

                // Tag filter
                if (tagFilter != "Untagged" && col.tag != tagFilter)
                    continue;

                // Get attached rigid body
                Rigidbody rb = col.attachedRigidbody;

                // Create projectile if rigid body new. Could be several colliders on one object.
                if (rb != null && rb.isKinematic == false && rigidbodies.Contains (rb) == false)
                    rigidbodies.Add (rb);
            }
        }

        // Apply explosion force, vector and rotation to projectiles
        void SetForce()
        {
            // Set same random state
            Random.InitState (1);

            // Set speed offset
            SetSpeed();

            // Set forceMode by mass state
            ForceMode forceMode = ForceMode.Acceleration;
            if (forceByMass == true)
                forceMode = ForceMode.Force;

            // Get str for each object by expl type with variation
            foreach (Rigidbody rb in rigidbodies)
            {
                Vector3 rbPos = rb.transform.position;

                // Get perlin noise at object position
                float perlinVal = PerlinFixedGlobal (rbPos);

                // Get wind strength at object position
                float windStr = WindStrength (perlinVal) * 10f;

                // Get vector
                Vector3 vector = GetVectorGlobal (rbPos);

                // Apply force
                rb.AddForce (vector * windStr, forceMode);

                // Set rotation impulse
                if (torque > 0)
                    rb.AddTorque (windStr * torque * transForm.right, forceMode);

                // TORQUE by divergence
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Vector 
        /// /////////////////////////////////////////////////////////

        // Get vector for global position
        Vector3 GetVectorGlobal (Vector3 worldPos)
        {
            return GetVectorLocal (transForm.InverseTransformPoint (worldPos));
        }

        // Get vector for local position
        public Vector3 GetVectorLocal (Vector3 localPos)
        {
            // Initial vector TODO optimise for runtime. slow because of editor
            Vector3 vector = transform.forward;

            // Add divergency
            if (divergency > 0)
            {
                // Get perlin noise for rotation vector
                float perlinCustomVal = PerlinCustomLocal (localPos, gizmoSize.x, gizmoSize.z, widthScale, lengthScale, globalScale * turbulence, offset + gizmoSize.z);
                float ang             = Mathf.Lerp (-divergency, divergency, perlinCustomVal);

                // Get wind vector
                vector = Quaternion.Euler (0, ang, 0) * vector;
            }

            return vector;
        }

        /// /////////////////////////////////////////////////////////
        /// Perlin Fixed
        /// /////////////////////////////////////////////////////////

        // Get strength for global position
        float PerlinFixedGlobal (Vector3 worldPos)
        {
            return PerlinFixedLocal (transForm.InverseTransformPoint (worldPos));
        }

        // Get strength for local position
        public float PerlinFixedLocal (Vector3 localPos)
        {
            // Set local position
            localPos.z += offset;

            // Coordinates values
            float xVal = (localPos.x + gizmoSize.x / 2f) / widthScale * globalScale;
            float zVal = (localPos.z + gizmoSize.z / 2f) / lengthScale * globalScale;

            // Perlin noise strength
            float val = Mathf.PerlinNoise (xVal, zVal);

            return val;
        }

        /// /////////////////////////////////////////////////////////
        /// Perlin Custom
        /// /////////////////////////////////////////////////////////

        // Get strength for global position
        public float PerlinCustomGlobal (Vector3 worldPos, float SizeX, float SizeZ, float WidthScale, float LengthScale, float GlobalScale, float Offset)
        {
            return PerlinCustomLocal (transForm.InverseTransformPoint (worldPos), SizeX, SizeZ, WidthScale, LengthScale, GlobalScale, Offset);
        }

        // Get strength for local position
        public float PerlinCustomLocal (Vector3 localPos, float SizeX, float SizeZ, float WidthScale, float LengthScale, float GlobalScale, float Offset)
        {
            // Set local position
            localPos.z += Offset;

            // Coordinates values
            float xVal = (localPos.x + SizeX / 2f) / WidthScale * GlobalScale;
            float zVal = (localPos.z + SizeZ / 2f) / LengthScale * GlobalScale;

            // Perlin noise strength
            float val = Mathf.PerlinNoise (xVal, zVal);

            return val;
        }

        /// /////////////////////////////////////////////////////////
        /// Other
        /// /////////////////////////////////////////////////////////

        // Get average strength
        public float WindStrength (float val)
        {
            return Mathf.Lerp (minimum, maximum, val);
        }

        // Set speed offset
        void SetSpeed()
        {
            if (speed != 0)
            {
                // Get offset by time
                offset -= 0.015f * speed;

                // Reset offset
                if (offset < -100000 || offset > 100000)
                    offset = 0;
            }
        }
    }
}