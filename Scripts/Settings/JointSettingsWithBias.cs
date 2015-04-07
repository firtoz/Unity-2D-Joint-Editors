public abstract class JointSettingsWithBias : Joint2DSettingsBase {
    public enum AnchorPriority {
        Main,
        Connected
    }

    public AnchorPriority anchorPriority = AnchorPriority.Connected;
}