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

public abstract class Joint2DEditorBase : Editor {
    protected const float AnchorEpsilon = JointHelpers.AnchorEpsilon;

    protected static JointEditorSettings editorSettings;

    private static readonly HashSet<string> Empty = new HashSet<string>();

    protected virtual HashSet<string> GetControlNames() {
        return Empty;
    }

    private HashSet<string> controlNames;

    private EditorWindow utilityWindow;

    protected void ShowUtility(string title, Rect windowRect, Action<Action, bool> action) {
        if (utilityWindow) {
            utilityWindow.Close();
            utilityWindow = null;
        }

        utilityWindow = EditorHelpers.ShowUtility(title, windowRect, action);
    }

    protected virtual Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias) {
        var oppositeBias = JointHelpers.GetOppositeBias(bias);
        return JointHelpers.GetAnchorPosition(anchoredJoint2D, oppositeBias);
    }

    protected virtual void ReAlignAnchors(AnchoredJoint2D joint2D, JointHelpers.AnchorBias alignmentBias) {
        var oppositeBias = JointHelpers.GetOppositeBias(alignmentBias);
        JointHelpers.SetWorldAnchorPosition(joint2D, GetWantedAnchorPosition(joint2D, oppositeBias), oppositeBias);
    }

    public bool HasFrameBounds() {
        if (editorSettings == null) {
            return false;
        }
        var anchoredJoint2D = target as AnchoredJoint2D;
        if (anchoredJoint2D == null || !anchoredJoint2D.enabled) {
            return false;
        }
        return true;
    }

    protected virtual bool WantsLocking() {
        return false;
    }

    protected virtual Vector2 GetTargetPosition(AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias) {
        return JointHelpers.GetTargetPosition(joint2D, bias);
    }

    public virtual Bounds OnGetFrameBounds() {
        var activeRenderer = Selection.activeGameObject.GetComponent<Renderer>();
        var bounds = activeRenderer
            ? activeRenderer.bounds
            : new Bounds((Vector2) Selection.activeGameObject.transform.position, Vector2.zero);
        foreach (var selectedTransform in Selection.transforms) {
            bounds.Encapsulate((Vector2) selectedTransform.position);
        }

        foreach (var joint2D in targets.Cast<AnchoredJoint2D>()) {
            var midPoint = (JointHelpers.GetAnchorPosition(joint2D) +
                            JointHelpers.GetConnectedAnchorPosition(joint2D)) * .5f;
            var distance = Vector2.Distance(midPoint,
                GetTargetPosition(joint2D, JointHelpers.AnchorBias.Main));
            if (joint2D.connectedBody) {
                var connectedDistance = Vector2.Distance(midPoint,
                    GetTargetPosition(joint2D, JointHelpers.AnchorBias.Connected));
                distance = Mathf.Max(distance, connectedDistance);
            }
            var jointBounds = new Bounds(midPoint, Vector2.one * distance * 0.5f);
            bounds.Encapsulate(jointBounds);
        }

        return bounds;
    }

    protected Vector2 AnchorSlider(int controlID, float handleScale, IEnumerable<Vector2> snapPositions,
                                   JointHelpers.AnchorBias bias, AnchoredJoint2D joint, AnchorInfo anchorInfo) {
        var sliderState = StateObject.Get<AnchorSliderState>(controlID);

        var anchorPosition = JointHelpers.GetAnchorPosition(joint, bias);
        var handleSize = HandleUtility.GetHandleSize(anchorPosition) * handleScale;
        EditorGUI.BeginChangeCheck();
        Vector2 targetPosition;
        var connectedBody = joint.connectedBody;
        if (bias == JointHelpers.AnchorBias.Connected) {
            if (connectedBody) {
                targetPosition = connectedBody.transform.position;
            } else {
                targetPosition = anchorPosition;
            }
        } else {
            targetPosition = joint.gameObject.transform.position;
        }

        var originalAngle = JointHelpers.AngleFromAnchor(anchorPosition, targetPosition,
            joint.gameObject.transform.rotation.eulerAngles.z);

        if (GUIUtility.hotControl == controlID) {
            using (var drawer = CreateHotDrawer(originalAngle)) {
                drawer.alwaysVisible = true;
                drawer.DrawSquare(anchorPosition, Quaternion.identity, handleSize);
            }
        }

        var hovering = HandleUtility.nearestControl == controlID;

        var hoveringOrHot = (hovering && GUIUtility.hotControl == 0) || controlID == GUIUtility.hotControl;

        if (hoveringOrHot && _hoverControlID != controlID) {
            _hoverControlID = controlID;

            HandleUtility.Repaint();
        } else if (!hoveringOrHot && _hoverControlID == controlID) {
            _hoverControlID = 0;
            HandleUtility.Repaint();
        }


        var joint2DSettings = SettingsHelper.GetOrCreate(joint);

        HandleAnchorContext(controlID, bias, joint, joint2DSettings, anchorPosition, connectedBody);

        HandleSliderEvents(controlID, joint, sliderState, anchorPosition, hoveringOrHot, handleSize);

        HandleDragDrop(controlID, joint, joint2DSettings);

        var result = HandleSliding(controlID, bias, originalAngle, anchorPosition, handleSize);

        result = HandleSnapping(controlID, snapPositions, bias, joint, anchorInfo, result, handleSize);

        return result;
    }

    private Vector2 HandleSnapping(int controlID, IEnumerable<Vector2> snapPositions, JointHelpers.AnchorBias bias,
                                   AnchoredJoint2D joint,
                                   AnchorInfo anchorInfo, Vector2 result, float handleSize) {
        Vector2[] snapPositionsArray;

        var customSnapPositions = GetSnapPositions(joint, anchorInfo, bias, result);

        if (customSnapPositions != null) {
            if (snapPositions == null) {
                snapPositions = customSnapPositions;
            } else {
                snapPositions = snapPositions.Concat(customSnapPositions);
            }
        }

        if (snapPositions != null) {
            snapPositionsArray = snapPositions as Vector2[] ?? snapPositions.ToArray();
        } else {
            snapPositionsArray = null;
        }

        if (Event.current.type == EventType.repaint &&
            (editorSettings.highlightSnapPositions &&
             GUIUtility.hotControl == controlID && EditorGUI.actionKey &&
             snapPositionsArray != null)) {
            var snapHighlightColor = GetAdjustedColor(editorSettings.snapHighlightColor);

            using (new HandleColor(snapHighlightColor)) {
                foreach (var snapPosition in snapPositionsArray) {
                    Handles.CircleCap(0, snapPosition, Quaternion.identity,
                        HandleUtility.GetHandleSize(snapPosition) * EditorHelpers.HandleSizeToPixels *
                        editorSettings.snapDistance * 0.5f);
                }
            }
        }

        if (EditorGUI.EndChangeCheck() && EditorGUI.actionKey && snapPositionsArray != null) {
            foreach (var snapPosition in snapPositionsArray) {
                var distance = Vector2.Distance(result, snapPosition);
                if (distance < handleSize * EditorHelpers.HandleSizeToPixels * editorSettings.snapDistance) {
                    result = snapPosition;
                    break;
                }
            }
        }
        return result;
    }

    protected Color GetAdjustedColor(Color color) {
        if (isCreatedByTarget) {
            color.a *= editorSettings.connectedJointTransparency;
        }

        return color;
    }

    private Vector2 HandleSliding(int controlID, JointHelpers.AnchorBias bias, float originalAngle,
                                  Vector2 anchorPosition,
                                  float handleSize) {
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
        using (var drawer = CreateTextureDrawer(sliderTexture, originalAngle)) {
            drawer.alwaysVisible = true;
            result = Handles.Slider2D(controlID, anchorPosition, Vector3.forward, Vector3.up, Vector3.right, handleSize,
                drawer.DrawSquare, Vector2.zero);
        }
        return result;
    }

    private void HandleSliderEvents(int controlID,
                                    AnchoredJoint2D joint,
                                    AnchorSliderState sliderState,
                                    Vector2 anchorPosition,
                                    bool hoveringOrHot,
                                    float handleSize) {
        var current = Event.current;

        switch (current.GetTypeForControl(controlID)) {
            case EventType.mouseDrag:
                if (sliderState.pressed) {
                    sliderState.dragging = true;
                }
                break;
            case EventType.mouseDown:
                if (HandleUtility.nearestControl == controlID) {
                    if (current.button == 0) {
                        sliderState.mouseOffset = Helpers2D.GUIPointTo2DPosition(current.mousePosition) - anchorPosition;
                        sliderState.dragging = false;
                        sliderState.pressed = true;
                    }
                }
                break;
            case EventType.mouseUp:
                if (sliderState.pressed && !sliderState.dragging) {
                    EditorHelpers.SelectObject(joint.gameObject);
                }
                sliderState.dragging = false;
                sliderState.pressed = false;
                break;
            case EventType.repaint:

                if (_hoverControlID == controlID) {
                    if (isCreatedByTarget && !(sliderState.pressed && sliderState.dragging)) {
                        EditorHelpers.SetEditorCursor(MouseCursor.Link);
                    } else {
                        EditorHelpers.SetEditorCursor(MouseCursor.MoveArrow);
                    }
                }

                if (hoveringOrHot) {
                    using (new HandleColor(GetAdjustedColor(editorSettings.anchorHoverColor))) {
                        Handles.DrawSolidDisc(anchorPosition, Vector3.forward, handleSize * .5f);
                        Handles.DrawWireDisc(anchorPosition, Vector3.forward, handleSize * .5f);
                    }
                }
                break;
        }
    }

    private void HandleAnchorContext(int controlID, JointHelpers.AnchorBias bias, AnchoredJoint2D joint,
                                     Joint2DSettingsBase joint2DSettings, Vector2 anchorPosition,
                                     Rigidbody2D connectedBody) {
        EditorHelpers.ContextClick(controlID, () => {
            var menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent(joint.GetType()
                                                     .Name));
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
                var otherBias = JointHelpers.GetOppositeBias(bias);
                var otherPosition = JointHelpers.GetAnchorPosition(joint, otherBias);
                if (Vector2.Distance(otherPosition, anchorPosition) <= AnchorEpsilon) {
                    menu.AddDisabledItem(new GUIContent("Bring other anchor here"));
                } else {
                    menu.AddItem(new GUIContent("Bring other anchor here"), false, () => {
                        EditorHelpers.RecordUndo("Move Joint Anchor", joint);
                        JointHelpers.SetWorldAnchorPosition(joint, anchorPosition, otherBias);
                        EditorUtility.SetDirty(joint);
                    });
                }
            }

            menu.AddItem(new GUIContent("Toggle Collide Connected",
                "Whether rigid bodies connected with this joint can collide or not."), joint.collideConnected,
                () => {
                    EditorHelpers.RecordUndo("Move Joint Anchor", joint);
                    joint.collideConnected = !joint.collideConnected;
                    EditorUtility.SetDirty(joint);
                });

            menu.AddSeparator("");

            var itemCount = menu.GetItemCount();

            ExtraMenuItems(menu, joint);

            if (itemCount != menu.GetItemCount()) {
                menu.AddSeparator("");
            }

            if (connectedBody) {
                var connectedBodyName = connectedBody.name;
                var selectConnectedBodyContent = new GUIContent(string.Format("Select '{0}'", connectedBodyName));
                if (isCreatedByTarget) {
                    menu.AddDisabledItem(selectConnectedBodyContent);
                } else {
                    menu.AddItem(selectConnectedBodyContent, false,
                        () => { Selection.activeGameObject = connectedBody.gameObject; });
                }
                menu.AddItem(new GUIContent(string.Format("Move ownership to '{0}'", connectedBodyName)), false, () => {
                    var connectedObject = connectedBody.gameObject;

                    var cloneJoint =
                        Undo.AddComponent(connectedObject, joint.GetType()) as AnchoredJoint2D;
                    if (!cloneJoint) {
                        return;
                    }
                    EditorUtility.CopySerialized(joint, cloneJoint);
                    cloneJoint.connectedBody = joint.GetComponent<Rigidbody2D>();

                    JointHelpers.SetWorldAnchorPosition(cloneJoint,
                        JointHelpers.GetAnchorPosition(joint, JointHelpers.AnchorBias.Main),
                        JointHelpers.AnchorBias.Connected);
                    JointHelpers.SetWorldAnchorPosition(cloneJoint,
                        JointHelpers.GetAnchorPosition(joint, JointHelpers.AnchorBias.Connected),
                        JointHelpers.AnchorBias.Main);

                    var jointSettings = SettingsHelper.GetOrCreate(joint);
                    var cloneSettings =
                        Undo.AddComponent(connectedObject, jointSettings.GetType()) as Joint2DSettingsBase;

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
                        var selectedObjects = new List<Object>(Selection.objects) {connectedObject};

                        if (selectedObjects.Contains(joint.gameObject)) {
                            selectedObjects.Remove(joint.gameObject);
                        }

                        Selection.objects = selectedObjects.ToArray();
                    }

                    Undo.DestroyObjectImmediate(joint);

                    OwnershipMoved(cloneJoint);
                });
                menu.AddItem(new GUIContent("Disconnect from '" + connectedBodyName + "'"), false, () => {
                    var worldConnectedPosition = JointHelpers.GetConnectedAnchorPosition(joint);

                    using (new Modification("Disconnect from connected body", joint)) {
                        joint.connectedBody = null;
                        JointHelpers.SetWorldConnectedAnchorPosition(joint, worldConnectedPosition);
                    }
                });
            } else {
                menu.AddDisabledItem(new GUIContent("Select connected body"));
                menu.AddDisabledItem(new GUIContent("Move ownership to connected body"));
                menu.AddDisabledItem(new GUIContent("Disconnect from connected body"));
            }

            menu.AddItem(new GUIContent("Delete " + joint.GetType()
                                                         .Name), false,
                () => Undo.DestroyObjectImmediate(joint));
            menu.ShowAsContext();
        });
    }


    protected void HandleDragDrop(int controlID, AnchoredJoint2D joint, Joint2DSettingsBase joint2DSettings) {
        var current = Event.current;

        if (HandleUtility.nearestControl == controlID) {
            switch (current.GetTypeForControl(controlID)) {
                case EventType.DragPerform:
                    foreach (var o in DragAndDrop.objectReferences) {
                        var gameObject = o as GameObject;
                        if (gameObject == null) {
                            continue;
                        }
                        var go = gameObject;
                        var rigidbody2D = go.GetComponent<Rigidbody2D>();
                        if (go.Equals(joint.gameObject) || rigidbody2D == null || rigidbody2D == joint.connectedBody) {
                            continue;
                        }
                        var wantsLock = joint2DSettings.lockAnchors;

                        EditorHelpers.RecordUndo("Drag Onto Anchor", joint);
                        var connectedBodyPosition = JointHelpers.GetConnectedAnchorPosition(joint);

                        var previousConnectedBody = joint.connectedBody;
                        joint.connectedBody = rigidbody2D;

                        JointHelpers.SetWorldConnectedAnchorPosition(joint, connectedBodyPosition);

                        if (wantsLock) {
                            ReAlignAnchors(joint, JointHelpers.AnchorBias.Main);
                        }

                        if (isCreatedByTarget) {
                            EditorHelpers.SelectObject(rigidbody2D.gameObject, true);
                            Selection.objects = Selection.objects.Where(o1 => o1 != previousConnectedBody.gameObject)
                                                         .ToArray();
                        }

                        EditorUtility.SetDirty(joint);
                        DragAndDrop.AcceptDrag();
                        break;
                    }
                    break;
                case EventType.DragUpdated:
                    if (DragAndDrop.objectReferences.OfType<GameObject>()
                                   .Any(go => {
                                       var rigidbody2D = go.GetComponent<Rigidbody2D>();
                                       return !go.Equals(joint.gameObject) && rigidbody2D != null &&
                                              rigidbody2D != joint.connectedBody;
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
        new GUIContent("Default Gizmos",
            "Toggles the display of default joint gizmos on the scene GUI (only effective if custom gizmos are disabled).");

    private void ToggleShowGizmos() {
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedSettings.FindProperty("showCustomGizmos"), CustomGizmosContent);
        using (new GUIEnabled(!serializedSettings.FindProperty("showCustomGizmos")
                                                 .boolValue)) {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("showDefaultgizmos"), DefaultGizmosContent);
        }
    }

    public override sealed void OnInspectorGUI() {
        if (editorSettings == null || !target || PrefabUtility.GetPrefabType(target) == PrefabType.Prefab ||
            editorSettings.disableEverything) {
            DrawDefaultInspector();
            return;
        }


        if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed") {
            Repaint();
        }

        EditorGUI.BeginChangeCheck();
        var showAdvancedOptions = EditorGUILayout.Foldout(editorSettings.showAdvancedOptions, "Advanced Options");
        if (EditorGUI.EndChangeCheck()) {
            //no need to record undo here.
            editorSettings.showAdvancedOptions = showAdvancedOptions;
            EditorUtility.SetDirty(editorSettings);
        }
        if (showAdvancedOptions && serializedSettings != null) {
            EditorGUI.BeginChangeCheck();
            using (new Indent()) {
                serializedSettings.UpdateIfDirtyOrScript();

                var showJointGizmos = serializedSettings.FindProperty("showCustomGizmos");
                var enabled = GUI.enabled &&
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
                    }
                }

                serializedSettings.ApplyModifiedProperties();
            }

            if (EditorGUI.EndChangeCheck()) {
                foreach (var targetObject in serializedSettings.targetObjects) {
                    EditorUtility.SetDirty(targetObject);
                }
            }
        }
        InspectorGUI();
    }

    private static readonly GUIContent AnchorLockContent =
        new GUIContent("Lock Anchors",
            "Toggles anchor locking, which helps you keep the main and connected anchors of the joint properly aligned.");


    private void ToggleAnchorLock() {
        EditorGUI.BeginChangeCheck();

        var lockAnchors = serializedSettings.FindProperty("lockAnchors");
        EditorGUILayout.PropertyField(lockAnchors, AnchorLockContent);
        var wantsLock = lockAnchors.boolValue;

        if (EditorGUI.EndChangeCheck()) {
            AnchorLockToggled(wantsLock);
        }
    }

    private void AnchorLockToggled(bool wantsLock) {
        var wantsContinue = true;
        var choice = 1;

        if (wantsLock) {
            var farAway = targets.Cast<AnchoredJoint2D>()
                                 .Any(joint2D =>
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
            foreach (var t in targets) {
                var joint2D = (AnchoredJoint2D) t;
                var bias = choice == 0
                    ? JointHelpers.AnchorBias.Main
                    : JointHelpers.AnchorBias.Connected;
                ToggleIndividualAnchorLock(wantsLock, joint2D, bias);
            }
        }
    }

    protected virtual void ToggleIndividualAnchorLock(bool wantsLock, AnchoredJoint2D joint2D,
                                                      JointHelpers.AnchorBias alignmentBias) {
        var jointSettings = SettingsHelper.GetOrCreate(joint2D);

        var action = wantsLock ? "Lock Anchors" : "Unlock Anchors";
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
        var grp = Undo.GetCurrentGroup();
        EditorGUI.BeginChangeCheck();

        /*SerializedProperty propertyIterator = serializedObject.GetIterator();
            do
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField(propertyIterator.propertyPath);
                EditorGUILayout.LabelField(propertyIterator.type);
                EditorGUILayout.EndHorizontal();
            } while (propertyIterator.Next(true));*/

        var originalAnchor = serializedObject.FindProperty("m_Anchor")
                                             .vector2Value;
        var originalConnectedAnchor = serializedObject.FindProperty("m_ConnectedAnchor")
                                                      .vector2Value;
        var connectedRigidBody = serializedObject.FindProperty("m_ConnectedRigidBody")
                                                 .objectReferenceValue;

        var worldConnectedAnchors =
            targets.Cast<AnchoredJoint2D>()
                   .ToDictionary(joint2D => joint2D,
                       joint2D => JointHelpers.GetConnectedAnchorPosition(joint2D));


        EditorGUI.BeginChangeCheck();
        DrawCustomInspector();

        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) {
            if (WantsLocking()) {
                var curAnchor = serializedObject.FindProperty("m_Anchor")
                                                .vector2Value;
                var curConnectedAnchor = serializedObject.FindProperty("m_ConnectedAnchor")
                                                         .vector2Value;

                var mainAnchorChanged = Vector2.Distance(curAnchor, originalAnchor) > AnchorEpsilon;
                var connectedAnchorChanged = Vector2.Distance(curConnectedAnchor, originalConnectedAnchor) >
                                             AnchorEpsilon;

                if (mainAnchorChanged || connectedAnchorChanged) {
                    JointHelpers.AnchorBias bias;

                    if (mainAnchorChanged) {
                        bias = connectedAnchorChanged
                            ? JointHelpers.AnchorBias.Either
                            : JointHelpers.AnchorBias.Main;
                    } else {
                        bias = JointHelpers.AnchorBias.Connected;
                    }
                    foreach (var tar in targets) {
                        var joint2D = (AnchoredJoint2D) tar;
                        var joint2DSettings =
                            SettingsHelper.GetOrCreate(joint2D);
                        var wantsLock = joint2DSettings.lockAnchors;

                        if (wantsLock) {
                            EditorHelpers.RecordUndo("Inspector", joint2D);
                            ReAlignAnchors(joint2D, bias);
                            EditorUtility.SetDirty(joint2D);
                        }
                    }
                }
            }

            if (connectedRigidBody != serializedObject.FindProperty("m_ConnectedRigidBody")
                                                      .objectReferenceValue) {
                foreach (var tar in targets) {
                    var joint2D = (AnchoredJoint2D) tar;
                    EditorHelpers.RecordUndo("Inspector", joint2D);
                    JointHelpers.SetWorldConnectedAnchorPosition(joint2D, worldConnectedAnchors[joint2D]);

                    var joint2DSettings =
                        SettingsHelper.GetOrCreate(joint2D);
                    var wantsLock = joint2DSettings.lockAnchors;

                    if (WantsLocking() && wantsLock) {
                        ReAlignAnchors(joint2D, JointHelpers.AnchorBias.Main);
                    }
                    EditorUtility.SetDirty(joint2D);
                }
            }
        }

        if (EditorGUI.EndChangeCheck()) {
            Undo.CollapseUndoOperations(grp);
        }
    }

    protected virtual void DrawCustomInspector() {}


    protected static List<Vector2> GetAllAnchorsInSelection(AnchoredJoint2D joint2D) {
        var otherAnchors = new List<Vector2>();
        foreach (var selectedObject in Selection.GetFiltered(typeof (AnchoredJoint2D), SelectionMode.Deep)) {
            var otherJointObject = (AnchoredJoint2D) selectedObject;
            foreach (var otherJoint in otherJointObject.GetComponents<AnchoredJoint2D>()) {
                if (otherJoint == joint2D) {
                    continue;
                }

                var otherWorldAnchor = Helpers2D.TransformPoint(otherJoint.transform,
                    otherJoint.anchor);
                var otherConnectedWorldAnchor = otherJoint.connectedBody
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
            foreach (var controlName in controlNames) {
                controlIDs[controlName] = GUIUtility.GetControlID(FocusType.Passive);
            }
        }

        public bool IsActive() {
            var hotControl = GUIUtility.hotControl;

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

        var lockPressed = EditorHelpers.CustomHandleButton(controlID,
            center,
            HandleUtility.GetHandleSize(center) * editorSettings.lockButtonScale,
            editorSettings.unlockButtonTexture, editorSettings.lockButtonTexture,
            GetAdjustedColor(Color.white));

        if (lockPressed) {
            var jointSettings = SettingsHelper.GetOrCreate(joint2D);

            EditorHelpers.RecordUndo("Lock Anchors", jointSettings, joint2D);
            jointSettings.lockAnchors = true;
            EditorUtility.SetDirty(jointSettings);

            ReAlignAnchors(joint2D, bias);
        }

        return lockPressed;
    }

    protected static bool ToggleUnlockButton(int controlID, AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias) {
        Vector3 center = JointHelpers.GetAnchorPosition(joint2D, bias);

        var lockPressed = EditorHelpers.CustomHandleButton(controlID,
            center,
            HandleUtility.GetHandleSize(center) * editorSettings.lockButtonScale,
            editorSettings.lockButtonTexture, editorSettings.unlockButtonTexture);

        if (lockPressed) {
            var jointSettings = SettingsHelper.GetOrCreate(joint2D);

            EditorHelpers.RecordUndo("Unlock Anchors", jointSettings);
            jointSettings.lockAnchors = false;
            EditorUtility.SetDirty(jointSettings);
        }

        return lockPressed;
    }

    protected virtual IEnumerable<Vector2> GetSnapPositions(AnchoredJoint2D joint2D,
                                                            AnchorInfo anchorInfo,
                                                            JointHelpers.AnchorBias bias,
                                                            Vector2 anchorPosition) {
        return null;
    }

    protected bool SliderGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, IEnumerable<Vector2> otherAnchors,
                             JointHelpers.AnchorBias bias) {
        var sliderID = anchorInfo.GetControlID("slider");
        List<Vector2> snapPositions = null;
        if (EditorGUI.actionKey) {
            snapPositions = new List<Vector2> {
                GetTargetPosition(joint2D, JointHelpers.AnchorBias.Main),
                JointHelpers.GetTargetTransform(joint2D, JointHelpers.AnchorBias.Main)
                            .position
            };

            if (joint2D.connectedBody) {
                snapPositions.Add(GetTargetPosition(joint2D, JointHelpers.AnchorBias.Connected));
                snapPositions.Add(JointHelpers.GetTargetTransform(joint2D, JointHelpers.AnchorBias.Connected)
                                              .position);
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
        }
        EditorGUI.BeginChangeCheck();
        var position = AnchorSlider(sliderID, editorSettings.anchorScale, snapPositions, bias, joint2D, anchorInfo);

        var changed = false;
        if (EditorGUI.EndChangeCheck()) {
            EditorHelpers.RecordUndo("Anchor Move", joint2D);
            changed = true;

            JointHelpers.SetWorldAnchorPosition(joint2D, position, bias);
        }
        return changed;
    }

    protected bool AnchorDraggingWidgetGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo,
                                           IEnumerable<Vector2> otherAnchors, JointHelpers.AnchorBias bias) {
        var lockID = anchorInfo.GetControlID("lock");

        var changed = PreSliderGUI(joint2D, anchorInfo, bias);

        if (!changed && WantsLocking() && (Event.current.shift || GUIUtility.hotControl == lockID) &&
            (GUIUtility.hotControl == lockID || !anchorInfo.IsActive())) {
            var farAway =
                Vector2.Distance(
                    JointHelpers.GetMainAnchorPosition(joint2D),
                    GetWantedAnchorPosition(joint2D, JointHelpers.AnchorBias.Main)
                    ) > AnchorEpsilon
                || Vector2.Distance(
                    JointHelpers.GetConnectedAnchorPosition(joint2D),
                    GetWantedAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected)
                    ) > AnchorEpsilon;


            if (SettingsHelper.GetOrCreate(joint2D)
                              .lockAnchors && (bias == JointHelpers.AnchorBias.Either || !farAway)) {
                //locked! show unlock
                if (ToggleUnlockButton(lockID, joint2D, bias)) {
                    changed = true;
                }
            } else {
                if (ToggleLockButton(lockID, joint2D, bias)) {
                    changed = true;
                }
            }
        } else if (SliderGUI(joint2D, anchorInfo, otherAnchors, bias)) {
            changed = true;
        }

        if (bias == JointHelpers.AnchorBias.Either) {
            DrawLineToBody(joint2D, JointHelpers.AnchorBias.Main);
            DrawLineToBody(joint2D, JointHelpers.AnchorBias.Connected);
        } else {
            DrawLineToBody(joint2D, bias);
        }

        changed = SingleAnchorGUI(joint2D, anchorInfo, bias) || changed;

        return changed;
    }

    private void DrawLineToBody(AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias) {
        if (bias == JointHelpers.AnchorBias.Connected && !joint2D.connectedBody) {
            return;
        }

        if (editorSettings.drawLinesToBodies || isCreatedByTarget) {
            Color lineColor;

            if (bias == JointHelpers.AnchorBias.Main) {
                lineColor = editorSettings.anchorsToMainBodyColor;
            } else {
                lineColor = editorSettings.anchorsToConnectedBodyColor;
            }

            using (new HandleColor(GetAdjustedColor(lineColor))) {
                Vector3 bodyPosition = JointHelpers.GetTargetPosition(joint2D, bias);
                var anchorPosition = JointHelpers.GetAnchorPosition(joint2D, bias);

                if (Vector2.Distance(bodyPosition, anchorPosition) > AnchorEpsilon) {
                    Handles.DrawLine(bodyPosition, anchorPosition);
                }
            }
        }
    }

    protected virtual bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        return false;
    }

    protected virtual bool PreSliderGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        return false;
    }

    private GUITextureDrawer CreateTextureDrawer(Texture2D sliderTexture, Quaternion angle) {
        return new GUITextureDrawer(sliderTexture,
            angle,
            editorSettings.anchorDisplayScale,
            isCreatedByTarget ? editorSettings.connectedJointTransparency : 1f);
    }


    private GUITextureDrawer CreateTextureDrawer(Texture2D sliderTexture, float angle) {
        return CreateTextureDrawer(sliderTexture, Helpers2D.Rotate(angle));
    }

    private GUITextureDrawer CreateHotDrawer(float angle) {
        return CreateTextureDrawer(editorSettings.hotAnchorTexture, angle);
    }

    protected void AnchorGUI(AnchoredJoint2D joint2D) {
        var jointSettings = SettingsHelper.GetOrCreate(joint2D);

        var anchorLock = WantsLocking() && jointSettings.lockAnchors;

        var playing = EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused;
        var worldAnchor = JointHelpers.GetMainAnchorPosition(joint2D);
        var worldConnectedAnchor = JointHelpers.GetConnectedAnchorPosition(joint2D);

        var overlapping = Vector2.Distance(worldConnectedAnchor, worldAnchor) <= AnchorEpsilon;

        var changed = false;

        AnchorInfo main = new AnchorInfo(controlNames),
                   connected = new AnchorInfo(controlNames),
                   locked = new AnchorInfo(controlNames);

        if (jointAnchorInfos.ContainsKey(joint2D)) {
            jointAnchorInfos[joint2D].Clear();
        } else {
            jointAnchorInfos[joint2D] = new List<AnchorInfo>();
        }

        jointAnchorInfos[joint2D].AddRange(new[] {
            main, connected, locked
        });

        var otherAnchors = GetAllAnchorsInSelection(joint2D);

        if (anchorLock && DragBothAnchorsWhenLocked()) {
            if (playing || overlapping) {
                if (AnchorDraggingWidgetGUI(joint2D, locked, otherAnchors, JointHelpers.AnchorBias.Either)) {
                    changed = true;
                }
            } else {
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
        } else {
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

    private readonly Dictionary<Joint2D, List<AnchorInfo>> jointAnchorInfos =
        new Dictionary<Joint2D, List<AnchorInfo>>();

    public void OnSceneGUI() {
        var joint2D = target as AnchoredJoint2D;
        if (joint2D == null || !joint2D.enabled) {
            return;
        }
        using (new HandleColor()) {
            if (editorSettings == null && !isCreatedByTarget ||
                (editorSettings != null && editorSettings.disableEverything)) {
                DrawDefaultSceneGUI(joint2D);

                return;
            }

            var settings = SettingsHelper.GetOrCreate(joint2D);
            if (settings && !settings.showCustomGizmos) {
                if (settings.showDefaultgizmos && !isCreatedByTarget) {
                    DrawDefaultSceneGUI(joint2D);
                }
                return;
            }

            AnchorGUI(joint2D);
        }
    }

    private readonly Dictionary<AnchoredJoint2D, Editor> defaultEditors = new Dictionary<AnchoredJoint2D, Editor>();

    private void DrawDefaultSceneGUI(AnchoredJoint2D joint2D) {
        Editor defaultEditor = null;
        if (defaultEditors.ContainsKey(joint2D)) {
            defaultEditor = defaultEditors[joint2D];
        } else {
            var editorType = GetEditorType(joint2D);
            if (editorType != null) {
                defaultEditor = CreateEditor(joint2D, editorType);
                defaultEditors[joint2D] = defaultEditor;
            }
        }

        if (defaultEditor == null) {
            return;
        }

        var method = defaultEditor.GetType()
                                  .GetMethod("OnSceneGUI",
                                      BindingFlags.Instance
                                      | BindingFlags.Public
                                      | BindingFlags.NonPublic
                                      | BindingFlags.FlattenHierarchy);

        if (method != null) {
            method.Invoke(defaultEditor, null);
        }
    }

    private static Type GetEditorType(AnchoredJoint2D joint2D) {
        Type editorType = null;

        var assembly = typeof (Editor).Assembly;

        if (joint2D is HingeJoint2D) {
            editorType = assembly.GetType("UnityEditor.HingeJoint2DEditor", true);
        } else if (joint2D is DistanceJoint2D) {
            editorType = assembly.GetType("UnityEditor.DistanceJoint2DEditor", true);
        } else if (joint2D is SliderJoint2D) {
            editorType = assembly.GetType("UnityEditor.SliderJoint2DEditor", true);
        } else if (joint2D is SpringJoint2D) {
            editorType = assembly.GetType("UnityEditor.SpringJoint2DEditor", true);
        } else if (joint2D is WheelJoint2D) {
            editorType = assembly.GetType("UnityEditor.WheelJoint2DEditor", true);
        }
        return editorType;
    }

    private SerializedObject serializedSettings;

    public void OnEnable() {
        editorSettings = JointEditorSettings.Singleton;

        if (editorSettings == null) {
            return;
        }

        var defaultNames = new HashSet<string> {"slider", "lock", "offset"};
        var childControlNames = GetControlNames();


        if (defaultNames.Overlaps(childControlNames)) {
            Debug.LogError("Reserved control names: " +
                           String.Join(",", defaultNames.Intersect(childControlNames)
                                                        .ToArray()) + ".");
        }
        controlNames = new HashSet<string>(defaultNames.Union(childControlNames));

        if (EditorHelpers.AllowMultiObjectAccess) {
            var allSettings =
                targets.Cast<Joint2D>()
                       .Select(joint2D => SettingsHelper.GetOrCreate(joint2D))
                       .Where(jointSettings => jointSettings != null)
                       .Cast<Object>()
                       .ToList();

            if (allSettings.Count > 0) {
                serializedSettings = new SerializedObject(allSettings.ToArray());
            } else {
                serializedSettings = null;
            }
        } else {
            if (target) {
                serializedSettings = new SerializedObject(new Object[] {SettingsHelper.GetOrCreate(target as Joint2D)});
            } else {
                serializedSettings = null;
            }
        }
    }

    public virtual void OnDisable() {
        foreach (var defaultEditor in defaultEditors.Values) {
            if (defaultEditor != null) {
                DestroyImmediate(defaultEditor);
            }
        }

        CleanupHotControls();

        if (utilityWindow) {
            utilityWindow.Close();
            utilityWindow = null;
        }
    }

    private void CleanupHotControls() {
        if ((from jointAnchorInfo in jointAnchorInfos.Values
             let names = GetControlNames()
             where
                 jointAnchorInfo.Any(
                     info =>
                         names.Select(controlName => info.GetControlID(controlName))
                              .Any(controlID => GUIUtility.hotControl == controlID))
             select jointAnchorInfo).Any()) {
            GUIUtility.hotControl = 0;
        }
    }

    private static int _hoverControlID;
    public bool isCreatedByTarget = false;

    internal enum Limit {
        Min,
        Max
    }

    public float LineAngleHandle(int controlID, float angle, Vector2 center,
                                 float handleScale = 1f,
                                 float lineThickness = 1f) {
        var handleSize = HandleUtility.GetHandleSize(center) * handleScale;

        var rotated2DVector = Helpers2D.GetDirection(angle) * handleSize * 0.6f;

        var left = center - rotated2DVector;
        var right = center + rotated2DVector;

        var angleState = StateObject.Get<AngleState>(controlID);
        var hoverState = angleState.hoverState;

        var current = Event.current;
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
                    var current2DPosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
                    var curAngle = Helpers2D.GetAngle(current2DPosition - angleState.center);
                    var prevAngle = Helpers2D.GetAngle(angleState.mousePosition - angleState.center);

                    var deltaAngle = Mathf.DeltaAngle(prevAngle, curAngle);
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
                        wantedColor = GetAdjustedColor(editorSettings.activeAngleColor);

                        using (new HandleColor(wantedColor)) {
                            Handles.DrawLine(angleState.center,
                                Helpers2D.ClosestPointToRay(
                                    new Ray(center,
                                        Helpers2D.GetDirection(angleState.startAngle + angleState.angleDelta)),
                                    angleState.mousePosition));
                        }

                        var cameraVectorA = EditorHelpers.HandleToScreenPoint(left);
                        var cameraVectorB = EditorHelpers.HandleToScreenPoint(right);

                        cameraVectorA.z -= 0.01f;
                        cameraVectorB.z -= 0.01f;

                        left = EditorHelpers.ScreenToHandlePoint(cameraVectorA);
                        right = EditorHelpers.ScreenToHandlePoint(cameraVectorB);
                    } else {
                        if (GUIUtility.hotControl == 0 && hoverState.hovering) {
                            wantedColor = editorSettings.hoverAngleColor;
                        } else {
                            wantedColor = editorSettings.angleWidgetColor;
                            if (GUIUtility.hotControl != 0) {
                                wantedColor.a *= editorSettings.connectedJointTransparency;
                                //semitransparent if not active control
                            }
                        }
                        wantedColor = GetAdjustedColor(wantedColor);
                    }

                    using (new HandleColor(wantedColor)) {
                        EditorHelpers.DrawThickLineWithOutline(left, right, lineThickness, lineThickness);
                        Handles.DrawWireDisc(left, Vector3.forward, handleSize * 0.125f);
                    }
                }
                break;
        }
        return angle;
    }
}

public class AnchorSliderState {
    public Vector2 mouseOffset;
    public bool dragging;
    public bool pressed;
}

public class AngleState {
    public float angleDelta;
    public Vector2 mousePosition;
    public Vector2 center;
    public float startAngle;
    public HoverState hoverState = new HoverState();
}