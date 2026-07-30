using UnityEngine;

namespace FlappyTemplate
{
    // Holds a RectTransform between a minimum and a maximum size, whatever the canvas scaler and the
    // parent hand it. Each limit is switched on individually, so 0 is a size like any other.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class RectTransformSizeClamp : MonoBehaviour
    {
        [Tooltip("Canvas Units are what the inspector's Width/Height show. Screen Pixels divides by the canvas scale factor first, so the limits hold in real pixels however the CanvasScaler scales the UI.")]
        [SerializeField]
        private ESizeUnits units = ESizeUnits.CanvasUnits;

        [SerializeField]
        private bool useMinWidth = true;

        [SerializeField]
        private float minWidth = 400f;

        [SerializeField]
        private bool useMinHeight = true;

        [SerializeField]
        private float minHeight = 600f;

        [SerializeField]
        private bool useMaxWidth;

        [SerializeField]
        private float maxWidth;

        [SerializeField]
        private bool useMaxHeight;

        [SerializeField]
        private float maxHeight;

        private RectTransform rect;
        private Canvas canvas;

        // The sizeDelta the rect was authored with, which is not the same thing as the one it is
        // carrying: the limits are enforced by writing sizeDelta, so clamping against the current value
        // compounds. On a stretched rect width is parentWidth + sizeDelta, so a delta raised on a narrow
        // screen stays baked in and the rect ends up permanently too wide once the screen grows back.
        private Vector2 baseSizeDelta;

        // Marks the axes this component overrides. Driven properties are left out when Unity serializes
        // a scene or prefab and are greyed out in the inspector, which is what makes running in edit
        // mode safe: the authored sizeDelta stays in the scene file no matter what gets clamped while
        // the Game view happens to be small.
        private DrivenRectTransformTracker tracker;

        void OnEnable()
        {
            rect = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();

            // Whatever is on the rect right now is the authored value - anything this component wrote
            // before was driven, so it never made it into the file this was loaded from.
            baseSizeDelta = rect.sizeDelta;
            Apply();
        }

        void OnDisable()
        {
            tracker.Clear();

            // Hand the rect back the way it was found, rather than frozen at the last clamped size.
            if (rect != null)
                rect.sizeDelta = baseSizeDelta;
        }

        void LateUpdate() => Apply();

        // Canvas units are pixels divided by the scale factor, so a pixel limit converts the same way.
        // The factor is the root canvas's - a nested canvas inherits it - and it is read every frame
        // because the CanvasScaler recomputes it whenever the resolution or orientation changes.
        private float LimitScale => canvas != null && units == ESizeUnits.ScreenPixels ? 1f / canvas.rootCanvas.scaleFactor : 1f;

        private void Apply()
        {
            if (rect == null)
                return;

            var parentSize = rect.parent is RectTransform parent ? parent.rect.size : Vector2.zero;
            float scale = LimitScale;

            // How much of the parent the anchors already contribute; the rest is the authored delta.
            var stretch = rect.anchorMax - rect.anchorMin;
            var stretched = new Vector2(parentSize.x * stretch.x, parentSize.y * stretch.y);
            var natural = stretched + baseSizeDelta;

            var size = new Vector2(
                Clamp(natural.x, useMinWidth, minWidth * scale, useMaxWidth, maxWidth * scale),
                Clamp(natural.y, useMinHeight, minHeight * scale, useMaxHeight, maxHeight * scale)
            );

            bool drivenX = !Mathf.Approximately(size.x, natural.x);
            bool drivenY = !Mathf.Approximately(size.y, natural.y);

            tracker.Clear();
            if (drivenX || drivenY)
            {
                var properties = DrivenTransformProperties.None;
                if (drivenX)
                    properties |= DrivenTransformProperties.SizeDeltaX;
                if (drivenY)
                    properties |= DrivenTransformProperties.SizeDeltaY;
                tracker.Add(this, rect, properties);
            }

            // What SetSizeWithCurrentAnchors does for each axis, written once for both.
            var sizeDelta = size - stretched;
            if (rect.sizeDelta != sizeDelta)
                rect.sizeDelta = sizeDelta;

            // An axis that is not being clamped is carrying its authored delta, so the baseline follows
            // anything else that resizes the rect - a layout group, an animation, an inspector edit.
            if (!drivenX)
                baseSizeDelta.x = rect.sizeDelta.x;
            if (!drivenY)
                baseSizeDelta.y = rect.sizeDelta.y;
        }

        // The minimum is applied last on purpose: limits set the wrong way round would otherwise leave
        // the rect below its stated minimum, and being too big is the more forgiving failure.
        private static float Clamp(float size, bool useMin, float min, bool useMax, float max)
        {
            if (useMax)
                size = Mathf.Min(size, max);
            if (useMin)
                size = Mathf.Max(size, min);
            return size;
        }
    }
}
