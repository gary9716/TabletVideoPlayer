using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFTVArtefact : KTEffectBase {

	public CameraFilterPack_TV_Artefact effect;

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
				effect.Colorisation = val;
				break;
			case 2:
				effect.Parasite = val;
				break;
			case 3:
				effect.Noise = val;
				break;
		}
    }
}
