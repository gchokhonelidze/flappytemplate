using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A sprite drawn with a gradient laid over it and a border grown around its silhouette, rather than the
    // one flat tint an Image can give it. The sprite supplies the shape and the detail; everything else here
    // is geometry, so a single white glyph, badge or blob becomes as many coloured variants as are wanted
    // without another file in the atlas.
    //
    // What makes the border possible is that the silhouette is traced out of the alpha channel and kept as a
    // path - see SpriteOutline. Once the shape is a path, a border around it is the same strip of triangles
    // a RoundedBox draws around its own outline, and every way of colouring that strip carries over: one
    // ramp across the sprite, one running around it, or one across the thickness of the border itself.
    //
    // It replaces Image rather than sitting on top of one: a GameObject has a single CanvasRenderer and two
    // Graphics fighting over it end up with one of them not drawn.
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Sprite Gradient")]
    public class SpriteGradient : MaskableGraphic, ILayoutElement
    {
        // Enough that the ring between two stops reads as a curve rather than a polygon, on a fill that is
        // usually a soft wash behind something else. The four corner directions are added on top of these.
        private const int RadialRays = 48;

        [Tooltip("The picture. Its alpha is the shape: the gradient is painted through it, and the border is grown around the outline traced from it.")]
        [SerializeField]
        private Sprite sprite;

        [Tooltip("Fits the sprite inside the rect at its own proportions instead of stretching it to fill. The border follows whatever the sprite ends up filling.")]
        [SerializeField]
        private bool preserveAspect;

        [Tooltip("Painted through the sprite. With a gradient running it becomes a tint over that instead - white leaves the gradient as it is, and its alpha fades the whole thing.")]
        [SerializeField]
        private Color fillColor = Color.white;

        [Tooltip("None paints the sprite in one colour. Linear runs the gradient across it at the angle below; Radial runs it from the middle out to the corners.")]
        [SerializeField]
        private EFillGradient fillGradientMode = EFillGradient.Linear;

        [Tooltip("The colours the fill runs through. Every key costs geometry: vertex colours only blend in a straight line, so the quad is cut at each stop for the blend to bend there.")]
        [SerializeField]
        private Gradient fillGradient = CreateDefaultGradient();

        [Range(0f, 360f)]
        [Tooltip("Which way a linear gradient runs, in degrees: 0 left to right, 90 bottom to top, 180 right to left. It always spans the sprite, so the ends stay on its edges at any angle.")]
        [SerializeField]
        private float fillGradientAngle = 90f;

        [Tooltip("Border thickness, in the rect's own units. 0 leaves the sprite alone.")]
        [SerializeField]
        private float borderSize;

        [Tooltip("Border colour. Replaced by the border gradient when one is running.")]
        [SerializeField]
        private Color borderColor = Color.black;

        [Tooltip("Outside grows the border off the silhouette and leaves the picture whole. Inside eats into the picture's edge and keeps the shape its own size. Center splits the difference.")]
        [SerializeField]
        private ESpriteBorderPlacement borderPlacement = ESpriteBorderPlacement.Outside;

        [Tooltip("None leaves the border one colour. Linear runs a ramp across the sprite at the angle below; Around runs it along the outline; Frame runs it across the thickness, outer edge to inner.")]
        [SerializeField]
        private EBorderGradient borderGradientMode = EBorderGradient.None;

        [Tooltip("The colours the border runs through, in place of the border colour.")]
        [SerializeField]
        private Gradient borderGradient = CreateDefaultGradient();

        [Range(0f, 360f)]
        [Tooltip("Which way a linear border gradient runs, in degrees: 0 left to right, 90 bottom to top.")]
        [SerializeField]
        private float borderGradientAngle = 90f;

        [Range(0.01f, 0.99f)]
        [Tooltip("How much alpha counts as part of the shape when the silhouette is traced. Lower catches more of a soft edge and puts the border further out; higher hugs the solid core.")]
        [SerializeField]
        private float alphaThreshold = 0.5f;

        [Range(0f, 0.05f)]
        [Tooltip("How much detail is thinned out of the traced outline, as a fraction of the sprite. Up here means fewer vertices and a smoother, looser border; 0 keeps every wobble the trace found.")]
        [SerializeField]
        private float outlineSimplify = 0.004f;

        [Range(0f, 8f)]
        [Tooltip("Width of the fade added outside the border, which is what smooths it; the mesh has no anti-aliasing of its own. About a pixel is right. Dropped for an inside border, where the sprite's own edge is already the outer one.")]
        [SerializeField]
        private float edgeSoftness = 1f;

        // Rebuilt in place on every geometry pass, so that a rebuild - which a layout change is enough to
        // cause - does not hand the collector a few hundred bytes each time.
        private readonly List<Vector2> outerPoints = new List<Vector2>();
        private readonly List<Vector2> innerPoints = new List<Vector2>();
        private readonly List<Vector2> outwardNormals = new List<Vector2>();
        private readonly List<Color32> borderColors = new List<Color32>();

        // The border is one list of points holding several closed loops end to end - a doughnut is two, a
        // dotted glyph is more - and these say where each of them starts and how long it runs. Every loop
        // has to wrap round to its own beginning rather than to the next one's.
        private readonly List<int> loopStarts = new List<int>();
        private readonly List<int> loopCounts = new List<int>();

        private readonly List<Vector2> loopPoints = new List<Vector2>();

        private readonly List<float> gradientStops = new List<float>();
        private readonly List<Vector2> sliceRemainder = new List<Vector2>();
        private readonly List<Vector2> sliceBand = new List<Vector2>();
        private readonly List<Vector2> sliceRest = new List<Vector2>();

        private readonly List<float> rayAngles = new List<float>();
        private readonly List<Vector2> rayDirections = new List<Vector2>();
        private readonly List<float> rayDistances = new List<float>();

        // Where the sprite is being drawn and which corner of the atlas it comes from. Both are settled once
        // at the top of a mesh pass and read by everything below it, rather than threaded through a dozen
        // signatures that would all be carrying the same two values.
        private Rect drawRect;
        private Vector4 uvRect;

        // The one spot in the picture the whole border is drawn through - see the note in BuildBorder. What
        // is there is kept alongside it, so each vertex can be corrected for it.
        private Vector2 borderUv;
        private Color borderSource = Color.white;

        /// <summary>The picture. Its alpha is the shape the gradient is painted through and the border is grown around.</summary>
        public Sprite Sprite
        {
            get => sprite;
            set
            {
                if (sprite == value)
                    return;

                sprite = value;

                // A new sprite is a new texture as often as not, and a Graphic only rebinds that when the
                // material is dirtied - the mesh alone would come back the right shape in the wrong picture.
                SetVerticesDirty();
                SetMaterialDirty();
                SetLayoutDirty();
            }
        }

        // Falls back to whatever Graphic would have used, which is a one-pixel white texture: with no sprite
        // the mesh is still drawn, and every uv on it reads the same opaque texel.
        public override Texture mainTexture => sprite != null ? sprite.texture : base.mainTexture;

        public bool PreserveAspect { get => preserveAspect; set => SetGeometryValue(ref preserveAspect, value); }

        /// <summary>Painted through the sprite, or the tint over the gradient when one is running.</summary>
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

        public float BorderSize { get => borderSize; set => SetGeometryValue(ref borderSize, Mathf.Max(0f, value)); }

        public Color BorderColor { get => borderColor; set => SetGeometryValue(ref borderColor, value); }

        public ESpriteBorderPlacement BorderPlacement { get => borderPlacement; set => SetGeometryValue(ref borderPlacement, value); }

        public EBorderGradient BorderGradientMode { get => borderGradientMode; set => SetGeometryValue(ref borderGradientMode, value); }

        public float BorderGradientAngle { get => borderGradientAngle; set => SetGeometryValue(ref borderGradientAngle, value); }

        /// <summary>The colours a gradient border runs through, in place of the border colour.</summary>
        public Gradient BorderGradient
        {
            get => borderGradient;
            set
            {
                borderGradient = value ?? CreateDefaultGradient();
                SetVerticesDirty();
            }
        }

        public float AlphaThreshold { get => alphaThreshold; set => SetGeometryValue(ref alphaThreshold, Mathf.Clamp(value, 0.01f, 0.99f)); }

        public float OutlineSimplify { get => outlineSimplify; set => SetGeometryValue(ref outlineSimplify, Mathf.Clamp(value, 0f, 0.05f)); }

        public float EdgeSoftness { get => edgeSoftness; set => SetGeometryValue(ref edgeSoftness, Mathf.Max(0f, value)); }

        /// <summary>Whether the border is following this sprite's real silhouette, or its coarser importer mesh.</summary>
        // Read/Write being off is not what decides this - the pixels are taken off the GPU when the texture
        // will not hand them over - so the answer is only no for a sprite that cannot be read at all. The
        // border still appears there, but on a full-rect sprite the importer's outline is the quad, and a
        // border around the quad is a rectangle rather than the shape anybody was looking at.
        public bool CanTraceOutline => sprite != null && SpriteOutline.Get(sprite, alphaThreshold, outlineSimplify).Traced;

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

        /// <summary>Sizes the rect to the sprite's own pixel size, the way Image's Set Native Size does.</summary>
        public override void SetNativeSize()
        {
            if (sprite == null)
                return;

            rectTransform.anchorMax = rectTransform.anchorMin;
            rectTransform.sizeDelta = sprite.rect.size;
            SetAllDirty();
        }

        /// <summary>
        /// Fills the two lists with the border's outline in this object's local space - the outer edge and
        /// the inner edge, one point each, paired index for index. Loops are laid end to end; `starts` and
        /// `counts` say where each one begins and how long it runs.
        /// </summary>
        // Measured on the spot rather than handed out from the last mesh pass, so it answers for the sprite
        // as it stands now: a caller asking before the first rebuild, or after a resize in the same frame,
        // gets what is true rather than what was last drawn.
        public void GetBorderPath(List<Vector2> outer, List<Vector2> inner, List<int> starts, List<int> counts)
        {
            outer.Clear();
            inner.Clear();
            starts?.Clear();
            counts?.Clear();

            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            drawRect = SpriteRect(rect);
            uvRect = sprite != null ? DataUtility.GetOuterUV(sprite) : new Vector4(0f, 0f, 1f, 1f);

            // Writes the same working lists the mesh pass uses, which is safe only because that pass
            // rebuilds them from nothing every time it runs.
            if (!BuildBorder())
                return;

            outer.AddRange(outerPoints);
            inner.AddRange(innerPoints);
            starts?.AddRange(loopStarts);
            counts?.AddRange(loopCounts);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            drawRect = SpriteRect(rect);
            uvRect = sprite != null ? DataUtility.GetOuterUV(sprite) : new Vector4(0f, 0f, 1f, 1f);

            bool hasBorder = BuildBorder();

            // Submission order is draw order for UI, and nothing here is depth tested. A border grown
            // outward goes down first so the sprite's own soft edge lands on top of it and the two meet
            // without a seam; one grown inward has to go last, or the sprite would cover it.
            if (hasBorder && borderPlacement == ESpriteBorderPlacement.Outside)
                AddBorder(vh);

            AddFill(vh);

            if (hasBorder && borderPlacement != ESpriteBorderPlacement.Outside)
                AddBorder(vh);
        }

        // The part of the rect the sprite actually covers. Everything - the gradient's span, the outline's
        // mapping, the UVs - is measured against this rather than the rect, so preserving the aspect moves
        // the border with the picture instead of leaving it out on the letterboxing.
        private Rect SpriteRect(Rect rect)
        {
            if (sprite == null || !preserveAspect || rect.height <= 0f)
                return rect;

            var size = sprite.rect.size;
            if (size.x <= 0f || size.y <= 0f)
                return rect;

            float wanted = size.x / size.y;
            float given = rect.width / rect.height;

            if (wanted > given)
            {
                float height = rect.width / wanted;
                return new Rect(rect.x, rect.y + (rect.height - height) * 0.5f, rect.width, height);
            }

            float width = rect.height * wanted;
            return new Rect(rect.x + (rect.width - width) * 0.5f, rect.y, width, rect.height);
        }

        // ---- the border --------------------------------------------------------------------------------

        private bool BuildBorder()
        {
            outerPoints.Clear();
            innerPoints.Clear();
            outwardNormals.Clear();
            borderColors.Clear();
            loopStarts.Clear();
            loopCounts.Clear();

            if (sprite == null || borderSize <= 0f)
                return false;

            var outline = SpriteOutline.Get(sprite, alphaThreshold, outlineSimplify);
            if (outline.IsEmpty)
                return false;

            float outward = borderPlacement == ESpriteBorderPlacement.Outside ? borderSize
                : borderPlacement == ESpriteBorderPlacement.Center ? borderSize * 0.5f
                : 0f;

            float inward = borderSize - outward;

            // The outline is only as accurate as the grid it was read off, and the corner cutting that
            // takes the sampling's zigzag out of it moves the result again - so it can end up a shade
            // outside the picture as easily as inside. A border stopping exactly on it would leave a
            // hairline of background showing between the two, which is the one flaw on an outside border
            // that is impossible to miss. So its inner edge is pushed under the sprite by about what the
            // outline could be out by, and the picture is drawn over the overlap.
            if (borderPlacement == ESpriteBorderPlacement.Outside)
            {
                float slack = outline.Spacing * Mathf.Min(drawRect.width, drawRect.height);
                inward = Mathf.Max(1f, slack * 1.5f);
            }

            // A UI graphic draws through one texture, and here that texture is the picture. So the border
            // is only as opaque as whatever it lands on - and it lands on the edge of the picture, where
            // the alpha is running out, or outside it, where there is none at all. Drawn where it sits it
            // would come out in pieces: solid only where the outline happens to cross something opaque.
            //
            // So the border keeps its own position and reads the texture from one fixed spot instead: the
            // solid, near-white place the trace found well inside the shape. Every vertex shares it, and
            // what it does hold is divided back out below, which leaves the border exactly the colour it
            // was asked for even on a picture that is nowhere near white.
            //
            // With no trace to go on - a sprite rotated in an atlas, whose outline came from its mesh - the
            // middle of the sprite is the best guess available, and nothing is corrected for.
            bool traced = outline.Traced && outline.SolidColor.a > 0.02f;
            var read = traced ? outline.Solid : new Vector2(0.5f, 0.5f);

            borderSource = traced ? outline.SolidColor : Color.white;
            borderUv = new Vector2(
                Mathf.Lerp(uvRect.x, uvRect.z, read.x),
                Mathf.Lerp(uvRect.y, uvRect.w, read.y));

            // Extra points along the outline, but only for a ramp that has somewhere to put them. Two
            // vertices blend in a straight line between themselves, which is already exact for a two-key
            // ramp along a straight run - it is a key in the middle that would otherwise fall inside a
            // segment, with no vertex there to carry the colour, and be blended straight past.
            bool ramped = NeedsRampDetail();
            float spacing = Mathf.Max(drawRect.width, drawRect.height) * SpriteOutline.MaxSegment;

            for (int c = 0; c < outline.Contours.Count; c++)
            {
                var contour = outline.Contours[c];
                int count = contour.Length;
                if (count < 3)
                    continue;

                loopPoints.Clear();
                for (int i = 0; i < count; i++)
                {
                    loopPoints.Add(new Vector2(
                        Mathf.Lerp(drawRect.xMin, drawRect.xMax, contour[i].x),
                        Mathf.Lerp(drawRect.yMin, drawRect.yMax, contour[i].y)));
                }

                if (ramped)
                    SpriteOutline.Subdivide(loopPoints, spacing);

                int start = outerPoints.Count;
                int laid = SpriteBorderPath.Build(loopPoints, outward, inward, outerPoints, innerPoints, outwardNormals);
                if (laid < 3)
                    continue;

                loopStarts.Add(start);
                loopCounts.Add(laid);
            }

            if (outerPoints.Count == 0)
                return false;

            BuildBorderColors();
            return true;
        }

        // Whether the border's colouring needs more of the outline than the shape itself does. Only the two
        // modes that run a ramp along the outline can, and only when the ramp has a key somewhere in the
        // middle to land on: Frame runs across the border rather than along it, and a plain two-key ramp
        // is already exact between any two points.
        private bool NeedsRampDetail()
        {
            if (borderGradient == null)
                return false;

            if (borderGradientMode != EBorderGradient.Linear && borderGradientMode != EBorderGradient.Around)
                return false;

            var colorKeys = borderGradient.colorKeys;
            for (int i = 0; i < colorKeys.Length; i++)
            {
                if (colorKeys[i].time > 0.001f && colorKeys[i].time < 0.999f)
                    return true;
            }

            var alphaKeys = borderGradient.alphaKeys;
            for (int i = 0; i < alphaKeys.Length; i++)
            {
                if (alphaKeys[i].time > 0.001f && alphaKeys[i].time < 0.999f)
                    return true;
            }

            // Around is a ramp along a path rather than across a straight line, so it bends whether the
            // gradient does or not: a shape with few, long segments would step from colour to colour at
            // each corner instead of travelling smoothly round.
            return borderGradientMode == EBorderGradient.Around;
        }

        // What a border vertex has to carry for the border to come out the colour it was asked for. The
        // shader multiplies the vertex by the texel it lands on, so whatever that texel holds is divided
        // back out here. A texel darker than the colour wanted cannot give it: the result clamps, and the
        // border comes out as bright as that spot of the picture allows.
        private Color32 BorderTint(Color wanted)
        {
            return (Color32)new Color(
                Mathf.Clamp01(wanted.r / Mathf.Max(0.004f, borderSource.r)),
                Mathf.Clamp01(wanted.g / Mathf.Max(0.004f, borderSource.g)),
                Mathf.Clamp01(wanted.b / Mathf.Max(0.004f, borderSource.b)),
                Mathf.Clamp01(wanted.a / Mathf.Max(0.004f, borderSource.a)));
        }

        // One colour per outline point, which is what turns the strip into a ramp: the blend between two
        // points is the blend between their two colours. It is read at the points the trace left behind
        // rather than at stops of its own, so a heavily simplified outline coarsens a gradient along with
        // the shape - which is why no segment is left longer than a fraction of the sprite.
        private void BuildBorderColors()
        {
            int count = outerPoints.Count;
            bool ramp = borderGradientMode != EBorderGradient.None && borderGradient != null;

            if (!ramp || borderGradientMode == EBorderGradient.Frame)
            {
                // Frame paints itself across the thickness and reads nothing from here. The fade off the
                // outer edge still needs a colour, and for a frame that is where its ramp begins.
                var flat = ramp ? BorderTint(borderGradient.Evaluate(0f) * color) : BorderTint(borderColor * color);
                for (int i = 0; i < count; i++)
                    borderColors.Add(flat);

                return;
            }

            if (borderGradientMode == EBorderGradient.Around)
            {
                // Measured by distance travelled rather than by point, so the ramp moves at the same speed
                // along a straight edge as it does around a curve. It starts wherever the trace happened to
                // start on each loop, which is a corner of the shape but not one anybody chose.
                for (int loop = 0; loop < loopStarts.Count; loop++)
                {
                    int start = loopStarts[loop];
                    int length = loopCounts[loop];

                    float total = 0f;
                    for (int i = 0; i < length; i++)
                        total += Vector2.Distance(outerPoints[start + i], outerPoints[start + (i + 1) % length]);

                    float travelled = 0f;
                    for (int i = 0; i < length; i++)
                    {
                        float t = total > Mathf.Epsilon ? travelled / total : 0f;
                        borderColors.Add(BorderTint(borderGradient.Evaluate(t) * color));
                        travelled += Vector2.Distance(outerPoints[start + i], outerPoints[start + (i + 1) % length]);
                    }
                }

                return;
            }

            float radians = borderGradientAngle * Mathf.Deg2Rad;
            var axis = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            // Measured off the outline itself rather than the rect, so the ramp reaches from one side of the
            // shape to the other at any angle instead of starting somewhere out in the empty corners.
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
            {
                float t = extent > Mathf.Epsilon ? (Vector2.Dot(outerPoints[i], axis) - min) / extent : 0.5f;
                borderColors.Add(BorderTint(borderGradient.Evaluate(t) * color));
            }
        }

        private void AddBorder(VertexHelper vh)
        {
            bool frame = borderGradientMode == EBorderGradient.Frame && borderGradient != null;
            if (frame)
                CollectStops(borderGradient);

            for (int loop = 0; loop < loopStarts.Count; loop++)
            {
                int start = loopStarts[loop];
                int length = loopCounts[loop];

                if (frame)
                    AddFrameLoop(vh, start, length);
                else
                    AddStripLoop(vh, start, length);
            }

            // The fade is geometry, and geometry is what a Mask cuts its hole from - any alpha above the
            // shader's clip threshold counts, and almost all of a fade is. An inside border has the sprite's
            // own edge as its outer one, which is already as smooth as the picture is, so there is nothing
            // for a fade to do there but spill colour past the shape.
            if (edgeSoftness <= 0f || borderPlacement == ESpriteBorderPlacement.Inside)
                return;

            for (int loop = 0; loop < loopStarts.Count; loop++)
                AddSoftEdge(vh, loopStarts[loop], loopCounts[loop]);
        }

        private void AddStripLoop(VertexHelper vh, int start, int length)
        {
            int first = vh.currentVertCount;

            for (int i = 0; i < length; i++)
            {
                int at = start + i;
                var uv = borderUv;
                vh.AddVert(outerPoints[at], borderColors[at], uv);
                vh.AddVert(innerPoints[at], borderColors[at], uv);
            }

            for (int i = 0; i < length; i++)
            {
                int j = (i + 1) % length;
                int here = first + i * 2;
                int next = first + j * 2;

                vh.AddTriangle(here, next, next + 1);
                vh.AddTriangle(next + 1, here + 1, here);
            }
        }

        // The moulding of a picture frame: the ramp crosses the border from its outer edge to its inner one,
        // so its direction turns with the outline instead of running one way across the whole sprite.
        //
        // No cutting is needed for this. The two edges are already paired point for point and the whole
        // width of the border lies between each pair, so a band is a strip between two lerps of that pair.
        private void AddFrameLoop(VertexHelper vh, int start, int length)
        {
            for (int s = 1; s < gradientStops.Count; s++)
            {
                float from = gradientStops[s - 1];
                float to = gradientStops[s];
                float inset = StopInset(from, to);

                var near = BorderTint(borderGradient.Evaluate(from + inset) * color);
                var far = BorderTint(borderGradient.Evaluate(to - inset) * color);

                int first = vh.currentVertCount;
                for (int i = 0; i < length; i++)
                {
                    int at = start + i;
                    var uv = borderUv;
                    vh.AddVert(Vector2.LerpUnclamped(outerPoints[at], innerPoints[at], from), near, uv);
                    vh.AddVert(Vector2.LerpUnclamped(outerPoints[at], innerPoints[at], to), far, uv);
                }

                for (int i = 0; i < length; i++)
                {
                    int j = (i + 1) % length;
                    int here = first + i * 2;
                    int next = first + j * 2;

                    vh.AddTriangle(here, next, next + 1);
                    vh.AddTriangle(next + 1, here + 1, here);
                }
            }
        }

        // A skirt of transparent triangles just outside the border. The UI mesh is drawn without
        // anti-aliasing, so this fade is the only thing standing between the outline and a staircase; it
        // borrows each point's own colour, so it fades out of the border rather than out of grey.
        private void AddSoftEdge(VertexHelper vh, int start, int length)
        {
            int first = vh.currentVertCount;

            for (int i = 0; i < length; i++)
            {
                int at = start + i;
                var uv = borderUv;
                var edge = borderColors[at];

                vh.AddVert(outerPoints[at], edge, uv);
                vh.AddVert(outerPoints[at] + outwardNormals[at] * edgeSoftness, new Color32(edge.r, edge.g, edge.b, 0), uv);
            }

            for (int i = 0; i < length; i++)
            {
                int j = (i + 1) % length;
                int here = first + i * 2;
                int next = first + j * 2;

                vh.AddTriangle(here, next, next + 1);
                vh.AddTriangle(next + 1, here + 1, here);
            }
        }

        // ---- the fill ----------------------------------------------------------------------------------

        private void AddFill(VertexHelper vh)
        {
            var tint = fillColor * color;
            if (tint.a <= 0f)
                return;

            if (fillGradientMode == EFillGradient.None || fillGradient == null)
            {
                var flat = (Color32)tint;
                if (flat.a != 0)
                    AddQuad(vh, flat);

                return;
            }

            if (fillGradientMode == EFillGradient.Radial)
                AddRadialFill(vh, tint);
            else
                AddLinearFill(vh, tint);
        }

        private void AddQuad(VertexHelper vh, Color32 fill)
        {
            int first = vh.currentVertCount;

            AddCorner(vh, new Vector2(drawRect.xMin, drawRect.yMin), fill);
            AddCorner(vh, new Vector2(drawRect.xMin, drawRect.yMax), fill);
            AddCorner(vh, new Vector2(drawRect.xMax, drawRect.yMax), fill);
            AddCorner(vh, new Vector2(drawRect.xMax, drawRect.yMin), fill);

            vh.AddTriangle(first, first + 1, first + 2);
            vh.AddTriangle(first + 2, first + 3, first);
        }

        private void AddCorner(VertexHelper vh, Vector2 point, Color32 fill) => vh.AddVert(point, fill, Uv(point));

        // Vertex colours blend in a straight line and nothing else, so a gradient with a stop in the middle
        // cannot be painted onto one quad - the stop sits inside a triangle, where there is no vertex to put
        // the colour on and the blend runs straight past it. The quad is cut into a band per pair of stops
        // instead, and each band is a straight blend between two colours, which is what vertex colours give.
        // A two-key ramp comes out of this as one band: the common case stays one quad.
        private void AddLinearFill(VertexHelper vh, Color tint)
        {
            float radians = fillGradientAngle * Mathf.Deg2Rad;
            var axis = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            sliceRemainder.Clear();
            sliceRemainder.Add(new Vector2(drawRect.xMin, drawRect.yMin));
            sliceRemainder.Add(new Vector2(drawRect.xMax, drawRect.yMin));
            sliceRemainder.Add(new Vector2(drawRect.xMax, drawRect.yMax));
            sliceRemainder.Add(new Vector2(drawRect.xMin, drawRect.yMax));

            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0; i < sliceRemainder.Count; i++)
            {
                float distance = Vector2.Dot(sliceRemainder[i], axis);
                min = Mathf.Min(min, distance);
                max = Mathf.Max(max, distance);
            }

            if (max - min <= Mathf.Epsilon)
            {
                AddQuad(vh, (Color32)(fillGradient.Evaluate(0.5f) * tint));
                return;
            }

            CollectStops(fillGradient);

            for (int i = 1; i < gradientStops.Count; i++)
            {
                float from = gradientStops[i - 1];
                float to = gradientStops[i];

                // The last band is whatever is left over, which also saves cutting along the far edge and
                // coming away with a slice of nothing.
                if (i == gradientStops.Count - 1)
                {
                    AddBand(vh, sliceRemainder, axis, min, max, from, to, tint);
                    break;
                }

                Split(sliceRemainder, axis, Mathf.Lerp(min, max, to), sliceBand, sliceRest);
                AddBand(vh, sliceBand, axis, min, max, from, to, tint);

                sliceRemainder.Clear();
                sliceRemainder.AddRange(sliceRest);
            }
        }

        // One band of a linear gradient: a convex offcut, fanned from its first vertex, coloured by where
        // each corner of it falls along the axis.
        private void AddBand(VertexHelper vh, List<Vector2> polygon, Vector2 axis, float min, float max, float from, float to, Color tint)
        {
            int count = polygon.Count;
            if (count < 3)
                return;

            float inset = StopInset(from, to);
            int first = vh.currentVertCount;

            for (int i = 0; i < count; i++)
            {
                var point = polygon[i];
                float t = Mathf.InverseLerp(min, max, Vector2.Dot(point, axis));

                // Held inside the band it belongs to. A vertex sitting exactly on a stop would otherwise
                // read the colour from the far side of it and both bands would agree on the wrong one,
                // turning a hard stop into a blend.
                t = Mathf.Clamp(t, from + inset, to - inset);

                vh.AddVert(point, (Color32)(fillGradient.Evaluate(t) * tint), Uv(point));
            }

            for (int i = 1; i < count - 1; i++)
                vh.AddTriangle(first, first + i, first + i + 1);
        }

        // Rings out from the middle, each ray stopping where it meets the edge of the sprite. The ramp is
        // measured against the distance to the corner, so it is a circle spreading over the picture rather
        // than a rectangle following its outline - and because a ray that has reached the edge is coloured
        // by where it actually stopped, clamping the geometry does not bend the gradient.
        private void AddRadialFill(VertexHelper vh, Color tint)
        {
            float radius = new Vector2(drawRect.width, drawRect.height).magnitude * 0.5f;
            if (radius <= Mathf.Epsilon)
                return;

            CollectStops(fillGradient);
            BuildRays();

            var center = drawRect.center;
            int rays = rayDirections.Count;

            for (int s = 1; s < gradientStops.Count; s++)
            {
                float from = gradientStops[s - 1];
                float to = gradientStops[s];
                float inset = StopInset(from, to);
                int first = vh.currentVertCount;

                for (int i = 0; i < rays; i++)
                {
                    float near = Mathf.Min(from * radius, rayDistances[i]);
                    float far = Mathf.Min(to * radius, rayDistances[i]);

                    var a = center + rayDirections[i] * near;
                    var b = center + rayDirections[i] * far;

                    vh.AddVert(a, RadialColor(near / radius, from, to, inset, tint), Uv(a));
                    vh.AddVert(b, RadialColor(far / radius, from, to, inset, tint), Uv(b));
                }

                for (int i = 0; i < rays; i++)
                {
                    int j = (i + 1) % rays;
                    int here = first + i * 2;
                    int next = first + j * 2;

                    // The innermost ring has every near vertex sitting on the centre, so half of these come
                    // out with no area at all and the other half are the fan that ring wants to be.
                    vh.AddTriangle(here, next, next + 1);
                    vh.AddTriangle(next + 1, here + 1, here);
                }
            }
        }

        private Color32 RadialColor(float t, float from, float to, float inset, Color tint)
        {
            return (Color32)(fillGradient.Evaluate(Mathf.Clamp(t, from + inset, to - inset)) * tint);
        }

        private void BuildRays()
        {
            rayAngles.Clear();
            rayDirections.Clear();
            rayDistances.Clear();

            for (int i = 0; i < RadialRays; i++)
                rayAngles.Add(i * Mathf.PI * 2f / RadialRays);

            float halfWidth = drawRect.width * 0.5f;
            float halfHeight = drawRect.height * 0.5f;

            // The four corner directions go in by hand. Every ray lands on the edge of the rect, so the
            // straight line between two of them lies along the edge they share - but only while they do
            // share one, and a pair straddling a corner would cut it off and take a wedge of the picture
            // with it.
            rayAngles.Add(Mathf.Atan2(halfHeight, halfWidth));
            rayAngles.Add(Mathf.Atan2(halfHeight, -halfWidth));
            rayAngles.Add(Mathf.Atan2(-halfHeight, -halfWidth) + Mathf.PI * 2f);
            rayAngles.Add(Mathf.Atan2(-halfHeight, halfWidth) + Mathf.PI * 2f);
            rayAngles.Sort();

            for (int i = 0; i < rayAngles.Count; i++)
            {
                if (i > 0 && rayAngles[i] - rayAngles[i - 1] < 1e-4f)
                    continue;

                var direction = new Vector2(Mathf.Cos(rayAngles[i]), Mathf.Sin(rayAngles[i]));

                float toSide = Mathf.Abs(direction.x) < 1e-6f ? float.MaxValue : halfWidth / Mathf.Abs(direction.x);
                float toCap = Mathf.Abs(direction.y) < 1e-6f ? float.MaxValue : halfHeight / Mathf.Abs(direction.y);

                rayDirections.Add(direction);
                rayDistances.Add(Mathf.Min(toSide, toCap));
            }
        }

        // ---- shared bits -------------------------------------------------------------------------------

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
            // band with no width, and a cut along a line the shape has already been cut on.
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

        private static float StopInset(float from, float to) => Mathf.Min(0.001f, (to - from) * 0.25f);

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

        // Mapped through the sprite's own corner of the atlas, so every vertex reads the picture from the
        // place that lines up with where it is being drawn.
        private Vector2 Uv(Vector2 point)
        {
            return new Vector2(
                Mathf.Lerp(uvRect.x, uvRect.z, Mathf.InverseLerp(drawRect.xMin, drawRect.xMax, point.x)),
                Mathf.Lerp(uvRect.y, uvRect.w, Mathf.InverseLerp(drawRect.yMin, drawRect.yMax, point.y)));
        }

        private void SetGeometryValue<T>(ref T field, T value) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            SetVerticesDirty();
        }

        // ---- layout ------------------------------------------------------------------------------------

        // What a layout group asks for when it is deciding how much room this needs. The sprite's own pixel
        // size is the answer, the way it is for an Image, plus whatever a border grown outward adds to it.
        public virtual void CalculateLayoutInputHorizontal() { }

        public virtual void CalculateLayoutInputVertical() { }

        public virtual float minWidth => 0f;

        public virtual float preferredWidth => sprite == null ? -1f : sprite.rect.width + BorderSpread;

        public virtual float flexibleWidth => -1f;

        public virtual float minHeight => 0f;

        public virtual float preferredHeight => sprite == null ? -1f : sprite.rect.height + BorderSpread;

        public virtual float flexibleHeight => -1f;

        public virtual int layoutPriority => 0;

        private float BorderSpread
        {
            get
            {
                switch (borderPlacement)
                {
                    case ESpriteBorderPlacement.Outside: return borderSize * 2f;
                    case ESpriteBorderPlacement.Center: return borderSize;
                    default: return 0f;
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            borderSize = Mathf.Max(0f, borderSize);
            alphaThreshold = Mathf.Clamp(alphaThreshold, 0.01f, 0.99f);
            outlineSimplify = Mathf.Clamp(outlineSimplify, 0f, 0.05f);
            edgeSoftness = Mathf.Max(0f, edgeSoftness);

            // Traced outlines are kept for as long as the sprite lives, and in the editor the sprite can be
            // reimported under us - Read/Write switched on, the picture repainted. Anything touched in the
            // inspector is cheap enough to trace again that it is not worth being clever about which.
            SpriteOutline.Clear();

            // Graphic's own OnValidate is what marks the mesh for a rebuild, so a change shows in the scene
            // view as the field is edited, without play mode.
            base.OnValidate();
        }
#endif
    }
}
