using System;
using UnityEngine;

namespace FlappyTemplate
{
    /// <summary>One row or column of a <see cref="UiGrid"/>.</summary>
    // A class rather than a struct so the inspector can hand a whole track around by reference and so a
    // list of them reorders without copying; nothing here is touched per frame, so the allocation is one
    // per track for the lifetime of the grid.
    [Serializable]
    public class GridTrack
    {
        [Tooltip("Fixed is canvas units. Flexible is a weight - the leftover space is split between the flexible tracks in proportion. Percent is a share of the content box with the gaps already taken out. Auto is as big as the largest item in the track says it needs to be.")]
        [SerializeField]
        private EGridTrack mode = EGridTrack.Flexible;

        [SerializeField]
        private float size = 1f;

        // Held per track rather than as one setting on the grid: a sidebar that must never fall below its
        // icons and a body that may grow without limit are the usual pair, and they sit next to each other.
        [Tooltip("Floor in canvas units. A flexible track that would be squeezed below this stops here and the others take the squeeze instead.")]
        [SerializeField]
        private float min;

        [Tooltip("Ceiling in canvas units. 0 means none - a track cannot be limited to nothing, and a track that could be is a Fixed track set to 0.")]
        [SerializeField]
        private float max;

        public GridTrack()
        {
        }

        public GridTrack(EGridTrack mode, float size)
        {
            this.mode = mode;
            this.size = size;
        }

        public EGridTrack Mode
        {
            get => mode;
            set => mode = value;
        }

        /// <summary>Canvas units, a weight, or a percentage, depending on <see cref="Mode"/>. Unused by Auto.</summary>
        public float Size
        {
            get => size;
            set => size = value;
        }

        public float Min
        {
            get => min;
            set => min = value;
        }

        /// <summary>Ceiling in canvas units; 0 or less is no ceiling.</summary>
        public float Max
        {
            get => max;
            set => max = value;
        }

        public static GridTrack Fixed(float units) => new GridTrack(EGridTrack.Fixed, units);

        public static GridTrack Flexible(float weight = 1f) => new GridTrack(EGridTrack.Flexible, weight);

        public static GridTrack Percent(float percent) => new GridTrack(EGridTrack.Percent, percent);

        public static GridTrack Auto() => new GridTrack(EGridTrack.Auto, 0f);

        /// <summary>Holds a measured size inside this track's own limits.</summary>
        // The minimum is applied after the maximum, so limits set the wrong way round leave the track at
        // its minimum rather than at its maximum - being too big is the failure that can still be seen.
        public float Clamp(float value)
        {
            if (max > 0f)
                value = Mathf.Min(value, max);

            return Mathf.Max(value, Mathf.Max(0f, min));
        }

        public GridTrack Copy() => new GridTrack(mode, size) { min = min, max = max };
    }
}
