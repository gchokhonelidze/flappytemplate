using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A way of saying what a sprite should look like in one go, rather than in a dozen assignments to a
    // dozen properties. The component keeps every value on its own so the inspector can lay them out; from
    // code that same spread reads as noise, and this is the other face of it.
    //
    //     SpriteGradientBuilder.Create(panel, "Coin")
    //         .Picture(coinSprite)
    //         .Size(96f, 96f)
    //         .Fill(gold, 90f)
    //         .Border(3f, new Color(0.35f, 0.2f, 0f))
    //         .Done();
    //
    // A struct holding one reference, so a chain of these allocates nothing. Every step returns a fresh copy
    // pointing at the same graphic, and a step against one that has gone is passed over rather than thrown -
    // a chain half built on a destroyed object is a bug worth surviving, not worth crashing on.
    public readonly struct SpriteGradientBuilder
    {
        private readonly SpriteGradient graphic;

        private SpriteGradientBuilder(SpriteGradient graphic)
        {
            this.graphic = graphic;
        }

        /// <summary>The graphic being built. Null only if it was destroyed part way through.</summary>
        public SpriteGradient Graphic => graphic;

        /// <summary>Starts on one that already exists.</summary>
        public static SpriteGradientBuilder For(SpriteGradient existing) => new SpriteGradientBuilder(existing);

        /// <summary>Makes a new one under a parent and starts on that.</summary>
        // Built with its components named up front rather than added one by one: a Graphic with no
        // CanvasRenderer draws nothing, and Unity keeps that component out of reach of the inspector, so an
        // object that ends up without one is stuck that way.
        public static SpriteGradientBuilder Create(Transform parent, string name = "Sprite Gradient")
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(SpriteGradient));
            var rect = (RectTransform)created.transform;

            if (parent != null)
            {
                // WorldPositionStays off, or a rect dropped into a scaled canvas arrives keeping its world
                // size and comes out the wrong shape.
                rect.SetParent(parent, false);
                created.layer = parent.gameObject.layer;
            }

            return new SpriteGradientBuilder(created.GetComponent<SpriteGradient>());
        }

        /// <summary>The finished graphic.</summary>
        public SpriteGradient Done() => graphic;

        public static implicit operator SpriteGradient(SpriteGradientBuilder builder) => builder.graphic;

        /// <summary>The picture, whose alpha becomes the shape everything else here is measured against.</summary>
        public SpriteGradientBuilder Picture(Sprite sprite, bool preserveAspect = false)
        {
            if (graphic == null)
                return this;

            graphic.Sprite = sprite;
            graphic.PreserveAspect = preserveAspect;
            return this;
        }

        public SpriteGradientBuilder Size(float width, float height) => Size(new Vector2(width, height));

        public SpriteGradientBuilder Size(Vector2 size)
        {
            if (graphic != null)
                graphic.rectTransform.sizeDelta = size;

            return this;
        }

        /// <summary>Sizes the rect to the sprite's own pixel size.</summary>
        public SpriteGradientBuilder NativeSize()
        {
            if (graphic != null)
                graphic.SetNativeSize();

            return this;
        }

        /// <summary>Fills the parent, inset on every side by the same margin.</summary>
        public SpriteGradientBuilder Stretch(float margin = 0f)
        {
            if (graphic == null)
                return this;

            var rect = graphic.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
            return this;
        }

        public SpriteGradientBuilder At(Vector2 anchoredPosition)
        {
            if (graphic != null)
                graphic.rectTransform.anchoredPosition = anchoredPosition;

            return this;
        }

        /// <summary>One flat colour through the sprite. Clears any gradient, since a colour is what was asked for.</summary>
        public SpriteGradientBuilder Fill(Color color)
        {
            if (graphic == null)
                return this;

            graphic.FillGradientMode = EFillGradient.None;
            graphic.FillColor = color;
            return this;
        }

        /// <summary>A gradient across the sprite - 0 degrees left to right, 90 bottom to top.</summary>
        public SpriteGradientBuilder Fill(Gradient gradient, float angle = 90f)
        {
            if (graphic == null)
                return this;

            graphic.FillGradientMode = EFillGradient.Linear;
            graphic.FillGradient = gradient;
            graphic.FillGradientAngle = angle;
            return this;
        }

        /// <summary>A gradient from the middle of the sprite out to its corners.</summary>
        public SpriteGradientBuilder RadialFill(Gradient gradient)
        {
            if (graphic == null)
                return this;

            graphic.FillGradientMode = EFillGradient.Radial;
            graphic.FillGradient = gradient;
            return this;
        }

        /// <summary>Multiplies the fill without replacing it - the way to fade or shade a gradient.</summary>
        public SpriteGradientBuilder FillTint(Color color)
        {
            if (graphic != null)
                graphic.FillColor = color;

            return this;
        }

        public SpriteGradientBuilder Border(float size, Color color)
        {
            if (graphic == null)
                return this;

            graphic.BorderSize = size;
            graphic.BorderColor = color;
            graphic.BorderGradientMode = EBorderGradient.None;
            return this;
        }

        /// <summary>A border coloured by a ramp - across the sprite, around its outline, or across the thickness.</summary>
        public SpriteGradientBuilder Border(float size, Gradient gradient, EBorderGradient mode = EBorderGradient.Around, float angle = 90f)
        {
            if (graphic == null)
                return this;

            graphic.BorderSize = size;
            graphic.BorderGradient = gradient;
            graphic.BorderGradientMode = mode;
            graphic.BorderGradientAngle = angle;
            return this;
        }

        /// <summary>Where the border sits against the silhouette: grown outward, inward, or half of each.</summary>
        public SpriteGradientBuilder Placement(ESpriteBorderPlacement placement)
        {
            if (graphic != null)
                graphic.BorderPlacement = placement;

            return this;
        }

        public SpriteGradientBuilder NoBorder()
        {
            if (graphic != null)
                graphic.BorderSize = 0f;

            return this;
        }

        /// <summary>How the silhouette is read: what counts as solid, and how much detail is thinned out of it.</summary>
        public SpriteGradientBuilder Trace(float alphaThreshold = 0.5f, float simplify = 0.004f)
        {
            if (graphic == null)
                return this;

            graphic.AlphaThreshold = alphaThreshold;
            graphic.OutlineSimplify = simplify;
            return this;
        }

        public SpriteGradientBuilder Softness(float pixels)
        {
            if (graphic != null)
                graphic.EdgeSoftness = pixels;

            return this;
        }

        public SpriteGradientBuilder Raycast(bool target)
        {
            if (graphic != null)
                graphic.raycastTarget = target;

            return this;
        }

        /// <summary>The standard Graphic tint, over the fill and the border alike.</summary>
        public SpriteGradientBuilder Tint(Color color)
        {
            if (graphic != null)
                graphic.color = color;

            return this;
        }

        public SpriteGradientBuilder Material(Material material)
        {
            if (graphic != null)
                graphic.material = material;

            return this;
        }
    }
}
