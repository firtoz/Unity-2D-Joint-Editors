using System;
using toxicFork.GUIHelpers.DisposableEditor;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (SpringJoint2D))]
[CanEditMultipleObjects]
public class SpringJoint2DEditor : JointEditorWithDistanceBase<SpringJoint2D> {
    public override float GetDistance(SpringJoint2D joint) {
        return joint.distance;
    }

    public override void SetDistance(SpringJoint2D joint, float distance) {
        joint.distance = distance;
    }

    protected override JointSettingsWithBias GetSettings(SpringJoint2D jointWithDistance) {
        return SettingsHelper.GetOrCreate<SpringJoint2DSettings>(jointWithDistance);
    }

    protected override void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint) {
        base.ExtraMenuItems(menu, joint);
        var springJoint2D = joint as SpringJoint2D;

        var mousePosition = Event.current.mousePosition;

        menu.AddItem(new GUIContent("Configure Spring"), false, () => ShowUtility("Configure Spring",
            new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 6),
            delegate(Action close, bool focused) {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DampingRatio"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Frequency"));

                if (EditorGUI.EndChangeCheck()) {
                    using (new Modification("Configure Spring", springJoint2D)) {
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                if (GUILayout.Button("Done") ||
                    (Event.current.isKey &&
                     (Event.current.keyCode == KeyCode.Escape) &&
                     focused)) {
                    close();
                }
            }));
    }
}