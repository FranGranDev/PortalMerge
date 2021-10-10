using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if (UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID)
using RayFire.DotNet;

namespace RayFire
{
    // Static class to handle all shatter methods
    public static class RFFragment
    {
        
        static List<Mesh>                 meshListStatic   = new List<Mesh>();
        static List<Vector3>              pivotListStatic  = new List<Vector3>();
        static List<Dictionary<int, int>> subIdsListStatic = new List<Dictionary<int, int>>();
        
        /// /////////////////////////////////////////////////////////
        /// Shatter
        /// /////////////////////////////////////////////////////////

        // Cache for shatter
        public static void CacheMeshes(ref Mesh[] meshes, ref Vector3[] pivots, ref List<RFDictionary> origSubMeshIdsRf, RayfireShatter scrShatter)
        {
            // TODO check vars by type: slice list, etc
            
            // Turn off fast mode for tets and slices
            int shatterMode = GetShatterMode(scrShatter);

            // Get mesh
            Mesh mesh = GetDemolitionMesh(scrShatter);;

            // Decompose in Editor only, slice runtime only
            FragmentMode mode = scrShatter.mode;
            if (scrShatter.type == FragType.Decompose) // TODO FIX
                mode = FragmentMode.Editor;
            if (scrShatter.type == FragType.Slices)
                mode = FragmentMode.Runtime;
            
            // Set up shatter
            RFShatter shatter = SetShatter(
                shatterMode, 
                mesh, 
                scrShatter.transform, 
                scrShatter.material, 
                scrShatter.advanced.decompose, 
                scrShatter.advanced.removeCollinear, 
                scrShatter.advanced.seed, 
                mode, 
                scrShatter.advanced.inputPrecap, 
                scrShatter.advanced.outputPrecap, 
                scrShatter.advanced.removeDoubleFaces, 
                scrShatter.advanced.excludeInnerFragments,
                scrShatter.advanced.elementSizeThreshold);
            
            // Failed input
            if (shatter == null)
            {
                meshes = null;
                pivots = null;
                return;
            }

            // Get innerSubId
            int innerSubId = RFSurface.SetInnerSubId(scrShatter);
            
            // Set fragmentation properties
            SetFragmentProperties (shatter, scrShatter, null);

            // Custom points check
            if (scrShatter.type == FragType.Custom && scrShatter.custom.noPoints == true)
            {
                meshes = null;
                pivots = null;
                Debug.Log ("No custom ponts");
                return;
            }

            // Calculate fragments
            List<Dictionary<int, int>> origSubMeshIds = new List<Dictionary<int, int>>();
            bool successState = Compute(
                shatterMode, 
                shatter, 
                scrShatter.transform, 
                ref meshes, 
                ref pivots, 
                mesh, 
                innerSubId, 
                ref origSubMeshIds, 
                scrShatter);
            
            // Create RF dictionary
            origSubMeshIdsRf = new List<RFDictionary>();
            for (int i = 0; i < origSubMeshIds.Count; i++)
                origSubMeshIdsRf.Add(new RFDictionary(origSubMeshIds[i]));
            
            // Failed fragmentation. Increase bad mesh 
            if (successState == false)
            {
                Debug.Log ("Bad shatter output mesh: " + scrShatter.name);
            }
            else
                for (int i = 0; i < meshes.Length; i++)
                    meshes[i].name = scrShatter.name + "_" + i;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Rigid
        /// /////////////////////////////////////////////////////////
        
        // Prepare rigid component to cache fragment meshes
        public static bool InputMesh(RayfireRigid scr)
        {
            // Set up shatter
            if (SetRigidShatter(scr) == false)
                return false;

            // Get innerSubId
            scr.meshDemolition.innerSubId = RFSurface.SetInnerSubId(scr);
            
            // Set fragmentation properties
            SetFragmentProperties (scr.meshDemolition.rfShatter, scr.meshDemolition.scrShatter, scr);

            return true;
        }

        // Set up rigid shatter
        static bool SetRigidShatter(RayfireRigid scr)
        {
            // Set up shatter
            if (scr.meshDemolition.rfShatter == null)
            {
                // Save rotation at caching to fix fragments rotation at demolition
                scr.cacheRotation = scr.transForm.rotation;
                
                // Turn off fast mode for tets and slices
                scr.meshDemolition.shatterMode = GetShatterMode(scr.meshDemolition.scrShatter);
    
                // Get innerSubId
                scr.meshDemolition.mesh = GetDemolitionMesh(scr);
                
                // Get shatter
                scr.meshDemolition.rfShatter = SetShatter (
                    scr.meshDemolition.shatterMode,
                    scr.meshDemolition.mesh,
                    scr.transform,
                    scr.materials,
                    scr.meshDemolition.properties.decompose,
                    scr.meshDemolition.properties.removeCollinear,
                    scr.meshDemolition.seed,
                    FragmentMode.Runtime,
                    false,
                    false,
                    false,
                    false,
                    3);
            }

            // Failed input. Instant bad mesh.
            if (scr.meshDemolition.rfShatter == null)
            {
                scr.limitations.demolitionShould = false;
                scr.meshDemolition.badMesh += 10;
                scr.meshDemolition.mesh = null;
                return false;
            }

            return true;
        }
        
        // Cache for rigid
        public static void CacheMeshesInst(ref Mesh[] meshes, ref Vector3[] pivots, ref List<RFDictionary> origSubMeshIdsRf, RayfireRigid scrRigid)
        {
            // Local data lists
            List<Dictionary<int, int>> origSubMeshIds = new List<Dictionary<int, int>>();
            
            // Calculate fragments
            bool successState = Compute(
                scrRigid.meshDemolition.shatterMode, 
                scrRigid.meshDemolition.rfShatter, 
                scrRigid.transform, 
                ref meshes, 
                ref pivots, 
                scrRigid.meshDemolition.mesh, 
                scrRigid.meshDemolition.innerSubId, 
                ref origSubMeshIds, 
                scrRigid);

            // Create RF dictionary
            if (origSubMeshIdsRf == null)
                origSubMeshIdsRf = new List<RFDictionary>();
            else
                origSubMeshIdsRf.Clear();
            for (int i = 0; i < origSubMeshIds.Count; i++)
                origSubMeshIdsRf.Add(new RFDictionary(origSubMeshIds[i]));
            
            // Final ops
            FinalCacheMeshes (ref meshes, scrRigid, successState);
        }
        
        // Cache for rigid
        public static void CacheMeshesMult(Transform tmSaved, ref List<Mesh> meshesList, ref List<Vector3> pivotsList, ref List<RFDictionary> subList, RayfireRigid scrRigid, List<int> batchAmount, int batchInd)
        {
            // Get list of meshes to calc
            List<int> markedElements = RFRuntimeCaching.GetMarkedElements (batchInd, batchAmount);
            
            // Local iteration data lists
            Mesh[] meshesLocal = new Mesh[batchAmount.Count];
            Vector3[] pivotsLocal = new Vector3[batchAmount.Count];
            List<Dictionary<int, int>> origSubMeshIds = new List<Dictionary<int, int>>();
            
            // Compute
            bool state = scrRigid.meshDemolition.rfShatter.SimpleCompute(
                tmSaved, 
                ref meshesLocal, 
                ref pivotsLocal, 
                scrRigid.meshDemolition.mesh, 
                scrRigid.meshDemolition.innerSubId, 
                ref origSubMeshIds, 
                markedElements, 
                batchInd == 0);
            
            // Set names
            if (state == false || meshesLocal == null || meshesLocal.Length == 0)
                return;

            // Set names
            for (int i = 0; i < meshesLocal.Length; i++)
            {
                meshesLocal[i].RecalculateTangents();
                meshesLocal[i].name = scrRigid.name + "_fr"; // + markedElements[i].ToString();
            }

            // Add data to main lists
            for (int i = 0; i < origSubMeshIds.Count; i++)
                subList.Add(new RFDictionary(origSubMeshIds[i]));
            meshesList.AddRange (meshesLocal);
            pivotsList.AddRange (pivotsLocal);
        }
        
        // Final step Cache for rigid
        static void FinalCacheMeshes (ref Mesh[] meshes, RayfireRigid scrRigid, bool successState)
        {
            // Failed fragmentation. Increase bad mesh 
            if (successState == false)
            {
                scrRigid.meshDemolition.badMesh++;
                Debug.Log("Bad mesh: " + scrRigid.name);
            }
            else
                for (int i = 0; i < meshes.Length; i++)
                    meshes[i].name = scrRigid.name + "_" + i;
        }

        // Get demolition mesh
        static Mesh GetDemolitionMesh(RayfireRigid scr)
        {
            if (scr.skinnedMeshRend != null)
                //return scr.skinnedMeshRend.sharedMesh;
                return RFMesh.BakeMesh (scr.skinnedMeshRend);
            return scr.meshFilter.sharedMesh;
        }
        
        // Get demolition mesh
        static Mesh GetDemolitionMesh(RayfireShatter scr)
        {
            if (scr.skinnedMeshRend != null)
                return RFMesh.BakeMesh (scr.skinnedMeshRend);
            return scr.meshFilter.sharedMesh;
        }

        /// /////////////////////////////////////////////////////////
        /// Slice
        /// /////////////////////////////////////////////////////////
        
        // Cache for slice
        public static void SliceMeshes(ref Mesh[] meshes, ref Vector3[] pivots, ref List<RFDictionary> origSubMeshIdsRf, RayfireRigid scr, List<Vector3> sliceData)
        {
            // Get mesh
            scr.meshDemolition.mesh = GetDemolitionMesh(scr);
            
            // Set up shatter
            RFShatter shatter = SetShatter(
                2, 
                scr.meshDemolition.mesh, 
                scr.transform, 
                scr.materials, 
                true, 
                scr.meshDemolition.properties.removeCollinear, 
                scr.meshDemolition.seed, 
                FragmentMode.Runtime, // TODO EDITOR???
                true,
                false,
                false,
                false,
                3);

            // Debug.Log ("slice");
            
            // Failed input
            if (shatter == null)
            {
                meshes = null;
                pivots = null;
                scr.meshDemolition.badMesh++;
                return;
            }

            // Get innerSubId
            int innerSubId = RFSurface.SetInnerSubId(scr);
            
            // Get slice data
            List<Vector3> points = new List<Vector3>();
            List<Vector3> norms = new List<Vector3>();
            for (int i = 0; i < sliceData.Count; i++)
            {
                points.Add(sliceData[i]);
                norms.Add(sliceData[i+1]);
                i++;
            }
            
            // Set params
            shatter.SetBricksParams(points.ToArray(), norms.ToArray(), scr.transform);
            
            // Calculate fragments
            List<Dictionary<int, int>> origSubMeshIds = new List<Dictionary<int, int>>();
            bool successState = Compute(
                2, 
                shatter, 
                scr.transform, 
                ref meshes, 
                ref pivots, 
                scr.meshDemolition.mesh,
                innerSubId, 
                ref origSubMeshIds, 
                scr.gameObject);
            
            // Create RF dictionary
            origSubMeshIdsRf = new List<RFDictionary>();
            for (int i = 0; i < origSubMeshIds.Count; i++)
                origSubMeshIdsRf.Add(new RFDictionary(origSubMeshIds[i]));
            
            // Failed fragmentation. Increase bad mesh 
            if (successState == false)
            {
                scr.meshDemolition.badMesh++;
                Debug.Log("Bad mesh: " + scr.name, scr.gameObject);
            }
            else
                for (int i = 0; i < meshes.Length; i++)
                    meshes[i].name = scr.name + "_" + i;
        }

        /// /////////////////////////////////////////////////////////
        /// Compute
        /// /////////////////////////////////////////////////////////
        
        // Compute
        static bool Compute(int shatterMode, RFShatter shatter, Transform tm, ref Mesh[] meshes, ref Vector3[] pivots, 
            Mesh mesh, int innerSubId, ref List<Dictionary<int, int>> subIds, Object obj, List<int> markedElements = null)
        {
            // Compute fragments
            bool state = shatterMode == 0 
                ? shatter.Compute(tm, ref meshes, ref pivots, mesh, innerSubId, ref subIds) 
                : shatter.SimpleCompute(tm, ref meshes, ref pivots, mesh, innerSubId, ref subIds, markedElements);

            // Failed fragmentation
            if (state == false)
            {
                meshes = null;
                pivots = null;
                subIds = new List<Dictionary<int, int>>();
                return false;
            }
            
            // Null check
            if (meshes == null)
            {
                Debug.Log("Null mesh", obj);
                meshes = null;
                pivots = null;
                subIds = new List<Dictionary<int, int>>();
                return false;
            }

            //Debug.Log (meshes.Length);
            
            // Empty mesh fix
            if (EmptyMeshState(meshes) == true)
            {
                for (int i = 0; i < meshes.Length; i++)
                {
                    if (meshes[i].vertexCount > 2)
                    {
                        meshListStatic.Add(meshes[i]);
                        pivotListStatic.Add(pivots[i]);
                        subIdsListStatic.Add (subIds[i]);
                    }
                }

                pivots = pivotListStatic.ToArray();
                meshes = meshListStatic.ToArray();
                subIds = subIdsListStatic;
                meshListStatic.Clear();
                pivotListStatic.Clear();
                subIdsListStatic.Clear();
                Debug.Log("Empty Mesh", obj);
            }
            
            // Single mesh after mesh fix check
            if (meshes.Length <= 1)
            {
                Debug.Log("Mesh amount " + meshes.Length, obj);
                meshes = null;
                pivots = null;
                subIds = new List<Dictionary<int, int>>();
                return false;
            }

            // TODO set in library
            for (int i = 0; i < meshes.Length; i++)
            {
                meshes[i].RecalculateTangents();
            }

            return true;
        }
        
        // Get shatter mode
        static int GetShatterMode(RayfireShatter scrShatter = null)
        {
            // Simple voronoi
            if (scrShatter == null)
                return 1;
            
            // Always 2
            if (scrShatter.type == FragType.Slices) 
                return 2;
            if (scrShatter.type == FragType.Decompose) 
                return 1;
            
            // Turn off fast mode for tests and radial
            int shatterMode = scrShatter.shatterMode;
            if (scrShatter.type == FragType.Custom) 
                shatterMode = 0;
            if (scrShatter.type == FragType.Tets) 
                shatterMode = 0;
            
            // Classic way for clustering. Not for slices
            if (scrShatter.clusters.enable == true)
                shatterMode = 0;
            
            return shatterMode;
        }

        // Check for at least one empty mesh in cached meshes
        static bool EmptyMeshState(Mesh[] meshes)
        {
            for (int i = 0; i < meshes.Length; i++)
                if (meshes[i].vertexCount <= 2)
                    return true; 
            return false;
        }
        
         // Set fragmentation properties
        static void SetFragmentProperties(RFShatter shatter, RayfireShatter scrSh, RayfireRigid scrRigid)
        {
            // Rigid demolition without shatter. Set and exit.
            if (scrRigid != null && scrSh == null)
            {
                // Get final amount
                int percVar = Random.Range(0, scrRigid.meshDemolition.amount * scrRigid.meshDemolition.variation / 100);
                scrRigid.meshDemolition.totalAmount = scrRigid.meshDemolition.amount + percVar;

                // Set Voronoi Uniform properties
                SetVoronoi (shatter, scrRigid.meshDemolition.totalAmount, scrRigid.transform, scrRigid.limitations.contactVector3, scrRigid.meshDemolition.contactBias);
                return;
            }

            // Rigid demolition with shatter. 
            if (scrRigid != null && scrSh != null)
            {
                // Set Contact point to shatter component
                scrSh.centerPosition = scrRigid.transForm.InverseTransformPoint (scrRigid.limitations.contactVector3);
                
                // Set total amount by rigid component
                if (scrSh.type == FragType.Voronoi)
                    scrRigid.meshDemolition.totalAmount = scrSh.voronoi.Amount;
                else if (scrSh.type == FragType.Splinters)
                    scrRigid.meshDemolition.totalAmount = scrSh.splinters.Amount;
                else if (scrSh.type == FragType.Slabs)
                    scrRigid.meshDemolition.totalAmount = scrSh.slabs.Amount;
                else if (scrSh.type == FragType.Radial)
                    scrRigid.meshDemolition.totalAmount = scrSh.radial.rings * scrSh.radial.rays;
            }
            
            // Shatter fragmentation
            if (scrSh != null)
            {
                // Center position and direction
                Vector3 centerPos = scrSh.transform.TransformPoint (scrSh.centerPosition);

                // Set properties
                if (scrSh.type == FragType.Voronoi)
                    SetVoronoi (shatter, scrSh.voronoi.Amount, scrSh.transform, centerPos, scrSh.voronoi.centerBias);
                else if (scrSh.type == FragType.Splinters)
                    SetSplinters (shatter, scrSh.splinters, scrSh.transform, centerPos, scrSh.splinters.centerBias);
                else if (scrSh.type == FragType.Slabs)
                    SetSlabs (shatter, scrSh.slabs, scrSh.transform, centerPos, scrSh.splinters.centerBias);
                else if (scrSh.type == FragType.Radial)
                    SetRadial (shatter, scrSh.radial, scrSh.transform, centerPos, scrSh.centerDirection);
                else if (scrSh.type == FragType.Custom) 
                    SetCustom (shatter, scrSh.custom, scrSh.transform, scrSh.meshFilter, scrSh.bound, scrSh.splinters, scrSh.slabs, scrSh.advanced.seed);
                else if (scrSh.type == FragType.Slices)
                    SetSlices (shatter, scrSh.transform, scrSh.slice);
                else if (scrSh.type == FragType.Tets)
                    SetTet (shatter, scrSh.bound, scrSh.tets);
                else if (scrSh.type == FragType.Decompose)
                    SetDecompose (shatter);

                // Clustering
                if (scrSh.clusters.enable == true)
                    SetClusters (shatter, scrSh.clusters);
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Properties setup
        /// /////////////////////////////////////////////////////////

        // Set common fragmentation properties
        static RFShatter SetShatter (int shatterMode, Mesh mesh, Transform transform, RFSurface interior, 
            bool decompose, bool deleteCol, int seed = 1, FragmentMode mode = FragmentMode.Runtime, 
            bool preCap = true, bool remCap = false, bool remDbl = true, bool exInside = false, int percSize = 3)
        {
            // Creating shatter
            RFShatter shatter = new RFShatter((RFShatter.RFShatterMode)shatterMode, true);
            
            // Safe/unsafe properties
            if (mode == FragmentMode.Editor)
            {
                float sizeFilter = mesh.bounds.size.magnitude * percSize / 100f; // TODO check render bound size
                SetShatterEditorMode(shatter, sizeFilter, preCap, remCap, remDbl, exInside);
            }
            else
                SetShatterRuntimeMode (shatter);

            // Detach by elements
            shatter.DecomposeResultMesh(decompose);
            
            // Set properties
            shatter.SetFragmentParameter(RFShatter.FragmentParams.seed, seed);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.pre_weld_threshold, 0.001f);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.delete_collinear, deleteCol);
            
            // Other
            shatter.SetGeneralParameter(RFShatter.GeneralParams.maping_scale, interior.mappingScale);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.restore_normals, true);

            // Setting shatter params
            bool inputState = shatter.SetInputMesh(transform, mesh);

            // Failed input
            if (inputState == false)
            {
                Debug.Log("Bad input mesh: " + transform.name, transform.gameObject);
                return null;
            }

            return shatter;
        }

        // Set Shatter Editor Mode properties
        static void SetShatterEditorMode(RFShatter shatter, float sizeFilter, bool preCap, bool remCap, bool remDbl, bool exInside)
        {
            shatter.EditorMode(true);
            
            shatter.SetGeneralParameter(RFShatter.GeneralParams.editor_mode_pre_cap, preCap);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.editor_mode_remove_cap_faces, remCap);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.editor_mode_separate_only, false);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.editor_mode_elliminateCollinears_maxIterFuse, 150);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.editor_mode_min_bbox_diag_size_filter, sizeFilter);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.editor_mode_exclude_inside, exInside);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.editor_mode_remove_double_faces, remDbl);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.editor_mode_remove_inversed_double_faces, remDbl);

            shatter.SetGeneralParameter(RFShatter.GeneralParams.minFacesFilter, 0);
        }
        
        // Set Shatter Runtime Mode properties
        static void SetShatterRuntimeMode(RFShatter shatter)
        {
            shatter.EditorMode(false);
            //shatter.SetGeneralParameter(RFShatter.GeneralParams.pre_shatter, true);
            //shatter.SetGeneralParameter(RFShatter.GeneralParams.pre_cap,     true);
            //shatter.SetGeneralParameter(RFShatter.GeneralParams.pre_weld,    true);
            
            // TODO tests vals
            shatter.SetGeneralParameter(RFShatter.GeneralParams.pre_shatter, true);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.pre_cap,     true);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.pre_weld,    true);
            
            shatter.SetGeneralParameter(RFShatter.GeneralParams.minFacesFilter, 3);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Fragmentation types
        /// /////////////////////////////////////////////////////////
        
        // Set Uniform
        static void SetVoronoi(RFShatter shatter, int numFragments, Transform tm, Vector3 centerPos, float centerBias)
        {
            // Get amount
            int amount = numFragments;
            if (amount < 1)
                amount = 1;
            if (amount > 20000)
                amount = 2;
            
            // Set properties
            shatter.SetFragmentParameter(RFShatter.FragmentParams.type, (int)RFShatter.FragmentType.voronoi);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_type, (int)RFShatter.VoronoiType.irregular);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_irr_num, amount);
            
            // Set bias to center
            if (centerBias > 0)
            {
                shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_irr_bias, centerBias);
                shatter.SetCenterParameter(centerPos, tm, Vector3.forward);
            }
        }

        // Set Splinters
        static void SetSplinters(RFShatter shatter, RFSplinters splint, Transform tm, Vector3 centerPos, float centerBias)
        {
            // Set properties
            shatter.SetFragmentParameter(RFShatter.FragmentParams.type, (int)RFShatter.FragmentType.voronoi);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_type, (int)RFShatter.VoronoiType.irregular);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_irr_num, splint.Amount);

            // Set center
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_irr_bias, centerBias);
            shatter.SetCenterParameter(centerPos, tm, Vector3.forward);

            // Set Stretching for slabs
            SetStretching (shatter, splint.axis, splint.strength, FragType.Splinters);
        }
        
        // Set Slabs
        static void SetSlabs(RFShatter shatter, RFSplinters slabs, Transform tm, Vector3 centerPos, float centerBias)
        {
            // Set properties
            shatter.SetFragmentParameter(RFShatter.FragmentParams.type, (int)RFShatter.FragmentType.voronoi);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_type, (int)RFShatter.VoronoiType.irregular);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_irr_num, slabs.Amount);
            
            // Set center
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_irr_bias, centerBias);
            shatter.SetCenterParameter(centerPos, tm, Vector3.forward);

            // Set Stretching for slabs
            SetStretching (shatter, slabs.axis, slabs.strength, FragType.Slabs);
        }

        // Set Radial
        static void SetRadial(RFShatter shatter, RFRadial radial, Transform tm, Vector3 centerPos, Quaternion centerDirection)
        {
            // Set radial properties
            shatter.SetFragmentParameter(RFShatter.FragmentParams.type, (int)RFShatter.FragmentType.voronoi);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_type, (int)RFShatter.VoronoiType.radial);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_rad_radius, radial.radius);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_rad_divergence, radial.divergence);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_rad_restrict, radial.restrictToPlane);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_rad_rings_count, radial.rings);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_rad_rings_focus, radial.focus);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_rad_rings_strenght, radial.focusStr);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_rad_rings_random, radial.randomRings);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_rad_rays_count, radial.rays);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_rad_rays_random, radial.randomRays);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_rad_rays_twist, radial.twist);

            // Get direction axis
            Vector3 directionAxis = DirectionAxis(radial.centerAxis);
            Vector3 centerRot = tm.rotation * centerDirection * directionAxis;
            shatter.SetCenterParameter(centerPos, tm, centerRot);
        }

        // Set custom point cloud
        static void SetCustom(RFShatter shatter, RFCustom custom, Transform tm, MeshFilter mf, Bounds bound, RFSplinters splint, RFSplinters slabs, int seed)
        {
            // Set properties
            shatter.SetFragmentParameter(RFShatter.FragmentParams.type, (int)RFShatter.FragmentType.voronoi);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.voronoi_type, (int)RFShatter.VoronoiType.custom);

            // Get Point Cloud
            List<Vector3> pointCloud = GetCustomPointCLoud (custom, tm, seed, bound);
            
            // Set points
            shatter.SetVoroCustomPoints(pointCloud.ToArray(), tm);
            
            // Set Stretching TODO point cloud rescale by transform
            // if (custom.modifier == RFCustom.RFModifierType.Splinters)
            //     SetStretching (shatter, splint.axis, splint.strength, FragType.Splinters);
            // else if (custom.modifier == RFCustom.RFModifierType.Slabs)
            //     SetStretching (shatter, slabs.axis, slabs.strength, FragType.Slabs);
        }

        // Set slicing objects
        static void SetSlices(RFShatter shatter, Transform tm, RFSlice slices)
        {
            // Filter 
            List<Transform> list = new List<Transform>();
            for (int i = 0; i < slices.sliceList.Count; i++)
                if (slices.sliceList[i] != null)
                    list.Add(slices.sliceList[i]);

            // No objects
            if (list.Count == 0)
                return;

            // Get slice data
            Vector3[] points = list.Select(t => t.position).ToArray();
            Vector3[] norms = list.Select(t => slices.Axis(t)).ToArray();

            // Set params
            shatter.SetBricksParams(points, norms, tm);
        }

        // Set Custom Voronoi properties
        static void SetTet(RFShatter shatter, Bounds bounds, RFTets tets)
        {
            // Main
            shatter.SetFragmentParameter(RFShatter.FragmentParams.type, (int)RFShatter.FragmentType.tetra);
            shatter.SetFragmentParameter(RFShatter.FragmentParams.tetra_type, (int)tets.lattice);
            
            // Get max
            float max = bounds.size.x;
            if (bounds.size.y > max)
                max = bounds.size.y;
            if (bounds.size.z > max)
                max = bounds.size.z;
            if (max == 0)
                max = 0.01f;
            
            // Get density
            Vector3Int density = new Vector3Int(
                (int)Mathf.Ceil (bounds.size.x / max * tets.density), 
                (int)Mathf.Ceil (bounds.size.y / max * tets.density), 
                (int)Mathf.Ceil (bounds.size.z / max * tets.density));
            
            // Limit
            if (density.x > 30) 
                density.x = 30;
            else if (density.x < 1) 
                density.x = 1;
            if (density.y > 30) 
                density.y = 30;
            else if (density.y < 1) 
                density.y = 1;
            if (density.z > 30) 
                density.z = 30;
            else if (density.z < 1) 
                density.z = 1;

            // Set density
            shatter.SetPoint3Parameter((int)RFShatter.FragmentParams.tetra2_density, density);
            shatter.SetPoint3Parameter((int)RFShatter.FragmentParams.tetra1_density, density);
            
            // Noise
            shatter.SetFragmentParameter(RFShatter.FragmentParams.tetra_noise, tets.noise);
        }
        
        // Decompose to elements
        static void SetDecompose(RFShatter shatter)
        {
            shatter.SetGeneralParameter(RFShatter.GeneralParams.editor_mode_separate_only, true);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Custom
        /// /////////////////////////////////////////////////////////

        // Get final point cloud for custom fragmentation
        public static List<Vector3> GetCustomPointCLoud (RFCustom custom, Transform tm, int seed, Bounds bound)
        {
            // Get input points
            List<Vector3> inputPoints = GetCustomInputCloud (custom, tm);

            // Get final output point cloud
            List<Vector3> outputPoints = GetCustomOutputCloud (custom, inputPoints, seed, bound);
            
            // Get points in bound
            List<Vector3> boundPoints = GetCustomBoundPoints (outputPoints, bound);
            
            // Stop if no points
            if (boundPoints.Count <= 1)
                custom.noPoints = true;
            
            return boundPoints;
        }
        
        // Get custom input cloud
        static List<Vector3> GetCustomInputCloud(RFCustom custom, Transform tm)
        {
            // Vars
            custom.noPoints = false;
            List<Vector3> inputPoints = new List<Vector3> ();
            
            // Children transform
            if (custom.source == RFCustom.RFPointCloudSourceType.ChildrenTransform)
            {
                if (tm.childCount > 0)
                    for (int i = 0; i < tm.childCount; i++)
                        inputPoints.Add (tm.GetChild (i).position);
            }        
            
            // Transform array
            else if (custom.source == RFCustom.RFPointCloudSourceType.TransformArray)
            {
                if (custom.transforms != null && custom.transforms.Length > 0)
                    for (int i = 0; i < custom.transforms.Length; i++)
                         if (custom.transforms[i] != null)
                             inputPoints.Add (custom.transforms[i].position);
            }
            
            // Vector 3 array
            else if (custom.source == RFCustom.RFPointCloudSourceType.Vector3Array)
            {
                if (custom.vector3 != null && custom.vector3.Length > 0)
                    for (int i = 0; i < custom.vector3.Length; i++)
                        inputPoints.Add (custom.vector3[i]);
            }
            
            return inputPoints;
        }

        // Get final output point cloud
        static List<Vector3> GetCustomOutputCloud(RFCustom custom, List<Vector3> inputPoints, int seed, Bounds bound)
        {
            // Use same input point
            if (custom.useAs == RFCustom.RFPointCloudUseType.PointCloud)
                return inputPoints;
            
            // Volume around point
            if (custom.useAs == RFCustom.RFPointCloudUseType.VolumePoints)
            {
                // Stop if no points
                if (inputPoints.Count == 0)
                    return inputPoints;
                
                // Get amount of points in radius 
                int pointsPerPoint = custom.amount / inputPoints.Count;
                int localSeed = seed;
                
                // Generate new points around point
                List<Vector3> newPoints = new List<Vector3>();
                for (int p = 0; p < inputPoints.Count; p++)
                {
                    localSeed++;
                    Random.InitState (localSeed);
                    for (int i = 0; i < pointsPerPoint; i++)
                    {
                        Vector3 randomPoint = RandomPointInRadius (inputPoints[p], custom.radius);
                        if (bound.Contains (randomPoint) == false)
                        {
                            randomPoint = RandomPointInRadius (inputPoints[p], custom.radius);
                            if (bound.Contains (randomPoint) == false)
                                randomPoint = RandomPointInRadius (inputPoints[p], custom.radius);
                        }
                        newPoints.Add (randomPoint);
                    }
                }
                return newPoints;
            }
            return inputPoints;
        }
        
        // Filter world points by bound intersection
        static List<Vector3> GetCustomBoundPoints(List<Vector3> inputPoints, Bounds bound)
        {
            for (int i = inputPoints.Count - 1; i >= 0; i--)
                if (bound.Contains(inputPoints[i]) == false)
                    inputPoints.RemoveAt (i);
            return inputPoints;
        }

        // Random vector
        static Vector3 RandomVector()
        {
            return new Vector3(Random.Range (-1f, 1f), Random.Range (-1f, 1f), Random.Range (-1f, 1f));
        }
        
        // Random point in radius around input point
        static Vector3 RandomPointInRadius(Vector3 point, float radius)
        {
            return RandomVector() * Random.Range (0f, radius) + point;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Clusters
        /// /////////////////////////////////////////////////////////
        
        // Set clusters
        static void SetClusters(RFShatter shatter, RFShatterCluster gluing)
        {
            shatter.InitClustering(true);
            shatter.SetClusterParameter(RFShatter.ClusterParams.enabled, true);
            shatter.SetClusterParameter(RFShatter.ClusterParams.by_pcloud_count, gluing.count);
            shatter.SetClusterParameter(RFShatter.ClusterParams.options_seed, gluing.seed);
            shatter.SetClusterParameter(RFShatter.ClusterParams.preview_scale, 100f);
            
            // Debris
            shatter.SetClusterParameter(RFShatter.ClusterParams.debris_layers_count, gluing.layers);
            shatter.SetClusterParameter(RFShatter.ClusterParams.debris_count, gluing.amount);
            shatter.SetClusterParameter(RFShatter.ClusterParams.debris_scale, gluing.scale);
            shatter.SetClusterParameter(RFShatter.ClusterParams.debris_min, gluing.min);
            shatter.SetClusterParameter(RFShatter.ClusterParams.debris_max, gluing.max);
            shatter.SetClusterParameter(RFShatter.ClusterParams.debris_tessellate, false);
            shatter.SetClusterParameter(RFShatter.ClusterParams.debris_remove, false);
                
            // Glue 
            shatter.SetGeneralParameter(RFShatter.GeneralParams.glue, true);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.glue_weld_threshold, 0.001f);
            shatter.SetGeneralParameter(RFShatter.GeneralParams.relax, gluing.relax);
        }

        /// /////////////////////////////////////////////////////////
        /// Stretching
        /// /////////////////////////////////////////////////////////
        
        // Set stretching
        static void SetStretching(RFShatter shatter, AxisType axis, float strength, FragType fragType)
        {
            // Get slab vector
            Vector3 stretchDir = DirectionAxis(axis);

            // Adjust for slabs
            if (fragType == FragType.Slabs)
            {
                Vector3 vector = new Vector3();
                if (stretchDir.x <= 0)
                    vector.x = 1f;
                if (stretchDir.x >= 1f)
                    vector.x = 0;
                if (stretchDir.y <= 0)
                    vector.y = 1f;
                if (stretchDir.y >= 1f)
                    vector.y = 0;
                if (stretchDir.z <= 0)
                    vector.z = 1f;
                if (stretchDir.z >= 1f)
                    vector.z = 0;
                stretchDir = vector;
            }

            // Set stretch vector
            shatter.SetPoint3Parameter((int)RFShatter.FragmentParams.stretching, stretchDir * Mathf.Lerp(40f, 99f, strength));
        }
        
        // Get axis by type
        static Vector3 DirectionAxis(AxisType axisType)
        {
            if (axisType == AxisType.YGreen)
                return Vector3.up;
            if (axisType == AxisType.ZBlue)
                return Vector3.forward;
            return Vector3.right;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Mesh
        /// /////////////////////////////////////////////////////////
        
        // Scale mesh
        public static void RescaleMesh(Mesh mesh, float scale)
        {
            Vector3[] verts = mesh.vertices;
            for (int j = 0; j < verts.Length; j++)
                verts[j] /= scale;
            mesh.SetVertices (verts.ToList());
        }
    }
}

// Static dummy class for other platforms
#else 

namespace RayFire
{
    public static class RFFragment
    {
        public static bool PrepareCacheMeshes(RayfireRigid scr)
        {
            BuildTest(scr);
            return false;
        }

        public static void CacheMeshesMult(Transform tmSaved, ref List<Mesh> meshesList, ref List<Vector3> pivotsList, ref List<RFDictionary> subList, RayfireRigid scrRigid, List<int> batchAmount, int batchInd) 
        {
            BuildTest();
        }

        public static void CacheMeshesInst(ref Mesh[] meshes, ref Vector3[] pivots, ref List<RFDictionary> origSubMeshIdsRf, RayfireRigid scrRigid)  
        {
            BuildTest();
        }

        public static void CacheMeshes(ref Mesh[] meshes, ref Vector3[] pivots, ref List<RFDictionary> origSubMeshIdsRf, RayfireShatter scrShatter)  
        {
            BuildTest();
        }

        public static void SliceMeshes(ref Mesh[] meshes, ref Vector3[] pivots, ref List<RFDictionary> origSubMeshIdsRf, RayfireRigid scrRigid, List<Vector3> sliceData)  
        {
            BuildTest();
        }

        public static void RescaleMesh (Mesh mesh, float scale)
        {
            BuildTest();
        }

        public static bool InputMesh(RayfireRigid scr)
        {
            BuildTest();
            return false;
        }
        
        static void BuildTest(RayfireRigid scr)
        {
            //Debug.Log ("Dummy");
        }
        
        static void BuildTest()
        {
            //Debug.Log ("Dummy");
        }
        
    }

    public class RFShatter{}
}

#endif