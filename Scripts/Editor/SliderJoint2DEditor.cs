using System.Collections.Generic;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (SliderJoint2D))]
[CanEditMultipleObjects]
public class SliderJoint2DEditor : Joint2DEditor {
    private static readonly string[] ControlNames = {"slider"};

    protected override IEnumerable<string> GetControlNames() {
        return ControlNames;
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        SliderJoint2D sliderJoint2D = (SliderJoint2D) joint2D;

        Vector2 center = JointHelpers.GetAnchorPosition(sliderJoint2D, bias);
        float scale = editorSettings.anchorScale;
        float handleSize = HandleUtility.GetHandleSize(center)*scale;

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);
        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(sliderJoint2D);
        if (Vector2.Distance(mainAnchorPosition, connectedAnchorPosition) > AnchorEpsilon) {
            using (new HandleColor(Color.green)) {
                Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
            }
        }

        if (bias != JointHelpers.AnchorBias.Connected) {
            DrawSlider(sliderJoint2D, anchorInfo);
        }

        Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);

        if (bias == JointHelpers.AnchorBias.Main) {
            Vector2 mainBodyPosition = GetTargetPosition(sliderJoint2D, JointHelpers.AnchorBias.Main);
            using (new HandleColor(editorSettings.mainDiscColor)) {
                if (Vector2.Distance(mainBodyPosition, center) > AnchorEpsilon) {
                    Handles.DrawLine(mainBodyPosition, center);
                }
            }
        }
        else if (bias == JointHelpers.AnchorBias.Connected) {
            Vector2 connectedBodyPosition = GetTargetPosition(sliderJoint2D, JointHelpers.AnchorBias.Connected);
            if (sliderJoint2D.connectedBody) {
                using (new HandleColor(editorSettings.connectedDiscColor)) {
                    if (Vector2.Distance(connectedBodyPosition, center) > AnchorEpsilon) {
                        Handles.DrawLine(connectedBodyPosition, center);
                    }
                    else {
                        float rot = JointHelpers.GetTargetRotation(sliderJoint2D, JointHelpers.AnchorBias.Connected);
                        Handles.DrawLine(center, center + Helpers2D.Rotated2DVector(rot)*handleSize);
                    }
                }
            }
        }
        return false;
    }

    private void DrawSlider(SliderJoint2D sliderJoint2D, AnchorInfo anchorInfo) {
        Vector2 direction = Helpers2D.Rotated2DVector(sliderJoint2D.angle);
        direction = Helpers2D.Transform2DVector(sliderJoint2D.transform, direction);
        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);
        float handleSize = HandleUtility.GetHandleSize(mainAnchorPosition) * 0.5f;
        Vector2 left = mainAnchorPosition - direction*handleSize;
        Vector2 right = mainAnchorPosition + direction*handleSize;
        Handles.DrawLine(left, right);

        using (new HandleGUI())
        {
            GUILayout.Label(sliderJoint2D.angle + "");
            GUILayout.Label(sliderJoint2D.referenceAngle + "");
        }
    }
}