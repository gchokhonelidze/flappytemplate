using UnityEngine;

namespace FlappyTemplate
{
    // Repeats a RoundedBox's border on top of everything inside it.
    //
    // It exists because of the order a canvas draws in: a graphic goes down before its own children, so a
    // picture parented to a box - which is how the box clips it to the rounded corners - lands on top of
    // the border the box just drew, and the frame disappears behind the content it was framing. A border
    // above the content has to come from a renderer that draws later, and the only thing that draws later
    // than a child is another child. So this is one: the last one, holding a second RoundedBox that draws
    // the border and nothing else.
    //
    // Nothing on it is authored. Every value is copied from the source box, so the border stays one set of
    // fields on the parent and this follows whatever they become - including at edit time, where the point
    // is to see the result without entering play mode.
    [ExecuteAlways]
    [RequireComponent(typeof(RoundedBox))]
    [AddComponentMenu("UI/Rounded Box Border Overlay")]
    public class RoundedBoxBorderOverlay : MonoBehaviour
    {
        [Tooltip("The box whose border is repeated. Empty uses the parent, which is how this is normally set up - the overlay is a child of the box it draws for.")]
        [SerializeField]
        private RoundedBox source;

        [Tooltip("Keeps this the last child, so it draws over every sibling however they are reordered. Turn it off to place it by hand - anything below it in the hierarchy will draw over the border.")]
        [SerializeField]
        private bool keepOnTop = true;

        private RoundedBox self;

        /// <summary>The box being copied - the assigned source, or the parent when there is none.</summary>
        public RoundedBox Source
        {
            get
            {
                if (source != null)
                    return source;

                return transform.parent != null ? transform.parent.GetComponent<RoundedBox>() : null;
            }
        }

        private RoundedBox Self
        {
            get
            {
                if (self == null)
                    self = GetComponent<RoundedBox>();

                return self;
            }
        }

        void OnEnable()
        {
            if (keepOnTop)
                transform.SetAsLastSibling();

            Sync();
        }

        // Late so the source has settled for the frame: a border driven by a tween or a state change has
        // already been written by the time this reads it, rather than being picked up a frame behind.
        void LateUpdate()
        {
            Sync();
        }

        /// <summary>Copies the source box's border and corners onto this one.</summary>
        [ContextMenu("Sync Now")]
        public void Sync()
        {
            var from = Source;
            var to = Self;

            // A source pointing at itself would be a box copying its own border onto itself; harmless, but
            // it means the overlay is misconfigured and drawing nothing useful.
            if (from == null || to == null || from == to)
                return;

            to.RadiusTopLeft = from.RadiusTopLeft;
            to.RadiusTopRight = from.RadiusTopRight;
            to.RadiusBottomRight = from.RadiusBottomRight;
            to.RadiusBottomLeft = from.RadiusBottomLeft;

            to.BorderLeft = from.BorderLeft;
            to.BorderTop = from.BorderTop;
            to.BorderRight = from.BorderRight;
            to.BorderBottom = from.BorderBottom;

            to.BorderColorLeft = from.BorderColorLeft;
            to.BorderColorTop = from.BorderColorTop;
            to.BorderColorRight = from.BorderColorRight;
            to.BorderColorBottom = from.BorderColorBottom;

            to.BorderGradientMode = from.BorderGradientMode;
            to.BorderGradientAngle = from.BorderGradientAngle;
            SyncGradient(from, to);

            to.CornerSegments = from.CornerSegments;
            to.EdgeSoftness = from.EdgeSoftness;

            // The fill belongs to the box underneath - this draws the frame and leaves the middle alone.
            to.FillColor = Color.clear;

            // Clicks belong to the box and whatever is inside it, not to a frame drawn over the top.
            if (to.raycastTarget)
                to.raycastTarget = false;

            if (from.transform == transform.parent)
                FitToParent();
        }

        // The one value here that is a reference rather than a number, which makes copying it a question
        // rather than an assignment.
        private static void SyncGradient(RoundedBox from, RoundedBox to)
        {
            var source = from.BorderGradient;
            if (source == null)
                return;

            // At runtime the two share the object outright. Nothing is editing keys behind anyone's back,
            // and pointing at the same ramp costs nothing to check on the frames after the first.
            if (Application.isPlaying)
            {
                if (!ReferenceEquals(to.BorderGradient, source))
                    to.BorderGradient = source;

                return;
            }

            // In the editor the keys are being dragged about in place, and a shared reference would leave
            // this mesh holding a ramp it has no way of knowing has changed - which is the whole failure
            // this is here to avoid. Compared key by key instead: a handful of them, once a repaint,
            // against a border that has to follow what the inspector is doing.
            if (Matches(source, to.BorderGradient))
                return;

            var copy = new Gradient();
            copy.SetKeys(source.colorKeys, source.alphaKeys);
            copy.mode = source.mode;
            to.BorderGradient = copy;
        }

        private static bool Matches(Gradient left, Gradient right)
        {
            if (right == null || left.mode != right.mode)
                return false;

            var leftColors = left.colorKeys;
            var rightColors = right.colorKeys;
            if (leftColors.Length != rightColors.Length)
                return false;

            for (int i = 0; i < leftColors.Length; i++)
            {
                if (leftColors[i].color != rightColors[i].color || !Mathf.Approximately(leftColors[i].time, rightColors[i].time))
                    return false;
            }

            var leftAlphas = left.alphaKeys;
            var rightAlphas = right.alphaKeys;
            if (leftAlphas.Length != rightAlphas.Length)
                return false;

            for (int i = 0; i < leftAlphas.Length; i++)
            {
                if (!Mathf.Approximately(leftAlphas[i].alpha, rightAlphas[i].alpha) || !Mathf.Approximately(leftAlphas[i].time, rightAlphas[i].time))
                    return false;
            }

            return true;
        }

        // Only meaningful when the source is the parent; an overlay pointed at a box somewhere else is
        // placed by hand, and moving its rect from here would fight whoever put it there.
        private void FitToParent()
        {
            var rect = (RectTransform)transform;

            // Compared before writing: a RectTransform notifies its children and its layout every time one
            // of these is set, even when the value it is set to is the one already there.
            if (rect.anchorMin != Vector2.zero)
                rect.anchorMin = Vector2.zero;
            if (rect.anchorMax != Vector2.one)
                rect.anchorMax = Vector2.one;
            if (rect.offsetMin != Vector2.zero)
                rect.offsetMin = Vector2.zero;
            if (rect.offsetMax != Vector2.zero)
                rect.offsetMax = Vector2.zero;
            if (rect.localScale != Vector3.one)
                rect.localScale = Vector3.one;
            if (rect.localRotation != Quaternion.identity)
                rect.localRotation = Quaternion.identity;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Deferred: OnValidate runs mid-import and mid-undo, where touching sibling order or another
            // component's serialised state is not safe.
            UnityEditor.EditorApplication.delayCall += ValidateDeferred;
        }

        private void ValidateDeferred()
        {
            if (this == null)
                return;

            if (keepOnTop && isActiveAndEnabled)
                transform.SetAsLastSibling();

            Sync();
        }
#endif
    }
}
