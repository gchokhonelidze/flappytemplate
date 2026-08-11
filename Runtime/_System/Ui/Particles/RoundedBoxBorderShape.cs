using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    // Hands a particle system the shape of a RoundedBox to spawn from, so an effect follows the box - fire
    // around a frame, sparks along an outline, smoke rising off a panel - through a resize or a change of
    // radius without being redrawn by hand.
    //
    // Either part of the box will do. The border becomes a mesh of the ring itself rather than a line
    // around it, so particles spawn across the thickness rather than on a hairline; the body becomes a fan
    // over the whole shape, corners and all, which is what to reach for when the effect belongs to the
    // panel rather than to its frame.
    //
    // Sides with no thickness are left out of the ring entirely: leaving them in as collapsed quads would
    // look like nothing but emit like a line, since a particle system picks a triangle before it looks at
    // how big it is.
    [ExecuteAlways]
    [RequireComponent(typeof(ParticleSystem))]
    [AddComponentMenu("UI/Rounded Box Border Shape")]
    public class RoundedBoxBorderShape : MonoBehaviour
    {
        [Tooltip("The box whose border is spawned from. Empty uses the parent, which is how this is normally set up.")]
        [SerializeField]
        private RoundedBox source;

        [Tooltip("Border spawns from the frame itself. Fill spawns from the whole body out to the outline, and Inside from the body within the border - which is the same thing wherever there is no border to stay inside of.")]
        [SerializeField]
        private EBoxEmitArea area = EBoxEmitArea.Border;

        [Tooltip("Which way particles set off. Outward is square to the edge they spawned on, Around travels along it. Needs Start Speed above zero on the particle system - this only points them, it does not push them.")]
        [SerializeField]
        private EBoxEmitDirection direction = EBoxEmitDirection.Outward;

        [Tooltip("Border only. Sides thinner than this are left out, so a border set to 0 emits nothing at all. Raise it to also drop the hairline sides of a frame that is mostly on one edge.")]
        [SerializeField]
        private float minimumThickness = 0.01f;

        [Tooltip("Off builds the shape once on enable, which is enough for a box that never changes size. On keeps it following the box - through a layout pass, an animated radius, a border that grows.")]
        [SerializeField]
        private bool follow = true;

        private ParticleSystem particles;
        private Mesh mesh;

        // Where the box sat in this object's space when the mesh was built. See Rebuild.
        private Matrix4x4 builtRelation;

        private readonly List<Vector2> outerPath = new List<Vector2>();
        private readonly List<Vector2> innerPath = new List<Vector2>();
        private readonly List<Vector2> builtOuter = new List<Vector2>();
        private readonly List<Vector2> builtInner = new List<Vector2>();
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> pathNormals = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        /// <summary>Which part of the box particles spawn from.</summary>
        public EBoxEmitArea Area
        {
            get => area;
            set
            {
                if (area == value)
                    return;

                area = value;

                // The outline has not moved, so the comparison that normally guards a rebuild would let
                // this through unnoticed. Forgetting what was built is what asks for it again.
                builtOuter.Clear();
                Rebuild();
            }
        }

        /// <summary>Which way particles set off from where they spawned.</summary>
        public EBoxEmitDirection Direction
        {
            get => direction;
            set
            {
                if (direction == value)
                    return;

                direction = value;
                builtOuter.Clear();
                Rebuild();
            }
        }

        /// <summary>The box being followed - the assigned source, or the parent when there is none.</summary>
        public RoundedBox Source
        {
            get
            {
                if (source != null)
                    return source;

                return transform.parent != null ? transform.parent.GetComponent<RoundedBox>() : null;
            }
        }

        void OnEnable()
        {
            Rebuild();
        }

        void OnDisable()
        {
            // Built at runtime and never saved, so nothing else will clear it up.
            if (mesh != null)
                DestroyImmediate(mesh);

            mesh = null;
            builtOuter.Clear();
            builtInner.Clear();
        }

        // Late so the box has settled for the frame: a layout pass or a fit component has already moved it
        // by now, and this reads the outline it ended up with rather than the one it started with.
        void LateUpdate()
        {
            if (follow)
                Rebuild();
        }

        /// <summary>Reads the box's border and hands its shape to the particle system.</summary>
        [ContextMenu("Rebuild Now")]
        public void Rebuild()
        {
            var box = Source;
            if (box == null)
                return;

            // An effect spawning from the box it hangs on is an overlay, not a cell: said before anything is
            // measured, so a layout group on the box - a UiGrid, a UiWindow - never gets to place this and
            // carry the emitter away from the outline it is meant to trace.
            if (box.transform == transform.parent)
                UiOverlay.KeepOutOfLayout(gameObject);

            box.GetBorderPath(outerPath, innerPath);
            if (outerPath.Count < 3)
                return;

            // Compared before rebuilding rather than after: the mesh is uploaded to the GPU, and doing that
            // every frame for an outline that has not moved is the whole cost of this component.
            //
            // The relation is compared alongside it because the mesh is the box's outline written in this
            // object's own space, so it goes stale when *either* moves - and the paths above are the box's
            // own and say nothing about the emitter. Without it, an emitter that is moved after the fact
            // keeps a shape offset by however far it travelled: particles around where the box used to be.
            var relation = transform.worldToLocalMatrix * box.transform.localToWorldMatrix;

            if (relation == builtRelation && Matches(outerPath, builtOuter) && Matches(innerPath, builtInner))
                return;

            BuildMesh(box);

            builtRelation = relation;
            builtOuter.Clear();
            builtOuter.AddRange(outerPath);
            builtInner.Clear();
            builtInner.AddRange(innerPath);
        }

        private void BuildMesh(RoundedBox box)
        {
            vertices.Clear();
            normals.Clear();
            triangles.Clear();

            if (area == EBoxEmitArea.Border)
                BuildRing(box);
            else
                BuildBody(box, area == EBoxEmitArea.Inside ? innerPath : outerPath);

            if (mesh == null)
            {
                mesh = new Mesh
                {
                    name = "Rounded Box Border",
                    hideFlags = HideFlags.HideAndDontSave,
                };
            }

            mesh.Clear();
            if (triangles.Count > 0)
            {
                mesh.SetVertices(vertices);

                // Set rather than recalculated. Recalculating would give the normals of a flat sheet, all
                // pointing at the viewer, and a particle system reads the normal as the direction to set
                // off in - so every particle would fly out of the screen. These point along the shape.
                mesh.SetNormals(normals);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
            }

            Apply(triangles.Count > 0);
        }

        private void BuildRing(RoundedBox box)
        {
            int count = outerPath.Count;
            BuildPathNormals(count);

            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;

                // A quad is kept if either end has any thickness, so the taper where a border meets a side
                // that has none still emits - the frame fades out rather than stopping dead.
                if (Thickness(i) <= minimumThickness && Thickness(j) <= minimumThickness)
                    continue;

                var here = ToLocalDirection(box, Aim(pathNormals[i]));
                var next = ToLocalDirection(box, Aim(pathNormals[j]));

                int start = vertices.Count;
                vertices.Add(ToLocal(box, outerPath[i]));
                normals.Add(here);
                vertices.Add(ToLocal(box, innerPath[i]));
                normals.Add(here);
                vertices.Add(ToLocal(box, outerPath[j]));
                normals.Add(next);
                vertices.Add(ToLocal(box, innerPath[j]));
                normals.Add(next);

                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
                triangles.Add(start + 3);
                triangles.Add(start + 1);
                triangles.Add(start);
            }
        }

        // Which way is out, at every point along the outline. Across the border is the honest answer where
        // there is a border to measure across; where there is not, the outline's own direction gives it.
        private void BuildPathNormals(int count)
        {
            pathNormals.Clear();

            for (int i = 0; i < count; i++)
            {
                var across = outerPath[i] - innerPath[i];
                if (across.sqrMagnitude > 1e-8f)
                {
                    pathNormals.Add(across.normalized);
                    continue;
                }

                var along = outerPath[(i + 1) % count] - outerPath[i];
                if (along.sqrMagnitude <= 1e-8f)
                    along = outerPath[i] - outerPath[(i - 1 + count) % count];

                pathNormals.Add(along.sqrMagnitude > 1e-8f ? new Vector2(-along.y, along.x).normalized : Vector2.up);
            }
        }

        // The body, one triangle at a time from the middle out. Sharing a single centre vertex would be
        // fewer of them, but every triangle would then have to agree on which way that one vertex points -
        // and they all want a different answer, since out of the middle is a different way in each.
        //
        // It does not need the outline to be convex, only to be seen whole from the centre, which is true
        // even with the corners scooped: a scoop can never reach past the middle.
        private void BuildBody(RoundedBox box, List<Vector2> contour)
        {
            var middle = ((RectTransform)box.transform).rect.center;
            var center = ToLocal(box, middle);

            for (int i = 0; i < contour.Count; i++)
            {
                int j = (i + 1) % contour.Count;

                // Inside a border thick enough to have closed the middle up, every one of these collapses -
                // and a mesh with no triangles left is what switches the emission off below.
                if (contour[i] == contour[j])
                    continue;

                var rim = (contour[i] + contour[j]) * 0.5f - middle;
                var outward = rim.sqrMagnitude > 1e-8f ? rim.normalized : Vector2.up;
                var aim = ToLocalDirection(box, Aim(outward));

                int start = vertices.Count;
                vertices.Add(center);
                normals.Add(aim);
                vertices.Add(ToLocal(box, contour[i]));
                normals.Add(aim);
                vertices.Add(ToLocal(box, contour[j]));
                normals.Add(aim);

                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }
        }

        // Around is the outward direction turned a quarter turn, which lands it along the outline in the
        // direction the outline is walked - clockwise from the top left corner.
        private Vector3 Aim(Vector2 outward)
        {
            switch (direction)
            {
                case EBoxEmitDirection.Inward:
                    return new Vector3(-outward.x, -outward.y, 0f);

                case EBoxEmitDirection.Around:
                    return new Vector3(outward.y, -outward.x, 0f);

                default:
                    return new Vector3(outward.x, outward.y, 0f);
            }
        }

        // Emission is switched off rather than left pointing at an empty mesh: a shape with no triangles is
        // an error in the console every frame, and a border of nothing should simply be quiet.
        private void Apply(bool hasShape)
        {
            if (particles == null)
                particles = GetComponent<ParticleSystem>();

            var shape = particles.shape;
            shape.enabled = hasShape;
            if (!hasShape)
                return;

            shape.shapeType = ParticleSystemShapeType.Mesh;
            shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
            shape.mesh = mesh;
        }

        private float Thickness(int index) => Vector2.Distance(outerPath[index], innerPath[index]);

        // The box's outline is in the box's own space, and the particle system spawns in its own. Going
        // through the world between them means this works wherever it is parented and whatever has been
        // done to the rect on the way down.
        private Vector3 ToLocal(RoundedBox box, Vector2 point)
        {
            return transform.InverseTransformPoint(box.transform.TransformPoint(new Vector3(point.x, point.y, 0f)));
        }

        // Directions take the same trip between the two spaces as the points do, but without the position -
        // a rotation on the way down the hierarchy has to reach them, a move does not.
        private Vector3 ToLocalDirection(RoundedBox box, Vector3 aim)
        {
            var world = box.transform.TransformDirection(aim);
            var local = transform.InverseTransformDirection(world);
            return local.sqrMagnitude > 1e-8f ? local.normalized : Vector3.up;
        }

        private static bool Matches(List<Vector2> path, List<Vector2> built)
        {
            if (path.Count != built.Count)
                return false;

            for (int i = 0; i < path.Count; i++)
            {
                if (path[i] != built[i])
                    return false;
            }

            return true;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            minimumThickness = Mathf.Max(0f, minimumThickness);

            // Deferred: OnValidate runs mid-import and mid-undo, where building a mesh and handing it to
            // another component is not safe.
            UnityEditor.EditorApplication.delayCall += ValidateDeferred;
        }

        private void ValidateDeferred()
        {
            if (this == null || !isActiveAndEnabled)
                return;

            builtOuter.Clear();
            Rebuild();
        }
#endif
    }
}
