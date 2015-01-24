using UnityEngine;

public class SpringJoint2DSettings : JointWithDistanceSettings {
    public override bool IsValidType() {
        return attachedJoint is SpringJoint2D;
    }
}