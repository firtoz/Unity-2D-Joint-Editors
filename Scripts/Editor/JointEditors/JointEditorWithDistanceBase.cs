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

public abstract class JointEditorWithDistanceBase<T> : Joint2DEditorBase where T : AnchoredJoint2D {
    public abstract float GetDistance(T joint);
    public abstract void SetDistance(T joint, float distance);

// ReSharper disable StaticFieldInGenericType
    private static readonly HashSet<string> ControlNames = new HashSet<string> {"distance"};
// ReSharper restore StaticFieldInGenericType

    protected override HashSet<string> GetControlNames() {
        return ControlNames;
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        var jointWithDistance = joint2D as T;
        if (!jointWithDistance) {
            return false;
        }

        var mainAnchorPosition = JointHelpers.GetMainAnchorPosition(jointWithDistance);
        var connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(jointWithDistance);
        if (Vector2.Distance(mainAnchorPosition, connectedAnchorPosition) > AnchorEpsilon) {
            using (new HandleColor(Color.green)) {
                Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
            }
        }


        DrawDistance(jointWithDistance, anchorInfo, bias);

        Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);

        return false;
    }

    protected override IEnumerable<Vector2> GetSnapPositions(AnchoredJoint2D joint2D, AnchorInfo anchorInfo,
                                                             JointHelpers.AnchorBias bias, Vector2 anchorPosition) {
        if (!EditorGUI.actionKey) {
            return null;
        }

        var otherBias = bias == JointHelpers.AnchorBias.Main
            ? JointHelpers.AnchorBias.Connected
            : JointHelpers.AnchorBias.Main;

        var jointWithDistance = (T) joint2D;

        var anchorSliderState = StateObject.Get<AnchorSliderState>(anchorInfo.GetControlID("slider"));
        var currentMousePosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
        var currentAnchorPosition = currentMousePosition - anchorSliderState.mouseOffset;

        var otherAnchorPosition = JointHelpers.GetAnchorPosition(jointWithDistance, otherBias);
        var diff = otherAnchorPosition - currentAnchorPosition;
        if (diff.magnitude <= Mathf.Epsilon) {
            diff = -Vector2.up;
        }

        var normalizedDiff = diff.normalized;

        var wantedAnchorPosition = otherAnchorPosition - normalizedDiff * GetDistance(jointWithDistance);

        return new[] {wantedAnchorPosition};
    }

    public override Bounds OnGetFrameBounds() {
        var baseBounds = base.OnGetFrameBounds();

        foreach (var joint2D in targets.Cast<T>()) {
            var mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
            var connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
            var diff = connectedAnchorPosition - mainAnchorPosition;
            if (diff.magnitude <= Mathf.Epsilon) {
                diff = -Vector2.up;
            }
            var normalizedDiff = diff.normalized;
            var wantedMainAnchorPosition = connectedAnchorPosition - normalizedDiff * GetDistance(joint2D);

            baseBounds.Encapsulate(wantedMainAnchorPosition);
        }

        return baseBounds;
    }

    private void DrawDistance(T jointWithDistance, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        if (jointWithDistance == null) {
            return;
        }

        var otherBias = bias == JointHelpers.AnchorBias.Main
            ? JointHelpers.AnchorBias.Connected
            : JointHelpers.AnchorBias.Main;

        var anchorPosition = JointHelpers.GetAnchorPosition(jointWithDistance, bias);
        var otherAnchorPosition = JointHelpers.GetAnchorPosition(jointWithDistance, otherBias);
        var diff = anchorPosition - otherAnchorPosition;
        if (diff.magnitude <= Mathf.Epsilon) {
            diff = Vector2.up * (bias == JointHelpers.AnchorBias.Connected ? 1 : -1);
        }
        var normalizedDiff = diff.normalized;

        JointHelpers.AnchorBias wantedBias;
        switch (GetSettings(jointWithDistance).anchorPriority) {
            case JointSettingsWithBias.AnchorPriority.Main:
                wantedBias = JointHelpers.AnchorBias.Main;
                break;
            case JointSettingsWithBias.AnchorPriority.Connected:
                wantedBias = JointHelpers.AnchorBias.Connected;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (bias == wantedBias && EditorGUI.actionKey && GUIUtility.hotControl == anchorInfo.GetControlID("slider")) {
            Handles.DrawWireDisc(otherAnchorPosition, Vector3.forward, GetDistance(jointWithDistance));
        }

        if (bias != wantedBias) {
            var distanceControlID = anchorInfo.GetControlID("distance");

            EditorGUI.BeginChangeCheck();

            float newDistance;
            using (
                new HandleColor(isCreatedByTarget
                    ? new Color(1, 1, 1, editorSettings.connectedJointTransparency)
                    : Color.white)) {
                newDistance = EditorHelpers.LineSlider(distanceControlID, otherAnchorPosition,
                    GetDistance(jointWithDistance), Helpers2D.GetAngle(normalizedDiff), 0.125f, true);

                EditorHelpers.DrawThickLine(anchorPosition, otherAnchorPosition + normalizedDiff * newDistance,
                    Vector2.Distance(anchorPosition, otherAnchorPosition) > newDistance ? 2 : 1, true);
            }

            if (Event.current.type == EventType.repaint) {
                if (EditorHelpers.IsWarm(distanceControlID) && DragAndDrop.objectReferences.Length == 0) {
                    var labelContent =
                        new GUIContent(string.Format("Distance: {0:0.00}", GetDistance(jointWithDistance)));

                    var sliderPosition = otherAnchorPosition + normalizedDiff * GetDistance(jointWithDistance);

                    var fontSize = HandleUtility.GetHandleSize(sliderPosition) * (1f / 64f);

                    var labelOffset = fontSize * EditorHelpers.FontWithBackgroundStyle.CalcSize(labelContent).y +
                                      fontSize * 20 *
                                      Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * Helpers2D.GetAngle(normalizedDiff)));

                    EditorHelpers.OverlayLabel((Vector3) sliderPosition + (Camera.current.transform.up * labelOffset),
                        labelContent, EditorHelpers.FontWithBackgroundStyle);
                }
            }


            if (EditorGUI.EndChangeCheck()) {
                using (new Modification("Change Distance", jointWithDistance)) {
                    if (newDistance < 0) {
                        SetDistance(jointWithDistance, 0f);
                    } else {
                        var distanceBetweenAnchors = Vector2.Distance(otherAnchorPosition, anchorPosition);
                        SetDistance(jointWithDistance,
                            EditorGUI.actionKey && Mathf.Abs(newDistance - distanceBetweenAnchors) <
                            HandleUtility.GetHandleSize(anchorPosition) * 0.125f
                                ? distanceBetweenAnchors
                                : newDistance);
                    }
                }
            }

            DistanceContext(jointWithDistance, distanceControlID);
        }
    }


    private void DistanceContext(T jointWithDistance, int controlID) {
        var mousePosition = Event.current.mousePosition;

        EditorHelpers.ContextClick(controlID, () => {
            var menu = new GenericMenu();
            AddDistanceContextItem(jointWithDistance, menu, mousePosition);
            menu.ShowAsContext();
        });
    }

    private void AddDistanceContextItem(T jointWithDistance, GenericMenu menu, Vector2 mousePosition) {
        menu.AddItem(new GUIContent("Edit Distance"), false, () =>
            ShowUtility("Edit Distance",
                new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 3),
                delegate(Action close, bool focused) {
                    EditorGUI.BeginChangeCheck();
                    var newDistance = EditorGUILayout.FloatField("Distance", GetDistance(jointWithDistance));
                    if (EditorGUI.EndChangeCheck()) {
                        using (new Modification("Change Distance", jointWithDistance)) {
                            SetDistance(jointWithDistance, newDistance);
                        }
                    }
                    if (GUILayout.Button("Done") ||
                        (Event.current.isKey &&
                         (Event.current.keyCode == KeyCode.Escape) &&
                         focused)) {
                        close();
                    }
                }));
    }

// ReSharper disable StaticFieldInGenericType
    private static readonly GUIContent AngleLimitsModeContent =
// ReSharper restore StaticFieldInGenericType
        new GUIContent("Anchor Priority",
            "Which anchor's angle limits would you like to see? If there is no connected body this setting will be ignored.");

    private void SelectAngleLimitsMode(SerializedObject serializedSettings, bool enabled) {
        EditorGUI.BeginChangeCheck();
        JointSettingsWithBias.AnchorPriority value;

        using (new GUIEnabled(enabled)) {
            var anchorPriority = serializedSettings.FindProperty("anchorPriority");
            EditorGUILayout.PropertyField(anchorPriority, AngleLimitsModeContent);
            value = (JointSettingsWithBias.AnchorPriority)
                Enum.Parse(typeof (JointSettingsWithBias.AnchorPriority),
                    anchorPriority.enumNames[anchorPriority.enumValueIndex]);
        }

        if (EditorGUI.EndChangeCheck()) {
            foreach (var tar in targets) {
                var jointWithDistance = tar as T;
                var settings = GetSettings(jointWithDistance);

                using (new Modification("toggle angle limits display mode", settings)) {
                    settings.anchorPriority = value;
                }
            }
        }
    }

    protected abstract JointSettingsWithBias GetSettings(T jointWithDistance);


    protected override void InspectorDisplayGUI(bool enabled) {
        var allSettings =
            targets.Cast<T>()
                   .Select(jointWithDistance => GetSettings(jointWithDistance))
                   .Where(distanceSettings => distanceSettings != null).Cast<Object>().ToList();

        var serializedSettings = new SerializedObject(allSettings.ToArray());
        SelectAngleLimitsMode(serializedSettings, enabled);
    }

    protected override void OwnershipMoved(AnchoredJoint2D cloneJoint) {
        var jointWithDistance = cloneJoint as T;
        if (!jointWithDistance) {
            return;
        }

        var settings = GetSettings(jointWithDistance);

        if (settings.anchorPriority == JointSettingsWithBias.AnchorPriority.Main) {
            settings.anchorPriority = JointSettingsWithBias.AnchorPriority.Connected;
        } else if (settings.anchorPriority == JointSettingsWithBias.AnchorPriority.Connected) {
            settings.anchorPriority = JointSettingsWithBias.AnchorPriority.Main;
        }
    }


    protected override void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint) {
        base.ExtraMenuItems(menu, joint);

        var jointWithDistance = joint as T;

        var mousePosition = Event.current.mousePosition;

        AddDistanceContextItem(jointWithDistance, menu, mousePosition);
    }
}