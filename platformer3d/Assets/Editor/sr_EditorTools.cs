using UnityEngine;
using UnityEditor;
using System.Collections;


public class sr_EditorTools : EditorWindow {


    [MenuItem ("Tools/EditorTools")]
	static public void Initialize () {
		sr_EditorTools window = (sr_EditorTools)EditorWindow.GetWindow (typeof (sr_EditorTools));
	}

	// Use this for initialization
	void Start () {
	
	}

	public void SelectAllMeshTriggersInScene()
	{
		//Selection.objects = FindObjectsOfType<MeshCollider>().Where(mc => mc.isTrigger && !mc.convex).Select(mc => mc.gameObject).ToArray();
		/*Object[] array = FindObjectsOfType<MeshCollider>();
		foreach (Object o in array) {
			if ((GameObject)o.GetComponent<MeshCollider>().isTrigger) {

			}
		}*/

		Selection.objects = FindObjectsOfType<MeshCollider>();
	}

    private static void DeleteChildrenOfSelected()
    {
        //Do we have some object selected?
        if (Selection.transforms.Length > 0) {

            foreach (Transform t in Selection.transforms) {

                //Destroy all children
                int childs = t.childCount;
                for (int i = childs - 1; i >= 0; i--) {
                    DestroyImmediate(t.GetChild(i).gameObject);
                }

            }

        }
    }

    


    //the gui code inside the EditorWindow
    void OnGUI () {
		if (GUI.Button(new Rect(5, 5, 300, 50), "Select All Mesh Triggers")) {
			SelectAllMeshTriggersInScene();
		}

        if (GUI.Button(new Rect(5, 60, 300, 50), "Delete Children Of Selected")) {
            DeleteChildrenOfSelected();
        }
    }
}
