using System;
using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableEditor;
using toxicFork.GUIHelpers.DisposableGUI;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof (SpringJoint2D))]
[CanEditMultipleObjects]
public class SpringJoint2DEditor : Joint2DEditorBase {
    private static readonly HashSet<string> ControlNames = new HashSet<string> {"distance"};

    protected override HashSet<string> GetControlNames() {
        return ControlNames;
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        SpringJoint2D springJoint2D = (SpringJoint2D)joint2D;

//        Vector2 center = JointHelpers.GetAnchorPosition(springJoint2D, bias);
//        float scale = editorSettings.anchorScale;
//        float handleSize = HandleUtility.GetHandleSize(center)*scale;

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(springJoint2D);
        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(springJoint2D);
        if (Vector2.Distance(mainAnchorPosition, connectedAnchorPosition) > AnchorEpsilon) {
            using (new HandleColor(Color.green)) {
                Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
            }
        }

        DrawDistance(springJoint2D, anchorInfo, bias);

        Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);

//        if (bias == JointHelpers.AnchorBias.Main) {
//            Vector2 mainBodyPosition = GetTargetPosition(springJoint2D, JointHelpers.AnchorBias.Main);
//            using (new HandleColor(editorSettings.anchorsToMainBodyColor)) {
//                if (Vector2.Distance(mainBodyPosition, center) > AnchorEpsilon) {
//                    Handles.DrawLine(mainBodyPosition, center);
//                }
//            }
//        }
//        else if (bias == JointHelpers.AnchorBias.Connected) {
//            Vector2 connectedBodyPosition = GetTargetPosition(springJoint2D, JointHelpers.AnchorBias.Connected);
//            if (springJoint2D.connectedBody) {
//                using (new HandleColor(editorSettings.anchorsToConnectedBodyColor)) {
//                    if (Vector2.Distance(connectedBodyPosition, center) > AnchorEpsilon) {
//                        Handles.DrawLine(connectedBodyPosition, center);
//                    }
//                    else {
//                        float rot = JointHelpers.GetTargetRotation(springJoint2D, JointHelpers.AnchorBias.Connected);
//                        Handles.DrawLine(center, center + Helpers2D.GetDirection(rot)*handleSize);
//                    }
//                }
//            }
//        }
        return false;
    }

    protected override Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
        JointHelpers.AnchorBias bias, float snapDistance) {
        if (!EditorGUI.actionKey) {
            return position;
        }

        JointHelpers.AnchorBias otherBias = JointHelpers.GetOppositeBias(bias);

        SpringJoint2D springJoint2D = (SpringJoint2D)joint;

        AnchorSliderState anchorSliderState = StateObject.Get<AnchorSliderState>(sliderID);
        Vector2 currentMousePosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
        Vector2 currentAnchorPosition = currentMousePosition - anchorSliderState.mouseOffset;

        Vector2 otherAnchorPosition = JointHelpers.GetAnchorPosition(springJoint2D, otherBias);
        Vector2 diff = otherAnchorPosition - currentAnchorPosition;
        if (diff.magnitude <= Mathf.Epsilon) {
            diff = -Vector2.up;
        }

        Vector2 normalizedDiff = diff.normalized;

        Vector2 wantedAnchorPosition = otherAnchorPosition - normalizedDiff*springJoint2D.distance;

        Vector2 mainTargetPosition = JointHelpers.GetTargetPosition(springJoint2D, JointHelpers.AnchorBias.Main);
        if (Vector2.Distance(position, mainTargetPosition) < snapDistance)
        {
            return mainTargetPosition;
        }

        if (springJoint2D.connectedBody)
        {
            Vector2 connectedTargetPosition = JointHelpers.GetTargetPosition(springJoint2D, JointHelpers.AnchorBias.Connected);
            if (Vector2.Distance(position, connectedTargetPosition) < snapDistance)
            {
                return connectedTargetPosition;
            }
        }

        if (Vector2.Distance(position, wantedAnchorPosition) < snapDistance) {
            return wantedAnchorPosition;
        }

        return position;
    }

    public override Bounds OnGetFrameBounds() {
        Bounds baseBounds = base.OnGetFrameBounds();

        foreach (SpringJoint2D joint2D in targets.Cast<SpringJoint2D>())
        {
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

    private void DrawDistance(SpringJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias)
    {
        if (joint2D == null) {
            return;
        }

        JointHelpers.AnchorBias otherBias = bias == JointHelpers.AnchorBias.Main
            ? JointHelpers.AnchorBias.Connected
            : JointHelpers.AnchorBias.Main;

        Vector2 anchorPosition = JointHelpers.GetAnchorPosition(joint2D, bias);
        Vector2 otherAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, otherBias);
        Vector2 diff = anchorPosition - otherAnchorPosition;
        if (diff.magnitude <= Mathf.Epsilon) {
            diff = Vector2.up*(bias == JointHelpers.AnchorBias.Connected ? 1 : -1);
        }
        Vector2 normalizedDiff = diff.normalized;

        JointHelpers.AnchorBias wantedBias;
        switch (SettingsHelper.GetOrCreate<SpringJoint2DSettings>(joint2D).anchorPriority) {
            case DistanceJoint2DSettings.AnchorPriority.Main:
                wantedBias = JointHelpers.AnchorBias.Main;
                break;
            case DistanceJoint2DSettings.AnchorPriority.Connected:
                wantedBias = JointHelpers.AnchorBias.Connected;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (EditorGUI.actionKey && GUIUtility.hotControl == anchorInfo.GetControlID("slider"))
        {
            Handles.DrawWireDisc(otherAnchorPosition, Vector3.forward, joint2D.distance); 
        }

        if (bias != wantedBias )
        {
            int distanceControlID = anchorInfo.GetControlID("distance");

            EditorGUI.BeginChangeCheck();
            float newDistance = EditorHelpers.LineSlider(distanceControlID, otherAnchorPosition, joint2D.distance,
                Helpers2D.GetAngle(normalizedDiff), 0.125f, true);

            EditorHelpers.DrawThickLine(anchorPosition, otherAnchorPosition + normalizedDiff*newDistance,
                Vector2.Distance(anchorPosition, otherAnchorPosition) > newDistance ? 2 : 1, true);

            if (EditorGUI.EndChangeCheck()) {
                using (new Modification("Change Distance", joint2D)) {
                    if (newDistance < 0) {
                        joint2D.distance = 0f;
                    }
                    else {
                        float distanceBetweenAnchors = Vector2.Distance(otherAnchorPosition, anchorPosition);
                        joint2D.distance = EditorGUI.actionKey && Mathf.Abs(newDistance - distanceBetweenAnchors) <
                                           HandleUtility.GetHandleSize(anchorPosition)*0.125f
                            ? distanceBetweenAnchors
                            : newDistance;
                    }
                }
            }

            DistanceContext(joint2D, distanceControlID);
        }
    }

    private void DistanceContext(SpringJoint2D springJoint2D, int controlID)
    {
        Vector2 mousePosition = Event.current.mousePosition;

        EditorHelpers.ContextClick(controlID, () =>
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Edit Distance"), false, () =>
                ShowUtility(
                    "Edit Distance",
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 3),
                    delegate(Action close, bool focused)
                    {
                        EditorGUI.BeginChangeCheck();
                        float newDistance = EditorGUILayout.FloatField("Distance", springJoint2D.distance);
                        if (EditorGUI.EndChangeCheck())
                        {
                            using (new Modification("Change Distance", springJoint2D))
                            {
                                springJoint2D.distance = newDistance;
                            }
                        }
                        if (GUILayout.Button("Done") ||
                            (Event.current.isKey &&
                             (Event.current.keyCode == KeyCode.Escape) &&
                             focused))
                        {
                            close();
                        }
                    }));
            menu.ShowAsContext();
        });
    }

    private static readonly GUIContent AngleLimitsModeContent =
        new GUIContent("Anchor Priority",
            "Which anchor's angle limits would you like to see? If there is no connected body this setting will be ignored.");

    private void SelectAngleLimitsMode(SerializedObject serializedSettings, bool enabled)
    {
        EditorGUI.BeginChangeCheck();
        DistanceJoint2DSettings.AnchorPriority value;

        using (new GUIEnabled(enabled))
        {
            SerializedProperty anchorPriority = serializedSettings.FindProperty("anchorPriority");
            EditorGUILayout.PropertyField(anchorPriority, AngleLimitsModeContent);
            value = (DistanceJoint2DSettings.AnchorPriority)
                Enum.Parse(typeof(DistanceJoint2DSettings.AnchorPriority),
                    anchorPriority.enumNames[anchorPriority.enumValueIndex]);
        }

        if (EditorGUI.EndChangeCheck())
        {
            foreach (Object t in targets)
            {
                SpringJoint2D springJoint2D = (SpringJoint2D)t;
                SpringJoint2DSettings settings = SettingsHelper.GetOrCreate<SpringJoint2DSettings>(springJoint2D);

                EditorHelpers.RecordUndo("toggle angle limits display mode", settings);
                settings.anchorPriority = value;
                EditorUtility.SetDirty(settings);
            }
        }
    }


    protected override void InspectorDisplayGUI(bool enabled)
    {
        IEnumerable<SpringJoint2D> springJoints2D = targets.Cast<SpringJoint2D>();

        List<Object> allSettings =
            springJoints2D
                .Select(springJoint2D => SettingsHelper.GetOrCreate<SpringJoint2DSettings>(springJoint2D))
                .Where(springSettings => springSettings != null).Cast<Object>().ToList();

        SerializedObject serializedSettings = new SerializedObject(allSettings.ToArray());
        SelectAngleLimitsMode(serializedSettings, enabled);
    }

    protected override void OwnershipMoved(AnchoredJoint2D cloneJoint)
    {
        SpringJoint2D springJoint2D = cloneJoint as SpringJoint2D;
        if (!springJoint2D)
        {
            return;
        }

        SpringJoint2DSettings settings = SettingsHelper.GetOrCreate<SpringJoint2DSettings>(springJoint2D);

        if (settings.anchorPriority == DistanceJoint2DSettings.AnchorPriority.Main)
        {
            settings.anchorPriority = DistanceJoint2DSettings.AnchorPriority.Connected;
        }
        else if (settings.anchorPriority == DistanceJoint2DSettings.AnchorPriority.Connected)
        {
            settings.anchorPriority = DistanceJoint2DSettings.AnchorPriority.Main;
        }
    }
}