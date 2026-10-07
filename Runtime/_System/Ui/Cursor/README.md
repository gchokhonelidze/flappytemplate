# Cursor

[← All documentation](../../../../)

The hand over anything clickable. UGUI has no idea of a mouse cursor — a Button tints when it is hovered and
the arrow stays an arrow — so `UiCursor` looks at what is under the mouse and shows the cursor it wants:

| Under the mouse | Cursor |
| --- | --- |
| A `Button`, `Toggle`, `Slider`, `Scrollbar`, `Dropdown` — anything `Selectable` that can be used | the hand |
| A text field, `TMP_InputField` or `InputField` | the I-beam |
| A `Selectable` that is not interactable | the arrow |
| Anything else with a click handler on it — an `EventTrigger`, a game's own `IPointerClickHandler` | the hand |
| Anything with a [`UiCursorHint`](#hints) on it or above it | whatever that says |
| Nothing that answers a click | the arrow |

**There is nothing to set up.** No component goes in the scene: the first scene that loads starts a hidden
driver, the same way [Hotkeys](../Hotkeys/) does, and every Button in the game — the template's windows, the
navbar, the history chips, the game's own — gets the hand from then on.

*Describes package 1.0.89. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with tiles to hover over.*

> **Check it in a WebGL build, not in play mode.** The cursor is the browser's own, set as CSS on the canvas —
> the only cursor a page can show, and the reason this exists. The editor has no hand of its own to show, so in
> play mode nothing visibly changes. See [Outside the browser](#outside-the-browser).

## Hints

The rule is right for anything built from UGUI's own controls. For the rest, a **Ui Cursor Hint** says what an
object is: Add Component → UI → Cursor Hint, pick a cursor, done. It covers the object's children too, and the
nearest hint up the hierarchy wins — a hint on a card reaches the labels inside it, and a Button inside that card
with a hint of its own keeps its own.

```csharp
UiCursorHint.Set(handle, ECursor.Grab);      // a drag handle: nothing to click, but something to pick up
UiCursorHint.Set(card, ECursor.Pointer);     // clickable through code that is not a click handler
UiCursorHint.Set(backdrop, ECursor.Default); // a Button that should not look like one
```

The template uses one itself: a **window's backdrop** is a Button — it closes the dialog when the player clicks
past it — and without a hint the whole screen behind a modal would show the hand. It shows the arrow, as a page
does over one.

| `ECursor` | CSS | |
| --- | --- | --- |
| `Default` | `default` | the arrow |
| `Pointer` | `pointer` | the hand |
| `Text` | `text` | the I-beam |
| `Grab` | `grab` | an open hand |
| `Grabbing` | `grabbing` | a closed hand |
| `NotAllowed` | `not-allowed` | a circle with a bar through it |

## From code

| | |
| --- | --- |
| `UiCursor.Enabled` | On by default. Off puts the arrow back and leaves it — for a game that draws a cursor of its own. |
| `UiCursor.Current` | The cursor showing now. |
| `UiCursor.Resolve(gameObject)` | Which cursor an object asks for, by the rules above. |
| `UiCursor.SetTexture(cursor, texture, hotspot)` | A cursor image for platforms other than the web. See below. |
| `UiCursor.Reapply()` | Says the current cursor again. |
| `UiCursor.Css(cursor)` | The CSS name for a cursor. |
| `UiCursorHint.Set(gameObject, cursor)` | Adds a hint, or changes the one already there. |

## How it decides

Only the **first** thing a raycast hits is asked — the one a click would go to — and its hierarchy is walked
upwards one level at a time, the nearest answer winning. So a label inside a Button is a hand, and a disabled
Button inside a clickable card is an arrow.

It looks again whenever the mouse moves, and every tenth of a second while it does not: something can arrive
under a mouse that is standing still — a window opening, a list scrolling — and that is soon enough for it
without raycasting the canvas every frame for nothing. **While the button is held, the cursor stays what it was**
when the press began, so a drag that wanders off the slider it started on does not flick to an arrow mid-drag.

Only switched-on things count. A hint, a Button or a handler on an object that is switched off answers nothing,
the same as the click it stands for.

## Outside the browser

Every platform but the web has no hand of its own, so there the cursor changes only if it has been given an
image for it:

```csharp
UiCursor.SetTexture(ECursor.Pointer, handTexture, new Vector2(9f, 2f));  // the hotspot is the fingertip
```

With no textures given, nothing is touched at all — and that is deliberate: setting the cursor back to the
system's would throw away one a game had set with `Cursor.SetCursor` itself. The texture has to be imported as
**Cursor**, with Read/Write on, as Unity requires of any cursor.

## Files

| File | |
| --- | --- |
| `UiCursor.cs` | The rule, the driver's tick, and the cursor setting. |
| `UiCursorHint.cs` | The per-object override. |
| `ECursor.cs` | The cursors there are. |
| `UiCursorDriver.cs` | The hidden object that gives it a frame. Internal. |
| `UiCursorExample.cs` | A button, a disabled one, a card with a click handler and a hinted drag handle. Drop it on an empty RectTransform in a canvas. |
| `../../JSPlugins/Cursor.jslib` | Sets the CSS cursor on the canvas. |
| `../Window/UiWindow.cs` | Puts the arrow hint on a window's backdrop. |
