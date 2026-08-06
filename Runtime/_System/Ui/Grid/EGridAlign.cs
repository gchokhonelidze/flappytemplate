namespace FlappyTemplate
{
    /// <summary>Where an item sits inside its cell along one axis.</summary>
    // Stretch is the one that drives the item's size; the other three leave the size alone and only
    // place it - which is what makes a cell able to hold something smaller than itself.
    public enum EGridAlign
    {
        /// <summary>Fills the cell. The item's width or height is written for it.</summary>
        Stretch,

        /// <summary>Left, or top.</summary>
        Start,

        /// <summary>Centred in the cell.</summary>
        Center,

        /// <summary>Right, or bottom.</summary>
        End,
    }
}
