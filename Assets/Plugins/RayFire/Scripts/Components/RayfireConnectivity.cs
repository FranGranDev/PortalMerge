using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Fragments from demolition
// Scale support for bound + Unyielding component

namespace RayFire
{
    [AddComponentMenu ("RayFire/Rayfire Connectivity")]
    [HelpURL ("http://rayfirestudios.com/unity-online-help/unity-connectivity-component/")]
    public class RayfireConnectivity : MonoBehaviour
    {
        [Header ("  Connectivity")]
        [Space (3)]
        
        public ConnectivityType type = ConnectivityType.ByBoundingBox;
        
        [Header ("  Filters")]
        [Space (3)]
        
        
        [Range (0, 1f)] public float minimumArea;
        [Space (2)]
        [Range (0, 10f)] public float minimumSize;
        [Space (2)]
        [Range (0, 100)] public int percentage;
        [Space (2)]
        [Range (0, 100)] public int seed;

        // [Space (2)]
        // [Header ("Check")]
        // [HideInInspector] public bool onActivation = true;
        // [Space (1)]
        // [HideInInspector] public bool onDemolition = true;
        
        [Header ("  Cluster Properties")]
        [Space (3)]
        
        public bool clusterize = true;
        [Space (2)]
        public bool demolishable;

        [Header ("  Collapse")]
        [Space (3)]
        
        public RFCollapse collapse;
        
        // Hidden
        [HideInInspector] public bool showConnections = true;
        [HideInInspector] public bool showNodes = true;
        [HideInInspector] public bool checkConnectivity;
        [HideInInspector] public bool checkNeed;
        [HideInInspector] public bool showGizmo = true;
        [HideInInspector] public List<RayfireRigid> rigidList;
        [HideInInspector] public RFCluster cluster;

        [NonSerialized] bool childrenChanged;

        /// /////////////////////////////////////////////////////////
        /// Common
        /// /////////////////////////////////////////////////////////
                
        // Awake
        void Awake()
        {
            // Set by children.
            SetByChildren();
        }

        // Start is called before the first frame update
        void Start()
        {
            // Rigid check
            if (rigidList.Count == 0)
            {
                Debug.Log ("RayFire Connectivity: " + name + " has no objects to check for connectivity. Connectivity disabled.", gameObject);
                return;
            }
            
            // Check for not mesh root rigid
            RayfireRigid rigid = GetComponent<RayfireRigid>();
            if (rigid != null)
            {
                if (rigid.objectType != ObjectType.MeshRoot)
                {
                    Debug.Log ("RayFire Connectivity: " + name + " object has Rigid component but object type is not Mesh Root. Connectivity disabled.", gameObject);
                    return;
                }
            }
            
            // Connectivity check cor
            StartCoroutine(ChildrenCor());
            
            // Connectivity check cor
            StartCoroutine(ConnectivityCor());
        }

        /// /////////////////////////////////////////////////////////
        /// Children change
        /// /////////////////////////////////////////////////////////    
        
        // Child removed
        void OnTransformChildrenChanged()
        {
            childrenChanged = true;
        }
        
        // Connectivity check cor
        IEnumerator ChildrenCor()
        {
            bool checkChildren = true;
            while (checkChildren == true)
            {
                // Get not connected groups
                if (childrenChanged == true)
                    CheckConnectivity();

                yield return null;
            }
        }

        // Check for children
        void ChildrenCHeck()
        {
            for (int s = cluster.shards.Count - 1; s >= 0; s--)
            {
                if (cluster.shards[s].tm == null)
                {
                    if (cluster.shards[s].neibShards.Count > 0)
                    {
                        // Remove itself in neibs
                        for (int n = 0; n < cluster.shards[s].neibShards.Count; n++)
                        {
                            // Check every neib in neib
                            for (int i = 0; i < cluster.shards[s].neibShards[n].neibShards.Count; i++)
                            {
                                if (cluster.shards[s].neibShards[n].neibShards[i] == cluster.shards[s])
                                {
                                    cluster.shards[s].neibShards[n].neibShards.RemoveAt (i);
                                    cluster.shards[s].neibShards[n].nArea.RemoveAt (i);
                                    cluster.shards[s].neibShards[n].nIds.RemoveAt (i);
                                    break;
                                }
                            }
                        }
                        
                    }
                    cluster.shards.RemoveAt (s);
                }
            }
            childrenChanged = false;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Setup
        /// ///////////////////////////////////////////////////////// 
        
        // Children tms
        public void SetByChildren()
        {
            List<Transform> tmList = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
                tmList.Add (transform.GetChild (i));

            SetConnectivity (tmList);
        }
        
        // Set connectivity fragments and main node
        void SetConnectivity(List<Transform> tmList)
        {
            // Set rigids list and connect with Connectivity component
            SetRigids (tmList);

            // Create Base cluster
            SetCluster (tmList);
        }
        
        // Set cluster
        void SetRigids(List<Transform> tmList)
        {
            // No targets
            if (tmList.Count == 0)
                return;

            // Get rigid with byConnectivity
            rigidList = new List<RayfireRigid>();
            for (int i = 0; i < tmList.Count; i++)
            {
                RayfireRigid rigid = tmList[i].GetComponent<RayfireRigid>();
                if (rigid != null)
                    if (rigid.simulationType == SimType.Inactive || rigid.simulationType == SimType.Kinematic)
                        if (rigid.activation.byConnectivity == true)
                            rigidList.Add (rigid);
            }
            
            // No targets
            if (rigidList.Count == 0)
                return;
            
            // Set this connectivity as main connectivity node
            for (int i = 0; i < rigidList.Count; i++)
                rigidList[i].activation.connect = this;
        }

        // Set cluster
        void SetCluster (List<Transform> tmList)
        {
            // In case of runtime add
            if (cluster == null)
                cluster = new RFCluster();

            // Main cluster cached, reinit non serialized vars
            if (cluster.shards.Count > 0)
                InitShards (rigidList, cluster);

            // Create main cluster
            if (cluster.shards.Count == 0)
            {
                cluster              = new RFCluster();
                cluster.id           = RFCluster.GetUniqClusterId (cluster);
                cluster.tm           = transform;
                cluster.depth        = 0;
                cluster.pos          = transform.position;
                cluster.initialized  = true;
                cluster.demolishable = demolishable;

                // Set shards for main cluster
                if (Application.isPlaying == true)
                    SetShardsByRigids (cluster, rigidList, type);
                else
                    RFShard.SetShardsByTransforms (cluster, tmList, type);
                
                // Set shard neibs
                RFShard.SetShardNeibs (cluster.shards, type, minimumArea, minimumSize, percentage, seed);

                // Set range for area and size
                RFCollapse.SetRangeData (cluster, percentage, seed);
                
                // Debug.Log ("SetCluster" + rigidList.Count);
            }
        }
        
        // Prepare shards. Set bounds, set neibs
        static void SetShardsByRigids(RFCluster cluster, List<RayfireRigid> rigidList, ConnectivityType connectivity)
        {
            for (int i = 0; i < rigidList.Count; i++)
            {
                // Get mesh filter
                MeshFilter mf = rigidList[i].GetComponent<MeshFilter>();

                // Child has no mesh
                if (mf == null)
                    continue;

                // Create new shard
                RFShard shard = new RFShard(rigidList[i].transform, i);
                shard.cluster = cluster;
                shard.rigid = rigidList[i];
                shard.uny = rigidList[i].activation.unyielding;
                shard.col = rigidList[i].physics.meshCollider;

                // Set faces data for connectivity
                if (connectivity == ConnectivityType.ByMesh)
                    RFTriangle.SetTriangles(shard, mf);

                // Collect shard
                cluster.shards.Add(shard);
            }
        }
        
        // Reinit shard's non serialized fields in case of prefab use
        public static void InitShards (List<RayfireRigid> rigids, RFCluster cluster)
        {
            if (cluster.initialized == false)
            {
                // Rigid list doesn't match shards. TODO compare per shard
                if (cluster.shards.Count != rigids.Count)
                {
                    cluster.shards.Clear();
                    return;
                }
                
                // Reinit
                for (int s = 0; s < cluster.shards.Count; s++)
                {
                    if (rigids[s] != null)
                    {
                        cluster.shards[s].rigid    = rigids[s];
                        cluster.shards[s].uny = rigids[s].activation.unyielding;
                    }
                    
                    cluster.shards[s].cluster = cluster;
                    cluster.shards[s].neibShards = new List<RFShard>();
                    for (int n = 0; n < cluster.shards[s].nIds.Count; n++)
                        cluster.shards[s].neibShards.Add (cluster.shards[cluster.shards[s].nIds[n]]);
                }
                cluster.initialized = true;
            }
        }
        
         /// /////////////////////////////////////////////////////////
         /// Connectivity
         /// /////////////////////////////////////////////////////////   
        
        // Connectivity check cor
        IEnumerator ConnectivityCor()
        {
            checkConnectivity = true;
            while (checkConnectivity == true)
            {
                // Child deleted
                if (childrenChanged == true)
                    ChildrenCHeck();
                
                // Get not connected groups
                if (checkNeed == true)
                    CheckConnectivity();

                yield return null;
            }
        }
        
        // Check for connectivity
        public void CheckConnectivity()
        {
            // Do once
            checkNeed = false;
            
            // Clear all activated/demolished shards
            CleanUpActivatedShards (cluster);
            
            // No shards to check
            if (cluster.shards.Count == 0)
                return;
            
            // Reinit neibs after cleaning
            RFShard.ReinitNeibs (cluster.shards);
            
            // List of shards to be activated
            List<RFShard> soloShards = new List<RFShard>();

             // TODO do not collect solo uny shards
            // Check for solo shards and collect
            RFCluster.GetSoloShards (cluster, soloShards);
            
            // Reinit neibs before connectivity check
            RFShard.ReinitNeibs (cluster.shards);
            
            // Connectivity check
            RFCluster.ConnectivityCheck (cluster);
            
            // Get not connected and not unyielding child cluster
            CheckUnyielding (cluster);

            // TODO ONE NEIB DETACH FOR CHILD CLUSTERS
            
            // Activate not connected shards. 
            if (soloShards.Count > 0)
                for (int i = 0; i < soloShards.Count; i++)
                    soloShards[i].rigid.Activate();
            
            // Clusterize childClusters  or activate their shards
            if (cluster.HasChildClusters == true)
            {
                if (clusterize == true)
                    Clusterize();
                else
                    for (int c = 0; c < cluster.childClusters.Count; c++)
                        for (int s = 0; s < cluster.childClusters[c].shards.Count; s++)
                            cluster.childClusters[c].shards[s].rigid.Activate();
            }

            // Stop checking. Everything activated
            if (cluster.shards.Count == 0)
                checkConnectivity = false;
        }

        // Clusterize not connected groups
        void Clusterize()
        {
            for (int i = 0; i < cluster.childClusters.Count; i++)
            {
                // set demolishable state for child cluster
                cluster.demolishable = demolishable;
                
                // Set bound 
                cluster.childClusters[i].bound = RFCluster.GetShardsBound (cluster.childClusters[i].shards);
                
                // Create cluster
                cluster.childClusters[i].shards[0].rigid.simulationType = SimType.Dynamic; // TODO IN BETTER WAY
                cluster.childClusters[i].shards[0].rigid.objectType = ObjectType.ConnectedCluster;
                RFDemolitionCluster.CreateClusterRuntime (cluster.childClusters[i].shards[0].rigid, cluster.childClusters[i]);
                cluster.childClusters[i].shards[0].rigid.objectType = ObjectType.Mesh;
                
                // Copy preview
                cluster.childClusters[i].rigid.clusterDemolition.cn = showConnections;
                cluster.childClusters[i].rigid.clusterDemolition.nd = showNodes;
                
                // Destroy components
                for (int s = 0; s < cluster.childClusters[i].shards.Count; s++)
                {
                    Destroy (cluster.childClusters[i].shards[s].rigid.physics.rigidBody);
                    Destroy (cluster.childClusters[i].shards[s].rigid);
                }
            }
        }

        // Clear all activated/demolished shards
        static void CleanUpActivatedShards(RFCluster cluster)
        {
            for (int i = cluster.shards.Count - 1; i >= 0; i--)
            {
                if (cluster.shards[i].rigid == null ||
                    cluster.shards[i].rigid.activation.connect == null ||
                    cluster.shards[i].rigid.limitations.demolished == true)
                {
                    cluster.shards[i].cluster = null;
                    cluster.shards.RemoveAt (i);
                }
            }
        }
 
        // Collect solo shards, remove from cluster, reinit cluster
        static void CheckUnyielding(RFCluster cluster)
        {
            // Get not connected and not unyielding child cluster
            if (cluster.HasChildClusters == true)
            {
                // Remove all unyielding child clusters
                for (int c = cluster.childClusters.Count - 1; c >= 0; c--)
                {
                    if (cluster.childClusters[c].UnyieldingByRigid == true)
                    {
                        cluster.shards.AddRange (cluster.childClusters[c].shards);
                        cluster.childClusters.RemoveAt (c);
                    }
                }
                
                // Set unyielding cluster shards back to original cluster
                for (int s = 0; s < cluster.shards.Count; s++)
                    cluster.shards[s].cluster = cluster;
            }
        }
    }
}