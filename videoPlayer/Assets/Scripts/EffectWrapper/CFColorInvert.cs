using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFColorInvert : KTEffectBase {

	public override MonoBehaviour GetEffect() {
		return effect;
	}

	public CameraFilterPack_Color_Invert effect;

	public override void SetEffectActive(bool enable)
    {
			base.SetEffectActive(enable);
			effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect._Fade = val;
					break;
			}
    }
}
