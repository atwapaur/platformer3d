using UnityEngine;
using System.Collections;

public class sr_Door : MonoBehaviour {

    private bool isOpen = false;
    private Transform pivot;
    private Quaternion pivotRotOpen;
    private Quaternion pivotRotClose;
    private float turnSpeed = 5f;

	void Start () {
        pivot = transform.GetChild(0);
        pivotRotClose = transform.rotation;
        pivotRotOpen = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + 90, transform.eulerAngles.z);
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    public void Activate ()
    {
        Debug.Log("Activate door");
        StopCoroutine("LerpDoor");
        StartCoroutine("LerpDoor", isOpen);
        isOpen = !isOpen;

    }

    private IEnumerator LerpDoor(bool _isOpen)
    {
        Quaternion startRot = pivot.rotation;
        Quaternion targetRot = Quaternion.identity;
        if (_isOpen) {
            targetRot = pivotRotClose;
        } else {
            targetRot = pivotRotOpen;
        }

        float t = 0f;
        while (t < 1) {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return 0;
        }
        
    }
}
