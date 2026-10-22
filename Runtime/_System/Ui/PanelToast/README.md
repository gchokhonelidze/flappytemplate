# Toast

[← All documentation](../../../../)

A message that slides in over the top right corner of the game and goes away again: a dark pill with a sign on
its left and the message beside it, in one of four kinds — a yellow warning on a triangle, a green success with
a tick, a red error with a bang, and a plain charcoal one with an i. The pill is drawn, not imported: a
[Rounded Box](../RoundedBox/), a generated triangle and a label, so every colour on it is a field.

```csharp
UiManager.Inst.OnShowToast.Invoke(new ToastOptions
{
    Type = EToastType.Success,
    Message = "Bet placed",
});
```

Nothing to add to a scene: the `FlappyCanvas` prefab carries one as `ToastPanel`, and `UiManager.Inst.OnShowToast`
reaches it from anywhere. There is one toast; a message sent while another is up replaces it.

*Describes package 1.0.98. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the three toasts drawn.*

## What it draws

| | |
| --- | --- |
| **The pill** | A `RoundedBox` filled in the kind's colour, **Corner Radius** round, no border. As wide as its text wants up to **Max Width**, and **Min Height** tall unless the text wraps or a title is set, when it grows downwards. It takes the click, so a press on a toast does not land on the game under it. |
| **The sign** | A square of **Icon Size** on the left, drawn in the kind's ink: a rounded triangle with a bang, a disc with a tick, a disc with a bang, a disc with an i. The mark on each is painted in the pill's colour rather than cut out — see [Worth knowing](#worth-knowing). |
| **The text** | The message in the ink, left-aligned beside the sign; a bold title over it when one is given. Wraps at the room **Max Width** leaves. |
| **The cross** | A disc of the ink at **Close Disc** of its alpha, with two bars of the ink across it, on the right. Only when the options ask for it, with a [ripple](../Ripple/) when the style does. |

| Kind | Pill | Ink | Sign |
| --- | --- | --- | --- |
| `Warning` | `#292200` | `#FFE766` | A bang on a triangle |
| `Success` | `#081B0D` | `#46E46E` | A tick in a disc |
| `Error` | `#1F0504` | `#F56A55` | A bang in a disc |
| `Default` | the windows' charcoal | white | An i in a disc |

```
ToastPanel      Toast, on a RectTransform pinned to the top right corner
├ Panel         RoundedBox — the pill
├ Icon          the sign: Warn Sign · Warn Bar · Warn Dot, or Done Disc · Done Tick A · Done Tick B, …
├ Title         TextMeshProUGUI, bold — off while there is no title
├ Message       TextMeshProUGUI
└ Close         RoundedBox disc · Button · UiRipple — off unless asked for
  └ Cross       Bar A, Bar B
```

## Showing one

`ToastOptions` is the message: everything about one toast that is not its look.

| Field | Default | What it does |
| --- | --- | --- |
| Type | `Default` | Which kind — and so which palette and sign. |
| Message | empty | What it says. |
| Title | empty | A bold line over the message. Empty leaves the message on its own, which is the usual case. |
| HideAfter | 3 s | How long it stays once it has arrived, when Auto Hide is on. |
| AnimationDuration | 0.5 s | The slide in, and the slide out. |
| AutoHide | on | Go away on its own after Hide After. Off stays until `Hide` — give it a close cross. |
| ShowCloseButton | off | The cross on the right. Not in the design, so off unless asked for. |

The slide runs on unscaled time, so a message still arrives over a paused game. It comes from off the right edge
every time, even over a toast that is already up — the new one arriving is what tells the player it is new.

`UiManager` has the two events: `OnShowToast` takes the options, `OnHideToast` takes nothing. Both are forwarded to
the toast's own `Show` and `Hide`, which can be called straight on it as well.

## Style

The look is a `ToastStyle` on the component, laid out in the inspector. The sizes are the design's own, measured
off it: a pill 64 tall with corners of 8, a sign of 28 set 18 in from the left edge, the text 11 past the sign.

| Field | Default | What it does |
| --- | --- | --- |
| Corner Radius | 8 | The pill's corners. |
| Min Height | 64 | The pill is never shorter, however little is on it. |
| Max Width | 460 | Text wider than fits inside this wraps, and the pill grows downwards. |
| Padding X · Padding Y | 18 · 18 | Inside the pill: left and right, above and below the text. The vertical one is felt only once the text is tall enough to push past Min Height. |
| Inset | 20, 20 | From the top right corner of the canvas to the top right corner of the pill. |
| Icon Size · Icon Gap | 28 · 11 | The sign, and the room between it and the text. |
| Font | empty | The TextMeshPro default when empty. |
| Title Size · Title Style | 16 · Bold | |
| Message Size · Message Style | 14 · Normal | |
| Title Gap | 2 | Between the title and the message under it. |
| Close Size · Close Gap | 28 · 10 | The cross's disc, and the room between the text and it. |
| Close Thickness | 2 | The bars of the cross. |
| Close Disc | 0.14 | The disc under the cross, as a share of the ink's alpha. |
| Close Wave | on | A [ripple](../Ripple/) round the cross when it is pressed, the same as a window's. |
| Slack | 24 | How far past the canvas edge the pill starts from, beyond its own width. |
| Default · Error · Warning · Success | the table above | A **Fill** and an **Ink** each. |

A field changed in the inspector takes effect on the next `Show`, or on **Build Now**. The prefab saves the style
with the toast, so a default changed in `ToastStyle.cs` does not reach a toast that already exists — assign
`toast.Style = new ToastStyle()` to bring one back to the package's look.

## In the editor

The component's context menu has **Build Now**, which makes every part that is missing and paints them, and
**Preview Success / Warning / Error / Default**, which puts a sample of that kind where a real one would rest —
without animating it or taking it down again — so the palettes can be judged with nothing running. The toast is
inactive in the prefab; a preview switches it on, so switch it off again before saving.

## From code

| | |
| --- | --- |
| `Show(options)` | Slides a message in. Builds the toast first if it has not been. |
| `Hide()` | Slides it out and switches it off. Nothing happens if it is not up. |
| `Preview(kind, text)` | A sample on screen, no animation, no auto hide. |
| `Build()` | Makes the parts and paints them. Safe to run again. |
| `Style` | The look. Assigning one repaints a built toast. |
| `IsShowing` | On screen, or on its way there. |
| `Current` | The options being shown, or last shown. Null before the first `Show`. |
| `Panel` · `TitleText` · `MessageText` · `CloseBox` | The parts, for anything the style does not cover. |

```csharp
// A warning that stays until it is closed
UiManager.Inst.OnShowToast.Invoke(new ToastOptions
{
    Type = EToastType.Warning,
    Title = "Connection lost",
    Message = "Trying again. Wait for the line to come back before playing on.",
    AutoHide = false,
    ShowCloseButton = true,
});

// A palette of the game's own, on the toast the canvas carries
var style = UiManager.Inst.panelToast.Style.Clone();
style.Success = new ToastPalette(new Color(0.004f, 0.02f, 0.03f), new Color(0.1f, 0.8f, 1f));
UiManager.Inst.panelToast.Style = style;
```

`ToastExample` sends one of each from the keyboard: 1 a success, 2 a warning, 3 an error, 4 the plain kind, and T
a titled error with a cross that stays until it is closed.

## Worth knowing

- **The palette colours are linear values.** The template renders in linear colour space, where a `Color` is
  taken as linear and comes out lighter than its numbers read: `0.022, 0.016, 0` is what reaches the screen as
  `#292200`. Typed in as the hex they would be, the pills come out olive, mint and rose. The same goes for a
  palette of your own — convert it, or pick it by eye in a preview.
- **The marks are painted, not cut.** The bang, the tick and the i are drawn in the pill's colour over the sign,
  which against a flat pill is the same thing as a hole. Against a translucent pill it is not: the mark would
  show as the pill's colour while the pill shows the game through. Keep **Fill** opaque.
- **One toast.** A message sent while another is up replaces it, from off the edge again. A queue is the
  game's to keep, if it wants one.
- **It draws over everything.** `ToastPanel` is on `FlappyCanvas`, which sorts at 200 — above a game's own
  overlay canvases, which are usually at nought, and above the windows, which sort at 100. A message is no use
  under the thing it is about. A canvas of the game's own sorted higher than that covers it; set **Inset** to
  clear whatever the game puts in the corner instead.
- **Styling is not live.** The style is read when the toast is built and when a message is shown, not every
  frame, so a slider dragged in the inspector shows on the next **Build Now** or `Show`.
- **Built with `DOTween.To` rather than `DOAnchorPos`.** The shortcuts live in DOTween's UI module, which is
  compiled into the project's own assembly and cannot be reached from a package. The windows do the same.

## Files

| File | |
| --- | --- |
| `Toast.cs` | The component: the parts, the layout, the slide in and out. |
| `ToastStyle.cs` | `ToastStyle` and `ToastPalette`: every size and colour. |
| `ToastOptions.cs` | `ToastOptions` and `EToastType`: one message. |
| `ToastIcons.cs` | The four signs, drawn from a triangle and rounded boxes. |
| `UiTriangle.cs` | A triangle with rounded corners and soft edges, as a UI graphic. The one shape a `RoundedBox` cannot be. |
| `ToastExample.cs` | One of each kind from the keyboard. |
| `../UiManager.cs` | `OnShowToast` and `OnHideToast`, and the `panelToast` they reach. |
| `../../../Prefabs/FlappyCanvas.prefab` | Where `ToastPanel` lives. |
| `../RoundedBox/` | What the pill, the discs and the bars are drawn with. |
| `../Ripple/` | The wave round the close cross. |
