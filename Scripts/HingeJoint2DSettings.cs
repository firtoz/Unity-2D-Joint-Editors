using UnityEngine;

#if UNITY_EDITOR
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
#endif

public class HingeJoint2DSettings : Joint2DSettings {
    public bool showRadiusHandles = false;
    public bool showAngleLimits = true;

    public enum AnchorPriority {
        Main,
        Connected,
        Both
    }

    public AnchorPriority anchorPriority = AnchorPriority.Main;

#if UNITY_EDITOR
    public new void OnDrawGizmos() {
        base.OnDrawGizmos();
        AnchoredJoint2D joint2D = attachedJoint as AnchoredJoint2D;
        if (joint2D == null)
        {
            return;
        }


        Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
        Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
        Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);

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