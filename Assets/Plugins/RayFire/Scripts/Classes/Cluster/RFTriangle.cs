using System.Collections.Generic;
using UnityEngine;
using System;

namespace RayFire
{
    [Serializable]
    public class RFTriangle
    {
        public int       id;
        public float     area;
        public Vector3   normal;
        public Vector3   pos;
        public List<int> neibs;

        // Constructor
        public RFTriangle (int Id, float Area, Vector3 Normal, Vector3 Pos)
        {
            id     = Id;
            area   = Area;
            normal = Normal;
            pos    = Pos;
            neibs  = new List<int>();
        }

        // Set mesh triangles
        public static void SetTriangles (RFShard shard, MeshFilter mf)
        {
            // Check if triangles already calculated
            if (shard.tris != null)
            {
                //Debug.Log (" no calc tris");
                return;
            }
            
            //Debug.Log ("calc tris");
            
            // Cached Vars
            int[] triangles = mf.sharedMesh.triangles;
            Vector3[] vertices = mf.sharedMesh.vertices;
            
            // Collect tris
            int i1, i2, i3;
            Vector3 v1, v2, v3, cross, pos;
            shard.tris = new List<RFTriangle>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                // Vertex indexes
                i1 = triangles[i];
                i2 = triangles[i + 1];
                i3 = triangles[i + 2];

                // Get vertices position and area
                v1    = shard.tm.TransformPoint (vertices[i1]);
                v2    = shard.tm.TransformPoint (vertices[i2]);
                v3    = shard.tm.TransformPoint (vertices[i3]);
                cross = Vector3.Cross (v1 - v2, v1 - v3);

                // Set position
                pos = (v1 + v2 + v3) / 3f;

                // Create triangle and collect it
                shard.tris.Add (new RFTriangle (i / 3, (cross.magnitude * 0.5f), mf.sharedMesh.normals[i1], pos));
            }
        }

        // Clear
        public static void Clear(RFShard shard)
        {
            if (shard.tris != null)
                shard.tris.Clear();
            shard.tris = null;
        }
    }
}