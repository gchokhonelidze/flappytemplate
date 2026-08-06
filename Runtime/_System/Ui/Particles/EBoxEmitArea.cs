namespace FlappyTemplate
{
    /// <summary>Which part of a RoundedBox a particle effect spawns from.</summary>
    public enum EBoxEmitArea
    {
        /// <summary>The frame itself. Sides with no thickness are left out, so a border of nothing emits nothing.</summary>
        Border = 0,

        /// <summary>The whole body, out to the outline - the shape as it is drawn, corners and all.</summary>
        Fill = 1,

        /// <summary>The body within the border, which is the same as Fill wherever there is no border to stay inside of.</summary>
        Inside = 2,
    }
}
