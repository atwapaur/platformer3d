using UnityEngine;
using System.Collections;

public class sr_Atmosphere : MonoBehaviour {

	public bool isThunder;
	public Light[] lights;
	public GameObject[] godrays;
	public GameObject[] windows;

	private float thunderTimer = 0f;
	private float thunderInterval = 0f;

	

	void Start () {
		if (isThunder) {
			NewThunderInterval();
			SwitchLights(false);
		}
	}
	
	
	void Update () {
		if (isThunder) {
			thunderTimer += Time.deltaTime;
			if (thunderTimer >= thunderInterval) {
				StartCoroutine("Thunder");
				thunderTimer = 0f;
				NewThunderInterval();
			}
		}
	}

	private void SwitchLights (bool target) {
		foreach (Light l in lights) {
			l.enabled = target;
		}
	}

	private void GodraysThunder (bool doThunder) {
		float targetAlpha = 32f/255f;
		if (doThunder) {
			targetAlpha = 71f/255f;
		}
		foreach (GameObject gr in godrays) {
			Color c = gr.GetComponent<Renderer>().material.GetColor("_TintColor");
			c.a = targetAlpha;
			gr.GetComponent<Renderer>().material.SetColor("_TintColor", c);
		}
	}

	private void WindowsThunder (bool doThunder) {
		Color target = new Color(128f/255f, 128f/255f, 109f/255f, 1f);
		if (doThunder) {
			target = new Color(251f/255f, 1f, 180f/255f, 1f);
		}
		foreach (GameObject w in windows) {
			w.GetComponent<Renderer>().material.color = target;
		}
	}

	private void NewThunderInterval () {
		thunderInterval = (float)Random.Range (4,8); //6,18
	}

	
	private IEnumerator Thunder () {
		//audiosource2D.PlayOneShot(soundThunder);
		
		int nbr = Random.Range (2,6);
		
		for (int i=0; i<nbr; i++) {
			//RenderSettings.ambientLight = Color.white;
			//Camera.main.backgroundColor = Color.white;
			SwitchLights(true);
			GodraysThunder(true);
			WindowsThunder(true);
			yield return new WaitForSeconds(0.1f);
			//RenderSettings.ambientLight = new Color (51f/255f, 51f/255f, 51f/255f, 1f);
			RenderSettings.ambientLight = Color.black;
			//Camera.main.backgroundColor = Color.black;
			SwitchLights(false);
			GodraysThunder(false);
			WindowsThunder(false);
			yield return new WaitForSeconds(Random.Range (0.02f, 0.25f));
		}
	}

}
