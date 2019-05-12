using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFGlitch1 : KTEffectBase {

		public CameraFilterPack_FX_Glitch1 effect2;

    public override void SetEffectActive(bool enable)
    {
			base.SetEffectActive(enable);
			effect2.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect2.Glitch = val;
					break;
			}
		
    }
}
