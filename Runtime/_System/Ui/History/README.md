# Ui History

[← All documentation](../../../../README.md)

The strip of recent bets: the last few rounds as a row of chips over the game, or a column of them down the
side. It feeds itself from the socket, animates each arrival, drops the oldest when it runs out of room — or
scrolls instead — and opens the **bet info** dialog on whichever chip is clicked.

On a **shared** game it shows the rounds the room played instead of the player's own bets, and a chip opens the
**game history** dialog. Which of the two it is comes from the server; a round is drawn through the same element
as a bet, so nothing about your chip has to know. See [Which history](#which-history).

**It draws nothing of its own.** A bet on screen is your element prefab: what it is made of, how big it is and
what it says about the round are all decided there, and the strip hands it the whole `HistoryDto` and stays out
of it. So there is no chip in here to colour, no size to set and no value to format — and no padding either, the
elements sit in the middle of the strip.

What is left is the strip: which way it runs, which end the newest bet lands on, what happens when it fills up,
and the gap between one element and the next.

*Describes package 1.0.80. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the strips drawn rather than described.*

**GameObject → UI (Canvas) → FlappyBet → History**, or Add Component → UI → Ui History.

---

## Quick start

1. **GameObject ▸ UI (Canvas) ▸ FlappyBet ▸ History** — a 620×64 strip, holding sample bets.
2. Drop your element prefab into **Element Prefab**. Until you do, the strip has nothing to draw a bet with and
   says so once in the console — see [Your own element](#your-own-element) for the cheapest one that works.
3. Anchor the strip where it belongs. Stretch it if you want it to grow with the screen; the count of chips
   follows the width.
4. Drag your bet info window into **Bet Info** — and, on a shared game, your game history window into **Game
   History**. Nothing is searched for — see below.
5. Press play. The samples are replaced by `MainState.History`, and every `ON_HISTORY` after that arrives with
   a pop.
6. Click a chip. The dialog opens on that bet.

The only line of code in that is the one inside your element:

```csharp
public override void Write(HistoryDto data) => label.text = Outcome("multiplier") + "x";
```

---

## Your own element

**Element Prefab** is a prefab with a `UiHistoryElement` on its **root** — which is also the only thing the
strip needs from it. Everything else about the prefab is yours, and none of it is touched: no colour is
overwritten, no size is imposed, no text is written.

Derive from `UiHistoryElement` and override `Write`. That is the whole seam:

```csharp
public class DiceHistoryChip : UiHistoryElement
{
    [SerializeField] private Image face;
    [SerializeField] private TextMeshProUGUI amount;
    [SerializeField] private Color won, lost;

    public override void Write(HistoryDto data)
    {
        face.sprite = faces[int.Parse(Outcome("roll"))];
        amount.text = data.WinAmount;
        amount.color = decimal.Parse(data.WinAmount) > 0m ? won : lost;
    }

    public override void Appear() { … }         // arrive your own way
}
```

| Member | Is |
|---|---|
| `Write(data)` | Called with each bet the element is given. The base does nothing — there is no value the strip could work out that your game would not rather decide |
| `Data` | The bet the element is showing, or null while it is spare |
| `Outcome("key")` | A value out of that bet's own outcome payload. Both shapes are read — the flat `_Outcome` the socket fills in and the raw JSON it arrived as |
| `Appear()` | The arrival animation. Override for a prefab that arrives its own way |
| `Pick()` | Opens the bet info window on this bet, the same as a click. Public, so your own control can call it |
| `Plate` | The background, and the hit area. Found by itself: a child called `Plate`, else a graphic on the root, else the first one inside that is not text |

**The size of an element is the prefab's.** The strip measures it — its own `LayoutElement` if it has one, the
rect it was drawn at if not — and makes a track that size. Nothing in the inspector overrides that, and nothing
needs to: a chip that should be 40 wide is drawn 40 wide.

`UiHistoryExampleElement.cs` beside this file is a working one, in about ten lines.

---

## Shape and direction

| Field | Means |
|---|---|
| **Flow** | `Horizontal` — a row. `Vertical` — a column |
| **Order** | Which end the newest bet lands on: `NewestFirst` (left, or top) or `NewestLast` |
| **Align** | Where the elements sit while there are too few to fill the strip: `Start`, `Center` (the default), `End` |
| **Overflow** | `Clamp` — show as many as fit and drop the oldest. `Scroll` — keep them all and let it be dragged |
| **Capacity** | How many bets are *kept*, whatever is shown. Zero keeps everything. The socket itself keeps 15 |
| **Clip** | Cut anything reaching past the strip's rect. Always on while scrolling |

`Align: End` with `Order: NewestLast` is the crash-game look: bets arrive at the right edge and push the rest
leftwards. The default is `Center`, which is what a part-full strip over a game usually wants — and the reason
there is no padding to set.

### Clamping

How many fit is the strip's own width divided by the prefab's size along the flow, gaps taken out. An element
that measures as nothing on that axis leaves nothing to count with, so everything is shown and anything past
the edge is clipped rather than dropped.

The count follows the strip: an anchored strip resized by its parent, by the canvas scaler or by the window it
sits in re-counts the frame after. Nothing announces a rect change, so the strip watches its own size.

### Scrolling

`Overflow: Scroll` puts a `ScrollRect` and a `RectMask2D` on the strip and lets the content be as long as its
elements. Drag it, flick it — inertia is on, because WebGL on a phone is a touch screen — or roll the wheel
over it. **Follow Newest** slides it back to the newest bet as one arrives; **Scroll Sensitivity**,
**Scroll Inertia** and **Scroll Deceleration** on the style are the feel of it.

There is no scrollbar. A strip of a dozen chips with a bar under it reads as a list; the drag and the wheel
are the whole interaction, and `ScrollToNewest()` is there for the rest.

---

## Arrivals

A new element grows and fades in, from **Appear Scale** over **Appear Duration** on **Appear Ease**, on
unscaled time so a game that paused itself still animates its strip. Zero duration puts it there with no
animation; **Animate Arrivals** off does the same for every arrival. A prefab that wants to arrive its own way
overrides `Appear`.

Elements are kept with the bet they show rather than reassigned by position, so a strip that shifts along only
writes the one chip that is new — a prefab holding state of its own keeps it as it slides. The shift itself is
not animated: the elements are placed by a grid, and a tween fighting a layout is a tween that loses.

---

## Which history

Two feeds, and **Feed** picks between them:

| Feed | Reads | Listens for | A chip opens |
|---|---|---|---|
| **Auto** — the default | whichever of the two the server says the game is | | |
| **Player** | `MainState.History` | `ON_HISTORY` | the **bet info** window on that bet |
| **Shared** | `MainState.GameHistory` | `ON_GAME_HISTORY` | the **game history** window on that round |

`Auto` reads `SystemState.GameType`: `SHARED` is the shared feed, anything else the player's own bets. The game
type arrives with the session and routinely **after** the strip has already subscribed, so the strip keeps
asking and switches over on the frame the answer changes — off the old event, on to the new one, and filled in
again from the list that one writes to. Nothing has to be timed against it.

A round is not a bet, and the strip shows it as one anyway: `Id`, the outcome payload, and the two amounts —
`TotalBetAmountUsd` and `TotalWinAmountUsd`, in dollars, because a shared round is bet on in as many currencies
as there are players. So **an element written for the player's history draws a round without being changed**,
and `Outcome("multiplier")` reads the same key out of either.

What will not fit through that mapping is on the element beside `Data`:

```csharp
public override void Write(HistoryDto data)
{
    label.text = Outcome("multiplier") + "x";

    // null on the player's own history; the round it came from on a shared game
    count.text = Round != null ? Round.TotalBetCount + " bets" : string.Empty;
}
```

`UiHistory.RoundFor(id)` is the same thing for code holding an id rather than an element.

> A round carries **no timestamp**, so there is nothing for **Sort By Time** to sort by and it stands aside on
> the shared feed. The order is the one the server sent: `MainState.GameHistory` is kept newest first, the way
> the web front keeps it, and the strip turns it round on the way in.

---

## Clicking

A click raises `OnPicked(HistoryDto)` and then opens a dialog on what was clicked — `Show(id)`, so the dialog
asks the server for the whole thing rather than making do with the summary the history payload is.

Which dialog is the feed's business: **Bet Info** on the player's own bets, **Game History** on the shared feed.
A round is handed over whole rather than as an id, so the dialog has the outcome and the totals to draw while
the transactions are still on their way.

Either way it is the window in that field and only that one. **Nothing is searched for**, and that is a decision
rather than an omission — a scene holds as many bet info windows as somebody put in it, a game's own plus
whatever an example or a test built for itself, and a strip that guessed would open one of them off screen about
half the time. So it is a reference you set, once, and then it is certain.

Leave it empty and a click raises `OnPicked` and nothing else, which is exactly what a game that drives its own
dialog wants:

```csharp
strip.OnPicked.AddListener(data => myOwnDialog.Show(data.Id));
```

The strip says so in the console the first time a chip is clicked with no dialog assigned — once, not once a
click, because a strip wired to `OnPicked` alone is an ordinary thing to build. A bet with no id is logged too.
If a click seems to do nothing and there is no log at all, the dialog *is* being opened: check that its canvas
does not sort under something that covers it.

A `Button` is added to the element if the prefab has none, and its plate is made the hit area. A prefab that
brings its own `Button` keeps it — only the listener is added.

---

## Feeding it yourself

**Follow State** on — the default — seeds from `MainState.History` on enable and adds every `ON_HISTORY` after
that, or from `MainState.GameHistory` and `ON_GAME_HISTORY` on the shared feed. Off hands the feeding over:

| Call | Does |
|---|---|
| `Add(data)` | The newest bet. Animates, drops the oldest past `Capacity` |
| `Add(round)` | The same for a `GameHistoryDto` — mapped onto a bet, and kept for `Round` |
| `AddRange(values)` | One after another. Takes either |
| `Set(values)` | Replaces everything, oldest first. Animates nothing. Takes either |
| `Remove(id)`, `Clear()` | What they say |
| `Refresh()` | Writes every element again from the bet it holds — after something outside the bet has changed |

`Add` on a bet whose id is already in the strip replaces it where it is rather than adding a second chip
(**Dedupe**), which is what a payout arriving after its round should do.

### Which end is new

`ON_HISTORY` arrives as an *array*, and the server builds it **newest first** — while the bets that follow it
arrive one at a time, newest last. The socket appends both to `MainState.History`, so that list is not in any
one order and a strip that trusted it would show the seeded bets the opposite way round from the arrivals after
them.

**Sort By Time** — on by default — puts them in the order the server stamped them, so `Order` means the same
thing whichever feed a bet came from. Turn it off only on a feed with no `CreatedAt` to sort by, and then hand
the strip its bets in the order you want them.

---

## From code

```csharp
var strip = UiHistory.Create(canvas, "History", 620f, 64f);
strip.ElementPrefab = myChip;                 // not optional — there is nothing else to draw with
strip.Flow = EHistoryFlow.Vertical;
strip.Overflow = EHistoryOverflow.Scroll;
strip.Capacity = 0;
strip.OnPicked.AddListener(data => Debug.Log(data.Id));
```

| Member | Does |
|---|---|
| `Create(parent, name, width, height)` | Builds one, ready to be given an element and fed |
| `ElementPrefab` | The element. Setting it rebuilds — elements left over from the last prefab are thrown away |
| `Style` | The gap, the arrival, the scrolling. Change anything on it and call `ApplyStyle()` |
| `ApplyStyle()` | Writes the gap, the flow and the scroll settings, then lays out |
| `Layout()` | Binds, names the cells, sets the layout — now, rather than at the end of the frame |
| `Rebuild()` | From scratch. After swapping the prefab, or changing the style wholesale |
| `Items`, `Count` | The bets it holds, oldest first |
| `Elements` | What is on screen, in the order shown. Worth reading rather than keeping |
| `ElementFor(id)` | The element showing that bet, or null |
| `Sample(count)` | Fills it with made-up bets. What the editor preview uses |
| `ScrollToNewest(animated)` | Slides a scrolling strip to the newest end |
| `Read(data, key)` | A value out of a bet's outcome payload. Static, and useful outside the strip |
| `Grid`, `Scroller` | The parts, for anything the settings do not reach |
| `OnPicked`, `OnElement` | A chip was clicked; an element was filled in |

`OnElement` fires as each element is written, which is where a game decorates one from the inspector rather
than from a prefab script.

---

## The inspector

| Group | Fields |
|---|---|
| **Style** | Strip: gap. Arrival: duration, scale, fade, ease, unscaled, follow duration. Scrolling: sensitivity, inertia, deceleration |
| **Element** | Element Prefab |
| **Strip** | Flow, Order, Align, Overflow, Capacity, Clip, Follow Newest |
| **Behaviour** | Feed, Follow State, Dedupe, Sort By Time, Animate Arrivals, Bet Info, Game History, Preview, Sample Count |
| **Events** | On Picked, On Element |

That is the whole of it. Everything that used to sit under *Chip*, *Text* and *Value* — colours, border, radius,
font, size, decimals, format, padding, element size — is the prefab's now.

**Preview** fills a strip with sample bets when there is no socket running, so the component looks like a
history strip in the editor rather than like an empty rect. It needs an element prefab like anything else does.
Sample bets carry a `multiplier` in their outcome, since that is the key most games ask for; an element reading
some other key shows whatever it makes of an empty string until a real session replaces them. A strip set to
the shared feed is given sample **rounds** instead, totals and all, so `Round` reads in the editor too.

---

## Worth knowing

- **No prefab, nothing on screen.** The strip logs it once, keeps holding its bets, and fills itself in the
  moment an element is given to it. It is one warning rather than one a frame because a strip built from code
  is configured a line after it is created.

- **Elements from another prefab are thrown away, not reused.** Each one remembers which prefab it came from,
  so changing the field rebuilds the strip out of the new one rather than restyling chips that cannot be
  restyled.

- **A template kept switched off in the scene works as an element prefab.** `Instantiate` copies that, so the
  strip switches each copy back on as it makes it.

- **The elements sit in a `UiGrid`, one track each.** Every element the strip is not showing is a spare kept
  in the same grid under a name the layout does not mention. That is how it is hidden — a grid shows exactly
  what its layout names and re-asserts that every time it is enabled, so anything switched off behind its back
  comes straight back on. Naming is the only way to hide something in a grid that it will not argue with.

- **An empty strip still gets a layout** — one empty cell. A grid with *no* layout shows every child it has,
  which here would be every spare in the pool.

- **The cross axis is always one flexible track.** An element with a size across the flow keeps it and is
  centred in the strip; a fixed *track* would pin the row to the top edge and leave the rest of the strip empty
  under it.

- **Drags reach the scroller through the buttons.** A `Button` does not handle drag, so the event bubbles up to
  the `ScrollRect` above it — which is why a chip can be both clicked and dragged. The strip also carries an
  invisible `Image` behind the elements, enabled only while scrolling, so a drag that starts in a gap between
  chips still scrolls.

- **One arrangement per frame.** However many bets arrive in a frame, the strip is laid out once, at the end of
  it. `Layout()` is the immediate version, for a game that wants to read `Elements` in the same breath.

- **Text inside an element is taken off the raycast**, and its plate is put on it. A label is routinely laid out
  larger than the element it sits in — a 200-wide caption inside a 60-wide chip is an ordinary thing to build —
  and a raycast target that overhangs its neighbours means the pointer over one chip lands on the one beside it.
  The plate is the hit area, and the plate is exactly the size of the element. A label that overhangs still
  *draws* outside its chip, so stretch it to the element if the value can be long.

- **A clicked chip is deselected as the click lands.** UGUI leaves a clicked `Button` selected and a selected
  `Selectable` goes on drawing its highlighted colour, which on a strip reads as some other bet being hovered.
  Buttons the strip adds also get `Navigation.None`, so fifteen chips do not swallow the arrow keys.

- **In the editor, a setting change repaints.** Every element is written again on `ApplyStyle`, not only the
  ones whose bet changed, so a prefab that reads something outside the bet gets the chance to read it again.
  Nothing here is serialized, neither the parts nor the bets, so a script reload re-seeds the strip: in a scene
  with no socket that means the sample bets come back rather than the strip emptying itself.

- **Clipping cuts anything that reaches past the strip**, including a part of an element that was meant to hang
  outside it — a badge over a corner, a bar under the chip. Leave a little slack across the flow, or turn
  **Clip** off on a strip that is not scrolling.

- **Every tween is built with `DOTween.To`** rather than the `DOScale`/`DOFade` shortcuts. Those live in
  DOTween's UI module, which is compiled into the project's own assembly and cannot be reached from a package.

---

## Files

| File | What it is |
|---|---|
| `UiHistory.cs` | The component: the feed, the binding, the layout, the clicking |
| `UiHistoryElement.cs` | One bet on screen. What a prefab derives from |
| `UiHistoryExampleElement.cs` | A working element in ten lines — a label, a colour, and a `Write` |
| `UiHistoryStyle.cs` | The gap, the arrival and the scrolling |
| `EHistoryFlow.cs`<br>`EHistoryOrder.cs`<br>`EHistoryAlign.cs`<br>`EHistoryOverflow.cs` | Direction, which end is newest, where a part-full strip sits, and what happens when it fills |
| `EHistoryFeed.cs` | Which history is shown: the player's own bets, or a shared game's rounds |
| `UiHistoryExample.cs` | Four strips built from code, elements and all — the last one on the shared feed |
| `Editor/History/UiHistoryMenu.cs` | The GameObject menu entry |
| `../Grid/` | `UiGrid`, which places the elements |
| `../Window/BetInfo/BetInfoWindow.cs` | The dialog a click opens on the player's own bets |
| `../Window/GameHistory/GameHistoryWindow.cs` | The dialog a click opens on the shared feed |
