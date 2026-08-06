namespace FlappyTemplate
{
    /// <summary>How a RoundedBox colours its border.</summary>
    public enum EBorderGradient
    {
        /// <summary>Each side keeps its own colour, blending into its neighbours across the corners.</summary>
        None = 0,

        /// <summary>One ramp across the whole box at a set angle, the same one the fill would use.</summary>
        Linear = 1,

        /// <summary>One ramp travelling around the outline, starting at the top left corner and going clockwise.</summary>
        Around = 2,

        /// <summary>Across the thickness of each side, outer edge to inner - the moulding of a picture frame, where the ramp turns with the edge it is on rather than running one way for the whole box.</summary>
        Frame = 3,
    }
}
