# Window

A dialog that is one component rather than a prefab: a rounded panel, a caption, a close button, a drag,
and an opening that is animated rather than a `SetActive`. Everything it is made of is built from code the
first time it is needed, so there is no hierarchy to keep in step with the palette and nothing to rebuild
when a game decides its dialogs have square corners after all.

Inside, the caption and the body are two rows of a [`UiGrid`](../Grid/), and the body **scrolls** once the
content is taller than the screen has room for — so a dialog is bounded by what it is drawn on rather than by
what its author guessed.

`StatisticsWindow` and `BetInfoWindow` beside it are two worked examples of filling a window in: the two-tab
statistics panel reading `MainState.Statistics`, and the bet info dialog that asks the server for one bet and
lays out what came back.

*Describes package 1.0.58. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the windows drawn rather than described.*

**GameObject → UI (Canvas) → Window**, and **Statistics Window** and **Bet Info Window** beside it. Each
makes a canvas if the scene has none, drops the window under whatever was right-clicked, and builds the whole
thing on the spot so it arrives looking like a window rather than as an empty rect waiting for play mode.

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
| Fit Content Height | Ask the content how tall it wants to be on every open, and be that tall. Needs something under `Content` that reports a height — see below. |
| Max Height | The tallest the window may be, in its own units. Zero means the parent it is drawn in — the screen, for a full-screen canvas — less Screen Margin. |
| Screen Margin | Room left above and below a window that has grown as far as it may. |
| Scroll | `Never`, `WhenTooTall` (the default) or `Always`. |
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
| Show Scrollbar | The bar down the right of the body. Off leaves the wheel and the drag, and no hint that there is more below. |
| Scrollbar Width / Inset / Corner Radius | Inset is measured from the right edge of the body inwards; the content is moved over by the width and the inset together, so the two never overlap. A negative radius rounds the bar fully. |
| Scrollbar Track / Handle Color | |
| Scroll Sensitivity | Canvas units per notch of the wheel. |
| Scroll Inertia / Deceleration | Whether a flick carries on after the finger has left, and how quickly it runs out. On by default — a touch screen expects it, and WebGL on a phone is a touch screen. |
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

## How tall it may be

The inside of a window is a `UiGrid` of one column and two rows — the caption at the height the style says, the
body taking the rest:

```
Window            RoundedBox · UiGrid · CanvasGroup
├ Caption         cell "caption"        row 0, Fixed(Caption Height)
│  └ Title
├ Viewport        cell "body"           row 1, Flexible · RectMask2D · ScrollRect
│  ├ Content      ← whatever the window is for
│  └ Scrollbar    Track → Sliding Area → Handle
└ Close           an overlay: ignored by the grid entirely
```

Two things follow from that. **`Content` is a grandchild now**, inside `Viewport` — a window saved before this
moves its own `Content` in on the next build, children and all, so nothing is lost and nothing needs doing by
hand. And the caption is shown and hidden **by the grid's layout** rather than by `SetActive`, because a grid
takes its layout as the whole truth about which of its children are showing and re-asserts it whenever it is
enabled. The close button sits outside all of that: it carries an ignored `LayoutElement`, so the grid neither
places it nor has an opinion about whether it is visible.

### Fitting

| Call | Does |
| --- | --- |
| `Fit()` | Measures the content, makes the window that tall, clamps, and scrolls the rest. |
| `FitTo(height)` | The same from a height the caller worked out itself. |
| `ScrollToTop()` | Back to the top of the body. |
| `IsScrolling` · `Scroller` · `Viewport` · `Grid` | What it ended up doing, and the parts that do it. |

`Fit()` asks the content: `LayoutUtility.GetPreferredHeight(Content)` after an immediate rebuild. That needs
something under `Content` that **reports a height** — a layout group (`UiGrid` included), a label, a
`LayoutElement`. A window whose content is placed by hand reports nothing and must work its own number out and
call `FitTo` instead, which is exactly what the two windows in this folder do: `BetInfoWindow` lays its card out
with a grid and calls `Fit()`, `StatisticsWindow` adds its rows up and calls `FitTo()`.

Every route ends at the same clamp, so **the height a window asks for and the height it gets are two different
numbers** and the window remembers both. Reopening does not mistake a clamped height for the author's wish, and
a height set by hand between opens is noticed and taken as the new wish.

### Scrolling

Once the wanted height is past what is allowed, `Scroll` decides what happens:

- `Never` — the window stays too tall, as it did before any of this existed.
- `WhenTooTall` — the body scrolls, and only then. The default.
- `Always` — the bar and the room it takes are there whatever the content does, so the layout never shifts.

The limit is **Max Height**, or the parent's height less **Screen Margin** at both ends when that is zero. It is
measured in the window's own units: a window at half scale is allowed twice as much of itself, the same
reasoning the drag clamp uses. A phone that turns or a player window that is dragged wider changes that limit
without anything asking, so it is checked once a frame per open window — a float compare — and the clamp is
worked out again when it moves.

While scrolling, `Content` is as tall as it asked to be and anchored to the top of `Viewport`, which is the
shape a `ScrollRect` moves; the mask clips at the viewport, and the content's right padding grows by the bar's
width so the two never overlap. While not, `Content` fills the viewport exactly as it filled the window before,
and both the mask and the scroller are switched off — nothing is moving, so nothing needs clipping.

**Touch and the wheel both need something to land on.** A `ScrollRect` is only offered a drag or a scroll if the
pointer hits a raycast target that is the scroller itself or something under it — and every graphic a window puts
in its body is deliberately *not* one, because a card or a rule that swallowed clicks would break the buttons
under it. So the viewport wears an invisible `Image` of its own, over the whole body and behind everything in it:
that is what makes a finger drag work, and it is switched off along with the scrolling so a window that is not
scrolling neither draws it nor catches anything with it. Buttons inside the content still win, being deeper.

One thing to know if a scrollbar ever looks wrong: **UGUI's `Scrollbar` moves its handle by writing the anchors
and nothing else**, so the handle's own size and position have to be zero for it to sit in the band it is given.
Left at the 100 by 100 a new rect comes with, it draws a hundred units past the track on every side — which reads
as an enormous bar and a Scrollbar Width that does nothing at all.

```csharp
// A rules dialog that is as tall as its text and no taller than the screen
UiWindowBuilder.Create(canvas, "Rules")
    .Size(520f, 0f)
    .Title("How to play")
    .Done();

window.FitContentHeight = true;
window.MaxHeight = 0f;        // the parent, less Screen Margin
window.Scroll = EWindowScroll.WhenTooTall;
window.Open();                // fitted, clamped and scrolling on the way in
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
| Fit Window Height | Resize the window to exactly the rows it is showing. The number is handed to the window rather than written onto its rect, so a column of rows taller than the screen scrolls instead of overflowing. |

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

## Bet info

**GameObject → UI (Canvas) → Bet Info Window**, or Add Component → UI → Bet Info Window on a `UiWindow`, or
the whole thing in one line:

```csharp
var info = BetInfoWindow.Create(canvas);
info.Show(betId);
```

One bet: what it cost, what it paid, who made it, in which game, and the seeds it was rolled from. `Show(id)`
emits `BET_INFO` through `Emitter` and opens the window on a row of pulsing dots; the answer arrives as
`ON_BET_INFO_ID`, which the socket puts in `MainState.BetInfoById` and announces as `OnBetInfoById`, and the
window fills itself in from that. Nothing is polled, and nothing is shown that the server has not sent.

Most games never call `Show` themselves: a **history strip** (`../History/`) opens this dialog on whichever bet
was clicked, finding one in the scene if it was not given one.

| Call | Does |
| --- | --- |
| `Show(id)` | Asks for that bet and opens on the loader. Uses `MainState.BetInfoById` at once if it already holds this id, so looking at the same bet twice asks once. |
| `Show(transaction)` | Opens on a `TransactionPublic` the game already has. Nothing is asked. |
| `Show()` | Opens on whatever the state last received — for a window driven by the game's own `Emitter.OnBetInfo`. |
| `Request(id)` | Asks without opening anything. |
| `Clear()` | Back to the loader, for a window about to be pointed at another bet. |
| `ToggleDetails()` · `DetailsOpen` | The seeds half. |
| `Verify()` | Opens the verifier in a browser — the same thing the button does. |
| `Refresh()` · `Layout()` · `Rebuild()` | Fill in again, lay out again, build again. |

| Field | What it does |
| --- | --- |
| Style | The card and its fields. The panel around them is the window's own style. |
| Labels | Every caption on the dialog, `Profit` to `Server SHA-512`. Nothing here is translated. |
| Show Profit / Bet Payout / Player / Game | The blocks, each a row of the card. The pairs share a row: bet with payout, player with the bet's id, game with the time. |
| Show Details | The Details button and the seeds it opens. |
| Show Verify | The Verify button. It appears only on a transaction the server sent a `VerifyUrl` with. |
| Outcome Height | Height of the block the game's own view is given. Zero measures what is in it — see below. |
| Follow State | Fill in when the server sends a transaction. |
| Request On Show | Ask the server when `Show` is given an id. Off leaves the asking to the game. |
| Match Requested Id | Ignore an answer whose id is not the one asked for. On by default: every open window hears every answer, and a window opened on one bet must not show another. |
| Load Images | Fetch the currency, game and avatar pictures the server sends as urls. Off leaves the drawn stand-ins showing. |
| Fit Window Height | Resize the window to exactly the blocks it is showing. |
| Collapse Details On Close | Close the seeds when the window closes, so a dialog reopened on another bet starts the way it first opened. On by default: the seeds one bet needed are not the height the next one's do. |

`OnTransaction`, `OnRequested`, `OnVerify` and `OnDetailsToggled` are UnityEvents.

The window is fitted **twice over, and again on the next two frames**. A label reports the height it needs at
the width it has, and its width is what the first pass settles — so on the way into a window that has just
been activated a wrapped hash measures as one line, and a card fitted to that leaves the seeds hanging below
the panel. Anything else the canvas settles late — a font finishing loading, a picture arriving — is caught by
the frames after.

### The game's own half

What a bet *meant* is the one thing the template cannot know: a dice game draws the roll, a crash game the
multiplier it stopped at, a plinko game the slot the ball fell into. So the fields stop at what every
transaction carries, and **`Outcome` is a full-width block under them that the game fills in**.

```csharp
info.OnTransaction.AddListener(data =>
{
    // data.Outcome and data.Custom are the server's own payloads for this bet
    var view = Instantiate(rollPrefab, info.Outcome, false);
    view.Draw(data.Outcome);
});
```

`Outcome` is a one-column grid with an auto row each, so several things stacked into it come out in
hierarchy order. Auto means *as tall as what is in it says it needs to be* — a label or a layout group
answers that, **a plain panel or an image does not** and comes out at nothing. Give that a `LayoutElement`,
or set **Outcome Height** and the block is that tall whatever is in it.

`Outcome` is the only way in, and the card is not: which blocks the card is showing is said to the grid as a
**layout** — a picture of the grid naming one area per cell — and *a grid hides every child its layout does not
name*. A panel parented straight into the card would be switched off the next time the layout was set. Inside
`Outcome` nothing is named, so everything shows; the flip side is that a game which hides one of its own views
in there with `SetActive` will find it switched back on, since a grid with no layout takes every child as
showing. Keep a view you mean to hide out of the grid, or hide what is inside it.

### Values

Money arrives as **strings** and is parsed only to be printed: truncated to the transaction's own
`DecimalPoints` and trimmed of trailing zeroes, which is the order the web front does it in — so the same bet
reads the same in both, `0` as `0.0` and a satoshi as `0.00000001`. `Decimals` overrides the transaction's
count; `Payout Decimals` is separate, because a multiplier is not money.

| | |
| --- | --- |
| Decimals | Below zero uses the transaction's `DecimalPoints`, or 8 where the server did not fill it in. |
| Payout Decimals | Four, as the design draws it. |
| Trim Zeros | Drop trailing zeroes, keeping one after the point. |
| Day / Time Format, Local Time | `CreatedAt` is milliseconds; a seconds feed is read as seconds rather than as 1970. The day is drawn bold and the time beside it. |

The banner is green from a **payout of one** and red below it — not from the win amount, which says nothing
on its own about whether the bet came back.

### Details and verify

Details opens the seeds: the nonce, the seed the player contributed, the seed the server kept, and the hash it
published before the roll. The nonce and the client seed are a single-player idea, so on a `SHARED` or `MULTI`
transaction the nonce row is dropped and the seed is captioned as the block hash instead. A server seed still
in play arrives empty and is printed as **Hidden** rather than as a blank.

Verify sends the browser to `VerifyUrl` with both seeds base64'd into the query, the nonce, and the house edge
the roll was made under — byte for byte the string the web front builds, so a bet checked from the game and a
bet checked from the site are checked the same way.

### Pictures

The currency icon, the game thumbnail and the avatar are urls, not assets. Each is drawn as a plate with the
first letter on it and the picture laid over the top when it arrives, so a window with no network still reads
as a window. `UiRemoteImage` fetches them once per session and hands the same sprite to everything that asks:
it hangs the download on `UnityWebRequest`'s own completion rather than on a coroutine, so closing the dialog
does not abandon the fetch, and the picture is already there next time it opens.

With no socket at all — a scene with no `StateManager` — the window shows a sample bet, the same way the
statistics window shows sample figures. A `StateManager` that is running but has not been sent a transaction
shows the loader; a real game never sees invented numbers.

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

- **The close cross, the reset arrow, the tick and the clock are drawn, not fetched.** Rotated boxes, a ring
  with a notch in it, and two bars between three points, which costs no atlas entry and stays sharp at any
  size. The reset notch is *painted in the button's own colour*, not cut, so it only disappears against a flat
  button — give the style a `ResetIcon` sprite if the button is ever a gradient. `TickIcon` and `ClockIcon` do
  the same for the bet info dialog.
- **The bet info dialog is laid out by `UiGrid`, not by arithmetic.** Two columns, one auto row per block, and
  a nested grid per field. That is what lets a bet id wrap to three lines and a game's own outcome view be any
  height without anything measuring them: widths are settled before heights are read, so every label reports
  the height it needs at the width the grid just gave it, and the window is then sized from the grid's own
  answer. The cost is that **an auto row is only as tall as its items claim to be** — a plain panel claims
  nothing. Anything square in a cell (a coin, a thumbnail, a button) is centred and carries a `LayoutElement`
  for exactly that reason.
- **Which blocks are showing is said as a layout, not with `SetActive`.** A grid takes its layout as the whole
  truth about which children are showing, and re-asserts it every time it is enabled and every time its child
  list changes — and a grid with *no* layout takes **every** child as showing and re-asserts that. So a cell
  hidden behind the grid's back comes back the next time the window opens: the loader over the profit banner,
  the seeds under a card that was never sized for them. Every cell here has a one-word area name and every pass
  draws the picture that names the ones it wants.
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
| `UiWindowParts.cs` | Making and finding children, and naming them for a grid. Internal. |
| `EWindowTransition.cs`, `EWindowScroll.cs` | |
| `StatisticsWindow.cs` | The statistics panel. |
| `StatisticsWindowStyle.cs`, `StatisticsRow.cs` | What it looks like, and what it shows. |
| `EStatsTab.cs`, `EStatField.cs`, `EStatTint.cs` | |
| `BetInfoWindow.cs` | The bet info dialog. |
| `BetInfoWindowStyle.cs` | What it looks like. |
| `UiRemoteImage.cs` | Sprites from urls, cached for the session. Internal. |
| `UiWindowExample.cs` | Four windows built from code, including a game's own outcome block. Drop it on an empty RectTransform in a canvas. |
| `Editor/Window/UiWindowMenu.cs` | The three GameObject → UI entries. |
| `../RoundedBox/` | Every panel here is one. |
| `../Grid/` | What lays the bet info card out. |
