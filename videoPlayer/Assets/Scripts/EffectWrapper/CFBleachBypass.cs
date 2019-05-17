using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFBleachBypass : KTEffectBase {

	public override MonoBehaviour GetEffect() {
		return effect;
	}

	public CameraFilterPack_Colors_BleachBypass effect;

	public override void SetEffectActive(bool enable)
    {
			base.SetEffectActive(enable);
			effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect.Value = val;
					break;
			}
    }

}
