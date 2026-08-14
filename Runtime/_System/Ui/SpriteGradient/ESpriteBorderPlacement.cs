namespace FlappyTemplate
{
    /// <summary>Where a SpriteGradient's border sits against the sprite's own silhouette.</summary>
    public enum ESpriteBorderPlacement
    {
        /// <summary>Grown outward from the silhouette, so the picture is left whole and the shape gets bigger.</summary>
        Outside = 0,

        /// <summary>Half out and half in, with the silhouette running down the middle of the border.</summary>
        Center = 1,

        /// <summary>Grown inward, so the shape keeps its size and the border is painted over the picture's edge.</summary>
        Inside = 2,
    }
}
