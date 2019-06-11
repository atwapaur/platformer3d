using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

[Serializable]
public class sr_Order : MonoBehaviour  {

    //All types exposed variables
    public enum Type
    {
        //enum, go, string, float, go, float
        SetControl, //bool
        SetCam,   //go
        Traveling, //go
        Text, //string
        Audio,  //string
        Animator, //go, string
        Animation, //go, string
        Astar, //go, go
        Instantiate, //go, go
        Lerp, //go, go
        Event, //go, string
        Teleport, //go, go
        EnableDisable, //go
        Wait 
        //LookAt
        //TimeScale
        //Zoom / FOV
    }
    public Type type;

    public float duration;
    public bool autoCallback;

    public sr_Order nextOrder;
    public sr_Order[] parallelOrders;


    //Per-type variables
    public GameObject gizmo2;
    public GameObject gizmo;
    public string textline;
    public AudioClip audiofile;
    public Animator characterAnimator;
    public string animatorState;
    public Animation animationComponent;
    public string animationClip;
    public GameObject astarAgent;
    public GameObject prefab;
    public GameObject ingameObject;
    public string methodToCall;
    public float timer;
    public bool boolean;
    public bool boolean2;


    //Private vars
    private sr_Main main;
    private GameObject cam;
    private Text uiSubtitles;
    //[HideInInspector]public sr_Sequence sequence;
    private bool hasCallback = false;


    void Start ()
    {
        main = GameObject.Find("MAIN").GetComponent<sr_Main>();
        cam = GameObject.Find("FirstCamera");
        uiSubtitles = GameObject.Find("uiSubtitles").GetComponent<Text>();
    }


    public void Callback ()
    {
        if (!hasCallback) {

            if (nextOrder) {
                nextOrder.ExecuteOrder();
                //parallel orders ?
            }
            else {
                //main.ControlResume();
            }
            

            //sequence.OrderCallback();
            hasCallback = true;
        }
        
    }

    public void ExecuteOrder ()
    {
        if (parallelOrders.Length > 0) {
            foreach (sr_Order ord in parallelOrders) {
                ord.ExecuteOrder();
            }
        }

        hasCallback = false;

        autoCallback = false;

        switch (type) {
            case Type.SetControl:
                if (boolean) {
                    main.ResumeGameplay(boolean2);
                }else {
                    main.SwitchToCutscene();
                }
                break;

            case Type.SetCam:
                cam.transform.position = gizmo.transform.position;
                cam.transform.rotation = gizmo.transform.rotation;
                break;

            case Type.Traveling:
                StartCoroutine(Traveling(cam, gizmo.transform));
                break;

            case Type.Text:
                uiSubtitles.text = textline;
                Invoke("ClearSubtitles", (textline.ToCharArray().Length * 0.05f) + 1.4f); //0.04 + 1.2
                break;

            case Type.Audio:

                break;

            case Type.Animator:
                //Find the appropriate Animator component
                Animator animator;
                if (ingameObject.tag == "Player") {
                    //Player
                    animator = ingameObject.GetComponent<sr_Player>().myAnimator;
                }else {
                    //NPC
                    animator = ingameObject.GetComponent<sr_Character>().myAnimator;
                }

                if (timer > 0) {
                    animator.CrossFade(animatorState, timer);
                } else {
                    animator.Play(animatorState);
                }


                /*if (timer > 0) {
                    characterAnimator.CrossFade(animatorState, timer);
                } else {
                    characterAnimator.Play(animatorState);
                }*/
                //characterAnimator.GetComponent<CutsceneInitializer>().stateToPlay = animatorState;
                //characterAnimator.GetBehaviour<CutsceneInitializer>().stateToPlay = animatorState;
                break;

            case Type.Animation:
                animationComponent.Play(animationClip);
                break;

            case Type.Astar:
                ingameObject.GetComponent<sr_Character>().OrderPathToTarget(gizmo.transform.position, this);
                ingameObject.GetComponent<sr_Character>().SetRun(boolean);
                ingameObject.GetComponent<sr_Character>().targetNode = gizmo;
                autoCallback = true;
                break;

            case Type.Instantiate:
                Instantiate(prefab, gizmo.transform.position, gizmo.transform.rotation);
                break;

            case Type.Lerp:
                StartCoroutine(Traveling(ingameObject, gizmo.transform));
                break;

            case Type.Event:
                ingameObject.SendMessage(methodToCall);
                break;

            case Type.Teleport:
                ingameObject.transform.position = gizmo.transform.position;
                ingameObject.transform.rotation = gizmo.transform.rotation;
                break;

            case Type.EnableDisable:
                ingameObject.SetActive(boolean);
                break;

            case Type.Wait:
                break;

        }

        if (!autoCallback) {
            Invoke("Callback", duration);
        }
            

    }


    public IEnumerator Traveling(GameObject obj, Transform end)
    {
        Vector3 originPos = obj.transform.position;
        Quaternion originRot = obj.transform.rotation;
        float t = 0f;
        while (t < 1) {
            t += Time.deltaTime / timer;
            obj.transform.position = Vector3.Lerp(originPos, end.position, t);
            obj.transform.rotation = Quaternion.Lerp(originRot, end.rotation, t);
            yield return 0;
        }
    }

    public void ClearSubtitles()
    {
        uiSubtitles.text = "";
        //Callback();
    }
    


    public void TryFixingFor (GameObject subject)
    {
        if (ingameObject == null) {
            if (type == Type.Animator || type == Type.Astar || type == Type.EnableDisable || type == Type.Event || type == Type.Lerp || type == Type.Teleport) {
                ingameObject = subject;
                Debug.Log(subject.name + "  assigned to order of type  " + type.ToString());

                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        
    }

    
    void OnDrawGizmosSelected () {
        if (gizmo != null) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(gizmo.transform.position, 0.6f);
        }
        if (ingameObject != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ingameObject.transform.position, 0.6f);
        }


		/*switch(type) {
		case Type.SetCam:
			break;

		case Type.Traveling:
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(gizmo.transform.position, 0.4f);
            break;
		}*/

	}

	void OnDrawGizmos () {
		if (nextOrder != null) {
			Gizmos.color = Color.green;
			Gizmos.DrawLine(transform.position, nextOrder.transform.position);

            Vector3 right = Quaternion.LookRotation(nextOrder.transform.position- transform.position) * Quaternion.Euler(0, 180 + 30, 0) * new Vector3(0, 0, 1);
            Vector3 left = Quaternion.LookRotation(nextOrder.transform.position - transform.position) * Quaternion.Euler(0, 180 - 30, 0) * new Vector3(0, 0, 1);
            Gizmos.DrawRay(nextOrder.transform.position, right * 0.5f);
            Gizmos.DrawRay(nextOrder.transform.position, left * 0.5f);
        }

        if (parallelOrders.Length > 0) {
            for (int i=0; i<parallelOrders.Length; i++) {
                if (parallelOrders[i] != null) {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(transform.position, parallelOrders[i].transform.position);

                    Vector3 right = Quaternion.LookRotation(parallelOrders[i].transform.position - transform.position) * Quaternion.Euler(0, 180 + 30, 0) * new Vector3(0, 0, 1);
                    Vector3 left = Quaternion.LookRotation(parallelOrders[i].transform.position - transform.position) * Quaternion.Euler(0, 180 - 30, 0) * new Vector3(0, 0, 1);
                    Gizmos.DrawRay(parallelOrders[i].transform.position, right * 0.5f);
                    Gizmos.DrawRay(parallelOrders[i].transform.position, left * 0.5f);
                }
            }
        }
    }
    


}
