namespace FlappyTemplate
{
    // Which of the rect's axes an object is scaled to fit.
    public enum EFitMode
    {
        None,

        // Match the rect's width; the object may overflow the rect vertically.
        Width,

        // Match the rect's height; the object may overflow the rect horizontally.
        Height,

        // Stay inside both - the tighter axis wins, so the object always fits with room to spare on
        // the other one. Proportions are kept either way, scaling is always uniform.
        Both,
    }
}
