using UnityEngine;
using System.Collections;

public class sr_Breakable : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnTriggerEnter(Collider collider)
    {
        if (collider.GetComponent<sr_Hitbox>() != null) {
            if (collider.GetComponent<sr_Hitbox>().canHitAI) {
                ObjectBreak();
                collider.GetComponent<sr_Hitbox>().canHitAI = false;
            }
        }
    }

    private void ObjectBreak ()
    {
        Debug.Log("Object Break!");
    }

}
