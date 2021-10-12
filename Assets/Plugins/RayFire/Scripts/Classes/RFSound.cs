using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

namespace RayFire
{
    [Serializable]
    public class RFSound
    {
        [Space (2)]
        public bool enable;
        
        [Space (1)]
        [Tooltip ("Volume multiplier")]
        [Range(0.01f, 1f)] public float multiplier;
        
        [Space (2)]
        public AudioClip       clip;
        
        [Space (1)]
        [Tooltip ("Random List")]
        public List<AudioClip> clips;
        
        /// /////////////////////////////////////////////////////////
        /// Constructor
        /// /////////////////////////////////////////////////////////
        
        // Constructor
        public RFSound()
        {
            enable     = true;
            multiplier = 1f;
        }
        
        // Copy from
        public RFSound (RFSound source)
        {
            enable = source.enable;
            multiplier = source.multiplier;
            clip = source.clip;
            
            if (source.HasClips == true)
            {
                clips = new List<AudioClip>();
                for (int i = 0; i < source.clips.Count; i++)
                    clips.Add (source.clips[i]);
            }
        }
        
        // Copy debris and dust
        public static void CopyRootMeshSound (RayfireRigid source, List<RayfireRigid> targets)
        {
            // No sound
            if (source.sound == null)
                return;
            
            // TODO CHECK
            
            // Copy sound
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].sound = targets[i].gameObject.AddComponent<RayfireSound>();
                targets[i].sound.CopyFrom (source.sound);
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Play on events
        /// /////////////////////////////////////////////////////////

        // Initialization sound
        public static void InitializationSound (RayfireSound scr, float size)
        {
            // Null
            if (scr == null)
                return;

            // Turned off
            if (scr.initialization.enable == false)
                return;

            // No Rigid
            if (scr.rigid == null)
            {
                Debug.Log ("RayFire Sound: " + scr.name + " Initialization sound warning. Rigid component required", scr.gameObject);
                return;
            }

            // Get size if not defined
            if (size <= 0)
                size = scr.rigid.limitations.bboxSize;
            
            // Filtering
            if (FilterCheck(scr, size) == false)
                return;
            
            // Get play clip
            if (scr.initialization.HasClips == true)
                scr.initialization.clip = scr.initialization.clips[Random.Range (0, scr.activation.clips.Count - 1)];
            
            // Has no clip
            if (scr.initialization.clip == null)
                return;

            // Get volume
            float volume = GeVolume (scr, size);
            
            // Play
            AudioSource.PlayClipAtPoint (scr.initialization.clip, scr.gameObject.transform.position, volume);
        }
        
        // Activation sound
        public static void ActivationSound (RayfireSound scr, float size)
        {
            // Null
            if (scr == null)
                return;

            // Turned off
            if (scr.activation.enable == false)
                return;
            
            // No Rigid
            if (scr.rigid == null)
            {
                Debug.Log ("RayFire Sound: " + scr.name + " Activation sound warning. Rigid component required", scr.gameObject);
                return;
            }

            // Get size if not defined
            if (size <= 0)
                size = scr.rigid.limitations.bboxSize;
            
            // Filtering
            if (FilterCheck(scr, size) == false)
                return;
            
            // Get play clip
            if (scr.activation.HasClips == true)
                scr.activation.clip = scr.activation.clips[Random.Range (0, scr.activation.clips.Count - 1)];
            
            // Has no clip
            if (scr.activation.clip == null)
                return;

            // Get volume
            float volume = GeVolume (scr, size);
            
            // Play
            AudioSource.PlayClipAtPoint (scr.activation.clip, scr.gameObject.transform.position, volume);
        }

        // Demolition sound
        public static void DemolitionSound (RayfireSound scr, float size)
        {
            // Null
            if (scr == null)
                return;
            
            // Turned off
            if (scr.demolition.enable == false)
                return;

            // No Rigid
            if (scr.rigid == null)
            {
                Debug.Log ("RayFire Sound: " + scr.name + " Demolition sound warning. Rigid component required", scr.gameObject);
                return;
            }
            
            // Get size if not defined
            if (size <= 0)
                size = scr.rigid.limitations.bboxSize;

            // Filtering
            if (FilterCheck(scr, size) == false)
                return;
           
            // Get play clip
            if (scr.demolition.HasClips == true)
                scr.demolition.clip = scr.demolition.clips[Random.Range (0, scr.demolition.clips.Count - 1)];

            // Has no clip
            if (scr.demolition.clip == null)
                return;

            // Get volume
            float volume = GeVolume (scr, size);
            
            // Play
            AudioSource.PlayClipAtPoint (scr.demolition.clip, scr.gameObject.transform.position, volume);
        }
        
        /// /////////////////////////////////////////////////////////
        /// Static
        /// /////////////////////////////////////////////////////////
        
        // Get volume
        public static float GeVolume (RayfireSound scr, float size)
        {
            // Get size if not defined
            if (size <= 0)
                if (scr.rigid != null)
                    size = scr.rigid.limitations.bboxSize;
            
            // Get volume
            float volume = scr.baseVolume;
            if (scr.sizeVolume > 0)
                volume += size * scr.sizeVolume;
            volume *= scr.activation.multiplier;
            return volume;
        }
        
        // Filters check
        static bool FilterCheck (RayfireSound scr, float size)
        {
            // Small size
            if (scr.minimumSize > 0)
                if (size < scr.minimumSize)
                    return false;

            // Far from camera
            if (scr.cameraDistance > 0)
                if (Camera.main != null)
                    if (Vector3.Distance (Camera.main.transform.position, scr.transform.position) > scr.cameraDistance)
                        return false;
            return true;
        }
        
        // Has clips
        public bool HasClips { get { return clips != null && clips.Count > 0; } }
    }
    
    // Demolition sound class
    [Serializable]
    public class RFSoundActivation
    {
        [Space (2)]
        public bool enable;
        [Space (1)]
        [Range(0.01f, 2f)] public float multiplier;

        [Space (2)]
        public AudioClip clip;
        [Space (1)]
        public List<AudioClip> clips;
    }
}

