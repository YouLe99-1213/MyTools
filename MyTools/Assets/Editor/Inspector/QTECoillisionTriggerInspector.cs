﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MarsPC
{
    [CustomEditor(typeof(QTECollisionTrigger))]
    public class QTECoillisionTriggerInspector : Editor
    {
        private QTECollisionTrigger qteTrigger;
        private SerializedProperty singleKeyContinue;
        private SerializedProperty singleKeyPhythm;
        private SerializedProperty doubleKeyRepeat;
        private SerializedProperty linearClick;
        private SerializedProperty linearDirection;
        private SerializedProperty scrollBarClick;
        private SerializedProperty powerGauge;
        private SerializedProperty mouseGestures;
        private SerializedProperty focusPoint;

        private void Awake()
        {
            qteTrigger = target as QTECollisionTrigger;
        }

        private void OnEnable()
        {
            singleKeyContinue = serializedObject.FindProperty("info.singleKeyContinue");
            singleKeyPhythm = serializedObject.FindProperty("info.singleKeyPhythm");
            doubleKeyRepeat = serializedObject.FindProperty("info.doubleKeyRepeat");
            linearClick = serializedObject.FindProperty("info.linearClick");
            linearDirection = serializedObject.FindProperty("info.linearDirection");
            scrollBarClick = serializedObject.FindProperty("info.scrollBarClick");
            powerGauge = serializedObject.FindProperty("info.powerGauge");
            mouseGestures = serializedObject.FindProperty("info.mouseGestures");
            focusPoint = serializedObject.FindProperty("info.focusPoint");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            qteTrigger.info.isAutomaticActive = EditorGUILayout.Toggle("Is Automatic Active", qteTrigger.info.isAutomaticActive);
            qteTrigger.info.ID = EditorGUILayout.IntField("ID", qteTrigger.info.ID);
            qteTrigger.info.duration = EditorGUILayout.FloatField("Duration", qteTrigger.info.duration);
            qteTrigger.info.description = EditorGUILayout.TextField("Description", qteTrigger.info.description);

            qteTrigger.info.type = (EQTEType)EditorGUILayout.EnumPopup("Type", qteTrigger.info.type);
            switch (qteTrigger.info.type)
            {
                case EQTEType.None:
                    break;

                case EQTEType.SingleKeyContinue:
                    EditorGUILayout.PropertyField(singleKeyContinue, true);
                    break;

                case EQTEType.SingleKeyPhythm:
                    EditorGUILayout.PropertyField(singleKeyPhythm, true);
                    break;

                case EQTEType.DoubleKeyRepeat:
                    EditorGUILayout.PropertyField(doubleKeyRepeat, true);
                    break;

                case EQTEType.LinearClick:
                    EditorGUILayout.PropertyField(linearClick, true);
                    break;

                case EQTEType.LinearDirection:
                    EditorGUILayout.PropertyField(linearDirection, true);
                    break;

                case EQTEType.ScrollBarClick:
                    EditorGUILayout.PropertyField(scrollBarClick, true);
                    break;

                case EQTEType.PowerGauge:
                    EditorGUILayout.PropertyField(powerGauge, true);
                    break;

                case EQTEType.MouseGestures:
                    EditorGUILayout.PropertyField(mouseGestures, true);
                    break;

                case EQTEType.FocusPoint:
                    EditorGUILayout.PropertyField(focusPoint, true);
                    break;

                default:
                    break;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}