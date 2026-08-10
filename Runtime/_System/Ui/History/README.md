# Ui History

The strip of recent bets: the last few rounds as a row of chips over the game, or a column of them down the
side. It feeds itself from the socket, animates each arrival, drops the oldest when it runs out of room — or
scrolls instead — and opens the **bet info** dialog on whichever chip is clicked.

One component and nothing else, for the ordinary case. What it cannot know — what a chip should *say* about a
round of your game — is a key in the inspector or a one-line function, with fallbacks that work without either.
What a round *looks* like is not its business at all: a game that wants more than a value on a plate gives the
strip its own element prefab and fills that in.

*Describes package 1.0.58. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the strips drawn rather than described.*

**GameObject → UI (Canvas) → History**, or Add Component → UI → Ui History.

---

## Quick start

1. **GameObject ▸ UI (Canvas) ▸ History** — a 620×64 strip, filled with sample bets so you can style it.
2. Anchor it where it belongs. Stretch it if you want it to grow with the screen; the count of chips follows
   the width.
3. Drag your bet info window into **Bet Info**. Nothing is searched for — see below.
4. Press play. The samples are replaced by `MainState.History`, and every `ON_HISTORY` after that arrives with
   a pop.
5. Click a chip. The dialog opens on that bet.

Nothing above needed a line of code. From here on it is a matter of what your game's round looks like:

```csharp
strip.Style.ElementSize = new Vector2(104f, 52f);   // shape
strip.TextKey = "multiplier";                       // what a chip says
strip.TextFormat = "{0}x";
strip.TextDecimals = 2;
strip.Text = data => Multiplier(data) + "x";        // or from code, for anything a key cannot say
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
gets the value, the background it finds becomes the hit area, and a `Button` is wired to open the bet info
window. Nothing is recoloured — the prefab looks exactly as it was drawn. The cheapest custom element is a
prefab with a label in it and *Add Component ▸ UI ▸ Ui History Element* on the root.

**The component with `Label` and `Plate` pointed at by hand** — for a prefab whose first label is not the one
the value belongs in.

**Your own component derived from it**, which is where a game ends up:

```csharp
public class DiceHistoryChip : UiHistoryElement
{
    [SerializeField] private Image face;
    [SerializeField] private TextMeshProUGUI amount;
    [SerializeField] private Color won, lost;

    public override void Write(HistoryDto data)
    {
        base.Write(data);                       // the label the strip decided on
        face.sprite = faces[int.Parse(Outcome("roll"))];
        amount.text = data.WinAmount;
        amount.color = decimal.Parse(data.WinAmount) > 0m ? won : lost;
    }

    public override void Appear() { … }         // arrive your own way
}
```

**This is where a game says what a win looks like.** The strip has no opinion about it and no settings for it:
it hands over the whole `HistoryDto` and a serialized field of your own beats any list of cases it could have
offered. `Data` is that bet, `Text` is what the strip decided this element should say, and `Outcome("key")`
reaches the game's own payload.

**Element Size** at zero on an axis leaves that side of the element to itself — to its own `LayoutElement`, or
to the width its text asks for. An element with no opinion about its size in an auto track measures as nothing,
so give it one or set **Element Size**.

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

Which dialog: the one in **Bet Info**, and only that one. **Nothing is searched for**, and that is a decision
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
| **Style** | Strip: element size, gap, padding. Chip: fill, border colour and size, radius, softness. Text: colour, font, size and style, alignment, inset, shrink. Arrival: duration, scale, fade, ease, unscaled. Scrolling: sensitivity, inertia, deceleration |
| **Element** | Element Prefab |
| **Strip** | Flow, Order, Align, Overflow, Capacity, Clip, Follow Newest |
| **Value** | Text Key, Text Format, Text Decimals, Text Pad, Id Length, Id From End |
| **Behaviour** | Follow State, Dedupe, Sort By Time, Animate Arrivals, Bet Info, Preview, Sample Count |
| **Events** | On Picked, On Element |

Everything under **Chip** and **Text** describes the built-in element and is ignored once an Element Prefab is
given — a prefab is its own look.

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

- **The element's label is taken off the raycast**, and its plate is put on it. A label is routinely laid out
  larger than the element it sits in — a 200-wide caption inside a 60-wide chip is an ordinary thing to build —
  and a raycast target that overhangs its neighbours means hovering one chip lights up the one beside it. The
  plate is the hit area, and the plate is exactly the size of the element. A label that overhangs still *draws*
  outside its chip, so stretch it to the element if the value can be long.

- **A clicked chip is deselected as the click lands.** UGUI leaves a clicked `Button` selected and a selected
  `Selectable` goes on drawing its highlighted colour, which on a strip reads as some other bet being hovered.
  Buttons the strip adds also get `Navigation.None`, so fifteen chips do not swallow the arrow keys.

- **In the editor, a style change repaints.** Every element is written again on `ApplyStyle`, not only the ones
  whose bet changed — otherwise a setting edited in the inspector would rearrange the strip and rewrite nothing.
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
| `UiHistoryStyle.cs` | The built-in chip, the spacing, the arrival and the scrolling |
| `EHistoryFlow.cs`<br>`EHistoryOrder.cs`<br>`EHistoryAlign.cs`<br>`EHistoryOverflow.cs` | Direction, which end is newest, where a part-full strip sits, and what happens when it fills |
| `UiHistoryExample.cs` | Three strips built from code: plain, multipliers, and a scrolling column |
| `Editor/History/UiHistoryMenu.cs` | The GameObject menu entry |
| `../Grid/` | `UiGrid`, which places the elements |
| `../RoundedBox/` | `RoundedBox`, which draws the built-in chip |
| `../Window/BetInfoWindow.cs` | The dialog a click opens |
