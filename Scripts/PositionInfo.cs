using UnityEngine;

public class PositionInfo
{
    public enum Change
    {
        NoChange,
        MainChanged,
        ConnectedChanged,
        BothChanged
    }

    private readonly Vector2 worldAnchor, worldConnectedAnchor;
    public PositionInfo(HingeJoint2D hingeJoint2D)
    {
        worldAnchor = JointHelpers.GetAnchorPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);
        worldConnectedAnchor = JointHelpers.GetAnchorPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);
    }

    public Change Changed(HingeJoint2D hingeJoint2D)
    {
        Change result = Change.NoChange;

        Vector2 main = JointHelpers.GetAnchorPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);
        Vector2 connected = JointHelpers.GetAnchorPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);

        bool mainChanged = Vector3.Distance(worldAnchor, main) > JointHelpers.AnchorEpsilon;
        bool connectedChanged = Vector3.Distance(worldConnectedAnchor, connected) > JointHelpers.AnchorEpsilon;

        if (mainChanged)
        {
            result = connectedChanged ? Change.BothChanged : Change.MainChanged;
        }
        else if (connectedChanged)
        {
            result = Change.ConnectedChanged;
        }
        return result;
    }
}