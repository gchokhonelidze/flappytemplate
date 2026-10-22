using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // A triangle drawn from a generated mesh, with its corners rounded and its edges softened - the one shape
    // a RoundedBox cannot be turned into, and the one a warning sign is. Pointing up, the apex at the top of
    // the rect and the base along the bottom, so it fills whatever rect it is given and is equilateral in a
    // square one.
    //
    // Built the way RoundedBox is rather than fetched from an atlas, and for the same reasons: it costs no
    // import, follows a colour from the inspector without a second sprite being authored, and stays sharp at
    // any size the layout ends up at.
    [AddComponentMenu("UI/Ui Triangle")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class UiTriangle : MaskableGraphic
    {
        private static readonly List<Vector2> Points = new List<Vector2>(96);
        private static readonly List<Vector2> Normals = new List<Vector2>(96);

        [Tooltip("Radius of each corner. Held to the triangle, so a radius too large for it gives a rounded blob rather than a folded shape.")]
        [SerializeField]
        [Min(0f)]
        private float cornerRadius = 4f;

        [Tooltip("Straight pieces per corner arc.")]
        [SerializeField]
        [Range(1, 24)]
        private int cornerSegments = 8;

        [Tooltip("Width of the fade that smooths the edges, in pixels. 0 leaves them stepped.")]
        [SerializeField]
        [Min(0f)]
        private float edgeSoftness = 1.25f;

        public float CornerRadius
        {
            get => cornerRadius;
            set
            {
                value = Mathf.Max(0f, value);
                if (Mathf.Approximately(cornerRadius, value))
                    return;

                cornerRadius = value;
                SetVerticesDirty();
            }
        }

        public int CornerSegments
        {
            get => cornerSegments;
            set
            {
                value = Mathf.Clamp(value, 1, 24);
                if (cornerSegments == value)
                    return;

                cornerSegments = value;
                SetVerticesDirty();
            }
        }

        public float EdgeSoftness
        {
            get => edgeSoftness;
            set
            {
                value = Mathf.Max(0f, value);
                if (Mathf.Approximately(edgeSoftness, value))
                    return;

                edgeSoftness = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            // Apex at the top middle, base corners at the bottom. Listed anticlockwise, which is the winding
            // the outward normals below are worked out for.
            var apex = new Vector2(rect.center.x, rect.yMax);
            var left = new Vector2(rect.xMin, rect.yMin);
            var right = new Vector2(rect.xMax, rect.yMin);

            Points.Clear();
            Normals.Clear();

            // The radius is held so that no corner's tangent points run past the middle of its edges - past
            // that, the arcs cross and the shape folds. The half-angle of each corner is what sets how far a
            // tangent point sits from it for a given radius.
            float radius = cornerRadius;
            radius = Mathf.Min(radius, Reach(apex, left, right));
            radius = Mathf.Min(radius, Reach(left, right, apex));
            radius = Mathf.Min(radius, Reach(right, apex, left));
            radius = Mathf.Max(0f, radius);

            Corner(apex, right, left, radius);
            Corner(left, apex, right, radius);
            Corner(right, left, apex, radius);

            int count = Points.Count;
            if (count < 3)
                return;

            var centre = Vector2.zero;
            for (int i = 0; i < count; i++)
                centre += Points[i];
            centre /= count;

            var fill = color;
            var clear = fill;
            clear.a = 0f;

            // The fan: the middle, then the outline. A second outline a little way out, with no alpha, is what
            // smooths the edge - the same fringe RoundedBox draws.
            vh.AddVert(centre, fill, Vector4.zero);

            for (int i = 0; i < count; i++)
                vh.AddVert(Points[i], fill, Vector4.zero);

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                vh.AddTriangle(0, 1 + i, 1 + next);
            }

            if (edgeSoftness <= 0f)
                return;

            int outer = 1 + count;
            for (int i = 0; i < count; i++)
                vh.AddVert(Points[i] + Normals[i] * edgeSoftness, clear, Vector4.zero);

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                vh.AddTriangle(1 + i, outer + i, outer + next);
                vh.AddTriangle(1 + i, outer + next, 1 + next);
            }
        }

        // The largest radius a corner can take before its tangent points pass the middle of the shorter edge
        // meeting there.
        private static float Reach(Vector2 corner, Vector2 before, Vector2 after)
        {
            var u = before - corner;
            var v = after - corner;
            float half = Vector2.Angle(u, v) * 0.5f * Mathf.Deg2Rad;
            float edge = Mathf.Min(u.magnitude, v.magnitude) * 0.5f;
            return edge * Mathf.Tan(half);
        }

        // One corner of the outline: the arc between the two edges meeting at it, or the point itself when
        // there is no radius. Each point is written with its outward normal, which on an arc is the direction
        // out from the arc's centre and at a sharp point is the bisector.
        private void Corner(Vector2 corner, Vector2 before, Vector2 after, float radius)
        {
            var u = (before - corner).normalized;
            var v = (after - corner).normalized;
            var inward = (u + v).normalized;

            if (radius <= 0f || inward.sqrMagnitude < 0.0001f)
            {
                Points.Add(corner);
                Normals.Add(-inward);
                return;
            }

            float half = Vector2.Angle(u, v) * 0.5f * Mathf.Deg2Rad;
            float toCentre = radius / Mathf.Sin(half);
            var centre = corner + inward * toCentre;

            float along = radius / Mathf.Tan(half);
            var start = corner + u * along;
            var end = corner + v * along;

            float a0 = Mathf.Atan2(start.y - centre.y, start.x - centre.x);
            float a1 = Mathf.Atan2(end.y - centre.y, end.x - centre.x);
            float sweep = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;

            for (int i = 0; i <= cornerSegments; i++)
            {
                float a = a0 + sweep * i / cornerSegments;
                var normal = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Points.Add(centre + normal * radius);
                Normals.Add(normal);
            }
        }
    }
}
