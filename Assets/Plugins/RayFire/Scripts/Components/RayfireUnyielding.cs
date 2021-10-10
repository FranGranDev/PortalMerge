using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RayFire
{
    [AddComponentMenu ("RayFire/Rayfire Unyielding")]
    [HelpURL ("http://rayfirestudios.com/unity-online-help/unity-unyielding-component/")]
    public class RayfireUnyielding : MonoBehaviour
    {
        public enum RFUnyType
        {
            AtStart = 0,
            ByMethod  = 3
        }
        
        
        
        
        [HideInInspector] public Vector3 size = new Vector3(1f,1f,1f);
        [HideInInspector] public Vector3 centerPosition;
        [HideInInspector] public List<RayfireRigid> rigidList;
        [HideInInspector] public bool initialized;
        
        // Hidden
        [HideInInspector] public bool showGizmo = true;
        [HideInInspector] public bool showCenter;

        [Space (3)]
        public RFUnyType initialize = RFUnyType.ByMethod;
        
        /// /////////////////////////////////////////////////////////
        /// Collider
        /// /////////////////////////////////////////////////////////
        
        void Start()
        {
            if (initialize == RFUnyType.AtStart)
            {
                Initialize();
            }
        }
        
        // Set uny state
        public void SetUnyByOverlap(RayfireRigid scr)
        {
            if (enabled == false)
                return;
            
            if (initialize == RFUnyType.AtStart)
                return;
            
            // Check if component already did the job to prevent several use on same object
            if (initialized == true)
                return;
            
            // Get target mask TODO check fragments layer
            // int mask = 1 << scr.gameObject.layer;
            
            // Get box overlap colliders
            Collider[] colliders = Physics.OverlapBox (transform.TransformPoint (centerPosition), size / 2f, transform.rotation, 1 << scr.gameObject.layer);
            
            // Check with mesh object
            if (scr.objectType == ObjectType.Mesh)
            {
                if (scr.physics.meshCollider != null)
                    if (colliders.Contains (scr.physics.meshCollider) == true)
                        scr.activation.unyielding = true;
            }

            // Check with mesh root object
            else if (scr.objectType == ObjectType.MeshRoot)
            {
                for (int i = 0; i < scr.fragments.Count; i++)
                    if (scr.fragments[i].physics.meshCollider != null)
                        if (colliders.Contains (scr.fragments[i].physics.meshCollider) == true)
                            scr.fragments[i].activation.unyielding = true;
            }

            // Check with connected cluster
            else if (scr.objectType == ObjectType.ConnectedCluster)
            {
                for (int i = 0; i < scr.physics.clusterColliders.Count; i++)
                    if (scr.physics.clusterColliders[i] != null)
                        if (colliders.Contains (scr.physics.clusterColliders[i]) == true)
                            scr.clusterDemolition.cluster.shards[i].uny = true;
            }

            initialized = true;
        }
        
        
        // Set uny state
        public void Initialize ()
        {
            if (enabled == false)
                return;
            
            // Check if component already did the job to prevent several use on same object
            if (initialized == true)
                return;
            
            // Get target mask TODO check fragments layer
            // int mask = 1 << scr.gameObject.layer;
            
            // Get box overlap colliders
            Collider[] colliders = Physics.OverlapBox (transform.TransformPoint (centerPosition), size / 2f, transform.rotation);

            // Set state for ovelapped rigids
            SetUnyByColliders (colliders);
        }
        
        // Set uny state
        public void SetUnyByColliders (Collider[] colliders)
        {
            // Get rigids
            if (rigidList == null)
                rigidList = new List<RayfireRigid>();
            else
                rigidList.Clear();
            
            // Collect TODO get shard's cluster rigid
            for (int i = 0; i < colliders.Length; i++)
            {
                RayfireRigid rigid = colliders[i].GetComponent<RayfireRigid>();
                if (rigid != null)
                    if (rigidList.Contains (rigid) == false)
                        rigidList.Add (rigid);
            }
            
            // Set this uny state
            SetUnyRigids (rigidList);
        }
        
        // Set this uny state
        public void SetUnyRigids (List<RayfireRigid> rigids)
        {
            if (rigids.Count > 0)
                for (int i = 0; i < rigids.Count; i++)
                    rigids[i].activation.unyielding = true;
        }
    }
}