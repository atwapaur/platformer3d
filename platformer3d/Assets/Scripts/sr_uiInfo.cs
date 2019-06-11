using UnityEngine;
using System.Collections;

public class sr_uiInfo : MonoBehaviour {

	public int index = -1;
	public string title;
	public string caption;

	public void WipeOutInfo () {
		index = -1;
		title = "";
		caption = "";
	}
	
}
