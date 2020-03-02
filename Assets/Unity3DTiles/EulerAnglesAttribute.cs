/*
 * Copyright 2018, by the California Institute of Technology. ALL RIGHTS 
 * RESERVED. United States Government Sponsorship acknowledged. Any 
 * commercial use must be negotiated with the Office of Technology 
 * Transfer at the California Institute of Technology.
 * 
 * This software may be subject to U.S.export control laws.By accepting 
 * this software, the user agrees to comply with all applicable 
 * U.S.export laws and regulations. User has the responsibility to 
 * obtain export licenses, or other export authority as may be required 
 * before exporting such information to foreign countries or providing 
 * access to foreign persons.
 */

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

//add [EulerAngles] attribute to Quaternion fields to edit as Euler angles
//avoids user having to type in unit normal Quaternion components
//http://answers.unity.com/answers/1314095/view.html

public class EulerAnglesAttribute : PropertyAttribute {}

[CustomPropertyDrawer(typeof(EulerAnglesAttribute))]
public class EulerAnglesDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1f : 2f);
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Vector3 euler = property.quaternionValue.eulerAngles;
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        euler = EditorGUI.Vector3Field(position, label, euler);
        if (EditorGUI.EndChangeCheck())
        {
            property.quaternionValue = Quaternion.Euler(euler);
        }
        EditorGUI.EndProperty();
    }
}
#endif
