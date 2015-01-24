using UnityEngine;

public abstract class JointWithDistanceSettings : Joint2DSettingsBase {
    public enum AnchorPriority {
        Main,
        Connected
    }

    public AnchorPriority anchorPriority = AnchorPriority.Connected;
}