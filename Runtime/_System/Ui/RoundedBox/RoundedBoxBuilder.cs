using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A way of saying what a box should look like in one go, rather than in a dozen assignments to a dozen
    // properties. The component keeps every value on its own so the inspector can lay them out corner by
    // corner and side by side; from code that same spread reads as noise, and this is the other face of it.
    //
    //     RoundedBoxBuilder.Create(panel, "Card")
    //         .Size(280f, 160f)
    //         .Fill(Color.white)
    //         .Corners(18f)
    //         .Border(2f, new Color(0.85f, 0.85f, 0.9f))
    //         .Done();
    //
    // A struct holding one reference, so a chain of these allocates nothing. Every step returns a fresh
    // copy pointing at the same box, and a step against a box that has gone is passed over rather than
    // thrown - a chain half built on a destroyed object is a bug worth surviving, not worth crashing on.
    public readonly struct RoundedBoxBuilder
    {
        private readonly RoundedBox box;

        private RoundedBoxBuilder(RoundedBox box)
        {
            this.box = box;
        }

        /// <summary>The box being built. Null only if it was destroyed part way through.</summary>
        public RoundedBox Box => box;

        /// <summary>Starts on a box that already exists.</summary>
        public static RoundedBoxBuilder For(RoundedBox existing) => new RoundedBoxBuilder(existing);

        /// <summary>Makes a new box under a parent and starts on that.</summary>
        // Built with its components named up front rather than added one by one: a Graphic with no
        // CanvasRenderer draws nothing, and Unity keeps that component out of reach of the inspector, so an
        // object that ends up without one is stuck that way.
        public static RoundedBoxBuilder Create(Transform parent, string name = "Rounded Box")
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RoundedBox));
            var rect = (RectTransform)created.transform;

            if (parent != null)
            {
                // WorldPositionStays off, or a rect dropped into a scaled canvas arrives keeping its world
                // size and comes out the wrong shape.
                rect.SetParent(parent, false);
                created.layer = parent.gameObject.layer;
            }

            return new RoundedBoxBuilder(created.GetComponent<RoundedBox>());
        }

        /// <summary>The finished box.</summary>
        public RoundedBox Done() => box;

        public static implicit operator RoundedBox(RoundedBoxBuilder builder) => builder.box;

        public RoundedBoxBuilder Size(float width, float height) => Size(new Vector2(width, height));

        public RoundedBoxBuilder Size(Vector2 size)
        {
            if (box != null)
                box.rectTransform.sizeDelta = size;

            return this;
        }

        /// <summary>Fills the parent, inset on every side by the same margin.</summary>
        public RoundedBoxBuilder Stretch(float margin = 0f)
        {
            if (box == null)
                return this;

            var rect = box.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
            return this;
        }

        public RoundedBoxBuilder At(Vector2 anchoredPosition)
        {
            if (box != null)
                box.rectTransform.anchoredPosition = anchoredPosition;

            return this;
        }

        /// <summary>A flat fill. Clears any gradient, since a colour is what was asked for.</summary>
        public RoundedBoxBuilder Fill(Color color)
        {
            if (box == null)
                return this;

            box.FillGradientMode = EFillGradient.None;
            box.FillColor = color;
            return this;
        }

        /// <summary>A gradient across the box - 0 degrees left to right, 90 bottom to top.</summary>
        public RoundedBoxBuilder Fill(Gradient gradient, float angle = 90f)
        {
            if (box == null)
                return this;

            box.FillGradientMode = EFillGradient.Linear;
            box.FillGradient = gradient;
            box.FillGradientAngle = angle;
            return this;
        }

        /// <summary>A gradient from the middle out to the edge, following the shape.</summary>
        public RoundedBoxBuilder RadialFill(Gradient gradient)
        {
            if (box == null)
                return this;

            box.FillGradientMode = EFillGradient.Radial;
            box.FillGradient = gradient;
            return this;
        }

        /// <summary>Multiplies the fill without replacing it - the way to fade a gradient.</summary>
        public RoundedBoxBuilder FillTint(Color tint)
        {
            if (box != null)
                box.FillColor = tint;

            return this;
        }

        public RoundedBoxBuilder Border(float size, Color color)
        {
            if (box == null)
                return this;

            box.SetBorderSize(size);
            box.SetBorderColor(color);
            return this;
        }

        public RoundedBoxBuilder Border(float left, float top, float right, float bottom)
        {
            if (box == null)
                return this;

            box.BorderLeft = left;
            box.BorderTop = top;
            box.BorderRight = right;
            box.BorderBottom = bottom;
            return this;
        }

        public RoundedBoxBuilder BorderColors(Color left, Color top, Color right, Color bottom)
        {
            if (box == null)
                return this;

            box.BorderColorLeft = left;
            box.BorderColorTop = top;
            box.BorderColorRight = right;
            box.BorderColorBottom = bottom;
            return this;
        }

        /// <summary>A gradient border, in place of the four side colours.</summary>
        public RoundedBoxBuilder BorderGradient(Gradient gradient, EBorderGradient mode = EBorderGradient.Linear, float angle = 90f)
        {
            if (box == null)
                return this;

            box.BorderGradientMode = mode;
            box.BorderGradient = gradient;
            box.BorderGradientAngle = angle;
            return this;
        }

        public RoundedBoxBuilder NoBorder()
        {
            if (box != null)
                box.SetBorderSize(0f);

            return this;
        }

        public RoundedBoxBuilder Corners(float radius)
        {
            if (box != null)
                box.SetCornerRadius(radius);

            return this;
        }

        public RoundedBoxBuilder Corners(float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            if (box == null)
                return this;

            box.RadiusTopLeft = topLeft;
            box.RadiusTopRight = topRight;
            box.RadiusBottomRight = bottomRight;
            box.RadiusBottomLeft = bottomLeft;
            return this;
        }

        /// <summary>Bites the corners out instead of rounding them off.</summary>
        public RoundedBoxBuilder Scoop(float size) => Corners(-Mathf.Abs(size));

        /// <summary>Rounds the ends off completely. The radius is held to the box, so this is a capsule.</summary>
        public RoundedBoxBuilder Pill() => Corners(100000f);

        /// <summary>Width of the fade that smooths the arcs. 0 leaves them stepped.</summary>
        public RoundedBoxBuilder Softness(float pixels)
        {
            if (box != null)
                box.EdgeSoftness = pixels;

            return this;
        }

        /// <summary>Straight pieces per corner arc. Raise it for corners that are large on screen.</summary>
        public RoundedBoxBuilder Segments(int segments)
        {
            if (box != null)
                box.CornerSegments = segments;

            return this;
        }

        public RoundedBoxBuilder Raycast(bool target)
        {
            if (box != null)
                box.raycastTarget = target;

            return this;
        }

        /// <summary>Multiplies everything the box draws - what a fade or an Animator should drive.</summary>
        public RoundedBoxBuilder Tint(Color tint)
        {
            if (box != null)
                box.color = tint;

            return this;
        }

        public RoundedBoxBuilder Material(Material material)
        {
            if (box != null)
                box.material = material;

            return this;
        }

        /// <summary>Clips everything inside the box to its shape.</summary>
        // Worth knowing before reaching for it: children draw over the box, so a picture put inside this
        // way covers the border. RoundedBoxBorderOverlay on a last child is what puts the border back on
        // top - the Add Masked Image button in the inspector builds that whole arrangement.
        public RoundedBoxBuilder Masked(bool showBox = true)
        {
            if (box == null)
                return this;

            var mask = box.GetComponent<Mask>();
            if (mask == null)
                mask = box.gameObject.AddComponent<Mask>();

            mask.showMaskGraphic = showBox;
            return this;
        }
    }
}
