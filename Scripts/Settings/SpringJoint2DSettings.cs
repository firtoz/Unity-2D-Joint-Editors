using UnityEngine;

public class SpringJoint2DSettings : Joint2DSettings
{
    public enum AnchorPriority
    {
        Main,
        Connected
    }

    public AnchorPriority anchorPriority = AnchorPriority.Connected;

    public override bool IsValidType() {
        return attachedJoint is SpringJoint2D;
    }
}
