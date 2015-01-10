using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof (Joint2DTarget))]
public class Joint2DTargetEditor : Editor {
    private readonly Dictionary<Joint2DTarget, Dictionary<Joint2D, Joint2DEditorBase>> editorMaps =
        new Dictionary<Joint2DTarget, Dictionary<Joint2D, Joint2DEditorBase>>();

    public void OnDisable() {
        foreach (var editorMap in editorMaps.Values) {
            foreach (var editor in editorMap.Values) {
                DestroyImmediate(editor);
            }
        }
    }

    public void OnSceneGUI() {
        var jointTarget = target as Joint2DTarget;
        if (jointTarget) {
            if (!editorMaps.ContainsKey(jointTarget)) {
                editorMaps[jointTarget] = new Dictionary<Joint2D, Joint2DEditorBase>();
            }

            var editors = editorMaps[jointTarget];

            var unseenJoints = new HashSet<Joint2D>(editors.Keys);

            IEnumerable<Joint2D> jointsToEdit = jointTarget.attachedJoints
                .Where(attachedJoint => !Selection.Contains(attachedJoint.gameObject));

            foreach (Joint2D attachedJoint in jointsToEdit) {
                unseenJoints.Remove(attachedJoint);

                if (!editors.ContainsKey(attachedJoint)) {
                    editors[attachedJoint] = (Joint2DEditorBase) CreateEditor(attachedJoint);
                    editors[attachedJoint].isCreatedByTarget = true;
                }

                var joint2DEditor = editors[attachedJoint];
                joint2DEditor.OnSceneGUI();
            }

            foreach (Joint2D joint2D in unseenJoints) {
                DestroyImmediate(editors[joint2D]);

                editors.Remove(joint2D);
            }
        }
    }
}