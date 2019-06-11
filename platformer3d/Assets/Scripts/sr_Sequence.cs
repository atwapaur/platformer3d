using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class sr_Sequence : MonoBehaviour {


    //does it suspend player control?
    public bool isCutscene = false;

    public bool isPlaying = false;
    public bool isLooping = false;

    public List<sr_Order> orders = new List<sr_Order>();

    private int currentOrder = 0;

    private sr_Main main;
    private GameObject player;
    private GameObject cam;
    private GameObject playerBody;
    private Animator playerAnimator;


    void Awake ()
    {
        main = GameObject.Find("MAIN").GetComponent<sr_Main>();

        player = main.player;
        cam = main.cam;
        playerAnimator = main.playerAnimator;

        /*foreach(SeqOrder order in orders) {
            order.sequence = this;
        }*/
    }
	

    public void PlaySequence ()
    {
        if (isCutscene) {
            main.SwitchToCutscene();
        }

        isPlaying = true;
        currentOrder = 0;
        orders[0].ExecuteOrder();
    }

    public void OrderCallback ()
    {
        currentOrder += 1;
        if (currentOrder < orders.Count) {
            orders[currentOrder].ExecuteOrder();
        }
        else {
            FinishSequence();
        }
    }

    private void FinishSequence ()
    {
        if (isLooping) {
            PlaySequence();
        }
        else {
            if (isCutscene) {
                main.ResumeGameplay(true);
            }
            isPlaying = false;
        }
    }



}
