using UnityEngine;
using System.Collections;

public class sr_CameraControl : MonoBehaviour {

    private GameObject player;
    private sr_Movement playerMovement;
    private sr_Main main;

    private float camDistance = 7f; //6.5f
    private float camHeight = 4f;
    private float camAngleTilt = 10f; //20f
    private float correctedDistance;

    private Vector3 vinesLastNormal = Vector3.zero;
    private bool isVinesCamLerping = false;

    void Awake () {
        player = GameObject.Find("Player");
        playerMovement = player.GetComponent<sr_Movement>();
        main = GameObject.Find("MAIN").GetComponent<sr_Main>();
	}

    void Start ()
    {
        //Quaternion lookAtPlayerBack = 
        transform.rotation = Quaternion.Euler(camAngleTilt, player.transform.eulerAngles.y, 0);
        correctedDistance = camDistance;
    }
	

    //different speed for move and rotate depending on delta
    private IEnumerator LerpVinesCamera ()
    {
        isVinesCamLerping = true;

        Vector3 previousPos = transform.position;
        Vector3 targetPos = player.transform.position + vinesLastNormal * camDistance;

        Quaternion previousRot = transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(-vinesLastNormal);

        float t = 0f;
        while (t < 1) {
            t += Time.deltaTime / 1.5f;
            targetPos = player.transform.position + vinesLastNormal * camDistance;
            transform.position = Vector3.Lerp(previousPos, targetPos, t);
            transform.rotation = Quaternion.Lerp(previousRot, targetRot, t);
            yield return 0;
        }
        isVinesCamLerping = false;
    }


    

    


    



    public void ResetTilt ()
    {
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, transform.eulerAngles.z);
    }


    void Update () {
        /*if (playerMovement.m_GrabbingVines) {

            //While moving, auto cam
            if (playerMovement.m_Input.magnitude > 0.1f) {
                //new surface!
                if (playerMovement.vinesNormal != vinesLastNormal) {
                    vinesLastNormal = playerMovement.vinesNormal;
                    StopCoroutine("LerpVinesCamera");
                    StartCoroutine("LerpVinesCamera");

                }
                //same surface...
                else {
                    if (!isVinesCamLerping) {
                        //Set camera to maintain distance to the plane/normal and follow the player
                        Vector3 targetPos = player.transform.position + vinesLastNormal * camDistance;
                        transform.position = targetPos;
                    }
                }
            }else {
                //While not moving, 'free look'

            }

            //Camera vision collision
            RaycastHit hitt;
            Vector3 playerPos = player.transform.position;
            //use target to cam vector, or use cam.forward ??
            if (Physics.Raycast(playerPos, (transform.position - playerPos).normalized, out hitt, camDistance)) {
                correctedDistance = hitt.distance - 0.5f;
            } else {
                correctedDistance = Mathf.Lerp(correctedDistance, camDistance, Time.deltaTime * 4f);
            }

            //very important line
            transform.position = playerPos + (transform.position - playerPos).normalized * correctedDistance; //* 1.5f;

            return;
        }*/




        Vector3 camAbsoluteForward = Vector3.Cross(transform.right, Vector3.up);
        Vector3 camLockPos = player.transform.position - camAbsoluteForward * camDistance + Vector3.up * camHeight;

        if (playerMovement.m_GrabbingVines) {
            camLockPos = player.transform.position - camAbsoluteForward * camDistance; //- Vector3.up * camHeight;
        }

        //transform.position = Vector3.Lerp(transform.position, camLockPos, Time.deltaTime * 4f);
        transform.position = camLockPos;



        float horizontal = Input.GetAxis("Cam X") * 3.5f; //5f

        Vector3 target = player.transform.position;
        //Vector3 target = player.transform.position + Vector3.up * heightOffset;

        transform.RotateAround(target, Vector3.up, horizontal);




        //IF NOT MOVING / JUMPING ? ...
        if (!playerMovement.m_Airborne && playerMovement.m_Input.magnitude <= 0.1f) {
            float vertical = Input.GetAxis("Cam Y") * 1.5f;

            float angleX = transform.eulerAngles.x;
            bool xRotAuthorized = false;

            if (vertical < 0) {
                if (angleX < 45 || angleX > 180) { //30
                    xRotAuthorized = true;
                }
            } else if (vertical > 0) {
                if (angleX > 300 || angleX < 180) { //320
                    xRotAuthorized = true;
                }
            }

            if (xRotAuthorized) {
                transform.RotateAround(target, transform.right, -vertical);
            }
        }
        else {
            if (playerMovement.m_GrabbingVines == false && playerMovement.isCatchingPrey == false) {
                //auto tilt cam to X rot 30
                Quaternion desiredRot = Quaternion.Euler(camAngleTilt, transform.eulerAngles.y, transform.eulerAngles.z);

                //accentuate the tilt towards the fall the more important is playerMovement.m_MoveDir.y
                if (playerMovement.m_MoveDir.y < Physics.gravity.magnitude) {
                    desiredRot = Quaternion.Euler(camAngleTilt - (playerMovement.m_MoveDir.y / 2), transform.eulerAngles.y, transform.eulerAngles.z);
                }

                //Kinda do a tilt up impulsion in reverse inclination: interesting
                //if (playerMovement.m_MoveDir.y > Physics.gravity.magnitude) {
                //    desiredRot = Quaternion.Euler(30f + (playerMovement.m_MoveDir.y/2), transform.eulerAngles.y, transform.eulerAngles.z);
                //}

                transform.rotation = Quaternion.Lerp(transform.rotation, desiredRot, Time.deltaTime * 6f);
            }
            else {
                
            }
        }

        //CHANGES FOCUS !!!!!!!!!!!!!!!!
        //transform.LookAt(target);


        if (playerMovement.m_GrabbingVines) {
            //target -= player.transform.forward; //problem, it makes him collide with himself maybe ?
        }
        
        //Camera vision collision
        RaycastHit hit;
        bool dontupdateCamDist = false;
        //use target to cam vector, or use cam.forward ??
        if (Physics.Raycast(target, (transform.position - target).normalized, out hit, camDistance)) {
            correctedDistance = hit.distance - 0.5f;
            if (hit.transform.tag == "Prey") {
                dontupdateCamDist = true;
            }
        } else {
            correctedDistance = Mathf.Lerp(correctedDistance, camDistance, Time.deltaTime * 4f);
        }

        
        if (!dontupdateCamDist) {
            //very important line
            transform.position = target + (transform.position - target).normalized * correctedDistance; //* 1.5f;
        }
        
        
    }

}
