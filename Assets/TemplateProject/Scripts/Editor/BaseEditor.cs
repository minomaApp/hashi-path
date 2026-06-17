#if UNITY_EDITOR
using System;
using System.IO;
using BoxPuller.Scripts.Data.Enums;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TemplateProject.Scripts.Editor
{
	public class BaseEditor : UnityEditor.Editor
	{
#region Colors


		protected readonly Color RED = new Color(1f, 0f, 0f, 1f) + Color.red;
		protected readonly Color AQUA = new Color(0f, 1f, 1f, 1f);
		protected readonly Color CYAN = new Color(0f, 1f, 1f, 1f);
		protected readonly Color GREEN = new Color(0f, 1f, 0f, 1f) + Color.green;
		protected readonly Color WHITE = new Color(1f, 1f, 1f, 1f);
		protected readonly Color BLACK = new Color(0f, 0f, 0f, 1f);
		protected readonly Color BLUE = new Color(0f, 0f, 1f, 1f) + Color.cyan;
		protected readonly Color NAVY = new Color(0f, 0f, 0.5f, 1f);
		protected readonly Color MAGENTA = new Color(1f, 0f, 1f, 1f);
		protected readonly Color LIME = new Color(0.75f, 1f, 0f, 1f);
		protected readonly Color GOLD = new Color(1f, 0.84f, 0f, 1f);
		protected readonly Color TEAL = new Color(0f, 0.5f, 0.5f, 1f);
		protected readonly Color ORANGE = new Color(1f, 0f, 0f, 1f) + Color.yellow;
		protected readonly Color MAROON = new Color(0.5f, 0f, 0f, 1f);
		protected readonly Color OLIVE = new Color(0.5f, 0.5f, 0f, 1f);
		protected readonly Color PINK = new Color(1f, 0f, 1f, 1f) + Color.white;
		protected readonly Color PURPLE = new Color(1f, 0f, 0f, 1f) + Color.blue;
		protected readonly Color CORAL = new Color(1f, 0.5f, 0.31f, 1f);
		protected readonly Color DARK_RED = new Color(0.55f, 0f, 0f, 1f);
		protected readonly Color PEACH = new Color(1f, 0.85f, 0.72f, 1f);
		protected readonly Color DARK_BLUE = new Color(0.1f, 0.1f, 0.9f, 1f) + Color.blue;
		protected readonly Color SALMON = new Color(1f, 0.55f, 0.41f, 1f);
		protected readonly Color CHARTREUSE = new Color(0.5f, 1f, 0f, 1f);
		protected readonly Color INDIGO = new Color(0.29f, 0f, 0.51f, 1f);
		protected readonly Color TAN = new Color(0.82f, 0.71f, 0.55f, 1f);
		protected readonly Color DARK_GREEN = new Color(0f, 0.39f, 0f, 1f);
		protected readonly Color PLUM = new Color(0.56f, 0.27f, 0.52f, 1f);
		protected readonly Color LIGHT_RED = new Color(1f, 0.5f, 0.5f, 1f);
		protected readonly Color SAGE = new Color(0.74f, 0.72f, 0.54f, 1f);
		protected readonly Color SPRING_GREEN = new Color(0f, 1f, 0.5f, 1f);
		protected readonly Color LIGHT_YELLOW = new Color(1f, 1f, 0.88f, 1f);
		protected readonly Color LAVENDER = new Color(0.9f, 0.9f, 0.98f, 1f);
		protected readonly Color VIOLET = new Color(0.56f, 0.24f, 0.86f, 1f);
		protected readonly Color DARK_BROWN = new Color(0.65f, 0.2f, 0.2f, 0.6f);
		protected readonly Color DARK_YELLOW = new Color(1f, 1f, 0f, 1f) + Color.yellow;
		protected readonly Color LIGHT_BLUE = new Color(0.68f, 0.85f, 0.9f, 1f);
		protected readonly Color TURQUOISE = new Color(0f, 1f, 1f, 1f) + Color.white;
		protected readonly Color DARK_GRAY = new Color(0.33f, 0.33f, 0.33f, 1f);
		protected readonly Color LIGHT_GRAY = new Color(0.83f, 0.83f, 0.83f, 1f);
		protected readonly Color LIGHT_GREEN = new Color(0.56f, 0.93f, 0.56f, 1f);


#endregion


#region Styles


		protected GUIStyle BoldLabelStyle => new GUIStyle(EditorStyles.boldLabel)
		{
			richText = true,
			fontSize = 12
		};


#endregion


#region Icon Contents


		protected static GUIContent PopupIcon => EditorGUIUtility.IconContent("_Popup");
		protected static GUIContent PlayIcon => EditorGUIUtility.IconContent("d_PlayButton");
		protected static GUIContent PauseIcon => EditorGUIUtility.IconContent("d_PauseButton");
		protected static GUIContent InfoIcon => EditorGUIUtility.IconContent("Info");


#endregion


#region Functions


		public override void OnInspectorGUI() { base.OnInspectorGUI(); }

		public void SetDirty(Object obj) => EditorUtility.SetDirty(obj);


#endregion Functions


#region Color Functions


		protected void SetColor(Color color) => GUI.color = color;
		protected void SetContentColor(Color color) => GUI.contentColor = color;
		protected void SetBackgroundColor(Color color) => GUI.backgroundColor = color;
		protected void ResetColor() => GUI.color = Color.white;
		protected void ResetContentColor() => GUI.contentColor = Color.white;
		protected void ResetBackgroundColor() => GUI.backgroundColor = Color.white;


		protected void ResetAllColors()
		{
			ResetColor();
			ResetContentColor();
			ResetBackgroundColor();
		}


#endregion


#region Display Functions


		protected void DisplayHelpBox(string message, MessageType type) => EditorGUILayout.HelpBox(message, type);
		protected bool DisplayBoolean(string label, bool value) => EditorGUILayout.Toggle(label, value);
		protected bool DisplayToggle(string label, bool value) => EditorGUILayout.Toggle(label, value);
		protected bool DisplayFoldout(string label, bool value) => EditorGUILayout.Foldout(value, label);
		protected void DisplayLabel(string label) => EditorGUILayout.LabelField(label);


		protected void DisplayLabel(string label, GUIStyle labelStyle, int width, int height) =>
			EditorGUILayout.LabelField(label, labelStyle, GUILayout.Width(width), GUILayout.Height(height));


		protected void DisplayLabel(string label, int fontSize) =>
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);


		protected void DisplayLabel(string label, int fontSize, Color textColor) =>
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);


		protected void DisplayLabel(string label, int fontSize, Color textColor, TextAnchor textAnchor) =>
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);


		protected void DisplayLabel(string label, int fontSize, Color textColor, TextAnchor textAnchor,
			FontStyle fontStyle) => EditorGUILayout.LabelField(label, EditorStyles.boldLabel);


		protected void DisplayLabel(string label, int fontSize, Color textColor, TextAnchor textAnchor,
			FontStyle fontStyle, int width) =>
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(width));


		protected void DisplayLabel(string label, int fontSize, Color textColor, TextAnchor textAnchor,
			FontStyle fontStyle, int width, int height) => EditorGUILayout.LabelField(label, EditorStyles.boldLabel,
			GUILayout.Width(width), GUILayout.Height(height));


		public void DisplayText(string label, ref string variable) =>
			variable = EditorGUILayout.TextField(label, variable);


		public void DisplayEnum<T>(string label, ref T variable) where T : Enum =>
			variable = (T)EditorGUILayout.EnumPopup(label, variable);


		public void DisplayObject<T>(string label, ref T variable) where T : Object =>
			variable = (T)EditorGUILayout.ObjectField(label, variable, typeof(T), true);


		protected Vector3 DisplayVector3(string label, Vector3 value) => EditorGUILayout.Vector3Field(label, value);


		protected Vector3Int DisplayVector3Int(string label, Vector3Int value) =>
			EditorGUILayout.Vector3IntField(label, value);


		protected Vector2 DisplayVector2(string label, Vector2 value) => EditorGUILayout.Vector2Field(label, value);


		protected Vector2Int DisplayVector2Int(string label, Vector2Int value) =>
			EditorGUILayout.Vector2IntField(label, value);


#endregion


#region Display Field Functions


		protected void DisplayLabelField(string label, int fontSize = 12, Color textColor = default(Color),
			TextAnchor textAnchor = TextAnchor.MiddleCenter, FontStyle fontStyle = FontStyle.Normal,
			params GUILayoutOption[] options)
		{
			if (textColor == default) textColor = Color.white;

			var style = new GUIStyle
			{
				fontSize = fontSize,
				fontStyle = fontStyle,
				normal =
				{
					textColor = textColor
				},
				alignment = textAnchor
			};

			EditorGUILayout.LabelField(label, style ?? EditorStyles.boldLabel, options);
		}


		protected void DisplayIntFieldWithChangeCheck(string label, ref int value, Action onValueChanged = null)
		{
			EditorGUI.BeginChangeCheck(); // Start change check
			int newValue = EditorGUILayout.IntField(label, value);

			if (EditorGUI.EndChangeCheck())
			{
				// If a change has occurred, update the value and invoke the action
				value = newValue;
				onValueChanged?.Invoke();
			}
		}


		protected void DisplayLabelFieldWithChangeCheck(SerializedProperty property, string label,
			Action onValueChanged = null)
		{
			// Display the label field
			EditorGUI.BeginChangeCheck(); // Start change check
			EditorGUILayout.PropertyField(property, new GUIContent(label));

			if (EditorGUI.EndChangeCheck())
			{
				// If a change has occurred, invoke the action
				onValueChanged?.Invoke();
			}
		}


		protected void DisplayIntFieldWithIncrementDecrementButton(string label, ref int value,
			Action onValueChanged = null, int minValue = 0, int maxValue = int.MaxValue, int changeAmount = 1)
		{
			EditorGUI.BeginChangeCheck(); // Start change check

			EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(5), GUILayout.MaxWidth(5));

			//GUILayout.Space(10);
			// Display the integer field

			DisplayLabelField(label, 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal,
				GUILayout.MaxWidth(120));

			value = EditorGUILayout.IntField(value, GUILayout.Width(50), GUILayout.MaxWidth(50));
			value = Mathf.Clamp(value, minValue, maxValue);

			// Display the increment and decrement buttons
			if (GUILayout.Button("-", GUILayout.Width(20), GUILayout.MaxWidth(20)))
			{
				value = Mathf.Max(minValue, value - changeAmount);
			}

			if (GUILayout.Button("+", GUILayout.Width(20)))
			{
				value = Mathf.Min(maxValue, value + changeAmount);
			}


			EditorGUILayout.EndHorizontal();

			if (EditorGUI.EndChangeCheck())
			{
				// If a change has occurred, invoke the action
				onValueChanged?.Invoke();
			}
		}


		protected void DisplayIntField(string label, ref int value, int minValue = int.MinValue,
			int maxValue = int.MaxValue)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(label);
			value = EditorGUILayout.IntField(value);
			value = Mathf.Clamp(value, minValue, maxValue);
			EditorGUILayout.EndHorizontal();
		}


		protected void DisplayFloatField(string label, ref float value, int minValue = int.MinValue,
			int maxValue = int.MaxValue)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(label);
			value = EditorGUILayout.FloatField(value);
			value = Mathf.Clamp(value, minValue, maxValue);
			EditorGUILayout.EndHorizontal();
		}


		protected void DisplayObjectField<T>(string label, ref T obj) where T : Object
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));
			obj = (T)EditorGUILayout.ObjectField(obj, typeof(T), false);
			EditorGUILayout.EndHorizontal();
		}


		protected void DisplayVector3Field(string label, ref Vector3 vector)
		{
			vector = EditorGUILayout.Vector3Field(label, vector);
		}


#endregion


#region Display Button Functions


		/// <summary>
		/// Displays a button with a given label and click action
		/// </summary>
		protected void DisplayButton(string label, Action onButtonClick, float buttonHeight = 0,
			float buttonWitdh = 0)
		{
			if (buttonHeight > 0 || buttonWitdh > 0)
			{
				if (buttonHeight > 0 && buttonWitdh > 0)
				{
					if (GUILayout.Button(label, GUILayout.Height(buttonHeight), GUILayout.Width(buttonWitdh)))
					{
						onButtonClick?.Invoke();
					}
				}
				else if (buttonHeight > 0)
				{
					if (GUILayout.Button(label, GUILayout.Height(buttonHeight)))
					{
						onButtonClick?.Invoke();
					}
				}
				else if (buttonWitdh > 0)
				{
					if (GUILayout.Button(label, GUILayout.Width(buttonWitdh)))
					{
						onButtonClick?.Invoke();
					}
				}
			}
			else
			{
				if (GUILayout.Button(label))
				{
					onButtonClick?.Invoke();
				}
			}
		}


		private bool isDragging;


		/// <summary>
		/// Displays a button with multiple click events
		/// </summary>
		protected void DisplayButton(string buttonText, float buttonWidth, float buttonHeight,
			Action<EnumHolder.Direction> onLeftClickWithDirection = null,
			Action onRightClick = null,
			Action onClickUp = null,
			Action onCtrlPlusLeftClick = null,
			Action onCtrlPlusRightClick = null,
			GUIStyle style = null,
			Rect buttonRect = default
			)
		{
			if (style == null)
			{
				style = new GUIStyle(GUI.skin.button)
				{
					richText = true,
					fontSize = 15,
					fontStyle = FontStyle.Bold,
					alignment = TextAnchor.MiddleCenter
				};
			}

			if (buttonRect == default)
			{
				buttonRect = GUILayoutUtility.GetRect(new GUIContent(buttonText), style, GUILayout.Width(buttonWidth),
					GUILayout.Height(buttonHeight));
			}

			if (Event.current.type == EventType.MouseDown && buttonRect.Contains(Event.current.mousePosition))
			{
				if (Event.current.button == 1) // Right Click
				{
					if (Event.current.control) // Ctrl + Right Click
					{
						isDragging = true;
						onCtrlPlusRightClick?.Invoke();
						Event.current.Use();
					}

					else // Plain Right Click
					{
						onRightClick?.Invoke();
						Event.current.Use();
					}
				}

				else if (Event.current.button == 0) // Left Click
				{
					if (Event.current.control) // Ctrl + Left Click
					{
						onCtrlPlusLeftClick?.Invoke();
					}
					else // Plain Left Click
					{
						float clickPositionX = Event.current.mousePosition.x - buttonRect.x;
						float clickPositionY = Event.current.mousePosition.y - buttonRect.y;

						// Calculate distances to each edge
						float distanceLeft = clickPositionX;
						float distanceRight = buttonWidth - clickPositionX;
						float distanceUp = clickPositionY;
						float distanceDown = buttonHeight - clickPositionY;

						EnumHolder.Direction direction = EnumHolder.Direction.Right;

						// Find the nearest edge
						float minDistance = Mathf.Min(distanceLeft, distanceRight, distanceUp, distanceDown);

						// Set direction based on the nearest edge
						if (Mathf.Approximately(minDistance, distanceLeft))
						{
							direction = EnumHolder.Direction.Left;
						}
						else if (Mathf.Approximately(minDistance, distanceRight))
						{
							direction = EnumHolder.Direction.Right;
						}
						else if (Mathf.Approximately(minDistance, distanceUp))
						{
							direction = EnumHolder.Direction.Up;
						}
						else if (Mathf.Approximately(minDistance, distanceDown))
						{
							direction = EnumHolder.Direction.Down;
						}

						onLeftClickWithDirection?.Invoke(direction);
					}

					Event.current.Use();
				}
			}

			if (Event.current.type == EventType.MouseUp && buttonRect.Contains(Event.current.mousePosition))
			{
				if (Event.current.button == 0 && isDragging)
				{
					isDragging = false;
					onClickUp?.Invoke();
					Event.current.Use();
				}
			}

			if (GUI.Button(buttonRect, buttonText, style))
			{
				
			}
		}


		/// <summary>
		/// // Displays a button with an icon and click action
		/// </summary>
		protected void DisplayButton(GUIContent iconContent, float buttonWidth, float buttonHeight,
			Action onClick)
		{
			if (GUILayout.Button(iconContent, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
			{
				onClick?.Invoke();
			}
		}


#endregion


#region Display Enum Functions


		/// <summary>
		/// Sets an enum value with keyboard input by pressing 1-9 keys anr ctrl + 1-9 keys
		/// </summary>
		/// <param name="variable"></param>
		/// <typeparam name="T"></typeparam>
		protected void SetEnumWithKeyboardInput<T>(ref T variable) where T : Enum
		{
			T[] enumValues = (T[])Enum.GetValues(typeof(T));

			Event e = Event.current;
			if (e.type == EventType.KeyDown)
			{
				int numberKey = e.keyCode - KeyCode.Alpha1 + 1;
				if (Event.current.control)
				{
					numberKey += 10;
				}
				else if (numberKey == 0)
				{
					numberKey = 10;
				}

				if (numberKey >= 0 && numberKey < enumValues.Length)
				{
					variable = enumValues[numberKey];
					e.Use();
				}
			}
		}


		/// <summary>
		/// Displays buttons for each enum value
		/// </summary>
		/// <param name="variable">Enum variable to be assigned</param>
		/// <param name="title">Title of button field</param>
		/// <param name="enumColors">Enum colors must match with enum member count or leave it null</param>
		/// <param name="automaticButtonSize">If true sets sizes of buttons according to their text</param>
		/// <param name="buttonHeight">Set automaticButtonSize false to use this </param>
		/// <param name="buttonWidth">Set automaticButtonSize false to use this</param>
		/// <typeparam name="T">Enum type</typeparam>
		protected void DisplayEnumButtons<T>(ref T variable, string title,
			Color[] enumColors = null,
			bool automaticButtonSize = true,
			float buttonHeight = 30,
			float buttonWidth = 65,
			T[] enumValuesToExclude = null) where T : Enum
		{
			T[] enumValues = (T[])Enum.GetValues(typeof(T));

			if (enumValuesToExclude != null)
			{
				foreach (var value in enumValuesToExclude)
				{
					enumValues = Array.FindAll(enumValues, x => !x.Equals(value));
				}
			}

			int buttonsPerRow = Mathf.FloorToInt(EditorGUIUtility.currentViewWidth / buttonWidth);

			//DisplayMiniSeparator();
			DisplayLabelField(title, 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
			Space(7);

			var currentButtonHeight = buttonHeight;
			var isColored = enumColors != null;
			var isCountsMatch = enumColors != null && enumColors.Length == enumValues.Length ||
			                    enumValuesToExclude != null;
			if (isColored)
			{
				if (!isCountsMatch)
				{
					DisplayHelpBox("!Color count does not matches with enum count", MessageType.Warning);
				}
			}

			GUILayout.BeginHorizontal();

			var style = new GUIStyle(GUI.skin.button)
			{
				richText = true,
				fontSize = 10
			};

			int i = 0;
			foreach (var value in enumValues)
			{
				if (i % buttonsPerRow == 0)
				{
					if (i > 0)
					{
						GUILayout.EndHorizontal();
						Space(15);
						FlexibleSpace();
						GUILayout.BeginHorizontal();
					}
				}

				i++;
				if (isColored && isCountsMatch)
				{
					GUI.backgroundColor = enumColors[(int)(value as object)];
				}
				else
				{
					if (Equals(variable, value))
						GUI.backgroundColor = Color.gray;
					else
						GUI.backgroundColor = Color.white;
				}

				if (Equals(variable, value))
					currentButtonHeight *= 1.5f;
				else
					currentButtonHeight = buttonHeight;

				string buttonLabel = value.ToString();

				// Calculate button width based on content size
				if (automaticButtonSize)
				{
					var contentSize = style.CalcSize(new GUIContent(buttonLabel));
					buttonWidth = contentSize.x + 10f;
				}


				var rect = GUILayoutUtility.GetRect(new GUIContent(buttonLabel), style,
					GUILayout.Width(buttonWidth),
					GUILayout.Height(currentButtonHeight));

				if (GUI.Button(rect, buttonLabel, style))
				{
					variable = value;
				}
			}

			GUI.color = Color.white;

			GUILayout.EndHorizontal();
			GUI.color = Color.white;
		}


		protected void DisplayEnumButtons<T>(T[] enumValues, ref Vector2 scrollPosition,
			Action<T> onEnumButtonClick)
			where T : Enum
		{
			float scrollViewHeight =
				Mathf.Min(enumValues.Length * 30, 200f); // Adjust button height and max height as needed
			scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(scrollViewHeight));

			GUILayout.BeginHorizontal();

			foreach (T value in enumValues)
			{
				string buttonLabel = value.ToString();
				if (GUILayout.Button(buttonLabel))
				{
					onEnumButtonClick?.Invoke(value);
				}
			}

			GUILayout.EndHorizontal();
			GUILayout.EndScrollView();
		}


#endregion


#region Layout Utilities


		protected void Space(float pixels) => GUILayout.Space(pixels);
		protected void Space() => EditorGUILayout.Space();

		protected void FlexibleSpace() => GUILayout.FlexibleSpace();

		protected void BeginVertical() => GUILayout.BeginVertical();

		protected void EndVertical() => GUILayout.EndVertical();

		protected void BeginHorizontal() => GUILayout.BeginHorizontal();

		protected void EndHorizontal() => GUILayout.EndHorizontal();

		protected void BeginChangeCheck() => EditorGUI.BeginChangeCheck();

		protected bool EndChangeCheck() => EditorGUI.EndChangeCheck();
		protected void BeginVerticalBoxed(string title) => GUILayout.BeginVertical(title, "window");
		protected void BeginVerticalBoxed() => GUILayout.BeginVertical("window");
		protected void BeginHorizontalBoxed(string title) => GUILayout.BeginHorizontal(title, "window");
		protected void BeginHorizontalBoxed() => GUILayout.BeginHorizontal("window");
		protected void EndVerticalBoxed() => GUILayout.EndVertical();
		protected void EndHorizontalBoxed() => GUILayout.EndHorizontal();

		protected Vector2 BeginScrollView(Vector2 scrollPosition) => GUILayout.BeginScrollView(scrollPosition);

		protected void EndScrollView() => GUILayout.EndScrollView();


		protected void DisplaySeparator()
		{
			GUI.backgroundColor = Color.white;
			GUI.color = Color.black;
			Space(30);
			EditorGUILayout.LabelField("", GUI.skin.box, GUILayout.Height(10), GUILayout.ExpandWidth(true));
			Space(20);
			GUI.color = Color.white;
		}


		protected void DisplayMiniSeparator()
		{
			GUI.backgroundColor = Color.white;
			Space(5);
			EditorGUILayout.LabelField("", GUI.skin.box, GUILayout.Height(5), GUILayout.ExpandWidth(true));
			Space(2);
		}


#endregion


#region Handle Clicks Utilities


		private void HandleClick(Rect buttonRect, Action<EnumHolder.Direction> onClick,
			Action onRightClick = null, Action onMiddleClick = null,
			Action onCtrlPlusClick = null, Action onClickUp = null)
		{
			// MouseDown Events
			if (Event.current.type == EventType.MouseDown && buttonRect.Contains(Event.current.mousePosition))
			{
				if (Event.current.button == 1)
				{
					// Right Click
					onRightClick?.Invoke();
					Event.current.Use();
				}
				else if (Event.current.button == 2)
				{
					// Middle Click
					onMiddleClick?.Invoke();
					Event.current.Use();
				}
				else if (Event.current.button == 0 && Event.current.control)
				{
					// Ctrl + Left Click
					isDragging = true;
					onCtrlPlusClick?.Invoke();
					Event.current.Use();
				}
			}

			// MouseUp Events (handling drag release)
			if (Event.current.type == EventType.MouseUp && buttonRect.Contains(Event.current.mousePosition))
			{
				if (Event.current.button == 0 && isDragging)
				{
					isDragging = false;
					onClickUp?.Invoke();
					Event.current.Use();
				}
			}

			// Standard Button Click (Left Click)
			if (GUI.Button(buttonRect,
				    string.Empty)) // Empty string since actual Displaying of button text is in DisplayButton
			{
				float clickPositionX = Event.current.mousePosition.x - buttonRect.x;
				float clickPositionY = Event.current.mousePosition.y - buttonRect.y;

				// Calculate distances to each edge
				float distanceLeft = clickPositionX;
				float distanceRight = buttonRect.width - clickPositionX;
				float distanceUp = clickPositionY;
				float distanceDown = buttonRect.height - clickPositionY;

				EnumHolder.Direction direction = EnumHolder.Direction.Right;

				// Find the nearest edge
				float minDistance = Mathf.Min(distanceLeft, distanceRight, distanceUp, distanceDown);

				// Set direction based on the nearest edge
				if (Mathf.Approximately(minDistance, distanceLeft))
				{
					direction = EnumHolder.Direction.Left;
				}
				else if (Mathf.Approximately(minDistance, distanceRight))
				{
					direction = EnumHolder.Direction.Right;
				}
				else if (Mathf.Approximately(minDistance, distanceUp))
				{
					direction = EnumHolder.Direction.Up;
				}
				else if (Mathf.Approximately(minDistance, distanceDown))
				{
					direction = EnumHolder.Direction.Down;
				}

				onClick?.Invoke(direction);
			}
		}


		private void ClickHandler(Action onLeftClicked = null, Action onRightClicked = null,
			Action onMiddleClicked = null)
		{
			if (Event.current.button == 0)
				onLeftClicked?.Invoke();
			else if (Event.current.button == 1)
				onRightClicked?.Invoke();
			else if (Event.current.button == 2)
				onMiddleClicked?.Invoke();
		}


#endregion


#region Utilites


		public T CreateScriptableObject<T>(string path, string label) where T : ScriptableObject
		{
			T newScriptableObject = ScriptableObject.CreateInstance<T>();

			string fullPath = Path.Combine(path, label + $".asset");
			AssetDatabase.CreateAsset(newScriptableObject, fullPath);

			AssetDatabase.Refresh();
			AssetDatabase.SaveAssets();

			return AssetDatabase.LoadAssetAtPath<T>(fullPath);
		}


		protected string GetColoredRichText(string text, Color color)
		{
			return $"<color=#{color.ToHex()}>{text}</color>";
		}


#endregion
	}


	// Used for rich text color conversions on buttons etc.
	public static class ColorExtensions
	{
		public static string ToHex(this Color color) { return ColorUtility.ToHtmlStringRGB(color); }
	}


}
#endif