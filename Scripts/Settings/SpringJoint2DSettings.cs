using UnityEngine;

public class SpringJoint2DSettings : JointSettingsWithBias {
    public override bool IsValidType() {
        return attachedJoint is SpringJoint2D;
    }
}