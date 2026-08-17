# Sprite Gradient

[← All documentation](../../../../README.md)

A sprite drawn with a gradient painted through it and a border grown around its silhouette, rather than the
one flat tint an Image can give it. The sprite supplies the shape and the detail; everything else is
geometry, so a single white glyph, badge or coin becomes as many coloured variants as are wanted without
another file in the atlas.

*Describes package 1.0.78. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the shapes drawn rather than described.*

**GameObject → UI (Canvas) → FlappyBet → Sprite Gradient**, or Add Component → UI → Sprite Gradient.

It **replaces Image** rather than sitting on top of one. A GameObject has a single CanvasRenderer, and two
Graphics fighting over it end up with one of them not drawn.

## How the border is possible

An Image can be tinted but not outlined, because a sprite is pixels and a border is a path — and nothing in
a sprite says where its edge is. So the edge is found: the alpha channel is sampled onto a grid and the
threshold line through it is traced into closed loops, once per sprite and then kept. Once the silhouette is
a path, a border around it is the same strip of triangles a [Rounded Box](../RoundedBox/README.md) draws
around its own outline, and every way of colouring that strip carries over.

The trace handles more than one loop, so a doughnut, a dotted glyph or a shape with a hole in it gets a
border around every part of itself, holes included.

What comes off a grid is a staircase, and a staircase is the one thing a border cannot survive: offset by
more than the depth of its steps, the strip's own quads fan out and cross each other. So the alpha is
averaged over each sample rather than read from the middle of it — which turns a hard edge into a ramp the
crossing can be read off to a fraction of a texel — and the loop is then corner-cut twice before being
thinned by Douglas–Peucker, which bounds how far the finished outline strays from the real one. On a 200px
circle the result is 32 points, none of them more than half a pixel off.

**No import settings are needed.** `GetPixels` only works on a texture with Read/Write on, which is off by
default, doubles what the texture costs in memory, and cannot be turned on at all for a sprite that arrived
in an asset bundle or was downloaded at runtime — a border that needed it would be a border that usually
did not work. So when the texture will not hand its pixels over, they are taken off the GPU instead: the
sprite's corner of the atlas is blitted into a render target, which decompresses it and puts it somewhere
`ReadPixels` can reach. That works for any texture the renderer can draw at all, and costs one stall, once
per sprite.

The one sprite this cannot read is one **rotated inside an atlas**, whose pixels no longer line up with the
rect being drawn. There the border falls back to the outline the importer generated — which for a **Full
Rect** sprite is the plain quad, so the border comes back as a rectangle. Untick *Allow Rotation* on the
atlas, or set **Mesh Type** to **Tight** for a rougher outline as things stand. The inspector says so when
it applies.

## The inspector

| Field | What it does |
| --- | --- |
| Sprite | The picture. Its alpha is the shape. |
| Preserve Aspect | Fits the sprite in the rect at its own proportions. The border follows what it ends up filling. |
| Fill Color | Painted through the sprite. With a gradient running it becomes a tint over it — white leaves the gradient alone, and its alpha fades the whole thing. |
| Gradient | `None`, `Linear` (at an angle) or `Radial` (middle to corners). |
| Angle | 0 left to right, 90 bottom to top, 180 right to left. It always spans the sprite, so the ends stay on its edges at any angle. |
| Border Size | Thickness, in the rect's own units. 0 leaves the sprite alone. |
| Placement | `Outside` grows off the silhouette and leaves the picture whole. `Inside` eats into its edge and keeps the shape its size. `Center` splits the difference. |
| Border Gradient | `Linear` across the sprite, `Around` the outline, or `Frame` across the thickness. Replaces the border colour. |
| Alpha Threshold | How much alpha counts as part of the shape. Lower catches more of a soft edge and puts the border further out; higher hugs the solid core. |
| Simplify | How far the outline may stray from the traced one, as a fraction of the sprite. Up means fewer vertices and a looser border. It is floored at half a sample either way — nothing finer than the grid it was read off can be true. |
| Edge Softness | The fade that smooths the border; the mesh has no anti-aliasing of its own. About a pixel is right. |

### Why the border does not fold over itself

Moving every point of an outline outwards is not the same thing as offsetting it, and the difference is
all folds — the border painted twice over the same ground, which shows as a spike off a sharp corner or a
patch of double-strength colour in a notch. `SpriteBorderPath` answers three ways of folding, in order:

| | |
| --- | --- |
| **Corners** | A corner turning away from the border is rounded once a mitre would reach more than twice the width; a mitre on something sharp *is* the spike. A corner turning towards it keeps the mitre at any angle — there the mitre is not a corner treatment but exactly where the two offset edges cross. |
| **Reach** | No point may end up nearer to another part of the outline than the border is wide, which is what going past the middle of a narrow place means. Points that do are pulled back along the line they were pushed out on, so the two sides of a slot come to rest against each other. |
| **Folds** | Edge that genuinely crosses itself has everything between the crossing segments collapsed onto the crossing point, so what it covered twice is covered once by a fan from there. Which side is the fold is settled by winding, since a fold and a hole look identical from close up: a border thick enough to bridge a C's mouth turns it into a ring, and a ring's inside is a hole, not a mistake. |

Measured two ways — sampling the plane to count how many of the border's own triangles sit on each point,
and walking outward from every point of the outline to see how far the border actually reaches — a C, a
star (outside or centred) and a circle come out at **0% double coverage and a uniform 0.99× width**, at
border widths from a hairline up to half the shape.

**A border does come out thinner inside a sharp notch**, and there is no version of this that does not. A
notch narrower than twice the border has nowhere to put the whole of it: the two sides meet in the middle
and the border tapers to the point. Widen the notch, or narrow the border, or accept it — the only other
answer is to let the two sides run through each other, which is the border painted twice.

It is not a true polygon offset — that would mean rebuilding the border as an area and unioning it with
itself — so the tips of a nearly closed crescent still overlap by a few percent.

### What the border is drawn through

The border is drawn through the sprite's own texture, like everything else in this mesh — a UI graphic has
one texture and no say in the matter. So the border is only as opaque as whatever it lands on, and it lands
on the edge of the picture where the alpha is on its way out, or outside it where there is none at all.
Drawn where it sits it would come out in pieces: solid only where the outline happened to cross something
opaque, and gone everywhere else.

So the border keeps its own position and reads the texture from one fixed spot instead. The trace already
knows where the picture is solid, so it comes back with a place that is fully opaque, has opaque
neighbours, and is as close to white as the sprite gets. Every border vertex reads from there, and what
that spot does hold is divided back out of the vertex colour — so the border comes out exactly the colour
it was asked for, even on a picture that is nowhere near white. A spot darker than the colour wanted cannot
give it: the correction clamps, and the border comes out as bright as that part of the picture allows.

For a sprite that could not be traced at all, the middle of the sprite is used and nothing is corrected
for. It is a guess, but a better one than any point on the outline.

## From code

```csharp
using FlappyTemplate;

SpriteGradientBuilder.Create(panel, "Coin")
    .Picture(coinSprite, true)
    .Size(96f, 96f)
    .Fill(gold, 90f)
    .Border(3f, new Color(0.35f, 0.2f, 0f))
    .Done();

// Or reconfigure one that already exists
SpriteGradientBuilder.For(badge)
    .Fill(Color.red)
    .Border(4f, ramp, EBorderGradient.Around);
```

| Step | Notes |
| --- | --- |
| `Create(parent, name)` / `For(graphic)` | Start. `Create` adds the RectTransform and CanvasRenderer with it. |
| `Picture(sprite, preserveAspect)` | The sprite, and whether to fit it rather than stretch it. |
| `Size(w, h)` · `NativeSize()` · `Stretch(margin)` · `At(pos)` | Layout. |
| `Fill(color)` | Flat. Clears any gradient. |
| `Fill(gradient, angle)` · `RadialFill(gradient)` | 0° is left to right, 90° bottom to top. |
| `FillTint(color)` | Multiplies without replacing — the way to fade or dim a gradient. |
| `Border(size, color)` · `Border(size, gradient, mode, angle)` · `NoBorder()` | `Linear`, `Around` or `Frame`. |
| `Placement(mode)` | `Outside`, `Center` or `Inside`. |
| `Trace(alphaThreshold, simplify)` · `Softness(px)` | How the silhouette is read and drawn. |
| `Raycast(bool)` · `Tint(color)` · `Material(m)` | |
| `Done()` | Returns the graphic. There is an implicit conversion too. |

### Recipes

```csharp
// One white glyph, three states — no second atlas entry for any of them
.Picture(icon).Fill(idle)
.Picture(icon).Fill(hot).Border(2f, Color.white)
.Picture(icon).FillTint(new Color(.4f, .4f, .45f))     // disabled

// Metal coin: a ramp through the sprite, a darker one around its edge
.Picture(coin, true).Fill(gold, 90f).Border(4f, edge, EBorderGradient.Frame)

// Sticker: a thick white keyline grown outward, the picture untouched
.Picture(art, true).Border(8f, Color.white).Softness(1.5f)

// Rim light that travels: one ramp run around the outline
.Picture(shape, true).Fill(dark).Border(5f, rim, EBorderGradient.Around)

// Inked edge — grown inward, so the shape keeps the size the layout gave it
.Picture(shape, true).Fill(ramp, 90f)
.Border(3f, ink).Placement(ESpriteBorderPlacement.Inside)

// Glow behind a shape: a wide, soft, half-transparent border
.Picture(shape, true).Border(14f, new Color(1f, .8f, .2f, .35f)).Softness(6f)
```

`SpriteGradientExample` builds one of each as a grid — drop it on an empty RectTransform in a canvas and
press play, or use **Build Now** from its context menu. With no sprite handed to it, it paints a star and a
ring of its own, so it runs against nothing.

Individual properties are public as well (`Sprite`, `FillColor`, `BorderSize`, `BorderPlacement` …), plus
`SetNativeSize`, `GetBorderPath` for the outline in local space, and `CanTraceOutline` for whether the real
silhouette is being used or the importer's mesh is standing in. Each setter rebuilds only when the value
actually changes, so driving one from a tween is safe.

## Worth knowing

- **Tint** is the standard `Graphic.color`. It multiplies the fill and the border alike — leave it white
  unless a fade, an Animator or a CanvasGroup is driving it.
- Tracing is done **once per sprite** and kept, so the cost — a GPU readback and a march over the alpha —
  lands on the first frame a sprite is drawn with a border rather than on every rebuild. Changing Alpha
  Threshold or Simplify is a new trace; changing size, colour or thickness is not. Editing anything in the
  inspector drops the cache, since a sprite can be reimported under us.
- Gradients cost geometry. Vertex colours only blend in a straight line, so the sprite is cut at every stop
  and the border reads its ramp at each outline point; a two-key ramp is nearly free, a dozen keys is not.
  A `Linear` or `Around` border with a key in the middle also has the outline broken up finely enough for
  that key to land on, which roughly doubles the border's vertices — the other modes do not pay it.
- An outline is capped at 96 points however low **Simplify** is set. Every point is four vertices of border
  and four of fade, and a silhouette with a lot of coastline — lettering, fur, a torn edge — would
  otherwise hand the canvas a mesh nobody budgeted for.
- The border is **offset rather than pushed out**, so it does not fold over itself — see below. What is
  left is that two convex corners facing each other across a gap narrower than the border, the tips of a
  nearly closed crescent, still overlap a little.
- **Around** starts wherever the trace started on each loop. It is a corner of the shape, but not one
  anybody chose — turn the ramp's keys around rather than looking for an origin field.
- A sprite **rotated inside an atlas** is the one thing that cannot be traced; its pixels no longer line up
  with the rect being drawn. Untick *Allow Rotation* on the atlas if it matters.
- Raycasting is by the rect, as with any Graphic — the traced silhouette is not used for hit testing.

## Files

| File | |
| --- | --- |
| `SpriteGradient.cs` | The component and its mesh. |
| `SpriteOutline.cs` | Traces the silhouette and keeps it. Internal. |
| `SpriteBorderPath.cs` | Offsets that outline into the two edges of a border, without folds. Internal. |
| `SpriteGradientBuilder.cs` | The fluent API. |
| `SpriteGradientExample.cs` | One of each, built from code, on a sprite it paints itself. |
| `ESpriteBorderPlacement.cs` | Outside, Center, Inside. |
| `../RoundedBox/EFillGradient.cs`, `EBorderGradient.cs` | Gradient modes, shared with Rounded Box. |
| `Editor/SpriteGradient/` | The inspector and the GameObject menu. |
