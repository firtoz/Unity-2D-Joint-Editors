using UnityEngine;

public class SpringJoint2DSettings : Joint2DSettingsBase {
    public DistanceJoint2DSettings.AnchorPriority anchorPriority = DistanceJoint2DSettings.AnchorPriority.Connected;

    public override bool IsValidType() {
        return attachedJoint is SpringJoint2D;
    }
}