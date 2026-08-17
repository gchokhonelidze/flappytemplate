# FlappyBet Template

Every readme in the package, in one place. Each row below opens a component's documentation; a `README.html`
twin sits beside every one of them in the same folder, with the same content laid out for a browser and the
component **drawn** rather than described.

*Package 1.0.82. Update this file when a readme is added or removed — and the versions in
[Which are current](#which-are-current) when one is brought up to date. **README.html** beside it is the same
content for reading in a browser.*

> **Read the `.html` copies locally, the `.md` copies here.** The browser copies draw each component out of CSS —
> a keyboard with its bound caps lit, the navbar's glyphs, the dialogs themselves. None of that survives a
> repository page: GitLab and GitHub both render only Markdown for a folder's landing view, show `.html` files as
> source, and strip `<style>` and `class` out of any HTML embedded in Markdown. So the two copies are not
> redundant — they are for two different places to be standing.

## Where to start

Three roads in, depending on what you are here for:

- **Putting a dialog or a bar on screen.** Start with [Window](Runtime/_System/Ui/Window/) — it is the
  component the six built-in dialogs are made from — then [Navbar](Runtime/_System/Ui/Navbar/), the row
  of buttons that opens four of them.
- **Drawing something of your own.** [Rounded Box](Runtime/_System/Ui/RoundedBox/) is the panel
  everything here is built out of, and [Ui Grid](Runtime/_System/Ui/Grid/) is what arranges it.
- **Wiring the game to the server.** There is no readme for the socket yet — see
  [Not documented yet](#not-documented-yet) — but [Translations](Runtime/_System/Translations/) covers
  every string the player reads, and [Hotkeys](Runtime/_System/Ui/Hotkeys/) and
  [Sound](Runtime/_System/Audio/) are worked examples of a feature that reads a setting, emits one, and
  follows the socket both ways.

## Game chrome

The furniture drawn over the game: dialogs, the bar that opens them, the strip of recent bets, and the keys.

| | Describes | What it is |
| --- | --- | --- |
| **[Window](Runtime/_System/Ui/Window/)** | 1.0.82 | A dialog that is one component rather than a prefab: panel, caption, close button, drag, an animated opening, and a body that scrolls when the content outgrows the screen. The six windows built on it — Statistics, Bet info, Game history, Fairness, Hotkeys and Sound — are documented here too. |
| **[Ui Navbar](Runtime/_System/Ui/Navbar/)** | 1.0.82 | The row of buttons over the game: home, statistics, fairness, hotkeys, sound, and whatever the game adds beside them. Home leaves for the address the server sent — taking the whole page, not the iframe — and hides itself while there is none. The rest find their windows, or build them. |
| **[Ui History](Runtime/_System/Ui/History/)** | 1.0.80 | The strip of recent bets, as a row of chips over the game or a column down the side. It feeds itself from the socket, animates each arrival, drops or scrolls the oldest, and opens the bet info dialog on whichever chip is clicked. |
| **[Hotkeys](Runtime/_System/Ui/Hotkeys/)** | 1.0.81 | Keys bound to things the game does, and the dialog that tells the player about them. One line binds a key; the window reads the registry, so there is no list to keep in step. Gated on the player's own `keyboard` setting, which the socket remembers. |

## Drawing and layout

What everything above is made out of. Both drawing components generate their own geometry rather than taking a
sprite per variant, which is why a card, a highlighted card and a warning card are a few fields apart instead of
three more atlas entries.

| | Describes | What it is |
| --- | --- | --- |
| **[Rounded Box](Runtime/_System/Ui/RoundedBox/)** | 1.0.65 | A panel drawn from a generated mesh rather than a sprite: fill, border and rounded corners, with every side and every corner set on its own. Every panel in this package is one. |
| **[Ui Grid](Runtime/_System/Ui/Grid/)** | 1.0.59 | A grid layout for uGUI in the shape of CSS's: tracks that are fixed, a share of the leftover, a percentage or as big as their contents; items that span more than one; and an arrangement you can hand over from code as a single string. |
| **[Sprite Gradient](Runtime/_System/Ui/SpriteGradient/)** | 1.0.78 | A sprite drawn with a gradient through it and a border grown around its silhouette, rather than the one flat tint an Image can give it. One white glyph, badge or coin becomes as many coloured variants as are wanted without another file in the atlas. |

## Scene helpers

Two things that are not UI at all: they hold scene objects against each other, and roll a dice to an answer the
server has already given.

| | Describes | What it is |
| --- | --- | --- |
| **[Transform Constraints](Runtime/_System/Components/Constraints/)** | 1.0.79 | Position, Rotation and Look At: point one at a target and it follows, every frame, while you edit as well as while you play — with a blend knob, per-axis ticks, offsets, and a duration if it should trail rather than stick. |
| **[Dice Roller](Runtime/_System/Components/Dice/)** | 1.0.73 | `Roll(6)` throws the cube, tumbles it, drops it, bounces it, and it comes to rest showing 6 — every time, on any frame rate, with no physics involved. Which is the point: the server has already said the answer. |

## Systems

| | Describes | What it is |
| --- | --- | --- |
| **[Sound](Runtime/_System/Audio/)** | 1.0.82 | Playing a clip with the player's own switches and volumes already applied — one line, no manager in the scene, no mixer. A named bank fills itself from the inspector, music is one clip faded rather than cut, and the four settings behind it are the same ones the web front keeps. |
| **[Translations](Runtime/_System/Translations/)** | 1.0.68 | Every word the player reads, in the language `MainState.Locale` names. No file to load and no initialisation step — the strings are dictionaries compiled into the package. Every caption the package draws goes through it, and a wording of your own survives. |

## Which are current

Every readme carries the package version it was last checked against. The ones below **1.0.82** are the pages to
distrust first if something on them does not match the code.

| Describes | Pages |
| --- | --- |
| **1.0.82** — current | Window, Ui Navbar, Sound |
| 1.0.81 | Hotkeys |
| 1.0.80 | Ui History |
| 1.0.79 | Transform Constraints |
| 1.0.78 | Sprite Gradient |
| 1.0.73 | Dice Roller |
| 1.0.68 | Translations |
| 1.0.65 | Rounded Box |
| 1.0.59 | Ui Grid |

Behind does not mean wrong. It means nobody has re-read that page since that version, so a field added later may
be missing from it. The code is the authority; each readme's **Files** table at the bottom says which sources it
is describing.

## Not documented yet

These have no readme. They are listed so the gap is a known one rather than something to go looking for — the
source is the documentation for now, and the files named are the ones worth opening first.

| | |
| --- | --- |
| [`Socket/`](Runtime/_System/Socket/) | The connection itself. `Emitter` is everything the game sends, `Incoming` everything it receives and where each payload lands in the state. |
| [`States/`](Runtime/_System/States/) | `MainState` is what the server has told us, `MainEvents` is how to hear about it changing, and `StateManager` is the object holding both. |
| [`Navigation/`](Runtime/_System/Navigation/) | `Navigator`: leaving the game for the operator's page, and what an address has to look like to be followed. |
| [`Performance/`](Runtime/_System/Performance/) | The frame rate limiter, the anti-aliasing policy and the FPS counter. |
| [`Ui/PanelToast/`](Runtime/_System/Ui/PanelToast/) | `Toast`: a message that appears over the game and goes away again. |
| [`Ui/Particles/`](Runtime/_System/Ui/Particles/) | `UiParticleRenderer`: particles that draw on a canvas and sort with it. |
| [`Ui/RectTransforms/`](Runtime/_System/Ui/RectTransforms/) | Anchoring, fitting and clamping a rect against another, in 3D as well as 2D. |
| [`Components/Bounds/`](Runtime/_System/Components/Bounds/) | Measuring a hierarchy's bounds, and fitting something to them. |
| [`Dto/`](Runtime/_System/Dto/), [`DtoMulti/`](Runtime/_System/DtoMulti/), [`Enums/`](Runtime/_System/Enums/) | The shapes on the wire. Read alongside `Socket/Incoming.cs`. |

## How these read

Every page in the set is laid out the same way, so a component you have not met before is navigated the same as
one you have:

| | |
| --- | --- |
| **The lede** | What the thing is, and the one-line version of how to make one. Enough to decide whether to keep reading. |
| **A drawn specimen** | The component itself, built out of HTML and CSS rather than screenshotted — so it stays right when the colours change and costs no image to store. **In the `.html` copy only**, for the reason at the top of this page. |
| **Quick start** | The menu path, the component, and the smallest useful code. |
| **The middle** | One section per thing it does, with the reasoning where a choice is not obvious. This is the part worth reading before changing a field you have not used. |
| **In the editor** | What happens outside play mode — several of these components draw sample data so they can be styled with nothing running. |
| **From code** | The public surface as a table. What to skim when you know what you want. |
| **Worth knowing** | The things that surprise people. Read it before filing a bug. |
| **Files** | Every source file the page describes, and what each is for. |

Two copies of each page are kept deliberately: `README.md` for reading in an editor or on a repository page, and
`README.html` for reading in a browser with the component drawn. They carry the same content and are updated
together, in the same edit as the code they describe.

**Every page links back to this one** — under the heading in the Markdown copy, top of the contents rail in the
browser copy — so the set is walkable in both directions from wherever you landed. Each format links within
itself: `.md` back to `README.md`, `.html` back to `README.html`.

A readme added later belongs in three places: a row in the section above, a row in
[Which are current](#which-are-current), and a back-link of its own pointing here. The two forms, with as many
`../` as the folder is deep — four from anything under `Ui/` or `Components/`, three from `Translations/` or
`Audio/`:

```
README.md    [← All documentation](../../../../)               directly under the # heading
README.html  <a class="home" href="../../../../README.html">   first entry inside <nav class="toc">
```

**Markdown links point at the folder, never at its `README.md`.** GitHub and GitLab both render a folder's
readme underneath its file listing, so `Runtime/_System/Ui/Window/` lands on the rendered page — the `/tree/`
view — while `Runtime/_System/Ui/Window/README.md` lands on the plain file view, `/blob/`. The HTML copies are
the exception and keep their `README.html` filenames: they are opened from disk, where a folder href is a
directory listing rather than a page.
