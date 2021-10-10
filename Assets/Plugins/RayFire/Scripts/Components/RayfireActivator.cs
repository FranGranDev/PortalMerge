using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RayFire
{
    
    // TODO TAG LAYER FILTERS
    // TODO OPTIMIZE OVERLAP || BBOX overlap
    
    [AddComponentMenu ("RayFire/Rayfire Activator")]
    [HelpURL ("http://rayfirestudios.com/unity-online-help/unity-activator-component/")]
    public class RayfireActivator : MonoBehaviour
    {

        // Activation Type
        public enum ActivationType
        {
            OnEnter = 0,
            OnExit  = 1
        }

        // Activation Type
        public enum AnimationType
        {
            ByPositionList = 0,
            ByStaticLine   = 1,
            ByDynamicLine  = 2
        }

        // Gizmo Type
        public enum GizmoType
        {
            Sphere   = 0,
            Box      = 1,
            Collider = 2
        }

        [Header ("  Gizmo")]
        [Space (3)]
        
        public GizmoType gizmoType = GizmoType.Sphere;
        [Space (2)]
        [Range (0.1f, 100f)] public float sphereRadius = 5f;
        [Space (2)]
        public Vector3 boxSize = new Vector3 (5f, 2f, 5f);

        [Header ("  Activation")]
        [Space (3)]
        
        public ActivationType type = ActivationType.OnEnter;
        [Space (2)]
        [Range (0f, 100f)] public float delay = 0f;
        [Space (2)]
        public bool demolishCluster;

        [Header ("  Animation")]
        [Space (3)]
        
        [Range (0.1f, 100f)] public float duration = 3f;
        [Space (2)]
        [Range (1f, 50f)] public float scaleAnimation = 1f;
        [Space (2)]
        public AnimationType positionAnimation = AnimationType.ByPositionList;
        [Space (2)]
        public LineRenderer  line;
        [Space (2)]
        public List<Vector3> positionList;

        // Hidden
        [HideInInspector] public bool showGizmo = true;
        bool        animating   = false;
        float       pathRatio   = 0f;
        float       lineLength  = 0f;
        List<float> checkpoints = new List<float>();
        float       delta;
        float       deltaRatioStep;
        float       distDeltaStep;
        float       distRatio;
        float       timePassed;
        int         activeSegment;
        Vector3     positionStart;
        Vector3     scaleStart;

        // TODO list of objects to check or all colliders objects

        /// /////////////////////////////////////////////////////////
        /// Common
        /// /////////////////////////////////////////////////////////
        void Start()
        {
            
        }

        // Start is called before the first frame update
        void Awake()
        {

            // Check collider and triggers
            ColliderCheck();

            positionStart = transform.position;
            scaleStart    = transform.localScale;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Trigger
        /// /////////////////////////////////////////////////////////

        // Activate on enter
        private void OnTriggerEnter (Collider coll)
        {
            if (type == ActivationType.OnEnter)
                    ActivationCheck (coll);
        }

        // Activate on exit
        private void OnTriggerExit (Collider coll)
        {
            if (type == ActivationType.OnExit)
                    ActivationCheck (coll);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Activation
        /// /////////////////////////////////////////////////////////

        // Check for RayFire Rigid component activation
        void ActivationCheck (Collider coll)
        {
            // Get rigid from collider or rigid body
            RayfireRigid rigid = coll.attachedRigidbody == null 
                ? coll.GetComponent<RayfireRigid>() 
                : coll.attachedRigidbody.GetComponent<RayfireRigid>();
            
            // Has no rigid
            if (rigid == null)
                return;
                
            // Activation
            if (rigid.activation.byActivator == true)
                if (rigid.simulationType == SimType.Inactive || rigid.simulationType == SimType.Kinematic)
                {
                    if (delay <= 0)
                        rigid.Activate();
                    else
                        StartCoroutine (DelayedActivationCor (rigid));
                }
            
            // Connected cluster one fragment detach
           if (rigid.objectType == ObjectType.ConnectedCluster)
               if (demolishCluster == true)
               {
                   if (delay <= 0) 
                       RFDemolitionCluster.DemolishConnectedCluster (rigid, new[] {coll});
                   else
                       StartCoroutine (DelayedClusterCor (rigid, coll));
               }
        }

        // Exclude from simulation and keep object in scene
        IEnumerator DelayedActivationCor (RayfireRigid rigid)
        {
            // Wait life time
            yield return new WaitForSeconds (delay);

            // Activate
            if (rigid != null)
                rigid.Activate();
        }
        
        // Demolish cluster
        IEnumerator DelayedClusterCor (RayfireRigid rigid, Collider coll)
        {
            // Wait life time
            yield return new WaitForSeconds (delay);

            // Activate
            if (rigid != null && coll != null)
                RFDemolitionCluster.DemolishConnectedCluster (rigid, new[] {coll});
        }

        /// /////////////////////////////////////////////////////////
        /// Collider
        /// /////////////////////////////////////////////////////////

        // Check collider and triggers
        void ColliderCheck()
        {
            // Sphere collider
            if (gizmoType == GizmoType.Sphere)
            {
                SphereCollider col = gameObject.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius    = sphereRadius;
            }

            // Box collider
            if (gizmoType == GizmoType.Box)
            {
                BoxCollider col = gameObject.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size      = boxSize;
            }

            // Custom colliders
            if (gizmoType == GizmoType.Collider)
            {
                Collider[] colliders = GetComponents<Collider>();
                if (colliders.Length == 0)
                    Debug.Log (gameObject.name + " has no activation collider", gameObject);
                foreach (Collider coll in colliders)
                    coll.isTrigger = true;
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Animation
        /// /////////////////////////////////////////////////////////

        // Trigger animation start
        public void TriggerAnimation()
        {
            // Already animating
            if (animating == true)
                return;

            // Set animation adata
            SetAnimation();

            // Positions check
            if (positionList.Count < 2 && scaleAnimation == 1f)
            {
                Debug.Log ("Position list is empty and scale is not animated");
                return;
            }

            // Start animation
            StartCoroutine (AnimationCor());
        }

        // Set animation adata
        void SetAnimation()
        {
            // Set points
            if (positionAnimation == AnimationType.ByStaticLine || positionAnimation == AnimationType.ByDynamicLine)
                SetWorldPointsByLine();

            // Set ration checkpoints
            SetCheckPoints();
        }

        // Set points by line
        void SetWorldPointsByLine()
        {
            // Null check
            if (line == null)
            {
                Debug.Log ("Path line is not defined");
                return;
            }

            // Set points
            positionList = new List<Vector3>();
            for (int i = 0; i < line.positionCount; i++)
                positionList.Add (line.transform.TransformPoint (line.GetPosition (i)));

            // Add first point if looped
            if (line.loop == true)
                positionList.Add (positionList[0]);
        }

        // Set ration checkpoints
        void SetCheckPoints()
        {
            // Positions check
            if (positionList.Count < 2)
                return;

            // Total and segments length
            lineLength = 0f;
            List<float> segmentsLength = new List<float>();
            if (positionList.Count >= 2)
            {
                for (int i = 0; i < positionList.Count - 1; i++)
                {
                    float length = Vector3.Distance (positionList[i], positionList[i + 1]);
                    segmentsLength.Add (length);
                    lineLength += length;
                }
            }

            // Get segments ration checkpoints
            float sum = 0f;
            checkpoints = new List<float>();
            for (int i = 0; i < segmentsLength.Count; i++)
            {
                float localRation = segmentsLength[i] / lineLength * 100f;
                checkpoints.Add (sum);
                sum += localRation;
            }

            checkpoints.Add (100f);
        }

        //Animation over line coroutine
        IEnumerator AnimationCor()
        {
            // Stop
            if (animating == true)
                yield break;

            // Set state On
            animating = true;

            // Set starting position
            if (positionList.Count >= 2)
                transform.position = positionList[0];

            while (timePassed < duration)
            {
                // Stop
                if (animating == false)
                    yield break;

                // Update all info for dynamic line
                if (positionAnimation == AnimationType.ByDynamicLine)
                    SetAnimation();

                // Prepare info
                delta      =  Time.deltaTime;
                timePassed += delta;

                // Position animation
                if (positionList.Count >= 2)
                {
                    // Increase time and path ratio
                    deltaRatioStep =  delta / duration;
                    distDeltaStep  =  lineLength * deltaRatioStep;
                    distRatio      =  distDeltaStep / lineLength * 100f;
                    pathRatio      += distRatio;

                    // Get active line segment
                    activeSegment = GetSegment (pathRatio);
                    float   segmentRate = (checkpoints[activeSegment + 1] - pathRatio) / (checkpoints[activeSegment + 1] - checkpoints[activeSegment]);
                    Vector3 stepPos     = Vector3.Lerp (positionList[activeSegment + 1], positionList[activeSegment], segmentRate);
                    transform.position = stepPos;
                }

                // Scale animation
                if (scaleAnimation > 1f)
                {
                    float   scaleRate = timePassed / duration;
                    Vector3 maxScale  = new Vector3 (scaleAnimation, scaleAnimation, scaleAnimation);
                    Vector3 newScale  = Vector3.Lerp (scaleStart, maxScale, scaleRate);
                    transform.localScale = newScale;
                }

                // Wait
                yield return null;
            }

            // Reset data
            ResetData();
        }

        // Get active segment id
        int GetSegment (float ration)
        {
            if (checkpoints.Count > 2)
            {
                for (int i = 0; i < checkpoints.Count - 1; i++)
                    if (ration > checkpoints[i] && ration < checkpoints[i + 1])
                        return i;
                return checkpoints.Count - 2;
            }

            return 0;
        }

        // Reset animation info
        void ResetData()
        {
            animating  = false;
            pathRatio  = 0f;
            lineLength = 0f;
            checkpoints.Clear();
            delta          = 0f;
            deltaRatioStep = 0f;
            distDeltaStep  = 0f;
            distRatio      = 0f;
            timePassed     = 0f;
            activeSegment  = 0;
        }

        // Stop animation
        public void StopAnimation()
        {
            animating = false;
        }

        // Stop animation
        public void ResetAnimation()
        {
            // Reset info
            ResetData();

            // Reset position
            transform.position = positionStart;
        }

        // Add new position
        public void AddPosition (Vector3 newPos)
        {
            if (positionList == null)
            {
                positionList = new List<Vector3>();
            }
            
            // Same position
            if (positionList.Count > 0 && newPos == positionList[positionList.Count - 1])
            {
                Debug.Log ("Activator at the same position");
                return;
            }

            // Check for empty list or same position
            if (positionList.Count == 0 || newPos != positionList[positionList.Count - 1])
                positionList.Add (newPos);
        }
    }
}