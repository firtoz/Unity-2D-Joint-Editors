using UnityEngine;

public class HingeJoint2DSettings : Joint2DSettings {
    public bool showRadiusHandles = false;
    public bool showAngleLimits = true;

    public enum AnchorPriority {
        Main,
        Connected,
        Both
    }

    public AnchorPriority anchorPriority = AnchorPriority.Main;

    public override bool IsValidType()
    {
        return attachedJoint is HingeJoint2D;
    }

#if UNITY_EDITOR

    public new void OnDrawGizmos() {
        base.OnDrawGizmos();
        HingeJoint2D hingeJoint2D = attachedJoint as HingeJoint2D;
        if (hingeJoint2D == null)
        {
            return;
        }

        DrawAnchorLines();
    }
#endif

}