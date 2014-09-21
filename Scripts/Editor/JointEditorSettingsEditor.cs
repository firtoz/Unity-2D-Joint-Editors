using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (JointEditorSettings))]
internal class JointEditorSettingsEditor : Editor {
    private static readonly PersistentFoldoutHelper FoldoutHelper = new PersistentFoldoutHelper("2DJointEditors.JointEditorSettings");

    public float anchorScale = 0.5f;
    public float anchorDisplayScale = 1.75f;
    public float angleLimitRadius = 1.5f;
    public float lockButtonScale = 0.5f;

    public override void OnInspectorGUI() {
        EditorGUI.BeginChangeCheck();

        FoldoutHelper.Foldout("textures", new GUIContent("Textures", "Custom Textures"), () => {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("connectedAnchorTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mainAnchorTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lockedAnchorTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hotAnchorTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("offsetTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lockButtonTexture"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unlockButtonTexture"));
        });

        FoldoutHelper.Foldout("sizes", new GUIContent("Sizes", "Sizes for visual editor components"), () =>
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("anchorScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("anchorDisplayScale"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lockButtonScale"));
        });

        if (EditorGUI.EndChangeCheck()) {
            serializedObject.ApplyModifiedProperties();
        }

        DrawDefaultInspector();
    }
}