using UnityEngine;
using System.Collections;

public class Balancing : MonoBehaviour {

    public float z1;
    public float z2;
    public float x1;
    public float x2;

    private int seq = 0;

	// Use this for initialization
	void Start () {
        transform.rotation = Quaternion.Euler(x1, 0, z1);


        StartCoroutine("seq1");
	}
	
	
    private IEnumerator seq1 ()
    {
        Quaternion baseRot = transform.rotation;
        Quaternion target = Quaternion.Euler(x2, 0, z2);
        float t = 0f;
        while (t < 1) {
            t += Time.deltaTime / 3;
            transform.rotation = Quaternion.Lerp(baseRot, target, t);
            yield return 0;
        }
        StartCoroutine("seq2");
    }

    private IEnumerator seq2()
    {
        Quaternion baseRot = transform.rotation;
        Quaternion target = Quaternion.Euler(x1, 0, z1);
        float t = 0f;
        while (t < 1) {
            t += Time.deltaTime / 3;
            transform.rotation = Quaternion.Lerp(baseRot, target, t);
            yield return 0;
        }
        StartCoroutine("seq1");
    }

}
