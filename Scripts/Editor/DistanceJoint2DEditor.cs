using System.Collections.Generic;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (DistanceJoint2D))]
[CanEditMultipleObjects]
public class DistanceJoint2DEditor : Joint2DEditor {
    private static readonly string[] ControlNames = {"distance"};

    protected override IEnumerable<string> GetControlNames() {
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
            DrawDistance(distanceJoint2D, anchorInfo);
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
                        Handles.DrawLine(center, center + Helpers.Rotated2DVector(rot)*handleSize);
                    }
                }
            }
        }
        return false;
    }

    public static float DistanceToLine(Ray ray, Vector3 point) {
        return Vector3.Cross(ray.direction, point - ray.origin).magnitude;
    }

    protected override IEnumerable<Vector2> GetSnapPositions(AnchoredJoint2D joint2D, AnchorInfo anchorInfo,
        JointHelpers.AnchorBias bias) {
        if (bias == JointHelpers.AnchorBias.Connected) {
            return base.GetSnapPositions(joint2D, anchorInfo, bias);
        }

        DistanceJoint2D distanceJoint2D = (DistanceJoint2D) joint2D;
        Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
//        if (GUIUtility.hotControl == anchorInfo.GetControlID("slider")) {
//            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
//            Vector2 intersection = Intersect2DPlane(ray);
//            mainAnchorPosition = intersection;
//        }
        Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
        Vector2 diff = connectedAnchorPosition - mainAnchorPosition;
        if (diff.magnitude <= Mathf.Epsilon) {
            diff = -Vector2.up;
        }
        Vector2 normalizedDiff = diff.normalized;
        Vector2 wantedMainAnchorPosition = connectedAnchorPosition - normalizedDiff*distanceJoint2D.distance;
        return new List<Vector2> {wantedMainAnchorPosition};
    }

    private Vector2 Intersect2DPlane(Ray ray) {
        float d = Vector3.Dot(-ray.origin, Vector3.forward)/Vector3.Dot(ray.direction, Vector3.forward);
        return ray.GetPoint(d);
    }

    private void DrawDistance(DistanceJoint2D joint2D, AnchorInfo anchorInfo) {
        if (joint2D == null) {
            return;
        }

        Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
        Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
        Vector2 diff = connectedAnchorPosition - mainAnchorPosition;
        if (diff.magnitude <= Mathf.Epsilon) {
            diff = -Vector2.up;
        }
        Vector2 normalizedDiff = diff.normalized;
        Vector2 wantedMainAnchorPosition = connectedAnchorPosition - normalizedDiff*joint2D.distance;
        Vector2 tangent = new Vector2(-normalizedDiff.y, normalizedDiff.x)*
                          HandleUtility.GetHandleSize(connectedAnchorPosition)*0.125f;


        int distanceControlID = anchorInfo.GetControlID("distance");

        DistanceSliderState state = StateObject.Get<DistanceSliderState>(distanceControlID);

        EditorGUI.BeginChangeCheck();
        wantedMainAnchorPosition = Handles.Slider2D(distanceControlID,
            wantedMainAnchorPosition,
            Vector3.forward,
            Vector3.up,
            Vector3.right,
            tangent.magnitude*2,
            DrawFunc,
            Vector2.zero);
        if (EditorGUI.EndChangeCheck()) {
            EditorHelpers.RecordUndo("Change Distance", joint2D);

            if (
                Vector2.Dot(wantedMainAnchorPosition - connectedAnchorPosition,
                    mainAnchorPosition - connectedAnchorPosition) < 0) {
                joint2D.distance = 0;
            }
            else {
                float distance = Vector2.Distance(wantedMainAnchorPosition, mainAnchorPosition);
                if (distance < HandleUtility.GetHandleSize(mainAnchorPosition)*0.25f) {
                    joint2D.distance = Vector2.Distance(connectedAnchorPosition, mainAnchorPosition);
                }
                else {
                    joint2D.distance = DistanceToLine(new Ray(connectedAnchorPosition, tangent), wantedMainAnchorPosition);
                }
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
                Color color = Color.white;
                float drawScale = 1;

                if (GUIUtility.hotControl == distanceControlID) {
                    color = Color.red;
                    drawScale = 2;
                }
                else if (state.hovering) {
                    color = Color.yellow;
                    drawScale = 2;
                }
                using (new HandleColor(Color.white)) {
                    Handles.DrawLine(wantedMainAnchorPosition, connectedAnchorPosition);
                }
                using (new HandleColor(color)) {
                    Handles.DrawLine(wantedMainAnchorPosition, wantedMainAnchorPosition + tangent*drawScale);
                    Handles.DrawLine(wantedMainAnchorPosition, wantedMainAnchorPosition - tangent*drawScale);
                }
                break;
        }
    }

    private void DrawFunc(int controlID, Vector3 position, Quaternion rotation, float size) {
//        throw new NotImplementedException();
    }

    public class DistanceSliderState {
        public bool hovering = false;
        public Vector2 mousePos = Vector2.zero;
    }
}