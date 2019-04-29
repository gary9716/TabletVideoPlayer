using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFBrokenGlass : KTEffectBase {

	public CameraFilterPack_TV_BrokenGlass effect;

	
    public override void SetEffectActive(bool enable)
    {
		effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
		switch(index) {
			case 0:
				effect.Broken_Small = val;
				break;
			case 1:
				effect.Broken_Medium = val;
				break;
			case 2:
				effect.Broken_High = val;
				break;
			case 3:
				effect.Broken_Big = val;
				break;
		}
    }
}
