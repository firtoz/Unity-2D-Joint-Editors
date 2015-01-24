using UnityEngine;

public class DistanceJoint2DSettings : JointWithDistanceSettings {
    public override bool IsValidType() {
        return attachedJoint is DistanceJoint2D;
    }
}