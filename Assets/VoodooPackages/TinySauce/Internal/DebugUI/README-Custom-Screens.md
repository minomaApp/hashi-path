# Creating Custom Debugger Screens

This debugger UI is built from runtime-created Unity UI widgets. A custom screen is a
`TSDebugScreen` subclass that adds widgets when the screen is shown.

## Basic Screen

Create a class in your own project namespace:

```csharp
using UnityEngine;
using Voodoo.Tiny.Sauce.Internal.Debugger;

namespace MyGame.Debug
{
    public class TSDebugPlayerScreen : TSDebugScreen
    {
        protected override string ScreenTitle => "Player";

        protected override void OnScreenShow()
        {
            ClearWidgets();

            AddTitle("Player");
            AddTextPair("Name", SystemInfo.deviceName);
            AddTextPair("Level", PlayerPrefs.GetInt("PlayerLevel", 1).ToString());

            AddButton("Give 100 Coins", () =>
            {
                var coins = PlayerPrefs.GetInt("Coins", 0) + 100;
                PlayerPrefs.SetInt("Coins", coins);
                ShowScreen();
            });
        }
    }
}
```

Important details:

- Override `ScreenTitle`; it is shown in the screen header.
- Override `OnScreenShow()`; it runs every time the screen opens.
- Call `ClearWidgets()` before adding widgets, otherwise the screen duplicates content every time it is reopened.
- Use `ShowScreen()` after changing values if you want to refresh the content immediately.
- The default back button returns to the main menu.

## Add The Screen To The Debugger

Register the screen in `TSDebugUIManager`.

If the screen is in another namespace, add the matching `using` directive to
`TSDebugUIManager` or reference the screen type with its fully-qualified name.

Add a field:

```csharp
private TSDebugPlayerScreen _playerScreen;
```

Add a public method:

```csharp
public void ShowPlayerScreen()
{
    EnsureScreen(ref _playerScreen);
    _playerScreen.ShowScreen();
}
```

Then add a menu entry in `TSDebugMainMenuScreen.OnScreenShow()`:

```csharp
AddButton("Player", () => TSDebugUIManager.Instance?.ShowPlayerScreen());
```

## Widgets

`TSDebugScreen` and `TSDebugFoldableSectionWidget` expose the same widget creation
methods. Use them from `OnScreenShow()` or from a foldable section instance.

### `TSDebugTextWidget`

```csharp
protected TSDebugTextWidget AddText(string text);
```

Screenshot: add screenshot here.

### `TSDebugTitleWidget`

```csharp
protected TSDebugTitleWidget AddTitle(string title);
protected TSDebugTitleWidget AddSubTitle(string title);
```

Screenshot: add screenshot here.

### `TSDebugTextPairWidget`

```csharp
protected TSDebugTextPairWidget AddTextPair(
    string label,
    string value,
    string copyValue = null);
```

Screenshot: add screenshot here.

### `TSDebugFoldableSectionWidget`

```csharp
protected TSDebugFoldableSectionWidget AddFoldableSection(
    string title,
    bool expanded = true);
```

Screenshot: add screenshot here.

Foldable sections can contain widgets too:

```csharp
var inventorySection = AddFoldableSection("Inventory", expanded: true);
inventorySection.AddTextPair("Coins", PlayerPrefs.GetInt("Coins", 0).ToString());
inventorySection.AddButton("Clear Inventory", ClearInventory);
```

### `TSDebugButtonWidget`

```csharp
protected TSDebugButtonWidget AddButton(
    string label,
    Action onClick = null);
```

Screenshot: add screenshot here.

### `TSDebugToggleWidget`

```csharp
protected TSDebugToggleWidget AddToggle(
    string label,
    bool defaultValue = false,
    Action<bool> onValueChanged = null);
```

Screenshot: add screenshot here.

### `TSDebugDropdownWidget`

```csharp
protected TSDebugDropdownWidget AddDropdown(
    string label,
    IEnumerable<string> options,
    string defaultValue = null,
    Action<string> onValueChanged = null);

protected TSDebugDropdownWidget AddEnumDropdown<TEnum>(
    string label,
    TEnum defaultValue,
    Action<TEnum> onValueChanged = null)
    where TEnum : struct;
```

Screenshot: add screenshot here.

### `TSDebugTextInputWidget`

```csharp
protected TSDebugTextInputWidget AddTextInput(
    string label,
    string defaultValue = null,
    Action<string> onValueChanged = null);
```

Screenshot: add screenshot here.

### `TSDebugNumberInputWidget`

```csharp
protected TSDebugNumberInputWidget AddNumberInput(
    string label,
    float defaultValue = 0f,
    bool isInt = false,
    Action<float> onValueChanged = null);
```

Screenshot: add screenshot here.

### `TSDebugNumberInputWithSliderWidget`

```csharp
protected TSDebugNumberInputWithSliderWidget AddNumberInputWithSlider(
    string label,
    float defaultValue = 0f,
    float minValue = 0f,
    float maxValue = 100f,
    bool isInt = false,
    Action<float> onValueChanged = null);
```

Screenshot: add screenshot here.

### Value Change Callbacks

Value widgets can receive an `onValueChanged` callback. Prefer passing a method that
takes the new widget value instead of re-reading every widget from one generic apply
method.

```csharp
protected override void OnScreenShow()
{
    ClearWidgets();

    AddTextInput("Player Name", PlayerName, ApplyPlayerName);
    AddToggle("God Mode", IsGodModeEnabled, ApplyGodMode);
    AddNumberInput("Level", PlayerLevel, isInt: true, onValueChanged: ApplyLevel);
    AddNumberInputWithSlider(
        "Health",
        PlayerHealth,
        minValue: 0f,
        maxValue: 1000f,
        isInt: true,
        onValueChanged: ApplyHealth);
}

private void ApplyPlayerName(string value)
{
    PlayerName = string.IsNullOrWhiteSpace(value) ? "Player" : value.Trim();
    RefreshSummary();
}

private void ApplyGodMode(bool value)
{
    IsGodModeEnabled = value;
    RefreshSummary();
}

private void ApplyLevel(float value)
{
    PlayerLevel = Mathf.Max(1, Mathf.RoundToInt(value));
    RefreshSummary();
}

private void ApplyHealth(float value)
{
    PlayerHealth = value;
    RefreshSummary();
}
```

For slider widgets, the visual value updates while dragging, but `onValueChanged`
fires only when the slider is released.

### Custom Widget Creation

```csharp
protected void AddWidget(TSDebugWidget widget);
protected T CreateWidget<T>() where T : TSDebugWidget;
```

## Custom Back Behavior

By default, the back button opens the main menu. Override `OnBackPressed()` when the
screen should return somewhere else:

```csharp
protected override void OnBackPressed()
{
    TSDebugUIManager.Instance?.ShowGameAnalyticsScreen();
}
```

Hide the back button by overriding `ShowBackButton`:

```csharp
protected override bool ShowBackButton => false;
```

## Custom Widgets

For one-off UI, prefer the screen helper methods. Create a custom widget only when the
same UI needs to be reused or has complex behavior.

A custom widget should inherit from `TSDebugWidget`, create its layout in `Awake()`,
and expose clear setter/action methods. Existing widgets follow this pattern:

```csharp
public static TSDebugButtonWidget Instantiate()
{
    var widgetObject = new GameObject(
        nameof(TSDebugButtonWidget),
        typeof(RectTransform),
        typeof(Image),
        typeof(Button),
        typeof(TSDebugButtonWidget));

    return widgetObject.GetComponent<TSDebugButtonWidget>();
}
```

Add a custom widget to a screen with:

```csharp
var widget = CreateWidget<TSDebugCustomWidget>();
widget.Configure(...);
```

## Checklist

- Put screen classes in your own project namespace.
- Keep screen-building logic in `OnScreenShow()`.
- Call `ClearWidgets()` before rebuilding dynamic screens.
- Add the screen to `TSDebugUIManager`.
- Add a button to `TSDebugMainMenuScreen`.
- Use descriptive labels and values so screenshots are easy to understand.
