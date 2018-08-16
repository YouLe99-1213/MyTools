﻿using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.DialogueEditor
{

    /// <summary>
    /// This part of the Dialogue Editor window handles the Watches tab, which replaces the 
    /// Templates tab at runtime to allow the user to watch Lua values.
    /// </summary>
    public partial class DialogueEditorWindow
    {

        [Serializable]
        public class Watch
        {
            public bool isVariable = false;
            public int variableIndex = -1;
            public string expression = string.Empty;
            public string value = string.Empty;
            public Variable variable = null;

            public Watch() { }

            public Watch(bool isVariable, string expression, string value)
            {
                this.isVariable = isVariable;
                this.variableIndex = -1;
                this.expression = expression;
                this.value = value;
                this.variable = null;
            }

            public void Evaluate()
            {
                value = string.IsNullOrEmpty(expression)
                    ? string.Empty
                    : Lua.Run("return " + expression).AsString;
            }
        }

        [SerializeField]
        private List<Watch> watches;

        [SerializeField]
        private bool autoUpdateWatches = false;

        [SerializeField]
        private float watchUpdateFrequency = 1f;

        [SerializeField]
        private double nextWatchUpdateTime = 0f;

        [SerializeField]
        private string[] watchableVariableNames = null;

        [SerializeField]
        private string luaCommand = string.Empty;

        private void DrawWatchSection()
        {
            if (watches == null) watches = new List<Watch>();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Watches", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            DrawWatchMenu();
            EditorGUILayout.EndHorizontal();

            EditorWindowTools.StartIndentedSection();
            DrawWatches();
            DrawGlobalWatchControls();
            EditorWindowTools.EndIndentedSection();
        }

        private void DrawWatchMenu()
        {
            if (GUILayout.Button("Menu", "MiniPullDown", GUILayout.Width(56)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add Watch"), false, AddWatch);
                menu.AddItem(new GUIContent("Add Runtime Variable"), false, AddRuntimeVariableWatch);
                menu.AddItem(new GUIContent("Add All Runtime Variables"), false, AddAllRuntimeVariableWatches);
                menu.AddItem(new GUIContent("Refresh Runtime Variable List"), false, RefreshWatchableVariableList);
                menu.AddItem(new GUIContent("Reset"), false, ResetWatches);
                menu.ShowAsContext();
            }
        }

        private void RefreshWatchableVariableList()
        {
            watchableVariableNames = null;
            CatalogueWatchableVariableNames();
        }

        private void ResetWatches()
        {
            watches.Clear();
        }

        private void AddWatch()
        {
            watches.Add(new Watch(false, string.Empty, string.Empty));
        }

        private void AddRuntimeVariableWatch()
        {
            watches.Add(new Watch(true, string.Empty, string.Empty));
            watchableVariableNames = null;
        }

        private void AddAllRuntimeVariableWatches()
        {
            CatalogueWatchableVariableNames();
            for (int i = 0; i < watchableVariableNames.Length; i++)
            {
                var existing = watches.Find(x => x.isVariable && x.variableIndex == i);
                if (existing == null)
                {

                    var watch = new Watch(true, string.Empty, string.Empty);
                    watch.variableIndex = i;
                    watch.expression = string.Format("Variable[\"{0}\"]", watchableVariableNames[i]);
                    watch.variable = database.variables.Find(x => string.Equals(DialogueLua.StringToTableIndex(x.Name), watchableVariableNames[i]));
                    watch.Evaluate();
                    watches.Add(watch);
                }
            }
        }

        private void DrawWatches()
        {
            Watch watchToDelete = null;
            foreach (var watch in watches)
            {
                EditorGUILayout.BeginHorizontal();
                if (watch.isVariable)
                {
                    DrawWatchVariableNamePopupAndEditField(watch);
                }
                else
                {
                    watch.expression = EditorGUILayout.TextField(watch.expression);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(watch.value);
                    EditorGUI.EndDisabledGroup();
                }
                if (GUILayout.Button(new GUIContent("Update", "Re-evaluate now."), EditorStyles.miniButton, GUILayout.Width(56)))
                {
                    watch.Evaluate();
                }
                if (GUILayout.Button(new GUIContent(" ", "Delete this watch."), "OL Minus", GUILayout.Width(27), GUILayout.Height(16)))
                {
                    watchToDelete = watch;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (watchToDelete != null) watches.Remove(watchToDelete);
        }

        private void DrawGlobalWatchControls()
        {
            EditorGUILayout.BeginHorizontal();
            autoUpdateWatches = EditorGUILayout.ToggleLeft("Auto-Update", autoUpdateWatches, GUILayout.Width(100));
            watchUpdateFrequency = EditorGUILayout.FloatField(watchUpdateFrequency, GUILayout.Width(128));
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(watches.Count == 0);
            if (GUILayout.Button(new GUIContent("Update All", "Re-evaluate all now."), EditorStyles.miniButton, GUILayout.Width(56 + 27)))
            {
                UpdateAllWatches();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawWatchVariableNamePopupAndEditField(Watch watch)
        {
            if (watch == null) return;
            CatalogueWatchableVariableNames();
            int newIndex = EditorGUILayout.Popup(watch.variableIndex, watchableVariableNames);
            if (newIndex != watch.variableIndex)
            {
                watch.variableIndex = newIndex;
                if (0 <= watch.variableIndex && watch.variableIndex < watchableVariableNames.Length)
                {
                    var variableTableIndex = DialogueLua.StringToTableIndex(watchableVariableNames[watch.variableIndex]);
                    watch.expression = string.Format("Variable[\"{0}\"]", variableTableIndex);
                    watch.variable = database.variables.Find(x => string.Equals(DialogueLua.StringToTableIndex(x.Name), variableTableIndex));
                }
                else
                {
                    watch.expression = string.Empty;
                    watch.variable = null;
                }
                watch.Evaluate();
            }
            if (watch.variable == null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(watch.value);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                switch (watch.variable.Type)
                {
                    case FieldType.Actor:
                        var newActor = DrawAssetPopup<Actor>(watch.value, database.actors, GUIContent.none);
                        if (newActor != watch.value)
                        {
                            watch.value = newActor;
                            Lua.Run(watch.expression + " = " + newActor);
                        }
                        break;
                    case FieldType.Boolean:
                        var newBoolValue = EditorGUILayout.EnumPopup(StringToBooleanType(watch.value)).ToString();
                        if (newBoolValue != watch.value)
                        {
                            watch.value = newBoolValue;
                            Lua.Run(watch.expression + " = " + newBoolValue.ToString());
                        }
                        break;
                    case FieldType.Location:
                        var newLocation = DrawAssetPopup<Location>(watch.value, database.locations, GUIContent.none);
                        if (newLocation != watch.value)
                        {
                            watch.value = newLocation;
                            Lua.Run(watch.expression + " = " + newLocation);
                        }
                        break;
                    case FieldType.Number:
                        var oldFloatValue = Tools.StringToFloat(watch.value);
                        var newFloatValue = EditorGUILayout.FloatField(oldFloatValue);
                        if (newFloatValue != oldFloatValue)
                        {
                            watch.value = newFloatValue.ToString();
                            Lua.Run(watch.expression + " = " + newFloatValue);
                        }
                        break;
                    default:
                        var newValue = EditorGUILayout.TextField(watch.value);
                        if (!string.Equals(newValue, watch.value))
                        {
                            watch.value = newValue;
                            Lua.Run(watch.expression + " = \"" + newValue + "\"");
                        }
                        break;
                }
            }
        }

        private void CatalogueWatchableVariableNames()
        {
            if (database == null) return;
            if (watchableVariableNames == null || watchableVariableNames.Length == 0)
            {
                List<string> variableNames = new List<string>();
                var variableTable = Lua.Run("return Variable").AsTable;
                if (variableTable != null)
                {
                    foreach (var variableName in variableTable.Keys)
                    {
                        variableNames.Add(variableName);
                    }
                }
                variableNames.Sort();
                watchableVariableNames = variableNames.ToArray();
            }
        }

        private void UpdateRuntimeWatchesTab()
        {
            if (autoUpdateWatches && EditorApplication.timeSinceStartup > nextWatchUpdateTime)
            {
                UpdateAllWatches();
            }
        }

        private void UpdateAllWatches()
        {
            foreach (var watch in watches)
            {
                watch.Evaluate();
            }
            Repaint();
            ResetWatchTime();
        }

        private void ResetWatchTime()
        {
            nextWatchUpdateTime = EditorApplication.timeSinceStartup + watchUpdateFrequency;
        }

        private bool IsLuaWatchBarVisible
        {
            get { return Application.isPlaying && (toolbar.Current == Toolbar.Tab.Templates); }
        }

        private void DrawLuaWatchBar()
        {
            EditorWindowTools.DrawHorizontalLine();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Run Code:", GUILayout.Width(64));
            GUI.SetNextControlName("LuaEmptyLabel");
            EditorGUILayout.LabelField(string.Empty, GUILayout.Width(8));
            luaCommand = EditorGUILayout.TextField(string.Empty, luaCommand);
            if (GUILayout.Button("Clear", "ToolbarSeachCancelButton"))
            {
                luaCommand = string.Empty;
                GUI.FocusControl("LuaEmptyLabel"); // Need to deselect field to clear text field's display.
            }
            if (GUILayout.Button("Run", EditorStyles.miniButton, GUILayout.Width(32)))
            {
                Debug.Log("Running: " + luaCommand);
                Lua.Run(luaCommand, true);
                luaCommand = string.Empty;
                GUI.FocusControl("LuaEmptyLabel"); // Need to deselect field to clear text field's display.
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(string.Empty, GUILayout.Height(1));
        }

    }

}