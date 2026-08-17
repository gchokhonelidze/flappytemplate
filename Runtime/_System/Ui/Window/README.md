# Window

[← All documentation](../../../../)

A dialog that is one component rather than a prefab: a rounded panel, a caption, a close button, a drag,
and an opening that is animated rather than a `SetActive`. Everything it is made of is built from code the
first time it is needed, so there is no hierarchy to keep in step and nothing to assemble by hand.

**The window owns where things go, not what they look like.** Panel, Caption, Title, Close, Scrollbar and
the sheet behind it are ordinary [`RoundedBox`](../RoundedBox/) and TextMeshPro objects — select one and
style it as you would any other. Nothing here paints over it afterwards.

Inside, the caption and the body are two rows of a [`UiGrid`](../Grid/), and the body **scrolls** once the
content is taller than the screen has room for — so a dialog is bounded by what it is drawn on rather than by
what its author guessed.

Five windows built on it live in folders of their own beside it, and are worked examples of filling one in:
`Statistics/` — the two-tab panel reading `MainState.Statistics`; `BetInfo/` — the dialog that asks the server
for one bet and lays out what came back; `GameHistory/` — the same for a whole shared round, everybody who bet
on it included; `Fairness/` — the seed pair the game is rolling from, and the two ways a player may change it;
and `Hotkeys/` — which keys are bound to what, drawn on a keyboard, and the switch that turns them on.

Every caption in all five, and the title of any window, is **translated** — see
[Translations](#translations) below.

*Describes package 1.0.81. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the windows drawn rather than described.*

**GameObject → UI (Canvas) → FlappyBet → Window**, and **Statistics Window**, **Bet Info Window**, **Game
History Window**, **Fairness Window** and **Hotkeys Window** beside it. Each makes a canvas if the scene has none, drops the window under whatever was
right-clicked, and builds the whole thing on the spot so it arrives looking like a window rather than as an
empty rect waiting for play mode. Everything else this template adds is in that same **FlappyBet** group —
Rounded Box, Grid, History and [Navbar](../Navbar/), which is the row of buttons that opens three of the
windows below.

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
| Title | The caption text. Also `Title` from code, live. What the label looks like is the label's own — select **Title** under the caption. |
| Show Caption | Off drops the whole header and starts the content at the top of the panel. The close button stays. |
| Show Close Button | |
| Start Closed | Hide at Awake and wait for `Open`. Off leaves the window exactly as the scene saved it. |
| Destroy On Close | For a window built for one message — it takes itself down with it. |
| Caption Height | The whole header block: the drag strip, the close button and the title. Content starts under it. |
| Content Padding Left / Top / Right / Bottom | Inset of the content area. Top is measured from the bottom of the caption, not the top of the panel. Four floats rather than a `RectOffset`: that type is a handle onto a native object and cannot be built in a field initialiser. |
| Fit Content Height | Ask the content how tall it wants to be on every open, and be that tall. Needs something under `Content` that reports a height — see below. |
| Max Height | The tallest the window may be, in its own units. Zero means the parent it is drawn in — the screen, for a full-screen canvas — less Screen Margin. |
| Screen Margin | Room left above and below a window that has grown as far as it may. |
| Scroll | `Never`, `WhenTooTall` (the default) or `Always`. |
| Show Scrollbar | The bar down the right of the body. Off leaves the wheel and the drag, and no hint that there is more below. |
| Scrollbar Width / Inset | Inset is measured from the right edge of the body inwards; the content is moved over by the width and the inset together, so the two never overlap. |
| Scroll Sensitivity | Canvas units per notch of the wheel. |
| Scroll Inertia / Deceleration | Whether a flick carries on after the finger has left, and how quickly it runs out. On by default — a touch screen expects it, and WebGL on a phone is a touch screen. |
| Draggable / Drag Anywhere | By the caption, or by any part of the window. Anywhere catches drags on everything inside it that does not handle its own. |
| Clamp To Parent | Keep the window inside its parent, per axis — see below. |
| Keep Visible | How much of a window too big for its parent has to stay inside it. |
| Bring To Front | Put the window in front of every other open one when it is opened, grabbed, or clicked anywhere — see [Drawing over the game](#drawing-over-the-game). |
| Show Backdrop | A sheet across the parent behind the window, which is what makes it modal — it swallows every click that misses. |
| Close On Backdrop Click | |
| Always On Top | On by default. Gives the window a canvas of its own with Override Sorting, which is what lifts it clear of the game — see below. |
| Sorting Order / Layer | The floor the open windows are stacked up from, rather than the number any one of them ends up at. The backdrop takes one less than its own window. Leave the layer empty for the parent canvas's own. |
| Transition | `None`, `Fade`, `Scale`, `ScaleFade`, `SlideUp/Down/Left/Right`. Slides are named for the way the window travels as it opens. |
| Open / Close Duration, Easing | |
| Open Scale | What a window grows from, and shrinks back to. |
| Unscaled Time | On by default, so a window still opens over a paused game. |

`OnOpened`, `OnClosed` and `OnCloseClicked` are UnityEvents. **`OnClosed` fires when the animation has
finished**, not when `Close` was called — a window is still on screen for the length of its close.

## Styling

There is no style object. **Select the part and set it**, the way you would any other box or label:

| Part | Where | What it is |
| --- | --- | --- |
| Panel | the window object itself | `RoundedBox` — fill, gradient, border, corners, edge softness |
| Caption | child | `RoundedBox`. Its fill is usually a wash *over* the panel's, so alpha at nothing leaves the header the colour of the body |
| Title | under Caption | `TextMeshProUGUI` — font, size, colour, style, alignment, and where in the caption it sits |
| Close | child, ignored by the grid | `RoundedBox` — move it, resize it, round it. `Close/Cross/Bar A` and `Bar B` are the two rotated boxes the cross is drawn from; drop an `Image` with a sprite in the button and turn the cross off instead if you would rather |
| Scrollbar | under Viewport | `RoundedBox` for the track, `Handle` under it. The window sets the **width** and where it sits; both colours are its own |
| Window Backdrop | beside the window, in its parent | `Image`. Only its colour — the window fades it through a `CanvasGroup`, so opening and closing never touch it |

From code the same parts are properties: `Panel`, `Caption`, `TitleText`, `CloseBox`, `ScrollTrack`,
`ScrollHandle`, `Backdrop`, and `CloseButton` for the click.

```csharp
window.Panel.FillColor = new Color(.13f, .12f, .24f);
window.Panel.SetCornerRadius(20f);
window.TitleText.color = Color.white;
```

Two things the window still writes, because they are about clicks rather than looks: the panel and the
caption **catch** raycasts and the title does not — a panel that let a click through would pass it to the
backdrop and close the dialog the player was aiming at, and a label that caught one would swallow the drag
that starts on it.

`ApplyLayout()` puts every part back where it belongs — the caption's row, the content's inset, the bar down
the side — and is safe to call on a window somebody has styled. It runs on `Awake`, on every `Open`, and on
any inspector change.

**A window arrives styled once.** Each part is given a plain dark look at the moment it is *made*, so a
window created from the menu is a dialog rather than a white square. That is `UiWindowSeed`, it happens once
per part, and it never runs over a part that already exists — a hand-styled window stays as it was, through
`Rebuild()` and through every reload.

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
    .Panel(box => box.FillColor = Color.black)
    .NoBackdrop();
```

| Step | Notes |
| --- | --- |
| `Create(parent, name)` / `For(window)` | Start. `Create` centres the new rect in its parent. |
| `Size(w, h)` · `At(pos)` · `Scale(uniform)` | |
| `Title(text)` | The caption text. |
| `Panel(box => …)` · `Caption(box => …)` · `Title(label => …)` · `Close(box => …)` | Hand back the part itself to style. A callback rather than a step per colour: what a box can be told is `RoundedBox`'s business, and a chain mirroring all of it would be a second copy of that inspector. |
| `Caption(height)` · `NoCaption()` · `Padding(l, t, r, b)` | |
| `CloseButton(bool)` | |
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

The inside of a window is a `UiGrid` of one column and two rows — the caption at **Caption Height**, the body
taking the rest, both inset by the panel's own border, which is read off the `RoundedBox` rather than held
anywhere:

```
Window            RoundedBox · UiGrid · CanvasGroup
├ Caption         cell "caption"        row 0, Fixed(Caption Height)
│  └ Title
├ Viewport        cell "body"           row 1, Flexible · ScrollRect
│  ├ Clip         the viewport less Content Padding · RectMask2D
│  │  └ Content   ← whatever the window is for
│  └ Scrollbar    Track → Sliding Area → Handle
└ Close           an overlay: ignored by the grid entirely
```

**`Clip` is where the padding lives**, and that is not a detail: a `ScrollRect` holds its content to the rect
it was handed as a viewport, so padding written onto the content as an offset is padding the scroller takes
straight back off — the first drag pulls the top margin away and the end of the travel leaves the last row
flat against the bottom of the panel. Padding the rect the content moves *inside* keeps the margin at rest,
mid-scroll and at the end alike. `Clip` is what the scroller is given and what the mask cuts at, so content on
its way past is gone before it reaches the margin.

A window saved before either of these **moves its own `Content` in on the next build**, children and all, so
nothing is lost and nothing needs doing by hand. The caption is shown and hidden **by the grid's layout**
rather than by `SetActive`, because a grid
takes its layout as the whole truth about which of its children are showing and re-asserts it whenever it is
enabled. The close button sits outside all of that: it carries an ignored `LayoutElement`, so the grid neither
places it nor has an opinion about whether it is visible.

### Fitting

| Call | Does |
| --- | --- |
| `Fit()` | Measures the content, makes the window that tall, clamps, and scrolls the rest. |
| `FitTo(height)` | The same from a height the caller worked out itself. |
| `ScrollToTop()` | Back to the top of the body. |
| `IsScrolling` · `Scroller` · `Viewport` · `ClipArea` · `Grid` | What it ended up doing, and the parts that do it. |

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

While scrolling, `Content` is as tall as it asked to be and anchored to the top of `Clip`, which is the shape
a `ScrollRect` moves; the mask clips at `Clip`, and `Clip`'s right inset grows by the bar's width so the two
never overlap. While not, `Content` is exactly `Clip` — same rect, no travel — and both the mask and the
scroller are switched off, since nothing is moving and nothing needs clipping. The **Content Padding** is on
`Clip` in both cases, which is why the gap under the last row is the same whether the body scrolls or not.

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

**GameObject → UI (Canvas) → FlappyBet → Statistics Window**, or Add Component → UI → Statistics Window on a
`UiWindow`, or the whole thing in one line:

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
| Style | Tabs, rows and reset button. The panel around them is styled on its own parts. |
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

**GameObject → UI (Canvas) → FlappyBet → Bet Info Window**, or Add Component → UI → Bet Info Window on a
`UiWindow`, or the whole thing in one line:

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
| Style | The card and its fields. The panel around them is styled on its own parts. |
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

## Game history

**GameObject → UI (Canvas) → FlappyBet → Game History Window**, or Add Component → UI → Game History Window on
a `UiWindow`, or the whole thing in one line:

```csharp
var rounds = GameHistoryWindow.Create(canvas);
rounds.BetInfo = betInfoWindow;      // so a row can be pressed for the bet behind it
rounds.Show(roundId);
```

One round of a **shared** game: what the room got, everybody who bet on it, what the round came to, and the
seeds it was rolled from. It is the shared-game half of the bet info dialog, and the two work together — a
round lists its transactions, and pressing one opens the bet info window on that bet.

`Show(id)` emits `GAME_HISTORY_INFO` through `Emitter` and opens the window on a row of pulsing dots; the answer
arrives as `ON_GAME_HISTORY_ID`, which the socket puts in `MainState.GameHistoryById` and announces as
`OnGameHistoryById`, and the window fills itself in from that.

Most games never call `Show` themselves: a **history strip** (`../History/`) on the shared feed opens this
dialog on whichever round was clicked, and hands over the round it already has so the outcome and the totals are
drawn while the transactions are still on their way.

| Call | Does |
| --- | --- |
| `Show(id)` | Asks for that round and opens on the loader. Uses `MainState.GameHistoryById` at once if it already holds this id. |
| `Show(summary)` | The same, given the `GameHistoryDto` from the history strip — the totals and the outcome show immediately. |
| `Show(round)` | Opens on a `GameHistoryByIdDto` the game already has. Nothing is asked. |
| `Show()` | Opens on whatever the state last received. |
| `Request(id)` | Asks without opening anything. |
| `Clear()` | Back to the loader, for a window about to be pointed at another round. |
| `Pick(betId)` | What a row press does: raises `OnPicked`, then opens the bet info dialog. |
| `ToggleDetails()` · `DetailsOpen` | The seeds half. |
| `ToggleCurrency()` · `UsdView` | Every amount in its own currency, or the lot converted to dollars. |
| `Verify()` | Opens the verifier in a browser — the same thing the button does. |
| `Refresh()` · `Layout()` · `Rebuild()` | Fill in again, lay out again, build again. |

| Field | What it does |
| --- | --- |
| Style | The list, the rows and the blocks around them. The panel is styled on its own parts. |
| Labels | Every caption on the dialog, `Total bet count` to `Server SHA-512`, plus the two glyphs on the currency toggle. Nothing here is translated. |
| Show Outcome | The strip across the top the game draws the round into. Laid out only once something is parented into `Outcome`. |
| Show Currency Toggle | The button that swaps every amount between its own currency and dollars. |
| Show Transactions | The list. |
| Show Totals | The three figures under it: how many bets there were, what they came to, and what the round paid. |
| Show Details / Show Verify | The two buttons, and the seeds Details opens. Verify appears only on a round the server sent a `VerifyUrl` with. |
| Usd View | Start converted to dollars rather than in each bet's own currency. |
| Follow State · Request On Show · Match Requested Id | As the bet info window, and for the same reasons. |
| Mine First | Put the player's own row at the top, ahead of the sort. |
| Load Images | Fetch the currency and avatar pictures the server sends as urls. |
| Fit Window Height · Collapse Details On Close | As the bet info window. |
| Bet Info | The dialog a row press opens. Nothing is searched for — the same decision the history strip makes. |

`OnRound`, `OnRequested`, `OnPicked`, `OnVerify`, `OnDetailsToggled` and `OnCurrencyToggled` are UnityEvents.

### The round itself

What a round *was* is the game's own: a multiplier, a colour, a card, where the ball fell. So the window stops
at what every shared round carries and **`Outcome` is a strip across the top that the game fills in** — the same
container the bet info window has, with the same rules about height:

```csharp
rounds.OnRound.AddListener(data =>
{
    var view = Instantiate(rollPrefab, rounds.Outcome, false);
    view.Draw(data.Outcome);
});
```

### The list

One row per transaction: the mark that says it can be pressed, the player, and what they staked over what came
back. **The whole row is the button** rather than a target the size of a fingernail, and pressing it opens the
bet info dialog on that bet — a round's transaction is a summary, so the dialog asks the server for the rest.

A row is **green from a payout of one** and red below it — the bet came back whole or better — and the player's
own row is outlined so it can be found in a list of forty. It is the first row too, unless **Mine First** is off;
the rest are ordered by what they came away with, in dollars, which is the order the web front puts them in.

The list is the one block given a height rather than measured: past **List Max Height** it **scrolls inside
itself**, which is what keeps the totals and the buttons on screen under it while the window as a whole stays
the size it was. Rows are pooled — a round of forty bets and one of two are the same list with a different
layout on it.

### Amounts

A shared round is bet on in as many currencies as there are players, so the amounts are shown in each bet's own
currency and the **toggle beside the outcome converts the lot to dollars** at the rate the server sent with each
transaction. The three totals underneath are always dollars: nothing else adds up.

Money is truncated and trimmed the way the bet info window does it, so the same bet reads the same in both. The
totals come off the round where the server filled them in and off the history strip's summary where it did not —
which is exactly what the web front does.

### Details and verify

Details opens the seeds: the salt the round was rolled from, the seed the server kept, and the hash it published
before the roll. A server seed still in play arrives empty and is printed as **Hidden**. Verify sends the browser
to `VerifyUrl` with both seeds base64'd into the query and the house edge — **no nonce**, and that is the
difference from a single-player bet: a shared round is one roll for the whole room, so there is nothing to count.

With no socket at all — a scene with no `StateManager` — the window shows a sample round with three players in
it, the same way the bet info window shows a sample bet. A `StateManager` that is running but has not been sent
a round shows the loader.

## Fairness

**GameObject → UI (Canvas) → FlappyBet → Fairness Window**, or Add Component → UI → Fairness Window on a
`UiWindow`, or the whole thing in one line:

```csharp
var fairness = FairnessWindow.Create(canvas);
fairness.Show();
```

The seed pair the game is rolling from, the pair before it, and the two ways a player may change them.
Everything comes from `MainState.Seeds`, which the server fills in and refreshes as `ON_SEED`; the window
redraws when `OnSeed` fires and sits still the rest of the time.

**Opening the window emits `SEED_INFO`**, and the answer arrives as an ordinary `ON_SEED`. That is not
belt and braces: a pair arrives with the session, but the **nonce moves with every bet**, so by the time a
player opens this the pair on hand is several bets out of date. The request is made from `OnEnable` — a
`UiWindow` switches its object on to open and off again to close, so being enabled *is* being opened, and a
window opened straight through `Window.Open()` asks just the same as one opened through `Show()`. It does not
lock the controls or raise the loader: what is on screen is already a pair the server sent, and there is no
reason to stop reading it while a newer one is on its way.

**The two buttons are not the same button twice.** That is the whole point of the dialog:

| Button | Emits | Does |
| --- | --- | --- |
| **Randomize** | `RANDOMIZE` | Asks for a *whole new pair* — a new server seed as well as a new client seed. The one that makes the next rolls unknowable to everyone, the house included. |
| The small one beside the box | `RANDOMIZE_CLIENTSALT_ONLY` | Keeps the server's seed and replaces only the half the player owns, with whatever was typed. |

Randomize deliberately does **not** send what is in the box — the server picks both halves, which is what makes
that button worth pressing. Type a seed and press the small one to choose your own half. Both come back as an
ordinary `ON_SEED` broadcast, so the window is never showing a pair the server has not confirmed.

| Call | Does |
| --- | --- |
| `Show()` | Opens on whatever pair the state holds, and asks for a fresh one. |
| `Request()` | Emits `SEED_INFO` without opening anything. For a game that keeps the dialog alive rather than switching it off. |
| `Randomize()` | The Randomize button, from code. Refused while the controls are locked. |
| `RenewClientSeed()` | The renew button, from code. Sends `ClientSeed`. |
| `ClientSeed` | What is in the box, read or written. Writing it sends nothing. |
| `Refresh()` · `Layout()` · `Rebuild()` | Fill in again, lay out again, build again. |
| `IsLocked` · `IsPending` | Whether the controls are locked, and whether a request is out and unanswered. |
| `Input` · `RandomizeButton` · `RenewButton` | The parts, for a game that would rather drive them itself. |

| Field | What it does |
| --- | --- |
| Style | The box, the buttons and the rows. The panel around them is styled on its own parts. |
| Labels | Every caption on the dialog, `New client seed` to `Bets made with pair`. Nothing here is translated. |
| Show Client Seed Box / Randomize | The two controls. Both are dropped on a shared or multiplayer game whatever these say — see below. |
| Show Current / Previous Pair | The two sections. The previous one is dropped anyway until there has been one — see below. |
| Client Seed Length | Longest client seed the box will take. Sixteen, which is what the web front allows. |
| Follow State | Redraw when the server sends a new pair, and when the round starts or ends. |
| Request On Open | Emit `SEED_INFO` when the window opens. |
| Lock While Running | Lock the controls while a round is in play. On, and it should stay on — see below. |
| Fit Window Height | Resize the window to exactly the blocks it is showing. |

`OnSeeds`, `OnRandomizeRequested` and `OnClientSeedRequested` are UnityEvents.

### Locking

The controls lock themselves while **a round is in play**, while **there is no pair yet**, and while **a
request is out and unanswered**. The first is the server's rule rather than this window's: a seed pair changed
under a bet would make that bet uncheckable, so the request would be refused anyway — and a button that sends
something to be refused is worse than one that says it cannot be pressed. The third is what stops a second
press going out against a pair that is already being replaced.

A locked control is **painted and made uninteractable, not taken out of the arrangement**: the box still shows
the current client seed, which is a thing worth reading. That is the one part of this dialog that is not said
as a layout.

Running is read from `MainState.SystemState`, so a `StateManager` that has not been sent a system state yet
counts as running — there is no word either way, and that is the same thing as far as sending a seed change
goes.

### Shared and multiplayer games

A single-player roll is seeded from a client salt and counted by a nonce. A `SHARED` or `MULTI` round is seeded
from a **block hash** nobody can have known in advance, where a nonce per player would mean nothing and nothing
a player typed would reach the roll. So on those the box and the Randomize button are dropped, the nonce and
the bets-made rows go with them, and the two seed rows are captioned as the block hash instead. `GameType`
arrives on the system state, and a change to it redraws the dialog.

### The previous pair

The first pair of a session has nothing before it, and the server says so by sending `PrevClientSalt` and
`PrevServerSeed` as `null`. Three rows of *N/A* and a bet count of zero is not information, so **the whole
previous section goes until there is a pair to put in it** — heading included. A half-filled one, where the
server sent a client seed but is still holding the server seed, keeps its rows and prints `N/A` in the gap.

### The box

The box follows the pair, and only the pair: a broadcast that left the client seed alone does not throw away
what the player was half way through typing. It is refilled when the pair's client seed is a different string
from the one it was last filled from — which is what makes a randomize show up in it, and an unrelated
broadcast not.

With no socket at all — a scene with no `StateManager` — the window shows a sample pair and **both buttons roll
that sample over locally**, so the dialog can be pressed and read in the editor. A real game never sees this:
the moment there is an `Emitter`, the buttons only ever send.

Like the bet info dialog, the window is fitted **twice over, and again on the next two frames** — a SHA-512
wraps to three lines, and a label only reports the height it needs at the width the first pass gave it.

## Hotkeys

**GameObject → UI (Canvas) → FlappyBet → Hotkeys Window**, or Add Component → UI → Hotkeys Window on a
`UiWindow`, or the whole thing in one line:

```csharp
var keys = HotkeysWindow.Create(canvas);
keys.Window.Open();
```

Which keys are bound to what, and the switch that turns them on. **Nothing is filled in here** — the window
reads the [hotkey registry](../Hotkeys/), so a key bound anywhere in the game is in its list the moment it is
bound and gone the moment it is dropped. There is no list to keep in step, and no way for the dialog and the
game to disagree about what `D` does.

Three blocks, top to bottom:

- **The drawn keyboard.** Every cap in `HotkeyCaps.Rows`, with the bound ones in the accent colour and
  whichever is held down lit. It is what turns a list of key names into something a player reads at a
  glance — *the gold ones do something, and the one under my finger is the one I pressed*. Built once and
  repainted rather than rebuilt: sixty caps is sixty boxes and sixty labels, and a key going down happens in
  the middle of a round. It catches no clicks — it is a picture of a keyboard, not a keyboard.
- **The list.** A caption and a key cap per binding, in the order the game bound them. It scrolls on its own
  past `List Max Height`, which is what keeps the button below it on screen. A binding whose `Enabled` is off
  stays in the list, greyed — a player who has learned `B` should see the key still exists and is simply not
  doing anything yet.
- **The switch.** Pressing it emits `SETTING`, so the choice follows the player to their next session and to
  the web front. Its caption says **which way things stand** rather than what pressing it would do —
  `Hotkeys off` while they are off — which is how the web front's own button reads, so a player who has used
  one recognises the other.

**Hotkeys are off until the player presses that button.** The server defaults the `keyboard` setting to `0`,
so a game that binds keys and never puts this dialog anywhere has bound keys that never fire. That is the one
thing to know before wiring the feature up.

Pressing a bound key while the window is open lights its cap in both the picture and the list. That is the
other half of what the dialog is for: a player who cannot tell whether the game heard them can hold a key and
watch.

| Call | Does |
| --- | --- |
| `Toggle()` | Switches hotkeys on or off, and emits `SETTING`. What the footer button does. |
| `Refresh()` | Reads the bindings again. Called for you by everything the window watches. |
| `Tint()` | Recolours the caps and nothing else. What a key press calls. |
| `Layout()` · `Rebuild()` | Lay out again, build again. |
| `ToggleButton` · `Scroller` | The parts, for a game that would rather drive them itself. |

| Field | What it does |
| --- | --- |
| Style | The keyboard, the list and the button. The panel around them is styled on its own parts. |
| Labels | The footer's two captions and the empty-list line. Nothing here is translated. |
| Show Keyboard / List / Toggle | The three blocks. Turning the toggle off leaves the player no way to enable hotkeys, so only do it if the game offers that somewhere else. |
| Follow Bindings | Rebuild when a key is bound or dropped, and repaint when one is pressed. |
| Fit Window Height | Resize the window to exactly the blocks it is showing. |

`OnToggled` is a UnityEvent carrying what hotkeys have just been switched to.

With no socket at all — a scene with no `StateManager` — the window shows **four sample bindings** so it can be
laid out and styled from a menu rather than from a running game. They are rows of text bound to nothing. A real
game never sees them: the moment anything is bound, only the real list is shown.

## Translations

The windows follow `MainState.Locale` on their own. Nothing has to be set up for it and no field changes
meaning: every caption is written through [`Translator.Label`](../../Translations/), which

1. translates the string if it is a **key** — `bet_info.payout`;
2. else translates it if it matches a **known en_US label** — `"Payout"`, which is what a scene saved;
3. else prints it **exactly as typed** — so a wording of your own survives.

That is why an existing scene translates without being opened. The label fields under **Labels** in each
inspector, the window `Title`, and a `StatisticsRow`'s title all go through it.

| To… | Do |
|---|---|
| Change the language | Set `MainState.Locale`, or `Translator.Locale`. Open windows repaint on the next frame |
| Reword a caption everywhere | `Translator.Add(locale, "bet_info.payout", "…")`, once per language |
| Reword one window only | Type it into that window's inspector field — an unknown string is left alone |

Only captions are translated. Every value beside one — an amount, a seed, a hash, a bet id, a player name — is
printed as the server sent it. The two exceptions are the ones that are words rather than data: **N/A** in the
fairness rows and **Hidden** where a server seed has not been revealed yet.

> A window is switched off while it is closed, so a closed one is not listening. It repaints on the way back
> in, which is why a language changed behind a closed dialog is already applied when it opens.

## Drawing over the game

A window that comes out behind the game is not a hierarchy problem, and moving it down the hierarchy will
not fix it. **Hierarchy position only settles the window against its own siblings.** Everything else on screen —
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

### Windows in front of each other

Always On Top settles a window against the game and leaves a second question open: with two of them on screen,
which is in front? Every window at Sorting Order 100 is a tie, and a tie is broken by nothing anybody chose.

So the open windows are kept as **one pile, newest on top**, shared by the whole game:

| It happens when | Because |
| --- | --- |
| A window opens | Newest on top. A dialog that arrives behind the one it was opened from has, from where the player is sitting, not opened. |
| A window that is already open is opened again | Asking for a window is nearly always asking to look at it. |
| The window is clicked or touched anywhere | Grabbing the caption, pressing a button inside it, scrolling the body. |

`Raise()` does it from code, for a front that something else decides.

The pile hands out the sorting orders: **two apart, counted up from the highest Sorting Order among the windows
that are open**, so each window has the odd number below it for its own backdrop and every raise lands inside a
known band rather than climbing forever. A window deliberately set to 200 lifts the whole pile with it instead
of being buried by it. Closing one takes it out and closes the gap.

Sibling order is moved with it, so a window with Always On Top *off* is stacked by the same rule — that is the
only thing hierarchy position can be sorted by. Turning **Bring To Front** off leaves a window where it is when
it is touched; it still joins the pile as it opens, because it has to be somewhere.

> **Clicking a button inside a window counts as clicking the window.** It has to be looked for rather than
> waited for: a pointer press is delivered to the first handler found walking up from whatever was hit and
> stops there, so a press on a `Button`, a `Toggle` or an input field never reaches the window around it. Those
> are all `Selectable`s, and pressing one moves the event system's selection — so the pile watches the
> selection for a change into a window it has not already raised. One reference compare per frame while a
> window is open, and nothing at all while none is.

## Worth knowing

- **The close cross, the reset arrow, the tick, the clock, the padlock and the circular arrows are drawn, not
  fetched.** Rotated boxes, a ring with a notch in it, and two bars between three points, which costs no atlas
  entry and stays sharp at any size. The reset notch is *painted in the button's own colour*, not cut, so it
  only disappears against a flat button — give the style a `ResetIcon` sprite if the button is ever a gradient,
  and note that the fairness arrows follow their button being dimmed for exactly that reason. `TickIcon`,
  `ClockIcon`, `LockIcon` and `ArrowIcon` take a sprite instead. The padlock is the same trick once more: a
  ring with the body drawn over its lower half, which is all an arch needs.
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
- **An animation cut short puts the transform back.** Something that switches a window off part-way through
  an opening or a closing — a parent panel hidden, a `SetActive` by hand, a scene unloading — kills the tween,
  and the rect is returned to its resting scale and its alpha as it goes. That is not tidiness: the resting
  scale is read from the rect, so a window left at nine tenths of the way through an opening would have taken
  *that* for its resting scale next time, and a dialog that loses a tenth of itself per interrupted animation
  ends up too small to see — which looks, from the outside, like a window that has stopped opening. For the
  same reason `Open` on a window it considers already open **re-asserts** the scale and the alpha rather than
  returning early: whatever left one invisible, the press that asks for it again puts it right.
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
  component's context menu is the way out of a hierarchy that has been edited into a state the layout can no
  longer describe — and it keeps how every part that is still there looks.
- **Anything else parented to the window root is a cell of its grid.** The window is a `UiGrid` whose
  layout names `caption` and `body` and nothing else, so a child it has no name for is flowed into a cell of
  its own — below the body, not over the panel — and switched off the next time the grid is enabled. An
  overlay says otherwise with a `LayoutElement` set to **Ignore Layout**, which is what the close button
  wears; a border overlay or border particles from the Rounded Box menu now add one for themselves. Drop it
  under `Content` instead if it belongs to the content rather than to the frame.
- Every tween is built with `DOTween.To` rather than the `DOFade`/`DOAnchorPos` shortcuts. Those live in
  DOTween's UI module, which is compiled into the project's own assembly and cannot be reached from a
  package. `Toast` does the same thing for the same reason.

## Files

Each window built on `UiWindow` keeps its own files in its own folder; what is loose in this one is the window
itself.

| File | |
| --- | --- |
| `UiWindow.cs` | The window: parts, layout, drag, transition. |
| `UiWindowBuilder.cs` | The fluent API. |
| `UiWindowSeed.cs` | The look each part is given when it is made, and never again. Internal. |
| `UiWindowDragHandle.cs` | The grab. Usable on its own, for a custom header. |
| `UiWindowParts.cs` | Making and finding children, and naming them for a grid. Internal. |
| `EWindowTransition.cs`, `EWindowScroll.cs` | |
| `UiWindowExample.cs` | Six windows built from code, including a game's own outcome block. Drop it on an empty RectTransform in a canvas. |
| `Statistics/StatisticsWindow.cs` | The statistics panel. |
| `Statistics/StatisticsWindowStyle.cs`, `Statistics/StatisticsRow.cs` | What it looks like, and what it shows. |
| `Statistics/EStatsTab.cs`, `Statistics/EStatField.cs`, `Statistics/EStatTint.cs` | |
| `BetInfo/BetInfoWindow.cs` | The bet info dialog. |
| `BetInfo/BetInfoWindowStyle.cs` | What it looks like. |
| `BetInfo/UiRemoteImage.cs` | Sprites from urls, cached for the session. Internal. Used by the game history window too. |
| `GameHistory/GameHistoryWindow.cs` | The game history dialog: one shared round and everybody who bet on it. |
| `GameHistory/GameHistoryWindowStyle.cs` | What it looks like. |
| `Fairness/FairnessWindow.cs` | The fairness dialog. |
| `Fairness/FairnessWindowStyle.cs` | What it looks like. |
| `Hotkeys/HotkeysWindow.cs` | The hotkeys dialog: the drawn keyboard, the list, and the switch. |
| `Hotkeys/HotkeysWindowStyle.cs` | What it looks like. |
| `Hotkeys/HotkeyKeyboard.cs` | The drawn keyboard inside it. Internal. |
| [`../Hotkeys/`](../Hotkeys/) | The registry the dialog reads, and the two components that bind a key. |
| `Editor/Window/UiWindowMenu.cs` | The six GameObject → UI (Canvas) → FlappyBet entries. |
| [`../../Translations/`](../../Translations/) | `Translator.Label`, which every caption above is written through. |
| `Editor/FlappyBetMenu.cs` | The one group they all go in, path and priority. Internal. |
| `../RoundedBox/` | Every panel here is one. |
| `../Grid/` | What lays the bet info card and the game history list out. |
| `../History/` | The strip that opens the bet info and game history dialogs. |
| `../Navbar/` | The bar whose Statistics, Fairness and Hotkeys buttons open three of these. |
