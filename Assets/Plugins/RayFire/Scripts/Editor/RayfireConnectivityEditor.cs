using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RayFire
{
    [CanEditMultipleObjects]
    [CustomEditor (typeof(RayfireConnectivity))]
    public class RayfireConnectivityEditor : Editor
    {
        RayfireConnectivity conn;
        static Color wireColor = new Color (0.58f, 0.77f, 1f);

        // Draw gizmo
        [DrawGizmo (GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmosSelected (RayfireConnectivity targ, GizmoType gizmoType)
        {
            // Connections
            //if (targ.enabled == true)
            {

                if (RFCluster.IntegrityCheck (targ.cluster) == false)
                {
                    Debug.Log ("RayFire Connectivity: " + targ.name + " has missing shards. Reset or Setup cluster.", targ.gameObject);
                }

                ClusterDraw (targ);
                GizmoDraw (targ);
            }
        }

        static void GizmoDraw(RayfireConnectivity targ)
        {
            if (targ.showGizmo == true)
            {
                // Gizmo properties
                Gizmos.color = wireColor;
                if (targ.transform.childCount > 0)
                {
                    Bounds bound = RFCluster.GetChildrenBound (targ.transform);
                    Gizmos.DrawWireCube (bound.center, bound.size);
                }
            }
        }
        
        // Inspector
        public override void OnInspectorGUI()
        {
            // Get target
            conn = target as RayfireConnectivity;
            if (conn == null)
                return;

            GUILayout.Space (8);
            
            ClusterSetupUI();
            
            ClusterPreviewUI();

            ClusterCollapseUI();
            
            GUILayout.Space (3);
            
            if (conn.cluster.shards.Count > 0)
                GUILayout.Label ("    Cluster Shards: " + conn.cluster.shards.Count);

            DrawDefaultInspector();
        }
        
        void ClusterSetupUI()
        {
            GUILayout.Label ("  Cluster", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();

            if (GUILayout.Button ("Setup Cluster", GUILayout.Height (25)))
            {
                if (Application.isPlaying == false)
                    foreach (var targ in targets)
                        if (targ as RayfireConnectivity != null)
                        {
                            (targ as RayfireConnectivity).cluster = new RFCluster();
                            (targ as RayfireConnectivity).SetByChildren();
                            SetDirty (targ as RayfireConnectivity);
                        }
                SceneView.RepaintAll();
            }

            if (GUILayout.Button ("Reset Cluster", GUILayout.Height (25)))
            {
                if (Application.isPlaying == false)
                    foreach (var targ in targets)
                        if (targ as RayfireConnectivity != null)
                        {
                            (targ as RayfireConnectivity).cluster = new RFCluster();
                            SetDirty (targ as RayfireConnectivity);
                        }
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }
        
        void ClusterCollapseUI()
        {
            GUILayout.Label ("  Collapse", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            GUILayout.Label ("By Area:", GUILayout.Width (55));

            // Start check for slider change
            EditorGUI.BeginChangeCheck();
            conn.cluster.areaCollapse = EditorGUILayout.Slider(conn.cluster.areaCollapse, conn.cluster.minimumArea, conn.cluster.maximumArea);
            if (EditorGUI.EndChangeCheck() == true)
                if (Application.isPlaying)
                    RFCollapse.AreaCollapse (conn, conn.cluster.areaCollapse);;

            EditorGUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label ("By Size:", GUILayout.Width (55));

            // Start check for slider change
            EditorGUI.BeginChangeCheck();
            conn.cluster.sizeCollapse = EditorGUILayout.Slider(conn.cluster.sizeCollapse, conn.cluster.minimumSize, conn.cluster.maximumSize);
            if (EditorGUI.EndChangeCheck() == true)
                if (Application.isPlaying)
                    RFCollapse.SizeCollapse (conn, conn.cluster.sizeCollapse);;

            EditorGUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Label ("Random:", GUILayout.Width (55));

            // Start check for slider change
            EditorGUI.BeginChangeCheck();
            conn.cluster.randomCollapse = EditorGUILayout.IntSlider(conn.cluster.randomCollapse, 0, 100);
            if (EditorGUI.EndChangeCheck() == true)
                if (Application.isPlaying)
                    RFCollapse.RandomCollapse (conn, conn.cluster.randomCollapse, conn.seed);;
            
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button ("Start Collapse", GUILayout.Height (25)))
                if (Application.isPlaying)
                    foreach (var targ in targets)
                        if (targ as RayfireConnectivity != null)
                            RFCollapse.StartCollapse (targ as RayfireConnectivity);
        }

        void ClusterPreviewUI()
        {
            // Show center toggle
            EditorGUI.BeginChangeCheck();
            
            conn.showGizmo = GUILayout.Toggle (conn.showGizmo, " Show Gizmo ", "Button", GUILayout.Height (22));
            
            GUILayout.BeginHorizontal();

            // Show nodes
            conn.showConnections = GUILayout.Toggle (conn.showConnections, "Show Connections", "Button", GUILayout.Height (22));
            conn.showNodes = GUILayout.Toggle (conn.showNodes,             "    Show Nodes     ", "Button", GUILayout.Height (22));
            
            if (EditorGUI.EndChangeCheck())
            { 
                foreach (var targ in targets)
                    if (targ as RayfireConnectivity != null)
                    {
                        (targ as RayfireConnectivity).showConnections = conn.showConnections;
                        (targ as RayfireConnectivity).showNodes = conn.showNodes;
                        SetDirty (targ as RayfireConnectivity);
                    }
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }

        static void ClusterDraw(RayfireConnectivity targ)
        {
            if (targ.showNodes == true || targ.showConnections == true)
            {
                if (targ.cluster != null && targ.cluster.shards.Count > 0)
                {
                    // Reinit connections
                    if (targ.cluster.initialized == false)
                       RayfireConnectivity.InitShards (targ.rigidList, targ.cluster);

                    for (int i = 0; i < targ.cluster.shards.Count; i++)
                    {
                        if (targ.cluster.shards[i].tm != null)
                        {
                            // Color
                            if (targ.cluster.shards[i].rigid == null)
                                Gizmos.color = targ.cluster.shards[i].uny == true ? Color.red : Color.green;
                            else
                                Gizmos.color = targ.cluster.shards[i].rigid.activation.unyielding == true ? Color.red : Color.green;

                            // Nodes
                            if (targ.showNodes == true)
                                Gizmos.DrawWireSphere (targ.cluster.shards[i].tm.position, targ.cluster.shards[i].sz / 12f);

                            // Connection
                            if (targ.showConnections == true)
                                for (int j = 0; j < targ.cluster.shards[i].neibShards.Count; j++)
                                    if (targ.cluster.shards[i].neibShards[j].tm != null)
                                        Gizmos.DrawLine (targ.cluster.shards[i].tm.position, targ.cluster.shards[i].neibShards[j].tm.position);
                        }
                    }
                }
            }
        }
        
        // Set dirty
        void SetDirty (RayfireConnectivity scr)
        {
            if (Application.isPlaying == false)
            {
                EditorUtility.SetDirty (scr);
                EditorSceneManager.MarkSceneDirty (scr.gameObject.scene);
            }
        }
    }
}