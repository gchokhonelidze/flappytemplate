using UnityEngine;

namespace FlappyTemplate
{
    // Holds this object's position against one or more targets. Absolute puts it on the target, Relative
    // has it trail the target while keeping the gap the scene was authored with, and the axis mask lets it
    // take only part of the movement - a label that follows a ball across the board but never rises with
    // it is Axes: X and nothing else.
    //
    // Rotation is not touched. Pair it with a RotationConstraint or a LookAtConstraint on the same object
    // when both are wanted; they write different channels and do not fight over them.
    [AddComponentMenu("FlappyBet/Position Constraint")]
    [DisallowMultipleComponent]
    public class PositionConstraint : TransformConstraint
    {
        [Header("Offset")]
        [Tooltip("Added to the constrained position. Three units up puts a marker above the peg it follows rather than inside it.")]
        [SerializeField]
        private Vector3 offset;

        [Tooltip("Off, the offset points along the constraint's own axes - up is up, whatever the target is doing. On, it turns with the target, so an offset of (0, 0, -4) stays four units behind a target that spins. With several targets it turns with their blended rotation.")]
        [SerializeField]
        private bool offsetInTargetSpace;

        /// <summary>Offset added to the constrained position, in the constraint's space.</summary>
        public Vector3 Offset
        {
            get => offset;
            set => offset = value;
        }

        protected override void Evaluate()
        {
            if (!TryBlendPosition(out var goal))
                return;

            if (Mode == EConstraintMode.Relative)
                goal += RelativePositionDelta;

            if (offset != Vector3.zero)
                goal += offsetInTargetSpace && TryBlendRotation(out var targetRotation) ? targetRotation * offset : offset;

            ApplyPosition(goal);
        }
    }
}
