using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageAnimator : KTEffectBase {

	public Image img;
	
	private int activeIndex = -1;
	private bool isAnimating = false;

	//effect0
	private float minA;
	private float maxA;
	private float attackDuration;
	private int attackFuncIndex;
	private float decayDuration;
	private int decayFuncIndex;

	//effect1 
	private float flashingInterval; //flip(minA->maxA, maxA->minA) interval

	public override bool isEffectActive {
		get {
			return isAnimating;
		}
	}

	public override void SetParameter(int index, float val) {
		switch(index) {
			case 0:
				activeIndex = (int)val;
				break;
			case 1:
				minA = val;
				break;
			case 2:
				maxA = val;
				break;
			case 3:
				attackDuration = val;
				break;
			case 4:
				attackFuncIndex = (int)val;
				break;
			case 5:
				decayDuration = val;
				break;
			case 6:
				decayFuncIndex = (int)val;
				break;
			case 7:
				flashingInterval = (int)val;
				break;
		}
		return;
	}

	public override void SetEffectActive(bool enable) {
		isAnimating = enable;
		StopAllCoroutines();
		if(enable) {
			if(img.sprite == null) return;
			switch(activeIndex) {
				case 0:
					StartCoroutine(AlphaAttackDecay(minA, maxA, attackDuration, attackFuncIndex, decayDuration, decayFuncIndex));
					break;
				case 1:
					StartCoroutine(ConstantFlashing());
					break;
			}
		}
		else {			
			ResetImageAlpha();
		}
	}

	private void ResetImageAlpha() {
		var color = img.color;
		color.a = 0;
		img.color = color;
	}

	float stepFunc(float s, float e, float val) {
		if(val > 0) 
			return e;
		else 
			return s;
	}

	float[] duration = new float[2];

	IEnumerator AlphaAttackDecay(float minA, float maxA, float attackDuration, int attackFuncIndex, float decayDuration, int decayFuncIndex) {
		duration[0] = attackDuration;
		duration[1] = decayDuration;

		var attackFunc = attackFuncIndex < 0? stepFunc:EasingFunction.GetEasingFunction((EasingFunction.Ease)attackFuncIndex);
		var decayFunc = decayFuncIndex < 0? stepFunc:EasingFunction.GetEasingFunction((EasingFunction.Ease)decayFuncIndex);
		float start = 0, end = 0;
		EasingFunction.Function easeFunc;

		for(int i = 0;i < 2;i++) {
			if(i == 0) {
				easeFunc = attackFunc;
				start = minA;
				end = maxA;
			}
			else {
				easeFunc = decayFunc;
				start = maxA;
				end = minA;
			}
			float timer = 0;
			float d = duration[i];
			while(timer < d) {
				var progress = timer / d;
				var val = easeFunc(start, end, progress);
				var color = img.color;
				color.a = val;
				img.color = color;

				timer += Time.deltaTime;
				yield return null;
			}
		}
		
	}

	IEnumerator ConstantFlashing() {
		int state = 0;
		
		var color = img.color;
		color.a = maxA * state + minA * (1 - state);
		img.color = color;
		
		while(true) {
			float timer = 0;
			while(timer < flashingInterval) {
				timer += Time.deltaTime;
				yield return null;
			}

			state = 1 - state; //state flip
		
			color = img.color;
			color.a = maxA * state + minA * (1 - state);
			img.color = color;
		}
	
	}

}
