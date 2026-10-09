# Selectable Text

[← All documentation](../../../../)

A label the player can select and copy from, and not type into. It goes on any TextMeshPro label, which keeps
everything it had — its text, its style, its place in a grid:

```csharp
UiSelectableText.On(seedLabel);
```

| With | To |
| --- | --- |
| A mouse drag | select from where it started to where it is |
| A double-click | select everything |
| Shift-click | extend the selection |
| Ctrl+C · Cmd+C | copy the selection |
| Ctrl+A · Cmd+A | select everything |
| A finger held still for half a second | select everything, and copy it when the finger lifts |
| A click anywhere else | drop the selection |
| The [copy icon](#the-copy-button) at the right | copy all of it, and show a tick for a moment |

The [bet info sheet](../Window/) uses it for the round id, the three totals and the three seeds, and puts a copy
icon on the id and the seeds — its **Copyable Values** switch.

*Describes package 1.0.93. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with text to select.*

## Why not a read-only input field

`TMP_InputField` with **Read Only** on selects and copies too, and was the obvious way to do this. It is the wrong
one inside the template's dialogs:

- It needs a viewport and makes a caret object beside the text. Inside a [`UiGrid`](../Grid/) cell, that object
  is a child the grid has not been told about, so the grid hides it — and the selection highlight is drawn by it.
- It keeps its own copy of the text and writes it over the label whenever it takes focus, so the label has to be
  written through the field instead.
- It takes every drag. In a dialog that scrolls, a finger meant to scroll the body selects text instead.

`UiSelectableText` is one component on the label itself. It draws the highlight in a child of the label, which
is not a grid cell, and reads the text from the label every time.

## The copy button

For a value nobody wants to select by hand — a 128-character hash — a small copy icon sits at the label's right
edge and copies the whole text with one click or tap:

```csharp
UiCopyButton.On(hashLabel, 18f, 10f);   // 18 across, 10 clear of the text
```

It is made **inside** the label rather than beside it, because a label is usually a cell of a
[`UiGrid`](../Grid/) and a sibling would be a cell the grid hides. The label's right margin is widened by the icon
and the gap, so wrapped text stops short of it. After a copy the icon turns into a green tick for **Copied
Seconds**, and `OnCopied` fires.

The icon is drawn — two pages, then the tick — from [Rounded Boxes](../RoundedBox/), so it has no sprite to
import and takes **Color**, **Hover Color** and **Copied Color** like any other part. Its hit area is a little
bigger than the icon, so a finger can find it.

## On a phone

A finger dragging over the text is **scrolling**, not selecting: the drag is handed to whatever is behind the
label — the [window](../Window/)'s scrolling body, in a dialog — exactly as if the label were not there. A phone
has no Ctrl+C, so **holding a finger still** on the label for **Long Press Seconds** selects all of it, and
lifting the finger copies it. `OnCopied` fires then, and is the place for a "Copied" toast.

## In a browser

A WebGL build cannot write to the clipboard whenever it likes. A browser only allows it inside the key press —
on Safari — or close to it elsewhere, and a game in a **cross-origin iframe**, which is what a game embedded on
an operator's site is, has no `navigator.clipboard` at all unless the page around it allowed it.

So the copy is done by the page, not the game, inside the browser's own event — Unity only hears of an event a
frame after the browser has finished with it. The game says what would be copied ahead of time, and
`JSPlugins/Clipboard.jslib` copies it through a hidden textarea and `execCommand("copy")`, which works in an
iframe and on Safari alike:

| | Offered | Copied in |
| --- | --- | --- |
| Ctrl+C | whenever the selection changes — `UiClipboard.Offer` | the keydown |
| The copy button | when it is pressed — `UiClipboard.OfferClick` | the mouseup or touchend that ends the press |
| A long press on a phone | — | `UiClipboard.Copy` on the lift, which the browser counts as the player doing something |

A press on the copy button that slides off it, or turns into a scroll, withdraws its offer before the release, so
scrolling past a seed never copies it.

`UiClipboard.Copy(text)` copies at once, for anything else. In a browser it only works close to a press or a click,
and on Safari not at all outside the event itself — the reason the copy button offers ahead instead.

## Hotkeys

While a value is selected, [Hotkeys](../Hotkeys/) stand back, as they do for a text field: Ctrl+C is not a press
of C, and Ctrl+A is not "half the amount". The selection lives on the `EventSystem`'s selected object, so a click
anywhere else gives the keys back.

## The cursor

[Cursor](../Cursor/) shows the I-beam over selectable text, as it does over a text field.

## From code

| | |
| --- | --- |
| `UiSelectableText.On(label)` | Adds the component to a label, or finds the one already there, and turns raycasting on. |
| `Select(from, to)` · `SelectAll()` · `ClearSelection()` | Character positions, either way round. 0 is before the first character. |
| `HasSelection` · `SelectedText` | What is selected, as the player reads it: rich text tags left out, a wrapped line joined back up. |
| `Copy()` | Copies the selection, or all of the text when nothing is selected. Call it from a click. |
| `OnCopied` | Something was copied, with what. |
| `HighlightColor` | The tint over the selection. Drawn over the glyphs, so keep it see-through. |
| `LongPressSeconds` | How long a finger has to stay still. Zero turns the long press off. |
| `UiCopyButton.On(label, size, gap)` | Puts a copy icon at a label's right edge, or finds the one already there, and keeps the text clear of it. |
| `UiCopyButton.Copy()` · `OnCopied` · `ShowingCopied` | Copy from code; something was copied; the tick is showing. |
| `UiCopyButton.Source` · `Text` | The label it copies, or fixed text when there is none. |
| `UiClipboard.Copy(text)` | Copies any text. |
| `UiClipboard.Offer(text)` | Says what the next Ctrl+C copies in a browser. `UiSelectableText` does this itself. |
| `UiClipboard.OfferClick(text)` | Says what the release of the press now going on copies in a browser. `UiCopyButton` does this itself. |

## Files

| File | |
| --- | --- |
| `UiSelectableText.cs` | The selection: pointer, keys, the touch hand-off and the long press. |
| `UiSelectionHighlight.cs` | The tint over the selection, one rectangle a line. Made when first needed, never saved. Internal. |
| `UiCopyButton.cs` | The copy icon, the tick, and the offer made on the press. |
| `UiClipboard.cs` | The clipboard, the same call on every platform. |
| `UiSelectableTextExample.cs` | A seed, a line with a tinted caption, and a hash long enough to wrap, each with a copy icon. Drop it on an empty RectTransform in a canvas. |
| `../../JSPlugins/Clipboard.jslib` | The browser's half: the copy inside the key press or the release. |
| `../Window/BetInfoSheet/BetInfoSheetWindow.cs` | Puts it on the sheet's values. |
