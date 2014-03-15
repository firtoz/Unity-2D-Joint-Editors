using System.Collections.Generic;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (SliderJoint2D))]
[CanEditMultipleObjects]
public class SliderJoint2DEditor : Joint2DEditor {
    private static readonly HashSet<string> ControlNames = new HashSet<string> {"sliderAngle"};

    protected override HashSet<string> GetControlNames() {
        return ControlNames;
    }

//    protected override Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
//        JointHelpers.AnchorBias bias, float snapDistance)
//    {
//        JointHelpers.AnchorBias otherBias = bias == JointHelpers.AnchorBias.Main
//            ? JointHelpers.AnchorBias.Connected
//            : JointHelpers.AnchorBias.Main;
//
//        SliderJoint2D distanceJoint2D = (SliderJoint2D)joint;
//
//        AnchorSliderState anchorSliderState = StateObject.Get<AnchorSliderState>(sliderID);
//        Vector2 currentMousePosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
//        Vector2 currentAnchorPosition = currentMousePosition - anchorSliderState.mouseOffset;
//
//        Vector2 otherAnchorPosition = JointHelpers.GetAnchorPosition(distanceJoint2D, otherBias);
//        Vector2 diff = otherAnchorPosition - currentAnchorPosition;
//        if (diff.magnitude <= Mathf.Epsilon)
//        {
//            diff = -Vector2.up;
//        }
//        Vector2 normalizedDiff = diff.normalized;
//
//        Vector2 wantedAnchorPosition = otherAnchorPosition - normalizedDiff * distanceJoint2D.angle;
//
//        if (Vector2.Distance(position, wantedAnchorPosition) < snapDistance)
//        {
//            return wantedAnchorPosition;
//        }
//
//        return position;
//    }

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

    private float LineAngleHandle(int controlID, float angle, Vector2 center, 
        float handleScale = 1f,
        float lineThickness = 1f) {

        float handleSize = HandleUtility.GetHandleSize(center) * handleScale;

        Vector2 rotated2DVector = Helpers2D.Rotated2DVector(angle) * handleSize;

        Vector2 left = center - rotated2DVector;
        Vector2 right = center + rotated2DVector;


        AngleState angleState = StateObject.Get<AngleState>(controlID);
        HoverState hoverState = angleState.hoverState;

        Event current = Event.current;
        if (current.type == EventType.layout)
        {
            HandleUtility.AddControl(controlID, HandleUtility.DistanceToLine(left, right) - lineThickness);
        }

        switch (current.GetTypeForControl(controlID))
        {
            case EventType.mouseMove:
                bool hovering = (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID);

                if (hoverState.hovering != hovering)
                {
                    hoverState.hovering = hovering;
                    HandleUtility.Repaint();
                }
                break;
            case EventType.mouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                }
                break;
            case EventType.mouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    Vector2 current2DPosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
                    float curAngle = Helpers2D.GetAngle2D(current2DPosition - angleState.center);
                    float prevAngle = Helpers2D.GetAngle2D(angleState.mousePosition - angleState.center);

                    float deltaAngle = Mathf.DeltaAngle(prevAngle, curAngle);
                    if (Mathf.Abs(deltaAngle) > Mathf.Epsilon) {
                        angleState.angleDelta += deltaAngle;

                        angle = angleState.startAngle + angleState.angleDelta;

                        GUI.changed = true;
                    }

                    angleState.mousePosition = current2DPosition;
                }
                break;
            case EventType.mouseDown:
                if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID)
                {
                    GUIUtility.hotControl = controlID;
                    angleState.angleDelta = 0;
                    angleState.startAngle = angle;
                    angleState.mousePosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
                    angleState.center = center;
                    Event.current.Use();
                }
                break;
            case EventType.repaint:
                using (new HandleColor(Color.black))
                {
                    EditorHelpers.DrawThickLine(left, right, lineThickness * 2f);
                    if (GUIUtility.hotControl == controlID)
                    {
                        Handles.color = new Color(1f, 1f, 1f, 0.25f);
                        Handles.DrawLine(angleState.center, angleState.mousePosition);
                        Handles.color = Color.red;
                    }
                    else {
                        if (GUIUtility.hotControl == 0 && hoverState.hovering)
                        {
                            Handles.color = Color.yellow;
                        }
                        else
                        {
                            Handles.color = Color.white;
                        }
                    }
                    EditorHelpers.DrawThickLine(left, right, lineThickness);
                }
                break;
        }
        return angle;
    }

    private void DrawSlider(SliderJoint2D sliderJoint2D, AnchorInfo anchorInfo) {
        float worldAngle = sliderJoint2D.transform.eulerAngles.z + sliderJoint2D.angle;
        int controlID = anchorInfo.GetControlID("sliderAngle");
        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);
        EditorGUI.BeginChangeCheck();
        float newAngle = LineAngleHandle(controlID, worldAngle, mainAnchorPosition, 0.5f, 4);
        if (EditorGUI.EndChangeCheck())
        {
            EditorHelpers.RecordUndo("Alter Slider Joint 2D Angle", sliderJoint2D);
            sliderJoint2D.angle = newAngle - sliderJoint2D.transform.eulerAngles.z;
        }

        
    }
}

internal class AngleState {
    public float angleDelta;
    public Vector2 mousePosition;
    public Vector2 center;
    public float startAngle;
    public HoverState hoverState = new HoverState();
}