using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFColorContrast : KTEffectBase {

	public override MonoBehaviour GetEffect() {
		return effect;
	}

	public CameraFilterPack_Color_Contrast effect;

	public override void SetEffectActive(bool enable)
    {
			base.SetEffectActive(enable);
			effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect.Contrast = val;
					break;
			}
    }

}
