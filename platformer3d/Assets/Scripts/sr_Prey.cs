using UnityEngine;
//using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class sr_Prey : MonoBehaviour
{

    public bool reactToPlayer = true;
    public float visionDistance = 40f;
    public float fieldOfViewAngle = 110f;
    public float hearingDistance = 30f;
    public Transform[] registeredSpawns;
    private sr_Hitbox hitboxAttack;
    public float fightingDecisionFrequency = 0.5f;
    public float distanceToAttack = 6f;
    public float attackMoveSpeed = 12f;
    public float attackRotationCapacity = 6f;
    public float cooldownBetweenAttacks = 3f;
    public int hp = 4;
    public float blood = 30f; //1 sec for 5 of blood
    public float bloodLostPerHit = 3f;

    private sr_Character chara;
    private GameObject player;
    private sr_Movement playerMovement;
    private sr_Main main;

    private float distanceToPlayer = 1000f;
    private bool isPlayerDetected = false;
    private Vector3 lastPlayerSighting;

    [HideInInspector]
    public bool isDown = false;

    private bool isScrutinizing = false;

    private float scrutTimer = 0f;
    private bool eyesOnPlayerThisFrame = false;
    private float scrutinyLostTimer = 0f;

    private Quaternion lastIdleRot;

    private bool isFighting = false;
    private float decisionTimer = 0f;

    List<Vector3> dirs = new List<Vector3>();

    private bool steering = false;
    private Vector3 steerTarget;
    private Vector3 steerDir;
    private Vector3 previousDir = Vector3.zero;

    private bool isAttacking = false;
    
    private float atkTimer = 0f;
    private bool hitboxHasBeenActivated = false;
    private float lastAttackTime = 0f;

    private int fear = 0;
    

    private float[] weight = new float[6];
    private const int atk = 0;
    private const int fwd = 1;
    private const int strafe = 2;
    private const int back = 3;
    private const int idle = 4;
    private const int flee = 5;

    private bool strafeIsLastDecision = false;
    private int lastStrafeDir = 0;

    [HideInInspector]
    public bool isFleeing = false;
    private int fleeZonePointsNumber = 4;

    private Vector3[] gizmoPoints;
    private bool drawGizmoPoints = false;



    void Start()
    {
        chara = GetComponent<sr_Character>();
        player = GameObject.Find("Player");
        playerMovement = player.GetComponent<sr_Movement>();
        main = GameObject.Find("MAIN").GetComponent<sr_Main>();

        Transform body = transform.GetChild(0);
        foreach (Transform child in body) {
            if (child.name == "Hitbox_Attack") {
                hitboxAttack = child.GetComponent<sr_Hitbox>();
            }
        }
        if (hitboxAttack == null) {
            Debug.Log("Hitbox_Attack not found on AI: " + name);
        }

        Vector3[] gizmoPoints = new Vector3[fleeZonePointsNumber];

        //fix on the ground
        GetComponent<CharacterController>().Move(-Vector3.up * 10f);

    }


    void Update()
    {
        distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (reactToPlayer && !isPlayerDetected && !isDown) {
            Sight();
            Hearing();
        }

        if (!isDown) {
            if (chara.myAnimator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Sacrifice")) {
                isDown = true;
            }
        }

        if (isScrutinizing) {
            if (eyesOnPlayerThisFrame) {
                //try to discern who it is...
                scrutTimer += Time.deltaTime;

                //reset any 'doubt'
                scrutinyLostTimer = 0f;

                if (scrutTimer > 0.5f) { //1.0
                    //detection!
                    scrutTimer = 0f;
                    DetectPlayer();

                }
            } else {
                //I don't see anything anymore...
                scrutinyLostTimer += Time.deltaTime;

                if (scrutinyLostTimer > 1.0f) {
                    scrutinyLostTimer = 0f;
                    ReturnFromScrutinizeToIdle();
                }
            }
        }

        if (isPlayerDetected && !isAttacking && !isDown && !isFleeing) {
            Quaternion rot = Quaternion.LookRotation(player.transform.position - transform.position);
            transform.rotation = Quaternion.Euler(0, rot.eulerAngles.y, 0);
        }

        if (isFighting && !isAttacking) {
            //in fight stance, the mongrel faces the player. he either go forward, backward or sideways as movement, or remain stationary
            decisionTimer += Time.deltaTime;

            if (decisionTimer > fightingDecisionFrequency) { //decisionFrequency

                decisionTimer = 0f;

                //if low life, fear augments at each decision time
                if (hp <= 1 && distanceToPlayer <= 10f) {
                    fear += 1;
                }

                //if distance is big, fear diminishes
                if (distanceToPlayer > 18f) {
                    fear -= 1;
                }

                //reset actions' weights, new random weight for each
                for (int i = 0; i < weight.Length; i++) {
                    weight[i] = Random.Range(1, 4) + Random.Range(0.0f, 1.0f);
                }
                //except for flee, which is not considered except in case of high fear (condition checked below)
                weight[flee] = 0f;

                //check if directions are walkable (no obstacle ahead, ground below feet)
                weight[fwd] += RaycastDirectionWeight(transform.forward);
                weight[back] += RaycastDirectionWeight(-transform.forward);
                weight[strafe] += RaycastDirectionsWeights(transform.right, -transform.right);

                //prioritize some actions depending on reading the AI situation
                if (distanceToPlayer <= distanceToAttack) {

                    if (Time.realtimeSinceStartup > lastAttackTime + cooldownBetweenAttacks) {
                        //prioritize attack, attack is allowed
                        weight[atk] += 2.0f;
                        weight[strafe] += 0.5f;
                    } else {
                        //attack is in cooldown but we are in the zone for attacking: prioritize strafing or backward movement
                        weight[strafe] += 1.5f;
                        //weight[back] += 0.5f;
                    }

                } else {
                    if (fear > 50) {
                        //we are scared, prioritize maintaining a certain distance
                        if (distanceToPlayer < distanceToAttack * 2) {
                            //prioritize backward move or strafing
                            weight[strafe] += 1.0f;
                            weight[back] += 1.5f;
                        } else {
                            //we are at a good distance, slightly prioritize stationary/scream anim
                            weight[idle] += 0.5f;
                        }
                    } else {
                        //we are not overly scared
                        //we are too far away to attack, prioritize forward movement or strafing
                        weight[fwd] += 1.5f;
                        weight[strafe] += 0.5f;
                    }
                }

                //High FEAR modificators
                if (fear > 80) {
                    //check switch to flee mode
                    weight[back] += 3.5f;
                    weight[strafe] += 1.0f;
                    weight[fwd] -= 5.0f;
                    weight[atk] -= 2.0f;

                    if (chara.myAnimator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.FearFlee") == false) {
                        chara.myAnimator.SetTrigger("doFearFlee");
                    }

                    if (distanceToPlayer <= 5f) {
                        weight[flee] = Random.Range(0, 11);
                    }
                }

                int higherWeight = System.Array.IndexOf(weight, weight.Max());

                chara.myAnimator.SetFloat("moveSpeed", 0f);
                steering = false;

                switch (higherWeight) {
                    case atk:
                        //if (distanceToPlayer <= distanceToAttack) {
                        Attack();
                        break;

                    case fwd:
                        steerDir = transform.forward * 1f;
                        steering = true;
                        chara.myAnimator.SetFloat("moveSpeed", 1f);
                        break;

                    case strafe:
                        bool r = RaycastDirectionValidity(transform.right);
                        bool l = RaycastDirectionValidity(-transform.right);
                        if (r && l) {
                            if (strafeIsLastDecision) {
                                if (lastStrafeDir == 0) {
                                    steerDir = transform.right * 1f;
                                    lastStrafeDir = 0;
                                } else {
                                    steerDir = -transform.right * 1f;
                                    lastStrafeDir = 1;
                                }
                            } else {
                                lastStrafeDir = Random.Range(0, 2);
                                if (lastStrafeDir == 0) {
                                    steerDir = transform.right * 1f;
                                } else {
                                    steerDir = -transform.right * 1f;
                                }
                            }
                        } else {
                            if (r) {
                                steerDir = transform.right * 1f;
                                lastStrafeDir = 0;
                            } else {
                                steerDir = -transform.right * 1f;
                                lastStrafeDir = 1;
                            }
                        }
                        strafeIsLastDecision = true;
                        steering = true;
                        chara.myAnimator.SetFloat("moveSpeed", 1f);
                        break;

                    case back:
                        steerDir = -transform.forward * 1f;
                        steering = true;
                        chara.myAnimator.SetFloat("moveSpeed", 1f);
                        break;

                    case idle:
                        //stationary
                        break;

                    case flee:
                        isFighting = false;
                        ActionFlee();
                        break;

                }

                if (higherWeight != strafe) {
                    strafeIsLastDecision = false;
                }
            }
        }

        if (isAttacking) {
            float hitboxWindow_min = 0.5f;
            float hitboxWindow_max = 0.75f;
            atkTimer += Time.deltaTime;
            if (!hitboxAttack.canHitPlayer) {
                if (atkTimer > hitboxWindow_min && atkTimer < hitboxWindow_max && !hitboxHasBeenActivated) {
                    hitboxAttack.canHitPlayer = true;
                    hitboxHasBeenActivated = true;

                    //hitboxAttack.GetComponent<MeshRenderer>().enabled = true;
                }
            } else {
                if (atkTimer > hitboxWindow_max) {
                    hitboxAttack.canHitPlayer = false;

                    //hitboxAttack.GetComponent<MeshRenderer>().enabled = false;
                }
            }
        }
    }


    void FixedUpdate()
    {
        if (steering) {

            chara.controller.Move(steerDir * Time.fixedDeltaTime * chara.walkSpeed);
        }
    }



    public void ModifyFear(int changeAmount)
    {
        fear += changeAmount;
    }


    private bool RaycastDirectionValidity(Vector3 dir)
    {
        if (Physics.Raycast(transform.position, dir, 2f) == false) {
            if (Physics.Raycast(transform.position + dir * 2f, -Vector3.up, 2f)) {
                return true;
            }
        }
        return false;
    }

    private float RaycastDirectionWeight(Vector3 dir)
    {
        if (Physics.Raycast(transform.position, dir, 2f) == false) {
            if (Physics.Raycast(transform.position + dir * 2f, -Vector3.up, 2f)) {
                return 0f;
            }
        }
        return -10f;
    }

    private float RaycastDirectionsWeights(Vector3 dir, Vector3 dir2)
    {
        if (Physics.Raycast(transform.position, dir, 2f) == false) {
            if (Physics.Raycast(transform.position + dir * 2f, -Vector3.up, 2f)) {
                return 0f;
            }
        }
        if (Physics.Raycast(transform.position, dir2, 2f) == false) {
            if (Physics.Raycast(transform.position + dir2 * 2f, -Vector3.up, 2f)) {
                return 0f;
            }
        }
        return -10f;
    }


    private void ReturnFromScrutinizeToIdle()
    {
        isScrutinizing = false;
        transform.rotation = lastIdleRot;
    }


    private void Sight()
    {
        eyesOnPlayerThisFrame = false;

        // If the player is close enough to be seen
        if (distanceToPlayer <= visionDistance) {

            // Create a vector from the enemy to the player and store the angle between it and forward.
            Vector3 direction = player.transform.position - transform.position;
            float angle = Vector3.Angle(direction, transform.forward);

            // If the angle between forward and where the player is, is less than half the angle of view...
            if (angle < fieldOfViewAngle * 0.5f) {
                //Debug.Log("player is in the mongrel's cone of vision");
                RaycastHit hit;
                if (Physics.Raycast(transform.position, direction.normalized, out hit, visionDistance)) {
                    //Debug.Log("raycast hit: " + hit.transform.name);
                    // if the raycast hits the player...
                    if (hit.collider.gameObject == player) {
                        // ... the player is in sight theoretically
                        //is he lighted enough to be seen?
                        float playerVisibility = player.GetComponent<sr_Player>().lightAtPlayerPos;

                        if (playerVisibility > 20f && playerVisibility <= 30f) {
                            //barely visible, need to scrutinize
                            Scrutinize(player.transform.position);
                            eyesOnPlayerThisFrame = true;
                        } else if (playerVisibility > 30f) {
                            //clearly seen, straight away!
                            DetectPlayer();
                        }
                    }
                }
            }

        }
    }

    public void PickupSound(Vector3 emittedPosition, float strength)
    {
        //rotate towards the sound
        Vector3 direction = emittedPosition - transform.position;
        Quaternion rot = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Euler(0, rot.eulerAngles.y, 0);

        Scrutinize(emittedPosition);
        //StartCoroutine("Scrutinize", emittedPosition);
    }

    private void Hearing()
    {
        //If the player is close enough to be heard
        if (distanceToPlayer <= hearingDistance) {
            //if he made noise
            if (playerMovement.justMadeNoise) {
                //stop the prey in case it was moving
                chara.InterruptMovement();

                //insert very small reaction delay here

                //TODO: the ai turns in the direction of the perceived sound (not necessarily emitted at the player position - like a mechanism activating something somewhere else)
                //the ai will now look in that direction and check if the player is seen and lighted before "Player detected!"

                Vector3 direction = player.transform.position - transform.position;
                Quaternion rot = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Euler(0, rot.eulerAngles.y, 0);

                //insert Alert animation
                Debug.Log("AI " + name + "  heard something...");
                Scrutinize(player.transform.position);

            }
        }
    }

    private void DetectPlayer()
    {
        Debug.Log("AI " + name + "  detects the player.");
        isPlayerDetected = true;
        isScrutinizing = false;

        chara.myAnimator.SetTrigger("doScream");

        // Save the last known player position from the point of view of this AI
        lastPlayerSighting = player.transform.position;

        //fight or flee?
        fear += Random.Range(10, 20);

        //ActionFlee();
        isFighting = true;
        chara.SetRun(false);
    }

    private void Scrutinize(Vector3 pointOfInterest)
    {
        //transition from idle to scrutinize
        if (isScrutinizing == false) {
            isScrutinizing = true;
            chara.myAnimator.SetTrigger("doAlert");
            //save idle rot to restaure it if scrutinize fails
            lastIdleRot = transform.rotation;
        }

        //rotation to look at the point of interest
        Quaternion lookRot = Quaternion.LookRotation(pointOfInterest - transform.position, Vector3.up);
        transform.rotation = Quaternion.Euler(0, lookRot.eulerAngles.y, 0);


    }



    private void ActionFlee()
    {
        Debug.Log("ACTION FLEE");
        isFighting = false;
        isFleeing = true;
        RunAway();
    }

    public void RunAwayTargetReached()
    {
        isFleeing = false;
        chara.SetRun(false);

        isFighting = true;
    }

    private void RunAway()
    {
        //Pick a number of random points in the pathfinderZone
        if (chara.pathfinderZone == null) {
            Debug.Log("No pathfindingZone assigned in sr_Character inspector. Abort RunAway.");
            return;
        }
        //float u = Random.value;
        //float v = Random.value;

        float sizex = chara.pathfinderZone.localScale.x;
        float sizez = chara.pathfinderZone.localScale.z;
        Vector3 zonePos = chara.pathfinderZone.position;

        Vector3[] points = new Vector3[fleeZonePointsNumber];
        for (int i = 0; i < points.Length; i++) {
            points[i] = new Vector3(Random.Range(zonePos.x - (sizex / 2), zonePos.x + (sizex / 2)), zonePos.y, Random.Range(zonePos.z - (sizez / 2), zonePos.z + (sizez / 2)));
        }
        //DEBUG
        gizmoPoints = points;
        drawGizmoPoints = true;
        Invoke("StopDrawGizmoPoints", 5f);

        int selectedPoint = -1;
        float bestValue = -100f; //0f can results in all inferior to 0 and thus result in prey staying petrified instead of fleeing
        for (int k = 0; k < points.Length; k++) {
            Vector3 pnt = points[k];
            float value = 0;

            Vector3 dirToPoint = pnt - transform.position;
            Vector3 dirToPreda = player.transform.position - transform.position;
            Vector3 predaToPoint = pnt - player.transform.position;

            //this is the most important param. the greater the value the better
            float pointPromiscuity = predaToPoint.magnitude - dirToPoint.magnitude;
            value += pointPromiscuity;

            //predator is nearer than the point, so he could be in the way, if the angle is tight
            if (dirToPreda.magnitude < dirToPoint.magnitude) {
                float angle = Vector3.Angle(dirToPoint, dirToPreda);
                if (angle < 20) {
                    //the tighter the angle, the more dangerous to pass near the predator
                    value -= (20 - angle) / 2;
                }

            }

            //param
            value += predaToPoint.magnitude / 2;

            if (value > bestValue) {
                selectedPoint = k;
                bestValue = value;
            }
        }

        if (selectedPoint == -1) {
            Debug.Log("Nowhere to run to! Stay petrified.");
        } else {
            chara.SetRun(true);
            chara.PathToTarget(points[selectedPoint]);
            Debug.Log("Run Away!");
        }

    }


    private void Attack()
    {
        atkTimer = 0f;
        hitboxHasBeenActivated = false;
        isAttacking = true;
        chara.myAnimator.SetTrigger("doAttack");
        StartCoroutine("AttackMove");

        lastAttackTime = Time.realtimeSinceStartup;
    }

    private IEnumerator AttackMove()
    {
        Vector3 atkDir = transform.forward;

        //prep time
        yield return new WaitForSeconds(0.3f);

        //hitboxAttack.isValidForHit = true;

        float t = 0f;
        while (t < 1) {
            t += Time.fixedDeltaTime * 1.8f;

            //ability to slightly adjust his rotation towards the moving player
            Quaternion lookAtPlayerRot = Quaternion.LookRotation(player.transform.position - transform.position);
            lookAtPlayerRot = Quaternion.Euler(0, lookAtPlayerRot.eulerAngles.y, 0);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookAtPlayerRot, Time.fixedDeltaTime * attackRotationCapacity);

            atkDir = transform.forward;
            chara.controller.Move(atkDir * Time.fixedDeltaTime * attackMoveSpeed);

            yield return new WaitForFixedUpdate();
        }

        //hitboxAttack.isValidForHit = false;

        isAttacking = false;
    }


    public void GetHurt(int dmg)
    {
        //lifeblood -= dmg;
        chara.InterruptMovement();

        Quaternion lookDir = Quaternion.LookRotation(player.transform.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Euler(0, lookDir.eulerAngles.y, 0);

        chara.myAnimator.SetTrigger("getHurt");

        GameObject fxb = Instantiate(main.fxBloodBurst, transform.position, Quaternion.Euler(-70f, 0f, 10f)) as GameObject;
        Destroy(fxb, 1f);

        blood -= bloodLostPerHit;
        hp -= dmg;
        if (hp <= 0) {
            chara.myAnimator.SetBool("isDead", true);

            chara.InterruptMovement();
            //Quaternion preyLookAtRot = Quaternion.LookRotation((transform.position - prey.transform.position), Vector3.up);
            //preyLookAtRot = Quaternion.Euler(0, preyLookAtRot.eulerAngles.y, 0);
            //prey.transform.rotation = preyLookAtRot;

            //prey.GetComponent<sr_Character>().SetAnimatorTrigger("caughtByPlayer");
            GetComponent<CharacterController>().enabled = false;
            isFighting = false;
            isAttacking = false;
            isDown = true;
        }
        else {
            int fearAdded = Random.Range(10, 20);
            fear += fearAdded;

            if (fearAdded >= 18) { // 2 chances pour 9 possibles
                Debug.Log("Flee from a brutal hit!");
                ActionFlee();
            }
            else {
                StopCoroutine("Pushback");
                StartCoroutine("Pushback");
            }
        }
    }

    private IEnumerator Pushback()
    {
        float t = 0f;
        while (t < 1) {
            t += Time.fixedDeltaTime * 2;
            chara.controller.Move(-transform.forward * Time.fixedDeltaTime * 8);
            yield return new WaitForFixedUpdate();
        }
    }




    void OnTriggerEnter(Collider collider)
    {
        if (collider.GetComponent<sr_Hitbox>() != null) {
            if (collider.GetComponent<sr_Hitbox>().canHitAI) {
                HitboxHitResult();
                collider.GetComponent<sr_Hitbox>().canHitAI = false;
            }
        }
    }

    private void HitboxHitResult()
    {
        //vampire blood attack on  unaware prey   or  already beaten prey
        if (!isPlayerDetected || isDown) {
            if (blood > 0) {
                player.GetComponent<sr_Movement>().VampireEmbrace(gameObject);
            }
        }
        //regular dmg hit
        else {
            GetHurt(1);
        }
    }


    private void StopDrawGizmoPoints()
    {
        drawGizmoPoints = false;
    }

    void OnDrawGizmos ()
    {
        if (drawGizmoPoints) {
            foreach (Vector3 point in gizmoPoints) {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(point, 0.5f);
            }
        }
    }

}
