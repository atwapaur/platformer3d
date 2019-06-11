using UnityEngine;
using System.Collections;

public class sr_Worm : MonoBehaviour {

    public float attackDistance = 20f;
    public float attackSpeed = 1f;
    public float returnSpeed = 2.5f;
    public float cooldown = 3f;

    public float bodyFollowSpeed = 4f;

	public GameObject[] bodyparts;
    public Vector3[] offsets;

    private bool isPlayerInZone = false;
    private bool isAtking = false;
    private bool isReturning = false;
    private Transform player;
    private Animation myAnimation;
    private Transform myParent;


    // Use this for initialization
    void Start () {
        player = GameObject.Find("Player").transform;
        myAnimation = GetComponent<Animation>();
        myParent = bodyparts[0].transform.parent;
    }
	
	// Update is called once per frame
	void Update () {

        if (!isAtking) {
            
            if (isPlayerInZone) {
            //if (Vector3.Distance(transform.position, player.position) <= attackDistance) {
                isAtking = true;
                myAnimation.Stop();
                //StartCoroutine("WormAttack");
                StartCoroutine("WormAttack");
            }
        }

        BodyFollowAnimation();

        //parent look at
        //Quaternion lookAtPlayerAxis = Quaternion.LookRotation(player.position - transform.position, Vector3.up);
        //myParent.rotation = Quaternion.Euler(myParent.eulerAngles.x, lookAtPlayerAxis.eulerAngles.y, myParent.eulerAngles.z);

    }

    private IEnumerator WormAttack2 ()
    {
        Vector3 basePos = transform.position;
        Vector3 attackTarget = player.position;

        Quaternion baseRot = transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(attackTarget - basePos);
        transform.rotation = targetRot;

        float t = 0f;
        while (t < 1) {
            t += Time.deltaTime/attackSpeed;
            transform.position = Vector3.Lerp(basePos, attackTarget, t);

            Quaternion updatedRot = Quaternion.LookRotation(player.position - transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, updatedRot, Time.deltaTime * 0.5f); //attackUpdateSpeed
            yield return 0;
        }

        t = 0f;
        isReturning = true;
        /*while (t < 1) {
            t += Time.deltaTime / returnSpeed;
            transform.position = Vector3.Lerp(attackTarget, basePos, t);
            transform.rotation = Quaternion.Lerp(targetRot, baseRot, t);
            yield return 0;
        }
        isReturning = false;*/

        //myAnimation.Play();
        yield return new WaitForSeconds(cooldown);
        
        isAtking = false;
    }


    private IEnumerator WormAttack()
    {
        Vector3 basePos = transform.position;
        Vector3 attackTarget = player.position;
        Quaternion targetRot = Quaternion.LookRotation(attackTarget - basePos);
        transform.rotation = targetRot;

        float t = 0f;
        while (t < 1) {
            t += Time.deltaTime / attackSpeed;

            Quaternion updatedRot = Quaternion.LookRotation(player.position - transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, updatedRot, Time.deltaTime * 20f); //attackUpdateSpeed

            transform.Translate(transform.forward * Time.deltaTime * 20f);

            yield return 0;
        }

        t = 0f;
        isReturning = true;
        /*while (t < 1) {
            t += Time.deltaTime / returnSpeed;
            transform.position = Vector3.Lerp(attackTarget, basePos, t);
            transform.rotation = Quaternion.Lerp(targetRot, baseRot, t);
            yield return 0;
        }
        isReturning = false;*/

        //myAnimation.Play();
        yield return new WaitForSeconds(cooldown);

        isAtking = false;
    }


    private void BodyFollowAnimation()
    {
        if (!isReturning) {
            for (int i = 1; i < bodyparts.Length; i++) {
                //bodyparts[i].transform.position = Vector3.Lerp (bodyparts[i].transform.position, new Vector3(bodyparts[i-1].transform.position.x, bodyparts[i].transform.position.y, bodyparts[i-1].transform.position.z) , Time.deltaTime*3);

                Vector3 localOffset = bodyparts[i].transform.parent.InverseTransformVector(offsets[i]);
                bodyparts[i].transform.position = Vector3.Lerp(bodyparts[i].transform.position,
                    new Vector3(bodyparts[i - 1].transform.position.x + localOffset.x, bodyparts[i - 1].transform.position.y + localOffset.y, bodyparts[i - 1].transform.position.z + localOffset.z), Time.deltaTime * bodyFollowSpeed);
                //Vector3 dir = ((bodyparts[i - 1].transform.position + localOffset) - bodyparts[i - 1].transform.position).normalized;
                Vector3 dir = (bodyparts[i].transform.position - bodyparts[i - 1].transform.position).normalized;
                bodyparts[i].transform.rotation = Quaternion.Lerp(bodyparts[i].transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * bodyFollowSpeed);
            }
        } else {
            Vector3 localOffsetCumuled = Vector3.zero;
            for (int i = 0; i < bodyparts.Length; i++) {
                localOffsetCumuled += offsets[i];
                bodyparts[i].transform.localPosition = Vector3.Lerp(bodyparts[i].transform.localPosition, localOffsetCumuled, Time.deltaTime * returnSpeed);
            }
            if (Vector3.Distance(bodyparts[0].transform.position, bodyparts[0].transform.parent.position) <= 1f) {
                isReturning = false;
                myAnimation.Play();
            }
        }
    }


    void OnTriggerEnter (Collider collider)
    {
        if (collider.tag == "Player") {
            Debug.Log("PLAYER EATEN!");
            StopCoroutine("WormAttack");
            isReturning = true;

            collider.gameObject.GetComponent<sr_Player>().DeathByAnchorWorm(transform); 
        }
    }

    public void GuardingZonePlayerEnter()
    {
        isPlayerInZone = true;
    }

    public void GuardingZonePlayerExit ()
    {
        isPlayerInZone = false;
    }
}
