using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kino;
public class DigitalGlitchWrapper : KTEffectBase {

	public DigitalGlitch effect;
	
	public override void SetParameter(int index, float val) {
		switch(index) {
			case 0:
				effect.intensity = val;
				break;
		}
	}

	public override void SetEffectActive(bool enable) {
		base.SetEffectActive(enable);
		effect.enabled = enable;
	}

}
