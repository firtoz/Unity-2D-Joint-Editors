using System;
using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
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

    protected override bool WantsOffset() {
        return true;
    }


    private readonly static HashSet<string> Names = new HashSet<String>  {
            "radius", "lowerMainAngle",
            "upperMainAngle", "lowerConnectedAngle", "upperConnectedAngle"
        };

    protected override HashSet<String> GetControlNames() {
        return Names;
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        HingeJoint2D hingeJoint2D = joint2D as HingeJoint2D;

        bool changed = !anchorInfo.ignoreHover && RadiusGUI(hingeJoint2D, anchorInfo, bias);

        changed = DrawAngleLimits(hingeJoint2D, anchorInfo, bias) || changed;

        DrawDiscs(hingeJoint2D, anchorInfo, bias);

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(hingeJoint2D);
        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(hingeJoint2D);
        if (Vector2.Distance(mainAnchorPosition, connectedAnchorPosition) > AnchorEpsilon) {
            using (new HandleColor(Color.green)) {
                Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
            }
        }
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

            float angleHandleSize = editorSettings.angleHandleSize;
            HingeJoint2DSettings.AnchorPriority anchorPriority = settings.anchorPriority;

            bool showMain = anchorPriority == HingeJoint2DSettings.AnchorPriority.Main ||
                            anchorPriority == HingeJoint2DSettings.AnchorPriority.Both;

            bool showConnected = (anchorPriority == HingeJoint2DSettings.AnchorPriority.Connected ||
                                  anchorPriority == HingeJoint2DSettings.AnchorPriority.Both);

            Vector2 anchorPosition = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

            float handleSize = HandleUtility.GetHandleSize(anchorPosition)*editorSettings.orbitRangeScale*.5f;
            float distanceFromCenter = (handleSize + (angleHandleSize*HandleUtility.GetHandleSize(anchorPosition)/64));

            float jointAngle = EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying
                ? hingeJoint2D.jointAngle
                : 0;

            Vector2 mainBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);

            float mainBodyAngle = JointHelpers.AngleFromAnchor(anchorPosition, mainBodyPosition,
                JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Main));

            Vector2 connectedBodyPosition = GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);

            float connectedBodyAngle = hingeJoint2D.connectedBody
                ? JointHelpers.AngleFromAnchor(anchorPosition, connectedBodyPosition,
                    JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Connected))
                : 0;

            float angleDiff = jointAngle - (connectedBodyAngle - mainBodyAngle);

            float liveMainAngle = connectedBodyAngle + angleDiff;

            float minMainAngle = liveMainAngle - minLimit;
            float maxMainAngle = liveMainAngle - maxLimit;

            if (showMain && bias != JointHelpers.AnchorBias.Connected) {
                using (new HandleColor(editorSettings.angleAreaColor)) {
                    Handles.DrawSolidArc(anchorPosition, Vector3.forward,
                        Helpers2D.Rotated2DVector(maxMainAngle),
                        maxLimit - minLimit, distanceFromCenter);
                }
                using (new HandleColor(editorSettings.angleLimitColor)) {
                    Vector3 minMainEnd = anchorPosition +
                                         Helpers2D.Rotated2DVector(minMainAngle)*distanceFromCenter;
                    Handles.DrawLine(anchorPosition, minMainEnd);

                    Vector3 maxMainEnd = anchorPosition +
                                         Helpers2D.Rotated2DVector(maxMainAngle)*distanceFromCenter;
                    Handles.DrawLine(anchorPosition, maxMainEnd);

                    if ((minLimit < 0 && maxLimit < 0) || (minLimit > 0 && maxLimit > 0)) {
                        using (new HandleColor(Color.red)) {
                            Handles.DrawWireArc(anchorPosition, Vector3.forward,
                                Helpers2D.Rotated2DVector(maxMainAngle),
                                maxLimit - minLimit, distanceFromCenter);
                        }
                    }
                    else {
                        Handles.DrawWireArc(anchorPosition, Vector3.forward, Helpers2D.Rotated2DVector(maxMainAngle),
                            maxLimit - minLimit, distanceFromCenter);
                    }

                    EditorGUI.BeginChangeCheck();
                    using (HandleDrawerBase drawer = new HandleCircleDrawer(Color.white, Color.black)) {
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
                    using (HandleDrawerBase drawer = new HandleCircleDrawer(Color.white, Color.black)) {
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
                        Handles.DrawSolidArc(anchorPosition, Vector3.forward,
                            Helpers2D.Rotated2DVector(minConnectedAngle),
                            maxLimit - minLimit, distanceFromCenter);
                    }
                }
                using (new HandleColor(editorSettings.angleLimitColor)) {
                    {
                        Vector3 minConnectedEnd = anchorPosition +
                                                  Helpers2D.Rotated2DVector(minConnectedAngle)*distanceFromCenter;
                        Handles.DrawLine(anchorPosition, minConnectedEnd);

                        Vector3 maxConnectedEnd = anchorPosition +
                                                  Helpers2D.Rotated2DVector(maxConnectedAngle)*distanceFromCenter;
                        Handles.DrawLine(anchorPosition, maxConnectedEnd);

                        if ((minLimit < 0 && maxLimit < 0) || (minLimit > 0 && maxLimit > 0)) {
                            using (new HandleColor(Color.red)) {
                                Handles.DrawWireArc(anchorPosition, Vector3.forward,
                                    Helpers2D.Rotated2DVector(minConnectedAngle),
                                    maxLimit - minLimit, distanceFromCenter);
                            }
                        }
                        else {
                            Handles.DrawWireArc(anchorPosition, Vector3.forward,
                                Helpers2D.Rotated2DVector(minConnectedAngle),
                                maxLimit - minLimit, distanceFromCenter);
                        }

                        EditorGUI.BeginChangeCheck();
                        using (HandleDrawerBase drawer = new HandleCircleDrawer(Color.white, Color.black)) {
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
                        using (HandleDrawerBase drawer = new HandleCircleDrawer(Color.white, Color.black)) {
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
                Handles.DrawLine(center, center + Helpers2D.Rotated2DVector(rot)*handleSize);
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
                    Handles.DrawLine(center, center + Helpers2D.Rotated2DVector(rot)*handleSize);
                }
            }
        }
        else {
            using (new HandleColor(editorSettings.connectedDiscColor)) {
                Handles.DrawLine(center, center + Helpers2D.Rotated2DVector(0)*handleSize);
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
        RadiusHandle(controlID,
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

    protected override void InspectorGUI(bool foldout) {
        if (WantsLocking()) {
            int grp = Undo.GetCurrentGroup();
            EditorGUI.BeginChangeCheck();

            /*SerializedProperty propertyIterator = serializedObject.GetIterator();
            do
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField(propertyIterator.propertyPath);
                EditorGUILayout.LabelField(propertyIterator.type);
                EditorGUILayout.EndHorizontal();
            } while (propertyIterator.Next(true));*/

            Vector2 originalAnchor = serializedObject.FindProperty("m_Anchor").vector2Value;
            Vector2 originalConnectedAnchor = serializedObject.FindProperty("m_ConnectedAnchor").vector2Value;
            Object connectedRigidBody = serializedObject.FindProperty("m_ConnectedRigidBody").objectReferenceValue;

            Dictionary<AnchoredJoint2D, Vector2> worldConnectedAnchors =
                targets.Cast<AnchoredJoint2D>()
                    .ToDictionary(joint2D => joint2D,
                        joint2D => JointHelpers.GetConnectedAnchorPosition(joint2D));


            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck()) {
                Vector2 curAnchor = serializedObject.FindProperty("m_Anchor").vector2Value;
                Vector2 curConnectedAnchor = serializedObject.FindProperty("m_ConnectedAnchor").vector2Value;

                bool mainAnchorChanged = Vector2.Distance(curAnchor, originalAnchor) > AnchorEpsilon;
                bool connectedAnchorChanged = Vector2.Distance(curConnectedAnchor, originalConnectedAnchor) >
                                              AnchorEpsilon;

                if (mainAnchorChanged || connectedAnchorChanged) {
                    JointHelpers.AnchorBias bias;

                    if (mainAnchorChanged) {
                        bias = connectedAnchorChanged
                            ? JointHelpers.AnchorBias.Either
                            : JointHelpers.AnchorBias.Main;
                    }
                    else {
                        bias = JointHelpers.AnchorBias.Connected;
                    }
                    foreach (HingeJoint2D hingeJoint2D in targets) {
                        HingeJoint2DSettings hingeSettings =
                            SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);
                        bool wantsLock = hingeSettings.lockAnchors;

                        if (wantsLock) {
                            EditorHelpers.RecordUndo("Inspector", hingeJoint2D);
                            ReAlignAnchors(hingeJoint2D, bias);
                            EditorUtility.SetDirty(hingeJoint2D);
                        }
                    }
                }

                if (connectedRigidBody != serializedObject.FindProperty("m_ConnectedRigidBody").objectReferenceValue) {
                    foreach (HingeJoint2D hingeJoint2D in targets) {
                        EditorHelpers.RecordUndo("Inspector", hingeJoint2D);
                        JointHelpers.SetWorldConnectedAnchorPosition(hingeJoint2D, worldConnectedAnchors[hingeJoint2D]);

                        EditorUtility.SetDirty(hingeJoint2D);
                    }
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                Undo.CollapseUndoOperations(grp);
                //Debug.Log("!!!");
                //hinge angle changed...
            }
        }
        else {
            DrawDefaultInspector();
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
            foreach (HingeJoint2D hingeJoint2D in targets) {
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
}