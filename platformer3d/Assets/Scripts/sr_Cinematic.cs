using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.UI;

public class sr_Cinematic : MonoBehaviour {

    [Serializable] public class Plan
    {
        public GameObject cameraGizmo;
        public GameObject targetTraveling;
        public float startFOV;
        public float targetFOV;
        public float lightRange;
        public float lightIntensity;
        public float duration;
        public string subtitles;
        public AudioClip voiceover;
        public float timeBeforeVoice;
        public GameObject animObject;
        public string animToPlay;
    }

    [Serializable] public class Scene
    {
        public string sceneID;
        public List<Plan> plans = new List<Plan>();
    }
    public List<Scene> scenes = new List<Scene>();

    private Scene currentScene;
    private Plan currentPlan;
    private int planIndex;
    private GameObject player;
    private GameObject cam;
    private Text uiSubtitles;
    private Light camLight;
    private Animation playerAnimation;
    private Animator playerAnimator;
    private GameObject playerBody;


	// Use this for initialization
	void Start () {
        player = GameObject.Find("Player");
        cam = GameObject.Find("FirstCamera");
        //uiSubtitles = GameObject.Find("uiSubtitles").GetComponent<Text>();
        //camLight = GameObject.Find("CamLight").GetComponent<Light>();
        playerAnimation = player.GetComponent<Animation>();
        playerBody = GameObject.Find("PlayerBody");
        playerAnimator = playerBody.GetComponent<Animator>();

        //playerBody.SetActive(false);

        //PlayScene("introTest");
        CleanGizmos();
        //PlayScene("charretteDump");
    }
	
	// Update is called once per frame
	void Update () {

	}

    private void CleanGizmos ()
    {
        GameObject[] gizs = GameObject.FindGameObjectsWithTag("Gizmo");
        foreach (GameObject g in gizs)
        {
            g.GetComponent<Renderer>().enabled = false;
        }
    }

    public void PlayScene (string id)
    {
        foreach (Scene scene in scenes)
        {
            if (scene.sceneID == id)
            {
                //Suspend control
                player.GetComponent<sr_Movement>().enabled = false;
                cam.GetComponent<sr_CameraControl>().enabled = false;
                playerAnimator.SetBool("isGameplay", false);

                currentScene = scene;
                planIndex = 0;
                ShowPlan();
                return;
            }
        }
    }

    //Give Control back to the Player FPS Controller/Camera
    public void ResumeGameplay ()
    {
        cam.GetComponent<Camera>().fieldOfView = 70f;

        player.GetComponent<sr_Movement>().enabled = true;
        cam.GetComponent<sr_CameraControl>().enabled = true;

        playerAnimator.SetBool("isGameplay", true);
    }

    private void ShowPlan ()
    {
        Plan plan = currentScene.plans[planIndex];
        currentPlan = plan;
        cam.transform.position = plan.cameraGizmo.transform.position;
        cam.transform.rotation = plan.cameraGizmo.transform.rotation;

        if (plan.lightIntensity > 0)
        {
            camLight.transform.position = cam.transform.position;
            camLight.range = plan.lightRange;
            camLight.intensity = plan.lightIntensity;
        }
        
        if (plan.startFOV != 0)
        {
            cam.GetComponent<Camera>().fieldOfView = plan.startFOV;
        }
        
        if (plan.targetFOV != 0 && plan.targetFOV != plan.startFOV)
        {
            StartCoroutine("LerpFOV");
        }

        if (!string.IsNullOrEmpty(plan.subtitles))
        {
            //dialogue text
            if (plan.timeBeforeVoice > 0)
            {
                Invoke("DisplayDialogue", plan.timeBeforeVoice);
            }else
            {
                DisplayDialogue();
            }
        }

        if (plan.targetTraveling != null)
        {
            StartCoroutine(Traveling(plan.cameraGizmo.transform, plan.targetTraveling.transform));
        }

        if (!string.IsNullOrEmpty(plan.animToPlay))
        {
            if (plan.animObject == null)
                playerAnimator.Play(plan.animToPlay);
                //playerAnimation.CrossFade(plan.animToPlay);
            else
                plan.animObject.GetComponent<sr_AnimationEventHandler>().TurnOn(plan.animToPlay);
                //plan.animObject.GetComponent<Animation>().CrossFade(plan.animToPlay);
        }
            
        //full duration
        Invoke("EndPlan", plan.duration);

    }

    public void DisplayDialogue ()
    {
        uiSubtitles.text = currentPlan.subtitles;
        Invoke("ClearSubtitles", (currentPlan.subtitles.ToCharArray().Length * 0.04f) + 1f);
    }
    public void ClearSubtitles ()
    {
        uiSubtitles.text = "";
    }

    public IEnumerator LerpFOV ()
    {
        float t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime / currentPlan.duration;
            cam.GetComponent<Camera>().fieldOfView = Mathf.Lerp(currentPlan.startFOV, currentPlan.targetFOV, t);
            yield return 0;
        }
    }

    public IEnumerator Traveling (Transform origin, Transform end)
    {
        float t = 0f;
        while (t <1)
        {
            t += Time.deltaTime / currentPlan.duration;
            cam.transform.position = Vector3.Lerp(origin.position, end.position, t);
            cam.transform.rotation = Quaternion.Lerp(origin.rotation, end.rotation, t);
            yield return 0;
        }
    }

    public void EndPlan ()
    {
        //camLight.intensity = 0f;

        planIndex += 1;
        if (planIndex >= currentScene.plans.Count)
        {
            ResumeGameplay();
        }else
        {
            ShowPlan();
        }
    }

}
