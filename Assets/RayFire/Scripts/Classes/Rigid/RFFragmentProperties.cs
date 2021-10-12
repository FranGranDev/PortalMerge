using System;
using UnityEngine;

namespace RayFire
{
	[Serializable]
	public class RFFragmentProperties
	{
		[Header ("  Collider")]
		[Space (2)]

		public RFColliderType colliderType;
		
		[Tooltip ("Fragments with size less than this value will not get collider")]
		[Range (0, 10)]
		public float sizeFilter;

		[Header ("  Mesh Ops")]
		[Space (2)]
		
		[Tooltip ("Detach all not connected with each other faces into separate meshes.")]
		public bool decompose;

		[Tooltip ("Remove collier vertices to decrease amount of triangles")]
		public bool removeCollinear;

		[Header ("  Custom Layer")]
		[Space (2)]
		
		[Tooltip ("Custom layer for fragments")]
		public string layer;

        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////
		
		// Constructor
		public RFFragmentProperties()
		{
			colliderType = RFColliderType.Mesh;
			sizeFilter   = 0;
			decompose    = false;

			removeCollinear = false;
			layer           = "";
		}

		// Copy from
		public void CopyFrom (RFFragmentProperties fragmentProperties)
		{
			colliderType = fragmentProperties.colliderType;
			sizeFilter   = fragmentProperties.sizeFilter;
			decompose    = false;

			removeCollinear = fragmentProperties.removeCollinear;
			layer           = fragmentProperties.layer;
		}
	}
}