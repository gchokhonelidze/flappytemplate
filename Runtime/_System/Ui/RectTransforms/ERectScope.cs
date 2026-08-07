namespace FlappyTemplate
{
    // How much of a UI panel counts as the thing being fitted.
    public enum ERectScope
    {
        // The panel's own rect - its Width and Height as the inspector shows them. The right choice for
        // a panel authored at a fixed design size, whose children were laid out to sit inside it.
        Rect,

        // The panel plus every active child rect, so a panel that is only a container still measures as
        // the box its children spread over - including anything hanging outside its own rect.
        Content,
    }
}
