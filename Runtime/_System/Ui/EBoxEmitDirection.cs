namespace FlappyTemplate
{
    /// <summary>Which way particles set off from the part of a RoundedBox they spawned on.</summary>
    // Written into the shape mesh as its normals, which is where a particle system takes a particle's
    // starting direction from. Nothing happens until Start Speed is above zero - that is what turns a
    // direction into movement.
    public enum EBoxEmitDirection
    {
        /// <summary>Away from the box, square to the edge it spawned on - or away from the middle, spawning from the body.</summary>
        Outward = 0,

        /// <summary>Back into the box, the same line the other way.</summary>
        Inward = 1,

        /// <summary>Along the edge rather than off it, travelling around the outline - a swirl, spawning from the body.</summary>
        Around = 2,
    }
}
