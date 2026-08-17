# Dice Roller

[← All documentation](../../../../README.md)

A dice that rolls to a number you decided beforehand. `Roll(6)` throws the cube in the air, tumbles it,
drops it, bounces it, and it comes to rest showing 6 — every time, on any frame rate, with no physics
involved and nothing to settle down afterwards.

That is the whole point of it. A rigidbody dice is a lovely thing until the server has already said the
answer is 6, and then it is a simulation you have to fight. Here the animation is a DOTween sequence built
around the answer, so the throw is free to be as showy as you like without ever putting the wrong face up.

*Describes package 1.0.73. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the flight drawn rather than described.*

**Add Component → FlappyBet → Dice Roller**, on the cube that carries the dice mesh. Then:

```csharp
dice.Roll(6);                      // jump, tumble, fall, bounce - lands on 6
dice.Roll(6, 2f);                  // the same, hanging in the air for 2s first
dice.Roll(6, () => Pay(6));        // told when it has settled
dice.SetFace(1);                   // no animation, just put 1 up
```

---

## The flight

One sequence, four stretches, in this order:

| Stretch | What happens | Timing |
| --- | --- | --- |
| **Rise** | Up to the apex, easing out — and part of the way to where it lands | 45% of `Jump Duration` |
| **Hover** | Hangs at the apex, still turning, drifting gently up and down | `Hover Duration`, 0 by default |
| **Fall** | Down onto the landing spot, easing in, spin decelerating | the other 55% |
| **Bounces** | Each keeps `Bounce Damping` of the last one's height | added on the end |

Rise and fall are separate tweens with opposite eases — `OutQuad` up, `InQuad` down — which is what makes
the arc read as gravity rather than as a lerp. A bounce's duration falls with the **square root** of its
height, which is how long a real bounce of that height lasts; scaling it linearly makes the small late
bounces crawl.

The hover simply pushes the fall later on the timeline. Neither half of the arc changes shape because of it,
and horizontal travel is split around it — the share of the ground covered on the way up, the rest on the
way down — so a hovering dice hangs over one spot instead of sliding sideways through the air.

## Why it always lands right

The spin is one continuous tumble from wherever the dice is standing to the orientation that puts the asked
for face up, plus a random number of **whole extra turns per axis**. `RotateMode.FastBeyond360` makes DOTween
interpolate the raw euler numbers instead of taking the shortest arc, so those extra turns actually play out
as tumbling — and because a whole turn leaves an orientation exactly where it found it, the dice still ends
on the face it was told to. Add fifteen turns for a long hover and the guarantee is untouched.

Two more things fall out of that:

- **Yaw is free.** Turning about the up axis cannot change which face is up, so each roll picks one of the
  four quarter turns at random. The dice comes to rest at a different angle every time and still reads as
  the same number.
- **The last quarter turn rides the first bounce.** With **Tumble On Landing** ticked the spin stops one
  quarter turn short and the dice rolls over an edge onto its final face as it lands, instead of arriving
  already flat.

When the sequence finishes, the resting rotation is *assigned* rather than tweened to — a tween chain ends a
fraction of a degree off, and a dice resting 0.3° over reads as one that did not quite settle.

## Faces

Which face is which is a table, because it depends on the mesh:

| Value | Face | Local euler |
| --- | --- | --- |
| 1 | +Y (up) | `0, 0, 0` |
| 2 | +Z (forward) | `-90, 0, 0` |
| 3 | +X (right) | `0, 0, 90` |
| 4 | −X (left) | `0, 0, -90` |
| 5 | −Z (back) | `90, 0, 0` |
| 6 | −Y (down) | `180, 0, 0` |

Those defaults are the usual Unity cube layout, which is what a standard dice texture atlas gives you. If
your pips sit elsewhere, re-point the six entries in **Face Orientations** — nothing else in the component
assumes a layout.

To check them, press **A** with [`DiceRollerExample`](DiceRollerExample.cs) on the object: it rolls 1
through 6 in order and logs each. Whichever one comes up wrong is the entry to fix.

## Hover

`Hover Duration` is **0 out of the box**, and while it is 0 the three fields under it do nothing at all —
there is no time at the apex for them to act on. Set it, or pass one per roll with `Roll(6, 2f)`, which
overrides the inspector for that throw.

| Field | Means |
| --- | --- |
| `Hover Duration` | Seconds at the apex. The default for `Roll(value)` calls. |
| `Hover Spin Multiplier` | How much **faster** it spins up there. 1 = the speed of the rest of the jump, 3 = three times faster, 0 = it stops turning. |
| `Hover Bob Height` | How far it drifts up and down while hanging — cosmetic. Scale it against `Jump Height`: at 2 units of jump, 0.15 is a slight float and 0.5 is obvious. |
| `Hover Bob Period` | Seconds per drift up and back down. |

The multiplier is there because a hover would otherwise make the dice spin *slower*: the same turn count
spread over more seconds. Hovering time is weighted by it when the turn count is worked out, so turning the
knob up buys extra turns rather than stretching the same ones thinner. The bob always runs an even number
of legs, so it finishes back at the apex — the exact height the fall starts from.

> **Try** Hover Duration `2`, Hover Spin Multiplier `4`, Hover Bob Height `0.5`: a dice that leaps up, spins
> hard in place while floating, then drops onto its face.

## Jumping

**Jump Enabled** off, there is no arc and no bounces — the dice spins where it stands and settles on the
value. The same call site serves a showy throw and a quiet re-roll in a tight layout:

```csharp
dice.JumpEnabled = false;
dice.ToggleJump();          // straight onto a UI Toggle
```

It is read when a roll *starts*, so flipping it mid-flight never cuts a running animation short. A hover
needs air to hang in, so with the jump off it is ignored.

## Everything local

Every tween here is `DOLocalMove*` / `DOLocalRotate`, and the yaw and tumble quaternions are pre-multiplied
onto `localRotation`, which turns them about the **parent's** axes rather than the scene's. "The face points
up" means up as the parent sees it — tilt, spin or scale the parent and the whole roll follows it. There is
no axis in the component left pointing at the world, which is what lets a dice sit on a board that is itself
being moved about.

## The inspector

| Field | Means |
| --- | --- |
| `Face Orientations` | The six values and the local euler that puts each one up |
| `Jump Enabled` | The arc and the bounces, on or off |
| `Jump Height` | Apex above the higher of take-off and landing, in local units |
| `Jump Duration` | Rise plus fall. Hover and bounces are added on top |
| `Landing Offset` | Where it lands, relative to where it started the scene |
| `Landing Scatter` | Random horizontal spread around that, so repeated rolls do not stack |
| `Hover …` | [Above](#hover) |
| `Bounce Count`, `Bounce Damping` | How many bounces and how much height each keeps |
| `Spin Turns` | Whole extra turns per axis, picked at random in this range |
| `Tumble On Landing` | Land a quarter turn short and roll onto the face |
| `On Roll Complete` | `UnityEvent<int>`, given the value it settled on |
| `Test Value` | What the component's **Test Roll** context menu item throws |

## From code

| Member | Does |
| --- | --- |
| `Roll(value, onComplete = null)` | Rolls, hovering for whatever the inspector says. Returns the `Tween` |
| `Roll(value, hover, onComplete = null)` | The same with the hover given here instead |
| `SetFace(value)` | Puts a face up with no animation |
| `IsRolling` | Whether a roll is playing |
| `JumpEnabled`, `ToggleJump()` | The jump |
| `HoverDuration`, `HoverSpinMultiplier` | The hover, at runtime |

```csharp
// Guard a re-roll, and pay out on the way down
if (!dice.IsRolling)
    dice.Roll(serverValue, 2f, () => wallet.Add(payouts[serverValue]));
```

An unconfigured value logs an error and returns `null` rather than rolling to something arbitrary.

## Notes

- **A roll cancels the one before it.** `Roll` kills the running sequence, so a spammed button re-throws
  rather than stacking tweens on one transform.
- **Disabling mid-flight completes the roll** rather than freezing the dice in the air showing a face nobody
  asked for. That is `Kill(true)` in `OnDisable`; drop the argument if you would rather it hang there.
- **The dice never walks off the table.** Landing is worked out from the position it held at `Awake`, not
  from where the last roll left it, so a hundred rolls with a `Landing Offset` all land in the same place.
- **`Landing Scatter` is horizontal only** — the dice always comes down to the height it started at.
- **`OnValidate` clamps** durations and periods, because a zero or negative one reads as a broken roll
  rather than as "off".

## Files

| File | What |
| --- | --- |
| `DiceRoller.cs` | The component: the sequence, the faces, the guarantee |
| `DiceRollerExample.cs` | Keys 1–6, Space, H, J and A driving all of it |
| `README.html` | This, laid out for a browser, with the flight drawn |
