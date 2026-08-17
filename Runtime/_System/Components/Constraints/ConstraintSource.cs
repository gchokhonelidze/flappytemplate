using System;
using UnityEngine;

namespace FlappyTemplate
{
    // One target a constraint copies from, and how much of a say it has. Constraints hold a list of
    // these, so several targets can be blended: two pegs at equal weight put the object exactly between
    // them, and sliding one weight from 0 to 1 walks it across.
    [Serializable]
    public class ConstraintSource
    {
        [Tooltip("The scene object whose position or rotation is copied. Left empty this entry is skipped, so a half-built list never breaks the constraint.")]
        public Transform target;

        [Tooltip("This target's share of the result. Weights are normalised against the targets that actually took part, so they read as shares rather than absolutes - two targets at 1 and the same two at 0.5 give the same blend. 0 takes the target out without removing the row.")]
        [Min(0f)]
        public float weight = 1f;

        public ConstraintSource()
        {
        }

        public ConstraintSource(Transform target, float weight = 1f)
        {
            this.target = target;
            this.weight = Mathf.Max(0f, weight);
        }

        public bool IsActive => target != null && weight > 0f;
    }
}
