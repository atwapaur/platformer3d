using UnityEngine;
using System.Collections;

public class sr_Trigger : MonoBehaviour {

    public bool wantActionButton;
    public sr_Order startSequence;
    public sr_Worm guardingWorm;

    private bool isConsumed = false;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}


    public void DoTrigger ()
    {
        if (isConsumed) {
            return;
        }

        if (startSequence != null) {
            startSequence.ExecuteOrder();
            isConsumed = true;
        }

        if (guardingWorm != null) {
            guardingWorm.GuardingZonePlayerEnter();
        }
    }

    public void LeaveTrigger ()
    {
        if (guardingWorm != null) {
            guardingWorm.GuardingZonePlayerExit();
        }
    }

    void OnDrawGizmos()
    {
        if (startSequence != null) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, startSequence.transform.position);
        }

    }


}
