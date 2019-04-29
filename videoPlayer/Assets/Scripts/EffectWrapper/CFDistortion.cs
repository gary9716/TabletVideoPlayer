﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CFDistortion : KTEffectBase {

	public CameraFilterPack_Distortion_Noise effect;

    public override void SetEffectActive(bool enable)
    {
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