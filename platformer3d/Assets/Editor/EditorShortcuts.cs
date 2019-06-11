using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EditorShortcuts  {

    [MenuItem("Tools/Hide Or Show Pathfinding Gizmos %#q")]
    static public void HideOrShowPathfindingGizmos()
    {
        

        GameObject container = GameObject.Find("PATHNODES");

        MeshRenderer[] renderers = container.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers) {
            renderer.enabled = !renderer.enabled;
        }

        /*foreach (Transform child in container.transform) {
            MeshRenderer renderer = child.GetComponent<MeshRenderer>();
            renderer.enabled = !renderer.enabled;
        }*/
    }



    [MenuItem("Tools/Fix Character Routines %#r")]
    static public void FixCharacterRoutines()
    {
        if (Selection.activeTransform != null) {
            if (Selection.activeTransform.GetComponent<sr_Character>() != null) {
                Selection.activeTransform.GetComponent<sr_Character>().FixRoutines();
            }

        }
    }



    [MenuItem("Tools/Select Brothers Axis X %#x")]
    static public void SelectBrothersOfSelected_X ()
    {
        List<GameObject> newSelection = new List<GameObject>();

        //Do we have some object selected?
        if (Selection.transforms.Length > 0) {

            foreach (Transform t in Selection.transforms) {

                float coordoCheck = t.position.x;
                Transform parent = t.parent;

                for (int i=0; i<parent.childCount; i++) {
                    Transform brother = parent.GetChild(i);
                    if (brother.position.x == coordoCheck) {
                        newSelection.Add(brother.gameObject);
                    }
                }

            }
            Selection.objects = newSelection.ToArray();
        }
    }

    [MenuItem("Tools/Select Brothers Axis Y %#y")]
    static public void SelectBrothersOfSelected_Y()
    {
        List<GameObject> newSelection = new List<GameObject>();

        //Do we have some object selected?
        if (Selection.transforms.Length > 0) {

            foreach (Transform t in Selection.transforms) {

                float coordoCheck = t.position.y;
                Transform parent = t.parent;

                for (int i = 0; i < parent.childCount; i++) {
                    Transform brother = parent.GetChild(i);
                    if (brother.position.y == coordoCheck) {
                        newSelection.Add(brother.gameObject);
                    }
                }

            }
            Selection.objects = newSelection.ToArray();
        }
    }

    [MenuItem("Tools/Select Brothers Axis Z %#z")]
    static public void SelectBrothersOfSelected_Z()
    {
        List<GameObject> newSelection = new List<GameObject>();

        //Do we have some object selected?
        if (Selection.transforms.Length > 0) {

            foreach (Transform t in Selection.transforms) {

                float coordoCheck = t.position.z;
                Transform parent = t.parent;

                for (int i = 0; i < parent.childCount; i++) {
                    Transform brother = parent.GetChild(i);
                    if (brother.position.z == coordoCheck) {
                        newSelection.Add(brother.gameObject);
                    }
                }

            }
            Selection.objects = newSelection.ToArray();
        }
    }




}
