using UnityEngine;
using System.Collections;

public class sr_Pit : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnTriggerEnter (Collider collider)
    {
        if (collider.tag == "Player") {

        }
    }
}
