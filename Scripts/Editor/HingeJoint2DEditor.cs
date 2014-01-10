using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof (HingeJoint2D))]
[CanEditMultipleObjects]
public class HingeJoint2DEditor : JointEditor
{
    private const float AnchorEpsilon = JointEditorSettings.AnchorEpsilon;

    public void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
    }

//    private bool injectingUndo;
//
//    private UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications) {
//        foreach (UndoPropertyModification modification in modifications) {
//            PropertyModification propertyModification = modification.propertyModification;
//            Logger.Log(propertyModification.propertyPath, propertyModification.target,
//                       propertyModification.value, propertyModification.objectReference);
//        }
//        if (injectingUndo) {
//            injectingUndo = false;
//            Logger.Log(modifications);
//            return new UndoPropertyModification[0];
//        }
//        else {
//            List<UndoPropertyModification> newModifications = new List<UndoPropertyModification>(modifications);
//            foreach (HingeJoint2D hingeJoint2D in targets) {
//                for (int i = 0; i < modifications.Length; i++) {
//                    UndoPropertyModification modification = modifications[i];
//                    PropertyModification propertyModification = modification.propertyModification;
//
//                    bool jointChanged = false;
//                    if (propertyModification.target == hingeJoint2D.transform) {
//                        jointChanged = true;
//                    }
//                    else {
//                        Rigidbody2D connectedBody = hingeJoint2D.connectedBody;
//                        if (connectedBody && propertyModification.target == connectedBody.transform) {
//                            jointChanged = true;
//                        }
//                    }
//
//                    if (jointChanged) {
//                        HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.Get(hingeJoint2D);
//
//                        bool anchorLock = hingeSettings != null && hingeSettings.lockAnchors;
//                        if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused) {
//                            anchorLock = false;
//                        }
//
//                        PositionChange change;
//                        if (anchorLock &&
//                            (change = positionCache[hingeJoint2D].Changed(hingeJoint2D)) != PositionChange.NoChange) {
////                            injectingUndo = true;
////                            RecordUndo("...", hingeJoint2D);
////                            Debug.Log("test");
//
//
////
////                            Logger.Log("Joint", hingeJoint2D, "changed:");
////                            Logger.Log(propertyModification.propertyPath, propertyModification.target,
////                                       propertyModification.value, propertyModification.objectReference);
//
//                            ReAlignAnchors(hingeJoint2D, GetBias(change));
//                            positionCache[hingeJoint2D] = new PositionInfo(hingeJoint2D);
//
//                            newModifications.Add(new UndoPropertyModification {
//                                propertyModification = new PropertyModification {
//                                    target = hingeJoint2D,
//                                    propertyPath = "m_Anchor.x",
//                                    value = hingeJoint2D.anchor.x.ToString(CultureInfo.InvariantCulture)
//                                }
//                            });
//
//                            newModifications.Add(new UndoPropertyModification {
//                                propertyModification = new PropertyModification {
//                                    target = hingeJoint2D,
//                                    propertyPath = "m_Anchor.y",
//                                    value = hingeJoint2D.anchor.y.ToString(CultureInfo.InvariantCulture)
//                                }
//                            });
//
//                            newModifications.Add(new UndoPropertyModification {
//                                propertyModification = new PropertyModification {
//                                    target = hingeJoint2D,
//                                    propertyPath = "m_ConnectedAnchor.x",
//                                    value = hingeJoint2D.connectedAnchor.x.ToString(CultureInfo.InvariantCulture)
//                                }
//                            });
//
//                            newModifications.Add(new UndoPropertyModification {
//                                propertyModification = new PropertyModification {
//                                    target = hingeJoint2D,
//                                    propertyPath = "m_ConnectedAnchor.y",
//                                    value = hingeJoint2D.connectedAnchor.y.ToString(CultureInfo.InvariantCulture)
//                                }
//                            });
//
//                            positionCache[hingeJoint2D] = new PositionInfo(hingeJoint2D);
//
//                            EditorUtility.SetDirty(hingeJoint2D);
//                        }
//                    }
//                }
//            }
//
//            return newModifications.ToArray();
//        }
//    }

    public void OnDisable()
    {
// ReSharper disable DelegateSubtraction
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
// ReSharper restore DelegateSubtraction
    }

    private void OnUndoRedoPerformed()
    {
//        foreach (HingeJoint2D hingeJoint2D in targets) {
//            PositionInfo.Record(hingeJoint2D);

//        }
    }

    public void OnPreSceneGUI()
    {
//                if (Event.current.type == EventType.keyDown)
//                {
//                    if ((Event.current.character + "").ToLower().Equals("f") || Event.current.keyCode == KeyCode.F)
//                    {
//                        //frame hotkey pressed
//                        Event.current.Use();
//        
//                        Bounds bounds;
//                        if (Selection.activeGameObject.renderer)
//                        {
//                            bounds = Selection.activeGameObject.renderer.bounds;
//                            using (new DisposableHandleColor(Color.red))
//                            {
//                                Handles.RectangleCap(0, bounds.center, Quaternion.identity, bounds.size.magnitude*0.5f);
//                            }
//                        }
//                        else
//                        {
//                            bounds = new Bounds((Vector2) Selection.activeGameObject.transform.position, Vector2.zero);
//                        }
//                        foreach (Transform selectedTransform in Selection.transforms)
//                        {
//                            bounds.Encapsulate((Vector2) selectedTransform.position);
//                        }
//        //				using (new DisposableHandleColor(Color.green)) {
//        ////					Handles.RectangleCap(0, bounds.center, Quaternion.identity, bounds.size.magnitude * 0.5f);
//        //				}
//        
//                        Vector2 midPoint = (JointEditorHelpers.GetAnchorPosition(hingeJoint2D) +
//                                            JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D))*.5f;
//                        float distance = Vector2.Distance(midPoint, hingeJoint2D.transform.position);
//                        Bounds hingeBounds = new Bounds(midPoint, Vector2.one*distance*2);
//                        bounds.Encapsulate(hingeBounds);
//        
//                        using (new DisposableHandleColor(Color.blue))
//                        {
//                            Handles.RectangleCap(0, bounds.center, Quaternion.identity, bounds.size.magnitude*0.5f);
//                        }
//        
//                        SceneView.lastActiveSceneView.LookAt(bounds.center, Quaternion.identity, bounds.size.magnitude);
//                    }
//                }
    }

    public void OnSceneGUI()
    {
        HingeJoint2D hingeJoint2D = target as HingeJoint2D;
        if (hingeJoint2D == null)
        {
            return;
        }
        HingeJoint2DSettings settings = HingeJoint2DSettingsEditor.Get(hingeJoint2D);
        if (settings && !settings.showJointGizmos)
        {
            return;
        }


        List<Vector2> otherAnchors = new List<Vector2>();
        foreach (HingeJoint2D otherHingeObject in Selection.GetFiltered(typeof (HingeJoint2D), SelectionMode.Deep))
        {
            foreach (HingeJoint2D otherHingeJoint in otherHingeObject.GetComponents<HingeJoint2D>())
            {
                if (otherHingeJoint == hingeJoint2D)
                {
                    continue;
                }

                Vector2 otherWorldAnchor = JointEditorHelpers.Transform2DPoint(otherHingeJoint.transform,
                    otherHingeJoint.anchor);
                Vector2 otherConnectedWorldAnchor = otherHingeJoint.connectedBody
                    ? JointEditorHelpers.Transform2DPoint(
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

    private class AnchorInfo
    {
        public readonly int sliderID;
        public readonly int lockID;
        public readonly int radiusID;
        public bool showRadius;

        public bool IsActive()
        {
            int hotControl = GUIUtility.hotControl;

            return hotControl == radiusID || hotControl == sliderID || hotControl == lockID;
        }

        public AnchorInfo()
        {
            sliderID = GUIUtility.GetControlID(FocusType.Passive);
            lockID = GUIUtility.GetControlID(FocusType.Passive);
            radiusID = GUIUtility.GetControlID(FocusType.Passive);

            showRadius = true;
        }
    }

    private void AnchorGUI(HingeJoint2D hingeJoint2D, List<Vector2> otherAnchors)
    {
        HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.Get(hingeJoint2D);

        bool anchorLock = hingeSettings != null && hingeSettings.lockAnchors;

        if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused)
        {
            anchorLock = false;
        }

        Vector2 worldAnchor = JointEditorHelpers.GetMainAnchorPosition(hingeJoint2D);
        Vector2 worldConnectedAnchor = JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D);

        bool overlapping = Vector2.Distance(worldConnectedAnchor, worldAnchor) <= AnchorEpsilon;

        bool changed = false;

        AnchorInfo main = new AnchorInfo(),
            connected = new AnchorInfo(),
            locked = new AnchorInfo();

        if (anchorLock)
        {
            if (overlapping)
            {
                if (SingleAnchorGUI(hingeJoint2D, locked, otherAnchors, AnchorBias.Either))
                {
                    changed = true;
                }
            }
            else
            {
                //draw the locks instead, force them to show
                if (ToggleLockButton(main.lockID, hingeJoint2D, AnchorBias.Main))
                {
                    changed = true;
                }
                if (ToggleLockButton(connected.lockID, hingeJoint2D, AnchorBias.Connected))
                {
                    changed = true;
                }
            }
        }
        else
        {
            if (SingleAnchorGUI(hingeJoint2D, main, otherAnchors, AnchorBias.Main))
            {
                changed = true;
            }

            float mainHandleSize = HandleUtility.GetHandleSize(worldAnchor)*jointSettings.orbitRangeScale;
            float distanceFromMain = HandleUtility.DistanceToCircle(worldAnchor, mainHandleSize*.5f);
            bool hoveringOverMain = distanceFromMain <= AnchorEpsilon;
            if (hoveringOverMain)
            {
                connected.showRadius = false;
            }

            if (!overlapping)
            {
                if (SingleAnchorGUI(hingeJoint2D, connected, otherAnchors, AnchorBias.Connected))
                {
                    changed = true;
                }
            }
        }

//        DrawDiscs(hingeJoint2D, false);

        if (DrawAngleLimits(hingeJoint2D))
        {
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(hingeJoint2D);
        }
    }

    private static bool SingleAnchorGUI(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, IEnumerable<Vector2> otherAnchors, AnchorBias bias)
    {
        int lockID = anchorInfo.lockID;

        bool changed = false;
        if (Event.current.shift)
        {
            if (bias == AnchorBias.Either)
            {
                //locked! show unlock
                if (ToggleUnlockButton(lockID, hingeJoint2D, bias))
                {
                    changed = true;
                }
            }
            else
            {
                if (ToggleLockButton(lockID, hingeJoint2D, bias))
                {
                    changed = true;
                }
            }
        }
        else
        {
            if (SliderGUI(hingeJoint2D, anchorInfo, otherAnchors, bias))
            {
                changed = true;
            }
        }

        if (anchorInfo.showRadius && RadiusGUI(hingeJoint2D, anchorInfo, bias))
        {
            changed = true;
        }

        DiscGui(hingeJoint2D, anchorInfo, bias);

        return changed;
    }

    private static void DiscGui(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, AnchorBias bias)
    {
        Vector3 center = GetAnchorPosition(hingeJoint2D, bias);

        float handleSize = HandleUtility.GetHandleSize(center)*jointSettings.orbitRangeScale;
        float distance = HandleUtility.DistanceToCircle(center, handleSize*.5f);
        bool inZone = distance <= AnchorEpsilon;

        if (jointSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Always ||
            (jointSettings.ringDisplayMode == JointEditorSettings.RingDisplayMode.Hover &&
             (anchorInfo.showRadius && (inZone || anchorInfo.IsActive()))))
        {
            Vector3 bodyPosition = hingeJoint2D.transform.position;
            using (new DisposableHandleColor(jointSettings.mainDiscColor))
            {
                Handles.DrawLine(bodyPosition, center);
                Handles.DrawWireDisc(center, Vector3.forward, Vector2.Distance(center, bodyPosition));
            }

            if (hingeJoint2D.connectedBody)
            {
                using (new DisposableHandleColor(jointSettings.connectedDiscColor))
                {
                    Handles.DrawLine(hingeJoint2D.connectedBody.transform.position, center);
                    Handles.DrawWireDisc(center, Vector3.forward,
                        Vector2.Distance(center,
                            hingeJoint2D.connectedBody.transform.position));
                }
            }
        }
    }

    private static bool RadiusGUI(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, AnchorBias bias)
    {
        Vector3 center = GetAnchorPosition(hingeJoint2D, bias);

        List<Transform> transforms = new List<Transform>();
        List<Transform> rightTransforms = new List<Transform>();
        switch (bias)
        {
            case AnchorBias.Connected:
                if (hingeJoint2D.connectedBody)
                {
                    transforms.Add(hingeJoint2D.connectedBody.transform);
                    rightTransforms.Add(hingeJoint2D.transform);
                    if (Event.current.shift)
                    {
                        transforms.Add(hingeJoint2D.transform);
                    }
                }
                else
                {
                    transforms.Add(hingeJoint2D.transform);
                }
                break;
            default:
                transforms.Add(hingeJoint2D.transform);
                if (hingeJoint2D.connectedBody) {
                    rightTransforms.Add(hingeJoint2D.connectedBody.transform);
                    if (Event.current.shift)
                    {
                        transforms.Add(hingeJoint2D.connectedBody.transform);
                    }
                }
                break;
        }
        if (Event.current.shift)
        {
            rightTransforms = transforms;
        }

        EditorGUI.BeginChangeCheck();
        DrawRadiusHandle(anchorInfo.radiusID, transforms, rightTransforms, center);

        return EditorGUI.EndChangeCheck();
    }

    private static bool SliderGUI(HingeJoint2D hingeJoint2D, AnchorInfo anchorInfo, IEnumerable<Vector2> otherAnchors, AnchorBias bias)
    {
        int sliderID = anchorInfo.sliderID;
        List<Vector2> snapPositions = new List<Vector2> {hingeJoint2D.transform.position};

        if (hingeJoint2D.connectedBody)
        {
            snapPositions.Add(hingeJoint2D.connectedBody.transform.position);
        }

        switch (bias)
        {
            case AnchorBias.Main:
                snapPositions.Add(GetAnchorPosition(hingeJoint2D, AnchorBias.Connected));
                break;
            case AnchorBias.Connected:
                snapPositions.Add(GetAnchorPosition(hingeJoint2D, AnchorBias.Main));
                break;
        }

        snapPositions.AddRange(otherAnchors);

        Vector3 position = GetAnchorPosition(hingeJoint2D, bias);

        EditorGUI.BeginChangeCheck();
        position = AnchorSlider(sliderID, position, jointSettings.anchorScale, snapPositions, bias, hingeJoint2D);

        bool changed = false;
        if (EditorGUI.EndChangeCheck())
        {
            GUIHelpers.RecordUndo("Anchor Move", hingeJoint2D);
            changed = true;

            SetPosition(hingeJoint2D, position, bias);
        }
        return changed;
    }

    

    private static void DrawRadiusHandle(int controlID, IEnumerable<Transform> transforms, IEnumerable<Transform> rightTransforms,
        Vector2 midPoint)
    {
        RadiusHandle(controlID, 
            transforms,
            rightTransforms,
            midPoint,
            HandleUtility.GetHandleSize(midPoint)*jointSettings.anchorScale*0.5f,
            HandleUtility.GetHandleSize(midPoint)*jointSettings.orbitRangeScale*0.5f);
    }

    private bool DrawAngleLimits(HingeJoint2D hingeJoint2D)
    {
        bool changed = false;

        JointAngleLimits2D limits = hingeJoint2D.limits;
        float minLimit = limits.min;
        float maxLimit = limits.max;

        return changed;
    }

    

    private static bool ToggleLockButton(int controlID, HingeJoint2D hingeJoint2D, AnchorBias bias)
    {
        Vector3 center = GetAnchorPosition(hingeJoint2D, bias);

        bool lockPressed = GUIHelpers.CustomHandleButton(controlID,
            center,
            HandleUtility.GetHandleSize(center)*jointSettings.lockButtonScale,
            jointSettings.unlockButtonTexture, jointSettings.lockButtonTexture);

        if (lockPressed)
        {
            HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.GetOrCreate(hingeJoint2D);

            GUIHelpers.RecordUndo("Lock Anchors", hingeSettings, hingeJoint2D);
            hingeSettings.lockAnchors = true;
            EditorUtility.SetDirty(hingeSettings);

            ReAlignAnchors(hingeJoint2D, bias);
        }

        return lockPressed;
    }

    private static bool ToggleUnlockButton(int controlID, HingeJoint2D hingeJoint2D, AnchorBias bias)
    {
        Vector3 center = GetAnchorPosition(hingeJoint2D, bias);

        bool lockPressed = GUIHelpers.CustomHandleButton(controlID,
            center,
            HandleUtility.GetHandleSize(center)*jointSettings.lockButtonScale,
            jointSettings.lockButtonTexture, jointSettings.unlockButtonTexture);

        if (lockPressed)
        {
            HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.GetOrCreate(hingeJoint2D);

            GUIHelpers.RecordUndo("Unlock Anchors", hingeSettings);
            hingeSettings.lockAnchors = false;
            EditorUtility.SetDirty(hingeSettings);
        }

        return lockPressed;
    }


    private static Vector3 GetAnchorPosition(HingeJoint2D hingeJoint2D, AnchorBias bias)
    {
        switch (bias)
        {
            case AnchorBias.Connected:
                return JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D);
            default:
                return JointEditorHelpers.GetMainAnchorPosition(hingeJoint2D);
        }
    }

    private static void SetPosition(HingeJoint2D hingeJoint2D, Vector3 position, AnchorBias bias)
    {
        switch (bias)
        {
            case AnchorBias.Connected:
                JointEditorHelpers.SetWorldConnectedAnchorPosition(hingeJoint2D, position);
                break;
            case AnchorBias.Main:
                JointEditorHelpers.SetWorldAnchorPosition(hingeJoint2D, position);
                break;
            case AnchorBias.Either:
                JointEditorHelpers.SetWorldAnchorPosition(hingeJoint2D, position);
                JointEditorHelpers.SetWorldConnectedAnchorPosition(hingeJoint2D, position);
                break;
        }
    }

    public override void OnInspectorGUI()
    {
        int grp = Undo.GetCurrentGroup();

        EditorGUI.BeginChangeCheck();

        bool? lockAnchors = null;
        bool? showJointGizmos = null;
        bool valueDifferent = false;
        bool gizmoValueDifferent = false;

        foreach (HingeJoint2D hingeJoint2D in targets)
        {
            HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.Get(hingeJoint2D);
            bool wantsLock = hingeSettings != null && hingeSettings.lockAnchors;
            bool wantsGizmos = hingeSettings == null || hingeSettings.showJointGizmos;
            if (lockAnchors != null)
            {
                if (lockAnchors.Value != wantsLock)
                {
                    valueDifferent = true;
                }
            }
            else
            {
                lockAnchors = wantsLock;
            }
            if (showJointGizmos != null)
            {
                if (showJointGizmos.Value != wantsGizmos)
                {
                    gizmoValueDifferent = true;
                }
            }
            else
            {
                showJointGizmos = wantsGizmos;
            }
        }

        using (new DisposableEditorGUIMixedValue(gizmoValueDifferent))
        {
            bool enabled = true;
            if (showJointGizmos == null)
            {
                showJointGizmos = false;
                enabled = false;
            }
            EditorGUI.BeginChangeCheck();
            using (new DisposableGUIEnabled(enabled))
            {
                showJointGizmos = EditorGUILayout.Toggle("Show Gizmos", showJointGizmos.Value);
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (HingeJoint2D hingeJoint2D in targets)
                {
                    HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.GetOrCreate(hingeJoint2D);

                    GUIHelpers.RecordUndo("toggle gizmo display", hingeSettings);
                    hingeSettings.showJointGizmos = showJointGizmos.Value;
                    EditorUtility.SetDirty(hingeSettings);
                }
            }
        }
        using (new DisposableEditorGUIMixedValue(valueDifferent))
        {
            bool enabled = true;
            if (lockAnchors == null)
            {
                lockAnchors = false;
                enabled = false;
            }
            EditorGUI.BeginChangeCheck();
            using (new DisposableGUIEnabled(enabled))
            {
                lockAnchors = EditorGUILayout.Toggle("Lock Anchors", lockAnchors.Value);
            }

            if (EditorGUI.EndChangeCheck())
            {
                bool wantsContinue = true;
                int choice = 1;

                if (lockAnchors.Value)
                {
                    bool farAway = targets.Cast<HingeJoint2D>().Any(hingeJoint2D =>
                        Vector2.Distance(
                            JointEditorHelpers.GetMainAnchorPosition(hingeJoint2D),
                            JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D)
                            ) > AnchorEpsilon);
                    if (farAway)
                    {
                        choice = EditorUtility.DisplayDialogComplex("Enable Anchor Lock",
                            "Which anchor would you like to lock to?",
                            "Main",
                            "Connected",
                            "Cancel");

                        if (choice == 2)
                        {
                            wantsContinue = false;
                        }
                    }
                }
                if (wantsContinue)
                {
                    foreach (HingeJoint2D hingeJoint2D in targets)
                    {
                        HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.GetOrCreate(hingeJoint2D);

                        GUIHelpers.RecordUndo("toggle anchor locking", hingeSettings);
                        hingeSettings.lockAnchors = lockAnchors.Value;
                        EditorUtility.SetDirty(hingeSettings);

                        if (lockAnchors.Value)
                        {
                            AnchorBias bias = choice == 0 ? AnchorBias.Main : AnchorBias.Connected;

                            GUIHelpers.RecordUndo("toggle anchor locking", hingeJoint2D);
                            ReAlignAnchors(hingeJoint2D, bias);
                            EditorUtility.SetDirty(hingeJoint2D);
                        }
                    }
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

        /*SerializedProperty angleLimits = serializedObject.FindProperty("m_AngleLimits");
        float lowerAngle = angleLimits.FindPropertyRelative("m_LowerAngle").floatValue;
        float upperAngle = angleLimits.FindPropertyRelative("m_UpperAngle").floatValue;
        EditorGUI.BeginChangeCheck();
        lowerAngle = EditorGUILayout.FloatField("Lower Angle", lowerAngle);
        upperAngle = EditorGUILayout.FloatField("Upper Angle", upperAngle);
        if (EditorGUI.EndChangeCheck())
        {
            angleLimits.FindPropertyRelative("m_LowerAngle").floatValue = lowerAngle;
            angleLimits.FindPropertyRelative("m_UpperAngle").floatValue = upperAngle;
            serializedObject.ApplyModifiedProperties();
        }*/

        Dictionary<HingeJoint2D, Vector2> worldConnectedAnchors =
            targets.Cast<HingeJoint2D>()
                .ToDictionary(hingeJoint2D => hingeJoint2D,
                    hingeJoint2D => JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D));

        
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            Vector2 curAnchor = serializedObject.FindProperty("m_Anchor").vector2Value;
            Vector2 curConnectedAnchor = serializedObject.FindProperty("m_ConnectedAnchor").vector2Value;

            bool mainAnchorChanged = Vector2.Distance(curAnchor, originalAnchor) > AnchorEpsilon;
            bool connectedAnchorChanged = Vector2.Distance(curConnectedAnchor, originalConnectedAnchor) >
                                          AnchorEpsilon;

            if (mainAnchorChanged || connectedAnchorChanged)
            {
                AnchorBias bias;

                if (mainAnchorChanged)
                {
                    bias = connectedAnchorChanged ? AnchorBias.Either : AnchorBias.Main;
                }
                else
                {
                    bias = AnchorBias.Connected;
                }
                foreach (HingeJoint2D hingeJoint2D in targets)
                {
                    HingeJoint2DSettings hingeSettings = HingeJoint2DSettingsEditor.Get(hingeJoint2D);
                    bool wantsLock = hingeSettings != null && hingeSettings.lockAnchors;

                    if (wantsLock)
                    {
                        GUIHelpers.RecordUndo("Inspector", hingeJoint2D);
                        ReAlignAnchors(hingeJoint2D, bias);
                        EditorUtility.SetDirty(hingeJoint2D);
                    }
                }
            }

            if (connectedRigidBody != serializedObject.FindProperty("m_ConnectedRigidBody").objectReferenceValue)
            {
                foreach (HingeJoint2D hingeJoint2D in targets)
                {
                    GUIHelpers.RecordUndo("Inspector", hingeJoint2D);
                    JointEditorHelpers.SetWorldConnectedAnchorPosition(hingeJoint2D, worldConnectedAnchors[hingeJoint2D]);

                    EditorUtility.SetDirty(hingeJoint2D);
                }
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.CollapseUndoOperations(grp);
            //Debug.Log("!!!");
            //hinge angle changed...
        }
    }

    private static void ReAlignAnchors(HingeJoint2D hingeJoint2D, AnchorBias bias = AnchorBias.Either)
    {
        Transform transform = hingeJoint2D.transform;

        Vector2 connectedAnchor = hingeJoint2D.connectedAnchor;
        Vector2 worldAnchor = JointEditorHelpers.Transform2DPoint(transform, hingeJoint2D.anchor);

        if (hingeJoint2D.connectedBody)
        {
            Rigidbody2D connectedBody = hingeJoint2D.connectedBody;
            Transform connectedTransform = connectedBody.transform;

            if (bias != AnchorBias.Main
                && (bias == AnchorBias.Connected
                    || (!transform.rigidbody2D.isKinematic && connectedBody.isKinematic)))
            {
                //other body is static or there is a bias
                Vector2 worldConnectedAnchor = JointEditorHelpers.Transform2DPoint(connectedTransform, connectedAnchor);
                hingeJoint2D.anchor = JointEditorHelpers.InverseTransform2DPoint(transform, worldConnectedAnchor);
            }
            else if (bias == AnchorBias.Main
                     || (transform.rigidbody2D.isKinematic && !connectedBody.isKinematic))
            {
                //this body is static or there is a bias
                hingeJoint2D.connectedAnchor = JointEditorHelpers.InverseTransform2DPoint(connectedTransform,
                    worldAnchor);
            }
            else
            {
                Vector2 midPoint = (JointEditorHelpers.Transform2DPoint(connectedTransform, connectedAnchor) +
                                    worldAnchor)*.5f;
                hingeJoint2D.anchor = JointEditorHelpers.InverseTransform2DPoint(transform, midPoint);
                hingeJoint2D.connectedAnchor = JointEditorHelpers.InverseTransform2DPoint(connectedTransform, midPoint);
            }
        }
        else
        {
            if (bias == AnchorBias.Main)
            {
                hingeJoint2D.connectedAnchor = worldAnchor;
            }
            else
            {
                hingeJoint2D.anchor = JointEditorHelpers.InverseTransform2DPoint(transform, connectedAnchor);
            }
        }
    }
}