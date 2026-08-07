# Window

A dialog that is one component rather than a prefab: a rounded panel, a caption, a close button, a drag,
and an opening that is animated rather than a `SetActive`. Everything it is made of is built from code the
first time it is needed, so there is no hierarchy to keep in step with the palette and nothing to rebuild
when a game decides its dialogs have square corners after all.

`StatisticsWindow` beside it is one worked example of filling a window in: the two-tab statistics panel,
reading `MainState.Statistics` and resetting the current session through the socket.

*Describes package 1.0.55. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the window drawn rather than described.*

**GameObject → UI (Canvas) → Window**, which makes a canvas if the scene has none, drops the window under
whatever was right-clicked, and builds the whole thing on the spot so it arrives looking like a window
rather than as an empty rect waiting for play mode.

Adding the component by hand instead, it belongs on an **empty RectTransform**, not a UI Panel: the window
draws its own panel, so a Panel's Image leaves two backgrounds stacked, and anything already inside that
Panel ends up beside the caption rather than under `Content`. Or build one from code:

```csharp
UiWindowBuilder.Create(canvas, "Settings")
    .Size(360f, 480f)
    .Title("Settings")
    .Draggable()
    .Backdrop()
    .Open();
```

## The window

| Field | What it does |
| --- | --- |
| Title | The caption text. Also `Title` from code, live. |
| Style | Colours, sizes and fonts — the table below. |
| Show Caption | Off drops the whole header and starts the content at the top of the panel. The close button stays. |
| Show Close Button | |
| Start Closed | Hide at Awake and wait for `Open`. Off leaves the window exactly as the scene saved it. |
| Destroy On Close | For a window built for one message — it takes itself down with it. |
| Draggable / Drag Anywhere | By the caption, or by any part of the window. Anywhere catches drags on everything inside it that does not handle its own. |
| Clamp To Parent | Keep the window inside its parent, per axis — see below. |
| Keep Visible | How much of a window too big for its parent has to stay inside it. |
| Bring To Front | Draw over the siblings when grabbed or opened. |
| Show Backdrop | A sheet across the parent behind the window, which is what makes it modal — it swallows every click that misses. |
| Close On Backdrop Click | |
| Always On Top | On by default. Gives the window a canvas of its own with Override Sorting, which is what lifts it clear of the game — see below. |
| Sorting Order / Layer | Where that canvas sorts. The backdrop takes one less, so it stays behind its own window and over everything else. Leave the layer empty for the parent canvas's own. |
| Transition | `None`, `Fade`, `Scale`, `ScaleFade`, `SlideUp/Down/Left/Right`. Slides are named for the way the window travels as it opens. |
| Open / Close Duration, Easing | |
| Unscaled Time | On by default, so a window still opens over a paused game. |

`OnOpened`, `OnClosed` and `OnCloseClicked` are UnityEvents. **`OnClosed` fires when the animation has
finished**, not when `Close` was called — a window is still on screen for the length of its close.

### Style

| | |
| --- | --- |
| Fill, Border Color / Size, Corner Radius, Edge Softness | The panel. Straight through to `RoundedBox`. |
| Caption Height | The whole header block: the drag strip, the close button and the title. Content starts under it. |
| Caption Fill | Drawn **over** the panel fill, so the default is a wash rather than a colour — alpha at nothing leaves the header the same colour as the body. |
| Title Top Inset | Space above the title, which is what leaves room for the close button over it. |
| Title Font / Size / Color / Style / Alignment | |
| Close Size / Offset / Fill / Border / Corner Radius | Offset is measured inwards from the top right corner. A negative corner radius means a circle at any size. |
| Close Icon Color / Thickness / Scale | |
| Close Icon | A sprite in place of the drawn cross. Its colour still comes from the style. |
| Content Padding Left / Top / Right / Bottom | Inset of the content area. Top is measured from the bottom of the caption, not the top of the panel. Four floats rather than a `RectOffset`: that type is a handle onto a native object and cannot be built in a field initialiser. |
| Backdrop Color | |
| Open Scale | What a window grows from, and shrinks back to. |

`ApplyStyle()` pushes the lot onto the parts and is cheap enough to call on a window that is already open —
which is what a theme change is. `Style` as a property applies itself when assigned. `Clone()` gives a
window its own copy so it can be themed without the change reaching every other window sharing that style.

## From code

`UiWindowBuilder` says a whole window in one chain, the way `RoundedBoxBuilder` does a box. A struct holding
one reference, so a chain allocates nothing, and a step against a window that has been destroyed is passed
over rather than thrown.

```csharp
// A modal that slides up and takes itself down afterwards
UiWindowBuilder.Create(canvas, "Message")
    .Size(420f, 260f)
    .Title("Well played")
    .Backdrop(new Color(0f, 0f, 0f, .65f))
    .Transition(EWindowTransition.SlideUp, .34f, .24f)
    .Easing(Ease.OutCubic, Ease.InCubic)
    .Fixed()
    .DestroyOnClose()
    .OnClosed(NextRound)
    .Open();

// Or reconfigure one that already exists
UiWindowBuilder.For(window)
    .Title("Paused")
    .Fill(Color.black)
    .NoBackdrop();
```

| Step | Notes |
| --- | --- |
| `Create(parent, name)` / `For(window)` | Start. `Create` centres the new rect in its parent. |
| `Size(w, h)` · `At(pos)` · `Scale(uniform)` | |
| `Title(text)` · `TitleFont(font, size)` · `TitleColor(c)` | |
| `Caption(height, fill)` · `NoCaption()` | |
| `Style(style)` | A whole style at once. Copied. |
| `Fill(c)` · `Border(size, c)` · `Corners(r)` · `Padding(l, t, r, b)` | |
| `CloseButton(bool)` · `CloseIcon(sprite)` · `CloseColors(fill, icon)` | |
| `Draggable(anywhere, clamp)` · `Fixed()` | |
| `OnTop(order, layer)` · `InLine()` | Own canvas and sorting, or hierarchy order like any other UI object. |
| `Backdrop(closeOnClick)` · `Backdrop(color, closeOnClick)` · `NoBackdrop()` | |
| `Transition(kind, open, close)` · `Easing(open, close)` | |
| `StartOpen()` · `DestroyOnClose()` | |
| `OnOpened(handler)` · `OnClosed(handler)` | |
| `Add<T>(out T)` | A component on the window before it wakes — see below. |
| `With(child)` | Parents something into the content area. |
| `Done()` · `Open()` · `OpenInstant()` | The end of the chain. |

`Create` leaves the new object **switched off** until the chain ends. That is not a detail: a MonoBehaviour
added to an active object runs `Awake` there and then, and a window that woke in the middle of the chain
would have built and hidden itself before it had been told how big it is. `Done` switches it on, and the
window wakes with every answer already in place. `Add<T>` exists for the same reason — it is how
`StatisticsWindow` gets onto a window built from code.

Whatever the window is for goes under `Content`:

```csharp
var window = UiWindowBuilder.Create(canvas, "Rules").Size(420f, 520f).Title("Rules").Done();
Instantiate(rulesPrefab, window.Content, false);
window.Open();
```

## Statistics

**GameObject → UI (Canvas) → Statistics Window**, or Add Component → UI → Statistics Window on a `UiWindow`,
or the whole thing in one line:

```csharp
var stats = StatisticsWindow.Create(canvas);
stats.Window.Open();
```

Two tabs over a column of values, and a reset button under them. Everything it shows comes from
`MainState.Statistics`, which the server fills in and refreshes over the socket. Nothing is polled: the
window redraws when `OnStatistics` fires and when the player switches tabs, and sits still the rest of the
time. Both halves — current and overall — arrive together, so switching tabs asks the server for nothing.

| Field | What it does |
| --- | --- |
| Style | Tabs, rows and reset button. The panel around them is the window's own style. |
| Current / Overall Label | The tab captions. Nothing here is translated. |
| Tab | Which half is showing. `Tab`, `SetTab`, `ShowCurrent()` and `ShowOverall()` from code. |
| Rows | What it shows, in the order it shows it — see below. |
| Show Reset | |
| Reset On Current Only | Hide the button on the Overall tab. On by default: there is no such thing as resetting the overall figures, and a button that does nothing is worse than no button. |
| Reserve Reset Room | Keep the rows clear of the button. Off lets it sit over the bottom of the column, which is tighter. |
| Follow State | Redraw when the server sends new statistics. |
| Fit Window Height | Resize the window to exactly the rows it is showing. |

### Rows

`StatsDto` carries ten fields and no game wants all ten, so the rows are a list rather than anything fixed.
Each is a caption, the field under it, how it is coloured, and an optional suffix.

| | |
| --- | --- |
| Label | The caption above the value. |
| Field | `Wager`, `WagerWon`, `WagerLost`, `GrossWin`, `NetProfit`, `Payouts`, `Luck`, `BetCount`, `WinCount`, `LoseCount`, or `Counts` for all three counts on one line. |
| Tint | `Plain`, `Signed`, `Strict` or `Counts`. |
| Suffix | Printed after the value — a percent sign, a currency code. |
| Enabled | Off leaves the row out entirely rather than blank, so the ones below close up. |

Where the line between the two colours falls is not the same for every number, which is why there are two
signed tints. `Signed` prints zero and above in the positive colour: nothing wagered yet is not a loss.
`Strict` prints only above zero: luck of zero is the worst there is. The defaults are the five rows the
design was drawn with —

```csharp
new StatisticsRow("Total wagered", EStatField.Wager),
new StatisticsRow("Bets / Wins / Losses", EStatField.Counts, EStatTint.Counts),
new StatisticsRow("Revenue", EStatField.GrossWin, EStatTint.Signed),
new StatisticsRow("Total profit", EStatField.NetProfit, EStatTint.Signed),
new StatisticsRow("Luck", EStatField.Luck, EStatTint.Strict),
```

— and the list is public, so a game can build its own and call `Rebuild()`:

```csharp
stats.Rows.Clear();
stats.Rows.Add(new StatisticsRow("Staked", EStatField.Wager));
stats.Rows.Add(new StatisticsRow("Return", EStatField.Luck, EStatTint.Strict, "%"));
stats.Rebuild();
```

The counts line is one label with the colours written into it as rich text rather than three labels to lay
out, so the row stays one row however wide the numbers get.

### Values

Money arrives from the server as **strings**, and is kept as strings all the way to the label: a decimal
that has been through a float is no longer the number that was wagered. It is parsed only to decide which
side of zero it falls on, and reformatted only when `Decimals` says to.

| | |
| --- | --- |
| Decimals | Below zero prints the number exactly as the server sent it, which is the only way to be sure nothing was rounded off. |
| Group Digits | Thousands separators, when a decimal count is set. |
| Prefix | Printed before every money value — a currency symbol. Counts and luck are left alone. |

Anything that will not parse is printed exactly as it came, whatever the settings say.

### Reset

The button emits `STAT_RESET` through `Emitter` and returns. The figures come back as an ordinary
statistics broadcast, so the window is never showing anything the server has not confirmed. It refuses on
the Overall tab whatever is wired to it — `ResetCurrent()` is a no-op there, not just a hidden button.

With no socket at all — a scene with no `StateManager` in it — the window falls back to a sample set of
figures so there is something to look at, and the reset clears those instead. A `StateManager` that is
running but has not been sent statistics yet shows zeroes rather than the sample; a real game must never
show invented numbers.

```csharp
stats.OnResetRequested.AddListener(PlayClearSound);
stats.OnTabChanged.AddListener(tab => Debug.Log(tab));
```

## Drawing over the game

A window that comes out behind the game is not a hierarchy problem, and moving it down the hierarchy will
not fix it. **Bring To Front only settles the window against its own siblings.** Everything else on screen —
another canvas, and anything the scene draws rather than the canvas: sprites, meshes, particles — is sorted
long before sibling order is consulted.

**Always On Top** is the answer, and is on by default: the window gets a `Canvas` of its own with *Override
Sorting*, at **Sorting Order** 100. A canvas and a sprite sort against each other by exactly that number, so
100 clears anything the game draws below it. A `GraphicRaycaster` goes on with it and is not optional — a
nested canvas takes its graphics out of the parent canvas's raycast list, and without one the window would
draw on top and let every click fall through to whatever is behind it.

Two cases it does not cover on its own:

- **The game draws on a higher sorting layer.** A layer outranks every order within it, so no number wins.
  Name that layer in **Sorting Layer**, or move the game's sprites to one below the UI's.
- **The window is under a Screen Space - Overlay canvas and still hidden.** An overlay canvas draws over the
  whole scene, so the thing in front is other UI. Raise the order, or check that the window is not inside a
  panel that clips it.

## Worth knowing

- **The close cross and the reset arrow are drawn, not fetched.** Two rotated boxes and a ring with a notch
  in it, which costs no atlas entry and stays sharp at any size. The notch is *painted in the button's own
  colour*, not cut, so it only disappears against a flat button — give the style a `ResetIcon` sprite if the
  button is ever a gradient.
- **Scaling the rect is a fair way to size a whole window down.** The panel is generated geometry and the
  text is SDF, so neither softens the way a sprite would. The transition measures its scaling against
  whatever the window rests at rather than against 1, so opening does not throw that scale away; `RestScale`
  and `SetScale(uniform)` set the same thing from code, and take effect on a window already on screen. The
  drag clamp and the slide distance both take the scale into account, so a scaled window is bounded by what
  it draws rather than by what its rect says.
- **Slides are measured against the parent**, not the window, so a window leaves the screen rather than
  moving its own width and stopping where it can still be seen. That assumes a window roughly centred in its
  parent, which is what `Create` leaves.
- **Dragging is a delta**, not a placement: the pointer's travel since the grab is added to where the window
  was. A window grabbed by its corner does not jump so its middle lands under the cursor, and none of it
  depends on how the anchors or the pivot are set.
- **The clamp works per axis, and changes rule when the window does not fit.** On an axis where it fits, the
  window is held wholly inside the parent. On one where it does not — a window taller than the canvas, which
  is easy to arrive at — containment has no meaning, so it is dropped for an overlap: drag as far as leaving
  **Keep Visible** units of the window inside. Without that, a too-tall window is pinned on the axis it
  overflows and the drag reads as working sideways and dead vertically.
- **Tweens run on unscaled time by default.** A window that opens over a paused game is the usual reason to
  have one.
- **A window saved switched off wakes inside the first `Open`.** Unity runs `Awake` from the `SetActive`
  that call makes, so **Start Closed** has to know it is being asked to hide a window that is in the middle
  of being opened, and leave it alone. `IsOpen` is set before that `SetActive` for exactly this reason —
  without it the first press is swallowed and the window appears on the second.
- **Listeners are re-hooked on every load, not only when the window is built.** `AddListener` is not
  serialized, so the handlers put on the close button and the backdrop when a window was built in the editor
  are gone by the time the scene is played — while the parts they were added to, and the flag saying the
  window is built, both survive. Hooking only where the parts are made leaves a close button that does
  nothing.
- Parts are found by name before they are made, so `EnsureBuilt` can be called as often as you like and a
  window saved as a prefab rebuilds into itself rather than into two of everything. `Rebuild()` from the
  component's context menu is the way out of a hierarchy that has been edited into a state the style can no
  longer describe.
- Every tween is built with `DOTween.To` rather than the `DOFade`/`DOAnchorPos` shortcuts. Those live in
  DOTween's UI module, which is compiled into the project's own assembly and cannot be reached from a
  package. `Toast` does the same thing for the same reason.

## Files

| File | |
| --- | --- |
| `UiWindow.cs` | The window: parts, style, drag, transition. |
| `UiWindowBuilder.cs` | The fluent API. |
| `UiWindowStyle.cs` | Everything a window looks like. |
| `UiWindowDragHandle.cs` | The grab. Usable on its own, for a custom header. |
| `UiWindowParts.cs` | Making and finding children. Internal. |
| `EWindowTransition.cs` | |
| `StatisticsWindow.cs` | The statistics panel. |
| `StatisticsWindowStyle.cs`, `StatisticsRow.cs` | What it looks like, and what it shows. |
| `EStatsTab.cs`, `EStatField.cs`, `EStatTint.cs` | |
| `UiWindowExample.cs` | Three windows built from code. Drop it on an empty RectTransform in a canvas. |
| `Editor/Window/UiWindowMenu.cs` | The two GameObject → UI entries. |
| `../RoundedBox/` | Every panel here is one. |
