using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RayFire
{
    [System.Serializable]
    public class RFPoolingParticles
    {
        public bool enable = true;
        [Range (1, 500)] public int capacity = 60;
        
        // Hidden
        [HideInInspector] public int poolRate = 2;
        [HideInInspector] public ParticleSystem poolInstance;
        [HideInInspector] public Transform poolRoot;
        [HideInInspector] public List<ParticleSystem> poolList;

        // Constructor
        public RFPoolingParticles()
        {



        }

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////

        // Create pool root
        public void CreatePoolRoot (Transform manTm)
        {
            if (poolRoot == null)
            {
                GameObject poolGo = new GameObject ("Pool_Particles");
                poolRoot          = poolGo.transform;
                poolRoot.position = manTm.position;
                poolRoot.parent   = manTm;
            }
        }

        // Create pool object
        public void CreateInstance (Transform manTm)
        {
            // Return if not null
            if (poolInstance != null)
                return;

            // Create pool instance
            poolInstance = CreateParticleInstance();

            // Set tm
            poolInstance.transform.position = manTm.position;
            poolInstance.transform.rotation = manTm.rotation;

            // Set parent
            poolInstance.transform.parent = poolRoot;
        }

        // Create pool object
        public static ParticleSystem CreateParticleInstance()
        {
            // Create root
            GameObject host = new GameObject("Instance");
            host.SetActive (false);

            // Particle system
            ParticleSystem ps = host.AddComponent<ParticleSystem>();
            
            // Stop for further properties set
            ps.Stop();
            
            return ps;
        }
        
        // Get pool object
        public ParticleSystem GetPoolObject (Transform manTm)
        {
            ParticleSystem scr;
            if (poolList.Count > 0)
            {
                scr = poolList[poolList.Count - 1];
                poolList.RemoveAt (poolList.Count - 1);
            }
            else
                scr = CreatePoolObject (manTm);

            return scr;
        }

        // Create pool object
        ParticleSystem CreatePoolObject (Transform manTm)
        {
            // Create instance if null
            if (poolInstance == null)
                CreateInstance (manTm);

            // Create
            return Object.Instantiate (poolInstance, poolRoot);
        }

        // Keep full pool 
        public IEnumerator StartPoolingCor (Transform manTm)
        {
            // Clear list
            poolList = new List<ParticleSystem>();

            WaitForSeconds delay = new WaitForSeconds (0.53f);

            // Pooling loop
            while (enable == true)
            {
                // Create if not enough
                if (poolList.Count < capacity)
                    for (int i = 0; i < poolRate; i++)
                        poolList.Add (CreatePoolObject (manTm));

                // Wait next frame
                yield return delay;
            }
        }
    }

    [System.Serializable]
    public class RFPoolingFragment
    {
        public bool enable = true;
        [Range (1, 500)] public int capacity = 60;
        
        // Hidden
        [HideInInspector] public int poolRate = 2;
        [HideInInspector] public RayfireRigid poolInstance;
        [HideInInspector] public Transform poolRoot;
        [HideInInspector] public List<RayfireRigid> poolList;

        // Constructor
        public RFPoolingFragment()
        {



        }

        /// /////////////////////////////////////////////////////////
        /// Methods
        /// /////////////////////////////////////////////////////////

        // Create pool root
        public void CreatePoolRoot (Transform manTm)
        {
            if (poolRoot == null)
            {
                GameObject poolGo = new GameObject ("Pool_Fragments");
                poolRoot          = poolGo.transform;
                poolRoot.position = manTm.position;
                poolRoot.parent   = manTm;
            }
        }

        // Create pool object
        public void CreateInstance (Transform manTm)
        {
            // Return if not null
            if (poolInstance != null)
                return;

            // Create pool instance
            poolInstance = CreateRigidInstance();

            // Set tm
            poolInstance.transForm.position = manTm.position;
            poolInstance.transForm.rotation = manTm.rotation;

            // Set parent
            poolInstance.transForm.parent = poolRoot;
        }

        // Create pool object
        public static RayfireRigid CreateRigidInstance()
        {
            // Create
            GameObject instance = new GameObject ("Instance");

            // Turn off
            instance.SetActive (false);

            // Setup
            MeshFilter   mf            = instance.AddComponent<MeshFilter>();
            MeshRenderer mr            = instance.AddComponent<MeshRenderer>();
            RayfireRigid rigidInstance = instance.AddComponent<RayfireRigid>();
            rigidInstance.initialization = RayfireRigid.InitType.AtStart;
            Rigidbody rb = instance.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = RayfireMan.inst.collisionDetection;
            
            // Define components
            rigidInstance.transForm         = instance.transform;
            rigidInstance.meshFilter        = mf;
            rigidInstance.meshRenderer      = mr;
            rigidInstance.physics.rigidBody = rb;

            return rigidInstance;
        }

        // Get pool object
        public RayfireRigid GetPoolObject (Transform manTm)
        {
            RayfireRigid scr;
            if (poolList != null && poolList.Count > 0)
            {
                scr = poolList[poolList.Count - 1];
                poolList.RemoveAt (poolList.Count - 1);
            }
            else
                scr = CreatePoolObject (manTm);

            return scr;
        }

        // Create pool object
        RayfireRigid CreatePoolObject (Transform manTm)
        {
            // Create instance if null
            if (poolInstance == null)
                CreateInstance (manTm);

            // Create
            return Object.Instantiate (poolInstance, poolRoot);
        }

        // Keep full pool 
        public IEnumerator StartPoolingCor (Transform manTm)
        {
            // Clear list
            poolList = new List<RayfireRigid>();

            WaitForSeconds delay = new WaitForSeconds (0.5f);

            // Pooling loop
            while (enable == true)
            {
                // Create if not enough
                if (poolList.Count < capacity)
                    for (int i = 0; i < poolRate; i++)
                        poolList.Add (CreatePoolObject (manTm));

                // Wait next frame
                yield return delay;
            }
        }
    }
}
