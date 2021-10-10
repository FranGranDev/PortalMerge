using UnityEngine;

namespace RayFire
{
    [SelectionBase]
    [DisallowMultipleComponent]
    [AddComponentMenu("RayFire/Rayfire Snapshot")]
    [HelpURL("http://rayfirestudios.com/unity-online-help/unity-snapshot-component/")]
    public class RayfireSnapshot: MonoBehaviour
    {
        [Header ("  Save Properties")]
        [Space (2)]
        
        public string assetName;
        public bool compress;
        
        [Header ("  Load Properties")]
        [Space (2)]
        
        public Object snapshotAsset;
        [Range(0f, 1f)] 
        public float sizeFilter;

        // Reset
        void Reset()
        {
            assetName = gameObject.name;
        }
        
#if UNITY_EDITOR
        
        // Save asset
        public void Snapshot()
        {
            RFSnapshotAsset.Snapshot (gameObject, compress, assetName);
        }

        // Load asset
        public void Load()
        {
            RFSnapshotAsset.Load (snapshotAsset, gameObject, sizeFilter);
        }
#endif     
        
    }
}
