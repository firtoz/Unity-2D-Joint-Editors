#if UNITY_EDITOR
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;
#endif

public class DistanceJoint2DSettings : Joint2DSettings
{
#if UNITY_EDITOR
    public new void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        AnchoredJoint2D joint2D = attachedJoint as AnchoredJoint2D;
        if (joint2D == null)
        {
            return;
        }


        Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
        Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);

        using (new HandleColor(editorSettings.mainDiscColor))
        {
            Vector2 mainPosition = GetTargetPositionWithOffset(joint2D, JointHelpers.AnchorBias.Main);
            Handles.DrawLine(mainAnchorPosition, mainPosition);
        }
        if (joint2D.connectedBody)
        {
            using (new HandleColor(editorSettings.connectedDiscColor))
            {
                Vector2 connectedPosition = GetTargetPositionWithOffset(joint2D, JointHelpers.AnchorBias.Connected);
                Handles.DrawLine(connectedAnchorPosition, connectedPosition);
            }
        }
    }
#endif
}
