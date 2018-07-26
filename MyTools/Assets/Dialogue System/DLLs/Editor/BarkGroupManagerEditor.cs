﻿using UnityEngine;
using UnityEditor;

namespace PixelCrushers.DialogueSystem
{

    /// <summary>
    /// Custom editor for BarkGroupManager that shows the current membership
    /// of bark group members.
    /// </summary>
    [CustomEditor(typeof(BarkGroupManager))]
    public class BarkGroupManagerEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var manager = target as BarkGroupManager;
            foreach (var kvp in manager.groups)
            {
                EditorGUILayout.LabelField(kvp.Key, EditorStyles.boldLabel);
                EditorWindowTools.StartIndentedSection();
                foreach (var member in kvp.Value)
                {
                    if (GUILayout.Button(member.name)) Selection.activeGameObject = member.gameObject;
                }
                EditorWindowTools.EndIndentedSection();
            }
        }

    }

}