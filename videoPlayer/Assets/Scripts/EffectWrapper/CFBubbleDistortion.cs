using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFBubbleDistortion : KTEffectBase {

	public override MonoBehaviour GetEffect() {
		return effect;
	}

	public CameraFilterPack_Special_Bubble effect;

	public override void SetEffectActive(bool enable)
    {
		base.SetEffectActive(enable);
		effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect.X = val;
					break;
				case 1:
					effect.Y = val;
					break;
				case 2:
					effect.Rate = val;
					break;
					
			}
    }
}
