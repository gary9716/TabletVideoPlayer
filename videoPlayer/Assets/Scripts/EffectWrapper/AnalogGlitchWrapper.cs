using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kino;
public class AnalogGlitchWrapper : KTEffectBase {
	public AnalogGlitch effect;
	
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

	public override void SetEffectActive(bool enable) {
		base.SetEffectActive(enable);
		effect.enabled = enable;
	}

	public override MonoBehaviour GetEffect() {
		return effect;
	}
}
