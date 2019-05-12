using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFVCRDistortion : KTEffectBase {

	public CameraFilterPack_TV_Vcr effect;

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
