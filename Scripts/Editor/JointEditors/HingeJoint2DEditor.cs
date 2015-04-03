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

[CustomEditor(typeof (HingeJoint2D))]
[CanEditMultipleObjects]
public class HingeJoint2DEditor : Joint2DEditorBase {
    protected override bool WantsLocking() {
        return true;
    }

    private static readonly HashSet<string> Names = new HashSet<String> {
        "lowerMainAngle",
        "upperMainAngle",
        "lowerConnectedAngle",
        "upperConnectedAngle"
    };

    protected override HashSet<String> GetControlNames() {
        return Names;
    }

    protected override bool PostAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo info, List<Vector2> otherAnchors,
                                          JointHelpers.AnchorBias bias) {
        var hingeJoint2D = joint2D as HingeJoint2D;
        if (hingeJoint2D == null) {
            return false;
        }

        if (!hingeJoint2D.useLimits) {
            return false;
        }

        var showAngle = false;
        float angle = 0;
        float displayAngle = 0;

        float jointAngle;
        if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) {
            jointAngle = hingeJoint2D.jointAngle;
        } else {
            jointAngle = 0;
        }

        var startPosition = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

        var mainBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);

        var mainBodyAngle = JointHelpers.AngleFromAnchor(startPosition, mainBodyPosition,
            JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Main));

        var connectedBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);

        float connectedBodyAngle;
        if (hingeJoint2D.connectedBody) {
            connectedBodyAngle = JointHelpers.AngleFromAnchor(startPosition, connectedBodyPosition,
                JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Connected));
        } else {
            connectedBodyAngle = 0;
        }

        var angleDiff = jointAngle - (connectedBodyAngle - mainBodyAngle);

        var liveMainAngle = connectedBodyAngle + angleDiff;

        var minMainAngle = liveMainAngle - hingeJoint2D.limits.min;
        var maxMainAngle = liveMainAngle - hingeJoint2D.limits.max;

        var labelText = "";

        float angleOffset = 0;

        var settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

        if (EditorHelpers.IsWarm(info.GetControlID("lowerMainAngle"))) {
            showAngle = true;
            angle = minMainAngle;
            displayAngle = hingeJoint2D.limits.min;
            labelText = "Lower: ";
            angleOffset = settings.mainAngleOffset;
        } else if (EditorHelpers.IsWarm(info.GetControlID("upperMainAngle"))) {
            showAngle = true;
            angle = maxMainAngle;
            displayAngle = hingeJoint2D.limits.max;
            labelText = "Upper: ";
            angleOffset = settings.mainAngleOffset;
        } else if (EditorHelpers.IsWarm(info.GetControlID("lowerConnectedAngle"))) {
            showAngle = true;
            angle = hingeJoint2D.limits.min;
            displayAngle = angle;
            labelText = "Lower: ";
            startPosition = JointHelpers.GetConnectedAnchorPosition(hingeJoint2D);
            angleOffset = settings.connectedAngleOffset;
        } else if (EditorHelpers.IsWarm(info.GetControlID("upperConnectedAngle"))) {
            showAngle = true;
            angle = hingeJoint2D.limits.max;
            labelText = "Upper: ";
            displayAngle = angle;
            startPosition = JointHelpers.GetConnectedAnchorPosition(hingeJoint2D);
            angleOffset = settings.connectedAngleOffset;
        }

        LimitContext(hingeJoint2D, info.GetControlID("lowerMainAngle"), Limit.Min);
        LimitContext(hingeJoint2D, info.GetControlID("upperMainAngle"), Limit.Max);
        LimitContext(hingeJoint2D, info.GetControlID("lowerConnectedAngle"), Limit.Min);
        LimitContext(hingeJoint2D, info.GetControlID("upperConnectedAngle"), Limit.Max);

        if (showAngle) {
            var distanceFromCenter = GetAngleSliderRadius(startPosition);


            var anglePosition = startPosition + Helpers2D.GetDirection(angle + angleOffset) * distanceFromCenter;

            var labelContent = new GUIContent(labelText + "\n" + String.Format("{0:0.00}", displayAngle));

            var fontSize = HandleUtility.GetHandleSize(anglePosition) * (1f / 64f);

            var labelOffset = fontSize * EditorHelpers.FontWithBackgroundStyle.CalcSize(labelContent).y;

            EditorHelpers.OverlayLabel((Vector3) anglePosition + (Camera.current.transform.up * labelOffset),
                labelContent,
                EditorHelpers.FontWithBackgroundStyle);
        }
        return false;
    }

    private static float GetAngleSliderRadius(Vector2 startPosition) {
        return HandleUtility.GetHandleSize(startPosition) * EditorHelpers.HandleSizeToPixels *
               editorSettings.angleLimitRadius;
    }

    private void LimitContext(HingeJoint2D hingeJoint2D, int controlID, Limit limit) {
        var mousePosition = Event.current.mousePosition;

        var limitName = (limit == Limit.Min ? "Lower" : "Upper") + " Angle Limit";

        EditorHelpers.ContextClick(controlID, () => {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Edit " + limitName), false, () =>
                ShowUtility(
                    "Edit " + limitName,
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 2),
                    delegate(Action close, bool focused) {
                        EditorGUI.BeginChangeCheck();
                        GUI.SetNextControlName(limitName);
                        var newLimit = EditorGUILayout.FloatField(limitName,
                            limit == Limit.Min
                                ? hingeJoint2D.limits.min
                                : hingeJoint2D.limits.max);
                        if (EditorGUI.EndChangeCheck()) {
                            var limits = hingeJoint2D.limits;
                            if (limit == Limit.Min) {
                                limits.min = newLimit;
                            } else {
                                limits.max = newLimit;
                            }
                            EditorHelpers.RecordUndo(limitName, hingeJoint2D);
                            hingeJoint2D.limits = limits;
                            EditorUtility.SetDirty(hingeJoint2D);
                        }
                        if (GUILayout.Button("Done") ||
                            (Event.current.isKey &&
                             (Event.current.keyCode == KeyCode.Escape) &&
                             focused)) {
                            close();
                        }
                    }));
            menu.ShowAsContext();
        });
    }

    protected override bool PreSliderGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        var hingeJoint2D = joint2D as HingeJoint2D;
        if (hingeJoint2D == null) {
            return false;
        }

        return DrawAngleLimits(hingeJoint2D, anchorInfo, bias);
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        var hingeJoint2D = joint2D as HingeJoint2D;

        if (hingeJoint2D == null) {
            return false;
        }

        if (bias == JointHelpers.AnchorBias.Either) {
            DrawLinesAndDiscs(hingeJoint2D, anchorInfo, JointHelpers.AnchorBias.Main);
            DrawLinesAndDiscs(hingeJoint2D, anchorInfo, JointHelpers.AnchorBias.Connected);
        } else {
            DrawLinesAndDiscs(hingeJoint2D, anchorInfo, bias);
        }

        var mainAnchorPosition = JointHelpers.GetMainAnchorPosition(hingeJoint2D);
        var connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(hingeJoint2D);
        if (bias == JointHelpers.AnchorBias.Main &&
            Vector2.Distance(mainAnchorPosition, connectedAnchorPosition) > AnchorEpsilon) {
            using (new HandleColor(GetAdjustedColor(Color.green))) {
                Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
            }
        }
        return false;
    }


    private bool DrawAngleLimits(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        var changed = false;
        var settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);
        if (!settings.showAngleLimits) {
            return false;
        }

        if (hingeJoint2D.useLimits) {
            var limits = hingeJoint2D.limits;
            var minLimit = limits.min;
            var maxLimit = limits.max;


            var anchorPriority = settings.anchorPriority;

            var showMain = anchorPriority == HingeJoint2DSettings.AnchorPriority.Main ||
                           anchorPriority == HingeJoint2DSettings.AnchorPriority.Both;

            var showConnected = (anchorPriority == HingeJoint2DSettings.AnchorPriority.Connected ||
                                 anchorPriority == HingeJoint2DSettings.AnchorPriority.Both);

            var anchorPosition = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

            var distanceFromCenter = GetAngleSliderRadius(anchorPosition);
            var angleHandleSize = editorSettings.angleHandleSize;

            float jointAngle;
            var isPlaying = EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying;
            if (isPlaying) {
                jointAngle = hingeJoint2D.jointAngle;
            } else {
                jointAngle = 0;
            }

            var mainBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);

            var mainBodyAngle = JointHelpers.AngleFromAnchor(anchorPosition, mainBodyPosition,
                JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Main));

            var connectedBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);

            float connectedBodyAngle;
            if (hingeJoint2D.connectedBody) {
                connectedBodyAngle = JointHelpers.AngleFromAnchor(anchorPosition, connectedBodyPosition,
                    JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Connected));
            } else {
                connectedBodyAngle = 0;
            }

            var angleDiff = jointAngle - (connectedBodyAngle - mainBodyAngle);

            var liveMainAngle = connectedBodyAngle + angleDiff;

            var minMainAngle = liveMainAngle - minLimit;
            var maxMainAngle = liveMainAngle - maxLimit;

            var limitDifference = maxLimit - minLimit;

            Color limitColor, limitAreaColor;
            if (!isPlaying
                &&
                ((minLimit < jointAngle && maxLimit < jointAngle) || (minLimit > jointAngle && maxLimit > jointAngle))) {
                limitColor = editorSettings.incorrectLimitsColor;
                limitAreaColor = editorSettings.incorrectLimitsArea;
            } else {
                limitColor = editorSettings.correctLimitsColor;
                limitAreaColor = editorSettings.limitsAreaColor;
            }

            var angleWidgetColor = editorSettings.angleWidgetColor;
            var activeAngleColor = editorSettings.activeAngleColor;
            var hoverAngleColor = editorSettings.hoverAngleColor;

            if (isCreatedByTarget) {
                angleWidgetColor.a *= editorSettings.connectedJointTransparency;
                activeAngleColor.a *= editorSettings.connectedJointTransparency;
                hoverAngleColor.a *= editorSettings.connectedJointTransparency;

                limitColor.a *= editorSettings.connectedJointTransparency;
                limitAreaColor.a *= editorSettings.connectedJointTransparency;
            }

            if (showMain && bias != JointHelpers.AnchorBias.Connected) { //main or 'both'
                changed = HandleMainLimits(hingeJoint2D, anchorInfo, limitAreaColor, limitDifference, anchorPosition, distanceFromCenter, maxMainAngle, settings, limitColor, minMainAngle, angleWidgetColor, activeAngleColor, hoverAngleColor, angleHandleSize, limits, liveMainAngle);
            }
            if (showConnected && bias != JointHelpers.AnchorBias.Main) { //connected or both?
                changed = HandleConnectedLimits(hingeJoint2D, anchorInfo, mainBodyAngle, angleDiff, minLimit, maxLimit, limitAreaColor, limitDifference, anchorPosition, distanceFromCenter, settings, limitColor, angleWidgetColor, activeAngleColor, hoverAngleColor, angleHandleSize, limits, changed);
            }
        }

        return changed;
    }

    private static bool HandleConnectedLimits(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, float mainBodyAngle,
                                              float angleDiff, float minLimit, float maxLimit, Color limitAreaColor,
                                              float limitDifference, Vector2 anchorPosition, float distanceFromCenter,
                                              HingeJoint2DSettings settings, Color limitColor, Color angleWidgetColor,
                                              Color activeAngleColor, Color hoverAngleColor, float angleHandleSize,
                                              JointAngleLimits2D limits, bool changed) {
        var liveConnectedAngle = mainBodyAngle - angleDiff;

        var minConnectedAngle = liveConnectedAngle + minLimit;
        var maxConnectedAngle = liveConnectedAngle + maxLimit;

        using (new HandleColor(limitAreaColor)) {
            {
                if (limitDifference > 360) {
                    Handles.DrawSolidDisc(anchorPosition, Vector3.forward, distanceFromCenter);
                } else {
                    Handles.DrawSolidArc(anchorPosition, Vector3.forward,
                        Helpers2D.GetDirection(minConnectedAngle + settings.connectedAngleOffset),
                        limitDifference, distanceFromCenter);
                }
            }
        }
        using (new HandleColor(limitColor)) {
            {
                Vector3 minConnectedEnd = anchorPosition +
                                          Helpers2D.GetDirection(minConnectedAngle +
                                                                 settings.connectedAngleOffset) *
                                          distanceFromCenter;
                Handles.DrawLine(anchorPosition, minConnectedEnd);

                Vector3 maxConnectedEnd = anchorPosition +
                                          Helpers2D.GetDirection(maxConnectedAngle +
                                                                 settings.connectedAngleOffset) *
                                          distanceFromCenter;
                Handles.DrawLine(anchorPosition, maxConnectedEnd);

                if (limitDifference > 360) {
                    Handles.DrawWireDisc(anchorPosition, Vector3.forward, distanceFromCenter);
                } else {
                    Handles.DrawWireArc(anchorPosition, Vector3.forward,
                        Helpers2D.GetDirection(minConnectedAngle + settings.connectedAngleOffset),
                        limitDifference, distanceFromCenter);
                }

                EditorGUI.BeginChangeCheck();
                using (
                    HandleDrawerBase drawer = new HandleCircleDrawer(angleWidgetColor,
                        activeAngleColor, hoverAngleColor)) {
                    minConnectedAngle = EditorHelpers.AngleSlider(
                        anchorInfo.GetControlID("lowerConnectedAngle"), drawer,
                        anchorPosition,
                        minConnectedAngle + settings.connectedAngleOffset,
                        distanceFromCenter, angleHandleSize * HandleUtility.GetHandleSize(minConnectedEnd) / 64) -
                                        settings.connectedAngleOffset;
                }

                if (EditorGUI.EndChangeCheck()) {
                    EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                    limits.min = Handles.SnapValue(minConnectedAngle - liveConnectedAngle,
                        editorSettings.snapAngle);
                    hingeJoint2D.limits = limits;
                    changed = true;
                }

                EditorGUI.BeginChangeCheck();
                using (
                    HandleDrawerBase drawer = new HandleCircleDrawer(angleWidgetColor,
                        activeAngleColor, hoverAngleColor)) {
                    maxConnectedAngle = EditorHelpers.AngleSlider(
                        anchorInfo.GetControlID("upperConnectedAngle"), drawer,
                        anchorPosition,
                        maxConnectedAngle + settings.connectedAngleOffset,
                        distanceFromCenter, angleHandleSize * HandleUtility.GetHandleSize(maxConnectedEnd) / 64) -
                                        settings.connectedAngleOffset;
                }

                if (EditorGUI.EndChangeCheck()) {
                    EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                    limits.max = Handles.SnapValue(maxConnectedAngle - liveConnectedAngle,
                        editorSettings.snapAngle);
                    hingeJoint2D.limits = limits;
                    changed = true;
                }
            }
        }
        return changed;
    }

    private static bool HandleMainLimits(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, Color limitAreaColor,
                                         float limitDifference, Vector2 anchorPosition, float distanceFromCenter,
                                         float maxMainAngle, HingeJoint2DSettings settings, Color limitColor,
                                         float minMainAngle, Color angleWidgetColor, Color activeAngleColor,
                                         Color hoverAngleColor, float angleHandleSize, JointAngleLimits2D limits,
                                         float liveMainAngle) {
        var changed = false;
        using (new HandleColor(limitAreaColor)) {
            if (limitDifference > 360) {
                Handles.DrawSolidDisc(anchorPosition, Vector3.forward, distanceFromCenter);
            } else {
                Handles.DrawSolidArc(anchorPosition, Vector3.forward,
                    Helpers2D.GetDirection(maxMainAngle + settings.mainAngleOffset),
                    limitDifference, distanceFromCenter);
            }
        }
        using (new HandleColor(limitColor)) {
            Vector3 minMainEnd = anchorPosition +
                                 Helpers2D.GetDirection(minMainAngle + settings.mainAngleOffset) *
                                 distanceFromCenter;
            Handles.DrawLine(anchorPosition, minMainEnd);

            Vector3 maxMainEnd = anchorPosition +
                                 Helpers2D.GetDirection(maxMainAngle + settings.mainAngleOffset) *
                                 distanceFromCenter;
            Handles.DrawLine(anchorPosition, maxMainEnd);

            if (limitDifference > 360) {
                Handles.DrawWireDisc(anchorPosition, Vector3.forward, distanceFromCenter);
            } else {
                Handles.DrawWireArc(anchorPosition, Vector3.forward,
                    Helpers2D.GetDirection(maxMainAngle + settings.mainAngleOffset),
                    limitDifference, distanceFromCenter);
            }


            EditorGUI.BeginChangeCheck();
            using (
                HandleDrawerBase drawer = new HandleCircleDrawer(angleWidgetColor, activeAngleColor,
                    hoverAngleColor)) {
                minMainAngle = EditorHelpers.AngleSlider(anchorInfo.GetControlID("lowerMainAngle"), drawer,
                    anchorPosition,
                    minMainAngle + settings.mainAngleOffset,
                    distanceFromCenter, angleHandleSize * HandleUtility.GetHandleSize(minMainEnd) / 64) -
                               settings.mainAngleOffset;
            }

            if (EditorGUI.EndChangeCheck()) {
                EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                limits.min = Handles.SnapValue(liveMainAngle - minMainAngle, editorSettings.snapAngle);
                hingeJoint2D.limits = limits;
                changed = true;
            }

            EditorGUI.BeginChangeCheck();
            using (
                HandleDrawerBase drawer = new HandleCircleDrawer(angleWidgetColor,
                    activeAngleColor, hoverAngleColor)) {
                maxMainAngle = EditorHelpers.AngleSlider(anchorInfo.GetControlID("upperMainAngle"), drawer,
                    anchorPosition,
                    maxMainAngle + settings.mainAngleOffset,
                    distanceFromCenter, angleHandleSize * HandleUtility.GetHandleSize(maxMainEnd) / 64) -
                               settings.mainAngleOffset;
            }

            if (EditorGUI.EndChangeCheck()) {
                EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                limits.max = Handles.SnapValue(liveMainAngle - maxMainAngle, editorSettings.snapAngle);
                hingeJoint2D.limits = limits;
                changed = true;
            }
        }
        return changed;
    }


    private void DrawLinesAndDiscs(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        var center = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

        var scale = editorSettings.anchorScale;
        var handleSize = HandleUtility.GetHandleSize(center) * scale;

        var mainBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);
        var connectedBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);

        var settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

        if (bias == JointHelpers.AnchorBias.Main) {
            float angleToMain;

            if (Vector2.Distance(mainBodyPosition, center) > AnchorEpsilon) {
                angleToMain = Helpers2D.GetAngle(mainBodyPosition - center);
            } else {
                angleToMain = JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Main);
            }

            using (new HandleColor(GetAdjustedColor(editorSettings.anchorsToMainBodyColor)))
            {
                Handles.DrawLine(center,
                    center + Helpers2D.GetDirection(angleToMain + settings.mainAngleOffset) * handleSize);
            }
        } else if (bias == JointHelpers.AnchorBias.Connected) {
            if (hingeJoint2D.connectedBody) {
                float angleToConnected;

                if (Vector2.Distance(connectedBodyPosition, center) > AnchorEpsilon) {
                    angleToConnected = Helpers2D.GetAngle(connectedBodyPosition - center);
                } else {
                    angleToConnected = JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Connected);
                }

                using (new HandleColor(GetAdjustedColor(editorSettings.anchorsToConnectedBodyColor)))
                {
                    Handles.DrawLine(center,
                        center + Helpers2D.GetDirection(angleToConnected + settings.connectedAngleOffset) * handleSize);
                }
            } else {
                using (new HandleColor(GetAdjustedColor(editorSettings.anchorsToConnectedBodyColor)))
                {
                    Handles.DrawLine(center, center + Helpers2D.GetDirection(settings.connectedAngleOffset) * handleSize);
                }
            }
        }

        if (settings.showDiscs) {
            var sliderControlID = anchorInfo.GetControlID("slider");

            if (editorSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Always ||
                (editorSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Hover &&
                 //if nothing else is hot and we are being hovered, or the anchor's widgets are hot
                 ((GUIUtility.hotControl == 0 && HandleUtility.nearestControl == sliderControlID) ||
                  GUIUtility.hotControl == sliderControlID))
                ) {
                    using (new HandleColor(GetAdjustedColor(editorSettings.mainRingColor)))
                    {
                    Handles.DrawWireDisc(center, Vector3.forward, Vector2.Distance(center, mainBodyPosition));
                }

                if (hingeJoint2D.connectedBody) {
                    using (new HandleColor((editorSettings.connectedRingColor))) {
                        Handles.DrawWireDisc(center, Vector3.forward,
                            Vector2.Distance(center,
                                connectedBodyPosition));
                    }
                }
            }
        }

        HandleUtility.Repaint();
    }

    protected override void InspectorDisplayGUI(bool enabled) {
        var allSettings =
            targets.Cast<HingeJoint2D>()
                   .Select(hingeJoint2D => SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D))
                   .Where(hingeSettings => hingeSettings != null).Cast<Object>().ToList();

        var serializedSettings = new SerializedObject(allSettings.ToArray());
        ToggleShowAngleLimits(serializedSettings, enabled);
        ToggleShowDiscs(serializedSettings, enabled);
        var showAngleLimits = serializedSettings.FindProperty("showAngleLimits");
        SelectAngleLimitsMode(serializedSettings,
            enabled && (showAngleLimits.boolValue || showAngleLimits.hasMultipleDifferentValues));

        var mainAngleOffset = serializedSettings.FindProperty("mainAngleOffset");
        var connectedAngleOffset = serializedSettings.FindProperty("connectedAngleOffset");

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(mainAngleOffset);
        EditorGUILayout.PropertyField(connectedAngleOffset);

        if (EditorGUI.EndChangeCheck()) {
            using (new Modification("Angle Offset", targets)) {
                serializedSettings.ApplyModifiedProperties();
            }
        }
    }

    private static readonly GUIContent AngleLimitsModeContent =
        new GUIContent("Anchor Priority",
            "Which anchor's angle limits would you like to see? If there is no connected body this setting will be ignored.");

    private void SelectAngleLimitsMode(SerializedObject serializedSettings, bool enabled) {
        EditorGUI.BeginChangeCheck();
        HingeJoint2DSettings.AnchorPriority value;

        using (new GUIEnabled(enabled)) {
            var anchorPriority = serializedSettings.FindProperty("anchorPriority");
            EditorGUILayout.PropertyField(anchorPriority, AngleLimitsModeContent);
            value = (HingeJoint2DSettings.AnchorPriority)
                Enum.Parse(typeof (HingeJoint2DSettings.AnchorPriority),
                    anchorPriority.enumNames[anchorPriority.enumValueIndex]);
        }

        if (EditorGUI.EndChangeCheck()) {
            foreach (var t in targets) {
                var hingeJoint2D = (HingeJoint2D) t;
                var hingeSettings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

                EditorHelpers.RecordUndo("toggle angle limits display mode", hingeSettings);
                hingeSettings.anchorPriority = value;
                EditorUtility.SetDirty(hingeSettings);
            }
        }
    }

    private static readonly GUIContent AngleLimitsContent =
        new GUIContent("Angle Limits", "Toggles the display of angle limits on the scene GUI.");

    private void ToggleShowAngleLimits(SerializedObject serializedSettings, bool enabled) {
        EditorGUI.BeginChangeCheck();

        using (new GUIEnabled(enabled)) {
            var showAngleLimits = serializedSettings.FindProperty("showAngleLimits");
            EditorGUILayout.PropertyField(showAngleLimits, AngleLimitsContent);
        }

        if (EditorGUI.EndChangeCheck()) {
            serializedSettings.ApplyModifiedProperties();
        }
    }

    private static readonly GUIContent DiscsContent =
        new GUIContent("Discs", "Toggles the display of movement discs on the scene GUI.");

    private void ToggleShowDiscs(SerializedObject serializedSettings, bool enabled) {
        EditorGUI.BeginChangeCheck();

        using (new GUIEnabled(enabled)) {
            var showAngleLimits = serializedSettings.FindProperty("showDiscs");
            EditorGUILayout.PropertyField(showAngleLimits, DiscsContent);
        }

        if (EditorGUI.EndChangeCheck()) {
            serializedSettings.ApplyModifiedProperties();
        }
    }

    protected override void OwnershipMoved(AnchoredJoint2D cloneJoint) {
        //swap limits
        var hingeJoint2D = cloneJoint as HingeJoint2D;
        if (!hingeJoint2D) {
            return;
        }

        var settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

        if (settings.anchorPriority == HingeJoint2DSettings.AnchorPriority.Main) {
            settings.anchorPriority = HingeJoint2DSettings.AnchorPriority.Connected;
        } else if (settings.anchorPriority == HingeJoint2DSettings.AnchorPriority.Connected) {
            settings.anchorPriority = HingeJoint2DSettings.AnchorPriority.Main;
        }

        var useLimits = hingeJoint2D.useLimits;

        var limits = hingeJoint2D.limits;
        limits.min = -hingeJoint2D.limits.max;
        limits.max = -hingeJoint2D.limits.min;
        hingeJoint2D.limits = limits;

        hingeJoint2D.useLimits = useLimits;
    }


    protected override void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint) {
        var hingeJoint2D = joint as HingeJoint2D;
        if (hingeJoint2D != null) {
            menu.AddItem(new GUIContent("Use Motor"), hingeJoint2D.useMotor, () => {
                EditorHelpers.RecordUndo("Use Motor", hingeJoint2D);
                hingeJoint2D.useMotor = !hingeJoint2D.useMotor;
                EditorUtility.SetDirty(hingeJoint2D);
            });

            var mousePosition = Event.current.mousePosition;

            menu.AddItem(new GUIContent("Configure Motor"), false, () =>
                ShowUtility(
                    "Configure Motor",
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 6),
                    delegate(Action close, bool focused) {
                        EditorGUILayout.LabelField(new GUIContent("Hinge Joint 2D Motor", "The joint motor."));
                        using (new Indent()) {
                            EditorGUI.BeginChangeCheck();

                            var useMotor =
                                EditorGUILayout.Toggle(
                                    new GUIContent("Use Motor", "Whether to use the joint motor or not."),
                                    hingeJoint2D.useMotor);

                            GUI.SetNextControlName("Motor Config");
                            var motorSpeed = EditorGUILayout.FloatField(
                                new GUIContent("Motor Speed",
                                    "The target motor speed in degrees/second. [-100000, 1000000 ]."),
                                hingeJoint2D.motor.motorSpeed);
                            GUI.SetNextControlName("Motor Config");
                            var maxMotorTorque = EditorGUILayout.FloatField(
                                new GUIContent("Maximum Motor Force",
                                    "The maximum force the motor can use to achieve the desired motor speed. [ 0, 1000000 ]."),
                                hingeJoint2D.motor.maxMotorTorque);

                            if (EditorGUI.EndChangeCheck()) {
                                using (new Modification("Configure Motor", hingeJoint2D)) {
                                    var motor = hingeJoint2D.motor;
                                    motor.motorSpeed = motorSpeed;
                                    motor.maxMotorTorque = maxMotorTorque;
                                    hingeJoint2D.motor = motor;

                                    hingeJoint2D.useMotor = useMotor;
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

            menu.AddItem(new GUIContent("Use Limits"), hingeJoint2D.useLimits, () => {
                EditorHelpers.RecordUndo("Use Limits", hingeJoint2D);
                hingeJoint2D.useLimits = !hingeJoint2D.useLimits;
                EditorUtility.SetDirty(hingeJoint2D);
            });

            menu.AddItem(new GUIContent("Configure Limits"), false, () =>
                ShowUtility(
                    "Configure Limits",
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 6),
                    delegate(Action close, bool focused) {
                        EditorGUILayout.LabelField(new GUIContent("Angle Limits", "The joint angle limits"));
                        using (new Indent()) {
                            EditorGUI.BeginChangeCheck();

                            var useLimits =
                                EditorGUILayout.Toggle(
                                    new GUIContent("Use Limits", "Whether to use the angle limits or not."),
                                    hingeJoint2D.useLimits);

                            GUI.SetNextControlName("Limits Config");
                            var lowerAngle = EditorGUILayout.FloatField(
                                new GUIContent("Lower Angle",
                                    "The minimum value that the joint angle will be limited to. [ -100000, 1000000 ]."),
                                hingeJoint2D.limits.min);
                            GUI.SetNextControlName("Limits Config");
                            var upperAngle = EditorGUILayout.FloatField(
                                new GUIContent("Upper Angle",
                                    "The maximum value that the joint angle will be limited to. [ -100000, 1000000 ]."),
                                hingeJoint2D.limits.max);

                            if (EditorGUI.EndChangeCheck()) {
                                using (new Modification("Configure Limits", hingeJoint2D)) {
                                    var limits2D = hingeJoint2D.limits;
                                    limits2D.min = lowerAngle;
                                    limits2D.max = upperAngle;
                                    hingeJoint2D.limits = limits2D;

                                    hingeJoint2D.useLimits = useLimits;
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
}