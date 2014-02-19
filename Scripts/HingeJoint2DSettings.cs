using System;
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


        Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);
        Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);
        Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);

        using (new HandleColor(editorSettings.mainDiscColor))
        {
            Vector2 mainPosition = GetTargetPositionWithOffset(hingeJoint2D, JointHelpers.AnchorBias.Main);
            Handles.DrawLine(mainAnchorPosition, mainPosition);
        }
        if (hingeJoint2D.connectedBody)
        {
            using (new HandleColor(editorSettings.connectedDiscColor))
            {
                Vector2 connectedPosition = GetTargetPositionWithOffset(hingeJoint2D, JointHelpers.AnchorBias.Connected);
                Handles.DrawLine(connectedAnchorPosition, connectedPosition);
            }
        }
    }
#endif

}