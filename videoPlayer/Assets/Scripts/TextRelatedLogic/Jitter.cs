using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Jitter : MonoBehaviour {


	public float duration = 3f;
	public float factor = 0.5f;

	public UnityAction<GameObject> cb;
	public TextManager txtManager;
	public string[] substitutes;
	private Text txt;

	/// <summary>
	/// Start is called on the frame when a script is enabled just before
	/// any of the Update methods is called the first time.
	/// </summary>
	void Awake()
	{
		txt = gameObject.GetComponent<Text>();
	}

	/// <summary>
	/// This function is called when the object becomes enabled and active.
	/// </summary>
	void OnEnable()
	{
		StopAllCoroutines();
		StartCoroutine(JitterAnim());
	}

	void OnDisable()
	{
		StopAllCoroutines();
	}

	IEnumerator JitterAnim() {
		var curTrans = transform;
		float time = 0;
		var originalPos = curTrans.position;
		var charArray = txt.text.ToCharArray();
		var numChars = charArray.Length;
		var fullText = txt.text;

		while(time < duration) {
			curTrans.position = originalPos + new Vector3(Random.value * factor, Random.value * factor, 0);
			
			if(Random.value > 0.5 && substitutes != null) {
				var replaceCharIndex = (int)(Random.value * numChars);
				if(replaceCharIndex >= numChars) replaceCharIndex = numChars - 1;
				if(substitutes != null && substitutes.Length > replaceCharIndex)
					txt.text = fullText.Replace(charArray[replaceCharIndex].ToString(), substitutes[replaceCharIndex]);
			}
			
			yield return null;
			time += Time.deltaTime;
		}

		txtManager = null;
		substitutes = null;
		enabled = false;
		if(cb != null) cb.Invoke(gameObject);
	}

}
