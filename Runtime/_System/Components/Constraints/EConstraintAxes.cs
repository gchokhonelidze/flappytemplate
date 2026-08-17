using System;

namespace FlappyTemplate
{
    // Which axes a constraint is allowed to write. An unticked axis is not read, not blended and not
    // written, so whatever else owns it - an animation, another script, your own hand in the scene view -
    // stays in charge of it. That is what turns "follow the ball's x but keep my own height" into two
    // clicks instead of a script.
    [Flags]
    public enum EConstraintAxes
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
        All = X | Y | Z,
    }
}
