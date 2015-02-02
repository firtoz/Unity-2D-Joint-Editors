using UnityEngine;

public class SliderJoint2DSettings : JointSettingsWithBias
{
    public override void Setup(Joint2D joint2D) {
        base.Setup(joint2D);

        anchorPriority = AnchorPriority.Main;
    }

    public override bool IsValidType() {
        return attachedJoint is SliderJoint2D;
    }
}