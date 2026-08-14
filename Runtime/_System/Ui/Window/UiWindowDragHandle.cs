using UnityEngine;
using UnityEngine.EventSystems;

namespace FlappyTemplate
{
    // Put on whatever is meant to be grabbed - a caption bar, or the whole panel - and it moves the window
    // it points at. UiWindow adds and configures this itself; it is public because a window with a custom
    // header built from prefabs still wants one, on whichever part of that header should be the handle.
    //
    // Dragging is done as a delta rather than by placing the window under the pointer: the pointer's travel
    // since the grab is added to where the window was when it was grabbed. That way a window grabbed by its
    // corner does not jump so its middle lands under the cursor, and none of it depends on how the window's
    // anchors or pivot happen to be set.
    [AddComponentMenu("UI/Ui Window Drag Handle")]
    public class UiWindowDragHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("What moves. Usually the window root, not this object.")]
        [SerializeField]
        private RectTransform target;

        [Tooltip("Keep the window inside its parent, on each axis it fits. On an axis it does not fit, it may be dragged past the edge as long as Keep Visible of it stays showing.")]
        [SerializeField]
        private bool clampToParent = true;

        [Tooltip("How much of a window too big for its parent has to stay inside it. Enough to leave something to grab it by.")]
        [Min(0f)]
        [SerializeField]
        private float keepVisible = 64f;

        [Tooltip("Draw the window over its siblings when it is grabbed.")]
        [SerializeField]
        private bool bringToFront = true;

        [SerializeField]
        private bool interactable = true;

        private Vector2 pointerStart;
        private Vector2 targetStart;
        private bool dragging;

        /// <summary>What this handle moves. Defaults to the object it is on.</summary>
        public RectTransform Target
        {
            get => target;
            set => target = value;
        }

        public bool ClampToParent
        {
            get => clampToParent;
            set => clampToParent = value;
        }

        /// <summary>How much of a window too big for its parent has to stay inside it.</summary>
        public float KeepVisible
        {
            get => keepVisible;
            set => keepVisible = Mathf.Max(0f, value);
        }

        public bool BringToFront
        {
            get => bringToFront;
            set => bringToFront = value;
        }

        /// <summary>Off leaves the handle in place but ignores the pointer - the way to lock a window down
        /// without pulling the component off it.</summary>
        public bool Interactable
        {
            get => interactable;
            set => interactable = value;
        }

        /// <summary>True between the grab and the release.</summary>
        public bool IsDragging => dragging;

        void Awake()
        {
            if (target == null)
                target = transform as RectTransform;
        }

        // The window's own raise rather than the sibling move, where there is a window to ask: sibling order
        // settles nothing between two windows drawn on canvases of their own, which is what Always On Top
        // gives them. The handle sits on the caption, and a press that lands there is delivered to the first
        // handler walking up from it and stops - so the window never hears about this one by itself.
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable || !bringToFront || target == null)
                return;

            var window = target.GetComponent<UiWindow>();

            if (window != null)
                window.Raise();
            else
                target.SetAsLastSibling();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!interactable || target == null)
                return;

            var area = Area;
            if (area == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out pointerStart))
                return;

            targetStart = target.anchoredPosition;
            dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || target == null)
                return;

            var area = Area;
            if (area == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out var pointer))
                return;

            target.anchoredPosition = targetStart + (pointer - pointerStart);

            if (clampToParent)
                Clamp();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
        }

        /// <summary>Pulls the window back inside its parent, for after a resize rather than a drag.</summary>
        public void Clamp()
        {
            if (target == null)
                return;

            var area = Area;
            if (area == null)
                return;

            // Measured on localPosition rather than anchoredPosition, because that is the one that means the
            // same thing whatever the anchors are, and the correction is then applied as a delta - which is
            // the same number in either. The two axes are worked out apart, since a window can easily fit
            // across its parent and not down it.
            //
            // The window's own rect is in its own space and the parent's bounds are in the parent's, so the
            // scale between them has to be taken out before the two can be compared. Skip that and a window
            // at 0.7 is clamped as though it were half as big again as it draws - held off the edges by a
            // margin of nothing, and stopped short on any axis it only overflows on paper.
            var bounds = area.rect;
            var self = target.rect;
            var scale = target.localScale;
            var position = (Vector2)target.localPosition;

            Span(self.xMin, self.xMax, scale.x, out var left, out var right);
            Span(self.yMin, self.yMax, scale.y, out var bottom, out var top);

            target.anchoredPosition += new Vector2(
                Fit(position.x, bounds.xMin, bounds.xMax, left, right) - position.x,
                Fit(position.y, bounds.yMin, bounds.yMax, bottom, top) - position.y);
        }

        // Both ends are ordered again afterwards rather than assumed: a negative scale is a mirrored window,
        // and it puts the low edge of the rect above the high one.
        private static void Span(float min, float max, float scale, out float low, out float high)
        {
            float a = min * scale;
            float b = max * scale;

            low = Mathf.Min(a, b);
            high = Mathf.Max(a, b);
        }

        // Two rules on one axis, because "inside the parent" has no meaning for a window taller than the
        // parent is. Where the window fits, it is held wholly inside. Where it does not, containment is
        // dropped for an overlap: it can be dragged as far as leaving Keep Visible of itself showing, which
        // is what lets a window taller than the screen be pulled up and down to read the rest of it.
        //
        // The old rule centred the window on any axis it did not fit, which read as a drag that worked one
        // way and was dead the other.
        private float Fit(float value, float boundsMin, float boundsMax, float selfMin, float selfMax)
        {
            float min = boundsMin - selfMin;
            float max = boundsMax - selfMax;

            if (min <= max)
                return Mathf.Clamp(value, min, max);

            // Never more than either rect: a margin larger than the window itself would push the two apart
            // rather than hold them together, and the bounds would come out inverted again.
            float keep = Mathf.Max(0f, keepVisible);
            keep = Mathf.Min(keep, Mathf.Min(selfMax - selfMin, boundsMax - boundsMin));

            return Mathf.Clamp(value, boundsMin + keep - selfMax, boundsMax - keep - selfMin);
        }

        private RectTransform Area => target != null ? target.parent as RectTransform : null;
    }
}
