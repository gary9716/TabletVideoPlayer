//#define EFFECT_TEST
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;

public class TextManager : KTEffectBase {

	public enum Orientation {
		Horizontal = 0,
		Vertical
	}

	public override MonoBehaviour GetEffect() {
		return this;
	}

	public TextAsset sentencesInfo;

	public Text txtPrefab;
	public Canvas rootCanvas;
	Vector3[] corners;

	public const string sentenceInputPathKey = "txtEffectInputTextFilePath";	

	public class SentenceData {
		public string main;
		public string[] substitutes;
	}

	public SentenceData[] sentenceDataArray;

	public void ProcessInputData(string all) {
		string[] lines = all.Split('\n');
		sentenceDataArray = new SentenceData[lines.Length];
		int lineIndex = 0;
		foreach(var line in lines) {
			string[] elements = line.Split(',');
			var data = new SentenceData();
			data.main = elements[0];
			data.substitutes = new string[elements.Length - 1];
			for(int i = 1;i < elements.Length;i++) {
				data.substitutes[i - 1] = elements[i];
			}
			sentenceDataArray[lineIndex] = data;
			lineIndex++;
		}
	}

	public void DespawnAll() {
		foreach(Transform trans in rootCanvas.transform) {
			LeanPool.Despawn(trans.gameObject);
		}
	}

	/// <summary>
	/// Start is called on the frame when a script is enabled just before
	/// any of the Update methods is called the first time.
	/// </summary>
	void Start() {
		string inputFilePath = PlayerPrefs.GetString(TextManager.sentenceInputPathKey, null);
		if(inputFilePath != null && System.IO.File.Exists(inputFilePath)) 
			ProcessInputData(System.IO.File.ReadAllText(inputFilePath));
		else
			ProcessInputData(sentencesInfo.text);

		corners = new Vector3[4];
		rootCanvas.GetComponent<RectTransform>().GetWorldCorners(corners);
		
		#if EFFECT_TEST && UNITY_EDITOR
		StartCoroutine(PeriodicallyShow(1));
		#endif
	}
	
	IEnumerator PeriodicallyShow(float interval) {
		
		while(true) {
			float timer = 0;
			while(timer < interval) {
				timer += Time.deltaTime;
				yield return null;
			}

			Invoke("ShowText", 0.5f);
		}
		
	}

	private void OnTextAnimationEnd(GameObject txtObj) {
		LeanPool.Despawn(txtObj);

	}

	public string GetRandSentence() {
		int index = (int)(sentenceDataArray.Length * Random.value);
		if(index == sentenceDataArray.Length) index = sentenceDataArray.Length - 1;
		var sentence = sentenceDataArray[index].main;

		return sentence;
	}

	public SentenceData GetRandSentenceData() {
		int index = (int)(sentenceDataArray.Length * Random.value);
		if(index == sentenceDataArray.Length) index = sentenceDataArray.Length - 1;
		return sentenceDataArray[index];
	}

	[ContextMenu("show")]
	public void ShowText() {
		
		float weight = Random.value;
		float randVal = Random.value;
		int effectIndex = randVal < 0.33f? 0: (randVal > 0.66f? 1:2); 
		ShowText(GetRandSentenceData(), 
			Random.value > 0.5? Orientation.Horizontal : Orientation.Vertical,
			(int)(22 * weight + 40 * (1 - weight)),
			-1, -1, effectIndex, null, false);
	}
	
	public void ShowText(SentenceData sentenceData, Orientation orientation, int fontSize, float posX, float posY, int effectIndex, List<object> paramList, bool useGlobalParam = false) {
		rootCanvas.enabled = true;

		var txt = LeanPool.Spawn(txtPrefab);
		txt.transform.SetParent(rootCanvas.transform, false);
		txt.transform.localScale = Vector3.one;
		txt.text = sentenceData.main;

		var size = txt.rectTransform.sizeDelta;
		txt.alignment = TextAnchor.MiddleCenter;

		if(fontSize > 0)
			txt.fontSize = fontSize;
		else
			fontSize = txt.fontSize;

		var pos = new Vector3();
		var val = Random.value;
		var range = val * 0.8f + 0.1f;	
		var sinVal = (Mathf.Sin(range * 3.14f) + 1)/2;
		//var range2 = sinVal * range;
		pos.y = corners[0].y * range + (corners[2].y) * (1 - range);
		pos.x = corners[0].x * range + (corners[2].x) * (1 - range);

		pos.x = posX >= 0? (corners[0].x * posX + corners[2].x * (1 - posX)):pos.x;
		pos.y = posY >= 0? (corners[0].y * posY + corners[2].y * (1 - posY)):pos.y;
		pos.z = 0;

		txt.transform.position = pos;

		if(orientation == Orientation.Horizontal) {
			size.y = fontSize + 2;
			txt.horizontalOverflow = HorizontalWrapMode.Overflow;
			txt.verticalOverflow = VerticalWrapMode.Overflow;
		}
		else {
			size.x = fontSize + 2;
			txt.horizontalOverflow = HorizontalWrapMode.Wrap;
			txt.verticalOverflow = VerticalWrapMode.Overflow;
		}
		txt.rectTransform.sizeDelta = size;

		int nextEffectIndex = effectIndex;
		float duration = 1;

		if(nextEffectIndex == 0) {
			var effect = txt.GetComponent<MovingFade>();
			int dirX = val > 0.5f? 1:-1;
			int	dirY = dirX != 0? 0 : (val > 0.5? 1 : -1);
			float speed = effect.speed;
			duration = effect.duration;
			effect.txtManager = this;
			effect.substitutes = sentenceData.substitutes;

			if(paramList != null) {
				for(int i = 0;i < paramList.Count;i++) {
					switch(i) {
						case 0:
							int.TryParse(paramList[i].ToString(), out dirX);
							break;
						case 1:
							int.TryParse(paramList[i].ToString(), out dirY);
							break;
						case 2:
							float.TryParse(paramList[i].ToString(), out speed);
							break;
						case 3:
							float.TryParse(paramList[i].ToString(), out duration);
							break;
					}
				}
			}
			else if(useGlobalParam) {
				duration = this.duration;
				dirX = (int)this.dirX;
				dirY = (int)this.dirY;
				speed = this.speed;
			}
			
			effect.speed = speed;
			effect.duration = duration;
			effect.dir = new Vector2(dirX, dirY);

			effect.cb += OnTextAnimationEnd;
			effect.enabled = true;
		}
		else if(nextEffectIndex == 1){
			var effect = txt.GetComponent<Jitter>();
			float factor = effect.factor;
			duration = effect.duration;
			effect.txtManager = this;
			effect.substitutes = sentenceData.substitutes;

			if(paramList != null) {
				for(int i = 0;i < paramList.Count;i++) {
					switch(i) {
						case 0:
							float.TryParse(paramList[i].ToString(), out factor);
							break;
						case 1:
							float.TryParse(paramList[i].ToString(), out duration);
							break;
					}
				}
			}
			else if(useGlobalParam) {
				duration = this.duration;
				factor = this.jitterFactor;
			}

			effect.factor = factor;
			effect.duration = duration;

			effect.cb += OnTextAnimationEnd;
			effect.enabled = true;
		}
		else if(nextEffectIndex == 2){
			var effect = txt.GetComponent<TypeWord>();
			effect.txtManager = this;
			effect.substitutes = sentenceData.substitutes;
			float interval = effect.typeInterval;
			duration = effect.duration;

			if(paramList != null) {
				for(int i = 0;i < paramList.Count;i++) {
					switch(i) {
						case 0:
							float.TryParse(paramList[i].ToString(), out interval);
							break;
						case 1:
							float.TryParse(paramList[i].ToString(), out duration);
							break;
						case 2:
							break;
						case 3:
							break;
					}
				}
			}
			else if(useGlobalParam) {
				duration = this.duration;
				interval = this.interval;
			}

			effect.duration = duration;
			effect.typeInterval = interval;

			effect.orientation = orientation;
			effect.cb += OnTextAnimationEnd;
			effect.enabled = true;
		}

	}

	int activeIndex = -1;
	int sentenceIndex = -1;
	float dirX;
	float dirY;
	float speed;
	float duration;
	float jitterFactor;
	float interval;
	bool isAnimating;
	Orientation orientation;
	int fontSize;
	float posX;
	float posY;

	public override bool isEffectActive {
		get {
			return isAnimating;
		}
	}

	public override void SetParameter(int index, float val)
	{
		switch(index) {
			case 0:
				activeIndex = (int)val;
				break;
			case 1:
				sentenceIndex = (int)val;
				break;
			case 2:
				orientation = (Orientation)((int)val);
				break;
			case 3:
				fontSize = (int)val;
				break;
			case 4:
				posX = val;
				break;
			case 5:
				posY = val;
				break;
			case 6:
				dirX = val;
				break;
			case 7:
				dirY = val;
				break;
			case 8:
				speed = val;
				break;
			case 9:
				duration = val;
				break;
			case 10:
				jitterFactor = val;
				break;
			case 11:
				interval = val;
				break;
				
		}
  }

	public override void SetEffectActive(bool enable) {
		base.SetEffectActive(enable);
		isAnimating = enable;
		if(enable) {
			if(activeIndex >= 0 && activeIndex < 3) {
				if(sentenceIndex < 0) {
					var newIndex = (int)Random.Range(0, sentenceDataArray.Length);
					if(newIndex == sentenceDataArray.Length) newIndex = sentenceDataArray.Length - 1;
					sentenceIndex = newIndex;
				}
				
				ShowText(sentenceDataArray[sentenceIndex], orientation, fontSize, posX, posY, activeIndex, null, true);
			}
			else {
				StartCoroutine(PeriodicallyShow(interval));
			}
		}
	}
}
