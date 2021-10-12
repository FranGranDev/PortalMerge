using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RayFire
{
    // Gun script
    [AddComponentMenu("RayFire/Rayfire Gun")]
    [HelpURL("http://rayfirestudios.com/unity-online-help/unity-gun-component/")]
    public class RayfireGun : MonoBehaviour
    {
        [Header("  Properties")]
        [Space (3)]
        
        public AxisType axis = AxisType.XRed;
        [Space (1)]
        [Range(0f, 100f)] public float maxDistance = 50f;
        [Space (1)]
        public Transform target;

        [Header("  Burst")]
        [Space (3)]
        
        [Range(2, 20)] public int rounds = 1;
        [Space (1)]
        [Range(0.01f, 5f)] public float rate = 0.3f;

        [Header("  Impact")]
        [Space (3)]
        
        [Range(0f, 2f)] public float strength = 1f;
        [Space (1)]
        [Range(0f, 10)] public float radius = 1f;
        [Space (1)]
        public bool affectInactive = true;
        [Space (1)]
        public bool demolishCluster = true;
        [Space (1)]
        public bool affectRigidBodies = true;

        [Header("  Damage")]
        [Space (3)]
        
        [Range(0, 100)] public float damage = 1f;

        [Header("  Vfx")]
        [Space (3)]
        
        public bool debris = true;
        [Space (1)]
        public bool dust = true;
        [Space (1)]
        public bool flash = true;
        
        //[HideInInspector] public bool sparks = false;

        // [Header("  Decals")]
        //[HideInInspector] public bool decals = false;
        //[HideInInspector] public List<Material> decalsMaterial;

        [Header("  Properties")]
        [Space (2)]
        
        public RFFlash Flash = new RFFlash();
        
        //[Header("Projectile")]
        //[HideInInspector] public bool projectile = false;
        
        [HideInInspector] public int mask = -1;
        [HideInInspector] public string tagFilter = "Untagged";
        [HideInInspector] public bool showRay = true;
        [HideInInspector] public bool showHit = true;
        [HideInInspector] public bool shooting = false;

        static string untagged = "Untagged";

        // Event
        public RFShotEvent shotEvent = new RFShotEvent();


        Collider[] impactColliders;
        
        // Impact Sparks
        //[Header("Shotgun")]
        //public int pellets = 1;
        //public int spread = 2;
        //public float recoilStr = 1f;
        //public float recoilFade = 1f;
        // Projectile: laser, bullet, pellets
        // Muzzle flash: position, color, str
        // Shell drop: position, direction, prefab, str, rotation
        // Impact decals
        // Impact blood
        // Ricochet

        //// Start is called before the first frame update
        //void Start()
        //{

        //    Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

        //    Debug.Log(mesh.vertices.Length);
        //    Debug.Log(mesh.triangles.Length);
        //    List<Vector3> vertChecked = new List<Vector3>();
        //    Vector3 norm = new Vector3(0f, 0f, -1f);

        //    for (int i = 0; i < mesh.vertices.Length; i++)
        //    {

        //        if (mesh.normals[i] == norm)
        //        {
        //            Debug.Log(mesh.triangles[i]);
        //            Debug.Log(mesh.vertices[i]);
        //        }                
        //    }
        //}


        /// /////////////////////////////////////////////////////////
        /// Single Shot
        /// /////////////////////////////////////////////////////////

        // Start shooting
        public void StartShooting()
        {
            if (shooting == false)
            {
                StartCoroutine(StartShootCor());
            }
        }

        // Start shooting
        IEnumerator StartShootCor()
        {
            // Vars
            int shootId = 0;
            shooting = true;

            while (shooting == true)
            {
                // Single shot
                Shoot(shootId);
                shootId++;

                yield return new WaitForSeconds(rate);
            }
        }

        // Stop shooting
        public void StopShooting()
        {
            shooting = false;
        }

        // Shoot over axis
        public void Shoot(int shootId = 1)
        {
            // Set vector
            Vector3 shootVector = ShootVector;

            // Consider burst recoil // TODO
            if (shootId > 1)
                shootVector = ShootVector;

            // Set position
            Vector3 shootPosition = transform.position;

            // Shoot
            Shoot(shootPosition, shootVector);
        }

        // Shoot over axis
        public void Shoot(Vector3 shootPos, Vector3 shootVector)
        {
            // Event
            shotEvent.InvokeLocalEvent(this);
            RFShotEvent.InvokeGlobalEvent(this);
            
            // Get intersection collider
            RaycastHit hit;
            bool hitState = Physics.Raycast(shootPos, shootVector, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore);
            
            // No hits
            if (hitState == false)
                return;

            // Check for tag
            if (tagFilter != untagged && CompareTag (hit.transform.tag) == false)
                return;
            
            // Pos and normal info
            Vector3 impactPoint  = hit.point;
            Vector3 impactNormal = hit.normal;

            // If mesh collider
            // int triId = hit.triangleIndex;
            // Vector3 bar = hit.barycentricCoordinate;

            // Create impact flash
            ImpactFlash(impactPoint, impactNormal);
            
            // Get rigid from collider or rigid body
            RayfireRigid rigid = hit.collider.attachedRigidbody == null 
                ? hit.collider.GetComponent<RayfireRigid>() 
                : hit.collider.attachedRigidbody.transform.GetComponent<RayfireRigid>();
            
            // Collider has Rigid
            if (rigid != null)
            {
                // Impact Debris and dust
                ImpactDebris (rigid, impactPoint, impactNormal);

                // Impact Dust
                ImpactDust (rigid, impactPoint, impactNormal);

                // Apply damage and return new demolished rigid fragment 
                rigid = ImpactDamage (rigid, hit, shootPos, shootVector, impactPoint);
            }

            // No Rigid script. TODO impact with object without rigid. get Rigid bodies around impact radius
            if (rigid == null)
                return;
     
            // Impact hit to rigid bodies. Activated inactive, detach clusters
            ImpactHit(rigid, hit, impactPoint, shootVector);
        }
        
        // Impact hit to rigid bodies. Activated inactive, detach clusters
        void ImpactHit(RayfireRigid rigid, RaycastHit hit, Vector3 impactPoint, Vector3 shootVector)
        {
            // Prepare impact list
            List<Rigidbody> impactRbList = new List<Rigidbody>();
            
            // Hit object Impact activation and detach before impact force
            if (radius == 0)
            {
                // Inactive Activation
                if (rigid.objectType == ObjectType.Mesh)
                    if (rigid.simulationType == SimType.Inactive || rigid.simulationType == SimType.Kinematic)
                        if (rigid.activation.byImpact == true)
                            rigid.Activate();

                // Connected cluster one fragment detach
                if (rigid.objectType == ObjectType.ConnectedCluster)
                    if (demolishCluster == true)
                        RFDemolitionCluster.DemolishConnectedCluster (rigid, new[] {hit.collider});

                // Collect for impact
                impactRbList.Add (hit.collider.attachedRigidbody);
            }
            
            // Group by radius Impact activation and detach before impact force
            if (radius > 0)
            {
                // Get all colliders
                impactColliders = null;
                impactColliders = Physics.OverlapSphere (impactPoint, radius, mask);
                
                // TODO tag filter
                if (tagFilter != untagged)
                {
                   //  && colliders[i].CompareTag (tagFilter) == false)
                }
                 
                // No colliders. Stop
                if (impactColliders == null) 
                    return;
                    
                // Connected cluster group detach first, check for rigids in range next
                if (rigid.objectType == ObjectType.ConnectedCluster)
                    if (demolishCluster == true)
                        RFDemolitionCluster.DemolishConnectedCluster (rigid, impactColliders);
                
                // Collect all rigid bodies in range
                RayfireRigid scr;
                List<RayfireRigid> impactRigidList = new List<RayfireRigid>();
                for (int i = 0; i < impactColliders.Length; i++)
                {
                    // Get rigid from collider or rigid body
                    scr = impactColliders[i].attachedRigidbody == null 
                        ? impactColliders[i].GetComponent<RayfireRigid>() 
                        : impactColliders[i].attachedRigidbody.transform.GetComponent<RayfireRigid>();
                    
                    // Collect uniq rigids in radius
                    if (scr != null)
                    {
                        if (impactRigidList.Contains (scr) == false)
                            impactRigidList.Add (scr);
                    }
                    // Collect RigidBodies without rigid script
                    else 
                    {
                        if (affectRigidBodies == true)
                            if (impactColliders[i].attachedRigidbody == null)
                                if (impactRbList.Contains (impactColliders[i].attachedRigidbody) == false)
                                    impactRbList.Add (impactColliders[i].attachedRigidbody);
                    }
                }
                
                // Group Activation first
                for (int i = 0; i < impactRigidList.Count; i++)
                    if (impactRigidList[i].activation.byImpact == true)
                        if (impactRigidList[i].simulationType == SimType.Inactive || impactRigidList[i].simulationType == SimType.Kinematic)
                            impactRigidList[i].Activate();
                
                // Collect rigid body from rigid components
                if (strength > 0)
                {
                    for (int i = 0; i < impactRigidList.Count; i++)
                    {
                        // Skip inactive objects
                        if (impactRigidList[i].simulationType == SimType.Inactive && affectInactive == false)
                            continue;

                        // Collect
                        impactRbList.Add (impactRigidList[i].physics.rigidBody);
                    }
                }
            }
            
            // NO Strength
            if (strength == 0)
                return;
            
            // No rigid bodies
            if (impactRbList.Count == 0)
                return;
            
            // Apply force
            for (int i = 0; i < impactRbList.Count; i++)
            {
                // Skip static and kinematik objects
                if (impactRbList[i] == null || impactRbList[i].isKinematic == true)
                    continue;

                // Add force
                impactRbList[i].AddForceAtPosition(shootVector * strength, impactPoint, ForceMode.VelocityChange);
            }
        }
        
        /// /////////////////////////////////////////////////////////
        /// Damage
        /// /////////////////////////////////////////////////////////
        
        // Apply damage. Return new rigid
        RayfireRigid ImpactDamage (RayfireRigid scrRigid, RaycastHit hit, Vector3 shootPos, Vector3 shootVector, Vector3 impactPoint)
        {
            // No damage or damage disabled
            if (damage == 0 || scrRigid.damage.enable == false)
                return scrRigid;
            
            // Check for demolition TODO input collision collider if radius is 0
            bool damageDemolition = scrRigid.ApplyDamage(damage, impactPoint, radius);

            // object was not demolished
            if (damageDemolition == false)
                return scrRigid;
            
            // Target was demolished
            if (scrRigid.HasFragments == true)
            {
                // Get new fragment target
                bool dmlHitState = Physics.Raycast(shootPos, shootVector, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore);
                
                // Get new hit rigid
                if (dmlHitState == true)
                {
                    if (hit.collider.attachedRigidbody != null)
                        return hit.collider.attachedRigidbody.transform.GetComponent<RayfireRigid>();
                    
                    if (hit.collider != null)
                        return hit.collider.transform.GetComponent<RayfireRigid>();
                }
            }
            
            return null;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Burst
        /// /////////////////////////////////////////////////////////

        // Shoot over axis
        public void Burst()
        {
            if (shooting == false)
                StartCoroutine(BurstCor());
        }

        // Burst shooting coroutine
        IEnumerator BurstCor()
        {
            shooting = true;
            for (int i = 0; i < rounds; i++)
            {
                // Stop shooting
                if (shooting == false)
                    break;

                // Single shot
                Shoot(i);

                yield return new WaitForSeconds(rate);
            }
        }

        /// /////////////////////////////////////////////////////////
        /// Vfx
        /// /////////////////////////////////////////////////////////

        // Create impact flash
        void ImpactFlash(Vector3 position, Vector3 normal)
        {
            if (flash == true)
            {
                // Get light position
                Vector3 lightPos = normal * Flash.distance + position;

                // Create light object
                GameObject impactFlashGo = new GameObject ("impactFlash");
                impactFlashGo.transform.position = lightPos;

                // Create light
                Light lightScr = impactFlashGo.AddComponent<Light>();
                lightScr.color     = Flash.color;
                lightScr.intensity = Random.Range (Flash.intensityMin, Flash.intensityMax);
                lightScr.range     = Random.Range (Flash.rangeMin,     Flash.rangeMax);

                lightScr.shadows = LightShadows.Hard;

                // Destroy with delay
                Destroy (impactFlashGo, 0.2f);
            }
        }

        // Impact Debris
        void ImpactDebris(RayfireRigid source, Vector3 impactPos, Vector3 impactNormal)
        {
            if (debris == true && source.HasDebris == true)
                for (int i = 0; i < source.debrisList.Count; i++)
                    if (source.debrisList[i].onImpact == true)
                        RFParticles.CreateDebrisImpact(source.debrisList[i], impactPos, impactNormal);
        }

        // Impact Dust
        void ImpactDust(RayfireRigid source, Vector3 impactPos, Vector3 impactNormal)
        {
            if (dust == true && source.HasDust == true)
                for (int i = 0; i < source.dustList.Count; i++)
                    if (source.dustList[i].onImpact == true)
                        RFParticles.CreateDustImpact(source.dustList[i], impactPos, impactNormal);
        }

        /// /////////////////////////////////////////////////////////
        /// Impact Activation
        /// /////////////////////////////////////////////////////////
        
        // Activate all rigid scripts in radius range
        List<RayfireRigid> ActivationCheck(RayfireRigid scrTarget, Vector3 position)
        {
            // Get rigid list with target object
            List<RayfireRigid> rigidList = new List<RayfireRigid>();
            if (scrTarget != null)
                rigidList.Add (scrTarget);

            // Check fo radius activation
            if (radius > 0)
            {
                // Get all colliders
                Collider[] colliders = Physics.OverlapSphere(position, radius, mask);

                // Collect all rigid bodies in range
                for (int i = 0; i < colliders.Length; i++)
                {
                    // Tag filter
                    if (tagFilter != untagged && colliders[i].CompareTag (tagFilter) == false)
                        continue;

                    // Get attached rigid body
                    RayfireRigid scrRigid = colliders[i].gameObject.GetComponent<RayfireRigid>();

                    // TODO check for connected cluster

                    // Collect new Rigid bodies and rigid scripts
                    if (scrRigid != null && rigidList.Contains(scrRigid) == false)
                        rigidList.Add(scrRigid);
                }
            }

            // Activate Rigid
            for (int i = 0; i < rigidList.Count; i++)
                if (rigidList[i].simulationType == SimType.Inactive || rigidList[i].simulationType == SimType.Kinematic)
                    if (rigidList[i].activation.byImpact == true)
                        rigidList[i].Activate();

            return rigidList;
        }
        
        /// /////////////////////////////////////////////////////////
        /// Getters
        /// /////////////////////////////////////////////////////////

        // Get shooting ray
        public Vector3 ShootVector
        {
            get
            {
                // Vector to target if defined
                if (target != null)
                {
                    Vector3 targetRay = target.position - transform.position;
                    return targetRay.normalized;
                }

                // Vectors by axis
                if (axis == AxisType.XRed)
                    return transform.right;
                if (axis == AxisType.YGreen)
                    return transform.up;
                if (axis == AxisType.ZBlue)
                    return transform.forward;
                return transform.up;
            }
        }
    }
}
