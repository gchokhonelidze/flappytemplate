using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FlappyTemplate
{
    // Shared machinery behind PositionConstraint, RotationConstraint and LookAtConstraint: the target
    // list and how several targets blend, the rest pose, Power, the axis mask, the DOTween easing and
    // when the whole thing runs. A subclass answers one question - what value should this transform hold
    // this frame - and hands it to ApplyPosition or ApplyRotation, which do the rest.
    //
    // This is for scene objects, not UI: everything here drives Transform.position/rotation or
    // localPosition/localRotation and nothing touches a RectTransform's anchors. The UI equivalents live
    // under Ui/RectTransforms.
    //
    // Three ideas carry most of the behaviour:
    //
    //   Rest pose - the object's authored local pose, captured when the component is added. Power blends
    //     between it and the constrained value, and Relative mode measures its gap to the target from it.
    //     Without it, a half-power constraint would blend from the pose it wrote last frame and creep onto
    //     the target over a second or two.
    //   Axis mask - an unticked axis is never written, so it is left to whatever else owns it rather than
    //     pinned to the rest pose.
    //   Power 0 - not "write the rest pose", but "do not write at all". The constraint stops being there.
    //
    // [ExecuteAlways] and an EditorApplication.update tick keep all of that live while editing, so the
    // relationship is visible in the scene view without entering play mode. See README.md beside this file.
    [ExecuteAlways]
    public abstract class TransformConstraint : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("The objects this constraint copies from. Several are blended by weight; one is the common case.")]
        [SerializeField]
        private List<ConstraintSource> targets = new() { new ConstraintSource() };

        [Header("Constraint")]
        [Tooltip("How much of the constrained value is used. 1 is the whole of it, 0.5 sits the object halfway between its rest pose and the target, and 0 means no constraint at all - nothing is written and the object is left entirely to whatever else moves it.")]
        [SerializeField, Range(0f, 1f)]
        private float power = 1f;

        [Tooltip("Which axes may be written. An unticked axis is left exactly as it is, so 'follow the ball's x, keep my own y' needs no code.")]
        [SerializeField]
        private EConstraintAxes axes = EConstraintAxes.All;

        [Tooltip("World reads and writes world position/rotation. Local copies the target's localPosition/localRotation onto this object's - the same place relative to each one's own parent. The axis mask applies in whichever space is chosen.")]
        [SerializeField]
        private EConstraintSpace space = EConstraintSpace.World;

        [Tooltip("Absolute puts this object on the target, so it jumps there as soon as the constraint runs. Relative keeps the gap it had when it started following, so it trails the target from wherever it was authored - Maintain Offset, in other words.")]
        [SerializeField]
        private EConstraintMode mode = EConstraintMode.Absolute;

        [Header("Tween")]
        [Tooltip("Seconds the object takes to reach the constrained value, as a DOTween ease. 0 is a hard constraint - it is simply there, every frame. Anything above it trails the target by that long, which is how a camera or a label follows something without snapping about. Play mode only: while editing the value is always applied instantly, since DOTween does not run outside play mode.")]
        [SerializeField, Min(0f)]
        private float tweenDuration;

        [Tooltip("Ease of that trailing move. OutQuad reads as weight - fast off the mark, settling gently. Linear reads as machinery.")]
        [SerializeField]
        private Ease tweenEase = Ease.OutQuad;

        [Header("When")]
        [Tooltip("On keeps the constraint live every frame, in LateUpdate, after animation has run. Off applies it once on enable and then leaves the object alone until something calls Apply().")]
        [SerializeField]
        private bool follow = true;

        [Tooltip("Also constrains while editing, so the relationship is visible in the scene view without entering play mode.")]
        [SerializeField]
        private bool applyInEditMode = true;

        [Header("Rest pose")]
        [Tooltip("The object's authored local position - the baseline Power blends from and Relative measures its gap from. Captured when the component is added; use the component's Capture Rest Pose menu item after moving the object by hand, and note that a prefab instance inherits the prefab's rest pose until you do.")]
        [SerializeField]
        private Vector3 restLocalPosition;

        [Tooltip("The object's authored local rotation, in euler angles. Same story as Rest Local Position.")]
        [SerializeField]
        private Vector3 restLocalEuler;

        [SerializeField, HideInInspector]
        private bool restCaptured;

        // Where the targets stood when this constraint started following them, which is what Relative
        // mode measures its gap against. Deliberately not serialized: it is re-sampled on enable and
        // after any inspector edit, so it can never go stale against a target that has been swapped,
        // re-weighted or moved into another space. In play mode that means the gap is taken on the first
        // frame the constraint runs, which is exactly the offset the scene was authored with.
        private Vector3 targetRestPosition;
        private Quaternion targetRestRotation = Quaternion.identity;
        private bool targetRestSampled;

        private Tweener positionTween;
        private Vector3 positionGoal;
        private Tweener rotationTween;
        private Quaternion rotationGoal = Quaternion.identity;

        /// <summary>How much of the constrained value is used, 0 (off) to 1 (fully constrained).</summary>
        public float Power
        {
            get => power;
            set => power = Mathf.Clamp01(value);
        }

        /// <summary>Which axes the constraint is allowed to write.</summary>
        public EConstraintAxes Axes
        {
            get => axes;
            set => axes = value;
        }

        /// <summary>Seconds the object takes to reach the constrained value. 0 is a hard constraint.</summary>
        public float TweenDuration
        {
            get => tweenDuration;
            set
            {
                tweenDuration = Mathf.Max(0f, value);
                // The live tween carries the old duration inside it, so it has to go rather than be
                // re-aimed; the next frame builds one with the new timing.
                KillTweens();
            }
        }

        /// <summary>Whether the constraint is re-applied every frame.</summary>
        public bool Follow
        {
            get => follow;
            set => follow = value;
        }

        /// <summary>The target list itself, so callers can re-weight rows in place.</summary>
        public List<ConstraintSource> Targets => targets ??= new List<ConstraintSource>();

        /// <summary>Whether the constraint reads and writes world or local values.</summary>
        public EConstraintSpace Space
        {
            get => space;
            set
            {
                if (space == value)
                    return;

                space = value;
                // A live tween is either a world move or a local one and cannot be re-aimed across that
                // line, and the gap Relative holds was measured in the space being left behind.
                KillTweens();
                targetRestSampled = false;
            }
        }

        /// <summary>Whether the object is put on the target or trails it, keeping its authored gap.</summary>
        public EConstraintMode Mode
        {
            get => mode;
            set
            {
                if (mode == value)
                    return;

                mode = value;
                // Switching to Relative should hold the gap as it is now - that is what "start following
                // from here" means - rather than a gap measured before the target had moved.
                targetRestSampled = false;
            }
        }

        // The rest pose in whichever space the constraint works in. Stored as a local pose and converted
        // on the way out, so a rest pose stays correct under a parent that moves, turns or scales - a
        // stored world pose would quietly point at where the parent used to be.
        protected Vector3 RestPosition
        {
            get
            {
                if (space == EConstraintSpace.Local)
                    return restLocalPosition;
                var parent = transform.parent;
                return parent != null ? parent.TransformPoint(restLocalPosition) : restLocalPosition;
            }
        }

        protected Quaternion RestRotation
        {
            get
            {
                var rest = Quaternion.Euler(restLocalEuler);
                if (space == EConstraintSpace.Local)
                    return rest;
                var parent = transform.parent;
                return parent != null ? parent.rotation * rest : rest;
            }
        }

        // Relative mode in one line: the gap between the rest pose and where the target stood when the
        // constraint started following it, which is then carried along by the target.
        protected Vector3 RelativePositionDelta => RestPosition - targetRestPosition;

        protected Quaternion RelativeRotationDelta => Quaternion.Inverse(targetRestRotation) * RestRotation;

        protected bool HasTarget
        {
            get
            {
                if (targets == null)
                    return false;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != null && targets[i].IsActive)
                        return true;
                }

                return false;
            }
        }

        // Power 0 and an empty mask both mean "not constrained", and are checked here rather than in each
        // subclass so turning a constraint off costs nothing per frame and writes nothing.
        private bool CanApply =>
            (Application.isPlaying || applyInEditMode) &&
            power > 0f &&
            (axes & EConstraintAxes.All) != 0 &&
            HasTarget;

        private bool Tweening => tweenDuration > 0f && Application.isPlaying;

        // Called when the component is added and when Reset is picked from its menu: the one moment the
        // object is guaranteed to be standing in its authored pose, before the constraint has written
        // anything to it.
        protected virtual void Reset()
        {
            CaptureRest();
        }

        protected virtual void OnEnable()
        {
            // A component added at runtime never sees Reset, so its rest pose is taken here instead.
            if (!restCaptured)
                CaptureRest();

            targetRestSampled = false;
            Apply();

#if UNITY_EDITOR
            // [ExecuteAlways] gets LateUpdate called while editing, but only when something has already
            // dirtied the scene - which is no good for a constraint whose target is being dragged about,
            // or driven by another constraint. EditorApplication.update ticks regardless.
            if (!Application.isPlaying)
                EditorApplication.update += EditorTick;
#endif
        }

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorTick;
#endif
            KillTweens();
        }

        // LateUpdate, so the frame's movement has already happened: the Animator has applied its clips,
        // scripts have moved their objects, and this reads settled values instead of last frame's.
        private void LateUpdate()
        {
            if (follow)
                Apply();
        }

        /// <summary>Evaluates the constraint and writes the result to this transform.</summary>
        // With Follow off this is the hook to call when something has changed - a target swapped, a peg
        // rebuilt - and it is also what the component's Apply Now menu item runs.
        [ContextMenu("Apply Now")]
        public void Apply()
        {
            if (!CanApply)
            {
                // A constraint turned off mid-tween has no business finishing the move it had started.
                KillTweens();
                return;
            }

            if (!restCaptured)
                CaptureRest();

            if (!targetRestSampled)
                SampleTargetRest();

            Evaluate();
        }

        /// <summary>Takes the object's current local pose as the rest pose the constraint blends from.</summary>
        // Worth pressing after moving the object by hand, and on a prefab instance that has been placed
        // somewhere other than where the prefab was authored.
        [ContextMenu("Capture Rest Pose")]
        public void CaptureRest()
        {
            restLocalPosition = transform.localPosition;
            restLocalEuler = transform.localEulerAngles;
            restCaptured = true;
            // The gap Relative mode holds is measured from the rest pose, so a new rest pose invalidates it.
            targetRestSampled = false;
        }

        /// <summary>Puts the object back on its rest pose and drops any running tween.</summary>
        [ContextMenu("Reset To Rest Pose")]
        public void ResetToRest()
        {
            KillTweens();
            transform.localPosition = restLocalPosition;
            transform.localRotation = Quaternion.Euler(restLocalEuler);
        }

        /// <summary>Replaces the whole target list with one target at full weight.</summary>
        public void SetTarget(Transform target)
        {
            Targets.Clear();
            if (target != null)
                Targets.Add(new ConstraintSource(target));
            OnTargetsChanged();
        }

        /// <summary>Adds a target to blend in alongside the ones already there.</summary>
        public void AddTarget(Transform target, float weight = 1f)
        {
            if (target == null)
                return;

            Targets.Add(new ConstraintSource(target, weight));
            OnTargetsChanged();
        }

        /// <summary>Removes the first row pointing at this target. Returns whether one was found.</summary>
        public bool RemoveTarget(Transform target)
        {
            for (int i = Targets.Count - 1; i >= 0; i--)
            {
                if (targets[i] == null || targets[i].target != target)
                    continue;

                targets.RemoveAt(i);
                OnTargetsChanged();
                return true;
            }

            return false;
        }

        /// <summary>Drops every target. The constraint stops writing until it is given another one.</summary>
        public void ClearTargets()
        {
            Targets.Clear();
            OnTargetsChanged();
        }

        /// <summary>Re-measures the gap Relative mode holds, taking the targets as they stand right now.</summary>
        // Only Relative mode cares. Call it when a target has been moved deliberately and the object
        // should trail it from its new place rather than snap back to the old separation.
        public void ResampleOffset()
        {
            targetRestSampled = false;
        }

        // The one thing a subclass has to do: work out the value this transform should hold and pass it
        // to ApplyPosition or ApplyRotation. Targets, Power, the mask and the tween are handled for it.
        protected abstract void Evaluate();

        // Where the rest gap is measured from. Position and rotation constraints compare against the
        // targets themselves; LookAtConstraint overrides the rotation half, because the thing it holds a
        // gap against is the direction it aims, not the target's own facing.
        protected virtual bool TrySampleTargetPosition(out Vector3 position) => TryBlendPosition(out position);

        protected virtual bool TrySampleTargetRotation(out Quaternion rotation) => TryBlendRotation(out rotation);

        /// <summary>Weighted average of the targets' positions, in the constraint's space.</summary>
        // Normalised by the weights that actually took part, so weights read as shares: two targets at 1
        // put the object between them, and the same pair at 0.5 each does exactly the same thing.
        protected bool TryBlendPosition(out Vector3 result) => TryBlendPosition(space, out result);

        /// <summary>Weighted average of the targets' world positions, whatever space the constraint uses.</summary>
        protected bool TryBlendWorldPosition(out Vector3 result) => TryBlendPosition(EConstraintSpace.World, out result);

        private bool TryBlendPosition(EConstraintSpace inSpace, out Vector3 result)
        {
            result = Vector3.zero;
            if (targets == null)
                return false;

            float total = 0f;
            for (int i = 0; i < targets.Count; i++)
            {
                var source = targets[i];
                if (source == null || !source.IsActive)
                    continue;

                result += (inSpace == EConstraintSpace.World ? source.target.position : source.target.localPosition) * source.weight;
                total += source.weight;
            }

            if (total <= 0f)
                return false;

            result /= total;
            return true;
        }

        /// <summary>Weighted blend of the targets' rotations, in the constraint's space.</summary>
        protected bool TryBlendRotation(out Quaternion result) => TryBlendRotation(space, out result);

        private bool TryBlendRotation(EConstraintSpace inSpace, out Quaternion result)
        {
            result = Quaternion.identity;
            if (targets == null)
                return false;

            float total = 0f;
            for (int i = 0; i < targets.Count; i++)
            {
                var source = targets[i];
                if (source == null || !source.IsActive)
                    continue;

                var rotation = inSpace == EConstraintSpace.World ? source.target.rotation : source.target.localRotation;
                total += source.weight;
                // Rotations do not average by adding them up, so they are folded in one at a time: each
                // new one pulls the running result over by its share of the weight gathered so far. The
                // first target lands on itself, since its share is the whole of it.
                result = Quaternion.Slerp(result, rotation, source.weight / total);
            }

            return total > 0f;
        }

        /// <summary>Converts a world rotation into the space this constraint writes in.</summary>
        protected Quaternion FromWorldRotation(Quaternion world)
        {
            if (space == EConstraintSpace.World)
                return world;

            var parent = transform.parent;
            return parent != null ? Quaternion.Inverse(parent.rotation) * world : world;
        }

        /// <summary>Blends the constrained position by Power, masks it by axis and writes or tweens it.</summary>
        protected void ApplyPosition(Vector3 constrained)
        {
            var t = transform;
            var current = space == EConstraintSpace.World ? t.position : t.localPosition;
            var goal = current;

            if (power >= 1f && (axes & EConstraintAxes.All) == EConstraintAxes.All)
            {
                goal = constrained;
            }
            else
            {
                // Part power blends from the rest pose, never from where the object stands: blending from
                // the current position would take a half-power constraint onto its target over a handful
                // of frames, since each frame's result becomes the next frame's starting point.
                var rest = RestPosition;
                if ((axes & EConstraintAxes.X) != 0)
                    goal.x = Mathf.Lerp(rest.x, constrained.x, power);
                if ((axes & EConstraintAxes.Y) != 0)
                    goal.y = Mathf.Lerp(rest.y, constrained.y, power);
                if ((axes & EConstraintAxes.Z) != 0)
                    goal.z = Mathf.Lerp(rest.z, constrained.z, power);
            }

            WritePosition(goal);
        }

        /// <summary>Blends the constrained rotation by Power, masks it by axis and writes or tweens it.</summary>
        protected void ApplyRotation(Quaternion constrained)
        {
            Quaternion goal;

            if (power >= 1f && (axes & EConstraintAxes.All) == EConstraintAxes.All)
            {
                // Straight through as a quaternion, which is the case worth protecting: a full-power
                // constraint never takes the trip through euler angles below, so it cannot pick up the
                // flips that a euler round trip springs on rotations near the poles.
                goal = constrained;
            }
            else
            {
                // Masking and part power are per-axis by definition, and an axis only exists in euler
                // terms - so this stretch has to convert. LerpAngle rather than Lerp, so 350 to 10 goes
                // the short way round instead of backwards through 180.
                var t = transform;
                var current = space == EConstraintSpace.World ? t.eulerAngles : t.localEulerAngles;
                var rest = RestRotation.eulerAngles;
                var target = constrained.eulerAngles;
                var euler = current;

                if ((axes & EConstraintAxes.X) != 0)
                    euler.x = Mathf.LerpAngle(rest.x, target.x, power);
                if ((axes & EConstraintAxes.Y) != 0)
                    euler.y = Mathf.LerpAngle(rest.y, target.y, power);
                if ((axes & EConstraintAxes.Z) != 0)
                    euler.z = Mathf.LerpAngle(rest.z, target.z, power);

                goal = Quaternion.Euler(euler);
            }

            WriteRotation(goal);
        }

        private void WritePosition(Vector3 goal)
        {
            var t = transform;

            if (!Tweening)
            {
                KillPositionTween();
                // Writing an unchanged value would flag the scene as modified on every editor tick.
                if (space == EConstraintSpace.World)
                {
                    if (t.position != goal)
                        t.position = goal;
                }
                else if (t.localPosition != goal)
                {
                    t.localPosition = goal;
                }

                return;
            }

            if (positionTween == null || !positionTween.IsActive())
            {
                // SetAutoKill(false) so this one tween lives as long as the component and can be re-aimed
                // frame after frame. A new tween per frame would allocate on every single one of them.
                positionTween = (space == EConstraintSpace.World
                        ? t.DOMove(goal, tweenDuration)
                        : t.DOLocalMove(goal, tweenDuration))
                    .SetEase(tweenEase)
                    .SetAutoKill(false);
                positionGoal = goal;
                return;
            }

            if (positionGoal == goal)
                return;

            positionGoal = goal;
            // snapStartValue: true is what makes this read as lag rather than as a stutter - the ease
            // restarts from wherever the object has got to, instead of from the point it set off from
            // when the target was somewhere else entirely.
            positionTween.ChangeEndValue(goal, tweenDuration, true).Restart();
        }

        private void WriteRotation(Quaternion goal)
        {
            var t = transform;

            if (!Tweening)
            {
                KillRotationTween();
                if (space == EConstraintSpace.World)
                {
                    if (t.rotation != goal)
                        t.rotation = goal;
                }
                else if (t.localRotation != goal)
                {
                    t.localRotation = goal;
                }

                return;
            }

            if (rotationTween == null || !rotationTween.IsActive())
            {
                // The quaternion shortcuts rather than DORotate: a rotation that is being re-aimed every
                // frame has no business being interpolated as three euler numbers, which is where sudden
                // flips near straight up come from.
                rotationTween = (space == EConstraintSpace.World
                        ? t.DORotateQuaternion(goal, tweenDuration)
                        : t.DOLocalRotateQuaternion(goal, tweenDuration))
                    .SetEase(tweenEase)
                    .SetAutoKill(false);
                rotationGoal = goal;
                return;
            }

            if (rotationGoal == goal)
                return;

            rotationGoal = goal;
            rotationTween.ChangeEndValue(goal, tweenDuration, true).Restart();
        }

        private void SampleTargetRest()
        {
            targetRestPosition = TrySampleTargetPosition(out var position) ? position : RestPosition;
            targetRestRotation = TrySampleTargetRotation(out var rotation) ? rotation : RestRotation;
            targetRestSampled = true;
        }

        private void OnTargetsChanged()
        {
            // A different target means a different gap, so Relative mode re-measures rather than carrying
            // the old target's separation over to the new one.
            targetRestSampled = false;
            Apply();
        }

        private void KillTweens()
        {
            KillPositionTween();
            KillRotationTween();
        }

        private void KillPositionTween()
        {
            positionTween?.Kill();
            positionTween = null;
        }

        private void KillRotationTween()
        {
            rotationTween?.Kill();
            rotationTween = null;
        }

#if UNITY_EDITOR
        // So an edit to the mask, the space or the power shows up straight away, even with Follow off.
        // Deferred by a frame because OnValidate runs mid-import and mid-undo, where writing to a
        // transform is not safe; delayCall fires once and clears itself.
        protected virtual void OnValidate()
        {
            tweenDuration = Mathf.Max(0f, tweenDuration);
            power = Mathf.Clamp01(power);

            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] == null)
                        continue;
                    targets[i].weight = Mathf.Max(0f, targets[i].weight);
                    // A constraint aimed at itself is a loop: it would read the value it just wrote.
                    if (targets[i].target == transform)
                    {
                        Debug.LogWarning($"{GetType().Name} on {name} cannot target its own transform.", this);
                        targets[i].target = null;
                    }
                }
            }

            // Any edit could have changed the targets, the space or the rest pose, all of which the
            // Relative gap is measured from - so it is dropped and taken again on the next apply.
            targetRestSampled = false;
            // Duration and ease are baked into a live tween, so it is rebuilt rather than re-aimed.
            KillTweens();

            if (Application.isPlaying || !applyInEditMode)
                return;

            EditorApplication.delayCall += ApplyDeferred;
        }

        private void ApplyDeferred()
        {
            // The frame in between is enough for an undo, a delete or a scene close to take the
            // component with it.
            if (this == null || !enabled)
                return;

            Apply();
        }

        private void EditorTick()
        {
            // Unsubscribing happens in OnDisable, but a domain reload or a deleted object can leave the
            // callback holding a corpse.
            if (this == null)
            {
                EditorApplication.update -= EditorTick;
                return;
            }

            if (!enabled || Application.isPlaying || !follow)
                return;

            Apply();
        }
#endif
    }
}
