#if UNITY_EDITOR
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using BoxPuller.Scripts.Runtime.LevelCreation;
using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Editor
{
    public class HashiGridEditorWindow : EditorWindow
    {
        private LevelCreator levelCreator;
        private Vector2 gridScrollPosition;
        private Vector2 sideScrollPosition;

        private EnumHolder.HashiEditorMode editorMode =
            EnumHolder.HashiEditorMode.Island;

        private bool hasPendingCoordinate;
        private Vector2Int pendingCoordinate;

        private string editorMessage;
        private MessageType editorMessageType = MessageType.Info;

        private float cellSize = 54f;
        private bool showCoordinates = true;
        private bool showDetails = true;

        public static void Open(LevelCreator creator)
        {
            HashiGridEditorWindow window =
                GetWindow<HashiGridEditorWindow>("Hashi Grid");

            window.levelCreator = creator;
            window.minSize = new Vector2(900f, 600f);
            window.Show();
            window.Focus();
        }

        private void OnGUI()
        {
            DrawHeader();

            if (levelCreator == null)
            {
                EditorGUILayout.HelpBox(
                    "Select a LevelCreator object first.",
                    MessageType.Warning);
                return;
            }

            levelCreator.EnsureLevelData();

            EditorGUILayout.BeginHorizontal();

            DrawSidePanel();
            DrawGridPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            levelCreator = (LevelCreator)EditorGUILayout.ObjectField(
                "Level Creator",
                levelCreator,
                typeof(LevelCreator),
                true);

            if (GUILayout.Button("Use Selection", GUILayout.Width(110f)))
            {
                GameObject selected = Selection.activeGameObject;
                if (selected != null)
                {
                    levelCreator = selected.GetComponent<LevelCreator>();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (levelCreator != null)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Generate Level", GUILayout.Height(28f)))
                {
                    levelCreator.GenerateLevel();
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Save", GUILayout.Height(28f)))
                {
                    levelCreator.SaveLevel();
                }

                if (GUILayout.Button("Load", GUILayout.Height(28f)))
                {
                    levelCreator.LoadLevel();
                    ClearPendingSelection();
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button("Reset", GUILayout.Height(28f)))
                {
                    bool confirmed = EditorUtility.DisplayDialog(
                        "Reset Level",
                        "Delete the current level data and generated prefab?",
                        "Reset",
                        "Cancel");

                    if (confirmed)
                    {
                        levelCreator.ResetLevel();
                        ClearPendingSelection();
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSidePanel()
        {
            EditorGUILayout.BeginVertical(
                "box",
                GUILayout.Width(310f),
                GUILayout.ExpandHeight(true));

            sideScrollPosition = EditorGUILayout.BeginScrollView(sideScrollPosition);

            EditorGUILayout.LabelField("Paint Settings", EditorStyles.boldLabel);

            EnumHolder.HashiEditorMode previousMode = editorMode;

            editorMode = (EnumHolder.HashiEditorMode)GUILayout.Toolbar(
                (int)editorMode,
                new[] { "Island", "Fixed", "Chain" },
                GUILayout.Height(32f));

            if (previousMode != editorMode)
            {
                ClearPendingSelection();
            }

            EditorGUILayout.Space(8f);

            DrawModeSettings();

            EditorGUILayout.Space(8f);

            EditorGUILayout.LabelField("View", EditorStyles.boldLabel);

            cellSize = EditorGUILayout.Slider(
                "Cell Size",
                cellSize,
                28f,
                100f);

            showCoordinates = EditorGUILayout.Toggle(
                "Show Coordinates",
                showCoordinates);

            showDetails = EditorGUILayout.Toggle(
                "Show Details",
                showDetails);

            EditorGUILayout.Space(8f);

            DrawLevelInfo();
            DrawMessage();
            DrawDefinitions();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawModeSettings()
        {
            switch (editorMode)
            {
                case EnumHolder.HashiEditorMode.Island:
                    DrawIslandSettings();
                    EditorGUILayout.HelpBox(
                        "Left click places or updates an island. Right click removes it.",
                        MessageType.Info);
                    break;

                case EnumHolder.HashiEditorMode.FixedBridge:
                    levelCreator.fixedBridgeCount = EditorGUILayout.IntPopup(
                        "Fixed Bridge Count",
                        Mathf.Clamp(levelCreator.fixedBridgeCount, 1, 2),
                        new[] { "Single", "Double" },
                        new[] { 1, 2 });

                    DrawPendingInfo(
                        "Select two island cells for a fixed bridge.");
                    break;

                case EnumHolder.HashiEditorMode.Chain:
                    levelCreator.chainUnlockRequirement = Mathf.Max(
                        0,
                        EditorGUILayout.IntField(
                            "Unlock After Completed Islands",
                            levelCreator.chainUnlockRequirement));

                    DrawPendingInfo(
                        "Select any two grid points for a chain.");
                    break;
            }
        }

        private void DrawIslandSettings()
        {
            levelCreator.islandRequiredBridgeCount = Mathf.Max(
                1,
                EditorGUILayout.IntField(
                    "Required Bridge Count",
                    levelCreator.islandRequiredBridgeCount));

            levelCreator.islandBridgeMode =
                (EnumHolder.IslandBridgeMode)EditorGUILayout.EnumPopup(
                    "Bridge Mode",
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

        private void DrawPendingInfo(string text)
        {
            EditorGUILayout.HelpBox(text, MessageType.Info);

            if (!hasPendingCoordinate)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                "First Point: " + pendingCoordinate,
                EditorStyles.boldLabel);

            if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
            {
                ClearPendingSelection();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLevelInfo()
        {
            LevelData data = levelCreator.GetLevelData();
            if (data == null)
            {
                return;
            }

            List<IslandCellData> islands = data.GetIslandCells();

            EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Index: " + levelCreator.levelIndex);
            EditorGUILayout.LabelField("Grid: " + data.Width + " x " + data.Height);
            EditorGUILayout.LabelField("Islands: " + islands.Count);
            EditorGUILayout.LabelField("Fixed Bridges: " + data.fixedBridges.Count);
            EditorGUILayout.LabelField("Chains: " + data.chainBarriers.Count);
        }

        private void DrawMessage()
        {
            if (string.IsNullOrEmpty(editorMessage))
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(editorMessage, editorMessageType);
        }

        private void DrawGridPanel()
        {
            LevelData data = levelCreator.GetLevelData();

            if (data == null || data.GridData == null)
            {
                EditorGUILayout.HelpBox(
                    "LevelData is not available.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Large Grid", EditorStyles.boldLabel);

            gridScrollPosition = EditorGUILayout.BeginScrollView(
                gridScrollPosition,
                true,
                true,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            for (int y = data.Height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();

                for (int x = 0; x < data.Width; x++)
                {
                    DrawCell(new Vector2Int(x, y), data);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawCell(Vector2Int coordinate, LevelData data)
        {
            Rect rect = GUILayoutUtility.GetRect(
                cellSize,
                cellSize,
                GUILayout.Width(cellSize),
                GUILayout.Height(cellSize));

            IslandCellData island =
                data.GridData[coordinate.x, coordinate.y].BasePlaceable
                as IslandCellData;

            Color background = new Color(0.32f, 0.32f, 0.32f);

            if (island != null)
            {
                background = island.startsLocked
                    ? new Color(0.48f, 0.48f, 0.48f)
                    : new Color(0.33f, 0.58f, 0.36f);
            }

            if (hasPendingCoordinate && pendingCoordinate == coordinate)
            {
                background = new Color(0.85f, 0.66f, 0.22f);
            }

            EditorGUI.DrawRect(rect, background);

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(cellSize * 0.13f, 7f, 12f)),
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            GUI.Box(rect, BuildCellText(coordinate, island, data), style);

            HandleCellInput(rect, coordinate, island);
        }

        private string BuildCellText(
            Vector2Int coordinate,
            IslandCellData island,
            LevelData data)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            if (showCoordinates)
            {
                builder.AppendLine(coordinate.x + "," + coordinate.y);
            }

            if (island != null)
            {
                string mode = island.bridgeMode ==
                              EnumHolder.IslandBridgeMode.DoubleAllowed
                    ? "DOUBLE "
                    : "SINGLE ";

                if (showDetails)
                {
                    builder.AppendLine("ISLAND " + island.requiredBridgeCount);
                    builder.AppendLine(mode);

                    if (island.startsLocked)
                    {
                        builder.AppendLine("LOCK " +
                                           island.unlockAfterCompletedIslandCount);
                    }
                }
                else
                {
                    builder.AppendLine(island.requiredBridgeCount + " " + mode);
                }
            }

            if (showDetails)
            {
                AppendConnectionMarks(builder, coordinate, data);
            }

            return builder.ToString();
        }

        private void AppendConnectionMarks(
            System.Text.StringBuilder builder,
            Vector2Int coordinate,
            LevelData data)
        {
            for (int i = 0; i < data.fixedBridges.Count; i++)
            {
                FixedBridgeDefinitionData bridge = data.fixedBridges[i];

                if (bridge.startCoordinate == coordinate)
                {
                    builder.AppendLine("F" + bridge.id + "A");
                }
                else if (bridge.endCoordinate == coordinate)
                {
                    builder.AppendLine("F" + bridge.id + "B");
                }
            }

            for (int i = 0; i < data.chainBarriers.Count; i++)
            {
                ChainBarrierData chain = data.chainBarriers[i];

                if (chain.startCoordinate == coordinate)
                {
                    builder.AppendLine("C" + chain.id + "A");
                }
                else if (chain.endCoordinate == coordinate)
                {
                    builder.AppendLine("C" + chain.id + "B");
                }
            }
        }

        private void HandleCellInput(
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
                    SetMessage(
                        "Island removed at " + coordinate + ".",
                        MessageType.Info);
                }

                ClearPendingSelection(false);
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
                    SetMessage(
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
                SetMessage(
                    "Fixed bridge points must contain islands.",
                    MessageType.Error);
                return;
            }

            if (!hasPendingCoordinate)
            {
                pendingCoordinate = coordinate;
                hasPendingCoordinate = true;
                SetMessage(
                    "Select the second island.",
                    MessageType.Info);
                return;
            }

            if (pendingCoordinate == coordinate)
            {
                SetMessage(
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
                SetMessage("Fixed bridge added.", MessageType.Info);
                ClearPendingSelection(false);
            }
            else
            {
                SetMessage(error, MessageType.Error);
            }
        }

        private void HandleChainPoint(Vector2Int coordinate)
        {
            if (!hasPendingCoordinate)
            {
                pendingCoordinate = coordinate;
                hasPendingCoordinate = true;
                SetMessage(
                    "Select the second grid point.",
                    MessageType.Info);
                return;
            }

            if (pendingCoordinate == coordinate)
            {
                SetMessage(
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
                SetMessage("Chain added.", MessageType.Info);
                ClearPendingSelection(false);
            }
            else
            {
                SetMessage(error, MessageType.Error);
            }
        }

        private void DrawDefinitions()
        {
            LevelData data = levelCreator.GetLevelData();
            if (data == null)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Definitions", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Fixed Bridges", EditorStyles.boldLabel);

            if (data.fixedBridges.Count == 0)
            {
                EditorGUILayout.LabelField("None");
            }

            for (int i = 0; i < data.fixedBridges.Count; i++)
            {
                FixedBridgeDefinitionData bridge = data.fixedBridges[i];

                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(
                    "ID " + bridge.id + " " +
                    bridge.startCoordinate + " -> " +
                    bridge.endCoordinate);

                if (GUILayout.Button("X", GUILayout.Width(26f)))
                {
                    levelCreator.RemoveFixedBridge(bridge.id);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Chains", EditorStyles.boldLabel);

            if (data.chainBarriers.Count == 0)
            {
                EditorGUILayout.LabelField("None");
            }

            for (int i = 0; i < data.chainBarriers.Count; i++)
            {
                ChainBarrierData chain = data.chainBarriers[i];

                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(
                    "ID " + chain.id + " " +
                    chain.startCoordinate + " -> " +
                    chain.endCoordinate +
                    " R:" + chain.unlockAfterCompletedIslandCount);

                if (GUILayout.Button("X", GUILayout.Width(26f)))
                {
                    levelCreator.RemoveChain(chain.id);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void SetMessage(string message, MessageType type)
        {
            editorMessage = message;
            editorMessageType = type;
        }

        private void ClearPendingSelection(bool clearMessage = true)
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