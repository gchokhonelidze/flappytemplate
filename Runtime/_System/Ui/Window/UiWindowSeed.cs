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
    // The flat charcoal the bet info sheet is drawn in: a near-black panel with a hairline round it, white
    // text, and a close button that is a faint disc with a white cross. A game with its own palette selects
    // Panel, Caption, Title and Close and sets them to whatever it likes; nothing here runs again - unless
    // UiWindowTheme.Charcoal is asked to, which repaints a window that was made before this was the look.
    //
    // Where a part goes and what it looks like are separate methods, so a repaint changes the colours without
    // moving anything a game has placed.
    internal static class UiWindowSeed
    {
        public static readonly Color PanelFill = new Color(0.106f, 0.106f, 0.106f);

        public static void Panel(RoundedBox box)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = PanelFill;
            box.SetCornerRadius(18f);
            box.SetBorderSize(1f);
            box.SetBorderColor(new Color(1f, 1f, 1f, 0.06f));
            box.EdgeSoftness = 1.25f;
            box.raycastTarget = true;
        }

        // Clear over the panel, with a hairline under it: the header is the same charcoal as the body, set off
        // by a rule rather than by a second shade. Round at the top, inside the panel's own border.
        public static void Caption(RoundedBox box)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = new Color(1f, 1f, 1f, 0f);
            box.SetBorderSize(0f);
            box.BorderBottom = 1f;
            box.BorderColorBottom = new Color(1f, 1f, 1f, 0.08f);
            box.RadiusTopLeft = 17f;
            box.RadiusTopRight = 17f;
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
            label.alignment = TextAlignmentOptions.Center;
            TitleLook(label);
        }

        public static void TitleLook(TextMeshProUGUI label)
        {
            label.fontSize = 26f;
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
        }

        public static void Close(RoundedBox box)
        {
            UiWindowParts.Pin(box.rectTransform, new Vector2(1f, 1f), new Vector2(44f, 44f), new Vector2(-18f, -18f));
            CloseLook(box);
        }

        public static void CloseLook(RoundedBox box)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = new Color(1f, 1f, 1f, 0.08f);
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
            float span = Mathf.Min(closeSize.x, closeSize.y) * 0.4f;
            UiWindowParts.Pin(rect, new Vector2(0.5f, 0.5f), new Vector2(span, span), Vector2.zero);
        }

        /// <summary>One arm of the cross: a pill across the middle, turned. Two of them at opposite angles
        /// make the cross, which costs no atlas entry and stays sharp at any size.</summary>
        public static void Bar(RoundedBox bar, float span, float angle)
        {
            UiWindowParts.Pin(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(span, 3f), Vector2.zero);
            bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            BarLook(bar);
        }

        public static void BarLook(RoundedBox bar)
        {
            bar.FillGradientMode = EFillGradient.None;
            bar.FillColor = new Color(1f, 1f, 1f, 0.85f);
            bar.SetBorderSize(0f);
            bar.SetCornerRadius(100000f);
            bar.EdgeSoftness = 1.25f;
            bar.raycastTarget = false;
        }

        public static void ScrollTrack(RoundedBox box) => Bar(box, new Color(1f, 1f, 1f, 0.06f));

        public static void ScrollHandle(RoundedBox box) => Bar(box, new Color(1f, 1f, 1f, 0.3f));

        public static void Backdrop(Image sheet) => sheet.color = new Color(0f, 0f, 0f, 0.6f);

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
