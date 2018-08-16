﻿using UnityEngine;
using UnityEditor;
using System.IO;

namespace PixelCrushers.DialogueSystem.DialogueEditor
{

    /// <summary>
    /// Dialogue database editor window. This is the main part of a class that spans 
    /// several files using partial classes. Each file handles one aspect of the 
    /// Dialogue Editor, such as the Actors tab or the Items tab.
    /// </summary>
    public partial class DialogueEditorWindow : EditorWindow
    {

        public static DialogueEditorWindow instance = null;

        public static object inspectorSelection
        {
            get
            {
                return _inspectorSelection;
            }
            set
            {
                _inspectorSelection = value;
                if ((value != null) && (instance != null)) Selection.activeObject = DialogueEditorWindow.instance.database;
                if (DialogueDatabaseEditor.instance != null) DialogueDatabaseEditor.instance.Repaint();
            }
        }
        [SerializeField]
        private static object _inspectorSelection = null;

        [SerializeField]
        private Toolbar toolbar = new Toolbar();

        [SerializeField]
        private DialogueDatabase database = null;

        private SerializedObject serializedObject = null;

        [SerializeField]
        private bool debug = false;

        [SerializeField]
        private bool registerCompleteObjectUndo = false;

        private bool verboseDebug = false;

        [SerializeField]
        private Vector2 scrollPosition = Vector2.zero;

        private Template template = Template.FromDefault();

        private const string CompleteUndoKey = "PixelCrushers.DialogueSystem.DialogueEditor.registerCompleteObjectUndo";
        private const string ShowNodeEditorKey = "PixelCrushers.DialogueSystem.DialogueEditor.ShowNodeEditor";
        private const string ShowDatabaseNameKey = "PixelCrushers.DialogueSystem.DialogueEditor.ShowDatabaseName";
        private const string AutoBackupKey = "PixelCrushers.DialogueSystem.DialogueEditor.AutoBackupFrequency";
        private const string AutoBackupFolderKey = "PixelCrushers.DialogueSystem.DialogueEditor.AutoBackupFolder";

        private const float RuntimeUpdateFrequency = 0.5f;
        private float timeSinceLastRuntimeUpdate = 0;

        private const float DefaultAutoBackupFrequency = 600;
        private float autoBackupFrequency = DefaultAutoBackupFrequency;
        private float timeForNextAutoBackup = 0;
        private string autoBackupFolder = string.Empty;

        private bool showDatabaseName = true;

        private void OnEnable()
        {
            if (debug) Debug.Log("<color=green>Dialogue Editor: OnEnable (Selection.activeObject=" + Selection.activeObject + ", database=" + database + ")</color>", Selection.activeObject);
            instance = this;
            template = TemplateTools.LoadFromEditorPrefs();
            minSize = new Vector2(720, 240);
            if (Selection.activeObject != null)
            {
                SelectObject(Selection.activeObject);
            }
            else
            {
                SelectObject(database);
            }
            toolbar.UpdateTabNames(template.treatItemsAsQuests);
            if (database != null) ValidateConversationMenuTitleIndex();
#if false && UNITY_2017_1_OR_NEWER
            EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
            EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
#else
            EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;
            EditorApplication.playmodeStateChanged += OnPlaymodeStateChanged;
#endif
            registerCompleteObjectUndo = EditorPrefs.GetBool(CompleteUndoKey, true);
            showDatabaseName = EditorPrefs.GetBool(ShowDatabaseNameKey, true);
            autoBackupFrequency = EditorPrefs.GetFloat(AutoBackupKey, DefaultAutoBackupFrequency);
            timeForNextAutoBackup = Time.realtimeSinceStartup + autoBackupFrequency;
        }

        private void OnDisable()
        {
            if (debug) Debug.Log("<color=orange>Dialogue Editor: OnDisable</color>");
#if false && UNITY_2017_1_OR_NEWER
            EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
#else
            EditorApplication.playmodeStateChanged -= OnPlaymodeStateChanged;
#endif
            TemplateTools.SaveToEditorPrefs(template);
            inspectorSelection = null;
            instance = null;
            EditorPrefs.SetBool(CompleteUndoKey, registerCompleteObjectUndo);
            EditorPrefs.SetBool(ShowDatabaseNameKey, showDatabaseName);
            EditorPrefs.SetFloat(AutoBackupKey, autoBackupFrequency);
            EditorPrefs.SetString(AutoBackupFolderKey, autoBackupFolder);
        }

        private void OnSelectionChange()
        {
            SelectObject(Selection.activeObject);
        }

        public void SelectObject(UnityEngine.Object obj)
        {
            var newDatabase = obj as DialogueDatabase;
            if (newDatabase != null)
            {
                var databaseID = (database != null) ? database.GetInstanceID() : -1;
                var newDatabaseID = newDatabase.GetInstanceID();
                var needToReset = (database != null) && (databaseID != newDatabaseID);
                if (debug) Debug.Log("<color=yellow>Dialogue Editor: SelectDatabase " + newDatabase + "(ID=" + newDatabaseID + "), old=" + database + "(ID=" + databaseID + "), reset=" + needToReset + "</color>", newDatabase);
                database = newDatabase;
                serializedObject = new SerializedObject(database);
                if (needToReset)
                {
                    Reset();
                }
                else
                {
                    ResetVariableSection();
                    ResetConversationSection();
                    ResetConversationNodeEditor();
                    UpdateReferencesByID();
                }
                Repaint();
            }
        }

        private void UpdateReferencesByID()
        {
            SetCurrentConversationByID();
            ValidateConversationMenuTitleIndex();
        }

        public void Reset()
        {
            ResetActorSection();
            ResetItemSection();
            ResetLocationSection();
            ResetVariableSection();
            ResetConversationSection();
            ResetConversationNodeEditor();
        }

        private void OnPlaymodeStateChanged()
        {
            if (debug) Debug.Log("<color=cyan>Dialogue Editor: OnPlaymodeStateChanged - isPlaying=" + EditorApplication.isPlaying + "/" + EditorApplication.isPlayingOrWillChangePlaymode + "</color>");
            toolbar.UpdateTabNames(template.treatItemsAsQuests);
            currentConversationState = null;
            currentRuntimeEntry = null;
        }

        void Update()
        {
            if (Application.isPlaying)
            {
                // At runtime, check if we need to update the display:
                timeSinceLastRuntimeUpdate += Time.deltaTime;
                if (timeSinceLastRuntimeUpdate > RuntimeUpdateFrequency)
                {
                    timeSinceLastRuntimeUpdate = 0;
                    switch (toolbar.Current)
                    {
                        case Toolbar.Tab.Conversations:
                            UpdateRuntimeConversationsTab();
                            break;
                        case Toolbar.Tab.Templates:
                            UpdateRuntimeWatchesTab();
                            break;
                    }
                }
            }
            else
            {
                if (autoBackupFrequency > 0.001f && Time.realtimeSinceStartup > timeForNextAutoBackup)
                {
                    timeForNextAutoBackup = Time.realtimeSinceStartup + autoBackupFrequency;
                    if (EditorWindow.focusedWindow == this) MakeAutoBackup();
                }
            }
        }

        public void OnGUI()
        {
            try
            {
                RecordUndo();
                DrawDatabaseName();
                DrawToolbar();
                DrawMainBody();
            }
            catch (System.ArgumentException)
            {
                // Not ideal to hide ArgumentException, but window can change layout during a repaint,
                // which can cause this exception.
            }
        }

        private Color proDatabaseNameColor = new Color(1, 1, 1, 0.2f);
        private Color freeDatabaseNameColor = new Color(0, 0, 0, 0.2f);

        private GUIStyle databaseNameStyle
        {
            get
            {
                if (_databaseNameStyle == null)
                {
                    _databaseNameStyle = new GUIStyle(EditorStyles.label);
                    _databaseNameStyle.fontSize = 20;
                    _databaseNameStyle.fontStyle = FontStyle.Bold;
                    _databaseNameStyle.alignment = TextAnchor.LowerLeft;
                    _databaseNameStyle.normal.textColor = EditorGUIUtility.isProSkin
                        ? proDatabaseNameColor
                            : freeDatabaseNameColor;
                }
                return _databaseNameStyle;
            }
        }
        private GUIStyle _databaseNameStyle = null;

        private GUIStyle conversationParticipantsStyle
        {
            get
            {
                if (_conversationParticipantsStyle == null)
                {
                    _conversationParticipantsStyle = new GUIStyle(databaseNameStyle);
                    _conversationParticipantsStyle.alignment = TextAnchor.LowerRight;
                }
                return _conversationParticipantsStyle;
            }
        }
        private GUIStyle _conversationParticipantsStyle = null;

        private void DrawDatabaseName()
        {
            if (!showDatabaseName) return;
            if (database == null) return;
            if (IsSearchBarVisible || IsLuaWatchBarVisible) return;
            EditorGUI.LabelField(new Rect(4, position.height - 30, position.width, 30), database.name, databaseNameStyle);
        }

        private void DrawToolbar()
        {
            Toolbar.Tab previous = toolbar.Current;
            toolbar.Draw();
            if (toolbar.Current != previous)
            {
                ResetAssetLists();
                ResetDialogueTreeGUIStyles();
                ResetLanguageList();
                if (toolbar.Current == Toolbar.Tab.Items)
                {
                    BuildLanguageListFromItems();
                }
                else if (!((toolbar.Current == Toolbar.Tab.Conversations) && showNodeEditor))
                {
                    inspectorSelection = null;
                }
            }
        }

        private void DrawMainBody()
        {
            if ((database != null) || (toolbar.Current == Toolbar.Tab.Templates))
            {
                DrawCurrentSection();
            }
            else {
                DrawNoDatabaseSection();
            }
        }

        private void DrawCurrentSection()
        {
            EditorGUI.BeginChangeCheck();
            EditorStyles.textField.wordWrap = true;
            var isInNodeEditor = (toolbar.Current == Toolbar.Tab.Conversations) && showNodeEditor;
            if (isInNodeEditor) HandleNodeEditorScrollWheelEvents();
            if (!isInNodeEditor) scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            try
            {
                switch (toolbar.Current)
                {
                    case Toolbar.Tab.Database:
                        DrawDatabaseSection();
                        break;
                    case Toolbar.Tab.Actors:
                        DrawActorSection();
                        break;
                    case Toolbar.Tab.Items:
                        DrawItemSection();
                        break;
                    case Toolbar.Tab.Locations:
                        DrawLocationSection();
                        break;
                    case Toolbar.Tab.Variables:
                        DrawVariableSection();
                        break;
                    case Toolbar.Tab.Conversations:
                        DrawConversationSection();
                        break;
                    case Toolbar.Tab.Templates:
                        if (Application.isPlaying)
                        {
                            DrawWatchSection();
                        }
                        else {
                            DrawTemplateSection();
                        }
                        break;
                }
            }
            finally
            {
                if (!isInNodeEditor) EditorGUILayout.EndScrollView();
            }
            if (IsSearchBarVisible) DrawDialogueTreeSearchBar();
            if (IsLuaWatchBarVisible) DrawLuaWatchBar();

            if (EditorGUI.EndChangeCheck()) SetDatabaseDirty("Standard Editor Change");
        }

        private void RecordUndo()
        {
            if (database == null) return;
            if (registerCompleteObjectUndo)
            {
                Undo.RegisterCompleteObjectUndo(database, "Dialogue Database");
            }
            else
            {
                Undo.RecordObject(database, "Dialogue Database");
            }
        }

        public void SetDatabaseDirty(string reason)
        {
            if (debug)
            {
                if (database == null)
                {
                    if (string.IsNullOrEmpty(reason))
                    {
                        Debug.LogWarning("Dialogue Editor wants to mark database to be saved, but it doesn't have a reference to a database!");
                    }
                    else {
                        Debug.LogWarning("Dialogue Editor wants to mark database to be saved after change '" + reason + "', but it doesn't have a reference to a database!");
                    }
                }
                else {
                    if (string.IsNullOrEmpty(reason))
                    {
                        Debug.Log("Dialogue Editor marking database '" + database.name + "' to be saved.", database);
                    }
                    else {
                        Debug.Log("Dialogue Editor marking database '" + database.name + "' to be saved after change '" + reason + "'.", database);
                    }
                }
            }
            if (database != null) EditorUtility.SetDirty(database);
        }

        private void MakeAutoBackup()
        {
            if (database == null) return;
            try
            {
                var path = AssetDatabase.GetAssetPath(database);
                if (path.EndsWith("(Auto-Backup).asset"))
                {
                    Debug.Log("Dialogue Editor: Not creating an auto-backup. You're already editing the auto-backup file.");
                    return;
                }
                var backupPath = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + " (Auto-Backup).asset";
                if (debug) Debug.Log("Dialogue Editor: Making auto-backup " + backupPath);
                EditorUtility.DisplayProgressBar("Dialogue Editor Auto-Backup", "Creating auto-backup " + Path.GetFileNameWithoutExtension(backupPath), 0);
                AssetDatabase.DeleteAsset(backupPath);
                AssetDatabase.CopyAsset(path, backupPath);
                AssetDatabase.Refresh();
#if !(UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9)
                if (!string.IsNullOrEmpty(AssetImporter.GetAtPath(backupPath).assetBundleName))
                {
                    AssetImporter.GetAtPath(backupPath).assetBundleVariant = string.Empty;
                    AssetImporter.GetAtPath(backupPath).assetBundleName = string.Empty;
                }
#endif
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

    }

}