# Navbar

[← All documentation](../../../../)

The row of buttons over the game: **home**, **statistics**, **fairness**, **hotkeys**, and whatever the game
adds beside them. Like the [windows](../Window/) next door it is one component on an empty RectTransform — the strip, the
buttons and the glyphs in them are all built from code the first time they are needed, so there is no prefab
to keep in step and no hierarchy to rebuild when a game decides its chrome looks different after all.

It knows two things the game would otherwise have to be told. **Home** leaves for the address the server sent,
taking the whole page with it rather than the iframe the build is drawn in — and hides itself while there is
no such address. **Statistics**, **Fairness** and **Hotkeys** find their windows in the scene, or build them,
and light up while the one they opened is on screen.

*Describes package 1.0.81. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the bar and its glyphs drawn rather than described.*

**GameObject → UI (Canvas) → FlappyBet → Navbar**, or Add Component → UI → Ui Navbar. It belongs on an
**empty RectTransform**, not a UI Panel: the bar draws its own strip, so a Panel's Image leaves two
backgrounds stacked. Or build one from code:

```csharp
var navbar = UiNavbar.Create(canvas);
```

That is the whole of it. The three buttons are the default list, the bar pins itself to the top right corner,
and it sizes itself to whatever is showing in it.

## The buttons

| Kind | What it does |
| --- | --- |
| Home | Leaves the game for `SystemDto.ReturnUrl`. **Hides itself while there is no such address**, which is the usual case for a demo or a direct link — see [Leaving the game](#leaving-the-game). |
| Statistics | Opens the [statistics window](../Window/), and closes it again on a second press. |
| Fairness | Opens the [fairness window](../Window/), the same way. |
| Hotkeys | Opens the [hotkeys window](../Hotkeys/): which keys are bound to what, and the switch that turns them on. It reads the hotkey registry, so there is nothing to hand it. |
| Custom | Nothing of its own: it fires the slot's event and the bar's. For a button of the game's own. |

Each slot in the **Buttons** list carries its kind, a caption, an optional sprite, an **Enabled** tick and a
`UnityEvent` of its own. Reordering the list reorders the bar. Unticking Enabled takes the button out
entirely rather than greying it — nothing is placed for it, and the buttons after it close the gap.

```csharp
var mine = new NavbarButton(ENavbarButton.Custom, "Sound");
mine.OnPressed.AddListener(() => audio.Toggle());

navbar.Buttons.Add(mine);
navbar.Rebuild();          // the list changed, so the bar builds the button now in it
```

Every press also fires the bar's own `OnPressed`, which carries the kind — one listener for the lot, where a
game wants to play a click or close something else first.

Adding a **built-in** button later is four cases: a value on `ENavbarButton`, a glyph in
`UiNavbarIcons.Draw` with its name in `Prefix` beside it, and a case in `UiNavbar.Press`. One that opens a
window of its own wants three more — a slot for it, a resolve property, and a case in `Active` so the button
lights while it is open — which is exactly what Hotkeys was. Until a game needs that, `Custom` plus a listener
is the same button without the package having an opinion about it.

## Leaving the game

A FlappyBet build is nearly always an **iframe inside an operator's page**, and `ReturnUrl` is a page on that
operator's site. So Home aims at the **top** window: pointing the iframe at a lobby would draw that lobby
inside the game's rectangle, with the operator's chrome still wrapped around it.

Whether that is allowed is the browser's decision, and not one that can be asked about in advance —
`Location.href` is writable across origins, so a frame may set `window.top.location` without an error and be
refused all the same. `Navigation.jslib` therefore tries every route there is, in this order:

1. `window.top.location.href` — works same-origin, and wherever the frame is allowed to navigate the top.
2. A link with `target="_top"`, clicked. The route `allow-top-navigation-by-user-activation` opens, and a
   press on the canvas is that activation.
3. `window.open(url, "_top")`.
4. Failing all three, the frame goes there itself. Not what was asked for — but it is the destination, and a
   button that does nothing at all is worse.

Alongside that it **posts to the parent page** in the envelope the aggregator front already speaks, so a host
that keeps top navigation to itself can act on it:

```js
{ EventName: "NAVIGATE", Data: { Url: "https://…" } }
```

Both paths lead to the same address, so it does not matter which of them the browser honours.

`Navigator` is the C# half, and is worth calling directly for a Home button of the game's own:

```csharp
if (Navigator.CanReturn)
    Navigator.Home();
```

Outside WebGL — the editor, a standalone build — there is no top window and it is `Application.OpenURL`.

> **Only http and https.** The address arrives over the socket rather than out of the scene, which makes it
> worth a look before it is handed to a browser: `javascript:` and `data:` are addresses the way an
> instruction is an address. `Navigator.IsNavigable` allows those two schemes, a path on the page
> (`/lobby`, `?back=1`), and a protocol-relative `//host/path`. Anything else is refused and logged rather
> than followed.

To try the button in the editor, where there is no socket to send an address over, fill in **Return Url** on
the bar. A build leaves it empty.

## The windows

Statistics, Fairness and Hotkeys need a window each, and the bar will find one three ways, in this order:

1. **The one in the slot.** Drag a window from the scene into Statistics Window, Fairness Window or Hotkeys
   Window, and that is the one opened — styled, placed and sized however it was built.
2. **The one in the scene.** Left empty, the bar looks for a window of that type, switched off ones included
   — which every closed window is.
3. **One it builds.** Nothing found, it builds one under the canvas. Not under the bar: a window parented
   into the bar would move with it, be clipped by it, and sit in the corner the bar was pinned to. **Window
   Parent** overrides where it goes; **Create Windows** off leaves the button firing its event and nothing
   more, for a game that opens its own dialogs.

No window is built until its button is first pressed, so a bar in a scene that never opens one costs
nothing. While a window is open its button takes `ButtonActiveFill`, so a player can see which dialog they
are looking at from the bar rather than from the dialog. **Toggle Windows** off makes a second press leave it
open instead of closing it.

## Shape and place

**Flow** is which way the bar runs — `Horizontal`, a row, or `Vertical`, a column down one side. That is the
bar's own direction and has nothing to do with the game being played in portrait or landscape.

**Dock** pins the bar to a corner or an edge of whatever it is drawn in, and **Dock Offset** is how far in
from there. Both numbers are read as a distance from the edge they are nearest, so one pair means the same
thing in every corner — a bar moved from the top right to the bottom left in the inspector stays the same
distance in rather than jumping off the screen. Off leaves the rect exactly as the scene saved it.

> **A parent with no size does not defeat it.** An anchor is a *fraction* of the parent's rect, so in a
> parent that is nothing by nothing — an empty holder, or a [grid](../Grid/) cell the layout never gave an
> area to — every fraction lands on the same point: top right, centre and bottom left all come out in one
> place, and only the pivot appears to move anything. The bar therefore measures its corner on the nearest
> rect above it that *has* a size, usually the canvas, and follows it as that resizes. `DockHost()` hands
> back whichever rect that turned out to be. Docking inside a parent that is properly sized is untouched by
> any of this — including a grid cell that the grid does give an area to.

**Fit To Buttons** sizes the bar to what is showing in it, padding included, which is what a bar in a corner
wants. Off leaves the rect the size it was given and **Align** decides where the buttons sit in it:

| Align | |
| --- | --- |
| Start | Left, or top. |
| Center | The middle of the bar. |
| End | Right, or bottom. |
| Spread | First and last against the ends, the rest evenly between. Spacing becomes a minimum rather than the gap. |

A bar across the bottom of a phone screen is the second shape:

```csharp
var navbar = UiNavbar.Create(canvas);

navbar.DockTo = ERectAnchor.Bottom;
navbar.DockOffset = new Vector2(0f, 24f);
navbar.FitToButtons = false;
navbar.Align = ENavbarAlign.Spread;

navbar.Rect.sizeDelta = new Vector2(420f, 92f);
navbar.Layout();
```

## Styling

Everything the bar draws is on **Style**, and unlike the window next door the bar does paint over its parts —
they are generated geometry rather than objects to be styled by hand, and a glyph has to follow the colour
its button was given.

| Field | |
| --- | --- |
| Show Bar | The strip behind the buttons. Off leaves them floating over the game, and the strip stops swallowing the clicks that miss a button. |
| Bar Fill / Bar Corner Radius | |
| Bar Padding | Between the strip's edge and the buttons in it. |
| Button Size / Button Spacing / Button Corner Radius | |
| Button Fill | |
| Button Active Fill | While the window that button opens is on screen. |
| Icon Color | |
| Icon Scale | The glyph's square as a fraction of the smaller side of the button. |
| Icon Thickness | Stroke width of the glyphs drawn from boxes. |
| Show Labels | A caption under each glyph. Off is the usual case for small buttons over the game. |
| Label Font / Size / Color / Style | |
| Label Gap / Label Height | The band the caption is drawn in, off the bottom of the button. The glyph gets what is left — so a bar with labels wants a higher Icon Scale than one without. |

A style is read rather than watched, so assign it back to have it taken up:

```csharp
var style = navbar.Style;
style.ShowLabels = true;
style.IconScale = 0.62f;

navbar.Style = style;      // assigning is what rebuilds the bar around it
```

### The glyphs

The house, the bars and the shield are drawn from [rounded boxes](../RoundedBox/) rather than fetched from an
atlas — the same reasoning as the window's close cross. Generated geometry stays sharp at any size, costs no
import and no texture memory, and follows the style's colour without a second sprite being authored for a
light theme.

Two of them are painted over rather than cut out: the door of the house and the tick on the shield are drawn
in the *button's* colour. Worth knowing before a gradient is put on a button — against anything but a flat
fill they would show as the wrong colour rather than disappear.

A **sprite on the slot** replaces the drawn glyph entirely. It is tinted with the icon colour and fitted to
the same square, so a game with an icon set of its own keeps every other setting on this page.

## In the editor

The bar is `[ExecuteAlways]`, so **every field takes effect as it is typed** — a colour, a size, a flow, a
dock corner or a whole button changes what is on screen without entering play mode. The parts it is made of
are not authored but built from these fields, and a field whose result cannot be seen is a field being
guessed at.

Two things follow from that:

- **The Home button is drawn while a scene is only being laid out** — no `StateManager` and not playing means
  there is no socket to have sent an address over, so the button is shown to be seen, measured and styled.
  The same reasoning as the statistics window showing sample figures to a scene with nothing feeding it. A
  build always has a `StateManager`, so this never hides a real absence from a player. Fill in **Return Url**
  to make the button actually go somewhere from the editor.
- **Rebuilding is safe to ask for at any time.** Parts that exist are found by the name they were made under
  and only what is missing is made, so the bar heals itself after a script reload, an undo, or a button
  deleted from the hierarchy by hand. `Rebuild` and `Layout` are both on the component's context menu.

## What it watches

| | |
| --- | --- |
| `OnSystem` | The return address arrives with the rest of the system state, after the scene has loaded rather than with it. This is what puts the Home button on screen at all. |
| A window opening or closing | Neither goes through the socket, so the bar listens to the windows it resolved. This is where the active colour comes from. |
| `Translator.OnLocaleChanged` | The captions are written through `Translator.Label`, and one that is a word in English may be two in German — so a language change is a re-layout, not a repaint. |

**Follow State** off drops all three. The bar then shows whatever it was last told, which is what a scene
built to look at the bar rather than to play in wants.

## Translations

The captions go through `Translator.Label`, the same as every other caption the package draws. A key is
translated, the en_US wording of a key is translated, and a word of your own is printed as it was typed —
which is why the defaults are `Home`, `Statistics` and `Fairness` and they still come out in the player's
language. `Home` is `navbar.home`; the other three are `statistics.title`, `fairness.title` and
`hotkeys.title`, shared with the windows they open, since a button named one thing and a dialog named another
read as two features.

See [Translations](../../Translations/) for adding a caption of your own.

## From code

| | |
| --- | --- |
| `UiNavbar.Create(parent, name, flow)` | Builds the whole thing under a parent, usually a canvas. |
| `Buttons` | The slots, in the order they are drawn. `Rebuild` after changing it. |
| `Style` | Read it, change it, assign it back. |
| `Flow`, `Align`, `FitToButtons`, `Docked`, `DockTo`, `DockOffset` | Each lays the bar out again as it is set. |
| `Press(kind)` | Does whatever the kind says, and fires the events. For driving the bar from a key. |
| `GoHome()` | Leaves for the return address. False means there was nowhere to go. |
| `ShowStatistics()` / `ShowFairness()` / `ShowHotkeys()` | Open the window, or close it again. |
| `Statistics` / `Fairness` / `Hotkeys` | The windows themselves — found or built on the first read. |
| `Destination` / `CanReturn` | Where Home would go, and whether it would go anywhere. |
| `DockHost()` | The rect the dock corner is measured on: the parent, or the nearest sized thing above it. |
| `Rebuild()` | Builds the buttons from scratch. After changing the list. |
| `Layout()` | Places and repaints. After changing a rect by hand. |
| `Refresh()` | Reads the state again. Called for you by everything the bar watches. |
| `OnPressed` / `OnHome` | |

## Worth knowing

- **The bar is not a window.** It has no backdrop, no transition and no canvas of its own, so it sorts by
  hierarchy position like any other UI object. A bar that ends up behind the game wants to be later in the
  canvas than whatever is covering it — or on a canvas of its own with Override Sorting, the way `UiWindow`
  does it.
- **A strip that is drawn swallows the clicks that miss its buttons**, which is what stops a press on the bar
  reaching the game behind it. Show Bar off catches nothing, and a press between two buttons goes through.
- **Nothing showing means no strip either.** A bar whose only button was Home, on a game with nowhere to
  return to, would otherwise be an empty tab of colour in the corner.
- **Buttons the bar made are named `Button 0`, `Button 1`…** and only those are swept away on a rebuild, so a
  badge or a divider parked under the bar by the game survives.
- **`Custom` draws nothing** without a sprite: an empty coloured button, waiting for one.

## Files

| | |
| --- | --- |
| `UiNavbar.cs` | The component: building, placing, painting, and what each press does. |
| `UiNavbarStyle.cs` | Everything it looks like. |
| `UiNavbarIcons.cs` | The house, the bars, the shield and the keyboard, drawn from boxes. |
| `NavbarButton.cs` | One slot: kind, caption, sprite, event. |
| `ENavbarButton.cs` | What a button does. |
| `ENavbarFlow.cs` | A row or a column. |
| `ENavbarAlign.cs` | Where the buttons sit along a bar longer than they need. |
| `UiNavbarExample.cs` | Three bars, built at runtime. Read as much as run. |
| `../../Navigation/Navigator.cs` | Leaving the game, and what an address has to look like to be followed. |
| `../../JSPlugins/Navigation.jslib` | The four routes to the top window, and the message to the parent. |
