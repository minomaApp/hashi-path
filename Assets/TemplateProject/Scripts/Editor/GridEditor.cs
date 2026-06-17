#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Editor
{
    public class GridEditor : BaseEditor
    {
        protected void DrawNormalGrid(int gridWidth, int gridHeight, Action<int, int> drawCellAction, ref Vector2 scrollPosition,
            int groupWidth = 0, int groupHeight = 0, int groupSpacing = 0)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            for (var y = gridHeight - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (var x = 0; x < gridWidth; x++)
                {
                    if (groupWidth > 0 && x % groupWidth == 0)
                    {
                        Space(groupSpacing);
                    }

                    drawCellAction?.Invoke(x, y);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (groupHeight > 0 && y % groupHeight == 0)
                {
                    Space(groupSpacing);
                }
            }

            GUILayout.EndScrollView();
        }

        protected void DrawNormalGrid3D(int gridWidth, int gridHeight, int gridDepth, Action<int, int, int> drawCellAction, ref Vector2 scrollPosition,
            int groupWidth = 0, int groupHeight = 0, int groupSpacing = 0)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            for (var y = gridHeight - 1; y >= 0; y--)
            {
                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = 15,
                    fontStyle = FontStyle.Bold
                };

                EditorGUILayout.LabelField("Layer: " + (y + 1), style, GUILayout.ExpandWidth(true));
                Space(5);

                for (var z = gridDepth - 1; z >= 0; z--)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    for (var x = 0; x < gridWidth; x++)
                    {
                        if (groupWidth > 0 && x % groupWidth == 0)
                        {
                            Space(groupSpacing);
                        }

                        drawCellAction?.Invoke(x, y, z);
                    }

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    if (groupHeight > 0 && z % groupHeight == 0)
                    {
                        Space(groupSpacing);
                    }
                }

                Space(10);
            }

            GUILayout.EndScrollView();
        }

        protected void DrawScrollableDynamicSpacedGrid(int gridWidth, int gridHeight, float cellWidth, float cellHeight,
            Action<int, int> drawCellAction, LevelData levelData, ref Vector2 scrollPosition)
        {
            int[,] verticalEmptyArea = levelData.VerticalEmptyAreaData;
            int[,] horizontalEmptyArea = levelData.HorizontalEmptyAreaData;


            // Begin a scroll view with a horizontal scrollbar
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            var emptyAreaCellStyle = new GUIStyle(GUI.skin.button)
            {
                richText = true,
                fontSize = 10
            };

            for (int y = gridHeight - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (var x = 0; x < gridWidth; x++)
                {
                    var y1 = y;
                    var x1 = x;
                    DrawEmptyAreaButtonForDynamicSpacedGrid(emptyAreaCellStyle, cellWidth, cellHeight, false,
                        horizontalEmptyArea[x, y],
                        () => { levelData.SetEmptyAreaNumber(x1, y1, false); },
                        () => { levelData.RemoveEmptyAreaNumber(x1, y1, false); });
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < gridWidth; x++)
                {
                    var x1 = x;
                    var y1 = y;
                    DrawEmptyAreaButtonForDynamicSpacedGrid(emptyAreaCellStyle, cellWidth, cellHeight, true,
                        verticalEmptyArea[x, y],
                        () => { levelData.SetEmptyAreaNumber(x1, y1, true); },
                        () => { levelData.RemoveEmptyAreaNumber(x1, y1, true); });

                    drawCellAction?.Invoke(x, y);
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private void DrawEmptyAreaButtonForDynamicSpacedGrid(GUIStyle style, float width, float height, bool isVertical,
            int emptyAreaAmount, Action onLeftClick, Action onRightClick)
        {
            float emptyButtonWidth = 20;

            GUI.backgroundColor = emptyAreaAmount == 0 ? Color.gray : Color.white;

            var text = emptyAreaAmount.ToString();
            if (!isVertical)
            {
                width = emptyButtonWidth * 1.15f + width;
                height = emptyButtonWidth;
            }
            else
            {
                width = emptyButtonWidth;
            }

            var buttonRect = GUILayoutUtility.GetRect(new GUIContent(text), style, GUILayout.Width(width),
                GUILayout.Height(height));

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 &&
                buttonRect.Contains(Event.current.mousePosition))
            {
                onRightClick?.Invoke();
                Event.current.Use();
            }

            if (GUI.Button(buttonRect, text, style))
            {
                onLeftClick?.Invoke();
            }
        }

        public void DrawScrollableGroupedGridWithParentButton(int gridWidth, int gridHeight, int groupWidth, int groupHeight,
            Action<int, int, int> drawCellAction, Action<int, int> drawParentButtonAction, ref Vector2 scrollPosition)
        {
            if (groupWidth == 0)
            {
                Debug.LogError("Divide by zero: groupWidth");
                return;
            }

            if (groupHeight == 0)
            {
                Debug.LogError("Divide by zero: groupHeight");
                return;
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            for (var y = gridHeight - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (var x = 0; x < gridWidth; x++)
                {
                    GUILayout.BeginVertical("window");

                    drawParentButtonAction?.Invoke(x, y);

                    EditorGUILayout.BeginHorizontal();
                    for (var iy = groupHeight - 1; iy >= 0; iy--)
                    {
                        GUILayout.BeginVertical();
                        for (var ix = groupWidth - 1; ix >= 0; ix--)
                        {
                            var index = ix * groupWidth + (groupHeight - 1 - iy);
                            drawCellAction?.Invoke(x, y, index);
                        }
                        GUILayout.EndVertical();
                    }
                    EditorGUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                    Space(15);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                Space(15);
            }

            GUILayout.EndScrollView();
        }

        protected void DrawDefaultHexagonGrid(bool isFlatTopped, int gridWidth, int gridHeight, ref Vector2 scrollPosition,
            Action<int, int> drawCellAction = null, float hexSize = 80f)
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            if (isFlatTopped)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (var x = 0; x < gridWidth; x++)
                {
                    EditorGUILayout.BeginVertical();
                    GUILayout.Space(hexSize / 2 * (x % 2));

                    for (var y = 0; y < gridHeight; y++)
                    {
                        var yReverse = gridHeight - 1 - y;
                        drawCellAction?.Invoke(x, yReverse);
                    }

                    EditorGUILayout.EndVertical();
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            else
            {
                GUILayout.BeginVertical();

                for (var y = 0; y < gridHeight; y++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(hexSize / 2 * (y % 2));

                    for (var x = 0; x < gridWidth; x++)
                    {
                        var yPosReverse = (gridHeight - 1 - y);
                        drawCellAction?.Invoke(x, yPosReverse);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        /// <summary>
        /// This function draws a hexagon grid but makes is more manually 
        /// </summary>
   
        protected void DrawHexagonGrid(int gridWidth, int gridHeight,
            Action<int, int, Rect> drawCellAction = null, bool isEven = true)
        {
            Vector2 lastPos = GUILayoutUtility.GetLastRect().position;
            lastPos.x = 5;
            lastPos.y += 30;

            float hexWidth = 80f; // Adjust the size of each hexagon
            float hexHeight = Mathf.Sqrt(3) * hexWidth / 2; // Height of a hexagon

            GUILayout.BeginVertical(); // Begin a vertical group


            for (int y = 0; y < gridHeight; y++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(hexWidth / 2 * (y % 2));

                for (int x = 0; x < gridWidth; x++)
                {
                    var yPosReverse = (gridHeight - 1 - y);

                    float recess = 0;
                    if (isEven)
                    {
                        recess = (x % 2) * hexHeight / 2;
                    }
                    else
                    {
                        if (x % 2 == 0)
                        {
                            recess = hexHeight / 2;
                        }
                    }

                    Vector2 buttonPos = new Vector2(x * hexWidth, y * hexHeight + recess);
                    buttonPos += lastPos;
                    Rect buttonRect = new Rect(buttonPos.x, buttonPos.y, hexWidth - 4, hexHeight - 4);
                    drawCellAction?.Invoke(x, yPosReverse, buttonRect);

                    /*float segmentHeight = (hexHeight - 5) / 10;

                    // Draw the hexagon button with segmented colors
                    for (int i = 0; i < 10; i++)
                    {
                        Rect segmentRect = new Rect(buttonRect.x + 1,
                            buttonRect.y + (hexHeight - 7) - i * segmentHeight, hexWidth - 6, segmentHeight - 2);
                        if (i < cell.colorIndexes.Count)
                        {
                            LevelCreator.ColorEnum colorEnum = (LevelCreator.ColorEnum)cell.colorIndexes[i];
                            EditorGUI.DrawRect(segmentRect,
                                _levelCreator.gameColors.ActiveColors[cell.colorIndexes[i]]);

                            if (colorCounts.ContainsKey(colorEnum))
                            {
                                colorCounts[colorEnum]++;
                            }
                            else
                            {
                                colorCounts[colorEnum] = 1;
                            }
                        }
                    }*/
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(65 + hexHeight * gridHeight);
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Draws a scrollable 1 dimensional buttons with a given action
        /// </summary>
     
        protected void DrawNormalQueue(int queueLength, Action<int> drawButtonAction,
            ref Vector2 scrollPosition, bool reversed = false)
        {
            scrollPosition =
                GUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (reversed)
            {
                for (int x = queueLength - 1; x >= 0; x--)
                {
                    drawButtonAction?.Invoke(x);
                }
            }
            else
            {
                for (int x = 0; x < queueLength; x++)
                {
                    drawButtonAction?.Invoke(x);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.EndScrollView();
        }

        protected void DrawConnections(LevelData levelData)
        {
            DisplaySeparator();
            GUI.backgroundColor = Color.white;

            if (levelData.connections == null) levelData.connections = new List<ConnectionData>();

            DisplayLabelField("Connections", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

            for (var i = 0; i < levelData.connections.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();

                var connection = levelData.connections[i];
                EditorGUILayout.LabelField("Connected Group Index: " + levelData.connections.IndexOf(connection), EditorStyles.boldLabel);

                if (GUILayout.Button("-"))
                {
                    levelData.connections.RemoveAt(i);
                    return;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField("Connections:");

                for (var j = 0; j < connection.connectedGridPositions.Count; j++)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.LabelField($" - Grid Position: {connection.connectedGridPositions[j]}");

                    var layoutOptions = new[] { GUILayout.Width(100) };
                    var newGridPosition = EditorGUILayout.Vector2IntField("", connection.connectedGridPositions[j], layoutOptions);

                    if (newGridPosition != connection.connectedGridPositions[j])
                    {
                        connection.connectedGridPositions[j] = newGridPosition;
                        levelData.connections[i] = connection;
                    }

                    if (GUILayout.Button("Remove"))
                    {
                        connection.connectedGridPositions.RemoveAt(j);
                        levelData.connections[i] = connection;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add New Connection"))
                {
                    connection.connectedGridPositions.Add(new Vector2Int(0,0));
                    levelData.connections[i] = connection;
                }

                EditorGUILayout.EndVertical();
            }


            Space(15);
            if (GUILayout.Button("+"))
            {
                levelData.connections.Add(new ConnectionData());
            }
        }
    }
}
#endif