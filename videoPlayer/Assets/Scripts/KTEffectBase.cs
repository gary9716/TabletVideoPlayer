using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IEffectCtrler
{
	void SetParameter(int index, float val);
}

public class EffectParamsRecording {
	
	public class EffectParams {
		public long videoFrame;
		public int effectIndex;
		public List<int> paramIndex = new List<int>();
		public List<float> paramVal = new List<float>();
	}

	public enum State {
		Idle,
		Recording,
		Playing
	}

	public List<EffectParams> seq = new List<EffectParams>();
	public int progress;
	public State state;

	public void Reset() {
		seq.Clear();
		progress = 0;
		state = State.Idle;
	}
}

public abstract class KTEffectBase : MonoBehaviour,IEffectCtrler {

	public virtual bool isEffectActive {
		get {
			return true;
		}
	}

	public abstract void SetParameter(int index, float val);

	public abstract void SetEffectActive(bool enable);

}

