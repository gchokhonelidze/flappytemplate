# Ripple

[← All documentation](../../../../)

A wave round a control when it is pressed. A ring starts on the control's own outline, runs outwards and fades,
with a soft glow inside it, while the control itself dips a little and springs back — so the thing under the
player's finger answers before anything else does.

```csharp
UiRipple.On(button.gameObject);                  // a wave round the button on every press
UiRipple.On(slider.gameObject, handle).Play();   // round the handle, once, now
```

**Add Component → UI → Ripple** on anything that can be pressed, or `UiRipple.On` from code. That is the whole of
the usual case: Play On Press is on, and the wave goes out whenever the object takes a left-button press.

*Describes package 1.0.94. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the rings drawn.*

## What it draws

| | |
| --- | --- |
| **The ring** | The control's own shape grown outwards. The corner radius is read off the control's [`RoundedBox`](../RoundedBox/), so a pill stays a pill and a circle a circle; a plain `Image` has square corners and gets a square ring. It sets off at **Thickness** and thins to a quarter of that as it travels **Spread** past the edge, easing out — fast off the edge, slowing as it fades — over **Duration**. |
| **The glow** | A soft fill inside the ring, **Glow** of the ring's own alpha, fading faster than the ring does. Nought leaves a bare ring. |
| **More rings** | **Rings** per wave, one to three, each setting off **Stagger** seconds after the one before. |
| **The dip** | The control shrinks by **Punch** of its size and springs back, over the first half of the wave — over by the time the ring is properly on its way, so the control is back where the eye left it. |

**The ring is drawn behind the control, not across it.** It is a sibling placed just before the target in the
hierarchy, so it comes out from under the control's edge. It carries a `LayoutElement` with **Ignore Layout** on,
the same way a window's close button does, so a [grid](../Grid/) or a layout group does not take it for one more
cell and shuffle everything beside the control on every press.

```
Card
├ Caption
├ Ripple      a ring: a RoundedBox just before the control · Ignore Layout · one per ring, made on the first press
└ Switch      the control, with the Ui Ripple on it
```

## Fields

| Field | Default | What it does |
| --- | --- | --- |
| Target | empty | What the wave runs round. Empty is the object the ripple is on. |
| Play On Press | on | Start a wave whenever this object is pressed. Off leaves it to `Play` — see [Answering what it did](#answering-what-it-did). |
| Color | white, 55% | The ring and the glow inside it. Its alpha is where the wave starts; it fades from there to nothing. |
| Spread | 14 | How far past the control's edge the wave runs before it is gone. |
| Duration | 0.6 | Seconds from the press to the last ring fading out. |
| Thickness | 3 | Width of the ring as it sets off. It thins as it travels. |
| Glow | 0.35 | The fill inside the ring, as a share of the ring's own alpha. |
| Rings | 2 | Rings per wave, one to three. |
| Stagger | 0.12 | Seconds between one ring of a wave and the next. |
| Punch | 0.06 | How far the control dips, as a share of its size. Nought keeps it still. |

A press is a **left-button** press — or a touch — on the object or anything under it that does not catch the
press itself. A `Selectable` on the same object that is **not interactable** sends no wave: a wave off a
greyed-out button would say the press did something.

## From code

| | |
| --- | --- |
| `UiRipple.On(gameObject, target)` | Puts a ripple on an object, or hands back the one already there. The target, when given, is what the wave runs round instead of the object itself. |
| `Play()` | Sends a wave out in the ripple's own colour. |
| `Play(color)` | Sends one in a colour of its own — a switch's on colour, say. The alpha is where it starts. |
| `Stop()` | Takes every ring down at once and puts the control back at its own size. Called for you when the ripple is switched off or destroyed. |
| `Target` · `PlayOnPress` · `Color` · `Spread` · `Duration` · `Punch` | The fields above, from code. |

```csharp
// One wide, slow ring and a button that stays still
var wide = UiRipple.On(round.gameObject);
wide.Spread = 40f;
wide.Duration = 1.1f;
wide.Punch = 0f;
wide.Color = new Color(0.98f, 0.72f, 0.2f, 0.8f);
```

### Answering what it did

Play On Press answers the press. A **switch** wants to answer what the press *did* — and to answer a hotkey, or a
setting arriving from another tab, the same way. So turn Play On Press off and call `Play` from wherever the
switch is flipped, in the colour of where it is going:

```csharp
var wave = UiRipple.On(pill.gameObject);
wave.PlayOnPress = false;

void Flip()
{
    on = !on;
    pill.FillColor = on ? green : grey;
    wave.Play(on ? green : new Color(1f, 1f, 1f, 0.5f));   // green going on, white-grey going off
}
```

Each wave keeps its own colour, so a green one on its way out is not repainted grey by the press that follows it.

## Where the template uses it

| Where | How |
| --- | --- |
| Every [window's](../Window/) close button | A ripple on the `Close` object, at its own defaults. The window's **Close Wave** switches it on and off, and puts one on a window built before there was such a thing. Its colour and reach are the ripple's own from then on — select `Close` to change them. |
| Every [navbar](../Navbar/) button | Set from the bar's style — **Waves**, **Wave Color**, **Wave Spread**, **Wave Duration** — on every hook, so a style changed from code reaches buttons that already exist. |
| The [sound window's](../Window/#sound) switches | Play On Press off, and played as the channel flips: green going on, white-grey going off. |
| The sound window's slider handles | On the rail, round the handle, the moment it is taken hold of. Green, from the slider's fill colour. |

## Worth knowing

- **A ring is clipped by whatever clips the control.** It is a sibling of the control, so a `RectMask2D` or a
  `Mask` above the control cuts the ring off at the same edge — a square edge, however round the ring. Inside a
  window's body that edge is the **Content Padding**: the body's `Clip` masks there. That is why the sound
  window holds its switch's spread to the card's padding, which is all the room there is before the clip; the
  close button sits outside the body and has no such limit. Keep **Spread** within the room around the control,
  or put the control somewhere nothing clips it.
- **Rings are only made in play mode.** They are scene objects, so a switch flipped from an editor script would
  otherwise leave rings saved into the scene. None of them is serialized, and they go with the scene.
- **It runs on unscaled time**, so a wave still plays over a paused game — the same as a window opening.
- **The dip is the target's `localScale`.** The size it rests at is taken only while it is resting, so a second
  press mid-dip springs back to the real size rather than to the dip. Something else animating the same scale —
  a tween of the game's own — will fight it; set **Punch** to nought on that control.
- **The rings are pooled.** A ring that has finished is switched off and kept for the next wave, rather than
  destroyed and made again. They are destroyed with the ripple — siblings do not go with it on their own.
- **A target with no parent draws nothing.** The ring has to be a sibling, and a root has none.

## Files

| File | |
| --- | --- |
| `UiRipple.cs` | The component: the press, the rings and the dip. |
| `UiRippleExample.cs` | A button that waves on every press, a switch that waves when it flips, and a round button with one wide, slow ring and no dip. Drop it on an empty RectTransform in a canvas and press play. |
| `../Window/UiWindow.cs` | **Close Wave**: a ripple on every window's close button. |
| `../Window/Sound/SoundWindow.cs` | The switch and slider waves. |
| `../Navbar/UiNavbar.cs` | A ripple on every navbar button, set from the style. |
| `../RoundedBox/` | What a ring is drawn with, and where the control's corner radius is read from. |
