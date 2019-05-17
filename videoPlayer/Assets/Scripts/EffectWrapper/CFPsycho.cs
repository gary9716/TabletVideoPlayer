using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFPsycho : KTEffectBase {

	public override MonoBehaviour GetEffect() {
		return effect;
	}

	public CameraFilterPack_FX_Psycho effect;

	public override void SetEffectActive(bool enable)
    {
			base.SetEffectActive(enable);
			effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect.Distortion = val;
					break;
			}
    }

}
