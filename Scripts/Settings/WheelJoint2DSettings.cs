using UnityEngine;

public class WheelJoint2DSettings : Joint2DSettingsBase {
    public override bool IsValidType() {
        return attachedJoint is WheelJoint2D;
    }
}