using UnityEngine;
using System.Collections;

public class sr_NodeConnection : MonoBehaviour {

    public Transform highPoint;
    public Transform targetPoint;

    [HideInInspector] public Vector3 startPoint;

	// Use this for initialization
	void Start () {
        startPoint = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnDrawGizmosSelected()
    {
        if (highPoint != null) {
            Gizmos.color = Color.grey;
            Gizmos.DrawLine(transform.position, highPoint.transform.position);
        }

        if (highPoint != null && targetPoint != null) {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(highPoint.transform.position, targetPoint.transform.position);
        }

    }

    public Vector3 Parabola (float t)
    {
        return (((1 - t) * (1 - t)) * startPoint) + (2 * t * (1 - t) * highPoint.position) + ((t * t) * targetPoint.position);
    }
}
