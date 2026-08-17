namespace FlappyTemplate
{
    // Whether the constraint puts this object *on* the target or keeps the gap it already had. Both are
    // worth having and they are not interchangeable: the first is how you pin a label to a peg, the
    // second is how a camera trails a ball from three units back without you working out the maths.
    public enum EConstraintMode
    {
        // Take the target's value as your own. The object jumps onto the target the moment the
        // constraint runs, which is what a position or rotation constraint is usually asked for.
        Absolute,

        // Keep the gap between the rest pose and where the target stood when the constraint started
        // following it. The object never jumps: it holds its authored separation and the target's
        // movement is passed on to it. Blender calls this Keep Transform and Unity's own constraints
        // call it Maintain Offset - the same idea.
        Relative,
    }
}
