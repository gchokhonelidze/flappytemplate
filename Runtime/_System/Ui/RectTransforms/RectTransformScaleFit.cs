using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FlappyTemplate
{
    // Sizes a UI panel to the rect it sits in, by scaling it. BoundsFit does this for sprites; this is
    // the same idea for a panel whose children are laid out at fixed sizes - a window drawn at a design
    // size of 1080x1920, a keypad of fixed buttons, a stats board - where stretching the panel would
    // pull the art apart and the whole thing should simply get smaller instead.
    //
    // Scale is used rather than size on purpose. Writing the panel's width and height only moves the
    // panel's own edges: children anchored at fixed sizes stay the size they were and spill out, and a
    // layout group would overwrite the write anyway. localScale takes the children with it, keeps every
    // proportion the panel was authored with, and nothing else in uGUI competes for it.
    //
    // Scope decides what is measured. Rect measures the panel's own rect, which is what a panel with a
    // real design size wants - note that a panel stretched to its parent has no size of its own to fit,
    // so it needs fixed Width/Height to be worth fitting. Content measures the panel and its children
    // together, for a container whose children stick out past it.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class RectTransformScaleFit : MonoBehaviour
    {
        [Tooltip("The rect that is fitted into. Empty uses this panel's parent rect.")]
        [SerializeField]
        private RectTransform source;

        [Tooltip("Rect measures the panel's own Width and Height - right for a panel authored at a design size. Content measures the panel and its active children together, for a container whose children spread past its own rect.")]
        [SerializeField]
        private ERectScope scope = ERectScope.Rect;

        [Tooltip("Which axes are fitted. Width or Height match that axis of the source; Both keeps the panel inside the source on either axis.")]
        [SerializeField]
        private EFitMode fit = EFitMode.Both;

        [Tooltip("Off keeps the panel's proportions - it fits inside the source with room to spare on one axis. On scales width and height separately so it matches the source exactly, distorting the art.")]
        [SerializeField]
        private bool stretch;

        [Range(0.01f, 1f)]
        [Tooltip("Share of the source to fill. 1 fits it exactly, 0.9 leaves a tenth of it as margin.")]
        [SerializeField]
        private float fill = 1f;

        [Tooltip("Trimmed off the source before fitting - x off its width, y off its height, in the source's own units. A fixed margin that stays put as the panel scales, where Fill is a share that grows with the source.")]
        [SerializeField]
        private Vector2 padding;

        [Tooltip("Off never scales the panel past the scale it was authored with, so it only ever shrinks to fit and a roomy source leaves it at its design size. On also grows it to fill the source.")]
        [SerializeField]
        private bool allowUpscale = true;

        [Tooltip("Also moves the panel onto the source, matching feature to feature - its top edge to the source's top edge, its centre to the centre - so an off-centre pivot does not push it out. Off leaves the position to the panel's own anchors and only the scale is driven.")]
        [SerializeField]
        private bool align;

        [SerializeField]
        private ERectAnchor anchor = ERectAnchor.Center;

        [Tooltip("Nudge applied after the anchor is resolved, as a share of the source's size - 0.5 is half its width across. Relative on purpose, so the nudge grows with the source instead of drifting off it.")]
        [SerializeField]
        private Vector2 offset;

        [Tooltip("Off fits once on enable; on keeps the fit while the source resizes, the canvas rescales or the screen rotates.")]
        [SerializeField]
        private bool follow = true;

        [Tooltip("Also fits while editing, so the result is visible without entering play mode. The source is measured at the Game view resolution, not the Scene view's.")]
        [SerializeField]
        private bool applyInEditMode = true;

        private RectTransform rect;

        // The scale and position the panel was authored with. Both are driven while this runs, so they
        // are never serialized and this is what the scene file still holds; keeping them lets Off give
        // the panel back untouched, and gives Allow Upscale a ceiling to cap at.
        private Vector3 baseScale = Vector3.one;
        private Vector3 basePosition;

        // Marks what this component overrides. Driven properties are left out when Unity serializes a
        // scene or prefab, which is what makes fitting in edit mode safe: whatever scale the Game view's
        // current resolution happens to call for never gets baked into the scene.
        private DrivenRectTransformTracker tracker;

        private static readonly Vector3[] Corners = new Vector3[4];

        private RectTransform Source => source != null ? source : rect != null ? rect.parent as RectTransform : null;

        void OnEnable()
        {
            rect = GetComponent<RectTransform>();
            baseScale = rect.localScale;
            basePosition = rect.localPosition;

            if (Source == null)
            {
                // A component added before the source is picked is normal in the editor, so nagging
                // there is just noise; at runtime a panel with no rect above it is a setup error.
                if (Application.isPlaying)
                {
                    Debug.LogError($"{nameof(RectTransformScaleFit)} on {name} has no source and no parent rect to fall back on.", this);
                    enabled = false;
                }
                return;
            }

            // A canvas enabled this same frame has not been laid out yet, so the source would still be
            // sitting at its authored size and the first fit would be measured against the wrong box.
            Canvas.ForceUpdateCanvases();
            Apply();
        }

        void OnDisable()
        {
            tracker.Clear();

            // Hand the panel back as it was authored rather than frozen at the last fit. It matters
            // more here than it would elsewhere: once the value stops being driven it serializes again,
            // so leaving it behind would bake an editor-resolution fit into the scene.
            if (rect == null)
                return;

            if (fit != EFitMode.None && rect.localScale != baseScale)
                rect.localScale = baseScale;
            if (align && rect.localPosition != basePosition)
                rect.localPosition = basePosition;
        }

        // LateUpdate so this frame's layout pass has already run and the source is at its final size.
        void LateUpdate()
        {
            if (follow)
                Apply();
        }

        /// <summary>Measures the source and scales this panel to fit inside it.</summary>
        // With Follow off this is the hook to call when either side changes - a panel rebuilt with more
        // rows, a source resized by a layout group. Fitting on demand also dodges the trap of a
        // per-frame fit: a panel that tweens in from zero scale would otherwise be measured mid-animation.
        [ContextMenu("Apply Now")]
        public void Apply()
        {
            if (!Resolve(out var position, out var scale))
                return;

            Write(position, scale);
        }

        private bool Resolve(out Vector3 position, out Vector3 scale)
        {
            position = transform.position;
            scale = transform.localScale;

            var src = Source;
            if (src == null || rect == null)
                return false;
            if (!Application.isPlaying && !applyInEditMode)
                return false;

            // A source inside the panel would grow as the panel grows and shrink as it shrinks, so the
            // fit would chase its own tail instead of settling.
            if (src == rect || src.IsChildOf(rect))
                return false;

            var available = (src.rect.size - padding) * fill;
            if (available.x <= 0f || available.y <= 0f)
                return false;

            // Both sides are measured in the source's local space, so the factor comes out clean
            // whatever sits between the two - a nested canvas, a scaled holder, an extra parent.
            var bounds = Measure(src);
            if (bounds.size.x <= Mathf.Epsilon && bounds.size.y <= Mathf.Epsilon)
                return false;

            var factor = Vector2.one;
            if (fit != EFitMode.None)
            {
                // Relative, not absolute: the factor is measured from where the panel stands right now,
                // so re-applying it every frame converges instead of compounding - once it fits, the
                // factor is 1. z is left alone; a UI panel has no depth to scale and zeroing it would
                // take the children with it.
                factor = TransformBounds.GetFitScale(available, bounds.size, fit, stretch);
                var fitted = new Vector3(scale.x * factor.x, scale.y * factor.y, scale.z);

                if (!allowUpscale)
                {
                    fitted.x = ClampToBase(fitted.x, baseScale.x);
                    fitted.y = ClampToBase(fitted.y, baseScale.y);

                    // Align lines the panel up at the scale it will actually reach, so it has to be
                    // told what the cap left of the factor rather than what the fit asked for.
                    factor = new Vector2(Ratio(fitted.x, scale.x), Ratio(fitted.y, scale.y));
                }

                scale = fitted;
            }

            if (align)
            {
                var t = RectTransformPoint3D.GetNormalized(anchor);
                var area = src.rect;
                var anchorPoint = new Vector2(Mathf.Lerp(area.xMin, area.xMax, t.x), Mathf.Lerp(area.yMin, area.yMax, t.y))
                    + Vector2.Scale(area.size, offset);

                // A pivot that is not in the middle of what the panel draws - the usual bottom-left
                // pivot, or content sitting off to one side of the rect - would put the pivot on the
                // anchor and leave the panel itself hanging outside. The correction is scaled by factor
                // because localScale scales about the pivot, so the pivot-to-content offset grows with
                // it: this aligns where the panel will be once it reaches the fitted scale.
                var pivot = src.InverseTransformPoint(rect.position);
                var contentPoint = RectTransformFit3D.GetBoundsPoint(bounds, anchor);
                var local = new Vector3(
                    anchorPoint.x + (pivot.x - contentPoint.x) * factor.x,
                    anchorPoint.y + (pivot.y - contentPoint.y) * factor.y,
                    pivot.z
                );

                // Back out through the source rather than writing anchoredPosition, so the panel does
                // not have to be a direct child of the rect it is being fitted into. z is kept as
                // authored, so fitting never reorders anything sorted by depth.
                position = src.TransformPoint(local);
            }

            return true;
        }

        // Both scopes report in the source's local space. Content leans on uGUI's own measurement,
        // which walks the active child rects and takes their world corners - so it covers children that
        // hang outside the panel's rect, and skips ones switched off.
        private Bounds Measure(RectTransform src)
        {
            if (scope == ERectScope.Content)
                return RectTransformUtility.CalculateRelativeRectTransformBounds(src, rect);

            // Corners come back with the panel's own rotation and scale already baked in, which is what
            // makes the factor relative in the same way Content's is.
            rect.GetWorldCorners(Corners);
            var toLocal = src.worldToLocalMatrix;

            var min = toLocal.MultiplyPoint3x4(Corners[0]);
            var max = min;
            for (int i = 1; i < Corners.Length; i++)
            {
                var point = toLocal.MultiplyPoint3x4(Corners[i]);
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        private void Write(Vector3 position, Vector3 scale)
        {
            var driven = DrivenTransformProperties.None;
            if (fit != EFitMode.None)
                driven |= DrivenTransformProperties.ScaleX | DrivenTransformProperties.ScaleY;
            if (align)
                driven |= DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.AnchoredPositionY;

            tracker.Clear();
            if (driven != DrivenTransformProperties.None)
                tracker.Add(this, rect, driven);

            // Writing an unchanged value would flag the scene as modified on every editor repaint.
            if (fit != EFitMode.None && rect.localScale != scale)
                rect.localScale = scale;
            if (align && rect.position != position)
                rect.position = position;
        }

        // Caps how far the fit may go without touching its sign, so a panel mirrored by a negative
        // scale is held to its own magnitude instead of being flipped back the right way round.
        private static float ClampToBase(float value, float baseValue)
        {
            float limit = Mathf.Abs(baseValue);
            return Mathf.Abs(value) <= limit ? value : Mathf.Sign(value) * limit;
        }

        // A panel left at zero scale - tweened out, or authored that way - has no factor to speak of,
        // so alignment treats it as unscaled rather than dividing by nothing.
        private static float Ratio(float fitted, float current) =>
            Mathf.Abs(current) > Mathf.Epsilon ? fitted / current : 1f;

#if UNITY_EDITOR
        // So changing the fit or the anchor in the inspector takes effect right away, even with Follow
        // off. Applying inside OnValidate itself is not allowed - it drives Canvas layout, which Unity
        // refuses mid-validate - so it is deferred by a frame. delayCall fires once and clears itself.
        void OnValidate()
        {
            if (Application.isPlaying || !applyInEditMode)
                return;

            EditorApplication.delayCall += ApplyDeferred;
        }

        private void ApplyDeferred()
        {
            // The frame in between is enough for an undo, a delete or a scene close to take the
            // component with it.
            if (this == null || !isActiveAndEnabled)
                return;

            Canvas.ForceUpdateCanvases();
            Apply();
        }
#endif
    }
}
