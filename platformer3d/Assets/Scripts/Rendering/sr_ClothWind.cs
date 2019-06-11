using UnityEngine;
using System.Collections;

public class sr_ClothWind : MonoBehaviour {
	
	private Cloth cloth;
	private float timer = 0f;
	
	void Awake () {
		cloth = transform.GetComponent<Cloth>();
	}
	
	void Update () {
		/*timer += Time.deltaTime;
		if (timer > 4f) {
			timer = 0f;	
			float randomValue = Random.value;
			float output = Mathf.Lerp(-3, 3, randomValue);
			Debug.Log("wind output == "+output);
			cloth.randomAcceleration = new Vector3(0, 0, 2);
			cloth.externalAcceleration = new Vector3(0, 0, output);
			
			//cloth.randomAcceleration = transform.right.normalized * 2;
			//cloth.externalAcceleration = transform.right.normalized * output;
		}*/
		
		
	}
}
