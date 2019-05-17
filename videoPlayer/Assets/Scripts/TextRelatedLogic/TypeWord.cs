using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class TypeWord : MonoBehaviour {
	
	public float duration = 6f;
	public float typeInterval = 0.3f;

	public UnityAction<GameObject> cb;

	[HideInInspector]
	public TextManager.Orientation orientation;

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
		//start fading
		StopAllCoroutines();
		StartCoroutine(Animation());
	}

	void OnDisable()
	{
		StopAllCoroutines();
	}

	IEnumerator Animation() {
		if(Mathf.Approximately(duration, 0)) yield break;
		if(orientation == TextManager.Orientation.Horizontal) {
			txt.alignment = TextAnchor.MiddleLeft;
		}
		else {
			txt.alignment = TextAnchor.UpperCenter;
		}

		var fullText = txt.text;
		var randVal = Random.value;
		float waitTime = typeInterval + randVal * 0.03f;
		WaitForSeconds wait = new WaitForSeconds(waitTime);
		float timer = 0;
		var charArray = fullText.ToCharArray();
		int numChars = charArray.Length;

		while(timer < duration) {
			var progress = timer / duration;
			var fixedNum = (int)(progress * numChars);
			randVal = Random.value;
			
			var txtPtr = fullText;
			if(randVal > 0.5 && substitutes != null) {
				var replaceCharIndex = (int)(Random.value * numChars);
				if(replaceCharIndex >= numChars) replaceCharIndex = numChars - 1;
				if(substitutes != null && substitutes.Length > replaceCharIndex)
					txtPtr = fullText.Replace(charArray[replaceCharIndex].ToString(), substitutes[replaceCharIndex]);
			}
			
			int numCharsToShow = fixedNum + (int)(randVal * Random.Range(1,4));
			if(numCharsToShow > numChars) numCharsToShow = numChars;
			txt.text = txtPtr.Substring(0, numCharsToShow);
			
			yield return wait;
			timer += waitTime;
		}

		txtManager = null;
		substitutes = null;
		enabled = false;
		if(cb != null) cb.Invoke(gameObject);
	}


}
