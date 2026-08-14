using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    // The two edges of a border, worked out from the outline it runs along.
    //
    // Offsetting a closed path is not the same problem as moving each of its points outwards. A corner
    // needs a different answer depending on which way it turns, and a shape can be too narrow in places to
    // hold the border it is being asked for. Moving the points regardless folds the border over itself - a
    // spike shooting out of a sharp corner, a patch of double-strength colour in a notch - and a fold is
    // the one flaw in a border that cannot be argued away, because it is the border painted twice.
    //
    // Three things happen here, in this order, and each is answering a different way of folding:
    //
    // Corners. A corner that turns away from the border is rounded once a mitre would reach further than it
    // is worth, since a mitre on something sharp is the spike. On the side that turns towards the border
    // the mitre is kept whatever the angle - there it is not a corner treatment at all, it is exactly where
    // the two offset edges cross, and rounding it would cut the corner off.
    //
    // Reach. No point of an edge is allowed to end up nearer to some other part of the outline than the
    // border is wide, because that is what having gone past the middle of a narrow place means. Points that
    // have are pulled back along the line they were pushed out on, which lands the two sides of a slot or a
    // mouth against each other rather than through each other.
    //
    // Folds. What is left is edge that genuinely crosses itself, and there everything between the two
    // crossing segments is collapsed onto the crossing point - the fold's triangles go to nothing, and what
    // it was covering twice is covered once by a fan from that point. Which side of a crossing is the fold
    // is decided by winding rather than by shape, since a fold and a hole look the same from close up.
    //
    // The reach rule is why a border comes out thinner inside a sharp notch, and there is no version of
    // this that does not. A notch narrower than twice the border has nowhere to put the whole of it: the
    // two sides meet in the middle and the border tapers to the point. The only other answer is to let them
    // run through each other, which is the border painted twice.
    //
    // What none of this does is a true polygon offset, which would want the whole thing rebuilt as an area
    // and unioned with itself. Two convex corners facing each other across a gap narrower than the border -
    // the tips of a nearly closed crescent - still overlap a little. Everything ordinary comes out clean.
    //
    // Both edges are built together and stay paired index for index, so the caller still has a strip.
    internal static class SpriteBorderPath
    {
        // How far a mitre may reach, as a multiple of the border's own width, before the corner is rounded
        // instead. Two lets a right angle stay square - a mitre reaches root two there - and rounds off
        // anything sharper than about sixty degrees, which is where a mitre starts to read as a spike.
        private const float MitreLimit = 2f;

        private const float JoinStep = 22f;
        private const int MaxJoinPieces = 8;

        // A fold takes one pass to find and one to fix, and fixing one can expose another. This is a
        // backstop against a pathological outline rather than a limit anything real runs into.
        private const int MaxFolds = 32;

        // A piece of edge smaller than this share of the loop is taken as a fold without further argument.
        private const float TinyFold = 0.03f;

        private static readonly List<Vector2> Normals = new List<Vector2>();

        // Where each point of the border came from on the outline, and which stretch of it that was. An
        // edge that has to give way gives way along the line it travelled, so it needs to remember both.
        private static readonly List<Vector2> Anchors = new List<Vector2>();
        private static readonly List<int> Sources = new List<int>();

        /// <summary>
        /// Lays a border along one closed loop, appending its outer edge, its inner edge and the direction
        /// each point faces to the three lists. Returns how many points were added to each.
        /// </summary>
        public static int Build(List<Vector2> loop, float outward, float inward,
            List<Vector2> outer, List<Vector2> inner, List<Vector2> normals)
        {
            int count = loop.Count;
            if (count < 3)
                return 0;

            int first = outer.Count;
            Anchors.Clear();
            Sources.Clear();

            // The direction the border leaves each segment at: to the right of the way that segment is
            // walked. A loop wound with the shape on its left therefore faces away from the shape, and a
            // hole - wound the other way - faces into the hole, without either being a special case.
            Normals.Clear();
            for (int i = 0; i < count; i++)
            {
                var edge = loop[(i + 1) % count] - loop[i];
                Normals.Add(edge.sqrMagnitude < 1e-10f ? Vector2.zero : new Vector2(edge.y, -edge.x).normalized);
            }

            for (int i = 0; i < count; i++)
            {
                var before = NearestNormal(i - 1, -1, count);
                var after = NearestNormal(i, 1, count);
                if (before == Vector2.zero || after == Vector2.zero)
                    continue;

                Join(i, loop[i], before, after, outward, inward, outer, inner, normals);
            }

            int added = outer.Count - first;
            if (added < 3)
                return added;

            Clamp(loop, outer, first, added, outward);
            Unfold(outer, first, added);

            // Only worth doing when there is an inner edge to fold. An outside border's inner edge is the
            // outline itself, give or take the sliver tucked under the picture.
            if (inward > 0f)
            {
                Clamp(loop, inner, first, added, inward);
                Unfold(inner, first, added);
            }

            return added;
        }

        // One corner. Mitred where a mitre is right, and swept round where it is not - and either way the
        // inner edge gets a point for every point the outer edge gets, so the strip between them stays a
        // strip.
        private static void Join(int source, Vector2 point, Vector2 before, Vector2 after, float outward, float inward,
            List<Vector2> outer, List<Vector2> inner, List<Vector2> normals)
        {
            // Positive turns away from the border, negative turns into it. The two normals rotate with the
            // segments they belong to, so the sign of their cross product is the sign of the turn.
            float turn = before.x * after.y - before.y * after.x;

            var mitre = before + after;
            bool mitred = mitre.sqrMagnitude > 1e-10f;
            float grip = 1f;

            if (mitred)
            {
                mitre.Normalize();
                grip = 1f / Mathf.Max(0.05f, Vector2.Dot(mitre, after));

                // A corner turning into the border is not rounded however sharp it is. The mitre there is
                // where the two offset edges actually cross, so it is the answer rather than a stand-in for
                // one, and rounding it would cut the corner off.
                if (turn <= 0f || grip <= MitreLimit)
                {
                    Add(source, point, mitre, grip, outward, mitre, grip, inward, outer, inner, normals);
                    return;
                }
            }

            // Swept from one segment's facing to the next, at the border's own width - which is the shape a
            // disc rolled around the corner would leave, and the offset a corner is supposed to have.
            //
            // Only on the side of the corner that opens up, though. The other side is closing, and a sweep
            // there would rake the edge backwards round the point - which on something sharp, a star's tip
            // or the end of a crescent, throws it clear of the shape and lands the border on the far side.
            // That side wants the mitre, which is where its two edges meet whatever the angle.
            float from = Mathf.Atan2(before.y, before.x) * Mathf.Rad2Deg;
            float sweep = Vector2.Angle(before, after) * (turn < 0f ? -1f : 1f);
            int pieces = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(sweep) / JoinStep), 1, MaxJoinPieces);

            for (int i = 0; i <= pieces; i++)
            {
                float radians = (from + sweep * i / pieces) * Mathf.Deg2Rad;
                var swept = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

                if (mitred)
                    Add(source, point, swept, 1f, outward, mitre, grip, inward, outer, inner, normals);
                else
                    Add(source, point, swept, 1f, outward, swept, 1f, inward, outer, inner, normals);
            }
        }

        private static void Add(int source, Vector2 point,
            Vector2 outwards, float outerReach, float outward,
            Vector2 inwards, float innerReach, float inward,
            List<Vector2> outer, List<Vector2> inner, List<Vector2> normals)
        {
            outer.Add(point + outwards * (outerReach * outward));
            inner.Add(point - inwards * (innerReach * inward));
            normals.Add(outwards);
            Anchors.Add(point);
            Sources.Add(source);
        }

        private static Vector2 NearestNormal(int index, int step, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int at = ((index + step * i) % count + count) % count;
                if (Normals[at] != Vector2.zero)
                    return Normals[at];
            }

            return Vector2.zero;
        }

        // Nothing may reach past the middle of a narrow place.
        //
        // A border is every point within its own width of the outline, and no more. So a point of its edge
        // that some *other* stretch of the outline is nearer to than the border is wide has gone past the
        // middle of a narrow place and out the far side - and whatever it is covering there, the border
        // coming the other way is covering too. Neither of them has to cross anything for that to happen,
        // which is why nothing simpler than this question catches it.
        //
        // Asked of every point, and answered by pulling the point back along the line it was pushed out on
        // until it is true. Two sides of a slot, a mouth or the gap between two letters then come to rest
        // against each other, exactly meeting and nowhere overlapping.
        private static void Clamp(List<Vector2> loop, List<Vector2> points, int first, int count, float width)
        {
            if (width <= 0f)
                return;

            for (int i = 0; i < count; i++)
            {
                var anchor = Anchors[i];
                var offset = points[first + i] - anchor;
                if (offset.sqrMagnitude < 1e-10f)
                    continue;

                int source = Sources[i];
                if (Clear(loop, anchor + offset, width, source))
                    continue;

                // Halved rather than solved. The distance from a point to a whole outline is not something
                // with a formula behind it, and a dozen halvings settle this to a thousandth of the width.
                float low = 0f;
                float high = 1f;
                for (int step = 0; step < 12; step++)
                {
                    float mid = (low + high) * 0.5f;
                    if (Clear(loop, anchor + offset * mid, width * mid, source))
                        low = mid;
                    else
                        high = mid;
                }

                points[first + i] = anchor + offset * low;
            }
        }

        private static bool Clear(List<Vector2> loop, Vector2 point, float width, int source)
        {
            int count = loop.Count;

            // A hair under, since the segments this point was offset from sit at exactly the width and are
            // not what is being looked for.
            float limit = width * width * 0.95f;

            for (int i = 0; i < count; i++)
            {
                // The stretch of outline this point belongs to is skipped outright. It is the right distance
                // away by construction, and on a curve its neighbours are a rounding error either side of
                // that - close enough to answer the question wrongly on a shape with nothing wrong with it.
                int step = Mathf.Abs(i - source);
                if (Mathf.Min(step, count - step) <= 1)
                    continue;

                if (SqrDistance(point, loop[i], loop[(i + 1) % count]) < limit)
                    return false;
            }

            return true;
        }

        private static float SqrDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            var span = b - a;
            float length = span.sqrMagnitude;
            if (length < 1e-12f)
                return (point - a).sqrMagnitude;

            float along = Mathf.Clamp01(Vector2.Dot(point - a, span) / length);
            return (point - (a + span * along)).sqrMagnitude;
        }

        // Takes the folds out of one edge, in place and without changing how many points it has - which is
        // what lets the other edge stay paired with it. Where two of its segments cross, everything between
        // them is moved onto the crossing point: the fold's own triangles collapse to nothing, and the
        // ground it was covering twice is covered once by a fan from that point.
        private static void Unfold(List<Vector2> points, int first, int count)
        {
            float loop = Area(points, first, count);
            if (Mathf.Abs(loop) < 1e-12f)
                return;

            for (int fold = 0; fold < MaxFolds; fold++)
            {
                if (!Collapse(points, first, count, loop))
                    return;
            }
        }

        // Finds a crossing worth acting on and flattens the fold behind it. Every crossing is looked at,
        // not just the first, because one that has to be let alone must not stop the rest being found.
        private static bool Collapse(List<Vector2> points, int first, int count, float loop)
        {
            for (int i = 0; i < count; i++)
            {
                var a = points[first + i];
                var b = points[first + (i + 1) % count];
                if ((b - a).sqrMagnitude < 1e-12f)
                    continue;

                for (int j = i + 2; j < count; j++)
                {
                    // The last segment runs back into the first, so those two touch and are not a crossing.
                    if (i == 0 && j == count - 1)
                        continue;

                    var c = points[first + j];
                    var d = points[first + (j + 1) % count];
                    if ((d - c).sqrMagnitude < 1e-12f)
                        continue;

                    if (!Crosses(a, b, c, d, out var at))
                        continue;

                    // Which of the two pieces the crossing leaves is the fold - if either of them is.
                    if (Folded(points, first, count, i, j, at, loop))
                    {
                        for (int k = i + 1; k <= j; k++)
                            points[first + k] = at;

                        return true;
                    }

                    if (Folded(points, first, count, j, count + i, at, loop))
                    {
                        for (int k = j + 1; k <= count + i; k++)
                            points[first + k % count] = at;

                        return true;
                    }
                }
            }

            return false;
        }

        // Whether the piece of edge between two indices, closed off through the crossing point, is a fold.
        //
        // A fold and a hole look identical from close up: a run of edge that comes back on itself and
        // encloses something. What tells them apart is what the rest of the edge is doing over the same
        // ground. A fold lies on top of border that is already there - the edge wraps that ground twice -
        // while a hole is ground the border never covers at all, which is exactly what an offset thick
        // enough to close a gap in the shape leaves behind. A C whose mouth the border bridges becomes a
        // ring, and a ring's inside is a hole, not a mistake.
        //
        // So the question is put to the enclosed ground itself: how many times does the whole edge wind
        // around it? Twice or more and it is a fold. Once or none and it is the shape, and is left alone.
        private static bool Folded(List<Vector2> points, int first, int count, int from, int to, Vector2 at, float loop)
        {
            if (to - from < 2)
                return false;

            // Area-weighted centre of the piece, which for the blob a fold makes is inside it. Should a
            // wilder shape put it outside, the winding comes back low and the piece is left alone - the
            // safe way round to be wrong.
            float twice = 0f;
            var centre = Vector2.zero;
            var previous = at;

            for (int i = from + 1; i <= to + 1; i++)
            {
                var point = i > to ? at : points[first + i % count];
                float cross = previous.x * point.y - point.x * previous.y;

                twice += cross;
                centre += (previous + point) * cross;
                previous = point;
            }

            if (Mathf.Abs(twice) < 1e-9f)
                return false;

            // A sliver is taken as a fold without asking. The winding is the honest test but it needs a
            // point safely inside the piece to ask about, and on something long and thin the centre is as
            // likely to fall outside it - where the answer comes back low and a real fold is kept. Nothing
            // this small is worth the shape it would leave behind either way.
            if (Mathf.Abs(twice * 0.5f) < Mathf.Abs(loop) * TinyFold)
                return true;

            return Mathf.Abs(Winding(points, first, count, centre / (3f * twice))) >= 2;
        }

        private static int Winding(List<Vector2> points, int first, int count, Vector2 at)
        {
            int winding = 0;

            for (int i = 0; i < count; i++)
            {
                var a = points[first + i];
                var b = points[first + (i + 1) % count];

                if (a.y <= at.y)
                {
                    if (b.y > at.y && Side(a, b, at) > 0f)
                        winding++;
                }
                else if (b.y <= at.y && Side(a, b, at) < 0f)
                {
                    winding--;
                }
            }

            return winding;
        }

        private static float Side(Vector2 a, Vector2 b, Vector2 at)
            => (b.x - a.x) * (at.y - a.y) - (at.x - a.x) * (b.y - a.y);

        // Where two segments cross, if they cross anywhere other than at their own ends. Ends are left out
        // on purpose: neighbouring segments share one, and a border made of them would otherwise report a
        // fold at every corner it has.
        private static bool Crosses(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 at)
        {
            at = Vector2.zero;

            var ab = b - a;
            var cd = d - c;

            float denominator = ab.x * cd.y - ab.y * cd.x;
            if (Mathf.Abs(denominator) < 1e-12f)
                return false;

            var ac = c - a;
            float alongAb = (ac.x * cd.y - ac.y * cd.x) / denominator;
            float alongCd = (ac.x * ab.y - ac.y * ab.x) / denominator;

            const float Edge = 1e-4f;
            if (alongAb <= Edge || alongAb >= 1f - Edge || alongCd <= Edge || alongCd >= 1f - Edge)
                return false;

            at = a + ab * alongAb;
            return true;
        }

        private static float Area(List<Vector2> points, int first, int count)
        {
            float sum = 0f;
            for (int i = 0; i < count; i++)
            {
                var a = points[first + i];
                var b = points[first + (i + 1) % count];
                sum += a.x * b.y - b.x * a.y;
            }

            return sum * 0.5f;
        }

        // The area of the piece running from one index to another, closed off through the crossing point.
        private static float Area(List<Vector2> points, int first, int count, int from, int to, Vector2 at)
        {
            float sum = 0f;
            var previous = at;

            for (int i = from; i <= to; i++)
            {
                var point = points[first + i % count];
                sum += previous.x * point.y - point.x * previous.y;
                previous = point;
            }

            sum += previous.x * at.y - at.x * previous.y;
            return sum * 0.5f;
        }
    }
}
