using UnityEngine;

public class SliderJoint2DSettings : Joint2DSettingsBase {
    public enum AnchorPriority {
        Main,
        Connected
    }

    public AnchorPriority anchorPriority = AnchorPriority.Main;

    public override bool IsValidType() {
        return attachedJoint is SliderJoint2D;
    }
}