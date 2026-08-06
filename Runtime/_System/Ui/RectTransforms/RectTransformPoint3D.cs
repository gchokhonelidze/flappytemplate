using UnityEngine;

namespace FlappyTemplate
{
    // Turns a spot on a UI rect into a position in the 3D scene: pick a corner, edge midpoint or the
    // centre of the rect and get back the world position that lines up with it on a chosen z plane.
    //
    // The route is always rect point -> screen point -> world point rather than reading the rect's
    // world corners directly, because for a Screen Space - Overlay canvas those corners are canvas
    // pixels, not scene units. Going through the screen makes every canvas render mode behave the
    // same, and works for both orthographic and perspective cameras.
    public static class RectTransformPoint3D
    {
        // (0,0) is the bottom-left of the rect, (1,1) the top-right.
        public static Vector2 GetNormalized(ERectAnchor anchor) =>
            anchor switch
            {
                ERectAnchor.TopLeft => new Vector2(0f, 1f),
                ERectAnchor.Top => new Vector2(0.5f, 1f),
                ERectAnchor.TopRight => new Vector2(1f, 1f),
                ERectAnchor.Left => new Vector2(0f, 0.5f),
                ERectAnchor.Right => new Vector2(1f, 0.5f),
                ERectAnchor.BottomLeft => new Vector2(0f, 0f),
                ERectAnchor.Bottom => new Vector2(0.5f, 0f),
                ERectAnchor.BottomRight => new Vector2(1f, 0f),
                _ => new Vector2(0.5f, 0.5f),
            };

        // The point on the rect in the rect's own world space. Only useful directly when the canvas
        // renders in world space; for an overlay canvas the value is in canvas pixels.
        public static Vector3 GetRectPoint(RectTransform rect, ERectAnchor anchor)
        {
            // GetWorldCorners fills bottom-left, top-left, top-right, bottom-right - in that order -
            // and already has the rect's rotation and scale baked in, so lerping between them follows
            // a rotated rect correctly.
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            var t = GetNormalized(anchor);
            var bottom = Vector3.Lerp(corners[0], corners[3], t.x);
            var top = Vector3.Lerp(corners[1], corners[2], t.x);
            return Vector3.Lerp(bottom, top, t.y);
        }

        // camera is only consulted for a World Space canvas that has no Event Camera assigned;
        // it falls back to Camera.main.
        public static Vector2 GetScreenPoint(RectTransform rect, ERectAnchor anchor, Camera camera = null) =>
            RectTransformUtility.WorldToScreenPoint(ResolveUiCamera(rect, camera), GetRectPoint(rect, anchor));

        /// <summary>World position lining up with <paramref name="anchor"/> on the rect, on the z = <paramref name="z"/> plane.</summary>
        /// <param name="z">World-space z the returned point lands on - the depth the object should sit at.</param>
        /// <param name="camera">Camera the point is seen through. Defaults to Camera.main.</param>
        public static Vector3 GetWorldPoint(RectTransform rect, ERectAnchor anchor, float z = 0f, Camera camera = null)
        {
            var cam = camera != null ? camera : Camera.main;
            if (cam == null)
                return GetRectPoint(rect, anchor);

            var screenPoint = GetScreenPoint(rect, anchor, cam);

            // Intersecting the view ray with the z plane covers ortho and perspective in one path -
            // under perspective the x/y drift with depth, and this keeps the point on screen where
            // the rect is regardless of the z asked for.
            var ray = cam.ScreenPointToRay(screenPoint);
            var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, z));
            if (plane.Raycast(ray, out float distance))
            {
                // Solving the intersection tens of units down the ray leaves the result a few float
                // epsilons off the plane (z reads 2e-06 rather than 0). The plane's z is exact by
                // definition, so take it verbatim instead of whatever the division rounded to.
                var point = ray.GetPoint(distance);
                point.z = z;
                return point;
            }

            // Only reached when the camera looks along the plane instead of at it, so there is no
            // intersection to speak of; place the point at the requested depth in front of the camera.
            return cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Mathf.Abs(z - cam.transform.position.z)));
        }

        /// <summary>World position lining up with <paramref name="anchor"/>, at a fixed distance in front of the camera.</summary>
        /// <param name="distance">World units from the camera along its forward axis.</param>
        /// <param name="camera">Camera the point is seen through. Defaults to Camera.main.</param>
        // Unlike GetWorldPoint the result moves with the camera, so it holds its apparent size - which
        // is what you want for anything meant to sit visually on a UI rect. ScreenToWorldPoint reads
        // the z it is given as exactly that distance, under ortho and perspective alike.
        public static Vector3 GetWorldPointAtDistance(RectTransform rect, ERectAnchor anchor, float distance, Camera camera = null)
        {
            var cam = camera != null ? camera : Camera.main;
            if (cam == null)
                return GetRectPoint(rect, anchor);

            var screenPoint = GetScreenPoint(rect, anchor, cam);
            return cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, distance));
        }

        public static Vector3 GetWorldPoint(RectTransform rect, ERectAnchor anchor, EAnchorDepth depthMode, float depth, Camera camera = null) =>
            depthMode == EAnchorDepth.DistanceFromCamera
                ? GetWorldPointAtDistance(rect, anchor, depth, camera)
                : GetWorldPoint(rect, anchor, depth, camera);

        // Overlay canvases are addressed in screen space already, so WorldToScreenPoint wants a null
        // camera there - passing a real one would project the pixel values a second time.
        private static Camera ResolveUiCamera(RectTransform rect, Camera camera)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null)
                return camera != null ? camera : Camera.main;

            canvas = canvas.rootCanvas;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;
            if (canvas.worldCamera != null)
                return canvas.worldCamera;
            return camera != null ? camera : Camera.main;
        }
    }
}
