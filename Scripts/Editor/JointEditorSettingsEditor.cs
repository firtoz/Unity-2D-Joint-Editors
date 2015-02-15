using toxicFork.GUIHelpers.DisposableEditor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (JointEditorSettings))]
internal class JointEditorSettingsEditor : Editor {
    private static readonly PersistentFoldoutHelper FoldoutHelper =
        new PersistentFoldoutHelper("2DJointEditors.JointEditorSettings");

    private static readonly GUIContent CustomTexturesLabel =
        new GUIContent("Textures", "Textures for visual editor components.");

    private static readonly GUIContent SizesLabel =
        new GUIContent("Sizes", "Sizes for visual editor components");

    private static readonly GUIContent ColorsLabel =
        new GUIContent("Colors", "Color settings for the visual editor components");

    private static readonly GUIContent HingeJoint2DLabel =
        new GUIContent("Hinge Joint 2D", "Settings for hinge joint 2d component editors");

    private static readonly GUIContent SliderJoint2DLabel =
        new GUIContent("Slider Joint 2D", "Settings for slide joint 2d component editors");

    private static readonly GUIContent ConnectedJointsLabel =
        new GUIContent("Connected Joints", "Settings for the display of connected joints");

    private static readonly GUIContent TogglesLabel =
        new GUIContent("Misc", "Settings for the display of additional features");

    public override void OnInspectorGUI() {
        if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed") {
            Repaint();
        }

        serializedObject.UpdateIfDirtyOrScript();

        var disableEverythingProperty = serializedObject.FindProperty("disableEverything");

        if (!disableEverythingProperty.boolValue) {
            EditorGUI.BeginChangeCheck();

            FoldoutHelper.Foldout("textures", CustomTexturesLabel, () => {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("connectedAnchorTexture"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mainAnchorTexture"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lockedAnchorTexture"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hotAnchorTexture"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lockButtonTexture"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unlockButtonTexture"));
            });

            FoldoutHelper.Foldout("sizes", SizesLabel, () => {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("anchorScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("anchorDisplayScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lockButtonScale"));
            });

            FoldoutHelper.Foldout("colors", ColorsLabel, () => {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("anchorHoverColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("anchorsToMainBodyColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("anchorsToConnectedBodyColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("incorrectLimitsColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("correctLimitsColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("angleWidgetColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverAngleColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("activeAngleColor"));
            });

            FoldoutHelper.Foldout("hingejoint2d", HingeJoint2DLabel, () => {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("angleLimitRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("angleHandleSize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("snapAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("limitsAreaColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("incorrectLimitsArea"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ringDisplayMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mainRingColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("connectedRingColor"));
            });

            FoldoutHelper.Foldout("sliderjoint2d", SliderJoint2DLabel, () => {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minLimitColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxLimitColor"));
            });


            FoldoutHelper.Foldout("connectedJoints", ConnectedJointsLabel, () => {
                SerializedProperty showConnectedJointsProperty = serializedObject.FindProperty("showConnectedJoints");
                EditorGUILayout.PropertyField(showConnectedJointsProperty,
                    new GUIContent("Show", showConnectedJointsProperty.tooltip));
                SerializedProperty connectedJointTransparencyProperty =
                    serializedObject.FindProperty("connectedJointTransparency");
                EditorGUILayout.PropertyField(connectedJointTransparencyProperty,
                    new GUIContent("Opacity", connectedJointTransparencyProperty.tooltip));
            });

            FoldoutHelper.Foldout("toggles", TogglesLabel,
                () => EditorGUILayout.PropertyField(serializedObject.FindProperty("drawLinesToBodies")));

            if (EditorGUI.EndChangeCheck()) {
                using (new Modification("Inspector", target)) {
                    serializedObject.ApplyModifiedProperties();
                }
            }
            if (GUILayout.Button("Disable 2D Joint Editors")) {
                if (EditorUtility.DisplayDialog("Disable 2D Joint Editors",
                    "Are you sure about disabling all 2D Joint Editors features? \n\nThis will wipe your joint-specific settings forever! \n\nThis should ONLY be used if you would like to uninstall the package!",
                    "Yes", "No")) {
                    disableEverythingProperty.boolValue = true;

                    serializedObject.ApplyModifiedProperties();
                    
                    Joint2DSettingsBase[] editorSettings = Resources.FindObjectsOfTypeAll<Joint2DSettingsBase>();
                    foreach (Joint2DSettingsBase jointEditorSettings in editorSettings) {
                        DestroyImmediate(jointEditorSettings, true);
                    }

                    Joint2DTarget[] joint2DTargets = Resources.FindObjectsOfTypeAll<Joint2DTarget>();
                    foreach (Joint2DTarget joint2DTarget in joint2DTargets) {
                        DestroyImmediate(joint2DTarget, true);
                    }
                }
            }
        }
        else {
            if (GUILayout.Button("Enable 2D Joint Editors")) {
                disableEverythingProperty.boolValue = false;

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}