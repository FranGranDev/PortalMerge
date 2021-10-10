using System.Collections.Generic;
using UnityEngine;

namespace RayFire
{
    [System.Serializable]
    public class RFReferenceDemolition
    {
        [Header ("  Source")]
        [Space (1)]
        
        public GameObject reference;
        public List<GameObject> randomList;
        
        [Header ("  Properties")]
        [Space (1)]
        
        //public AlignType type;
        
        [Tooltip ("Add RayFire Rigid component to reference with mesh")]
        public bool addRigid;
        public bool inheritScale;
        
        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////
        
        // Constructor
        public RFReferenceDemolition()
        {
            reference    = null;
            addRigid     = true;
            inheritScale = true;
        }

        // Copy from
        public void CopyFrom (RFReferenceDemolition referenceDemolitionDml)
        {
            reference    = referenceDemolitionDml.reference;
            if (referenceDemolitionDml.randomList != null && referenceDemolitionDml.randomList.Count > 0)
            {
                if (randomList == null)
                    randomList = new List<GameObject>();
                randomList = referenceDemolitionDml.randomList;
            }
            addRigid     = referenceDemolitionDml.addRigid;
            inheritScale = referenceDemolitionDml.inheritScale;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////   
        
        // Get reference
        public GameObject GetReference()
        {
            // Return single ref
            if (reference != null && randomList.Count == 0)
                return reference;

            // Get random ref
            List<GameObject> refs = new List<GameObject>();
            if (randomList.Count > 0)
            {
                for (int i = 0; i < randomList.Count; i++)
                    if (randomList[i] != null)
                        refs.Add (randomList[i]);
                if (refs.Count > 0)
                    return refs[Random.Range (0, refs.Count)];
            }

            return null;
        }
        
        // Demolish object to reference
        public static bool DemolishReference (RayfireRigid scr)
        {
            if (scr.demolitionType == DemolitionType.ReferenceDemolition)
            {
                // Demolished
                scr.limitations.demolished = true;
                
                // Turn off original
                scr.gameObject.SetActive (false);
                
                // Get instance
                GameObject refGo = scr.referenceDemolition.GetReference();
                
                // Has no reference
                if (refGo == null)
                    return true;
                
                // Instantiate turned off reference with null parent
                GameObject instGo = Object.Instantiate (refGo, scr.transForm.position, scr.transForm.rotation);
                instGo.name = refGo.name;
                
                // Set root to manager or to the same parent
                if (RayfireMan.inst != null && RayfireMan.inst.advancedDemolitionProperties.parent == RFManDemolition.FragmentParentType.Manager)
                    instGo.transform.parent = RayfireMan.inst.transform;
                else
                    instGo.transform.parent = scr.transForm.parent;
                
                // Set tm
                scr.rootChild = instGo.transform;
                
                // Copy scale
                if (scr.referenceDemolition.inheritScale == true)
                    scr.rootChild.localScale = scr.transForm.localScale;

                // Clear list for fragments
                scr.fragments = new List<RayfireRigid>();
                
                // Check root for rigid props
                RayfireRigid refScr = instGo.gameObject.GetComponent<RayfireRigid>();

                // Reference Root has not rigid. Add to
                if (refScr == null && scr.referenceDemolition.addRigid == true)
                {
                    // Add rigid and copy
                    refScr = instGo.gameObject.AddComponent<RayfireRigid>();

                    // Copy rigid
                    scr.CopyPropertiesTo (refScr);

                    // Copy particles
                    RFParticles.CopyParticles (scr, refScr);   
                    
                    // Single mesh TODO improve
                    if (instGo.transform.childCount == 0)
                    {
                        refScr.objectType = ObjectType.Mesh;
                    }

                    // Multiple meshes
                    if (instGo.transform.childCount > 0)
                    {
                        refScr.objectType = ObjectType.MeshRoot;
                    }
                }

                // Activate and init rigid
                instGo.transform.gameObject.SetActive (true);

                // Reference has rigid
                if (refScr != null)
                {
                    // Init if not initialized yet
                    refScr.Initialize();
                    
                    // Create rigid for root children
                    if (refScr.objectType == ObjectType.MeshRoot)
                    {
                        for (int i = 0; i < refScr.fragments.Count; i++)
                            refScr.fragments[i].limitations.currentDepth++;
                        scr.fragments.AddRange (refScr.fragments);
                        scr.DestroyRigid (refScr);
                    }

                    // Get ref rigid
                    else if (refScr.objectType == ObjectType.Mesh ||
                             refScr.objectType == ObjectType.SkinnedMesh)
                    {
                        refScr.meshDemolition.runtimeCaching.type = CachingType.Disable;
                        RFDemolitionMesh.DemolishMesh(refScr);
                        
                        // TODO COPY MESH DATA FROM ROOTSCR TO THIS TO REUSE
                        
                        scr.fragments.AddRange (refScr.fragments);
                        
                        
                        RayfireMan.DestroyFragment (refScr, refScr.rootParent, 1f);
                    }

                    // Get ref rigid
                    else if (refScr.objectType == ObjectType.NestedCluster ||
                             refScr.objectType == ObjectType.ConnectedCluster)
                    {
                        refScr.Default();
                        
                        // Copy contact data
                        refScr.limitations.contactPoint   = scr.limitations.contactPoint;
                        refScr.limitations.contactVector3 = scr.limitations.contactVector3;
                        refScr.limitations.contactNormal  = scr.limitations.contactNormal;
                        
                        // Demolish
                        RFDemolitionCluster.DemolishCluster (refScr);
                        
                        // Collect new fragments
                        scr.fragments.AddRange (refScr.fragments);
                        
                        
                        //refScr.physics.exclude = true;
                        //RayfireMan.DestroyFragment (refScr, refScr.rootParent, 1f);
                    }
                }
            }

            return true;
        }
    }
}