using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace FlappyTemplate
{
    // Put this on the cube that carries the dice mesh. Everything below is expressed in local (parent)
    // space - DOLocalMove*/DOLocalRotate for the animation, and the yaw and tumble quaternions are
    // pre-multiplied onto localRotation, which turns them about the parent's axes and not the scene's.
    // So "the face points up" means up as the parent sees it: tilt, spin or scale the parent and the
    // whole roll follows it, with no axis in here left pointing at the world.
    //
    // The value is decided before the animation, not by physics: Roll(6) jumps, optionally hangs in the
    // air still rolling, then falls, tumbles and bounces - and is guaranteed to come to rest showing 6.
    // The whole flight is rise -> hover -> fall -> bounces, so Roll(6, 2f) spends two of those seconds
    // hovering at the top of the jump before it drops. See README.md beside this file.
    [AddComponentMenu("FlappyBet/Dice Roller")]
    [DisallowMultipleComponent]
    public class DiceRoller : MonoBehaviour
    {
        [Serializable]
        private struct FaceOrientation
        {
            [Tooltip("Pip count printed on the face.")]
            public int value;

            [Tooltip("Local euler angles that leave that face pointing up.")]
            public Vector3 euler;
        }

        // Defaults describe the usual Unity cube layout - 1 on +Y, 6 on -Y, 2 on +Z, 5 on -Z, 3 on +X,
        // 4 on -X - which is what you get from a standard dice texture atlas. A mesh whose pips sit on
        // other faces only needs these six entries re-pointed; nothing else in here assumes a layout.
        [SerializeField]
        private FaceOrientation[] faceOrientations =
        {
            new() { value = 1, euler = new Vector3(0f, 0f, 0f) },
            new() { value = 2, euler = new Vector3(-90f, 0f, 0f) },
            new() { value = 3, euler = new Vector3(0f, 0f, 90f) },
            new() { value = 4, euler = new Vector3(0f, 0f, -90f) },
            new() { value = 5, euler = new Vector3(90f, 0f, 0f) },
            new() { value = 6, euler = new Vector3(180f, 0f, 0f) },
        };

        [Header("Jump")]
        // Off, the dice stays where it stands and only spins into its face, so the same call site works
        // for a showy roll and for a quiet re-roll in a tight layout.
        [SerializeField]
        private bool jumpEnabled = true;

        [Tooltip("Apex of the jump above the higher of the take-off and landing points, in local units.")]
        [SerializeField]
        private float jumpHeight = 2f;

        [Tooltip("Time of the jump itself - rise plus fall. Hover and bounces are added on top of it.")]
        [SerializeField]
        private float jumpDuration = 0.9f;

        [Tooltip("Where the dice lands, relative to the position it started the scene at.")]
        [SerializeField]
        private Vector3 landingOffset = Vector3.zero;

        [Tooltip("Random horizontal spread around the landing point, so repeated rolls do not stack.")]
        [SerializeField]
        private float landingScatter = 0f;

        // This whole group is dead while Hover Duration is 0, which is the value it ships with - nothing
        // below it can have any effect until the dice actually spends time at the apex.
        [Header("Hover - the three fields below do nothing while Hover Duration is 0")]
        [Tooltip("Seconds the dice hangs at the top of the jump, still rolling, before it falls. This is " +
            "the default for Roll(value) calls; Roll(value, 2f) overrides it per roll.")]
        [SerializeField]
        private float hoverDuration = 0f;

        [Tooltip("How much faster the dice spins while it hangs at the apex. 1 = the same speed as the " +
            "rest of the jump, 3 = three times faster, 0 = it stops turning up there.")]
        [SerializeField, Range(0f, 8f)]
        private float hoverSpinMultiplier = 2f;

        [Tooltip("How far the dice drifts up and down while it hangs, in local units - cosmetic only. " +
            "Scale it against Jump Height: at 2 units of jump, 0.15 is a slight float and 0.5 is obvious.")]
        [SerializeField]
        private float hoverBobHeight = 0.15f;

        [Tooltip("Seconds per bob cycle - one drift up and back down.")]
        [SerializeField]
        private float hoverBobPeriod = 0.5f;

        [Header("Bounce")]
        [SerializeField, Range(0, 5)]
        private int bounceCount = 2;

        [Tooltip("Height each bounce keeps of the previous one.")]
        [SerializeField, Range(0.05f, 0.9f)]
        private float bounceDamping = 0.35f;

        [Header("Spin")]
        [Tooltip("Whole extra turns added per axis while airborne, picked at random within the range.")]
        [SerializeField]
        private Vector2Int spinTurns = new(2, 4);

        [Tooltip("Land one quarter-turn off the final face and roll onto it on the first bounce.")]
        [SerializeField]
        private bool tumbleOnLanding = true;

        [Header("Events")]
        [SerializeField]
        private UnityEvent<int> onRollComplete;

        [Header("Debug")]
        [SerializeField, Range(1, 6)]
        private int testValue = 6;

        private Vector3 homePosition;
        private Sequence sequence;

        public bool IsRolling => sequence != null && sequence.IsActive() && sequence.IsPlaying();

        // Toggle the jump from code or from a UI button; it is read when a roll starts, so flipping it
        // mid-roll never cuts the running animation short.
        public bool JumpEnabled
        {
            get => jumpEnabled;
            set => jumpEnabled = value;
        }

        public void ToggleJump() => jumpEnabled = !jumpEnabled;

        // Default hover for Roll() calls that do not pass their own. Only ever applies with the jump on -
        // there is no air to hang in otherwise.
        public float HoverDuration
        {
            get => hoverDuration;
            set => hoverDuration = Mathf.Max(0f, value);
        }

        // Spin speed at the apex, as a multiple of the speed the dice turns at during the rest of the
        // jump. Only bites while a hover is actually running.
        public float HoverSpinMultiplier
        {
            get => hoverSpinMultiplier;
            set => hoverSpinMultiplier = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            // Captured once, so a hundred rolls with a landingOffset do not walk the dice off the table:
            // every roll takes off from wherever the dice currently rests and lands relative to home.
            homePosition = transform.localPosition;
        }

        private void OnValidate()
        {
            // Negative or zero values here do not read as "off", they read as a broken roll, so the
            // inspector is held to the ranges the sequence can actually build from.
            jumpDuration = Mathf.Max(0.01f, jumpDuration);
            hoverDuration = Mathf.Max(0f, hoverDuration);
            hoverBobHeight = Mathf.Max(0f, hoverBobHeight);
            hoverBobPeriod = Mathf.Max(0.05f, hoverBobPeriod);
        }

        private void OnDisable()
        {
            // complete: true - a dice killed mid-flight would otherwise be left hanging in the air and,
            // worse, showing a face nobody asked for. Completing snaps it onto its resting value.
            sequence?.Kill(true);
            sequence = null;
        }

        // Rolls to a value decided up front, hovering for whatever the inspector's hoverDuration says.
        // Returns the tween so a caller can await or chain it; the same information arrives through
        // onComplete and the onRollComplete event.
        public Tween Roll(int faceValue, Action onComplete = null) => Roll(faceValue, hoverDuration, onComplete);

        // Same roll with the hover given at the call site - Roll(6, 2f) hangs the dice in the air rolling
        // for two seconds at the top of its jump, then drops it onto the 6.
        public Tween Roll(int faceValue, float hover, Action onComplete = null)
        {
            if (!TryGetFaceRotation(faceValue, out var faceRotation))
            {
                Debug.LogError($"{nameof(DiceRoller)}: no face orientation configured for value {faceValue}.", this);
                return null;
            }

            sequence?.Kill();

            var t = transform;
            var start = t.localPosition;
            var startEuler = t.localEulerAngles;

            // Yaw is the one rotation that cannot change which face points up, so the dice is free to come
            // to rest at any of the four quarter turns and still read as the value that was asked for.
            var restRotation = Quaternion.Euler(0f, 90f * Random.Range(0, 4), 0f) * faceRotation;

            // The orientation the spin ends on: a quarter turn short of the answer when tumbling, so the
            // first bounce rolls the dice over one edge and onto its final face.
            var tumbleAxis = Random.value < 0.5f ? Vector3.right : Vector3.forward;
            var tumbleSign = Random.value < 0.5f ? 1f : -1f;
            var landingRotation = tumbleOnLanding
                ? Quaternion.AngleAxis(90f * tumbleSign, tumbleAxis) * restRotation
                : restRotation;

            float jumpTime = Mathf.Max(0.01f, jumpDuration);
            float riseTime = jumpTime * 0.45f;
            float fallTime = jumpTime - riseTime;
            // A hover is time spent at the top of a jump, so with the jump off there is nowhere to hover.
            float hoverTime = jumpEnabled ? Mathf.Max(0f, hover) : 0f;
            float fallStart = riseTime + hoverTime;
            // Everything from take-off to the first touch down, hover included.
            float airTime = fallStart + fallTime;

            sequence = DOTween.Sequence();

            if (jumpEnabled)
            {
                var landing = homePosition + landingOffset;
                if (landingScatter > 0f)
                {
                    var scatter = Random.insideUnitCircle * landingScatter;
                    landing += new Vector3(scatter.x, 0f, scatter.y);
                }

                float apex = Mathf.Max(start.y, landing.y) + jumpHeight;
                // Rise and fall are separate tweens with opposite eases: one OutQuad up and one InQuad down
                // is what makes the arc read as gravity rather than as a lerp. The hover simply pushes the
                // fall later on the timeline; neither half of the arc changes shape because of it.
                sequence.Insert(0f, t.DOLocalMoveY(apex, riseTime).SetEase(Ease.OutQuad));
                sequence.Insert(fallStart, t.DOLocalMoveY(landing.y, fallTime).SetEase(Ease.InQuad));

                // Horizontal travel is split around the hover - the share of the ground covered on the way
                // up, the rest on the way down - so a hovering dice hangs over one spot instead of sliding
                // sideways through the air. With no hover the two halves are one continuous linear move.
                float risenFraction = riseTime / jumpTime;
                float midX = Mathf.Lerp(start.x, landing.x, risenFraction);
                float midZ = Mathf.Lerp(start.z, landing.z, risenFraction);
                sequence.Insert(0f, t.DOLocalMoveX(midX, riseTime).SetEase(Ease.Linear));
                sequence.Insert(0f, t.DOLocalMoveZ(midZ, riseTime).SetEase(Ease.Linear));
                sequence.Insert(fallStart, t.DOLocalMoveX(landing.x, fallTime).SetEase(Ease.Linear));
                sequence.Insert(fallStart, t.DOLocalMoveZ(landing.z, fallTime).SetEase(Ease.Linear));

                if (hoverTime > 0f && hoverBobHeight > 0f)
                    InsertHoverBob(t, apex, hoverTime, riseTime);

                AppendBounces(t, landing.y, fallTime, airTime);
            }

            // Hovering time counts for more than its seconds when the multiplier is up: weighting it here
            // is what buys the extra turns, so a boosted hover adds rotation instead of stretching the same
            // rotation thinner. At a multiplier of 1 the weight is plain time and the dice keeps a steady
            // speed from take-off to landing; at 0 the weighting drops the hover out and the dice hangs
            // there without turning at all.
            float spinWeight = riseTime + hoverTime * hoverSpinMultiplier + fallTime;
            float spinScale = spinWeight / jumpTime;

            // FastBeyond360: DOTween interpolates the raw euler numbers instead of taking the shortest arc,
            // so the whole turns added below actually play out as tumbling - and because whole turns leave
            // the orientation unchanged, the dice still ends exactly on landingRotation however many of
            // them the hover piles on. Rounding keeps them whole, which is what protects that guarantee.
            var spinEnd = landingRotation.eulerAngles + new Vector3(
                360f * Mathf.Round(RandomTurns() * spinScale),
                360f * Mathf.Round(RandomTurns() * spinScale),
                360f * Mathf.Round(RandomTurns() * spinScale)
            );

            if (hoverTime > 0f)
            {
                // One continuous tumble cut into rise, hover and fall so each stretch can turn at its own
                // speed - the hover taking the share its weight earned it, and only the drop decelerating.
                // The cuts are plain lerps of the euler numbers, which is exactly what FastBeyond360
                // interpolates anyway, so each tween picks up precisely where the last left off and the
                // landing orientation stays exact.
                var afterRise = Vector3.Lerp(startEuler, spinEnd, riseTime / spinWeight);
                var afterHover = Vector3.Lerp(startEuler, spinEnd, (riseTime + hoverTime * hoverSpinMultiplier) / spinWeight);
                sequence.Insert(0f, t.DOLocalRotate(afterRise, riseTime, RotateMode.FastBeyond360).SetEase(Ease.Linear));
                sequence.Insert(riseTime, t.DOLocalRotate(afterHover, hoverTime, RotateMode.FastBeyond360).SetEase(Ease.Linear));
                sequence.Insert(fallStart, t.DOLocalRotate(spinEnd, fallTime, RotateMode.FastBeyond360).SetEase(Ease.OutSine));
            }
            else
            {
                sequence.Insert(0f, t.DOLocalRotate(spinEnd, airTime, RotateMode.FastBeyond360).SetEase(Ease.OutSine));
            }

            if (tumbleOnLanding)
            {
                // Rides the first bounce, or a short settle when there is none: the last quarter turn onto
                // the face the caller asked for.
                // Off jumpTime, never airTime: the hover is dead time at the apex and has no business
                // stretching a quarter turn that happens after the dice has already landed.
                float tumbleTime = jumpEnabled && bounceCount > 0
                    ? fallTime * Mathf.Sqrt(bounceDamping) * 0.5f
                    : jumpTime * 0.15f;
                sequence.Insert(airTime, t.DOLocalRotate(restRotation.eulerAngles, tumbleTime, RotateMode.Fast).SetEase(Ease.OutQuad));
            }

            sequence.OnComplete(() =>
            {
                // Assigned outright rather than tweened to: the tween chain ends a fraction of a degree off
                // and a dice resting 0.3 degrees over reads as a dice that did not quite settle.
                t.localRotation = restRotation;
                onRollComplete?.Invoke(faceValue);
                onComplete?.Invoke();
            });

            return sequence.Play();
        }

        // Snaps to a face with no animation - for setting up a board before the first roll.
        public void SetFace(int faceValue)
        {
            if (!TryGetFaceRotation(faceValue, out var faceRotation))
            {
                Debug.LogError($"{nameof(DiceRoller)}: no face orientation configured for value {faceValue}.", this);
                return;
            }

            sequence?.Kill();
            sequence = null;
            transform.localRotation = faceRotation;
        }

        // A slow bob so a hovering dice looks like it is holding itself up rather than frozen mid-frame.
        private void InsertHoverBob(Transform t, float apex, float hoverTime, float startTime)
        {
            // An even number of yoyo legs, so the bob always finishes back at the apex - which is the exact
            // height the fall tween is going to start from.
            int legs = Mathf.Max(2, Mathf.RoundToInt(hoverTime / Mathf.Max(0.05f, hoverBobPeriod)) * 2);
            float legTime = hoverTime / legs;

            sequence.Insert(
                startTime,
                t.DOLocalMoveY(apex + hoverBobHeight, legTime).SetEase(Ease.InOutSine).SetLoops(legs, LoopType.Yoyo)
            );
        }

        // Each bounce keeps bounceDamping of the previous height. Its duration falls with the square root
        // of that, which is how long a real bounce of that height lasts - scaling duration linearly makes
        // the small late bounces crawl.
        private void AppendBounces(Transform t, float groundY, float fallTime, float startTime)
        {
            float height = jumpHeight;
            float duration = fallTime;
            float time = startTime;

            for (int i = 0; i < bounceCount; i++)
            {
                height *= bounceDamping;
                duration *= Mathf.Sqrt(bounceDamping);
                float half = duration * 0.5f;

                sequence.Insert(time, t.DOLocalMoveY(groundY + height, half).SetEase(Ease.OutQuad));
                sequence.Insert(time + half, t.DOLocalMoveY(groundY, half).SetEase(Ease.InQuad));
                time += duration;
            }
        }

        private float RandomTurns()
        {
            int min = Mathf.Min(spinTurns.x, spinTurns.y);
            int max = Mathf.Max(spinTurns.x, spinTurns.y);
            int turns = Random.Range(min, max + 1);
            return Random.value < 0.5f ? -turns : turns;
        }

        private bool TryGetFaceRotation(int faceValue, out Quaternion rotation)
        {
            if (faceOrientations != null)
            {
                foreach (var face in faceOrientations)
                {
                    if (face.value != faceValue)
                        continue;
                    rotation = Quaternion.Euler(face.euler);
                    return true;
                }
            }

            rotation = Quaternion.identity;
            return false;
        }

        [ContextMenu("Test Roll")]
        private void TestRoll() => Roll(testValue);
    }
}
