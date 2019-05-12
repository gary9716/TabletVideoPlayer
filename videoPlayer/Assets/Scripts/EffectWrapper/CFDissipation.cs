using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFDissipation : KTEffectBase {

	public CameraFilterPack_Distortion_Dissipation effect;

	public override void SetEffectActive(bool enable)
    {
		base.SetEffectActive(enable);
		effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect.Dissipation = val;
					break;
			}
    }
}
