using System.Linq;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Shatter fragment's output size filter wit low value to delete very small pieces

namespace RayFire
{
    [AddComponentMenu("RayFire/Rayfire Shatter")]
    [HelpURL("http://rayfirestudios.com/unity-online-help/unity-shatter-component/")]
    public class RayfireShatter : MonoBehaviour
    {
        enum PrefabMode
        {
        	Scene,
        	Asset,
        	PrefabEditingMode
        }
        
        [Header ("  Fragments")]
        [Space (2)]
        
        public FragType    type = FragType.Voronoi;
        [Space (2)]
        
        public RFVoronoi   voronoi   = new RFVoronoi();
        [Space (2)]
        public RFSplinters splinters = new RFSplinters();
        [Space (2)]
        public RFSplinters slabs     = new RFSplinters();
        [Space (2)]
        public RFRadial    radial    = new RFRadial();
        [Space (2)]
        public RFCustom    custom = new RFCustom();
        [Space (2)]
        public RFSlice     slice = new RFSlice();
        [Space (2)]
        public RFTets      tets  = new RFTets();

        [Header ("  Properties")]
        [Space (2)]
        
		[Tooltip ("Editor: Allows to fragment complex multi element hi poly meshes with topology issues like open edges and unwelded vertices.")]
		public FragmentMode mode = FragmentMode.Editor;
		[Space (2)]
        
        public RFSurface material = new RFSurface();
        public RFShatterCluster clusters = new RFShatterCluster();
        public RFShatterAdvanced advanced = new RFShatterAdvanced();

        [Header ("  Export to asset")]
        [Space (2)]
        
        public RFMeshExport export = new RFMeshExport();

        [Header("Center")]
        [HideInInspector] public bool       showCenter;
        [HideInInspector] public Vector3    centerPosition;
        [HideInInspector] public Quaternion centerDirection;

        [Header("Components")]
        [HideInInspector] public Transform           transForm;
        [HideInInspector] public MeshFilter          meshFilter;
        [HideInInspector] public MeshRenderer        meshRenderer;
        [HideInInspector] public SkinnedMeshRenderer skinnedMeshRend;

        [Header("Variables")]
        [HideInInspector] public Mesh[]             meshes           = null;
        [HideInInspector] public Vector3[]          pivots           = null;
        [HideInInspector] public List<Transform>    rootChildList    = new List<Transform>();
        [HideInInspector] public List<GameObject>   fragmentsAll     = new List<GameObject>();
        [HideInInspector] public List<GameObject>   fragmentsLast    = new List<GameObject>();
        [HideInInspector] public List<RFDictionary> origSubMeshIdsRF = new List<RFDictionary>();

        // Hidden
        [HideInInspector] public int   shatterMode  = 1;
        [HideInInspector] public bool  colorPreview = false;
        [HideInInspector] public bool  scalePreview = true;
        [HideInInspector] public float previewScale = 0f;
        [HideInInspector] public float size = 0f;
        [HideInInspector] public float rescaleFix = 1f;
        [HideInInspector] public Vector3 originalScale;
        [HideInInspector] public Bounds bound;
        
        static float minSize = 0.01f;
        
        // Preview variables
        [HideInInspector] public bool resetState = false;
        
        /// /////////////////////////////////////////////////////////
        /// Common
        /// /////////////////////////////////////////////////////////

        // Reset
        private void Reset()
        {
            ResetCenter();
        }

        // Set default vars before fragment
        void SetVariables()
        {
            size          = 0f;
            rescaleFix    = 1f;
            originalScale = transForm.localScale;
        }
        
        // Cache variables
        bool DefineComponents()
        {
            // Check if prefab
            if (gameObject.scene.rootCount == 0)
            {
                Debug.Log ("Shatter component unable to fragment prefab because prefab unable to store Unity mesh. Fragment prefab in scene.");
                return false;
            }

            // Mesh storage 
            meshFilter = GetComponent<MeshFilter>();
            skinnedMeshRend = GetComponent<SkinnedMeshRenderer>();

            // 
            if (meshFilter == null && skinnedMeshRend == null)
            {
              Debug.Log ("No mesh"); 
              return false;
            }
            
            if (meshFilter != null && meshFilter.sharedMesh == null)
            {
              Debug.Log ("No mesh");  
              return false;
            }
              
            if (skinnedMeshRend != null && skinnedMeshRend.sharedMesh == null)
            {
                  Debug.Log ("No mesh"); 
                  return false;
            }

            // Not readable mesh
            if (meshFilter != null && meshFilter.sharedMesh.isReadable == false)
            {
                Debug.Log ("Mesh is not readable. Open Import Settings and turn On Read/Write Enabled", meshFilter.gameObject); 
                return false;
            }
            
            // Get components
            transForm        = GetComponent<Transform>();
            origSubMeshIdsRF = new List<RFDictionary>();
            
            // Mesh renderer
            if (skinnedMeshRend == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                    meshRenderer = gameObject.AddComponent<MeshRenderer>();
                bound = meshRenderer.bounds;
            }
            
            // Skinned mesh
            if (skinnedMeshRend != null)
                bound = skinnedMeshRend.bounds;
            
            return true;
        }

        // Get bounds
        public Bounds GetBound()
        {
            // Mesh renderer
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    return meshRenderer.bounds;
            }
            else
                return meshRenderer.bounds;
            
            // Skinned mesh
            if (skinnedMeshRend == null)
            {
                skinnedMeshRend = GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRend != null)
                    return skinnedMeshRend.bounds;
            }

            return new Bounds();
        }
        
        // Get prefab mode
        static PrefabMode GetPrefabMode (GameObject go)
        {
            // scene, prefab, mode
            // Debug.Log (go.scene.path); // fullpath.unity,  null, ""
            // Debug.Log (go.scene.name); // scene name, null, box_pf
            // Debug.Log (go.scene.rootCount); // 4, 0, 1
            // Debug.Log (go.scene.isLoaded); // true, false, true
            // Debug.Log (go.scene.IsValid()); // true, false, true
            // return PrefabMode.Asset;
            
            // Prefab is asset
            if (go.scene.path.EndsWith(".prefab"))
                return PrefabMode.Asset;
            
            // Prefab is in editing mode
            if (string.IsNullOrEmpty(go.scene.path))
                return PrefabMode.PrefabEditingMode;
            
            // Prefab is in scene
            return PrefabMode.Scene;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////
        
        // Fragment this object by shatter properties
        public void Fragment(int fragmentMode = 0)
        {
            // Cache variables
            if (DefineComponents() == false)
                return;
            
            // Cache default vars
            SetVariables();
            
            // Check if object is too small
            ScaleCheck();
            
            // Cache
            RFFragment.CacheMeshes(ref meshes, ref pivots, ref origSubMeshIdsRF, this);

            // Stop
            if (meshes == null)
                return;
            
            // Create fragments
            if (fragmentMode == 1)
            {
                if (rootChildList[rootChildList.Count - 1] != null)
                    fragmentsLast = CreateFragments(rootChildList[rootChildList.Count - 1].gameObject);
                else
                    fragmentMode = 0;
            }
            if (fragmentMode == 0)
            {
                fragmentsLast = CreateFragments();
            }

            // Vertex limitation
            VertexLimitation();
            
            // Collect to all fragments
            fragmentsAll.AddRange(fragmentsLast);
            
            // Reset original object back if it was scaled
            transForm.localScale = originalScale;
        }

        // Create fragments by mesh and pivots array
        List<GameObject> CreateFragments(GameObject lastRoot = null)
        {
            // No mesh were cached
            if (meshes == null)
                return null;

            // Clear array for new fragments
            GameObject[] fragArray = new GameObject[meshes.Length];

            // Vars 
            string goName = gameObject.name;
            string baseName = goName + "_sh_";
            
            // Create root object
            GameObject root = lastRoot;
            if (lastRoot == null)
            {
                root = new GameObject (goName + "_rootss");
                root.transform.position = transForm.position;
                root.transform.rotation = transForm.rotation;
                rootChildList.Add (root.transform);
            }
            
            // when operating on project assets, causes the new root object to be in the scene rather than a child of the prefab
            // Use https://docs.unity3d.com/ScriptReference/PrefabUtility.LoadPrefabContents.html in order to be able to set the parent
            // PrefabMode prefabMode = GetPrefabMode(gameObject);
            // if ( prefabMode != PrefabMode.Scene)
            // {
            // 	// PREFAB, AVOID CREATING INTO SCENE
            // 	root.transform.parent = transForm;
            // }
            // else
            // {
            // 	// ORIGINAL BEHAVIOR
            // 	root.transform.parent = transForm.parent;
            // }

            // Create instance for fragments
            GameObject fragInstance;
            if (advanced.copyComponents == true)
            {
                fragInstance = Instantiate(gameObject);
                fragInstance.transform.rotation = Quaternion.identity;
                fragInstance.transform.localScale = Vector3.one;

                // Destroy shatter
                DestroyImmediate(fragInstance.GetComponent<RayfireShatter>());
            }
            else
            {
                fragInstance = new GameObject();
                fragInstance.AddComponent<MeshFilter>();
                fragInstance.AddComponent<MeshRenderer>();
            }
            
            // Get original mats
            Material[] mats = skinnedMeshRend != null 
                ? skinnedMeshRend.sharedMaterials 
                : meshRenderer.sharedMaterials;
            
            // Create fragment objects
            for (int i = 0; i < meshes.Length; ++i)
            {
                // Rescale mesh
                if (rescaleFix != 1f)
                    RFFragment.RescaleMesh (meshes[i], rescaleFix);

                // Instantiate. IMPORTANT do not parent when Instantiate
                GameObject fragGo = Instantiate(fragInstance);
                fragGo.transform.localScale = Vector3.one;
                
                // Set multymaterial
                MeshRenderer targetRend = fragGo.GetComponent<MeshRenderer>();
                RFSurface.SetMaterial(origSubMeshIdsRF, mats, material, targetRend, i, meshes.Length);
                
                // Set fragment object name and tm
                fragGo.name               = baseName + (i + 1);
                fragGo.transform.position = root.transform.position + (pivots[i] / rescaleFix);
                fragGo.transform.parent   = root.transform;
                
                // Set fragment mesh
                MeshFilter mf = fragGo.GetComponent<MeshFilter>();
                
                
                /*
                #if UNITY_EDITOR
                // Up to the caller to use AssetDatabase.RemoveObjectFromAsset to remove meshes from any prior calls to CreateFragments()
                if (prefabMode == PrefabMode.Asset)
                {
                	AssetDatabase.AddObjectToAsset(meshes[i], gameObject.scene.path);
                }
                else if (prefabMode == PrefabMode.PrefabEditingMode)
                {
                	//string assetPath = UnityEditor.Experimental.GetPrefabStage(gameObject).prefabAssetPath;
                	//AssetDatabase.AddObjectToAsset(meshes[i], assetPath);
                }
                #endif
                */
                
                
                mf.sharedMesh = meshes[i];
                mf.sharedMesh.name = fragGo.name;

                // Set mesh collider
                MeshCollider mc = fragGo.GetComponent<MeshCollider>();
                if (mc != null)
                    mc.sharedMesh = meshes[i];

                // Add in array
                fragArray[i] = fragGo;
            }

            // Root back to original parent
            root.transform.parent = transForm.parent;
            root.transform.localScale = Vector3.one;

            // Destroy instance
            DestroyImmediate(fragInstance);

            // Empty lists
            meshes = null;
            pivots = null;
            origSubMeshIdsRF = new List<RFDictionary>();

            return fragArray.ToList();
        }
        
        /// /////////////////////////////////////////////////////////
        /// Deleting
        /// /////////////////////////////////////////////////////////

        // Delete fragments from last Fragment method
        public void DeleteFragmentsLast(int destroyMode = 0)
        {
            // Destroy last fragments
            if (destroyMode == 1)
                for (int i = fragmentsLast.Count - 1; i >= 0; i--)
                    if (fragmentsLast[i] != null)
                        DestroyImmediate (fragmentsLast[i]);

            // Clean fragments list pre
            fragmentsLast.Clear();
            for (int i = fragmentsAll.Count - 1; i >= 0; i--)
                if (fragmentsAll[i] == null)
                    fragmentsAll.RemoveAt (i);
            
            // Check for all roots
            for (int i = rootChildList.Count - 1; i >= 0; i--)
                if (rootChildList[i] == null)
                    rootChildList.RemoveAt (i);
            
            // No roots
            if (rootChildList.Count == 0)
                return;

            // Destroy with root
            if (destroyMode == 0)
            {
                // Destroy root with fragments
                DestroyImmediate (rootChildList[rootChildList.Count - 1].gameObject);

                // Remove from list
                rootChildList.RemoveAt (rootChildList.Count - 1);
            }

            // Clean all fragments list post
            for (int i = fragmentsAll.Count - 1; i >= 0; i--)
                if (fragmentsAll[i] == null)
                    fragmentsAll.RemoveAt (i);
        }

        // Delete all fragments and roots
        public void DeleteFragmentsAll()
        {
            // Clear lists
            fragmentsLast.Clear();
            fragmentsAll.Clear();
            
            // Check for all roots
            for (int i = rootChildList.Count - 1; i >= 0; i--)
                if (rootChildList[i] != null)
                    DestroyImmediate(rootChildList[i].gameObject);
            rootChildList.Clear();
        }

        // Reset center helper
        public void ResetCenter()
        {
            centerPosition = Vector3.zero;
            centerDirection = Quaternion.identity;

            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
                centerPosition = transform.InverseTransformPoint (rend.bounds.center);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Scale
        /// /////////////////////////////////////////////////////////
        
        // Check if object is too small
        void ScaleCheck()
        {
            // Ge size from renderers
            if (meshRenderer != null)
                size = meshRenderer.bounds.size.magnitude;
            if (skinnedMeshRend != null)
                size = skinnedMeshRend.bounds.size.magnitude;
            
            // Get rescaleFix if too small
            if (size != 0f && size < minSize)
            {
                // Get rescaleFix factor
                rescaleFix = 1f / size;
                
                // Scale small object up to shatter
                Vector3 newScale = transForm.localScale * rescaleFix;
                transForm.localScale = newScale;
                
                // Warning
                Debug.Log ("Warning. Object " + name + " is too small.");
            }
        }
        
        // Reset original object and fragments scale
        public void ResetScale (float scaleValue)
        {
            // Reset scale
            if (resetState == true && scaleValue == 0f)
            {
                if (skinnedMeshRend != null)
                    skinnedMeshRend.enabled = true;

                if (meshRenderer != null)
                    meshRenderer.enabled = true;

                if (fragmentsLast.Count > 0)
                    foreach (GameObject fragment in fragmentsLast)
                        if (fragment != null)
                            fragment.transform.localScale = Vector3.one;

                resetState = false;
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Copy
        /// /////////////////////////////////////////////////////////
        
        // Copy shatter component
        public static void CopyRootMeshShatter (RayfireRigid source, List<RayfireRigid> targets)
        {
            // No shatter
            if (source.meshDemolition.scrShatter == null)
                return;

            // Copy shatter
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].meshDemolition.scrShatter = targets[i].gameObject.AddComponent<RayfireShatter>();
                targets[i].meshDemolition.scrShatter.CopyFrom (source.meshDemolition.scrShatter);
            }
        }
        
        // Copy from
        void CopyFrom (RayfireShatter shatter)
        {
            type      = shatter.type;

            voronoi   = new RFVoronoi(shatter.voronoi);
            splinters = new RFSplinters(shatter.splinters);
            slabs     = new RFSplinters(shatter.slabs);
            radial    = new RFRadial(shatter.radial); 
            custom    = new RFCustom(shatter.custom);
            slice     = new RFSlice(shatter.slice);
            tets      = new RFTets(shatter.tets);

            mode     = shatter.mode;
            material.CopyFrom (shatter.material);
            clusters = new RFShatterCluster(shatter.clusters);
            advanced = new RFShatterAdvanced(shatter.advanced);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Other
        /// /////////////////////////////////////////////////////////
        
        // Vertex limitation
        void VertexLimitation()
        {
            // Vertex limitation
            if (advanced.vertexLimitation == true)
            {
                for (int i = fragmentsLast.Count - 1; i >= 0; i--)
                {
                    MeshFilter mf = fragmentsLast[i].GetComponent<MeshFilter>();
                    if (mf.sharedMesh.vertexCount > advanced.vertexAmount)
                    {
                        RayfireShatter shat = fragmentsLast[i].AddComponent<RayfireShatter>();
                        shat.voronoi.amount = 4;
                        
                        shat.Fragment ();
                        Debug.Log (shat.name);

                        if (shat.fragmentsLast.Count > 0)
                        {
                            fragmentsLast.AddRange (shat.fragmentsLast);
                            DestroyImmediate (shat.gameObject);
                            fragmentsLast.RemoveAt (i);
                        }
                    }
                }
            }
        }
    }
}