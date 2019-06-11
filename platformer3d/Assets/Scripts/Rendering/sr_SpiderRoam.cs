using UnityEngine;
using System.Collections;

public class sr_SpiderRoam : MonoBehaviour {

	private Vector3 desti;
	private float waitTimer = 0f;
	private bool isTranslating = false;


	void Start () {
		float size = Random.Range (0.6f, 0.8f);
		transform.localScale = new Vector3(size, size, 1);
		FindDesti();
	}


	void Update () {

		if (waitTimer > 0f) {
			waitTimer -= Time.deltaTime;
			return;
		}

		if (!isTranslating) {
			//do we wait a bit before translating to another pos?
			float waitChances = Random.value;
			if (waitChances > 0.5f) {
				waitTimer = Random.Range(0f, 5f);
			}
			else {
				FindDesti();
			}

			isTranslating = true;
			StartCoroutine("Translation");
		}
	}


	private IEnumerator Translation () {
		float t = 0f;
		Vector3 origin = transform.position;
		while (t < 1) {
			t += Time.deltaTime/2f;
			//go to desti over time
			transform.position = Vector3.Lerp (origin, desti, t);
			yield return 0;
		}
		isTranslating = false;
	}


	private void FindDesti () {
		//find desti
		desti = transform.position + Vector3.right*Random.Range(-4f, 4f) + Vector3.forward*Random.Range(-4f, 4f);
		transform.rotation = Quaternion.LookRotation(desti - transform.position);
	}

}
