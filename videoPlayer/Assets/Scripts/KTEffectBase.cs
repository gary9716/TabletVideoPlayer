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

	float triggeredEffectDuration = 0;
	List<ParamFunc> patterns = new List<ParamFunc>();
	List<int> paramIndices = new List<int>();
	
	public void ResetTriggerData() {
		patterns.Clear();
		paramIndices.Clear();
	}

	public void SetEffectRoutineDuration(float duration) {
		triggeredEffectDuration = duration;
	}

	public void AddData(int paramIndex, ParamFunc func) {
		patterns.Add(func);
		paramIndices.Add(paramIndex);
	}

	public void TriggerEffect() {
		StopAllCoroutines();
		StartCoroutine(EvaluateRoutine());
	}

	IEnumerator EvaluateRoutine() {
		float timer = 0;
		if(Mathf.Approximately(triggeredEffectDuration, 0)) yield break;

		while(timer < triggeredEffectDuration) {
			float progress = timer / triggeredEffectDuration;
			for(int i = 0;i < paramIndices.Count;i++) {
				int paramIndex = paramIndices[i];
				var pattern = patterns[i];
				SetParameter(paramIndex, pattern.GetValue(progress));
			}

			yield return null;
			timer += Time.deltaTime;
		}
	}

	public abstract void SetParameter(int index, float val);

	public virtual void SetEffectActive(bool enable) {
		if(!enable) StopAllCoroutines();
	}

}

