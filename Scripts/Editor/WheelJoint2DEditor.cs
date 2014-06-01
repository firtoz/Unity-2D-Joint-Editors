using System.Collections.Generic;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (WheelJoint2D))]
[CanEditMultipleObjects]
public class WheelJoint2DEditor : Joint2DEditor
{
    private static readonly HashSet<string> ControlNames = new HashSet<string> {
        "sliderAngle"
    };

    protected override HashSet<string> GetControlNames()
    {
        return ControlNames;
    }

    protected override bool WantsLocking()
    {
        return true;
    }

    protected override Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
       JointHelpers.AnchorBias bias, float snapDistance)
    {
        if (!EditorGUI.actionKey)
        {
            return position;
        }

        WheelJoint2D wheelJoint2D = (WheelJoint2D)joint;

        if (!SettingsHelper.GetOrCreate(joint).lockAnchors &&
            !(Vector2.Distance(JointHelpers.GetMainAnchorPosition(joint),
                JointHelpers.GetConnectedAnchorPosition(joint)) <= AnchorEpsilon))
        {
            Vector2 wantedAnchorPosition = GetWantedAnchorPosition(wheelJoint2D, bias, position);

            if (Vector2.Distance(position, wantedAnchorPosition) < snapDistance)
            {
                return wantedAnchorPosition;
            }
        }

        return position;
    }

    protected override bool DragBothAnchorsWhenLocked()
    {
        return false;
    }

    protected override void ReAlignAnchors(AnchoredJoint2D joint2D, JointHelpers.AnchorBias alignmentBias)
    {
        WheelJoint2D wheelJoint2D = (WheelJoint2D)joint2D;

        //align the angle to the connected anchor
        Vector2 direction = JointHelpers.GetConnectedAnchorPosition(joint2D) -
                            JointHelpers.GetMainAnchorPosition(joint2D);

        float wantedAngle = Helpers2D.GetAngle(direction);

        EditorHelpers.RecordUndo("Realign angle", wheelJoint2D);
        JointSuspension2D susp = wheelJoint2D.suspension;
        susp.angle = wantedAngle - wheelJoint2D.transform.eulerAngles.z;
        wheelJoint2D.suspension = susp;
    }

    protected override Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias)
    {
        return GetWantedAnchorPosition(anchoredJoint2D, bias, JointHelpers.GetAnchorPosition(anchoredJoint2D, bias));
    }

    private static Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias,
        Vector2 position)
    {
        WheelJoint2D wheelJoint2D = (WheelJoint2D)anchoredJoint2D;

        JointHelpers.AnchorBias otherBias = JointHelpers.GetOppositeBias(bias);

        float worldAngle = wheelJoint2D.transform.eulerAngles.z + wheelJoint2D.suspension.angle;

        Ray slideRay = new Ray(JointHelpers.GetAnchorPosition(wheelJoint2D, otherBias),
            Helpers2D.GetDirection(worldAngle));
        Vector2 wantedAnchorPosition = Helpers2D.ClosestPointToRay(slideRay, position);
        return wantedAnchorPosition;
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias)
    {
        WheelJoint2D wheelJoint2D = (WheelJoint2D)joint2D;

        Vector2 center = JointHelpers.GetAnchorPosition(wheelJoint2D, bias);
        float scale = editorSettings.anchorScale;
        float handleSize = HandleUtility.GetHandleSize(center) * scale;

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(wheelJoint2D);
        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(wheelJoint2D);

        if (bias != JointHelpers.AnchorBias.Connected && (GUIUtility.hotControl == anchorInfo.GetControlID("sliderAngle") || !Event.current.shift))
        {
            DrawSlider(wheelJoint2D, anchorInfo);
        }

        if (GUIUtility.hotControl == anchorInfo.GetControlID("slider"))
        {
            Vector2 snap = GetWantedAnchorPosition(wheelJoint2D, bias);
            using (new HandleColor(new Color(1, 1, 1, .5f)))
            {
                Handles.DrawLine(connectedAnchorPosition, snap);
                Handles.DrawLine(mainAnchorPosition, snap);
            }
        }

        using (new HandleColor(new Color(1, 1, 1, 0.125f)))
        {
            Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
        }

        if (bias == JointHelpers.AnchorBias.Main)
        {
            Vector2 mainBodyPosition = GetTargetPosition(wheelJoint2D, JointHelpers.AnchorBias.Main);
            using (new HandleColor(editorSettings.mainDiscColor))
            {
                if (Vector2.Distance(mainBodyPosition, center) > AnchorEpsilon)
                {
                    Handles.DrawLine(mainBodyPosition, center);
                }
            }
        }
        else if (bias == JointHelpers.AnchorBias.Connected)
        {
            Vector2 connectedBodyPosition = GetTargetPosition(wheelJoint2D, JointHelpers.AnchorBias.Connected);
            if (wheelJoint2D.connectedBody)
            {
                using (new HandleColor(editorSettings.connectedDiscColor))
                {
                    if (Vector2.Distance(connectedBodyPosition, center) > AnchorEpsilon)
                    {
                        Handles.DrawLine(connectedBodyPosition, center);
                    }
                    else
                    {
                        float rot = JointHelpers.GetTargetRotation(wheelJoint2D, JointHelpers.AnchorBias.Connected);
                        Handles.DrawLine(center, center + Helpers2D.GetDirection(rot) * handleSize);
                    }
                }
            }
        }
        return false;
    }

    private static void DrawSlider(WheelJoint2D wheelJoint2D, AnchorInfo anchorInfo)
    {
        float worldAngle = wheelJoint2D.transform.eulerAngles.z + wheelJoint2D.suspension.angle;

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(wheelJoint2D);


        EditorGUI.BeginChangeCheck();
        int controlID = anchorInfo.GetControlID("sliderAngle");

        float newAngle = LineAngleHandle(controlID, worldAngle, mainAnchorPosition, 0.5f, 2);

        if (EditorGUI.EndChangeCheck())
        {
            if (SettingsHelper.GetOrCreate(wheelJoint2D).lockAnchors)
            {
                Vector2 connectedOffset = JointHelpers.GetConnectedAnchorPosition(wheelJoint2D) - mainAnchorPosition;

                float wantedAngle = newAngle - wheelJoint2D.transform.eulerAngles.z;
                float angleDelta = Mathf.DeltaAngle(wheelJoint2D.suspension.angle, wantedAngle);
                EditorHelpers.RecordUndo("Alter Wheel Joint 2D Angle", wheelJoint2D);
                JointSuspension2D susp = wheelJoint2D.suspension;
                susp.angle = wantedAngle;
                wheelJoint2D.suspension = susp;

                JointHelpers.SetWorldConnectedAnchorPosition(wheelJoint2D,
                    mainAnchorPosition + (Vector2)(Helpers2D.Rotate(angleDelta) * connectedOffset));
            }
            else
            {
                Ray wantedAngleRay = new Ray(mainAnchorPosition,
                    (JointHelpers.GetConnectedAnchorPosition(wheelJoint2D) - mainAnchorPosition).normalized);
                float handleSize = HandleUtility.GetHandleSize(mainAnchorPosition);
                Vector2 mousePosition2D = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
                Vector2 anglePosition =
                    Helpers2D.ClosestPointToRay(new Ray(mainAnchorPosition, Helpers2D.GetDirection(newAngle)),
                        mousePosition2D);


                Vector2 closestPosition = Helpers2D.ClosestPointToRay(wantedAngleRay, anglePosition);

                if (EditorGUI.actionKey && Vector2.Distance(closestPosition, anglePosition) < handleSize * 0.125f)
                {
                    Vector2 currentDirection = Helpers2D.GetDirection(newAngle);
                    Vector2 closestPositionToDirection =
                        Helpers2D.ClosestPointToRay(wantedAngleRay,
                            mainAnchorPosition + currentDirection);

                    newAngle = Helpers2D.GetAngle(closestPositionToDirection - mainAnchorPosition);
                }
                EditorHelpers.RecordUndo("Alter Slider Joint 2D Angle", wheelJoint2D);
                JointSuspension2D susp = wheelJoint2D.suspension;
                susp.angle = newAngle - wheelJoint2D.transform.eulerAngles.z;
                wheelJoint2D.suspension = susp;
            }
        }
    }
}