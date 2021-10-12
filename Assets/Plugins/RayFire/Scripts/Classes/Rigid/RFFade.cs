using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RayFire
{
    [Serializable]
    public class RFFade
    {
        // Fade life Type
        public enum RFFadeLifeType
        {
            ByLifeTime = 4,
            BySimulationAndLifeTime = 8
        }

        [Header ("  Initiate")]
        [Space (2)]
        
        public bool onDemolition;
        public bool onActivation;

        [Header ("  Life")]
        [Space (2)]

        public RFFadeLifeType lifeType;
        [Range (0f, 90f)] public float lifeTime;
        [Range (0f, 20f)] public float lifeVariation;
        
        [Header("  Fade")]
        [Space(2)]
        
        public FadeType fadeType;
        [Range (1f, 20f)] public float fadeTime;
        [Range (0f, 20f)] public float sizeFilter;
        
        [NonSerialized] public int state;
        [NonSerialized] public bool stop;
        [NonSerialized] public Vector3 position;
        
        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////

        // Constructor
        public RFFade()
        {
            onDemolition = true;
            onActivation = false;
            
            lifeType      = RFFadeLifeType.ByLifeTime;
            lifeTime      = 7f;
            lifeVariation = 3f;
                        
            fadeType      = FadeType.None;
            fadeTime      = 5f;
            sizeFilter    = 0f;
            
            Reset();
        }

        // Copy from
        public void CopyFrom (RFFade fade)
        {
            onDemolition  = fade.onDemolition;
            onActivation  = fade.onActivation;
            
            lifeType      = fade.lifeType;
            lifeTime      = fade.lifeTime;
            lifeVariation = fade.lifeVariation;
            
            fadeType      = fade.fadeType;
            fadeTime      = fade.fadeTime;
            sizeFilter    = fade.sizeFilter;
            
            Reset();
        }
        
        // Reset
        public void Reset()
        {
            state = 0;
            stop  = false;
        }

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////

        // Fading init from parent node
        public void DemolitionFade (List<RayfireRigid> fadeObjects)
        {
            // No fading
            if (fadeType == FadeType.None)
                return;

            // No objects
            if (fadeObjects.Count == 0)
                return;

            // Life time fix
            if (lifeTime < 1f)
                lifeTime = 1f;

            // Add Fade script and init fading
            for (int i = 0; i < fadeObjects.Count; i++)
            {
                // Check for null
                if (fadeObjects[i] == null)
                    continue;
                
                // Size check
                if (sizeFilter > 0 && fadeObjects[i].limitations.bboxSize > sizeFilter)
                    continue;
                
                // Init fading
                Fade (fadeObjects[i]);
            }
        }

        // Fading init for fragment objects
        public static void Fade (RayfireRigid scr)
        {
            // Initialize if not
            if (scr.initialized == false)
                scr.Initialize();
            
            // No fading
            if (scr.fading.fadeType == FadeType.None)
                return;
            
            // Object inactive, SKip
            if (scr.gameObject.activeSelf == false)
                return;
                       
            // Object living, fading or faded
            if (scr.fading.state > 0)
                return;
            
            // Start life coroutine
            scr.StartCoroutine (scr.fading.LivingCor (scr));
        }
        
        /// /////////////////////////////////////////////////////////
        /// Coroutines
        /// /////////////////////////////////////////////////////////

        // Start life coroutine
        IEnumerator LivingCor (RayfireRigid scr)
        {
            // Wait for simulation get rest
            if (scr.fading.lifeType == RFFadeLifeType.BySimulationAndLifeTime)
                yield return scr.StartCoroutine(SimulationCor (scr));
            
            // Set living
            scr.fading.state = 1;
            
            // Get final life duration
            float lifeDuration = scr.fading.lifeTime;
            if (scr.fading.lifeVariation > 0)
                lifeDuration += Random.Range (0f, scr.fading.lifeVariation);
            
            // Wait life time
            if (lifeDuration > 0)
                yield return new WaitForSeconds (lifeDuration);

            // Stop fading
            if (stop == true)
            {
                scr.fading.Reset();
                yield break;
            }
            
            // Set fading
            scr.fading.state = 2;

            // TODO MAKE RESETABLE
            // scr.reset.action = RFReset.PostDemolitionType.DestroyWithDelay;
            
            // Exclude from simulation and keep object in scene
            if (scr.fading.fadeType == FadeType.SimExclude)
                FadeExclude (scr);

            // Exclude from simulation, move under ground, destroy
            else if (scr.fading.fadeType == FadeType.MoveDown)
                scr.StartCoroutine (FadeMoveDown (scr));

            // Start scale down and destroy
            else if (scr.fading.fadeType == FadeType.ScaleDown)
                scr.StartCoroutine (FadeScaleDownCor (scr));

            // Destroy object
            else if (scr.fading.fadeType == FadeType.Destroy)
                RayfireMan.DestroyFragment (scr, scr.rootParent);
        }
        
        // Exclude from simulation and keep object in scene
        static void FadeExclude (RayfireRigid scr)
        {
            // Set faded
            scr.fading.state = 2;

            // Not going to be reused
            if (scr.reset.action == RFReset.PostDemolitionType.DestroyWithDelay)
            {
                scr.DestroyRb (scr.physics.rigidBody);
                scr.DestroyCollider (scr.physics.meshCollider);
                scr.DestroyRigid (scr);
            }

            // Going to be reused 
            else if (scr.reset.action == RFReset.PostDemolitionType.DeactivateToReset)
            {
                scr.physics.rigidBody.isKinematic = true;
                scr.physics.meshCollider.enabled = false; // TODO CHECK CLUSTER COLLIDERS
                scr.StopAllCoroutines();
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Coroutines
        /// /////////////////////////////////////////////////////////
        
        // Exclude from simulation, move under ground, destroy
        static IEnumerator FadeMoveDown (RayfireRigid scr)
        {
            // Activate inactive
            if (scr.simulationType == SimType.Inactive)
                scr.Activate();

            // Wale up if sleeping
            scr.physics.rigidBody.WakeUp();
            
            // Turn off collider
            if (scr.objectType == ObjectType.Mesh)
            {
                if (scr.physics.meshCollider != null)
                    scr.physics.meshCollider.enabled = false;
            }
            else if (scr.objectType == ObjectType.ConnectedCluster || scr.objectType == ObjectType.NestedCluster)
            {
                if (scr.physics.clusterColliders != null)
                    for (int i = 0; i < scr.physics.clusterColliders.Count; i++)
                        scr.physics.clusterColliders[i].enabled = false;
            }
            
            // Wait to fall down
            yield return new WaitForSeconds (scr.fading.fadeTime);
            
            // Check if fragment is the last child in root and delete root as well
            RayfireMan.DestroyFragment (scr, scr.rootParent);
        }

        // Exclude from simulation, move under ground, destroy
        static IEnumerator FadeScaleDownCor (RayfireRigid scr)
        {
            // Scale object down during fade time
            float   waitStep   = 0.04f;
            int     steps      = (int)(scr.fading.fadeTime / waitStep);
            Vector3 vectorStep = scr.transForm.localScale / steps;
            
            // Repeat
            while (steps > 0)
            {
                steps--;
                
                // Scale down
                scr.transForm.localScale -= vectorStep;
                
                // Wait
                yield return new WaitForSeconds (waitStep);

                // Destroy when too small
                if (steps < 4)
                    RayfireMan.DestroyFragment (scr, scr.rootParent);
            }
        }
        
        // Check for simulation state TODO
        static IEnumerator SimulationCor (RayfireRigid scr)
        {
            float timeStep = Random.Range (2.5f, 3.5f);
            float distanceThreshold = 0.15f;
            bool check = true;

            while (check == true)
            {
                // Save position
                scr.fading.position = scr.transForm.position;
                
                // Wait step time
                yield return new WaitForSeconds (timeStep);
                
                float dist = Vector3.Distance (scr.fading.position, scr.transForm.position);           
                if (dist < distanceThreshold)
                {
                    check = false;
                }
            }
        }
    }
}