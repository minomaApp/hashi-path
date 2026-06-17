using System.Collections.Generic;
using System.Text;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using BoxPuller.Scripts.Runtime.LevelCreation;
using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Editor
{
    [CustomEditor(typeof(LevelCreator))]
    public class LevelCreatorEditor : GridEditor
    {
        private LevelCreator _levelCreator;
        private Vector2 colorScrollPosition;
        private Vector2 directionScrollPosition;
        private Vector2 gridScrollPosition;
        private Vector2 conveyorScrollPosition;
        private Vector2 targetQueueScrollPosition;
        private Vector2 colorEnumScrollPosition;
        private GridCellData savedCellToCarry;
        private BasePlaceableData savedPlaceableToCarry;
        private bool isGridPaintDragging;
        private int lastPaintedX = -1;
        private int lastPaintedY = -1;
        private bool isGridEraseDragging;

        private bool showPrefabs;
        private bool showTargetQueueSettings;
        private bool showMatchAreaSettings;
        private bool showGridSettings = true;
        private bool showGridSpaceSettings;
        private bool displayColorAndObjectCount = true;

        private void OnEnable()
        {
            _levelCreator = (LevelCreator)target;
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            BeginChangeCheck();

            DisplayPrefabs();
            DisplayGridSettings();
            DisplayLevelButtons();

            if (_levelCreator.GetLevelData() == null) _levelCreator.LoadLevel();
            if (_levelCreator.GetLevelData().GridData == null) _levelCreator.LoadLevel();

            if (!IsLevelDataAvailable())
            {
                DisplayHelpBox("Please Reset the Grid!", MessageType.Error);
                return;
            }

            DisplayEnumButtons();
            DisplayBottomShooterLanes();
            DisplayGrid();

            // Eski conveyor/color mismatch sistemi yeni oyunda generate'i kilitlemesin.
            // DisplayColorCounts();

            if (EndChangeCheck())
            {
                Undo.RecordObject(_levelCreator, "Change Level Index");
                EditorUtility.SetDirty(_levelCreator);
            }

            serializedObject.ApplyModifiedProperties();
        }


        #region Display Prefabs

        private void DisplayPrefabs()
        {
            Space(3);
            BeginVerticalBoxed("Prefabs");
            EditorGUI.indentLevel++;
            showPrefabs = DisplayFoldout("", showPrefabs);
            if (showPrefabs)
            {
                EditorGUI.indentLevel++;
                DisplayObjectField("Normal Object Prefab", ref _levelCreator.normalObjectPrefab);
                DisplayObjectField("Spawner Prefab", ref _levelCreator.spawnerObjectPrefab);
                DisplayObjectField("Locked Object Prefab", ref _levelCreator.lockedObjectPrefab);
                DisplayObjectField("Hidden Object Prefab", ref _levelCreator.hiddenObjectPrefab);
                Space(5);
                DisplayObjectField("Match Area Prefab", ref _levelCreator.matchAreaPrefab);
                Space(5);
                DisplayObjectField("Target Object Prefab", ref _levelCreator.targetObjectPrefab);
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            EndVerticalBoxed();
        }

        #endregion


        #region Display Settings Functions

        private void DisplayGridSettings()
        {
            Space(3);

            BeginVerticalBoxed("Grid Settings");
            EditorGUI.indentLevel++;

            showGridSettings = DisplayFoldout("", showGridSettings);
            if (showGridSettings)
            {
                EditorGUI.indentLevel++;
                DisplayEnum("Grid Type", ref _levelCreator.gridType);
                showGridSpaceSettings = DisplayFoldout("Grid Space Settings", showGridSpaceSettings);
                if (showGridSpaceSettings)
                {
                    EditorGUI.indentLevel++;
                    DisplayFloatField("Horizontal Space", ref _levelCreator.horizontalSpaceModifier);
                    DisplayFloatField("Vertical Space", ref _levelCreator.verticalSpaceModifier);
                    DisplayFloatField("Empty Area Space", ref _levelCreator.emptyAreaSpaceModifier);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
                Space(5);
                DisplayIntFieldWithIncrementDecrementButton("Level Index", ref _levelCreator.levelIndex);

                BeginHorizontal();
                DisplayIntFieldWithIncrementDecrementButton("Grid Width", ref _levelCreator.gridWidth,
                    () => OnGridSizeChanged());
                DisplayButton(_levelCreator.expandGridToLeft ? "\u2b05" : "\u27a1",
                    () => _levelCreator.expandGridToLeft = !_levelCreator.expandGridToLeft,
                    20, 40);
                EndHorizontal();

                BeginHorizontal();
                DisplayIntFieldWithIncrementDecrementButton("Grid Height", ref _levelCreator.gridHeight,
                    OnGridSizeChanged);
                DisplayButton(_levelCreator.expandGridToUp ? "\u2b07" : "\u2b06",
                    () => _levelCreator.expandGridToUp = !_levelCreator.expandGridToUp,
                    20, 40);
                EndHorizontal();

                _levelCreator.conveyorLength = 0;
            }

            EditorGUI.indentLevel--;
            EndVerticalBoxed();
        }

        #endregion


        #region Display Level Buttons

        private void DisplayLevelButtons()
        {
            DisplayMiniSeparator();
            BeginHorizontalBoxed();

            bool mismatch = HasColorMismatch();

            // Eğer mismatch varsa bu üçü disable et
            GUI.enabled = !mismatch;
            SetBackgroundColor(LIGHT_GREEN);
            DisplayButton("Generate Level", () => _levelCreator.GenerateLevel(), 35);
            SetBackgroundColor(LIGHT_YELLOW);
            DisplayButton("Save", () =>
            {
                _levelCreator.SaveLevel();
                // _levelCreator.LoadLevel();
            }, 35);
            SetBackgroundColor(LIGHT_BLUE);
            DisplayButton("Load", () => _levelCreator.LoadLevel(), 35);

            // Sonra restore enabled ve draw Reset
            GUI.enabled = true;
            SetBackgroundColor(LIGHT_RED);
            DisplayButton("Reset", () =>
            {
                // Confirmation popup
                if (EditorUtility.DisplayDialog(
                        "Reset Level",
                        "Are you sure you want to reset the level?",
                        "Yes",
                        "No"))
                {
                    _levelCreator.ResetLevel();
                }
            }, 35);

            // Restore defaults
            ResetBackgroundColor();
            GUI.enabled = true;
            EndHorizontalBoxed();
        }

        #endregion


        #region Display Enum Buttons

        private void DisplayEnumButtons()
        {
            Space(5);

            BeginVerticalBoxed("Paint Settings");

            DisplayEnumButtons(ref _levelCreator.color, "Colors", _levelCreator.gameColors.editorColors);
            SetEnumWithKeyboardInput(ref _levelCreator.color);

            Space(10);
            DisplayMiniSeparator();

            _levelCreator.boxMoldGroupId = EditorGUILayout.IntField("Box Mold Group Id (-1 = None)", _levelCreator.boxMoldGroupId);

            Space(10);
            DisplayMiniSeparator();

            _levelCreator.shooterBulletCount = EditorGUILayout.IntField("Shooter Bullet Count", _levelCreator.shooterBulletCount);
            _levelCreator.shooterLinkGroupId = EditorGUILayout.IntField("Shooter Link Group Id (-1 = None)", _levelCreator.shooterLinkGroupId);
            _levelCreator.shooterIsHidden = EditorGUILayout.Toggle("Shooter Hidden", _levelCreator.shooterIsHidden);

            ResetAllColors();
            EndVerticalBoxed();
        }
        #endregion


        #region Grid Functions

        private void DisplayConveyors()
        {
            Space(5);
            BeginVerticalBoxed();
            var conveyorData = _levelCreator.GetLevelData().ConveyorData;
            if (conveyorData == null || conveyorData.Length == 0)
            {
                EditorGUILayout.HelpBox("Conveyor Length is zero.", MessageType.Info);
                EndVerticalBoxed();
                return;
            }

            float cellW = 50f, cellH = 50f;

            BeginHorizontal();
            DisplayLabelField("Conveyor Preview", 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            DisplayWindowPopup();
            EndHorizontal();
            Space(10);

            DrawNormalGrid(
                _levelCreator.gridWidth,
                _levelCreator.conveyorLength,
                (x, y) => DisplayConveyorCell(x, y, cellW, cellH),
                ref conveyorScrollPosition
            );

            EndVerticalBoxed();
        }

        private void DisplayConveyorCell(int x, int y, float width, float height)
        {
            if (x >= _levelCreator.GetLevelData().Width)
            {
                _levelCreator.conveyorLength = 0;
            }
            var cell = _levelCreator.GetLevelData().ConveyorData[x, y];

            SetColor(Color.white);
            SetBackgroundColor(cell.isActive ? Color.white : Color.gray);

            int fontSize = 8;
            string cellText = GetConveyorCellText(x, y, ref fontSize);
            var style = new GUIStyle(GUI.skin.button)
            {
                richText = true,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };

            DisplayButton(
                cellText, width, height,
                direction => ConveyorButtonAction(x, y, direction, _levelCreator.isSecret),
                () => ConveyorRemoveButtonAction(x, y),
                () => savedPlaceableToCarry = cell.BasePlaceable,
                null,
                null,
                style
            );

            ResetBackgroundColor();
            ResetColor();
        }

        private string GetConveyorCellText(int x, int y, ref int fontSize)
        {
            var cell = _levelCreator.GetLevelData().ConveyorData[x, y];
            var placeable = cell.BasePlaceable;
            string txt = "";

            if (!cell.isActive)
            {
            }
            else if (cell.blockCount > 0)
            {
                txt = "\nLOCK\n" + cell.blockCount;
                SetBackgroundColor(Color.black);
            }
            else if (placeable is ConveyorItemData itemData)
            {
                txt += GetColoredRichText("📦", GetEditorColor((int)itemData.Color)) + "\n";
                if (itemData.isSecret) txt += GetColoredRichText("S", Color.black) + "\n";
                SetBackgroundColor(GetEditorColor((int)itemData.Color));
                fontSize = 22;
            }
            else if (placeable is ChainData chain)
            {
                if (chain.isHead) txt += "H";
                if (chain.isFrozen) txt += "❄️" + chain.BlockCount;
                txt += GetColoredRichText("🔗", GetEditorColor((int)chain.Color)) + "\n";
                txt += DirectionToEmoji(chain.direction);
                SetBackgroundColor(GetEditorColor((int)chain.Color));
                fontSize = 22;
            }
            else if (placeable is KeyFoodData kf)
            {
                txt = GetColoredRichText($"{kf.id}🔑🥝\n", GetEditorColor((int)kf.Color));
                fontSize = 15;
                SetBackgroundColor(GetEditorColor((int)kf.Color));
            }
            else if (placeable is LockFoodData lf)
            {
                txt = GetColoredRichText($"{lf.id}🔒🥝\n", GetEditorColor((int)lf.Color));
                fontSize = 15;
                SetBackgroundColor(GetEditorColor((int)lf.Color));
            }
            else if (placeable is FoodData f)
            {
                if (f.isFrozen) txt += "❄️\n";
                txt += GetColoredRichText("🥝\n", GetEditorColor((int)f.Color));
                SetBackgroundColor(GetEditorColor((int)f.Color));
                fontSize = 18;
                return txt;
            }
            else if (placeable is SingleObjectData so)
            {
                txt = GetSingleObjectText(so, x, y);
                SetBackgroundColor(GetEditorColor((int)so.Color));
            }
            else if (placeable is StackedObjectData sod)
            {
                for (int i = sod.Stack.Count - 1; i >= 0; i--)
                    txt += "\n" + GetCellTextForStacked(sod.Stack[i], x, y);
            }

            return txt;
        }


        private void DisplayGrid()
        {
            Space(5);
            BeginVerticalBoxed();
            Space(20);
            var gridData = _levelCreator.GetLevelData().GridData;
            if (ReferenceEquals(gridData, null) || gridData.Length.Equals(0)) return;

            float cellWidth = 50f;
            float cellHeight = 50f;

            BeginHorizontal();
            DisplayLabelField("Grid", 20, Color.white, TextAnchor.UpperCenter, FontStyle.Bold);
            Space(20);
            DisplayWindowPopup();
            EndHorizontal();

            Space(10);
            DisplayBoxColorCounts();
            Space(15);

            switch (_levelCreator.gridType)
            {
                case EnumHolder.GridType.Normal:
                    DrawNormalGrid(_levelCreator.gridWidth, _levelCreator.gridHeight,
                        (x, y) => DisplayGridCell(x, y, cellWidth, cellHeight), ref gridScrollPosition);
                    break;
                case EnumHolder.GridType.DynamicSpaced:
                    DrawScrollableDynamicSpacedGrid(_levelCreator.gridWidth, _levelCreator.gridHeight,
                        cellWidth, cellHeight, (x, y) => DisplayGridCell(x, y, cellWidth, cellHeight),
                        _levelCreator.GetLevelData(), ref gridScrollPosition);
                    break;

                case EnumHolder.GridType.Hexagon:
                    DrawHexagonGrid(_levelCreator.gridWidth, _levelCreator.gridHeight,
                        (x, y, rect) => DisplayGridCell(x, y, cellWidth, cellHeight));
                    break;
            }

            Space(20);
            SetBackgroundColor(Color.grey);

            ResetBackgroundColor();
            ResetContentColor();
            ResetColor();
            EndVerticalBoxed();
        }

        private void DisplayGridCell(int x, int y, float buttonWidth, float buttonHeight, Rect buttonRect = default)
        {
            var cell = _levelCreator.GetLevelData().GridData[x, y];

            SetColor(Color.white);
            SetBackgroundColor(Color.white);

            int fontSize = 8;
            var cellText = GetCellText(x, y, ref fontSize);

            var style = new GUIStyle(GUI.skin.button)
            {
                richText = true,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };

            Rect cellRect = buttonRect != default
                ? buttonRect
                : GUILayoutUtility.GetRect(
                    buttonWidth,
                    buttonHeight,
                    GUILayout.Width(buttonWidth),
                    GUILayout.Height(buttonHeight)
                );

            HandleGridDragPaint(x, y, cellRect);

            // ÖNEMLİ:
            // GUI.Button kullanmıyoruz, çünkü mouse drag eventini yutabiliyor.
            // Sadece görsel çiziyoruz.
            GUI.Box(cellRect, cellText, style);

            SetColor(Color.white);
            SetBackgroundColor(Color.white);
            SetContentColor(Color.white);
        }
        private void HandleGridDragPaint(int x, int y, Rect cellRect)
        {
            Event currentEvent = Event.current;

            if (currentEvent == null)
                return;

            if (currentEvent.type == EventType.MouseUp)
            {
                isGridPaintDragging = false;
                isGridEraseDragging = false;
                lastPaintedX = -1;
                lastPaintedY = -1;
                return;
            }

            if (!cellRect.Contains(currentEvent.mousePosition))
                return;

            if (currentEvent.type == EventType.MouseDown)
            {
                if (currentEvent.button == 0)
                {
                    isGridPaintDragging = true;
                    isGridEraseDragging = false;
                }
                else if (currentEvent.button == 1)
                {
                    isGridPaintDragging = true;
                    isGridEraseDragging = true;
                }
                else
                {
                    return;
                }

                lastPaintedX = -1;
                lastPaintedY = -1;

                PaintGridCellByMouseButton(x, y, isGridEraseDragging);

                currentEvent.Use();
                GUI.changed = true;
                Repaint();
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && isGridPaintDragging)
            {
                if (lastPaintedX == x && lastPaintedY == y)
                    return;

                PaintGridCellByMouseButton(x, y, isGridEraseDragging);

                currentEvent.Use();
                GUI.changed = true;
                Repaint();
            }
        }
        private void PaintGridCellByMouseButton(int x, int y, bool remove)
        {
            Undo.RecordObject(_levelCreator, remove ? "Erase Grid Cell" : "Paint Grid Cell");

            if (remove)
            {
                GridRemoveButtonAction(x, y);
            }
            else
            {
                GridButtonAction(x, y, EnumHolder.Direction.None);
            }

            lastPaintedX = x;
            lastPaintedY = y;

            EditorUtility.SetDirty(_levelCreator);
        }
        private void DisplayWindowPopup()
        {
            /*if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup"), GUILayout.Width(40), GUILayout.Height(20)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Open Color Window"), false,
                    () => CustomEnumWindowPublisher<ColorWindow>.ShowWindow(CreateInstance<ColorWindow>(),
                        _levelCreator,
                        "Colors"));
                menu.AddItem(new GUIContent("Open Objects Window"), false,
                    () => CustomEnumWindowPublisher<ObjectWindow>.ShowWindow(CreateInstance<ObjectWindow>(),
                        _levelCreator,
                        "Objects"));

                menu.ShowAsContext();
            }*/
        }

        private void DisplayBoxColorCounts()
        {
            var levelData = _levelCreator.GetLevelData();

            if (levelData == null || levelData.GridData == null)
                return;

            Dictionary<EnumHolder.GameColor, int> colorCounts = new Dictionary<EnumHolder.GameColor, int>();

            int totalBoxCount = 0;

            for (int x = 0; x < _levelCreator.gridWidth; x++)
            {
                for (int y = 0; y < _levelCreator.gridHeight; y++)
                {
                    var cell = levelData.GridData[x, y];

                    if (cell == null)
                        continue;

                    if (cell.BasePlaceable is not BoxCellData boxCell)
                        continue;

                    if (boxCell.color == EnumHolder.GameColor.None)
                        continue;

                    if (!colorCounts.ContainsKey(boxCell.color))
                    {
                        colorCounts.Add(boxCell.color, 0);
                    }

                    colorCounts[boxCell.color]++;
                    totalBoxCount++;
                }
            }

            BeginVerticalBoxed("Box Color Counts");

            EditorGUILayout.LabelField($"Total Boxes: {totalBoxCount}", EditorStyles.boldLabel);

            foreach (var pair in colorCounts)
            {
                EnumHolder.GameColor color = pair.Key;
                int count = pair.Value;

                Color editorColor = Color.white;

                int colorIndex = (int)color;

                if (_levelCreator.gameColors != null &&
                    _levelCreator.gameColors.editorColors != null &&
                    colorIndex >= 0 &&
                    colorIndex < _levelCreator.gameColors.editorColors.Length)
                {
                    editorColor = _levelCreator.gameColors.editorColors[colorIndex];
                }

                GUIStyle style = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal =
            {
                textColor = editorColor
            }
                };

                EditorGUILayout.LabelField($"{color}: {count}", style);
            }

            if (colorCounts.Count == 0)
            {
                EditorGUILayout.LabelField("No boxes painted yet.");
            }

            EndVerticalBoxed();
        }

        #endregion


        #region Button Actions

        private void GridButtonAction(int x, int y, EnumHolder.Direction newDirection)
        {
            var levelData = _levelCreator.GetLevelData();

            levelData.SetBoxCell(
                x,
                y,
                _levelCreator.color,
                _levelCreator.boxMoldGroupId
            );
        }

        private void GridRemoveButtonAction(int x, int y)
        {
            _levelCreator.GetLevelData().RemoveBoxCell(x, y);
        }

        private void ConveyorButtonAction(int x, int y, EnumHolder.Direction newDirection, bool isSecret)
        {
            var levelData = _levelCreator.GetLevelData();

            if (_levelCreator.isDirection)
                _levelCreator.direction = newDirection;
            else
                _levelCreator.direction = EnumHolder.Direction.None;

            levelData.SetConveyorCellStack(
                x,
                y,
                _levelCreator.color,
                isSecret
            );
        }

        private void ConveyorRemoveButtonAction(int x, int y)
        {
            _levelCreator
                .GetLevelData()
                .RemoveConveyorCellStack(x, y);
        }


        private void SpawnerQueueButtonAction(int x, int y, int order)
        {
            var level = _levelCreator.GetLevelData();
            level.SetSpawnerCellStack(x, y, order, _levelCreator.color, _levelCreator.direction);
        }


        private void SpawnerQueueRemoveButtonAction(int x, int y, int order)
        {
            _levelCreator.GetLevelData().RemoveSpawnerCellStack(x, y, order);
        }


        private void TargetQueueButtonAction(int x)
        {
            var level = _levelCreator.GetLevelData();
            level.SetQueueCellStack(x, _levelCreator.color, level.TargetQueue);
        }


        private void TargetQueueRemoveButtonAction(int x)
        {
            var level = _levelCreator.GetLevelData();
            _levelCreator.GetLevelData().RemoveQueueCellStack(x, level.TargetQueue);
        }


        private void OnGridSizeChanged()
        {
            var levelData = _levelCreator.GetLevelData();
            if (_levelCreator.gridHeight <= 0 && _levelCreator.gridWidth <= 0 &&
                _levelCreator.GetLevelData().GridData.GetLength(0) <= 0 &&
                levelData.GridData.GetLength(1) <= 0) return;
            levelData.ResizeGridCells(_levelCreator.gridWidth, _levelCreator.gridHeight, _levelCreator.expandGridToUp,
                _levelCreator.expandGridToLeft);
            // Yeni oyunda tek grid kullanılacak, conveyor grid resize edilmeyecek.
            // levelData.ResizeConveyorCells(_levelCreator.gridWidth, _levelCreator.gridHeight,
            //     _levelCreator.expandConveyorToUp, _levelCreator.expandConveyorToLeft);
        }

        private void OnConveyorSizeChanged()
        {
            var data = _levelCreator.GetLevelData();
            if (data == null) return;

            data.ResizeConveyorCells(
                _levelCreator.gridWidth,
                _levelCreator.conveyorLength,
                _levelCreator.expandConveyorToUp,
                _levelCreator.expandConveyorToLeft
            );
            EditorUtility.SetDirty(_levelCreator);
        }


        private void OnTargetGridSizeChanged(int targetQueueLength)
        {
            var levelData = _levelCreator.GetLevelData();
            if (levelData.TargetQueue.Count < 0) return;
            levelData.ResizeList(levelData.TargetQueue, targetQueueLength, levelData.levelDataDefaultObjectType);
        }


        private void OnSpawnerSizeChanged(int x, int y, int spawnerCount, SpawnerObjectData spawnerObject)
        {
            var levelData = _levelCreator.GetLevelData();

            if ((spawnerCount <= 0))
            {
                levelData.RemoveSpawner(x, y);
            }
            else
            {
                levelData.ResizeList(spawnerObject.Stack, spawnerCount, EnumHolder.LevelDataDefaultObjectType.Single);
            }
        }

        #endregion


        #region Utility

        private bool IsLevelDataAvailable()
        {
            if (_levelCreator.gridWidth * _levelCreator.gridHeight != _levelCreator.GetLevelData().GridData.Length)
            {
                Debug.Log("Sizes Doesn't Match: " + _levelCreator.GetLevelData().GridData.Length + " " +
                          _levelCreator.gridWidth + " " + _levelCreator.gridHeight);
            }

            return _levelCreator.gridWidth * _levelCreator.gridHeight == _levelCreator.GetLevelData().GridData.Length &&
                   _levelCreator.gridWidth * _levelCreator.gridHeight != 0;
        }


        // This function is used to get the text for cells Single or Stacked type of objects
        private string GetCellText(int x, int y, ref int fontSize)
        {
            var cell = _levelCreator.GetLevelData().GridData[x, y];
            //string cellText = ""; //$"{cell.coordinates.x} x {cell.coordinates.y} \n";
            string cellText = $"{x},{y}\n";
            var placable = cell.BasePlaceable;


            if (!cell.isActive)
            {
                cellText += "NON ACTIVE";
                SetBackgroundColor(Color.gray);
            }
            else if (cell.blockCount > 0)
            {
                cellText += "\n LOCK \n" + cell.blockCount;
                SetBackgroundColor(Color.black);
            }
            else if (placable is BoxCellData boxCell)
            {
                cellText += GetColoredRichText("BOX\n", GetEditorColor((int)boxCell.color));
                cellText += boxCell.color + "\n";

                if (boxCell.moldGroupId >= 0)
                {
                    cellText += "M:" + boxCell.moldGroupId;
                }

                SetBackgroundColor(GetEditorColor((int)boxCell.color));
                fontSize = 10;
            }
            else if (placable is ChainData chainData)
            {
                if (chainData.isHead)
                {
                    cellText += "H";
                }

                if (chainData.isFrozen) cellText += "❄️" + chainData.BlockCount;
                else cellText += "";

                cellText += GetColoredRichText("🔗", GetEditorColor((int)chainData.Color)) + "\n";
                cellText += DirectionToEmoji(chainData.direction);
                SetBackgroundColor(GetEditorColor((int)chainData.Color));
                fontSize = 22;
            }
            else if (placable is KeyFoodData foodK)
            {
                cellText += GetColoredRichText($"{foodK.id}🔑🥝\n", GetEditorColor((int)foodK.Color));
                fontSize = 15;
                SetBackgroundColor(GetEditorColor((int)foodK.Color));
            }
            else if (placable is LockFoodData foodL)
            {
                cellText += GetColoredRichText($"{foodL.id}🔒🥝\n", GetEditorColor((int)foodL.Color));
                fontSize = 15;
                SetBackgroundColor(GetEditorColor((int)foodL.Color));
            }
            else if (placable is FoodData food)
            {
                if (food.isFrozen) cellText += "❄️\n";
                cellText += GetColoredRichText("🥝\n", GetEditorColor((int)food.Color));
                SetBackgroundColor(GetEditorColor((int)food.Color));

                fontSize = 18;
                return cellText;
            }
            else if (placable is SingleObjectData)
            {
                return GetSingleObjectText(placable, x, y);
            }
            else if (placable is StackedObjectData stackedPlacable)
            {
                if (stackedPlacable.Stack == null) return $"{x} x {y}";
                cellText = $"{x} x {y}";
                for (var i = stackedPlacable.Stack.Count - 1; i >= 0; i--)
                {
                    cellText += "\n" + GetCellTextForStacked(stackedPlacable.Stack[i], x, y);
                }

                return cellText;
            }

            return cellText;
        }


        // This function is used to get the text for Stacked type of objects
        private string GetCellTextForStacked(BasePlaceableData placable, int x, int y)
        {
            var cellText = "";

            if (placable is HiddenObjectData)
            {
                cellText = GetColoredRichText("[H]", BLACK) + GetSingleObjectText(placable, x, y);
            }
            else if (placable is SingleObjectData)
            {
                return GetSingleObjectText(placable, x, y);
            }

            return cellText;
        }


        // This function is used to get the text for Single type of objects
        private string GetSingleObjectText(BasePlaceableData placable, int x, int y)
        {
            var singleObject = (SingleObjectData)placable;
            if (singleObject == null) return $"";
            var color = GetEditorColor((int)singleObject.Color);


            return GetColoredRichText(singleObject.Color.ToString(), color);
        }


        private string DirectionToEmoji(EnumHolder.Direction direction)
        {
            switch (direction)
            {
                case EnumHolder.Direction.Up:
                    return "\u2b06";

                case EnumHolder.Direction.Down:
                    return "\u2b07";

                case EnumHolder.Direction.Left:
                    return "\u2b05";

                case EnumHolder.Direction.Right:
                    return "\u27a1";

                default:
                    return "";
            }
        }
        private bool HasColorMismatch()
        {
            return false;
        }
        //private bool HasColorMismatch()
        //{
        //    var data = _levelCreator.GetLevelData();
        //    if (data == null || data.GridData == null) return false;

        //    // 1) Grid sayımları
        //    var gridCounts = new Dictionary<EnumHolder.GameColor, int>();
        //    for (int x = 0; x < _levelCreator.gridWidth; x++)
        //    {
        //        for (int y = 0; y < _levelCreator.gridHeight; y++)
        //        {
        //            var cell = data.GridData[x, y];
        //            if (cell == null || cell.BasePlaceable == null) continue;

        //            if (cell.BasePlaceable is SingleObjectData so)
        //            {
        //                gridCounts[so.Color] = gridCounts.GetValueOrDefault(so.Color) + 1;
        //            }
        //            else if (cell.BasePlaceable is StackedObjectData st && st.Stack != null)
        //            {
        //                foreach (var elt in st.Stack)
        //                {
        //                    if (elt == null) continue;
        //                    gridCounts[elt.Color] = gridCounts.GetValueOrDefault(elt.Color) + 1;
        //                }
        //            }
        //        }
        //    }

        //    // 2) Conveyor sayımları
        //    var convCounts = new Dictionary<EnumHolder.GameColor, int>();
        //    var conveyorData = data.ConveyorData;
        //    if (conveyorData != null)
        //    {
        //        int w = conveyorData.GetLength(0), h = conveyorData.GetLength(1);
        //        for (var x = 0; x < w; x++)
        //        {
        //            for (var y = 0; y < h; y++)
        //            {
        //                var cell = conveyorData[x, y];
        //                if (cell == null || cell.BasePlaceable == null) continue;

        //                if (cell.BasePlaceable is ConveyorItemData so)
        //                {
        //                    convCounts[so.Color] = convCounts.GetValueOrDefault(so.Color) + 1;
        //                }
        //                else if (cell.BasePlaceable is ConveyorItemData st && st.Stack != null)
        //                {
        //                    foreach (var elt in st.Stack)
        //                    {
        //                        if (elt == null) continue;
        //                        convCounts[elt.Color] = convCounts.GetValueOrDefault(elt.Color) + 1;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    // 3) Karşılaştır
        //    var allColors = new HashSet<EnumHolder.GameColor>(gridCounts.Keys);
        //    foreach (var c in convCounts.Keys) allColors.Add(c);

        //    foreach (var color in allColors)
        //    {
        //        if (gridCounts.GetValueOrDefault(color) != convCounts.GetValueOrDefault(color))
        //            return true;
        //    }

        //    return false;
        //}

        private void DisplayColorCounts()
        {
            Space(20);
            BeginVerticalBoxed();
            var gridData = _levelCreator.GetLevelData().GridData;
            var gridCounts = new Dictionary<EnumHolder.GameColor, int>();
            for (int x = 0; x < _levelCreator.gridWidth; x++)
                for (int y = 0; y < _levelCreator.gridHeight; y++)
                {
                    var cell = gridData[x, y];
                    if (cell.BasePlaceable is SingleObjectData so)
                        gridCounts[so.Color] = gridCounts.GetValueOrDefault(so.Color) + 1;
                    else if (cell.BasePlaceable is StackedObjectData st)
                        foreach (var elt in st.Stack)
                            gridCounts[elt.Color] = gridCounts.GetValueOrDefault(elt.Color) + 1;
                }

            var conveyorData = _levelCreator.GetLevelData().ConveyorData;
            var convCounts = new Dictionary<EnumHolder.GameColor, int>();
            if (conveyorData != null)
            {
                for (int x = 0; x < conveyorData.GetLength(0); x++)
                {
                    for (int y = 0; y < conveyorData.GetLength(1); y++)
                    {
                        var cell = conveyorData[x, y];
                        var p = cell.BasePlaceable;
                        if (p == null) continue;

                        // 1) ConveyorItemData ise doğrudan oku
                        if (p is ConveyorItemData ci)
                        {
                            convCounts[ci.Color] = convCounts.GetValueOrDefault(ci.Color) + 1;
                        }
                        // 2) SingleObjectData (eğer conveyor'ınızda SingleObjectData da kullanıyorsanız)
                        else if (p is SingleObjectData so)
                        {
                            convCounts[so.Color] = convCounts.GetValueOrDefault(so.Color) + 1;
                        }
                        // 3) StackedObjectData için yığılı tüm elemanları say
                        else if (p is StackedObjectData st && st.Stack != null)
                        {
                            foreach (var elt in st.Stack)
                            {
                                if (elt == null) continue;
                                convCounts[elt.Color] = convCounts.GetValueOrDefault(elt.Color) + 1;
                            }
                        }
                        // 4) (Opsiyonel) Eğer ChainData ya da başka tip conveyor'da duruyorsa:
                        else if (p is ChainData ch)
                        {
                            convCounts[ch.Color] = convCounts.GetValueOrDefault(ch.Color) + 1;
                        }
                    }
                }
            }

            // Sonra ekrana bastığınız kısım aynı kalabilir:
            // g: gridCounts, c: convCounts
            var allColors = new HashSet<EnumHolder.GameColor>(gridCounts.Keys);
            foreach (var c in convCounts.Keys) allColors.Add(c);

            foreach (var color in allColors)
            {
                int g = gridCounts.GetValueOrDefault(color);
                int c = convCounts.GetValueOrDefault(color);
                bool match = g == c;
                string label = $"{color}: {g}  Conv: {c}";
                if (!match) label = StrikeThrough(label);

                var style = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor = match
                            ? _levelCreator.gameColors.editorColors[(int)color]
                            : Color.red
                    }
                };

                EditorGUILayout.LabelField(label, style, GUILayout.Height(20));
            }

            EndVerticalBoxed();
            SetBackgroundColor(Color.white);
        }

        private string StrikeThrough(string input)
        {
            var sb = new StringBuilder();
            foreach (char ch in input)
            {
                sb.Append(ch).Append('\u0336');
            }

            return sb.ToString();
        }


        private void SwapQueueCells<T>(int x, List<T> listToSwap) where T : BasePlaceableData
        {
            BasePlaceableData temp = savedPlaceableToCarry;
            int index = listToSwap.IndexOf(savedPlaceableToCarry as T);
            listToSwap[index] = listToSwap[x];
            listToSwap[x] = temp as T;
        }


        private void SwapGridCells(int x, int y)
        {
            var levelData = _levelCreator.GetLevelData();
            (levelData.GridData[x, y].BasePlaceable, savedCellToCarry.BasePlaceable) = (savedCellToCarry.BasePlaceable,
                levelData.GridData[x, y].BasePlaceable);
        }


        private Color GetEditorColor(int i)
        {
            return _levelCreator.gameColors.editorColors[i];
        }

        #endregion

        private void DisplayBottomShooterLanes()
        {
            var levelData = _levelCreator.GetLevelData();
            if (levelData == null) return;

            Space(5);
            BeginVerticalBoxed("Bottom Shooter Lanes");

            EditorGUI.BeginChangeCheck();

            BeginVerticalBoxed("Dynamic Bottom Slot Layout");

            _levelCreator.useDynamicBottomSlotLayout =
                EditorGUILayout.Toggle("Use Dynamic Bottom Slot Layout", _levelCreator.useDynamicBottomSlotLayout);

            if (_levelCreator.useDynamicBottomSlotLayout)
            {
                _levelCreator.bottomSlotStartReference =
                    (Transform)EditorGUILayout.ObjectField(
                        "Bottom Slot Start Reference",
                        _levelCreator.bottomSlotStartReference,
                        typeof(Transform),
                        true
                    );

                _levelCreator.bottomLaneOffset =
                    EditorGUILayout.Vector3Field("Bottom Lane Offset", _levelCreator.bottomLaneOffset);

                _levelCreator.bottomNodeOffset =
                    EditorGUILayout.Vector3Field("Bottom Node Offset", _levelCreator.bottomNodeOffset);

                _levelCreator.bottomSlotEulerOffset =
                    EditorGUILayout.Vector3Field("Bottom Slot Euler Offset", _levelCreator.bottomSlotEulerOffset);

                EditorGUILayout.HelpBox(
                    "Dynamic açıkken Generate, BottomLaneReferences listesini kullanmaz. " +
                    "Lane Count ve Visible Shooter Count değerlerine göre bottom node/shooter pozisyonlarını otomatik üretir.",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Dynamic kapalıyken eski sistem kullanılır. Bu durumda BottomLaneReferences listesindeki node transformları gerekir.",
                    MessageType.Warning
                );
            }

            EndVerticalBoxed();

            Space(10);

            int laneCount = EditorGUILayout.IntField("Lane Count", _levelCreator.bottomLaneCount);
            laneCount = Mathf.Max(0, laneCount);

            int visibleCount = EditorGUILayout.IntField("Visible Shooter Count Per Lane", _levelCreator.visibleShooterCountPerLane);
            visibleCount = Mathf.Max(1, visibleCount);


            if (laneCount != _levelCreator.bottomLaneCount)
            {
                _levelCreator.bottomLaneCount = laneCount;
                levelData.bottomLaneCount = laneCount;
                levelData.EnsureBottomLaneCount(laneCount);
            }

            if (visibleCount != _levelCreator.visibleShooterCountPerLane)
            {
                _levelCreator.visibleShooterCountPerLane = visibleCount;
                levelData.visibleShooterCountPerLane = visibleCount;
            }

            levelData.EnsureBottomLaneCount(_levelCreator.bottomLaneCount);
            levelData.RefreshBottomShooterIndexes();

            Space(10);

            for (int laneIndex = 0; laneIndex < levelData.bottomShooterLanes.Count; laneIndex++)
            {
                var lane = levelData.bottomShooterLanes[laneIndex];
                lane.shooters ??= new List<ShooterSpawnData>();

                BeginVerticalBoxed($"Lane {laneIndex}");

                BeginHorizontal();
                DisplayLabelField($"Shooters: {lane.shooters.Count}", 13, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);

                if (GUILayout.Button("+ Add Shooter", GUILayout.Height(25)))
                {
                    Undo.RecordObject(_levelCreator, "Add Bottom Shooter");

                    levelData.AddShooterToLane(
                          laneIndex,
                          _levelCreator.color,
                          Mathf.Max(0, _levelCreator.shooterBulletCount),
                          _levelCreator.shooterLinkGroupId,
                          _levelCreator.shooterIsHidden
                      );

                    levelData.RefreshBottomShooterIndexes();
                    EditorUtility.SetDirty(_levelCreator);
                    Repaint();
                }

                EndHorizontal();

                Space(5);

                for (int orderIndex = 0; orderIndex < lane.shooters.Count; orderIndex++)
                {
                    var shooter = lane.shooters[orderIndex];

                    BeginHorizontal();

                    EditorGUILayout.LabelField($"#{orderIndex}", GUILayout.Width(35));

                    shooter.color = (EnumHolder.GameColor)EditorGUILayout.EnumPopup(shooter.color, GUILayout.Width(100));
                    shooter.bulletCount = EditorGUILayout.IntField(shooter.bulletCount, GUILayout.Width(50));
                    shooter.linkGroupId = EditorGUILayout.IntField(shooter.linkGroupId, GUILayout.Width(50));
                    shooter.isHidden = EditorGUILayout.ToggleLeft("Hidden", shooter.isHidden, GUILayout.Width(75));

                    if (GUILayout.Button("↑", GUILayout.Width(25)) && orderIndex > 0)
                    {
                        (lane.shooters[orderIndex - 1], lane.shooters[orderIndex]) =
                            (lane.shooters[orderIndex], lane.shooters[orderIndex - 1]);
                        levelData.RefreshBottomShooterIndexes();
                        EndHorizontal();
                        break;
                    }

                    if (GUILayout.Button("↓", GUILayout.Width(25)) && orderIndex < lane.shooters.Count - 1)
                    {
                        (lane.shooters[orderIndex + 1], lane.shooters[orderIndex]) =
                            (lane.shooters[orderIndex], lane.shooters[orderIndex + 1]);
                        levelData.RefreshBottomShooterIndexes();
                        EndHorizontal();
                        break;
                    }

                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        Undo.RecordObject(_levelCreator, "Remove Bottom Shooter");

                        levelData.RemoveShooterFromLane(laneIndex, orderIndex);
                        levelData.RefreshBottomShooterIndexes();
                        EditorUtility.SetDirty(_levelCreator);
                        Repaint();

                        EndHorizontal();
                        break;
                    }

                    EndHorizontal();
                }

                EndVerticalBoxed();
                Space(5);
            }

            Space(10);
            DisplayShooterColorCounts(levelData);

            if (EditorGUI.EndChangeCheck())
            {
                levelData.RefreshBottomShooterIndexes();
                EditorUtility.SetDirty(_levelCreator);
            }

            EndVerticalBoxed();
        }

        private void DisplayShooterColorCounts(LevelData levelData)
        {
            if (levelData == null || levelData.bottomShooterLanes == null)
                return;

            Dictionary<EnumHolder.GameColor, int> bulletCountsByColor = new Dictionary<EnumHolder.GameColor, int>();
            Dictionary<EnumHolder.GameColor, int> shooterCountsByColor = new Dictionary<EnumHolder.GameColor, int>();

            int totalShooterCount = 0;
            int totalBulletCount = 0;

            foreach (BottomShooterLaneData lane in levelData.bottomShooterLanes)
            {
                if (lane == null || lane.shooters == null)
                    continue;

                foreach (ShooterSpawnData shooter in lane.shooters)
                {
                    if (shooter == null)
                        continue;

                    if (shooter.color == EnumHolder.GameColor.None)
                        continue;

                    if (!bulletCountsByColor.ContainsKey(shooter.color))
                    {
                        bulletCountsByColor.Add(shooter.color, 0);
                        shooterCountsByColor.Add(shooter.color, 0);
                    }

                    int bulletCount = Mathf.Max(0, shooter.bulletCount);

                    bulletCountsByColor[shooter.color] += bulletCount;
                    shooterCountsByColor[shooter.color]++;

                    totalBulletCount += bulletCount;
                    totalShooterCount++;
                }
            }

            BeginVerticalBoxed("Shooter Bullet Counts");

            EditorGUILayout.LabelField($"Total Shooters: {totalShooterCount}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Bullets: {totalBulletCount}", EditorStyles.boldLabel);

            if (bulletCountsByColor.Count == 0)
            {
                EditorGUILayout.LabelField("No shooters added yet.");
                EndVerticalBoxed();
                return;
            }

            foreach (var pair in bulletCountsByColor)
            {
                EnumHolder.GameColor color = pair.Key;
                int bulletTotal = pair.Value;
                int shooterCount = shooterCountsByColor[color];

                Color editorColor = Color.white;
                int colorIndex = (int)color;

                if (_levelCreator.gameColors != null &&
                    _levelCreator.gameColors.editorColors != null &&
                    colorIndex >= 0 &&
                    colorIndex < _levelCreator.gameColors.editorColors.Length)
                {
                    editorColor = _levelCreator.gameColors.editorColors[colorIndex];
                }

                GUIStyle style = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold,
                    normal =
            {
                textColor = editorColor
            }
                };

                EditorGUILayout.LabelField($"{color}: {bulletTotal} bullets ({shooterCount} shooters)", style);
            }

            EndVerticalBoxed();
        }
    }
}