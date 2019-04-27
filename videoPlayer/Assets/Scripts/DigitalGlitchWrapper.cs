using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kino;
public class DigitalGlitchWrapper : KTEffectBase {

	public DigitalGlitch effect;
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
				effect.intensity = val;
				break;
		}
	}

	public override EffectParamsRecording.EffectParams GetCurrentParams() {
		var effectParam = new EffectParamsRecording.EffectParams();
		effectParam.param = new float[1];
		effectParam.param[0] = effect.intensity;
		return effectParam;
	}

}
