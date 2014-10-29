using System;
using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableEditor;
using toxicFork.GUIHelpers.DisposableEditorGUI;
using toxicFork.GUIHelpers.DisposableGUI;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof (SliderJoint2D))]
[CanEditMultipleObjects]
public class SliderJoint2DEditor : Joint2DEditor {
    private static readonly HashSet<string> ControlNames = new HashSet<string> {
        "sliderAngle",
        "minLimit",
        "maxLimit"
    };

    protected override HashSet<string> GetControlNames() {
        return ControlNames;
    }

    protected override bool WantsLocking() {
        return true;
    }

    protected override Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
        JointHelpers.AnchorBias bias, float snapDistance) {
        if (!EditorGUI.actionKey) {
            return position;
        }


        SliderJoint2D sliderJoint2D = (SliderJoint2D) joint;

        bool lockAnchors = SettingsHelper.GetOrCreate(sliderJoint2D).lockAnchors;
        JointHelpers.AnchorBias oppositeBias = JointHelpers.GetOppositeBias(bias);
        Vector2 oppositeAnchorPosition = JointHelpers.GetAnchorPosition(sliderJoint2D, oppositeBias);

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

            float min = sliderJoint2D.limits.min;
            float max = sliderJoint2D.limits.max;


            if (lockAnchors) {
                slideRay = new Ray(oppositeAnchorPosition,
                    (position - oppositeAnchorPosition).normalized);

//                if (Helpers2D.DistanceToLine(slideRay, position) < snapDistance) {
//                    
//                }

//                Vector2 minPos = slideRay.GetPoint(min);
//
//                if (Vector2.Distance(position, minPos) < snapDistance)
//                {
//                    return minPos;
//                }
//
//
//                Vector2 maxPos = slideRay.GetPoint(max);
//                Debug.DrawLine(position, maxPos);
//
//
//                if (Vector2.Distance(position, maxPos) < snapDistance)
//                {
//                    return maxPos;
//                }

                foreach (Vector2 targetPosition in targetPositions) {
                    if (Vector2.Distance(oppositeAnchorPosition, targetPosition) <= AnchorEpsilon) {
                        continue;
                    }

                    Ray fromConnectedToTarget = new Ray(oppositeAnchorPosition,
                        (targetPosition - oppositeAnchorPosition).normalized);

                    if (Helpers2D.DistanceToLine(fromConnectedToTarget, position) >= snapDistance) {
                        continue;
                    }

                    Vector2 closestPointToRay = Helpers2D.ClosestPointToRay(fromConnectedToTarget, position);

                    Ray ray = new Ray(oppositeAnchorPosition, (closestPointToRay - oppositeAnchorPosition).normalized);

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
                float worldAngle = sliderJoint2D.transform.eulerAngles.z + sliderJoint2D.angle;

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
            Debug.DrawLine(position, maxPos);


            if (Vector2.Distance(position, maxPos) < snapDistance) {
                return maxPos;
            }
        }

        if (lockAnchors) {
            //align onto the rays from either target towards the opposite bias
            foreach (Vector2 targetPosition in targetPositions) {
                if (Vector2.Distance(targetPosition, oppositeAnchorPosition) <= AnchorEpsilon) {
                    continue;
                }
                Ray fromConnectedToTarget = new Ray(oppositeAnchorPosition,
                    (targetPosition - oppositeAnchorPosition).normalized);

                if (Helpers2D.DistanceToLine(fromConnectedToTarget, position) < snapDistance) {
                    Vector2 closestPointToRay = Helpers2D.ClosestPointToRay(fromConnectedToTarget, position);
                    return closestPointToRay;
                }
            }
        }

        if (!lockAnchors &&
            !(Vector2.Distance(JointHelpers.GetMainAnchorPosition(joint),
                JointHelpers.GetConnectedAnchorPosition(joint)) <= AnchorEpsilon)) {
            Vector2 wantedAnchorPosition = GetWantedAnchorPosition(sliderJoint2D, bias, position);

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
        SliderJoint2D sliderJoint2D = (SliderJoint2D) joint2D;

        //align the angle to the connected anchor
        Vector2 direction = JointHelpers.GetConnectedAnchorPosition(joint2D) -
                            JointHelpers.GetMainAnchorPosition(joint2D);
        if (direction.magnitude > AnchorEpsilon) {
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
        Vector2 wantedAnchorPosition = Helpers2D.ClosestPointToRay(slideRay, position);
        return wantedAnchorPosition;
    }


    protected override void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint) {
        SliderJoint2D sliderJoint2D = joint as SliderJoint2D;
        if (sliderJoint2D != null) {
            Vector2 mousePosition = Event.current.mousePosition;

            AddEditSliderAngleMenuItem(sliderJoint2D, menu, mousePosition);

            menu.AddItem(new GUIContent("Use Motor"), sliderJoint2D.useMotor, () => {
                EditorHelpers.RecordUndo("Use Motor", sliderJoint2D);
                sliderJoint2D.useMotor = !sliderJoint2D.useMotor;
                EditorUtility.SetDirty(sliderJoint2D);
            });


            menu.AddItem(new GUIContent("Configure Motor"), false, () =>
                EditorHelpers.ShowDropDown(
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight*6),
                    delegate(Action close, bool focused) {
                        EditorGUILayout.LabelField(new GUIContent("Slider Joint 2D Motor", "The joint motor."));
                        using (new Indent()) {
                            EditorGUI.BeginChangeCheck();

                            bool useMotor =
                                EditorGUILayout.Toggle(
                                    new GUIContent("Use Motor", "Whether to use the joint motor or not."),
                                    sliderJoint2D.useMotor);

                            GUI.SetNextControlName("Motor Config");
                            float motorSpeed = EditorGUILayout.FloatField(
                                new GUIContent("Motor Speed",
                                    "The target motor speed in degrees/second. [-100000, 1000000 ]."),
                                sliderJoint2D.motor.motorSpeed);
                            GUI.SetNextControlName("Motor Config");
                            float maxMotorTorque = EditorGUILayout.FloatField(
                                new GUIContent("Maximum Motor Force",
                                    "The maximum force the motor can use to achieve the desired motor speed. [ 0, 1000000 ]."),
                                sliderJoint2D.motor.maxMotorTorque);

                            if (EditorGUI.EndChangeCheck()) {
                                using (new Modification("Configure Motor", sliderJoint2D)) {
                                    JointMotor2D motor = sliderJoint2D.motor;
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
                EditorHelpers.ShowDropDown(
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight*6),
                    delegate(Action close, bool focused) {
                        EditorGUILayout.LabelField(new GUIContent("Translation Limits", "The joint translation limits"));
                        using (new Indent()) {
                            EditorGUI.BeginChangeCheck();

                            bool useLimits =
                                EditorGUILayout.Toggle(
                                    new GUIContent("Use Limits", "Whether to use the translation limits or not."),
                                    sliderJoint2D.useLimits);

                            GUI.SetNextControlName("Limits Config");
                            float lowerTranslationLimit = EditorGUILayout.FloatField(
                                new GUIContent("Lower Angle",
                                    "The lower translation limit to constrain the joint to. [ -100000, 1000000 ]."),
                                sliderJoint2D.limits.min);
                            GUI.SetNextControlName("Limits Config");
                            float upperTranslationLimit = EditorGUILayout.FloatField(
                                new GUIContent("Upper Angle",
                                    "The upper translation limit to constraint the joint to. [ -100000, 1000000 ]."),
                                sliderJoint2D.limits.max);

                            if (EditorGUI.EndChangeCheck()) {
                                using (new Modification("Configure Limits", sliderJoint2D)) {
                                    JointTranslationLimits2D limits2D = sliderJoint2D.limits;
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
        SliderJoint2D sliderJoint2D = (SliderJoint2D) joint2D;

//        Vector2 center = JointHelpers.GetAnchorPosition(sliderJoint2D, bias);

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);
        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(sliderJoint2D);

        int sliderAngleControlID = anchorInfo.GetControlID("sliderAngle");

        if (bias != JointHelpers.AnchorBias.Connected &&
            (
                GUIUtility.hotControl == sliderAngleControlID ||
                GUIUtility.hotControl == anchorInfo.GetControlID("minLimit") ||
                GUIUtility.hotControl == anchorInfo.GetControlID("maxLimit") ||
                !Event.current.shift)) {
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
            if (sliderJoint2D.connectedBody && GUIUtility.hotControl == sliderAngleControlID) {
                Handles.DrawLine(mainAnchorPosition, GetTargetPosition(sliderJoint2D, JointHelpers.AnchorBias.Connected));
            }
        }

//        if (bias == JointHelpers.AnchorBias.Main) {
//            Vector2 mainBodyPosition = GetTargetPosition(sliderJoint2D, JointHelpers.AnchorBias.Main);
//            using (new HandleColor(editorSettings.anchorsToMainBodyColor)) {
//                if (Vector2.Distance(mainBodyPosition, center) > AnchorEpsilon) {
//                    Handles.DrawLine(mainBodyPosition, center);
//                }
//            }
//        }
//        else if (bias == JointHelpers.AnchorBias.Connected) {
//            Vector2 connectedBodyPosition = GetTargetPosition(sliderJoint2D, JointHelpers.AnchorBias.Connected);
//            if (sliderJoint2D.connectedBody) {
//                using (new HandleColor(editorSettings.anchorsToConnectedBodyColor)) {
//                    if (Vector2.Distance(connectedBodyPosition, center) > AnchorEpsilon) {
//                        Handles.DrawLine(connectedBodyPosition, center);
//                    }
//                }
//            }
//        }
        return false;
    }

    protected override string GetAnchorLockTooltip() {
        return "Locking the Slider Joint 2D aligns the connected anchor to the angle of the main anchor.";
    }


    public override Bounds OnGetFrameBounds() {
        Bounds baseBounds = base.OnGetFrameBounds();

        foreach (SliderJoint2D joint2D in targets.Cast<SliderJoint2D>()) {
            Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
            Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
            Vector2 diff = connectedAnchorPosition - mainAnchorPosition;
            if (diff.magnitude <= Mathf.Epsilon) {
                diff = -Vector2.up;
            }
            Vector2 normalizedDiff = diff.normalized;

            baseBounds.Encapsulate(mainAnchorPosition + normalizedDiff*joint2D.limits.min);
            baseBounds.Encapsulate(mainAnchorPosition + normalizedDiff*joint2D.limits.max);
            baseBounds.Encapsulate(mainAnchorPosition - normalizedDiff*joint2D.limits.min);
            baseBounds.Encapsulate(mainAnchorPosition - normalizedDiff*joint2D.limits.max);
        }

        return baseBounds;
    }

    private void DrawSlider(SliderJoint2D sliderJoint2D, AnchorInfo anchorInfo) {
        float worldAngle = sliderJoint2D.transform.eulerAngles.z + sliderJoint2D.angle;

//        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);
//        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(sliderJoint2D);

        SliderJoint2DSettings joint2DSettings = SettingsHelper.GetOrCreate <SliderJoint2DSettings>(sliderJoint2D);

        int controlID = anchorInfo.GetControlID("sliderAngle");

        HandleDragDrop(controlID, sliderJoint2D, joint2DSettings);

        EditorGUI.BeginChangeCheck();


        JointHelpers.AnchorBias sliderBias;

        if (joint2DSettings.anchorPriority == SliderJoint2DSettings.AnchorPriority.Main) {
            sliderBias = JointHelpers.AnchorBias.Main;
        } else {
            sliderBias = JointHelpers.AnchorBias.Connected;
        }

        JointHelpers.AnchorBias oppositeBias = JointHelpers.GetOppositeBias(sliderBias);
        Vector2 angleWidgetPosition = JointHelpers.GetAnchorPosition(sliderJoint2D, sliderBias);

        Vector2 otherAnchorPosition = JointHelpers.GetAnchorPosition(sliderJoint2D, oppositeBias);

        Vector2 offsetToOther = otherAnchorPosition - angleWidgetPosition;

        float newAngle = LineAngleHandle(controlID, worldAngle, angleWidgetPosition, 0.5f, 2);

        Vector2 mousePosition = Event.current.mousePosition;

        EditorHelpers.ContextClick(controlID, () => {
            GenericMenu menu = new GenericMenu();
            AddEditSliderAngleMenuItem(sliderJoint2D, menu, mousePosition);
            menu.ShowAsContext();
        });

        if (EditorGUI.EndChangeCheck()) {

            bool snapped = false;

            if (EditorGUI.actionKey) {
                float handleSize = HandleUtility.GetHandleSize(angleWidgetPosition);

                Vector2 mousePosition2D = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);

                Ray currentAngleRay = new Ray(angleWidgetPosition, Helpers2D.GetDirection(newAngle));

                Vector2 mousePositionProjectedToAngle = Helpers2D.ClosestPointToRay(currentAngleRay, mousePosition2D);

                List<Vector2> directionsToSnapTo = new List<Vector2> {
                    (GetTargetPosition(sliderJoint2D, sliderBias) - angleWidgetPosition).normalized
                };

                if (!joint2DSettings.lockAnchors) {
                    directionsToSnapTo.Insert(0, offsetToOther.normalized);
                }

                if (sliderJoint2D.connectedBody) {
                    directionsToSnapTo.Add(
                        (GetTargetPosition(sliderJoint2D, oppositeBias) - angleWidgetPosition)
                            .normalized);
                }

                foreach (Vector2 direction in directionsToSnapTo) {
                    Ray rayTowardsDirection = new Ray(angleWidgetPosition, direction);

                    Vector2 closestPointTowardsDirection = Helpers2D.ClosestPointToRay(rayTowardsDirection,
                        mousePositionProjectedToAngle);

                    if (Vector2.Distance(closestPointTowardsDirection, mousePositionProjectedToAngle) <
                        handleSize*0.125f) {
                        Vector2 currentDirection = Helpers2D.GetDirection(newAngle);
                        Vector2 closestPositionToDirection =
                            Helpers2D.ClosestPointToRay(rayTowardsDirection,
                                angleWidgetPosition + currentDirection);

                        snapped = true;
                        newAngle = Helpers2D.GetAngle(closestPositionToDirection - angleWidgetPosition);

                        break;
                    }
                }

            }

            float wantedAngle = newAngle - sliderJoint2D.transform.eulerAngles.z;

            if (!snapped)
            {
                wantedAngle = Handles.SnapValue(wantedAngle, 45);
            }

            EditorHelpers.RecordUndo("Alter Slider Joint 2D Angle", sliderJoint2D);

            if (joint2DSettings.lockAnchors) {
                float angleDelta = Mathf.DeltaAngle(sliderJoint2D.angle, wantedAngle);


                JointHelpers.SetWorldAnchorPosition(sliderJoint2D,
                    angleWidgetPosition + (Vector2)(Helpers2D.Rotate(angleDelta) * offsetToOther), oppositeBias);

                sliderJoint2D.angle = wantedAngle;
            }

            sliderJoint2D.angle = wantedAngle;
        }

        if (sliderJoint2D.useLimits) {
            HandleLimits(sliderJoint2D, anchorInfo);
        }
    }

    private static void AddEditSliderAngleMenuItem(SliderJoint2D sliderJoint2D, GenericMenu menu, Vector2 mousePosition) {
        SliderJoint2DSettings joint2DSettings = SettingsHelper.GetOrCreate<SliderJoint2DSettings>(sliderJoint2D);
        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);

        menu.AddItem(new GUIContent("Edit Slider Angle"), false,
            () =>
                EditorHelpers.ShowDropDown(
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight*3),
                    delegate(Action close, bool focused) {
                        EditorGUI.BeginChangeCheck();
                        GUI.SetNextControlName("SliderAngle");
                        float sliderAngle =
                            EditorGUILayout.FloatField(
                                new GUIContent("Slider Angle",
                                    "The translation angle that the joint slides along. [ -1000000, 1000000 ]."),
                                sliderJoint2D.angle);
                        if (EditorGUI.EndChangeCheck()) {
                            using (new Modification("Slider Angle", sliderJoint2D)) {
                                if (joint2DSettings.lockAnchors) {
                                    float angleDelta = Mathf.DeltaAngle(sliderJoint2D.angle, sliderAngle);

                                    Vector2 connectedAnchorPosition =
                                        JointHelpers.GetConnectedAnchorPosition(sliderJoint2D);
                                    Vector2 connectedOffset = connectedAnchorPosition - mainAnchorPosition;

                                    JointHelpers.SetWorldConnectedAnchorPosition(sliderJoint2D,
                                        mainAnchorPosition +
                                        (Vector2) (Helpers2D.Rotate(angleDelta)*connectedOffset));
                                }

                                sliderJoint2D.angle = sliderAngle;
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

    private static void HandleLimits(SliderJoint2D sliderJoint2D, AnchorInfo anchorInfo) {
        float worldAngle = sliderJoint2D.transform.eulerAngles.z + sliderJoint2D.angle;

        SliderJoint2DSettings settings = SettingsHelper.GetOrCreate<SliderJoint2DSettings>(sliderJoint2D);

        JointHelpers.AnchorBias bias;
        switch (settings.anchorPriority) {
            case SliderJoint2DSettings.AnchorPriority.Main:
                bias = JointHelpers.AnchorBias.Main;
                break;
            case SliderJoint2DSettings.AnchorPriority.Connected:
                bias = JointHelpers.AnchorBias.Connected;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        LimitWidget(sliderJoint2D, anchorInfo, bias, worldAngle);
    }

    private static void LimitWidget(SliderJoint2D sliderJoint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias,
        float worldAngle) {
        Vector2 anchorPosition = JointHelpers.GetAnchorPosition(sliderJoint2D, bias);

        JointHelpers.AnchorBias oppositeBias = JointHelpers.GetOppositeBias(bias);

        Vector2 oppositeAnchorPosition = JointHelpers.GetAnchorPosition(sliderJoint2D, oppositeBias);
        Vector2 direction = Helpers2D.GetDirection(worldAngle);
        if (bias == JointHelpers.AnchorBias.Connected) {
            direction *= -1f;
        }
        Vector2 delta = oppositeAnchorPosition - anchorPosition;

        float angleDiff = Mathf.DeltaAngle(Helpers2D.GetAngle(delta), worldAngle);

        Vector2 rotatedDelta = Helpers2D.Rotate(angleDiff)*delta;

        Vector2 wantedOppositeAnchorPosition = anchorPosition + rotatedDelta;
        Vector2 wantedOppositeAnchorPosition2 = anchorPosition - rotatedDelta;

        int minLimitControlID = anchorInfo.GetControlID("minLimit");
        int maxLimitControlID = anchorInfo.GetControlID("maxLimit");

        LimitContext(sliderJoint2D, minLimitControlID, Limit.Min);
        LimitContext(sliderJoint2D, maxLimitControlID, Limit.Max);

        Color limitColor = sliderJoint2D.limits.min > sliderJoint2D.limits.max
            ? editorSettings.incorrectLimitsColor
            : editorSettings.correctLimitsColor;
        using (new HandleColor(limitColor)) {
            Handles.DrawLine(anchorPosition + direction*sliderJoint2D.limits.min,
                anchorPosition + direction*sliderJoint2D.limits.max);

            float fontSize = HandleUtility.GetHandleSize(anchorPosition)*(1f/64f);

            if (GUIUtility.hotControl == minLimitControlID) {
                String text = String.Format("{0:0.00}", sliderJoint2D.limits.min);
                float minSign = Mathf.Sign(sliderJoint2D.limits.min);
                float minLabelDistance = minSign*
                                         fontSize*
                                         EditorHelpers.FontWithBackgroundStyle.CalcSize(new GUIContent(text)).magnitude*
                                         (minSign < 0 ? (1f) : 0.75f);


                Handles.Label(anchorPosition + (direction)*(sliderJoint2D.limits.min + minLabelDistance),
                    text, EditorHelpers.FontWithBackgroundStyle);
            }
            if (GUIUtility.hotControl == maxLimitControlID) {
                String text = String.Format("{0:0.00}", sliderJoint2D.limits.max);
                float maxSign = Mathf.Sign(sliderJoint2D.limits.max);
                float maxLabelDistance = maxSign*
                                         fontSize*
                                         EditorHelpers.FontWithBackgroundStyle.CalcSize(new GUIContent(text)).magnitude*
                                         (maxSign < 0 ? (1f) : 0.75f);
                Handles.Label(anchorPosition + (direction)*(sliderJoint2D.limits.max + maxLabelDistance),
                    text, EditorHelpers.FontWithBackgroundStyle);
            }
            if (GUIUtility.hotControl == minLimitControlID ||
                GUIUtility.hotControl == maxLimitControlID) {
                using (new HandleColor(new Color(1, 1, 1, 0.25f))) {
                    float handleSize = HandleUtility.GetHandleSize(wantedOppositeAnchorPosition)*0.0625f;

                    Handles.DrawLine(wantedOppositeAnchorPosition - direction*handleSize,
                        wantedOppositeAnchorPosition + direction*handleSize);
                    handleSize = HandleUtility.GetHandleSize(wantedOppositeAnchorPosition2)*0.0625f;
                    Handles.DrawLine(wantedOppositeAnchorPosition2 - direction*handleSize,
                        wantedOppositeAnchorPosition2 + direction*handleSize);
                    Handles.DrawWireArc(anchorPosition, Vector3.forward, wantedOppositeAnchorPosition, 360,
                        Vector2.Distance(wantedOppositeAnchorPosition, anchorPosition));
                }
            }


            bool actionKey = EditorGUI.actionKey;

            List<Vector2> snapList = null;
            if (actionKey) {
                snapList = new List<Vector2> {
                    anchorPosition,
                    wantedOppositeAnchorPosition,
                    wantedOppositeAnchorPosition2
                };
            }

            using (new HandleColor(editorSettings.minLimitColor)) {
                DrawLimitSlider(sliderJoint2D, minLimitControlID, anchorPosition, direction, snapList, Limit.Min);
            }
            using (new HandleColor(editorSettings.maxLimitColor)) {
                DrawLimitSlider(sliderJoint2D, maxLimitControlID, anchorPosition, direction, snapList, Limit.Max);
            }
        }
    }

    private static void DrawLimitSlider(SliderJoint2D sliderJoint2D, int limitControlID, Vector2 anchorPosition,
        Vector2 direction, IEnumerable<Vector2> snapList, Limit limit) {
        EditorGUI.BeginChangeCheck();

        float val;
        JointTranslationLimits2D limits2D = sliderJoint2D.limits;
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

        float newLimit = EditorHelpers.LineSlider(limitControlID, anchorPosition,
            val,
            Helpers2D.GetAngle(direction), 0.125f, false, limit == Limit.Min);

        if (EditorGUI.EndChangeCheck()) {
            if (snapList != null) {
                List<Vector2> limitSnapList = new List<Vector2>(snapList);
                switch (limit) {
                    case Limit.Min:
                        limitSnapList.Add(anchorPosition + direction*limits2D.max);
                        break;
                    case Limit.Max:
                        limitSnapList.Add(anchorPosition + direction*limits2D.min);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("limit");
                }

                Vector2 limitGUIPosition =
                    HandleUtility.WorldToGUIPoint(anchorPosition + direction*newLimit);

                foreach (Vector2 snapPosition in limitSnapList) {
                    Vector2 snapGUIPosition = HandleUtility.WorldToGUIPoint(snapPosition);
                    if (Vector2.Distance(limitGUIPosition, snapGUIPosition) < 10) {
                        newLimit = Helpers2D.DistanceAlongLine(new Ray(anchorPosition, direction),
                            snapPosition);
                    }
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
    }

    private static void LimitContext(SliderJoint2D sliderJoint2D, int controlID, Limit limit) {
        Vector2 mousePosition = Event.current.mousePosition;

        string limitName = (limit == Limit.Min ? "Lower" : "Upper") + " Translation Limit";

        EditorHelpers.ContextClick(controlID, () => {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Edit " + limitName), false, () =>
                EditorHelpers.ShowDropDown(
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight*3),
                    delegate(Action close, bool focused) {
                        EditorGUI.BeginChangeCheck();
                        GUI.SetNextControlName(limitName);
                        float newLimit = EditorGUILayout.FloatField(limitName,
                            limit == Limit.Min
                                ? sliderJoint2D.limits.min
                                : sliderJoint2D.limits.max);
                        if (EditorGUI.EndChangeCheck()) {
                            JointTranslationLimits2D limits = sliderJoint2D.limits;
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

    protected override void InspectorDisplayGUI(bool enabled) {
        List<Object> allSettings =
            targets.Cast<SliderJoint2D>()
                .Select(sliderJoint2D => SettingsHelper.GetOrCreate<SliderJoint2DSettings>(sliderJoint2D))
                .Where(sliderSettings => sliderSettings != null).Cast<Object>().ToList();

        SerializedObject serializedSettings = new SerializedObject(allSettings.ToArray());
        SelectAngleLimitsMode(serializedSettings, enabled);
    }

    private static readonly GUIContent AngleLimitsModeContent =
        new GUIContent("Anchor Priority",
            "Which anchor's angle limits would you like to see? If there is no connected body this setting will be ignored.");

    private void SelectAngleLimitsMode(SerializedObject serializedSettings, bool enabled) {
        EditorGUI.BeginChangeCheck();
        SliderJoint2DSettings.AnchorPriority value;

        using (new GUIEnabled(enabled)) {
            SerializedProperty anchorPriority = serializedSettings.FindProperty("anchorPriority");
            EditorGUILayout.PropertyField(anchorPriority, AngleLimitsModeContent);
            value = (SliderJoint2DSettings.AnchorPriority)
                Enum.Parse(typeof (SliderJoint2DSettings.AnchorPriority),
                    anchorPriority.enumNames[anchorPriority.enumValueIndex]);
        }

        if (EditorGUI.EndChangeCheck()) {
            foreach (Object t in targets) {
                SliderJoint2D sliderJoint2D = (SliderJoint2D) t;
                SliderJoint2DSettings settings = SettingsHelper.GetOrCreate<SliderJoint2DSettings>(sliderJoint2D);

                EditorHelpers.RecordUndo("toggle angle limits display mode", settings);
                settings.anchorPriority = value;
                EditorUtility.SetDirty(settings);
            }
        }
    }


    protected override void OwnershipMoved(AnchoredJoint2D cloneJoint) {
        //swap limits
        SliderJoint2D sliderJoint2D = cloneJoint as SliderJoint2D;
        if (!sliderJoint2D) {
            return;
        }


        SliderJoint2DSettings settings = SettingsHelper.GetOrCreate<SliderJoint2DSettings>(sliderJoint2D);

        if (settings.anchorPriority == SliderJoint2DSettings.AnchorPriority.Main) {
            settings.anchorPriority = SliderJoint2DSettings.AnchorPriority.Connected;
        }
        else if (settings.anchorPriority == SliderJoint2DSettings.AnchorPriority.Connected) {
            settings.anchorPriority = SliderJoint2DSettings.AnchorPriority.Main;
        }

        float worldAngle = sliderJoint2D.connectedBody.transform.eulerAngles.z + sliderJoint2D.angle;

        sliderJoint2D.angle = (180.0f + worldAngle) - sliderJoint2D.transform.eulerAngles.z;
    }

    protected override bool PostAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo info, List<Vector2> otherAnchors,
        JointHelpers.AnchorBias bias)
    {
        SliderJoint2D sliderJoint2D = joint2D as SliderJoint2D;
        if (sliderJoint2D == null)
        {
            return false;
        }

        if (EditorHelpers.IsWarm(info.GetControlID("sliderAngle")) && DragAndDrop.objectReferences.Length == 0)
        {
            float suspensionAngle = sliderJoint2D.angle;

            GUIContent labelContent = new GUIContent(String.Format("{0:0.00}", suspensionAngle));
            Vector3 mainAnchorPosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);

            float fontSize = HandleUtility.GetHandleSize(mainAnchorPosition) * (1f / 64f);

            float labelOffset = fontSize * EditorHelpers.FontWithBackgroundStyle.CalcSize(labelContent).y;

            Handles.Label(mainAnchorPosition + (Camera.current.transform.up * labelOffset), labelContent,
                EditorHelpers.FontWithBackgroundStyle);
        }

        return false;
    }
}