using UnityEngine;

namespace FlappyTemplate
{
    // Turns this object to face one or more targets - the blended point between them, when there are
    // several. Unlike RotationConstraint it does not copy the target's facing; it works out the facing
    // that points at it, which is what a camera, a spotlight, an arrow or an eye wants.
    //
    // Aim Axis is the part worth setting first. A model usually points down its Forward, but a sprite
    // drawn facing the camera has its Forward pointing out of the screen, so a 2D arrow aims down Up or
    // Right instead - and for that flat case, ticking only Z on the axis mask keeps the whole thing in
    // the plane of the board.
    //
    // Aiming is a world-space job, so the targets are always read in world space here. The constraint's
    // Space still decides which euler frame the axis mask and the tween work in: World masks against the
    // scene's axes, Local against the parent's - the latter being what keeps a billboard turning only
    // about the axis its holder calls up.
    [AddComponentMenu("FlappyBet/Look At Constraint")]
    [DisallowMultipleComponent]
    public class LookAtConstraint : TransformConstraint
    {
        [Header("Aim")]
        [Tooltip("The local axis pointed at the target. Forward for a model, Up or Right for a sprite whose art faces the camera.")]
        [SerializeField]
        private EAimAxis aimAxis = EAimAxis.Forward;

        [Tooltip("The local axis kept pointing at World Up (or at Up Target). It decides the roll left over once the aim axis is fixed, and must not be the same axis as Aim Axis.")]
        [SerializeField]
        private EAimAxis upAxis = EAimAxis.Up;

        [Tooltip("The direction Up Axis is held against. The scene's up is the usual answer; the board's normal suits a game played on a tilted surface.")]
        [SerializeField]
        private Vector3 worldUp = Vector3.up;

        [Tooltip("Optional: holds Up Axis pointing at this object instead of at World Up, which is how a camera stays level with a rig that banks.")]
        [SerializeField]
        private Transform upTarget;

        [Header("Offset")]
        [Tooltip("Moves the point being aimed at, in world units - (0, 1, 0) aims a unit above the target, so a camera looks at a character's head rather than their feet.")]
        [SerializeField]
        private Vector3 aimOffset;

        [Tooltip("Euler angles applied after the aim, about this object's own axes. A few degrees here is how a spotlight leads the thing it tracks instead of sitting dead on it.")]
        [SerializeField]
        private Vector3 offsetEuler;

        /// <summary>Moves the point being aimed at, in world units.</summary>
        public Vector3 AimOffset
        {
            get => aimOffset;
            set => aimOffset = value;
        }

        /// <summary>Euler angles applied after the aim, in this object's own space.</summary>
        public Vector3 OffsetEuler
        {
            get => offsetEuler;
            set => offsetEuler = value;
        }

        protected override void Evaluate()
        {
            if (!TryResolveAim(out var goal))
                return;

            // Relative here means "keep the skew you were authored with": an object standing at a few
            // degrees off its target keeps those few degrees while it tracks it, rather than snapping
            // dead on. The gap is measured against the aim direction, not the target's own facing.
            if (Mode == EConstraintMode.Relative)
                goal *= RelativeRotationDelta;

            if (offsetEuler != Vector3.zero)
                goal *= Quaternion.Euler(offsetEuler);

            ApplyRotation(goal);
        }

        // The rest gap for a look-at is measured against where the object was aiming, which is not the
        // same thing as the target's rotation the base class would otherwise sample.
        protected override bool TrySampleTargetRotation(out Quaternion rotation) => TryResolveAim(out rotation);

        // The facing that points Aim Axis at the blended target, in the constraint's space.
        private bool TryResolveAim(out Quaternion rotation)
        {
            rotation = Quaternion.identity;

            if (!TryBlendWorldPosition(out var point))
                return false;

            var direction = point + aimOffset - transform.position;
            // An object sitting exactly on its target has no direction to face, and no useful guess to
            // make either - so it holds the facing it already had rather than snapping to identity.
            if (direction.sqrMagnitude <= 1e-8f)
                return false;

            var up = upTarget != null ? upTarget.position - transform.position : worldUp;
            if (up.sqrMagnitude <= 1e-8f)
                up = Vector3.up;

            if (Vector3.Cross(direction, up).sqrMagnitude <= 1e-8f)
            {
                // Aiming straight along the up direction leaves the roll undecided and LookRotation
                // complaining, so borrow an axis that cannot be in line with this one.
                var normalized = direction.normalized;
                up = Mathf.Abs(normalized.y) > 0.99f ? Vector3.forward : Vector3.up;
            }

            var aimVector = AxisVector(aimAxis);
            var upVector = AxisVector(upAxis);
            if (Vector3.Cross(aimVector, upVector).sqrMagnitude <= 1e-8f)
                upVector = Mathf.Abs(aimVector.y) > 0.5f ? Vector3.forward : Vector3.up;

            // LookRotation only knows how to point Forward at things. The second half re-labels the axes:
            // it undoes the rotation that carries Forward/Up onto the pair of axes chosen above, so what
            // ends up on the target is Aim Axis rather than Forward. With Forward and Up it is identity
            // and this is a plain LookRotation.
            var world = Quaternion.LookRotation(direction, up) *
                Quaternion.Inverse(Quaternion.LookRotation(aimVector, upVector));

            rotation = FromWorldRotation(world);
            return true;
        }

        private static Vector3 AxisVector(EAimAxis axis) => axis switch
        {
            EAimAxis.Forward => Vector3.forward,
            EAimAxis.Back => Vector3.back,
            EAimAxis.Up => Vector3.up,
            EAimAxis.Down => Vector3.down,
            EAimAxis.Right => Vector3.right,
            _ => Vector3.left,
        };

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (worldUp.sqrMagnitude <= 1e-8f)
                worldUp = Vector3.up;

            // Two axes in line leave no roll to resolve. The evaluation quietly substitutes one, but the
            // inspector is where the mistake was made, so it is worth saying so.
            if (Vector3.Cross(AxisVector(aimAxis), AxisVector(upAxis)).sqrMagnitude <= 1e-8f)
                Debug.LogWarning($"{nameof(LookAtConstraint)} on {name}: Aim Axis and Up Axis are the same axis, so the roll is undecided. Pick two axes at right angles.", this);
        }
#endif
    }
}
