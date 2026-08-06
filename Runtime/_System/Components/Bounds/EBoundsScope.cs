namespace FlappyTemplate
{
    // How much of an object counts as its bounds.
    public enum EBoundsScope
    {
        // Only what the object itself draws - its own SpriteRenderer or mesh. The safe default for a
        // sprite acting as a frame or a background: nothing else parented under it can change the
        // measured size.
        Renderer,

        // The object plus everything drawn beneath it, so a parent that is only an empty holding a
        // handful of sprites still measures as the box those sprites occupy.
        Hierarchy,
    }
}
