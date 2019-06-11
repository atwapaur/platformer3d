using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class sr_Player : MonoBehaviour {

    public float lifeblood = 20f;
    public float bloodSuckedPerSecond = 5f;

    public Color gaugeFeedColor1;
    public Color gaugeFeedColor2;
    public Color gaugeBloodDamageColor;
    [HideInInspector]
    public Color gaugeBloodBaseColor;

    [HideInInspector]
    public GameObject playerBody;
    [HideInInspector]
    public Animator myAnimator;
    private CharacterController charController;
    private Rigidbody rbody;
    private sr_Movement movement;
    private sr_CameraControl camControl;
    private sr_Trigger currentInsideTrigger = null;
    private sr_Main main;

    private bool isInputSick = false;

    private GameObject[] pointLights;
    private GameObject directionalLight;

    [HideInInspector]
    public float lightAtPlayerPos = 0f;

    private CollisionFlags aM_CollisionFlags;
    private Vector3 authoredMove_Dir;
    private float authoredMove_Speed;
    private float authoredMove_Time = 0f;

    private GameObject gaugeBloodBar;
    [HideInInspector]public sr_Prey currentPrey;

    private bool damageOverTimeIsRunning = false;
    private float currentPendingDamage = 0f;


    // Use this for initialization
    void Awake () {
        playerBody = GameObject.Find("PlayerBody");
        myAnimator = playerBody.GetComponent<Animator>();

        charController = GetComponent<CharacterController>();
        rbody = GetComponent<Rigidbody>();

        camControl = GameObject.Find("FirstCamera").GetComponent<sr_CameraControl>();
        movement = GetComponent<sr_Movement>();

        main = GameObject.Find("MAIN").GetComponent<sr_Main>();

        gaugeBloodBar = GameObject.Find("GaugeBlood");
    }
	

    void Start ()
    {
        pointLights = main.allLights;
        directionalLight = main.directionalLight;
        gaugeBloodBaseColor = gaugeBloodBar.GetComponent<Image>().color;
    }


	// Update is called once per frame
	void Update () {

        if (Input.GetButtonDown("ButtonY")) {
            if (currentInsideTrigger != null) {
                currentInsideTrigger.DoTrigger();
            }
            movement.Punch();
        }

        //Vampire attack
        if (Input.GetButtonDown("ButtonX")) {
            movement.VampireAttack(); //is dependent on srMovement being enabled because the leap movement is made therein in the FixedUpdate
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            Debug.Log("Key E");
            RaycastFront();

        }

        CalculateShadowConcealment();

        /*if (Input.GetKeyDown(KeyCode.X)) {
            Debug.Log("Delirium!");
            isInputSick = !isInputSick;
            myAnimator.SetBool("isGameplay", !isInputSick);
            myAnimator.SetBool("isSick", isInputSick);
        }*/

        GaugeBlood();
	}


    void FixedUpdate ()
    {
        //gravity
        //s_moveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
        //move
        if (authoredMove_Time > 0) {
            authoredMove_Time -= Time.fixedDeltaTime;
            aM_CollisionFlags = charController.Move(authoredMove_Dir * Time.fixedDeltaTime * authoredMove_Speed);
        }

        
    }


    private void GaugeBlood ()
    {
        //stamina bar
        float percentage = (lifeblood / 100f /*max blood is 100*/ ) * 100f;
        float barFill = (334f / 100f) * percentage; //180 is the gauge size hard-coded
        gaugeBloodBar.GetComponent<RectTransform>().sizeDelta = new Vector2(12, barFill);

        if (movement.isBloodSucking) {
            float t = Mathf.PingPong(Time.time, 1f);
            gaugeBloodBar.GetComponent<Image>().color = Color.Lerp(gaugeFeedColor1, gaugeFeedColor2, t);
        }
    }

    public void ResetGaugeBloodColor ()
    {
        gaugeBloodBar.GetComponent<Image>().color = gaugeBloodBaseColor;
    }


    public void DecreaseLifeblood (int dmg)
    {
        currentPendingDamage += (float)dmg;

        if (damageOverTimeIsRunning) {
            StopCoroutine("ApplyDamageOverTime");
        }
        StartCoroutine("ApplyDamageOverTime");
    }
    public IEnumerator ApplyDamageOverTime ()
    {
        damageOverTimeIsRunning = true;

        float totalPendingDamage = currentPendingDamage;
        float prevLife = lifeblood;
        float t = 0f;
        while (t < 1f) {
            t += Time.deltaTime;
            lifeblood = Mathf.Lerp(prevLife, prevLife - totalPendingDamage, t);
            currentPendingDamage = Mathf.Lerp(totalPendingDamage, 0f, t);
            gaugeBloodBar.GetComponent<Image>().color = Color.Lerp(gaugeBloodDamageColor, gaugeBloodBaseColor, t);
            yield return 0;
        }

        damageOverTimeIsRunning = false;
        currentPendingDamage = 0f;
    }

    private void CalculateShadowConcealment ()
    {
        //overlapshere lights ? lights dont have colliders? add one? or put a script in each light to check distance to player and send its influence on him?
        //Better to have all information in one place.

        //Collider[] hitColliders = Physics.OverlapSphere(center, radius);  > problem with this, the radius needs to be really big and the physics will register many colliders
        //even if only in the light layer.

        //Collider[] hitColliders = Physics.OverlapSphere(transform.position, 100f, main.layerLights.value);
        //for ()

        lightAtPlayerPos = 0f;

        //Point lights
        for (int i = 0; i < pointLights.Length; i++) {
            Transform lightTr = pointLights[i].transform;
            Light light = pointLights[i].GetComponent<Light>();
            RaycastHit hit;
            //if a ray from the light to the player within the light...
            if (Physics.Raycast(lightTr.position, (transform.position - lightTr.position).normalized, out hit, light.range)) {
                //... hits the player
                if (hit.transform.tag == "Player") {
                    //the player is lighted by it (and moreso the closer he is from the light source)
                    float luminance = light.color.r * 0.3f + light.color.g * 0.59f + light.color.b * 0.11f;
                    lightAtPlayerPos += (light.range - hit.distance) * light.intensity * luminance;
                    //lightAtPlayerPos += 1f; //For simple debug
                }
            }

            //if a ray from player to the light within its range hits no obstacle
            /*if (Physics.Raycast(transform.position, (light.transform.position - transform.position).normalized, light.range) == false) {
                //the player is lighted by it (and moreso the closer he is from the light source)
                //lightAtPlayerPos +=  (light.range - Vector3.Distance(transform.position, light.transform.position)/100f) * light.intensity;
                lightAtPlayerPos += 1;
            }*/
        }
        //Directional
        if (Physics.Raycast(transform.position, Vector3.up) == false) {
            lightAtPlayerPos += 100f; // arbitrary

        }

        //Debug.Log("Light at player position indice is: " + lightAtPlayerPos);

    }


    public void GetHurt (int dmg)
    {
        main.AllowPlayerMovement(false);
        main.AllowPlayerInputs(false);

        if (movement.isPunching) {
            movement.AbortPunching();
        }

        movement.isBloodSucking = false;
        movement.isCatchingPrey = false;

        DecreaseLifeblood(dmg);

        myAnimator.SetTrigger("getHurt");

        //StartCoroutine(PlayerGenericLerp(transform.position - transform.forward * 1, 2f));
        StartCoroutine(PlayerGenericLerp(transform.position - transform.forward * 3, 2f)); //TODO: change it for a controller.Move

        GameObject fxb = Instantiate(main.fxBloodBurst, transform.position, Quaternion.Euler(-70f, 0f, 10f)) as GameObject;
        Destroy(fxb, 1f);

        //main.AllowCameraControl(true);
    }



    //LERP SUCKS BECAUSE IT GOES THROUGH WALLS. REPLACE ALL LERP BY CHARCONTROLLER.MOVE !!!!
    private IEnumerator PlayerGenericLerp (Vector3 desti, float deltaSpeed)
    {
        Vector3 origin = transform.position;

        float t = 0f;
        while (t < 1) {
            t += Time.deltaTime * deltaSpeed;
            transform.position = Vector3.Lerp(origin, desti, t);
            yield return 0;
        }

        camControl.transform.rotation = Quaternion.Euler(0f, camControl.transform.eulerAngles.y, 0f);
        main.SwitchAllControls(true);
    }


    private IEnumerator PlayerRotSlerp(Quaternion targetRot)
    {
        Quaternion origin = transform.rotation;
        float t = 0.3f;
        while (t < 1f) {
            t += Time.deltaTime * 15f;
            transform.rotation = Quaternion.Slerp(origin, targetRot, t);
            yield return 0;
        }
    }

    private void RaycastFront ()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 3f)) {
            Debug.Log("Raycast hit something");
            if (hit.transform.tag == "Door") {
                hit.transform.parent.GetComponent<sr_Door>().Activate();
            }
        }
    }

    void OnTriggerEnter (Collider collider)
    {
        if (collider.GetComponent<sr_Trigger>()) {
            if (collider.GetComponent<sr_Trigger>().wantActionButton) {
                currentInsideTrigger = collider.GetComponent<sr_Trigger>();
            } else {
                collider.GetComponent<sr_Trigger>().DoTrigger();
            }
            
        }

        if (collider.tag == "Deathpit") {
            Debug.Log("Deathpit!");
            main.Deathpit();
        }
    }

    
    void OnTriggerStay (Collider collider)
    {
        if (collider.GetComponent<sr_Hitbox>() != null) {
            if (collider.GetComponent<sr_Hitbox>().canHitPlayer) {
                Debug.Log("PLAYER TAKES DAMAGE");
                GetHurt(6);

                //consume the hitbox for this atk
                collider.GetComponent<sr_Hitbox>().canHitPlayer = false;

                //give confidence to the atker
                collider.transform.parent.parent.GetComponent<sr_Prey>().ModifyFear(-40); //was transform.root, except the AIs are organized in a go folder in the scene hierarchy
            }
        }
    }


    void OnTriggerExit (Collider collider)
    {
        if (collider.GetComponent<sr_Trigger>()) {
            collider.GetComponent<sr_Trigger>().LeaveTrigger();

            if (collider.GetComponent<sr_Trigger>() == currentInsideTrigger) {
                currentInsideTrigger = null;
            }
        }
    }

    public void DeathByAnchorWorm (Transform wormHead)
    {
        movement.enabled = false;
        camControl.enabled = false;

        transform.SetParent(wormHead);

        StartCoroutine("CameraSmoothTurnAt", wormHead.parent);
        //camControl.transform.LookAt(wormHead.parent);

    }

    public IEnumerator CameraSmoothTurnAt (Transform target)
    {
        Quaternion baseRot = camControl.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(target.position - camControl.transform.position);

        float t = 0f;
        while (t<1) {
            t += Time.deltaTime * 1.2f;
            camControl.transform.rotation = Quaternion.Lerp(baseRot, targetRot, t);
            yield return 0;
        }
    }

}
