using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class sr_AnimationEventHandler : MonoBehaviour {

    [Serializable]
    public class CharacterSetup
    {
        public string animID;
        public GameObject character;
        public GameObject animParent;
        //public string animatorBool;
    }
    public List<CharacterSetup> characterSetups = new List<CharacterSetup>();

    private string currentAnim;
    private Animation animComponent;
    private bool detectAnimIsOver = false;

    void Awake ()
    {
        animComponent = GetComponent<Animation>();
    }

    void Start ()
    {
        //CALL WHENEVER WE WANT
        //TurnOn("Anim_Charrette");
	}
	
	void Update ()
    {
        if (detectAnimIsOver && !animComponent.isPlaying) {
            detectAnimIsOver = false;
            TurnOff();
        }
    }


    public void TurnOn(string _animID)
    {
        //Check all the characters that should be parented to the Animation parent
        foreach (CharacterSetup setup in characterSetups)
        {
            if (setup.animID == _animID)
            {
                setup.character.transform.SetParent(setup.animParent.transform, false); //parented
                setup.character.transform.localPosition = Vector3.zero;
                setup.character.transform.localRotation = Quaternion.identity;

                setup.character.GetComponent<Animator>().SetBool("isGameplay", false); //controlled by the Animation Event
                setup.character.GetComponent<Animator>().SetTrigger(setup.animID); //Launch it in the Animator Controller
            }
        }
        //Start the Animation
        animComponent.CrossFade(_animID);
        currentAnim = _animID;
        detectAnimIsOver = true;
    }

    private void TurnOff()
    {
        Debug.Log("TurnOff");
        foreach (CharacterSetup setup in characterSetups) {
            setup.character.transform.SetParent(null); //released
            setup.character.GetComponent<Animator>().SetBool("isGameplay", true);
        }
       // TurnOn("Anim_Charrette");
    }

    //Can be called from the Animation via an Event to 'release' the Character from Animation parenting
    //If you want to unparent a Character DURING an animation (for example to make in move via pathfinding)
    public void UnparentCharacter (string _charName)
    {
        foreach (CharacterSetup setup in characterSetups) {
            if (setup.character.name == _charName) {
                setup.character.transform.SetParent(null); //released
                setup.character.GetComponent<Animator>().SetBool("isGameplay", true);               
                Debug.Log(_charName + " is unparented!");
            }

        }
    }
    
    //Set Bool to be transmitted to a Character Animator component
    public void EventSetBool (AnimationEvent animationEvent)
    {
        string characterName = animationEvent.stringParameter;
        int step = animationEvent.intParameter;

        foreach (CharacterSetup setup in characterSetups) {
            if (setup.character.name == characterName) {
                setup.character.GetComponent<Animator>().SetTrigger(currentAnim + step.ToString());
                break;
            }
        }
    }

    
}
