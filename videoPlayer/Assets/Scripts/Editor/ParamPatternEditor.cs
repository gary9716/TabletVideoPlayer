using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ParamPatternSet))]
[CanEditMultipleObjects]
public class ParamPatternEditor : Editor {

	SerializedProperty m_prop;

	void OnEnable()
    {
        // Fetch the objects from the GameObject script to display in the inspector
        m_prop = serializedObject.FindProperty("paramPatternList");
    }

    public override void OnInspectorGUI()
    {
        //The variables and GameObject from the MyGameObject script are displayed in the Inspector with appropriate labels
        if(EditorGUILayout.PropertyField(m_prop, new GUIContent("Param Pattern List"), true , GUILayout.Height(20))) {
			var iter = m_prop.GetEnumerator();
			while(iter.MoveNext()) {
				var paramPatternProp = iter.Current as SerializedProperty;
				var paramFunc = paramPatternProp.serializedObject.FindProperty("curFunc");
				
			}
		}

        // Apply changes to the serializedProperty - always do this at the end of OnInspectorGUI.
        serializedObject.ApplyModifiedProperties();
    }

}
