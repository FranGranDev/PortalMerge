using System.Collections.Generic;
using UnityEngine;
using System;

namespace RayFire
{

    [Serializable]
    public class RFFace
    {
        public int       id;
        public float     area;
        public Vector3   pos;
        public Vector3   normal;
        public List<int> tris;

        // Constructor
        public RFFace (int Id, float Area, Vector3 Normal)
        {
            id     = Id;
            area   = Area;
            normal = Normal;
            tris   = new List<int>();
        }

        // Get all face in mesh by triangles. IMPORTANT turn on triangle neib calculation in RFTriangle
        List<RFFace> GetFaces (List<RFTriangle> Triangles)
        {
            List<int>    checkedTris = new List<int>();
            List<RFFace> localFaces  = new List<RFFace>();

            // Check every triangle
            int faceId = 0;
            foreach (RFTriangle tri in Triangles)
            {
                // Skip triangle if it is already part of face
                if (checkedTris.Contains (tri.id) == false)
                {
                    // Mark tri as checked
                    checkedTris.Add (tri.id);

                    // Create face
                    RFFace face = new RFFace (faceId, tri.area, tri.normal);
                    face.pos = tri.pos;
                    faceId++;
                    face.tris.Add (tri.id);

                    // List of all triangles to check
                    List<RFTriangle> trisToCheck = new List<RFTriangle>();
                    trisToCheck.Add (tri);

                    // Check all neibs
                    while (trisToCheck.Count > 0)
                    {
                        // Check neib tris
                        foreach (int neibId in trisToCheck[0].neibs)
                        {
                            if (checkedTris.Contains (neibId) == false)
                            {
                                // Get neib tri
                                RFTriangle neibTri = Triangles[neibId];

                                // Compare normals
                                if (tri.normal == neibTri.normal)
                                {
                                    face.area += neibTri.area;
                                    face.pos  += neibTri.pos;
                                    face.tris.Add (neibId);
                                    checkedTris.Add (neibId);
                                    trisToCheck.Add (neibTri);
                                }
                            }
                        }

                        trisToCheck.RemoveAt (0);
                    }

                    // Set pos
                    face.pos /= face.tris.Count;

                    // Collect face
                    localFaces.Add (face);
                }
            }

            return localFaces;
        }
    }
}