using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFTVNoise1 : KTEffectBase {

	public CameraFilterPack_Noise_TV_2 effect;

    public override void SetEffectActive(bool enable)
    {
		effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
		switch(index) {
			case 0:
				effect.Fade = val;
				break;
			case 1:
				effect.Fade_Additive = val;
				break;
			case 2:
				effect.Fade_Distortion = val;
				break;
		}
    }
}
