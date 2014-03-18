using System;
using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (DistanceJoint2D))]
[CanEditMultipleObjects]
public class DistanceJoint2DEditor : Joint2DEditor {
    private static readonly HashSet<string> ControlNames = new HashSet<string> { "distance" };

    protected override HashSet<string> GetControlNames() {
        return ControlNames;
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        DistanceJoint2D distanceJoint2D = (DistanceJoint2D) joint2D;

        Vector2 center = JointHelpers.GetAnchorPosition(distanceJoint2D, bias);
        float scale = editorSettings.anchorScale;
        float handleSize = HandleUtility.GetHandleSize(center)*scale;

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(distanceJoint2D);
        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(distanceJoint2D);
        if (Vector2.Distance(mainAnchorPosition, connectedAnchorPosition) > AnchorEpsilon) {
            using (new HandleColor(Color.green)) {
                Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
            }
        }

        if (bias != JointHelpers.AnchorBias.Connected) {
            DrawDistance(distanceJoint2D, anchorInfo, bias);
        }
        else {
            int sliderID = anchorInfo.GetControlID("slider");
            if (GUIUtility.hotControl == sliderID) {
                DrawDistance(distanceJoint2D, anchorInfo, bias);
            }
        }

        Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);

        if (bias == JointHelpers.AnchorBias.Main) {
            Vector2 mainBodyPosition = GetTargetPosition(distanceJoint2D, JointHelpers.AnchorBias.Main);
            using (new HandleColor(editorSettings.mainDiscColor)) {
                if (Vector2.Distance(mainBodyPosition, center) > AnchorEpsilon) {
                    Handles.DrawLine(mainBodyPosition, center);
                }
            }
        }
        else if (bias == JointHelpers.AnchorBias.Connected) {
            Vector2 connectedBodyPosition = GetTargetPosition(distanceJoint2D, JointHelpers.AnchorBias.Connected);
            if (distanceJoint2D.connectedBody) {
                using (new HandleColor(editorSettings.connectedDiscColor)) {
                    if (Vector2.Distance(connectedBodyPosition, center) > AnchorEpsilon) {
                        Handles.DrawLine(connectedBodyPosition, center);
                    }
                    else {
                        float rot = JointHelpers.GetTargetRotation(distanceJoint2D, JointHelpers.AnchorBias.Connected);
                        Handles.DrawLine(center, center + Helpers2D.GetDirection(rot)*handleSize);
                    }
                }
            }
        }
        return false;
    }

    protected override Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
        JointHelpers.AnchorBias bias, float snapDistance) {
        JointHelpers.AnchorBias otherBias = bias == JointHelpers.AnchorBias.Main
            ? JointHelpers.AnchorBias.Connected
            : JointHelpers.AnchorBias.Main;

        DistanceJoint2D distanceJoint2D = (DistanceJoint2D) joint;

        AnchorSliderState anchorSliderState = StateObject.Get<AnchorSliderState>(sliderID);
        Vector2 currentMousePosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
        Vector2 currentAnchorPosition = currentMousePosition - anchorSliderState.mouseOffset;

        Vector2 otherAnchorPosition = JointHelpers.GetAnchorPosition(distanceJoint2D, otherBias);
        Vector2 diff = otherAnchorPosition - currentAnchorPosition;
        if (diff.magnitude <= Mathf.Epsilon) {
            diff = -Vector2.up;
        }
        Vector2 normalizedDiff = diff.normalized;

        Vector2 wantedAnchorPosition = otherAnchorPosition - normalizedDiff*distanceJoint2D.distance;

        if (Vector2.Distance(position, wantedAnchorPosition) < snapDistance) {
            return wantedAnchorPosition;
        }

        return position;
    }

    public override Bounds OnGetFrameBounds() {
        Bounds baseBounds = base.OnGetFrameBounds();

        foreach (DistanceJoint2D joint2D in targets.Cast<DistanceJoint2D>()) {
            Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
            Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
            Vector2 diff = connectedAnchorPosition - mainAnchorPosition;
            if (diff.magnitude <= Mathf.Epsilon) {
                diff = -Vector2.up;
            }
            Vector2 normalizedDiff = diff.normalized;
            Vector2 wantedMainAnchorPosition = connectedAnchorPosition - normalizedDiff*joint2D.distance;

            baseBounds.Encapsulate(wantedMainAnchorPosition);
        }

        return baseBounds;
    }

    private void DrawDistance(DistanceJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        if (joint2D == null) {
            return;
        }

        JointHelpers.AnchorBias otherBias = bias == JointHelpers.AnchorBias.Main
            ? JointHelpers.AnchorBias.Connected
            : JointHelpers.AnchorBias.Main;

        Vector2 anchorPosition = JointHelpers.GetAnchorPosition(joint2D, bias);
        Vector2 otherAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, otherBias);
        Vector2 diff = otherAnchorPosition - anchorPosition;
        if (diff.magnitude <= Mathf.Epsilon) {
            diff = Vector2.up*(bias == JointHelpers.AnchorBias.Connected ? 1 : -1);
        }
        Vector2 normalizedDiff = diff.normalized;
        Vector2 wantedAnchorPosition = otherAnchorPosition - normalizedDiff*joint2D.distance;
        Vector2 tangent = new Vector2(-normalizedDiff.y, normalizedDiff.x)*
                          HandleUtility.GetHandleSize(otherAnchorPosition)*0.125f;
        Color color = Color.white;
        float drawScale = 1;


        if (bias != JointHelpers.AnchorBias.Connected) {
            int distanceControlID = anchorInfo.GetControlID("distance");

			HoverState state = StateObject.Get<HoverState>(distanceControlID);

            EditorGUI.BeginChangeCheck();
            wantedAnchorPosition = Handles.Slider2D(distanceControlID,
                wantedAnchorPosition,
                Vector3.forward,
                Vector3.up,
                Vector3.right,
                tangent.magnitude*2,
                DrawFunc,
                Vector2.zero);
            if (EditorGUI.EndChangeCheck()) {
                EditorHelpers.RecordUndo("Change Distance", joint2D);

                if (
                    Vector2.Dot(wantedAnchorPosition - otherAnchorPosition,
                        anchorPosition - otherAnchorPosition) < 0) {
                    joint2D.distance = 0;
                }
                else {
                    float distanceToLine = Helpers2D.DistanceToLine(new Ray(otherAnchorPosition, tangent),
                        wantedAnchorPosition);
                    float distanceBetweenAnchors = Vector2.Distance(otherAnchorPosition, anchorPosition);
                    joint2D.distance = Mathf.Abs(distanceToLine - distanceBetweenAnchors) <
                                       HandleUtility.GetHandleSize(anchorPosition)*0.125f
                        ? distanceBetweenAnchors
                        : distanceToLine;
                }

                EditorUtility.SetDirty(joint2D);
            }

            Event current = Event.current;
            switch (current.GetTypeForControl(distanceControlID)) {
                case EventType.mouseMove:
                    bool hovering = HandleUtility.nearestControl == distanceControlID;
                    if (state.hovering != hovering) {
                        current.Use();
                        state.hovering = hovering;
                    }
                    break;
                case EventType.repaint:

		            if (GUIUtility.hotControl == distanceControlID || state.hovering) {
						color = GUIUtility.hotControl == distanceControlID ? Color.red : Color.yellow;

						var cursor = EditorHelpers.RotatedResizeCursor(normalizedDiff);


			            EditorHelpers.SetEditorCursor(cursor, distanceControlID);
						drawScale = 2;
		            }
                    break;
            }
        }
        if (Event.current.type == EventType.repaint) {
            using (new HandleColor(Color.black)) {
                EditorHelpers.DrawThickLine(wantedAnchorPosition, otherAnchorPosition, 5);
                Handles.color = Color.white;
                EditorHelpers.DrawThickLine(wantedAnchorPosition, otherAnchorPosition, 2);
            }
            using (new HandleColor(Color.black)) {
                EditorHelpers.DrawThickLine(wantedAnchorPosition - tangent*drawScale,
                    wantedAnchorPosition + tangent*drawScale, 4);
                Handles.color = color;
                EditorHelpers.DrawThickLine(wantedAnchorPosition - tangent*drawScale,
                    wantedAnchorPosition + tangent*drawScale, 2);
            }
        }
    }


	private void DrawFunc(int controlID, Vector3 position, Quaternion rotation, float size) {
//        throw new NotImplementedException();
    }
}