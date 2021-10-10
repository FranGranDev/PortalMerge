using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireRigid))]
    public class RayfireRigidEditor : Editor
    {
        // Target
        RayfireRigid rigid;
 
        // Draw gizmo
        [DrawGizmo (GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmosSelected (RayfireRigid targ, GizmoType gizmoType)
        {
            ClusterDraw (targ);
        }

        public override void OnInspectorGUI()
        {
            // Get target
            rigid = target as RayfireRigid;
            if (rigid == null)
                return;
            
            // Space
            GUILayout.Space (8);
            
            /*// Input mesh in Editor
            if (Application.isPlaying == false)
            {
                // Delete last
                if (rigid.meshDemolition.meshInput == RFDemolitionMesh.MeshInputType.InEditor &&
                    rigid.objectType == ObjectType.Mesh)
                {
                    // Fragmentation section Begin
                    GUILayout.BeginHorizontal();
                    
                    // Input mesh
                    if (GUILayout.Button (" Input Mesh ", GUILayout.Height (22)))
                        foreach (var targ in targets)
                            if (targ as RayfireRigid != null)
                            {
                                (targ as RayfireRigid).meshDemolition.rfShatter = null;
                                (targ as RayfireRigid).MeshInput();
                            }

                    // Remove mesh
                    if (rigid.meshDemolition.rfShatter != null)
                        if (GUILayout.Button (" Remove ", GUILayout.Height (22)))
                            foreach (var targ in targets)
                                if (targ as RayfireRigid != null)
                                    (targ as RayfireRigid).meshDemolition.rfShatter = null;

                    // Fragmentation section End
                    EditorGUILayout.EndHorizontal();
                    
                    // Notification
                    GUILayout.Label (rigid.meshDemolition.rfShatter == null 
                        ? "Input mesh is not defined" 
                        : "Input mesh is defined");
                }
            }*/
            
            // Initialize
            if (Application.isPlaying == true)
            {
                if (rigid.initialized == false)
                {
                    if (GUILayout.Button ("Initialize", GUILayout.Height (25)))
                        foreach (var targ in targets)
                            if (targ as RayfireRigid != null)
                                if ((targ as RayfireRigid).initialized == false)
                                    (targ as RayfireRigid).Initialize();
                }
                
                // Reuse
                else
                {
                    if (GUILayout.Button ("Reset Rigid", GUILayout.Height (25)))
                            foreach (var targ in targets)
                                if (targ as RayfireRigid != null)
                                    if ((targ as RayfireRigid).initialized == true)
                                        (targ as RayfireRigid).ResetRigid();
                }
            }
            
            RigidManUI();
            
            
            // Clusters
            if (rigid.objectType == ObjectType.NestedCluster || rigid.objectType == ObjectType.ConnectedCluster)
            {
                GUILayout.Label ("  Cluster", EditorStyles.boldLabel);
                
                ClusterSetupUI();
                
                GUILayout.Space (1);

                ClusterPreviewUI(rigid);

                ClusterCollapseUI();
            }
            
            // Cache info
            if (rigid.HasMeshes == true)
                GUILayout.Label ("Precached Unity Meshes: " + rigid.meshes.Length);
            if (rigid.HasFragments == true)
                GUILayout.Label ("Fragments: " + rigid.fragments.Count);
            if (rigid.HasRfMeshes == true)
                GUILayout.Label ("Precached Serialized Meshes: " + rigid.rfMeshes.Length);

            // Demolition info
            if (Application.isPlaying == true && rigid.enabled == true && rigid.initialized == true)
            {
                // Space
                GUILayout.Space (3);

                // Info
                GUILayout.Label ("Info", EditorStyles.boldLabel);

                // Excluded
                if (rigid.physics.exclude == true)
                    GUILayout.Label ("WARNING: Object excluded from simulation.");

                // Size
                GUILayout.Label ("    Size: " + rigid.limitations.bboxSize.ToString());

                // Demolition
                GUILayout.Label ("    Demolition depth: " + rigid.limitations.currentDepth.ToString() + "/" + rigid.limitations.depth.ToString());

                // Damage
                if (rigid.damage.enable == true)
                    GUILayout.Label ("    Damage applied: " + rigid.damage.currentDamage.ToString() + "/" + rigid.damage.maxDamage.ToString());
                
                // Fading
                if (rigid.fading.state == 1)
                    GUILayout.Label ("    Object about to fade...");
                
                // Fading
                if (rigid.fading.state == 2)
                    GUILayout.Label ("    Fading in progress...");

                // Bad mesh
                if (rigid.meshDemolition.badMesh > RayfireMan.inst.advancedDemolitionProperties.badMeshTry)
                    GUILayout.Label ("    Object has bad mesh and will not be demolished anymore");
            }
            
            // Cluster info
            if (rigid.objectType == ObjectType.NestedCluster || rigid.objectType == ObjectType.ConnectedCluster)
            {
                if (rigid.physics.clusterColliders != null && rigid.physics.clusterColliders.Count > 0)
                {
                    if (rigid.clusterDemolition != null && rigid.clusterDemolition.cluster != null)
                    {
                        GUILayout.Label ("    Cluster Colliders: " + rigid.physics.clusterColliders.Count);
                        GUILayout.Label ("    Cluster Shards: " + rigid.clusterDemolition.cluster.shards.Count);
                    }
                }
            }
            
            GUILayout.Space (5);

            // Draw script UI
            DrawDefaultInspector();
        }

        void RigidManUI()
        {
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button ("Demolish", GUILayout.Height (25)))
                if (Application.isPlaying == true)
                    foreach (var targ in targets)
                        if (targ as RayfireRigid != null)
                        {
                            (targ as RayfireRigid).physics.position = (targ as RayfireRigid).transform.position;
                            (targ as RayfireRigid).physics.rotation = (targ as RayfireRigid).transform.rotation;
                            (targ as RayfireRigid).Demolish();
                        }

            if (GUILayout.Button ("Activate", GUILayout.Height (25)))
                if (Application.isPlaying == true)
                    foreach (var targ in targets)
                        if (targ as RayfireRigid != null)
                            if ((targ as RayfireRigid).simulationType == SimType.Inactive || (targ as RayfireRigid).simulationType == SimType.Kinematic)
                                (targ as RayfireRigid).Activate();
            
            if (GUILayout.Button ("Fade", GUILayout.Height (25)))
                if (Application.isPlaying == true)
                    foreach (var targ in targets)
                        if (targ as RayfireRigid != null)
                            if ((targ as RayfireRigid).fading.fadeType != FadeType.None)
                                (targ as RayfireRigid).Fade();
            
            EditorGUILayout.EndHorizontal();
        }

        void ClusterSetupUI()
        {
            GUILayout.BeginHorizontal();
             
            if (GUILayout.Button ("Setup Cluster", GUILayout.Height (25)))
                if (Application.isPlaying == false)
                    foreach (var targ in targets)
                        if (targ as RayfireRigid != null)
                        {
                            RFDemolitionCluster.SetupCluster (targ as RayfireRigid);
                            SetDirty (targ as RayfireRigid); 
                        }
            
            if (GUILayout.Button (  "Reset Cluster", GUILayout.Height (25)))
                if (Application.isPlaying == false)
                    foreach (var targ in targets)
                        if (targ as RayfireRigid != null)
                        {
                            RFPhysic.DestroyColliders (targ as RayfireRigid);
                            (targ as RayfireRigid).clusterDemolition.cluster = new RFCluster();
                            (targ as RayfireRigid).clusterDemolition.clsCount = 1;
                            (targ as RayfireRigid).clusterDemolition.minorClusters = null;
                            SetDirty (targ as RayfireRigid); 
                        }

            EditorGUILayout.EndHorizontal();
        }
        
        // CLuster collapse ui
        void ClusterCollapseUI()
        {
            if (rigid.objectType == ObjectType.ConnectedCluster)
            {
                GUILayout.Label ("  Collapse", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();

                GUILayout.Label ("By Area:", GUILayout.Width (55));

                // Start check for slider change
                EditorGUI.BeginChangeCheck();
                rigid.clusterDemolition.cluster.areaCollapse = EditorGUILayout.Slider (rigid.clusterDemolition.cluster.areaCollapse,
                    rigid.clusterDemolition.cluster.minimumArea, rigid.clusterDemolition.cluster.maximumArea);
                if (EditorGUI.EndChangeCheck() == true)
                    if (Application.isPlaying == true)
                        RFCollapse.AreaCollapse (rigid, rigid.clusterDemolition.cluster.areaCollapse);
                ;

                EditorGUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUILayout.Label ("By Size:", GUILayout.Width (55));

                // Start check for slider change
                EditorGUI.BeginChangeCheck();
                rigid.clusterDemolition.cluster.sizeCollapse = EditorGUILayout.Slider (rigid.clusterDemolition.cluster.sizeCollapse,
                    rigid.clusterDemolition.cluster.minimumSize, rigid.clusterDemolition.cluster.maximumSize);
                if (EditorGUI.EndChangeCheck() == true)
                    if (Application.isPlaying == true)
                        RFCollapse.SizeCollapse (rigid, rigid.clusterDemolition.cluster.sizeCollapse);
                ;

                EditorGUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                GUILayout.Label ("Random:", GUILayout.Width (55));

                // Start check for slider change
                EditorGUI.BeginChangeCheck();
                rigid.clusterDemolition.cluster.randomCollapse = EditorGUILayout.IntSlider (rigid.clusterDemolition.cluster.randomCollapse, 0, 100);
                if (EditorGUI.EndChangeCheck() == true)
                    RFCollapse.RandomCollapse (rigid, rigid.clusterDemolition.cluster.randomCollapse, rigid.clusterDemolition.seed);

                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button ("Start Collapse", GUILayout.Height (25)))
                    if (Application.isPlaying)
                        foreach (var targ in targets)
                            if (targ as RayfireRigid != null)
                                RFCollapse.StartCollapse (targ as RayfireRigid);
            }
        }
        
        // Cluster preview ui
        void ClusterPreviewUI(RayfireRigid scr)
        {
            if (rigid.objectType == ObjectType.ConnectedCluster)
            {
                GUILayout.BeginHorizontal();

                // Show nodes
                EditorGUI.BeginChangeCheck();
                scr.clusterDemolition.cn = GUILayout.Toggle (scr.clusterDemolition.cn, "Show Connections",   "Button", GUILayout.Height (22));
                scr.clusterDemolition.nd       = GUILayout.Toggle (scr.clusterDemolition.nd,       "    Show Nodes    ", "Button", GUILayout.Height (22));
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (Object targ in targets)
                        if (targ as RayfireRigid != null)
                        {
                            (targ as RayfireRigid).clusterDemolition.cn = rigid.clusterDemolition.cn;
                            (targ as RayfireRigid).clusterDemolition.nd       = rigid.clusterDemolition.nd;
                            SetDirty (targ as RayfireRigid); 
                        }
                    SceneView.RepaintAll();
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        void SetDirty(RayfireRigid scr)
        {
            if (Application.isPlaying == false)
            {
                EditorUtility.SetDirty (scr);
                EditorSceneManager.MarkSceneDirty (scr.gameObject.scene);
            }
        }
        
        // CLuster connection and nodes viewport preview
        static void ClusterDraw(RayfireRigid targ)
        {
            if (targ.objectType == ObjectType.ConnectedCluster)
            {
                if (targ.clusterDemolition.cluster != null && targ.clusterDemolition.cluster.shards.Count > 0)
                {
                    // Reinit connections
                    if (targ.clusterDemolition.cluster.initialized == false)
                        RFCluster.InitCluster (targ, targ.clusterDemolition.cluster);
                    
                    // Draw
                    for (int i = 0; i < targ.clusterDemolition.cluster.shards.Count; i++)
                    {
                        if (targ.clusterDemolition.cluster.shards[i].uny == false)
                        {
                            if (targ.clusterDemolition.cluster.shards[i].nIds.Count > 0)
                                Gizmos.color = Color.blue;
                            else
                                Gizmos.color = Color.gray;
                        }
                        else
                            Gizmos.color = Color.red;
                       
                        if (targ.clusterDemolition.nd == true)
                            Gizmos.DrawWireSphere (targ.clusterDemolition.cluster.shards[i].tm.position, targ.clusterDemolition.cluster.shards[i].sz / 12f);
                        if (targ.clusterDemolition.cn == true)
                            if (targ.clusterDemolition.cluster.shards[i].neibShards != null)
                                for (int j = 0; j < targ.clusterDemolition.cluster.shards[i].neibShards.Count; j++)
                                    Gizmos.DrawLine (targ.clusterDemolition.cluster.shards[i].tm.position, targ.clusterDemolition.cluster.shards[i].neibShards[j].tm.position);
                    }
                }
            }
        }
    }
}