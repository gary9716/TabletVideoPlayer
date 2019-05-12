using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFTVDistorted : KTEffectBase {
	public CameraFilterPack_TV_Distorted effect;

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
				case 1:
					effect.RGB = val;
					break;
			}
    }
}
