using System;
using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableEditor;
using toxicFork.GUIHelpers.DisposableGUI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public abstract class JointEditorWithAngleBase<TJointType> : Joint2DEditorBase where TJointType : AnchoredJoint2D {
    protected override bool WantsLocking() {
        return true;
    }

    protected override void InspectorDisplayGUI(bool enabled) {
        var allSettings =
            targets.Cast<TJointType>()
                .Select(joint2D => GetSettings(joint2D))
                .Where(jointSettings => jointSettings != null).Cast<Object>().ToList();

        var serializedSettings = new SerializedObject(allSettings.ToArray());
        SelectAngleLimitsMode(serializedSettings, enabled);
    }

    protected abstract JointSettingsWithBias GetSettings(TJointType joint2D);

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
                var joint2D = (TJointType) tar;
                var settings = GetSettings(joint2D);

                EditorHelpers.RecordUndo("toggle angle limits display mode", settings);
                settings.anchorPriority = value;
                EditorUtility.SetDirty(settings);
            }
        }
    }

    protected abstract GUIContent GetAngleEditinGUIContent();

    protected override void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint) {
        base.ExtraMenuItems(menu, joint);

        var sliderJoint2D = joint as TJointType;
        if (sliderJoint2D == null) {
            return;
        }

        var mousePosition = Event.current.mousePosition;

        AddEditAngleMenuItem(sliderJoint2D, menu, mousePosition);
    }

    protected void AddEditAngleMenuItem(TJointType joint2D, GenericMenu menu, Vector2 mousePosition) {
        var joint2DSettings = GetSettings(joint2D);
        var mainAnchorPosition = JointHelpers.GetMainAnchorPosition(joint2D);

        var guiContent = GetAngleEditinGUIContent();

        menu.AddItem(new GUIContent("Edit " + guiContent.text), false,
            () =>
                ShowUtility(
                    "Edit " + guiContent.text,
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 3),
                    delegate(Action close, bool focused) {
                        EditorGUI.BeginChangeCheck();
                        var newAngle =
                            EditorGUILayout.FloatField(
                                guiContent,
                                GetAngle(joint2D));
                        if (EditorGUI.EndChangeCheck()) {
                            using (new Modification(guiContent.text, joint2D)) {
                                if (joint2DSettings.lockAnchors) {
                                    var angleDelta = Mathf.DeltaAngle(GetAngle(joint2D), newAngle);

                                    var connectedAnchorPosition =
                                        JointHelpers.GetConnectedAnchorPosition(joint2D);
                                    var connectedOffset = connectedAnchorPosition - mainAnchorPosition;

                                    JointHelpers.SetWorldConnectedAnchorPosition(joint2D,
                                        mainAnchorPosition +
                                        (Vector2) (Helpers2D.Rotate(angleDelta) * connectedOffset));
                                }

                                SetAngle(joint2D, newAngle);
                            }
                        }
                        if (GUILayout.Button("Done") ||
                            (Event.current.isKey &&
                             (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape) &&
                             focused)) {
                            close();
                        }
                    }));
    }

    protected void DrawAngleWidget(TJointType joint2D, int controlID) {
        var joint2DSettings = GetSettings(joint2D);

        var worldAngle = joint2D.transform.eulerAngles.z + GetAngle(joint2D);

        HandleDragDrop(controlID, joint2D, joint2DSettings);

        EditorGUI.BeginChangeCheck();

        JointHelpers.AnchorBias bias;

        if (joint2DSettings.anchorPriority == JointSettingsWithBias.AnchorPriority.Main) {
            bias = JointHelpers.AnchorBias.Main;
        }
        else {
            bias = JointHelpers.AnchorBias.Connected;
        }

        var oppositeBias = JointHelpers.GetOppositeBias(bias);

        var angleWidgetPosition = JointHelpers.GetAnchorPosition(joint2D, bias);
        var otherAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, oppositeBias);

        var offsetToOther = otherAnchorPosition - angleWidgetPosition;

        var newAngle = LineAngleHandle(controlID, worldAngle, angleWidgetPosition, 0.5f, 2);

        var mousePosition = Event.current.mousePosition;

        EditorHelpers.ContextClick(controlID, () => {
            var menu = new GenericMenu();
            AddEditAngleMenuItem(joint2D, menu, mousePosition);
            menu.ShowAsContext();
        });

        if (!EditorGUI.EndChangeCheck()) {
            return;
        }
        var snapped = false;

        if (EditorGUI.actionKey) {
            var handleSize = HandleUtility.GetHandleSize(angleWidgetPosition);

            var mousePosition2D = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);

            var currentAngleRay = new Ray(angleWidgetPosition, Helpers2D.GetDirection(newAngle));

            var mousePositionProjectedToAngle = Helpers2D.ClosestPointToRay(currentAngleRay, mousePosition2D);

            var directionsToSnapTo = new List<Vector2> {
                (GetTargetPosition(joint2D, bias) - angleWidgetPosition).normalized
            };

            if (!joint2DSettings.lockAnchors) {
                directionsToSnapTo.Insert(0, offsetToOther.normalized);
            }

            if (joint2D.connectedBody) {
                directionsToSnapTo.Add(
                    (GetTargetPosition(joint2D, oppositeBias) - angleWidgetPosition)
                        .normalized);
            }

            foreach (var direction in directionsToSnapTo) {
                var rayTowardsDirection = new Ray(angleWidgetPosition, direction);

                var closestPointTowardsDirection = Helpers2D.ClosestPointToRay(rayTowardsDirection,
                    mousePositionProjectedToAngle);

                if (Vector2.Distance(closestPointTowardsDirection, mousePositionProjectedToAngle) <
                    handleSize * 0.125f) {
                    var currentDirection = Helpers2D.GetDirection(newAngle);
                    var closestPositionToDirection =
                        Helpers2D.ClosestPointToRay(rayTowardsDirection,
                            angleWidgetPosition + currentDirection);

                    snapped = true;
                    newAngle = Helpers2D.GetAngle(closestPositionToDirection - angleWidgetPosition);

                    break;
                }
            }
        }

        var wantedAngle = newAngle - joint2D.transform.eulerAngles.z;

        if (!snapped) {
            wantedAngle = Handles.SnapValue(wantedAngle, editorSettings.snapAngle);
        }

        EditorHelpers.RecordUndo("Alter Angle", joint2D);

        if (joint2DSettings.lockAnchors) {
            var angleDelta = Mathf.DeltaAngle(GetAngle(joint2D), wantedAngle);

            JointHelpers.SetWorldAnchorPosition(joint2D,
                angleWidgetPosition + (Vector2) (Helpers2D.Rotate(angleDelta) * offsetToOther), oppositeBias);
        }

        SetAngle(joint2D, wantedAngle);
    }

    protected abstract void SetAngle(TJointType joint2D, float wantedAngle);

    protected abstract float GetAngle(TJointType joint2D);
}