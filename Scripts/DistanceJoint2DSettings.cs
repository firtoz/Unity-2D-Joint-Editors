using UnityEngine;

public class DistanceJoint2DSettings : Joint2DSettings
{
    public override bool IsValidType() {
        return attachedJoint is DistanceJoint2D;
    }
}
