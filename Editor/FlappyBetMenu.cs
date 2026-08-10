namespace FlappyTemplate.Editor
{
    // One group for everything this template adds to GameObject > UI (Canvas). Held in a const rather than
    // typed into each MenuItem, because a menu path is matched as text: a submenu whose name is spelled two
    // ways is two submenus, and there is no compiler to say so.
    //
    // The entries inside it are spaced ten apart per kind - boxes, grids, windows, the history strip - since
    // Unity draws a separator wherever two neighbouring priorities differ by more than ten. That is the whole
    // reason the numbers below are not consecutive.
    internal static class FlappyBetMenu
    {
        /// <summary>The submenu everything goes in. Ends in a slash, so an entry is Group + its own name.</summary>
        public const string Group = "GameObject/UI (Canvas)/FlappyBet/";

        // Image and Raw Image sit at 2001 and 2002 and Panel at 2020, so this lands past UGUI's own entries
        // rather than in the middle of them. The number decides where the *submenu* sits in the UI menu as
        // well as where the first entry sits inside it.
        public const int Priority = 2100;
    }
}
