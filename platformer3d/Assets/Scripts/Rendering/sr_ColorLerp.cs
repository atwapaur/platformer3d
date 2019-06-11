using UnityEngine;
using System.Collections;

public class sr_ColorLerp : MonoBehaviour {
	
	private static Color red = new Color (255/255f, 120/255f, 120/255f, 1);
	private static Color green = new Color (120/255f, 255/255f, 120/255f, 1);
	private static Color blue = new Color (120/255f, 120/255f, 255/255f, 1);
	private Color[] colors = new Color[] {Color.white, red, green, blue};
	private float timer = 0f;
	private int multiplicator = 1;
	private Color fromColor;
	private Color toColor;
	
	void Awake () {
		fromColor = colors[0];
		toColor = colors[Random.Range(0, colors.Length)];
	}
	
	// Update is called once per frame
	void Update () {
		timer += Time.deltaTime * multiplicator;
		GetComponent<Renderer>().material.color = Color.Lerp(fromColor, toColor, timer);
		if (timer > 1) {
			timer = 0f;
			multiplicator = Random.Range(1,4);
			fromColor = toColor;
			toColor = colors[Random.Range(0, colors.Length)];
		}
	}
}
