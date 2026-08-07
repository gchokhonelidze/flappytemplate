# Rounded Box

A panel drawn from a generated mesh rather than a sprite: a fill, a border and rounded corners, with every
side and every corner set on its own. It replaces the usual round-rect workflow of exporting a 9-sliced
image per variant — a card, a highlighted card and a warning card differ only in border colour and radius,
and here that is a few fields instead of three more atlas entries. The corners are geometry, not pixels, so
they stay clean at any size the layout ends up at.

*Describes package 1.0.43. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the shapes drawn rather than described.*

**GameObject → UI (Canvas) → Rounded Box**, or Add Component → UI → Rounded Box.

## The inspector

The fields are laid out on a live picture of the box: each radius sits on the corner it rounds, each
thickness and swatch against the side it thickens. The preview is drawn from the real RectTransform's
proportions, so the percentage in the header tells you why a 2 unit border reads as a hairline.

| Field | What it does |
| --- | --- |
| Fill Color | The interior. Alpha is honoured, so a transparent fill leaves an outline on its own. With a gradient running it becomes a tint over that instead. |
| Gradient | `None`, `Linear` (at an angle) or `Radial` (middle to edge, following the shape rather than a circle). |
| Border | Thickness and colour per side. Sides blend into each other across the corner between them. |
| Border Gradient | `Linear` across the box, `Around` the outline, or `Frame` across the thickness of each side. Replaces the four side colours. |
| Corners | A radius each. **Negative scoops the corner out** — a bite taken from it rather than a round taken off it. |
| Corner Segments | Straight pieces per arc. Raise for corners that are large on screen. |
| Edge Softness | The fade that smooths the arcs; the mesh has no anti-aliasing of its own. About a pixel is right. |
| Link Sides / Link Corners | Type one value, all four follow. The four fields stay live either way. |

## From code

`RoundedBoxBuilder` says in one chain what would otherwise be a dozen assignments. It is a struct holding
one reference, so a chain allocates nothing, and every step is skipped rather than thrown if the box has
been destroyed.

```csharp
using FlappyTemplate;

// Build a new one under a parent
RoundedBoxBuilder.Create(panel, "Card")
    .Size(280f, 160f)
    .Fill(Color.white)
    .Corners(18f)
    .Border(2f, new Color(0.85f, 0.85f, 0.9f))
    .Done();

// Or reconfigure one that already exists
RoundedBoxBuilder.For(card)
    .Fill(Color.red)
    .Border(3f, Color.white);
```

| Step | Notes |
| --- | --- |
| `Create(parent, name)` / `For(box)` | Start. `Create` adds the RectTransform and CanvasRenderer with it. |
| `Size(w, h)` · `Size(v)` · `Stretch(margin)` · `At(pos)` | Layout. |
| `Fill(color)` | Flat. Clears any gradient. |
| `Fill(gradient, angle)` · `RadialFill(gradient)` | 0° is left to right, 90° bottom to top. |
| `FillTint(color)` | Multiplies without replacing — the way to fade a gradient. |
| `Border(size, color)` · `Border(l, t, r, b)` · `BorderColors(l, t, r, b)` · `NoBorder()` | |
| `BorderGradient(gradient, mode, angle)` | `Linear`, `Around` or `Frame`. |
| `Corners(radius)` · `Corners(tl, tr, br, bl)` · `Scoop(size)` · `Pill()` | `Pill` is a radius larger than the box, held to it. |
| `Softness(px)` · `Segments(n)` · `Raycast(bool)` · `Tint(color)` · `Material(m)` | |
| `Masked(showBox)` | Clips children to the shape. Read the masking notes below first. |
| `Done()` | Returns the box. There is an implicit conversion too. |

Some recipes:

```csharp
.Fill(Color.clear).Border(2f, accent)      // outline only
.Corners(18f, 18f, 0f, 0f)                 // tab: rounded on top, square below
.Scoop(20f)                                // ticket: corners bitten out
.RadialFill(ramp).Pill()                   // capsule with the ramp following the shape
.Border(12f, Color.white)
.BorderGradient(ramp, EBorderGradient.Frame)   // picture-frame moulding
```

`RoundedBoxExample` builds one of each as a row — drop it on an empty RectTransform in a canvas and press
play, or use **Build Now** from its context menu.

Individual properties are public as well (`FillColor`, `BorderLeft`, `RadiusTopLeft`, `CornerSegments` …),
plus `SetBorderSize`, `SetBorderColor` and `SetCornerRadius` for the all-at-once case. Each setter rebuilds
only when the value actually changes, so driving one from a tween is safe.

## A picture inside the box

**Add Masked Image** builds the whole arrangement:

```
Rounded Box     RoundedBox + Mask
├── Image       stretched, clipped to the rounded corners
└── Border      RoundedBox + RoundedBoxBorderOverlay — drawn last, so it sits over the image
```

The overlay exists because of draw order: a canvas draws a graphic before its own children, so a picture
parented to the box lands on top of the border the box just drew. The overlay repeats the border as the
last child. Nothing on it is authored — it copies the parent's border, corners and gradient every frame, so
the border stays one set of fields on the box.

Two things to know about masking:

- A Mask cuts its shape from wherever the mesh has alpha, so **a transparent fill cuts nothing** and hides
  every child. With *Show Mask Graphic* off the fill is forced opaque for you; with it on, the inspector
  warns instead.
- Masked boxes have **hard edges**. The soft edge is dropped while a Mask is enabled, because the fade
  would widen the hole and let the picture show past the border. Stencils are on or off per pixel; no
  anti-aliasing survives one.

## Particles

**Add Border Particles** adds a fire, sized to the box, that follows its shape. The components live in
`../Particles/`.

- `RoundedBoxBorderShape` turns the box's outline into a mesh for the particle system's shape module.
  **Area** picks `Border`, `Fill` or `Inside`. Sides with no thickness are left out of the border mesh
  entirely — a border of nothing emits nothing.
- **Direction** (`Outward`, `Inward`, `Around`) is written into that mesh as its normals, which is where a
  particle system takes a starting direction from. It only points particles; nothing moves until **Start
  Speed** is above zero.
- `UiParticleRenderer` draws the particles as a UI mesh instead of letting the particle system draw them. A
  particle system is a renderer and a UI element is not, so the two cannot be sorted against each other
  inside a canvas — an overlay canvas covers them whatever the sorting layers say. Drawn as UI they sort by
  hierarchy position like anything else, and get clipped by any mask they are under.

Cost is a canvas rebuild per frame while particles are alive, which scales with everything in that canvas
rather than just the effect. Put busy effects under a nested `Canvas` to keep the rebuild local. An idle
system costs one integer read per frame.

## Worth knowing

- **Tint** is the standard `Graphic.color`. It multiplies the fill and every border colour — leave it white
  unless a fade, an Animator or a CanvasGroup is driving it.
- Radii that would overlap on an edge are scaled down together, so a radius larger than the box gives a
  pill rather than a folded-over shape. Sizes are measured, not signs: a scoop collides the same way.
- The inspector preview is analytic, so it always shows smooth corners — it will not show a **Corner
  Segments** value set too low for a large radius. The scene view will.
- Gradients cost geometry. Vertex colours only blend in a straight line, so the shape is cut at every stop;
  a two-key ramp is free, a dozen keys is not.

## Files

| File | |
| --- | --- |
| `RoundedBox.cs` | The component and its mesh. |
| `RoundedBoxBuilder.cs` | The fluent API. |
| `RoundedBoxBorderOverlay.cs` | Repeats the border above masked content. |
| `RoundedBoxExample.cs` | One of each kind of box, built from code. |
| `EFillGradient.cs`, `EBorderGradient.cs` | Gradient modes. |
| `../Particles/` | `RoundedBoxBorderShape`, `UiParticleRenderer` and their enums. |
| `Editor/RoundedBox/` | The inspector and the GameObject menu. |
