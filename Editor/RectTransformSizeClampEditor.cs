using UnityEditor;
using UnityEngine;

namespace FlappyTemplate.Editor
{
    // Draws each limit the way LayoutElement does - a checkbox that switches the limit on, then the
    // value - so that 0 is an ordinary size rather than the off switch.
    [CustomEditor(typeof(RectTransformSizeClamp))]
    [CanEditMultipleObjects]
    public class RectTransformSizeClampEditor : UnityEditor.Editor
    {
        private const float ToggleWidth = 16f;
        private const float PercentWidth = 26f;
        private const float Gap = 2f;

        private static readonly GUIContent PercentContent = new GUIContent("%", "Read this limit as a percentage of the parent's size on the same axis - 100 is the full parent - rather than in the units above.");

        private SerializedProperty units;
        private SerializedProperty useMinWidth;
        private SerializedProperty minWidth;
        private SerializedProperty minWidthIsPercent;
        private SerializedProperty useMinHeight;
        private SerializedProperty minHeight;
        private SerializedProperty minHeightIsPercent;
        private SerializedProperty useMaxWidth;
        private SerializedProperty maxWidth;
        private SerializedProperty maxWidthIsPercent;
        private SerializedProperty useMaxHeight;
        private SerializedProperty maxHeight;
        private SerializedProperty maxHeightIsPercent;

        void OnEnable()
        {
            units = serializedObject.FindProperty("units");
            useMinWidth = serializedObject.FindProperty("useMinWidth");
            minWidth = serializedObject.FindProperty("minWidth");
            minWidthIsPercent = serializedObject.FindProperty("minWidthIsPercent");
            useMinHeight = serializedObject.FindProperty("useMinHeight");
            minHeight = serializedObject.FindProperty("minHeight");
            minHeightIsPercent = serializedObject.FindProperty("minHeightIsPercent");
            useMaxWidth = serializedObject.FindProperty("useMaxWidth");
            maxWidth = serializedObject.FindProperty("maxWidth");
            maxWidthIsPercent = serializedObject.FindProperty("maxWidthIsPercent");
            useMaxHeight = serializedObject.FindProperty("useMaxHeight");
            maxHeight = serializedObject.FindProperty("maxHeight");
            maxHeightIsPercent = serializedObject.FindProperty("maxHeightIsPercent");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(units);
            EditorGUILayout.Space();

            LimitField(useMinWidth, minWidth, minWidthIsPercent, "Min Width", "Smallest width the rect may take.");
            LimitField(useMinHeight, minHeight, minHeightIsPercent, "Min Height", "Smallest height the rect may take.");
            LimitField(useMaxWidth, maxWidth, maxWidthIsPercent, "Max Width", "Largest width the rect may take.");
            LimitField(useMaxHeight, maxHeight, maxHeightIsPercent, "Max Height", "Largest height the rect may take.");

            if (Conflicts(useMinWidth, minWidth, minWidthIsPercent, useMaxWidth, maxWidth, maxWidthIsPercent) ||
                Conflicts(useMinHeight, minHeight, minHeightIsPercent, useMaxHeight, maxHeight, maxHeightIsPercent))
            {
                EditorGUILayout.HelpBox("A minimum above the maximum wins - the rect will sit at the minimum.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Two limits in different units cannot be compared without knowing the parent's size, so only the
        // like-for-like case is worth warning about.
        private static bool Conflicts(SerializedProperty useMin, SerializedProperty min, SerializedProperty minIsPercent,
            SerializedProperty useMax, SerializedProperty max, SerializedProperty maxIsPercent) =>
            useMin.boolValue && useMax.boolValue &&
            minIsPercent.boolValue == maxIsPercent.boolValue &&
            min.floatValue > max.floatValue;

        private static void LimitField(SerializedProperty use, SerializedProperty value, SerializedProperty isPercent, string label, string tooltip)
        {
            var position = EditorGUILayout.GetControlRect();
            var content = new GUIContent(label, tooltip);

            // BeginProperty on the value so the prefab-override bar and right-click revert cover the row.
            EditorGUI.BeginProperty(position, content, value);

            var fieldPosition = EditorGUI.PrefixLabel(position, content);

            // PrefixLabel has already accounted for the indent; leaving it on would shift both controls
            // a second time.
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var togglePosition = fieldPosition;
            togglePosition.width = ToggleWidth;
            EditorGUI.PropertyField(togglePosition, use, GUIContent.none);

            var percentPosition = fieldPosition;
            percentPosition.xMin = percentPosition.xMax - PercentWidth;

            var valuePosition = fieldPosition;
            valuePosition.xMin += ToggleWidth;
            valuePosition.xMax = percentPosition.xMin - Gap;

            using (new EditorGUI.DisabledScope(!use.boolValue))
            {
                EditorGUI.PropertyField(valuePosition, value, GUIContent.none);
                PercentToggle(percentPosition, isPercent);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        // A pressed-in "%" reads as a unit on the number beside it, which a second bare checkbox next to
        // the enable one would not.
        private static void PercentToggle(Rect position, SerializedProperty isPercent)
        {
            EditorGUI.BeginProperty(position, PercentContent, isPercent);
            EditorGUI.BeginChangeCheck();

            bool pressed = GUI.Toggle(position, isPercent.boolValue, PercentContent, EditorStyles.miniButton);

            if (EditorGUI.EndChangeCheck())
                isPercent.boolValue = pressed;

            EditorGUI.EndProperty();
        }
    }
}
