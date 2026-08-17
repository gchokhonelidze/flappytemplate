using UnityEditor;
using UnityEngine;

namespace FlappyTemplate.Editor
{
    // The rest pose earns its place on the inspector - part Power and Relative mode both measure from it,
    // and a prefab instance is the one case where it needs correcting by hand - but it is bookkeeping, not
    // something you set while building a scene. Left to the default inspector it lands in the middle of the
    // component, because C# declares the base class's fields before each constraint's own aim and offsets
    // and Unity draws them in that order.
    //
    // So this draws every field in declaration order, holds those two back, and puts them at the bottom in a
    // foldout with the two buttons that maintain them. Everything else is the default inspector, which keeps
    // the tooltips and the headers the fields already carry.
    [CustomEditor(typeof(TransformConstraint), true)]
    [CanEditMultipleObjects]
    public class TransformConstraintEditor : UnityEditor.Editor
    {
        private const string RestPositionPath = "restLocalPosition";
        private const string RestEulerPath = "restLocalEuler";

        private static readonly GUIContent RestContent = new GUIContent("Rest pose", "The object's authored local pose. Power blends from it and Relative measures its gap from it, so it is the one thing to fix on a prefab instance placed somewhere other than where the prefab was authored.");
        private static readonly GUIContent CaptureContent = new GUIContent("Capture", "Take the object's current pose as its rest pose. Press it after moving the object by hand.");
        private static readonly GUIContent RestoreContent = new GUIContent("Put Object Back", "Move the object onto its rest pose. With Follow on the constraint will pull it off again on the next frame.");

        // Static, so the foldout stays as you left it while you click between constrained objects.
        private static bool showRest;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                // Only the top level is walked; PropertyField below draws each field's children itself.
                enterChildren = false;

                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(iterator);
                    continue;
                }

                // Skipping the field skips the [Header] on it too, which is why no empty "Rest pose"
                // heading is left behind here.
                if (iterator.propertyPath == RestPositionPath || iterator.propertyPath == RestEulerPath)
                    continue;

                EditorGUILayout.PropertyField(iterator, true);
            }

            DrawRestPose();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRestPose()
        {
            EditorGUILayout.Space(6f);
            showRest = EditorGUILayout.Foldout(showRest, RestContent, true);
            if (!showRest)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(RestPositionPath));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(RestEulerPath));

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(GUIContent.none, GUILayout.Width(EditorGUIUtility.labelWidth - 14f));

                    if (GUILayout.Button(CaptureContent))
                        Capture();
                    if (GUILayout.Button(RestoreContent))
                        Restore();
                }
            }
        }

        private void Capture()
        {
            foreach (var target in targets)
            {
                if (target is not TransformConstraint constraint)
                    continue;

                Undo.RecordObject(constraint, "Capture Rest Pose");
                constraint.CaptureRest();
            }

            // The properties on screen were read before the buttons ran, so a plain repaint would show the
            // old numbers until the next click.
            serializedObject.Update();
        }

        private void Restore()
        {
            foreach (var target in targets)
            {
                if (target is not TransformConstraint constraint)
                    continue;

                // The transform is what moves, so that is what the undo has to hold.
                Undo.RecordObject(constraint.transform, "Reset To Rest Pose");
                constraint.ResetToRest();
            }
        }
    }
}
