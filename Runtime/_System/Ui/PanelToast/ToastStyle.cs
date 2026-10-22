using System;
using TMPro;
using UnityEngine;

namespace FlappyTemplate
{
    /// <summary>The two colours a kind of toast is drawn in: the pill, and the ink of everything on it.</summary>
    [Serializable]
    public struct ToastPalette
    {
        [Tooltip("The pill itself. Keep it opaque - the marks on the icon are painted in this colour, not cut out, so anything behind a translucent pill shows through the pill and not through the mark.")]
        public Color Fill;

        [Tooltip("The icon, the text and the close cross.")]
        public Color Ink;

        public ToastPalette(Color fill, Color ink)
        {
            Fill = fill;
            Ink = ink;
        }
    }

    // What a toast looks like: one pill, one icon on its left, text beside it, and a close cross on the right
    // when one is asked for. Four palettes, one per kind, and the rest is geometry shared by all of them.
    //
    // The defaults are the design this was drawn from: a dark pill tinted towards its ink, with the ink
    // itself a bright yellow, green or red. Each pill is a near-black that leans the same way as its ink, so
    // the three read as one family rather than as three unrelated boxes.
    [Serializable]
    public class ToastStyle
    {
        // The sizes are the design's own, measured off it: a pill 64 tall with corners of 8, a sign of 28 set
        // 18 in from the left edge, the text 11 past the sign with capitals 10 tall - which is a 14 point face.
        [Header("Pill")]
        [Min(0f)]
        public float CornerRadius = 8f;

        [Tooltip("The pill is never shorter than this, however little text is on it.")]
        [Min(0f)]
        public float MinHeight = 64f;

        [Tooltip("Text wider than fits inside this wraps, and the pill grows downwards instead.")]
        [Min(80f)]
        public float MaxWidth = 460f;

        [Tooltip("Inside the pill, left and right.")]
        [Min(0f)]
        public float PaddingX = 18f;

        [Tooltip("Inside the pill, above and below the text - felt only once the text is tall enough to push past Min Height.")]
        [Min(0f)]
        public float PaddingY = 18f;

        [Tooltip("From the top right corner of the canvas to the top right corner of the pill.")]
        public Vector2 Inset = new Vector2(20f, 20f);

        [Header("Icon")]
        [Min(0f)]
        public float IconSize = 28f;

        [Tooltip("Between the icon and the text.")]
        [Min(0f)]
        public float IconGap = 11f;

        [Header("Text")]
        [Tooltip("Leave empty for the TextMeshPro default.")]
        public TMP_FontAsset Font;

        [Min(1f)]
        public float TitleSize = 16f;

        public FontStyles TitleStyle = FontStyles.Bold;

        [Min(1f)]
        public float MessageSize = 14f;

        public FontStyles MessageStyle = FontStyles.Normal;

        [Tooltip("Between the title and the message under it.")]
        [Min(0f)]
        public float TitleGap = 2f;

        [Header("Close")]
        [Min(0f)]
        public float CloseSize = 28f;

        [Tooltip("Between the text and the close button.")]
        [Min(0f)]
        public float CloseGap = 10f;

        [Min(0.5f)]
        public float CloseThickness = 2f;

        [Tooltip("The disc under the cross, as a share of the ink's alpha.")]
        [Range(0f, 1f)]
        public float CloseDisc = 0.14f;

        [Tooltip("A ripple round the close button when it is pressed, the same as a window's.")]
        public bool CloseWave = true;

        [Header("Motion")]
        [Tooltip("How far past the edge of the canvas the pill starts from, beyond its own width.")]
        [Min(0f)]
        public float Slack = 24f;

        // Written as linear values, which is what a Color is in a project rendering in linear colour space -
        // the one this template runs in - so that what reaches the screen is the design's own colour, read off
        // it: #292200 under #FFE766 for a warning, #081B0D under #46E46E for a success, #1F0504 under #F56A55
        // for an error. Typed in as those hex values they would come out far too light. The plain kind is the
        // windows' charcoal under white.
        [Header("Colours")]
        public ToastPalette Default = new ToastPalette(new Color(0.106f, 0.106f, 0.106f), Color.white);

        public ToastPalette Error = new ToastPalette(new Color(0.0137f, 0.0015f, 0.0012f), new Color(0.913f, 0.144f, 0.091f));

        public ToastPalette Warning = new ToastPalette(new Color(0.0222f, 0.016f, 0f), new Color(1f, 0.799f, 0.133f));

        public ToastPalette Success = new ToastPalette(new Color(0.0024f, 0.011f, 0.004f), new Color(0.061f, 0.776f, 0.156f));

        /// <summary>The palette for a kind.</summary>
        public ToastPalette Palette(EToastType kind)
        {
            switch (kind)
            {
                case EToastType.Error:
                    return Error;
                case EToastType.Warning:
                    return Warning;
                case EToastType.Success:
                    return Success;
                default:
                    return Default;
            }
        }

        /// <summary>A copy, for a toast that wants its own colours without editing the shared style.</summary>
        // Every field is a value or a reference to something the style does not own - a font - so a shallow
        // copy is a whole copy.
        public ToastStyle Clone() => (ToastStyle)MemberwiseClone();
    }
}
