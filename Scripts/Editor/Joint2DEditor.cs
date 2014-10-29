using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableEditor;
using toxicFork.GUIHelpers.DisposableEditorGUI;
using toxicFork.GUIHelpers.DisposableGUI;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public abstract class Joint2DEditor : Editor, IJoint2DEditor {
    protected const float AnchorEpsilon = JointHelpers.AnchorEpsilon;

    protected static JointEditorSettings editorSettings;

    private static readonly HashSet<string> Empty = new HashSet<string>();

    protected virtual HashSet<string> GetControlNames() {
        return Empty;
    }

    private HashSet<string> controlNames;

    protected virtual Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias) {
        JointHelpers.AnchorBias oppositeBias = JointHelpers.GetOppositeBias(bias);
        return JointHelpers.GetAnchorPosition(anchoredJoint2D, oppositeBias);
    }

    protected virtual void ReAlignAnchors(AnchoredJoint2D joint2D, JointHelpers.AnchorBias alignmentBias) {
        JointHelpers.AnchorBias oppositeBias = JointHelpers.GetOppositeBias(alignmentBias);
        JointHelpers.SetWorldAnchorPosition(joint2D, GetWantedAnchorPosition(joint2D, oppositeBias), oppositeBias);
    }

    public bool HasFrameBounds() {
        if (editorSettings == null) {
            return false;
        }
        AnchoredJoint2D anchoredJoint2D = target as AnchoredJoint2D;
        if (anchoredJoint2D == null || !anchoredJoint2D.enabled) {
            return false;
        }
        return true;
    }

    protected virtual bool WantsLocking() {
        return false;
    }

    protected virtual Vector2 GetTargetPosition(AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias) {
        return GetTargetPositionWithOffset(joint2D, bias);
    }

    private static Vector2 GetTargetPositionWithOffset(AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias)
    {
        Joint2DSettings joint2DSettings = SettingsHelper.GetOrCreate(joint2D);

        Vector2 targetPosition = JointHelpers.GetTargetPosition(joint2D, bias);

        if (!joint2DSettings.useOffsets)
        {
            return targetPosition;
        }

        Transform targetTransform = JointHelpers.GetTargetTransform(joint2D, bias);

        Vector2 offset = joint2DSettings.GetOffset(bias);

        Vector2 worldOffset = offset;
        if (targetTransform != null)
        {
            worldOffset = Helpers2D.TransformVector(targetTransform, worldOffset);
        }

        return targetPosition + worldOffset;
    }

    public virtual Bounds OnGetFrameBounds() {
        Bounds bounds = Selection.activeGameObject.renderer
            ? Selection.activeGameObject.renderer.bounds
            : new Bounds((Vector2) Selection.activeGameObject.transform.position, Vector2.zero);
        foreach (Transform selectedTransform in Selection.transforms) {
            bounds.Encapsulate((Vector2) selectedTransform.position);
        }

        foreach (AnchoredJoint2D joint2D in targets.Cast<AnchoredJoint2D>()) {
            Vector2 midPoint = (JointHelpers.GetAnchorPosition(joint2D) +
                                JointHelpers.GetConnectedAnchorPosition(joint2D))*.5f;
            float distance = Vector2.Distance(midPoint,
                GetTargetPosition(joint2D, JointHelpers.AnchorBias.Main));
            if (joint2D.connectedBody) {
                float connectedDistance = Vector2.Distance(midPoint,
                    GetTargetPosition(joint2D, JointHelpers.AnchorBias.Connected));
                distance = Mathf.Max(distance, connectedDistance);
            }
            Bounds jointBounds = new Bounds(midPoint, Vector2.one*distance*0.5f);
            bounds.Encapsulate(jointBounds);
        }

        return bounds;
    }

    protected Vector2 AnchorSlider(int controlID, float handleScale,
        IEnumerable<Vector2> snapPositions, JointHelpers.AnchorBias bias, AnchoredJoint2D joint) {
        Vector2 anchorPosition = JointHelpers.GetAnchorPosition(joint, bias);
        float handleSize = HandleUtility.GetHandleSize(anchorPosition)*handleScale;
        EditorGUI.BeginChangeCheck();
        Vector2 targetPosition;
        if (bias == JointHelpers.AnchorBias.Connected) {
            if (joint.connectedBody) {
                targetPosition = joint.connectedBody.transform.position;
            }
            else {
                targetPosition = anchorPosition;
            }
        }
        else {
            targetPosition = joint.gameObject.transform.position;
        }

        float originalAngle = JointHelpers.AngleFromAnchor(anchorPosition, targetPosition,
            joint.gameObject.transform.rotation.eulerAngles.z);

        if (GUIUtility.hotControl == controlID) {
            using (
                GUITextureDrawer drawer =
                    new GUITextureDrawer(editorSettings.hotAnchorTexture,
                        Helpers2D.Rotate(originalAngle),
                        editorSettings.anchorDisplayScale)) {
                drawer.alwaysVisible = true;
                drawer.DrawSquare(anchorPosition, Quaternion.identity, handleSize);
            }
        }

        bool hovering = HandleUtility.nearestControl == controlID;

        bool showCursor = (hovering && GUIUtility.hotControl == 0) || controlID == GUIUtility.hotControl;

        if (showCursor && _hoverControlID != controlID) {
            _hoverControlID = controlID;

            HandleUtility.Repaint();
        }
        else if (!showCursor && _hoverControlID == controlID) {
            _hoverControlID = 0;
            HandleUtility.Repaint();
        }

        if (_hoverControlID == controlID && Event.current.type == EventType.repaint) {
            EditorHelpers.SetEditorCursor(MouseCursor.MoveArrow);
        }

        if (showCursor && Event.current.type == EventType.repaint) {
            using (new HandleColor(editorSettings.anchorHoverColor)) {
                Handles.DrawSolidDisc(anchorPosition, Vector3.forward, handleSize*.5f);
                Handles.DrawWireDisc(anchorPosition, Vector3.forward, handleSize*.5f);
            }
        }

        Event current = Event.current;

        Joint2DSettings joint2DSettings = SettingsHelper.GetOrCreate(joint);

        EditorHelpers.ContextClick(controlID, () => {
            GenericMenu menu = new GenericMenu();
            menu.AddSeparator(joint.GetType().Name);
            menu.AddSeparator("");
            if (WantsLocking()) {
                menu.AddItem(new GUIContent("Lock Anchors", GetAnchorLockTooltip()),
                    joint2DSettings.lockAnchors, () => {
                        EditorHelpers.RecordUndo(
                            joint2DSettings.lockAnchors ? "Unlock Anchors" : "Lock Anchors", joint2DSettings,
                            joint);
                        if (!joint2DSettings.lockAnchors) {
                            ReAlignAnchors(joint, bias);
                        }
                        joint2DSettings.lockAnchors = !joint2DSettings.lockAnchors;
                        EditorUtility.SetDirty(joint2DSettings);
                        EditorUtility.SetDirty(joint);
                    });
            }
            {
                JointHelpers.AnchorBias otherBias = JointHelpers.GetOppositeBias(bias);
                Vector2 otherPosition = JointHelpers.GetAnchorPosition(joint, otherBias);
                if (Vector2.Distance(otherPosition, anchorPosition) <= AnchorEpsilon) {
                    menu.AddDisabledItem(new GUIContent("Bring other anchor here"));
                }
                else {
                    menu.AddItem(new GUIContent("Bring other anchor here"), false, () => {
                        EditorHelpers.RecordUndo("Move Joint Anchor", joint);
                        JointHelpers.SetWorldAnchorPosition(joint, anchorPosition, otherBias);
                        EditorUtility.SetDirty(joint);
                    });
                }
            }


            menu.AddItem(new GUIContent("Collide Connected",
                "Whether rigid bodies connected with this joint can collide or not."), joint.collideConnected,
                () => {
                    EditorHelpers.RecordUndo("Move Joint Anchor", joint);
                    joint.collideConnected = !joint.collideConnected;
                    EditorUtility.SetDirty(joint);
                });

            //                    EditorGUI.ObjectField()
            menu.AddSeparator("");
            int itemCount = menu.GetItemCount();

            ExtraMenuItems(menu, joint);

            if (itemCount != menu.GetItemCount()) {
                menu.AddSeparator("");
            }

            if (joint.connectedBody) {
                menu.AddItem(new GUIContent("Move ownership to '" + joint.connectedBody.name + "'"), false, () =>
                {
                    GameObject connectedObject = joint.connectedBody.gameObject;

                    AnchoredJoint2D cloneJoint =
                        Undo.AddComponent(connectedObject, joint.GetType()) as AnchoredJoint2D;
                    if (!cloneJoint) {
                        return;
                    }
                    EditorUtility.CopySerialized(joint, cloneJoint);
                    cloneJoint.connectedBody = joint.rigidbody2D;

                    JointHelpers.SetWorldAnchorPosition(cloneJoint,
                        JointHelpers.GetAnchorPosition(joint, JointHelpers.AnchorBias.Main),
                        JointHelpers.AnchorBias.Connected);
                    JointHelpers.SetWorldAnchorPosition(cloneJoint,
                        JointHelpers.GetAnchorPosition(joint, JointHelpers.AnchorBias.Connected),
                        JointHelpers.AnchorBias.Main);

                    Joint2DSettings jointSettings = SettingsHelper.GetOrCreate(joint);
                    Joint2DSettings cloneSettings =
                        Undo.AddComponent(connectedObject, jointSettings.GetType()) as Joint2DSettings;

                    if (cloneSettings == null) {
                        return;
                    }
                    cloneSettings.hideFlags = HideFlags.HideInInspector;

                    EditorUtility.CopySerialized(jointSettings, cloneSettings);
                    cloneSettings.Setup(cloneJoint);

                    cloneSettings.SetOffset(JointHelpers.AnchorBias.Main,
                        jointSettings.GetOffset(JointHelpers.AnchorBias.Connected));
                    cloneSettings.SetOffset(JointHelpers.AnchorBias.Connected,
                        jointSettings.GetOffset(JointHelpers.AnchorBias.Main));

                    if (!Selection.Contains(connectedObject)) {
                        List<Object> selectedObjects = new List<Object>(Selection.objects) {connectedObject};

                        if (selectedObjects.Contains(joint.gameObject)) {
                            selectedObjects.Remove(joint.gameObject);
                        }

                        Selection.objects = selectedObjects.ToArray();
                    }

                    Undo.DestroyObjectImmediate(joint);

                    OwnershipMoved(cloneJoint);
                });
                menu.AddItem(new GUIContent("Disconnect from '" + joint.connectedBody.name+"'"), false, () => {
                    Vector2 worldConnectedPosition = JointHelpers.GetConnectedAnchorPosition(joint);

                    using (new Modification("Disconnect from connected body", joint)) {
                        joint.connectedBody = null;
                        JointHelpers.SetWorldConnectedAnchorPosition(joint, worldConnectedPosition);
                    }
                });
            }
            else {
                menu.AddDisabledItem(new GUIContent("Move ownership to connected body"));
                menu.AddDisabledItem(new GUIContent("Disconnect from connected body"));
            }

            menu.AddItem(new GUIContent("Delete " + joint.GetType().Name), false,
                () => Undo.DestroyObjectImmediate(joint));
            menu.ShowAsContext();
        });

        switch (current.GetTypeForControl(controlID)) {
            case EventType.mouseDown:
                if (HandleUtility.nearestControl == controlID) {
                    if (current.button == 0) {
                        AnchorSliderState state = StateObject.Get<AnchorSliderState>(controlID);
                        state.mouseOffset = Helpers2D.GUIPointTo2DPosition(current.mousePosition) - anchorPosition;
                    }
                }
                break;
            case EventType.mouseUp:
                if (current.button == 0 && GUIUtility.hotControl == controlID) {}
                break;
        }
        
        HandleDragDrop(controlID, joint, joint2DSettings);

        Vector2 result;

        Texture2D sliderTexture;

        switch (bias) {
            case JointHelpers.AnchorBias.Main:
                sliderTexture = editorSettings.mainAnchorTexture;
                break;
            case JointHelpers.AnchorBias.Connected:
                sliderTexture = editorSettings.connectedAnchorTexture;
                break;
            case JointHelpers.AnchorBias.Either:
                sliderTexture = editorSettings.lockedAnchorTexture;
                break;
            default:
                throw new ArgumentOutOfRangeException("bias");
        }
        using (
            GUITextureDrawer drawer =
                new GUITextureDrawer(sliderTexture,
                    Helpers2D.Rotate(originalAngle),
                    editorSettings.anchorDisplayScale)) {
            drawer.alwaysVisible = true;
            result = Handles.Slider2D(controlID, anchorPosition, Vector3.forward, Vector3.up, Vector3.right, handleSize,
                drawer.DrawSquare, Vector2.zero);
        }
        if (EditorGUI.EndChangeCheck() && EditorGUI.actionKey && snapPositions != null) {
            foreach (Vector2 snapPosition in snapPositions) {
                float distance = Vector2.Distance(result, snapPosition);
                if (distance < handleSize*0.25f) {
                    result = snapPosition;
                    break;
                }
            }
        }

        return result;
    }

    protected void HandleDragDrop(int controlID, AnchoredJoint2D joint, Joint2DSettings joint2DSettings) {
        Event current = Event.current;

        if (HandleUtility.nearestControl == controlID) {
            switch (current.GetTypeForControl(controlID)) {
                case EventType.DragPerform:
                    foreach (Object o in DragAndDrop.objectReferences) {
                        GameObject gameObject = o as GameObject;
                        if (gameObject == null) {
                            continue;
                        }
                        GameObject go = gameObject;
                        Rigidbody2D rigidbody2D = go.GetComponent<Rigidbody2D>();
                        if (go.Equals(joint.gameObject) || rigidbody2D == null || rigidbody2D == joint.connectedBody) {
                            continue;
                        }
                        bool wantsLock = joint2DSettings.lockAnchors;

                        EditorHelpers.RecordUndo("Drag Onto Anchor", joint);
                        Vector2 connectedBodyPosition = JointHelpers.GetConnectedAnchorPosition(joint);
                        joint.connectedBody = rigidbody2D;

                        JointHelpers.SetWorldConnectedAnchorPosition(joint, connectedBodyPosition);

                        if (wantsLock) {
                            ReAlignAnchors(joint, JointHelpers.AnchorBias.Main);
                        }

                        EditorUtility.SetDirty(joint);
                        DragAndDrop.AcceptDrag();
                        break;
                    }
                    break;
                case EventType.DragUpdated:
                    if (DragAndDrop.objectReferences.OfType<GameObject>()
                        .Any(go => {
                            Rigidbody2D rigidbody2D = go.GetComponent<Rigidbody2D>();
                            return !go.Equals(joint.gameObject) && rigidbody2D != null && rigidbody2D != joint.connectedBody;
                        })) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        Event.current.Use();
                    }
                    break;
                case EventType.DragExited:
                    break;
            }
        }
    }

    protected virtual void OwnershipMoved(AnchoredJoint2D cloneJoint) {}

    protected virtual void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint) {}

    protected virtual string GetAnchorLockTooltip() {
        return "";
    }

    protected virtual Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
        JointHelpers.AnchorBias bias, float snapDistance) {
        return position;
    }

    public struct TransformInfo {
        public readonly Vector3 pos;
        public readonly Quaternion rot;

        public TransformInfo(Vector3 position, Quaternion rotation) {
            pos = position;
            rot = rotation;
        }
    }


    private static readonly GUIContent CustomGizmosContent =
        new GUIContent("Custom Gizmos", "Toggles the display of custom joint gizmos on the scene GUI.");
    private static readonly GUIContent DefaultGizmosContent =
        new GUIContent("Default Gizmos", "Toggles the display of default joint gizmos on the scene GUI (only effective if custom gizmos are disabled).");

    private void ToggleShowGizmos() {
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedSettings.FindProperty("showCustomGizmos"), CustomGizmosContent);
        using (new GUIEnabled(!serializedSettings.FindProperty("showCustomGizmos").boolValue)) {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("showDefaultgizmos"), DefaultGizmosContent);
        }
//        if (EditorGUI.EndChangeCheck()) {
//            serializedSettings.ApplyModifiedProperties();
//        }
    }

    public override sealed void OnInspectorGUI() {
        if (editorSettings == null) {
            DrawDefaultInspector();
            return;
        }
        if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed") {
            Repaint();
        }

        EditorGUI.BeginChangeCheck();
        bool showAdvancedOptions = EditorGUILayout.Foldout(editorSettings.showAdvancedOptions, "Advanced Options");
        if (EditorGUI.EndChangeCheck()) {
            //no need to record undo here.
            editorSettings.showAdvancedOptions = showAdvancedOptions;
            EditorUtility.SetDirty(editorSettings);
        }
        if (showAdvancedOptions && serializedSettings != null)
        {
            EditorGUI.BeginChangeCheck();
            using (new Indent()) {
                serializedSettings.UpdateIfDirtyOrScript();

                SerializedProperty showJointGizmos = serializedSettings.FindProperty("showCustomGizmos");
                bool enabled = GUI.enabled &&
                               (showJointGizmos.boolValue || showJointGizmos.hasMultipleDifferentValues);
                EditorGUILayout.LabelField("Display:");
                using (new Indent()) {
                    ToggleShowGizmos();
                    InspectorDisplayGUI(enabled);
                }
                if (WantsLocking()) {
                    EditorGUILayout.LabelField("Features:");
                    using (new Indent()) {
                        if (WantsLocking()) {
                            ToggleAnchorLock();
                        }
//                        AlterOffsets(enabled);
                    }
                }

                serializedSettings.ApplyModifiedProperties();
            }

            if (EditorGUI.EndChangeCheck()) {
                foreach (Object targetObject in serializedSettings.targetObjects)
                {
                    EditorUtility.SetDirty(targetObject);
                }
            }
        }
        InspectorGUI();
    }

    private static readonly GUIContent MainOffsetContent = new GUIContent("Main Offset",
        "This offset is used to display the current angle of the object that owns the joint.");

    private static readonly GUIContent ConnectedOffsetContent = new GUIContent("Connected Offset",
        "This offset is used to display the current angle of the object that is connected by joint.");

    private void AlterOffsets(bool enabled) {
        EditorGUI.BeginChangeCheck();

        using (new GUIEnabled(enabled)) {
//            SerializedProperty useOffsets = serializedSettings.FindProperty("useOffsets");
//            EditorGUILayout.PropertyField(useOffsets);
            
            SerializedProperty mainBodyOffset = serializedSettings.FindProperty("mainBodyOffset");
            EditorGUILayout.PropertyField(mainBodyOffset, MainOffsetContent);

            SerializedProperty connectedBodyOffset = serializedSettings.FindProperty("connectedBodyOffset");
            EditorGUILayout.PropertyField(connectedBodyOffset, ConnectedOffsetContent);
        }
    }

    private static readonly GUIContent AnchorLockContent =
        new GUIContent("Lock Anchors",
            "Toggles anchor locking, which helps you keep the main and connected anchors of the joint properly aligned.");


    private void ToggleAnchorLock() {
        EditorGUI.BeginChangeCheck();

        SerializedProperty lockAnchors = serializedSettings.FindProperty("lockAnchors");
        EditorGUILayout.PropertyField(lockAnchors, AnchorLockContent);
        bool wantsLock = lockAnchors.boolValue;

        if (EditorGUI.EndChangeCheck()) {
            AnchorLockToggled(wantsLock);
        }
    }

    private void AnchorLockToggled(bool wantsLock) {
        bool wantsContinue = true;
        int choice = 1;

        if (wantsLock) {
            bool farAway = targets.Cast<AnchoredJoint2D>().Any(joint2D =>
                Vector2.Distance(
                    JointHelpers.GetMainAnchorPosition(joint2D),
                    GetWantedAnchorPosition(joint2D, JointHelpers.AnchorBias.Main)
                    ) > AnchorEpsilon || Vector2.Distance(
                        JointHelpers.GetConnectedAnchorPosition(joint2D),
                        GetWantedAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected)
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
            foreach (Object t in targets) {
                AnchoredJoint2D joint2D = (AnchoredJoint2D) t;
                JointHelpers.AnchorBias bias = choice == 0
                    ? JointHelpers.AnchorBias.Main
                    : JointHelpers.AnchorBias.Connected;
                ToggleIndividualAnchorLock(wantsLock, joint2D, bias);
            }
        }
    }

    protected virtual void ToggleIndividualAnchorLock(bool wantsLock, AnchoredJoint2D joint2D,
        JointHelpers.AnchorBias alignmentBias) {
        Joint2DSettings jointSettings = SettingsHelper.GetOrCreate(joint2D);

        string action = wantsLock ? "Lock Anchors" : "Unlock Anchors";
        EditorHelpers.RecordUndo(action, jointSettings);
        jointSettings.lockAnchors = wantsLock;
        EditorUtility.SetDirty(jointSettings);

        if (wantsLock) {
            EditorHelpers.RecordUndo(action, joint2D);
            ReAlignAnchors(joint2D, alignmentBias);
            EditorUtility.SetDirty(joint2D);
        }
    }

    protected virtual void InspectorDisplayGUI(bool enabled) {}

    private void InspectorGUI() {
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
            if (WantsLocking()) {
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
                    foreach (AnchoredJoint2D joint2D in targets) {
                        Joint2DSettings joint2DSettings =
                            SettingsHelper.GetOrCreate(joint2D);
                        bool wantsLock = joint2DSettings.lockAnchors;

                        if (wantsLock) {
                            EditorHelpers.RecordUndo("Inspector", joint2D);
                            ReAlignAnchors(joint2D, bias);
                            EditorUtility.SetDirty(joint2D);
                        }
                    }
                }
            }

            if (connectedRigidBody != serializedObject.FindProperty("m_ConnectedRigidBody").objectReferenceValue) {
                foreach (AnchoredJoint2D joint2D in targets) {
                    EditorHelpers.RecordUndo("Inspector", joint2D);
                    JointHelpers.SetWorldConnectedAnchorPosition(joint2D, worldConnectedAnchors[joint2D]);

                    Joint2DSettings joint2DSettings =
                        SettingsHelper.GetOrCreate(joint2D);
                    bool wantsLock = joint2DSettings.lockAnchors;

                    if (WantsLocking() && wantsLock) {
                        ReAlignAnchors(joint2D, JointHelpers.AnchorBias.Main);
                    }
                    EditorUtility.SetDirty(joint2D);
                }
            }
        }

        if (EditorGUI.EndChangeCheck()) {
            Undo.CollapseUndoOperations(grp);
            //Debug.Log("!!!");
        }
    }


    protected static List<Vector2> GetAllAnchorsInSelection(AnchoredJoint2D joint2D) {
        List<Vector2> otherAnchors = new List<Vector2>();
        foreach (AnchoredJoint2D otherJointObject in Selection.GetFiltered(typeof (AnchoredJoint2D), SelectionMode.Deep)
            ) {
            foreach (AnchoredJoint2D otherJoint in otherJointObject.GetComponents<AnchoredJoint2D>()) {
                if (otherJoint == joint2D) {
                    continue;
                }

                Vector2 otherWorldAnchor = Helpers2D.TransformPoint(otherJoint.transform,
                    otherJoint.anchor);
                Vector2 otherConnectedWorldAnchor = otherJoint.connectedBody
                    ? Helpers2D.TransformPoint(
                        otherJoint
                            .connectedBody
                            .transform,
                        otherJoint
                            .connectedAnchor)
                    : otherJoint.connectedAnchor;

                otherAnchors.Add(otherWorldAnchor);
                otherAnchors.Add(otherConnectedWorldAnchor);
            }
        }
        return otherAnchors;
    }

    protected class AnchorInfo {
        public bool ignoreHover = false;

        private readonly Dictionary<string, int> controlIDs = new Dictionary<string, int>();

        public AnchorInfo(IEnumerable<string> controlNames) {
            foreach (string controlName in controlNames) {
                controlIDs[controlName] = GUIUtility.GetControlID(FocusType.Passive);
            }
        }

        public bool IsActive() {
            int hotControl = GUIUtility.hotControl;

            return controlIDs.Any(pair => hotControl == pair.Value);
        }

        public int GetControlID(string name) {
            if (!controlIDs.ContainsKey(name)) {
                throw new ArgumentException("There is no controlID named " + name + ".");
            }
            return controlIDs[name];
        }
    }

    protected bool ToggleLockButton(int controlID, AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias) {
        Vector3 center = JointHelpers.GetAnchorPosition(joint2D, bias);

        bool lockPressed = EditorHelpers.CustomHandleButton(controlID,
            center,
            HandleUtility.GetHandleSize(center)*editorSettings.lockButtonScale,
            editorSettings.unlockButtonTexture, editorSettings.lockButtonTexture);

        if (lockPressed) {
            Joint2DSettings jointSettings = SettingsHelper.GetOrCreate(joint2D);

            EditorHelpers.RecordUndo("Lock Anchors", jointSettings, joint2D);
            jointSettings.lockAnchors = true;
            EditorUtility.SetDirty(jointSettings);

            ReAlignAnchors(joint2D, bias);
        }

        return lockPressed;
    }

    protected static bool ToggleUnlockButton(int controlID, AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias) {
        Vector3 center = JointHelpers.GetAnchorPosition(joint2D, bias);

        bool lockPressed = EditorHelpers.CustomHandleButton(controlID,
            center,
            HandleUtility.GetHandleSize(center)*editorSettings.lockButtonScale,
            editorSettings.lockButtonTexture, editorSettings.unlockButtonTexture);

        if (lockPressed) {
            Joint2DSettings jointSettings = SettingsHelper.GetOrCreate(joint2D);

            EditorHelpers.RecordUndo("Unlock Anchors", jointSettings);
            jointSettings.lockAnchors = false;
            EditorUtility.SetDirty(jointSettings);
        }

        return lockPressed;
    }

    private static readonly List<Vector2> SnapPositions = new List<Vector2>();

    protected virtual IEnumerable<Vector2> GetSnapPositions(AnchoredJoint2D joint2D, AnchorInfo anchorInfo,
        JointHelpers.AnchorBias bias) {
        return SnapPositions;
    }

    protected bool SliderGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, IEnumerable<Vector2> otherAnchors,
        JointHelpers.AnchorBias bias) {
        int sliderID = anchorInfo.GetControlID("slider");
        List<Vector2> snapPositions = null;
        if (EditorGUI.actionKey) {
            snapPositions = new List<Vector2> {
                GetTargetPosition(joint2D, JointHelpers.AnchorBias.Main),
                JointHelpers.GetTargetTransform(joint2D, JointHelpers.AnchorBias.Main).position
            };

            if (joint2D.connectedBody) {
                snapPositions.Add(GetTargetPosition(joint2D, JointHelpers.AnchorBias.Connected));
                snapPositions.Add(JointHelpers.GetTargetTransform(joint2D, JointHelpers.AnchorBias.Connected).position);
            }

            switch (bias) {
                case JointHelpers.AnchorBias.Main:
                    snapPositions.Add(JointHelpers.GetAnchorPosition(joint2D,
                        JointHelpers.AnchorBias.Connected));
                    break;
                case JointHelpers.AnchorBias.Connected:
                    snapPositions.Add(JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main));
                    break;
            }

            snapPositions.AddRange(otherAnchors);
            snapPositions.AddRange(GetSnapPositions(joint2D, anchorInfo, bias));
        }
        EditorGUI.BeginChangeCheck();
        Vector2 position = AnchorSlider(sliderID, editorSettings.anchorScale, snapPositions, bias, joint2D);

        bool changed = false;
        if (EditorGUI.EndChangeCheck()) {
            EditorHelpers.RecordUndo("Anchor Move", joint2D);
            changed = true;

            position = AlterDragResult(sliderID, position, joint2D, bias,
                HandleUtility.GetHandleSize(position)*editorSettings.anchorScale*0.25f);

            JointHelpers.SetWorldAnchorPosition(joint2D, position, bias);
        }
        return changed;
    }

    protected bool AnchorDraggingWidgetGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo,
        IEnumerable<Vector2> otherAnchors, JointHelpers.AnchorBias bias) {
        int lockID = anchorInfo.GetControlID("lock");

        bool changed = PreSliderGUI(joint2D, anchorInfo, bias);

        if (!changed && WantsLocking() && Event.current.shift &&
            (GUIUtility.hotControl == lockID || !anchorInfo.IsActive())) {
            bool farAway =
                Vector2.Distance(
                    JointHelpers.GetMainAnchorPosition(joint2D),
                    GetWantedAnchorPosition(joint2D, JointHelpers.AnchorBias.Main)
                    ) > AnchorEpsilon
                || Vector2.Distance(
                    JointHelpers.GetConnectedAnchorPosition(joint2D),
                    GetWantedAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected)
                    ) > AnchorEpsilon;


            if (SettingsHelper.GetOrCreate(joint2D).lockAnchors && (bias == JointHelpers.AnchorBias.Either || !farAway)) {
                //locked! show unlock
                if (ToggleUnlockButton(lockID, joint2D, bias)) {
                    changed = true;
                }
            }
            else {
                if (ToggleLockButton(lockID, joint2D, bias)) {
                    changed = true;
                }
            }
        }
        else if (SliderGUI(joint2D, anchorInfo, otherAnchors, bias)) {
            changed = true;
        }

        changed = SingleAnchorGUI(joint2D, anchorInfo, bias) || changed;

        return changed;
    }

    protected virtual bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        return false;
    }

    protected virtual bool PreSliderGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        return false;
    }


    protected void DrawOffset(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        Joint2DSettings jointSettings = SettingsHelper.GetOrCreate(joint2D);

        Vector2 localOffset = jointSettings.GetOffset(bias);
        Transform transform = JointHelpers.GetTargetTransform(joint2D, bias);
        if (transform == null) {
            return;
        }


        Vector2 worldOffset = Helpers2D.TransformPoint(transform, localOffset);

        EditorGUI.BeginChangeCheck();
        float handleSize = HandleUtility.GetHandleSize(worldOffset)*0.5f;

        using (
            GUITextureDrawer drawer =
                new GUITextureDrawer(editorSettings.offsetTexture,
                    Quaternion.identity,
                    editorSettings.anchorDisplayScale)) {
            drawer.alwaysVisible = true;

            int controlID = anchorInfo.GetControlID("offset");

            worldOffset = Handles.Slider2D(controlID, worldOffset, Vector3.forward, Vector3.up, Vector3.right,
                handleSize,
                drawer.DrawSquare, Vector2.zero);

            bool hovering = HandleUtility.nearestControl == controlID;

            bool showCursor = (hovering && GUIUtility.hotControl == 0) || controlID == GUIUtility.hotControl;

            if (showCursor && _hoverControlID != controlID) {
                _hoverControlID = controlID;

                HandleUtility.Repaint();
            }
            else if (!showCursor && _hoverControlID == controlID) {
                _hoverControlID = 0;
                HandleUtility.Repaint();
            }

            if (_hoverControlID == controlID && Event.current.type == EventType.repaint) {
                EditorHelpers.SetEditorCursor(MouseCursor.MoveArrow);
            }

            if (showCursor && Event.current.type == EventType.repaint) {
                using (new HandleColor(editorSettings.anchorHoverColor)) {
                    Handles.DrawSolidDisc(worldOffset, Vector3.forward, handleSize*.5f);
                    Handles.DrawWireDisc(worldOffset, Vector3.forward, handleSize*.5f);
                }
            }
        }


        if (EditorGUI.EndChangeCheck()) {
            if (EditorGUI.actionKey) {
                List<Vector2> snapPositions = new List<Vector2> {
                    transform.position,
                    JointHelpers.GetMainAnchorPosition(joint2D),
                    JointHelpers.GetConnectedAnchorPosition(joint2D)
                };


                //snap to other offset as well!
                JointHelpers.AnchorBias oppositeBias = JointHelpers.GetOppositeBias(bias);

                Transform oppositeTransform = JointHelpers.GetTargetTransform(joint2D, oppositeBias);
                if (oppositeTransform)
                {
                    if (jointSettings.useOffsets) {
                        snapPositions.Add(Helpers2D.TransformPoint(oppositeTransform, jointSettings.GetOffset(oppositeBias)));
                    }
                    snapPositions.Add(oppositeTransform.position);
                }

                foreach (Vector2 position in snapPositions) {
                    if (Vector2.Distance(worldOffset, position) < handleSize*0.25f) {
                        worldOffset = position;
                        break;
                    }
                }
            }

            EditorHelpers.RecordUndo("Change Offset", jointSettings);
            jointSettings.SetOffset(bias, Helpers2D.InverseTransformPoint(transform, worldOffset));
            EditorUtility.SetDirty(jointSettings);
        }
    }


    protected void AnchorGUI(AnchoredJoint2D joint2D) {
        Joint2DSettings jointSettings = SettingsHelper.GetOrCreate(joint2D);

        bool anchorLock = WantsLocking() && jointSettings.lockAnchors;

        bool playing = EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused;
        Vector2 worldAnchor = JointHelpers.GetMainAnchorPosition(joint2D);
        Vector2 worldConnectedAnchor = JointHelpers.GetConnectedAnchorPosition(joint2D);

        bool overlapping = Vector2.Distance(worldConnectedAnchor, worldAnchor) <= AnchorEpsilon;

        bool changed = false;

        AnchorInfo main = new AnchorInfo(controlNames),
            connected = new AnchorInfo(controlNames),
            locked = new AnchorInfo(controlNames);

        if (jointSettings.useOffsets) {

            if ((EditorGUI.actionKey || GUIUtility.hotControl == main.GetControlID("offset")))
            {
                DrawOffset(joint2D, main, JointHelpers.AnchorBias.Main);
            }

            if ((EditorGUI.actionKey || GUIUtility.hotControl == connected.GetControlID("offset")))
            {
                DrawOffset(joint2D, connected, JointHelpers.AnchorBias.Connected);
            }   
        }

        List<Vector2> otherAnchors = GetAllAnchorsInSelection(joint2D);

        if (anchorLock && DragBothAnchorsWhenLocked()) {
            if (playing || overlapping) {
                if (AnchorDraggingWidgetGUI(joint2D, locked, otherAnchors, JointHelpers.AnchorBias.Either)) {
                    changed = true;
                }
            }
            else {
                //draw the locks instead, force them to show
                if (ToggleLockButton(main.GetControlID("lock"), joint2D, JointHelpers.AnchorBias.Main)) {
                    changed = true;
                }
                if (ToggleLockButton(connected.GetControlID("lock"), joint2D, JointHelpers.AnchorBias.Connected)) {
                    changed = true;
                }
            }

            if (!changed) {
                changed = PostAnchorGUI(joint2D, locked, otherAnchors, JointHelpers.AnchorBias.Either);
            }
        }
        else {
            if (AnchorDraggingWidgetGUI(joint2D, connected, otherAnchors, JointHelpers.AnchorBias.Connected)) {
                changed = true;
                if (anchorLock) {
                    ReAlignAnchors(joint2D, JointHelpers.AnchorBias.Connected);
                }
            }

            if (AnchorDraggingWidgetGUI(joint2D, main, otherAnchors, JointHelpers.AnchorBias.Main)) {
                changed = true;
                if (anchorLock) {
                    ReAlignAnchors(joint2D, JointHelpers.AnchorBias.Main);
                }
            }

            if (!changed) {
                changed = PostAnchorGUI(joint2D, main, otherAnchors, JointHelpers.AnchorBias.Main)
                          || PostAnchorGUI(joint2D, connected, otherAnchors, JointHelpers.AnchorBias.Connected);
            }
        }

        if (changed) {
            EditorUtility.SetDirty(joint2D);
        }
    }

    protected virtual bool PostAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo info, List<Vector2> otherAnchors,
        JointHelpers.AnchorBias bias) {
        return false;
    }

    protected virtual bool DragBothAnchorsWhenLocked() {
        return true;
    }

    public void OnSceneGUI() {
        AnchoredJoint2D joint2D = target as AnchoredJoint2D;
        if (joint2D == null || !joint2D.enabled)
        {
            return;
        }
        
        if (editorSettings == null) {
            DrawDefaultSceneGUI(joint2D);

            return;
        }

        Joint2DSettings settings = SettingsHelper.GetOrCreate(joint2D);
        if (settings && !settings.showCustomGizmos) {
            if (settings.showDefaultgizmos) {
                DrawDefaultSceneGUI(joint2D);
            }
            return;
        }

        AnchorGUI(joint2D);
    }

    private readonly Dictionary<AnchoredJoint2D, Editor> defaultEditors = new Dictionary<AnchoredJoint2D, Editor>();

    private void DrawDefaultSceneGUI(AnchoredJoint2D joint2D) {
        Editor defaultEditor = null;
        if (defaultEditors.ContainsKey(joint2D)) {
            defaultEditor = defaultEditors[joint2D];
        }
        else {
            Type editorType = null;

            Assembly assembly = typeof (Editor).Assembly;

            if (joint2D is HingeJoint2D) {
                editorType = assembly.GetType("UnityEditor.HingeJoint2DEditor", true);
            }
            else if (joint2D is DistanceJoint2D) {
                editorType = assembly.GetType("UnityEditor.DistanceJoint2DEditor", true);
            }
            else if (joint2D is SliderJoint2D) {
                editorType = assembly.GetType("UnityEditor.SliderJoint2DEditor", true);
            }
            else if (joint2D is SpringJoint2D) {
                editorType = assembly.GetType("UnityEditor.SpringJoint2DEditor", true);
            }
            else if (joint2D is WheelJoint2D) {
                editorType = assembly.GetType("UnityEditor.WheelJoint2DEditor", true);
            }
            if (editorType != null) {
                defaultEditor = CreateEditor(joint2D, editorType);
                defaultEditors[joint2D] = defaultEditor;
            }
        }
        if (defaultEditor != null) {
            MethodInfo method = defaultEditor.GetType()
                .GetMethod("OnSceneGUI",
                    BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.FlattenHierarchy);

            if (method != null) {
                method.Invoke(defaultEditor, null);
            }
        }
    }

    SerializedObject serializedSettings;

    public void OnEnable() {
        editorSettings = JointEditorSettings.Singleton;

        if (editorSettings == null) {
            return;
        }

        HashSet<string> defaultNames = new HashSet<string> {"slider", "lock", "offset"};
        HashSet<string> childControlNames = GetControlNames();


        if (defaultNames.Overlaps(childControlNames)) {
            Debug.LogError("Reserved control names: " +
                           String.Join(",", defaultNames.Intersect(childControlNames).ToArray()) + ".");
        }
        controlNames = new HashSet<string>(defaultNames.Union(childControlNames));

        if (WantsLocking()) {
            SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
        }

        List<Object> allSettings =
            targets.Cast<Joint2D>().Select(joint2D => SettingsHelper.GetOrCreate(joint2D))
                .Where(jointSettings => jointSettings != null).Cast<Object>().ToList();

        serializedSettings = new SerializedObject(allSettings.ToArray());
    }

    public void OnDisable() {
        foreach (Editor defaultEditor in defaultEditors.Values) {
            if (defaultEditor != null) {
                DestroyImmediate(defaultEditor);
            }
        }
        if (editorSettings == null) {
            return;
        }
        if (WantsLocking()) {
            // ReSharper disable DelegateSubtraction
            SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
            // ReSharper restore DelegateSubtraction
        }
    }


    


    private readonly Dictionary<AnchoredJoint2D, PositionInfo> positions =
        new Dictionary<AnchoredJoint2D, PositionInfo>();

    private static int _hoverControlID;

    public void OnPreSceneGUI() {
        if (editorSettings == null) {
            return;
        }
        if (WantsLocking() && editorSettings.automaticRealign) {
            //gets called before gizmos!
            AnchoredJoint2D joint2D = target as AnchoredJoint2D;
            if (joint2D) {
                positions[joint2D] = new PositionInfo(joint2D);
            }
        }
    }

    public void OnSceneGUIDelegate(SceneView sceneView) {
        if (editorSettings == null) {
//            SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
            return;
        }

        if (!editorSettings.automaticRealign) {
            return;
        }

        //gets called after gizmos!
        foreach (AnchoredJoint2D joint2D in targets.Cast<AnchoredJoint2D>()) {
            if (joint2D == null || !joint2D.enabled) {
                continue;
            }
            if (positions.ContainsKey(joint2D)) {
                PositionInfo.Change change = positions[joint2D].Changed(joint2D);
                Joint2DSettings settings = SettingsHelper.GetOrCreate(joint2D);

                Vector2 main = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
                Vector2 connected = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
                if (settings.lockAnchors && Vector2.Distance(main, connected) > JointHelpers.AnchorEpsilon &&
                    change != PositionInfo.Change.NoChange) {
                    EditorHelpers.RecordUndo("Realign", joint2D);
                    ReAlignAnchors(joint2D, JointHelpers.GetBias(change));
                    EditorUtility.SetDirty(joint2D);
                }
            }
        }
    }


    internal enum Limit {
        Min,
        Max
    }

    public static float LineAngleHandle(int controlID, float angle, Vector2 center,
        float handleScale = 1f,
        float lineThickness = 1f) {
        float handleSize = HandleUtility.GetHandleSize(center)*handleScale;

        Vector2 rotated2DVector = Helpers2D.GetDirection(angle)*handleSize;

        Vector2 left = center - rotated2DVector;
        Vector2 right = center + rotated2DVector;

        AngleState angleState = StateObject.Get<AngleState>(controlID);
        HoverState hoverState = angleState.hoverState;

        Event current = Event.current;
        if (current.type == EventType.layout) {
            HandleUtility.AddControl(controlID, HandleUtility.DistanceToLine(left, right) - lineThickness);
        }

        bool hovering;
        switch (current.GetTypeForControl(controlID)) {
            case EventType.mouseMove:
                hovering = (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID);

                if (hoverState.hovering != hovering) {
                    hoverState.hovering = hovering;
                    HandleUtility.Repaint();
                }
                break;
            case EventType.mouseUp:
                if (GUIUtility.hotControl == controlID && Event.current.button == 0) {
                    GUIUtility.hotControl = 0;

                    hovering = (HandleUtility.nearestControl == controlID);

                    if (hoverState.hovering != hovering) {
                        hoverState.hovering = hovering;
                        HandleUtility.Repaint();
                    }

                    Event.current.Use();
                }
                break;
            case EventType.mouseDrag:
                if (GUIUtility.hotControl == controlID) {
                    Vector2 current2DPosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
                    float curAngle = Helpers2D.GetAngle(current2DPosition - angleState.center);
                    float prevAngle = Helpers2D.GetAngle(angleState.mousePosition - angleState.center);

                    float deltaAngle = Mathf.DeltaAngle(prevAngle, curAngle);
                    if (Mathf.Abs(deltaAngle) > Mathf.Epsilon) {
                        angleState.angleDelta += deltaAngle;

                        angle = angleState.startAngle + angleState.angleDelta;

                        GUI.changed = true;
                    }

                    angleState.mousePosition = current2DPosition;
                }
                break;
            case EventType.mouseDown:
                if (Event.current.button == 0 && GUIUtility.hotControl == 0 && HandleUtility.nearestControl == controlID) {
                    GUIUtility.hotControl = controlID;
                    angleState.angleDelta = 0;
                    angleState.startAngle = angle;
                    angleState.mousePosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
                    angleState.center = center;
                    Event.current.Use();
                }
                break;
            case EventType.repaint:
                using (new HandleColor()) {
                    if (GUIUtility.hotControl == controlID || (GUIUtility.hotControl == 0 && hoverState.hovering)) {
                        EditorHelpers.SetEditorCursor(MouseCursor.RotateArrow, controlID);
                    }
                    Color wantedColor;
                    if (GUIUtility.hotControl == controlID) {
                        wantedColor = editorSettings.activeAngleColor;
                        using (new HandleColor(wantedColor)) {
                            Handles.DrawLine(angleState.center,
                                Helpers2D.ClosestPointToRay(
                                    new Ray(center,
                                        Helpers2D.GetDirection(angleState.startAngle + angleState.angleDelta)),
                                    angleState.mousePosition));
                        }

                        Vector3 cameraVectorA = EditorHelpers.HandleToScreenPoint(left);
                        Vector3 cameraVectorB = EditorHelpers.HandleToScreenPoint(right);

                        cameraVectorA.z -= 0.01f;
                        cameraVectorB.z -= 0.01f;

                        left = EditorHelpers.ScreenToHandlePoint(cameraVectorA);
                        right = EditorHelpers.ScreenToHandlePoint(cameraVectorB);
                    }
                    else {
                        if (GUIUtility.hotControl == 0 && hoverState.hovering) {
                            wantedColor = editorSettings.hoverAngleColor;
                        }
                        else {
                            wantedColor = editorSettings.angleWidgetColor;
                            if (GUIUtility.hotControl != 0) {
                                wantedColor.a = 0.25f; //semitransparent if not active control
                            }
                        }
                    }

                    using (new HandleColor(wantedColor)) {
                        EditorHelpers.DrawThickLineWithOutline(left, right, lineThickness, lineThickness);
                        Handles.DrawWireDisc(left, Vector3.forward, handleSize*0.125f);
                    }
                }
                break;
        }
        return angle;
    }
}

public class AnchorSliderState {
    public Vector2 mouseOffset;
}

public class AngleState {
    public float angleDelta;
    public Vector2 mousePosition;
    public Vector2 center;
    public float startAngle;
    public HoverState hoverState = new HoverState();
}