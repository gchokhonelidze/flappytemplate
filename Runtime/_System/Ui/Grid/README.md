# Ui Grid

A grid layout for uGUI in the shape of CSS's, cut to what a canvas can do cheaply. Tracks that are a fixed
size, a share of the leftover, a percentage of the box or as big as their contents; items that span more
than one of either; a flow that drops anything unplaced into the first free cell; and an arrangement you can
hand over from code as a single string.

Every cell is a **child RectTransform of its own** — a panel you can select, colour, animate and prefab.
That is the difference from `GridLayoutGroup`, whose cells are all one size and are not objects at all.

*Describes package 1.0.41. Update this file with the code.*

---

## Quick start

1. **GameObject ▸ UI (Canvas) ▸ Grid** — a 2×2 grid with four panels. *Grid (Empty)* leaves out the panels.
2. Click an empty cell in the inspector map to add a panel there. Drag a divider to resize a track.
3. Name the panels (`header`, `body`, `footer`) and press **Read Grid** to get a layout string.
4. Hand that string to `grid.Layout` from code to switch arrangements.

---

## Tracks

A grid is a list of columns and a list of rows. Each track is one of:

| Mode | Means | CSS |
|---|---|---|
| `Fixed` | Canvas units, the same whatever the grid is given | `120px` |
| `Flexible` | A weight — the leftover is split between the flexible tracks in proportion | `1fr` |
| `Percent` | A share of the content box, gaps already taken out, so 50 and 50 fill it exactly | `50%` |
| `Auto` | As big as the largest item in it reports itself to be | `auto` |

Each also has a **Min** and a **Max** in canvas units (`Max` of 0 means none). A flexible track that hits a
limit takes itself out of the sharing and the rest is split again, so a sidebar capped at 300 does not
swallow the space it could not use. Together they are CSS's `minmax(160px, 1fr)`.

**Gaps** sit between tracks; **Padding** sits inside the grid's own rect. Both are taken out before
percentages and shares are worked out, so tracks still add up to the full width.

Tracks asked for beyond the end of a list are **implicit** — drawn from the Implicit Rows / Implicit
Columns template under Flow and Alignment. They show faintly in the map; editing one makes it real.

A [layout](#layouts) can override these sizes for as long as it is set, which is how one arrangement gets a
fixed sidebar and the next gets a single column that fills the width.

> `Auto` asks the items in a track how big they need to be. A plain panel has no answer and the track comes
> out at **zero, and is not on screen at all** — give it a `Min`, make it `Fixed`, or put a `LayoutElement`
> on what is inside it. The inspector names any track this happens to.

## Items

Any child is laid out. A child with a **Ui Grid Item** can say more about where it goes:

| Field | Means |
|---|---|
| `Area` | The name this panel answers to in a layout. Blank means the object's own name. |
| `Auto Place` | On, the flow finds it a cell. Off, `Column`/`Row` are used exactly. |
| `Column`, `Row` | Counted from 0, left and top. |
| `Span` | How many columns across and rows down it covers. Honoured while auto-placing too. |
| `Override Align` | Use this item's alignment instead of the grid's. |

**Alignment** is per axis: `Stretch` fills the cell and writes the item's size; `Start`, `Center` and `End`
leave the size alone and only place it. Where a non-stretched item has no preferred size of its own — a
plain panel, which is most of them — its own width and height stay editable and only its position is driven.

A child with `ignoreLayout` (a `LayoutElement`) is left out of everything, placement and layouts both. That
is the way to keep a background or an overlay inside the grid without it being part of the arrangement.

## Flow

Items with no placement of their own are carried into the first free cell. `Auto Flow` picks the direction:
`Row` fills the columns you defined and adds rows underneath as it needs them, `Column` the opposite.
`Dense` lets a later small item fall back into a hole an earlier large one skipped — off, what is on screen
follows the order in the hierarchy.

---

## Layouts

An arrangement written as a picture, one name per cell. CSS calls it `grid-template-areas`.

```
header header header
nav    body   aside
footer footer footer
```

- A name repeated over neighbouring cells is **one panel spanning them**.
- `.` is an empty cell.
- Rows split on a **newline or a slash**, so the same layout fits on one line where that reads better:
  `"header header / nav body / footer footer"`. In the inspector's Layout box, Return starts a new row and a
  slash does the same thing without leaving the line.
- Names are the panels' **object names**, unless a Ui Grid Item's `Area` overrides them.
- Quotes around a row, commas and pipes are all tolerated — paste in CSS and it will read.

Setting a layout **shows the panels it names and hides the ones it does not**. It also decides **how many
columns and rows there are** — a grid set up with four rows and given a layout of three has three, and the
fourth track stops taking up the space its size asked for. Tracks are still added past the picture where an
item needs one: a panel the flow carried below it, or one holding a row of its own.

A name with nothing to match is not an error — those cells are held open, so a layout can be written before
the panel exists. The inspector lists any it cannot match, since a misspelling looks exactly the same.

### Track sizes in a layout

A layout can carry the track sizes too, which is the other half of what CSS changes in a media query. Add a
`cols:` or `rows:` line — anywhere in the string, in any order:

```
cols: 200 1fr 240
rows: auto 1fr auto
header header header
nav    body   aside
footer footer footer
```

Without them, a narrow layout that leaves one column standing gets whatever column 0 already was — a fixed
sidebar width, most likely — which is rarely what you want:

```
cols: 1fr            ← the one column left now fills the width
rows: auto 1fr auto
header
body
footer
```

| Token | Track |
|---|---|
| `120`, `120px`, `120u` | Fixed, canvas units |
| `1fr`, `2.5fr`, `*` | Flexible, that weight |
| `25%` | Percent |
| `auto` | Auto |
| `1fr[160]` | …with a Min of 160 |
| `1fr[160..400]` | …with a Min and a Max — CSS's `minmax` |
| `auto[..300]` | …with a Max only |

> **`auto` on a row of plain panels comes out at nothing.** It means "as big as the items in it need", and a
> panel with no text and no sprite needs nothing — so the row is 0 high and the grid looks like it lost a
> row. Write a size (`64`), a floor (`auto[64]`), or put a `LayoutElement` on what is inside it. CSS does the
> same thing to an empty div; it is just easier to walk into here. The inspector names any track it happens
> to.

A track the line covers is **taken from the layout entirely**, limits included; the grid's own list is what
tracks past the end of the line fall back to. `cols:` and `rows:` are headings, not names — a panel can still
be called `rows` if the colon is not there. Anything on the line that is not a size is ignored rather than
guessed at, so a typo leaves that track alone instead of collapsing it.

While a layout gives a track its size, the map header for it is drawn in the layout colour, and resizing it
by dragging or by the header menu **rewrites that line in the layout** rather than the list underneath.

### From code

```csharp
grid.Layout = "cols: 200 1fr / nav body";
grid.SetLayout(compact);              // the same thing as a method — takes a string or a UiGridLayout
grid.ClearLayout();                   // every panel shown again, placed by its own Column and Row
bool visible = grid.Shows("aside");   // would this panel be showing?
string now = grid.ReadLayout();       // what it is arranged as right now, in the same form
```

`ReadLayout` is how you get the first one: arrange the panels in the inspector, print it, paste it into the
code that switches between it and the next.

### Built rather than written

A picture is the right shape for a layout you keep whole — in the inspector, in a `const`, switched between
at runtime. It is the wrong shape for one assembled from parts, where it turns into string concatenation and
the compiler stops helping. `UiGridLayout.Build()` produces the same thing with types:

```csharp
grid.SetLayout(UiGridLayout.Build()
    .Columns(GridTrack.Fixed(200), GridTrack.Flexible(), GridTrack.Fixed(240))
    .Rows(GridTrack.Fixed(64), GridTrack.Flexible(), GridTrack.Fixed(48))
    .Row("header", "header", "header")
    .Row("nav",    "body",   "aside")
    .Row("footer", "footer", "footer")
    .Done());
```

| Call | Does |
|---|---|
| `.Columns(…)`, `.Rows(…)` | The track sizes — the same as a `cols:` / `rows:` line |
| `.Row(names…)` | Adds a row of the picture. `UiGridLayout.Empty` for a gap |
| `.Area(name, column, row, columnSpan, rowSpan)` | Places one name outright, instead of drawing every cell |
| `.Size(columns, rows)` | Holds the grid open to at least this, for rows that hold nothing |
| `.Done()` | The finished `UiGridLayout`, for `SetLayout` |

`Row` and `Area` mix — rows are laid down first and blocks stamped over them — so the bulk can be drawn and
the exceptions stated. Where only a few panels are placed, `Area` says it in a line each:

```csharp
grid.SetLayout(UiGridLayout.Build()
    .Columns(GridTrack.Flexible(), GridTrack.Flexible(2f))
    .Rows(GridTrack.Fixed(64), GridTrack.Flexible())
    .Area("header", 0, 0, columnSpan: 2)
    .Area("nav", 0, 1)
    .Area("body", 1, 1)
    .Done());
```

`grid.SetLayout` takes either a `string` or a `UiGridLayout`, and `UiGridLayout.Parse` and `ToString` convert
between them, so the two forms are interchangeable — the same layout, chosen by which reads better where you
are standing. A layout given as an object is kept as it is; the string is still written, because that is what
the scene saves and the inspector shows.

> Names must be **one word**. A layout is stored as text, so a name with a space or a separator in it comes
> back as two cells. The builder says so when it happens; give that panel a `Ui Grid Item` and set its `Area`.

### Tracks from code

The track lists are ordinary lists of ordinary objects. Nothing watches them, so say when you are done:

```csharp
grid.Columns[0].Mode = EGridTrack.Fixed;
grid.Columns[0].Size = 320f;
grid.Columns.Add(GridTrack.Flexible());
grid.ColumnGap = 12f;
grid.Rebuild();
```

`GridTrack.Fixed(120f)`, `GridTrack.Flexible(2f)`, `GridTrack.Percent(25f)` and `GridTrack.Auto()` build
them. The properties on `UiGrid` itself — `ColumnGap`, `RowGap`, `Flow`, `Dense`, `HorizontalAlign`,
`VerticalAlign` — rebuild on their own.

See **UiGridExample.cs** next to this file for all of it working, including swapping layouts as the window
changes shape.

### Reading the result

`grid.Snapshot()` measures the whole layout and hands back where every track and every item landed —
positions, sizes, spans, and which item is in which cell. The inspector draws its map from this, so what it
shows is what the layout really did rather than a second guess at it. `CellAt`, `CountAt` and `HasOverlap`
answer questions about cells; `UiGridCell.Overlaps` compares two items.

---

## The inspector

The map is a picture of the grid at its real proportions, and everything in it is live.

| Do this | Get this |
|---|---|
| Click an empty cell | Builds a panel there — the kind is the dropdown next to *Fill Empty Cells* |
| Click a panel | Selects it in the hierarchy |
| Drag a panel | Moves it. Onto another panel, the two **swap** |
| Drag a divider | Resizes that track. Dragging an `Auto` track makes it `Fixed` at the size it had |
| Click a track header | Mode, fit to size, insert, duplicate, delete |
| Right-click a cell | Pin, let it flow, span wider/taller, delete |

Colours: green is pinned by hand, blue was placed by the flow, purple came from the layout, red is a stack.

Insert and Delete move the items' line numbers with the tracks, the way inserting a row in a spreadsheet
does — everything below comes along, and a panel spanning across the cut grows or shrinks by one.

It warns about what it cannot fix on its own: cells holding more than one panel (with a **Separate Stacked
Panels** button), `Auto` tracks that measured zero, names in the layout that match nothing, and names drawn
in two separate blocks.

---

## Notes

- **Two panels can share a cell.** Explicit placements are allowed to overlap, as in CSS, and a stack draws
  as a single panel. The map marks them `×2` and offers to separate them. Auto-placed items never overlap.
- **z is not touched** at runtime, matching every other uGUI layout group. Panels built from the menu are
  zeroed; one dragged in by hand keeps whatever z it had — check that before its placement if it vanishes.
- **Rebuilding** is automatic for anything with a property setter and for anything the inspector does. It is
  not automatic for reaching into `Columns`/`Rows` and changing a `GridTrack`; call `Rebuild()`.
- **Widths are settled before heights are measured**, as uGUI expects, so a label that wraps reports the
  height it needs at the width the grid just gave it.

## Files

| File | What |
|---|---|
| `UiGrid.cs` | The layout group: tracks, placement, sizing, the layout string |
| `UiGridItem.cs` | Per-panel name, placement and alignment |
| `UiGridLayout.cs` | Layout strings: parsing, formatting, editing, and the builder |
| `GridTrack.cs` | One row or column |
| `UiGridCell.cs`, `UiGridSnapshot.cs` | Where everything landed, for the inspector and for you |
| `UiGridExample.cs` | A MonoBehaviour doing all of the above |
| `Editor/Grid/` | The map, the item inspector, the menu and cell building |
