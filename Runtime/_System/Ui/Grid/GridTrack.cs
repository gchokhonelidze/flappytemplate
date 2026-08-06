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

        /// <summary>Reads a track written the way a layout writes one.</summary>
        // The forms are the ones CSS uses, so they are already familiar: 120 is canvas units, 1fr a share,
        // 25% a percentage and auto its contents. A range in brackets adds the limits - 1fr[160..400] is a
        // share that will not go below 160 or past 400, and either end may be left out.
        public static bool TryParse(string token, out GridTrack track)
        {
            track = null;
            if (string.IsNullOrEmpty(token))
                return false;

            token = token.Trim();

            float min = 0f;
            float max = 0f;

            int open = token.IndexOf('[');
            if (open >= 0)
            {
                int close = token.IndexOf(']', open);
                if (close < 0)
                    return false;

                var range = token.Substring(open + 1, close - open - 1);
                token = token.Substring(0, open).Trim();

                int dots = range.IndexOf("..", System.StringComparison.Ordinal);
                if (dots >= 0)
                {
                    Number(range.Substring(0, dots), out min);
                    Number(range.Substring(dots + 2), out max);
                }
                else
                {
                    Number(range, out min);
                }
            }

            if (token.Length == 0)
                return false;

            EGridTrack mode;
            float size;

            if (token == "*")
            {
                mode = EGridTrack.Flexible;
                size = 1f;
            }
            else if (token.Equals("auto", System.StringComparison.OrdinalIgnoreCase))
            {
                mode = EGridTrack.Auto;
                size = 0f;
            }
            else if (token.EndsWith("fr", System.StringComparison.OrdinalIgnoreCase))
            {
                mode = EGridTrack.Flexible;

                // "fr" on its own is one share, the way CSS reads a bare fr in a shorthand.
                if (!Number(token.Substring(0, token.Length - 2), out size))
                    size = 1f;
            }
            else if (token.EndsWith("%", System.StringComparison.Ordinal))
            {
                mode = EGridTrack.Percent;
                if (!Number(token.Substring(0, token.Length - 1), out size))
                    return false;
            }
            else
            {
                mode = EGridTrack.Fixed;

                // px and u are both allowed to be written and both mean canvas units; there is only one
                // unit here, and refusing the word for it would only be pedantry.
                var number = token;
                if (number.EndsWith("px", System.StringComparison.OrdinalIgnoreCase))
                    number = number.Substring(0, number.Length - 2);
                else if (number.EndsWith("u", System.StringComparison.OrdinalIgnoreCase))
                    number = number.Substring(0, number.Length - 1);

                if (!Number(number, out size))
                    return false;
            }

            track = new GridTrack(mode, size) { min = min, max = max };
            return true;
        }

        /// <summary>Writes this track the way a layout writes one, so a layout survives a round trip.</summary>
        public string ToToken()
        {
            string core;
            switch (mode)
            {
                case EGridTrack.Fixed:
                    core = Text(size);
                    break;

                case EGridTrack.Percent:
                    core = Text(size) + "%";
                    break;

                case EGridTrack.Auto:
                    core = "auto";
                    break;

                default:
                    core = Text(size) + "fr";
                    break;
            }

            if (min <= 0f && max <= 0f)
                return core;

            if (max <= 0f)
                return core + "[" + Text(min) + "]";

            return core + "[" + (min > 0f ? Text(min) : string.Empty) + ".." + Text(max) + "]";
        }

        private static bool Number(string text, out float value)
        {
            return float.TryParse(text.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        // Invariant, because a layout string is written in code and read in an inspector, and a machine set
        // to a comma decimal separator would otherwise write one that no longer parses.
        private static string Text(float value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
