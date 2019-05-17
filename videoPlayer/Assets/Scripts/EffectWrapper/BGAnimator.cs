using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FunPlus.DeviceUtils;
using UnityEngine.UI;

public class BGAnimator : KTEffectBase {

	public override MonoBehaviour GetEffect() {
		return this;
	}

	//public Canvas bgCanvas;
	public Graphic bg;

	public override bool isEffectActive {
		get {
			return true;
		}
	}

	Color color = Color.white;
	public override void SetParameter(int index, float val) {
		switch(index) {
			case 0: //set r channel
				bg.enabled = true;
				color = bg.color;
				color.r = val;
				bg.color = color;
				break;
			case 1: //set g channel
				bg.enabled = true;
				color = bg.color;
				color.g = val;
				bg.color = color;
				break;
			case 2: //set b channel
				bg.enabled = true;
				color = bg.color;
				color.b = val;
				bg.color = color;
				break;
			case 3: //set a channel
				bg.enabled = true;
				color = bg.color;
				color.a = val;
				bg.color = color;
				break;
			case 4: //set brightness
				DeviceUtils.SetScreenBrightness((int)(255 * val));
				break;
		}
		return;
	}

}
