using System;
using System.Linq;
using UnityEngine;

// Reuse for fading
// Instantiate preserved

namespace RayFire
{
    [Serializable]
    public class RFReset
    {
        // Post dml object 
        public enum PostDemolitionType
        {
            DestroyWithDelay  = 0,
            DeactivateToReset = 1
        }
        
        // Mesh reuse
        public enum MeshResetType
        {
            Destroy              = 0,
            ReuseInputMesh       = 2,
            ReuseFragmentMeshes  = 4
        }
        
        // Fragments reuse
        public enum FragmentsResetType
        {
            Destroy     = 0,
            Reuse       = 2,
            Preserve    = 4
        }
        
        // Fragments reuse
        public enum ShardsResetType
        {
            DestroyRigid  = 0,
            ReuseRigid    = 2
        }
        
        [Header ("  Reset")]
        [Space (3)]
        
        public bool transform;
        [Space (2)]
        public bool damage;

        [Header ("  Post Demolition")]
        [Space (3)]
        
        public PostDemolitionType action;
        [Space (2)]
        [Range (1, 60)] public int destroyDelay;
        
        [Header ("  Reuse")]
        [Space (3)]
        
        public MeshResetType mesh;
        [Space (2)]
        public FragmentsResetType fragments;
        [Space (2)]
        
        [HideInInspector] public ShardsResetType shards;
        
        [NonSerialized] public bool toBeDestroyed;

        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////

        // Constructor
        public RFReset()
        {
            action        = PostDemolitionType.DestroyWithDelay;
            destroyDelay  = 1;
            transform     = true;
            damage        = true;
            mesh          = MeshResetType.ReuseFragmentMeshes;
            fragments     = FragmentsResetType.Destroy;
            shards        = ShardsResetType.DestroyRigid;
            toBeDestroyed = false;
        }

        // Copy from
        public void CopyFrom (RayfireRigid scr)
        {
            // Copy to initial object: mesh root copy
            if (scr.objectType == ObjectType.MeshRoot)
            {
                action    = scr.reset.action;
                transform = scr.reset.transform;
                damage    = scr.reset.damage;
                mesh      = scr.reset.mesh;
                fragments = scr.reset.fragments;
                shards    = scr.reset.shards;
            }

            // Copy to cluster shards
            else if (scr.objectType == ObjectType.ConnectedCluster)
            {
                // Fragments going to be reused by original object, should be deactivated
                action = scr.reset.fragments == FragmentsResetType.Reuse 
                    ? PostDemolitionType.DeactivateToReset 
                    : PostDemolitionType.DestroyWithDelay;

                transform = false;
                damage    = false;
                mesh      = scr.reset.mesh;
                fragments = scr.reset.fragments;
                shards    = scr.reset.shards;
            }
            
            // Copy to fragments
            else
            {
                // Fragments going to be reused by original object, should be deactivated
                action = scr.reset.fragments == FragmentsResetType.Reuse 
                    ? PostDemolitionType.DeactivateToReset 
                    : PostDemolitionType.DestroyWithDelay;

                transform = false;
                damage    = false;
                mesh      = MeshResetType.Destroy;
                fragments = FragmentsResetType.Destroy;
                shards    = scr.reset.shards;
            }
            
            destroyDelay = scr.reset.destroyDelay;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////
        
        // Reinit demolished mesh object
        public static void ResetRigid (RayfireRigid scr)
        {
            // Object can't be reused
            if (ObjectReuseState (scr) == false)
                return;
            
            // Save faded/demolished state before reset
            int faded = scr.fading.state;
            bool demolished = scr.limitations.demolished;

            // Reset tm
            if (scr.reset.transform == true)
                RestoreTransform(scr);
            
            // Reset activation TODO check ifi t was Kinematik
            if (scr.activation.activated == true)
                scr.simulationType = SimType.Inactive;
            
            // Reset rigid props
            Reset (scr);
            
            // Stop all cors in case object restarted
            scr.StopAllCoroutines();

            // Reset if object fading/faded
            if (faded >= 1)
                ResetFade(scr);
            
            // Demolished. Restore
            if (demolished == true)
                ResetMeshDemolition (scr);
            
            // Restore cluster even if it was not demolished
            ResetClusterDemolition (scr);
            
            // Activate if deactivated
            if (scr.gameObject.activeSelf == false)
                scr.gameObject.SetActive (true);

            // Start all coroutines
            scr.StartAllCoroutines();
        }

        // Reset if object fading/faded
        static void ResetFade (RayfireRigid scr)
        {
            // Was excluded
            if (scr.fading.fadeType == FadeType.SimExclude)
            {
                scr.physics.meshCollider.enabled = true;// TODO CHECK CLUSTER COLLIDERS
            }   
               
            // Was moved down
            else if (scr.fading.fadeType == FadeType.MoveDown)
            {
                scr.physics.meshCollider.enabled = true;// TODO CHECK CLUSTER COLLIDERS
                scr.gameObject.SetActive (true);
            } 
            
            // Was scaled down
            else if (scr.fading.fadeType == FadeType.ScaleDown)
            {
                scr.transForm.localScale = scr.physics.initScale;
                scr.gameObject.SetActive (true);
            }

            // Was destroyed
            else if (scr.fading.fadeType == FadeType.Destroy)
                scr.gameObject.SetActive (true);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Demolition reset
        /// /////////////////////////////////////////////////////////
        
        // Reinit demolished mesh object
        static void ResetMeshDemolition (RayfireRigid scr)
        {
            // Edit meshes and fragments only if object was demolished
            if (scr.objectType == ObjectType.Mesh)
            {
                // Reset input shatter
                if (scr.reset.mesh != MeshResetType.ReuseInputMesh)
                    scr.meshDemolition.rfShatter = null;
                
                // Reset Meshes
                if (scr.reset.mesh != MeshResetType.ReuseFragmentMeshes)
                    scr.meshes = null;

                // Fragments need to be reused
                if (scr.reset.fragments == FragmentsResetType.Reuse)
                {
                    // Can be reused. Destroyed if can not
                    if (FragmentReuseState (scr) == true)
                        ReuseFragments (scr);
                    else
                        DestroyFragments (scr);
                }
                
                // Destroy fragments
                else if (scr.reset.fragments == FragmentsResetType.Destroy)
                    DestroyFragments (scr);
                
                // Fragments should be kept in scene. Forget about them
                else if (scr.reset.fragments == FragmentsResetType.Preserve)
                    PreserveFragments (scr);
            }
      
            // Activate
            scr.gameObject.SetActive (true);
        }
        
        // Destroy fragments and root
        static void DestroyFragments (RayfireRigid scr)
        {
            // Destroy fragments    
            if (scr.HasFragments == true)
            {
                // Get amount of fragments
                int fragmentNum = scr.fragments.Count (t => t != null);

                // Destroy fragments and root
                for (int i = scr.fragments.Count - 1; i >= 0; i--)
                {
                    if (scr.fragments[i] != null)
                    {
                        // Destroy particles
                        DestroyParticles (scr.fragments[i]);
                        
                        // Destroy fragment
                        scr.fragments[i].gameObject.SetActive (false);
                        RayfireMan.DestroyGo (scr.fragments[i].gameObject);

                        // Destroy root
                        if (scr.fragments[i].rootParent != null)
                        {
                            scr.fragments[i].rootParent.gameObject.SetActive (false);
                            RayfireMan.DestroyGo (scr.fragments[i].rootParent.gameObject);
                        }
                    }
                }
                
                // Nullify
                scr.fragments = null;

                // Subtract amount of deleted fragments
                RayfireMan.inst.advancedDemolitionProperties.currentAmount -= fragmentNum;
                
                // Destroy descendants
                if (scr.limitations.descendants.Count > 0)
                {
                    // Get amount of descendants
                    int descendantNum = scr.limitations.descendants.Count (t => t != null);
                    
                    // Destroy fragments and root
                    for (int i = 0; i < scr.limitations.descendants.Count; i++)
                    {
                        if (scr.limitations.descendants[i] != null)
                        {
                            // Destroy fragment
                            scr.limitations.descendants[i].gameObject.SetActive (false);
                            RayfireMan.DestroyGo (scr.limitations.descendants[i].gameObject);

                            // Destroy root
                            if (scr.limitations.descendants[i].rootParent != null)
                            {
                                scr.limitations.descendants[i].rootParent.gameObject.SetActive (false);
                                RayfireMan.DestroyGo (scr.limitations.descendants[i].rootParent.gameObject);
                            }
                        }
                    }
                    
                    // Clear
                    scr.limitations.descendants.Clear();
                    
                    // Subtract amount of deleted fragments
                    RayfireMan.inst.advancedDemolitionProperties.currentAmount -= descendantNum;
                }
            }
        }

        // Destroy particles
        static void DestroyParticles (RayfireRigid scr)
        {
            // Destroy debris
            if (scr.HasDebris == true)
                for (int d = 0; d < scr.debrisList.Count; d++)
                    if (scr.debrisList[d].hostTm != null)
                    {
                        scr.debrisList[d].hostTm.gameObject.SetActive (false);
                        RayfireMan.DestroyGo (scr.debrisList[d].hostTm.gameObject);
                    }

            // Destroy debris
            if (scr.HasDust == true)
                for (int d = 0; d < scr.dustList.Count; d++)
                    if (scr.dustList[d].hostTm != null)
                    {
                        scr.dustList[d].hostTm.gameObject.SetActive (false);
                        RayfireMan.DestroyGo (scr.dustList[d].hostTm.gameObject);
                    }
        }
        
        // Fragments need and can be reused
        static void ReuseFragments (RayfireRigid scr)
        {
            // Sub amount
            RayfireMan.inst.advancedDemolitionProperties.currentAmount -= scr.fragments.Count;
            
            // Activate root
            if (scr.rootChild != null)
            {
                scr.rootChild.gameObject.SetActive (false);
                scr.rootChild.position = scr.transForm.position;
                scr.rootChild.rotation = scr.transForm.rotation;
            }

            // Reset fragments tm
            for (int i = scr.fragments.Count - 1; i >= 0; i--)
            {
                // Destroy particles
                DestroyParticles (scr.fragments[i]);
                
                scr.fragments[i].transForm.localScale = scr.fragments[i].physics.initScale;
                scr.fragments[i].transForm.position = scr.transForm.position + scr.pivots[i];
                scr.fragments[i].transForm.rotation = Quaternion.identity;

                // Reset activation TODO check if it was Kinematik
                if (scr.fragments[i].activation.activated == true)
                    scr.fragments[i].simulationType = SimType.Inactive;
                
                // Reset fading
                if (scr.fragments[i].fading.state >= 1)
                    ResetFade(scr.fragments[i]);
                
                // Reset rigid props
                Reset (scr.fragments[i]);
            }

            // Clear descendants
            scr.limitations.descendants.Clear();
        }
        
        // Preserve Fragments
        static void PreserveFragments (RayfireRigid scr)
        {
            scr.fragments = null;
            scr.rootChild = null;
            scr.limitations.descendants.Clear();
        }
          
        // Reinit demolished mesh object
        static void ResetClusterDemolition (RayfireRigid scr)
        {
            if (scr.objectType == ObjectType.ConnectedCluster || scr.objectType == ObjectType.NestedCluster)
            {
                RFBackupCluster.RestoreBackup (scr);
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Reuse state
        /// /////////////////////////////////////////////////////////          
        
        // Check fragments reuse state
        static bool ObjectReuseState (RayfireRigid scr)
        {
            // Excluded from sim
            if (scr.physics.exclude == true)
            {
                Debug.Log ("Demolished " + scr.objectType.ToString() + " reset not supported yet.");
                return false;
            }
            
            // Not mesh object type
            if (scr.objectType == ObjectType.Mesh 
                || scr.objectType == ObjectType.ConnectedCluster
                || scr.objectType == ObjectType.NestedCluster)
                return true;
            
            // Object can be reused
            return false;
        }
                
        // Check fragments reuse state
        static bool FragmentReuseState (RayfireRigid scr)
        {
            // Do not reuse reference demolition
            if (scr.demolitionType == DemolitionType.ReferenceDemolition)
                return false;
            
            // Fragments list null or empty
            if (scr.HasFragments == false)
                return false;

            // One of the fragment null
            if (scr.fragments.Any (t => t == null))
                return false;
            
            // One of the fragment going to be destroyed TODO make reusable
            if (scr.fragments.Any (t => t.reset.toBeDestroyed == true))
                return false;
            
            // One of the fragment demolished TODO make reusable
            if (scr.fragments.Any (t => t.limitations.demolished == true))
                return false;
  
            // Fragments can be reused
            return true;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Other
        /// /////////////////////////////////////////////////////////      
        
        // Restore transform or initial
        static void RestoreTransform (RayfireRigid scr)
        {
            // Restore tm
            scr.transForm.localScale = scr.physics.initScale;
            scr.transForm.position   = scr.physics.initPosition;
            scr.transForm.rotation   = scr.physics.initRotation;
            scr.physics.velocity     = Vector3.zero;
            
            // Restore rigidbody TODO save initial velocity into vars and reset to them
            if (scr.physics.rigidBody != null)
            {
                scr.physics.rigidBody.velocity        = Vector3.zero;
                scr.physics.rigidBody.angularVelocity = Vector3.zero;
            }
        }
        
        // Restore rigid properties
        static void Reset (RayfireRigid scr)
        {
            // Reset caching if it is on
            scr.meshDemolition.StopRuntimeCaching();
            
            // Reset limitations
            scr.activation.Reset();

            if (scr.restriction != null)
                scr.restriction.Reset();
            
            scr.limitations.Reset();
            scr.meshDemolition.Reset();
            scr.clusterDemolition.Reset();
            scr.fading.Reset();
            
            // Reset damage
            if (scr.reset.damage == true)
                scr.damage.Reset();

            // Set physical simulation type. Important. Should after collider material define
            RFPhysic.SetSimulationType (scr);
        }
    }
}