using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    // Drop this anywhere in the scene, drag the constraints you are testing into the list, and press play.
    // With a Moving Target assigned it walks that object round a circle, so there is something for the
    // constraints to chase.
    //
    //   1 2 3 4   Power 0, a third, two thirds, 1
    //   X Y Z     tick that axis on or off
    //   T         cycle the tween duration - hard, 0.25s, 1s
    //   M         Absolute or Relative
    //   S         World or Local space
    //   F         Follow on or off - off freezes the constraint where it stands
    //   R         put every object back on its rest pose
    //
    // Every key has a public method behind it, so the same script wires straight onto UI buttons.
    [AddComponentMenu("FlappyBet/Constraint Example")]
    public class ConstraintExample : MonoBehaviour
    {
        [Tooltip("The constraints being driven. Any mix of Position, Rotation and Look At.")]
        [SerializeField]
        private List<TransformConstraint> constraints = new();

        [Tooltip("Optional: an object walked round a circle so the constraints have something to chase. Usually the same object the constraints target.")]
        [SerializeField]
        private Transform movingTarget;

        [Tooltip("Radius of that circle, in world units.")]
        [SerializeField]
        private float orbitRadius = 3f;

        [Tooltip("Turns per second the target makes round the circle.")]
        [SerializeField]
        private float orbitSpeed = 0.25f;

        [Tooltip("How far the target rises and falls while it goes round, so masking an axis off has something to show.")]
        [SerializeField]
        private float orbitHeight = 1f;

        [Tooltip("Degrees per second the target spins about its own up axis - what a Rotation Constraint copies.")]
        [SerializeField]
        private float orbitSpin = 90f;

        [Tooltip("The durations the T key steps through.")]
        [SerializeField]
        private float[] tweenDurations = { 0f, 0.25f, 1f };

        private Vector3 orbitCentre;
        private int tweenIndex;
        private float angle;

        private void Awake()
        {
            if (movingTarget != null)
                orbitCentre = movingTarget.position;
        }

        private void Update()
        {
            DriveTarget();
            ReadKeys();
        }

        // The target is moved here in Update, and every constraint reads it in LateUpdate - so what the
        // constraints see is always this frame's position rather than last frame's.
        private void DriveTarget()
        {
            if (movingTarget == null)
                return;

            angle += orbitSpeed * 360f * Time.deltaTime;
            float radians = angle * Mathf.Deg2Rad;
            movingTarget.position = orbitCentre + new Vector3(
                Mathf.Cos(radians) * orbitRadius,
                Mathf.Sin(radians * 2f) * orbitHeight,
                Mathf.Sin(radians) * orbitRadius
            );

            if (orbitSpin != 0f)
                movingTarget.Rotate(Vector3.up, orbitSpin * Time.deltaTime, Space.Self);
        }

        private void ReadKeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SetPower(0f);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                SetPower(1f / 3f);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                SetPower(2f / 3f);
            if (Input.GetKeyDown(KeyCode.Alpha4))
                SetPower(1f);

            if (Input.GetKeyDown(KeyCode.X))
                ToggleAxis(EConstraintAxes.X);
            if (Input.GetKeyDown(KeyCode.Y))
                ToggleAxis(EConstraintAxes.Y);
            if (Input.GetKeyDown(KeyCode.Z))
                ToggleAxis(EConstraintAxes.Z);

            if (Input.GetKeyDown(KeyCode.T))
                CycleTweenDuration();
            if (Input.GetKeyDown(KeyCode.M))
                ToggleMode();
            if (Input.GetKeyDown(KeyCode.S))
                ToggleSpace();
            if (Input.GetKeyDown(KeyCode.F))
                ToggleFollow();
            if (Input.GetKeyDown(KeyCode.R))
                ResetToRest();
        }

        // Power is the knob to reach for when a constraint is meant to be blended in and out rather than
        // simply on: 0 stops it writing at all and hands the object back to whatever else moves it.
        public void SetPower(float power)
        {
            foreach (var constraint in constraints)
            {
                if (constraint != null)
                    constraint.Power = power;
            }

            Debug.Log($"Constraint power {power:0.##}.", this);
        }

        public void ToggleAxis(EConstraintAxes axis)
        {
            foreach (var constraint in constraints)
            {
                if (constraint == null)
                    continue;

                // The mask is a flags enum, so a toggle is one exclusive-or - and the axes left alone by
                // the constraint are the ones whatever else moves the object keeps hold of.
                constraint.Axes ^= axis;
                Debug.Log($"{constraint.GetType().Name} axes {constraint.Axes}.", constraint);
            }
        }

        public void CycleTweenDuration()
        {
            if (tweenDurations == null || tweenDurations.Length == 0)
                return;

            tweenIndex = (tweenIndex + 1) % tweenDurations.Length;
            float duration = tweenDurations[tweenIndex];

            foreach (var constraint in constraints)
            {
                if (constraint != null)
                    constraint.TweenDuration = duration;
            }

            Debug.Log(duration <= 0f
                ? "Hard constraint - the value is simply held, every frame."
                : $"Trailing the target over {duration:0.##}s.", this);
        }

        public void ToggleMode()
        {
            foreach (var constraint in constraints)
            {
                if (constraint == null)
                    continue;

                constraint.Mode = constraint.Mode == EConstraintMode.Absolute
                    ? EConstraintMode.Relative
                    : EConstraintMode.Absolute;
                Debug.Log($"{constraint.GetType().Name} is {constraint.Mode}.", constraint);
            }
        }

        public void ToggleSpace()
        {
            foreach (var constraint in constraints)
            {
                if (constraint == null)
                    continue;

                constraint.Space = constraint.Space == EConstraintSpace.World
                    ? EConstraintSpace.Local
                    : EConstraintSpace.World;
                Debug.Log($"{constraint.GetType().Name} reads {constraint.Space} values.", constraint);
            }
        }

        public void ToggleFollow()
        {
            foreach (var constraint in constraints)
            {
                if (constraint != null)
                    constraint.Follow = !constraint.Follow;
            }

            Debug.Log("Follow toggled - off, the constraint holds whatever it wrote last.", this);
        }

        public void ResetToRest()
        {
            foreach (var constraint in constraints)
            {
                if (constraint != null)
                    constraint.ResetToRest();
            }

            Debug.Log("Objects put back on their rest poses.", this);
        }
    }
}
