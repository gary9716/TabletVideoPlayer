using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFGlitch3 : KTEffectBase {

	public CameraFilterPack_FX_Glitch3 effect;

	public override void SetEffectActive(bool enable)
    {
				base.SetEffectActive(enable);
				effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect._Glitch = val;
					break;
				case 1:
					effect._Noise = val;
					break;
			}
    }
}
