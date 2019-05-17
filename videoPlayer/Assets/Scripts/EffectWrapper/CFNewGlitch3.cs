using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFNewGlitch3 : KTEffectBase {

	public override MonoBehaviour GetEffect() {
		return effect;
	}


	public CameraFilterPack_NewGlitch3 effect;

	public override void SetEffectActive(bool enable)
    {
			base.SetEffectActive(enable);
			effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect.__Speed = val;
					break;
				case 1:
					effect._RedFade = val;
					break;
			}
    }

}
