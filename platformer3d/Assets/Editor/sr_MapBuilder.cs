using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using ProBuilder2.Common;
using ProBuilder2.Math;
using ProBuilder2.MeshOperations;

public class sr_MapBuilder : EditorWindow {

    public string levelRootName = "Level_A";
    public Texture2D levelMap;
    public Vector3 worldStartPoint = Vector3.zero;
    public float tileSize = 0.5f;
    public float wallHeight = 6f;

    private const int indexFaceRight = 1;
    private const int indexFaceFwd = 0;
    private const int indexFaceTop = 4;


    [MenuItem("Tools/Texture Map to 3D Builder")]
    static public void Initialize()
    {
        sr_MapBuilder window = (sr_MapBuilder)EditorWindow.GetWindow(typeof(sr_MapBuilder));
    }

    void OnGUI()
    {
        if (EditorApplication.isPlaying)
            return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Level Root Name");
        levelRootName = EditorGUILayout.TextField(levelRootName);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Level Texture");
        levelMap = (Texture2D)EditorGUILayout.ObjectField(levelMap, typeof(Texture2D), true);
        EditorGUILayout.EndHorizontal();

        worldStartPoint = EditorGUILayout.Vector3Field("World Start Point", worldStartPoint);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tile Size");
        tileSize = EditorGUILayout.FloatField(tileSize);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Wall Height");
        wallHeight = EditorGUILayout.FloatField(wallHeight);
        EditorGUILayout.EndHorizontal();


        if (GUI.Button(new Rect(20, 125, 400, 50), "CREATE MAP LAYOUT")) {
            if (!string.IsNullOrEmpty(levelRootName)) {
                CreateMapLayout();
            }
        }

        if (GUI.Button(new Rect(20, 200, 400, 50), "DELETE LEVEL ROOT CHILDREN")) {
            if (!string.IsNullOrEmpty(levelRootName)) {
                DeleteLevelRootChildren();
            }
        }

    }

    private void CreateMapLayout ()
    {
        GameObject levelRoot = GameObject.Find(levelRootName);
        if (levelRoot == null) {
            GameObject go = new GameObject();
            go.transform.position = Vector3.zero;
            go.name = levelRootName;
            levelRoot = go;
        }

        Color[] mapPixels = levelMap.GetPixels();
        List<int> indexesFilled = new List<int>();
        int vCount = 0;

        //Foreach pixels on the map
        for (int j = 0; j < mapPixels.Length; j++) {
            //increment vCount at width interval
            if (j >= levelMap.width * (vCount + 1)) { // was >
                vCount += 1;
            }
            //Is this index already filled?
            if (indexesFilled.Contains(j)) {
                //skip it
                continue;
            }
            //Look at the index color
            Color c = mapPixels[j];
            if (c == Color.black) {
                Debug.Log("Pixel index: " + j);
                Debug.Log("Row: " + vCount);
                //this is black, this is a wall starting... create it!
                pb_Object pbWall = pb_ShapeGenerator.CubeGenerator(new Vector3(tileSize, tileSize, tileSize));
                //heighten it!
                pb_Face faceTop = pbWall.faces[indexFaceTop];
                pbWall.TranslateVertices(faceTop.distinctIndices, Vector3.up * (wallHeight - tileSize));

                //is the wall continuing on the right side?
                int wallSizeH = 0;

                for (int k = j + 1; k <= levelMap.width * (vCount + 1); k++) {
                    if (indexesFilled.Contains(k)) {
                        break;
                    }
                    if (mapPixels[k] == Color.black) {
                        wallSizeH += 1;
                    } else {
                        break;
                    }
                }

                //or is the wall continuing in the forward direction (vertical axis on the map)?
                int wallSizeV = 0;

                int rowCount = levelMap.width;
                for (int k = j + rowCount; k < mapPixels.Length; k += rowCount) {
                    if (mapPixels[k] == Color.black) {
                        wallSizeV += 1;
                        indexesFilled.Add(k);
                    } else {
                        break;
                    }
                }

                //Now actually expand the wall in the correct direction(s)
                if (wallSizeH >= 1) {
                    pbWall.TranslateVertices(pbWall.faces[indexFaceRight].distinctIndices, Vector3.right * (wallSizeH * tileSize));
                }

                if (wallSizeV >= 1) {
                    pbWall.TranslateVertices(pbWall.faces[indexFaceFwd].distinctIndices, Vector3.forward * (wallSizeV * tileSize));
                }

                pbWall.ToMesh();
                pbWall.Refresh();

                GameObject finalWall = pbWall.gameObject;
                finalWall.AddComponent<MeshCollider>();

                float coordoY = vCount;
                float coordoX = j - (vCount * levelMap.width);

                finalWall.transform.position = worldStartPoint + Vector3.forward * tileSize * coordoY + Vector3.right * tileSize * coordoX;
                finalWall.transform.SetParent(levelRoot.transform, true);

                //If we advanced horizontally, offset j increment
                if (wallSizeH > 0) {
                    j += wallSizeH;
                }


            }

        }
    }

    
    private void DeleteLevelRootChildren ()
    {
        GameObject levelRoot = GameObject.Find(levelRootName);
        if (levelRoot == null) {
            Debug.Log("There are no level root object with the specified name.");
        }else {
            //Destroy all children
            int childs = levelRoot.transform.childCount;
            for (int i = childs - 1; i >= 0; i--) {
                DestroyImmediate(levelRoot.transform.GetChild(i).gameObject);
            }
            Debug.Log("Level root children destroyed.");
        }
    }


}
