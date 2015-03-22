using System;
using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableEditor;
using toxicFork.GUIHelpers.DisposableEditorGUI;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (SliderJoint2D))]
[CanEditMultipleObjects]
public class SliderJoint2DEditor : JointEditorWithAngleBase<SliderJoint2D> {
    private static readonly HashSet<string> ControlNames = new HashSet<string> {
        "sliderAngle",
        "minLimit",
        "maxLimit"
    };

    private const float ONE_OVER16 = 1f / 16f;
    private const float ONE_OVER64 = 1f / 64f;

    protected override HashSet<string> GetControlNames() {
        return ControlNames;
    }

    protected override bool WantsLocking() {
        return true;
    }

    protected override JointSettingsWithBias GetSettings(SliderJoint2D joint2D) {
        return SettingsHelper.GetOrCreate<SliderJoint2DSettings>(joint2D);
    }

    protected override GUIContent GetAngleEditinGUIContent() {
        return new GUIContent("Slider Angle",
            "The translation angle that the joint slides along. [ -1000000, 1000000 ].");
    }

    protected override void SetAngle(SliderJoint2D joint2D, float wantedAngle) {
        joint2D.angle = wantedAngle;
    }

    protected override float GetAngle(SliderJoint2D joint2D) {
        return joint2D.angle;
    }

    protected override Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
        JointHelpers.AnchorBias bias, float snapDistance) {
        if (!EditorGUI.actionKey) {
            return position;
        }

        var sliderJoint2D = (SliderJoint2D) joint;

        var lockAnchors = SettingsHelper.GetOrCreate(sliderJoint2D).lockAnchors;
        var oppositeBias = JointHelpers.GetOppositeBias(bias);
        var oppositeAnchorPosition = JointHelpers.GetAnchorPosition(sliderJoint2D, oppositeBias);

        Vector2[] targetPositions;

        if (joint.connectedBody) {
            targetPositions = new[] {
                GetTargetPosition(joint, JointHelpers.AnchorBias.Main),
                GetTargetPosition(joint, JointHelpers.AnchorBias.Connected)
            };
        }
        else {
            targetPositions = new[] {
                GetTargetPosition(joint, JointHelpers.AnchorBias.Main)
            };
        }

        if (sliderJoint2D.useLimits) {
            Ray slideRay;

            var min = sliderJoint2D.limits.min;
            var max = sliderJoint2D.limits.max;


            if (lockAnchors) {
                slideRay = new Ray(oppositeAnchorPosition,
                    (position - oppositeAnchorPosition).normalized);

                foreach (var targetPosition in targetPositions) {
                    if (Vector2.Distance(oppositeAnchorPosition, targetPosition) <= AnchorEpsilon) {
                        continue;
                    }

                    var fromConnectedToTarget = new Ray(oppositeAnchorPosition,
                        (targetPosition - oppositeAnchorPosition).normalized);

                    if (Helpers2D.DistanceToLine(fromConnectedToTarget, position) >= snapDistance) {
                        continue;
                    }

                    var closestPointToRay = Helpers2D.ClosestPointToRay(fromConnectedToTarget, position);

                    var ray = new Ray(oppositeAnchorPosition, (closestPointToRay - oppositeAnchorPosition).normalized);

                    Vector2 wantedMinPosition = ray.GetPoint(min);
                    Vector2 wantedMaxPosition = ray.GetPoint(max);

                    if (Vector2.Distance(wantedMinPosition, closestPointToRay) < snapDistance) {
                        return wantedMinPosition;
                    }

                    if (Vector2.Distance(wantedMaxPosition, closestPointToRay) < snapDistance) {
                        return wantedMaxPosition;
                    }
                }
            }
            else {
                var worldAngle = sliderJoint2D.transform.eulerAngles.z + sliderJoint2D.angle;

                if (bias == JointHelpers.AnchorBias.Main) {
                    worldAngle += 180;
                }

                slideRay = new Ray(oppositeAnchorPosition,
                    Helpers2D.GetDirection(worldAngle));
            }


            Vector2 minPos = slideRay.GetPoint(min);

            if (Vector2.Distance(position, minPos) < snapDistance) {
                return minPos;
            }


            Vector2 maxPos = slideRay.GetPoint(max);

            if (Vector2.Distance(position, maxPos) < snapDistance) {
                return maxPos;
            }
        }

        if (lockAnchors) {
            //align onto the rays from either target towards the opposite bias
            foreach (var targetPosition in targetPositions) {
                if (Vector2.Distance(targetPosition, oppositeAnchorPosition) <= AnchorEpsilon) {
                    continue;
                }
                var fromConnectedToTarget = new Ray(oppositeAnchorPosition,
                    (targetPosition - oppositeAnchorPosition).normalized);

                if (Helpers2D.DistanceToLine(fromConnectedToTarget, position) < snapDistance) {
                    var closestPointToRay = Helpers2D.ClosestPointToRay(fromConnectedToTarget, position);
                    return closestPointToRay;
                }
            }
        }

        if (!lockAnchors &&
            !(Vector2.Distance(JointHelpers.GetMainAnchorPosition(joint),
                JointHelpers.GetConnectedAnchorPosition(joint)) <= AnchorEpsilon)) {
            var wantedAnchorPosition = GetWantedAnchorPosition(sliderJoint2D, bias, position);

            if (Vector2.Distance(position, wantedAnchorPosition) < snapDistance) {
                return wantedAnchorPosition;
            }
        }

        return position;
    }

    protected override bool DragBothAnchorsWhenLocked() {
        return false;
    }

    protected override void ReAlignAnchors(AnchoredJoint2D joint2D, JointHelpers.AnchorBias alignmentBias) {
        var sliderJoint2D = (SliderJoint2D) joint2D;

        //align the angle to the connected anchor
        var direction = JointHelpers.GetConnectedAnchorPosition(joint2D) -
                        JointHelpers.GetMainAnchorPosition(joint2D);
        if (direction.magnitude > AnchorEpsilon) {
            var wantedAngle = Helpers2D.GetAngle(direction);

            EditorHelpers.RecordUndo("Realign angle", sliderJoint2D);
            sliderJoint2D.angle = wantedAngle - sliderJoint2D.transform.eulerAngles.z;
        }
    }

    protected override Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias) {
        return GetWantedAnchorPosition(anchoredJoint2D, bias, JointHelpers.GetAnchorPosition(anchoredJoint2D, bias));
    }

    private static Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias,
        Vector2 position) {
        var sliderJoint2D = (SliderJoint2D) anchoredJoint2D;

        var otherBias = JointHelpers.GetOppositeBias(bias);

        var worldAngle = sliderJoint2D.transform.eulerAngles.z + sliderJoint2D.angle;

        var slideRay = new Ray(JointHelpers.GetAnchorPosition(sliderJoint2D, otherBias),
            Helpers2D.GetDirection(worldAngle));
        var wantedAnchorPosition = Helpers2D.ClosestPointToRay(slideRay, position);
        return wantedAnchorPosition;
    }

    protected override void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint) {
        base.ExtraMenuItems(menu, joint);

        var sliderJoint2D = joint as SliderJoint2D;
        if (sliderJoint2D != null) {
            var mousePosition = Event.current.mousePosition;

            menu.AddItem(new GUIContent("Use Motor"), sliderJoint2D.useMotor, () => {
                EditorHelpers.RecordUndo("Use Motor", sliderJoint2D);
                sliderJoint2D.useMotor = !sliderJoint2D.useMotor;
                EditorUtility.SetDirty(sliderJoint2D);
            });

            menu.AddItem(new GUIContent("Configure Motor"), false, () =>
                ShowUtility(
                    "Configure Motor",
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 6),
                    delegate(Action close, bool focused) {
                        EditorGUILayout.LabelField(new GUIContent("Slider Joint 2D Motor", "The joint motor."));
                        using (new Indent()) {
                            EditorGUI.BeginChangeCheck();

                            var useMotor =
                                EditorGUILayout.Toggle(
                                    new GUIContent("Use Motor", "Whether to use the joint motor or not."),
                                    sliderJoint2D.useMotor);

                            GUI.SetNextControlName("Motor Config");
                            var motorSpeed = EditorGUILayout.FloatField(
                                new GUIContent("Motor Speed",
                                    "The target motor speed in degrees/second. [-100000, 1000000 ]."),
                                sliderJoint2D.motor.motorSpeed);
                            GUI.SetNextControlName("Motor Config");
                            var maxMotorTorque = EditorGUILayout.FloatField(
                                new GUIContent("Maximum Motor Force",
                                    "The maximum force the motor can use to achieve the desired motor speed. [ 0, 1000000 ]."),
                                sliderJoint2D.motor.maxMotorTorque);

                            if (EditorGUI.EndChangeCheck()) {
                                using (new Modification("Configure Motor", sliderJoint2D)) {
                                    var motor = sliderJoint2D.motor;
                                    motor.motorSpeed = motorSpeed;
                                    motor.maxMotorTorque = maxMotorTorque;
                                    sliderJoint2D.motor = motor;

                                    sliderJoint2D.useMotor = useMotor;
                                }
                            }
                        }


                        if (GUILayout.Button("Done") ||
                            (Event.current.isKey &&
                             (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape) &&
                             focused)) {
                            close();
                        }
                    }));

            menu.AddItem(new GUIContent("Use Limits"), sliderJoint2D.useLimits, () => {
                using (new Modification("Use Limits", sliderJoint2D)) {
                    sliderJoint2D.useLimits = !sliderJoint2D.useLimits;
                }
            });

            menu.AddItem(new GUIContent("Configure Limits"), false, () =>
                ShowUtility(
                    "Configure Limits",
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 6),
                    delegate(Action close, bool focused) {
                        EditorGUILayout.LabelField(new GUIContent("Translation Limits", "The joint translation limits"));
                        using (new Indent()) {
                            EditorGUI.BeginChangeCheck();

                            var useLimits =
                                EditorGUILayout.Toggle(
                                    new GUIContent("Use Limits", "Whether to use the translation limits or not."),
                                    sliderJoint2D.useLimits);

                            GUI.SetNextControlName("Limits Config");
                            var lowerTranslationLimit = EditorGUILayout.FloatField(
                                new GUIContent("Lower Angle",
                                    "The lower translation limit to constrain the joint to. [ -100000, 1000000 ]."),
                                sliderJoint2D.limits.min);
                            GUI.SetNextControlName("Limits Config");
                            var upperTranslationLimit = EditorGUILayout.FloatField(
                                new GUIContent("Upper Angle",
                                    "The upper translation limit to constraint the joint to. [ -100000, 1000000 ]."),
                                sliderJoint2D.limits.max);

                            if (EditorGUI.EndChangeCheck()) {
                                using (new Modification("Configure Limits", sliderJoint2D)) {
                                    var limits2D = sliderJoint2D.limits;
                                    limits2D.min = lowerTranslationLimit;
                                    limits2D.max = upperTranslationLimit;
                                    sliderJoint2D.limits = limits2D;

                                    sliderJoint2D.useLimits = useLimits;
                                }
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
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        var sliderJoint2D = (SliderJoint2D) joint2D;

        var mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);
        var connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(sliderJoint2D);

        var sliderAngleControlID = anchorInfo.GetControlID("sliderAngle");

        if (bias != JointHelpers.AnchorBias.Connected &&
            (
                GUIUtility.hotControl == sliderAngleControlID ||
                GUIUtility.hotControl == anchorInfo.GetControlID("minLimit") ||
                GUIUtility.hotControl == anchorInfo.GetControlID("maxLimit") ||
                !Event.current.shift)) {
            DrawAngleWidget(sliderJoint2D, sliderAngleControlID);

            if (sliderJoint2D.useLimits) {
                HandleLimits(sliderJoint2D, anchorInfo);
            }
        }

        if (GUIUtility.hotControl == anchorInfo.GetControlID("slider")) {
            var snap = GetWantedAnchorPosition(sliderJoint2D, bias);
            using (
                new HandleColor(new Color(1, 1, 1,
                    isCreatedByTarget ? .5f * editorSettings.connectedJointTransparency : .5f))) {
                Handles.DrawLine(connectedAnchorPosition, snap);
                Handles.DrawLine(mainAnchorPosition, snap);
            }
        }

        using (
            new HandleColor(new Color(1, 1, 1,
                0.125f * (isCreatedByTarget ? editorSettings.connectedJointTransparency : 1.0f)))) {
            Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
            if (sliderJoint2D.connectedBody && GUIUtility.hotControl == sliderAngleControlID) {
                Handles.DrawLine(mainAnchorPosition, GetTargetPosition(sliderJoint2D, JointHelpers.AnchorBias.Connected));
            }
        }
        return false;
    }

    protected override string GetAnchorLockTooltip() {
        return "Locking the Slider Joint 2D aligns the connected anchor to the angle of the main anchor.";
    }


    public override Bounds OnGetFrameBounds() {
        var baseBounds = base.OnGetFrameBounds();

        foreach (var joint2D in targets.Cast<SliderJoint2D>()) {
            var mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
            var connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
            var diff = connectedAnchorPosition - mainAnchorPosition;
            if (diff.magnitude <= Mathf.Epsilon) {
                diff = -Vector2.up;
            }
            var normalizedDiff = diff.normalized;

            baseBounds.Encapsulate(mainAnchorPosition + normalizedDiff * joint2D.limits.min);
            baseBounds.Encapsulate(mainAnchorPosition + normalizedDiff * joint2D.limits.max);
            baseBounds.Encapsulate(mainAnchorPosition - normalizedDiff * joint2D.limits.min);
            baseBounds.Encapsulate(mainAnchorPosition - normalizedDiff * joint2D.limits.max);
        }

        return baseBounds;
    }

    private void HandleLimits(SliderJoint2D sliderJoint2D, AnchorInfo anchorInfo) {
        var worldAngle = sliderJoint2D.transform.eulerAngles.z + sliderJoint2D.angle;

        var settings = SettingsHelper.GetOrCreate<SliderJoint2DSettings>(sliderJoint2D);

        JointHelpers.AnchorBias bias;
        switch (settings.anchorPriority) {
            case JointSettingsWithBias.AnchorPriority.Main:
                bias = JointHelpers.AnchorBias.Main;
                break;
            case JointSettingsWithBias.AnchorPriority.Connected:
                bias = JointHelpers.AnchorBias.Connected;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        LimitWidget(sliderJoint2D, anchorInfo, bias, worldAngle);
    }

    private void LimitWidget(SliderJoint2D sliderJoint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias,
        float worldAngle) {
        var anchorPosition = JointHelpers.GetAnchorPosition(sliderJoint2D, bias);

        var oppositeBias = JointHelpers.GetOppositeBias(bias);

        var oppositeAnchorPosition = JointHelpers.GetAnchorPosition(sliderJoint2D, oppositeBias);
        var direction = Helpers2D.GetDirection(worldAngle);
        if (bias == JointHelpers.AnchorBias.Connected) {
            direction *= -1f;
        }
        var delta = oppositeAnchorPosition - anchorPosition;

        var angleDiff = Mathf.DeltaAngle(Helpers2D.GetAngle(delta), worldAngle);

        Vector2 rotatedDelta = Helpers2D.Rotate(angleDiff) * delta;

        var wantedOppositeAnchorPosition = anchorPosition + rotatedDelta;
        var wantedOppositeAnchorPosition2 = anchorPosition - rotatedDelta;

        var minLimitControlID = anchorInfo.GetControlID("minLimit");
        var maxLimitControlID = anchorInfo.GetControlID("maxLimit");

        LimitContext(sliderJoint2D, minLimitControlID, Limit.Min);
        LimitContext(sliderJoint2D, maxLimitControlID, Limit.Max);

        var limitColor = sliderJoint2D.limits.min > sliderJoint2D.limits.max
            ? editorSettings.incorrectLimitsColor
            : editorSettings.correctLimitsColor;

        if (isCreatedByTarget) {
            limitColor.a *= editorSettings.connectedJointTransparency;
        }

        using (new HandleColor(limitColor)) {
            Handles.DrawLine(anchorPosition + direction * sliderJoint2D.limits.min,
                anchorPosition + direction * sliderJoint2D.limits.max);

            if (Event.current.type == EventType.repaint) {
                float fontSize;
                if (EditorHelpers.IsWarm(minLimitControlID) && DragAndDrop.objectReferences.Length == 0) {
                    var labelContent = new GUIContent(string.Format("Min: {0:0.00}", sliderJoint2D.limits.min));

                    var sliderPosition = anchorPosition + (direction) * (sliderJoint2D.limits.min);

                    fontSize = HandleUtility.GetHandleSize(sliderPosition) * ONE_OVER64;

                    var labelOffset = fontSize * EditorHelpers.FontWithBackgroundStyle.CalcSize(labelContent).y +
                                      fontSize * 20 *
                                      Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * Helpers2D.GetAngle(direction)));

                    EditorHelpers.OverlayLabel((Vector3) sliderPosition + (Camera.current.transform.up * labelOffset),
                        labelContent, EditorHelpers.FontWithBackgroundStyle);
                }
                if (EditorHelpers.IsWarm(maxLimitControlID) && DragAndDrop.objectReferences.Length == 0) {
                    var labelContent = new GUIContent(string.Format("Max: {0:0.00}", sliderJoint2D.limits.max));

                    var sliderPosition = anchorPosition + (direction) * (sliderJoint2D.limits.max);

                    fontSize = HandleUtility.GetHandleSize(sliderPosition) * ONE_OVER64;

                    var labelOffset = fontSize * EditorHelpers.FontWithBackgroundStyle.CalcSize(labelContent).y +
                                      fontSize * 20 *
                                      Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * Helpers2D.GetAngle(direction)));

                    EditorHelpers.OverlayLabel((Vector3) sliderPosition + (Camera.current.transform.up * labelOffset),
                        labelContent, EditorHelpers.FontWithBackgroundStyle);
                }
            }

            if (GUIUtility.hotControl == minLimitControlID ||
                GUIUtility.hotControl == maxLimitControlID) {
                using (
                    new HandleColor(new Color(1, 1, 1,
                        0.25f * (isCreatedByTarget ? editorSettings.connectedJointTransparency : 1.0f)))) {
                    var handleSize = HandleUtility.GetHandleSize(wantedOppositeAnchorPosition) * ONE_OVER16;

                    Handles.DrawLine(wantedOppositeAnchorPosition - direction * handleSize,
                        wantedOppositeAnchorPosition + direction * handleSize);
                    handleSize = HandleUtility.GetHandleSize(wantedOppositeAnchorPosition2) * ONE_OVER16;
                    Handles.DrawLine(wantedOppositeAnchorPosition2 - direction * handleSize,
                        wantedOppositeAnchorPosition2 + direction * handleSize);
                    Handles.DrawWireArc(anchorPosition, Vector3.forward, wantedOppositeAnchorPosition, 360,
                        Vector2.Distance(wantedOppositeAnchorPosition, anchorPosition));
                }
            }


            var actionKey = EditorGUI.actionKey;

            List<Vector2> snapList = null;
            if (actionKey) {
                snapList = new List<Vector2> {
                    anchorPosition,
                    wantedOppositeAnchorPosition,
                    wantedOppositeAnchorPosition2
                };
            }

            var minLimitColor = editorSettings.minLimitColor;
            var maxLimitColor = editorSettings.maxLimitColor;

            if (isCreatedByTarget) {
                minLimitColor.a *= editorSettings.connectedJointTransparency;
                maxLimitColor.a *= editorSettings.connectedJointTransparency;
            }

            using (new HandleColor(minLimitColor)) {
                DrawLimitSlider(sliderJoint2D, minLimitControlID, anchorPosition, direction, snapList, Limit.Min);
            }
            using (new HandleColor(maxLimitColor)) {
                DrawLimitSlider(sliderJoint2D, maxLimitControlID, anchorPosition, direction, snapList, Limit.Max);
            }
        }
    }

    private static void DrawLimitSlider(SliderJoint2D sliderJoint2D, int limitControlID, Vector2 anchorPosition,
        Vector2 direction, IEnumerable<Vector2> snapList, Limit limit) {
        EditorGUI.BeginChangeCheck();

        float val;
        var limits2D = sliderJoint2D.limits;
        switch (limit) {
            case Limit.Min:
                val = limits2D.min;
                break;
            case Limit.Max:
                val = limits2D.max;
                break;
            default:
                throw new ArgumentOutOfRangeException("limit");
        }

        var newLimit = EditorHelpers.LineSlider(limitControlID, anchorPosition,
            val,
            Helpers2D.GetAngle(direction), 0.125f, false, limit == Limit.Min);

        if (!EditorGUI.EndChangeCheck()) {
            return;
        }

        if (snapList != null) {
            var limitSnapList = new List<Vector2>(snapList);
            switch (limit) {
                case Limit.Min:
                    limitSnapList.Add(anchorPosition + direction * limits2D.max);
                    break;
                case Limit.Max:
                    limitSnapList.Add(anchorPosition + direction * limits2D.min);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("limit");
            }

            var limitGUIPosition =
                HandleUtility.WorldToGUIPoint(anchorPosition + direction * newLimit);

            foreach (
                var snapPosition in 
                    from snapPosition in limitSnapList
                    let snapGUIPosition = HandleUtility.WorldToGUIPoint(snapPosition)
                    where Vector2.Distance(limitGUIPosition, snapGUIPosition) < 10
                    select snapPosition) {
                newLimit = Helpers2D.DistanceAlongLine(new Ray(anchorPosition, direction),
                    snapPosition);
            }
        }


        EditorHelpers.RecordUndo("Change slider limit", sliderJoint2D);
        switch (limit) {
            case Limit.Min:
                limits2D.min = newLimit;
                break;
            case Limit.Max:
                limits2D.max = newLimit;
                break;
            default:
                throw new ArgumentOutOfRangeException("limit");
        }
        sliderJoint2D.limits = limits2D;
    }

    private void LimitContext(SliderJoint2D sliderJoint2D, int controlID, Limit limit) {
        var mousePosition = Event.current.mousePosition;

        var limitName = (limit == Limit.Min ? "Lower" : "Upper") + " Translation Limit";

        EditorHelpers.ContextClick(controlID, () => {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Edit " + limitName), false, () =>
                ShowUtility(
                    "Edit " + limitName,
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 3),
                    delegate(Action close, bool focused) {
                        EditorGUI.BeginChangeCheck();
                        GUI.SetNextControlName(limitName);
                        var newLimit = EditorGUILayout.FloatField(limitName,
                            limit == Limit.Min
                                ? sliderJoint2D.limits.min
                                : sliderJoint2D.limits.max);
                        if (EditorGUI.EndChangeCheck()) {
                            var limits = sliderJoint2D.limits;
                            if (limit == Limit.Min) {
                                limits.min = newLimit;
                            }
                            else {
                                limits.max = newLimit;
                            }
                            EditorHelpers.RecordUndo(limitName, sliderJoint2D);
                            sliderJoint2D.limits = limits;
                            EditorUtility.SetDirty(sliderJoint2D);
                        }
                        if (GUILayout.Button("Done") ||
                            (Event.current.isKey &&
                             (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape) &&
                             focused)) {
                            close();
                        }
                    }));
            menu.ShowAsContext();
        });
    }


    protected override void OwnershipMoved(AnchoredJoint2D cloneJoint) {
        //swap limits
        var sliderJoint2D = cloneJoint as SliderJoint2D;
        if (!sliderJoint2D) {
            return;
        }


        var settings = SettingsHelper.GetOrCreate<SliderJoint2DSettings>(sliderJoint2D);

        if (settings.anchorPriority == JointSettingsWithBias.AnchorPriority.Main) {
            settings.anchorPriority = JointSettingsWithBias.AnchorPriority.Connected;
        }
        else if (settings.anchorPriority == JointSettingsWithBias.AnchorPriority.Connected) {
            settings.anchorPriority = JointSettingsWithBias.AnchorPriority.Main;
        }

        var worldAngle = sliderJoint2D.connectedBody.transform.eulerAngles.z + sliderJoint2D.angle;

        sliderJoint2D.angle = (180.0f + worldAngle) - sliderJoint2D.transform.eulerAngles.z;
    }

    protected override bool PostAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo info, List<Vector2> otherAnchors,
        JointHelpers.AnchorBias bias) {
        var sliderJoint2D = joint2D as SliderJoint2D;
        if (sliderJoint2D == null) {
            return false;
        }

        if (Event.current.type == EventType.repaint) {
            if (!EditorHelpers.IsWarm(info.GetControlID("sliderAngle")) || DragAndDrop.objectReferences.Length != 0) {
                return false;
            }
            var suspensionAngle = sliderJoint2D.angle;

            var labelContent = new GUIContent(String.Format("{0:0.00}", suspensionAngle));
            Vector3 mainAnchorPosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);

            var fontSize = HandleUtility.GetHandleSize(mainAnchorPosition) * ONE_OVER64;

            var labelOffset = fontSize * EditorHelpers.FontWithBackgroundStyle.CalcSize(labelContent).y;

            EditorHelpers.OverlayLabel(mainAnchorPosition + (Camera.current.transform.up * labelOffset),
                labelContent,
                EditorHelpers.FontWithBackgroundStyle);
        }
        else {
            if (!EditorHelpers.IsWarm(info.GetControlID("sliderAngle")) || DragAndDrop.objectReferences.Length != 0) {
                return false;
            }
            if (SceneView.lastActiveSceneView) {
                SceneView.lastActiveSceneView.Repaint();
            }
        }

        return false;
    }

    protected override void DrawCustomInspector() {
//        float? referenceAngle = null;
//        var mixedValue = false;
//
//        foreach (
//            var targetReferenceAngle in
//                targets.Select(currentTarget => currentTarget as SliderJoint2D)
//                    .Select(targetSliderJoint => targetSliderJoint.referenceAngle)) {
//
//            if (referenceAngle == null) {
//                referenceAngle = targetReferenceAngle;
//                continue;
//            }
//
//            if (Mathf.Approximately(referenceAngle.Value, targetReferenceAngle)) {
//                continue;
//            }
//
//            mixedValue = true;
//            referenceAngle = targetReferenceAngle;
//        }
//
//        if (referenceAngle == null) {
//            return;
//        }
//
//        var showMixedValue = EditorGUI.showMixedValue;
//        EditorGUI.showMixedValue = mixedValue;
//        EditorGUI.BeginDisabledGroup(true);
//        EditorGUI.BeginChangeCheck();
//        EditorGUILayout.FloatField("Reference Angle", referenceAngle.Value);
//        EditorGUI.EndDisabledGroup();
//        EditorGUI.showMixedValue = showMixedValue;
    }
}