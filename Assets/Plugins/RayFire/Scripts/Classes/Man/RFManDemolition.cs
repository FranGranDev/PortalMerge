using System;
using UnityEngine;

// Namespace
namespace RayFire
{
    [Serializable]
    public class RFManDemolition
    {
        // Post dml fragments 
        public enum FragmentParentType
        {
            Manager = 0,
            Parent  = 1
        }

        [Header ("  Fragments")]
        [Space(2)]
        
        public FragmentParentType parent;
        public int maximumAmount = 1000;
        [HideInInspector] public int currentAmount = 0;
        [Range (1, 10)]
        public int badMeshTry = 3;

        [Header ("  Shadow Casting")]
        [Range (0, 1f)]
        public float sizeThreshold = 0.05f;

        // TODO Inherit velocity by impact normal
    }
}