using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace RayFire
{
    [Serializable]
    public class RFCluster : IComparable<RFCluster>
    {
        public                   int           id;
        public                   Transform     tm;
        [HideInInspector] public int           depth;
        [HideInInspector] public Vector3       pos;
        [HideInInspector] public Quaternion    rot;
        [HideInInspector] public Bounds        bound;
        [HideInInspector] public bool          demolishable;
        [HideInInspector] public RayfireRigid  rigid;
        [HideInInspector] public List<RFShard> shards;

        [HideInInspector] public float areaCollapse;
        [HideInInspector] public float minimumArea;
        [HideInInspector] public float maximumArea;
        [HideInInspector] public float sizeCollapse;
        [HideInInspector] public float minimumSize;
        [HideInInspector] public float maximumSize;
        [HideInInspector] public int   randomCollapse;
        [HideInInspector] public int   randomSeed;
        
        // Non serialized
        [NonSerialized] public bool            initialized;
        [NonSerialized] public RFCluster       mainCluster;
        [NonSerialized] public List<RFCluster> childClusters;

        // Use later
        [NonSerialized] public List<RFCluster> neibClusters;
        [NonSerialized] public List<float>     neibArea;
        [NonSerialized] public List<float>     neibPerc;

        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////
        
        // Constructor
        public RFCluster()
        {
            id = -1;
            tm = null;
            depth = 0;
            initialized = false;
            demolishable = true;
            shards = new List<RFShard>();
            rigid = null;
            
            // Collapse
            minimumArea = 0;
            maximumArea = 0;
            minimumSize = 0;
            maximumSize = 0;
            randomCollapse = 0;
            
            // Non serialized
            mainCluster = null;
            childClusters = null;
            
            // neibClusters = new List<RFCluster>();
            // neibArea = new List<float>();
            // neibPerc = new List<float>();
        }
        
        // Constructor
        public RFCluster (RFCluster source)
        {
            id           = source.id;
            tm           = source.tm;
            depth        = source.depth;
            pos          = source.pos;
            rot          = source.rot;
            bound        = source.bound;
            rigid        = source.rigid;
            demolishable = source.demolishable;
            
            areaCollapse   = source.areaCollapse;
            minimumArea    = source.minimumArea;
            maximumArea    = source.maximumArea;
            sizeCollapse   = source.sizeCollapse;
            minimumSize    = source.minimumSize;
            maximumSize    = source.maximumSize;
            randomCollapse = source.randomCollapse;
            randomSeed     = source.randomSeed;
            
            // Remapped shards
            shards       = new List<RFShard>();
            for (int i = 0; i < source.shards.Count; i++)
                shards.Add (new RFShard (source.shards[i]));
            
            // Copy child clusters
            if (source.HasChildClusters == true)
            {
                childClusters = new List<RFCluster>();
                for (int i = 0; i < source.childClusters.Count; i++)
                    childClusters.Add (new RFCluster(source.childClusters[i]));
            }
            
            // Set false to reinit it in SaveBackup after all shards will be copied
            initialized    = false;
        }
        
        // Compare by size
        public int CompareTo(RFCluster otherCluster)
        {
            float thisSize = bound.size.magnitude;
            float otherSize = otherCluster.bound.size.magnitude;
            if (thisSize > otherSize)
                return -1;
            if (thisSize < otherSize)
                return 1;
            return 0;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////
        
        // Reinit non serialized fields in case of prefab use
        public static void InitCluster (RayfireRigid scr, RFCluster cluster)
        {
            if (cluster.initialized == false)
            {
                // Reinit connected cluster shards non serialized fields
                if (scr.objectType == ObjectType.ConnectedCluster)
                {
                    for (int s = 0; s < cluster.shards.Count; s++)
                    {
                        cluster.shards[s].cluster  = cluster;
                        cluster.shards[s].neibShards = new List<RFShard>();
                        for (int n = 0; n < cluster.shards[s].nIds.Count; n++)
                            cluster.shards[s].neibShards.Add (cluster.shards[cluster.shards[s].nIds[n]]);
                    }
                }

                // Unfold nested cluster non serialized child clusters
                if (scr.objectType == ObjectType.NestedCluster)
                    UnfoldNestedCluster (scr, cluster);
                
                cluster.initialized = true;
            }
        }

        // Unfold nested cluster
        static void UnfoldNestedCluster (RayfireRigid scr, RFCluster cluster)
        {
            // Set shard cluster
            for (int s = 0; s < cluster.shards.Count; s++)
                cluster.shards[s].cluster = cluster;
            
            // Check all minor clusters and find child clusters among them
            for (int i = 0; i < scr.clusterDemolition.minorClusters.Count; i++)
            {
                if (cluster.tm == scr.clusterDemolition.minorClusters[i].tm.parent)
                {
                    // Check and create cluster list
                    if (cluster.HasChildClusters == false)
                        cluster.childClusters = new List<RFCluster>();
                    
                    // Set main cluster
                    scr.clusterDemolition.minorClusters[i].mainCluster = scr.clusterDemolition.cluster;
                    
                    // Collect
                    cluster.childClusters.Add (scr.clusterDemolition.minorClusters[i]);
                }
            }

            // Repeat for child clusters
            if (cluster.HasChildClusters == true)
                for (int i = 0; i < cluster.childClusters.Count; i++)
                    UnfoldNestedCluster (scr, cluster.childClusters[i]);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Connectivity
        /// /////////////////////////////////////////////////////////

        // Connectivity check
        public static void ConnectivityUnyCheck (RFCluster cluster)
        {
            // Set up child cluster
            if (cluster.childClusters != null)
                cluster.childClusters.Clear();
            
            // Not enough shards to check for connectivity
            if (cluster.shards.Count <= 1)
                return;
            
            // Check all shards and collect new clusters
            int shardsAmount = cluster.shards.Count;
            
            // Check all shards for connectivity but keep uny clusters in main cluster
            List<int> checkedShardsIds = new List<int>();
            List<RFShard> checkShards = new List<RFShard>();
            List<RFShard> newClusterShards = new List<RFShard>();
            for (int s = cluster.shards.Count - 1; s >= 0; s--)
            {
                // Skip checked shards
                if (checkedShardsIds.Contains (cluster.shards[s].id) == true)
                    continue;
                
                // Collect
                checkedShardsIds.Add (cluster.shards[s].id);
                
                // New possible cluster. Create new bacouse applied to cluster
                newClusterShards.Clear();
                newClusterShards.Add (cluster.shards[s]);
                
                // Shards in possible connection
                checkShards.Clear();
                checkShards.Add (cluster.shards[s]);

                // Collect by neibs
                while (checkShards.Count > 0)
                {
                    // Add neibs to check If neib among current cluster shards And not already collected
                    for (int n = 0; n < checkShards[0].neibShards.Count; n++)
                    {
                        if (newClusterShards.Contains(checkShards[0].neibShards[n]) == false)
                        {
                            checkShards.Add(checkShards[0].neibShards[n]);
                            newClusterShards.Add (checkShards[0].neibShards[n]);
                            checkedShardsIds.Add (checkShards[0].neibShards[n].id); 
                        }
                    }

                    // Remove checked
                    checkShards.RemoveAt(0);
                }
                
                // Child cluster connected
                if (shardsAmount == newClusterShards.Count)
                    break;
                
                // Start over if connected shards has uny
                if (RFShard.UnyieldingByShard(newClusterShards) == true)
                    continue;
                
                // Child cluster not connected. Create new cluster and add to parent
                RFCluster newCluster = new RFCluster();
                for (int i = 0; i < newClusterShards.Count; i++)
                    newCluster.shards.Add (newClusterShards[i]);
                newCluster.demolishable = cluster.demolishable;
                
                // Set main cluster
                if (cluster.mainCluster == null)
                    newCluster.mainCluster = cluster;
                else
                    newCluster.mainCluster = cluster.mainCluster;
                
                // Set uniq id after main cluster defined
                newCluster.id = GetUniqClusterId (newCluster);
                
                // Set shards cluster to new cluster
                for (int i = 0; i < newCluster.shards.Count; i++)
                    newCluster.shards[i].cluster = newCluster;
                
                // Set up child cluster
                if (cluster.childClusters == null)
                    cluster.childClusters = new List<RFCluster>();
                cluster.childClusters.Add(newCluster);
            }
            
            // Remove new child clusters shards from original cluster shards list before repeat while cycle
            if (cluster.childClusters != null && cluster.childClusters.Count > 0)
                for (int i = cluster.shards.Count - 1; i >= 0; i--)
                    if (cluster.shards[i].cluster != cluster)
                        cluster.shards.RemoveAt(i);
            
            checkedShardsIds.Clear();

            // No uny shards in cluster
            if (cluster.shards.Count == 0)
            {
                // Cluster is not connected. If not main cluster then set biggest child cluster shards to original cluster. 
                ReduceChildClusters (cluster); 
            }
        }
        
        // Connectivity check
        public static void ConnectivityCheck (RFCluster cluster)
        {
            // Set up child cluster
            if (cluster.childClusters != null)
                cluster.childClusters.Clear();
            
            // Not enough shards to check for connectivity
            if (cluster.shards.Count <= 1)
                return;
            
            // Check all shards and collect new clusters
            int shardsAmount = cluster.shards.Count;
            
            // Iterate through shards and check connectivity
            List<RFShard> checkShards = new List<RFShard>();
            List<RFShard> newClusterShards = new List<RFShard>();
            while (cluster.shards.Count > 0)
            {
                // Lists
                checkShards.Clear();
                checkShards.Add(cluster.shards[0]);
                
                newClusterShards.Clear();
                newClusterShards.Add (cluster.shards[0]);

                // Collect by neibs
                while (checkShards.Count > 0)
                {
                    // Add neibs to check If neib among current cluster shards And not already collected
                    for (int n = 0; n < checkShards[0].neibShards.Count; n++)
                    {
                        if (newClusterShards.Contains(checkShards[0].neibShards[n]) == false)
                        {
                            checkShards.Add(checkShards[0].neibShards[n]);
                            newClusterShards.Add(checkShards[0].neibShards[n]);
                        }
                    }

                    // Remove checked
                    checkShards.RemoveAt(0);
                }
                
                // Child cluster connected
                if (shardsAmount == newClusterShards.Count)
                    break;
                
                // Child cluster not connected. Create new cluster and add to parent
                RFCluster newCluster = new RFCluster();
                for (int i = 0; i < newClusterShards.Count; i++)
                    newCluster.shards.Add (newClusterShards[i]);
                newCluster.demolishable = cluster.demolishable;

                // Set main cluster
                if (cluster.mainCluster == null)
                    newCluster.mainCluster = cluster;
                else
                    newCluster.mainCluster = cluster.mainCluster;
                
                // Set uniq id after main cluster defined
                newCluster.id = GetUniqClusterId (newCluster);
                
                // Set shards cluster to new cluster
                for (int i = 0; i < newCluster.shards.Count; i++)
                    newCluster.shards[i].cluster = newCluster;
                
                // Set up child cluster
                if (cluster.childClusters == null)
                    cluster.childClusters = new List<RFCluster>();
                cluster.childClusters.Add(newCluster);
                
                // Remove new cluster shards from original cluster shards list before repeat while cycle
                for (int i = cluster.shards.Count - 1; i >= 0; i--)
                    if (cluster.shards[i].cluster != cluster)
                        cluster.shards.RemoveAt(i);
            }
        }
        
        // Set biggest child cluster shards to original cluster. Cant be 1 child cluster
        public static void ReduceChildClusters (RFCluster cluster)
        {
            // Cluster has child cluster
            if (cluster.childClusters != null && cluster.childClusters.Count > 0)
            {
                // Get biggest cluster
                int biggestInd = GetBiggestCluster (cluster.childClusters);

                // Set biggest cluster shards to original cluster shards to reuse it
                cluster.shards = cluster.childClusters[biggestInd].shards;

                // Set shards cluster back to original cluster
                for (int i = 0; i < cluster.shards.Count; i++)
                    cluster.shards[i].cluster = cluster;

                // Remove biggest cluster from child clusters
                cluster.childClusters.RemoveAt (biggestInd);
            }
        }

        // Unyielding Connectivity check
        public static void ConnectivityCheckUny (RFCluster cluster)
        {
            // NO shards
            if (cluster.shards.Count == 0)
                return;

            // Reset child clusters to store not connected
            cluster.childClusters = new List<RFCluster>();
            
            // Get all uny/regular shards
            List<RFShard> regShards = new List<RFShard>();
            List<RFShard> checkShards = new List<RFShard>();
            foreach (var shard in cluster.shards)
            {
                if (shard.rigid.activation.unyielding == true)
                    checkShards.Add (shard);
                else
                    regShards.Add (shard);
            }

            // Nothing to activate. Everything activated.
            if (regShards.Count == 0)
            {
                cluster.shards.Clear(); // TODO can't change uny to off later in this case, shard list ruined
                return;
            }

            // Remove all uny shards from regular shards and left not connected
            while (checkShards.Count > 0)
            {
                foreach (RFShard neibShard in checkShards[0].neibShards)
                {
                    if (regShards.Contains(neibShard) == true)
                    {
                        regShards.Remove (neibShard);
                        checkShards.Add (neibShard);
                    }
                }
                checkShards.RemoveAt(0);
            }

            // Update input cluster
            foreach (var shard in regShards)
                cluster.shards.Remove (shard);

            // Nothing to activate
            if (regShards.Count == 0)
                return;
            
            // Set not connected shards as child cluster shards to be activated
            RFCluster newCluster = new RFCluster();
            newCluster.shards = regShards;
            cluster.childClusters.Add (newCluster);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Bounds
        /// /////////////////////////////////////////////////////////

        // Get all children bounds
        public static Bounds GetChildrenBound(Transform tm)
        {
            // Collect all children transforms
            List<Renderer> renderers = tm.GetComponentsInChildren<Renderer>().ToList();

            // Get list of bounds
            List<Bounds> bounds = new List<Bounds>();
            for (int i = 0; i < renderers.Count; i++)
                bounds.Add(renderers[i].bounds);
            
            return GetBoundsBound(bounds);
        }
        
        // Get all children bounds
        public static Bounds GetClusterBound(RFCluster cluster)
        {
            // Get list of bounds
            List<Bounds> bounds = new List<Bounds>();
            for (int i = 0; i < cluster.shards.Count; i++)
                bounds.Add(cluster.shards[i].bnd);
            for (int i = 0; i < cluster.childClusters.Count; i++)
                bounds.Add(cluster.childClusters[i].bound);
            
            return GetBoundsBound(bounds);
        }

        // Get bound by list of bounds
        public static Bounds GetBoundsBound(List<Bounds> bounds)
        {
            // new bound
            Bounds bound = new Bounds();

            // No mesh renderers
            if (bounds.Count == 0)
            {
                Debug.Log("GetBoundsBound error");
                return bound;
            }

            // Basic bounds min and max values
            float minX = bounds[0].min.x;
            float minY = bounds[0].min.y;
            float minZ = bounds[0].min.z;
            float maxX = bounds[0].max.x;
            float maxY = bounds[0].max.y;
            float maxZ = bounds[0].max.z;

            // Compare with other bounds
            if (bounds.Count > 1)
            {
                for (int i = 1; i < bounds.Count; i++)
                {
                    if (bounds[i].min.x < minX)
                        minX = bounds[i].min.x;
                    if (bounds[i].min.y < minY)
                        minY = bounds[i].min.y;
                    if (bounds[i].min.z < minZ)
                        minZ = bounds[i].min.z;

                    if (bounds[i].max.x > maxX)
                        maxX = bounds[i].max.x;
                    if (bounds[i].max.y > maxY)
                        maxY = bounds[i].max.y;
                    if (bounds[i].max.z > maxZ)
                        maxZ = bounds[i].max.z;
                }
            }

            // Get center
            bound.center = new Vector3((maxX - minX) / 2f, (maxY - minY) / 2f, (maxZ - minZ) / 2f);

            // Get min and max vectors
            bound.min = new Vector3(minX, minY, minZ);
            bound.max = new Vector3(maxX, maxY, maxZ);

            return bound;
        }
        
        // Get bound by list of bounds
        public static Bounds GetShardsBound(List<RFShard> shards)
        {
            // new bound
            Bounds bound = new Bounds();

            // No mesh renderers
            if (shards.Count == 0)
            {
                Debug.Log("GetBoundsBound error");
                return bound;
            }

            // Basic bounds min and max values
            float minX = shards[0].bnd.min.x;
            float minY = shards[0].bnd.min.y;
            float minZ = shards[0].bnd.min.z;
            float maxX = shards[0].bnd.max.x;
            float maxY = shards[0].bnd.max.y;
            float maxZ = shards[0].bnd.max.z;

            // Compare with other bounds
            if (shards.Count > 1)
            {
                for (int i = 1; i < shards.Count; i++)
                {
                    if (shards[i].bnd.min.x < minX)
                        minX = shards[i].bnd.min.x;
                    if (shards[i].bnd.min.y < minY)
                        minY = shards[i].bnd.min.y;
                    if (shards[i].bnd.min.z < minZ)
                        minZ = shards[i].bnd.min.z;

                    if (shards[i].bnd.max.x > maxX)
                        maxX = shards[i].bnd.max.x;
                    if (shards[i].bnd.max.y > maxY)
                        maxY = shards[i].bnd.max.y;
                    if (shards[i].bnd.max.z > maxZ)
                        maxZ = shards[i].bnd.max.z;
                }
            }

            // Get center
            bound.center = new Vector3((maxX - minX) / 2f, (maxY - minY) / 2f, (maxZ - minZ) / 2f);

            // Get min and max vectors
            bound.min = new Vector3(minX, minY, minZ);
            bound.max = new Vector3(maxX, maxY, maxZ);

            return bound;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Static
        /// /////////////////////////////////////////////////////////
        
        // Get biggest cluster
        public static int GetBiggestCluster(List<RFCluster> clusters)
        {
            // Only one cluster
            if (clusters.Count == 1)
                return 0;

            int index = 0;
            int amount = 0;
            for (int i = 0; i < clusters.Count; i++)
            {
                if (clusters[i].shards.Count > amount)
                {
                    amount = clusters[i].shards.Count;
                    index = i;
                }
            }
            return index;
        }
        
        // Collect solo shards, remove from cluster, reinit cluster
        public static void GetSoloShards(RFCluster cluster, List<RFShard> soloShards)
        {
            // Collect solo shards, remove from cluster
            for (int s = cluster.shards.Count - 1; s >= 0; s--)
                if (cluster.shards[s].neibShards.Count == 0)
                {
                    soloShards.Add (cluster.shards[s]);
                    cluster.shards[s].cluster = null;
                    cluster.shards.RemoveAt (s);
                }
        }
        
        // Set uniq id after main cluster defined
        public static int GetUniqClusterId (RFCluster cluster)
        {
            // Main cluster is
            if (cluster.mainCluster == null)
                return 1;

            if (cluster.mainCluster.rigid == null)
                return 2;
            
            cluster.mainCluster.rigid.clusterDemolition.clsCount++;
            return cluster.mainCluster.rigid.clusterDemolition.clsCount;
        }
        
        // Integrity check
        public static bool IntegrityCheck(RFCluster cluster)
        {
            if (cluster != null && cluster.shards.Count > 0)
                for (int i = 0; i < cluster.shards.Count; i++)
                    if (cluster.shards[i].tm == null)
                        return false;
            return true;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Get
        /// /////////////////////////////////////////////////////////
        
        // Had child cluster
        public bool HasChildClusters { get { return childClusters != null && childClusters.Count > 0; } }

        // Check if cluster has unyielding shards
        public bool UnyieldingByShard
        {
            get
            {
                for (int i = 0; i < shards.Count; i++)
                    if (shards[i].uny == true)
                        return true;
                return false;
            }
        }
        
        // Check if cluster has unyielding shards
        public bool UnyieldingByRigid
        {
            get
            {
                for (int i = 0; i < shards.Count; i++)
                    if (shards[i].rigid.activation.unyielding == true)
                        return true;
                return false;
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Cluster component
        /// /////////////////////////////////////////////////////////    
        
        // Get all shards ain all child clusters
        List<RFShard> GetNestedShards(bool OwnShards = false)
        {
            List<RFShard> nestedShards = new List<RFShard>();
            List<RFCluster> nestedClusters = new List<RFCluster>();
            nestedClusters.AddRange(childClusters);

            // Collect own shards
            if (OwnShards == true)
                nestedShards.AddRange(shards);

            while (nestedClusters.Count > 0)
            {
                nestedShards.AddRange(nestedClusters[0].shards);
                nestedClusters.AddRange(nestedClusters[0].childClusters);
                nestedClusters.RemoveAt(0);
            }
            return nestedShards;
        }

        // Get all shards ain all child clusters
        public List<RFCluster> GetNestedClusters()
        {
            List<RFCluster> nestedClusters = new List<RFCluster>();
            nestedClusters.AddRange(childClusters);

            List<RFCluster> checkClusters = new List<RFCluster>();
            checkClusters.AddRange(childClusters);

            while (checkClusters.Count > 0)
            {
                nestedClusters.AddRange(checkClusters[0].childClusters);
                checkClusters.RemoveAt(0);
            }

            return nestedClusters;
        }
        
        // Check if other cluster has shared face
        bool TrisNeib(RFCluster otherCluster)
        {
            // Check if cluster shards has 1 neib in other cluster shards
            foreach (RFShard shard in shards)
                for (int i = 0; i < shard.neibShards.Count; i++)
                    if (otherCluster.shards.Contains(shard.neibShards[i]) == true)
                        return true;

            List<RFShard> nestedShards = GetNestedShards();
            List<RFShard> otherNestedShards = otherCluster.GetNestedShards();

            foreach (RFShard shard in nestedShards)
                for (int i = 0; i < shard.neibShards.Count; i++)
                    if (otherNestedShards.Contains(shard.neibShards[i]) == true)
                        return true;

            //// Check if other cluster among neib clusters
            //if (neibClusters.Contains(otherCluster) == true)
            //    return true;

            //// Check if other cluster children clusters has
            //foreach (RFCluster cluster in childClusters)
            //    for (int i = 0; i < cluster.neibClusters.Count; i++)
            //        if (otherCluster.neibClusters.Contains(cluster.neibClusters[i]) == true)
            //            return true;

            return false;
        }

        // Get shared area with another cluster
        float NeibArea(RFCluster otherCluster)
        {
            float area = 0f;
            foreach (RFShard shard in shards)
                for (int i = 0; i < shard.neibShards.Count; i++)
                    if (otherCluster.shards.Contains(shard.neibShards[i]) == true)
                        area += shard.nArea[i];


            List<RFShard> nestedShards = GetNestedShards();
            List<RFShard> otherNestedShards = otherCluster.GetNestedShards();

            foreach (RFShard shard in nestedShards)
                for (int i = 0; i < shard.neibShards.Count; i++)
                    if (otherNestedShards.Contains(shard.neibShards[i]) == true)
                        area += shard.nArea[i];


            //// Check if other cluster children clusters has
            //foreach (RFCluster cluster in childClusters)
            //    for (int i = 0; i < cluster.neibClusters.Count; i++)
            //        if (otherCluster.neibClusters.Contains(cluster.neibClusters[i]) == true)
            //            area += cluster.neibArea[i];

            return area;
        }

        // Get neib index with biggest shared area
        public int GetNeibIndArea(List<RFCluster> clusterList = null)
        {
            // Get neib index with biggest shared area
            float biggestArea = 0f;
            int neibInd = 0;
            for (int i = 0; i < neibClusters.Count; i++)
            {
                // Skip if check neib shard not in filter list
                if (clusterList != null)
                    if (clusterList.Contains(neibClusters[i]) == false)
                        continue;

                // Remember if bigger
                if (neibArea[i] > biggestArea)
                {
                    biggestArea = neibArea[i];
                    neibInd = i;
                }
            }

            // Return index of neib with biggest shared area
            if (biggestArea > 0)
                return neibInd;

            // No neib
            return -1;
        }

        // Find neib clusters amount cluster list and set them with neib area
        public static void SetClusterNeib(List<RFCluster> clusters, bool connectivity)
        {
            // Set list
            foreach (RFCluster cluster in clusters)
            {
                cluster.neibClusters = new List<RFCluster>();
                cluster.neibArea = new List<float>();
                cluster.neibPerc = new List<float>();
            }

            // Set neib and area info
            for (int i = 0; i < clusters.Count; i++)
            {
                for (int s = 0; s < clusters.Count; s++)
                {
                    if (s != i)
                    {
                        // Check if shard was not added as neib before
                        if (clusters[s].neibClusters.Contains(clusters[i]) == false)
                        {
                            // Bounding box intersection check
                            if (clusters[i].bound.Intersects(clusters[s].bound) == true)
                            {
                                // No need in face check connectivity
                                if (connectivity == false)
                                {
                                    float size = clusters[i].bound.size.magnitude;

                                    clusters[i].neibClusters.Add(clusters[s]);
                                    clusters[i].neibArea.Add(size);

                                    clusters[s].neibClusters.Add(clusters[i]);
                                    clusters[s].neibArea.Add(size);
                                }

                                // Face to face connectivity check
                                else
                                {
                                    // Check for shared faces and collect neibs and areas
                                    if (clusters[i].TrisNeib(clusters[s]) == true)
                                    {
                                        float area = clusters[i].NeibArea(clusters[s]);

                                        clusters[i].neibClusters.Add(clusters[s]);
                                        clusters[i].neibArea.Add(area);

                                        clusters[s].neibClusters.Add(clusters[i]);
                                        clusters[s].neibArea.Add(area);
                                    }
                                }
                            }
                        }
                    }
                }

                // Set area ratio
                float maxArea = Mathf.Max(clusters[i].neibArea.ToArray());
                foreach (float area in clusters[i].neibArea)
                {
                    if (maxArea > 0)
                        clusters[i].neibPerc.Add(area / maxArea);
                    else
                        clusters[i].neibPerc.Add(0f);
                }
            }
        }

        // Get neib cluster from shardList which is neib to one of the shards
        public static RFCluster GetNeibClusterArea(List<RFCluster> clusters, List<RFCluster> clusterList)
        {
            // No clusters to pick
            if (clusterList.Count == 0)
                return null;

            // Get all neibs for clusters, exclude neibs not from clusterList
            List<RFCluster> allNeibs = new List<RFCluster>();

            // Biggest area
            float biggestArea = 0f;
            RFCluster biggestCluster = null;

            // Check cluster
            foreach (RFCluster cluster in clusters)
            {
                // Check neibs
                for (int i = 0; i < cluster.neibClusters.Count; i++)
                {
                    // Shared are is too small relative other neibs
                    if (cluster.neibPerc[i] < 0.5f)
                        continue;

                    // Neib cluster has shared area lower than already founded 
                    if (biggestArea >= cluster.neibArea[i])
                        continue;

                    // Neib already in neib list
                    if (allNeibs.Contains(cluster.neibClusters[i]) == true)
                        continue;

                    // Neib not among allowed clusters
                    if (clusterList.Contains(cluster.neibClusters[i]) == false)
                        continue;

                    // Remember neib
                    allNeibs.Add(cluster.neibClusters[i]);
                    biggestArea = cluster.neibArea[i];
                    biggestCluster = cluster.neibClusters[i];
                }
            }

            // Pick shard with biggest area
            return biggestCluster;
        }
    }
}

