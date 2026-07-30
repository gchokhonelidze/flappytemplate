namespace FlappyTemplate
{
    // What a size limit is measured in.
    public enum ESizeUnits
    {
        // The numbers the RectTransform inspector shows as Width and Height. A CanvasScaler set to
        // Scale With Screen Size keeps these roughly fixed, which is exactly when a limit in these
        // units never triggers.
        CanvasUnits,

        // Real screen pixels, converted through the canvas scale factor.
        ScreenPixels,
    }
}
