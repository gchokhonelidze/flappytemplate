using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FlappyTemplate
{
    // Inspector wrapper around RectTransformPoint3D and RectTransformFit3D: parks a Transform on a spot
    // of a UI rect and optionally scales it to fit.
    [ExecuteAlways]
    public class RectTransformAnchor3D : MonoBehaviour
    {
        [SerializeField]
        private RectTransform rect;

        [SerializeField]
        private ERectAnchor anchor = ERectAnchor.Center;

        [Tooltip("World Z pins the target to an absolute z plane, so a camera moving in z changes how big it looks. Distance From Camera keeps it a fixed distance ahead, holding its apparent size.")]
        [SerializeField]
        private EAnchorDepth depthMode = EAnchorDepth.WorldZ;

        [Tooltip("Read as a world z plane, or as world units in front of the camera - see Depth Mode.")]
        [SerializeField]
        private float z;

        [Tooltip("Added after the anchor point is resolved, in world space.")]
        [SerializeField]
        private Vector3 offset;

        [Tooltip("Camera the rect is seen through. Empty falls back to Camera.main.")]
        [SerializeField]
        private Camera cam;

        [Tooltip("Moved to the anchor point. Empty moves this GameObject.")]
        [SerializeField]
        private Transform target;

        [Tooltip("On aligns what the object actually looks like - its rendered bounds - with the anchor, so an off-centre pivot does not push it out of the rect. Off aligns the transform pivot itself. Only corrects x and y; depth stays with the pivot.")]
        [SerializeField]
        private bool alignBounds = true;

        [Tooltip("Scales the target to fit the rect. Width or Height match that axis; Both keeps it inside the rect on either axis. Always uniform - proportions are kept.")]
        [SerializeField]
        private EFitMode fit = EFitMode.None;

        [Range(0.01f, 1f)]
        [Tooltip("Share of the rect to fill. 1 fits it exactly, 0.9 leaves a tenth of it as margin.")]
        [SerializeField]
        private float fill = 1f;

        [Tooltip("Layers that count towards the fitted size. Exclude transient children - balls in flight, effects - or they grow the bounds while alive and shrink the object to compensate.")]
        [SerializeField]
        private LayerMask measureLayers = ~0;

        [Tooltip("Off places the target once on enable; on keeps it there while the rect moves, the canvas rescales or the screen rotates.")]
        [SerializeField]
        private bool follow = true;

        [Tooltip("Roughly the seconds the target takes to catch up to a new position and size, easing as it goes. 0 moves instantly. Play mode only - edit mode always snaps.")]
        [SerializeField]
        private float smoothTime;

        [Tooltip("Also positions the target while editing, so the placement is visible without entering play mode. The rect is measured against the Game view resolution, not the Scene view.")]
        [SerializeField]
        private bool applyInEditMode = true;

        private Vector3 targetPosition;
        private Vector3 targetScale;
        private Vector3 positionVelocity;
        private Vector3 scaleVelocity;

        // Set by Apply while a smoothed move is still running, so a non-following component keeps
        // easing across frames without re-measuring the rect.
        private bool easing;

        private Transform Target => target != null ? target : transform;

        private bool CanApply => rect != null && (Application.isPlaying || applyInEditMode);

        // Edit mode has no reliable frame delta and its ticks only happen on repaint, so easing there
        // would stall half way. Nothing to smooth when the duration is zero either.
        private bool Snapping => smoothTime <= 0f || !Application.isPlaying;

        void OnEnable()
        {
            if (rect == null)
            {
                // The component is normally added before the rect is picked, so nagging about it in
                // the editor is just noise; at runtime an empty rect is a real setup error.
                if (Application.isPlaying)
                {
                    Debug.LogError($"{nameof(RectTransformAnchor3D)} on {name} has no rect assigned.", this);
                    enabled = false;
                }
                return;
            }

            // A canvas enabled this same frame has not been laid out yet, so the rect would still be
            // sitting at its authored size and the first placement would be wrong. Snap rather than
            // ease, so the target does not fly in from wherever it was left.
            Canvas.ForceUpdateCanvases();
            Snap();
        }

        // LateUpdate, not Update: by then this frame's layout pass has run and the rect is final. With
        // [ExecuteAlways] this also ticks in edit mode, though only as often as the editor repaints -
        // which is why inspector edits get their own OnValidate path rather than waiting for a tick.
        void LateUpdate()
        {
            if (follow)
            {
                if (Resolve(out var position, out var scale))
                    Write(position, scale, Snapping);
                return;
            }

            if (!easing)
                return;

            Write(targetPosition, targetScale, Snapping);
            if (Arrived())
            {
                // Land exactly rather than creeping at the asymptote.
                Write(targetPosition, targetScale, snap: true);
                easing = false;
            }
        }

        /// <summary>Works out where the target belongs and moves it there, easing over Smooth Time.</summary>
        // With Follow off this is the hook to call when the thing being fitted has changed - a plinko
        // row count, say. Measuring on demand also dodges the trap of a per-frame fit: sticks tween in
        // from zero scale, so a fit that samples mid-animation reads bounds that have not settled.
        [ContextMenu("Apply Now")]
        public void Apply()
        {
            if (!Resolve(out var position, out var scale))
                return;

            if (Snapping)
            {
                Write(position, scale, snap: true);
                easing = false;
                return;
            }

            easing = true;
        }

        /// <summary>Same as Apply, but arrives immediately regardless of Smooth Time.</summary>
        [ContextMenu("Snap Now")]
        public void Snap()
        {
            if (!Resolve(out var position, out var scale))
                return;

            Write(position, scale, snap: true);
            easing = false;
        }

        // Caches what it resolves as the smoothing target, so Apply can hand the easing over to
        // LateUpdate without it having to measure the rect again.
        private bool Resolve(out Vector3 position, out Vector3 scale)
        {
            position = Target.position;
            scale = Target.localScale;

            if (!CanApply)
                return false;

            bool needsBounds = fit != EFitMode.None || alignBounds;
            var bounds = needsBounds ? RectTransformFit3D.GetRenderBounds(Target, measureLayers) : default;

            float factor = 1f;
            if (fit != EFitMode.None)
            {
                var rectSize = RectTransformFit3D.GetWorldSize(rect, depthMode, z, cam) * fill;
                factor = RectTransformFit3D.GetFitScale(rectSize, bounds.size, fit);
                scale = Target.localScale * factor;
            }

            position = RectTransformPoint3D.GetWorldPoint(rect, anchor, depthMode, z, cam) + offset;

            if (alignBounds)
            {
                // A pivot that is not in the middle of the object - the plinko board hangs its rows
                // below an apex at y 0 - would otherwise sit the pivot on the anchor and leave the
                // object itself off to one side. The correction is scaled by factor because localScale
                // scales children about the pivot, so the pivot-to-bounds offset grows with it: this
                // aligns where the object will be once it reaches the target scale, not where it is.
                var boundsPoint = RectTransformFit3D.GetBoundsPoint(bounds, anchor);
                position.x += (Target.position.x - boundsPoint.x) * factor;
                position.y += (Target.position.y - boundsPoint.y) * factor;
            }

            targetPosition = position;
            targetScale = scale;
            return true;
        }

        private void Write(Vector3 position, Vector3 scale, bool snap)
        {
            if (snap)
            {
                positionVelocity = Vector3.zero;
                scaleVelocity = Vector3.zero;

                // Writing an unchanged value would flag the scene as modified on every editor repaint.
                if (Target.position != position)
                    Target.position = position;
                if (Target.localScale != scale)
                    Target.localScale = scale;
                return;
            }

            Target.position = Vector3.SmoothDamp(Target.position, position, ref positionVelocity, smoothTime);
            Target.localScale = Vector3.SmoothDamp(Target.localScale, scale, ref scaleVelocity, smoothTime);
        }

        private bool Arrived() =>
            (Target.position - targetPosition).sqrMagnitude < 1e-8f && (Target.localScale - targetScale).sqrMagnitude < 1e-8f;

#if UNITY_EDITOR
        // So changing anchor, depth or z in the inspector moves the target right away, even with
        // Follow off. Applying inside OnValidate itself is not allowed - it drives Canvas layout,
        // which Unity refuses mid-validate - so it is deferred by a frame. delayCall fires once and
        // clears itself.
        void OnValidate()
        {
            smoothTime = Mathf.Max(0f, smoothTime);

            if (Application.isPlaying || !applyInEditMode)
                return;

            EditorApplication.delayCall += SnapDeferred;
        }

        private void SnapDeferred()
        {
            // The frame in between is enough for an undo, a delete or a scene close to take the
            // component with it.
            if (this == null)
                return;

            Canvas.ForceUpdateCanvases();
            Snap();
        }
#endif
    }
}
