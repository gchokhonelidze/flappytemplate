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

        private SerializedProperty units;
        private SerializedProperty useMinWidth;
        private SerializedProperty minWidth;
        private SerializedProperty useMinHeight;
        private SerializedProperty minHeight;
        private SerializedProperty useMaxWidth;
        private SerializedProperty maxWidth;
        private SerializedProperty useMaxHeight;
        private SerializedProperty maxHeight;

        void OnEnable()
        {
            units = serializedObject.FindProperty("units");
            useMinWidth = serializedObject.FindProperty("useMinWidth");
            minWidth = serializedObject.FindProperty("minWidth");
            useMinHeight = serializedObject.FindProperty("useMinHeight");
            minHeight = serializedObject.FindProperty("minHeight");
            useMaxWidth = serializedObject.FindProperty("useMaxWidth");
            maxWidth = serializedObject.FindProperty("maxWidth");
            useMaxHeight = serializedObject.FindProperty("useMaxHeight");
            maxHeight = serializedObject.FindProperty("maxHeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(units);
            EditorGUILayout.Space();

            LimitField(useMinWidth, minWidth, "Min Width", "Smallest width the rect may take.");
            LimitField(useMinHeight, minHeight, "Min Height", "Smallest height the rect may take.");
            LimitField(useMaxWidth, maxWidth, "Max Width", "Largest width the rect may take.");
            LimitField(useMaxHeight, maxHeight, "Max Height", "Largest height the rect may take.");

            if (Conflicts(useMinWidth, minWidth, useMaxWidth, maxWidth) || Conflicts(useMinHeight, minHeight, useMaxHeight, maxHeight))
            {
                EditorGUILayout.HelpBox("A minimum above the maximum wins - the rect will sit at the minimum.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static bool Conflicts(SerializedProperty useMin, SerializedProperty min, SerializedProperty useMax, SerializedProperty max) =>
            useMin.boolValue && useMax.boolValue && min.floatValue > max.floatValue;

        private static void LimitField(SerializedProperty use, SerializedProperty value, string label, string tooltip)
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

            var valuePosition = fieldPosition;
            valuePosition.xMin += ToggleWidth;
            using (new EditorGUI.DisabledScope(!use.boolValue))
            {
                EditorGUI.PropertyField(valuePosition, value, GUIContent.none);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
