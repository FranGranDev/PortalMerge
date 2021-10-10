using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Face remove by angle

namespace RayFire
{
    [AddComponentMenu("RayFire/Rayfire Combine")]
    [HelpURL("http://rayfirestudios.com/unity-online-help/unity-combine-component/")]
    public class RayfireCombine : MonoBehaviour
    {
        public enum CombType
        {
            Children = 0,
            ObjectsList = 1,
        }

        [Header ("  Object source")]
        [Space (3)]
        
        public CombType type;
        [Space (1)]
        public List<GameObject> objects;
        
        [Header ("  Mesh source")]
        [Space (3)]
                
        public bool meshFilters = true;
        [Space (1)]
        public bool skinnedMeshes = true;
        [Space (1)]
        public bool particleSystems = true;      
        
        [Space (3)]
        
        [Header ("  Filters")]
        [Space (3)]
        
        [Range(0, 10)]public float sizeThreshold = 0.1f;
        [Space (1)]
        [Range(0, 100)]public int vertexThreshold = 5;

        // Self data
        private Transform transForm;
        private MeshFilter mFilter;
        private MeshRenderer meshRenderer;

        // Children data
        private List<bool> invertNormals;
        private List<Transform> transForms;
        private List<MeshFilter> mFilters;
        private List<SkinnedMeshRenderer> skinnedMeshRends;
        private List<ParticleSystemRenderer> particleRends;
        private List<Mesh> meshList;
        private List<List<int>> matIdList;
        private List<List<Material>> matList;

        // Combined mesh data
        private List<Material> allMaterials;
        private List<int> combTrianglesSubId;
        private List<List<int>> combTriangles;
        private List<Vector3> combVertices;
        private List<Vector3> combNormals;
        private List<Vector2> combUvs;
        private List<Vector4> combTangents;

        // /////////////////////////////////////////////////////////
        // Combine
        // /////////////////////////////////////////////////////////
        
        // Combine meshes
        public void Combine()
        {
            // Set combine data
            SetData();

            // Set combined mesh data
            SetCombinedMesh();

            // Create mesh
            CreateMesh();
        }

        // Set data
        void SetData()
        {
            transForm = GetComponent<Transform>();
            
            // Reset mesh
            mFilter = GetComponent<MeshFilter>();
            if (mFilter == null)
                mFilter = gameObject.AddComponent<MeshFilter>();
            mFilter.sharedMesh = null;
            
            // Reset mesh renderer
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = new Material[]{};

            // Get targets
            if (type == CombType.Children)
            {
                if (meshFilters == true)
                    mFilters = GetComponentsInChildren<MeshFilter>().ToList();
                if (skinnedMeshes == true)
                    skinnedMeshRends = GetComponentsInChildren<SkinnedMeshRenderer>().ToList();
                if (particleSystems == true)
                    particleRends = GetComponentsInChildren<ParticleSystemRenderer>().ToList();
            }
            if (type == CombType.ObjectsList)
            {
                mFilters = new List<MeshFilter>();
                if (meshFilters == true)
                {
                    foreach (var obj in objects)
                    {
                        MeshFilter mf = obj.GetComponent<MeshFilter>();
                        if (mf != null)
                            if (mf.sharedMesh != null)
                                mFilters.Add (mf);
                    }
                }

                skinnedMeshRends = new List<SkinnedMeshRenderer>();
                if (skinnedMeshes == true)
                {
                    foreach (var obj in objects)
                    {
                        SkinnedMeshRenderer sk = obj.GetComponent<SkinnedMeshRenderer>();
                        if (sk != null)
                            if (sk.sharedMesh != null)
                                skinnedMeshRends.Add (sk);
                    }
                }

                particleRends = new List<ParticleSystemRenderer>();
                if (particleSystems == true)
                {
                    foreach (var obj in objects)
                    {
                        ParticleSystemRenderer pr = obj.GetComponent<ParticleSystemRenderer>();
                        if (pr != null)
                            particleRends.Add (pr);
                    }
                }
            }
            
            // Clear mesh filters without mesh
            for (int i = mFilters.Count - 1; i >= 0; i--)
                if (mFilters[i].sharedMesh == null)
                    mFilters.RemoveAt(i);
            
            // Clear skinned meshes without mesh
            for (int i = skinnedMeshRends.Count - 1; i >= 0; i--)
                if (skinnedMeshRends[i].sharedMesh == null)
                    skinnedMeshRends.RemoveAt(i);
            
            // Get meshes and tms
            meshList = new List<Mesh>();
            transForms = new List<Transform>();
            matList = new List<List<Material>>();

            // Collect mesh, tm and mats for meshfilter
            foreach (var mf in mFilters)
            {
                // Filters
                if (mf.sharedMesh.vertexCount < vertexThreshold)
                    continue;
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr != null && mr.bounds.size.magnitude < sizeThreshold)
                    continue;
                
                meshList.Add(mf.sharedMesh);
                transForms.Add(mf.transform);
                
                // Collect mats
                List<Material> mats = new List<Material>();
                if (mr != null)
                    mats = mr.sharedMaterials.ToList();
                matList.Add(mats);
            }

            // Collect mesh, tm and mats for skinned mesh
            foreach (var sk in skinnedMeshRends)
            {
                // SKip by vertex amount
                if (sk.sharedMesh.vertexCount < vertexThreshold)
                    continue;
                if (sk.bounds.size.magnitude < sizeThreshold)
                    continue;
                
                meshList.Add(RFMesh.BakeMesh(sk));
                transForms.Add(sk.transform);
                matList.Add(sk.sharedMaterials.ToList());
            }
            
            // Particle system
            #if UNITY_2018_2_OR_NEWER
            {
                if (particleRends.Count > 0)
                {
                    GameObject g = new GameObject();
                    foreach (var pr in particleRends)
                    {
                        Mesh m = new Mesh();
                        pr.BakeMesh (m, true);
                        if (m.vertexCount > 3)
                        {
                            meshList.Add (m);
                            transForms.Add (g.transform);
                            matList.Add (pr.sharedMaterials.ToList());
                        }
                    }

                    DestroyImmediate (g);
                }
            }
            #endif

            // No meshes
            if (meshList.Count == 0)
            {
                Debug.Log("No meshes to combine");
                return;
            }
            
            // Get mesh data
            invertNormals = new List<bool>();
            allMaterials = new List<Material>();
            matIdList = new List<List<int>>();
            for (int f = 0; f < transForms.Count; f++)
            {
                // Collect uniq material list
                foreach (var material in matList[f])
                    if (allMaterials.Contains(material) == false)
                        allMaterials.Add(material);
                
                // Collect material ids per submesh
                matIdList.Add(matList[f].Select(t => allMaterials.IndexOf(t)).ToList());
                
                // Get invert normals because of negative scale
                bool invert = false;
                if (transForms[f].localScale.x < 0) invert = !invert;
                if (transForms[f].localScale.y < 0) invert = !invert;
                if (transForms[f].localScale.z < 0) invert = !invert;
                invertNormals.Add(invert);
            }
        }

        // Set combined mesh data
        void SetCombinedMesh()
        {
            // Check all meshes and convert to tris
            int meshVertIdOffset = 0;
            
            // Create new mesh data lists
            combTrianglesSubId = new List<int>();
            combTriangles = new List<List<int>>();
            combVertices = new List<Vector3>();
            combNormals = new List<Vector3>();
            combUvs = new List<Vector2>();
            combTangents = new List<Vector4>();
            
            for (int m = 0; m < meshList.Count; m++)
            {
                // Get local mesh
                Mesh mesh = meshList[m];

                // Collect combined vertices list
                combVertices.AddRange(mesh.vertices.Select(t => transForm.InverseTransformPoint(transForms[m].TransformPoint(t))));
                
                // Collect combined normals list
                combNormals.AddRange(invertNormals[m] == true
                    ? mesh.normals.Select(o => -o).ToList()
                    : mesh.normals.ToList());

                // Collect combined uvs list
                combUvs.AddRange(mesh.uv.ToList());

                // Collect combined tangents list TODO FLIP NORMAL FOR INVERTED
                combTangents.AddRange(mesh.tangents.ToList());
                
                // Iterate every submesh
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    // Get all triangles verts ids
                    int[] tris = mesh.GetTriangles(s);

                    // Invert normals
                    if (invertNormals[m] == true)
                        tris = tris.Reverse().ToArray();

                    // Increment by mesh vertices id offset
                    for (int i = 0; i < tris.Length; i++)
                        tris[i] = tris[i] + meshVertIdOffset;
                    
                    // Collect triangles with material which already has other triangles. >> add to existing list
                    if (combTrianglesSubId.Contains(matIdList[m][s]) == true)
                    {
                        int ind = combTrianglesSubId.IndexOf(matIdList[m][s]);
                        combTriangles[ind].AddRange(tris.ToList());
                    }
                    else
                    {
                        // Collect sub mesh triangles >> Create new list
                        combTriangles.Add(tris.ToList());
                                            
                        // Check every triangle and collect tris material id
                        combTrianglesSubId.Add(matIdList[m][s]);
                    }
                }

                // Offset verts ids per mesh
                meshVertIdOffset += mesh.vertices.Length;
            }
        }
        
        // Create combined mesh
        void CreateMesh()
        {
            // Create combined mesh
            Mesh newMesh = new Mesh();
            newMesh.name = name + "_Comb";
            newMesh.SetVertices(combVertices);
            newMesh.SetNormals(combNormals);
            newMesh.SetUVs(0, combUvs);
            newMesh.SetTangents(combTangents);
            
            // Set triangles by submeshes
            newMesh.subMeshCount = combTrianglesSubId.Count;
            for (int i = 0; i < combTriangles.Count; i++)
                newMesh.SetTriangles(combTriangles[i], combTrianglesSubId[i]);
        
            // Recalculate
            newMesh.RecalculateNormals();
            newMesh.RecalculateBounds();
            newMesh.RecalculateTangents();
 
            // Set mesh to object
            mFilter.sharedMesh = newMesh;
            
            // Set mesh renderer and materials
            meshRenderer.sharedMaterials = allMaterials.ToArray();
        }

        // /////////////////////////////////////////////////////////
        // Other
        // /////////////////////////////////////////////////////////
                
/*      public void Detach()
        {
            meshFilter = GetComponent<MeshFilter>();
            transForm = GetComponent<Transform>();
            
            // Get all triangles with verts data
            List<Tri> tris = GetTris(meshFilter.sharedMesh);
            
            // Set neib tris
            for (int i = 0; i < tris.Count; i++)
                foreach (var tri in tris)
                    //if (tri.neibTris.Count < 3)
                        if (CompareTri(tris[i], tri) == true)
                        {
                            tris[i].neibTris.Add(tri);
                            //tri.neibTris.Add(tris[i]);
                        }
            
            elements = new List<Element>();
            int subMeshId = 0;

            while (tris.Count > 0)
            {
                List<Tri> subTris = new List<Tri>();
                List<Tri> checkTris = new List<Tri>();
                checkTris.Add(tris[0]);
                
                while (checkTris.Count > 0)
                {
                    
                    if (subTris.Contains(checkTris[0]) == false)
                    {
                        checkTris[0].subMeshId = subMeshId;
                        subTris.Add(checkTris[0]);
                        
                        int ind = tris.IndexOf(checkTris[0]);
                        if (ind >= 0)
                            tris.RemoveAt(ind);
                    }
                    
                    foreach (var neibTri in checkTris[0].neibTris)
                        if (subTris.Contains(neibTri) == false)
                            checkTris.Add(neibTri);
                    checkTris.RemoveAt(0);
                }
                
                Element elem = new Element();
                elem.tris.AddRange(subTris);
                elements.Add(elem);
                subMeshId++;
            }
        }

        // Match tris by shared verts
        private bool CompareTri(Tri tri1, Tri tri2)
        {
            if (tri1 == tri2)
                return false;
            foreach (int id in tri1.ids)
                if (tri2.ids.Contains(id) == true)
                    return true;
            return false;
        }
        
       //[ContextMenu("MeshData")]
        public void GetMeshData()
        {
            meshFilter = GetComponent<MeshFilter>();

            // Check for same position
            List<WeldGroup> weldGroups = GetWeldGroups(meshFilter.sharedMesh.vertices,  0.001f);
           
            // Get all triangles with verts data
            List<Tri> tris = GetTris(meshFilter.sharedMesh);
            
            // Create new tri list with modified tri. Excluded welded vertices
            List<int> remapVertIds = new List<int>();
            List<int> excludeVertIds = new List<int>();
            foreach (WeldGroup weld in weldGroups)
                for (int i = 1; i < weld.verts.Count; i++)
                {
                    remapVertIds.Add(weld.verts[0]);
                    excludeVertIds.Add(weld.verts[i]);
                }
   
            // Remap vertices for tris
            foreach (Tri tri in tris)
            {
                for (int i = 0; i < tri.ids.Count; i++)
                {
                    for (int j = 0; j < excludeVertIds.Count; j++)
                    {
                        if (tri.ids[i] == excludeVertIds[j])
                        {
                            tri.ids[i] = remapVertIds[j];
                            tri.vpos[i] = meshFilter.sharedMesh.vertices[tri.ids[i]];
                            tri.vnormal[i] = meshFilter.sharedMesh.normals[tri.ids[i]];
                        } 
                    }
                }
            }
            
            // Set new triangles array
            List<int> newTriangles = new List<int>();
            foreach (Tri tri in tris)
                newTriangles.AddRange(tri.ids);
            GameObject go = new GameObject();
            go.transform.position = transform.position + new Vector3(0, 0, 1.5f);
            go.transform.rotation = transform.rotation;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = GetComponent<MeshRenderer>().sharedMaterials;
            
            Mesh mesh = new Mesh();
            mesh.name = meshFilter.sharedMesh.name + "_welded";
            mesh.vertices = meshFilter.sharedMesh.vertices;
            mesh.triangles = newTriangles.ToArray();
            mesh.normals = meshFilter.sharedMesh.normals;
            mesh.uv = meshFilter.sharedMesh.uv;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
        }

        // Get tris
        List<Tri> GetTris(Mesh mesh)
        {
            List<Tri> tris = new List<Tri>();
            for (int i = 0; i < mesh.triangles.Length; i++)
            {
                Tri tri = new Tri();
                
                // Gt vert ids
                int id0 = mesh.triangles[i + 0];
                int id1 = mesh.triangles[i + 1];
                int id2 = mesh.triangles[i + 2];
                
                // Save vert id
                tri.ids.Add(id0); 
                tri.ids.Add(id1); 
                tri.ids.Add(id2);
                
                // Save vert position
                tri.vpos.Add(mesh.vertices[id0]);
                tri.vpos.Add(mesh.vertices[id1]);
                tri.vpos.Add(mesh.vertices[id2]);
                
                // Save normal
                tri.vnormal.Add(mesh.normals[id0]);
                tri.vnormal.Add(mesh.normals[id1]);
                tri.vnormal.Add(mesh.normals[id2]);
                
                i += 2;
                
                tris.Add(tri);
            }
            return tris;
        }
        
        // Get index of vertex which share same/close position by threshold
        List<WeldGroup> GetWeldGroups(Vector3[] vertices, float threshold)
        {
            List<int> list = new List<int>();
            List<WeldGroup> weldGroups = new List<WeldGroup>();
            for (int i = 0; i < vertices.Length; i++)
            {
                // Already checked
                if (list.Contains(i) == true)
                    continue;
                
                WeldGroup weld = new WeldGroup();
                for (int v = 0; v < vertices.Length; v++)
                {
                    // Comparing with self
                    if (i == v)
                        continue;
                  
                    // Already checked
                    if (list.Contains(v) == true)
                        continue;
                        
                    // Save if close
                    if (Vector3.Distance(vertices[i], vertices[v]) < threshold)
                    {
                        list.Add(v);

                        if (weld.verts.Contains(i) == false)
                            weld.verts.Add(i);
                        
                        if (weld.verts.Contains(v) == false)
                            weld.verts.Add(v);
                    }
                }
                
                if (weld.verts.Count > 0)
                    weldGroups.Add(weld);
            }
            
            return weldGroups;
        }*/

    }
}


