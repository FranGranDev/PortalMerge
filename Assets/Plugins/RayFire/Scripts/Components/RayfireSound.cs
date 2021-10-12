using UnityEngine;

namespace RayFire
{
    // TODO collision, debris, impact
    // TODO delay
    // TODO Same clip play check. name hash
    // copy to fragments
    // Create sound object with advanced properties

    [SelectionBase]
    [AddComponentMenu ("RayFire/Rayfire Sound")]
    [HelpURL ("http://rayfirestudios.com/unity-online-help/unity-sound-component/")]
    public class RayfireSound : MonoBehaviour
    {
        [Header ("  Properties")]
        [Space (3)]
        
        [Tooltip ("Base volume. Can be increased by Size Volume property")]
        [Range(0.01f, 1f)] public float baseVolume;
        
        [Space (1)]
        [Tooltip ("Additional volume per one unit size")]
        [Range(0f, 1f)] public float sizeVolume;
        
        // [Space (2)] public bool copyToFragments;
        
        [Header ("  Events")]
        [Space (3)]
        
        public RFSound initialization;
        [Space (2)]
        public RFSound activation;
        [Space (2)]
        public RFSound demolition;

        [Header ("  Filters")]
        [Space (3)]
        
        // Filters
        [Tooltip ("Objects with size lower than defined value will not make sound")]
        [Range(0f, 1f)]  public float minimumSize;
        
        [Space (1)]
        [Tooltip ("Objects with distance to main camera higher than defined value will not make sound")]
        [Range(0, 999)] public float cameraDistance;

        // Hidden
        [HideInInspector] public RayfireRigid rigid;
        
        /// /////////////////////////////////////////////////////////
        /// Common
        /// /////////////////////////////////////////////////////////

        // Constructor
        public RayfireSound()
        {
            baseVolume = 1f;
            sizeVolume = 0.2f;
                
            minimumSize    = 0f;
            cameraDistance = 0;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Methods
        /// ///////////////////////////////////////////////////////// 

        // Initialize
        public void Initialize()
        {
            if (rigid == null)
                Debug.Log ("RayFire Sound: " + name + " Warning. Sound component has no attached Rigid", gameObject);
            
            // All disabled
            if (initialization.enable == false &&
                activation.enable == false &&
                demolition.enable == false)
                Debug.Log ("RayFire Sound: " + name + " Warning. All events disabled", gameObject);
            
            // No clips
            if (initialization.enable == true)
                if (initialization.clip == null && initialization.HasClips == false)
                    Debug.Log ("RayFire Sound: " + name + " Warning. Initialization sound has no clips to play", gameObject);
            if (activation.enable == true)
                if (activation.clip == null && activation.HasClips == false)
                    Debug.Log ("RayFire Sound: " + name + " Warning. Activation sound has no clips to play", gameObject);
            if (demolition.enable == true)
                if (demolition.clip == null && demolition.HasClips == false)
                    Debug.Log ("RayFire Sound: " + name + " Warning. Demolition sound has no clips to play", gameObject);
        }

        // Copy from
        public void CopyFrom (RayfireSound source)
        {
            baseVolume     = source.baseVolume;
            sizeVolume     = source.sizeVolume;
            initialization = new RFSound(source.initialization);
            activation     = new RFSound(source.activation);
            demolition     = new RFSound(source.demolition);
            minimumSize    = source.minimumSize;
            cameraDistance = source.cameraDistance;
        }
        
        // Create audio source and play clip
        void CreateSource(RayfireRigid scr)
        {
            GameObject soundGo = new GameObject("SoundSource");
            soundGo.transform.position = scr.gameObject.transform.position;
            AudioSource audioSource = soundGo.AddComponent<AudioSource>();
            audioSource.clip                  = demolition.clip;
            audioSource.mute                  = false;
            audioSource.bypassEffects         = false;
            audioSource.bypassListenerEffects = false;
            audioSource.bypassReverbZones     = false;
            audioSource.playOnAwake           = false;
            audioSource.loop                  = false;
            audioSource.priority              = 127;
            audioSource.volume                = demolition.multiplier;
            audioSource.pitch                 = 1f;
            audioSource.panStereo             = 0f;
            audioSource.spatialBlend          = 0f;
            audioSource.reverbZoneMix         = 1f;
            audioSource.minDistance           = 0f;
            //audioSource.maxDistance           = demolitionSound.maxDistance;
            audioSource.PlayOneShot (demolition.clip, demolition.multiplier);
            Destroy (soundGo, demolition.clip.length);
        }
    }
}
