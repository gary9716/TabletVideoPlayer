using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFBlurSteam : KTEffectBase {

	public override MonoBehaviour GetEffect() {
		return effect;
	}

	public CameraFilterPack_Blur_Steam effect;

	public override void SetEffectActive(bool enable)
    {
			base.SetEffectActive(enable);
			effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect.Radius = val;
					break;
				case 1:
					effect.Quality = val;
					break;
			}
    }

}
