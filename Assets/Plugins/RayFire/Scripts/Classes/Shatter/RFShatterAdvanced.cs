using System;
using UnityEngine;

namespace RayFire
{
	[Serializable]
    public class RFMeshExport
    {
        // Export type
        public enum MeshExportType
        {
            LastFragments         = 0,
            Children              = 3
        }

        // Mesh source
        public MeshExportType source;
        
    	// by object, by suffix
    	public string suffix = "_frags";
    	
    	// by path, by window
    	// public string path = "RayFireFragments";
    	
    	// all, last
        // generate colliders
    }
    
	[Serializable]
	public class RFShatterAdvanced
	{
		[Header ("  Common")]
		[Space (2)]
		
		[Range (0, 100)] public int seed;
        public bool decompose;
        public bool removeCollinear;
        public bool copyComponents;
        
        [Header ("  Editor Mode")]
        [Space (2)]
        
        [Tooltip ("Create extra triangles to connect open edges and close mesh volume.")]
        public bool inputPrecap;
        
        [Space(1)]
        [Tooltip ("Keep or Delete fragment's faces created by Input Precap.")]
        public bool outputPrecap;
        
        [Space(1)]
        [Tooltip ("Delete faces which overlap with each other.")]
        public bool removeDoubleFaces;
        
        [Space(1)]
        [Tooltip ("Delete all inner fragments which has no outer surface.")]
        public bool excludeInnerFragments;
        
        [Space(1)]
        [Tooltip ("Measures in percents relative to original object size. Do not fragment elements with size less than this value.")]
        [Range (1, 100)]
        public int elementSizeThreshold;
        
        [Header ("  Limitations")]
        [Space (2)]
        
        public bool vertexLimitation;
        
        [Space(1)]
        [Range(100, 1900)] public int vertexAmount;
        
        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////
		
		// Constructor
		public RFShatterAdvanced()
		{
			seed                  = 1;
			decompose             = true;
			removeCollinear       = false;
			copyComponents        = false;

			inputPrecap           = true;
			outputPrecap          = false;
			
			removeDoubleFaces     = true;
			excludeInnerFragments = false;
			elementSizeThreshold  = 5;

			vertexLimitation = false;
			vertexAmount     = 300;
		}
        
        // Constructor
        public RFShatterAdvanced (RFShatterAdvanced src)
        {
	        seed            = src.seed;
	        decompose       = src.decompose;
	        removeCollinear = src.removeCollinear;
	        copyComponents  = src.copyComponents;

	        inputPrecap  = src.inputPrecap;
	        outputPrecap = src.outputPrecap;
			
	        removeDoubleFaces     = src.removeDoubleFaces;
	        excludeInnerFragments = src.excludeInnerFragments;
	        elementSizeThreshold  = src.elementSizeThreshold;
	        
	        vertexLimitation = src.vertexLimitation;
	        vertexAmount     = src.vertexAmount;
        }
	}
}