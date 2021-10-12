using UnityEngine;
using Object = UnityEngine.Object;

namespace RayFire
{
    public class RFBackupCluster
    {
        // Connected
        RFCluster        cluster;
        
        // Common
        bool             saved;

        // Constructor
        RFBackupCluster()
        {
            saved = false;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Save / Restore
        /// /////////////////////////////////////////////////////////

        // Save backup cluster to restore it later
        public static void SaveBackup (RayfireRigid scr)
        {
            // No need to save
            if (scr.reset.action != RFReset.PostDemolitionType.DeactivateToReset)
                return;
            
            // Do not backup child clusters
            if (scr.clusterDemolition.cluster.id > 1)
                return;

            // Create backup if not exist
            if (scr.clusterDemolition.backup == null)
                scr.clusterDemolition.backup = new RFBackupCluster();
            
            // Already saved
            if (scr.clusterDemolition.backup.saved == true)
                return;
            
            // Copy class
            scr.clusterDemolition.backup.cluster = new RFCluster(scr.clusterDemolition.cluster);
            
            // Init shards: set non serialized vars
            RFCluster.InitCluster (scr, scr.clusterDemolition.backup.cluster);
            
            // Save nested clusters shards and clusters position and rotation
            SaveTmRecursive (scr.clusterDemolition.backup.cluster);
            
            // Backup created, do not create again at next reset
            scr.clusterDemolition.backup.saved = true;
            
            // Debug.Log ("Saved");
        }
        
        // Restore cluster using backup cluster
        public static void RestoreBackup (RayfireRigid scr)
        {
            if (scr.reset.action == RFReset.PostDemolitionType.DeactivateToReset)
            {
                // Do not restore child clusters
                if (scr.clusterDemolition.cluster.id > 1)
                    return;

                // Has no backup
                if (scr.clusterDemolition.backup == null)
                    return;
                
                // Cluster was not demolished. Stop
                if (scr.objectType == ObjectType.ConnectedCluster)
                    if (scr.clusterDemolition.cluster.shards.Count == scr.clusterDemolition.backup.cluster.shards.Count)
                        return;

                // TODO check if nested cluster was demolished
                // if (false) if (scr.objectType == ObjectType.NestedCluster)
                //     if (scr.clusterDemolition.cluster.tm.gameObject.activeSelf == true)
                //return;
                
                // Completely demolished child clusters do not deactivates if saved
                // Unyielding component with inactive overlap bug
                
                // Reset fragments list
                scr.fragments = null;
                
                // Remove particles
                DestroyParticles (scr);
                
                // Reset local shard rigid, destroy components TODO INPUT ORIGINAL CLUSTER, GET RIGIDS
                ResetDeepShardRigid (scr, scr.clusterDemolition.backup.cluster);

                // Create new child clusters roots destroy by nested cluster. BEFORE reparent shards
                if (scr.objectType == ObjectType.NestedCluster)
                {
                    ResetRootsRecursive (scr.clusterDemolition.backup.cluster);
                    RestoreClusterTmRecursive (scr.clusterDemolition.backup.cluster);
                    ResetRootsParentsRecursive (scr.clusterDemolition.backup.cluster);
                }

                // Restore shards parent, position and rotation 
                RestoreShardTmRecursive (scr.clusterDemolition.backup.cluster);
                
                // Destroy new child clusters roots created by connected cluster. AFTER reparent shards
                if (scr.objectType == ObjectType.ConnectedCluster)
                    DestroyRoots (scr);
                
                // Copy class
                scr.clusterDemolition.cluster = new RFCluster(scr.clusterDemolition.backup.cluster);
                
                // Reset colliders 
                RFPhysic.CollectClusterColliders (scr, scr.clusterDemolition.cluster);
                
                // Init shards: set non serialized vars
                RFCluster.InitCluster (scr, scr.clusterDemolition.cluster);

                scr.clusterDemolition.collapse.inProgress = false;
            }
        }

        // Remove particles
        static void DestroyParticles(RayfireRigid scr)
        {
            if (scr.HasDebris == true)
                for (int i = 0; i < scr.debrisList.Count; i++)
                {
                    for (int c = scr.debrisList[i].children.Count - 1; c >= 0; c--)
                    {
                        if (scr.debrisList[i].children[c] != null)
                        {
                            if (scr.debrisList[i].children[c].hostTm != null)
                                Object.Destroy (scr.debrisList[i].children[c].hostTm.gameObject);
                            Object.Destroy (scr.debrisList[i].children[c]);
                        }
                        scr.debrisList[i].children.RemoveAt (c);
                    }
                    scr.debrisList[i].children = null;
                }
            if (scr.HasDust == true)
                for (int i = 0; i < scr.dustList.Count; i++)
                {
                    for (int c = scr.dustList[i].children.Count - 1; c >= 0; c--)
                    {
                        if (scr.dustList[i].children[c] != null)
                        {
                            if (scr.dustList[i].children[c].hostTm != null)
                                Object.Destroy (scr.dustList[i].children[c].hostTm.gameObject);
                            Object.Destroy (scr.dustList[i].children[c]);
                        }
                        scr.dustList[i].children.RemoveAt (c);
                    }
                    scr.dustList[i].children = null;
                }
        }

        /// /////////////////////////////////////////////////////////
        /// Reset shard rigid
        /// /////////////////////////////////////////////////////////      
        
        // Reset local shard rigid, destroy components
        static void ResetDeepShardRigid (RayfireRigid scr, RFCluster cluster)
        {
            // Collect shards colliders
            for (int i = 0; i < cluster.shards.Count; i++)
                ResetShardRigid (cluster.shards[i]);

            // Set child cluster colliders
            if (scr.objectType == ObjectType.NestedCluster)
                if (cluster.HasChildClusters == true)
                    for (int i = 0; i < cluster.childClusters.Count; i++)
                        ResetDeepShardRigid (scr, cluster.childClusters[i]);
        }

        // Reset local shard rigid, destroy components
        static void ResetShardRigid (RFShard shard)
        {
            shard.rigid = shard.tm.GetComponent<RayfireRigid>();
            
            if (shard.rigid != null)
            {
                // Destroy rigid body
                if (shard.rigid.physics.rigidBody != null)
                {
                    shard.rigid.physics.rigidBody.velocity = Vector3.zero;
                    Object.Destroy (shard.rigid.physics.rigidBody);
                }

                // TODO TEMP SOLUTION, DESTROY ALL DEBRIS AS WELL
                if (shard.rigid.HasDebris || shard.rigid.HasDust)
                    for (int c = shard.tm.childCount - 1; c >= 0; c--)
                        Object.Destroy (shard.tm.GetChild (c).gameObject);

                Object.Destroy (shard.rigid);
                
                
                // shard.rigid.gameObject.SetActive (false);
                
                // Stop cors
                // shard.rigid.StopAllCoroutines();
                
                // shard.rigid.debrisList = null;
                // shard.rigid.dustList   = null;
                //
                // // Reset Rigid
                // shard.rigid.ResetRigid();
                //
                // // Destroy rigid component
                // if (shard.rigid.reset.shards == RFReset.ShardsResetType.DestroyRigid)
                //     
                // else
                //     shard.rigid.initialization = RayfireRigid.InitType.ByMethod;
                // shard.rigid.gameObject.SetActive (true);
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Transform / parent
        /// /////////////////////////////////////////////////////////
        
        // Save cluster/shards tm
        static void SaveTmRecursive(RFCluster cluster)
        {
            // Save cluster tm
            cluster.pos = cluster.tm.position;
            cluster.rot = cluster.tm.rotation;
            
            // Save shards tm
            for (int i = 0; i < cluster.shards.Count; i++)
            {
                cluster.shards[i].pos = cluster.shards[i].tm.position;
                cluster.shards[i].rot = cluster.shards[i].tm.rotation;
            }

            // Repeat for child clusters
            if (cluster.HasChildClusters == true)
                for (int i = 0; i < cluster.childClusters.Count; i++)
                    SaveTmRecursive (cluster.childClusters[i]);
        }
        
        // Save cluster/shards tm
        static void RestoreShardTmRecursive(RFCluster cluster)
        {
            // Save shards tm
            for (int i = 0; i < cluster.shards.Count; i++)
            {
                cluster.shards[i].tm.SetParent (null);
                cluster.shards[i].tm.SetPositionAndRotation (cluster.shards[i].pos, cluster.shards[i].rot);
                cluster.shards[i].tm.SetParent (cluster.tm, true);
                
                //cluster.shards[i].tm.parent = null;
                //cluster.shards[i].tm.position = cluster.shards[i].pos;
                //cluster.shards[i].tm.rotation = cluster.shards[i].rot;
                //cluster.shards[i].tm.parent = cluster.tm;
            }

            // Repeat for child clusters
            if (cluster.HasChildClusters == true)
                for (int i = 0; i < cluster.childClusters.Count; i++)
                    RestoreShardTmRecursive (cluster.childClusters[i]);
        }
        
        // Save cluster/shards tm
        static void RestoreClusterTmRecursive(RFCluster cluster)
        {
            // Save cluster tm
            cluster.tm.rotation   = cluster.rot;
            cluster.tm.position   = cluster.pos;
            
            // Repeat for child clusters
            if (cluster.HasChildClusters == true)
                for (int i = 0; i < cluster.childClusters.Count; i++)
                    RestoreClusterTmRecursive (cluster.childClusters[i]);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Roots
        /// /////////////////////////////////////////////////////////
        
        // Create deleted roots and restore their tm back
        static void ResetRootsRecursive(RFCluster cluster)
        {
            if (cluster.HasChildClusters == true)
            {
                for (int i = 0; i < cluster.childClusters.Count; i++)
                {

                    cluster.childClusters[i].tm.parent = null;
                    
                    // Destroy rigid
                    cluster.childClusters[i].rigid = cluster.childClusters[i].tm.GetComponent<RayfireRigid>();
                    if (cluster.childClusters[i].rigid != null)
                    {
                        // Destroy rigid body
                        if (cluster.childClusters[i].rigid.physics.rigidBody != null)
                        {
                            Object.Destroy (cluster.childClusters[i].rigid.physics.rigidBody);
                        }
                        
                        Object.Destroy (cluster.childClusters[i].rigid);
                    }

                    // Activate
                    cluster.childClusters[i].tm.gameObject.SetActive (true);

                    // Repeat for children
                    ResetRootsRecursive (cluster.childClusters[i]);
                }
            }
        }
        
        // Create deleted roots and restore their tm back
        static void ResetRootsParentsRecursive(RFCluster cluster)
        {
            if (cluster.HasChildClusters == true)
            {
                for (int i = 0; i < cluster.childClusters.Count; i++)
                {
                    cluster.childClusters[i].tm.parent = cluster.tm;
                    ResetRootsParentsRecursive (cluster.childClusters[i]);
                }
            }
        }
        
        // Destroy new child clusters roots created by connected cluster
        static void DestroyRoots (RayfireRigid scr)
        {
            for (int i = 0; i < scr.clusterDemolition.minorClusters.Count; i++)
            {
                if (scr.clusterDemolition.minorClusters[i].tm != null)
                {
                    scr.clusterDemolition.minorClusters[i].tm.gameObject.SetActive (false);
                    Object.Destroy (scr.clusterDemolition.minorClusters[i].tm.gameObject);
                }
            }
        }
    }
}