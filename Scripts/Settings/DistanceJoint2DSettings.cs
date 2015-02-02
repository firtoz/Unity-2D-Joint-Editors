using UnityEngine;

public class DistanceJoint2DSettings : JointSettingsWithBias {
    public override bool IsValidType() {
        return attachedJoint is DistanceJoint2D;
    }
}