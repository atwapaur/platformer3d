using UnityEngine;
using System.Collections;

public class sr_Hitbox : MonoBehaviour {

    [HideInInspector]
    public bool canHitAI = false;

    [HideInInspector]
    public bool canHitPlayer = false;

    // Use this for initialization
    void Start () {
	    
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnDrawGizmos ()
    {
        if (canHitAI == true || canHitPlayer == true) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
