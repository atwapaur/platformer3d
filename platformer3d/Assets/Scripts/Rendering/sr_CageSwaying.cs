using UnityEngine;
using System.Collections;

public class sr_CageSwaying : MonoBehaviour {

	private bool switchDirection = false;
	private float switchTimer = 0f;
	private float swayTimer = 0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		//float rot = Mathf.PingPong
		float target = 6f;
		float angle = Mathf.PingPong(Time.time, target);

		switchTimer += Time.deltaTime;
		if (switchTimer >= 8) {
			switchTimer = 0f;
			target = -target;
		}

		transform.Rotate(Vector3.up, angle * Time.deltaTime);

		//Swaying
		float targetSway = 3f;
		float angleSway = Mathf.PingPong(Time.time, targetSway);
		
		swayTimer += Time.deltaTime;
		if (swayTimer >= 3) {
			swayTimer = 0f;
			targetSway = -targetSway;
		}
		
		transform.Rotate(transform.forward, angleSway * Time.deltaTime);
	}
}
