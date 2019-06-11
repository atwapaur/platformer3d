using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


public class sr_NodeSourceEditor : EditorWindow {

    public Object nodeSource;

    //vertical raycast to detect ground
    public float heightOffset = 2f;
    public float raycastLength = 4f;

    //horizontal spacing of the generated node 'grid'
    public float nodeInterval = 2f;

    //cluster size
    public int sizeX = 10;
    public int sizeY = 10;
    public int sizeZ = 10;

    //debug speed
    public bool debugCreation = false;
    public float debugSpeed = 5f;

    //refs should be found in a permanent script like Main
    static private LayerMask layerGround;
    static private LayerMask layerObstacle;
    static private GameObject nodeContainer;
    static private GameObject nodePrefab;

    static private List<Vector3> exhaustedCastList = new List<Vector3>();
    static private Vector3[] expandOffsets;

    static private Queue<Vector3> creationPool = new Queue<Vector3>();


    private float m_LastEditorUpdateTime;
    private float timer = 0f;



    [MenuItem("Tools/Node Source Generate %#n")]
    static public void Initialize()
    {
        sr_NodeSourceEditor window = (sr_NodeSourceEditor)EditorWindow.GetWindow(typeof(sr_NodeSourceEditor));

        //Refs
        nodeContainer = GameObject.Find("PATHNODES");
        nodePrefab = Resources.Load<GameObject>("Pathfinding/pNode");
        sr_Main main = GameObject.Find("MAIN").GetComponent<sr_Main>();
        layerGround = main.layerGround;
        layerObstacle = main.layerObstacle;
    }


    void OnGUI()
    {
        if (EditorApplication.isPlaying)
            return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Source");
        nodeSource = EditorGUILayout.ObjectField(nodeSource, typeof(Object), true);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Node Interval");
        nodeInterval = EditorGUILayout.FloatField(nodeInterval);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Height Offset (Ground Grid)");
        heightOffset = EditorGUILayout.FloatField(heightOffset);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Raycast Length (Ground Grid)");
        raycastLength = EditorGUILayout.FloatField(raycastLength);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Size X (3D Cluster)");
        sizeX = EditorGUILayout.IntField(sizeX);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Size Y (3D Cluster)");
        sizeY = EditorGUILayout.IntField(sizeY);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Size Z (3D Cluster)");
        sizeZ = EditorGUILayout.IntField(sizeZ);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Debug Creation");
        debugCreation = EditorGUILayout.Toggle(debugCreation);
        EditorGUILayout.EndHorizontal();

        if (debugCreation) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Debug Speed");
            debugSpeed = EditorGUILayout.FloatField(debugSpeed);
            EditorGUILayout.EndHorizontal();
        }
         

        if (GUI.Button(new Rect(20, 175, 400, 50), "Generate Grid of Nodes on Ground Layer")) {
            if (nodeSource != null) {
                GenerateNodesFromSource((GameObject)nodeSource);
            }
        }

        

        if (GUI.Button(new Rect(20, 235, 400, 50), "Generate 3D Cluster of Nodes of dimensions XYZ")) {
            if (nodeSource != null) {
                GenerateNodesCluster((GameObject)nodeSource);
            }
        }

        if (GUI.Button(new Rect(20, 310, 400, 50), "Clear Source-generated Nodes")) {
            if (nodeSource != null) {
                ClearSourceGeneratedNodes();
            }
        }

    }


    public void GenerateNodesCluster (GameObject source)
    {
        ClearSourceGeneratedNodes();

        for (int x=0; x<sizeX; x++) {
            for (int y = 0; y < sizeY; y++) {
                for (int z = 0; z < sizeZ; z++) {
                    CreateClusterNode(source.transform.position + Vector3.right * x * nodeInterval + Vector3.up * y * nodeInterval + Vector3.forward * z * nodeInterval);
                }
            }
        }
    }


    public void GenerateNodesFromSource(GameObject source)
    {
        Debug.Log("GenerateNodesFromSource method");
        ClearSourceGeneratedNodes();

        exhaustedCastList.Clear();

        expandOffsets = new Vector3[] {
            Vector3.right * nodeInterval,
            -Vector3.right * nodeInterval,
            Vector3.forward * nodeInterval,
            -Vector3.forward * nodeInterval
        };

        //First raycast, located at the position of the source node GameObject
        Vector3 raycastStart = source.transform.position + Vector3.up * heightOffset;
        RaycastHit[] hits;
        hits = Physics.RaycastAll(raycastStart, -Vector3.up, raycastLength, layerGround);
        if (hits.Length > 0) {
            exhaustedCastList.Add(raycastStart);
            foreach (RaycastHit hit in hits) {
                CreateNode(hit.point);
            }
            RaycastNodeIntervalFrom(raycastStart);
        } else {
            Debug.Log("No ground in the height-length boundaries of the source node gameObject. Generate procedure aborted.");
        }
    }

    public void RaycastNodeIntervalFrom(Vector3 lastCastPos)
    {
        foreach (Vector3 offsetDir in expandOffsets) {
            Vector3 raycastStart = lastCastPos + offsetDir;
            if (!exhaustedCastList.Contains(raycastStart)) {
                exhaustedCastList.Add(raycastStart);
                RaycastHit[] hits;
                hits = Physics.RaycastAll(raycastStart, -Vector3.up, raycastLength, layerGround);
                if (hits.Length > 0) {
                    foreach (RaycastHit hit in hits) {
                        CreateNode(hit.point);
                    }
                    OrderRaycastNodeIntervalFrom(raycastStart);
                }
            }
        }
    }

    private void OrderRaycastNodeIntervalFrom(Vector3 lastCast)
    {
        RaycastNodeIntervalFrom(lastCast);
    }


    public void CreateClusterNode (Vector3 nodeFinalPos)
    {
        //Check if there is no obtacle close to the position
        RaycastHit[] sphereHits;
        sphereHits = Physics.SphereCastAll(nodeFinalPos, 0.75f, Vector3.up, 2f, layerObstacle);
        if (sphereHits.Length > 0) {
            //obstacle, don't create node
            //Debug.Log("OBSTACLE in the way!");
            return;
        }


        if (debugCreation) {
            creationPool.Enqueue(nodeFinalPos);
            return;
        }

        NodeInstantiate(nodeFinalPos);
    }


    public void CreateNode(Vector3 hitPos)
    {
        Vector3 nodeFinalPos = hitPos + Vector3.up * 0.5f;

        /*Collider[] overlapHits;
        overlapHits = Physics.OverlapSphere(nodeFinalPos, 5f, layerObstacle);
        if (overlapHits.Length > 0) {
            Debug.Log("OBSTACLE in the way!");
            return;
        }*/

        
        //Check if there is no obtacle close to the position
        RaycastHit[] sphereHits;
        sphereHits = Physics.SphereCastAll(nodeFinalPos, 0.75f, Vector3.up, 2f, layerObstacle);
        if (sphereHits.Length > 0) {
            //obstacle, don't create node
            //Debug.Log("OBSTACLE in the way!");
            return;
        }
        

        if (debugCreation) {
            creationPool.Enqueue(nodeFinalPos);
            return;
        }

        NodeInstantiate(nodeFinalPos);
        
    }

    public void ClearSourceGeneratedNodes()
    {
        GameObject source = (GameObject)nodeSource;
        
        foreach (GameObject n in source.GetComponent<sr_NodeSource>().nodesList) {
            DestroyImmediate(n);
        }
        source.GetComponent<sr_NodeSource>().nodesList.Clear();
    }







    protected virtual void OnEnable()
    {
#if UNITY_EDITOR
        Debug.Log("onEnable");
        timer = 0f;
        creationPool.Clear();
        m_LastEditorUpdateTime = Time.realtimeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
        #endif
    }

    protected virtual void OnDisable()
    {
#if UNITY_EDITOR
        Debug.Log("onDISABLE");
        EditorApplication.update -= OnEditorUpdate;
        #endif
    }

    protected virtual void OnEditorUpdate()
    {
        // In here you can check the current realtime, see if a certain
        // amount of time has elapsed, and perform some task.
        
        //Debug.Log(timer);
        if (debugCreation && creationPool.Count > 0) {

            timer += Time.deltaTime * 1000f * debugSpeed;

            if (timer > 1) {
                timer = 0f;

                Vector3 newNodePos = creationPool.Dequeue();

                NodeInstantiate(newNodePos);

            }
        }
        
    }

    private void NodeInstantiate (Vector3 posInstantiate)
    {
        GameObject node = Instantiate(nodePrefab, posInstantiate, Quaternion.identity) as GameObject;
        node.name = "pNode";
        
        GameObject source = (GameObject)nodeSource;
        source.GetComponent<sr_NodeSource>().nodesList.Add(node);

        node.transform.SetParent(source.transform, true);
    }


}
