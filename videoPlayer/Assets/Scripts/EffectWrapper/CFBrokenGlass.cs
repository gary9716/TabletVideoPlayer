using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFBrokenGlass : KTEffectBase {

	public override MonoBehaviour GetEffect() {
		return effect;
	}

		public CameraFilterPack_Broken_Screen effect;

    public override void SetEffectActive(bool enable)
    {
				base.SetEffectActive(enable);
				effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect.Fade = val;
					break;
				case 1:
					effect.Shadow = val;
					break;
			}
    }
}
