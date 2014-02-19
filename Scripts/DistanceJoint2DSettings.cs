using UnityEngine;

#if UNITY_EDITOR
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
#endif

public class DistanceJoint2DSettings : Joint2DSettings
{
    public override bool IsValidType() {
        return attachedJoint is DistanceJoint2D;
    }

#if UNITY_EDITOR
    public new void OnDrawGizmos() {
        base.OnDrawGizmos();
        if (Selection.Contains(gameObject))
            return;
        DistanceJoint2D joint2D = attachedJoint as DistanceJoint2D;
        if (joint2D == null) {
            return;
        }

        Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
        Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);

        using (new HandleColor(editorSettings.mainDiscColor)) {
            Vector2 mainPosition = GetTargetPositionWithOffset(joint2D, JointHelpers.AnchorBias.Main);
            Handles.DrawLine(mainAnchorPosition, mainPosition);
        }
        if (joint2D.connectedBody) {
            using (new HandleColor(editorSettings.connectedDiscColor)) {
                Vector2 connectedPosition = GetTargetPositionWithOffset(joint2D, JointHelpers.AnchorBias.Connected);
                Handles.DrawLine(connectedAnchorPosition, connectedPosition);
            }
        }

        Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
        DrawDistance();
    }


    public void DrawDistance()
    {
        DistanceJoint2D joint2D = attachedJoint as DistanceJoint2D;
        if (joint2D == null)
        {
            return;
        }

        Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
        Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
        Vector2 diff = connectedAnchorPosition - mainAnchorPosition;
        if (diff.magnitude <= Mathf.Epsilon)
        {
            diff = -Vector2.up;
        }
        Vector2 normalizedDiff = diff.normalized;
        using (new HandleColor(Color.white))
        {
            Vector2 wantedMainAnchorPosition = connectedAnchorPosition - normalizedDiff * joint2D.distance;
//            Vector2 wantedConnectedAnchorPosition = mainAnchorPosition + normalizedDiff*joint2D.distance;

            Handles.DrawLine(wantedMainAnchorPosition, connectedAnchorPosition);

            Vector2 tangent = new Vector2(-normalizedDiff.y, normalizedDiff.x) *
                              HandleUtility.GetHandleSize(connectedAnchorPosition) * 0.125f;
            Handles.DrawLine(wantedMainAnchorPosition, wantedMainAnchorPosition + tangent);
            Handles.DrawLine(wantedMainAnchorPosition, wantedMainAnchorPosition - tangent);
//            Handles.DrawLine(wantedConnectedAnchorPosition, wantedConnectedAnchorPosition + tangent);
//            Handles.DrawLine(wantedConnectedAnchorPosition, wantedConnectedAnchorPosition - tangent);
        }
    }
#endif

}
