using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kino;
public class AnalogGlitchWrapper : KTEffectBase {
	public AnalogGlitch effect;
	/// <summary>
	/// Awake is called when the script instance is being loaded.
	/// </summary>
	void Awake()
	{
		effectObj = effect;
	}

	public override void SetParameter(int index, float val) {
		switch(index) {
			case 0:
				effect.scanLineJitter = val;
				break;
			case 1:
				effect.verticalJump = val;
				break;
			case 2:
				effect.horizontalShake = val;
				break;
			case 3:
				effect.colorDrift = val;
				break;
		}
	}

	public override EffectParamsRecording.EffectParams GetCurrentParams() {
		var effectParam = new EffectParamsRecording.EffectParams();
		effectParam.param = new float[4];
		effectParam.param[0] = effect.scanLineJitter;
		effectParam.param[1] = effect.verticalJump;
		effectParam.param[2] = effect.horizontalShake;
		effectParam.param[3] = effect.colorDrift;
		return effectParam;
	}
}
