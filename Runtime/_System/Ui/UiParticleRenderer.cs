using UnityEngine;
using UnityEngine.UI;

namespace FlappyTemplate
{
    // Draws a particle system's particles as a UI mesh instead of letting the particle system draw them
    // itself, which takes the whole question of sorting off the table.
    //
    // A particle system is a renderer and a UI element is not, so the two are sorted against each other by
    // rules that a canvas has no say in: an overlay canvas draws last and always covers them, and a camera
    // canvas leaves them either in front of every element or behind every one, with sorting layers the only
    // lever and no way to land between two pieces of UI. Read the particles out and build a mesh from them
    // and none of that applies - this is a Graphic like any other, drawn where it sits in the hierarchy,
    // clipped by whatever masks it is under.
    //
    // Being clipped is the other half of it: a mask on the box now catches the effect too, so fire can be
    // kept inside the rounded shape rather than spilling past the corners.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    [RequireComponent(typeof(ParticleSystem))]
    [AddComponentMenu("UI/Ui Particle Renderer")]
    public class UiParticleRenderer : MaskableGraphic
    {
        // A UI mesh cannot hold more than 65535 vertices, and every particle is four of them.
        private const int MaxParticles = 16000;

        [Tooltip("The system whose particles are drawn. Empty uses the one on this object.")]
        [SerializeField]
        private ParticleSystem source;

        [Tooltip("Texture each particle is drawn with. Empty takes whatever the particle system's own material uses, which is usually what is wanted.")]
        [SerializeField]
        private Texture particleTexture;

        private ParticleSystem cachedSource;
        private ParticleSystemRenderer cachedRenderer;
        private ParticleSystem.Particle[] buffer;
        private int lastCount;

        /// <summary>The system being drawn - the assigned source, or the one on this object.</summary>
        public ParticleSystem Source
        {
            get
            {
                if (source != null)
                    return source;

                if (cachedSource == null)
                    cachedSource = GetComponent<ParticleSystem>();

                return cachedSource;
            }
        }

        // The texture the particles were authored against. Taken off the particle system's material rather
        // than asked for separately, so the effect looks the same however it is being drawn.
        public override Texture mainTexture
        {
            get
            {
                if (particleTexture != null)
                    return particleTexture;

                var material = SourceRenderer != null ? SourceRenderer.sharedMaterial : null;
                if (material != null && material.mainTexture != null)
                    return material.mainTexture;

                return s_WhiteTexture;
            }
        }

        private ParticleSystemRenderer SourceRenderer
        {
            get
            {
                var system = Source;
                if (cachedRenderer == null && system != null)
                    cachedRenderer = system.GetComponent<ParticleSystemRenderer>();

                return cachedRenderer;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Switched off rather than left running: it would draw the same particles a second time, in the
            // place this exists to move them out of.
            if (SourceRenderer != null)
                SourceRenderer.enabled = false;

            SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Handed back on the way out, so removing this component leaves a working particle system
            // rather than an invisible one.
            if (SourceRenderer != null)
                SourceRenderer.enabled = true;
        }

        // The particles have moved by now, whether they were stepped by the player or by the editor's own
        // preview. Nothing else will mark this dirty, since none of its own serialised state has changed.
        void LateUpdate()
        {
            var system = Source;
            if (system == null)
                return;

            int count = system.particleCount;
            if (count == 0 && lastCount == 0)
                return;

            lastCount = count;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var system = Source;
            if (system == null)
                return;

            int capacity = Mathf.Min(system.main.maxParticles, MaxParticles);
            if (capacity <= 0)
                return;

            if (buffer == null || buffer.Length < capacity)
                buffer = new ParticleSystem.Particle[capacity];

            int count = system.GetParticles(buffer);
            if (count == 0)
                return;

            // Particles simulated in local space are in the system's space and have to be brought into this
            // one; simulated in world space they are already out in the world. Going through the world in
            // both cases means this works with the system on a child, or somewhere else entirely.
            bool worldSpace = system.main.simulationSpace != ParticleSystemSimulationSpace.Local;
            var systemTransform = system.transform;
            bool sameObject = systemTransform == transform;

            // Sizes come back in the space the particles were simulated in, so they need the same change of
            // scale the positions get. One number for both axes: a canvas is scaled evenly or the UI would
            // already be a mess.
            float sizeScale = 1f;
            if (!sameObject)
            {
                float own = Mathf.Abs(transform.lossyScale.x);
                float theirs = worldSpace ? 1f : Mathf.Abs(systemTransform.lossyScale.x);
                sizeScale = own > 1e-6f ? theirs / own : 1f;
            }

            var tint = color;
            for (int i = 0; i < count; i++)
            {
                var particle = buffer[i];

                Vector3 position = particle.position;
                if (!sameObject)
                {
                    position = worldSpace
                        ? transform.InverseTransformPoint(position)
                        : transform.InverseTransformPoint(systemTransform.TransformPoint(position));
                }

                float half = particle.GetCurrentSize(system) * 0.5f * sizeScale;
                if (half <= 0f)
                    continue;

                var particleColor = (Color)particle.GetCurrentColor(system) * tint;
                if (particleColor.a <= 0f)
                    continue;

                // Flat in the canvas plane and spun about it. A billboard has nothing to face here - the UI
                // is already square to the camera - so the only rotation that means anything is this one.
                float radians = particle.rotation * Mathf.Deg2Rad;
                float cos = Mathf.Cos(radians);
                float sin = Mathf.Sin(radians);
                var across = new Vector2(cos, sin) * half;
                var up = new Vector2(-sin, cos) * half;

                var center = new Vector2(position.x, position.y);
                var color32 = (Color32)particleColor;

                int start = vh.currentVertCount;
                vh.AddVert(center - across - up, color32, new Vector2(0f, 0f));
                vh.AddVert(center - across + up, color32, new Vector2(0f, 1f));
                vh.AddVert(center + across + up, color32, new Vector2(1f, 1f));
                vh.AddVert(center + across - up, color32, new Vector2(1f, 0f));

                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start + 2, start + 3, start);
            }
        }
    }
}
