using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RayFire
{
    [Serializable]
    public class RFDemolitionCluster
    {
        public enum RFDetachType
        {
            RatioToSize = 0,
            WorldUnits  = 3
        }

        [Header ("  Properties")]
        [Space (3)]
        
        [Tooltip ("Defines Connectivity algorithm for clusters.")]
        public ConnectivityType connectivity; 
        
        [Header ("  Connection Filters")]
        [Space (3)]

        [Range (0, 1f)] public float minimumArea;
        [Space (2)]
        [Range (0, 10f)] public float minimumSize;
        [Space (2)]
        [Range (0, 100)] public int percentage;
        [Space (2)]
        [Range (0, 100)] public int seed;
        
        [Header ("  Demolition Distance")]
        [Space (3)]

        public RFDetachType type;
        
        [Space (2)]
        [Tooltip ("Defines distance from contact point in percentage relative to object's size which will be detached at contact.")]
        [Range (1, 100)] public int ratio;
        [Space (2)]
        [Range (0, 10)]  public float units;

        [Header ("  Shards")]
        [Space (3)]

        [Range (0, 100)] public int shardArea;
        [Space (2)]
        public bool shardDemolition;
        
        [Header ("  Clusters")]
        [Space (3)]
        
        [Range (2, 20)] public int minAmount;
        [Space (2)]
        [Range (2, 20)] public int maxAmount;
        [Space (2)]
        public bool demolishable;
        
        [Header ("  Collapse")]
        [Space (3)]
        
        public RFCollapse collapse;

        // Hidden
        [HideInInspector] public int clsCount;
        [HideInInspector] public RFCluster cluster;
        [HideInInspector] public List<RFCluster> minorClusters;
        [HideInInspector] public bool cn;
        [HideInInspector] public bool nd;
                
        // Nonserialized
        [NonSerialized] public RFBackupCluster backup;
        [NonSerialized] public float damageRadius;        
        [NonSerialized] [Range (0.8f, 1f)] public float scaleFactor;
        [NonSerialized] public int edgeShardArea;

        // New cluster name appendix
        static string nameApp = "_cls_";
        
        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////

        // Constructor
        public RFDemolitionCluster()
        {
            connectivity    = ConnectivityType.ByBoundingBox;

            minimumArea = 0f;
            minimumSize = 0f;
            percentage  = 0;
            seed        = 0;
            ratio       = 15;
            units       = 1f;

            shardArea       = 100;
            scaleFactor     = 1f;
            shardDemolition = false;
            edgeShardArea   = 0;

            minAmount    = 3;
            maxAmount    = 6;
            demolishable = true;
            
            cn = false;
            nd = false;

            clsCount = 1;
            
            Reset();
        }

        // Copy from
        public void CopyFrom (RFDemolitionCluster demolition)
        {
            connectivity    = demolition.connectivity;
            
            minimumArea     = demolition.minimumArea;
            minimumSize     = demolition.minimumSize;
            percentage      = demolition.percentage;
            seed            = demolition.seed;
            
            type            = demolition.type;
            ratio           = demolition.ratio;
            units           = demolition.units;
            
            shardArea       = demolition.shardArea;
            shardDemolition = demolition.shardDemolition;
            scaleFactor     = demolition.scaleFactor;

            maxAmount       = demolition.maxAmount;
            minAmount       = demolition.minAmount;
            demolishable    = demolition.demolishable;

            cn = demolition.cn;
            nd       = demolition.nd;
            
            Reset();
        }

        // Reset
        public void Reset()
        {
            damageRadius = 0f;
        }

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////
        
        // Setup Rigid cluster
        public static bool SetupCluster(RayfireRigid scr)
        {
            // Not cluster
            if (scr.objectType != ObjectType.ConnectedCluster &&
                scr.objectType != ObjectType.NestedCluster)
                return false;

            RFPhysic.DestroyColliders (scr);
            scr.clusterDemolition.cluster       = new RFCluster();
            scr.clusterDemolition.clsCount      = 1;
            scr.clusterDemolition.minorClusters = null;
            scr.SetComponentsBasic();
            RFParticles.SetParticleComponents(scr);
            
            return Clusterize (scr);
        }
        
        // Set fragments collider
        public static bool Clusterize (RayfireRigid scr)
        {
            // No children
            if (scr.transForm.childCount == 0)
                return false;

            // Clusterize
            if (scr.objectType == ObjectType.NestedCluster)
                ClusterizeNested (scr);
            else if (scr.objectType == ObjectType.ConnectedCluster)
                ClusterizeConnected (scr);

            // Reinit connected cluster shards non serialized fields if main cluster not initialized
            RFCluster.InitCluster (scr, scr.clusterDemolition.cluster);
            
            // Set colliders
            bool colliderSet = RFPhysic.SetClusterCollidersByShards (scr);

            // Set unyielding state
            RFActivation.SetUnyielding (scr);
            
            // Save backup if cluster will be restored
            RFBackupCluster.SaveBackup (scr);
            
            return colliderSet;
        }
        
        // Create one cluster which includes only children meshes, not children of children meshes.
        static void ClusterizeNested (RayfireRigid scr)
        {
            // Has not minor cluster. Never was Clusterized. DO NOT REPEAT FOR MINOR CLUSTERS
            if (scr.clusterDemolition.HasMinorClusters == false && scr.clusterDemolition.cluster.id == -1)
            {
                // Create main cluster
                scr.clusterDemolition.cluster              = new RFCluster();
                scr.clusterDemolition.cluster.id           = RFCluster.GetUniqClusterId (scr.clusterDemolition.cluster);
                scr.clusterDemolition.cluster.tm           = scr.transForm;
                scr.clusterDemolition.cluster.depth        = 0;
                scr.clusterDemolition.cluster.pos          = scr.transForm.position;
                scr.clusterDemolition.cluster.rot          = scr.transForm.rotation;
                scr.clusterDemolition.cluster.demolishable = true;
                scr.clusterDemolition.cluster.rigid        = scr;
                scr.clusterDemolition.cluster.initialized  = true;
                
                // List to store all clusters for prefabs
                scr.clusterDemolition.minorClusters        = new List<RFCluster>();
                
                // Create child clusters and their child clusters
                ClusterizeNestedRecursive(scr, scr.transForm, scr.clusterDemolition.cluster, scr.clusterDemolition.connectivity);
            }
        }

        // Setup shards and child clusters by children
        static void ClusterizeNestedRecursive( RayfireRigid scr, Transform transform, RFCluster cluster, ConnectivityType connectivity)
        {
            // Get shards and clusters transforms
            Transform       tm;
            List<Transform> tmShards   = new List<Transform>();
            List<Transform> tmClusters = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                tm = transform.GetChild (i);
                if (tm.childCount == 0) tmShards.Add (tm);
                else tmClusters.Add (tm);
            }

            // Setup shards
            if (tmShards.Count > 0)
                RFShard.SetShardsByTransforms (cluster, tmShards, connectivity, true);

            // Setup child Clusters
            if (tmClusters.Count > 0)
            {
                for (int i = tmClusters.Count - 1; i >= 0; i--)
                {
                    // TODO check if children have meshfilter
                    
                    // Create main cluster
                    RFCluster newCluster    = new RFCluster();
                    newCluster.mainCluster  = scr.clusterDemolition.cluster;
                    newCluster.id           = RFCluster.GetUniqClusterId (newCluster);
                    newCluster.tm           = tmClusters[i];
                    newCluster.depth        = 0;
                    newCluster.pos          = tmClusters[i].position;
                    newCluster.rot          = tmClusters[i].rotation;
                    newCluster.initialized  = true;
                    newCluster.demolishable = true;
                    newCluster.bound        = RFCluster.GetChildrenBound (newCluster.tm);
                    newCluster.rigid        = newCluster.tm.GetComponent<RayfireRigid>();
                    
                    // Save in minor cluster
                    scr.clusterDemolition.minorClusters.Add (newCluster);
                    
                    // Create Child Clusters and shards for new cluster
                    ClusterizeNestedRecursive (scr, tmClusters[i], newCluster, connectivity);

                    // Collect new cluster
                    if (cluster.childClusters == null)
                        cluster.childClusters = new List<RFCluster>();
                    cluster.childClusters.Add (newCluster);
                }
            }
        }

        // Create 
        static void ClusterizeConnected (RayfireRigid scr)
        {
            if (scr.clusterDemolition.cluster.shards.Count == 0)
            {
                // Create main cluster
                scr.clusterDemolition.cluster              = new RFCluster();
                scr.clusterDemolition.cluster.id           = RFCluster.GetUniqClusterId (scr.clusterDemolition.cluster);
                scr.clusterDemolition.cluster.tm           = scr.transForm;
                scr.clusterDemolition.cluster.depth        = 0;
                scr.clusterDemolition.cluster.pos          = scr.transForm.position;
                scr.clusterDemolition.cluster.demolishable = true;
                scr.clusterDemolition.cluster.rigid        = scr;
                scr.clusterDemolition.cluster.initialized  = true;

                // Set shards for main cluster
                RFShard.SetShards(scr.clusterDemolition.cluster, scr.clusterDemolition.connectivity, true);
                
                // Set shard neibs
                RFShard.SetShardNeibs (scr.clusterDemolition.cluster.shards, scr.clusterDemolition.connectivity, 
                    scr.clusterDemolition.minimumArea, scr.clusterDemolition.minimumSize, 
                    scr.clusterDemolition.percentage, scr.clusterDemolition.seed);
                
                // Set range for area and size
                RFCollapse.SetRangeData (scr.clusterDemolition.cluster, scr.clusterDemolition.percentage, scr.clusterDemolition.seed);
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Demolition
        /// /////////////////////////////////////////////////////////

        // Demolish cluster to children nodes
        public static bool DemolishCluster (RayfireRigid scr)
        {
            if (scr.objectType != ObjectType.NestedCluster && scr.objectType != ObjectType.ConnectedCluster)
                return false;
            
            // Skip if not runtime
            if (scr.demolitionType != DemolitionType.Runtime)
                return true;
            
            // TODO inherit original cluster velocity
            
            // Cluster demolition
            if (scr.objectType == ObjectType.NestedCluster)
                DemolishClusterNested (scr);
            else if (scr.objectType == ObjectType.ConnectedCluster)
                DemolishConnectedCluster (scr);
            
            // Demolition executed
            scr.limitations.demolitionShould = false;

            // Delete if demolished
            if (scr.limitations.demolished == true)
                RayfireMan.DestroyFragment (scr, null);
            
            return true;
        }

        // Final ops at connected cluster demolition, slice, collapse
        public static void PostDemolitionCluster (RayfireRigid scr, List<RFShard> detachShards)
        {
            // Prepare
            if (scr.fragments == null)
                scr.fragments = new List<RayfireRigid>();
            else
                scr.fragments.Clear();
            
            // Create Rigid Shards
            CreateRigidShards (scr, detachShards);
        
            // Create Rigid Child cluster
            CreateChildClusters (scr, scr.clusterDemolition.cluster.childClusters);
        
            // Update properties
            UpdateOriginalCluster (scr);
            
            // Set velocity
            RFPhysic.SetFragmentsVelocity (scr);
            
            // Fading. move to fragment
            if (scr.fading.onDemolition == true)
                scr.fading.DemolitionFade (scr.fragments);
            
            // Init particles
            RFParticles.InitDemolitionParticles(scr);

            // Init sound
            RFSound.DemolitionSound(scr.sound, scr.limitations.bboxSize);
            
            // Event
            scr.demolitionEvent.InvokeLocalEvent (scr);
            RFDemolitionEvent.InvokeGlobalEvent (scr);
        }

        /// /////////////////////////////////////////////////////////
        /// Nested cluster demolition
        /// /////////////////////////////////////////////////////////
        
        // Demolish nested cluster
        static void DemolishClusterNested (RayfireRigid scr)
        {
            // Set demolished state. IMPORTANT should be here
            scr.limitations.demolished = true;

            List<RFShard> detachShards = new List<RFShard>();
            for (int i = 0; i < scr.clusterDemolition.cluster.shards.Count; i++)
                detachShards.Add (scr.clusterDemolition.cluster.shards[i]);

            // Clear list if not going to be used
            if (scr.reset.action == RFReset.PostDemolitionType.DestroyWithDelay)
                scr.clusterDemolition.cluster.shards.Clear();
            
            // Create child clusters and shards
            PostDemolitionCluster (scr, detachShards);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Demolition
        /// /////////////////////////////////////////////////////////

        // Demolish connected cluster
        public static void DemolishConnectedCluster (RayfireRigid scr, Collider[] detachColliders = null)
        {
            // Get colliders to detach
            if (detachColliders == null)
                detachColliders = GetDetachColliders (scr);

            // No colliders to detach
            if (detachColliders.Length == 0)
                return;
            
            // Save original amount of shards
            int shardAmount = scr.clusterDemolition.cluster.shards.Count;

            // Get detach shards area and remove them from cluster. Includes not connected solo shards from cluster
            List<RFShard> detachShards = GetDetachShards (scr, detachColliders);
            
            // No shards to detach. Cluster not demolished
            if (detachShards.Count == 0)
                return;
            
            // All Shards was detached TODO 
            // if (scr.clusterDemolition.cluster.shards.Count == 0)
            //     return;
            
            // All detach shards uny if kinematik/inactive cluster TODO add to slice, collapse
            if (scr.simulationType == SimType.Kinematic || scr.simulationType == SimType.Inactive)
                if (RFShard.UnyieldingByShardAll (detachShards) == true)
                    return;
            
            // Get amount of clusters to create and amount of edge shards
            int clusterAmount = Random.Range (scr.clusterDemolition.maxAmount, scr.clusterDemolition.minAmount + 1);

            // All shards should be clusterized to one cluster. Stop
            // if (SameClusterCheck(scr, detachShards, shardAmount, clusterAmount) == true)
            //    return;

            // Clear fragments in case of previous demolition
            if (scr.HasFragments == true)
                scr.fragments.Clear();
            
            // Dynamic cluster connectivity check, all clusters are equal, pick biggest to keep as original 
            if (scr.simulationType == SimType.Dynamic || scr.simulationType == SimType.Sleeping)
            {
                // Check left cluster shards for connectivity and collect not connected child clusters. Should be before ClusterizeDetachShards
                RFCluster.ConnectivityCheck (scr.clusterDemolition.cluster);
             
                // Cluster is not connected. If not main cluster then set biggest child cluster shards to original cluster. 
                RFCluster.ReduceChildClusters (scr.clusterDemolition.cluster);
            }

            // Kinematik/ Inactive cluster, Connectivity check if cluster has uny shards. Main cluster keeps all not activated
            if (scr.simulationType == SimType.Kinematic || scr.simulationType == SimType.Inactive)
            {
                RFCluster.ConnectivityUnyCheck (scr.clusterDemolition.cluster);
            }

            // TODO Set solo uny detached shards back to cluster or prevent from collection
            
            // Clusterize detached shards if needed. Update child clusters and detached solo shards list
            ClusterizeDetachShards (scr, detachShards, clusterAmount, 0);

            // Init final cluster ops
            PostDemolitionCluster (scr, detachShards);
        }

        // Get colliders to detach
        static Collider[] GetDetachColliders (RayfireRigid scr)
        {
            // TODO instead overlap, get contact shard, go through all neibs and collect all in radius,
            // exclude and mark all not in radius, stop when there is no grow anymore
            
            // Get colliders by damage radius and reset it
            if (scr.clusterDemolition.damageRadius > 0)
            {
                Collider[] colliders = Physics.OverlapSphere (scr.limitations.contactVector3, scr.clusterDemolition.damageRadius, 1 << scr.gameObject.layer);
                scr.clusterDemolition.damageRadius = 0f;
                return colliders;
            }
            
            // Get detach colliders by manual damage radius
            if (scr.clusterDemolition.type == RFDetachType.WorldUnits)
                if (scr.clusterDemolition.units > 0)
                    return Physics.OverlapSphere (scr.limitations.contactVector3, scr.clusterDemolition.units, 1 << scr.gameObject.layer);
            
            // Use all colliders if contactRadius is 100%
            if (scr.clusterDemolition.ratio == 100)
                return scr.physics.clusterColliders.ToArray();
            
            // Get colliders by contactRadius by overlap
            float contactRadius = scr.limitations.bboxSize / 100f * scr.clusterDemolition.ratio;
            return Physics.OverlapSphere (scr.limitations.contactVector3, contactRadius, 1 << scr.gameObject.layer);
        }
        
        // Create runtime clusters
        static List<RFShard> GetDetachShards (RayfireRigid scr, Collider[] detachColliders)
        {
            // Collect detach shards. Mark removed shards
            List<RFShard> detachShards = new List<RFShard>();
            for (int i = scr.physics.clusterColliders.Count - 1; i >= 0; i--)
            {
                if (detachColliders.Contains (scr.physics.clusterColliders[i]) == true)
                {
                    detachShards.Add (scr.clusterDemolition.cluster.shards[i]);
                    scr.clusterDemolition.cluster.shards.RemoveAt (i);
                    scr.physics.clusterColliders.RemoveAt (i);
                }
            }
            
            // No detach shards. Cluster was not demolished
            if (detachShards.Count == 0)
                return detachShards;
            
            // Original cluster has only one shard left. Add it to all detached shards
            if (scr.clusterDemolition.cluster.shards.Count == 1)
            {
                detachShards.Add (scr.clusterDemolition.cluster.shards[0]);
                scr.clusterDemolition.cluster.shards.Clear();
                scr.physics.clusterColliders.Clear();
            }

            // Update shards cluster data before reinit left and detached shards neib data
            for (int i = 0; i < detachShards.Count; i++)
                detachShards[i].cluster  = null;
            
            // Original cluster still has shards
            if (scr.clusterDemolition.cluster.shards.Count > 0)
            {
                // Remove neib shards which are not in current cluster anymore
                RFShard.ReinitNeibs (scr.clusterDemolition.cluster.shards);
                
                // TODO detach shards with one neib now which had detached shards as neibs
                
                // Collect solo shards, remove from cluster
                RFCluster.GetSoloShards (scr.clusterDemolition.cluster, detachShards);
            }
            
            return detachShards;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Slice
        /// /////////////////////////////////////////////////////////
        
        // Slice connected cluster
        public static void SliceConnectedCluster (RayfireRigid scr)
        {
            // Get detach distance
            float detachDistance = 0f;
            if (scr.clusterDemolition.type == RFDetachType.WorldUnits)
                if (scr.clusterDemolition.units > 0)
                    detachDistance = scr.clusterDemolition.units;
            
            // Use all colliders if contactRadius is 100%
            if (scr.clusterDemolition.type == RFDetachType.RatioToSize)
                detachDistance = scr.limitations.bboxSize / 100f * scr.clusterDemolition.ratio;
            
            // Get two clusters for planes sides
            List<RFShard> detachShards = new List<RFShard>();
            List<RFShard> cluster1Shards = new List<RFShard>();
            List<RFShard> cluster2Shards = new List<RFShard>();

            // Separate shards by slice plane
            Vector3 shardPos;
            Plane plane = new Plane(scr.limitations.slicePlanes[1], scr.limitations.slicePlanes[0]);
            scr.limitations.slicePlanes.Clear();
            for (int s = 0; s < scr.clusterDemolition.cluster.shards.Count; s++)
            {
                // Save position
                shardPos = scr.clusterDemolition.cluster.shards[s].tm.position;
               
                // Check distance and add to detach shards if too close
                if (detachDistance > 0)
                {
                    if (Math.Abs(plane.GetDistanceToPoint (shardPos)) < detachDistance)
                    {
                        detachShards.Add (scr.clusterDemolition.cluster.shards[s]);
                        scr.clusterDemolition.cluster.shards[s].cluster = null;
                        continue;
                    }
                }
                
                // Get plane side
                if (plane.GetSide (shardPos) == true)
                    cluster1Shards.Add (scr.clusterDemolition.cluster.shards[s]);
                else
                    cluster2Shards.Add (scr.clusterDemolition.cluster.shards[s]);
            }
            
            // Check clusters for solo shards and send them to detach shards
            if (cluster1Shards.Count == 1)
            {
                detachShards.Add (cluster1Shards[0]);
                cluster1Shards.Clear();
            }
            if (cluster2Shards.Count == 1)
            {
                detachShards.Add (cluster2Shards[0]);
                cluster2Shards.Clear();
            }
            
            // No detach shards and One of the cluster equal to original cluster. Stop
            if (detachShards.Count == 0)
                if (cluster1Shards.Count == scr.clusterDemolition.cluster.shards.Count ||
                    cluster2Shards.Count == scr.clusterDemolition.cluster.shards.Count)
                    return;
            
            // Dynamic cluster connectivity check, all clusters are equal, pick biggest to keep as original 
            if (scr.simulationType == SimType.Dynamic || scr.simulationType == SimType.Sleeping)
            {
                // Prepare child clusters list
                if (cluster1Shards.Count >= 2 || cluster2Shards.Count >= 2)
                    if (scr.clusterDemolition.cluster.childClusters == null)
                        scr.clusterDemolition.cluster.childClusters = new List<RFCluster>();
                    else
                        scr.clusterDemolition.cluster.childClusters.Clear();
                
                // Setup cluster by one slice plane side shards. Connectivity check
                SetupPlaneShards (scr, cluster1Shards, detachShards);
                SetupPlaneShards (scr, cluster2Shards, detachShards);
             
                // Cluster is not connected. If not main cluster then set biggest child cluster shards to original cluster. 
                RFCluster.ReduceChildClusters (scr.clusterDemolition.cluster);
            }
            
            // Kinematik/ Inactive cluster, Connectivity check if cluster has uny shards. Main cluster keeps all not activated
            if (scr.simulationType == SimType.Kinematic || scr.simulationType == SimType.Inactive)
            {
                // Remove detach shards and child clusters shards from main cluster shards
                for (int i = scr.clusterDemolition.cluster.shards.Count - 1; i >= 0; i--)
                    if (scr.clusterDemolition.cluster.shards[i].cluster != scr.clusterDemolition.cluster)
                        scr.clusterDemolition.cluster.shards.RemoveAt (i);
                
                // Reset neibs
                RFShard.ReinitNeibs (scr.clusterDemolition.cluster.shards);
                
                // Check for uny connectivity
                RFCluster.ConnectivityUnyCheck (scr.clusterDemolition.cluster);
            }
            
            // Detach shards not going to be changed anymore. Reinit detach shards neib. 
            if (detachShards.Count > 0)
            {
                // DO LATER
                RFShard.ReinitNeibs (detachShards);
                
                // Get point in other way 
                scr.limitations.contactVector3 = plane.ClosestPointOnPlane (detachShards[0].tm.position);
                scr.limitations.contactNormal  = plane.normal;
                    
                // Get amount of clusters to create and amount of edge shards
                int clusterAmount = Random.Range (scr.clusterDemolition.maxAmount, scr.clusterDemolition.minAmount + 1);
                
                // Clusterize detach shards but over plane
                ClusterizeDetachShards (scr, detachShards, clusterAmount, 1);
            }
            
            // Init final cluster ops
            PostDemolitionCluster (scr, detachShards);
        }

        // Setup cluster by one slice plane side shards. Connectivity check
        static void SetupPlaneShards (RayfireRigid scr, List<RFShard> clusterShards, List<RFShard> detachShards)
        {
            // Reinit cluster neibs
            if (clusterShards.Count >= 2)
            {
                RFCluster cluster = new RFCluster();
                cluster.id           = 2;
                cluster.shards       = clusterShards;
                cluster.demolishable = true;
                for (int i = 0; i < cluster.shards.Count; i++)
                    cluster.shards[i].cluster = cluster;
                
                // Set main cluster
                cluster.mainCluster = scr.clusterDemolition.cluster;
                
                // Reset neibs 
                RFShard.ReinitNeibs (cluster.shards);
                
                // Remove not connected solo shards, shards without neibs
                for (int i = cluster.shards.Count - 1; i >= 0; i--)
                {
                    if (cluster.shards[i].neibShards.Count == 0)
                    {
                        detachShards.Add (cluster.shards[i]);
                        cluster.shards[i].cluster = null;
                        cluster.shards.RemoveAt (i);
                    }
                }

                // Cluster had only solo shards
                if (cluster.shards.Count == 0)
                    return;

                // Check connectivity, store all less not connected clusters as child cluster
                RFCluster.ConnectivityCheck (cluster);
                
                // Cluster is not connected. Set biggest child cluster shards to original cluster. Cant be 1 child cluster here
                RFCluster.ReduceChildClusters (cluster);
                
                // Collect original or not connected clusters
                scr.clusterDemolition.cluster.childClusters.Add (cluster);
                if (cluster.HasChildClusters == true)
                {
                    for (int c = 0; c < cluster.childClusters.Count; c++)
                        scr.clusterDemolition.cluster.childClusters.Add (cluster.childClusters[c]);
                    cluster.childClusters.Clear();
                }
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Demolition /Slicing common
        /// /////////////////////////////////////////////////////////
        
        // Demolish connected cluster by colliders
        static void ClusterizeDetachShards (RayfireRigid scr, List<RFShard> detachShards, int clusterAmount, int sortType)
        {
            // CLustering disabled or one solo shard. Nothing to clusterize
            if (scr.clusterDemolition.shardArea == 100 || detachShards.Count <= 1)
                return;

            // TODO complete clusterization
            if (scr.clusterDemolition.shardArea == 0)
            {
                
            }

            // Amount of solo shards
            int centerShardsAmount = detachShards.Count * scr.clusterDemolition.shardArea / 100;
            
            // Not enough solo shards for clustering
            if (detachShards.Count - centerShardsAmount <= 1)
                return;
            
            // Shards less than clusters needed or group is too small to be clustered. Stop
            if (detachShards.Count <= clusterAmount)
                return;
 
            // Set up child cluster
            if (scr.clusterDemolition.cluster.childClusters == null)
                scr.clusterDemolition.cluster.childClusters = new List<RFCluster>();
            int startIndex = scr.clusterDemolition.cluster.childClusters.Count;

            // Preserve center shards
            List<RFShard> center = null;
            if (centerShardsAmount > 0)
            {
                // Get center shards
                if (sortType == 0)
                    center = RFShard.SortByDistanceToPoint (detachShards, scr.limitations.contactVector3, centerShardsAmount);
                else if (sortType == 1)
                    center = RFShard.SortByDistanceToPlane (detachShards, scr.limitations.contactVector3, scr.limitations.contactNormal, centerShardsAmount);
                
                if (center != null)
                {
                    // Remove center shards from detach shards before slice them
                    for (int i = detachShards.Count - 1; i >= 0; i--)
                        if (center.Contains (detachShards[i]) == true)
                            detachShards.RemoveAt (i);
                    
                    // Change center shards cluster to reinit detach shards neibs
                    for (int i = 0; i < center.Count; i++)
                        center[i].cluster = scr.clusterDemolition.cluster;
                }
            }
            
            // Separate group of shards to several child clusters
            DivideAllShards (scr.clusterDemolition.cluster, detachShards, clusterAmount);
            
            // Disable colliders TODO prevent empty clusters
            // DetachEdgeShards (scr, scr.clusterDemolition.cluster, detachShards, scr.clusterDemolition.edgeShardArea);
            
            // Detach shards with one neib from clusters
            DetachOneNeibShards (scr.clusterDemolition.cluster.childClusters, detachShards, centerShardsAmount, startIndex);

            // Add center shards back
            if (center != null)
            {
                // Nullify center shards cluster back
                for (int i = 0; i < center.Count; i++)
                    center[i].cluster = null;
                detachShards.AddRange (center);
            }
        }

        // Create runtime clusters
        static void CreateRigidShards (RayfireRigid scr, List<RFShard> detachShards)
        {
            // No shards to create
            if (detachShards.Count == 0)
                return;

            Transform tm = scr.transform.parent;
            if (scr.clusterDemolition.cluster.mainCluster != null && scr.clusterDemolition.cluster.mainCluster.tm != null)
                tm = scr.clusterDemolition.cluster.mainCluster.tm.parent;
            
            // Add rigid component to detached children
            AddRigidComponent (scr, detachShards, tm);
        }
        
        // Create Rigid Child cluster
        static void CreateChildClusters (RayfireRigid scr, List<RFCluster> childClusters)
        {
            // No child clusters to create
            if (childClusters == null || childClusters.Count == 0)
                return;
            
            // Create child clusters
            for (int i = 0; i < childClusters.Count; i++)
            {
                // Set demolishable state
                childClusters[i].demolishable = scr.clusterDemolition.demolishable;
                
                // Create cluster
                CreateClusterRuntime (scr, childClusters[i]);
            }
        }

        // Demolish connected cluster by colliders
        static void UpdateOriginalCluster (RayfireRigid scr)
        {
            // All shards were detached. Set demolished state
            if (scr.clusterDemolition.cluster.shards.Count == 0)
            {
                scr.physics.rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                scr.physics.rigidBody.isKinematic = true;
                scr.limitations.demolished = true;
                
                // Remove from minor cluster list
                if (scr.objectType == ObjectType.ConnectedCluster)
                    if (scr.clusterDemolition.cluster.mainCluster != null && scr.clusterDemolition.cluster.mainCluster.rigid != null)
                        scr.clusterDemolition.cluster.mainCluster.rigid.clusterDemolition.minorClusters.Remove (scr.clusterDemolition.cluster);
                return;
            }

            // Original cluster shards not going to be changed anymore. Reinit colliders list
            RFPhysic.CollectClusterColliders (scr, scr.clusterDemolition.cluster);
            
            // Update mass
            RFPhysic.SetDensity (scr);
            
            // Set dynamic if cluster inherit from kinematik/inactive but has no uny shards
            if (scr.simulationType == SimType.Kinematic || scr.simulationType == SimType.Inactive)
                if(scr.activation.byConnectivity == true && scr.clusterDemolition.cluster.UnyieldingByShard == false)
                    scr.Activate();
            
            // Cluster was demolished but not completely, reuse what left 
            scr.limitations.birthTime = Time.time;
            
            // Update reduced bound
            scr.clusterDemolition.cluster.bound = RFCluster.GetShardsBound (scr.clusterDemolition.cluster.shards);
            scr.limitations.bboxSize = scr.clusterDemolition.cluster.bound.size.magnitude;
            
            // Reset original cluster center of mass because of detached colliders: solo shards and child clusters
            scr.physics.rigidBody.ResetCenterOfMass();
        }
        
        // Create runtime clusters
        public static void CreateClusterRuntime (RayfireRigid scr, RFCluster cluster)
        {
            if (cluster.shards.Count == 1)
                Debug.Log ("Solo cluster warning");

            // Register in main cluster
            if (scr.objectType == ObjectType.ConnectedCluster)
            {
                if (cluster.mainCluster != null && cluster.mainCluster.rigid != null)
                {
                    if (cluster.mainCluster.rigid.clusterDemolition.minorClusters == null)
                        cluster.mainCluster.rigid.clusterDemolition.minorClusters = new List<RFCluster>();
                    cluster.mainCluster.rigid.clusterDemolition.minorClusters.Add (cluster);
                }
            }

            // Set bound if has not
            if (cluster.bound.size.magnitude == 0)
            {
                if (scr.objectType == ObjectType.ConnectedCluster)
                    cluster.bound = RFCluster.GetShardsBound (cluster.shards);
                else if (scr.objectType == ObjectType.NestedCluster)
                    cluster.bound = RFCluster.GetClusterBound(cluster);
            }
            
            // Create root for left children
            if (cluster.tm == null)
            {
                GameObject leftRoot = new GameObject(scr.gameObject.name + nameApp + cluster.id);
                cluster.tm = leftRoot.transform;
                
                cluster.tm.position = cluster.shards[0].tm.position;
                cluster.tm.rotation = scr.transForm.rotation;

                // Copy layer and tag
                leftRoot.layer = scr.gameObject.layer;
                leftRoot.tag = scr.gameObject.tag;
            }
            
            // Set parent
            cluster.tm.parent = scr.transForm.parent;

            // Parent to main root. Nested cluster already has all shards rooted
            if (scr.objectType == ObjectType.ConnectedCluster)
                for (int s = 0; s < cluster.shards.Count; s++)
                    cluster.shards[s].tm.parent = cluster.tm;
            
            // Check if already has rigid but it was not referenced
            if (cluster.rigid == null)
                cluster.rigid = cluster.tm.gameObject.GetComponent<RayfireRigid>();
            
            // Add rigid to object
            if (cluster.rigid == null)
                cluster.rigid = cluster.tm.gameObject.AddComponent<RayfireRigid>();
            
            // Collect fragment
            if (scr.fragments == null)
                scr.fragments = new List<RayfireRigid>();
            scr.fragments.Add (cluster.rigid);

            // Copy properties from parent to fragment node
            scr.CopyPropertiesTo (cluster.rigid);

            // Copy particles
            RFParticles.CopyParticles (scr, cluster.rigid);
            
            // Set to mesh 
            // target.objectType = ObjectType.ConnectedCluster;
            cluster.rigid.physics.colliderType = RFColliderType.Mesh;

            // Set dynamic if cluster inherit from kinematik/inactive but has no uny shards
            if (cluster.rigid.simulationType == SimType.Kinematic || cluster.rigid.simulationType == SimType.Inactive)
                if(cluster.rigid.activation.byConnectivity == true && cluster.UnyieldingByShard == false)
                    cluster.rigid.simulationType = SimType.Dynamic;
            
            // Set demolishable state for detached area clusters
            cluster.rigid.demolitionType = cluster.demolishable == true 
                ? DemolitionType.Runtime 
                : DemolitionType.None;
            
            // Do not destroy fragment because cluster could be reused 
            if (scr.reset.action == RFReset.PostDemolitionType.DeactivateToReset)
                cluster.rigid.reset.action = RFReset.PostDemolitionType.DeactivateToReset;
            
            // Set cluster
            cluster.initialized = true;
            cluster.rigid.clusterDemolition.cluster = cluster;

            // Set range for area and size
            if (cluster.rigid.objectType == ObjectType.ConnectedCluster)
                RFCollapse.SetRangeData (cluster.rigid.clusterDemolition.cluster, cluster.rigid.clusterDemolition.percentage, cluster.rigid.clusterDemolition.seed);
            
            // Set colliders list
            RFPhysic.CollectClusterColliders (cluster.rigid, cluster.rigid.clusterDemolition.cluster);

            
            // IMPORTANT. Set mesh collider convex for gun impact detection
            //if (scr.simulationType == SimType.Kinematic)
            // {
            //     for (int i = 0; i < cluster.shards.Count; i++)
            //     {
            //         if (cluster.shards[i].col is MeshCollider)
            //         {
            //             ((MeshCollider)cluster.shards[i].col).convex = true;
            //         }
            //     }
            // }

            // Turn on
            cluster.rigid.Initialize();
        }

        /// /////////////////////////////////////////////////////////
        /// Cluster Init
        /// /////////////////////////////////////////////////////////
        
        // Separate group of shards to several clusters by half
        static void DivideAllShards (RFCluster cluster, List<RFShard> detachShards, int amount)
        {
            // Get starting search index to exclude first child cluster from Connectivity check
            int startIndex = cluster.childClusters.Count;
            
            // Remove neib shards which still belongs to some cluster. Detach shards has no cluster
            RFShard.ReinitNeibs (detachShards);
            
            // Create base child cluster with detach shards
            RFCluster baseCLuster = new RFCluster();
            
            // Set main cluster
            baseCLuster.mainCluster = cluster.mainCluster == null 
                ? cluster 
                : cluster.mainCluster;

            // Set uniq id after main cluster defined
            baseCLuster.id = RFCluster.GetUniqClusterId (baseCLuster);
            
            // Set shards
            for (int i = detachShards.Count - 1; i >= 0; i--)
            {
                if (detachShards[i].neibShards.Count > 0)
                {
                    baseCLuster.shards.Add (detachShards[i]);
                    detachShards[i].cluster = baseCLuster;
                    detachShards.RemoveAt (i);
                }
            }
            cluster.childClusters.Add (baseCLuster);
            
            // Base cluster neib check
            RFShard.ReinitNeibs (baseCLuster.shards);
            
            // SLice to half amount - 1 times
            for (int i = 0; i < amount - 1; i++)
            {
                // Get biggest child cluster
                int biggestInd = 0;
                int biggestAmount = 0;
                for (int b = startIndex; b < cluster.childClusters.Count; b++)
                    if (cluster.childClusters[b].shards.Count > biggestAmount)
                    {
                        biggestInd = b;
                        biggestAmount = cluster.childClusters[b].shards.Count;
                    }

                // Biggest child cluster is very small. Stop
                if (cluster.childClusters[biggestInd].shards.Count < 4)
                    break;
                
                // Slice biggest group
                DivideShards (cluster, cluster.childClusters[biggestInd]);

                // Biggest cluster was not separated. stop
                if (biggestAmount == cluster.childClusters[biggestInd].shards.Count)
                    break;
            }
            
            // Check new child clusters for solo shards
            for (int c = cluster.childClusters.Count - 1; c >= startIndex; c--)
            {
                for (int s = cluster.childClusters[c].shards.Count - 1; s >= 0; s--)
                {
                    if (cluster.childClusters[c].shards[s].neibShards.Count == 0)
                    {
                        detachShards.Add (cluster.childClusters[c].shards[s]);
                        cluster.childClusters[c].shards[s].cluster = null;
                        cluster.childClusters[c].shards.RemoveAt (s);
                    }
                }

                // Remove clusters with no shards. All was solo and removed
                if (cluster.childClusters[c].shards.Count == 0)
                    cluster.childClusters.RemoveAt (c);
            }
            
            // Check for connectivity of child cluster
            for (int c = startIndex; c < cluster.childClusters.Count; c++)
            {
                // Connectivity
                RFCluster.ConnectivityCheck (cluster.childClusters[c]);

                // Cluster is not connected. Set biggest child cluster shards to original cluster. Cant be 1 child cluster here
                RFCluster.ReduceChildClusters (cluster.childClusters[c]);
            }

            // Set their child cluster as current child cluster and clear list
            for (int c = cluster.childClusters.Count - 1; c >= startIndex; c--)
            {
                if (cluster.childClusters[c].HasChildClusters == true)
                {
                    cluster.childClusters.AddRange (cluster.childClusters[c].childClusters);
                    cluster.childClusters[c].childClusters = null;
                } 
            }
        }

        // Separate group of shards to half. Do not return solo shards
        static void DivideShards (RFCluster cluster, RFCluster childCluster)
        {
            // Get group bound
            childCluster.bound = RFCluster.GetShardsBound (childCluster.shards);
            
            // Get slice plane at middle of longest bound edge
            Plane plane = RFShard.GetSlicePlane (childCluster.bound);
            
            // Separate by plane and collect indexes of separated shards
            List<int> indexList = new List<int>();
            for (int i = 0; i < childCluster.shards.Count; i++)
                if (plane.GetSide (childCluster.shards[i].tm.position) == true)
                    indexList.Add (i);
            
            // One of the group contains only one shard. Group is too small. Stop.
            if (indexList.Count <= 1 || indexList.Count > childCluster.shards.Count - 2)
                return;

            // Create new group list and remove from input list
            RFCluster newChildCluster = new RFCluster();
            
            // Set main cluster
            newChildCluster.mainCluster = cluster.mainCluster == null 
                ? cluster 
                : cluster.mainCluster;
            
            // Set uniq id after main cluster defined
            newChildCluster.id = RFCluster.GetUniqClusterId (newChildCluster);

            // Set shards
            newChildCluster.shards = new List<RFShard>();
            for (int i = indexList.Count - 1; i >= 0; i--)
            {
                newChildCluster.shards.Add (childCluster.shards[indexList[i]]);
                childCluster.shards[indexList[i]].cluster = newChildCluster;
                childCluster.shards.RemoveAt (indexList[i]);
            }
            
            // Collect new cluster
            cluster.childClusters.Add (newChildCluster);
            
            // Remove neib shards which still belongs to some cluster. Detach solo shards
            RFShard.ReinitNeibs (newChildCluster.shards);
            RFShard.ReinitNeibs (childCluster.shards);
        }
        
        // Detach shards from child clusters at edges TODO
        static void DetachEdgeShards (RayfireRigid scr, RFCluster cluster, List<RFShard> detachShards, int edgeShardArea)
        {
            if (edgeShardArea == 0)
                return;

            for (int i = 0; i < cluster.childClusters.Count; i++)
            {
                if (cluster.childClusters[i].shards.Count >= 5)
                {
                    int amount = cluster.childClusters[i].shards.Count * edgeShardArea / 100;
                    if (amount > 0)
                    {
                        // Debug.Log (amount);
                        // TODO detach in better way: farthest, lowest shard area, ect
                        for (int j = cluster.childClusters[i].shards.Count - 1; j >= 0; j--)
                        {
                            if (cluster.childClusters[i].shards[j].neibShards.Count < cluster.childClusters[i].shards[j].nAm)
                            {
                                detachShards.Add (cluster.childClusters[i].shards[j]);
                                cluster.childClusters[i].shards[j].cluster = null;
                                cluster.childClusters[i].shards.RemoveAt (j);
                            }
                        }

                        // Reinit cluster
                        RFShard.ReinitNeibs (cluster.childClusters[i].shards);
                    }
                }
            }
        }

        // Detach one shard with one neib from clusters
        static void DetachOneNeibShards (List<RFCluster> childClusters, List<RFShard> detachShards, int edgeAmount, int startIndex)
        {
            // Check every detached child cluster
            while (edgeAmount >= detachShards.Count)
            {
                // Detach one shard with one neib from every cluster
                int detachAmount = detachShards.Count;
                for (int c = childClusters.Count - 1; c >= startIndex; c--)
                {
                    // Detach one shard with one neib
                    DetachOneNeibShard (childClusters[c], detachShards);

                    // Enough edge shards
                    if (edgeAmount >= detachShards.Count)
                        return;
                }

                // No cluster with shard with one neib. Stop while
                if (detachShards.Count == detachAmount)
                    return;
            }
        }

        // Detach one shard with one neib
        static void DetachOneNeibShard (RFCluster cls, List<RFShard> detachShards)
        {
            if (cls.shards.Count >= 3)
            {
                // Check every shard
                for (int s = cls.shards.Count - 1; s >= 0; s--)
                {
                    // Check amount of neibs
                    if (cls.shards[s].neibShards.Count == 1)
                    {
                        // Collect shard with one neib
                        detachShards.Add (cls.shards[s]);
                        cls.shards[s].cluster = null;
                        
                        // Clean up neib shard
                        for (int i = cls.shards[s].neibShards[0].neibShards.Count - 1; i >= 0; i--)
                        {
                            if (cls.shards[s].neibShards[0].neibShards[i].cluster == null)
                            {
                                cls.shards[s].neibShards[0].neibShards.RemoveAt (i);
                                cls.shards[s].neibShards[0].nArea.RemoveAt (i);
                                cls.shards[s].neibShards[0].nIds.RemoveAt (i);
                            }
                        }
                        
                        // Remove from cluster
                        cls.shards.RemoveAt (s);
                        
                        // Enough detach shards
                        if (cls.shards.Count <= 2)
                            return;
                    }
                }
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Add rigid
        /// /////////////////////////////////////////////////////////
        
        // Add rigid component to transform list
        static void AddRigidComponent (RayfireRigid scr, List<RFShard> shardList,Transform parent)
        {
            for (int i = 0; i < shardList.Count; i++)
                AddRigidComponent (scr, shardList[i], parent, shardList[i].uny);
        }
        
        // Add rigid component to transform list
        static void AddRigidComponent (RayfireRigid scr, RFShard shard, Transform parent, bool uny = false)
        {
            // TODO check if already has rigid
            
            // Set parent
            shard.tm.parent = parent;

            // Add new component if shard has not rigid
            if (shard.rigid == null)
                shard.rigid = shard.tm.gameObject.AddComponent<RayfireRigid>();
            
            // Skip excluded                                    
            if (shard.rigid.physics.exclude == true)
                return;

            // Collect fragment
            scr.fragments.Add (shard.rigid);

            // Copy properties from parent to fragment node
            scr.CopyPropertiesTo (shard.rigid);

            // Set unyielding
            shard.rigid.activation.unyielding = uny;
            
            // Copy particles
            RFParticles.CopyParticles (scr, shard.rigid);
            
            // Set to mesh 
            shard.rigid.objectType = ObjectType.Mesh;
            shard.rigid.physics.colliderType = RFColliderType.Mesh;
            
            // Set dynamic if cluster inherit from kinematik/inactive but has no uny shards
            if (shard.rigid.simulationType == SimType.Kinematic || shard.rigid.simulationType == SimType.Inactive)
                if(shard.rigid.activation.byConnectivity == true && uny == false)
                    shard.rigid.simulationType = SimType.Dynamic;

            // Turn off demolition for solo fragments
            if (scr.clusterDemolition.shardDemolition == false)
                shard.rigid.demolitionType = DemolitionType.None;
            
            // Do not destroy fragment because cluster could be reused 
            if (scr.reset.action == RFReset.PostDemolitionType.DeactivateToReset)
                shard.rigid.reset.action = RFReset.PostDemolitionType.DeactivateToReset;
            
            // Update depth level and amount
            shard.rigid.limitations.currentDepth = scr.limitations.currentDepth + 1;

            // Scale factor
            if (scr.clusterDemolition.scaleFactor != 1f)
                shard.rigid.transform.localScale *= scr.clusterDemolition.scaleFactor;
            
            // Turn on
            shard.rigid.Initialize();
            
            // IMPORTANT. Set mesh collider convex for gun impact detection
            if (shard.rigid.objectType == ObjectType.Mesh)
                if (shard.rigid.physics.meshCollider != null)
                    ((MeshCollider)shard.rigid.physics.meshCollider).convex = true;
        }

        /// /////////////////////////////////////////////////////////
        /// Get
        /// /////////////////////////////////////////////////////////

        // All shards should be clusterized to one cluster. Stop
        static bool SameClusterCheck(RayfireRigid scr, List<RFShard> detachShards, int shardAmount, int clusterAmount)
        {
            if (shardAmount == detachShards.Count && clusterAmount == 1)
            {
                Debug.Log ("same");
                
                scr.limitations.demolitionShould = false;
                scr.clusterDemolition.cluster.shards = detachShards;
                for (int i = 0; i < scr.clusterDemolition.cluster.shards.Count; i++)
                    scr.clusterDemolition.cluster.shards[i].cluster = scr.clusterDemolition.cluster;

                RFPhysic.CollectClusterColliders (scr, scr.clusterDemolition.cluster);
                return true;
            }

            return false;
        }
        
        // Had child cluster
        public bool HasChildClusters { get { return cluster.childClusters != null && cluster.childClusters.Count > 0; } }
        public bool HasMinorClusters { get { return minorClusters != null && minorClusters.Count > 0; } }
        
    }
}