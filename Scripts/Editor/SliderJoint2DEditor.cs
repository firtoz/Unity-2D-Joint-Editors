using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;

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

        if (!SettingsHelper.GetOrCreate(joint).lockAnchors &&
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

        float wantedAngle = Helpers2D.GetAngle(direction);

        EditorHelpers.RecordUndo("Realign angle", sliderJoint2D);
        sliderJoint2D.angle = wantedAngle - sliderJoint2D.transform.eulerAngles.z;
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


    protected override void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint)
    {
        SliderJoint2D sliderJoint2D = joint as SliderJoint2D;
        if (sliderJoint2D != null)
        {
            menu.AddItem(new GUIContent("Use Limits"), sliderJoint2D.useLimits, () =>
            {
                EditorHelpers.RecordUndo("Use Limits", sliderJoint2D);
                sliderJoint2D.useLimits = !sliderJoint2D.useLimits;
                EditorUtility.SetDirty(sliderJoint2D);
            });
        }
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        SliderJoint2D sliderJoint2D = (SliderJoint2D) joint2D;

        Vector2 center = JointHelpers.GetAnchorPosition(sliderJoint2D, bias);

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);
        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(sliderJoint2D);

        if (bias != JointHelpers.AnchorBias.Connected && (GUIUtility.hotControl == anchorInfo.GetControlID("sliderAngle") || !Event.current.shift)) {
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
            if (sliderJoint2D.connectedBody && GUIUtility.hotControl == anchorInfo.GetControlID("sliderAngle"))
            {
                Handles.DrawLine(mainAnchorPosition, GetTargetPosition(sliderJoint2D, JointHelpers.AnchorBias.Connected));
            }
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
                }
            }
        }
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

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);


        EditorGUI.BeginChangeCheck();
        int controlID = anchorInfo.GetControlID("sliderAngle");

        float newAngle = LineAngleHandle(controlID, worldAngle, mainAnchorPosition, 0.5f, 2);

        if (EditorGUI.EndChangeCheck()) {
            Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(sliderJoint2D);
            Vector2 connectedOffset = connectedAnchorPosition - mainAnchorPosition;

            Joint2DSettings joint2DSettings = SettingsHelper.GetOrCreate(sliderJoint2D);

            if (EditorGUI.actionKey)
            {
                float handleSize = HandleUtility.GetHandleSize(mainAnchorPosition);

                Vector2 mousePosition2D = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);

                Ray currentAngleRay = new Ray(mainAnchorPosition, Helpers2D.GetDirection(newAngle));

                Vector2 mousePositionProjectedToAngle = Helpers2D.ClosestPointToRay(currentAngleRay, mousePosition2D);

                List<Vector2> directionsToSnapTo = new List<Vector2> {
                        (GetTargetPosition(sliderJoint2D, JointHelpers.AnchorBias.Main) - mainAnchorPosition)
                            .normalized
                    };

                if (!joint2DSettings.lockAnchors) {
                    directionsToSnapTo.Insert(0, connectedOffset.normalized);
                }

                if (sliderJoint2D.connectedBody)
                {
                    directionsToSnapTo.Add(
                        (GetTargetPosition(sliderJoint2D, JointHelpers.AnchorBias.Connected) - mainAnchorPosition)
                            .normalized);
                }

                foreach (Vector2 direction in directionsToSnapTo)
                {
                    Ray rayTowardsConnectedAnchor = new Ray(mainAnchorPosition, direction);

                    Vector2 closestPointTowardsDirection = Helpers2D.ClosestPointToRay(rayTowardsConnectedAnchor,
                        mousePositionProjectedToAngle);

                    if (Vector2.Distance(closestPointTowardsDirection, mousePositionProjectedToAngle) <
                        handleSize * 0.125f)
                    {
                        Vector2 currentDirection = Helpers2D.GetDirection(newAngle);
                        Vector2 closestPositionToDirection =
                            Helpers2D.ClosestPointToRay(rayTowardsConnectedAnchor,
                                mainAnchorPosition + currentDirection);

                        newAngle = Helpers2D.GetAngle(closestPositionToDirection - mainAnchorPosition);

                        break;
                    }
                }
            }

            if (joint2DSettings.lockAnchors) {
                float wantedAngle = newAngle - sliderJoint2D.transform.eulerAngles.z;
                float angleDelta = Mathf.DeltaAngle(sliderJoint2D.angle, wantedAngle);

                EditorHelpers.RecordUndo("Alter Slider Joint 2D Angle", sliderJoint2D);
                sliderJoint2D.angle = wantedAngle;

                JointHelpers.SetWorldConnectedAnchorPosition(sliderJoint2D,
                    mainAnchorPosition + (Vector2) (Helpers2D.Rotate(angleDelta)*connectedOffset));
            }
            else {
                EditorHelpers.RecordUndo("Alter Slider Joint 2D Angle", sliderJoint2D);
                sliderJoint2D.angle = newAngle - sliderJoint2D.transform.eulerAngles.z;
            }
        }

        if (sliderJoint2D.useLimits) {
            HandleLimits(sliderJoint2D, anchorInfo);
        }
    }


    protected override bool WantsOffset() {
        return true;
    }

    private static void HandleLimits(SliderJoint2D sliderJoint2D, AnchorInfo anchorInfo) {
        float worldAngle = sliderJoint2D.transform.eulerAngles.z + sliderJoint2D.angle;

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(sliderJoint2D);

        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(sliderJoint2D);
        Vector2 direction = Helpers2D.GetDirection(worldAngle);
        Vector2 delta = connectedAnchorPosition - mainAnchorPosition;

        float angleDiff = Mathf.DeltaAngle(Helpers2D.GetAngle(delta), worldAngle);

        Vector2 rotatedDelta = Helpers2D.Rotate(angleDiff)*delta;

        Vector2 wantedConnectedAnchorPosition = mainAnchorPosition + rotatedDelta;
        Vector2 wantedConnectedAnchorPosition2 = mainAnchorPosition - rotatedDelta;

        using (new HandleColor(Color.green)) {
            using (new HandleColor(sliderJoint2D.limits.min > sliderJoint2D.limits.max ? Color.red : Color.green)) {
                Handles.DrawLine(mainAnchorPosition + direction*sliderJoint2D.limits.min,
                    mainAnchorPosition + direction*sliderJoint2D.limits.max);
            }
            if (GUIUtility.hotControl == anchorInfo.GetControlID("minLimit") ||
                GUIUtility.hotControl == anchorInfo.GetControlID("maxLimit")) {
                using (new HandleColor(new Color(1, 1, 1, 0.25f))) {
                    float handleSize = HandleUtility.GetHandleSize(wantedConnectedAnchorPosition)*0.0625f;

                    Handles.DrawLine(wantedConnectedAnchorPosition - direction*handleSize,
                        wantedConnectedAnchorPosition + direction*handleSize);
                    handleSize = HandleUtility.GetHandleSize(wantedConnectedAnchorPosition2)*0.0625f;
                    Handles.DrawLine(wantedConnectedAnchorPosition2 - direction*handleSize,
                        wantedConnectedAnchorPosition2 + direction*handleSize);
                    Handles.DrawWireArc(mainAnchorPosition, Vector3.forward, wantedConnectedAnchorPosition, 360,
                        Vector2.Distance(wantedConnectedAnchorPosition, mainAnchorPosition));
                }
            }

            EditorGUI.BeginChangeCheck();
            float newMinLimit = EditorHelpers.LineSlider(anchorInfo.GetControlID("minLimit"), mainAnchorPosition,
                sliderJoint2D.limits.min,
                Helpers2D.GetAngle(direction), 0.125f);

            bool actionKey = EditorGUI.actionKey;

            List<Vector2> snapList = null;
            if (actionKey) {
                snapList = new List<Vector2> {
                    mainAnchorPosition,
                    wantedConnectedAnchorPosition,
                    wantedConnectedAnchorPosition2
                };
            }

            JointTranslationLimits2D limits;
            if (EditorGUI.EndChangeCheck()) {
                if (actionKey) {
                    List<Vector2> minSnapList = new List<Vector2>(snapList) {
                        mainAnchorPosition + direction*sliderJoint2D.limits.max
                    };
                    Vector2 minLimitScreenPosition =
                        HandleUtility.WorldToGUIPoint(mainAnchorPosition + direction*newMinLimit);

                    foreach (Vector2 snapPosition in minSnapList) {
                        Vector2 screenSnapPosition = HandleUtility.WorldToGUIPoint(snapPosition);
                        if (Vector2.Distance(minLimitScreenPosition, screenSnapPosition) < 10) {
                            newMinLimit = Helpers2D.DistanceAlongLine(new Ray(mainAnchorPosition, direction),
                                snapPosition);
                        }
                    }
                }


                EditorHelpers.RecordUndo("Change slider limit", sliderJoint2D);
                limits = sliderJoint2D.limits;
                limits.min = newMinLimit;
                sliderJoint2D.limits = limits;
            }
            EditorGUI.BeginChangeCheck();
            float newMaxLimit = EditorHelpers.LineSlider(anchorInfo.GetControlID("maxLimit"), mainAnchorPosition,
                sliderJoint2D.limits.max,
                Helpers2D.GetAngle(direction), 0.125f);
            if (EditorGUI.EndChangeCheck()) {
                if (actionKey) {
                    List<Vector2> maxSnapList = new List<Vector2>(snapList) {
                        mainAnchorPosition + direction*sliderJoint2D.limits.min
                    };

                    Vector2 minLimitScreenPosition =
                        HandleUtility.WorldToGUIPoint(mainAnchorPosition + direction*newMaxLimit);

                    foreach (Vector2 snapPosition in from snapPosition in maxSnapList
                        let screenSnapPosition = HandleUtility.WorldToGUIPoint(snapPosition)
                        where Vector2.Distance(minLimitScreenPosition, screenSnapPosition) < 10
                        select snapPosition) {
                        newMaxLimit = Helpers2D.DistanceAlongLine(new Ray(mainAnchorPosition, direction),
                            snapPosition);
                    }
                }
                EditorHelpers.RecordUndo("Change slider limit", sliderJoint2D);
                limits = sliderJoint2D.limits;
                limits.max = newMaxLimit;
                sliderJoint2D.limits = limits;
            }
        }
    }
}