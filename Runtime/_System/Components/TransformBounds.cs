using UnityEngine;

namespace FlappyTemplate
{
    // Measures what an object actually draws, in world units, and works out the scale that fits one
    // object inside another's box. RectTransformFit3D does the same against a UI rect; this is the
    // scene-space counterpart, for sizing a sprite against the sprite it hangs under.
    //
    // The part that matters here is exclude: the object being fitted normally sits *under* the object
    // being measured, so without it the parent's bounds would include the child and every fit would
    // feed back into the next measurement.
    public static class TransformBounds
    {
        /// <summary>World bounds of everything drawn under <paramref name="root"/> - meshes, skinned meshes and sprites.</summary>
        /// <param name="layerMask">Layers that count. Excludes transient children - balls in flight, effects - which would otherwise grow the bounds while alive.</param>
        /// <param name="exclude">This transform and everything beneath it is left out of the measurement.</param>
        /// <param name="selfOnly">Measure only renderers on <paramref name="root"/> itself, ignoring its children.</param>
        // Renderers on inactive GameObjects are already skipped, so pooled-away objects never count.
        public static Bounds GetRenderBounds(Transform root, int layerMask = ~0, Transform exclude = null, bool selfOnly = false)
        {
            Bounds bounds = default;
            bool initialized = false;

            // An empty mask measures nothing, which is never a useful setting - it is far more likely
            // to be a LayerMask field left at its serialized default than an intentional choice.
            if (layerMask == 0)
                layerMask = ~0;

            var renderers = selfOnly ? root.GetComponents<Renderer>() : root.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled)
                    continue;
                if (!IsMeasurable(renderer))
                    continue;
                if ((layerMask & (1 << renderer.gameObject.layer)) == 0)
                    continue;
                if (exclude != null && renderer.transform.IsChildOf(exclude))
                    continue;

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return initialized ? bounds : new Bounds(root.position, Vector3.zero);
        }

        /// <summary>Per-axis factor to multiply localScale by so the object fills <paramref name="targetSize"/>. (1,1) when it already fits or cannot be measured.</summary>
        /// <param name="currentSize">The object's current world size, e.g. GetRenderBounds(t).size.</param>
        /// <param name="stretch">Off keeps proportions - both axes get the same factor. On drives width and height independently, so the object matches the box exactly and its aspect is whatever the box is.</param>
        // Relative, not absolute: the factor is measured from where the object stands right now, so
        // re-applying it every frame converges instead of compounding - once it fits, the factor is 1.
        public static Vector2 GetFitScale(Vector2 targetSize, Vector3 currentSize, EFitMode mode, bool stretch = false)
        {
            if (mode == EFitMode.None)
                return Vector2.one;

            if (!stretch)
            {
                float uniform = RectTransformFit3D.GetFitScale(targetSize, currentSize, mode);
                return new Vector2(uniform, uniform);
            }

            // Width and Height stretch the axis they name and leave the other one alone, rather than
            // matching it - a sprite told to fill the width only should keep the height it was drawn at.
            var factor = Vector2.one;
            if (mode != EFitMode.Height)
                factor.x = Axis(targetSize.x, currentSize.x);
            if (mode != EFitMode.Width)
                factor.y = Axis(targetSize.y, currentSize.y);
            return factor;
        }

        // An object with no extent on an axis - an empty, or a sprite whose renderer has not been
        // measured yet - would blow the factor up to infinity, so that axis stays as it is.
        private static float Axis(float target, float current)
        {
            if (current <= Mathf.Epsilon)
                return 1f;

            float factor = target / current;
            return factor > 0f ? factor : 1f;
        }

        /// <summary>The point on a bounds box matching <paramref name="anchor"/> - TopLeft gives the box's top-left, Center its middle.</summary>
        public static Vector3 GetPoint(Bounds bounds, ERectAnchor anchor) => RectTransformFit3D.GetBoundsPoint(bounds, anchor);

        // An allowlist rather than skipping the known-bad ones: particle, trail and line renderers
        // report bounds that lag the transform by a frame - the reason CameraService.CalculateBounds
        // measures the way it does - so they would make the fitted scale wobble. An unfamiliar
        // renderer type is more likely to behave like those than like a mesh, so it stays out.
        private static bool IsMeasurable(Renderer renderer) =>
            renderer is MeshRenderer or SkinnedMeshRenderer or SpriteRenderer;
    }
}
