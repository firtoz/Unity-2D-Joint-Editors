using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (DistanceJoint2D))]
[CanEditMultipleObjects]
public class DistanceJoint2DEditor : JointEditorWithDistanceBase<DistanceJoint2D> {
    protected override void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint)
    {
        var distanceJoint2D = joint as DistanceJoint2D;
        if (distanceJoint2D != null)
        {
            menu.AddItem(new GUIContent("Max Distance Only"), distanceJoint2D.maxDistanceOnly, () =>
            {
                EditorHelpers.RecordUndo("Max Distance Only", distanceJoint2D);
                distanceJoint2D.maxDistanceOnly = !distanceJoint2D.maxDistanceOnly;
                EditorUtility.SetDirty(distanceJoint2D);
            });
        }
    }

    public override float GetDistance(DistanceJoint2D joint) {
        return joint.distance;
    }

    public override void SetDistance(DistanceJoint2D joint, float distance) {
        joint.distance = distance;
    }

    protected override JointSettingsWithBias GetSettings(DistanceJoint2D jointWithDistance) {
        return SettingsHelper.GetOrCreate<DistanceJoint2DSettings>(jointWithDistance);
    }
}