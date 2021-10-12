using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RayFire
{
    
    //[Serializable]
    public class RFFrag
    {
        public Mesh mesh;
        public Vector3 pivot;
        //public RFMesh rfMesh;
        public RFDictionary subId;
        public RayfireRigid fragment;
    }
    
    public class RFTri
    {
        public int meshId;
        public int subMeshId = -1;
        public List<int> ids = new List<int>();
        public List<Vector3> vpos = new List<Vector3>();
        public List<Vector3> vnormal = new List<Vector3>();
        public List<Vector2> uvs = new List<Vector2>();
        public List<Vector4> tangents = new List<Vector4>();
        public List<RFTri> neibTris = new List<RFTri>();
    }

    [Serializable]
    public class RFDictionary
    {
        public List<int> keys;
        public List<int> values;

        // Constructor
        public RFDictionary(Dictionary<int, int> dictionary)
        {
            keys = new List<int>();
            values = new List<int>();
            keys = dictionary.Keys.ToList();
            values =  dictionary.Values.ToList();
        }
    }

    /// /////////////////////////////////////////////////////////
    /// Rigid
    /// /////////////////////////////////////////////////////////
    
    // Gluing
    [Serializable]
    public class RFShatterCluster
    {
        [Header ("  Main")]
        [Space (2)]
        
        public bool enable;
        
        [Tooltip ("Amount of clusters defined by random point cloud.")]
        [Range(2, 200)] 
        public int count;
        
        [Tooltip ("Random seed for clusters point cloud generator.")]
        [Range(0, 100)] 
        public int seed;
        
        [Tooltip ("Smooth strength for cluster inner surface.")]
        [Range(0f, 1f)] 
        public float relax;
        
        [Header ("  Debris")]
        [Space (2)]
        
        [Tooltip ("Amount of debris in last layer in percents relative to amount of fragments in cluster.")]
        [Range(0, 100)] 
        public int amount;
        
        [Tooltip ("Amount of debris layers at cluster border.")]
        [Range(0, 5)] 
        public int layers;

        [Tooltip ("Scale variation for inner debris.")]
        [Range(0.1f, 1f)] 
        public float scale;
        
        [Tooltip ("Minimum amount of fragments in debris cluster.")]
        [Range(1, 20)] 
        public int min;
        
        [Tooltip ("Maximum amount of fragments in debris cluster.")]
        [Range(1, 20)] 
        public int max;
        
        // Constructor
        public RFShatterCluster()
        {
            enable = false;
            count = 10;
            seed = 1;
            relax = 0.5f;
            
            layers = 0;
            amount = 0;
            scale = 1f;
            min = 1;
            max = 3;
        }
        
        // Constructor
        public RFShatterCluster (RFShatterCluster src)
        {
            enable = src.enable;
            count  = src.count;
            seed   = src.seed;
            relax  = src.relax;
            
            layers = src.layers;
            amount = src.amount;
            scale  = src.scale;
            min    = src.min;
            max    = src.max;
        }
    }

    /// /////////////////////////////////////////////////////////
    /// Shatter
    /// /////////////////////////////////////////////////////////

    [Serializable]
    public class RFVoronoi
    {
        public int amount;
        [Range(0f, 1f)] public float centerBias;

        // Constructor
        public RFVoronoi()
        {
            amount = 30;
            centerBias = 0f;
        }
        
        // Constructor
        public RFVoronoi(RFVoronoi src)
        {
            amount     = src.amount;
            centerBias = src.centerBias;
        }
        
        // Amount
        public int Amount
        {
            get
            {
                if (amount < 1)
                    return 1;
                if (amount > 20000)
                    return 2;
                return amount;
            }
        }
    }

    [Serializable]
    public class RFSplinters
    {
        public AxisType axis;
        public int amount;
        [Range(0f, 1f)] public float strength;
        [Range(0f, 1f)] public float centerBias;
        
        // Constructor
        public RFSplinters()
        {
            axis = AxisType.YGreen; 
            amount     = 30;
            strength   = 0.7f;
            centerBias = 0f;
        }
        
        // Constructor
        public RFSplinters(RFSplinters src)
        {
            axis       = src.axis; 
            amount     = src.amount;
            strength   = src.strength;
            centerBias = src.centerBias;
        }
        
        // Amount
        public int Amount
        {
            get
            {
                if (amount < 2)
                    return 2;
                if (amount > 20000)
                    return 2;
                return amount;
            }
        }
    }

    [Serializable]
    public class RFRadial
    {
        [Header("  Common")]
        [Space (2)]
        
        public AxisType centerAxis;
        [Range(0.01f, 30f)] public float radius;
        [Range(0f, 1f)] public float divergence;
        public bool restrictToPlane;

        [Header("  Rings")]
        [Space (2)]
        
        [Range(3, 60)]  public int rings;
        [Range(0, 100)] public int focus;
        [Range(0, 100)] public int focusStr;
        [Range(0, 100)] public int randomRings;

        [Header("  Rays")]
        [Space (2)]
        
        [Range(3, 60)]   public int rays;
        [Range(0, 100)]  public int randomRays;
        [Range(-90, 90)] public int twist;
        
        // Constructor
        public RFRadial()
        {
            centerAxis  = AxisType.YGreen;
            radius          = 1f;
            divergence      = 1f;
            restrictToPlane = true;
            rings           = 10;
            focus           = 0;
            focusStr        = 50;
            randomRings     = 50;
            rays            = 10;
            randomRays      = 0;
            twist           = 0;
        }
        
        // Constructor
        public RFRadial(RFRadial src)
        {
            centerAxis      = src.centerAxis;
            radius          = src.radius;
            divergence      = src.divergence;
            restrictToPlane = src.restrictToPlane;
            rings           = src.rings;
            focus           = src.focus;
            focusStr        = src.focusStr;
            randomRings     = src.randomRings;
            rays            = src.rays;
            randomRays      = src.randomRays;
            twist           = src.twist;
        }
    }

    [Serializable]
    public class RFSlice
    {
        public PlaneType       plane;
        public List<Transform> sliceList;

        // Constructor
        public RFSlice()
        {
            plane = PlaneType.XZ;
        }
        
        // Constructor
        public RFSlice(RFSlice src)
        {
            plane     = src.plane;
            sliceList = src.sliceList;
        }
        
        // Get axis
        public Vector3 Axis (Transform tm)
        {
            if (plane == PlaneType.YZ)
                return tm.right;
            if (plane == PlaneType.XZ)
                return tm.up;
            return tm.forward;
        }
    }

    [Serializable]
    public class RFTets
    {
        public enum TetType
        {
            Uniform = 0,
            Curved  = 1
        }
        
        [HideInInspector] public TetType lattice;
        [Range( 1, 30)]   public int     density;
        [Range (0, 100)]  public int     noise;
        
        // Constructor
        public RFTets()
        {
            lattice = TetType.Uniform;
            density = 7;
            noise   = 100;
        }
        
        // Constructor
        public RFTets(RFTets src)
        {
            lattice = src.lattice;
            density = src.density;
            noise   = src.noise;
        }
    }
}

