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
public class HingeJoint2DEditor : Joint2DEditor {
    protected override bool WantsLocking() {
        return true;
    }

    private static readonly HashSet<string> Names = new HashSet<String> {
        "radius",
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

        if (GUIUtility.hotControl == info.GetControlID("lowerMainAngle")) {
            showAngle = true;
            angle = minMainAngle;
            displayAngle = hingeJoint2D.limits.min;
        }
        else if (GUIUtility.hotControl == info.GetControlID("upperMainAngle")) {
            showAngle = true;
            angle = maxMainAngle;
            displayAngle = hingeJoint2D.limits.max;
        }
        if (GUIUtility.hotControl == info.GetControlID("lowerConnectedAngle")) {
            showAngle = true;
            angle = hingeJoint2D.limits.min;
            displayAngle = angle;
            startPosition = JointHelpers.GetConnectedAnchorPosition(hingeJoint2D);
        }
        else if (GUIUtility.hotControl == info.GetControlID("upperConnectedAngle")) {
            showAngle = true;
            angle = hingeJoint2D.limits.max;
            displayAngle = angle;
            startPosition = JointHelpers.GetConnectedAnchorPosition(hingeJoint2D);
        }

        LimitContext(hingeJoint2D, info.GetControlID("lowerMainAngle"), Limit.Min);
        LimitContext(hingeJoint2D, info.GetControlID("upperMainAngle"), Limit.Max);
        LimitContext(hingeJoint2D, info.GetControlID("lowerConnectedAngle"), Limit.Min);
        LimitContext(hingeJoint2D, info.GetControlID("upperConnectedAngle"), Limit.Max);

        if (showAngle) {
            float handleSize = HandleUtility.GetHandleSize(startPosition)*editorSettings.orbitRangeScale*.5f;
            float angleHandleSize = editorSettings.angleHandleSize;
            float distanceFromCenter = (handleSize +
                                        (angleHandleSize*HandleUtility.GetHandleSize(startPosition)/64));

            Vector2 anglePosition = startPosition + Helpers2D.GetDirection(angle)*distanceFromCenter;

            GUIContent labelContent = new GUIContent(String.Format("{0:0.00}", displayAngle));

            float fontSize = HandleUtility.GetHandleSize(anglePosition)*(1f/64f);

            float labelOffset = fontSize*EditorHelpers.FontWithBackgroundStyle.CalcSize(labelContent).y;

            Handles.Label((Vector3) anglePosition + (Camera.current.transform.up*labelOffset), labelContent,
                EditorHelpers.FontWithBackgroundStyle);
        }
        return false;
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

        bool changed = !anchorInfo.ignoreHover && RadiusGUI(hingeJoint2D, anchorInfo, bias);

        DrawDiscs(hingeJoint2D, anchorInfo, bias);

//        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(hingeJoint2D);
//        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(hingeJoint2D);
//        if (Vector2.Distance(mainAnchorPosition, connectedAnchorPosition) > AnchorEpsilon) {
//            using (new HandleColor(Color.green)) {
//                Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
//            }
//        }
        return changed;
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

            float handleSize = HandleUtility.GetHandleSize(anchorPosition)*editorSettings.orbitRangeScale*.5f;
            float angleHandleSize = editorSettings.angleHandleSize;
            float distanceFromCenter = (handleSize +
                                        (angleHandleSize*HandleUtility.GetHandleSize(anchorPosition)/64));

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

            Color limitColor;
            if (!isPlaying
                && ((minLimit < jointAngle && maxLimit < jointAngle) || (minLimit > jointAngle && maxLimit > jointAngle)))
            {
                limitColor = editorSettings.incorrectLimitsColor;
            }
            else {
                limitColor = editorSettings.angleLimitColor;
            }

            if (showMain && bias != JointHelpers.AnchorBias.Connected) {
                using (new HandleColor(editorSettings.angleAreaColor)) {
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
                    else
                    {
                        Handles.DrawWireArc(anchorPosition, Vector3.forward,
                            Helpers2D.GetDirection(maxMainAngle),
                            limitDifference, distanceFromCenter);
                    }

                    EditorGUI.BeginChangeCheck();
                    using (HandleDrawerBase drawer = new HandleCircleDrawer(editorSettings.inactiveAngleColor, editorSettings.activeAngleColor, editorSettings.hoverAngleColor))
                    {
                        minMainAngle = EditorHelpers.AngleSlider(anchorInfo.GetControlID("lowerMainAngle"), drawer,
                            anchorPosition,
                            minMainAngle,
                            distanceFromCenter, angleHandleSize*HandleUtility.GetHandleSize(minMainEnd)/64);
                    }

                    if (EditorGUI.EndChangeCheck()) {
                        EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                        limits.min = Handles.SnapValue(liveMainAngle - minMainAngle, 45);
                        hingeJoint2D.limits = limits;
                        changed = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    using (HandleDrawerBase drawer = new HandleCircleDrawer(editorSettings.inactiveAngleColor, editorSettings.activeAngleColor, editorSettings.hoverAngleColor))
                    {
                        maxMainAngle = EditorHelpers.AngleSlider(anchorInfo.GetControlID("upperMainAngle"), drawer,
                            anchorPosition,
                            maxMainAngle,
                            distanceFromCenter, angleHandleSize*HandleUtility.GetHandleSize(maxMainEnd)/64);
                    }

                    if (EditorGUI.EndChangeCheck()) {
                        EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                        limits.max = Handles.SnapValue(liveMainAngle - maxMainAngle, 45);
                        hingeJoint2D.limits = limits;
                        changed = true;
                    }
                }
            }


            if (showConnected && bias != JointHelpers.AnchorBias.Main) {
                float liveConnectedAngle = mainBodyAngle - angleDiff;

                float minConnectedAngle = liveConnectedAngle + minLimit;
                float maxConnectedAngle = liveConnectedAngle + maxLimit;


                using (new HandleColor(editorSettings.angleAreaColor)) {
                    {
                        if (limitDifference > 360)
                        {
                            Handles.DrawSolidDisc(anchorPosition, Vector3.forward, distanceFromCenter);
                        }
                        else
                        {
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
                        using (HandleDrawerBase drawer = new HandleCircleDrawer(editorSettings.inactiveAngleColor, editorSettings.activeAngleColor, editorSettings.hoverAngleColor))
                        {
                            minConnectedAngle = EditorHelpers.AngleSlider(
                                anchorInfo.GetControlID("lowerConnectedAngle"), drawer,
                                anchorPosition,
                                minConnectedAngle,
                                distanceFromCenter, angleHandleSize*HandleUtility.GetHandleSize(minConnectedEnd)/64);
                        }

                        if (EditorGUI.EndChangeCheck()) {
                            EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                            limits.min = Handles.SnapValue(minConnectedAngle - liveConnectedAngle, 45);
                            hingeJoint2D.limits = limits;
                            changed = true;
                        }

                        EditorGUI.BeginChangeCheck();
                        using (HandleDrawerBase drawer = new HandleCircleDrawer(editorSettings.inactiveAngleColor, editorSettings.activeAngleColor, editorSettings.hoverAngleColor))
                        {
                            maxConnectedAngle = EditorHelpers.AngleSlider(
                                anchorInfo.GetControlID("upperConnectedAngle"), drawer,
                                anchorPosition,
                                maxConnectedAngle,
                                distanceFromCenter, angleHandleSize*HandleUtility.GetHandleSize(maxConnectedEnd)/64);
                        }

                        if (EditorGUI.EndChangeCheck()) {
                            EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                            limits.max = Handles.SnapValue(maxConnectedAngle - liveConnectedAngle, 45);
                            hingeJoint2D.limits = limits;
                            changed = true;
                        }
                    }
                }
            }
        }

        return changed;
    }


    private void DrawDiscs(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        Vector2 center = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

        HingeJoint2DSettings settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

        float scale = settings.showRadiusHandles ? editorSettings.orbitRangeScale : editorSettings.anchorScale;
        float handleSize = HandleUtility.GetHandleSize(center)*scale;
        float distance = HandleUtility.DistanceToCircle(center, handleSize*.5f);
        bool inZone = distance <= AnchorEpsilon;

        Vector2 mainBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);
        using (new HandleColor(editorSettings.mainDiscColor)) {
            if (Vector2.Distance(mainBodyPosition, center) > AnchorEpsilon) {
                Handles.DrawLine(mainBodyPosition, center);
            }
            else {
                float rot = JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Main);
                Handles.DrawLine(center, center + Helpers2D.GetDirection(rot)*handleSize);
            }
        }
        Vector2 connectedBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);
        if (hingeJoint2D.connectedBody) {
            using (new HandleColor(editorSettings.connectedDiscColor)) {
                if (Vector2.Distance(connectedBodyPosition, center) > AnchorEpsilon) {
                    Handles.DrawLine(connectedBodyPosition, center);
                }
                else {
                    float rot = JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Connected);
                    Handles.DrawLine(center, center + Helpers2D.GetDirection(rot)*handleSize);
                }
            }
        }
        else {
            using (new HandleColor(editorSettings.connectedDiscColor)) {
                Handles.DrawLine(center, center + Helpers2D.GetDirection(0)*handleSize);
            }
        }

        if (editorSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Always ||
            (editorSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Hover &&
             (anchorInfo.ignoreHover && (inZone || anchorInfo.IsActive())))) {
            using (new HandleColor(editorSettings.mainDiscColor)) {
                Handles.DrawWireDisc(center, Vector3.forward, Vector2.Distance(center, mainBodyPosition));
            }

            if (hingeJoint2D.connectedBody) {
                using (new HandleColor(editorSettings.connectedDiscColor)) {
                    Handles.DrawWireDisc(center, Vector3.forward,
                        Vector2.Distance(center,
                            connectedBodyPosition));
                }
            }
        }
        HandleUtility.Repaint();
    }

    private static bool RadiusGUI(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        HingeJoint2DSettings settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);
        if (!settings.showRadiusHandles) {
            return false;
        }

        Vector3 center = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

        List<Transform> transforms = new List<Transform>();
        List<Transform> rightTransforms = new List<Transform>();
        switch (bias) {
            case JointHelpers.AnchorBias.Connected:
                if (hingeJoint2D.connectedBody) {
                    transforms.Add(hingeJoint2D.connectedBody.transform);
                    rightTransforms.Add(hingeJoint2D.transform);
                    if (Event.current.shift) {
                        transforms.Add(hingeJoint2D.transform);
                    }
                }
                else {
                    transforms.Add(hingeJoint2D.transform);
                }
                break;
            default:
                transforms.Add(hingeJoint2D.transform);
                if (hingeJoint2D.connectedBody) {
                    rightTransforms.Add(hingeJoint2D.connectedBody.transform);
                    if (Event.current.shift) {
                        transforms.Add(hingeJoint2D.connectedBody.transform);
                    }
                }
                break;
        }
        if (Event.current.shift) {
            rightTransforms = transforms;
        }

        EditorGUI.BeginChangeCheck();
        DrawRadiusHandle(anchorInfo.GetControlID("radius"), transforms, rightTransforms, center);

        return EditorGUI.EndChangeCheck();
    }


    private static void DrawRadiusHandle(int controlID, IEnumerable<Transform> transforms,
        IEnumerable<Transform> rightTransforms,
        Vector2 midPoint) {
        OrbitHandle(controlID,
            transforms,
            rightTransforms,
            midPoint,
            HandleUtility.GetHandleSize(midPoint)*editorSettings.anchorScale*0.5f,
            HandleUtility.GetHandleSize(midPoint)*editorSettings.orbitRangeScale*0.5f);
    }


    protected override void InspectorDisplayGUI(bool enabled) {
        List<Object> allSettings =
            targets.Cast<HingeJoint2D>()
                .Select(hingeJoint2D => SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D))
                .Where(hingeSettings => hingeSettings != null).Cast<Object>().ToList();

        SerializedObject serializedSettings = new SerializedObject(allSettings.ToArray());
        ToggleShowRadiusHandles(serializedSettings, enabled);
        ToggleShowAngleLimits(serializedSettings, enabled);
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


    private static readonly GUIContent RadiusHandlesContent =
        new GUIContent("Radius Handles", "Toggles the display of radius handles on the scene GUI.");

    private void ToggleShowRadiusHandles(SerializedObject serializedSettings, bool enabled) {
        EditorGUI.BeginChangeCheck();
        bool value;


        using (new GUIEnabled(enabled)) {
            SerializedProperty showRadiusHandles = serializedSettings.FindProperty("showRadiusHandles");
            EditorGUILayout.PropertyField(showRadiusHandles, RadiusHandlesContent);
            value = showRadiusHandles.boolValue;
        }

        if (EditorGUI.EndChangeCheck()) {
            foreach (HingeJoint2D hingeJoint2D in targets) {
                HingeJoint2DSettings hingeSettings =
                    SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

                EditorHelpers.RecordUndo("toggle radius handle display", hingeSettings);
                hingeSettings.showRadiusHandles = value;
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
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 6),
                    delegate(Action close, bool focused)
                    {
                        EditorGUILayout.LabelField(new GUIContent("Angle Limits", "The joint angle limits"));
                        using (new Indent())
                        {
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

                            if (EditorGUI.EndChangeCheck())
                            {
                                using (new Modification("Configure Limits", hingeJoint2D))
                                {
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
                             focused))
                        {
                            close();
                        }
                    }));
        }
    }
}