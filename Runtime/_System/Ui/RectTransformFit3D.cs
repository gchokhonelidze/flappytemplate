using UnityEngine;

namespace FlappyTemplate
{
    // Measures a UI rect in world units and works out the scale that makes a 3D object fit inside it.
    // Companion to RectTransformPoint3D, which places the object; this one sizes it.
    public static class RectTransformFit3D
    {
        // Under perspective the rect covers more world space the further out it is measured, so the
        // depth used here has to be the same one the object is positioned at - hence the shared
        // depthMode/depth pair. Opposite edge midpoints are used rather than corner-to-corner so a
        // pitched camera foreshortens height without bleeding into the width.
        public static Vector2 GetWorldSize(RectTransform rect, EAnchorDepth depthMode, float depth, Camera camera = null)
        {
            var left = RectTransformPoint3D.GetWorldPoint(rect, ERectAnchor.Left, depthMode, depth, camera);
            var right = RectTransformPoint3D.GetWorldPoint(rect, ERectAnchor.Right, depthMode, depth, camera);
            var bottom = RectTransformPoint3D.GetWorldPoint(rect, ERectAnchor.Bottom, depthMode, depth, camera);
            var top = RectTransformPoint3D.GetWorldPoint(rect, ERectAnchor.Top, depthMode, depth, camera);

            return new Vector2(Vector3.Distance(left, right), Vector3.Distance(bottom, top));
        }

        /// <summary>World bounds of everything drawn under <paramref name="root"/> - meshes, skinned meshes and sprites.</summary>
        // layerMask keeps transient children out of the measurement. Anything that comes and goes
        // under the fitted object - balls in flight, effects, spawned pickups - would otherwise grow
        // the bounds while it is alive and shrink the object to compensate. Renderers on inactive
        // GameObjects are already skipped, so pooled-away objects never count.
        public static Bounds GetRenderBounds(Transform root, int layerMask = ~0)
        {
            Bounds bounds = default;
            bool initialized = false;

            // An empty mask measures nothing, which is never a useful setting - it is far more likely
            // to be a LayerMask field left at its serialized default than an intentional choice.
            if (layerMask == 0)
                layerMask = ~0;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled)
                    continue;
                if (!IsMeasurable(renderer))
                    continue;
                if ((layerMask & (1 << renderer.gameObject.layer)) == 0)
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

        // An allowlist rather than skipping the known-bad ones: particle, trail and line renderers
        // report bounds that lag the transform by a frame - the reason CameraService.CalculateBounds
        // measures the way it does - so they would make the fitted scale wobble. An unfamiliar
        // renderer type is more likely to behave like those than like a mesh, so it stays out.
        private static bool IsMeasurable(Renderer renderer) =>
            renderer is MeshRenderer or SkinnedMeshRenderer or SpriteRenderer;

        /// <summary>The point on a bounds box matching <paramref name="anchor"/> - TopLeft gives the box's top-left, Center its middle.</summary>
        // Uses the same normalized mapping as the rect side, so aligning one to the other lines up the
        // matching features: an object's top edge to the rect's top edge, its centre to the centre.
        // Only x and y are meaningful; z stays at the box centre, since depth is settled separately by
        // the depth mode.
        public static Vector3 GetBoundsPoint(Bounds bounds, ERectAnchor anchor)
        {
            var t = RectTransformPoint3D.GetNormalized(anchor);
            return new Vector3(
                Mathf.Lerp(bounds.min.x, bounds.max.x, t.x),
                Mathf.Lerp(bounds.min.y, bounds.max.y, t.y),
                bounds.center.z
            );
        }

        /// <summary>Uniform factor to multiply the object's localScale by so it fits the given size. 1 when it already fits or cannot be measured.</summary>
        /// <param name="targetSize">World width and height to fit into.</param>
        /// <param name="currentSize">The object's current world size, e.g. GetRenderBounds(t).size.</param>
        public static float GetFitScale(Vector2 targetSize, Vector3 currentSize, EFitMode mode)
        {
            // A flat object measures zero on one axis - a quad lying in the board plane has no world
            // height - so an axis without extent is skipped rather than blowing the scale up to
            // infinity. Both then quietly degrades to whichever axis can be measured.
            bool hasWidth = currentSize.x > Mathf.Epsilon;
            bool hasHeight = currentSize.y > Mathf.Epsilon;

            float byWidth = hasWidth ? targetSize.x / currentSize.x : float.PositiveInfinity;
            float byHeight = hasHeight ? targetSize.y / currentSize.y : float.PositiveInfinity;

            float scale = mode switch
            {
                EFitMode.Width => byWidth,
                EFitMode.Height => byHeight,
                EFitMode.Both => Mathf.Min(byWidth, byHeight),
                _ => 1f,
            };

            return float.IsInfinity(scale) || scale <= 0f ? 1f : scale;
        }

        /// <summary>Scales the object so its rendered bounds fit the rect. Returns false when there was nothing to measure.</summary>
        /// <param name="fill">Share of the rect to occupy - 1 fits it exactly, 0.9 leaves a tenth as margin.</param>
        /// <param name="layerMask">Layers that count towards the object's size. Leave at every layer unless transient children need excluding.</param>
        public static bool Fit(
            Transform target,
            RectTransform rect,
            EFitMode mode,
            EAnchorDepth depthMode = EAnchorDepth.WorldZ,
            float depth = 0f,
            Camera camera = null,
            float fill = 1f,
            int layerMask = ~0
        )
        {
            if (mode == EFitMode.None || target == null || rect == null)
                return false;

            var currentSize = GetRenderBounds(target, layerMask).size;
            if (currentSize.x <= Mathf.Epsilon && currentSize.y <= Mathf.Epsilon)
                return false;

            var targetSize = GetWorldSize(rect, depthMode, depth, camera) * fill;
            float scale = GetFitScale(targetSize, currentSize, mode);

            // Relative, not absolute: the factor is measured from where the object stands right now, so
            // re-applying it every frame converges instead of compounding - once it fits, scale is 1.
            if (!Mathf.Approximately(scale, 1f))
                target.localScale *= scale;

            return true;
        }
    }
}
