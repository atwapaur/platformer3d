using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.FirstPerson;

public class sr_Main : MonoBehaviour {

    public Transform teleportPlayerToStartPoint;
    public bool playIntro;
    public sr_Order introSequence;

    public GameObject fxBloodBurst;

    public LayerMask layerGround;
    public LayerMask layerObstacle;
    public LayerMask layerVines;
    public LayerMask layerLights;

    public GameObject uiRootObject;

    //REFS
    [HideInInspector]public GameObject player;
    [HideInInspector]
    public sr_Player srPlayer;
    [HideInInspector]
    public sr_Movement playerMovement;
    [HideInInspector]public GameObject cam;
    [HideInInspector]public GameObject playerBody;
    [HideInInspector]public Animator playerAnimator;
    [HideInInspector]
    public GameObject[] allLights;
    [HideInInspector]
    public GameObject directionalLight;
    [HideInInspector]
    public sr_CameraControl camControl;

    private Vector3 camLastGameplayPos;
    private Quaternion camLastGameplayRot;


    void Awake()
    {
        player = GameObject.Find("Player");
        srPlayer = player.GetComponent<sr_Player>();
        playerMovement = player.GetComponent<sr_Movement>();
        cam = GameObject.Find("FirstCamera");
        playerBody = GameObject.Find("PlayerBody");
        playerAnimator = playerBody.GetComponent<Animator>();
        camControl = cam.GetComponent<sr_CameraControl>();

        directionalLight = GameObject.Find("Directional light");
        allLights = GameObject.FindGameObjectsWithTag("Light");

        uiRootObject.SetActive(true);
    }

    void Start()
    {
        CleanGizmos();

        if (teleportPlayerToStartPoint != null) {
            player.transform.position = teleportPlayerToStartPoint.position;
            player.transform.rotation = teleportPlayerToStartPoint.rotation;
        }

        if (introSequence != null && playIntro) {
            introSequence.ExecuteOrder();
        }
    }


    public void AllowPlayerMovement (bool target)
    {
        playerMovement.enabled = target;
    }

    public void AllowPlayerInputs (bool target)
    {
        srPlayer.enabled = target;
    }

    public void AllowCameraControl (bool target)
    {
        camControl.enabled = target;
    }

    public void SwitchAllControls (bool target)
    {
        playerMovement.enabled = target;
        srPlayer.enabled = target;
        camControl.enabled = target;
    }

    public void SwitchToCutscene ()
    {
        SwitchAllControls(false);

        //HACK FOR CUTSCENE TRIGGER TRANSITION
        playerAnimator.ResetTrigger("doLanding"); 

        playerAnimator.SetBool("isGameplay", false);
        playerAnimator.SetTrigger("switchCutscene");

        camLastGameplayPos = cam.transform.position;
        camLastGameplayRot = cam.transform.rotation;
    }

    public void ResumeGameplay (bool restoreLastGameplayRotation)
    {
        if (restoreLastGameplayRotation) {
            cam.transform.position = camLastGameplayPos;
            cam.transform.rotation = camLastGameplayRot;
        }

        cam.GetComponent<Camera>().fieldOfView = 70f;

        //force the player to the ground
        playerMovement.forgetNextLandingSound = true;
        player.GetComponent<CharacterController>().Move(-Vector3.up * 10f);

        //Give control back to player after cutscene
        SwitchAllControls(true);

        playerAnimator.SetBool("isGameplay", true);
        playerAnimator.SetTrigger("switchGameplay");
    }


    public void Deathpit ()
    {
        AllowCameraControl(false);
        AllowPlayerInputs(false);

        Invoke("Respawn", 1.25f);
    }

    public void Respawn ()
    {
        if (teleportPlayerToStartPoint != null) {
            player.transform.position = teleportPlayerToStartPoint.position;
            player.transform.rotation = teleportPlayerToStartPoint.rotation;
        }

        SwitchAllControls(true);
    }


    public void PredatorAttackPlayer (Transform preda)
    {
        AllowPlayerMovement(false);
        AllowCameraControl(false);

        //slow motion
        Time.timeScale = 0.1f;

        //Player turns towards predator quickly
        Quaternion lookAtRot = Quaternion.LookRotation((preda.position - player.transform.position), Vector3.up);
        lookAtRot = Quaternion.Euler(0, lookAtRot.eulerAngles.y, 0);
        srPlayer.StartCoroutine("PlayerRotSlerp", lookAtRot);

        //Surprise/ready-up animation
        srPlayer.myAnimator.SetTrigger("getSurprised");

        //Position the camera
        Vector3 predaToPlayerDir = (player.transform.position - preda.position).normalized;
        Vector3 horizVector = new Vector3(predaToPlayerDir.x, 0f, predaToPlayerDir.z);
        Vector3 sideVector = Vector3.Cross(horizVector, Vector3.up);
        Vector3 camPos = player.transform.position + horizVector.normalized * 4f + sideVector * 1.5f + Vector3.up * 0.5f;
        cam.transform.position = camPos;

        //Rotate the camera
        Quaternion rot = Quaternion.LookRotation(preda.position - cam.transform.position);
        cam.transform.rotation = Quaternion.Euler(0f, rot.eulerAngles.y, 10f);

        //interrupt some player actions in case they were being performed
        playerMovement.isCatchingPrey = false;
        playerMovement.isBloodSucking = false;

    }


    //Get damage from predator attack here or just reactivate time scale to 1 ?
    public void FailPredatorQTE()
    {
        //cam.transform.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);

        //Time.timeScale = 1f;
    }



    private void CleanGizmos()
    {
        GameObject[] gizs = GameObject.FindGameObjectsWithTag("Gizmo");
        foreach (GameObject g in gizs) {
            g.GetComponent<Renderer>().enabled = false;
        }
    }



}
