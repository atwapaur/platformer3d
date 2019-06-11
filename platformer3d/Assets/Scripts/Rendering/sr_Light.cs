using UnityEngine;
using System.Collections;

public class sr_Light : MonoBehaviour {
	
	public bool blink;
    public float intensityMin;
    public float intensityMax;
    public float approxVaryingTime;
	
	private float originalIntensity;
	private float timer = 0f;
	private float blinkTimer = 0f;

    private Light thisLight;
    private bool isRisingIntensity = true;
	
	void Start () {
        thisLight = GetComponent<Light>();
		originalIntensity = GetComponent<Light>().intensity;
		blinkTimer = Random.Range(1f, 5f);

        StartCoroutine(IntensityVariation(intensityMin, intensityMax, approxVaryingTime * Random.Range(0.7f, 1.4f)));
	}
	
    private IEnumerator IntensityVariation (float start, float target, float duration)
    {
        float t = 0f;
        while (t < 1) {
            t += Time.deltaTime / duration;
            thisLight.intensity = Mathf.Lerp(start, target, t);
            yield return 0;
        }
        NextVary();
    }

    private void NextVary ()
    {
        isRisingIntensity = !isRisingIntensity;
        if (isRisingIntensity) {
            StartCoroutine(IntensityVariation(intensityMin, intensityMax, approxVaryingTime * Random.Range(0.7f, 1.4f)));
        } else {
            StartCoroutine(IntensityVariation(intensityMax, intensityMin, approxVaryingTime * Random.Range(0.7f, 1.4f)));
        }
    }

	void Update () {
		if (blink) {
			timer += Time.deltaTime;
			if (timer >= blinkTimer) {
				GetComponent<Light>().intensity = 0;
				if (timer >= blinkTimer + 0.2f) {
					GetComponent<Light>().intensity = originalIntensity;
					timer = 0f;
					blinkTimer = Random.Range(1f, 5f);
				}
			}
		}
	}
	
}
