using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class TextProperties : MonoBehaviour {

	public enum Orientation {
		Horizontal,
		Vertical
	}

	public Orientation orientation;
	public Vector3 pos;
	public float fontSize;

	[ContextMenu("Save")]
	public void Save() {
		pos = transform.position;
	}

	public void SetText(Text txt) {
		txt.rectTransform.position = pos;
		var size = txt.rectTransform.sizeDelta;
		txt.alignment = TextAnchor.MiddleCenter;
		
		if(orientation == Orientation.Horizontal) {
			size.y = fontSize + 2;
			txt.horizontalOverflow = HorizontalWrapMode.Overflow;
		}
		else {
			size.x = fontSize + 2;
			txt.horizontalOverflow = HorizontalWrapMode.Wrap;
			txt.verticalOverflow = VerticalWrapMode.Overflow;
		}

		txt.rectTransform.sizeDelta = size;
	}
}
