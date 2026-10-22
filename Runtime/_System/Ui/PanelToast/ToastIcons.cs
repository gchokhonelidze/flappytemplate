using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // The sign on the left of a toast, drawn out of a triangle and rounded boxes rather than fetched from an
    // atlas - the same reasoning as the navbar's glyphs and the window's close cross. Generated geometry stays
    // sharp at any size, costs no import, and follows the style's colour without a second sprite having to be
    // authored for each palette.
    //
    // The mark on each sign - the bang, the tick, the i - is painted in the pill's own colour rather than cut
    // out. Against the flat pill a toast is, the two are the same thing; against a translucent one the mark
    // would show as the pill's colour rather than as a hole, which is why the style asks for an opaque fill.
    //
    // Every part is named for the kind that drew it, and Draw clears whatever the last kind left behind, so a
    // toast shown as a warning and then as a success redraws rather than stacking one sign on another.
    internal static class ToastIcons
    {
        /// <summary>Draws the sign for a kind inside a square that is already placed. Safe to call again: parts
        /// are found by name, and the ones belonging to another kind are taken away first.</summary>
        public static void Draw(RectTransform holder, EToastType kind, float span, Color ink, Color hole)
        {
            if (holder == null)
                return;

            Clear(holder, Prefix(kind));

            if (span <= 0f)
                return;

            switch (kind)
            {
                case EToastType.Warning:
                    Warning(holder, span, ink, hole);
                    break;
                case EToastType.Success:
                    Success(holder, span, ink, hole);
                    break;
                case EToastType.Error:
                    Error(holder, span, ink, hole);
                    break;
                default:
                    Info(holder, span, ink, hole);
                    break;
            }
        }

        private static string Prefix(EToastType kind)
        {
            switch (kind)
            {
                case EToastType.Warning:
                    return "Warn ";
                case EToastType.Success:
                    return "Done ";
                case EToastType.Error:
                    return "Fail ";
                default:
                    return "Info ";
            }
        }

        // Anything the last kind drew. Walked backwards because discarding a child in edit mode takes it out of
        // the list there and then.
        private static void Clear(RectTransform holder, string prefix)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
            {
                var child = holder.GetChild(i);
                if (!child.name.StartsWith(prefix))
                    UiWindowParts.Discard(child.gameObject);
            }
        }

        // A warning sign: a rounded triangle with a bang on it. The bang sits a little under the middle of the
        // square, where the triangle's own middle is - a triangle carries most of itself in its lower half.
        private static void Warning(RectTransform holder, float span, Color ink, Color hole)
        {
            var sign = Triangle(holder, "Warn Sign");
            UiWindowParts.Pin(sign.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(span, span * 0.92f), new Vector2(0f, -0.02f * span));
            sign.color = ink;
            sign.CornerRadius = span * 0.14f;
            sign.EdgeSoftness = 1.25f;
            sign.raycastTarget = false;

            Bang(holder, "Warn", span * 0.11f, new Vector2(0f, -0.1f * span), span * 0.28f, span * 0.24f, hole);
        }

        // A disc with a tick on it: two pills meeting at the lowest point of the tick, a short arm down to it
        // and a long one back up, both at forty-five degrees.
        private static void Success(RectTransform holder, float span, Color ink, Color hole)
        {
            Disc(holder, "Done Disc", span, ink);

            float stroke = span * 0.12f;
            var shortArm = Part(holder, "Done Tick A", new Vector2(0.28f * span, stroke), new Vector2(-0.13f * span, -0.04f * span), -45f);
            Paint(shortArm, hole, stroke);

            var longArm = Part(holder, "Done Tick B", new Vector2(0.46f * span, stroke), new Vector2(0.07f * span, 0.03f * span), 45f);
            Paint(longArm, hole, stroke);
        }

        // A disc with a bang on it.
        private static void Error(RectTransform holder, float span, Color ink, Color hole)
        {
            Disc(holder, "Fail Disc", span, ink);
            Bang(holder, "Fail", span * 0.12f, Vector2.zero, span * 0.32f, span * 0.28f, hole);
        }

        // A disc with an i on it: the bang turned over, dot above bar.
        private static void Info(RectTransform holder, float span, Color ink, Color hole)
        {
            Disc(holder, "Info Disc", span, ink);

            float stroke = span * 0.12f;
            var dot = Part(holder, "Info Dot", new Vector2(stroke, stroke), new Vector2(0f, 0.22f * span), 0f);
            Paint(dot, hole, stroke);

            var bar = Part(holder, "Info Bar", new Vector2(stroke, 0.34f * span), new Vector2(0f, -0.08f * span), 0f);
            Paint(bar, hole, stroke);
        }

        private static void Disc(RectTransform holder, string name, float span, Color ink)
        {
            var disc = Part(holder, name, new Vector2(span, span), Vector2.zero, 0f);
            Paint(disc, ink, span);
        }

        // An exclamation mark: a bar standing over a dot, measured about a middle so each sign can put it where
        // its own shape has room. Reach is from the middle to the top of the bar and to the bottom of the dot.
        private static void Bang(RectTransform holder, string prefix, float stroke, Vector2 middle, float reach, float barLength, Color hole)
        {
            var bar = Part(holder, prefix + " Bar", new Vector2(stroke, barLength), middle + new Vector2(0f, reach - barLength * 0.5f), 0f);
            Paint(bar, hole, stroke);

            var dot = Part(holder, prefix + " Dot", new Vector2(stroke, stroke), middle + new Vector2(0f, -reach + stroke * 0.5f), 0f);
            Paint(dot, hole, stroke);
        }

        // One box of the sign, placed in the middle of the square and turned. Rotation is written every time
        // rather than only when the box is made, or a part reused from a redraw at another angle would keep the
        // old one.
        private static RoundedBox Part(RectTransform holder, string name, Vector2 size, Vector2 offset, float angle)
        {
            var box = UiWindowParts.Box(holder, name);
            UiWindowParts.Pin(box.rectTransform, new Vector2(0.5f, 0.5f), size, offset);
            box.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            return box;
        }

        private static UiTriangle Triangle(RectTransform holder, string name)
        {
            var found = UiWindowParts.Find<UiTriangle>(holder, name);
            if (found != null)
                return found;

            // CanvasRenderer named up front: a Graphic without one draws nothing, and it cannot be added
            // afterwards from the inspector.
            var created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UiTriangle));
            var rect = (RectTransform)created.transform;
            rect.SetParent(holder, false);
            created.layer = holder.gameObject.layer;
            return created.GetComponent<UiTriangle>();
        }

        private static void Paint(RoundedBox box, Color fill, float radius)
        {
            box.FillGradientMode = EFillGradient.None;
            box.FillColor = fill;
            box.SetBorderSize(0f);
            box.SetCornerRadius(Mathf.Max(0f, radius));
            box.EdgeSoftness = 1.25f;

            // Nothing in a sign catches a click: the pill underneath takes it, and a part that took it would
            // leave a dead spot in the middle of the icon.
            box.raycastTarget = false;
        }
    }
}
