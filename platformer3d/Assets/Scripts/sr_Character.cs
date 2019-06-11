using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;

public class sr_Character : MonoBehaviour {

    public string defaultAnimatorState;
    public List<sr_Order> routines = new List<sr_Order>();
    public float walkSpeed = 3f;
    public float runSpeed = 8f;

    public Transform pathfinderZone;

    protected Seeker seeker;
    [HideInInspector] public CharacterController controller;
    protected Transform tr;
    private Transform body;
    [HideInInspector] public Animator myAnimator;
    //pathfinding variables
    //protected List<Vector3> path;
    protected List<Vector3> path;
    protected int pathIndex = 0;

    //pathfinding exposed
    public bool pathfindingWithoutGravity = false;
    public float pickNextWaypointDistance = 1f;
    public float targetReached = 0.2f;
    public float rotationSpeed = 10f;

    private float speed = 3f;
    private bool isMoving = false;

    private bool isOnRoutine = false;
    private int currentRoutine = 0;

    private sr_Order savedOrder = null;

    [HideInInspector]
    public bool isMovingToNode = false;

    [HideInInspector]
    public bool isPathfindingFlyMode = false;

    [HideInInspector]
    public bool isPathfindingNoRotationForward = false;

    [HideInInspector]
    public GameObject targetNode = null;
    private bool isParabolaJumping = false;

    private sr_Prey srPrey;
    private sr_Predator srPredator;


    void Awake () {
        seeker = GetComponent<Seeker>();
        controller = GetComponent<CharacterController>();
        tr = transform;
        body = tr.GetChild(0);
        myAnimator = body.GetComponent<Animator>();
        //myAnimator = GetComponent<Animator>();
    }

    void Start ()
    {
        speed = walkSpeed;

        if (!string.IsNullOrEmpty(defaultAnimatorState)) {
            myAnimator.Play(defaultAnimatorState);
        }

        //myAnimator.SetBool("isGameplay", true);
        //PathToTarget(targetPoint.position);

        if (routines.Count > 0) {
            StartRoutine();
        }
        
    }

    public void SetRun (bool run)
    {
        if (run) {
            speed = runSpeed;
        }else {
            speed = walkSpeed;
        }
    }

    public void SetAnimatorTrigger (string trigID)
    {
        myAnimator.SetTrigger(trigID);
    }

    private void StartRoutine ()
    {
        currentRoutine = Random.Range(0, routines.Count);
        routines[currentRoutine].ExecuteOrder();
    }


    //void Update () {
    public virtual void Update()
    {
        if (isPathfindingFlyMode) {
            FlyMovement();
        }else if (isPathfindingNoRotationForward) {
            Movement_NoForwardRotation();
        }else {
            Movement();
        }
        

        //myAnimator.SetBool("isMoving", isMoving);
    }


    public void OrderPathNoRotation (Vector3 targetPoint)
    {
        isPathfindingNoRotationForward = true;
        PathToTarget(targetPoint);
    }

    public void OrderPathToTarget (Vector3 targetPoint, sr_Order order)
    {
        PathToTarget(targetPoint);
        savedOrder = order;

        isMovingToNode = true;
    }


    //-------------------------- PATHFINDING -------------------------//
    public void PathToTarget(Vector3 targetPoint)
    {
        if (seeker == null)
        {
            return;
        }
        seeker.StartPath(transform.position, targetPoint, OnPathComplete);
        myAnimator.SetFloat("moveSpeed", 1f);
        myAnimator.SetTrigger("doMove");
    }

    public void OnPathComplete(Path p)
    {
        //If the path didn't succeed, don't proceed
        if (p.error)
        {
            return;
        }
        //Get the calculated path as a Vector3 array
        path = p.vectorPath;

        //Find the segment in the path which is closest to the AI
        //If a closer segment hasn't been found in '6' iterations, break because it is unlikely to find any closer ones then
        float minDist = Mathf.Infinity;
        int notCloserHits = 0;

        for (int i = 0; i < path.Count - 1; i++)
        {
            float dist = AstarMath.DistancePointSegmentStrict(path[i], path[i + 1], tr.position);
            if (dist < minDist)
            {
                notCloserHits = 0;
                minDist = dist;
                pathIndex = i + 1;
            }
            else if (notCloserHits > 6)
            {
                break;
            }
        }
    }

    //Called when the AI reached the end of path.
    public virtual void ReachedEndOfPath()
    {
        //Debug.Log ("AI Reached End of Path");
        //animParent.GetComponent<sr_AnimationEventHandler>().TurnOn(animID);

        if (savedOrder != null) {
            savedOrder.Callback();
            //myAnimator.SetTrigger("fireIdle");
        }
        myAnimator.SetFloat("moveSpeed", 0f);
        if (GetComponent<sr_Predator>() != null) {
            myAnimator.SetTrigger("grounded");
        }

        //Reaching a NODE / SPAWN PNT etc
        if (isMovingToNode) {
            isMovingToNode = false;

            if (targetNode.GetComponent<sr_NodeConnection>()) {
                sr_NodeConnection connect = targetNode.GetComponent<sr_NodeConnection>();
                connect.startPoint = transform.position;
                StartCoroutine("ParabolaJump", connect);
                isParabolaJumping = true;

                Quaternion lookAtRot = Quaternion.LookRotation((connect.highPoint.position - transform.position), Vector3.up);
                lookAtRot = Quaternion.Euler(0, lookAtRot.eulerAngles.y, 0);
                transform.rotation = lookAtRot;
                GetComponent<CharacterController>().enabled = false;
            }
        }

        if (GetComponent<sr_Prey>() != null) {
            sr_Prey prey = GetComponent<sr_Prey>();
            if (prey.isFleeing) {
                prey.RunAwayTargetReached();
            }
        }
    }




    private void Movement() //Movement_GroundedForward
    {
        if (path == null || pathIndex >= path.Count || pathIndex < 0)
        {
            isMoving = false;
            //myAnimator.SetFloat("moveSpeed", 0f);
            return;
        }
        isMoving = true;
        //Change target to the next waypoint if the current one is close enough
        Vector3 currentWaypoint = path[pathIndex];
        currentWaypoint.y = tr.position.y;
        
        while ((currentWaypoint - tr.position).sqrMagnitude < pickNextWaypointDistance * pickNextWaypointDistance)
        {
            pathIndex++;
            if (pathIndex >= path.Count)
            {
                //Use a lower pickNextWaypointDistance for the last point. If it isn't that close, then decrement the pathIndex to the previous value and break the loop
                if ((currentWaypoint - tr.position).sqrMagnitude < (pickNextWaypointDistance * targetReached) * (pickNextWaypointDistance * targetReached))
                {
                    ReachedEndOfPath();
                    return;
                }
                else
                {
                    pathIndex--;
                    //Break the loop, otherwise it will try to check for the last point in an infinite loop
                    break;
                }
            }
            currentWaypoint = path[pathIndex];
            currentWaypoint.y = tr.position.y;
        }

        Vector3 dir = currentWaypoint - tr.position;

        // Rotate towards the target
        tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
        tr.eulerAngles = new Vector3(0, tr.eulerAngles.y, 0);

        Vector3 forwardDir = transform.forward;
        //Move Forwards - forwardDir is already normalized
        forwardDir = forwardDir * speed;
        forwardDir *= Mathf.Clamp01(Vector3.Dot(dir.normalized, tr.forward));

        controller.SimpleMove(forwardDir);
        
    }


    private void Movement_NoForwardRotation() //Movement_GroundedForward
    {
        if (path == null || pathIndex >= path.Count || pathIndex < 0) {
            isMoving = false;
            return;
        }
        isMoving = true;
        //Change target to the next waypoint if the current one is close enough
        Vector3 currentWaypoint = path[pathIndex];
        currentWaypoint.y = tr.position.y;

        while ((currentWaypoint - tr.position).sqrMagnitude < pickNextWaypointDistance * pickNextWaypointDistance) {
            pathIndex++;
            if (pathIndex >= path.Count) {
                //Use a lower pickNextWaypointDistance for the last point. If it isn't that close, then decrement the pathIndex to the previous value and break the loop
                if ((currentWaypoint - tr.position).sqrMagnitude < (pickNextWaypointDistance * targetReached) * (pickNextWaypointDistance * targetReached)) {
                    ReachedEndOfPath();
                    return;
                } else {
                    pathIndex--;
                    //Break the loop, otherwise it will try to check for the last point in an infinite loop
                    break;
                }
            }
            currentWaypoint = path[pathIndex];
            currentWaypoint.y = tr.position.y;
        }

        Vector3 dir = currentWaypoint - tr.position;

        Vector3 moveDir = dir.normalized * speed;

        controller.SimpleMove(moveDir);

    }


    // FLYING PATHFINDING VARIANT
    private void FlyMovement()
    {
        if (path == null || pathIndex >= path.Count || pathIndex < 0) {
            isMoving = false;
            return;
        }
        isMoving = true;
        //Change target to the next waypoint if the current one is close enough
        Vector3 currentWaypoint = path[pathIndex];

        while ((currentWaypoint - tr.position).sqrMagnitude < pickNextWaypointDistance * pickNextWaypointDistance) {
            pathIndex++;
            if (pathIndex >= path.Count) {
                //Use a lower pickNextWaypointDistance for the last point. If it isn't that close, then decrement the pathIndex to the previous value and break the loop
                if ((currentWaypoint - tr.position).sqrMagnitude < (pickNextWaypointDistance * targetReached) * (pickNextWaypointDistance * targetReached)) {
                    ReachedEndOfPath();
                    return;
                } else {
                    pathIndex--;
                    //Break the loop, otherwise it will try to check for the last point in an infinite loop
                    break;
                }
            }
            currentWaypoint = path[pathIndex];
        }

        Vector3 dir = currentWaypoint - tr.position;

        // Rotate towards the target
        tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
        tr.eulerAngles = new Vector3(0, tr.eulerAngles.y, 0);

        Vector3 moveDir = dir.normalized * speed;
        //moveDir *= Mathf.Clamp01(Vector3.Dot(dir.normalized, tr.forward));

        controller.Move(moveDir * Time.deltaTime);

    }




    private IEnumerator ParabolaJump (sr_NodeConnection connect)
    {
        float jumpLength = Vector3.Distance(connect.highPoint.position, connect.targetPoint.position);

        float t = 0f;
        while (t < 1) {
            t += Time.deltaTime * (jumpLength/40);
            transform.position = connect.Parabola(t);
            yield return 0;
        }
    }


    public void InterruptMovement ()
    {
        path = null;
        isMoving = false;
    }




    
    public void FixRoutines ()
    {
        Debug.Log("Assigning self in routines orders of certain types... ");

        if (routines.Count > 0) {
            for (int i=0; i<routines.Count; i++) {
                FixOrder(routines[i]);
            }
        }
    }

    private void FixOrder(sr_Order order)
    {
        //fix it
        order.TryFixingFor(gameObject);

        //order the same for following / parallel orders
        if (order.parallelOrders.Length > 0) {
            for (int i = 0; i < order.parallelOrders.Length; i++) {
                FixOrder(order.parallelOrders[i]);
            }
        }

        if (order.nextOrder != null) {
            FixOrder(order.nextOrder);
        }
    }


    void OnDrawGizmos()
    {
        foreach (sr_Order order in routines) {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, order.transform.position);
        }

    }


}
