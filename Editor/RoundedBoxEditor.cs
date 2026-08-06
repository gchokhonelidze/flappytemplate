using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate.Editor
{
    // Twelve loose numbers - four radii, four thicknesses, four colours - read as a wall in a plain
    // inspector, and none of them say which part of the box they move. So the fields are laid out on a
    // picture of the box instead: each radius sits on the corner it rounds, each thickness and swatch
    // sits against the side it thickens, and the picture in the middle is the shape as it will render.
    //
    // The preview is drawn from a distance field rather than from the component's mesh. The mesh is
    // built for a canvas and cannot be pointed at an inspector rect, and rasterising the outline here
    // costs a few hundred microseconds on the frames where a value actually changes - the result is
    // cached against the values it was drawn from and reused on every other repaint.
    [CustomEditor(typeof(RoundedBox))]
    [CanEditMultipleObjects]
    public class RoundedBoxEditor : GraphicEditor
    {
        private const float WidgetHeight = 208f;
        private const float SideGutter = 58f;
        private const float FieldWidth = 46f;
        private const float FieldHeight = 16f;
        private const float SwatchWidth = 26f;
        private const float SwatchHeight = 13f;
        private const float CornerWidth = 42f;
        private const float Gap = 3f;
        private const float Inset = 5f;
        // Enough to stay crisp on a retina display without turning a slider drag into a per-pixel loop
        // over a megapixel; past this the texture is stretched, which nothing here is sharp enough to
        // suffer from.
        private const int MaxPreviewSide = 512;

        private const int RampSize = 256;

        // The preview is a picture of the shape, not of the object: an empty rect, or one still at its
        // default size, would give nothing to look at, so it stands in with a plain landscape box.
        private const float FallbackWidth = 240f;
        private const float FallbackHeight = 150f;

        private static readonly GUIContent LinkSidesContent = new GUIContent("Link Sides", "Edit one side and the other three follow. The four fields stay live either way - unlinking leaves them exactly as they look.");
        private static readonly GUIContent LinkCornersContent = new GUIContent("Link Corners", "Edit one corner and the other three follow.");
        private static readonly GUIContent FillColorContent = new GUIContent("Fill Color", "Colour painted inside the border. Alpha is honoured, so a transparent fill leaves an outline on its own.");
        private static readonly GUIContent FillTintContent = new GUIContent("Fill Tint", "Multiplies the gradient below. White leaves it as it is; the alpha fades the whole fill.");
        private static readonly GUIContent GradientModeContent = new GUIContent("Gradient", "Linear runs the ramp across the box at the angle below. Radial runs it from the middle out to the edge, following the shape rather than a circle.");
        private static readonly GUIContent GradientContent = new GUIContent("Colors", "Each key past the two ends cuts the fill into another band, since vertex colours can only blend in a straight line between stops. A handful is cheap.");
        private static readonly GUIContent GradientAngleContent = new GUIContent("Angle", "0 left to right, 90 bottom to top, 180 right to left. The ramp spans the shape at any angle.");
        private static readonly GUIContent BorderGradientContent = new GUIContent("Border Gradient", "Linear runs one ramp across the whole box; Around runs it along the outline from the top left, clockwise. Either replaces the four side colours above.");
        private static readonly GUIContent AddParticlesContent = new GUIContent("Add Border Particles", "Adds a particle system that spawns from this border - a fire to start with, sized to the box. Sides with no thickness emit nothing.");
        private static readonly GUIContent AddImageContent = new GUIContent("Add Masked Image", "Adds a picture clipped to these corners: a Mask on this box, an Image inside it, and an overlay that keeps the border above the picture.");
        private static readonly GUIContent TintContent = new GUIContent("Tint", "Multiplies the fill and every border colour. Left white unless something drives it - a fade, an Animator, a CanvasGroup - since those all reach for this field.");

        private static bool showAdvanced;
        private static bool showGraphic;

        private SerializedProperty fillColor;
        private SerializedProperty fillGradientMode;
        private SerializedProperty fillGradient;
        private SerializedProperty fillGradientAngle;
        private SerializedProperty fillUnderBorder;
        private SerializedProperty borderLeft;
        private SerializedProperty borderTop;
        private SerializedProperty borderRight;
        private SerializedProperty borderBottom;
        private SerializedProperty borderColorLeft;
        private SerializedProperty borderColorTop;
        private SerializedProperty borderColorRight;
        private SerializedProperty borderColorBottom;
        private SerializedProperty borderGradientMode;
        private SerializedProperty borderGradient;
        private SerializedProperty borderGradientAngle;
        private SerializedProperty radiusTopLeft;
        private SerializedProperty radiusTopRight;
        private SerializedProperty radiusBottomRight;
        private SerializedProperty radiusBottomLeft;
        private SerializedProperty cornerSegments;
        private SerializedProperty edgeSoftness;
        private SerializedProperty uniformBorder;
        private SerializedProperty uniformCorners;

        private Texture2D preview;
        private Color32[] previewPixels;
        private int previewHash;
        private float previewScale = 1f;

        protected override void OnEnable()
        {
            base.OnEnable();

            fillColor = serializedObject.FindProperty("fillColor");
            fillGradientMode = serializedObject.FindProperty("fillGradientMode");
            fillGradient = serializedObject.FindProperty("fillGradient");
            fillGradientAngle = serializedObject.FindProperty("fillGradientAngle");
            fillUnderBorder = serializedObject.FindProperty("fillUnderBorder");
            borderLeft = serializedObject.FindProperty("borderLeft");
            borderTop = serializedObject.FindProperty("borderTop");
            borderRight = serializedObject.FindProperty("borderRight");
            borderBottom = serializedObject.FindProperty("borderBottom");
            borderColorLeft = serializedObject.FindProperty("borderColorLeft");
            borderColorTop = serializedObject.FindProperty("borderColorTop");
            borderColorRight = serializedObject.FindProperty("borderColorRight");
            borderColorBottom = serializedObject.FindProperty("borderColorBottom");
            borderGradientMode = serializedObject.FindProperty("borderGradientMode");
            borderGradient = serializedObject.FindProperty("borderGradient");
            borderGradientAngle = serializedObject.FindProperty("borderGradientAngle");
            radiusTopLeft = serializedObject.FindProperty("radiusTopLeft");
            radiusTopRight = serializedObject.FindProperty("radiusTopRight");
            radiusBottomRight = serializedObject.FindProperty("radiusBottomRight");
            radiusBottomLeft = serializedObject.FindProperty("radiusBottomLeft");
            cornerSegments = serializedObject.FindProperty("cornerSegments");
            edgeSoftness = serializedObject.FindProperty("edgeSoftness");
            uniformBorder = serializedObject.FindProperty("uniformBorder");
            uniformCorners = serializedObject.FindProperty("uniformCorners");
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Hidden and not saved, so nothing cleans it up on selection change but this.
            if (preview != null)
                DestroyImmediate(preview);

            preview = null;
            previewPixels = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawRepairSection();
            DrawShapeHeader();
            DrawShapeWidget();

            EditorGUILayout.Space();
            DrawFillSection();
            DrawBorderGradientSection();
            DrawContentSection();

            EditorGUILayout.Space();
            showAdvanced = EditorGUILayout.BeginFoldoutHeaderGroup(showAdvanced, "Rendering");
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(fillUnderBorder);
                EditorGUILayout.PropertyField(cornerSegments);
                EditorGUILayout.PropertyField(edgeSoftness);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            showGraphic = EditorGUILayout.BeginFoldoutHeaderGroup(showGraphic, "Graphic");
            if (showGraphic)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Material);
                EditorGUILayout.PropertyField(m_Color, TintContent);
                RaycastControlsGUI();
                MaskableControlsGUI();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawFillSection()
        {
            bool flat = fillGradientMode.enumValueIndex == (int)EFillGradient.None || fillGradientMode.hasMultipleDifferentValues;

            // Renamed rather than hidden while a gradient runs: it still does something - it multiplies the
            // whole ramp - and a field that vanishes reads as a setting that stopped applying.
            EditorGUILayout.PropertyField(fillColor, flat ? FillColorContent : FillTintContent);
            EditorGUILayout.PropertyField(fillGradientMode, GradientModeContent);

            if (flat)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(fillGradient, GradientContent);

            if (fillGradientMode.enumValueIndex == (int)EFillGradient.Linear)
                EditorGUILayout.PropertyField(fillGradientAngle, GradientAngleContent);

            EditorGUI.indentLevel--;
        }

        private void DrawBorderGradientSection()
        {
            EditorGUILayout.PropertyField(borderGradientMode, BorderGradientContent);
            if (!HasBorderGradient)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(borderGradient, GradientContent);

            if (borderGradientMode.enumValueIndex == (int)EBorderGradient.Linear)
                EditorGUILayout.PropertyField(borderGradientAngle, GradientAngleContent);

            EditorGUI.indentLevel--;
        }

        // The four swatches in the widget above are still there, still holding what they held, but the
        // gradient is what gets drawn - so they are greyed rather than hidden, and the layout does not jump
        // about as the mode is switched.
        private bool HasBorderGradient =>
            !borderGradientMode.hasMultipleDifferentValues && borderGradientMode.enumValueIndex != (int)EBorderGradient.None;

        // A graphic with no CanvasRenderer draws nothing, and Unity leaves that component out of the Add
        // Component menu - so an object that ends up without one is stuck there with no way back. Objects
        // made before the requirement was declared are the ones this is for; new ones cannot get into the
        // state at all.
        private void DrawRepairSection()
        {
            if (!NeedsCanvasRenderer())
                return;

            EditorGUILayout.HelpBox(
                "This object has no Canvas Renderer, so nothing it draws reaches the screen. Unity keeps that component out of the Add Component menu, so add it from here.",
                MessageType.Error);

            if (!GUILayout.Button("Add Canvas Renderer"))
                return;

            foreach (var each in targets)
            {
                var graphic = each as RoundedBox;
                if (graphic == null || graphic.GetComponent<CanvasRenderer>() != null)
                    continue;

                Undo.AddComponent<CanvasRenderer>(graphic.gameObject);

                // The graphic is already enabled, so nothing else will ask it to rebuild - it has to be
                // told, or the renderer that was just added stays empty until something else disturbs it.
                graphic.SetAllDirty();
            }

            GUIUtility.ExitGUI();
        }

        private bool NeedsCanvasRenderer()
        {
            foreach (var each in targets)
            {
                if (each is RoundedBox graphic && graphic.GetComponent<CanvasRenderer>() == null)
                    return true;
            }

            return false;
        }

        // Clipping a picture to the corners takes three parts that have to agree - a Mask, the picture, and
        // an overlay that puts the border back above the picture - so it is a button rather than a note in
        // the documentation. What is left here is the two ways the rig can be half built, each said plainly
        // at the point where it is visible.
        private void DrawContentSection()
        {
            if (serializedObject.isEditingMultipleObjects)
                return;

            var box = (RoundedBox)target;

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(AddImageContent, GUILayout.Height(22f)))
                {
                    var image = RoundedBoxMenu.AddMaskedImage(box);
                    if (image != null)
                        Selection.activeGameObject = image;

                    // The rig adds a Mask to the object being inspected, which leaves this editor holding a
                    // serialised object that no longer matches. Bailing out now lets it be rebuilt.
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button(AddParticlesContent, GUILayout.Height(22f)))
                {
                    var effect = RoundedBoxMenu.AddBorderParticles(box);
                    if (effect != null)
                        Selection.activeGameObject = effect;

                    GUIUtility.ExitGUI();
                }
            }

            DrawParticleSortingNote(box);

            var mask = box.GetComponent<Mask>();
            if (mask == null || !mask.MaskEnabled())
                return;

            if (mask.showMaskGraphic && fillColor.colorValue.a <= 0f)
            {
                EditorGUILayout.HelpBox(
                    "Fill Color is transparent, so this box cuts no shape for its Mask and everything inside it is hidden. Give the fill some alpha, or turn off Show Mask Graphic to use the shape without painting it.",
                    MessageType.Warning);
            }

            if (HasBorder(box) && box.transform.childCount > 0 && !HasBorderOverlay(box))
            {
                EditorGUILayout.HelpBox(
                    "Children draw over this box, so the border is behind them. Add a border overlay to draw it back on top.",
                    MessageType.Info);

                if (GUILayout.Button("Add Border Overlay"))
                {
                    RoundedBoxMenu.EnsureBorderOverlay(box);
                    GUIUtility.ExitGUI();
                }
            }
        }

        // Particles are renderers, not graphics, so an overlay canvas has nowhere to put them in its draw
        // order - they land in front of the whole UI or behind all of it, never between this box and the
        // one next to it. Said only once there is actually an effect to be surprised by.
        private static void DrawParticleSortingNote(RoundedBox box)
        {
            if (box.GetComponentInChildren<RoundedBoxBorderShape>(true) == null)
                return;

            var canvas = box.canvas;
            if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                return;

            EditorGUILayout.HelpBox(
                "This canvas is Screen Space - Overlay, which draws particles either in front of the whole UI or behind all of it. Switch it to Screen Space - Camera or World Space and set the particle renderer's sorting layer and order to place the effect within the UI.",
                MessageType.Info);
        }

        private static bool HasBorder(RoundedBox box)
        {
            return box.BorderLeft > 0f || box.BorderTop > 0f || box.BorderRight > 0f || box.BorderBottom > 0f;
        }

        private static bool HasBorderOverlay(RoundedBox box)
        {
            foreach (var overlay in box.GetComponentsInChildren<RoundedBoxBorderOverlay>(true))
            {
                if (overlay.Source == box)
                    return true;
            }

            return false;
        }

        // Not DrawHeader: Editor already has one of those, and it draws the component's title bar.
        private void DrawShapeHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel, GUILayout.Width(46f));

                // Reads at a glance why a 2 unit border shows as a hairline: the preview is a scaled
                // picture of the real rect, so at 40% a thin border is genuinely thin.
                var scaleLabel = previewScale > 0f ? $"{previewScale * 100f:0}%" : string.Empty;
                EditorGUILayout.LabelField(scaleLabel, EditorStyles.miniLabel, GUILayout.Width(40f));

                GUILayout.FlexibleSpace();

                uniformBorder.boolValue = GUILayout.Toggle(uniformBorder.boolValue, LinkSidesContent, EditorStyles.miniButtonLeft, GUILayout.Width(76f));
                uniformCorners.boolValue = GUILayout.Toggle(uniformCorners.boolValue, LinkCornersContent, EditorStyles.miniButtonRight, GUILayout.Width(86f));
            }
        }

        private void DrawShapeWidget()
        {
            var area = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(WidgetHeight), GUILayout.ExpandWidth(true));

            // A docked inspector can be dragged narrower than the gutters need. Rather than let the side
            // fields slide over each other, the preview keeps a floor and the gutters give way first.
            float gutter = Mathf.Min(SideGutter, Mathf.Max(0f, (area.width - 90f) * 0.5f));
            var previewRect = new Rect(
                area.x + gutter,
                area.y + FieldHeight + Gap,
                Mathf.Max(40f, area.width - gutter * 2f),
                Mathf.Max(40f, area.height - (FieldHeight + Gap) * 2f));

            DrawPreview(previewRect);

            // Corners first so a side swatch can never be covered by one; both are drawn after the
            // preview, which is only ever a backdrop.
            DrawCorners(previewRect);
            DrawSides(area, previewRect, gutter);
        }

        private void DrawCorners(Rect previewRect)
        {
            // The two fields on a row must not meet in the middle however narrow the preview gets, or the
            // corner they belong to stops being obvious - which is the entire point of putting them here.
            float width = Mathf.Min(CornerWidth, (previewRect.width - Inset * 2f - Gap * 2f) * 0.5f);
            if (width < 24f)
                return;

            float right = previewRect.xMax - Inset - width;
            float bottom = previewRect.yMax - Inset - FieldHeight;

            DrawRadius(new Rect(previewRect.x + Inset, previewRect.y + Inset, width, FieldHeight), radiusTopLeft, "Top left corner radius.");
            DrawRadius(new Rect(right, previewRect.y + Inset, width, FieldHeight), radiusTopRight, "Top right corner radius.");
            DrawRadius(new Rect(previewRect.x + Inset, bottom, width, FieldHeight), radiusBottomLeft, "Bottom left corner radius.");
            DrawRadius(new Rect(right, bottom, width, FieldHeight), radiusBottomRight, "Bottom right corner radius.");
        }

        private void DrawSides(Rect area, Rect previewRect, float gutter)
        {
            // Top and bottom get thickness and swatch side by side across the middle; left and right have
            // only the gutter to work with, so theirs stack.
            float pairWidth = FieldWidth + Gap + SwatchWidth;
            float pairX = previewRect.center.x - pairWidth * 0.5f;

            DrawSide(new Rect(pairX, area.y, FieldWidth, FieldHeight), new Rect(pairX + FieldWidth + Gap, area.y + 1f, SwatchWidth, FieldHeight - 2f), borderTop, borderColorTop, "Top border.");
            DrawSide(new Rect(pairX, area.yMax - FieldHeight, FieldWidth, FieldHeight), new Rect(pairX + FieldWidth + Gap, area.yMax - FieldHeight + 1f, SwatchWidth, FieldHeight - 2f), borderBottom, borderColorBottom, "Bottom border.");

            float stackWidth = Mathf.Clamp(gutter - Gap * 2f, 20f, FieldWidth);
            float stackTop = previewRect.center.y - (FieldHeight + SwatchHeight + Gap) * 0.5f;

            // Held inside the widget rather than allowed to run off it: a gutter narrower than the fields
            // would otherwise put the left stack outside the inspector where it cannot be reached.
            float leftX = Mathf.Max(area.x, previewRect.x - Gap - stackWidth);
            float rightX = Mathf.Min(area.xMax - stackWidth, previewRect.xMax + Gap);

            DrawSide(new Rect(leftX, stackTop, stackWidth, FieldHeight), new Rect(leftX, stackTop + FieldHeight + Gap, stackWidth, SwatchHeight), borderLeft, borderColorLeft, "Left border.");
            DrawSide(new Rect(rightX, stackTop, stackWidth, FieldHeight), new Rect(rightX, stackTop + FieldHeight + Gap, stackWidth, SwatchHeight), borderRight, borderColorRight, "Right border.");
        }

        private void DrawRadius(Rect rect, SerializedProperty radius, string tooltip)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, radius, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                radius.floatValue = Mathf.Max(0f, radius.floatValue);
                if (uniformCorners.boolValue)
                {
                    // Written straight onto the other three rather than kept behind a flag the runtime
                    // would have to honour: the component stays four independent numbers, and the link is
                    // only ever a way of typing them.
                    float value = radius.floatValue;
                    radiusTopLeft.floatValue = value;
                    radiusTopRight.floatValue = value;
                    radiusBottomRight.floatValue = value;
                    radiusBottomLeft.floatValue = value;
                }
            }

            Tooltip(rect, tooltip + (uniformCorners.boolValue ? " Linked - this sets all four." : string.Empty));
        }

        private void DrawSide(Rect sizeRect, Rect colorRect, SerializedProperty size, SerializedProperty tint, string tooltip)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(sizeRect, size, GUIContent.none);
            if (EditorGUI.EndChangeCheck())
            {
                size.floatValue = Mathf.Max(0f, size.floatValue);
                if (uniformBorder.boolValue)
                {
                    float value = size.floatValue;
                    borderLeft.floatValue = value;
                    borderTop.floatValue = value;
                    borderRight.floatValue = value;
                    borderBottom.floatValue = value;
                }
            }

            using (new EditorGUI.DisabledScope(HasBorderGradient))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(colorRect, tint, GUIContent.none);
                if (EditorGUI.EndChangeCheck() && uniformBorder.boolValue)
                {
                    var value = tint.colorValue;
                    borderColorLeft.colorValue = value;
                    borderColorTop.colorValue = value;
                    borderColorRight.colorValue = value;
                    borderColorBottom.colorValue = value;
                }
            }

            string hint = tooltip + " Thickness, then colour."
                + (uniformBorder.boolValue ? " Linked - these set all four sides." : string.Empty)
                + (HasBorderGradient ? " The colour is overridden by the border gradient below." : string.Empty);
            Tooltip(sizeRect, hint);
            Tooltip(colorRect, hint);
        }

        // The fields are drawn without labels to keep the layout readable, which leaves them with no
        // tooltip of their own. A label carrying nothing but the tooltip, drawn over the top, gives it
        // back; labels take no input, so the field underneath still behaves normally.
        private static void Tooltip(Rect rect, string text)
        {
            GUI.Label(rect, new GUIContent(string.Empty, text));
        }

        private void DrawPreview(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // Layout passes hand out placeholder widths, so the texture is only ever sized off a repaint.
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width * EditorGUIUtility.pixelsPerPoint), 16, MaxPreviewSide);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.height * EditorGUIUtility.pixelsPerPoint), 16, MaxPreviewSide);

            int hash = PreviewHash(width, height);
            if (preview == null || hash != previewHash)
            {
                Rasterise(width, height);
                previewHash = hash;
            }

            GUI.DrawTexture(rect, preview, ScaleMode.StretchToFill, false);

            var frame = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.12f) : new Color(0f, 0f, 0f, 0.2f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), frame);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), frame);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), frame);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), frame);
        }

        private int PreviewHash(int width, int height)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + width;
                hash = hash * 31 + height;
                hash = hash * 31 + fillColor.colorValue.GetHashCode();
                hash = hash * 31 + fillGradientMode.enumValueIndex;
                hash = hash * 31 + fillGradientAngle.floatValue.GetHashCode();
                var component = target as RoundedBox;
                hash = hash * 31 + GradientHash(component != null ? component.FillGradient : null);
                hash = hash * 31 + borderGradientMode.enumValueIndex;
                hash = hash * 31 + borderGradientAngle.floatValue.GetHashCode();
                hash = hash * 31 + GradientHash(component != null ? component.BorderGradient : null);
                hash = hash * 31 + fillUnderBorder.boolValue.GetHashCode();
                hash = hash * 31 + m_Color.colorValue.GetHashCode();
                hash = hash * 31 + borderLeft.floatValue.GetHashCode();
                hash = hash * 31 + borderTop.floatValue.GetHashCode();
                hash = hash * 31 + borderRight.floatValue.GetHashCode();
                hash = hash * 31 + borderBottom.floatValue.GetHashCode();
                hash = hash * 31 + borderColorLeft.colorValue.GetHashCode();
                hash = hash * 31 + borderColorTop.colorValue.GetHashCode();
                hash = hash * 31 + borderColorRight.colorValue.GetHashCode();
                hash = hash * 31 + borderColorBottom.colorValue.GetHashCode();
                hash = hash * 31 + radiusTopLeft.floatValue.GetHashCode();
                hash = hash * 31 + radiusTopRight.floatValue.GetHashCode();
                hash = hash * 31 + radiusBottomRight.floatValue.GetHashCode();
                hash = hash * 31 + radiusBottomLeft.floatValue.GetHashCode();
                hash = hash * 31 + SourceSize().GetHashCode();
                hash = hash * 31 + (EditorGUIUtility.isProSkin ? 1 : 0);
                return hash;
            }
        }

        // Gradient is a class, so its own hash says nothing about the keys inside it - two different ramps
        // in the same object are the same reference, and the preview would sit on the old one.
        private static int GradientHash(Gradient gradient)
        {
            if (gradient == null)
                return 0;

            unchecked
            {
                int hash = 19;
                var colorKeys = gradient.colorKeys;
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    hash = hash * 31 + colorKeys[i].color.GetHashCode();
                    hash = hash * 31 + colorKeys[i].time.GetHashCode();
                }

                var alphaKeys = gradient.alphaKeys;
                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    hash = hash * 31 + alphaKeys[i].alpha.GetHashCode();
                    hash = hash * 31 + alphaKeys[i].time.GetHashCode();
                }

                return hash * 31 + (int)gradient.mode;
            }
        }

        // The rect the shape is really drawn into, so the preview shares its proportions and a border set
        // in canvas units reads at the size it will actually be.
        private Vector2 SourceSize()
        {
            var rectTransform = target is Component component ? component.transform as RectTransform : null;
            if (rectTransform == null)
                return new Vector2(FallbackWidth, FallbackHeight);

            var size = rectTransform.rect.size;
            return size.x > 1f && size.y > 1f ? size : new Vector2(FallbackWidth, FallbackHeight);
        }

        // A rect with a radius per corner, each held as a pair so the inner outline can be elliptical -
        // which it is wherever the two borders behind a corner differ, exactly as the mesh builds it.
        private struct Shape
        {
            public Rect Rect;
            public Vector2 TopLeft;
            public Vector2 TopRight;
            public Vector2 BottomRight;
            public Vector2 BottomLeft;
        }

        private void Rasterise(int width, int height)
        {
            if (preview == null || preview.width != width || preview.height != height)
            {
                if (preview != null)
                    DestroyImmediate(preview);

                preview = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
            }

            if (previewPixels == null || previewPixels.Length != width * height)
                previewPixels = new Color32[width * height];

            var source = SourceSize();
            float margin = 0.86f;
            previewScale = Mathf.Min(Mathf.Min(width / source.x, height / source.y) * margin, 8f);

            float boxWidth = source.x * previewScale;
            float boxHeight = source.y * previewScale;
            var box = new Rect((width - boxWidth) * 0.5f, (height - boxHeight) * 0.5f, boxWidth, boxHeight);

            var metrics = MeasuredMetrics(source, out var radii);
            var outer = Outer(box, radii);
            var inner = Inner(box, metrics, radii);

            var tint = m_Color.colorValue;
            var fill = fillColor.colorValue * tint;
            bool underBorder = fillUnderBorder.boolValue;

            // A border thick enough to swallow the box leaves no inner shape at all, and the coverage of a
            // rect with no area is meaningless - the distance to it is not, so only this is switched off.
            bool innerHasArea = inner.Rect.width > 0f && inner.Rect.height > 0f;

            var ramp = BuildRamp(fill, box, out var mode, out var axis, out float rampMin, out float rampMax);
            var borderRamp = BuildBorderRamp(tint, box, out var borderMode, out var borderAxis, out float borderMin, out float borderMax);

            var sides = new[]
            {
                borderColorLeft.colorValue * tint,
                borderColorTop.colorValue * tint,
                borderColorRight.colorValue * tint,
                borderColorBottom.colorValue * tint,
            };

            var checkerA = EditorGUIUtility.isProSkin ? new Color(0.24f, 0.24f, 0.24f) : new Color(0.78f, 0.78f, 0.78f);
            var checkerB = EditorGUIUtility.isProSkin ? new Color(0.19f, 0.19f, 0.19f) : new Color(0.70f, 0.70f, 0.70f);
            int cell = Mathf.Max(4, Mathf.RoundToInt(7f * EditorGUIUtility.pixelsPerPoint));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Half-pixel offset so coverage is sampled at the centre of the pixel rather than its
                    // corner, which is what keeps the arcs from reading a half pixel small.
                    var point = new Vector2(x + 0.5f, y + 0.5f);

                    // Kept as distances rather than only as coverage: how deep into the border a pixel sits
                    // is what a frame gradient is painted from, and that is the difference between these
                    // two rather than anything the clamped coverage still remembers.
                    float outerDistance = Distance(outer, point);
                    float innerDistance = Distance(inner, point);

                    float outerCoverage = Mathf.Clamp01(0.5f - outerDistance);
                    float innerCoverage = innerHasArea ? Mathf.Clamp01(0.5f - innerDistance) : 0f;

                    var pixel = ((x / cell + y / cell) & 1) == 0 ? checkerA : checkerB;

                    var painted = ramp == null ? fill : Sample(ramp, mode, point, box, axis, rampMin, rampMax);
                    float fillAlpha = painted.a * (underBorder ? outerCoverage : innerCoverage);
                    pixel = Over(pixel, painted, fillAlpha);

                    float ringCoverage = Mathf.Max(0f, outerCoverage - innerCoverage);
                    if (ringCoverage > 0f)
                    {
                        var side = borderRamp == null
                            ? SideColorAt(box, radii, sides, point)
                            : SampleBorder(borderRamp, borderMode, point, box, borderAxis, borderMin, borderMax, outerDistance, innerDistance);

                        pixel = Over(pixel, side, side.a * ringCoverage);
                    }

                    previewPixels[y * width + x] = pixel;
                }
            }

            preview.SetPixels32(previewPixels);
            preview.Apply(false);
        }

        // The gradient sampled into a table once instead of evaluated a quarter of a million times.
        // Gradient.Evaluate is a call into native code, and the preview would make one per pixel on every
        // drag of a key; a 256 step table is finer than the preview can show.
        private Color[] BuildRamp(Color tint, Rect box, out EFillGradient mode, out Vector2 axis, out float min, out float max)
        {
            mode = EFillGradient.None;
            axis = Vector2.right;
            min = 0f;
            max = 1f;

            var component = target as RoundedBox;
            var gradient = component != null ? component.FillGradient : null;
            if (gradient == null || component.FillGradientMode == EFillGradient.None)
                return null;

            mode = component.FillGradientMode;

            var ramp = new Color[RampSize];
            for (int i = 0; i < RampSize; i++)
                ramp[i] = gradient.Evaluate(i / (float)(RampSize - 1)) * tint;

            if (mode == EFillGradient.Linear)
            {
                float radians = component.FillGradientAngle * Mathf.Deg2Rad;
                axis = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

                // Taken off the box corners, where the mesh takes it off the rounded outline. At an angle
                // the two part by a fraction of a corner radius - a percent or so of the run, and nothing
                // that reads at preview size.
                min = float.MaxValue;
                max = float.MinValue;
                Project(new Vector2(box.xMin, box.yMin), axis, ref min, ref max);
                Project(new Vector2(box.xMin, box.yMax), axis, ref min, ref max);
                Project(new Vector2(box.xMax, box.yMin), axis, ref min, ref max);
                Project(new Vector2(box.xMax, box.yMax), axis, ref min, ref max);
            }

            return ramp;
        }

        private Color[] BuildBorderRamp(Color tint, Rect box, out EBorderGradient mode, out Vector2 axis, out float min, out float max)
        {
            mode = EBorderGradient.None;
            axis = Vector2.right;
            min = 0f;
            max = 1f;

            var component = target as RoundedBox;
            var gradient = component != null ? component.BorderGradient : null;
            if (gradient == null || component.BorderGradientMode == EBorderGradient.None)
                return null;

            mode = component.BorderGradientMode;

            var ramp = new Color[RampSize];
            for (int i = 0; i < RampSize; i++)
                ramp[i] = gradient.Evaluate(i / (float)(RampSize - 1)) * tint;

            if (mode == EBorderGradient.Linear)
            {
                float radians = component.BorderGradientAngle * Mathf.Deg2Rad;
                axis = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

                min = float.MaxValue;
                max = float.MinValue;
                Project(new Vector2(box.xMin, box.yMin), axis, ref min, ref max);
                Project(new Vector2(box.xMin, box.yMax), axis, ref min, ref max);
                Project(new Vector2(box.xMax, box.yMin), axis, ref min, ref max);
                Project(new Vector2(box.xMax, box.yMax), axis, ref min, ref max);
            }

            return ramp;
        }

        private static Color SampleBorder(Color[] ramp, EBorderGradient mode, Vector2 point, Rect box, Vector2 axis, float min, float max, float outerDistance, float innerDistance)
        {
            float t;
            if (mode == EBorderGradient.Frame)
            {
                // How far through the border this pixel is: it lies inside the outer shape and outside the
                // inner one, so the two distances to them add up to the thickness right here - which is the
                // thickness of the side it is on, whatever the other three are set to.
                float fromOuter = Mathf.Max(0f, -outerDistance);
                float thickness = fromOuter + Mathf.Max(0f, innerDistance);
                t = thickness > 0.0001f ? fromOuter / thickness : 0f;
            }
            else if (mode == EBorderGradient.Around)
            {
                // The mesh walks the outline from the top left corner clockwise and measures by distance
                // travelled; this measures by angle from the middle, which is the same journey and the same
                // corners but paced differently along a box that is much wider than it is tall.
                float degrees = Mathf.Atan2(point.y - box.center.y, point.x - box.center.x) * Mathf.Rad2Deg;
                t = Mathf.Repeat(180f - degrees, 360f) / 360f;
            }
            else
            {
                t = max - min > 0.0001f ? (Vector2.Dot(point, axis) - min) / (max - min) : 0.5f;
            }

            return ramp[Mathf.Clamp(Mathf.RoundToInt(t * (RampSize - 1)), 0, RampSize - 1)];
        }

        private static void Project(Vector2 point, Vector2 axis, ref float min, ref float max)
        {
            float distance = Vector2.Dot(point, axis);
            min = Mathf.Min(min, distance);
            max = Mathf.Max(max, distance);
        }

        private static Color Sample(Color[] ramp, EFillGradient mode, Vector2 point, Rect box, Vector2 axis, float min, float max)
        {
            float t;
            if (mode == EFillGradient.Radial)
            {
                // The mesh rings out along the shape, so its bands are the outline scaled about the centre.
                // Measured against the half size that is the same thing everywhere but the corners.
                var offset = point - box.center;
                t = Mathf.Max(
                    Mathf.Abs(offset.x) / Mathf.Max(box.width * 0.5f, 0.001f),
                    Mathf.Abs(offset.y) / Mathf.Max(box.height * 0.5f, 0.001f));
            }
            else
            {
                t = max - min > 0.0001f ? (Vector2.Dot(point, axis) - min) / (max - min) : 0.5f;
            }

            return ramp[Mathf.Clamp(Mathf.RoundToInt(t * (RampSize - 1)), 0, RampSize - 1)];
        }

        // The same reconciliation the component does before it builds anything - borders that will not fit
        // scaled down in proportion, radii that overlap an edge scaled down together - so the preview
        // shows what will be drawn rather than what was typed.
        private Vector4 MeasuredMetrics(Vector2 source, out Vector4 radii)
        {
            var borders = new Vector4(
                Mathf.Max(0f, borderLeft.floatValue),
                Mathf.Max(0f, borderTop.floatValue),
                Mathf.Max(0f, borderRight.floatValue),
                Mathf.Max(0f, borderBottom.floatValue));

            float horizontal = borders.x + borders.z;
            if (horizontal > source.x)
            {
                float k = source.x / horizontal;
                borders.x *= k;
                borders.z *= k;
            }

            float vertical = borders.y + borders.w;
            if (vertical > source.y)
            {
                float k = source.y / vertical;
                borders.y *= k;
                borders.w *= k;
            }

            radii = new Vector4(
                Mathf.Max(0f, radiusTopLeft.floatValue),
                Mathf.Max(0f, radiusTopRight.floatValue),
                Mathf.Max(0f, radiusBottomRight.floatValue),
                Mathf.Max(0f, radiusBottomLeft.floatValue));

            float scale = 1f;
            scale = Mathf.Min(scale, EdgeRatio(source.x, radii.x + radii.y));
            scale = Mathf.Min(scale, EdgeRatio(source.x, radii.w + radii.z));
            scale = Mathf.Min(scale, EdgeRatio(source.y, radii.x + radii.w));
            scale = Mathf.Min(scale, EdgeRatio(source.y, radii.y + radii.z));
            radii *= scale;

            // Both come back in source units; everything downstream works in preview pixels.
            borders *= previewScale;
            radii *= previewScale;
            return borders;
        }

        private static float EdgeRatio(float length, float demand) => demand > length ? length / demand : 1f;

        private static Shape Outer(Rect box, Vector4 radii)
        {
            return new Shape
            {
                Rect = box,
                TopLeft = new Vector2(radii.x, radii.x),
                TopRight = new Vector2(radii.y, radii.y),
                BottomRight = new Vector2(radii.z, radii.z),
                BottomLeft = new Vector2(radii.w, radii.w),
            };
        }

        private static Shape Inner(Rect box, Vector4 borders, Vector4 radii)
        {
            var rect = new Rect(
                box.x + borders.x,
                box.y + borders.w,
                Mathf.Max(0f, box.width - borders.x - borders.z),
                Mathf.Max(0f, box.height - borders.y - borders.w));

            return new Shape
            {
                Rect = rect,
                TopLeft = new Vector2(Mathf.Max(0f, radii.x - borders.x), Mathf.Max(0f, radii.x - borders.y)),
                TopRight = new Vector2(Mathf.Max(0f, radii.y - borders.z), Mathf.Max(0f, radii.y - borders.y)),
                BottomRight = new Vector2(Mathf.Max(0f, radii.z - borders.z), Mathf.Max(0f, radii.z - borders.w)),
                BottomLeft = new Vector2(Mathf.Max(0f, radii.w - borders.x), Mathf.Max(0f, radii.w - borders.w)),
            };
        }

        // How much of a pixel the shape covers, from its signed distance: one pixel of falloff either side
        // of the outline. That is all the anti-aliasing the preview needs, and the reason the corners read
        // as curves at this size at all. Applied at the call site, which needs the distance itself too.
        private static float Distance(Shape shape, Vector2 point)
        {
            var rect = shape.Rect;

            // Distance to the box itself: positive outside, and inside it is the distance to the nearest
            // edge, negated. Every point not in a corner's quadrant is answered by this alone.
            float box = Mathf.Max(
                Mathf.Max(rect.xMin - point.x, point.x - rect.xMax),
                Mathf.Max(rect.yMin - point.y, point.y - rect.yMax));

            Vector2 center;
            Vector2 radius;
            if (point.x <= rect.xMin + shape.TopLeft.x && point.y >= rect.yMax - shape.TopLeft.y)
            {
                center = new Vector2(rect.xMin + shape.TopLeft.x, rect.yMax - shape.TopLeft.y);
                radius = shape.TopLeft;
            }
            else if (point.x >= rect.xMax - shape.TopRight.x && point.y >= rect.yMax - shape.TopRight.y)
            {
                center = new Vector2(rect.xMax - shape.TopRight.x, rect.yMax - shape.TopRight.y);
                radius = shape.TopRight;
            }
            else if (point.x >= rect.xMax - shape.BottomRight.x && point.y <= rect.yMin + shape.BottomRight.y)
            {
                center = new Vector2(rect.xMax - shape.BottomRight.x, rect.yMin + shape.BottomRight.y);
                radius = shape.BottomRight;
            }
            else if (point.x <= rect.xMin + shape.BottomLeft.x && point.y <= rect.yMin + shape.BottomLeft.y)
            {
                center = new Vector2(rect.xMin + shape.BottomLeft.x, rect.yMin + shape.BottomLeft.y);
                radius = shape.BottomLeft;
            }
            else
            {
                return box;
            }

            if (radius.x <= 0f || radius.y <= 0f)
                return box;

            // Squashing the corner back to a unit circle handles the elliptical case for free; the result
            // is stretched by the smaller radius, which turns it back into something near enough a
            // distance for one pixel of falloff to be measured against.
            var offset = new Vector2((point.x - center.x) / radius.x, (point.y - center.y) / radius.y);
            return (offset.magnitude - 1f) * Mathf.Min(radius.x, radius.y);
        }

        // Which side's colour a point on the border takes. Inside a corner's quadrant the two sides that
        // meet there are blended by angle, which is what the mesh does across the arc; everywhere else the
        // nearest edge wins outright.
        private static Color SideColorAt(Rect box, Vector4 radii, Color[] sides, Vector2 point)
        {
            const int Left = 0;
            const int Top = 1;
            const int Right = 2;
            const int Bottom = 3;

            if (radii.x > 0f && point.x <= box.xMin + radii.x && point.y >= box.yMax - radii.x)
                return CornerBlend(new Vector2(box.xMin + radii.x, box.yMax - radii.x), point, 180f, 90f, sides[Left], sides[Top]);

            if (radii.y > 0f && point.x >= box.xMax - radii.y && point.y >= box.yMax - radii.y)
                return CornerBlend(new Vector2(box.xMax - radii.y, box.yMax - radii.y), point, 90f, 0f, sides[Top], sides[Right]);

            if (radii.z > 0f && point.x >= box.xMax - radii.z && point.y <= box.yMin + radii.z)
                return CornerBlend(new Vector2(box.xMax - radii.z, box.yMin + radii.z), point, 0f, -90f, sides[Right], sides[Bottom]);

            if (radii.w > 0f && point.x <= box.xMin + radii.w && point.y <= box.yMin + radii.w)
                return CornerBlend(new Vector2(box.xMin + radii.w, box.yMin + radii.w), point, -90f, -180f, sides[Bottom], sides[Left]);

            float left = point.x - box.xMin;
            float right = box.xMax - point.x;
            float top = box.yMax - point.y;
            float bottom = point.y - box.yMin;

            float nearest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(top, bottom));
            if (nearest == left)
                return sides[Left];
            if (nearest == right)
                return sides[Right];
            return nearest == top ? sides[Top] : sides[Bottom];
        }

        private static Color CornerBlend(Vector2 center, Vector2 point, float startAngle, float endAngle, Color start, Color end)
        {
            float angle = Mathf.Atan2(point.y - center.y, point.x - center.x) * Mathf.Rad2Deg;

            // Atan2 breaks at half a turn, which is exactly where the bottom-left corner sits; folding the
            // answer back into that corner's own range keeps its blend from running the wrong way.
            if (endAngle < -90f && angle > 0f)
                angle -= 360f;

            float t = Mathf.Clamp01(Mathf.InverseLerp(startAngle, endAngle, angle));
            return Color.Lerp(start, end, t);
        }

        // Source over an opaque background, so the checker shows through a partly transparent fill the way
        // the canvas behind the panel will.
        private static Color Over(Color background, Color source, float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (alpha <= 0f)
                return background;

            return new Color(
                background.r + (source.r - background.r) * alpha,
                background.g + (source.g - background.g) * alpha,
                background.b + (source.b - background.b) * alpha,
                1f);
        }
    }
}
