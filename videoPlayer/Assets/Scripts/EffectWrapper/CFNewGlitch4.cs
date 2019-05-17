using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFNewGlitch4 : KTEffectBase {

	public override MonoBehaviour GetEffect() {
		return effect;
	}

	public CameraFilterPack_NewGlitch4 effect;

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
					effect._Fade = val;
					break;
			}
    }

}
