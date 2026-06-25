#if UNITY_EDITOR
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using BoxPuller.Scripts.Runtime.LevelCreation;
using HashiGame.Scripts.Runtime;
using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Editor
{
    [CustomEditor(typeof(LevelCreator))]
    public class LevelCreatorEditor : UnityEditor.Editor
    {
        private LevelCreator levelCreator;
        private Vector2 gridScrollPosition;
        private Vector2 definitionScrollPosition;
        private EnumHolder.HashiEditorMode editorMode = EnumHolder.HashiEditorMode.Island;
        private bool hasPendingCoordinate;
        private Vector2Int pendingCoordinate;
        private string editorMessage;
        private MessageType editorMessageType = MessageType.Info;

        private SerializedProperty prefabSaverProperty;
        private SerializedProperty prefabLoaderProperty;
        private SerializedProperty prefabLoaderOldProperty;
        private SerializedProperty vCamProperty;

        private void OnEnable()
        {
            levelCreator = (LevelCreator)target;
            prefabSaverProperty = serializedObject.FindProperty("prefabSaver");
            prefabLoaderProperty = serializedObject.FindProperty("prefabLoader");
            prefabLoaderOldProperty = serializedObject.FindProperty("prefabLoaderOld");
            vCamProperty = serializedObject.FindProperty("vCam");
            levelCreator.EnsureLevelData();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            levelCreator.EnsureLevelData();

            DrawReferenceSection();
            DrawGridSettings();
            DrawLevelButtons();
            DrawLevelTimeSettings();
            DrawRuleSettings();
            DrawEditingMode();
            DrawGrid();
            DrawDefinitionLists();
            DrawValidationPanel();

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(levelCreator);
            }
        }

        private void DrawReferenceSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Game References", EditorStyles.boldLabel);

            levelCreator.prefabs = (BoxPuller.Scripts.Data.SO.GamePrefabs)
                EditorGUILayout.ObjectField(
                    "Game Prefabs",
                    levelCreator.prefabs,
                    typeof(BoxPuller.Scripts.Data.SO.GamePrefabs),
                    false);

            levelCreator.visualSettings = (BoxPuller.Scripts.Data.SO.HashiVisualSettings)
                EditorGUILayout.ObjectField(
                    "Hashi Visual Settings",
                    levelCreator.visualSettings,
                    typeof(BoxPuller.Scripts.Data.SO.HashiVisualSettings),
                    false);

            levelCreator.currentLevelContainer = (LevelContainer)
                EditorGUILayout.ObjectField(
                    "Current Level Container",
                    levelCreator.currentLevelContainer,
                    typeof(LevelContainer),
                    true);

            EditorGUILayout.PropertyField(prefabSaverProperty);
            EditorGUILayout.PropertyField(prefabLoaderProperty);
            EditorGUILayout.PropertyField(prefabLoaderOldProperty);
            EditorGUILayout.PropertyField(vCamProperty);
            EditorGUILayout.EndVertical();
        }

        private void DrawGridSettings()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

            levelCreator.levelIndex = EditorGUILayout.IntField(
                "Level Index",
                levelCreator.levelIndex);

            EditorGUILayout.BeginHorizontal();
            levelCreator.gridWidth = Mathf.Max(
                1,
                EditorGUILayout.IntField("Grid Width", levelCreator.gridWidth));
            levelCreator.expandGridToLeft = EditorGUILayout.ToggleLeft(
                "Resize From Left",
                levelCreator.expandGridToLeft,
                GUILayout.Width(120f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            levelCreator.gridHeight = Mathf.Max(
                1,
                EditorGUILayout.IntField("Grid Height", levelCreator.gridHeight));
            levelCreator.expandGridToUp = EditorGUILayout.ToggleLeft(
                "Resize From Bottom",
                levelCreator.expandGridToUp,
                GUILayout.Width(120f));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Apply Grid Size", GUILayout.Height(28f)))
            {
                levelCreator.ResizeGrid();
                CancelPendingSelection();
            }

            EditorGUILayout.Space(4f);
            levelCreator.horizontalSpaceModifier = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField(
                    "Horizontal Spacing",
                    levelCreator.horizontalSpaceModifier));

            levelCreator.verticalSpaceModifier = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField(
                    "Vertical Spacing",
                    levelCreator.verticalSpaceModifier));

            levelCreator.gridOriginOffset = EditorGUILayout.Vector3Field(
                "Grid Origin Offset",
                levelCreator.gridOriginOffset);

            levelCreator.islandBaseHeight = EditorGUILayout.FloatField(
                "Island Base Height",
                levelCreator.islandBaseHeight);

            levelCreator.islandEulerAngles = EditorGUILayout.Vector3Field(
                "Island Euler Angles",
                levelCreator.islandEulerAngles);

            EditorGUILayout.EndVertical();
        }

        private void DrawLevelButtons()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();

            Color oldColor = GUI.backgroundColor;

            GUI.backgroundColor = new Color(0.35f, 0.75f, 0.4f);
            if (GUILayout.Button("Generate Level", GUILayout.Height(36f)))
            {
                levelCreator.GenerateLevel();
                GUIUtility.ExitGUI();
            }

            GUI.backgroundColor = new Color(0.75f, 0.7f, 0.4f);
            if (GUILayout.Button("Save", GUILayout.Height(36f)))
            {
                levelCreator.SaveLevel();
            }

            GUI.backgroundColor = new Color(0.4f, 0.65f, 0.75f);
            if (GUILayout.Button("Load", GUILayout.Height(36f)))
            {
                levelCreator.LoadLevel();
                CancelPendingSelection();
                GUIUtility.ExitGUI();
            }

            GUI.backgroundColor = new Color(0.75f, 0.35f, 0.35f);
            if (GUILayout.Button("Reset", GUILayout.Height(36f)))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Reset Level",
                    "Delete the current level data and generated prefab?",
                    "Reset",
                    "Cancel");

                if (confirmed)
                {
                    levelCreator.ResetLevel();
                    CancelPendingSelection();
                    GUIUtility.ExitGUI();
                }
            }

            GUI.backgroundColor = oldColor;
            EditorGUILayout.EndHorizontal();
        }
        private void DrawLevelTimeSettings()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Level Time", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            levelCreator.levelTimeMinutes = Mathf.Max(
                0,
                EditorGUILayout.IntField(
                    "Minutes",
                    levelCreator.levelTimeMinutes));

            levelCreator.levelTimeSecondsPart = Mathf.Clamp(
                EditorGUILayout.IntField(
                    "Seconds",
                    levelCreator.levelTimeSecondsPart),
                0,
                59);

            if (levelCreator.levelTimeMinutes == 0 &&
                levelCreator.levelTimeSecondsPart == 0)
            {
                levelCreator.levelTimeSecondsPart = 1;
            }

            int totalSeconds = levelCreator.GetEditorLevelTimeTotalSeconds();

            EditorGUILayout.LabelField(
                "Total",
                FormatTime(totalSeconds) + " (" + totalSeconds + " sec)");

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(levelCreator);
            }

            EditorGUILayout.EndVertical();
        }

        private static string FormatTime(int totalSeconds)
        {
            totalSeconds = Mathf.Max(0, totalSeconds);

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            return minutes.ToString("00") + ":" + seconds.ToString("00");
        }
        private void DrawRuleSettings()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Level Rules", EditorStyles.boldLabel);

            levelCreator.blockBridgeThroughIsland = EditorGUILayout.Toggle(
                "Block Through Islands",
                levelCreator.blockBridgeThroughIsland);

            levelCreator.blockBridgeCrossing = EditorGUILayout.Toggle(
                "Block Bridge Crossing",
                levelCreator.blockBridgeCrossing);

            levelCreator.requireAllIslandsConnected = EditorGUILayout.Toggle(
                "Require One Connected Network",
                levelCreator.requireAllIslandsConnected);

            levelCreator.islandBlockingRadius = Mathf.Max(
                0.01f,
                EditorGUILayout.FloatField(
                    "Island Blocking Radius",
                    levelCreator.islandBlockingRadius));

            EditorGUILayout.EndVertical();
        }

        private void DrawEditingMode()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Paint Settings", EditorStyles.boldLabel);

            EnumHolder.HashiEditorMode previousMode = editorMode;
            editorMode = (EnumHolder.HashiEditorMode)GUILayout.Toolbar(
                (int)editorMode,
                new[] { "Island", "Fixed Bridge", "Chain" },
                GUILayout.Height(30f));

            if (previousMode != editorMode)
            {
                CancelPendingSelection();
            }

            EditorGUILayout.Space(6f);

            switch (editorMode)
            {
                case EnumHolder.HashiEditorMode.Island:
                    DrawIslandPaintSettings();
                    EditorGUILayout.HelpBox(
                        "Left click places or updates an island. Right click removes the island.",
                        MessageType.Info);
                    break;

                case EnumHolder.HashiEditorMode.FixedBridge:
                    levelCreator.fixedBridgeCount = EditorGUILayout.IntPopup(
                        "Fixed Bridge Count",
                        Mathf.Clamp(levelCreator.fixedBridgeCount, 1, 2),
                        new[] { "Single", "Double" },
                        new[] { 1, 2 });

                    DrawPendingSelectionHelp(
                        "Select two island cells. The generated bridge cannot be removed during play.");
                    break;

                case EnumHolder.HashiEditorMode.Chain:
                    levelCreator.chainUnlockRequirement = Mathf.Max(
                        0,
                        EditorGUILayout.IntField(
                            "Unlock After Completed Islands",
                            levelCreator.chainUnlockRequirement));

                    DrawPendingSelectionHelp(
                        "Select any two grid points. An active chain blocks every bridge that crosses it.");
                    break;
            }

            if (!string.IsNullOrEmpty(editorMessage))
            {
                EditorGUILayout.HelpBox(editorMessage, editorMessageType);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawIslandPaintSettings()
        {
            levelCreator.islandRequiredBridgeCount = Mathf.Max(
                1,
                EditorGUILayout.IntField(
                    "Required Bridge Count",
                    levelCreator.islandRequiredBridgeCount));

            levelCreator.islandBridgeMode =
             (EnumHolder.IslandBridgeMode)EditorGUILayout.EnumPopup(
                 "Island Type",
                 levelCreator.islandBridgeMode);

            levelCreator.islandStartsLocked = EditorGUILayout.Toggle(
                "Starts Locked",
                levelCreator.islandStartsLocked);

            using (new EditorGUI.DisabledScope(!levelCreator.islandStartsLocked))
            {
                levelCreator.islandUnlockRequirement = Mathf.Max(
                    0,
                    EditorGUILayout.IntField(
                        "Unlock After Completed Islands",
                        levelCreator.islandUnlockRequirement));
            }
        }

        private void DrawPendingSelectionHelp(string helpText)
        {
            EditorGUILayout.HelpBox(helpText, MessageType.Info);

            if (hasPendingCoordinate)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    "First Point: " + pendingCoordinate,
                    EditorStyles.boldLabel);

                if (GUILayout.Button("Cancel Selection", GUILayout.Width(130f)))
                {
                    CancelPendingSelection();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawGrid()
        {
            LevelData data = levelCreator.GetLevelData();
            if (data == null || data.GridData == null)
            {
                EditorGUILayout.HelpBox("LevelData is not available.", MessageType.Error);
                return;
            }

            //EditorGUILayout.Space(4f);
            //EditorGUILayout.BeginVertical("box");
            //EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Large Grid Editor", GUILayout.Width(190f)))
            {
                HashiGridEditorWindow.Open(levelCreator);
            }

            EditorGUILayout.EndHorizontal();

            List<IslandCellData> islands = data.GetIslandCells();
            EditorGUILayout.LabelField(
                "Islands: " + islands.Count +
                "   Fixed Bridges: " + data.fixedBridges.Count +
                "   Chains: " + data.chainBarriers.Count);

            const float cellWidth = 80f;
            const float cellHeight = 80f;
            float viewHeight = Mathf.Clamp(data.Height * 86f + 20f, 180f, 720f);

            gridScrollPosition = EditorGUILayout.BeginScrollView(
                gridScrollPosition,
                true,
                true,
                GUILayout.Height(viewHeight));

            for (int y = data.Height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();

                for (int x = 0; x < data.Width; x++)
                {
                    DrawGridCell(
                        new Vector2Int(x, y),
                        cellWidth,
                        cellHeight,
                        data);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawGridCell(
            Vector2Int coordinate,
            float width,
            float height,
            LevelData data)
        {
            Rect rect = GUILayoutUtility.GetRect(
                width,
                height,
                GUILayout.Width(width),
                GUILayout.Height(height));

            IslandCellData island =
                data.GridData[coordinate.x, coordinate.y].BasePlaceable as IslandCellData;

            Color background = new Color(0.35f, 0.35f, 0.35f);

            if (island != null)
            {
                background = island.startsLocked
                    ? new Color(0.45f, 0.45f, 0.45f)
                    : new Color(0.35f, 0.6f, 0.38f);
            }

            if (hasPendingCoordinate && pendingCoordinate == coordinate)
            {
                background = new Color(0.8f, 0.65f, 0.2f);
            }

            EditorGUI.DrawRect(rect, background);

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            GUI.Box(rect, BuildCellText(coordinate, island, data), style);
            HandleGridCellInput(rect, coordinate, island);
        }

        private string BuildCellText(
            Vector2Int coordinate,
            IslandCellData island,
            LevelData data)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine(coordinate.x + "," + coordinate.y);

            if (island != null)
            {
                string mode = island.bridgeMode == EnumHolder.IslandBridgeMode.DoubleAllowed
                    ? "DOUBLE"
                    : "SINGLE";

                builder.AppendLine("ISLAND " + island.requiredBridgeCount);
                builder.AppendLine(mode);

                if (island.startsLocked)
                {
                    builder.AppendLine("LOCK " + island.unlockAfterCompletedIslandCount);
                }
            }

            for (int i = 0; i < data.fixedBridges.Count; i++)
            {
                FixedBridgeDefinitionData bridge = data.fixedBridges[i];

                if (bridge.startCoordinate == coordinate)
                {
                    builder.AppendLine("F" + bridge.id + " A x" + bridge.bridgeCount);
                }
                else if (bridge.endCoordinate == coordinate)
                {
                    builder.AppendLine("F" + bridge.id + " B x" + bridge.bridgeCount);
                }
            }

            for (int i = 0; i < data.chainBarriers.Count; i++)
            {
                ChainBarrierData chain = data.chainBarriers[i];

                if (chain.startCoordinate == coordinate)
                {
                    builder.AppendLine("C" + chain.id + " A");
                }
                else if (chain.endCoordinate == coordinate)
                {
                    builder.AppendLine("C" + chain.id + " B");
                }
            }

            return builder.ToString();
        }

        private void HandleGridCellInput(
            Rect rect,
            Vector2Int coordinate,
            IslandCellData island)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseDown ||
                !rect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.button == 1)
            {
                if (island != null)
                {
                    levelCreator.RemoveIslandAt(coordinate);
                    SetEditorMessage(
                        "Island removed at " + coordinate + ".",
                        MessageType.Info);
                }

                CancelPendingSelection(false);
                currentEvent.Use();
                Repaint();
                return;
            }

            if (currentEvent.button != 0)
            {
                return;
            }

            switch (editorMode)
            {
                case EnumHolder.HashiEditorMode.Island:
                    levelCreator.SetIslandAt(coordinate);
                    SetEditorMessage(
                        "Island placed at " + coordinate + ".",
                        MessageType.Info);
                    break;

                case EnumHolder.HashiEditorMode.FixedBridge:
                    HandleFixedBridgePoint(coordinate, island);
                    break;

                case EnumHolder.HashiEditorMode.Chain:
                    HandleChainPoint(coordinate);
                    break;
            }

            currentEvent.Use();
            Repaint();
        }

        private void HandleFixedBridgePoint(
            Vector2Int coordinate,
            IslandCellData island)
        {
            if (island == null)
            {
                SetEditorMessage(
                    "Fixed bridge points must contain islands.",
                    MessageType.Error);
                return;
            }

            if (!hasPendingCoordinate)
            {
                pendingCoordinate = coordinate;
                hasPendingCoordinate = true;
                SetEditorMessage(
                    "Select the second island for the fixed bridge.",
                    MessageType.Info);
                return;
            }

            if (pendingCoordinate == coordinate)
            {
                SetEditorMessage(
                    "Select a different island.",
                    MessageType.Warning);
                return;
            }

            bool success = levelCreator.TryAddFixedBridge(
                pendingCoordinate,
                coordinate,
                out string error);

            if (success)
            {
                SetEditorMessage("Fixed bridge added.", MessageType.Info);
                CancelPendingSelection(false);
            }
            else
            {
                SetEditorMessage(error, MessageType.Error);
            }
        }

        private void HandleChainPoint(Vector2Int coordinate)
        {
            if (!hasPendingCoordinate)
            {
                pendingCoordinate = coordinate;
                hasPendingCoordinate = true;
                SetEditorMessage(
                    "Select the second grid point for the chain.",
                    MessageType.Info);
                return;
            }

            if (pendingCoordinate == coordinate)
            {
                SetEditorMessage(
                    "Select a different grid point.",
                    MessageType.Warning);
                return;
            }

            bool success = levelCreator.TryAddChain(
                pendingCoordinate,
                coordinate,
                out string error);

            if (success)
            {
                SetEditorMessage("Chain added.", MessageType.Info);
                CancelPendingSelection(false);
            }
            else
            {
                SetEditorMessage(error, MessageType.Error);
            }
        }

        private void DrawDefinitionLists()
        {
            LevelData data = levelCreator.GetLevelData();
            if (data == null)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Connection Definitions", EditorStyles.boldLabel);

            definitionScrollPosition = EditorGUILayout.BeginScrollView(
                definitionScrollPosition,
                GUILayout.MaxHeight(360f));

            DrawFixedBridgeList(data);
            EditorGUILayout.Space(8f);
            DrawChainList(data);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawFixedBridgeList(LevelData data)
        {
            EditorGUILayout.LabelField("Fixed Bridges", EditorStyles.boldLabel);

            if (data.fixedBridges.Count == 0)
            {
                EditorGUILayout.LabelField("None");
                return;
            }

            for (int i = 0; i < data.fixedBridges.Count; i++)
            {
                FixedBridgeDefinitionData bridge = data.fixedBridges[i];
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(
                    "ID " + bridge.id + "   " +
                    bridge.startCoordinate + " -> " + bridge.endCoordinate,
                    GUILayout.MinWidth(250f));

                int newCount = EditorGUILayout.IntPopup(
                    bridge.bridgeCount,
                    new[] { "Single", "Double" },
                    new[] { 1, 2 },
                    GUILayout.Width(90f));

                if (newCount != bridge.bridgeCount)
                {
                    bridge.bridgeCount = newCount;
                    EditorUtility.SetDirty(levelCreator);
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    levelCreator.RemoveFixedBridge(bridge.id);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawChainList(LevelData data)
        {
            EditorGUILayout.LabelField("Chains", EditorStyles.boldLabel);

            if (data.chainBarriers.Count == 0)
            {
                EditorGUILayout.LabelField("None");
                return;
            }

            for (int i = 0; i < data.chainBarriers.Count; i++)
            {
                ChainBarrierData chain = data.chainBarriers[i];
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(
                    "ID " + chain.id + "   " +
                    chain.startCoordinate + " -> " + chain.endCoordinate,
                    GUILayout.MinWidth(250f));

                int newRequirement = Mathf.Max(
                    0,
                    EditorGUILayout.IntField(
                        chain.unlockAfterCompletedIslandCount,
                        GUILayout.Width(55f)));

                if (newRequirement != chain.unlockAfterCompletedIslandCount)
                {
                    chain.unlockAfterCompletedIslandCount = newRequirement;
                    EditorUtility.SetDirty(levelCreator);
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    levelCreator.RemoveChain(chain.id);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawValidationPanel()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            HashiValidationResult validation = levelCreator.GetValidationResult();

            if (validation.issues.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No level data errors were found.",
                    MessageType.Info);
            }
            else
            {
                for (int i = 0; i < validation.issues.Count; i++)
                {
                    HashiValidationIssue issue = validation.issues[i];
                    MessageType messageType =
                        issue.severity == HashiValidationSeverity.Error
                            ? MessageType.Error
                            : MessageType.Warning;

                    EditorGUILayout.HelpBox(issue.message, messageType);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void SetEditorMessage(string message, MessageType messageType)
        {
            editorMessage = message;
            editorMessageType = messageType;
        }

        private void CancelPendingSelection(bool clearMessage = true)
        {
            hasPendingCoordinate = false;
            pendingCoordinate = default;

            if (clearMessage)
            {
                editorMessage = string.Empty;
            }
        }
    }
}
#endif
