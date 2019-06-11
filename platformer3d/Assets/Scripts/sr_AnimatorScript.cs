using UnityEngine;
using System.Collections;

public class sr_AnimatorScript : MonoBehaviour {

    private Animator myAnimator;
    private AnimatorStateInfo stateInfo;


	// Use this for initialization
	void Start () {
        myAnimator = GetComponent<Animator>();
        stateInfo = myAnimator.GetCurrentAnimatorStateInfo(0);
        //stateInfo.fullPathHash(Base Layer.Walk)
	}
	
	// Update is called once per frame
	void Update () {
        
    }

    public void SetTrigger (string id)
    {
        myAnimator.SetTrigger(id);
    }
}
