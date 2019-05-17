using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class MovingFade : MonoBehaviour {

	public Vector2 dir = Vector2.up;

	[Range(0.1f, 7)]
	public float speed;
	public float duration = 1f;
	public AnimationCurve curve;
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
		//start fading
		StopAllCoroutines();
		StartCoroutine(Animation());
		
	}

	void OnDisable()
	{
		StopAllCoroutines();
	}

	IEnumerator Animation() {
		float time = 0;
		var curTrans = transform;
	
		var color = txt.color;
		color.a = 0;
		txt.color = color;
		var charArray = txt.text.ToCharArray();
		var numChars = charArray.Length;
		var fullText = txt.text;

		while(time < duration) {
			float progress = time / duration;
			curTrans.position = curTrans.position + (Vector3)(dir * speed * Time.deltaTime);

			if(Random.value > 0.5 && substitutes != null) {
				var replaceCharIndex = (int)(Random.value * numChars);
				if(replaceCharIndex >= numChars) replaceCharIndex = numChars - 1;
				if(substitutes != null && substitutes.Length > replaceCharIndex)
					txt.text = fullText.Replace(charArray[replaceCharIndex].ToString(), substitutes[replaceCharIndex]);
			}

			color = txt.color;
			color.a = curve.Evaluate(progress);
			txt.color = color;

			yield return null;
			time += Time.deltaTime;
		}

		enabled = false;
		color.a = 1;
		txt.color = color;

		txtManager = null;
		substitutes = null;
		if(cb != null) cb.Invoke(gameObject);
	}

}
