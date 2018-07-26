﻿using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem.DialogueEditor
{

    /// <summary>
    /// This part of the Dialogue Editor window handles the Actors tab.
    /// </summary>
    public partial class DialogueEditorWindow
    {

        [SerializeField]
        private AssetFoldouts actorFoldouts = new AssetFoldouts();

        [SerializeField]
        private string actorFilter = string.Empty;

        private void ResetActorSection()
        {
            actorFoldouts = new AssetFoldouts();
            actorAssetList = null;
        }

        private void DrawActorSection()
        {
            if (database.syncInfo.syncActors)
            {
                DrawAssetSection<Actor>("Actor", database.actors, actorFoldouts, DrawActorMenu, DrawActorSyncDatabase, ref actorFilter);
            }
            else {
                DrawAssetSection<Actor>("Actor", database.actors, actorFoldouts, DrawActorMenu, ref actorFilter);
            }
        }

        private void DrawActorMenu()
        {
            if (GUILayout.Button("Menu", "MiniPullDown", GUILayout.Width(56)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("New Actor"), false, AddNewActor);
                menu.AddItem(new GUIContent("Sort/By Name"), false, SortActorsByName);
                menu.AddItem(new GUIContent("Sort/By ID"), false, SortActorsByID);
                menu.AddItem(new GUIContent("Sync From DB"), database.syncInfo.syncActors, ToggleSyncActorsFromDB);
                menu.ShowAsContext();
            }
        }

        private void AddNewActor()
        {
            AddNewAsset<Actor>(database.actors);
        }

        private void SortActorsByName()
        {
            database.actors.Sort((x, y) => x.Name.CompareTo(y.Name));
            SetDatabaseDirty("Sort Actors by Name");
        }

        private void SortActorsByID()
        {
            database.actors.Sort((x, y) => x.id.CompareTo(y.id));
            SetDatabaseDirty("Sort Actors by ID");
        }

        private void ToggleSyncActorsFromDB()
        {
            database.syncInfo.syncActors = !database.syncInfo.syncActors;
            SetDatabaseDirty("Toggle Sync Actors");
        }

        private void DrawActorSyncDatabase()
        {
            EditorGUILayout.BeginHorizontal();
            DialogueDatabase newDatabase = EditorGUILayout.ObjectField(new GUIContent("Sync From", "Database to sync actors from."),
                                                                       database.syncInfo.syncActorsDatabase, typeof(DialogueDatabase), false) as DialogueDatabase;
            if (newDatabase != database.syncInfo.syncActorsDatabase)
            {
                database.syncInfo.syncActorsDatabase = newDatabase;
                database.SyncActors();
                SetDatabaseDirty("Change Actor Sync Database");
            }
            if (GUILayout.Button(new GUIContent("Sync Now", "Syncs from the database."), EditorStyles.miniButton, GUILayout.Width(72)))
            {
                database.SyncActors();
                SetDatabaseDirty("Sync Actors");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActorPortrait(Actor actor)
        {
            if (actor == null) return;

            // Display Name:
            var displayNameField = Field.Lookup(actor.fields, "Display Name");
            var hasDisplayNameField = (displayNameField != null);
            var useDisplayNameField = EditorGUILayout.Toggle(new GUIContent("Use Display Name", "Tick to use a Display Name in UIs that's different from the Name."), hasDisplayNameField);
            if (hasDisplayNameField && !useDisplayNameField)
            {
                actor.fields.Remove(displayNameField);
                SetDatabaseDirty("Don't Use Display Name");
            }
            else if (useDisplayNameField)
            {
                EditTextField(actor.fields, "Display Name", "The name to show in UIs.", false);
                DrawLocalizedVersions(actor.fields, "Display Name {0}", false, FieldType.Text);
            }

            // Portraits
            EditorGUILayout.BeginHorizontal();
            try
            {
                var newPortrait = EditorGUILayout.ObjectField(new GUIContent("Portraits", "This actor's portrait. Only necessary if your UI uses portraits."),
                                                         actor.portrait, typeof(Texture2D), false, GUILayout.Height(64)) as Texture2D;
                if (newPortrait != actor.portrait)
                {
                    actor.portrait = newPortrait;
                    ClearActorInfoCaches();
                }
            }
            catch (NullReferenceException)
            {
            }
            int indexToDelete = -1;
            if (actor.alternatePortraits == null) actor.alternatePortraits = new List<Texture2D>();
            for (int i = 0; i < actor.alternatePortraits.Count; i++)
            {
                try
                {
                    EditorGUILayout.BeginVertical();

                    try
                    {
                        actor.alternatePortraits[i] = EditorGUILayout.ObjectField(actor.alternatePortraits[i], typeof(Texture2D), false, GUILayout.Width(54), GUILayout.Height(54)) as Texture2D;
                    }
                    catch (NullReferenceException)
                    {
                    }
                    try
                    {
                        EditorGUILayout.BeginHorizontal(GUILayout.Width(54));
                        EditorGUILayout.LabelField(string.Format("[{0}]", i + 2), CenteredLabelStyle, GUILayout.Width(27));
                        if (GUILayout.Button(new GUIContent(" ", "Delete this portrait."), "OL Minus", GUILayout.Width(27), GUILayout.Height(16)))
                        {
                            indexToDelete = i;
                        }
                    }
                    finally
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }
            }
            if (indexToDelete > -1)
            {
                actor.alternatePortraits.RemoveAt(indexToDelete);
                SetDatabaseDirty("Delete Portrait");
            }

            if (GUILayout.Button(new GUIContent(" ", "Add new alternate portrait image."), "OL Plus", GUILayout.Height(48)))
            {
                actor.alternatePortraits.Add(null);
                SetDatabaseDirty("Add Portrait");
            }
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            DrawActorNodeColor(actor);

            DrawActorPrimaryFields(actor);
        }

        private void DrawActorPrimaryFields(Actor actor)
        {
            if (actor == null || actor.fields == null || template.actorPrimaryFieldTitles == null) return;
            foreach (var field in actor.fields)
            {
                var fieldTitle = field.title;
                if (string.IsNullOrEmpty(fieldTitle)) continue;
                if (!template.actorPrimaryFieldTitles.Contains(field.title)) continue;
                EditorGUILayout.BeginHorizontal();
                DrawField(field, false, false);
                EditorGUILayout.EndHorizontal();
            }
        }

        private const string NodeColorFieldTitle = "NodeColor";

        private void DrawActorNodeColor(Actor actor)
        {
            if (actor == null) return;
            EditorGUILayout.BeginHorizontal();
            var useNodeColor = actor.FieldExists(NodeColorFieldTitle);
            var toggleValue = EditorGUILayout.Toggle("Custom Node Color", useNodeColor);
            if (toggleValue != useNodeColor)
            {
                ClearActorInfoCaches();
                switch (toggleValue)
                {
                    case true:
                        actor.fields.Add(new Field(NodeColorFieldTitle, (actor.LookupBool("IsPlayer") ? "Blue" : "Gray"), FieldType.Text));
                        break;
                    case false:
                        actor.fields.Remove(actor.fields.Find(x => string.Equals(x.title, NodeColorFieldTitle)));
                        break;
                }
            }
            if (toggleValue == true)
            {
                var nodeColorField = actor.fields.Find(x => string.Equals(x.title, NodeColorFieldTitle));
                if (nodeColorField != null)
                {
                    int index = 0;
                    for (int i = 0; i < EditorTools.StylesColorStrings.Length; i++)
                    {
                        if (string.Equals(nodeColorField.value, EditorTools.StylesColorStrings[i]))
                        {
                            index = i;
                        }
                    }
                    var newIndex = EditorGUILayout.Popup(index, EditorTools.StylesColorStrings);
                    if (newIndex != index)
                    {
                        ClearActorInfoCaches();
                        nodeColorField.value = (0 <= newIndex && newIndex < EditorTools.StylesColorStrings.Length)
                            ? EditorTools.StylesColorStrings[newIndex] 
                            : (actor.LookupBool("IsPlayer") ? "Blue" : "Gray");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

    }

}