using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace FlappyTemplate.Editor
{
    // The plain inspector would be right, near enough - the fields are ordinary and there is no shape to lay
    // them out on the way a RoundedBox has. What it would not do is say anything about the one case where
    // the border quietly stops being the shape it is supposed to be: a sprite whose silhouette cannot be
    // traced falls back to the outline the importer generated, which on a full-rect sprite is a rectangle.
    // That is not a failure anybody would guess at from the result, so it is called out here, and only while
    // a border is actually asked for.
    [CustomEditor(typeof(SpriteGradient))]
    [CanEditMultipleObjects]
    public class SpriteGradientEditor : GraphicEditor
    {
        private SerializedProperty sprite;
        private SerializedProperty preserveAspect;
        private SerializedProperty fillColor;
        private SerializedProperty fillGradientMode;
        private SerializedProperty fillGradient;
        private SerializedProperty fillGradientAngle;
        private SerializedProperty borderSize;
        private SerializedProperty borderColor;
        private SerializedProperty borderPlacement;
        private SerializedProperty borderGradientMode;
        private SerializedProperty borderGradient;
        private SerializedProperty borderGradientAngle;
        private SerializedProperty alphaThreshold;
        private SerializedProperty outlineSimplify;
        private SerializedProperty edgeSoftness;

        protected override void OnEnable()
        {
            base.OnEnable();

            sprite = serializedObject.FindProperty("sprite");
            preserveAspect = serializedObject.FindProperty("preserveAspect");
            fillColor = serializedObject.FindProperty("fillColor");
            fillGradientMode = serializedObject.FindProperty("fillGradientMode");
            fillGradient = serializedObject.FindProperty("fillGradient");
            fillGradientAngle = serializedObject.FindProperty("fillGradientAngle");
            borderSize = serializedObject.FindProperty("borderSize");
            borderColor = serializedObject.FindProperty("borderColor");
            borderPlacement = serializedObject.FindProperty("borderPlacement");
            borderGradientMode = serializedObject.FindProperty("borderGradientMode");
            borderGradient = serializedObject.FindProperty("borderGradient");
            borderGradientAngle = serializedObject.FindProperty("borderGradientAngle");
            alphaThreshold = serializedObject.FindProperty("alphaThreshold");
            outlineSimplify = serializedObject.FindProperty("outlineSimplify");
            edgeSoftness = serializedObject.FindProperty("edgeSoftness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(sprite);
            EditorGUILayout.PropertyField(preserveAspect);

            // The standard Graphic block - material, tint, raycast target, maskable - drawn where an Image
            // would draw it, since half of it is what somebody reaches for first.
            AppearanceControlsGUI();
            RaycastControlsGUI();
            MaskableControlsGUI();

            if (GUILayout.Button("Set Native Size"))
            {
                foreach (var target in targets)
                    ((SpriteGradient)target).SetNativeSize();
            }

            Section("Fill");
            EditorGUILayout.PropertyField(fillColor);
            EditorGUILayout.PropertyField(fillGradientMode, new GUIContent("Gradient"));

            var fillMode = (EFillGradient)fillGradientMode.enumValueIndex;
            if (fillMode != EFillGradient.None)
            {
                EditorGUILayout.PropertyField(fillGradient, new GUIContent("Colours"));
                if (fillMode == EFillGradient.Linear)
                    EditorGUILayout.PropertyField(fillGradientAngle, new GUIContent("Angle"));
            }

            Section("Border");
            EditorGUILayout.PropertyField(borderSize, new GUIContent("Size"));

            if (borderSize.floatValue > 0f || borderSize.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(borderPlacement, new GUIContent("Placement"));
                EditorGUILayout.PropertyField(borderGradientMode, new GUIContent("Gradient"));

                var borderMode = (EBorderGradient)borderGradientMode.enumValueIndex;
                if (borderMode == EBorderGradient.None)
                {
                    EditorGUILayout.PropertyField(borderColor, new GUIContent("Colour"));
                }
                else
                {
                    EditorGUILayout.PropertyField(borderGradient, new GUIContent("Colours"));
                    if (borderMode == EBorderGradient.Linear)
                        EditorGUILayout.PropertyField(borderGradientAngle, new GUIContent("Angle"));
                }

                Section("Outline");
                EditorGUILayout.PropertyField(alphaThreshold, new GUIContent("Alpha Threshold"));
                EditorGUILayout.PropertyField(outlineSimplify, new GUIContent("Simplify"));
                EditorGUILayout.PropertyField(edgeSoftness, new GUIContent("Edge Softness"));

                Traceability();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void Section(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        // Only raised once the border is on and a sprite is in, since that is the only time it changes what
        // anybody sees. Read/Write is not what causes it - the pixels are taken off the GPU when the texture
        // will not hand them over - so what is left is the one case that cannot be worked around. The fix is
        // on the asset rather than on this component, so the button goes to it.
        private void Traceability()
        {
            var graphic = (SpriteGradient)target;
            if (graphic.Sprite == null || graphic.CanTraceOutline)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "This sprite's silhouette could not be traced, so the border is following the outline the " +
                "importer generated for it - which is the plain rectangle for a Full Rect sprite. A sprite " +
                "turned on its side inside an atlas is the usual cause: turn off Allow Rotation on the atlas, " +
                "or set the sprite's Mesh Type to Tight for a rougher outline as things stand.",
                MessageType.Warning);

            var texture = graphic.Sprite.texture;
            if (texture != null && GUILayout.Button("Select Texture"))
                Selection.activeObject = texture;
        }
    }
}
