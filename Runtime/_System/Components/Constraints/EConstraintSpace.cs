namespace FlappyTemplate
{
    // The frame a constraint reads its targets in, writes its result in, and masks its axes in. It is the
    // same choice as world versus local anywhere else in Unity, but because a constraint reads one
    // transform and writes another, it decides two things at once.
    public enum EConstraintSpace
    {
        // Reads the target's world position or rotation and writes this object's. The usual choice: the
        // two objects can sit anywhere in the hierarchy and the object still ends up on the target.
        World,

        // Reads the target's localPosition or localRotation and writes this object's - "hold the same
        // place relative to your parent as the target holds relative to its own". For siblings under one
        // parent that is identical to World and cheaper to reason about; for objects under different
        // parents it copies the relationship rather than the location, which is what you want when two
        // scaled or rotated holders each carry a copy of the same layout.
        Local,
    }
}
