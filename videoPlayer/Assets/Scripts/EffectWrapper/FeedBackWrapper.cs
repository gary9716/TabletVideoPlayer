using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kino;
public class FeedBackWrapper : KTEffectBase {

	public Feedback effect;

	public override MonoBehaviour GetEffect() {
		return effect;
	}

	public override void SetEffectActive(bool enable)
    {
			base.SetEffectActive(enable);
			effect.enabled = enable;
    }

    public override void SetParameter(int index, float val)
    {
			switch(index) {
				case 0:
					effect.offsetX = val;
					break;
				case 1:
					effect.offsetY = val;
					break;
				case 2:
					effect.rotation = val;
					break;
				case 3:
					effect.scale = val;
					break;
				case 4:
					effect.jaggies = ((int)val) == 1;
					break;
			}
    }


}
