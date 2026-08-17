# Transform Constraints

Three components that hold one scene object against others: **Position Constraint**, **Rotation
Constraint** and **Look At Constraint**. Point one at a target and it follows — every frame, while you edit
as well as while you play, with a blend knob, per-axis ticks, offsets, and a duration if it should trail
rather than stick.

They exist so the ten-line follower script nobody ever quite finishes — the one that needs an offset by
Tuesday, only the x axis by Wednesday, and a bit of lag by Friday — is a component with an inspector
instead.

*Describes package 1.0.79. Update this file with the code — and **README.html** beside it, which is the same
content laid out for a browser, with the parts drawn rather than described.*

**Add Component → FlappyBet → Position Constraint** (or Rotation, or Look At), drag a target in, done. From
code:

```csharp
var follow = marker.AddComponent<PositionConstraint>();
follow.SetTarget(ball);            // copy the ball's position
follow.Axes = EConstraintAxes.X;   // only the x of it
follow.Offset = Vector3.up * 2f;   // two units above
follow.TweenDuration = 0.3f;       // trailing a third of a second behind
follow.Power = 0.5f;               // and only half the way there
```

---

## The three

| Component | Holds | Reach for it when |
| --- | --- | --- |
| **Position Constraint** | `position` / `localPosition` | A marker rides a peg, a camera trails a ball, a label tracks something across the board |
| **Rotation Constraint** | `rotation` / `localRotation` | A sign leans with the board it is bolted to, two dice tumble as one |
| **Look At Constraint** | `rotation`, worked out from a direction | An arrow, an eye, a spotlight or a camera has to *face* something rather than copy its facing |

All three share one base class, [`TransformConstraint`](TransformConstraint.cs), which owns everything
below this line. A subclass only answers "what value should this transform hold this frame".

Rotation and position are separate channels, so a Position Constraint and a Look At Constraint on the same
object cooperate rather than fight — that pair is the usual follow-cam.

## While you edit

`[ExecuteAlways]`, plus a tick on `EditorApplication.update`, so a constraint is live in the scene view with
no play mode involved: drag the target and the follower comes with it. **Apply In Edit Mode** off confines
it to play mode.

The one thing edit mode does not do is *tween*. DOTween does not run outside play mode, so while editing the
value is always applied instantly, whatever **Tween Duration** says. Nothing in the component touches DOTween
before you press play — which is also why no stray `[DOTween]` object ever turns up in your scene.

## Power

`Power` is a 0–1 knob, 1 by default.

| Power | What happens |
| --- | --- |
| `1` | Fully constrained |
| `0.5` | Halfway between the **rest pose** and the target |
| `0` | **No constraint.** Nothing is written at all |

0 means *off*, not "hold the rest pose": the component stops writing and hands the object back to whatever
else moves it. Any running tween is dropped on the spot rather than allowed to finish.

Part power blends from the rest pose — the object's authored pose, [below](#rest-pose) — and never from where
the object currently stands. Blending from the current pose is the classic mistake: each frame's result
becomes the next frame's starting point, and a half-power constraint quietly walks all the way onto its
target over a handful of frames.

## Axes

`Axes` is a tick per axis. An unticked axis is **not read, not blended and not written** — it is left
entirely to whatever else owns it, an animation, another script, or your own hand in the scene view.

```csharp
constraint.Axes = EConstraintAxes.X | EConstraintAxes.Z;   // follow across the floor, keep my own height
constraint.Axes ^= EConstraintAxes.Y;                       // toggle one
```

For rotation that mask is applied in euler angles, because that is the only frame an "axis" exists in — so
`Axes: Z` on a Look At Constraint is exactly how a 2D arrow spins in the plane of the board and nothing
else. At full power with all three axes ticked the rotation goes through as a quaternion and skips euler
entirely, which is what keeps a fully constrained object from flipping as it passes straight up.

## Absolute or relative

**Worth having, and the two are not interchangeable** — this is the option most likely to be mistaken for a
bug when it is missing.

| Mode | Result |
| --- | --- |
| **Absolute** | Take the target's value. The object jumps onto the target as soon as the constraint runs |
| **Relative** | Keep the gap it had when it started following, and let the target carry it about |

Absolute is what "copy the position" means and is the default, because it is also self-evident: you drag a
target in and the object moves. Relative is what you want the moment the object should stay *where you put
it* — a camera three units back and two up, a sign beside a peg rather than inside it, a hand held near a
bone. Doing that with Absolute means measuring the gap by hand and typing it into **Offset**, which then
stops being right the moment anything moves.

For **Look At**, Relative means "keep the skew you were authored with": an object standing a few degrees off
its target keeps those few degrees while it tracks.

The gap is measured between the [rest pose](#rest-pose) and where the targets stood when the constraint
started following them — on enable, and again after any inspector edit or target change. In play mode that
means the first frame it runs, which is the offset the scene was authored with. `ResampleOffset()` re-takes
it deliberately, after a target has been moved on purpose and the object should trail it from its new place.

## Space

| Space | Reads | Writes |
| --- | --- | --- |
| **World** | `target.position` / `.rotation` | `position` / `rotation` |
| **Local** | `target.localPosition` / `.localRotation` | `localPosition` / `localRotation` |

World is the usual answer: the two objects can sit anywhere in the hierarchy. Local copies the
*relationship* rather than the location — "hold the same place relative to your parent as the target holds
relative to its own" — which is what two scaled or rotated holders carrying a copy of the same layout want.
For siblings under one parent the two are identical.

Space also decides which frame the axis mask works in, and that is the whole of its effect on a Look At
Constraint: aiming is a world-space job either way, but `Local` masks against the parent's axes, which is
how a billboard turns only about the axis its holder calls up.

## Offsets

| Component | Field | Applied |
| --- | --- | --- |
| Position | `Offset` | Added to the constrained position |
| Position | `Offset In Target Space` | On, the offset turns with the target — `(0, 0, -4)` stays four units *behind* something that spins |
| Rotation | `Offset Euler` | After the copy, about this object's own axes |
| Look At | `Aim Offset` | Moves the point being aimed at, in world units — `(0, 1, 0)` looks at the head, not the feet |
| Look At | `Offset Euler` | After the aim — a few degrees leads the thing being tracked instead of sitting dead on it |

## Tween Duration

0 is a hard constraint: the value is simply held, every frame. Above 0 the object trails the target by that
long, which is what turns a rigid follower into a camera.

Behind it is one DOTween tweener that lives as long as the component and is **re-aimed** as the target
moves, rather than a new tween per frame. The retarget snaps the ease's start to wherever the object has got
to (`ChangeEndValue(goal, duration, true)`), which is what makes it read as lag rather than as a stutter.
Rotations use the quaternion shortcuts, so a tweened aim cannot flip as it passes overhead.

`Ease` shapes the trailing move: `OutQuad` reads as weight, `Linear` reads as machinery.

Editing `Tween Duration` or `Ease` — in the inspector or through the property — drops the live tween so the
next frame builds one with the new timing.

## Rest pose

The object's authored **local** pose, captured the moment the component is added. Two things need it: Power
blends from it, and Relative measures its gap from it. It is stored as a local pose and converted on
demand, so it stays correct under a parent that moves, turns or scales.

It sits in a foldout at the bottom of the inspector, with a **Capture** and a **Put Object Back** button, and
it is on the inspector at all rather than hidden because of one case: a **prefab instance** inherits the
prefab's rest pose. Place one somewhere else and its rest pose still points at where the prefab was authored
— invisible at Power 1 in Absolute mode, and obvious at anything else. Capture is the fix.

The component's context menu carries the same things: **Apply Now**, **Capture Rest Pose** and **Reset To
Rest Pose**.

## The inspector

Shared by all three:

| Field | Means |
| --- | --- |
| `Targets` | The objects copied from, each with a weight. Weights are shares: two targets at 1 sit the object between them, and the same pair at 0.5 does the same thing. 0, or an empty slot, is skipped |
| `Power` | [Above](#power) — 0 is off |
| `Axes` | Which axes may be written |
| `Space` | World or Local |
| `Mode` | Absolute or Relative |
| `Tween Duration`, `Tween Ease` | Trail the target instead of sticking to it. Play mode only |
| `Follow` | On, live every frame in `LateUpdate`. Off, applied once on enable and then left alone until `Apply()` |
| `Apply In Edit Mode` | Also constrain while editing |
| `Rest Local Position`, `Rest Local Euler` | The authored pose Power and Relative work from |

Position Constraint adds `Offset` and `Offset In Target Space`. Rotation Constraint adds `Offset Euler`.
Look At Constraint adds:

| Field | Means |
| --- | --- |
| `Aim Axis` | The local axis pointed at the target. `Forward` for a model; `Up` or `Right` for a sprite whose art faces the camera |
| `Up Axis` | The local axis held against `World Up`. Decides the roll left over. Must not be the same axis as `Aim Axis` |
| `World Up` | The direction that axis is held against. The board's normal, for a game played on a tilted surface |
| `Up Target` | Optional — holds `Up Axis` at another object instead, which is how a camera stays level with a rig that banks |
| `Aim Offset`, `Offset Euler` | [Above](#offsets) |

> **Aiming a sprite in 2D.** A sprite's Forward points out of the screen, so pointing *that* at a target
> turns the art edge-on and it vanishes. What you want is the art to spin in the plane of the board:
> **Aim Axis** `Up` (or `Right`, whichever way the art points), **Up Axis** `Forward`, **World Up**
> `(0, 0, 1)`, and **Axes** `Z` alone. The last two are what keep the sprite flat.

## From code

| Member | Does |
| --- | --- |
| `SetTarget(t)` | Replaces the whole list with one target at full weight |
| `AddTarget(t, weight = 1)` | Blends another one in |
| `RemoveTarget(t)`, `ClearTargets()` | The other way |
| `Targets` | The list itself, for re-weighting rows in place |
| `Power`, `Axes`, `Space`, `Mode`, `TweenDuration`, `Follow` | The inspector, at runtime |
| `Offset` / `OffsetEuler` / `AimOffset` | Per component |
| `Apply()` | Evaluate and write now — the hook to call with `Follow` off |
| `CaptureRest()`, `ResetToRest()` | The rest pose: take it, or go back to it |
| `ResampleOffset()` | Re-measure the gap Relative holds, as things stand now |

```csharp
// Blend a camera from its own rest pose onto a ball over a second, then let it trail
var rig = cam.GetComponent<PositionConstraint>();
rig.SetTarget(ball);
rig.TweenDuration = 0.4f;
DOTween.To(() => rig.Power, p => rig.Power = p, 1f, 1f);
```

Every setter is safe to poke every frame; the components hold no state that a change would corrupt.

## Worth knowing

- **Chains lag by a frame.** Constraints run in `LateUpdate` and Unity does not order components across
  objects, so a constraint whose target is itself constrained may read a value one frame old. Where that
  shows, put the driver earlier in the Script Execution Order, or call `Apply()` on the follower yourself.
- **`LateUpdate` beats animation.** The Animator has applied its clips by then, so a constraint overrides an
  animated channel rather than being overwritten by it.
- **A constraint cannot target its own transform.** `OnValidate` clears that and says so — it would be
  reading the value it had just written.
- **Two of the same kind on one object fight**, so `[DisallowMultipleComponent]` refuses. One of each kind
  is fine.
- **`Follow` off freezes the object where the constraint last left it**, which is not the rest pose.
  `ResetToRest()` is the way back.
- **Unity has its own `PositionConstraint`, `RotationConstraint` and `LookAtConstraint`** in
  `UnityEngine.Animations`, for animation rigging. These are unrelated and live in `FlappyTemplate`. A file
  that imports both namespaces needs to qualify the name.
- **Nothing here reads a `RectTransform`.** These are for scene objects; the UI equivalents live under
  `Ui/RectTransforms`.

## Files

| File | What |
| --- | --- |
| `TransformConstraint.cs` | The base: targets, rest pose, power, mask, tween, when it runs |
| `PositionConstraint.cs` | Position, with an offset that can turn with the target |
| `RotationConstraint.cs` | Rotation, copied |
| `LookAtConstraint.cs` | Rotation, aimed — the axis relabelling that lets any local axis do the pointing |
| `ConstraintSource.cs` | One target and its weight |
| `EConstraintAxes.cs`, `EConstraintSpace.cs`, `EConstraintMode.cs`, `EAimAxis.cs` | The four choices |
| `ConstraintExample.cs` | Keys 1–4, X, Y, Z, T, M, S, F and R driving all of it against an orbiting target |
| `README.html` | This, laid out for a browser, with the parts drawn |
| `Editor/Constraints/TransformConstraintEditor.cs` | The inspector: the default one, with the rest pose moved to a foldout at the bottom |
