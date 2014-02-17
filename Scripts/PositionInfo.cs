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
    public PositionInfo(AnchoredJoint2D joint2D)
    {
        worldAnchor = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
        worldConnectedAnchor = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
    }

    public Change Changed(AnchoredJoint2D joint2D)
    {
        Change result = Change.NoChange;

        Vector2 main = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
        Vector2 connected = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);

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