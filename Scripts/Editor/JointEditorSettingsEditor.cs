using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (JointEditorSettings))]
internal class JointEditorSettingsEditor : Editor {
    private static readonly FoldoutHelper FoldoutHelper = new FoldoutHelper();

    private static readonly GUIContent CustomTexturesLabel = new GUIContent("Textures",
        "Textures for visual editor components.");

    private static readonly GUIContent SizesLabel = new GUIContent("Sizes", "Sizes for visual editor components");

    private static readonly GUIContent ColorsLabel = new GUIContent("Colors",
        "Color settings for the visual editor components");

    private static readonly GUIContent HingeJoint2DLabel = new GUIContent("Hinge Joint 2D",
        "Settings for hinge joint 2d component editors");

    private static readonly GUIContent SliderJoint2DLabel = new GUIContent("Slider Joint 2D",
        "Settings for slide joint 2d component editors");

    public override void OnInspectorGUI() {
        EditorGUI.BeginChangeCheck();

        FoldoutHelper.Foldout("textures", CustomTexturesLabel, () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("connectedAnchorTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mainAnchorTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lockedAnchorTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hotAnchorTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetTexture"));
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
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ringDisplayMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mainDiscColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("connectedDiscColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("angleAreaColor"));
            });

        FoldoutHelper.Foldout("sliderjoint2d", SliderJoint2DLabel, () => {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minLimitColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxLimitColor"));
            });

        if (EditorGUI.EndChangeCheck()) {
            serializedObject.ApplyModifiedProperties();
        }
    }
}