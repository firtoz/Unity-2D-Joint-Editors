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
        HingeJoint2D hingeJoint2D = joint2D as HingeJoint2D;
        if (hingeJoint2D == null) {
            return false;
        }

        if (!hingeJoint2D.useLimits) {
            return false;
        }

        bool showAngle = false;
        float angle = 0;
        float displayAngle = 0;

        float jointAngle;
        if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) {
            jointAngle = hingeJoint2D.jointAngle;
        }
        else {
            jointAngle = 0;
        }

        Vector2 startPosition = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

        Vector2 mainBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);

        float mainBodyAngle = JointHelpers.AngleFromAnchor(startPosition, mainBodyPosition,
            JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Main));

        Vector2 connectedBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);

        float connectedBodyAngle;
        if (hingeJoint2D.connectedBody) {
            connectedBodyAngle = JointHelpers.AngleFromAnchor(startPosition, connectedBodyPosition,
                JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Connected));
        }
        else {
            connectedBodyAngle = 0;
        }

        float angleDiff = jointAngle - (connectedBodyAngle - mainBodyAngle);

        float liveMainAngle = connectedBodyAngle + angleDiff;

        float minMainAngle = liveMainAngle - hingeJoint2D.limits.min;
        float maxMainAngle = liveMainAngle - hingeJoint2D.limits.max;

        string labelText = "";

        if (EditorHelpers.IsWarm(info.GetControlID("lowerMainAngle"))) {
            showAngle = true;
            angle = minMainAngle;
            displayAngle = hingeJoint2D.limits.min;
            labelText = "Lower: ";
        } else if (EditorHelpers.IsWarm(info.GetControlID("upperMainAngle"))) {
            showAngle = true;
            angle = maxMainAngle;
            displayAngle = hingeJoint2D.limits.max;
            labelText = "Upper: ";
        } else if (EditorHelpers.IsWarm(info.GetControlID("lowerConnectedAngle"))) {
            showAngle = true;
            angle = hingeJoint2D.limits.min;
            displayAngle = angle;
            labelText = "Lower: ";
            startPosition = JointHelpers.GetConnectedAnchorPosition(hingeJoint2D);
        } else if (EditorHelpers.IsWarm(info.GetControlID("upperConnectedAngle"))) {
            showAngle = true;
            angle = hingeJoint2D.limits.max;
            labelText = "Upper: ";
            displayAngle = angle;
            startPosition = JointHelpers.GetConnectedAnchorPosition(hingeJoint2D);
        }

        LimitContext(hingeJoint2D, info.GetControlID("lowerMainAngle"), Limit.Min);
        LimitContext(hingeJoint2D, info.GetControlID("upperMainAngle"), Limit.Max);
        LimitContext(hingeJoint2D, info.GetControlID("lowerConnectedAngle"), Limit.Min);
        LimitContext(hingeJoint2D, info.GetControlID("upperConnectedAngle"), Limit.Max);

        if (showAngle) {
            float distanceFromCenter = GetAngleSliderRadius(startPosition);

            Vector2 anglePosition = startPosition + Helpers2D.GetDirection(angle)*distanceFromCenter;

            GUIContent labelContent = new GUIContent(labelText+"\n"+String.Format("{0:0.00}", displayAngle));

            float fontSize = HandleUtility.GetHandleSize(anglePosition)*(1f/64f);

            float labelOffset = fontSize*EditorHelpers.FontWithBackgroundStyle.CalcSize(labelContent).y;

            EditorHelpers.OverlayLabel((Vector3)anglePosition + (Camera.current.transform.up * labelOffset), labelContent,
                EditorHelpers.FontWithBackgroundStyle);

//            Handles.Label((Vector3) anglePosition + (Camera.current.transform.up*labelOffset), labelContent,
//                EditorHelpers.FontWithBackgroundStyle);
        }
        return false;
    }

    private static float GetAngleSliderRadius(Vector2 startPosition) {
        return HandleUtility.GetHandleSize(startPosition)*EditorHelpers.HandleSizeToPixels*
               editorSettings.angleLimitRadius;
    }

    private static void LimitContext(HingeJoint2D hingeJoint2D, int controlID, Limit limit) {
        Vector2 mousePosition = Event.current.mousePosition;

        string limitName = (limit == Limit.Min ? "Lower" : "Upper") + " Angle Limit";

        EditorHelpers.ContextClick(controlID, () => {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Edit " + limitName), false, () =>
                EditorHelpers.ShowDropDown(
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight*2),
                    delegate(Action close, bool focused) {
                        EditorGUI.BeginChangeCheck();
                        GUI.SetNextControlName(limitName);
                        float newLimit = EditorGUILayout.FloatField(limitName,
                            limit == Limit.Min
                                ? hingeJoint2D.limits.min
                                : hingeJoint2D.limits.max);
                        if (EditorGUI.EndChangeCheck()) {
                            JointAngleLimits2D limits = hingeJoint2D.limits;
                            if (limit == Limit.Min) {
                                limits.min = newLimit;
                            }
                            else {
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
        HingeJoint2D hingeJoint2D = joint2D as HingeJoint2D;
        if (hingeJoint2D == null) {
            return false;
        }

        return DrawAngleLimits(hingeJoint2D, anchorInfo, bias);
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        HingeJoint2D hingeJoint2D = joint2D as HingeJoint2D;

        if (hingeJoint2D == null) {
            return false;
        }

        if (bias == JointHelpers.AnchorBias.Either) {
            DrawLinesAndDiscs(hingeJoint2D, anchorInfo, JointHelpers.AnchorBias.Main);
            DrawLinesAndDiscs(hingeJoint2D, anchorInfo, JointHelpers.AnchorBias.Connected);
        } else {
            DrawLinesAndDiscs(hingeJoint2D, anchorInfo, bias);
        }

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(hingeJoint2D);
        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(hingeJoint2D);
        if (bias == JointHelpers.AnchorBias.Main && Vector2.Distance(mainAnchorPosition, connectedAnchorPosition) > AnchorEpsilon) {
            using (new HandleColor(Color.green)) {
                Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
            }
        }
        return false;
    }


    private bool DrawAngleLimits(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        bool changed = false;
        HingeJoint2DSettings settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);
        if (!settings.showAngleLimits) {
            return false;
        }

        if (hingeJoint2D.useLimits) {
            JointAngleLimits2D limits = hingeJoint2D.limits;
            float minLimit = limits.min;
            float maxLimit = limits.max;


            HingeJoint2DSettings.AnchorPriority anchorPriority = settings.anchorPriority;

            bool showMain = anchorPriority == HingeJoint2DSettings.AnchorPriority.Main ||
                            anchorPriority == HingeJoint2DSettings.AnchorPriority.Both;

            bool showConnected = (anchorPriority == HingeJoint2DSettings.AnchorPriority.Connected ||
                                  anchorPriority == HingeJoint2DSettings.AnchorPriority.Both);

            Vector2 anchorPosition = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

            float distanceFromCenter = GetAngleSliderRadius(anchorPosition);
            float angleHandleSize = editorSettings.angleHandleSize;

            float jointAngle;
            bool isPlaying = EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying;
            if (isPlaying) {
                jointAngle = hingeJoint2D.jointAngle;
            }
            else {
                jointAngle = 0;
            }

            Vector2 mainBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);

            float mainBodyAngle = JointHelpers.AngleFromAnchor(anchorPosition, mainBodyPosition,
                JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Main));

            Vector2 connectedBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);

            float connectedBodyAngle;
            if (hingeJoint2D.connectedBody) {
                connectedBodyAngle = JointHelpers.AngleFromAnchor(anchorPosition, connectedBodyPosition,
                    JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Connected));
            }
            else {
                connectedBodyAngle = 0;
            }

            float angleDiff = jointAngle - (connectedBodyAngle - mainBodyAngle);

            float liveMainAngle = connectedBodyAngle + angleDiff;

            float minMainAngle = liveMainAngle - minLimit;
            float maxMainAngle = liveMainAngle - maxLimit;

            float limitDifference = maxLimit - minLimit;

            Color limitColor, limitAreaColor;
            if (!isPlaying
                &&
                ((minLimit < jointAngle && maxLimit < jointAngle) || (minLimit > jointAngle && maxLimit > jointAngle))) {
                limitColor = editorSettings.incorrectLimitsColor;
                limitAreaColor = editorSettings.incorrectLimitsArea;
            }
            else {
                limitColor = editorSettings.correctLimitsColor;
                limitAreaColor = editorSettings.limitsAreaColor;
            }

            Color angleWidgetColor = editorSettings.angleWidgetColor;
            Color activeAngleColor = editorSettings.activeAngleColor;
            Color hoverAngleColor = editorSettings.hoverAngleColor;

            if (isCreatedByTarget)
            {
                angleWidgetColor.a *= editorSettings.connectedJointTransparency;
                activeAngleColor.a *= editorSettings.connectedJointTransparency;
                hoverAngleColor.a *= editorSettings.connectedJointTransparency;
            
                limitColor.a *= editorSettings.connectedJointTransparency;
                limitAreaColor.a *= editorSettings.connectedJointTransparency;
            }

            if (showMain && bias != JointHelpers.AnchorBias.Connected) {
                using (new HandleColor(limitAreaColor)) {
                    if (limitDifference > 360) {
                        Handles.DrawSolidDisc(anchorPosition, Vector3.forward, distanceFromCenter);
                    }
                    else {
                        Handles.DrawSolidArc(anchorPosition, Vector3.forward,
                            Helpers2D.GetDirection(maxMainAngle),
                            limitDifference, distanceFromCenter);
                    }
                }
                using (new HandleColor(limitColor)) {
                    Vector3 minMainEnd = anchorPosition +
                                         Helpers2D.GetDirection(minMainAngle)*distanceFromCenter;
                    Handles.DrawLine(anchorPosition, minMainEnd);

                    Vector3 maxMainEnd = anchorPosition +
                                         Helpers2D.GetDirection(maxMainAngle)*distanceFromCenter;
                    Handles.DrawLine(anchorPosition, maxMainEnd);

                    if (limitDifference > 360) {
                        Handles.DrawWireDisc(anchorPosition, Vector3.forward, distanceFromCenter);
                    }
                    else {
                        Handles.DrawWireArc(anchorPosition, Vector3.forward,
                            Helpers2D.GetDirection(maxMainAngle),
                            limitDifference, distanceFromCenter);
                    }


                    EditorGUI.BeginChangeCheck();
                    using (
                        HandleDrawerBase drawer = new HandleCircleDrawer(angleWidgetColor, activeAngleColor, hoverAngleColor)) {
                        minMainAngle = EditorHelpers.AngleSlider(anchorInfo.GetControlID("lowerMainAngle"), drawer,
                            anchorPosition,
                            minMainAngle,
                            distanceFromCenter, angleHandleSize*HandleUtility.GetHandleSize(minMainEnd)/64);
                    }

                    if (EditorGUI.EndChangeCheck()) {
                        EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                        limits.min = Handles.SnapValue(liveMainAngle - minMainAngle, editorSettings.hingeSnapAngle);
                        hingeJoint2D.limits = limits;
                        changed = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    using (
                        HandleDrawerBase drawer = new HandleCircleDrawer(angleWidgetColor,
                            activeAngleColor, hoverAngleColor)) {
                        maxMainAngle = EditorHelpers.AngleSlider(anchorInfo.GetControlID("upperMainAngle"), drawer,
                            anchorPosition,
                            maxMainAngle,
                            distanceFromCenter, angleHandleSize*HandleUtility.GetHandleSize(maxMainEnd)/64);
                    }

                    if (EditorGUI.EndChangeCheck()) {
                        EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                        limits.max = Handles.SnapValue(liveMainAngle - maxMainAngle, editorSettings.hingeSnapAngle);
                        hingeJoint2D.limits = limits;
                        changed = true;
                    }
                }
            }


            if (showConnected && bias != JointHelpers.AnchorBias.Main) {
                float liveConnectedAngle = mainBodyAngle - angleDiff;

                float minConnectedAngle = liveConnectedAngle + minLimit;
                float maxConnectedAngle = liveConnectedAngle + maxLimit;

                using (new HandleColor(limitAreaColor)) {
                    {
                        if (limitDifference > 360) {
                            Handles.DrawSolidDisc(anchorPosition, Vector3.forward, distanceFromCenter);
                        }
                        else {
                            Handles.DrawSolidArc(anchorPosition, Vector3.forward,
                                Helpers2D.GetDirection(minConnectedAngle),
                                limitDifference, distanceFromCenter);
                        }
                    }
                }
                using (new HandleColor(limitColor)) {
                    {
                        Vector3 minConnectedEnd = anchorPosition +
                                                  Helpers2D.GetDirection(minConnectedAngle)*distanceFromCenter;
                        Handles.DrawLine(anchorPosition, minConnectedEnd);

                        Vector3 maxConnectedEnd = anchorPosition +
                                                  Helpers2D.GetDirection(maxConnectedAngle)*distanceFromCenter;
                        Handles.DrawLine(anchorPosition, maxConnectedEnd);

                        if (limitDifference > 360) {
                            Handles.DrawWireDisc(anchorPosition, Vector3.forward, distanceFromCenter);
                        }
                        else {
                            Handles.DrawWireArc(anchorPosition, Vector3.forward,
                                Helpers2D.GetDirection(minConnectedAngle),
                                limitDifference, distanceFromCenter);
                        }

                        EditorGUI.BeginChangeCheck();
                        using (
                            HandleDrawerBase drawer = new HandleCircleDrawer(angleWidgetColor,
                                activeAngleColor, hoverAngleColor)) {
                            minConnectedAngle = EditorHelpers.AngleSlider(
                                anchorInfo.GetControlID("lowerConnectedAngle"), drawer,
                                anchorPosition,
                                minConnectedAngle,
                                distanceFromCenter, angleHandleSize*HandleUtility.GetHandleSize(minConnectedEnd)/64);
                        }

                        if (EditorGUI.EndChangeCheck()) {
                            EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                            limits.min = Handles.SnapValue(minConnectedAngle - liveConnectedAngle, editorSettings.hingeSnapAngle);
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
                                maxConnectedAngle,
                                distanceFromCenter, angleHandleSize*HandleUtility.GetHandleSize(maxConnectedEnd)/64);
                        }

                        if (EditorGUI.EndChangeCheck()) {
                            EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                            limits.max = Handles.SnapValue(maxConnectedAngle - liveConnectedAngle, editorSettings.hingeSnapAngle);
                            hingeJoint2D.limits = limits;
                            changed = true;
                        }
                    }
                }
            }
        }

        return changed;
    }


    private void DrawLinesAndDiscs(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        Vector2 center = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

        float scale = editorSettings.anchorScale;
        float handleSize = HandleUtility.GetHandleSize(center)*scale;

        Vector2 mainBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);
        Vector2 connectedBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);

        if (bias == JointHelpers.AnchorBias.Main) {
            if (Vector2.Distance(mainBodyPosition, center) <= AnchorEpsilon)
            {
                using (new HandleColor(editorSettings.anchorsToMainBodyColor))
                {
                    float rot = JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Main);
                    Handles.DrawLine(center, center + Helpers2D.GetDirection(rot) * handleSize);
                }
            }
        }
        else if (bias == JointHelpers.AnchorBias.Connected) {
            if (hingeJoint2D.connectedBody)
            {
                if (Vector2.Distance(connectedBodyPosition, center) <= AnchorEpsilon)
                {
                    using (new HandleColor(editorSettings.anchorsToConnectedBodyColor))
                    {
                        float rot = JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Connected);
                        Handles.DrawLine(center, center + Helpers2D.GetDirection(rot) * handleSize);
                    }
                }
            }
            else
            {
                using (new HandleColor(editorSettings.anchorsToConnectedBodyColor))
                {
                    Handles.DrawLine(center, center + Helpers2D.GetDirection(0) * handleSize);
                }
            }
        }

        HingeJoint2DSettings settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);
        if (settings.showDiscs)
        {
            int sliderControlID = anchorInfo.GetControlID("slider");

            if (editorSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Always ||
                (editorSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Hover &&
                //if nothing else is hot and we are being hovered, or the anchor's widgets are hot
                 ((GUIUtility.hotControl == 0 && HandleUtility.nearestControl == sliderControlID) || GUIUtility.hotControl == sliderControlID))
                )
            {
                using (new HandleColor(editorSettings.mainRingColor))
                {
                    Handles.DrawWireDisc(center, Vector3.forward, Vector2.Distance(center, mainBodyPosition));
                }

                if (hingeJoint2D.connectedBody)
                {
                    using (new HandleColor(editorSettings.connectedRingColor))
                    {
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
        List<Object> allSettings =
            targets.Cast<HingeJoint2D>()
                .Select(hingeJoint2D => SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D))
                .Where(hingeSettings => hingeSettings != null).Cast<Object>().ToList();

        SerializedObject serializedSettings = new SerializedObject(allSettings.ToArray());
        ToggleShowAngleLimits(serializedSettings, enabled);
        ToggleShowDiscs(serializedSettings, enabled);
        using (new Indent()) {
            SerializedProperty showAngleLimits = serializedSettings.FindProperty("showAngleLimits");
            SelectAngleLimitsMode(serializedSettings,
                enabled && (showAngleLimits.boolValue || showAngleLimits.hasMultipleDifferentValues));
        }
    }

    private static readonly GUIContent AngleLimitsModeContent =
        new GUIContent("Anchor Priority",
            "Which anchor's angle limits would you like to see? If there is no connected body this setting will be ignored.");

    private void SelectAngleLimitsMode(SerializedObject serializedSettings, bool enabled) {
        EditorGUI.BeginChangeCheck();
        HingeJoint2DSettings.AnchorPriority value;

        using (new GUIEnabled(enabled)) {
            SerializedProperty anchorPriority = serializedSettings.FindProperty("anchorPriority");
            EditorGUILayout.PropertyField(anchorPriority, AngleLimitsModeContent);
            value = (HingeJoint2DSettings.AnchorPriority)
                Enum.Parse(typeof (HingeJoint2DSettings.AnchorPriority),
                    anchorPriority.enumNames[anchorPriority.enumValueIndex]);
        }

        if (EditorGUI.EndChangeCheck()) {
            foreach (Object t in targets) {
                HingeJoint2D hingeJoint2D = (HingeJoint2D) t;
                HingeJoint2DSettings hingeSettings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

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
            SerializedProperty showAngleLimits = serializedSettings.FindProperty("showAngleLimits");
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
            SerializedProperty showAngleLimits = serializedSettings.FindProperty("showDiscs");
            EditorGUILayout.PropertyField(showAngleLimits, DiscsContent);
        }

        if (EditorGUI.EndChangeCheck()) {
            serializedSettings.ApplyModifiedProperties();
        }
    }

    protected override void OwnershipMoved(AnchoredJoint2D cloneJoint) {
        //swap limits
        HingeJoint2D hingeJoint2D = cloneJoint as HingeJoint2D;
        if (!hingeJoint2D) {
            return;
        }

        HingeJoint2DSettings settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

        if (settings.anchorPriority == HingeJoint2DSettings.AnchorPriority.Main) {
            settings.anchorPriority = HingeJoint2DSettings.AnchorPriority.Connected;
        }
        else if (settings.anchorPriority == HingeJoint2DSettings.AnchorPriority.Connected) {
            settings.anchorPriority = HingeJoint2DSettings.AnchorPriority.Main;
        }

        bool useLimits = hingeJoint2D.useLimits;

        JointAngleLimits2D limits = hingeJoint2D.limits;
        limits.min = -hingeJoint2D.limits.max;
        limits.max = -hingeJoint2D.limits.min;
        hingeJoint2D.limits = limits;

        hingeJoint2D.useLimits = useLimits;
    }


    protected override void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint) {
        HingeJoint2D hingeJoint2D = joint as HingeJoint2D;
        if (hingeJoint2D != null) {
            menu.AddItem(new GUIContent("Use Motor"), hingeJoint2D.useMotor, () => {
                EditorHelpers.RecordUndo("Use Motor", hingeJoint2D);
                hingeJoint2D.useMotor = !hingeJoint2D.useMotor;
                EditorUtility.SetDirty(hingeJoint2D);
            });

            Vector2 mousePosition = Event.current.mousePosition;

            menu.AddItem(new GUIContent("Configure Motor"), false, () =>
                EditorHelpers.ShowDropDown(
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight*6),
                    delegate(Action close, bool focused) {
                        EditorGUILayout.LabelField(new GUIContent("Hinge Joint 2D Motor", "The joint motor."));
                        using (new Indent()) {
                            EditorGUI.BeginChangeCheck();

                            bool useMotor =
                                EditorGUILayout.Toggle(
                                    new GUIContent("Use Motor", "Whether to use the joint motor or not."),
                                    hingeJoint2D.useMotor);

                            GUI.SetNextControlName("Motor Config");
                            float motorSpeed = EditorGUILayout.FloatField(
                                new GUIContent("Motor Speed",
                                    "The target motor speed in degrees/second. [-100000, 1000000 ]."),
                                hingeJoint2D.motor.motorSpeed);
                            GUI.SetNextControlName("Motor Config");
                            float maxMotorTorque = EditorGUILayout.FloatField(
                                new GUIContent("Maximum Motor Force",
                                    "The maximum force the motor can use to achieve the desired motor speed. [ 0, 1000000 ]."),
                                hingeJoint2D.motor.maxMotorTorque);

                            if (EditorGUI.EndChangeCheck()) {
                                using (new Modification("Configure Motor", hingeJoint2D)) {
                                    JointMotor2D motor = hingeJoint2D.motor;
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
                EditorHelpers.ShowDropDown(
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight*6),
                    delegate(Action close, bool focused) {
                        EditorGUILayout.LabelField(new GUIContent("Angle Limits", "The joint angle limits"));
                        using (new Indent()) {
                            EditorGUI.BeginChangeCheck();

                            bool useLimits =
                                EditorGUILayout.Toggle(
                                    new GUIContent("Use Limits", "Whether to use the angle limits or not."),
                                    hingeJoint2D.useLimits);

                            GUI.SetNextControlName("Limits Config");
                            float lowerAngle = EditorGUILayout.FloatField(
                                new GUIContent("Lower Angle",
                                    "The minimum value that the joint angle will be limited to. [ -100000, 1000000 ]."),
                                hingeJoint2D.limits.min);
                            GUI.SetNextControlName("Limits Config");
                            float upperAngle = EditorGUILayout.FloatField(
                                new GUIContent("Upper Angle",
                                    "The maximum value that the joint angle will be limited to. [ -100000, 1000000 ]."),
                                hingeJoint2D.limits.max);

                            if (EditorGUI.EndChangeCheck()) {
                                using (new Modification("Configure Limits", hingeJoint2D)) {
                                    JointAngleLimits2D limits2D = hingeJoint2D.limits;
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