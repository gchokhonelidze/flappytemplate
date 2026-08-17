using UnityEngine;

namespace FlappyTemplate
{
    // Copies rotation from one or more targets. Absolute takes the target's facing as its own, Relative
    // keeps the angular gap the scene was authored with and passes the target's turning on to it, and the
    // axis mask picks which euler axes come across - a sign that leans with a board's roll but stays
    // upright in yaw is Axes: Z alone.
    //
    // Position is not touched. To aim at something rather than copy its facing, use LookAtConstraint.
    [AddComponentMenu("FlappyBet/Rotation Constraint")]
    [DisallowMultipleComponent]
    public class RotationConstraint : TransformConstraint
    {
        [Header("Offset")]
        [Tooltip("Euler angles applied after the copy, about this object's own axes - so 90 on x tips the object a quarter turn away from whatever it copied. Turning the target turns the offset with it.")]
        [SerializeField]
        private Vector3 offsetEuler;

        /// <summary>Euler angles applied after the copy, in this object's own space.</summary>
        public Vector3 OffsetEuler
        {
            get => offsetEuler;
            set => offsetEuler = value;
        }

        protected override void Evaluate()
        {
            if (!TryBlendRotation(out var goal))
                return;

            if (Mode == EConstraintMode.Relative)
                goal *= RelativeRotationDelta;

            if (offsetEuler != Vector3.zero)
                goal *= Quaternion.Euler(offsetEuler);

            ApplyRotation(goal);
        }
    }
}
