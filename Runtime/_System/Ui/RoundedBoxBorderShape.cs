using System.Collections.Generic;
using UnityEngine;

namespace FlappyTemplate
{
    // Hands a particle system the shape of a RoundedBox's border to spawn from, so an effect follows the
    // frame - fire around a card, sparks along an outline, a glow that keeps to the edge - and follows it
    // through a resize or a change of radius without being redrawn by hand.
    //
    // The border becomes a mesh of the ring itself rather than a line around it, so particles spawn across
    // the thickness rather than on a hairline. Sides with no thickness are left out of that mesh entirely:
    // leaving them in as collapsed quads would look like nothing but emit like a line, since a particle
    // system picks a triangle before it looks at how big it is.
    [ExecuteAlways]
    [RequireComponent(typeof(ParticleSystem))]
    [AddComponentMenu("UI/Rounded Box Border Shape")]
    public class RoundedBoxBorderShape : MonoBehaviour
    {
        [Tooltip("The box whose border is spawned from. Empty uses the parent, which is how this is normally set up.")]
        [SerializeField]
        private RoundedBox source;

        [Tooltip("Sides thinner than this are left out, so a border set to 0 emits nothing at all. Raise it to also drop the hairline sides of a frame that is mostly on one edge.")]
        [SerializeField]
        private float minimumThickness = 0.01f;

        [Tooltip("Off builds the shape once on enable, which is enough for a box that never changes size. On keeps it following the box - through a layout pass, an animated radius, a border that grows.")]
        [SerializeField]
        private bool follow = true;

        private ParticleSystem particles;
        private Mesh mesh;

        private readonly List<Vector2> outerPath = new List<Vector2>();
        private readonly List<Vector2> innerPath = new List<Vector2>();
        private readonly List<Vector2> builtOuter = new List<Vector2>();
        private readonly List<Vector2> builtInner = new List<Vector2>();
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();

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

            box.GetBorderPath(outerPath, innerPath);
            if (outerPath.Count < 3)
                return;

            // Compared before rebuilding rather than after: the mesh is uploaded to the GPU, and doing that
            // every frame for an outline that has not moved is the whole cost of this component.
            if (Matches(outerPath, builtOuter) && Matches(innerPath, builtInner))
                return;

            BuildMesh(box);

            builtOuter.Clear();
            builtOuter.AddRange(outerPath);
            builtInner.Clear();
            builtInner.AddRange(innerPath);
        }

        private void BuildMesh(RoundedBox box)
        {
            vertices.Clear();
            triangles.Clear();

            int count = outerPath.Count;
            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;

                // A quad is kept if either end has any thickness, so the taper where a border meets a side
                // that has none still emits - the frame fades out rather than stopping dead.
                if (Thickness(i) <= minimumThickness && Thickness(j) <= minimumThickness)
                    continue;

                int start = vertices.Count;
                vertices.Add(ToLocal(box, outerPath[i]));
                vertices.Add(ToLocal(box, innerPath[i]));
                vertices.Add(ToLocal(box, outerPath[j]));
                vertices.Add(ToLocal(box, innerPath[j]));

                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
                triangles.Add(start + 3);
                triangles.Add(start + 1);
                triangles.Add(start);
            }

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
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
            }

            Apply(triangles.Count > 0);
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
