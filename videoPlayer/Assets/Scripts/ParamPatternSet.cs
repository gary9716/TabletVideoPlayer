using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface ParamFunc {

	float GetValue(float progess);

}


public class ComplexMath : ParamFunc {
	
	private float startVal;
	private float endVal;

	private float offset = 0;
	private float freq = 1;

	private EasingFunction.Function amEase;
	private EasingFunction.Function fmEase;

	public enum MathFunc {
		Linear,
		Poly2,
		Poly3,
		Sine,
		Exp,
		Log,
		Tan,
		Sqrt,
		Easing
	}

	private EasingFunction.Function constant = (s, e, v) => {
		return 1;
	};

	private delegate float SingleInputMath(float val);
	SingleInputMath mathFunc;

	SingleInputMath linear = (val) => {
		return val;
	};

	SingleInputMath poly2 = (val) => {
		return val*val;
	};

	SingleInputMath poly3 = (val) => {
		return val*val*val;
	};

	EasingFunction.Function easing;
	float innerEasingStart;
	float innerEasingEnd;

	public ComplexMath(List<object> parameters) {
		
		offset = float.Parse(parameters[0].ToString());
		freq = float.Parse(parameters[1].ToString());
		startVal = float.Parse(parameters[2].ToString());
		endVal = float.Parse(parameters[3].ToString());
		int amFuncIndex = int.Parse(parameters[4].ToString());
		int fmFuncIndex = int.Parse(parameters[5].ToString());
		var mathFuncType = (MathFunc)int.Parse(parameters[6].ToString());
		
		mathFunc = linear;
		switch(mathFuncType) {
			case MathFunc.Linear:
				mathFunc = linear;
				break;
			case MathFunc.Poly2:
				mathFunc = poly2;
				break;
			case MathFunc.Poly3:
				mathFunc = poly3;
				break;
			case MathFunc.Sine:
				mathFunc = Mathf.Sin;
				break;
			case MathFunc.Exp:
				mathFunc = Mathf.Exp;
				break;
			case MathFunc.Log:
				mathFunc = Mathf.Log;
				break;
			case MathFunc.Sqrt:
				mathFunc = Mathf.Sqrt;
				break;
			case MathFunc.Easing:
				if(parameters.Count > 7) {
					int easingIndex = int.Parse(parameters[7].ToString());
					innerEasingStart = parameters.Count > 8? float.Parse(parameters[8].ToString()):0;
					innerEasingEnd = parameters.Count > 9? float.Parse(parameters[9].ToString()):1;
					easing = EasingFunction.GetEasingFunction((EasingFunction.Ease)easingIndex);
					mathFunc = (val) => { 
						return easing(innerEasingStart, innerEasingEnd, val);
					};
				}
				break;
		}

		amEase = amFuncIndex >= 0? EasingFunction.GetEasingFunction((EasingFunction.Ease)amFuncIndex) : constant;
		fmEase = fmFuncIndex >= 0? EasingFunction.GetEasingFunction((EasingFunction.Ease)fmFuncIndex) : constant;
	}

	public float GetValue(float progess) {
		return Mathf.Clamp01(Mathf.Abs(amEase(startVal, endVal, progess) * mathFunc(offset + fmEase(startVal, endVal, progess) * freq * progess)));
	}

}

public class EasingFuncSeq : ParamFunc {

	List<EasingFunction.Function> easingList = new List<EasingFunction.Function>();
	int curFuncIndex = 0;
	float startVal = 0;
	List<float> endValList = new List<float>();
	List<float> endTimeList = new List<float>();

	public EasingFuncSeq(List<object> parameters) {
		startVal = float.Parse(parameters[0].ToString());
		for(int i = 1;i < parameters.Count;i += 3) {
			int funcIndex = int.Parse(parameters[i].ToString());
			float endVal = float.Parse(parameters[i+1].ToString());
			float endTime = float.Parse(parameters[i+2].ToString());
			endValList.Add(endVal);
			easingList.Add(EasingFunction.GetEasingFunction((EasingFunction.Ease)funcIndex));
			endTimeList.Add(endTime);
		}
	}

	public float GetValue(float progress) {
		if(curFuncIndex < endTimeList.Count && progress > endTimeList[curFuncIndex]) {
			startVal = endValList[curFuncIndex];
			curFuncIndex++;
		}

		if(curFuncIndex == endTimeList.Count) curFuncIndex = endTimeList.Count - 1;
		var start = curFuncIndex > 0? endTimeList[curFuncIndex - 1] : 0;
		var newProgress = (progress - start) / (endTimeList[curFuncIndex] - start);
		return easingList[curFuncIndex](startVal, endValList[curFuncIndex], newProgress);
	}

}


[System.Serializable]
public class ParamPattern {

	public enum PatternType {
		EasingFunc = 0,
		ComplexMath = 1,
	}

	PatternType _patternType;

	public PatternType patternType {
		get {
			return _patternType;
		}

		set {
			_patternType = value;
			Debug.Log(_patternType);
		}
	}

	public ParamFunc curFunc;
	public delegate float Evaluator(float progress);
	public Evaluator GetValue;

	public static ParamFunc GetFunc(PatternType type, List<object> parameters) {
		switch(type) {
			case PatternType.ComplexMath:
				return new ComplexMath(parameters);
			case PatternType.EasingFunc:
				return new EasingFuncSeq(parameters);
		}

		return null;
	}

}

[CreateAssetMenu(fileName = "ParamPatternData", menuName = "Data/Param Data", order = 1)]
public class ParamPatternSet : ScriptableObject {

	public List<ParamPattern> paramPatternList = new List<ParamPattern>();

}
