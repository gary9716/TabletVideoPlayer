using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFBlurry : KTEffectBase {

	public override MonoBehaviour GetEffect() {
		return effect;
	}

	public CameraFilterPack_Blur_Blurry effect;
	
	public override void SetEffectActive(bool enable)
    {
			base.SetEffectActive(enable);
			effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect.Amount = val;
					break;
				case 1:
					effect.FastFilter = (int)val;
					break;
			}
    }
}
