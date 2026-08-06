using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A panel drawn from a generated mesh rather than a sprite: a fill, a border and rounded corners,
    // every side and every corner set on its own. It replaces the usual round-rect workflow of exporting
    // a 9-sliced image per variant - a card, a highlighted card, a warning card differ only in border
    // colour and radius, and here that is four fields instead of three more atlas entries. Because the
    // corners are geometry and not pixels, they stay clean at any size the layout ends up at.
    //
    // Sizing, anchoring and masking are the RectTransform's job as with any Graphic; this only decides
    // what is painted inside the rect it is given.
    // Graphic asks for both of these already, but it asks for them on a base class two levels up, and that
    // is not always enough for a component built from a type list rather than added by hand. Stated here
    // they are also protected: Unity keeps CanvasRenderer out of the Add Component menu, so an object that
    // loses one cannot be repaired from the inspector at all.
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Rounded Box")]
    public class RoundedBox : MaskableGraphic
    {
        public const int MaxCornerSegments = 32;

        [Tooltip("Colour painted inside the border. Its alpha is honoured, so a transparent fill leaves an outline on its own. With a gradient running it becomes a tint over that instead - white leaves the gradient as it is, and its alpha fades the whole thing.")]
        [SerializeField]
        private Color fillColor = Color.white;

        [Tooltip("None paints the fill flat. Linear runs the gradient across the box at the angle below; Radial runs it from the middle out to the edge, following the shape rather than a circle.")]
        [SerializeField]
        private EFillGradient fillGradientMode = EFillGradient.None;

        [Tooltip("The colours the fill runs through. Every key costs geometry: vertex colours only blend in a straight line, so the shape is cut at each stop for the blend to bend there. A handful is cheap, a dozen is not free.")]
        [SerializeField]
        private Gradient fillGradient = CreateDefaultGradient();

        [Range(0f, 360f)]
        [Tooltip("Which way a linear gradient runs, in degrees: 0 left to right, 90 bottom to top, 180 right to left. It always spans the shape, so the ends stay on the edges at any angle.")]
        [SerializeField]
        private float fillGradientAngle = 90f;

        [Tooltip("On, the fill runs to the outer edge and the border is painted over it - what CSS does, and what keeps a hairline seam from showing between the two. Turn it off when a border colour is semi-transparent and the fill must not show through it.")]
        [SerializeField]
        private bool fillUnderBorder = true;

        [Tooltip("Border thickness on this side, in the rect's own units. 0 leaves the side open.")]
        [SerializeField]
        private float borderLeft;

        [SerializeField]
        private float borderTop;

        [SerializeField]
        private float borderRight;

        [SerializeField]
        private float borderBottom;

        [Tooltip("Border colour on this side. Sides blend into each other across the corner between them, so two colours meeting at a rounded corner fade rather than break.")]
        [SerializeField]
        private Color borderColorLeft = Color.black;

        [SerializeField]
        private Color borderColorTop = Color.black;

        [SerializeField]
        private Color borderColorRight = Color.black;

        [SerializeField]
        private Color borderColorBottom = Color.black;

        [Tooltip("None leaves each side its own colour. Linear runs one ramp across the whole box at the angle below; Around runs it along the outline, from the top left corner clockwise. Either replaces the four side colours - the gradient is the border's colour.")]
        [SerializeField]
        private EBorderGradient borderGradientMode = EBorderGradient.None;

        [Tooltip("The colours the border runs through. Stops cost geometry the same way the fill's do: the ring is cut where each one falls.")]
        [SerializeField]
        private Gradient borderGradient = CreateDefaultGradient();

        [Range(0f, 360f)]
        [Tooltip("Which way a linear border gradient runs, in degrees: 0 left to right, 90 bottom to top.")]
        [SerializeField]
        private float borderGradientAngle = 90f;

        [Tooltip("Corner radius, in the rect's own units. Radii that would overlap on an edge are scaled down together, so a radius larger than the box gives a pill or a circle rather than a folded-over shape.")]
        [SerializeField]
        private float radiusTopLeft;

        [SerializeField]
        private float radiusTopRight;

        [SerializeField]
        private float radiusBottomRight;

        [SerializeField]
        private float radiusBottomLeft;

        [Range(1, MaxCornerSegments)]
        [Tooltip("Straight pieces each corner arc is built from. Raise it for corners that are large on screen, drop it for small ones - every segment is four more vertices on the ring.")]
        [SerializeField]
        private int cornerSegments = 8;

        [Range(0f, 8f)]
        [Tooltip("Width of the fade added outside the shape, which is what smooths the arcs; the mesh has no anti-aliasing of its own. About a pixel is right - too much reads as a glow, 0 leaves the corners stepped. Dropped while a Mask is on this object: the fade would widen the hole it cuts and let the contents show past the edge.")]
        [SerializeField]
        private float edgeSoftness = 1f;

        // Drawn by the custom inspector as the one-value-for-all switches; kept on the component so the
        // choice survives a reselect and a domain reload, which an editor-side flag would not.
        [HideInInspector]
        [SerializeField]
        private bool uniformBorder = true;

        [HideInInspector]
        [SerializeField]
        private bool uniformCorners = true;

        // Rebuilt in place on every geometry pass. A UI rebuild runs on any layout change, so allocating
        // these per call would hand the collector a few hundred bytes each time a panel resizes.
        private readonly List<Vector2> outerPoints = new List<Vector2>();
        private readonly List<Vector2> innerPoints = new List<Vector2>();
        private readonly List<Color32> edgeColors = new List<Color32>();
        private readonly List<Vector2> segmentNormals = new List<Vector2>();

        // Gradient working room, kept for the same reason: the stop list and the two halves a slice cuts
        // the shape into are rebuilt from scratch on every pass.
        private readonly List<float> gradientStops = new List<float>();
        private readonly List<Vector2> sliceRemainder = new List<Vector2>();
        private readonly List<Vector2> sliceBand = new List<Vector2>();
        private readonly List<Vector2> sliceRest = new List<Vector2>();

        // The border ring, once a gradient has cut it at every stop. Held apart from the contour so the
        // contour stays the shape and this stays a way of colouring it.
        private readonly List<Vector2> ringOuter = new List<Vector2>();
        private readonly List<Vector2> ringInner = new List<Vector2>();
        private readonly List<Color32> ringColors = new List<Color32>();
        private readonly List<float> ringParameters = new List<float>();
        private readonly List<float> segmentSplits = new List<float>();

        // What the last mesh was built for. A Mask being switched on, off, or set to hide its graphic
        // changes what this mesh has to contain, and none of those dirty the geometry on their own.
        private bool builtAsMask;
        private bool builtForStencilOnly;

        /// <summary>Colour painted inside the border, or the tint over the gradient when one is running.</summary>
        public Color FillColor { get => fillColor; set => SetGeometryValue(ref fillColor, value); }

        public EFillGradient FillGradientMode { get => fillGradientMode; set => SetGeometryValue(ref fillGradientMode, value); }

        public float FillGradientAngle { get => fillGradientAngle; set => SetGeometryValue(ref fillGradientAngle, value); }

        /// <summary>The colours a Linear or Radial fill runs through.</summary>
        // Handed out by reference because that is what Gradient is; editing the returned object in place
        // will not be noticed, so set it back - or call SetVerticesDirty - once the keys are in.
        public Gradient FillGradient
        {
            get => fillGradient;
            set
            {
                fillGradient = value ?? CreateDefaultGradient();
                SetVerticesDirty();
            }
        }

        // White to a light grey: enough of a step to show that a gradient is running the moment the mode is
        // switched, without inventing a colour scheme for whoever turned it on.
        private static Gradient CreateDefaultGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.78f, 0.78f, 0.78f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            return gradient;
        }

        public float BorderLeft { get => borderLeft; set => SetGeometryValue(ref borderLeft, Mathf.Max(0f, value)); }

        public float BorderTop { get => borderTop; set => SetGeometryValue(ref borderTop, Mathf.Max(0f, value)); }

        public float BorderRight { get => borderRight; set => SetGeometryValue(ref borderRight, Mathf.Max(0f, value)); }

        public float BorderBottom { get => borderBottom; set => SetGeometryValue(ref borderBottom, Mathf.Max(0f, value)); }

        public Color BorderColorLeft { get => borderColorLeft; set => SetGeometryValue(ref borderColorLeft, value); }

        public Color BorderColorTop { get => borderColorTop; set => SetGeometryValue(ref borderColorTop, value); }

        public Color BorderColorRight { get => borderColorRight; set => SetGeometryValue(ref borderColorRight, value); }

        public Color BorderColorBottom { get => borderColorBottom; set => SetGeometryValue(ref borderColorBottom, value); }

        public float RadiusTopLeft { get => radiusTopLeft; set => SetGeometryValue(ref radiusTopLeft, Mathf.Max(0f, value)); }

        public float RadiusTopRight { get => radiusTopRight; set => SetGeometryValue(ref radiusTopRight, Mathf.Max(0f, value)); }

        public float RadiusBottomRight { get => radiusBottomRight; set => SetGeometryValue(ref radiusBottomRight, Mathf.Max(0f, value)); }

        public float RadiusBottomLeft { get => radiusBottomLeft; set => SetGeometryValue(ref radiusBottomLeft, Mathf.Max(0f, value)); }

        public EBorderGradient BorderGradientMode { get => borderGradientMode; set => SetGeometryValue(ref borderGradientMode, value); }

        public float BorderGradientAngle { get => borderGradientAngle; set => SetGeometryValue(ref borderGradientAngle, value); }

        /// <summary>The colours a gradient border runs through, in place of the four side colours.</summary>
        public Gradient BorderGradient
        {
            get => borderGradient;
            set
            {
                borderGradient = value ?? CreateDefaultGradient();
                SetVerticesDirty();
            }
        }

        public int CornerSegments { get => cornerSegments; set => SetGeometryValue(ref cornerSegments, Mathf.Clamp(value, 1, MaxCornerSegments)); }

        public float EdgeSoftness { get => edgeSoftness; set => SetGeometryValue(ref edgeSoftness, Mathf.Max(0f, value)); }

        /// <summary>Sets the same thickness on all four sides.</summary>
        public void SetBorderSize(float size)
        {
            size = Mathf.Max(0f, size);
            if (borderLeft == size && borderTop == size && borderRight == size && borderBottom == size)
                return;

            borderLeft = borderTop = borderRight = borderBottom = size;
            SetVerticesDirty();
        }

        /// <summary>Sets the same colour on all four sides.</summary>
        public void SetBorderColor(Color value)
        {
            if (borderColorLeft == value && borderColorTop == value && borderColorRight == value && borderColorBottom == value)
                return;

            borderColorLeft = borderColorTop = borderColorRight = borderColorBottom = value;
            SetVerticesDirty();
        }

        /// <summary>Sets the same radius on all four corners.</summary>
        public void SetCornerRadius(float radius)
        {
            radius = Mathf.Max(0f, radius);
            if (radiusTopLeft == radius && radiusTopRight == radius && radiusBottomRight == radius && radiusBottomLeft == radius)
                return;

            radiusTopLeft = radiusTopRight = radiusBottomRight = radiusBottomLeft = radius;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var metrics = Measure(rect);
            BuildContours(rect, metrics);
            if (outerPoints.Count < 3)
                return;

            builtAsMask = ResolveMask(out builtForStencilOnly);

            // Submission order is draw order for UI, and nothing here is depth tested: the fill goes down
            // first so the border sits on top of it, and the fade last so it covers whichever it meets.
            // The ring is the contour unless a gradient has something to say about it, in which case it is
            // the contour cut at every stop - same points, more of them, each carrying its own colour.
            // Frame is the exception: it runs across the thickness rather than along the outline, so the
            // outline needs no cutting and the ring is banded the other way instead.
            bool gradientRing = borderGradientMode != EBorderGradient.None && borderGradient != null;
            bool acrossThickness = gradientRing && borderGradientMode == EBorderGradient.Frame;

            if (acrossThickness)
                BuildFrameColors(metrics.HasBorder);
            else if (gradientRing)
                BuildGradientRing(metrics.HasBorder);

            bool cutRing = gradientRing && !acrossThickness;
            var outer = cutRing ? ringOuter : outerPoints;
            var inner = cutRing ? ringInner : innerPoints;
            var colors = gradientRing ? ringColors : edgeColors;

            AddFill(vh, rect, builtForStencilOnly);
            if (metrics.HasBorder)
            {
                if (acrossThickness)
                    AddFrameBorder(vh, rect);
                else
                    AddBorder(vh, rect, outer, inner, colors);
            }

            // The fade is geometry like everything else here, and a mask cuts its shape from geometry
            // rather than from what the shape looks like: any alpha above the shader's clip threshold
            // counts, and almost all of the fade is. Left in, it widens the hole by the width of the fade
            // and whatever is inside the box shows past its edge - a picture standing a pixel proud of the
            // border that is supposed to frame it. Stencils have hard edges; this is the shape of that.
            if (!builtAsMask)
                AddSoftEdge(vh, rect, outer, colors);
        }

        // A Mask alongside this turns the mesh into a stencil for everything below it. With the mask
        // graphic hidden none of the colour survives the pass and only the alpha matters - which is the one
        // case where what the fill is set to and what the fill has to be come apart.
        private bool ResolveMask(out bool stencilOnly)
        {
            var mask = GetComponent<Mask>();
            if (mask == null || !mask.MaskEnabled())
            {
                stencilOnly = false;
                return false;
            }

            stencilOnly = !mask.showMaskGraphic;
            return true;
        }

        // Adding a Mask, removing it, or ticking Show Mask Graphic dirties the material and nothing else,
        // so without this the mesh would keep whatever fill alpha it was last built with - and a mask with
        // no shape hides every child it was added for.
        public override void SetMaterialDirty()
        {
            base.SetMaterialDirty();

            if (ResolveMask(out bool stencilOnly) != builtAsMask || stencilOnly != builtForStencilOnly)
                SetVerticesDirty();
        }

        // Everything the shape needs, already reconciled against the rect. Both kinds of overflow have to
        // be resolved before any point is placed: borders thicker than the box would turn the inner
        // contour inside out, and radii that overshoot an edge would fold the outline over itself.
        private struct Metrics
        {
            public float BorderLeft;
            public float BorderTop;
            public float BorderRight;
            public float BorderBottom;
            public float RadiusTopLeft;
            public float RadiusTopRight;
            public float RadiusBottomRight;
            public float RadiusBottomLeft;

            public bool HasBorder => BorderLeft > 0f || BorderTop > 0f || BorderRight > 0f || BorderBottom > 0f;
        }

        private Metrics Measure(Rect rect)
        {
            var m = new Metrics
            {
                BorderLeft = Mathf.Max(0f, borderLeft),
                BorderTop = Mathf.Max(0f, borderTop),
                BorderRight = Mathf.Max(0f, borderRight),
                BorderBottom = Mathf.Max(0f, borderBottom),
                RadiusTopLeft = Mathf.Max(0f, radiusTopLeft),
                RadiusTopRight = Mathf.Max(0f, radiusTopRight),
                RadiusBottomRight = Mathf.Max(0f, radiusBottomRight),
                RadiusBottomLeft = Mathf.Max(0f, radiusBottomLeft),
            };

            // Opposite borders are scaled in proportion rather than clipped, so a box squeezed below its
            // own border thickness thins out evenly instead of losing one side entirely.
            float horizontal = m.BorderLeft + m.BorderRight;
            if (horizontal > rect.width)
            {
                float k = rect.width / horizontal;
                m.BorderLeft *= k;
                m.BorderRight *= k;
            }

            float vertical = m.BorderTop + m.BorderBottom;
            if (vertical > rect.height)
            {
                float k = rect.height / vertical;
                m.BorderTop *= k;
                m.BorderBottom *= k;
            }

            // One factor for all four radii, taken from the worst edge - the rule CSS uses. Scaling only
            // the offending pair would change the shape of corners the author never touched.
            float scale = 1f;
            scale = Mathf.Min(scale, EdgeRatio(rect.width, m.RadiusTopLeft + m.RadiusTopRight));
            scale = Mathf.Min(scale, EdgeRatio(rect.width, m.RadiusBottomLeft + m.RadiusBottomRight));
            scale = Mathf.Min(scale, EdgeRatio(rect.height, m.RadiusTopLeft + m.RadiusBottomLeft));
            scale = Mathf.Min(scale, EdgeRatio(rect.height, m.RadiusTopRight + m.RadiusBottomRight));

            if (scale < 1f)
            {
                m.RadiusTopLeft *= scale;
                m.RadiusTopRight *= scale;
                m.RadiusBottomRight *= scale;
                m.RadiusBottomLeft *= scale;
            }

            return m;
        }

        private static float EdgeRatio(float length, float demand) => demand > length ? length / demand : 1f;

        // Walks the outline clockwise from the top-left corner, laying down the outer point, the matching
        // inner point and the colour at the same index. Pairing them index for index is what lets the
        // border be one strip: every quad is outer[i], outer[i+1] and the two inner points beneath them.
        private void BuildContours(Rect rect, Metrics m)
        {
            outerPoints.Clear();
            innerPoints.Clear();
            edgeColors.Clear();

            var left = SideColor(m.BorderLeft, borderColorLeft);
            var top = SideColor(m.BorderTop, borderColorTop);
            var right = SideColor(m.BorderRight, borderColorRight);
            var bottom = SideColor(m.BorderBottom, borderColorBottom);

            // A corner is entered from one side and left on the other, so its two colours are the sides it
            // joins; the arc between them carries the blend. The inner arc is elliptical when the two
            // borders it sits behind differ, which is what keeps its ends flush with both sides.
            AddCorner(m.RadiusTopLeft, new Vector2(rect.xMin, rect.yMax), new Vector2(1f, -1f), m.BorderLeft, m.BorderTop, 180f, 90f, left, top);
            AddCorner(m.RadiusTopRight, new Vector2(rect.xMax, rect.yMax), new Vector2(-1f, -1f), m.BorderRight, m.BorderTop, 90f, 0f, top, right);
            AddCorner(m.RadiusBottomRight, new Vector2(rect.xMax, rect.yMin), new Vector2(-1f, 1f), m.BorderRight, m.BorderBottom, 0f, -90f, right, bottom);
            AddCorner(m.RadiusBottomLeft, new Vector2(rect.xMin, rect.yMin), new Vector2(1f, 1f), m.BorderLeft, m.BorderBottom, -90f, -180f, bottom, left);
        }

        // A side with no thickness still contributes a colour to the outline: the fade outside it, and the
        // taper of a neighbouring corner, both have to arrive at the fill rather than at an unused border
        // colour that is nowhere on screen.
        private Color32 SideColor(float size, Color border) => size > 0f ? border * color : fillColor * color;

        // `corner` is the rect's own corner and `inward` points from it towards the middle, which is all
        // that changes between the four; everything else below is the same construction mirrored.
        private void AddCorner(float radius, Vector2 corner, Vector2 inward, float borderX, float borderY, float startAngle, float endAngle, Color32 startColor, Color32 endColor)
        {
            var outerCenter = corner + new Vector2(inward.x * radius, inward.y * radius);

            // The inner arc keeps the outer centre while the border is thinner than the radius, so the two
            // stay concentric and the border holds an even width around the bend. Past that the corner has
            // squared off from the inside and the centre slides in with the border instead.
            var innerCenter = corner + new Vector2(inward.x * Mathf.Max(radius, borderX), inward.y * Mathf.Max(radius, borderY));
            float innerRadiusX = Mathf.Max(0f, radius - borderX);
            float innerRadiusY = Mathf.Max(0f, radius - borderY);

            // A square corner is still emitted as two points at the same spot, one per colour. That is what
            // turns the colour change into a hard break at the corner; a single point would hand its colour
            // to the whole of the next side and gradient it across.
            int steps = radius > 0f ? Mathf.Clamp(cornerSegments, 1, MaxCornerSegments) : 1;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                outerPoints.Add(outerCenter + new Vector2(cos * radius, sin * radius));
                innerPoints.Add(innerCenter + new Vector2(cos * innerRadiusX, sin * innerRadiusY));
                edgeColors.Add(Color32.Lerp(startColor, endColor, t));
            }
        }

        private void AddFill(VertexHelper vh, Rect rect, bool stencilOnly)
        {
            var contour = fillUnderBorder ? outerPoints : innerPoints;
            if (contour.Count < 3)
                return;

            // None of the colour reaches the screen under a mask that hides its graphic - the pass writes
            // the stencil and nothing else - but the alpha still decides where the shape is cut out. A fill
            // left transparent would cut nothing and take every child down with it, and a gradient is worth
            // nothing here either: one flat opaque fan is the cheapest shape to cut with.
            if (stencilOnly)
            {
                AddFan(vh, rect, contour, new Color32(255, 255, 255, 255));
                return;
            }

            var tint = fillColor * color;
            if (tint.a <= 0f)
                return;

            if (fillGradientMode == EFillGradient.None || fillGradient == null)
            {
                var flat = (Color32)tint;
                if (flat.a != 0)
                    AddFan(vh, rect, contour, flat);

                return;
            }

            if (fillGradientMode == EFillGradient.Radial)
                AddRadialFill(vh, rect, contour, tint);
            else
                AddLinearFill(vh, rect, contour, tint);
        }

        // A fan from the centre is enough for a flat fill: both contours are convex, so no triangle can
        // escape the shape however the radii and borders come out.
        private void AddFan(VertexHelper vh, Rect rect, List<Vector2> contour, Color32 fill)
        {
            int count = contour.Count;
            int start = vh.currentVertCount;

            var center = rect.center;
            vh.AddVert(center, fill, Uv(center, rect));
            for (int i = 0; i < count; i++)
                vh.AddVert(contour[i], fill, Uv(contour[i], rect));

            for (int i = 0; i < count; i++)
                vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);
        }

        // Vertex colours blend in a straight line and nothing else, so a gradient with a stop in the middle
        // cannot be painted onto one fan - the stop sits inside a triangle, where there is no vertex to put
        // the colour on and the blend runs straight past it. The shape is cut into a band per pair of stops
        // instead, and each band is a straight blend between two colours, which is exactly what vertex
        // colours do give. Two-stop gradients come out of this as one band: the common case stays one fan.
        private void AddLinearFill(VertexHelper vh, Rect rect, List<Vector2> contour, Color tint)
        {
            float radians = fillGradientAngle * Mathf.Deg2Rad;
            var axis = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            // Measured off the shape itself rather than the rect, so the gradient reaches from edge to edge
            // at any angle instead of starting and finishing somewhere out in the corners.
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < contour.Count; i++)
            {
                float distance = Vector2.Dot(contour[i], axis);
                min = Mathf.Min(min, distance);
                max = Mathf.Max(max, distance);
            }

            if (max - min <= Mathf.Epsilon)
            {
                AddFan(vh, rect, contour, (Color32)(fillGradient.Evaluate(0.5f) * tint));
                return;
            }

            CollectStops(fillGradient);

            sliceRemainder.Clear();
            sliceRemainder.AddRange(contour);

            for (int i = 1; i < gradientStops.Count; i++)
            {
                float from = gradientStops[i - 1];
                float to = gradientStops[i];

                // The last band is whatever is left over, which also saves cutting along the far edge and
                // ending up with a slice of nothing.
                if (i == gradientStops.Count - 1)
                {
                    AddBand(vh, rect, sliceRemainder, axis, min, max, from, to, tint);
                    break;
                }

                Split(sliceRemainder, axis, Mathf.Lerp(min, max, to), sliceBand, sliceRest);
                AddBand(vh, rect, sliceBand, axis, min, max, from, to, tint);

                sliceRemainder.Clear();
                sliceRemainder.AddRange(sliceRest);
            }
        }

        // Rings instead of bands: the shape scaled about its own centre, so the gradient follows a rounded
        // box out to a rounded box rather than a circle that leaves the corners behind.
        private void AddRadialFill(VertexHelper vh, Rect rect, List<Vector2> contour, Color tint)
        {
            CollectStops(fillGradient);

            var center = rect.center;
            for (int i = 1; i < gradientStops.Count; i++)
            {
                float from = gradientStops[i - 1];
                float to = gradientStops[i];
                float inset = StopInset(from, to);

                var inner = (Color32)(fillGradient.Evaluate(from + inset) * tint);
                var outer = (Color32)(fillGradient.Evaluate(to - inset) * tint);

                AddRing(vh, rect, contour, center, from, to, inner, outer);
            }
        }

        private void AddRing(VertexHelper vh, Rect rect, List<Vector2> contour, Vector2 center, float from, float to, Color32 inner, Color32 outer)
        {
            int count = contour.Count;
            int start = vh.currentVertCount;

            // The innermost ring closes to a point, so it is a fan rather than a strip.
            if (from <= 0f)
            {
                vh.AddVert(center, inner, Uv(center, rect));
                for (int i = 0; i < count; i++)
                {
                    var edge = Vector2.LerpUnclamped(center, contour[i], to);
                    vh.AddVert(edge, outer, Uv(edge, rect));
                }

                for (int i = 0; i < count; i++)
                    vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);

                return;
            }

            for (int i = 0; i < count; i++)
            {
                var near = Vector2.LerpUnclamped(center, contour[i], from);
                var far = Vector2.LerpUnclamped(center, contour[i], to);
                vh.AddVert(near, inner, Uv(near, rect));
                vh.AddVert(far, outer, Uv(far, rect));
            }

            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                int nearA = start + i * 2;
                int nearB = start + j * 2;

                vh.AddTriangle(nearA, nearB, nearB + 1);
                vh.AddTriangle(nearB + 1, nearA + 1, nearA);
            }
        }

        // One band of a linear gradient: a convex offcut, fanned from its first vertex, coloured by where
        // each corner of it falls along the axis.
        private void AddBand(VertexHelper vh, Rect rect, List<Vector2> polygon, Vector2 axis, float min, float max, float from, float to, Color tint)
        {
            int count = polygon.Count;
            if (count < 3)
                return;

            float inset = StopInset(from, to);
            int start = vh.currentVertCount;

            for (int i = 0; i < count; i++)
            {
                var point = polygon[i];
                float t = Mathf.InverseLerp(min, max, Vector2.Dot(point, axis));

                // Held inside the band it belongs to. A vertex sitting exactly on a stop would otherwise
                // read the colour from the far side of it and both bands would agree on the wrong one,
                // turning a hard stop into a blend.
                t = Mathf.Clamp(t, from + inset, to - inset);

                var color32 = (Color32)(fillGradient.Evaluate(t) * tint);
                vh.AddVert(point, color32, Uv(point, rect));
            }

            for (int i = 1; i < count - 1; i++)
                vh.AddTriangle(start, start + i, start + i + 1);
        }

        private static float StopInset(float from, float to) => Mathf.Min(0.001f, (to - from) * 0.25f);

        // The moulding of a picture frame: the ramp crosses each side from its outer edge to its inner one,
        // so its direction turns with every side instead of running one way for the whole box. What makes
        // it read as a frame is the corners - the bands follow the outline in and meet along the diagonal,
        // the way a mitred join does.
        //
        // The outline needs no cutting for this. The two contours are already paired point for point, and
        // the whole width of the border lies between each pair, so a band is a strip between two lerps of
        // that pair - the same construction the radial fill uses between the centre and the edge.
        private void AddFrameBorder(VertexHelper vh, Rect rect)
        {
            CollectStops(borderGradient);

            for (int i = 1; i < gradientStops.Count; i++)
            {
                float from = gradientStops[i - 1];
                float to = gradientStops[i];
                float inset = StopInset(from, to);

                var near = (Color32)(borderGradient.Evaluate(from + inset) * color);
                var far = (Color32)(borderGradient.Evaluate(to - inset) * color);

                AddFrameBand(vh, rect, from, to, near, far);
            }
        }

        private void AddFrameBand(VertexHelper vh, Rect rect, float from, float to, Color32 near, Color32 far)
        {
            int count = outerPoints.Count;
            int start = vh.currentVertCount;

            for (int i = 0; i < count; i++)
            {
                var outward = Vector2.LerpUnclamped(outerPoints[i], innerPoints[i], from);
                var inward = Vector2.LerpUnclamped(outerPoints[i], innerPoints[i], to);
                vh.AddVert(outward, near, Uv(outward, rect));
                vh.AddVert(inward, far, Uv(inward, rect));
            }

            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                int here = start + i * 2;
                int next = start + j * 2;

                // Sides with no thickness have both contours in the same place, so their bands collapse to
                // nothing - a frame down one side only needs no special case.
                vh.AddTriangle(here, next, next + 1);
                vh.AddTriangle(next + 1, here + 1, here);
            }
        }

        // The fade hangs off the outer contour, which for a frame is where the ramp starts - one colour all
        // the way round rather than the running blend the other modes leave behind.
        private void BuildFrameColors(bool hasBorder)
        {
            ringColors.Clear();

            var edge = hasBorder ? (Color32)(borderGradient.Evaluate(0f) * color) : (Color32)(fillColor * color);
            for (int i = 0; i < outerPoints.Count; i++)
                ringColors.Add(edge);
        }

        // The contour again, with a point added wherever a border stop falls between two of them, each
        // point carrying the colour the gradient has reached there.
        //
        // The cut is needed for the same reason the fill's is: two vertices can only blend in a straight
        // line, and a whole straight side of the box is a single pair of them - a stop halfway along it
        // would have nowhere to sit and the ramp would run straight past it. Arcs are already finely cut
        // by the corner segments, so this mostly matters on the long sides.
        private void BuildGradientRing(bool hasBorder)
        {
            ringOuter.Clear();
            ringInner.Clear();
            ringColors.Clear();

            int count = outerPoints.Count;
            if (count == 0)
                return;

            ComputeRingParameters(count);
            CollectStops(borderGradient);

            // With no border anywhere, the ring is only there for the fade to hang off, and a fade out of
            // the border colour where there is no border would be a line of colour from nowhere.
            var noBorder = (Color32)(fillColor * color);

            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                float from = ringParameters[i];
                float to = ringParameters[i + 1];

                AddRingPoint(i, j, 0f, RingColor(from, hasBorder, noBorder));

                // Which stops this segment crosses, in the order it meets them. A linear ramp runs up one
                // side of the box and back down the other, so a segment can cross them descending - the
                // fraction along the segment is what sorts them, not the stop's own place in the ramp.
                float span = to - from;
                if (Mathf.Abs(span) < 1e-6f)
                    continue;

                segmentSplits.Clear();
                for (int s = 1; s < gradientStops.Count - 1; s++)
                {
                    float fraction = (gradientStops[s] - from) / span;
                    if (fraction > 1e-4f && fraction < 1f - 1e-4f)
                        segmentSplits.Add(fraction);
                }

                segmentSplits.Sort();

                float direction = Mathf.Sign(span);
                for (int s = 0; s < segmentSplits.Count; s++)
                {
                    float fraction = segmentSplits[s];
                    float stop = Mathf.Lerp(from, to, fraction);

                    // Two points at the same spot, one holding the colour on each side of the stop. That is
                    // what keeps a hard stop hard; a single point would take one of the two colours and
                    // blend the whole way to the next vertex.
                    AddRingPoint(i, j, fraction, RingColor(stop - 0.0005f * direction, hasBorder, noBorder));
                    AddRingPoint(i, j, fraction, RingColor(stop + 0.0005f * direction, hasBorder, noBorder));
                }
            }
        }

        private Color32 RingColor(float t, bool hasBorder, Color32 noBorder)
        {
            return hasBorder ? (Color32)(borderGradient.Evaluate(Mathf.Clamp01(t)) * color) : noBorder;
        }

        private void AddRingPoint(int from, int to, float fraction, Color32 tint)
        {
            ringOuter.Add(Vector2.Lerp(outerPoints[from], outerPoints[to], fraction));
            ringInner.Add(Vector2.Lerp(innerPoints[from], innerPoints[to], fraction));
            ringColors.Add(tint);
        }

        // Where each contour point falls along the gradient, with one extra entry closing the loop so every
        // segment has both its ends. Around measures by distance travelled rather than by point, so the
        // ramp moves at the same speed along a straight side as it does around a corner.
        private void ComputeRingParameters(int count)
        {
            ringParameters.Clear();

            if (borderGradientMode == EBorderGradient.Around)
            {
                float travelled = 0f;
                ringParameters.Add(0f);
                for (int i = 0; i < count; i++)
                {
                    travelled += Vector2.Distance(outerPoints[i], outerPoints[(i + 1) % count]);
                    ringParameters.Add(travelled);
                }

                if (travelled > Mathf.Epsilon)
                {
                    for (int i = 0; i <= count; i++)
                        ringParameters[i] /= travelled;
                }

                return;
            }

            float radians = borderGradientAngle * Mathf.Deg2Rad;
            var axis = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                float distance = Vector2.Dot(outerPoints[i], axis);
                min = Mathf.Min(min, distance);
                max = Mathf.Max(max, distance);
            }

            float extent = max - min;
            for (int i = 0; i < count; i++)
                ringParameters.Add(extent > Mathf.Epsilon ? (Vector2.Dot(outerPoints[i], axis) - min) / extent : 0.5f);

            // The loop closes back on the first point rather than on 1: a linear ramp is a position, not a
            // distance travelled, so the last segment ends where the first began.
            ringParameters.Add(ringParameters[0]);
        }

        // Where the blend has to bend: the ends, plus every colour and alpha key in between. Keys of both
        // kinds count, since an alpha stop is as much a change of direction as a colour one.
        private void CollectStops(Gradient gradient)
        {
            gradientStops.Clear();
            gradientStops.Add(0f);

            var colorKeys = gradient.colorKeys;
            for (int i = 0; i < colorKeys.Length; i++)
                AddStop(colorKeys[i].time);

            var alphaKeys = gradient.alphaKeys;
            for (int i = 0; i < alphaKeys.Length; i++)
                AddStop(alphaKeys[i].time);

            gradientStops.Add(1f);
            gradientStops.Sort();

            // Two keys at the same time - the usual way of asking for a hard stop - would otherwise leave a
            // band with no width, and a slice along a line it has already been cut on.
            for (int i = gradientStops.Count - 1; i > 0; i--)
            {
                if (gradientStops[i] - gradientStops[i - 1] < 0.0005f)
                    gradientStops.RemoveAt(i);
            }
        }

        private void AddStop(float time)
        {
            if (time > 0f && time < 1f)
                gradientStops.Add(time);
        }

        // Cuts a convex polygon along a line square to the axis, into the part before the line and the part
        // after it. Both keep the crossing points, so the two halves meet exactly and no seam opens up.
        private static void Split(List<Vector2> polygon, Vector2 axis, float plane, List<Vector2> inside, List<Vector2> outside)
        {
            inside.Clear();
            outside.Clear();

            int count = polygon.Count;
            for (int i = 0; i < count; i++)
            {
                var current = polygon[i];
                var next = polygon[(i + 1) % count];

                float here = Vector2.Dot(current, axis) - plane;
                float there = Vector2.Dot(next, axis) - plane;

                if (here <= 0f)
                    inside.Add(current);
                else
                    outside.Add(current);

                if ((here < 0f && there > 0f) || (here > 0f && there < 0f))
                {
                    var crossing = Vector2.Lerp(current, next, here / (here - there));
                    inside.Add(crossing);
                    outside.Add(crossing);
                }
            }
        }

        private void AddBorder(VertexHelper vh, Rect rect, List<Vector2> outer, List<Vector2> inner, List<Color32> colors)
        {
            int count = outer.Count;
            int start = vh.currentVertCount;

            for (int i = 0; i < count; i++)
            {
                vh.AddVert(outer[i], colors[i], Uv(outer[i], rect));
                vh.AddVert(inner[i], colors[i], Uv(inner[i], rect));
            }

            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                int outerA = start + i * 2;
                int outerB = start + j * 2;

                // Sides with no thickness collapse to zero-area triangles here, so a box with a border on
                // one side only needs no special case - the other three simply cover nothing.
                vh.AddTriangle(outerA, outerB, outerB + 1);
                vh.AddTriangle(outerB + 1, outerA + 1, outerA);
            }
        }

        // A skirt of transparent triangles just outside the shape. The UI mesh is drawn without
        // anti-aliasing, so this fade is the only thing standing between the arcs and a staircase; it
        // borrows each point's own colour so it fades out of the border, or out of the fill where there is
        // no border, rather than out of grey.
        private void AddSoftEdge(VertexHelper vh, Rect rect, List<Vector2> outer, List<Color32> colors)
        {
            if (edgeSoftness <= 0f)
                return;

            int count = outer.Count;
            segmentNormals.Clear();
            for (int i = 0; i < count; i++)
            {
                var edge = outer[(i + 1) % count] - outer[i];

                // The doubled points at a square corner leave a zero-length edge with no direction of its
                // own; it is marked here and skipped when the corner points look for their normals.
                segmentNormals.Add(edge.sqrMagnitude < 1e-8f ? Vector2.zero : new Vector2(-edge.y, edge.x).normalized);
            }

            int start = vh.currentVertCount;
            for (int i = 0; i < count; i++)
            {
                var offset = OutwardOffset(i, count) * edgeSoftness;
                var outerColor = colors[i];
                var fadedColor = new Color32(outerColor.r, outerColor.g, outerColor.b, 0);

                vh.AddVert(outer[i], outerColor, Uv(outer[i], rect));
                vh.AddVert(outer[i] + offset, fadedColor, Uv(outer[i] + offset, rect));
            }

            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                int innerA = start + i * 2;
                int innerB = start + j * 2;

                vh.AddTriangle(innerA, innerB, innerB + 1);
                vh.AddTriangle(innerB + 1, innerA + 1, innerA);
            }
        }

        // The direction the fade leaves the outline at, mitred between the edges either side of the point
        // so that a square corner gets its diagonal and the fade keeps an even width right around it.
        private Vector2 OutwardOffset(int index, int count)
        {
            var before = NearestNormal(index - 1, -1, count);
            var after = NearestNormal(index, 1, count);
            if (before == Vector2.zero || after == Vector2.zero)
                return Vector2.zero;

            var mitre = before + after;
            if (mitre.sqrMagnitude < 1e-8f)
                return after;

            mitre.Normalize();

            // A mitre reaches further than the edges it joins - by root two at a right angle - and the
            // offset has to grow with it or the fade pinches in at the corners. Floored so that a fold
            // sharper than the shape should ever produce cannot send it off to infinity.
            float reach = Mathf.Clamp(1f / Mathf.Max(0.35f, Vector2.Dot(mitre, after)), 1f, 3f);
            return mitre * reach;
        }

        private Vector2 NearestNormal(int index, int step, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int at = ((index + step * i) % count + count) % count;
                if (segmentNormals[at] != Vector2.zero)
                    return segmentNormals[at];
            }

            return Vector2.zero;
        }

        // Mapped across the rect so a texture set on the material lands the way a sprite would. The
        // default UI material is a white texture, where every uv reads the same and this costs nothing.
        private static Vector2 Uv(Vector2 point, Rect rect)
        {
            return new Vector2(Mathf.InverseLerp(rect.xMin, rect.xMax, point.x), Mathf.InverseLerp(rect.yMin, rect.yMax, point.y));
        }

        private void SetGeometryValue<T>(ref T field, T value) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            SetVerticesDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            borderLeft = Mathf.Max(0f, borderLeft);
            borderTop = Mathf.Max(0f, borderTop);
            borderRight = Mathf.Max(0f, borderRight);
            borderBottom = Mathf.Max(0f, borderBottom);
            radiusTopLeft = Mathf.Max(0f, radiusTopLeft);
            radiusTopRight = Mathf.Max(0f, radiusTopRight);
            radiusBottomRight = Mathf.Max(0f, radiusBottomRight);
            radiusBottomLeft = Mathf.Max(0f, radiusBottomLeft);
            cornerSegments = Mathf.Clamp(cornerSegments, 1, MaxCornerSegments);

            // Graphic's own OnValidate is what marks the mesh for a rebuild, so the change shows in the
            // scene view as the field is edited, without play mode.
            base.OnValidate();
        }
#endif
    }
}
