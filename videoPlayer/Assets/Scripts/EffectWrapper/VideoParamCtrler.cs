using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FunPlus.DeviceUtils;

public class VideoParamCtrler : KTEffectBase {
	
	public RawImage videoImg;
	public OSCDaemon oscDaemon;

	public override bool isEffectActive {
		get {
			return true;
		}
	}

	public override void SetParameter(int index, float val) {
		switch(index) {
			case 0: //set alpha
				var color = videoImg.color;
				color.a = val;
				videoImg.color = color;
				break;
			case 1: //set video speed
				oscDaemon.SetVideoSpeed(val);
				break;
		}
		return;
	}

	public override void SetEffectActive(bool enable) {

	}
}
