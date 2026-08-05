using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FlappyTemplate
{
    // Sizes a scene object to the box its parent draws, and optionally parks it on a spot of that box.
    // Meant for sprites: drop it on a sprite that has to fill, cap or sit inside another sprite, and it
    // holds that relationship whatever size the parent ends up - a board that scales with the screen, a
    // frame swapped for a wider art asset, a holder scaled by an animation.
    //
    // The rect version of this is RectTransformAnchor3D, which measures a UI rect instead; the two are
    // otherwise the same idea. Nothing here touches the parent - only this object's localScale and,
    // with Align on, its x and y position.
    [ExecuteAlways]
    public class BoundsFit : MonoBehaviour
    {
        [Tooltip("The object whose bounds are fitted to. Empty uses this object's parent.")]
        [SerializeField]
        private Transform source;

        [Tooltip("Renderer measures only what the source draws itself - right for a sprite acting as a frame or background. Hierarchy measures the source and its children, for a parent that is just an empty holding sprites. This object and its children are always left out either way.")]
        [SerializeField]
        private EBoundsScope scope = EBoundsScope.Renderer;

        [Tooltip("Which axes are fitted. Width or Height match that axis of the source; Both keeps this object inside the source on either axis.")]
        [SerializeField]
        private EFitMode fit = EFitMode.Both;

        [Tooltip("Off keeps this object's proportions - it fits inside the source with room to spare on one axis. On drives width and height separately so it matches the source's box exactly, stretching the art.")]
        [SerializeField]
        private bool stretch;

        [Range(0.01f, 1f)]
        [Tooltip("Share of the source to fill. 1 fits it exactly, 0.9 leaves a tenth of it as margin.")]
        [SerializeField]
        private float fill = 1f;

        [Tooltip("Also moves this object onto the source's bounds, matching feature to feature - its top edge to the source's top edge, its centre to the centre - so an off-centre sprite pivot does not push it out. Off leaves the position alone and only the size is driven.")]
        [SerializeField]
        private bool align = true;

        [SerializeField]
        private ERectAnchor anchor = ERectAnchor.Center;

        [Tooltip("Nudge applied after the anchor is resolved, as a share of the source's size - 0.5 is half its width across. Relative on purpose, so the nudge grows with the source instead of drifting off it.")]
        [SerializeField]
        private Vector2 offset;

        [Tooltip("Layers that count towards both measurements. Exclude transient children - balls in flight, effects - or they grow the bounds while alive and shrink the object to compensate.")]
        [SerializeField]
        private LayerMask measureLayers = ~0;

        [Tooltip("Off fits once on enable; on keeps the fit while the source moves, scales or has its sprite swapped.")]
        [SerializeField]
        private bool follow = true;

        [Tooltip("Also fits while editing, so the result is visible without entering play mode.")]
        [SerializeField]
        private bool applyInEditMode = true;

        private Transform Source => source != null ? source : transform.parent;

        private bool CanApply => Source != null && (Application.isPlaying || applyInEditMode);

        void OnEnable()
        {
            if (Source == null)
            {
                // A component added before the source is picked is normal in the editor, so nagging
                // there is just noise; at runtime a root object with nothing to fit to is a setup error.
                if (Application.isPlaying)
                {
                    Debug.LogError($"{nameof(BoundsFit)} on {name} has no source and no parent to fall back on.", this);
                    enabled = false;
                }
                return;
            }

            Apply();
        }

        // LateUpdate so the source has finished moving for the frame - an animation or a parent fit of
        // its own has already run by then, and this reads the settled bounds rather than last frame's.
        void LateUpdate()
        {
            if (follow)
                Apply();
        }

        /// <summary>Measures the source and resizes this object to match.</summary>
        // With Follow off this is the hook to call when the source changes - a sprite swap, a board
        // rebuilt with a different row count. Fitting on demand also dodges the trap of a per-frame
        // fit: children that tween in from zero scale would otherwise be measured mid-animation, on
        // bounds that have not settled.
        [ContextMenu("Apply Now")]
        public void Apply()
        {
            if (!Resolve(out var position, out var scale))
                return;

            // Writing an unchanged value would flag the scene as modified on every editor repaint.
            if (transform.localScale != scale)
                transform.localScale = scale;
            if (align && transform.position != position)
                transform.position = position;
        }

        private bool Resolve(out Vector3 position, out Vector3 scale)
        {
            position = transform.position;
            scale = transform.localScale;

            if (!CanApply)
                return false;

            // Excluding this object is what keeps the measurement stable: it usually hangs under the
            // source, so measuring the source's hierarchy would otherwise include whatever the last
            // fit left behind and chase its own tail.
            var sourceBounds = TransformBounds.GetRenderBounds(Source, measureLayers, transform, scope == EBoundsScope.Renderer);
            if (sourceBounds.size.x <= Mathf.Epsilon && sourceBounds.size.y <= Mathf.Epsilon)
                return false;

            bool needsSelf = fit != EFitMode.None || align;
            var selfBounds = needsSelf ? TransformBounds.GetRenderBounds(transform, measureLayers) : default;

            var factor = Vector2.one;
            if (fit != EFitMode.None)
            {
                var targetSize = (Vector2)sourceBounds.size * fill;
                factor = TransformBounds.GetFitScale(targetSize, selfBounds.size, fit, stretch);

                // A uniform fit takes depth with it so a 3D object keeps its proportions; a stretch is
                // a 2D operation by definition and leaves z alone.
                scale = stretch
                    ? new Vector3(scale.x * factor.x, scale.y * factor.y, scale.z)
                    : scale * factor.x;
            }

            if (align)
            {
                var anchorPoint = TransformBounds.GetPoint(sourceBounds, anchor);
                position.x = anchorPoint.x + sourceBounds.size.x * offset.x;
                position.y = anchorPoint.y + sourceBounds.size.y * offset.y;

                // A pivot that is not in the middle of what is drawn - a sprite pivoted at its base,
                // say - would sit the pivot on the anchor and leave the sprite itself off to one side.
                // The correction is scaled by factor because localScale scales about the pivot, so the
                // pivot-to-bounds offset grows with it: this aligns where the object will be once it
                // reaches the fitted scale, not where it is standing now.
                var selfPoint = TransformBounds.GetPoint(selfBounds, anchor);
                position.x += (transform.position.x - selfPoint.x) * factor.x;
                position.y += (transform.position.y - selfPoint.y) * factor.y;

                // z is left as authored, so fitting never reorders sprites that are sorted by depth.
            }

            return true;
        }

#if UNITY_EDITOR
        // So changing the fit or the anchor in the inspector takes effect right away, even with Follow
        // off. Deferred by a frame because OnValidate runs mid-import and mid-undo, where writing to
        // the transform is not safe; delayCall fires once and clears itself.
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
            if (this == null)
                return;

            Apply();
        }
#endif
    }
}
