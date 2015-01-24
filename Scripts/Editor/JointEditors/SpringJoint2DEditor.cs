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

    protected override JointWithDistanceSettings GetSettings(SpringJoint2D jointWithDistance) {
        return SettingsHelper.GetOrCreate<SpringJoint2DSettings>(jointWithDistance);
    }
}