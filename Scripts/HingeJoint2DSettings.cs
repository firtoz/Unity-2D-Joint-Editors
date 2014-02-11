public class HingeJoint2DSettings : Joint2DSettings {
    public bool showRadiusHandles = false;
    public bool showAngleLimits = true;

    public enum AnchorPriority {
        Main,
        Connected,
        Both
    }

    public AnchorPriority anchorPriority = AnchorPriority.Main;

}