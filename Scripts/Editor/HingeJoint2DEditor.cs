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
public class HingeJoint2DEditor : JointEditor {
    private const float AnchorEpsilon = JointHelpers.AnchorEpsilon;

    public void OnEnable() {
        SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
    }

    public void OnDisable() {
// ReSharper disable DelegateSubtraction
        SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
// ReSharper restore DelegateSubtraction
    }


    private readonly Dictionary<HingeJoint2D, PositionInfo> positions = new Dictionary<HingeJoint2D, PositionInfo>();

    public void OnPreSceneGUI() {
        //gets called before gizmos!
        HingeJoint2D hingeJoint2D = target as HingeJoint2D;
        if (hingeJoint2D) {
            positions[hingeJoint2D] = new PositionInfo(hingeJoint2D);
        }
//        positions[target as HingeJoint2D] = new PositionInfo();
    }

    public void OnSceneGUIDelegate(SceneView sceneView) {
        //gets called after gizmos!


        foreach (HingeJoint2D hingeJoint2D in targets.Cast<HingeJoint2D>()) {
            if (hingeJoint2D == null || !hingeJoint2D.enabled) {
                continue;
            }
            PositionInfo.Change change = positions[hingeJoint2D].Changed(hingeJoint2D);
            var settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

            Vector2 main = JointHelpers.GetAnchorPosition(hingeJoint2D, JointHelpers.AnchorBias.Main);
            Vector2 connected = JointHelpers.GetAnchorPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected);
            if (settings.lockAnchors && Vector2.Distance(main, connected) > JointHelpers.AnchorEpsilon &&
                change != PositionInfo.Change.NoChange) {
                EditorHelpers.RecordUndo("Realign", hingeJoint2D);
                ReAlignAnchors(hingeJoint2D, JointHelpers.GetBias(change));
                EditorUtility.SetDirty(hingeJoint2D);
            }
        }
    }

    public bool HasFrameBounds() {
        HingeJoint2D hingeJoint2D = target as HingeJoint2D;
        if (hingeJoint2D == null || !hingeJoint2D.enabled) {
            return false;
        }
        return true;
    }

    public Bounds OnGetFrameBounds() {
        Bounds bounds = Selection.activeGameObject.renderer
            ? Selection.activeGameObject.renderer.bounds
            : new Bounds((Vector2) Selection.activeGameObject.transform.position, Vector2.zero);
        foreach (Transform selectedTransform in Selection.transforms) {
            bounds.Encapsulate((Vector2) selectedTransform.position);
        }

        foreach (HingeJoint2D hingeJoint2D in targets.Cast<HingeJoint2D>()) {
            Vector2 midPoint = (JointHelpers.GetAnchorPosition(hingeJoint2D) +
                                JointHelpers.GetConnectedAnchorPosition(hingeJoint2D))*.5f;
            float distance = Vector2.Distance(midPoint, GetTargetPositionWithOffset(hingeJoint2D, JointHelpers.AnchorBias.Main));
            if (hingeJoint2D.connectedBody) {
                float connectedDistance = Vector2.Distance(midPoint, GetTargetPositionWithOffset(hingeJoint2D, JointHelpers.AnchorBias.Connected));
                distance = Mathf.Max(distance, connectedDistance);
            }
            Bounds hingeBounds = new Bounds(midPoint, Vector2.one*distance*0.5f);
            bounds.Encapsulate(hingeBounds);
        }

        return bounds;
    }

    public void OnSceneGUI() {
        HingeJoint2D hingeJoint2D = target as HingeJoint2D;
        if (hingeJoint2D == null || !hingeJoint2D.enabled) {
            return;
        }
        HingeJoint2DSettings settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);
        if (settings && !settings.showJointGizmos) {
            return;
        }


        List<Vector2> otherAnchors = new List<Vector2>();
        foreach (HingeJoint2D otherHingeObject in Selection.GetFiltered(typeof (HingeJoint2D), SelectionMode.Deep)) {
            foreach (HingeJoint2D otherHingeJoint in otherHingeObject.GetComponents<HingeJoint2D>()) {
                if (otherHingeJoint == hingeJoint2D) {
                    continue;
                }

                Vector2 otherWorldAnchor = Helpers.Transform2DPoint(otherHingeJoint.transform,
                    otherHingeJoint.anchor);
                Vector2 otherConnectedWorldAnchor = otherHingeJoint.connectedBody
                    ? Helpers.Transform2DPoint(
                        otherHingeJoint
                            .connectedBody
                            .transform,
                        otherHingeJoint
                            .connectedAnchor)
                    : otherHingeJoint.connectedAnchor;

                otherAnchors.Add(otherWorldAnchor);
                otherAnchors.Add(otherConnectedWorldAnchor);
            }
        }

        AnchorGUI(hingeJoint2D, otherAnchors);
    }

    private class AnchorInfo {
        public readonly int sliderID = GUIUtility.GetControlID(FocusType.Passive);
        public readonly int lockID = GUIUtility.GetControlID(FocusType.Passive);
        public readonly int radiusID = GUIUtility.GetControlID(FocusType.Passive);
        public readonly int mainOffsetID = GUIUtility.GetControlID(FocusType.Passive);
        public readonly int connectedOffsetID = GUIUtility.GetControlID(FocusType.Passive);
        public readonly int lowerMainAngleID = GUIUtility.GetControlID(FocusType.Passive);
        public readonly int upperMainAngleID = GUIUtility.GetControlID(FocusType.Passive);
        public readonly int lowerConnectedAngleID = GUIUtility.GetControlID(FocusType.Passive);
        public readonly int upperConnectedAngleID = GUIUtility.GetControlID(FocusType.Passive);
        public bool showRadius = true;

        public bool IsActive() {
            int hotControl = GUIUtility.hotControl;

            return hotControl == radiusID || hotControl == sliderID || hotControl == lockID;
        }
    }

    private void AnchorGUI(HingeJoint2D hingeJoint2D, List<Vector2> otherAnchors) {
        HingeJoint2DSettings hingeSettings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

        bool anchorLock = hingeSettings.lockAnchors;

        bool playing = EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused;
        if (playing) {
//            anchorLock = false;
        }

        Vector2 worldAnchor = JointHelpers.GetMainAnchorPosition(hingeJoint2D);
        Vector2 worldConnectedAnchor = JointHelpers.GetConnectedAnchorPosition(hingeJoint2D);

        bool overlapping = Vector2.Distance(worldConnectedAnchor, worldAnchor) <= AnchorEpsilon;

        bool changed = false;

        AnchorInfo main = new AnchorInfo(),
            connected = new AnchorInfo(),
            locked = new AnchorInfo();


        {
            Vector2 localOffset = hingeSettings.GetOffset(JointHelpers.AnchorBias.Main);
            Transform transform = JointHelpers.GetTargetTransform(hingeJoint2D, JointHelpers.AnchorBias.Main);
            Vector2 worldOffset = Helpers.Transform2DPoint(transform, localOffset);

            EditorGUI.BeginChangeCheck();
                worldOffset = Handles.Slider2D(main.mainOffsetID,
                    worldOffset, Vector3.forward, Vector3.up, Vector3.right,
                    HandleUtility.GetHandleSize(worldOffset) * 0.25f,
                    Handles.SphereCap, Vector2.zero);
            if (EditorGUI.EndChangeCheck()) {
                EditorHelpers.RecordUndo("Change Main Offset", hingeSettings);
                hingeSettings.SetOffset(JointHelpers.AnchorBias.Main, Helpers.InverseTransform2DPoint(transform, worldOffset));
                EditorUtility.SetDirty(hingeSettings);
            } 
        }
        {
            Vector2 localOffset = hingeSettings.GetOffset(JointHelpers.AnchorBias.Connected);
            Transform transform = JointHelpers.GetTargetTransform(hingeJoint2D, JointHelpers.AnchorBias.Connected);
            if (transform != null) {
                Vector2 worldOffset = Helpers.Transform2DPoint(transform, localOffset);

                EditorGUI.BeginChangeCheck();
                worldOffset = Handles.Slider2D(main.connectedOffsetID,
                    worldOffset, Vector3.forward, Vector3.up, Vector3.right,
                    HandleUtility.GetHandleSize(worldOffset)*0.25f,
                    Handles.SphereCap, Vector2.zero);
                if (EditorGUI.EndChangeCheck()) {
                    EditorHelpers.RecordUndo("Change Connected Offset", hingeSettings);
                    hingeSettings.SetOffset(JointHelpers.AnchorBias.Connected,
                        Helpers.InverseTransform2DPoint(transform, worldOffset));
                    EditorUtility.SetDirty(hingeSettings);
                }
            }
        }

        if (anchorLock) {
            if (playing || overlapping) {
                if (SingleAnchorGUI(hingeJoint2D, locked, otherAnchors, JointHelpers.AnchorBias.Either)) {
                    changed = true;
                }
            }
            else {
                //draw the locks instead, force them to show
                if (ToggleLockButton(main.lockID, hingeJoint2D, JointHelpers.AnchorBias.Main)) {
                    changed = true;
                }
                if (ToggleLockButton(connected.lockID, hingeJoint2D, JointHelpers.AnchorBias.Connected)) {
                    changed = true;
                }
            }
        }
        else {
            if (SingleAnchorGUI(hingeJoint2D, connected, otherAnchors, JointHelpers.AnchorBias.Connected)) {
                changed = true;
            }

            float handleSize = HandleUtility.GetHandleSize(worldConnectedAnchor)*editorSettings.orbitRangeScale;
            float distance = HandleUtility.DistanceToCircle(worldConnectedAnchor, handleSize*.5f);
            bool hovering = distance <= AnchorEpsilon;
            if (hovering) {
                connected.showRadius = false;
            }

            if (SingleAnchorGUI(hingeJoint2D, main, otherAnchors, JointHelpers.AnchorBias.Main)) {
                changed = true;
            }
        }

        if (changed) {
            EditorUtility.SetDirty(hingeJoint2D);
        }
    }

    private static bool SingleAnchorGUI(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo,
        IEnumerable<Vector2> otherAnchors, JointHelpers.AnchorBias bias) {
        int lockID = anchorInfo.lockID;

        bool changed = false;
        if (Event.current.shift) {
            if (bias == JointHelpers.AnchorBias.Either) {
                //locked! show unlock
                if (ToggleUnlockButton(lockID, hingeJoint2D, bias)) {
                    changed = true;
                }
            }
            else {
                if (ToggleLockButton(lockID, hingeJoint2D, bias)) {
                    changed = true;
                }
            }
        }
        else {
            if (SliderGUI(hingeJoint2D, anchorInfo, otherAnchors, bias)) {
                changed = true;
            }
        }

        if (anchorInfo.showRadius && RadiusGUI(hingeJoint2D, anchorInfo, bias)) {
            changed = true;
        }

        if (DrawAngleLimits(hingeJoint2D, anchorInfo, bias)) {
            changed = true;
        }
        DrawDiscs(hingeJoint2D, anchorInfo, bias);


        return changed;
    }


    private static bool DrawAngleLimits(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
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

            Vector2 mainBodyPosition = GetTargetPositionWithOffset(hingeJoint2D, JointHelpers.AnchorBias.Main);

            float mainBodyAngle = JointHelpers.AngleFromAnchor(anchorPosition, mainBodyPosition,
                JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Main));

            Vector2 connectedBodyPosition = GetTargetPositionWithOffset(hingeJoint2D, JointHelpers.AnchorBias.Connected);

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
                        Helpers.Rotated2DVector(maxMainAngle),
                        maxLimit - minLimit, distanceFromCenter);
                }
                using (new HandleColor(editorSettings.angleLimitColor)) {
                    Vector3 minMainEnd = anchorPosition +
                                         Helpers.Rotated2DVector(minMainAngle)*distanceFromCenter;
                    Handles.DrawLine(anchorPosition, minMainEnd);

                    Vector3 maxMainEnd = anchorPosition +
                                         Helpers.Rotated2DVector(maxMainAngle)*distanceFromCenter;
                    Handles.DrawLine(anchorPosition, maxMainEnd);

                    if ((minLimit < 0 && maxLimit < 0) || (minLimit > 0 && maxLimit > 0)) {
                        using (new HandleColor(Color.red)) {
                            Handles.DrawWireArc(anchorPosition, Vector3.forward,
                                Helpers.Rotated2DVector(maxMainAngle),
                                maxLimit - minLimit, distanceFromCenter);
                        }
                    }
                    else {
                        Handles.DrawWireArc(anchorPosition, Vector3.forward, Helpers.Rotated2DVector(maxMainAngle),
                            maxLimit - minLimit, distanceFromCenter);
                    }

                    EditorGUI.BeginChangeCheck();
                    using (HandleDrawerBase drawer = new HandleCircleDrawer(Color.white, Color.black)) {
                        minMainAngle = EditorHelpers.AngleSlider(anchorInfo.lowerMainAngleID, drawer, anchorPosition,
                            minMainAngle,
                            distanceFromCenter, angleHandleSize*HandleUtility.GetHandleSize(minMainEnd)/64);
                        maxMainAngle = EditorHelpers.AngleSlider(anchorInfo.upperMainAngleID, drawer, anchorPosition,
                            maxMainAngle,
                            distanceFromCenter, angleHandleSize * HandleUtility.GetHandleSize(maxMainEnd) / 64);
                    }

                    if (EditorGUI.EndChangeCheck()) {
                        EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                        limits.min = Handles.SnapValue(liveMainAngle - minMainAngle,45);
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
                            Helpers.Rotated2DVector(minConnectedAngle),
                            maxLimit - minLimit, distanceFromCenter);
                    }
                }
                using (new HandleColor(editorSettings.angleLimitColor)) {
                    {
                        Vector3 minConnectedEnd = anchorPosition +
                                                  Helpers.Rotated2DVector(minConnectedAngle)*distanceFromCenter;
                        Handles.DrawLine(anchorPosition, minConnectedEnd);

                        Vector3 maxConnectedEnd = anchorPosition +
                                                  Helpers.Rotated2DVector(maxConnectedAngle)*distanceFromCenter;
                        Handles.DrawLine(anchorPosition, maxConnectedEnd);

                        if ((minLimit < 0 && maxLimit < 0) || (minLimit > 0 && maxLimit > 0)) {
                            using (new HandleColor(Color.red)) {
                                Handles.DrawWireArc(anchorPosition, Vector3.forward,
                                    Helpers.Rotated2DVector(minConnectedAngle),
                                    maxLimit - minLimit, distanceFromCenter);
                            }
                        }
                        else {
                            Handles.DrawWireArc(anchorPosition, Vector3.forward,
                                Helpers.Rotated2DVector(minConnectedAngle),
                                maxLimit - minLimit, distanceFromCenter);
                        }

                        EditorGUI.BeginChangeCheck();
                        using (
                            HandleDrawerBase drawer = new HandleCircleDrawer(Color.white, Color.black)
                            ) {
                            minConnectedAngle = EditorHelpers.AngleSlider(anchorInfo.lowerConnectedAngleID, drawer,
                                anchorPosition,
                                minConnectedAngle,
                                distanceFromCenter, angleHandleSize * HandleUtility.GetHandleSize(minConnectedEnd) / 64);
                            maxConnectedAngle = EditorHelpers.AngleSlider(anchorInfo.upperConnectedAngleID, drawer,
                                anchorPosition,
                                maxConnectedAngle,
                                distanceFromCenter, angleHandleSize * HandleUtility.GetHandleSize(maxConnectedEnd) / 64);
                        }

                        if (EditorGUI.EndChangeCheck()) {
                            EditorHelpers.RecordUndo("Change Angle Limits", hingeJoint2D);
                            limits.min = Handles.SnapValue(minConnectedAngle - liveConnectedAngle, 45);
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

    private static Vector2 GetTargetPositionWithOffset(HingeJoint2D hingeJoint2D, JointHelpers.AnchorBias bias) {
        Transform transform = JointHelpers.GetTargetTransform(hingeJoint2D, bias);
        Vector2 offset = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D).GetOffset(bias);

        Vector2 worldOffset = offset;
        if (transform != null) {
            worldOffset = Helpers.Transform2DVector(transform, worldOffset);
        }

        return JointHelpers.GetTargetPosition(hingeJoint2D, bias) + worldOffset;
    }

    private static void DrawDiscs(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        Vector2 center = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

        HingeJoint2DSettings settings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

        float scale = settings.showRadiusHandles ? editorSettings.orbitRangeScale : editorSettings.anchorScale;
        float handleSize = HandleUtility.GetHandleSize(center)*scale;
        float distance = HandleUtility.DistanceToCircle(center, handleSize*.5f);
        bool inZone = distance <= AnchorEpsilon;

        Vector2 mainBodyPosition = GetTargetPositionWithOffset(hingeJoint2D, JointHelpers.AnchorBias.Main);
        using (new HandleColor(editorSettings.mainDiscColor)) {
            if (Vector2.Distance(mainBodyPosition, center) > AnchorEpsilon) {
                Handles.DrawLine(mainBodyPosition, center);
            }
            else {
                float rot = JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Main);
                Handles.DrawLine(center, center + Helpers.Rotated2DVector(rot) * handleSize);
            }
        }
        Vector2 connectedBodyPosition = GetTargetPositionWithOffset(hingeJoint2D, JointHelpers.AnchorBias.Connected);
        if (hingeJoint2D.connectedBody) {
            using (new HandleColor(editorSettings.connectedDiscColor)) {
                if (Vector2.Distance(connectedBodyPosition, center) > AnchorEpsilon) {
                    Handles.DrawLine(connectedBodyPosition, center);
                }
                else {
                    float rot = JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Connected);
                    Handles.DrawLine(center, center + Helpers.Rotated2DVector(rot) * handleSize);
                }
            }
        }
        else {
            using (new HandleColor(editorSettings.connectedDiscColor)) {
                Handles.DrawLine(center, center + Helpers.Rotated2DVector(0) * handleSize);
            }
        }

        if (editorSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Always ||
            (editorSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Hover &&
             (anchorInfo.showRadius && (inZone || anchorInfo.IsActive())))) {
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
        DrawRadiusHandle(anchorInfo.radiusID, transforms, rightTransforms, center);

        return EditorGUI.EndChangeCheck();
    }

    private static bool SliderGUI(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, IEnumerable<Vector2> otherAnchors,
        JointHelpers.AnchorBias bias) {
        int sliderID = anchorInfo.sliderID;
        List<Vector2> snapPositions = new List<Vector2> {
            GetTargetPositionWithOffset(hingeJoint2D, JointHelpers.AnchorBias.Main),
            JointHelpers.GetTargetTransform(hingeJoint2D, JointHelpers.AnchorBias.Main).position
        };

        if (hingeJoint2D.connectedBody) {
            snapPositions.Add(GetTargetPositionWithOffset(hingeJoint2D, JointHelpers.AnchorBias.Connected));
            snapPositions.Add(JointHelpers.GetTargetTransform(hingeJoint2D, JointHelpers.AnchorBias.Connected).position);
        }

        switch (bias) {
            case JointHelpers.AnchorBias.Main:
                snapPositions.Add(JointHelpers.GetAnchorPosition(hingeJoint2D,
                    JointHelpers.AnchorBias.Connected));
                break;
            case JointHelpers.AnchorBias.Connected:
                snapPositions.Add(JointHelpers.GetAnchorPosition(hingeJoint2D, JointHelpers.AnchorBias.Main));
                break;
        }

        snapPositions.AddRange(otherAnchors);

        EditorGUI.BeginChangeCheck();
        Vector2 position = AnchorSlider(sliderID, editorSettings.anchorScale, snapPositions, bias, hingeJoint2D);

        bool changed = false;
        if (EditorGUI.EndChangeCheck()) {
            EditorHelpers.RecordUndo("Anchor Move", hingeJoint2D);
            changed = true;

            JointHelpers.SetAnchorPosition(hingeJoint2D, position, bias);
        }
        return changed;
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


    private static bool ToggleLockButton(int controlID, HingeJoint2D hingeJoint2D, JointHelpers.AnchorBias bias) {
        Vector3 center = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

        bool lockPressed = EditorHelpers.CustomHandleButton(controlID,
            center,
            HandleUtility.GetHandleSize(center)*editorSettings.lockButtonScale,
            editorSettings.unlockButtonTexture, editorSettings.lockButtonTexture);

        if (lockPressed) {
            HingeJoint2DSettings hingeSettings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

            EditorHelpers.RecordUndo("Lock Anchors", hingeSettings, hingeJoint2D);
            hingeSettings.lockAnchors = true;
            EditorUtility.SetDirty(hingeSettings);

            ReAlignAnchors(hingeJoint2D, bias);
        }

        return lockPressed;
    }

    private static bool ToggleUnlockButton(int controlID, HingeJoint2D hingeJoint2D, JointHelpers.AnchorBias bias) {
        Vector3 center = JointHelpers.GetAnchorPosition(hingeJoint2D, bias);

        bool lockPressed = EditorHelpers.CustomHandleButton(controlID,
            center,
            HandleUtility.GetHandleSize(center)*editorSettings.lockButtonScale,
            editorSettings.lockButtonTexture, editorSettings.unlockButtonTexture);

        if (lockPressed) {
            HingeJoint2DSettings hingeSettings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

            EditorHelpers.RecordUndo("Unlock Anchors", hingeSettings);
            hingeSettings.lockAnchors = false;
            EditorUtility.SetDirty(hingeSettings);
        }

        return lockPressed;
    }

    protected override void InspectorGUI(bool foldout) {
        int grp = Undo.GetCurrentGroup();
        EditorGUI.BeginChangeCheck();
        if (foldout) {
            using (new Indent()) {
                List<Object> allSettings =
                    targets.Cast<HingeJoint2D>()
                        .Select(hingeJoint2D => SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D))
                        .Where(hingeSettings => hingeSettings != null).Cast<Object>().ToList();

                SerializedObject serializedSettings = new SerializedObject(allSettings.ToArray());
                SerializedProperty showJointGizmos = serializedSettings.FindProperty("showJointGizmos");

                bool enabled = GUI.enabled &&
                               (showJointGizmos.boolValue || showJointGizmos.hasMultipleDifferentValues);

                using (new Indent()) {
                    ToggleShowRadiusHandles(serializedSettings, enabled);
                    ToggleShowAngleLimits(serializedSettings, enabled);
                    using (new Indent())
                    {
                        SerializedProperty showAngleLimits = serializedSettings.FindProperty("showAngleLimits");
                        SelectAngleLimitsMode(serializedSettings, enabled && (showAngleLimits.boolValue || showAngleLimits.hasMultipleDifferentValues));
                    }
                }
                EditorGUILayout.LabelField("Features:");
                using (new Indent()) {
                    ToggleAnchorLock(serializedSettings);
                    AlterOffsets(serializedSettings, enabled);
                }
            }
        }


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

        Dictionary<HingeJoint2D, Vector2> worldConnectedAnchors =
            targets.Cast<HingeJoint2D>()
                .ToDictionary(hingeJoint2D => hingeJoint2D,
                    hingeJoint2D => JointHelpers.GetConnectedAnchorPosition(hingeJoint2D));


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
                    HingeJoint2DSettings hingeSettings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);
                    bool wantsLock = hingeSettings != null && hingeSettings.lockAnchors;

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

    private static readonly GUIContent MainOffsetContent = new GUIContent("Main Offset",
        "This offset is used to display the current angle of the object that owns the joint.");
    private static readonly GUIContent ConnectedOffsetContent = new GUIContent("Connected Offset",
        "This offset is used to display the current angle of the object that is connected by joint.");

    private void AlterOffsets(SerializedObject serializedSettings, bool enabled) {
        EditorGUI.BeginChangeCheck();

        using (new GUIEnabled(enabled))
        {
            SerializedProperty mainBodyOffset = serializedSettings.FindProperty("mainBodyOffset");
            EditorGUILayout.PropertyField(mainBodyOffset, MainOffsetContent);

            SerializedProperty connectedBodyOffset = serializedSettings.FindProperty("connectedBodyOffset");
            EditorGUILayout.PropertyField(connectedBodyOffset, ConnectedOffsetContent);
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedSettings.ApplyModifiedProperties();
        }
    }

    private static readonly GUIContent AnchorLockContent =
        new GUIContent("Lock Anchors",
            "Toggles anchor locking, which helps you keep the main and connected anchors of the joint properly aligned.");

    private void ToggleAnchorLock(SerializedObject serializedSettings) {
        EditorGUI.BeginChangeCheck();

        SerializedProperty lockAnchors = serializedSettings.FindProperty("lockAnchors");
        EditorGUILayout.PropertyField(lockAnchors, AnchorLockContent);
        bool value = lockAnchors.boolValue;

        if (EditorGUI.EndChangeCheck()) {
            bool wantsContinue = true;
            int choice = 1;

            if (value) {
                bool farAway = targets.Cast<HingeJoint2D>().Any(hingeJoint2D =>
                    Vector2.Distance(
                        JointHelpers.GetMainAnchorPosition(hingeJoint2D),
                        JointHelpers.GetConnectedAnchorPosition(hingeJoint2D)
                        ) > AnchorEpsilon);
                if (farAway) {
                    choice = EditorUtility.DisplayDialogComplex("Enable Anchor Lock",
                        "Which anchor would you like to lock to?",
                        "Main",
                        "Connected",
                        "Cancel");

                    if (choice == 2) //cancel
                    {
                        wantsContinue = false;
                    }
                }
            }
            if (wantsContinue) {
                foreach (HingeJoint2D hingeJoint2D in targets) {
                    HingeJoint2DSettings hingeSettings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(hingeJoint2D);

                    EditorHelpers.RecordUndo("toggle anchor locking", hingeSettings);
                    hingeSettings.lockAnchors = value;
                    EditorUtility.SetDirty(hingeSettings);

                    if (value) {
                        JointHelpers.AnchorBias bias = choice == 0
                            ? JointHelpers.AnchorBias.Main
                            : JointHelpers.AnchorBias.Connected;

                        EditorHelpers.RecordUndo("toggle anchor locking", hingeJoint2D);
                        ReAlignAnchors(hingeJoint2D, bias);
                        EditorUtility.SetDirty(hingeJoint2D);
                    }
                }
            }
        }
    }

    private static void ReAlignAnchors(HingeJoint2D hingeJoint2D,
        JointHelpers.AnchorBias bias = JointHelpers.AnchorBias.Either) {
        Transform transform = hingeJoint2D.transform;

        Vector2 connectedAnchor = hingeJoint2D.connectedAnchor;
        Vector2 worldAnchor = Helpers.Transform2DPoint(transform, hingeJoint2D.anchor);

        if (hingeJoint2D.connectedBody) {
            Rigidbody2D connectedBody = hingeJoint2D.connectedBody;
            Transform connectedTransform = connectedBody.transform;

            if (bias != JointHelpers.AnchorBias.Main
                && (bias == JointHelpers.AnchorBias.Connected
                    || (!transform.rigidbody2D.isKinematic && connectedBody.isKinematic))) {
                //other body is static or there is a bias
                Vector2 worldConnectedAnchor = Helpers.Transform2DPoint(connectedTransform, connectedAnchor);
                hingeJoint2D.anchor = Helpers.InverseTransform2DPoint(transform, worldConnectedAnchor);
            }
            else if (bias == JointHelpers.AnchorBias.Main
                     || (transform.rigidbody2D.isKinematic && !connectedBody.isKinematic)) {
                //this body is static or there is a bias
                hingeJoint2D.connectedAnchor = Helpers.InverseTransform2DPoint(connectedTransform,
                    worldAnchor);
            }
            else {
                Vector2 midPoint = (Helpers.Transform2DPoint(connectedTransform, connectedAnchor) +
                                    worldAnchor)*.5f;
                hingeJoint2D.anchor = Helpers.InverseTransform2DPoint(transform, midPoint);
                hingeJoint2D.connectedAnchor = Helpers.InverseTransform2DPoint(connectedTransform, midPoint);
            }
        }
        else {
            if (bias == JointHelpers.AnchorBias.Main) {
                hingeJoint2D.connectedAnchor = worldAnchor;
            }
            else {
                hingeJoint2D.anchor = Helpers.InverseTransform2DPoint(transform, connectedAnchor);
            }
        }
    }
}