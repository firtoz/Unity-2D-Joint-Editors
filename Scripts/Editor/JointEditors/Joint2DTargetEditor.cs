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

    public override void OnInspectorGUI() {
        var guiEnabled = GUI.enabled;
        GUI.enabled = true;
        EditorGUILayout.LabelField("This component is used by the 2D Joint Editors plugin");
        EditorGUILayout.LabelField(" to display connected joints. If you would like to ");
        EditorGUILayout.LabelField(" disable this feature, please edit the settings ");
        EditorGUILayout.LabelField(" file which can be found in Joint2DEditor/Data/settings.");
        EditorGUILayout.LabelField("The option can be found at the bottom as 'Show Connected Joints'");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("This component will automatically be removed ");
        EditorGUILayout.LabelField(" when the scene is being built.");
        GUI.enabled = guiEnabled;
    }

    public void OnSceneGUI() {
        var jointTarget = target as Joint2DTarget;
        if (jointTarget) {
            if (!editorMaps.ContainsKey(jointTarget)) {
                editorMaps[jointTarget] = new Dictionary<Joint2D, Joint2DEditorBase>();
            }

            var editors = editorMaps[jointTarget];

            var unseenJoints = new HashSet<Joint2D>(editors.Keys);

            var jointsToEdit = jointTarget.attachedJoints
                .Where(attachedJoint => !Selection.Contains(attachedJoint.gameObject));

            foreach (var attachedJoint in jointsToEdit) {
                unseenJoints.Remove(attachedJoint);

                if (!editors.ContainsKey(attachedJoint)) {
                    editors[attachedJoint] = (Joint2DEditorBase) CreateEditor(attachedJoint);
                    editors[attachedJoint].isCreatedByTarget = true;
                }

                var joint2DEditor = editors[attachedJoint];
                joint2DEditor.OnSceneGUI();
            }

            foreach (var joint2D in unseenJoints) {
                DestroyImmediate(editors[joint2D]);

                editors.Remove(joint2D);
            }
        }
    }
}