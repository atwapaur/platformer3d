using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Random = UnityEngine.Random;

public class sr_Movement : MonoBehaviour
{
    [SerializeField] private float m_speedCrawl;
    [SerializeField] private float m_speedGround;
    [SerializeField] private float m_speedAir;
    [SerializeField] private float m_JumpSpeed;
    [SerializeField] private float m_JumpExtraPerFrame;
    [SerializeField] private float m_AirborneExtraJumpDuration;
    [SerializeField] private float m_LedgeClimbImpulse;
    [SerializeField] private float m_CrouchJumpImpulse;
    [SerializeField] private float vampireAttackImpulseY; 
    [SerializeField] private float vampireAttackForwardSpeed;
    [SerializeField] private float m_StickToGroundForce;
    [SerializeField] private float m_GravityMultiplier;
    [SerializeField] private float m_FlyingGravityMultiplier;
    [SerializeField] private float m_StepInterval;
    [SerializeField] private AudioClip[] m_FootstepSounds;
    [SerializeField] private AudioClip m_JumpSound;
    [SerializeField] private AudioClip m_LandSound;

    [HideInInspector] public bool justMadeNoise = false;
    private float clearNoiseTimer = 0f;
    private CollisionFlags m_CollisionFlags;
    private bool m_Jump;
    [HideInInspector] public Vector2 m_Input;
    [HideInInspector] public Vector3 m_MoveDir = Vector3.zero;
    private bool m_PreviouslyGrounded;
    private float m_StepCycle;
    private float m_NextStep;
    [HideInInspector] public bool m_Jumping;
    [HideInInspector] public bool m_Airborne;
    private bool m_GrabbingLedge;
    private bool m_LedgeRelease;
    private float m_LedgeReleaseCooldown = 0.25f;
    private bool m_ButtonB;
    private bool m_ButtonR1;
    private float m_jumpInputCacheLength = 0.1f; //0.25
    private float jumpInputCache = 0f;
    private bool m_JumpRaw;
    private float airborneTimer = 0f;
    private bool m_continuousJumpInput;
    private bool m_flyInput;
    private bool m_Flying;
    private bool m_Crawling;
    private bool m_CrouchJumping;

    private bool isOnMovingPlatform = false;

    private bool m_FlyRelease;
    private float m_FlyReleaseCooldown = 0.5f;

    [HideInInspector] public bool m_GrabbingVines = false;
    private bool m_VinesRelease;
    private float m_VinesReleaseCooldown = 0.25f;

    private bool continuousLedge = false;
    private Vector3 ledge_PreviousVolumeNormal;

    private Transform currentVines = null;

    private sr_Main main;
    private Camera cam;
    private sr_Player player;
    private AudioSource audio2D;
    private GameObject playerBody;
    private Animator myAnimator;
    private CharacterController characterController;

    [HideInInspector] public Vector3 vinesNormal = Vector3.zero;

    private Vector3 vinesLastValidPosition;
    private Vector3 vinesLastNormal = Vector3.zero;
    private List<RaycastHit> vinesSuccessHits = new List<RaycastHit>();
    private float vinesDetectRaycastDist = 8f;

    [HideInInspector]
    public bool isAtking = false;
    [HideInInspector]
    public bool isCatchingPrey = false;
    [HideInInspector]
    public bool isBloodSucking = false;
    private int dashCallback = 0;
    private Vector3 vampireAtk_MoveDir = Vector3.zero;
    private float atkTimer = 0f;
    private float bloodSuckingTimer = 0f;

    [HideInInspector]
    public bool forgetNextLandingSound = false;

    private int punchCounter = 0;
    [HideInInspector]
    public bool isPunching = false;
    private float punchTimer = 0f;
    private bool comboInputPending = false;
    private bool punchingLockRot = false;
    private bool punchingBlow = false;

    public sr_Hitbox hitboxLeftHand;
    public sr_Hitbox hitboxRightHand;



    private void Start()
    {
        main = GameObject.Find("MAIN").GetComponent<sr_Main>();
        characterController = GetComponent<CharacterController>();
        cam = GameObject.Find("FirstCamera").GetComponent<Camera>();
        player = GetComponent<sr_Player>();
        //hitboxLeftHand = GameObject.Find("Hitbox_LeftHand").GetComponent<sr_Hitbox>();
        //hitboxRightHand = GameObject.Find("Hitbox_RightHand").GetComponent<sr_Hitbox>();

        if (hitboxLeftHand == null) {
            Debug.Log("NULL HITBOX");
        }

        m_StepCycle = 0f;
        m_NextStep = m_StepCycle/2f;

        m_Jumping = false;
        m_Airborne = false;
        m_GrabbingLedge = false;
        m_LedgeRelease = false;
        m_continuousJumpInput = false;
        m_Crawling = false;
        m_CrouchJumping = false;
        m_FlyRelease = false;
        m_VinesRelease = false;

        audio2D = GameObject.Find("Audio2D").GetComponent<AudioSource>();

        playerBody = GameObject.Find("PlayerBody");
        myAnimator = playerBody.GetComponent<Animator>();
        myAnimator.SetBool("isGameplay", true);
    }



    private void Update()
    {
        if (isAtking) {
            atkTimer += Time.deltaTime;
            if (!characterController.isGrounded || atkTimer < 0.1f) {
                return;
            }

            //atk ends when landing... if no prey caught by the attack
            PlayLandingSound();
            m_MoveDir.y = 0f;
            isAtking = false;
            myAnimator.SetBool("isGrounded", true);
            myAnimator.SetTrigger("doLanding");
        }

        if (isPunching) {
            PunchUpdate();
        }

        if (isCatchingPrey) {
            if (Input.GetButton("ButtonX")) {
                if (!isBloodSucking) {
                    //AnimatorStateInfo currentState = myAnimator.GetCurrentAnimatorStateInfo(0);
                    //If we are in the Wait for blood Animator state
                    if (myAnimator.GetCurrentAnimatorStateInfo(0).IsName("Gameplay.WaitForBlood")) {
                        //start blood sucking
                        isBloodSucking = true;
                        myAnimator.SetTrigger("doBloodSucking");
                        bloodSuckingTimer = 0f;
                    }
                }
                else {
                    float bloodSuckedThisFrame = Time.deltaTime * player.bloodSuckedPerSecond;
                    player.currentPrey.blood -= bloodSuckedThisFrame;
                    player.lifeblood += bloodSuckedThisFrame;


                    //bloodSuckingTimer += Time.deltaTime;
                    //Release blood when all blood has been consumed on the prey
                    if (player.currentPrey.blood <= 0) {
                    //if (bloodSuckingTimer > 6f) {
                        isBloodSucking = false;
                        isCatchingPrey = false;
                        myAnimator.SetTrigger("doReleaseBlood");
                        player.ResetGaugeBloodColor();
                    }
                }
            }else {
                //Release blood by releasing the button
                if (isBloodSucking) {
                    isBloodSucking = false;
                    isCatchingPrey = false;
                    myAnimator.SetTrigger("doReleaseBlood");
                    player.ResetGaugeBloodColor();
                }
            }
            
            if (!isBloodSucking && myAnimator.GetCurrentAnimatorStateInfo(0).IsName("Gameplay.WaitForBlood") ) {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                Vector2 moveInput = new Vector2(horizontal, vertical);
                if (moveInput.magnitude > 0.1f) {
                    //Moving away from the predatory 'Wait for blood' state
                    isCatchingPrey = false;
                    myAnimator.SetTrigger("doReleaseBlood");
                }
            }
            

            return;
        }

        //Jump input is read
        if (!m_Jump) {
            m_Jump = Input.GetButtonDown("Jump");
            jumpInputCache = 0f;
        }else {
            jumpInputCache += Time.deltaTime;
            if (jumpInputCache > m_jumpInputCacheLength) {
                m_Jump = false;
            }
        }
        m_JumpRaw = Input.GetButton("Jump");

        //Crouch
        m_ButtonR1 = Input.GetButton("ButtonR1");

        //Fall of
        m_ButtonB = Input.GetButtonDown("ButtonB");

        //Landing code was there...
        //landing
        if (!m_PreviouslyGrounded && characterController.isGrounded && !m_GrabbingVines) {
            PlayLandingSound();
            m_MoveDir.y = 0f;
            m_Jumping = false;
            SetAirborne(false);
            m_Flying = false;
            m_CrouchJumping = false;

            myAnimator.ResetTrigger("doJump");
            myAnimator.SetBool("isGrounded", true);
            myAnimator.SetTrigger("doLanding");
            //StopCoroutine("AnimatorResetTriggerHack");
            //StartCoroutine("AnimatorResetTriggerHack", "doLanding"); //HACK
            myAnimator.ResetTrigger("doReleaseJump");
        }

        //falling
        if (!characterController.isGrounded && !m_Jumping && m_PreviouslyGrounded) {
            m_MoveDir.y = 0f;
            SetAirborne(true);
            StandUp(); m_CrouchJumping = false;
            myAnimator.SetBool("isGrounded", false);
        }

        m_PreviouslyGrounded = characterController.isGrounded;

        if (m_Airborne && !m_GrabbingLedge && !m_LedgeRelease) {
            CheckLedgeGrabbing();
        }

        CheckClearNoise();


        //DEBUG RAYCAST LINES AT ALL FRAMES
        //CheckLedgeGrabbing();
    }




    private void CheckLedgeGrabbing ()
    {
        RaycastHit grabHit;
        RaycastHit volumeHit;
        Vector3 grabPos = transform.position + Vector3.up * 2.5f;  //* 2.7f; //2.5
        Vector3 volumePos = transform.position + Vector3.up * 2.2f;  //* 2.2f;
        bool isgrabHit = false;
        bool isvolumeHit = false;
        bool isVolumeAbove = false;

        float distanceToLedge = 0f;

        if (Physics.Raycast(grabPos, transform.forward, out grabHit, 1f)) {
            //if (Physics.SphereCast(grabPos, 0.5f, transform.forward, out headHit, 1f)) {
            isgrabHit = true;
        }

        if (Physics.Raycast(volumePos, transform.forward, out volumeHit, 1f)) {
            //if (Physics.SphereCast(volumePos, 0.5f, transform.forward, out torsoHit, 1f)) {
            isvolumeHit = true;
            distanceToLedge = volumeHit.distance-0.5f;
        }

        if (Physics.Raycast(volumePos, Vector3.up, 0.4f)) {
            isVolumeAbove = true;
        }


        if (isgrabHit == false && isvolumeHit == true && isVolumeAbove == false) {

            if (volumeHit.normal.y > 0.5f || volumeHit.normal.y < -0.5f) {
                Debug.Log("Ledge Raycast found a glitchy normal. Too much vertical. Abort ledge grabbing.");
                return;
            }

            Debug.Log("LEDGE TO GRAB");
            m_GrabbingLedge = true; //will stop gravity falling in fixedUpdate
            SetAirborne(false); //!!!!!!!!!!!!!!!!!!! NEw addition, does it break anything? !!!!!!!!!!!!!!?????????????????????????????????
            m_Flying = false;
            StandUp(); m_CrouchJumping = false;
            //m_Jumping = false;
            myAnimator.SetTrigger("doGrabLedge");

            //face ledge angle
            //transform.rotation = Quaternion.LookRotation(-volumeHit.normal, Vector3.up);
            transform.rotation = Quaternion.LookRotation(-volumeHit.normal, volumeHit.transform.up); //is that for moving platforms???? PROBABLY! MAKE AN if STATEMENT

            //stick to ledge
            transform.position = transform.position + transform.forward * distanceToLedge;
            //transform.position = volumeHit.point + volumeHit.normal * 0.3f;

            

            //Is The ledge on a Moving platform?
            if (volumeHit.transform.tag == "MovingPlatform") {
                transform.SetParent(volumeHit.transform); //.parent);
                isOnMovingPlatform = true;
            }

            ledge_PreviousVolumeNormal = volumeHit.normal;
        }

        //Debug.DrawLine(volumePos, grabPos + Vector3.up * 0.1f, Color.magenta);
        //Debug.DrawLine(grabPos, grabPos + transform.forward * 1.0f, Color.yellow);
        //Debug.DrawLine(volumePos, volumePos + transform.forward * 1.0f, Color.green);
    }

    private void CheckLedgeMove(Vector3 playerPos, Vector3 slideDir)
    {
        CheckLedgeMove(playerPos, slideDir, false);
    }

    private void CheckLedgeMove (Vector3 playerPos, Vector3 slideDir, bool checkAngle90)
    {
        Vector3 desiredPos = playerPos + slideDir * 0.06f;
        Vector3 grabPos = desiredPos + Vector3.up * 2.5f;
        Vector3 volumePos = desiredPos + Vector3.up * 2.2f;

        RaycastHit grabHit;
        RaycastHit volumeHit;
        bool isgrabHit = false;
        bool isvolumeHit = false;
        bool isVolumeAbove = false;
        bool isVolumeOnSide = false;
        float distanceToLedge = 0f;
        Vector3 forwardDir = transform.forward;

        if (checkAngle90) {
            Debug.Log("Check angle at 90");
            forwardDir = -slideDir;

            //Debug.DrawLine(grabPos, grabPos + forwardDir * 1.0f, Color.yellow);
            //Debug.DrawLine(volumePos, volumePos + forwardDir * 1.0f, Color.green);
        }

        //Raycasts
        if (Physics.Raycast(grabPos, forwardDir, out grabHit, 1f)) {
            isgrabHit = true;
        }
        if (Physics.Raycast(volumePos, forwardDir, out volumeHit, 1f)) {
            isvolumeHit = true;
            distanceToLedge = volumeHit.distance - 0.5f;
        }
        if (Physics.Raycast(volumePos, Vector3.up, 0.4f)) {
            isVolumeAbove = true;
        }
        if (slideDir != Vector3.zero /*null*/ && !checkAngle90) { //WARNING: check Console. Probably not working as is.
            if (Physics.Raycast(volumePos, slideDir, 0.45f)) {
                isVolumeOnSide = true;
            }
        }

        //Valid ledge to move along
        if (isgrabHit == false && isvolumeHit == true && isVolumeAbove == false && isVolumeOnSide == false) {
            //face ledge angle  
            transform.rotation = Quaternion.LookRotation(-volumeHit.normal, volumeHit.transform.up); //Vector3.up for straight hanging. In that case, don't parent to platform, use a per-frame catch-up method

            //stick to ledge
            transform.position = desiredPos + transform.forward * distanceToLedge;

            if (!checkAngle90) {
                //check if we just crossed an angle (even the smallest)
                if (volumeHit.normal != ledge_PreviousVolumeNormal) {
                    transform.position += slideDir * 0.3f;
                }
                ledge_PreviousVolumeNormal = volumeHit.normal;
            }
        } else {
            if (!checkAngle90) {
                //Can't move in this ledge direction, but can we make a 90° angle turn?
                Vector3 playerPosAtNewAngle = playerPos + slideDir * 0.1f + transform.forward * 0.5f;

                CheckLedgeMove(playerPosAtNewAngle, slideDir, true);
            }

        }
    }


    private void GetInput(out float speed)
    {
        // Read movement input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        //the following to not go faster when going diagonally
        m_Input = new Vector2(horizontal, vertical);
        if (m_Input.sqrMagnitude > 1) {
            m_Input.Normalize();
        }

        // set the desired speed whether on ground or in the air
        //jump: goes fast to speedair
        //fall/airborne: goes slowly to speedair
        speed = m_Airborne ? m_speedAir : m_speedGround;

        /*if (!m_Airborne) {
            speed = m_speedGround;
        }else {
            if (m_Jumping) {
                speed = m_speedAir;
            }else {
                speed = Mathf.Lerp()
            }
        }*/
    }


    private void FixedUpdate()
    {

        if (isOnMovingPlatform && characterController.isGrounded == false && m_GrabbingLedge == false) {
            isOnMovingPlatform = false;
            transform.SetParent(null);
            //Debug.Log("unparented from moving platform");
        }

        float speed;
        GetInput(out speed);
        Vector3 desiredMove = Vector3.zero;

        // Create a new vector of the horizontal and vertical inputs.
        Vector3 inputVector = new Vector3(m_Input.x, 0f, m_Input.y);


        //Wall vines movement
        if (m_GrabbingVines) {

            //Change the input vector to be on the X Y axes
            inputVector = new Vector3(m_Input.x, m_Input.y, 0f);

            if (inputVector.magnitude <= 0.1f) {
                myAnimator.SetBool("isMoving", false);
            }
            else {
                //Make the input relative to the player local coordinates
                Vector3 headDirection = transform.TransformDirection(inputVector);
                headDirection.Normalize();

                //Do dirty local rotation (so that the raycasts are properly oriented)
                transform.rotation = Quaternion.LookRotation(transform.forward, headDirection);

                //Raycasts to detect the vines ground
                float bodyOffset = 0.4f;
                bool hasHitFrontR = false;
                bool hasHitRearR = false;
                bool hasHitFrontL = false;
                bool hasHitRearL = false;
                Vector3 rescueNormal = Vector3.zero;

                RaycastHit frontHitR;
                if (Physics.Raycast(transform.position + transform.up * bodyOffset + transform.right * bodyOffset, transform.forward, out frontHitR, vinesDetectRaycastDist, main.layerVines)) { //headDirection replaces transform.up
                    hasHitFrontR = true;
                    rescueNormal = frontHitR.normal;
                }
                RaycastHit rearHitR;
                if (Physics.Raycast(transform.position - transform.up * bodyOffset + transform.right * bodyOffset, transform.forward, out rearHitR, vinesDetectRaycastDist, main.layerVines)) {
                    hasHitRearR = true;
                    rescueNormal = rearHitR.normal;
                }
                RaycastHit frontHitL;
                if (Physics.Raycast(transform.position + transform.up * bodyOffset - transform.right * bodyOffset, transform.forward, out frontHitL, vinesDetectRaycastDist, main.layerVines)) { //headDirection replaces transform.up
                    hasHitFrontL = true;
                    rescueNormal = frontHitL.normal;
                }
                RaycastHit rearHitL;
                if (Physics.Raycast(transform.position - transform.up * bodyOffset - transform.right * bodyOffset, transform.forward, out rearHitL, vinesDetectRaycastDist, main.layerVines)) {
                    hasHitRearL = true;
                    rescueNormal = rearHitL.normal;
                }

                //If all the rays hit a vines, update the surface normal
                if (hasHitFrontR && hasHitRearR && hasHitFrontL && hasHitRearL) {
                    //Find the average surface normal
                    Vector3 averageNormal = (frontHitR.normal + frontHitL.normal + rearHitR.normal + rearHitL.normal) / 4;
                    Vector3 averagePoint = (frontHitR.point + frontHitL.point + rearHitR.point + rearHitL.point) / 4;
                    Debug.DrawLine(averagePoint, averagePoint + averageNormal * 6f, Color.cyan);
                    vinesNormal = averageNormal;

                    //stick to the surface
                    transform.position = averagePoint - transform.forward * 0.7f;
                }
                else if (vinesNormal == Vector3.zero) {
                    vinesNormal = rescueNormal;
                }

                //Rotate the player body in the head direction
                transform.rotation = Quaternion.LookRotation(-vinesNormal);
                headDirection = transform.TransformDirection(inputVector); //necessary to re-assign it now
                headDirection.Normalize();
                Vector3 planeNormal = vinesNormal.normalized;
                transform.rotation = Quaternion.LookRotation(-planeNormal, headDirection);

                Debug.DrawLine(transform.position, transform.position + headDirection * 3f, Color.yellow);

                //Vector Projection code
                //Vector3 headDirection = Camera.main.transform.TransformDirection(inputVector);
                //float distance = -Vector3.Dot(planeNormal.normalized, headDirection);
                //Vector3 projectedHeadDir = headDirection + planeNormal * distance;

                //transform.position = averagePoint - transform.forward * 0.75f; //distanceToSurface

                //Is there some vines to walk in front of the player head?
                if (Physics.Raycast(transform.position + transform.up * bodyOffset * 2 + transform.right * bodyOffset, transform.forward, vinesDetectRaycastDist, main.layerVines)
                    && Physics.Raycast(transform.position + transform.up * bodyOffset * 2 - transform.right * bodyOffset, transform.forward, vinesDetectRaycastDist, main.layerVines) ) {

                    //makes the character actually move
                    desiredMove = transform.up * 1f;
                    myAnimator.SetBool("isMoving", true);
                }

                Debug.DrawLine(transform.position + transform.up * bodyOffset + transform.right * bodyOffset, transform.position + transform.up * bodyOffset + transform.right * bodyOffset + transform.forward * 10f, Color.green);
                Debug.DrawLine(transform.position - transform.up * bodyOffset + transform.right * bodyOffset, transform.position - transform.up * bodyOffset + transform.right * bodyOffset + transform.forward * 10f, Color.magenta);
                Debug.DrawLine(transform.position + transform.up * bodyOffset - transform.right * bodyOffset, transform.position + transform.up * bodyOffset - transform.right * bodyOffset + transform.forward * 10f, Color.green);
                Debug.DrawLine(transform.position - transform.up * bodyOffset - transform.right * bodyOffset, transform.position - transform.up * bodyOffset - transform.right * bodyOffset + transform.forward * 10f, Color.magenta);
            }





            /*
            if (inputVector.magnitude <= 0.1f) {
                myAnimator.SetBool("isMoving", false);
            } else {
                //Make this vector relative to the player local coordinates
                Vector3 headDirection = transform.TransformDirection(inputVector);
                headDirection.Normalize();

                //Raycasts to detect the vines ground
                Vector3 rightDir = Vector3.Cross(headDirection, transform.forward);
                vinesSuccessHits.Clear();
                float bodyOffset = 0.4f;

                RaycastHit frontHitR;
                if (Physics.Raycast(transform.position + headDirection * bodyOffset + rightDir * bodyOffset, transform.forward, out frontHitR, 10f, main.layerVines)) { //headDirection replaces transform.up
                    vinesSuccessHits.Add(frontHitR);
                }
                RaycastHit rearHitR;
                if (Physics.Raycast(transform.position - headDirection * bodyOffset + rightDir * bodyOffset, transform.forward, out rearHitR, 10f, main.layerVines)) {
                    vinesSuccessHits.Add(rearHitR);
                }
                RaycastHit frontHitL;
                if (Physics.Raycast(transform.position + headDirection * bodyOffset - rightDir * bodyOffset, transform.forward, out frontHitL, 10f, main.layerVines)) { //headDirection replaces transform.up
                    vinesSuccessHits.Add(frontHitL);
                }
                RaycastHit rearHitL;
                if (Physics.Raycast(transform.position - headDirection * bodyOffset - rightDir * bodyOffset, transform.forward, out rearHitL, 10f, main.layerVines)) {
                    vinesSuccessHits.Add(rearHitL);
                }

                //does the raycasts in front of the body found some vines to walk towards?
                if (vinesSuccessHits.Contains(frontHitR) && vinesSuccessHits.Contains(frontHitL)) {
                    //Create the average surface normal
                    Vector3 averageNormal = Vector3.zero;
                    for (int i = 0; i < vinesSuccessHits.Count; i++) {
                        averageNormal += vinesSuccessHits[i].normal;
                    }
                    averageNormal /= vinesSuccessHits.Count;

                    //Rotate the player body, facing the surface (normal reversed) and towards the inputed headDirection
                    transform.rotation = Quaternion.LookRotation(-averageNormal);
                    Vector3 planeNormal = averageNormal.normalized;
                    transform.rotation = Quaternion.LookRotation(-planeNormal, headDirection);

                    //makes the character actually move
                    desiredMove = transform.up * 1f;
                    myAnimator.SetBool("isMoving", true);
                }

            }
            */




        } else {

            //NORMAL ROT + FWD MOVE
            if (inputVector.magnitude > 0.1f) {
                Vector3 targetDirection = Camera.main.transform.TransformDirection(inputVector);
                targetDirection = new Vector3(targetDirection.x, 0f, targetDirection.z);
                targetDirection.Normalize();

                // Create a rotation based on this new vector assuming that up is the global y axis.
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

                // Create a rotation that is an increment closer to the target rotation from the player's rotation.
                //Quaternion newRotation = Quaternion.Lerp(transform.rotation, targetRotation, 8f * Time.deltaTime);

                // Change the players rotation to this new rotation.
                if (!m_GrabbingLedge && isAtking == false && isCatchingPrey == false && !punchingBlow) { //punchingLockRot
                    transform.rotation = targetRotation;
                }

                if (punchingBlow) {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 2);
                }

                //rigidbody.MoveRotation(newRotation);
            }

            // always move along the player forward as it is the direction we have just rotated the character towards
            if (inputVector.magnitude > 0.1f) {
                desiredMove = transform.forward * 1f;
                myAnimator.SetBool("isMoving", true);
            } else {
                myAnimator.SetBool("isMoving", false);
            }

        }



        if (characterController.isGrounded)
        {
            m_MoveDir.y = -m_StickToGroundForce;

            //Take Off
            if (m_Jump)
            {
                m_MoveDir.y = m_JumpSpeed;

                if (m_Crawling) {
                    m_MoveDir.y += m_CrouchJumpImpulse;
                    m_CrouchJumping = true;
                    StandUp();
                }

                PlayJumpSound();
                m_Jump = false;
                m_Jumping = true;
                SetAirborne(true);
                airborneTimer = 0f;
                m_continuousJumpInput = true;

                myAnimator.ResetTrigger("doReleaseJump");
                myAnimator.SetTrigger("doJump");
                myAnimator.SetBool("isGrounded", false);
                //myAnimator.SetBool("isCrawling", false);
            }
            else {
                //Crouch & crawl
                if (m_ButtonR1) {
                    if (!m_Crawling) {
                        Crawl();
                    }

                } else {
                    if (m_Crawling) {
                        StandUp();
                    }
                }
            }

        }
        else
        {
            //Airborne
            if (!m_GrabbingLedge && !m_GrabbingVines) {
            //if (!m_GrabbingLedge) {

                airborneTimer += Time.fixedDeltaTime;

                if (m_Jumping) {

                    if (m_continuousJumpInput) {
                        if (m_JumpRaw) {
                            if (airborneTimer <= m_AirborneExtraJumpDuration) {
                                //extra jump height
                                m_MoveDir += Vector3.up * m_JumpExtraPerFrame;
                            }else {
                                m_continuousJumpInput = false;
                                myAnimator.SetTrigger("doReleaseJump");
                            }
                        }else {
                            m_continuousJumpInput = false;
                            myAnimator.SetTrigger("doReleaseJump");
                        }
                    }

                    //Jumping gravity (same as falling gravity in fact)
                    m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
                }


                //Flying conditions
                if (!m_Flying) {
                    //Start Flying airborne
                    if (m_Jump && !m_FlyRelease) {
                        Debug.Log("Flying!");
                        m_Jumping = false; //first jump quality consumed. now we will be either flying or falling
                        m_Flying = true;
                        m_continuousJumpInput = true;
                        myAnimator.SetTrigger("doFly");

                        m_MoveDir.y = 0f;
                    }

                    //Falling gravity
                    if (!m_Jumping) {
                        m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
                    }

                }else {
                    //Stop
                    if (!m_JumpRaw) {
                        m_Flying = false;
                        m_continuousJumpInput = false;
                        myAnimator.SetTrigger("doReleaseJump");

                        m_MoveDir.y = -10f;
                        m_FlyRelease = true;
                        Invoke("FlyReleaseComplete", m_FlyReleaseCooldown);
                    }

                    //Flying gravity
                    m_MoveDir += Physics.gravity * m_FlyingGravityMultiplier * Time.fixedDeltaTime;
                }

            }
            
        }


        //Apply speed. desiredMove * speed makes for the m_MoveDir vector used in the Controller.Move call
        if (m_Crawling || m_CrouchJumping) {
            m_MoveDir.x = desiredMove.x * m_speedCrawl;
            m_MoveDir.z = desiredMove.z * m_speedCrawl;
        } else if (m_GrabbingVines) {
            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.y = desiredMove.y * speed;
            m_MoveDir.z = desiredMove.z * speed;
        } else {
            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;
        }

        myAnimator.SetFloat("moveSpeed", desiredMove.magnitude);
        //Debug.Log("Speed = " + speed);


        if (m_GrabbingLedge) {
            //m_MoveDir.y = 0f;
            m_MoveDir = Vector3.zero;

            //move along edge
            if (inputVector.magnitude > 0.5f) {
                if (inputVector.x > 0) {
                    //right
                    CheckLedgeMove(transform.position, transform.right);
                }else {
                    //left
                    CheckLedgeMove(transform.position, -transform.right); //* 0.03f);
                }
            }

            //Climb Up
            if (m_Jump) {
                m_MoveDir.y = m_LedgeClimbImpulse;
                PlayJumpSound();
                m_Jump = false;
                m_Jumping = true;
                SetAirborne(true);
                //continuousJumpInput to allow extra jump ?

                m_GrabbingLedge = false;
                m_LedgeRelease = true;
                Invoke("LedgeReleaseComplete", m_LedgeReleaseCooldown);

                myAnimator.SetBool("isGrounded", false);
                myAnimator.SetTrigger("doClimbUp");
            }

            //fall off
            if (m_ButtonB) {
                SetAirborne(true);
                m_GrabbingLedge = false;

                m_LedgeRelease = true;
                Invoke("LedgeReleaseComplete", m_LedgeReleaseCooldown);

                myAnimator.SetBool("isGrounded", false);
                myAnimator.SetTrigger("doFallOff");
            }
        }

        //Specific on vines control
        if (m_GrabbingVines) {
            //Jump off
            if (m_Jump) {
                m_MoveDir.y = m_LedgeClimbImpulse;
                PlayJumpSound();
                m_Jump = false;
                m_Jumping = true;
                
                ReleaseVines();

                //myAnimator.SetBool("isGrounded", false);
                myAnimator.SetTrigger("doClimbUp");
            }

            //Fall off
            if (m_ButtonB) {
                ReleaseVines();

                //myAnimator.SetBool("isGrounded", false);
                myAnimator.SetTrigger("doFallOff");
            }
        }


        if (!isAtking) {
            if (!isCatchingPrey && !punchingBlow) {

                //ACTUAL MOVEMENT sent to the character controller
                m_CollisionFlags = characterController.Move(m_MoveDir * Time.fixedDeltaTime);

                if (!m_Crawling) { //less binary, make it so that the sound is faint and only heard by very close enemies as you approach them...
                    ProgressStepCycle(speed);
                }
                
            }
            
        }
        else {
            //Vampire attack hard coded move

            //gravity
            vampireAtk_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;

            //move
            m_CollisionFlags = characterController.Move(vampireAtk_MoveDir * Time.fixedDeltaTime);

            //diminish forward speed with time???
        }


        
    }



    public void VampireAttack()
    {
        if (isAtking || isCatchingPrey) {
            return;
        }

        //Setup attack movement to be used in the FixedUpdate
        vampireAtk_MoveDir = Vector3.zero;
        if (!m_Airborne) {
            vampireAtk_MoveDir.y = vampireAttackImpulseY;
        } else {
            vampireAtk_MoveDir.y = vampireAttackImpulseY / 2;
        }
        vampireAtk_MoveDir += transform.forward * vampireAttackForwardSpeed;

        atkTimer = 0f;

        //this bool tells the movement script to not perform updates while attacking and to not rotate the character according the the camera forward
        isAtking = true;
        myAnimator.SetTrigger("doAttack");

        //StartCoroutine(Dash(15f, 10f, 0.4f));
        //dashCallback = 1;
    }

    public void Punch ()
    {
        if (!isPunching) {
            //new series of punches
            punchCounter = 0;

            //update logic
            punchTimer = 0f;
            isPunching = true;
            punchingBlow = true;
            comboInputPending = false;

            //movement
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector2 moveInput = new Vector2(horizontal, vertical);
            if (moveInput.magnitude > 0.1f) {
                StartCoroutine("PunchMove");
            }

            //animator
            myAnimator.SetTrigger("doPunch1");
            SetPunchHitbox(punchCounter);
        }
        else {
            comboInputPending = true;
        }
    }

    private IEnumerator PunchMove()
    {
        Debug.Log("PunchMove");
        //AnimatorStateInfo currentState = myAnimator.GetCurrentAnimatorStateInfo(0);
        //if (myAnimator.GetCurrentAnimatorStateInfo(0).IsName("Gameplay.WaitForBlood")) {

        float t = 0f;
        while (t < 1) {
            //t += Time.deltaTime * 2;
            t += Time.fixedDeltaTime * 2;
            Vector3 attackVector = transform.forward * 8;
            attackVector.y = -1.0f; 
            characterController.Move(attackVector * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
    }

    private void PunchUpdate ()
    {
        punchTimer += Time.deltaTime;

        //duration of a blow
        if (punchingBlow && punchTimer > 0.4f) {
            //the blow is over, full control is given back
            punchingBlow = false;
            //remove hitbox validity to hit
            hitboxRightHand.canHitAI = false;
            hitboxLeftHand.canHitAI = false;
            //animation goes back to idle
            myAnimator.SetTrigger("endPunchSerie");
        }

        //window frame for combo after the blow
        if (punchTimer > 0.41f && punchTimer <= 0.7f) {
            if (punchCounter < 2 && comboInputPending) {
                comboInputPending = false;
                punchTimer = 0f;
                punchCounter += 1;

                punchingBlow = true;
                StopCoroutine("PunchMove");
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                Vector2 moveInput = new Vector2(horizontal, vertical);
                if (moveInput.magnitude > 0.1f) {
                    StartCoroutine("PunchMove");
                }

                myAnimator.ResetTrigger("endPunchSerie");
                myAnimator.SetTrigger("doPunch"+(punchCounter+1).ToString());
                //myAnimator.SetTrigger("doPunch1");
                SetPunchHitbox(punchCounter);
            }
        }

        //if window frame to combo is over
        if (punchTimer > 0.7f) {
            isPunching = false;
        }
    }


    public void AbortPunching ()
    {
        isPunching = false;
        //punchingBlow = false;

        hitboxRightHand.canHitAI = false;
        hitboxLeftHand.canHitAI = false;

        StopCoroutine("PunchMove");

        myAnimator.ResetTrigger("doPunch1");
        myAnimator.ResetTrigger("doPunch2");
        myAnimator.ResetTrigger("doPunch3");
        myAnimator.ResetTrigger("endPunchSerie");
    }


    private void SetPunchHitbox (int comboCounter)
    {
        if (comboCounter == 1) {
            hitboxRightHand.canHitAI = true;
            hitboxLeftHand.canHitAI = false;
        }else {
            hitboxRightHand.canHitAI = false;
            hitboxLeftHand.canHitAI = true;
        }
    }



    private IEnumerator Dash(float impulseY, float speed, float timer)
    {
        Vector3 vampireAtk_MoveDir = Vector3.zero;
        if (!m_Airborne) {
            vampireAtk_MoveDir.y = vampireAttackImpulseY;
        } else {
            vampireAtk_MoveDir.y = vampireAttackImpulseY / 2;
        }

        
        vampireAtk_MoveDir += transform.forward * vampireAttackForwardSpeed;

        float t = 0f;
        while (t < timer) {
            t += Time.deltaTime;

            //gravity
            vampireAtk_MoveDir += Physics.gravity * m_GravityMultiplier * Time.deltaTime;

            //move
            m_CollisionFlags = characterController.Move(vampireAtk_MoveDir * Time.deltaTime);

            yield return 0;
        }
    }



    //State changes
    private void Crawl ()
    {
        m_Crawling = true;
        myAnimator.SetBool("isCrawling", true);

        characterController.height = 1.4f;
        characterController.center = new Vector3(0, -0.3f, 0);  //(2 - 1.4) / 2 = 0.3
    }

    private void StandUp ()
    {
        m_Crawling = false;
        myAnimator.SetBool("isCrawling", false);

        characterController.center = new Vector3(0, 0f, 0);
        characterController.height = 2f;
    }


    private void SetAirborne (bool target)
    {
        m_Airborne = target;

        if (m_Airborne) {
            characterController.center = new Vector3(0, 0.3f, 0);
            characterController.height = 2.6f;
        }else {
            characterController.height = 2f;
            characterController.center = new Vector3(0, 0, 0);
        }
    }

    private void GrabVines(Transform vinesTransform, Vector3 contactNormal)
    {
        m_Airborne = false;
        m_GrabbingVines = true;

        m_MoveDir = Vector3.zero;
        //transform.rotation = Quaternion.LookRotation(vinesTransform.up, vinesTransform.forward);
        //transform.rotation = Quaternion.LookRotation(vinesTransform.forward, vinesTransform.up);
        transform.rotation = Quaternion.LookRotation(-contactNormal);


        Camera.main.GetComponent<sr_CameraControl>().ResetTilt();

        //This causes bugs, like entering, exiting in a loop maybe, the collider...?
        characterController.height = 1f;
        characterController.center = new Vector3(0, 0, 0);

        myAnimator.SetTrigger("doGrabVines");
    }

    private void ReleaseVines ()
    {
        SetAirborne(true);
        m_GrabbingVines = false;

        m_VinesRelease = true;
        Invoke("VinesReleaseComplete", m_VinesReleaseCooldown);

        vinesNormal = Vector3.zero;
        currentVines = null;

        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        characterController.center = new Vector3(0, 0f, 0);
        characterController.height = 2f;

        myAnimator.SetTrigger("doReleaseVines");
    }


    //Cooldown methods
    private void LedgeReleaseComplete ()
    {
        m_LedgeRelease = false;
    }

    private void FlyReleaseComplete ()
    {
        m_FlyRelease = false;
    }

    private void VinesReleaseComplete ()
    {
        m_VinesRelease = false;
    }


    //ANIM TRIGGER RESET HACK
    private IEnumerator AnimatorResetTriggerHack (string triggerName)
    {
        //small delay
        float t = 0f;
        while (t < 0.1f) {
            t += Time.deltaTime;
            yield return 0;
        }
        //reset the trigger!
        myAnimator.ResetTrigger(triggerName);
        Debug.Log("Reset Trigger HACK");
    }


    //AUDIO
    private void PlayLandingSound()
    {
        if (forgetNextLandingSound) {
            forgetNextLandingSound = false;
            return;
        }

        audio2D.clip = m_LandSound;
        audio2D.Play();
        m_NextStep = m_StepCycle + .5f;
    }

    private void PlayJumpSound()
    {
        audio2D.clip = m_JumpSound;
        audio2D.Play();
    }

    //footsteps
    private void ProgressStepCycle(float speed)
    {
        if (characterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0)) {
            m_StepCycle += (characterController.velocity.magnitude + (speed * 1f)) * Time.fixedDeltaTime;
        }
        if (!(m_StepCycle > m_NextStep)) {
            return;
        }
        m_NextStep = m_StepCycle + m_StepInterval;
        PlayFootStepAudio();
    }


    private void PlayFootStepAudio()
    {
        if (!characterController.isGrounded) {
            return;
        }
        // pick & play a random footstep sound from the array,
        // excluding sound at index 0
        int n = Random.Range(1, m_FootstepSounds.Length);
        audio2D.clip = m_FootstepSounds[n];
        audio2D.PlayOneShot(audio2D.clip);
        // move picked sound to index 0 so it's not picked next time
        m_FootstepSounds[n] = m_FootstepSounds[0];
        m_FootstepSounds[0] = audio2D.clip;
        //noise signal
        MakeNoise();
    }

    private void MakeNoise ()
    {
        clearNoiseTimer = 0.05f;
        justMadeNoise = true;
    }

    private void CheckClearNoise ()
    {
        if (justMadeNoise) {
            clearNoiseTimer -= Time.deltaTime;
            if (clearNoiseTimer <= 0f) {
                justMadeNoise = false;
            }
        }
    }


    //Collisions
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.transform.tag == "MovingPlatform") {
            //Is the surface hit horizontal enough (very vertical normal) ?
            //if (hit.normal.y > 0.5f) {
                transform.SetParent(hit.transform); //.parent);
                isOnMovingPlatform = true;
            //}
            
        }


        if (hit.transform.tag == "Vines") {
            //Debug.Log("Collides with a Vines volume");
            if (m_VinesRelease == false && !m_GrabbingVines) {
                if (m_Airborne) {
                    GrabVines(hit.transform, hit.normal);
                }
                if (m_GrabbingVines) {
                    Debug.Log("...enter a new vines adjacent");
                    GrabVines(hit.transform, hit.normal);
                }

            }
        }

        if (hit.transform.tag == "Prey") { //&& player is attacking(+ slight dash
            if (isAtking) {
                VampireEmbrace(hit.gameObject);

                /*Debug.Log("Catch a prey!");
                Quaternion lookAtRot = Quaternion.LookRotation((hit.transform.position - transform.position), Vector3.up);
                lookAtRot = Quaternion.Euler(0, lookAtRot.eulerAngles.y, 0);
                transform.rotation = lookAtRot;
                myAnimator.SetTrigger("doCatchPrey");
                StartCoroutine("AttackMoveLerp", hit.transform.position + transform.forward * 0.3f);

                hit.gameObject.GetComponent<sr_Character>().InterruptMovement();
                Quaternion preyLookAtRot = Quaternion.LookRotation((transform.position - hit.transform.position), Vector3.up);
                preyLookAtRot = Quaternion.Euler(0, preyLookAtRot.eulerAngles.y, 0);
                hit.transform.rotation = preyLookAtRot;
                hit.gameObject.GetComponent<sr_Character>().SetAnimatorTrigger("caughtByPlayer");
                hit.gameObject.GetComponent<CharacterController>().enabled = false;
                hit.gameObject.GetComponent<sr_Prey>().isDown = true;

                isCatchingPrey = true;
                isAtking = false;*/
                
            }
        }


        /*
        //Used to apply character weight on rigidbodies...?
        Rigidbody body = hit.collider.attachedRigidbody;
        //dont move the rigidbody if the character is on top of it
        if (m_CollisionFlags == CollisionFlags.Below)
        {
            return;
        }

        if (body == null || body.isKinematic)
        {
            return;
        }
        body.AddForceAtPosition(characterController.velocity*0.1f, hit.point, ForceMode.Impulse);*/
    }


    public void VampireEmbrace (GameObject prey)
    {
        isAtking = false;
        player.currentPrey = prey.GetComponent<sr_Prey>();

        StopCoroutine("PunchMove");
        isPunching = false;
        punchingBlow = false;
        hitboxLeftHand.canHitAI = false;
        hitboxRightHand.canHitAI = false;

        Debug.Log("Catch a prey!");
        Quaternion lookAtRot = Quaternion.LookRotation((prey.transform.position - transform.position), Vector3.up);
        lookAtRot = Quaternion.Euler(0, lookAtRot.eulerAngles.y, 0);
        transform.rotation = lookAtRot;
        myAnimator.SetTrigger("doCatchPrey");
        StartCoroutine("AttackMoveLerp", prey.transform.position + transform.forward * 0.3f);

        prey.GetComponent<sr_Character>().InterruptMovement();
        Quaternion preyLookAtRot = Quaternion.LookRotation((transform.position - prey.transform.position), Vector3.up);
        preyLookAtRot = Quaternion.Euler(0, preyLookAtRot.eulerAngles.y, 0);
        prey.transform.rotation = preyLookAtRot;
        prey.GetComponent<sr_Character>().SetAnimatorTrigger("caughtByPlayer");
        prey.GetComponent<CharacterController>().enabled = false;
        prey.GetComponent<sr_Prey>().isDown = true;

        isCatchingPrey = true;

        
    }


    private IEnumerator AttackMoveLerp (Vector3 desti)
    {
        Vector3 origin = transform.position;
        //Vector3 desti = transform.position + transform.forward * 1.2f;

        float t = 0f;
        while (t < 1) {
            t += Time.deltaTime * 2;
            transform.position = Vector3.Lerp(origin, desti, t);
            yield return 0;
        }
    }

    void OnTriggerEnter (Collider collider)
    {
        
        /*if (collider.gameObject.tag == "Vines") {
            //Debug.Log("Enter Vines Trigger");
            if (m_VinesRelease == false) {
                if (m_Airborne) {
                    GrabVines(collider.transform);
                }
                if (m_GrabbingVines) {
                    //enter a new vines adjacent
                    GrabVines(collider.transform);
                }
                
            }
        }*/
    }

    void OnTriggerExit(Collider collider)
    {
        /*if (collider.gameObject.tag == "Vines") {
            Debug.Log("exit Vines");

            //If we have walked inside another vines collider
            if (collider.transform != currentVines) {
                // don't release from vines
            }
            else {
                //but if the just exited vines is the current one (last recorded), then release the player
                ReleaseVines();

                //myAnimator.SetBool("isGrounded", false);
                myAnimator.SetTrigger("doFallOff");

                //currentVines = null;
            }
        }*/
    }



    


}

