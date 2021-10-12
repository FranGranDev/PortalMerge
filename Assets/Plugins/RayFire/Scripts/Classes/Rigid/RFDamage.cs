using System;
using UnityEngine;

namespace RayFire
{
    [Serializable]
    public class RFDamage
    {
        [Header ("  Properties")]
        [Space (2)]
        
        [Tooltip("Allows to demolish object by it's own floating Damage value.")]
        public bool enable;
        
        [Tooltip("Defines maximum allowed damage for object before it will be demolished.")]
        public float maxDamage;
        
        [Tooltip("Shows current damage value. Can be increased by public method: \nApplyDamage(float damageValue, Vector3 damagePosition)")]
        public float currentDamage;

        [Header ("  Collisions")]
        [Space (2)]

        [Tooltip("Allows to accumulate damage value by collisions during dynamic simulation.")]
        public bool collect;
        
        [Tooltip("Defines maximum allowed damage for object before it will be demolished.")]
        [Range(0.01f, 5f)]public float multiplier; 
        
        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////

        // Constructor
        public RFDamage()
        {
            enable     = false;
            maxDamage  = 100f;
            collect    = false;
            multiplier = 1f;
                        
            Reset();
        }

        // Copy from
        public void CopyFrom(RFDamage damage)
        {
            enable     = damage.enable;
            maxDamage  = damage.maxDamage;
            collect    = damage.collect;
            multiplier = damage.multiplier;
            
            Reset();
        }
        
        // Reset
        public void Reset()
        {
            currentDamage = 0f;
        }

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////     
       
        // Add damage
        static bool Apply(RayfireRigid scr, float damageValue)
        {
            // Add damage
            scr.damage.currentDamage = scr.damage.currentDamage + damageValue;
            
            // Check
            if (scr.damage.enable == true && scr.damage.currentDamage >= scr.damage.maxDamage)
                return true;

            return false;
        }
        
        // Apply damage
        public static bool ApplyDamage (RayfireRigid scr,  float damageValue, Vector3 damagePoint, float damageRadius = 0f)
        {
            // Initialize if not
            if (scr.initialized == false)
                scr.Initialize();
            
            // Already demolished or should be
            if (scr.limitations.demolished == true || scr.limitations.demolitionShould == true)
                return false;

            // Apply damage and get demolition state
            bool demolitionState = Apply (scr, damageValue);
            
            // TODO demolish first to activate only demolished fragments AND activate if object can't be demolished
            
            // Set demolition info
            if (demolitionState == true)
            {
                // Demolition available check
                if (scr.DemolitionState() == false)
                    return false;

                // Set damage position
                scr.limitations.contactVector3 = damagePoint;
                scr.clusterDemolition.damageRadius = damageRadius;

                // Demolish object
                scr.limitations.demolitionShould = true;

                // Demolish
                scr.Demolish();

                // Was demolished
                if (scr.limitations.demolished == true)
                    return true;
            }
            
            // Check for activation
            if (scr.activation.byDamage > 0 && scr.damage.currentDamage > scr.activation.byDamage)
                scr.Activate();
            
            return false;
        }
    }
}

