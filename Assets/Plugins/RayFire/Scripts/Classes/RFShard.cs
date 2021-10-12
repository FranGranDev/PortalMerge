using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

namespace RayFire
{
    [Serializable]
    public class RFShard : IComparable<RFShard>
    {
        public int id;
        public Transform tm;
        public Vector3 pos;
        public Quaternion rot;
        public Bounds bnd;
        public float sz;
        public bool uny;
        public int nAm;
        public List<int> nIds;
        public List<float> nArea;
        public RayfireRigid rigid;
        public Collider col;
        public Rigidbody rb;
        
        // Mesh filter for collider setup
        [NonSerialized] public MeshFilter mf;
        [NonSerialized] public Vector3    initPos;
        
        // Need only during calc to set neib data
        [NonSerialized] public List<RFTriangle> tris;
        
        // Reinit in awake from cluster
        [NonSerialized] public RFCluster cluster;
        [NonSerialized] public List<RFShard> neibShards;

        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////
        
        // Constructor
        public RFShard (RFShard source)
        {
            id  = source.id;
            tm  = source.tm;
            pos = source.pos;
            rot = source.rot;
            bnd = source.bnd;
            sz  = source.sz;
            nAm = source.nAm;
            uny = source.uny;
            
            if (source.nIds != null)
            {
                nIds = new List<int>();
                for (int i = 0; i < source.nIds.Count; i++)
                    nIds.Add (source.nIds[i]);
               
                nArea = new List<float>();
                for (int i = 0; i < source.nArea.Count; i++)
                    nArea.Add (source.nArea[i]);
            }
            
            col = source.col;
            rigid = source.rigid;
            
            // IMPORTANT: Cluster, neibShards init after all shards copied
        }
        
        // Constructor
        public RFShard(Transform Tm, int Id)
        {
            tm      = Tm;
            id      = Id;
            initPos = tm.position;
            
            // Set bounds
            Renderer mr = Tm.GetComponent<Renderer>();
            if (mr != null)
                bnd = mr.bounds;
            
            // TODO add property to expand bounds, consider small and big size objects
            bnd.Expand(0.01f);  
            
            sz = bnd.size.magnitude;
        }

        // Compare by size
        public int CompareTo(RFShard otherShard)
        {
            if (sz > otherShard.sz)
                return -1;
            if (sz < otherShard.sz)
                return 1;
            return 0;
        }

        /// /////////////////////////////////////////////////////////
        /// Shards
        /// /////////////////////////////////////////////////////////
                
        // Prepare shards. Set bounds, set neibs
        public static void SetShards(RFCluster cluster, ConnectivityType connectivity, bool setRigid = false)
        {
            // Get all children tms
            List<Transform> tmList = new List<Transform>();
            for (int i = 0; i < cluster.tm.childCount; i++)
                tmList.Add (cluster.tm.GetChild(i));
            
            // Get child shards
            SetShardsByTransforms (cluster, tmList, connectivity, setRigid);
        }
        

        
        // Prepare shards. Set bounds, set neibs
        public static void SetShardsByTransforms(RFCluster cluster, List<Transform> tmList, ConnectivityType connectivity, bool setRigid = false)
        {
            cluster.shards = new List<RFShard>();
            for (int i = 0; i < tmList.Count; i++)
            {
                // Get mesh filter
                MeshFilter mf = tmList[i].GetComponent<MeshFilter>();

                // Child has no mesh
                if (mf == null)
                    continue;

                // Has no mesh
                if (mf.sharedMesh == null)
                    continue;

                // Create new shard
                RFShard shard = new RFShard(tmList[i], i);
                shard.cluster = cluster;

                // Set faces data for connectivity
                if (connectivity == ConnectivityType.ByMesh || connectivity == ConnectivityType.ByBoundingBoxAndMesh)
                    RFTriangle.SetTriangles(shard, mf);

                // Collect shard
                cluster.shards.Add(shard);
            }
            
            // Set rigid component
            if (setRigid == true)
                for (int i = 0; i < cluster.shards.Count; i++)
                    cluster.shards[i].rigid = cluster.shards[i].tm.GetComponent<RayfireRigid>();
        }

        /// /////////////////////////////////////////////////////////
        /// Neibs
        /// /////////////////////////////////////////////////////////
        
        // Check if other shard has shared face TODO check speed by normal
        bool TrisNeib(RFShard otherShard)
        {
            for (int i = 0; i < tris.Count; i++)
            {
                for (int j = 0; j < otherShard.tris.Count; j++)
                {
                    // Area check
                    float areaDif = Mathf.Abs (tris[i].area - otherShard.tris[j].area);
                    if (areaDif < 0.001f)
                    {
                        // Position check
                        float posDif = Vector3.Distance (tris[i].pos, otherShard.tris[j].pos);
                        if (posDif < 0.01f)
                            return true;
                    }
                }
            }

            return false;
        }

        // Get shared area with another shard TODO not accurate with opposite triangles. use Face class to fix
        float NeibArea(RFShard otherShard)
        {
            float area = 0f;
            for (int i = 0; i < tris.Count; i++)
            {
                for (int j = 0; j < otherShard.tris.Count; j++)
                {
                    float areaDif = Mathf.Abs (tris[i].area - otherShard.tris[j].area);
                    if (areaDif < 0.001f)
                    {
                        float posDif = Vector3.Distance (tris[i].pos, otherShard.tris[j].pos);
                        if (posDif < 0.01f)
                        {
                            area += tris[i].area;
                            break;
                        }
                    }
                }
            }

            return area;
        }

        // Get neib index with biggest shared area
        public int GetNeibIndArea(List<RFShard> shardList = null)
        {
            // Get neib index with biggest shared area
            float biggestArea = 0f;
            int neibInd = 0;
            for (int i = 0; i < neibShards.Count; i++)
            {
                // Skip if check neib shard not in filter list
                if (shardList != null)
                    if (shardList.Contains(neibShards[i]) == false)
                        continue;

                // Remember if bigger
                if (nArea[i] > biggestArea)
                {
                    biggestArea = nArea[i];
                    neibInd = i;
                }
            }

            // Return index of neib with biggest shared area
            if (biggestArea > 0)
                return neibInd;

            // No neib
            return -1;
        }
        
        // Set shard neibs
        public static void SetShardNeibs(List<RFShard> shards, ConnectivityType type, float minArea = 0, float minSize = 0, int perc = 0, int seed = 0)
        {
            // Set list
            for (int i = 0; i < shards.Count; i++)
            {
                shards[i].neibShards = new List<RFShard>();
                shards[i].nArea = new List<float>();
                shards[i].nIds = new List<int>();
                shards[i].nAm = 0;
            }

            // Set neib and area info
            for (int i = 0; i < shards.Count; i++)
            {
                // Skip by size
                if (minSize > 0 && shards[i].sz < minSize)
                    continue;

                for (int s = 0; s < shards.Count; s++)
                {
                    // Skip itself
                    if (s != i)
                    {
                        // Skip by size
                        if (minSize > 0 && shards[s].sz < minSize)
                            continue;
                        
                        // Set random state for same pair
                        if (perc > 0)
                        {
                            Random.InitState (shards[i].id + shards[s].id + seed);
                            if (Random.Range (0, 100) < perc)
                                continue;
                        }

                        // Check if shard was not added as neib before
                        if (shards[s].nIds.Contains(shards[i].id) == false)
                        {
                            // Bounding box intersection check
                            if (shards[i].bnd.Intersects(shards[s].bnd) == true)
                            {
                                // Get areas
                                float area = 0;
                                
                                if (type != ConnectivityType.ByBoundingBox)
                                    area = shards[i].NeibArea (shards[s]);

                                if (type != ConnectivityType.ByMesh)
                                    area = (shards[i].sz + shards[s].sz) / 4f;

                                // Skip low area neibs TODO filter after all connected, leave one biggest ??
                                if (minArea > 0 && area < minArea)
                                    continue;
                                
                                // Collect
                                if (area > 0)
                                {
                                    shards[i].neibShards.Add (shards[s]);
                                    shards[i].nArea.Add (area);
                                    shards[i].nIds.Add (shards[s].id);

                                    shards[s].neibShards.Add (shards[i]);
                                    shards[s].nArea.Add (area);
                                    shards[s].nIds.Add (shards[i].id);
                                }
                            }
                        }
                    }
                }

                // Set original neib amount to know if neibs was removed
                shards[i].nAm = shards[i].nIds.Count;
            }
            
            // Clear triangles data
            if (type == ConnectivityType.ByMesh)
                for (int i = 0; i < shards.Count; i++)
                    RFTriangle.Clear (shards[i]);
        }

        // Get neib shard from shardList which is neib to one of the shards
        public static RFShard GetNeibShardArea(List<RFShard> shardGroup, List<RFShard> shardList)
        {
            // No shards to pick
            if (shardList.Count == 0)
                return null;

            // Get all neibs for shards, exclude neibs not from shardList
            List<RFShard> allNeibs = new List<RFShard>();

            // Biggest area
            float biggestArea = 0f;
            RFShard biggestShard = null;

            // Check shard
            foreach (RFShard shard in shardGroup)
            {
                // Check neibs
                for (int i = 0; i < shard.neibShards.Count; i++)
                {
                    // Neib shard has shared area lower than already founded 
                    if (biggestArea >= shard.nArea[i])
                        continue;

                    // Neib already in neib list
                    if (allNeibs.Contains(shard.neibShards[i]) == true)
                        continue;

                    // Neib not among allowed shards
                    if (shardList.Contains(shard.neibShards[i]) == false)
                        continue;

                    // Remember neib
                    allNeibs.Add(shard.neibShards[i]);
                    biggestArea = shard.nArea[i];
                    biggestShard = shard.neibShards[i];
                }
            }
            allNeibs = null;

            // Pick shard with biggest area
            return biggestShard;
        }
        
        // Remove neib shards which are not in current cluster anymore
        public static void ReinitNeibs (List<RFShard> shards)
        {
            if (shards.Count > 0)
            {
                // Remove detach shards from neib. Edit neib shards data
                for (int i = 0; i < shards.Count; i++)
                {
                    // Check very neib shard
                    for (int n = shards[i].neibShards.Count - 1; n >= 0; n--)
                    {
                        // Neib shard was detached
                        if (shards[i].neibShards[n].cluster != shards[i].cluster)
                        {
                            shards[i].nIds.RemoveAt (n);
                            shards[i].nArea.RemoveAt (n);
                            shards[i].neibShards.RemoveAt (n);
                        }
                    }
                }
            }
        }
        
        // Get slice plane at middle of longest bound edge
        public static Plane GetSlicePlane (Bounds bound)
        {
            Vector3 normal;
            Vector3 size = bound.size;
            Vector3 point = bound.center;
            if (size.x >= size.y && size.x >= size.z)
                normal = Vector3.right;
            else if (size.y >= size.x && size.y >= size.z)
                normal = Vector3.up;
            else
                normal = Vector3.forward;
            return new Plane(normal, point);
        }
        
        // Sort list by distance to point
        public static List<RFShard> SortByDistanceToPoint(List<RFShard> shards, Vector3 point, int amount)
        {
            List<float> distances = new List<float>();
            List<RFShard> sorted = new List<RFShard>();
            float dist = Vector3.Distance (point, shards[0].tm.position);
            distances.Add (dist);
            sorted.Add (shards[0]);
            for (int s = 1; s < shards.Count; s++)
            {
                dist = Vector3.Distance (point, shards[s].tm.position);
                for (int d = 0; d < distances.Count; d++)
                {
                    if (dist <= distances[d])
                    {
                        sorted.Insert (d, shards[s]);
                        distances.Insert (d, dist);
                        break;
                    } 
                }
            }

            // Center shards in range less than required
            if (amount > sorted.Count)
                amount = sorted.Count;
            
            sorted.RemoveRange (amount, sorted.Count - amount);
            return sorted;
        }
        
        // Sort list by distance to point
        public static List<RFShard> SortByDistanceToPlane(List<RFShard> shards, Vector3 point, Vector3 normal, int amount)
        {
            List<float>   distances = new List<float>();
            List<RFShard> sorted    = new List<RFShard>();
            Plane         plane     = new Plane(normal, point);
            float         dist      = Math.Abs(plane.GetDistanceToPoint (shards[0].tm.position));
            distances.Add (dist);
            sorted.Add (shards[0]);
            for (int s = 1; s < shards.Count; s++)
            {
                dist = Math.Abs(plane.GetDistanceToPoint (shards[s].tm.position));
                for (int d = 0; d < distances.Count; d++)
                {
                    if (dist <= distances[d])
                    {
                        sorted.Insert (d, shards[s]);
                        distances.Insert (d, dist);
                        break;
                    } 
                }
            }

            // Center shards in range less than required
            if (amount > sorted.Count)
                amount = sorted.Count;
            
            sorted.RemoveRange (amount, sorted.Count - amount);
            return sorted;
        }
        
        // Sort list by distance to point
        public static List<RFShard> ContactShards(RFShard shard, int amount)
        {
            List<RFShard> centerShards = new List<RFShard>();
            List<RFShard> checkShards = new List<RFShard>();
            
            checkShards.Add (shard);
            centerShards.Add (shard);

            while (checkShards.Count > 0 && centerShards.Count < amount)
            {
                for (int i = 0; i < checkShards[0].neibShards.Count; i++)
                {
                    if (checkShards[0].neibShards[i].cluster == null)
                    {
                        if (centerShards.Contains (checkShards[0].neibShards[i]) == false)
                        {
                            checkShards.Add (checkShards[0].neibShards[i]);
                            centerShards.Add (checkShards[0].neibShards[i]);
                        }
                    }
                }
                checkShards.RemoveAt (0);
            }
            
            return centerShards;
        }
        
        // Check if cluster has unyielding shards
        public static bool UnyieldingByShard (List<RFShard> shards)
        {
            for (int i = 0; i < shards.Count; i++)
                if (shards[i].uny == true)
                    return true;
            return false;
        }
        
        // Check if cluster has unyielding shards
        public static bool UnyieldingByShardAll (List<RFShard> shards)
        {
            for (int i = 0; i < shards.Count; i++)
                if (shards[i].uny == false)
                    return false;
            return true;
        }
        
    }
}

