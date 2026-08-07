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

        [Tooltip("Keep the window inside its parent. A window larger than its parent is centred on the axis it does not fit, rather than fighting the edges.")]
        [SerializeField]
        private bool clampToParent = true;

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

        public void OnPointerDown(PointerEventData eventData)
        {
            if (interactable && bringToFront && target != null)
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
            // the same number in either.
            var bounds = area.rect;
            var self = target.rect;
            var position = (Vector2)target.localPosition;

            target.anchoredPosition += new Vector2(
                Fit(position.x, bounds.xMin - self.xMin, bounds.xMax - self.xMax) - position.x,
                Fit(position.y, bounds.yMin - self.yMin, bounds.yMax - self.yMax) - position.y);
        }

        // Bounds arrive the wrong way round when the window is larger than the parent on that axis. Clamping
        // to them in that state pins the window to one edge and leaves it there, so it is centred instead.
        private static float Fit(float value, float min, float max) =>
            min <= max ? Mathf.Clamp(value, min, max) : (min + max) * 0.5f;

        private RectTransform Area => target != null ? target.parent as RectTransform : null;
    }
}
