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

    protected override bool WantsLocking() {
        return true;
    }

    protected override Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
        JointHelpers.AnchorBias bias, float snapDistance) {
        if (SettingsHelper.GetOrCreate(joint).lockAnchors
            || Vector2.Distance(JointHelpers.GetMainAnchorPosition(joint),
                JointHelpers.GetConnectedAnchorPosition(joint)) <= AnchorEpsilon) {
            return position;
        }


        SliderJoint2D sliderJoint2D = (SliderJoint2D) joint;

        Vector2 wantedAnchorPosition = GetWantedAnchorPosition(sliderJoint2D, bias, position);

        if (Vector2.Distance(position, wantedAnchorPosition) < snapDistance) {
            return wantedAnchorPosition;
        }

        return position;
    }

    protected override bool DragBothAnchorsWhenLocked() {
        return false;
    }

    protected override void ReAlignAnchors(AnchoredJoint2D joint2D, JointHelpers.AnchorBias alignmentBias) {
        SliderJoint2D sliderJoint2D = (SliderJoint2D) joint2D;
//        if (alignmentBias == JointHelpers.AnchorBias.Connected) 
        {
            //align the angle to the connected anchor
            Vector2 direction = JointHelpers.GetConnectedAnchorPosition(joint2D) -
                                JointHelpers.GetMainAnchorPosition(joint2D);

            float wantedAngle = Helpers2D.GetAngle(direction);

            EditorHelpers.RecordUndo("Realign angle", sliderJoint2D);
            sliderJoint2D.angle = wantedAngle - sliderJoint2D.transform.eulerAngles.z;
        }
    }

    protected override Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias) {
        return GetWantedAnchorPosition(anchoredJoint2D, bias, JointHelpers.GetAnchorPosition(anchoredJoint2D, bias));
    }

    private static Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias,
        Vector2 position) {
        SliderJoint2D sliderJoint2D = (SliderJoint2D) anchoredJoint2D;

        JointHelpers.AnchorBias otherBias = JointHelpers.GetOppositeBias(bias);

        float worldAngle = sliderJoint2D.transform.eulerAngles.z + sliderJoint2D.angle;

        Ray slideRay = new Ray(JointHelpers.GetAnchorPosition(sliderJoint2D, otherBias),
            Helpers2D.GetDirection(worldAngle));
        Vector2 wantedAnchorPosition = Helpers2D.ClosestPointToLine(slideRay, position);
        return wantedAnchorPosition;
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        SliderJoint2D sliderJoint2D = (SliderJoint2D) joint2D;

        Vector2 center = JointHelpers.GetAnchorPosition(sliderJoint2D, bias);
        float scale = editorSettings.anchorScale;
        float handleSize = HandleUtility.GetHandleSize(center)*scale;

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);
        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(sliderJoint2D);

        if (bias != JointHelpers.AnchorBias.Connected && !Event.current.shift) {
            DrawSlider(sliderJoint2D, anchorInfo);
        }

        if (GUIUtility.hotControl == anchorInfo.GetControlID("slider")) {
            Vector2 snap = GetWantedAnchorPosition(sliderJoint2D, bias);
            using (new HandleColor(new Color(1, 1, 1, .5f))) {
                Handles.DrawLine(connectedAnchorPosition, snap);
                Handles.DrawLine(mainAnchorPosition, snap);
            }
        }

        using (new HandleColor(new Color(1, 1, 1, 0.125f))) {
            Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
        }

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
                        Handles.DrawLine(center, center + Helpers2D.GetDirection(rot)*handleSize);
                    }
                }
            }
        }
        return false;
    }

    private float LineAngleHandle(int controlID, float angle, Vector2 center,
        float handleScale = 1f,
        float lineThickness = 1f) {
        float handleSize = HandleUtility.GetHandleSize(center)*handleScale;

        Vector2 rotated2DVector = Helpers2D.GetDirection(angle)*handleSize;

        Vector2 left = center - rotated2DVector;
        Vector2 right = center + rotated2DVector;


        AngleState angleState = StateObject.Get<AngleState>(controlID);
        HoverState hoverState = angleState.hoverState;

        Event current = Event.current;
        if (current.type == EventType.layout) {
            HandleUtility.AddControl(controlID, HandleUtility.DistanceToLine(left, right) - lineThickness);
        }

        bool hovering;
        switch (current.GetTypeForControl(controlID)) {
            case EventType.mouseMove:
                hovering = (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID);

                if (hoverState.hovering != hovering) {
                    hoverState.hovering = hovering;
                    HandleUtility.Repaint();
                }
                break;
            case EventType.mouseUp:
                if (GUIUtility.hotControl == controlID && Event.current.button == 0) {
                    GUIUtility.hotControl = 0;

                    hovering = (HandleUtility.nearestControl == controlID);

                    if (hoverState.hovering != hovering) {
                        hoverState.hovering = hovering;
                        HandleUtility.Repaint();
                    }

                    Event.current.Use();
                }
                break;
            case EventType.mouseDrag:
                if (GUIUtility.hotControl == controlID) {
                    Vector2 current2DPosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
                    float curAngle = Helpers2D.GetAngle(current2DPosition - angleState.center);
                    float prevAngle = Helpers2D.GetAngle(angleState.mousePosition - angleState.center);

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
                if (Event.current.button == 0 && GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID) {
                    GUIUtility.hotControl = controlID;
                    angleState.angleDelta = 0;
                    angleState.startAngle = angle;
                    angleState.mousePosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
                    angleState.center = center;
                    Event.current.Use();
                }
                break;
            case EventType.repaint:
                using (new HandleColor(Color.black)) {
                    EditorHelpers.DrawThickLine(left, right, lineThickness*2f);
                    if (GUIUtility.hotControl == controlID || (GUIUtility.hotControl == 0 && hoverState.hovering)) {
                        EditorHelpers.SetEditorCursor(MouseCursor.RotateArrow, controlID);
                    }
                    if (GUIUtility.hotControl == controlID) {
                        Handles.color = new Color(1f, 1f, 1f, 0.25f);
                        Handles.DrawLine(angleState.center, angleState.mousePosition);
                        Handles.color = Color.red;
                    }
                    else {
                        if (GUIUtility.hotControl == 0 && hoverState.hovering) {
                            Handles.color = Color.yellow;
                        }
                        else {
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

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);

        if (sliderJoint2D.useLimits) {
            Vector2 direction = Helpers2D.GetDirection(sliderJoint2D.transform.eulerAngles.z + sliderJoint2D.angle);
            Vector2 minLimitPosition = mainAnchorPosition + sliderJoint2D.limits.min*direction;
            Vector2 maxLimitPosition = mainAnchorPosition + sliderJoint2D.limits.max*direction;

            Vector2 normal = new Vector2(-direction.y, direction.x)*HandleUtility.GetHandleSize(minLimitPosition)*0.25f;

            using (new HandleColor(Color.green)) {
                EditorHelpers.DrawThickLine(minLimitPosition + normal, minLimitPosition - normal, 3);
                Handles.DrawLine(minLimitPosition, maxLimitPosition);
                EditorHelpers.DrawThickLine(maxLimitPosition + normal*1.25f, maxLimitPosition - normal*1.25f, 3);
            }
        }


        EditorGUI.BeginChangeCheck();
        int controlID = anchorInfo.GetControlID("sliderAngle");

        float newAngle = LineAngleHandle(controlID, worldAngle, mainAnchorPosition, 0.5f, 2);

        if (EditorGUI.EndChangeCheck()) {
            if (SettingsHelper.GetOrCreate(sliderJoint2D).lockAnchors) {
                Vector2 connectedOffset = JointHelpers.GetConnectedAnchorPosition(sliderJoint2D) - mainAnchorPosition;

                float wantedAngle = newAngle - sliderJoint2D.transform.eulerAngles.z;
                float angleDelta = Mathf.DeltaAngle(sliderJoint2D.angle, wantedAngle);
                EditorHelpers.RecordUndo("Alter Slider Joint 2D Angle", sliderJoint2D);
                sliderJoint2D.angle = wantedAngle;

                JointHelpers.SetWorldConnectedAnchorPosition(sliderJoint2D,
                    mainAnchorPosition + (Vector2) (Helpers2D.Rotate(angleDelta)*connectedOffset));
            }
            else {
                Ray wantedAngleRay = new Ray(mainAnchorPosition,
                    (JointHelpers.GetConnectedAnchorPosition(sliderJoint2D) - mainAnchorPosition).normalized);
                float handleSize = HandleUtility.GetHandleSize(mainAnchorPosition);
                Vector2 slidePosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);

                Vector2 closestPosition = Helpers2D.ClosestPointToLine(wantedAngleRay, slidePosition);

                if (Vector2.Distance(closestPosition, slidePosition) < handleSize*0.125f) {
                    newAngle = Helpers2D.GetAngle(wantedAngleRay.direction);
                }
                EditorHelpers.RecordUndo("Alter Slider Joint 2D Angle", sliderJoint2D);
                sliderJoint2D.angle = newAngle - sliderJoint2D.transform.eulerAngles.z;
            }
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