using UnityEngine;

public class HingeJoint2DSettings : Joint2DSettingsBase {
    public bool showRadiusHandles = false;
    public bool showAngleLimits = true;
    public bool showDiscs = true;

    public enum AnchorPriority {
        Main,
        Connected,
        Both
    }

    public AnchorPriority anchorPriority = AnchorPriority.Main;

    public float mainAngleOffset = 0;
    public float connectedAngleOffset = 0;

    public override bool IsValidType() {
        return attachedJoint is HingeJoint2D;
    }
}