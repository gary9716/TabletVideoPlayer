using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFGlitch1 : KTEffectBase {

	public CameraFilterPack_Color_Chromatic_Aberration effect1;
	public CameraFilterPack_FX_Glitch1 effect2;

	
    public override void SetEffectActive(bool enable)
    {
		effect1.enabled = enable;
		effect2.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
		switch(index) {
			case 0:
				effect1.Offset = val;
				break;
			case 1:
				effect2.Glitch = val;
				break;
		}
		
    }
}
