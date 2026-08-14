using UnityEngine;

namespace FlappyTemplate
{
    // The glyphs in the navbar's buttons, drawn out of rounded boxes rather than fetched from an atlas -
    // the same reasoning as the window's close cross and the statistics window's circular arrow. Generated
    // geometry stays sharp at any size, costs no import and no texture memory, and follows the style's
    // colour without a second sprite having to be authored for a light theme.
    //
    // Two of them are drawn partly in the button's own colour: the door of the house and the tick on the
    // shield are painted over, not cut out. That is worth knowing before a gradient is put on a button -
    // against anything but a flat fill they would show as the wrong colour rather than disappear.
    //
    // Every part is named for the kind that drew it, and Draw clears whatever the last kind left behind.
    // So a slot switched from Statistics to Fairness in the inspector redraws rather than ending up with
    // both glyphs stacked, which is what naming parts and finding them by name would otherwise do.
    internal static class UiNavbarIcons
    {
        /// <summary>Draws the glyph for a kind inside a square that is already placed. Safe to call again:
        /// parts are found by name, and the ones belonging to another kind are taken away first.</summary>
        public static void Draw(RectTransform holder, ENavbarButton kind, float span, Color ink, Color hole, float thickness)
        {
            if (holder == null)
                return;

            Clear(holder, Prefix(kind));

            if (span <= 0f)
                return;

            float stroke = Mathf.Max(0.5f, thickness);

            switch (kind)
            {
                case ENavbarButton.Home:
                    Home(holder, span, ink, hole);
                    break;
                case ENavbarButton.Statistics:
                    Statistics(holder, span, ink);
                    break;
                case ENavbarButton.Fairness:
                    Fairness(holder, span, ink, hole, stroke);
                    break;
                default:
                    // Custom, which draws nothing at all: the game either drops a sprite on the slot or
                    // puts whatever it likes under the button itself.
                    break;
            }
        }

        /// <summary>What the parts of a kind's glyph are named. Empty for a kind that draws nothing, which
        /// is what makes <see cref="Clear"/> take everything away.</summary>
        private static string Prefix(ENavbarButton kind)
        {
            switch (kind)
            {
                case ENavbarButton.Home:
                    return "Home ";
                case ENavbarButton.Statistics:
                    return "Stat ";
                case ENavbarButton.Fairness:
                    return "Fair ";
                default:
                    return string.Empty;
            }
        }

        // Anything the last kind drew. Walked backwards because discarding a child in edit mode takes it
        // out of the list there and then.
        private static void Clear(RectTransform holder, string prefix)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
            {
                var child = holder.GetChild(i);
                if (prefix.Length == 0 || !child.name.StartsWith(prefix))
                    UiWindowParts.Discard(child.gameObject);
            }
        }

        // A house: a square roof turned on its corner, the body over its lower half so only the eaves show,
        // and a door painted on in the button's colour. Added in that order, since a child added later is
        // drawn later.
        private static void Home(RectTransform holder, float span, Color ink, Color hole)
        {
            // Half the roof's diagonal. Its top corner reaches the top of the square and its bottom corner
            // meets the top of the body, so the whole house is exactly the square it was given.
            const float RoofCenter = 0.20f;
            const float RoofHalf = 0.30f;

            float side = RoofHalf * 2f * span / Mathf.Sqrt(2f);

            var roof = Part(holder, "Home Roof", new Vector2(side, side), new Vector2(0f, RoofCenter * span), 45f);
            Paint(roof, ink, span * 0.04f);

            var body = Part(holder, "Home Body", new Vector2(0.52f * span, 0.40f * span), new Vector2(0f, -0.30f * span), 0f);
            Paint(body, ink, span * 0.05f);

            // Bottom-aligned inside the body rather than centred in it, so the door stands on the floor.
            var door = Part(holder, "Home Door", new Vector2(0.16f * span, 0.22f * span), new Vector2(0f, -0.39f * span), 0f);
            Paint(door, hole, span * 0.03f);
        }

        // Three bars standing on the bottom of the square, rising left to right. The plainest thing that
        // reads as figures at 28 pixels, which is what a navbar glyph is.
        private static void Statistics(RectTransform holder, float span, Color ink)
        {
            float width = 0.22f * span;
            float radius = width * 0.35f;

            Bar(holder, "Stat Bar 0", -0.33f * span, 0.45f * span, width, radius, span, ink);
            Bar(holder, "Stat Bar 1", 0f, 0.72f * span, width, radius, span, ink);
            Bar(holder, "Stat Bar 2", 0.33f * span, 1f * span, width, radius, span, ink);
        }

        private static void Bar(RectTransform holder, string name, float x, float height, float width, float radius, float span, Color ink)
        {
            // Every bar stands on the bottom edge, so a taller one grows upwards rather than out of both
            // ends of the square.
            float y = -span * 0.5f + height * 0.5f;

            var bar = Part(holder, name, new Vector2(width, height), new Vector2(x, y), 0f);
            Paint(bar, ink, radius);
        }

        // A shield with a tick on it: a badge with square shoulders and a round foot - corner radii rather
        // than a mesh - and two turned pills over it in the button's colour.
        private static void Fairness(RectTransform holder, float span, Color ink, Color hole, float thickness)
        {
            float width = 0.78f * span;
            float height = 0.94f * span;

            var shield = Part(holder, "Fair Shield", new Vector2(width, height), Vector2.zero, 0f);
            shield.FillGradientMode = EFillGradient.None;
            shield.FillColor = ink;
            shield.SetBorderSize(0f);
            shield.RadiusTopLeft = width * 0.22f;
            shield.RadiusTopRight = width * 0.22f;
            shield.RadiusBottomLeft = width * 0.5f;
            shield.RadiusBottomRight = width * 0.5f;
            shield.EdgeSoftness = 1f;
            shield.raycastTarget = false;

            // The tick as two pills meeting at its bottom point: a short arm down to it and a long one back
            // up. Both at 45 degrees, which is what keeps it looking drawn rather than calculated.
            float stroke = Mathf.Min(thickness, span * 0.16f);

            var shortArm = Part(holder, "Fair Tick A", new Vector2(0.24f * span, stroke), new Vector2(-0.11f * span, -0.05f * span), -45f);
            Paint(shortArm, hole, stroke);

            var longArm = Part(holder, "Fair Tick B", new Vector2(0.40f * span, stroke), new Vector2(0.06f * span, 0.02f * span), 45f);
            Paint(longArm, hole, stroke);
        }

        // One box of the glyph, placed in the middle of the square and turned. Rotation is written every
        // time rather than only when the box is made, or a part reused from a redraw at another angle would
        // keep the old one.
        private static RoundedBox Part(RectTransform holder, string name, Vector2 size, Vector2 offset, float angle)
        {
            var box = UiWindowParts.Box(holder, name);

            UiWindowParts.Pin(box.rectTransform, new Vector2(0.5f, 0.5f), size, offset);
            box.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            return box;
        }

        private static void Paint(RoundedBox box, Color fill, float radius)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = fill;
            box.SetBorderSize(0f);
            box.SetCornerRadius(Mathf.Max(0f, radius));
            box.EdgeSoftness = 1f;

            // Nothing in a glyph catches a click: the press belongs to the button underneath it, and a
            // part that took it would leave a dead spot in the middle of the icon.
            box.raycastTarget = false;
        }
    }
}
