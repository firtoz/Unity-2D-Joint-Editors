using UnityEngine;

public class DistanceJoint2DSettings : Joint2DSettingsBase {
    public enum AnchorPriority {
        Main,
        Connected
    }

    public AnchorPriority anchorPriority = AnchorPriority.Connected;

    public override bool IsValidType() {
        return attachedJoint is DistanceJoint2D;
    }
}