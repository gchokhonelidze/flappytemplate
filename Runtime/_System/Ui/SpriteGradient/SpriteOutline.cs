using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    /// <summary>A sprite's silhouette as closed loops, in the sprite's own 0..1 rect space.</summary>
    // Loops are wound so that the shape is on the left of the direction of travel: the outer loop runs
    // anticlockwise and a hole runs clockwise, which is what lets one rule - "outward is to the right" -
    // push a border the correct way around both without knowing which it is looking at.
    internal sealed class SpriteOutlineData
    {
        /// <summary>One closed loop each, first point not repeated at the end.</summary>
        public readonly List<Vector2[]> Contours;

        /// <summary>How far apart the alpha was sampled, in the same 0..1 space. Zero when the outline did not come from a trace.</summary>
        public readonly float Spacing;

        /// <summary>A point well inside the shape, on the most solid and least tinted part of it. Negative when the trace could not look.</summary>
        // A UI graphic draws through one texture, so a border painted around a sprite is painted through
        // that sprite - and comes out as transparent as whatever it lands on, tinted by whatever colour is
        // there. So one spot is found that is fully opaque and as close to white as the picture gets, and
        // every border vertex reads the texture from there instead of from underneath itself.
        public readonly Vector2 Solid;

        /// <summary>What the texture actually holds at <see cref="Solid"/>, so a border can be corrected for it.</summary>
        public readonly Color SolidColor;

        /// <summary>Whether this is the real silhouette or the sprite's own importer mesh standing in for it.</summary>
        public bool Traced => Solid.x >= 0f;

        public SpriteOutlineData(List<Vector2[]> contours, float spacing, Vector2 solid, Color solidColor)
        {
            Contours = contours;
            Spacing = spacing;
            Solid = solid;
            SolidColor = solidColor;
        }

        public bool IsEmpty => Contours == null || Contours.Count == 0;
    }

    // Where a sprite ends. Everything a SpriteGradient draws around a picture - the border, the ramp running
    // around it, the fade off its edge - needs that edge as a path rather than as pixels, and a sprite
    // carries no such thing.
    //
    // Two ways of finding one, in the order they are worth having. The alpha channel traced at a threshold
    // gives the real silhouette, and the pixels for that are got hold of whatever the import settings say -
    // see TryReadPixels. Where even that cannot reach - a sprite lying on its side in an atlas - the
    // sprite's own mesh stands in: a tight-packed sprite carries a rough outline from the importer, and a
    // full-rect one carries its quad, which is the honest answer for a picture with no transparency to
    // speak of.
    //
    // A trace is not cheap and its answer never changes, so it is done once per sprite and kept. The cost is
    // paid on the first frame a sprite is drawn with a border, not on every rebuild.
    internal static class SpriteOutline
    {
        // The alpha is sampled onto a grid no larger than this on its longest side. A silhouette is a shape,
        // not a texture: past a couple of hundred samples the extra points land inside the width of the line
        // they describe, and every one of them is a vertex somebody has to pay for.
        private const int MaxSamples = 192;

        // Loops smaller than this fraction of the sprite are dropped. A stray pixel left over from an eraser
        // is not a shape anybody meant to put a border around.
        private const float MinimumLoopArea = 0.0002f;

        // No segment of a finished loop is longer than this fraction of the sprite. A gradient is read at
        // the points of the outline and blended in a straight line between them, so a long run - the flat
        // side of a card, a straight edge on a UI chip - would otherwise have nothing between its ends for
        // the ramp to bend at.
        /// <summary>How long a segment may be, as a fraction of the sprite, before a gradient needs it broken up.</summary>
        public const float MaxSegment = 1f / 24f;

        // A ceiling on how detailed a loop is allowed to end up. Every point of it is four vertices of
        // border and four more of fade, so a silhouette with a lot of coastline - lettering, fur, a torn
        // edge - could otherwise hand the canvas a mesh nobody budgeted for.
        private const int MaxLoopPoints = 96;

        private const int CacheLimit = 64;

        private readonly struct Key : IEquatable<Key>
        {
            private readonly int sprite;
            private readonly int threshold;
            private readonly int tolerance;

            public Key(Sprite sprite, float threshold, float tolerance)
            {
                this.sprite = sprite.GetInstanceID();
                this.threshold = Mathf.RoundToInt(threshold * 255f);
                this.tolerance = Mathf.RoundToInt(tolerance * 10000f);
            }

            public bool Equals(Key other) => sprite == other.sprite && threshold == other.threshold && tolerance == other.tolerance;

            public override bool Equals(object obj) => obj is Key other && Equals(other);

            public override int GetHashCode() => (sprite * 397 ^ threshold) * 397 ^ tolerance;
        }

        private static readonly Dictionary<Key, SpriteOutlineData> Cache = new Dictionary<Key, SpriteOutlineData>();

        // Working room for the two passes that rebuild a loop rather than editing it in place.
        private static readonly List<Vector2> Cut = new List<Vector2>();
        private static readonly List<Vector2> Kept = new List<Vector2>();
        private static readonly List<bool> Keep = new List<bool>();
        private static readonly List<int> Pending = new List<int>();

        private static readonly Vector2 NoSolid = new Vector2(-1f, -1f);

        private static readonly SpriteOutlineData Empty = new SpriteOutlineData(new List<Vector2[]>(), 0f, NoSolid, Color.white);

        /// <summary>The sprite's silhouette, traced on the first call and handed back from memory after that.</summary>
        public static SpriteOutlineData Get(Sprite sprite, float alphaThreshold, float tolerance)
        {
            if (sprite == null)
                return Empty;

            var key = new Key(sprite, alphaThreshold, tolerance);
            if (Cache.TryGetValue(key, out var cached))
                return cached;

            // Emptied rather than trimmed. Keeping the most recently used would mean tracking use on every
            // frame of every box, to save a trace that only happens when a scene has more than this many
            // outlined sprites alive at once.
            if (Cache.Count >= CacheLimit)
                Cache.Clear();

            var traced = Trace(sprite, alphaThreshold, tolerance);
            Cache[key] = traced;
            return traced;
        }

        /// <summary>Forgets every traced outline. Called when a sprite's import settings may have changed under us.</summary>
        public static void Clear() => Cache.Clear();

        /// <summary>Whether this sprite's real silhouette can be looked for at all, or whether its mesh has to stand in.</summary>
        // Read/Write is deliberately not asked about. It is off by default, it doubles what a texture costs
        // in memory, and a sprite handed over in an asset bundle or downloaded at runtime cannot have it
        // turned on at all - so a border that needed it would be a border that usually did not work.
        public static bool CanTraceAlpha(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return false;

            // A sprite turned on its side inside an atlas has its pixels somewhere they no longer line up
            // with the rect being drawn, and untangling that is more trouble than the fallback is worth.
            return !sprite.packed || sprite.packingRotation == SpritePackingRotation.None;
        }

        private static SpriteOutlineData Trace(Sprite sprite, float alphaThreshold, float tolerance)
        {
            var contours = new List<Vector2[]>();

            if (CanTraceAlpha(sprite) && TraceAlpha(sprite, alphaThreshold, tolerance, contours, out var traced))
                return traced;

            contours.Clear();
            TraceMesh(sprite, tolerance, contours);

            // Nothing was read, so nothing can be said about where the picture is solid. The component
            // falls back to reading the texture just inside each outline point instead.
            return new SpriteOutlineData(contours, 0f, NoSolid, Color.white);
        }

        // ---- the alpha channel -------------------------------------------------------------------------

        private static bool TraceAlpha(Sprite sprite, float threshold, float tolerance, List<Vector2[]> contours, out SpriteOutlineData result)
        {
            result = null;

            var texture = sprite.texture;
            var area = sprite.textureRect;

            int left = Mathf.Clamp(Mathf.FloorToInt(area.x), 0, Mathf.Max(0, texture.width - 1));
            int bottom = Mathf.Clamp(Mathf.FloorToInt(area.y), 0, Mathf.Max(0, texture.height - 1));
            int width = Mathf.Clamp(Mathf.RoundToInt(area.width), 1, texture.width - left);
            int height = Mathf.Clamp(Mathf.RoundToInt(area.height), 1, texture.height - bottom);

            if (!TryReadPixels(texture, left, bottom, width, height, out var pixels))
                return false;

            int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(width, height) / (float)MaxSamples));
            int columns = Mathf.Max(2, width / step);
            int rows = Mathf.Max(2, height / step);

            // A ring of empty cells all the way round. Marching squares only closes a loop when the shape is
            // surrounded by outside, and a sprite drawn right up to its own edge - which is most of them -
            // would otherwise leave a path that runs off the grid and never comes back.
            int gridWidth = columns + 2;
            int gridHeight = rows + 2;
            var alpha = new float[gridWidth * gridHeight];
            var sampled = new Color[columns * rows];

            for (int y = 0; y < rows; y++)
            {
                int fromY = Mathf.Min(height - 1, y * step);
                int toY = Mathf.Min(height, fromY + step);

                for (int x = 0; x < columns; x++)
                {
                    int fromX = Mathf.Min(width - 1, x * step);
                    int toX = Mathf.Min(width, fromX + step);

                    // Averaged over the whole block the sample stands for, rather than read from the middle
                    // of it. Reading the middle throws away everything the other texels knew: a hard edge
                    // stays hard at this resolution, the crossing below has nothing to interpolate between
                    // and lands in the middle of a cell by default, and the trace comes back as a staircase
                    // a texel deep. Averaged, the same edge arrives as a ramp across one cell, and where it
                    // crosses the threshold can be read off it to a fraction of a texel.
                    var sum = Color.clear;
                    int taken = 0;
                    for (int sy = fromY; sy < toY; sy++)
                    {
                        for (int sx = fromX; sx < toX; sx++)
                        {
                            sum += pixels[sy * width + sx];
                            taken++;
                        }
                    }

                    var pixel = taken > 0 ? sum / taken : pixels[fromY * width + fromX];

                    alpha[(y + 1) * gridWidth + (x + 1)] = pixel.a;
                    sampled[y * columns + x] = pixel;
                }
            }

            var marcher = new Marcher(alpha, gridWidth, gridHeight, threshold);
            marcher.March();

            var loops = new List<List<Vector2>>();
            marcher.BuildLoops(loops);
            if (loops.Count == 0)
                return false;

            float spacing = step / (float)Mathf.Min(width, height);

            // Nothing finer than the grid it was read off can be true, so the tolerance is floored at half a
            // sample however low it is set. Below that the simplification is not keeping detail, it is
            // keeping the sampling's own wobble and calling it part of the shape - which on a small sprite,
            // where one sample is a whole texel, is most of what there is to keep.
            float detail = Mathf.Max(tolerance, spacing * 0.5f);

            // Grid coordinates back to the sprite's own 0..1. Cell 1 holds the first real sample, which was
            // taken half a step into the picture, so the two offsets below are the sample's own position and
            // not a fudge.
            foreach (var loop in loops)
            {
                for (int i = 0; i < loop.Count; i++)
                {
                    var point = loop[i];
                    loop[i] = new Vector2(
                        ((point.x - 1f) * step + step * 0.5f) / width,
                        ((point.y - 1f) * step + step * 0.5f) / height);
                }

                Finish(loop, detail, contours);
            }

            if (contours.Count == 0)
                return false;

            var solid = FindSolid(sampled, columns, rows, out var solidColor);
            result = new SpriteOutlineData(
                contours,
                spacing,
                new Vector2((solid.x * step + step * 0.5f) / width, (solid.y * step + step * 0.5f) / height),
                solidColor);

            return true;
        }

        // The sprite's own corner of the atlas, as pixels, whatever its import settings say.
        //
        // GetPixels is tried first, since a texture that is already readable can be read without troubling
        // the GPU. Most are not - Read/Write is off by default and doubles what a texture costs in memory -
        // and a compressed one refuses even when it is on. So the fallback is to make the GPU produce them:
        // blitting the texture into a render target decompresses it and puts it somewhere ReadPixels can
        // reach, which works for any texture the renderer is able to draw at all. It costs one stall, once
        // per sprite, against a border that would otherwise not work on most projects' sprites.
        private static bool TryReadPixels(Texture2D texture, int left, int bottom, int width, int height, out Color[] pixels)
        {
            pixels = null;

            if (texture.isReadable)
            {
                try
                {
                    pixels = texture.GetPixels(left, bottom, width, height);
                    if (pixels != null && pixels.Length >= width * height)
                        return true;
                }
                catch (Exception)
                {
                    // Compressed on the CPU side as well, then. The GPU has to be able to decompress it -
                    // it is drawing the thing - so ask it instead.
                }
            }

            RenderTexture target = null;
            Texture2D scratch = null;
            var previous = RenderTexture.active;

            try
            {
                // sRGB rather than linear, so what comes back is what GetPixels would have given for the
                // same texture. In a linear project the shader converts on the way in and the render target
                // converts back on the way out; in a gamma one neither happens and it makes no odds.
                target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

                // Scaled rather than cropped afterwards, so one small sprite on a 4K atlas page costs a
                // sprite-sized readback and not a 4K one.
                var scale = new Vector2(width / (float)texture.width, height / (float)texture.height);
                var offset = new Vector2(left / (float)texture.width, bottom / (float)texture.height);
                Graphics.Blit(texture, target, scale, offset);

                RenderTexture.active = target;
                scratch = new Texture2D(width, height, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                scratch.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                scratch.Apply(false);

                pixels = scratch.GetPixels();
                return pixels != null && pixels.Length >= width * height;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                // Put back whatever was being drawn into. This can run in the middle of a canvas rebuild,
                // and leaving the active target pointing at a texture about to be released would take the
                // rest of that frame down with it.
                RenderTexture.active = previous;

                if (target != null)
                    RenderTexture.ReleaseTemporary(target);

                if (scratch != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(scratch);
                    else
                        UnityEngine.Object.DestroyImmediate(scratch);
                }
            }
        }

        // The one place in the picture a border can safely be drawn through: opaque, with opaque neighbours
        // so that filtering cannot drag anything transparent into it, and as close to white as the sprite
        // gets so that the least has to be corrected for afterwards.
        //
        // Neighbours are counted on the sampling grid rather than on the texture, so they are a whole step
        // away - a spot that survives this is in the body of the shape, not on a bright fleck at its edge.
        private static Vector2Int FindSolid(Color[] sampled, int columns, int rows, out Color color)
        {
            int bestX = columns / 2;
            int bestY = rows / 2;
            float best = -1f;

            for (int y = 1; y < rows - 1; y++)
            {
                for (int x = 1; x < columns - 1; x++)
                {
                    float weakest = 1f;
                    for (int ny = -1; ny <= 1; ny++)
                    {
                        for (int nx = -1; nx <= 1; nx++)
                            weakest = Mathf.Min(weakest, sampled[(y + ny) * columns + x + nx].a);
                    }

                    var here = sampled[y * columns + x];
                    float brightness = (here.r + here.g + here.b) / 3f;

                    // Opacity first and by a distance - a dim spot in the middle of the shape can be
                    // corrected for, and a bright one on its edge cannot be drawn on at all.
                    float score = weakest * 4f + brightness;
                    if (score <= best)
                        continue;

                    best = score;
                    bestX = x;
                    bestY = y;
                }
            }

            color = sampled.Length > 0 ? sampled[bestY * columns + bestX] : Color.white;
            return new Vector2Int(bestX, bestY);
        }

        // Marching squares. Each cell of the grid has its four corners either inside the shape or out, and
        // the sixteen ways that can happen say which of the cell's four edges the outline crosses and in
        // which direction it leaves. Where it crosses is read off the alpha either side, so the result
        // follows the shape rather than the pixel grid - no staircase to smooth away afterwards.
        //
        // Segments are stored as pairs of edge numbers rather than pairs of points. An edge belongs to the
        // two cells either side of it and gets the same number from both, so the loops stitch back together
        // by lookup instead of by comparing floats that were arrived at separately.
        private sealed class Marcher
        {
            private readonly float[] alpha;
            private readonly int width;
            private readonly int height;
            private readonly float threshold;
            private readonly int horizontalCount;

            private readonly Dictionary<int, Vector2> crossings = new Dictionary<int, Vector2>();
            private readonly List<int> from = new List<int>();
            private readonly List<int> to = new List<int>();

            public Marcher(float[] alpha, int width, int height, float threshold)
            {
                this.alpha = alpha;
                this.width = width;
                this.height = height;
                this.threshold = threshold;
                horizontalCount = (width - 1) * height;
            }

            public void March()
            {
                for (int y = 0; y < height - 1; y++)
                {
                    for (int x = 0; x < width - 1; x++)
                        Cell(x, y);
                }
            }

            private void Cell(int x, int y)
            {
                int mask = 0;
                if (Inside(x, y)) mask |= 1;
                if (Inside(x + 1, y)) mask |= 2;
                if (Inside(x + 1, y + 1)) mask |= 4;
                if (Inside(x, y + 1)) mask |= 8;

                if (mask == 0 || mask == 15)
                    return;

                // Read as "the shape is on the left of where we are going". Every case below is that one
                // rule applied to whichever corners are in, and the two ambiguous ones - a pair of opposite
                // corners in - are settled by the middle of the cell, so the choice is at least consistent
                // between the cell and its neighbour.
                switch (mask)
                {
                    case 1: Segment(Bottom(x, y), Left(x, y)); break;
                    case 2: Segment(Right(x, y), Bottom(x, y)); break;
                    case 3: Segment(Right(x, y), Left(x, y)); break;
                    case 4: Segment(Top(x, y), Right(x, y)); break;
                    case 5:
                        if (CenterInside(x, y))
                        {
                            Segment(Top(x, y), Left(x, y));
                            Segment(Bottom(x, y), Right(x, y));
                        }
                        else
                        {
                            Segment(Bottom(x, y), Left(x, y));
                            Segment(Top(x, y), Right(x, y));
                        }

                        break;
                    case 6: Segment(Top(x, y), Bottom(x, y)); break;
                    case 7: Segment(Top(x, y), Left(x, y)); break;
                    case 8: Segment(Left(x, y), Top(x, y)); break;
                    case 9: Segment(Bottom(x, y), Top(x, y)); break;
                    case 10:
                        if (CenterInside(x, y))
                        {
                            Segment(Left(x, y), Bottom(x, y));
                            Segment(Right(x, y), Top(x, y));
                        }
                        else
                        {
                            Segment(Right(x, y), Bottom(x, y));
                            Segment(Left(x, y), Top(x, y));
                        }

                        break;
                    case 11: Segment(Right(x, y), Top(x, y)); break;
                    case 12: Segment(Left(x, y), Right(x, y)); break;
                    case 13: Segment(Bottom(x, y), Right(x, y)); break;
                    case 14: Segment(Left(x, y), Bottom(x, y)); break;
                }
            }

            public void BuildLoops(List<List<Vector2>> loops)
            {
                if (from.Count == 0)
                    return;

                var starts = new Dictionary<int, int>(from.Count);
                for (int i = 0; i < from.Count; i++)
                    starts[from[i]] = i;

                var used = new bool[from.Count];
                for (int i = 0; i < from.Count; i++)
                {
                    if (used[i])
                        continue;

                    var loop = new List<Vector2>();
                    int at = i;
                    while (true)
                    {
                        used[at] = true;
                        loop.Add(crossings[from[at]]);

                        // Runs out either when the loop closes onto a segment already walked, which is the
                        // normal ending, or when the path leaves the grid - which the empty ring around the
                        // samples is there to prevent, and which leaves a usable open path anyway.
                        if (!starts.TryGetValue(to[at], out int next) || used[next])
                            break;

                        at = next;
                    }

                    if (loop.Count >= 3)
                        loops.Add(loop);
                }
            }

            private bool Inside(int x, int y) => alpha[y * width + x] >= threshold;

            private bool CenterInside(int x, int y)
            {
                float sum = alpha[y * width + x] + alpha[y * width + x + 1] +
                            alpha[(y + 1) * width + x] + alpha[(y + 1) * width + x + 1];

                return sum * 0.25f >= threshold;
            }

            private void Segment(int start, int end)
            {
                from.Add(start);
                to.Add(end);
            }

            private int Bottom(int x, int y) => Horizontal(x, y);

            private int Top(int x, int y) => Horizontal(x, y + 1);

            private int Left(int x, int y) => Vertical(x, y);

            private int Right(int x, int y) => Vertical(x + 1, y);

            private int Horizontal(int x, int y)
            {
                int id = y * (width - 1) + x;
                if (!crossings.ContainsKey(id))
                    crossings[id] = new Vector2(x + Crossing(alpha[y * width + x], alpha[y * width + x + 1]), y);

                return id;
            }

            private int Vertical(int x, int y)
            {
                int id = horizontalCount + y * width + x;
                if (!crossings.ContainsKey(id))
                    crossings[id] = new Vector2(x, y + Crossing(alpha[y * width + x], alpha[(y + 1) * width + x]));

                return id;
            }

            // Where between the two samples the alpha passes the threshold. Two equal samples cannot say, so
            // the crossing is put in the middle - which only comes up on a hard-edged shape, where the two
            // are a full step apart and half of that is as close as the grid can get anyway.
            private float Crossing(float a, float b)
            {
                float span = b - a;
                return Mathf.Abs(span) < 1e-6f ? 0.5f : Mathf.Clamp01((threshold - a) / span);
            }
        }

        // ---- the sprite's own mesh ---------------------------------------------------------------------

        // The importer already worked out a shape for the sprite - tight packing needs one - and that shape
        // is on the asset whether the texture can be read or not. It is coarser than a trace and it is the
        // bare quad for a full-rect sprite, but it is a real outline and it costs nothing to ask for.
        private static void TraceMesh(Sprite sprite, float tolerance, List<Vector2[]> contours)
        {
            var vertices = sprite.vertices;
            var triangles = sprite.triangles;
            if (vertices == null || triangles == null || vertices.Length < 3 || triangles.Length < 3)
                return;

            // An edge shared by two triangles has the same two ends in opposite orders; an edge on the
            // boundary is the one with nobody facing it back.
            var directed = new HashSet<long>();
            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                directed.Add(EdgeKey(triangles[i], triangles[i + 1]));
                directed.Add(EdgeKey(triangles[i + 1], triangles[i + 2]));
                directed.Add(EdgeKey(triangles[i + 2], triangles[i]));
            }

            var next = new Dictionary<int, int>();
            foreach (long edge in directed)
            {
                int a = (int)(edge >> 32);
                int b = (int)(edge & 0xffffffffL);
                if (!directed.Contains(EdgeKey(b, a)))
                    next[a] = b;
            }

            if (next.Count == 0)
                return;

            // The sprite's vertices are in world units measured from the pivot; this puts them back where
            // they came from, as a fraction across the sprite's rect.
            float ppu = sprite.pixelsPerUnit;
            var size = sprite.rect.size;
            var pivot = sprite.pivot;

            var loops = new List<List<Vector2>>();
            var visited = new HashSet<int>();
            foreach (int start in next.Keys)
            {
                if (visited.Contains(start))
                    continue;

                var loop = new List<Vector2>();
                int at = start;
                while (visited.Add(at))
                {
                    var vertex = vertices[at];
                    loop.Add(new Vector2(
                        (vertex.x * ppu + pivot.x) / Mathf.Max(1f, size.x),
                        (vertex.y * ppu + pivot.y) / Mathf.Max(1f, size.y)));

                    if (!next.TryGetValue(at, out at))
                        break;
                }

                if (loop.Count >= 3)
                    loops.Add(loop);
            }

            if (loops.Count == 0)
                return;

            // Which way round the importer wound its triangles is its own business, so it is measured rather
            // than assumed: whichever loop encloses the most is the outer one, and if that is running
            // clockwise then every loop is back to front and all of them are turned round together.
            float widestArea = 0f;
            for (int i = 0; i < loops.Count; i++)
            {
                float area = SignedArea(loops[i]);
                if (Mathf.Abs(area) > Mathf.Abs(widestArea))
                    widestArea = area;
            }

            bool flip = widestArea < 0f;
            for (int i = 0; i < loops.Count; i++)
            {
                if (flip)
                    loops[i].Reverse();

                Finish(loops[i], tolerance, contours);
            }
        }

        private static long EdgeKey(int a, int b) => ((long)a << 32) | (uint)b;

        // ---- tidying up --------------------------------------------------------------------------------

        // Everything between a raw path and one worth handing out: specks dropped, the sampling's zigzag
        // rounded off, the detail thinned, and then the long runs broken up again so that a gradient has
        // somewhere to bend along them.
        private static void Finish(List<Vector2> loop, float tolerance, List<Vector2[]> contours)
        {
            if (loop.Count < 3)
                return;

            if (Mathf.Abs(SignedArea(loop)) < MinimumLoopArea)
                return;

            // Corners are cut before anything is thrown away. Simplifying a staircase gives a coarser
            // staircase and not a curve, and a staircase is what a border cannot survive: offset by more
            // than the depth of its steps, the strip's own quads fan out and cross each other.
            //
            // Chaikin cuts each corner in proportion to the two segments meeting at it, which is exactly the
            // distinction wanted here - the zigzag left by reading a hard edge off a grid has segments a
            // cell long and rounds away, while a corner the shape actually has, with long sides either side
            // of it, is barely touched.
            Smooth(loop);
            Smooth(loop);

            Simplify(loop, tolerance);

            // Then loosened until the result is something a canvas can carry. A tolerance is a statement
            // about detail, not about cost, and on a silhouette with a lot of coastline the two part ways.
            for (int i = 0; i < 8 && loop.Count > MaxLoopPoints; i++)
            {
                tolerance = Mathf.Max(tolerance * 1.8f, 0.002f);
                Simplify(loop, tolerance);
            }

            if (loop.Count < 3)
                return;

            contours.Add(loop.ToArray());
        }

        // One pass of Chaikin's corner cutting: every point is replaced by two, a quarter and three
        // quarters of the way along the edges it sits between. Two passes take a right-angled step down to
        // something the eye reads as a curve.
        private static void Smooth(List<Vector2> loop)
        {
            int count = loop.Count;
            if (count < 4)
                return;

            Cut.Clear();
            for (int i = 0; i < count; i++)
            {
                var a = loop[i];
                var b = loop[(i + 1) % count];
                Cut.Add(Vector2.LerpUnclamped(a, b, 0.25f));
                Cut.Add(Vector2.LerpUnclamped(a, b, 0.75f));
            }

            loop.Clear();
            loop.AddRange(Cut);
        }

        private static float SignedArea(List<Vector2> loop)
        {
            float sum = 0f;
            for (int i = 0; i < loop.Count; i++)
            {
                var a = loop[i];
                var b = loop[(i + 1) % loop.Count];
                sum += a.x * b.y - b.x * a.y;
            }

            return sum * 0.5f;
        }

        // Douglas-Peucker. Of the points between two that are being kept, the one furthest from the line
        // between them is kept as well, and the two halves that leaves are done the same way, until nothing
        // is further out than the tolerance. What matters is that every point thrown away is measured
        // against the line that will actually replace it, so nothing ends up further than the tolerance
        // from the finished outline however many of its neighbours went with it.
        //
        // The obvious cheaper rule - drop a point that lies close to the line between the two either side
        // of it - does not survive a smooth closed curve. Each point is measured against a neighbour it has
        // not lost yet, so it looks close to one end of the chord however far the other end has run away,
        // and the loop is eaten whole rather than thinned.
        private static void Simplify(List<Vector2> loop, float tolerance)
        {
            int count = loop.Count;
            if (tolerance <= 0f || count < 6)
                return;

            // A closed loop has no ends to anchor the first split to, the way an open path does, so two are
            // made by hand: the first point, and whichever lies furthest from it. One alone would leave the
            // whole loop as a single chord from that point back to itself, with nothing to measure against.
            int far = 0;
            float furthest = -1f;
            for (int i = 1; i < count; i++)
            {
                float distance = (loop[i] - loop[0]).sqrMagnitude;
                if (distance <= furthest)
                    continue;

                furthest = distance;
                far = i;
            }

            Keep.Clear();
            for (int i = 0; i < count; i++)
                Keep.Add(false);

            Keep[0] = true;
            Keep[far] = true;

            // Walked with a stack rather than by recursion: the worst case is one frame per point, and a
            // silhouette can arrive with a couple of thousand of them. Indices run past the end and are
            // wrapped when read, so the half that closes the loop needs no special case.
            Pending.Clear();
            Pending.Add(0);
            Pending.Add(far);
            Pending.Add(far);
            Pending.Add(count);

            while (Pending.Count >= 2)
            {
                int to = Pending[Pending.Count - 1];
                int from = Pending[Pending.Count - 2];
                Pending.RemoveRange(Pending.Count - 2, 2);

                if (to - from < 2)
                    continue;

                var a = loop[from % count];
                var b = loop[to % count];

                int worst = -1;
                float deviation = tolerance;
                for (int i = from + 1; i < to; i++)
                {
                    float distance = DistanceToLine(loop[i % count], a, b);
                    if (distance <= deviation)
                        continue;

                    deviation = distance;
                    worst = i;
                }

                if (worst < 0)
                    continue;

                Keep[worst % count] = true;
                Pending.Add(from);
                Pending.Add(worst);
                Pending.Add(worst);
                Pending.Add(to);
            }

            Kept.Clear();
            for (int i = 0; i < count; i++)
            {
                if (Keep[i])
                    Kept.Add(loop[i]);
            }

            // A loop that came out of this as a line or a point was never a shape to begin with; what it
            // was is left alone rather than replaced with nothing.
            if (Kept.Count < 3)
                return;

            loop.Clear();
            loop.AddRange(Kept);
        }

        private static float DistanceToLine(Vector2 point, Vector2 a, Vector2 b)
        {
            var span = b - a;
            float length = span.sqrMagnitude;
            if (length < 1e-12f)
                return Vector2.Distance(point, a);

            float t = Mathf.Clamp01(Vector2.Dot(point - a, span) / length);
            return Vector2.Distance(point, a + span * t);
        }

        /// <summary>Breaks up any segment longer than <paramref name="maxSegment"/>, in whatever units the loop is in.</summary>
        // Kept out of the trace and handed to the caller, because whether it is wanted depends on how the
        // border is being coloured rather than on the sprite. A ramp between two points is already exact
        // along a straight run; what these points buy is somewhere for a gradient's middle keys to land,
        // and a border that has no middle keys is paying twice the vertices for nothing.
        public static void Subdivide(List<Vector2> loop, float maxSegment)
        {
            if (maxSegment <= 0f)
                return;

            for (int i = loop.Count - 1; i >= 0; i--)
            {
                var a = loop[i];
                var b = loop[(i + 1) % loop.Count];

                float length = Vector2.Distance(a, b);
                if (length <= maxSegment)
                    continue;

                int pieces = Mathf.Min(64, Mathf.CeilToInt(length / maxSegment));
                for (int piece = pieces - 1; piece >= 1; piece--)
                    loop.Insert(i + 1, Vector2.Lerp(a, b, piece / (float)pieces));
            }
        }
    }
}
