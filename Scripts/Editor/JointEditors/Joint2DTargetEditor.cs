using System;
using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers.DisposableEditorGUI;
using toxicFork.GUIHelpers.DisposableEditorGUILayout;
using UnityEditor;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof (Joint2DTarget))]
public class Joint2DTargetEditor : Editor {
    private readonly Dictionary<Joint2DTarget, Dictionary<Joint2D, Joint2DEditorBase>> editorMaps =
        new Dictionary<Joint2DTarget, Dictionary<Joint2D, Joint2DEditorBase>>();

    public static Type GetEditorType(AnchoredJoint2D joint2D) {
        Type editorType = null;

        if (joint2D is HingeJoint2D) {
            editorType = typeof (HingeJoint2DEditor);
        } else if (joint2D is DistanceJoint2D) {
            editorType = typeof (DistanceJoint2DEditor);
        } else if (joint2D is SliderJoint2D) {
            editorType = typeof (SliderJoint2DEditor);
        } else if (joint2D is SpringJoint2D) {
            editorType = typeof (SpringJoint2DEditor);
        } else if (joint2D is WheelJoint2D) {
            editorType = typeof (WheelJoint2DEditor);
        }
        return editorType;
    }

    public void OnEnable() {
        EditorApplication.update += Update;
    }

    private void Update() {
        foreach (var jointTarget in 
            targets.Select(targ => targ as Joint2DTarget)
                   .Where(jointTarget => jointTarget)) {
            if (!editorMaps.ContainsKey(jointTarget)) {
                editorMaps[jointTarget] = new Dictionary<Joint2D, Joint2DEditorBase>();
            }

            var editors = editorMaps[jointTarget];

            var unseenJoints = new HashSet<Joint2D>(editors.Keys);

            var jointsToEdit = jointTarget.attachedJoints
                                          .Where(
                                              attachedJoint =>
                                                  attachedJoint && !Selection.Contains(attachedJoint.gameObject));

            foreach (var attachedJoint in jointsToEdit) {
                unseenJoints.Remove(attachedJoint);

                if (editors.ContainsKey(attachedJoint)) {
                    continue;
                }

                editors[attachedJoint] = (Joint2DEditorBase) CreateEditor(attachedJoint);
                editors[attachedJoint].isCreatedByTarget = true;
            }

            foreach (var joint2D in unseenJoints) {
                DestroyImmediate(editors[joint2D]);

                editors.Remove(joint2D);
            }
        }
    }

    public void OnDisable() {
        EditorApplication.update -= Update;

        foreach (var editor in editorMaps.Values
                                         .SelectMany(editorMap => editorMap.Values)) {
            DestroyImmediate(editor);
        }
    }

    public override void OnInspectorGUI() {
        var guiEnabled = GUI.enabled;
        GUI.enabled = true;

        EditorGUILayout.HelpBox("This component is used by the 2D Joint Editors plugin  to display connected joints. " +
                                "If you would like to disable this feature, please edit the settings file which can be found in Joint2DEditor/Data/settings. " +
                                "The option can be found at the bottom as 'Show Connected Joints'. \n\n" +
                                "This component will automatically be removed when the scene is being built, therefore it will not cause any impact on the built game.", MessageType.Info, true);

        foreach (var editorMap in editorMaps) {
            var editors = editorMap.Value;
            foreach (var map in editors) {
                var originalEditor = map.Value;

                bool isToggled;
                if (!toggleEditors.TryGetValue(originalEditor, out isToggled)) {
                    isToggled = false;
                }

                EditorGUI.BeginChangeCheck();


                using (new Horizontal()) {
                    var jointOwner = map.Key;

                    isToggled = EditorGUILayout.Foldout(isToggled, jointOwner.GetType().Name);

                    if (GUILayout.Button("Select"))
                    {
                        Selection.activeObject = jointOwner;
                        return;
                    }
                }

                if (EditorGUI.EndChangeCheck()) {
                    toggleEditors[originalEditor] = isToggled;
                }

                if (isToggled) {
                    using (new Indent()) {
                        originalEditor.OnInspectorGUI();
                    }
                }
            }
        }

        GUI.enabled = guiEnabled;
    }

    private readonly Dictionary<Editor, bool> toggleEditors = new Dictionary<Editor, bool>();

    public void OnSceneGUI() {
        var jointTarget = target as Joint2DTarget;
        if (!jointTarget) {
            return;
        }

        if (!editorMaps.ContainsKey(jointTarget)) {
            editorMaps[jointTarget] = new Dictionary<Joint2D, Joint2DEditorBase>();
        }

        var editors = editorMaps[jointTarget];

        var jointsToEdit = jointTarget.attachedJoints
                                      .Where(
                                          attachedJoint =>
                                              attachedJoint && !Selection.Contains(attachedJoint.gameObject));

        foreach (var attachedJoint in jointsToEdit) {
            if (!editors.ContainsKey(attachedJoint)) {
                editors[attachedJoint] = (Joint2DEditorBase) CreateEditor(attachedJoint);
                editors[attachedJoint].isCreatedByTarget = true;
            }

            var joint2DEditor = editors[attachedJoint];
            EditorGUI.BeginChangeCheck();
            joint2DEditor.OnSceneGUI();
            if (EditorGUI.EndChangeCheck()) {
                Repaint();
                joint2DEditor.Repaint();                
            }
        }
    }
}