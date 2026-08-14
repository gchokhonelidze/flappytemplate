namespace FlappyTemplate
{
    /// <summary>What a button in the navbar does, and which glyph it is drawn with.</summary>
    // The kind is the whole of what a slot knows about itself: the bar looks up what to draw and what to do
    // from it, so adding a button to the bar is adding a value here and the two cases that go with it - one
    // in UiNavbarIcons.Draw, one in UiNavbar.Press.
    public enum ENavbarButton
    {
        /// <summary>Leaves the game for <see cref="SystemDto.ReturnUrl"/>, taking the whole page with it
        /// rather than the frame the build is drawn in. Hides itself while the server has sent no such
        /// address - see <see cref="Navigator"/>.</summary>
        Home,

        /// <summary>Opens the statistics window, or closes it again.</summary>
        Statistics,

        /// <summary>Opens the fairness window, or closes it again.</summary>
        Fairness,

        /// <summary>Nothing of its own: an empty button that fires <see cref="UiNavbar.OnPressed"/> and the
        /// slot's own event, for whatever the game wants in the row.</summary>
        Custom,
    }
}
