using System.Collections.Generic;
using UnityEngine;

namespace RayFire
{
    [AddComponentMenu ("RayFire/Rayfire Blade")]
    [HelpURL ("http://rayfirestudios.com/unity-online-help/unity-blade-component/")]
    public class RayfireBlade : MonoBehaviour
    {
        // Cut Type
        public enum CutType
        {
            Enter     = 0,
            Exit      = 1,
            EnterExit = 2
        }
        
        // Cut Type
        public enum ActionType
        {
            Slice     = 0,
            Demolish  = 1
        }
        
        [Header ("  Properties")]
        [Space (2)]

        public ActionType actionType = ActionType.Slice;
        public CutType onTrigger = CutType.Exit;
        
        // delay
        // damage
        
        [Header ("  Slicing")]
        [Space (2)]
        
        public PlaneType sliceType = PlaneType.XY;

        public GameObject target;
        public List<GameObject> targets;
        
        
        // Hidden
        [HideInInspector] public Transform transForm;
        [HideInInspector] public Vector3[] enterPlane;
        [HideInInspector] public Vector3[] exitPlane;
        [HideInInspector] public Collider colLider;
        [HideInInspector] public int mask = -1;
        [HideInInspector] public string tagFilter = "Untagged";

        // Event
        public RFSliceEvent sliceEvent = new RFSliceEvent();

        
        
        
       
        // check if one slice creates two halfs in one take
        // do not precap, but slice with cap (precap:true, removeCap:true)
        // plane to bound intersection check first `Plane.GetSide`.
        
        /// /////////////////////////////////////////////////////////
        /// Common
        /// /////////////////////////////////////////////////////////

        // Awake
        void Awake()
        {
            // Set components
            DefineComponents();
        }
        
        // Define components
        void DefineComponents()
        {
            transForm = GetComponent<Transform>();
            
            // Check collider
            colLider = GetComponent<Collider>();

            // No collider. Add own
            if (colLider == null)
                colLider = gameObject.AddComponent<MeshCollider>();
                
            // 
            if (colLider is MeshCollider)
                ((MeshCollider)colLider).convex = true;

            colLider.isTrigger = true;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Triggers
        /// /////////////////////////////////////////////////////////
        
        // Check for trigger
        void OnTriggerEnter (Collider col)
        {
            TriggerEnter (col);
        }

        // Exit trigger
        void OnTriggerExit (Collider col)
        {
            TriggerExit (col);
        }
        
        // Trigger enter
        void TriggerEnter (Collider col)
        {
            // Enter
            if (onTrigger == CutType.Enter)
            {
                if (actionType == ActionType.Slice)
                    Slice (col.gameObject, GetSlicePlane());
                else
                    Demolish (col.gameObject);
            }

            // Remember enter plane
            else if (onTrigger == CutType.EnterExit)
            {
                // Set enter plane
                if (actionType == ActionType.Slice)
                    enterPlane = GetSlicePlane();
            }
        }
        
        // Trigger exit
        void TriggerExit (Collider col)
        {
            // Exit
            if (onTrigger == CutType.Exit)
            {
                if (actionType == ActionType.Slice)
                    Slice (col.gameObject, GetSlicePlane());
                else
                    Demolish (col.gameObject);
            }

            // Remember exit plane and calculate average plane
            else if (onTrigger == CutType.EnterExit)
            {
                if (actionType == ActionType.Slice)
                {
                    // Get exit plane
                    exitPlane = GetSlicePlane();

                    // Get slice plane by enter plane and exit plane
                    Vector3[] slicePlane = new Vector3[2];
                    slicePlane[0] = (enterPlane[0] + exitPlane[0]) / 2f;
                    slicePlane[1] = (enterPlane[1] + exitPlane[1]) / 2f;

                    // Slice
                    Slice (col.gameObject, slicePlane);
                }
                else
                    Demolish (col.gameObject);
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Demolition
        /// /////////////////////////////////////////////////////////
        
        // Demolish
        void Demolish(GameObject targetObject)
        {
            // Check tag
            if (tagFilter != "Untagged" && targetObject.CompareTag (tagFilter) == false)
                return;

            // Check layer
            if (LayerCheck (targetObject.layer) == false)
                return;
            
            // Get RayFire script
            RayfireRigid rfScr = targetObject.GetComponent<RayfireRigid>();

            // No Rayfire Rigid script
            if (rfScr == null)
                return;

            // No demolition allowed
            if (rfScr.demolitionType == DemolitionType.None)
                return;
 
            // Available for demolition
            if (rfScr.State() == false)
                return;
            
            // Demolish
            rfScr.limitations.demolitionShould = true;;
        }

        /// /////////////////////////////////////////////////////////
        /// Slicing
        /// /////////////////////////////////////////////////////////

        // Slice target
        public void SliceTarget()
        {
            // Slice target
            if (target != null)
                Slice (target, GetSlicePlane());
            
            // Slice target list
            if (targets != null && targets.Count > 0)
                for (int i = 0; i < targets.Count; i++)
                    if (targets[i] != null)
                        Slice (targets[i], GetSlicePlane());
        }
        
        // Slice collider by blade
        void Slice (GameObject targetObject, Vector3[] slicePlane)
        {
            // Check tag
            if (tagFilter != "Untagged" && !targetObject.CompareTag (tagFilter))
                return;

            // Check layer
            if (LayerCheck (targetObject.layer) == false)
                return;
            
            // Get RayFire script
            RayfireRigid rfScr = targetObject.GetComponent<RayfireRigid>();
            
            // targetObject.GetComponentInParent<RayfireRigid>();
            
            
            // No Rayfire Rigid script
            if (rfScr == null)
                return;
            
            // No demolition allowed
            if (rfScr.demolitionType == DemolitionType.None)
                return;
            
            // Object can't be cut
            if (rfScr.limitations.sliceByBlade == false)
                return;
            
            // Global demolition state check
            if (rfScr.State() == false)
                return;
            
            // Slice object
            rfScr.AddSlicePlane (slicePlane);

            // Event
            sliceEvent.InvokeLocalEvent (this);
            RFSliceEvent.InvokeGlobalEvent (this);
        }

        // Get two points or slice
        Vector3[] GetSlicePlane()
        {
            // Get position and normal
            Vector3[] points = new Vector3[2];
            points[0] = transForm.position;

            // Slice plane direction
            if (sliceType == PlaneType.XY)
                points[1] = transForm.forward;
            else if (sliceType == PlaneType.XZ)
                points[1] = transForm.up;
            else if (sliceType == PlaneType.YZ)
                points[1] = transForm.right;

            return points;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Other
        /// /////////////////////////////////////////////////////////
        
        // Check if object layer is in layer mask
        bool LayerCheck (int layerId)
        {
            //// Layer mask check
            //LayerMask layerMask = new LayerMask();
            //layerMask.value = mask;
            //if (LayerCheck(projectile.rb.gameObject.layer, layerMask) == true)
            //    Debug.Log("In mask " + projectile.rb.name);
            //else
            //    Debug.Log("Not In mask " + projectile.rb.name);
            return mask == (mask | (1 << layerId));
        }

    }
}
