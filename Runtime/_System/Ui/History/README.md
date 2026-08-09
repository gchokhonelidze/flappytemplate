# Ui History

The strip of recent bets: the last few rounds as a row of chips over the game, or a column of them down the
side. It feeds itself from the socket, animates each arrival, drops the oldest when it runs out of room — or
scrolls instead — and opens the **bet info** dialog on whichever chip is clicked.

One component and nothing else, for the ordinary case. What it cannot know — what a chip should *say* about a
round of your game, and what counts as a win in it — is a key in the inspector or a one-line function, and
both have fallbacks that work without either.

*Describes package 1.0.58. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the strips drawn rather than described.*

**GameObject → UI (Canvas) → History**, or Add Component → UI → Ui History.

---

## Quick start

1. **GameObject ▸ UI (Canvas) ▸ History** — a 620×64 strip, filled with sample bets so you can style it.
2. Anchor it where it belongs. Stretch it if you want it to grow with the screen; the count of chips follows
   the width.
3. Press play. The samples are replaced by `MainState.History`, and every `ON_HISTORY` after that arrives with
   a pop.
4. Click a chip. The bet info window in the scene opens on that bet.

Nothing above needed a line of code. From here on it is a matter of what your game's round looks like:

```csharp
strip.Style.ElementSize = new Vector2(104f, 52f);   // shape
strip.TextKey = "multiplier";                       // what a chip says
strip.TextFormat = "{0}x";
strip.TextDecimals = 2;
strip.Classify = data => Payout(data) >= 2m ? "win" : "loss";
```

---

## What a chip says

Asked in this order, and the first answer that is not empty wins:

| # | Source | Set it | Use it for |
|---|---|---|---|
| 1 | `Text` | code: `strip.Text = data => …` | Anything a key cannot answer — two values, a symbol, a word |
| 2 | **Text Key** | inspector or `strip.TextKey` | A value the server already sends in the bet's outcome |
| 3 | Nonce | — | The default where the game has one |
| 4 | Tail of the bet id | **Id Length**, **Id From End** | The last resort, and the reason a chip is never blank |

`Text` returning `null` falls through to the rest, so a game that only wants to override *some* bets can.

**Text Key** reads the bet's own `Outcome` payload — both shapes of it, the flat `_Outcome` the socket fills
in and the raw JSON it arrived as. Whatever comes out is a string, so three fields shape it:

| Field | Does | Example |
|---|---|---|
| **Text Decimals** | Cuts a number to that many decimals. Negative leaves it exactly as sent | `2.839` → `2.83` |
| **Text Pad** | Pads the whole part with zeros so a strip of numbers lines up | `2.83` → `02.83` |
| **Text Format** | A `string.Format` pattern for the result | `{0}x` → `02.83x` |

Numbers are **truncated, not rounded** — the same as everywhere else in this package. A multiplier the server
called `2.839` was `2.83`; rounding it up would show the player a number that never happened.

---

## Which case a bet is

Every element is painted from a **scenario**: a name and four colours on the style. Which scenario a bet gets
is asked in the same shape as its text:

| # | Source | Set it |
|---|---|---|
| 1 | `Classify` | code: `strip.Classify = data => "win"` |
| 2 | **Scenario Key** | inspector — a key in the outcome whose *value* is the scenario name |
| 3 | The amounts | nothing to set |

The amounts give `win` when more came back than was staked, `push` when something did, `loss` when nothing
did — and the style ships with one scenario of each name. A game with three kinds of round adds a third entry
to the list and returns its name from `Classify`; nothing has to be widened for it, because a scenario is a
string rather than an enum.

A scenario carries **Fill**, **Border Colour**, **Border Size** (negative for the strip's own) and **Text
Colour**. Shape — the corner radius, the font, the size of a chip — is one look for the whole strip and lives
on the style, because a win that is a different *shape* from a loss reads as two components rather than one.

### The accent bar

Each scenario also has an **Accent Colour** and **Accent Size**: a bar under the element, drawn only on the
bets `Mark` picks out. Unset, `Mark` picks out **the player's own bets** — it compares `IPlayerId` against
`SystemState.Me` — which is what a shared history strip is really for. A game that wants it to mean something
else says so:

```csharp
strip.Mark = data => data.N > 100;        // the accent means whatever you want it to
```

Set a scenario's **Accent Size** to zero and that case never shows one, however the bet was marked.

---

## Shape and direction

| Field | Means |
|---|---|
| **Flow** | `Horizontal` — a row. `Vertical` — a column |
| **Order** | Which end the newest bet lands on: `NewestFirst` (left, or top) or `NewestLast` |
| **Align** | Where the elements sit while there are too few to fill the strip: `Start`, `Center`, `End` |
| **Overflow** | `Clamp` — show as many as fit and drop the oldest. `Scroll` — keep them all and let it be dragged |
| **Capacity** | How many bets are *kept*, whatever is shown. Zero keeps everything. The socket itself keeps 15 |
| **Clip** | Cut anything reaching past the strip's rect. Always on while scrolling |

`Align: End` with `Order: NewestLast` is the crash-game look: bets arrive at the right edge and push the rest
leftwards.

### Clamping

How many fit is the strip's own width divided by **Element Size** along the flow, gaps taken out. So clamping
needs that size set — with it at zero there is nothing to count with, everything is shown, and anything past
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

## Your own element

**Element Prefab** replaces the built-in chip with a prefab of your own. The field is typed
`UiHistoryElement`, so the component goes on the prefab's **root** — which is also the only thing the strip
needs from it. Three ways in, in the order they cost:

**The component alone, fields empty.** No script of your own: the first TextMeshPro label inside the prefab
gets the value, the background gets the scenario's colours, a child called `Accent` becomes the accent bar,
and a `Button` is wired to open the bet info window. The cheapest custom element is a prefab with a label in
it and *Add Component ▸ UI ▸ Ui History Element* on the root.

**The component with `Label`, `Plate` and `Accent` pointed at by hand** — for a prefab whose first label is
not the one the value belongs in.

**Your own component derived from it**, which is where a game ends up:

```csharp
public class DiceHistoryChip : UiHistoryElement
{
    [SerializeField] private Image face;
    [SerializeField] private TextMeshProUGUI amount;

    public override void Write(HistoryDto data)
    {
        base.Write(data);                       // the label the strip decided on
        face.sprite = faces[int.Parse(Outcome("roll"))];
        amount.text = data.WinAmount;
    }

    public override void Paint(UiHistoryScenario look)
    {
        base.Paint(look);                       // or nothing at all, and keep your own colours
        amount.color = look.TextColor;
    }

    public override void Appear() { … }         // arrive your own way
}
```

`Data` is the whole `HistoryDto`, `Text` is what the strip decided this element should say, `Scenario` is the
name it was put in, `Marked` says whether `Mark` picked it out, and `Outcome("key")` reaches the game's own
payload.

Two settings for prefabs: **Paint Elements** off leaves your colours exactly as drawn, and **Element Size** at
zero on an axis leaves that side of the element to itself — to its own `LayoutElement`, or to the width its
text asks for. An element with no opinion about its size in an auto track measures as nothing, so give it one
or set **Element Size**.

---

## Arrivals

A new element grows and fades in, from **Appear Scale** over **Appear Duration** on **Appear Ease**, on
unscaled time so a game that paused itself still animates its strip. Zero duration puts it there with no
animation; **Animate Arrivals** off does the same for every arrival.

Elements are kept with the bet they show rather than reassigned by position, so a strip that shifts along only
writes the one chip that is new — a prefab holding state of its own keeps it as it slides. The shift itself is
not animated: the elements are placed by a grid, and a tween fighting a layout is a tween that loses.

---

## Clicking

A click raises `OnPicked(HistoryDto)` and then opens the bet info dialog on that bet — `Show(id)`, so the
dialog asks the server for the whole transaction with its seeds rather than making do with the summary the
history payload is.

Which dialog: **Bet Info** if it is set, otherwise one found in the scene, including a closed one — which
every dialog is until it is opened. **Open Bet Info** off leaves the click to `OnPicked` alone.

---

## Feeding it yourself

**Follow State** on — the default — seeds from `MainState.History` on enable and adds every `ON_HISTORY` after
that. Off hands the feeding over:

| Call | Does |
|---|---|
| `Add(data)` | The newest bet. Animates, drops the oldest past `Capacity` |
| `AddRange(values)` | One after another |
| `Set(values)` | Replaces everything, oldest first. Animates nothing |
| `Remove(id)`, `Clear()` | What they say |
| `Refresh()` | Writes every element again from the bet it holds — after changing what a chip should say |

`Add` on a bet whose id is already in the strip replaces it where it is rather than adding a second chip
(**Dedupe**), which is what a payout arriving after its round should do.

---

## From code

```csharp
var strip = UiHistory.Create(canvas, "History", 620f, 64f);
strip.Flow = EHistoryFlow.Vertical;
strip.Overflow = EHistoryOverflow.Scroll;
strip.Capacity = 0;
strip.Style.ElementSize = new Vector2(0f, 48f);
strip.OnPicked.AddListener(data => Debug.Log(data.Id));
```

| Member | Does |
|---|---|
| `Create(parent, name, width, height)` | Builds one, ready to be fed |
| `Style` | The look. Change anything on it and call `ApplyStyle()` |
| `ApplyStyle()` | Writes every colour, gap and scroll setting, then lays out |
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
| **Style** | Strip: element size, gap, padding. Element: radius, softness, border, font, text size and style, alignment, inset, shrink. Accent: inset, offset. Scenarios: the default one and the list. Arrival: duration, scale, fade, ease, unscaled. Scrolling: sensitivity, inertia, deceleration |
| **Element** | Element Prefab, Paint Elements |
| **Strip** | Flow, Order, Align, Overflow, Capacity, Clip, Follow Newest |
| **Value** | Text Key, Text Format, Text Decimals, Text Pad, Id Length, Id From End |
| **Scenario** | Scenario Key |
| **Behaviour** | Follow State, Dedupe, Animate Arrivals, Open Bet Info, Bet Info, Find Bet Info, Preview, Sample Count |
| **Events** | On Picked, On Element |

**Preview** is what fills a strip with sample bets when there is no socket running, so the component looks
like a history strip in the editor rather than like an empty rect. A real session replaces them on enable.

---

## Worth knowing

- **The elements sit in a `UiGrid`, one track each.** Every element the strip is not showing is a spare kept
  in the same grid under a name the layout does not mention. That is how it is hidden — a grid shows exactly
  what its layout names and re-asserts that every time it is enabled, so anything switched off behind its back
  comes straight back on. Naming is the only way to hide something in a grid that it will not argue with.

- **An empty strip still gets a layout** — one empty cell. A grid with *no* layout shows every child it has,
  which here would be every spare in the pool.

- **A size from the style is written as a `LayoutElement`**, not only as a `sizeDelta`. An auto track is as big
  as the items in it say they need to be, and a plain rect says nothing at all.

- **The cross axis is always one flexible track.** An element with a fixed size across the flow keeps it and is
  centred in the strip; a fixed *track* would pin the row to the top edge and leave the rest of the strip empty
  under it.

- **Drags reach the scroller through the buttons.** A `Button` does not handle drag, so the event bubbles up to
  the `ScrollRect` above it — which is why a chip can be both clicked and dragged. The strip also carries an
  invisible `Image` behind the elements, enabled only while scrolling, so a drag that starts in a gap between
  chips still scrolls.

- **One arrangement per frame.** However many bets arrive in a frame, the strip is laid out once, at the end of
  it. `Layout()` is the immediate version, for a game that wants to read `Elements` in the same breath.

- **Clipping cuts the accent bar** if the elements fill the strip's height exactly. **Accent Offset** hangs the
  bar below the chip by default, so leave a couple of units of slack across the flow, or set the offset to
  zero.

- **Every tween is built with `DOTween.To`** rather than the `DOScale`/`DOFade` shortcuts. Those live in
  DOTween's UI module, which is compiled into the project's own assembly and cannot be reached from a package.

---

## Files

| File | What it is |
|---|---|
| `UiHistory.cs` | The component: the feed, the binding, the layout, the clicking |
| `UiHistoryElement.cs` | One bet on screen. What a prefab derives from |
| `UiHistoryStyle.cs` | Shape, spacing, arrival and scrolling, plus the scenarios |
| `UiHistoryScenario.cs` | One named look — what a win looks like |
| `EHistoryFlow.cs`<br>`EHistoryOrder.cs`<br>`EHistoryAlign.cs`<br>`EHistoryOverflow.cs` | Direction, which end is newest, where a part-full strip sits, and what happens when it fills |
| `UiHistoryExample.cs` | Three strips built from code: plain, multipliers, and a scrolling column |
| `Editor/History/UiHistoryMenu.cs` | The GameObject menu entry |
| `../Grid/` | `UiGrid`, which places the elements |
| `../RoundedBox/` | `RoundedBox`, which draws the built-in chip |
| `../Window/BetInfoWindow.cs` | The dialog a click opens |
