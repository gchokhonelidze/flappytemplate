using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // The look a window part is given at the moment it is made, and never again. A window styles nothing
    // after this - colours, corners, borders and fonts are the parts' own, set in the inspector like any
    // other RoundedBox or label - but a window that arrived as a white square with a white caption on it
    // would read as broken rather than as unstyled. So each part is born looking like something, and from
    // then on it is whatever anybody has made of it.
    //
    // A dark violet dialog, which is the one the readme is written around. A game with its own palette
    // selects Panel, Caption, Title and Close and sets them to whatever it likes; nothing here runs again.
    internal static class UiWindowSeed
    {
        public static void Panel(RoundedBox box)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = new Color(0.357f, 0.298f, 0.62f);
            box.SetCornerRadius(26f);
            box.SetBorderSize(3f);
            box.SetBorderColor(new Color(0.227f, 0.18f, 0.42f));
            box.EdgeSoftness = 1.25f;
            box.raycastTarget = true;
        }

        // Drawn over the panel's fill rather than instead of it, so the header is the same violet a shade
        // darker. Square at the bottom and round at the top, inside the panel's own border.
        public static void Caption(RoundedBox box)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = new Color(0f, 0f, 0f, 0.12f);
            box.SetBorderSize(0f);
            box.RadiusTopLeft = 23f;
            box.RadiusTopRight = 23f;
            box.RadiusBottomRight = 0f;
            box.RadiusBottomLeft = 0f;
            box.EdgeSoftness = 1.25f;
            box.raycastTarget = true;
        }

        // Below the close button rather than beside it: the inset from the top is what leaves the corner
        // free, and a title centred under a button that is over it reads as one row.
        public static void Title(TextMeshProUGUI label)
        {
            UiWindowParts.Stretch(label.rectTransform, 12f, 44f, 12f, 6f);
            label.fontSize = 34f;
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }

        public static void Close(RoundedBox box)
        {
            UiWindowParts.Pin(box.rectTransform, new Vector2(1f, 1f), new Vector2(44f, 44f), new Vector2(-18f, -18f));

            box.FillGradientMode = EFillGradient.None;
            box.FillColor = Color.white;
            box.SetBorderSize(0f);

            // A radius larger than the box is held to it, so this stays a circle whatever the button is
            // resized to afterwards.
            box.SetCornerRadius(100000f);
            box.EdgeSoftness = 1.25f;
            box.raycastTarget = true;
        }

        /// <summary>The square the two bars are drawn in, sized from the button it sits in.</summary>
        public static void Cross(RectTransform rect, Vector2 closeSize)
        {
            float span = Mathf.Min(closeSize.x, closeSize.y) * 0.42f;
            UiWindowParts.Pin(rect, new Vector2(0.5f, 0.5f), new Vector2(span, span), Vector2.zero);
        }

        /// <summary>One arm of the cross: a pill across the middle, turned. Two of them at opposite angles
        /// make the cross, which costs no atlas entry and stays sharp at any size.</summary>
        public static void Bar(RoundedBox bar, float span, float angle)
        {
            UiWindowParts.Pin(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(span, 4.5f), Vector2.zero);
            bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            bar.FillGradientMode = EFillGradient.None;
            bar.FillColor = new Color(0.18f, 0.16f, 0.35f);
            bar.SetBorderSize(0f);
            bar.SetCornerRadius(100000f);
            bar.EdgeSoftness = 1.25f;
            bar.raycastTarget = false;
        }

        public static void ScrollTrack(RoundedBox box) => Bar(box, new Color(0f, 0f, 0f, 0.22f));

        public static void ScrollHandle(RoundedBox box) => Bar(box, new Color(1f, 1f, 1f, 0.5f));

        public static void Backdrop(Image sheet) => sheet.color = new Color(0f, 0f, 0f, 0.55f);

        // Track and handle are the same shape in two colours: fully rounded, so each reads as a bar rather
        // than as a strip however wide the scrollbar is set.
        private static void Bar(RoundedBox box, Color fill)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = fill;
            box.SetBorderSize(0f);
            box.SetCornerRadius(100000f);
            box.EdgeSoftness = 1.25f;
        }
    }
}
