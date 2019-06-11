using UnityEngine;
using System.Collections;

public class sr_Predator : MonoBehaviour {

    //has several vantage point
    //can change vantage point at times
    //can be more omniscient about the player than preys, though its senses must evaluate the world and are not perfect, so the player can hide from it, outsmart it.
    //what does he do if he hears something he cannot see? change vantage point

    //they pose different challenges:
    // - traversal: must go through a zone of the level guarded by the predator
    // - puzzle to kill: avoid being killed by the predator to lure him into a set-up trap (mechanisms, spikes...)

    //pathfinding: if the environment gets more complex than plain open field, with pillard etc, if we want the beast to avoid obstacles as the player moves...
    //we can't make it translate straight to player target. We need a cluster of astar nodes in the air for avoidance.

        //sometimes he just makes a little aerial move without descending for an attack, but you can hear his presence, see his shadow...

    public float visionDistance;
    public float fieldOfViewAngle = 180f;
    public float visionDarkTreshold = 20f;
    public float hearingDistance = 30f;
    public Transform[] vantagePoints;

    public float attackReactDistance = 5f;
    public float attackHitDistance = 3f;
    public float reactionTimeframe = 0.1f;

    public float repathRate;

    private sr_Main main;
    private GameObject player;

    private sr_Character chara;
    private CharacterController charController;

    private bool playerDetected = false;
    private bool isAttacking = false;
    private bool isApproaching = false;
    private bool isAboutToHit = false;
    private bool isQTE = false;
    private Vector3 lastPlayerSighting = Vector3.zero;
    private float distanceToPlayer = 1000f;
    private float qteTimer = 0f;
    private bool thisAtkHasHit = false;
    private bool isReturningToStation = false;

    private Vector3 moveDir = Vector3.zero;

    private Vector3 moveTarget;
    

    private bool isPlayerInSight = false;
    private float repathTimer = 0f;



	void Awake ()
    {
        main = GameObject.Find("MAIN").GetComponent<sr_Main>();
        player = main.player;

        chara = GetComponent<sr_Character>();
        charController = GetComponent<CharacterController>();
    }

    void Start ()
    {
        //fix on the ground
        GetComponent<CharacterController>().Move(-Vector3.up * 10f);

        //set initial vantage position
        if (vantagePoints.Length > 0) {
            transform.position = vantagePoints[0].position;
        }
    }
	


    private void StateManagement ()
    {
        if (!isAttacking && isPlayerInSight && !thisAtkHasHit) {
            LaunchAttackOnPlayer();
        }

        //When attacking/approaching...
        if (isAttacking) {
            //if the distance is close enough, we go into reaction/QTE mode
            if (distanceToPlayer < attackReactDistance && isQTE == false) {
                isQTE = true;
                //frame the moment, rotate actors, place cam, slow mo
                main.PredatorAttackPlayer(transform);
                //the preda goes straight for the player now
                moveTarget = player.transform.position;
                chara.PathToTarget(moveTarget);

                chara.myAnimator.SetTrigger("doPunch");
            }

            //Timeframe of the QTE
            if (isQTE) {
                qteTimer += Time.deltaTime;
                if (qteTimer > reactionTimeframe) {
                    isQTE = false;
                    Time.timeScale = 1f;
                }
            }

            //If the distance is close enough for a HIT!
            if (distanceToPlayer < attackHitDistance && !thisAtkHasHit) {
                thisAtkHasHit = true;
                player.GetComponent<sr_Player>().GetHurt(10);
                isAttacking = false;

                //then return to a vantage point
                isReturningToStation = true;
                //chara.PathToTarget(vantagePoints[Random.Range(0, vantagePoints.Length)].position);
                chara.PathToTarget(vantagePoints[0].position);
            }
        }
    }



	void Update ()
    {
        //Get distance to player. Useless to evaluate situation if they are too far apart.
        distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        //Do we have vision on the player?
        isPlayerInSight = HasSight();
        //HACK TEST: omniscient vision!
        lastPlayerSighting = player.transform.position;

        //Check for state/goal change conditions
        StateManagement();

        //Update target when we are after a moving target: the player
        if (isAttacking) {
            if (!isQTE) {
                //on approach, aim for the last sighting of the player plus hover above it a bit
                moveTarget = lastPlayerSighting + Vector3.up * 0.35f;
            } else {
                //about to hit, go straight for the player
                //moveTarget = player.transform.position;
            }

            repathTimer += Time.deltaTime;
            if (repathTimer >= repathRate) {
                repathTimer = 0f;
                //Progress towards the target, send a new path order
                chara.PathToTarget(moveTarget);
            }

            
        }
    }



    private bool HasSight()
    {
        // If the player is close enough to be seen
        if (distanceToPlayer <= visionDistance) {
            // Create a vector from the enemy to the player and store the angle between it and forward.
            Vector3 eyesPosition = transform.position + Vector3.up * 1f;
            Vector3 direction = player.transform.position - eyesPosition;
            // For the angle of vision, we don't care about vertical angle differences, only the planar orientation
            Vector3 directionFlatedOut = new Vector3(direction.x, 0f, direction.z);
            float angle = Vector3.Angle(directionFlatedOut, transform.forward);

            // If the angle between forward and where the player is, is less than half the angle of view...
            if (angle < fieldOfViewAngle * 0.5f) {
                RaycastHit hit;
                if (Physics.Raycast(eyesPosition, direction.normalized, out hit, visionDistance)) {
                    if (hit.collider.gameObject == player) {
                        //is the player lighted enough to be seen?
                        if (player.GetComponent<sr_Player>().lightAtPlayerPos > visionDarkTreshold) {
                            // Save this position as the last player sighting
                            lastPlayerSighting = player.transform.position;
                            //return vision
                            return true;

                            //chara.myAnimator.SetTrigger("scream"); //only once detected
                        }
                    }
                }
            }
        }
        //no vision
        return false;
    }



    public void LaunchAttackOnPlayer ()
    {
        //reduce character controller collision capsule?
        chara.isPathfindingFlyMode = true;
        chara.SetRun(true);
        isAttacking = true;
        chara.myAnimator.SetTrigger("takeOff");
    }



}
