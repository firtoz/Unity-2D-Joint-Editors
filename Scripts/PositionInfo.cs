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

    private Vector2 worldAnchor, worldConnectedAnchor;
    public PositionInfo(AnchoredJoint2D joint2D)
    {
        worldAnchor = JointHelpers.GetMainAnchorPosition(joint2D);
        worldConnectedAnchor = JointHelpers.GetConnectedAnchorPosition(joint2D);
    }

    public void Update(AnchoredJoint2D joint2D)
    {
        worldAnchor = JointHelpers.GetMainAnchorPosition(joint2D);
        worldConnectedAnchor = JointHelpers.GetConnectedAnchorPosition(joint2D);
    }

    public Change Changed(AnchoredJoint2D joint2D)
    {
        Change result = Change.NoChange;

        Vector2 main = JointHelpers.GetMainAnchorPosition(joint2D);
        Vector2 connected = JointHelpers.GetConnectedAnchorPosition(joint2D);

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