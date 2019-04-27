using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IEffectCtrler
{
	void SetParameter(int index, float val);
}


[System.Serializable]
public class EffectParamSeq {

	[System.Serializable]
	public class ParamSeq {
		public float[] vals;
	}
	
	public int index;
	public ParamSeq[] seqs;		

}

public class EffectParamsRecording {
	
	public class EffectParams {
		public long videoFrame;
		public int index;
		public float[] param;
	}

	public List<EffectParams> seq = new List<EffectParams>();
	public int progress;
}

public abstract class KTEffectBase : MonoBehaviour,IEffectCtrler {

	Coroutine playRoutine;
	protected MonoBehaviour effectObj;

	public bool isEffectActive {
		get {
			return effectObj != null && effectObj.enabled;
		}
	}


	public void SetParameter(EffectParamsRecording.EffectParams effectParams) {
		for(int i = 0;i < effectParams.param.Length;i++) {
			SetParameter(i, effectParams.param[i]);
		}
	}

	public virtual void SetParameter(int index, float val) {
		return;
	}

	public virtual EffectParamsRecording.EffectParams GetCurrentParams() {
		return null;
	}

	public void SetEffectVisibility(bool enable) {
		effectObj.enabled = enable;
	}

	public void Play(EffectParamSeq param) {
		if(playRoutine != null) {
			StopCoroutine(playRoutine);
			playRoutine = null;
		}
		effectObj.enabled = true;
		playRoutine = StartCoroutine(UpdateParameters(param));
	}

	private IEnumerator UpdateParameters(EffectParamSeq param) {
		int frameIndex = 0;
		int numSamples = param.seqs.Length > 0? param.seqs[0].vals.Length:0;
		while (true) {
			for(int i = 0;i < param.seqs.Length;i++) {
				SetParameter(i, param.seqs[i].vals[frameIndex]);
			}
			frameIndex = (frameIndex + 1) % numSamples;
			yield return null;
		}
	}

	public void Stop() {
		if(playRoutine != null) {
			StopCoroutine(playRoutine);
			playRoutine = null;
		}
		effectObj.enabled = false;
	}

}

