using System.Collections.Generic;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof (HingeJoint2D))]
[CanEditMultipleObjects]
public class HingeJoint2DEditor : JointEditor {
    private readonly Dictionary<HingeJoint2D, PositionInfo> positionCache = new Dictionary<HingeJoint2D, PositionInfo>();

#if RECURSIVE_EDITING
    
    private static readonly Dictionary<HingeJoint2D, HingeJoint2DEditor> Editors =
        new Dictionary<HingeJoint2D, HingeJoint2DEditor>();

    private readonly Dictionary<HingeJoint2D, List<HingeJoint2DEditor>> tempEditors =
        new Dictionary<HingeJoint2D, List<HingeJoint2DEditor>>();
#endif

    public void OnEnable() {
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        foreach (HingeJoint2D hingeJoint2D in targets) {
            positionCache.Add(hingeJoint2D, new PositionInfo(hingeJoint2D));
#if RECURSIVE_EDITING
            if (!Editors.ContainsKey(hingeJoint2D)) {
                Editors.Add(hingeJoint2D, this);
            }

            AddRecursiveEditors(hingeJoint2D);
#endif
        }
    }

#if RECURSIVE_EDITING
    private void AddRecursiveEditors(HingeJoint2D hingeJoint2D) {
        List<HingeJoint2DEditor> currentEditors = new List<HingeJoint2DEditor>();

        if (hingeJoint2D.connectedBody) {
            HingeJoint2D[] connectedHinges = hingeJoint2D.connectedBody.GetComponents<HingeJoint2D>();
            List<Object> hingesToEdit = new List<Object>();
            foreach (HingeJoint2D connectedHinge in connectedHinges) {
                if (!Editors.ContainsKey(connectedHinge)) {
                    hingesToEdit.Add(connectedHinge);
                }
            }
            if (hingesToEdit.Count > 0) {
                currentEditors.Add(
                                   CreateEditor(hingesToEdit.ToArray(), typeof (HingeJoint2DEditor)) as
                                   HingeJoint2DEditor);
            }
        }
        tempEditors.Add(hingeJoint2D, currentEditors);
    }

    private void RemoveRecursiveEditors(HingeJoint2D hingeJoint2D) {
        if (tempEditors.ContainsKey(hingeJoint2D)) {
            foreach (HingeJoint2DEditor hingeJoint2DEditor in tempEditors[hingeJoint2D]) {
                DestroyImmediate(hingeJoint2DEditor);
            }
            tempEditors.Remove(hingeJoint2D);
        }
    }
#endif

    public void OnDisable() {
#if RECURSIVE_EDITING
        foreach (HingeJoint2D hingeJoint2D in targets) {
            RemoveRecursiveEditors(hingeJoint2D);
            Editors.Remove(hingeJoint2D);
        }
#endif
// ReSharper disable DelegateSubtraction
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
// ReSharper restore DelegateSubtraction
    }

    private void OnUndoRedoPerformed() {
        foreach (HingeJoint2D hingeJoint2D in targets) {
            positionCache[hingeJoint2D] = new PositionInfo(hingeJoint2D);

#if RECURSIVE_EDITING
            RemoveRecursiveEditors(hingeJoint2D);
            AddRecursiveEditors(hingeJoint2D);
#endif
        }
    }


    public void OnSceneGUI() {
        HingeJoint2D hingeJoint2D = target as HingeJoint2D;
        if (hingeJoint2D == null) {
            return;
        }

        if (Event.current.type == EventType.keyDown) {
            if ((Event.current.character + "").ToLower().Equals("f") || Event.current.keyCode == KeyCode.F) { //frame hotkey pressed
                Event.current.Use();

                Bounds bounds;
                if (Selection.activeGameObject.renderer) {
                    bounds = Selection.activeGameObject.renderer.bounds;
                    using (new DisposableHandleColor(Color.red)) {
                        Handles.RectangleCap(0, bounds.center, Quaternion.identity, bounds.size.magnitude*0.5f);
                    }
                }
                else {
                    bounds = new Bounds((Vector2) Selection.activeGameObject.transform.position, Vector2.zero);
                }
                foreach (Transform selectedTransform in Selection.transforms) {
                    bounds.Encapsulate((Vector2) selectedTransform.position);
                }
//				using (new DisposableHandleColor(Color.green)) {
////					Handles.RectangleCap(0, bounds.center, Quaternion.identity, bounds.size.magnitude * 0.5f);
//				}

                Vector2 midPoint = (GetAnchorPosition(hingeJoint2D) + GetConnectedAnchorPosition(hingeJoint2D))*.5f;
                float distance = Vector2.Distance(midPoint, hingeJoint2D.transform.position);
                Bounds hingeBounds = new Bounds(midPoint, Vector2.one*distance*2);
                bounds.Encapsulate(hingeBounds);

                using (new DisposableHandleColor(Color.blue)) {
                    Handles.RectangleCap(0, bounds.center, Quaternion.identity, bounds.size.magnitude*0.5f);
                }

                SceneView.lastActiveSceneView.LookAt(bounds.center, Quaternion.identity, bounds.size.magnitude);
            }
        }

        List<Vector2> otherAnchors = new List<Vector2>();
        foreach (HingeJoint2D otherHingeObject in Selection.GetFiltered(typeof (HingeJoint2D), SelectionMode.Deep)) {
            foreach (HingeJoint2D otherHingeJoint in otherHingeObject.GetComponents<HingeJoint2D>()) {
                if (otherHingeJoint == hingeJoint2D) {
                    continue;
                }

                Vector2 otherWorldAnchor = Transform2DPoint(otherHingeJoint.transform, otherHingeJoint.anchor);
                Vector2 otherConnectedWorldAnchor = otherHingeJoint.connectedBody
                                                        ? Transform2DPoint(otherHingeJoint.connectedBody.transform,
                                                                           otherHingeJoint.connectedAnchor)
                                                        : otherHingeJoint.connectedAnchor;

                otherAnchors.Add(otherWorldAnchor);
                otherAnchors.Add(otherConnectedWorldAnchor);
            }
        }

        if (DrawAnchorHandles(hingeJoint2D, otherAnchors)) {
            EditorUtility.SetDirty(hingeJoint2D);
        }

#if RECURSIVE_EDITING
            foreach (HingeJoint2DEditor tempEditor in tempEditors[hingeJoint2D]) {
                tempEditor.OnSceneGUI();
            }
#endif
    }

    private static void DrawExtraGizmos(IEnumerable<Transform> transforms, Vector2 midPoint) {
        RadiusHandle(transforms, midPoint, HandleUtility.GetHandleSize(midPoint)*jointSettings.anchorScale*0.5f,
                     HandleUtility.GetHandleSize(midPoint)*jointSettings.orbitRangeScale*0.5f);
    }


    private bool DrawAnchorHandles(HingeJoint2D hingeJoint2D, List<Vector2> otherAnchors) {
        bool changed = false;
        HingeJoint2DSettings hingeSettings = HingeJoint2DSettings.Get(hingeJoint2D);

        bool snapToOtherAnchor = true;
        bool anchorLock = hingeSettings != null && hingeSettings.lockAnchors;
        if (anchorLock) {
            snapToOtherAnchor = false;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPaused) {
            anchorLock = false;
            snapToOtherAnchor = false;
        }

        Transform transform = hingeJoint2D.transform;
        Vector2 transformPosition = transform.position;
        Vector2 worldAnchor = GetAnchorPosition(hingeJoint2D);
        Vector2 worldConnectedAnchor = GetConnectedAnchorPosition(hingeJoint2D);

        int mainControlID = GUIUtility.GetControlID(FocusType.Native);
        int connectedControlID = GUIUtility.GetControlID(FocusType.Native);
        int lockControlID = GUIUtility.GetControlID(FocusType.Native);
        int lockControlID2 = GUIUtility.GetControlID(FocusType.Native);

        bool overlapping = Vector2.Distance(worldConnectedAnchor, worldAnchor) <= JointEditorSettings.AnchorEpsilon;

        if (anchorLock && overlapping) {
            List<Vector2> snapPositions = new List<Vector2> {transformPosition};

            snapPositions.AddRange(otherAnchors);

            bool anchorChanged;
            worldConnectedAnchor = AnchorSlider(worldConnectedAnchor, jointSettings.anchorScale, out anchorChanged,
                                                snapPositions, AnchorBias.Connected, true, connectedControlID,
                                                hingeJoint2D);
            if (anchorChanged) {
                RecordUndo("Anchor Move", hingeJoint2D);
                changed = true;
                SetWorldConnectedAnchorPosition(hingeJoint2D, worldConnectedAnchor);
                SetWorldAnchorPosition(hingeJoint2D, worldAnchor = worldConnectedAnchor);
                positionCache[hingeJoint2D] = new PositionInfo(hingeJoint2D);
            }

            if (ToggleLockButton(lockControlID, worldConnectedAnchor, jointSettings.lockButtonTexture,
                                 jointSettings.unlockButtonTexture)) {
                RecordUndo("Unlock Anchors", hingeSettings);
                hingeSettings.lockAnchors = false;
                EditorUtility.SetDirty(hingeSettings);
            }
        }
        else {
            using (new DisposableHandleColor(Color.red)) {
                List<Vector2> snapPositions = new List<Vector2> {transformPosition};
                if (snapToOtherAnchor) {
                    snapPositions.Add(worldConnectedAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                bool anchorChanged;
                worldAnchor = AnchorSlider(worldAnchor, jointSettings.anchorScale, out anchorChanged, snapPositions,
                                           AnchorBias.Main,
                                           anchorLock, mainControlID, hingeJoint2D);
                if (anchorChanged && !anchorLock) {
                    RecordUndo("Anchor Move", hingeJoint2D);
                    changed = true;
                    SetWorldAnchorPosition(hingeJoint2D, worldAnchor);
                    positionCache[hingeJoint2D] = new PositionInfo(hingeJoint2D);
                }
            }

            using (new DisposableHandleColor(Color.green)) {
                List<Vector2> snapPositions = new List<Vector2> {transformPosition};

                if (snapToOtherAnchor) {
                    snapPositions.Add(worldAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                bool anchorChanged;
                worldConnectedAnchor = AnchorSlider(worldConnectedAnchor, jointSettings.anchorScale, out anchorChanged,
                                                    snapPositions,
                                                    AnchorBias.Connected, anchorLock, connectedControlID, hingeJoint2D);
                if (anchorChanged && !anchorLock) {
                    RecordUndo("Connected Anchor Move", hingeJoint2D);
                    changed = true;
                    SetWorldConnectedAnchorPosition(hingeJoint2D, worldConnectedAnchor);
                }
            }


            if (ToggleLockButton(lockControlID, worldAnchor,
                                 jointSettings.unlockButtonTexture, jointSettings.lockButtonTexture,
                                 anchorLock)) {
                changed = true;
                if (!anchorLock) {
                    if (hingeSettings == null) {
                        hingeSettings = HingeJoint2DSettings.GetOrCreate(hingeJoint2D);
                    }

                    RecordUndo("Lock Anchors", hingeSettings, hingeJoint2D);
                    hingeSettings.lockAnchors = true;
                    EditorUtility.SetDirty(hingeSettings);
                }
                else {
                    RecordUndo("Realign Anchors to Main", hingeJoint2D);
                }
                SetWorldConnectedAnchorPosition(hingeJoint2D, worldConnectedAnchor = worldAnchor);
                positionCache[hingeJoint2D] = new PositionInfo(hingeJoint2D);
            }

            if (!overlapping &&
                ToggleLockButton(lockControlID2, worldConnectedAnchor,
                                 jointSettings.unlockButtonTexture, jointSettings.lockButtonTexture,
                                 anchorLock)) {
                changed = true;
                if (!anchorLock) {
                    if (hingeSettings == null) {
                        hingeSettings = HingeJoint2DSettings.GetOrCreate(hingeJoint2D);
                    }
                    RecordUndo("Lock Anchors", hingeSettings, hingeJoint2D);
                    hingeSettings.lockAnchors = true;
                }
                else {
                    RecordUndo("Realign Anchors to Connected", hingeJoint2D);
                }

                SetWorldAnchorPosition(hingeJoint2D, worldAnchor = worldConnectedAnchor);
                positionCache[hingeJoint2D] = new PositionInfo(hingeJoint2D);

                EditorUtility.SetDirty(hingeSettings);
            }
        }

        if (anchorLock) {
            List<Transform> transforms = new List<Transform> {transform};

            if (hingeJoint2D.connectedBody && Event.current.shift) {
                transforms.Add(hingeJoint2D.connectedBody.transform);
            }

            DrawExtraGizmos(transforms, worldAnchor);
        }
        else {
            DrawExtraGizmos(new List<Transform> {transform}, worldAnchor);

            if (hingeJoint2D.connectedBody) {
                DrawExtraGizmos(new List<Transform> {hingeJoint2D.connectedBody.transform}, worldConnectedAnchor);
            }
        }

        if (Vector2.Distance(worldConnectedAnchor, worldAnchor) > JointEditorSettings.AnchorEpsilon) {
            using (new DisposableHandleColor(Color.cyan)) {
                Handles.DrawLine(worldAnchor, worldConnectedAnchor);
            }
        }

        using (new DisposableHandleColor(jointSettings.mainDiscColor)) {
            Handles.DrawWireDisc(worldAnchor, Vector3.forward, Vector2.Distance(worldAnchor, transform.position));
            Handles.DrawLine(transform.position, worldAnchor);
        }
        if (hingeJoint2D.connectedBody) {
            using (new DisposableHandleColor(jointSettings.connectedDiscColor)) {
                Handles.DrawWireDisc(worldConnectedAnchor, Vector3.forward,
                                     Vector2.Distance(worldConnectedAnchor,
                                                      hingeJoint2D.connectedBody.transform.position));
                Handles.DrawLine(hingeJoint2D.connectedBody.transform.position, worldConnectedAnchor);
            }
        }


        PositionChange change;
        if (anchorLock &&
            (change = positionCache[hingeJoint2D].Changed(hingeJoint2D)) != PositionChange.NoChange) {
            RecordUndo("...", hingeJoint2D);
            positionCache[hingeJoint2D] = new PositionInfo(hingeJoint2D);

            ReAlignAnchors(hingeJoint2D, GetBias(change));
            EditorUtility.SetDirty(hingeJoint2D);
        }
        return changed;
    }

    private static bool ToggleLockButton(int controlID, Vector2 center, Texture2D texture, Texture2D hotTexture,
                                         bool force = false) {
        bool result = false;

        Vector2 centerGUIPos = HandleUtility.WorldToGUIPoint(center);

        Vector2 lockPos = HandleUtility.GUIPointToWorldRay(centerGUIPos).origin;
        bool acceptEvents = force || Event.current.shift;

        Color color = Color.white;
        color.a = acceptEvents ? 1f : 0f;
        if (!acceptEvents && GUIUtility.hotControl == controlID) {
            GUIUtility.hotControl = 0;
            Event.current.Use();
            HandleUtility.Repaint();
        }
        if (acceptEvents
            && GUIHelpers.CustomHandleButton(controlID,
                                             lockPos,
                                             HandleUtility.GetHandleSize(lockPos)*jointSettings.lockButtonScale,
                                             texture,
                                             hotTexture,
                                             color)) {
            result = true;
        }
        return result;
    }

    public override void OnInspectorGUI() {
        int grp = Undo.GetCurrentGroup();

        EditorGUI.BeginChangeCheck();

        bool? lockAnchors = null;
        bool valueDifferent = false;

        foreach (HingeJoint2D hingeJoint2D in targets) {
            HingeJoint2DSettings hingeSettings = HingeJoint2DSettings.Get(hingeJoint2D);
            bool wantsLock = hingeSettings != null && hingeSettings.lockAnchors;
            if (lockAnchors != null) {
                if (lockAnchors.Value != wantsLock) {
                    valueDifferent = true;
                }
            }
            else {
                lockAnchors = wantsLock;
            }
        }

        using (new DisposableEditorGUIMixedValue(valueDifferent)) {
            bool enabled = true;
            if (lockAnchors == null) {
                lockAnchors = false;
                enabled = false;
            }
            EditorGUI.BeginChangeCheck();
            using (new DisposableGUIEnabled(enabled)) {
                lockAnchors = EditorGUILayout.Toggle("Lock Anchors", lockAnchors.Value);
            }

            if (EditorGUI.EndChangeCheck()) {
                bool wantsContinue = true;
                int choice = 1;

                if (lockAnchors.Value) {
                    bool farAway = false;
                    foreach (HingeJoint2D hingeJoint2D in targets) {
                        if (
                            Vector2.Distance(GetAnchorPosition(hingeJoint2D), GetConnectedAnchorPosition(hingeJoint2D)) >
                            JointEditorSettings.AnchorEpsilon) {
                            farAway = true;
                            break;
                        }
                    }
                    if (farAway) {
                        choice = EditorUtility.DisplayDialogComplex("Enable Anchor Lock",
                                                                    "Which anchor would you like to lock to?",
                                                                    "Main",
                                                                    "Connected",
                                                                    "Cancel");

                        if (choice == 2) {
                            wantsContinue = false;
                        }
                    }
                }
                if (wantsContinue) {
                    foreach (HingeJoint2D hingeJoint2D in targets) {
                        HingeJoint2DSettings hingeSettings = HingeJoint2DSettings.GetOrCreate(hingeJoint2D);

                        RecordUndo("toggle anchor locking", hingeSettings);
                        hingeSettings.lockAnchors = lockAnchors.Value;
                        EditorUtility.SetDirty(hingeSettings);

                        if (lockAnchors.Value) {
                            AnchorBias bias = choice == 0 ? AnchorBias.Main : AnchorBias.Connected;

                            RecordUndo("toggle anchor locking", hingeJoint2D);
                            ReAlignAnchors(hingeJoint2D, bias);
                            EditorUtility.SetDirty(hingeJoint2D);
                        }
                    }
                }
            }
        }

//        SerializedProperty propertyIterator = serializedObject.GetIterator();
//        do
//        {
//            Debug.Log(propertyIterator.name);
//        } while (propertyIterator.Next(true));

        Vector2 originalAnchor = serializedObject.FindProperty("m_Anchor").vector2Value;
        Vector2 originalConnectedAnchor = serializedObject.FindProperty("m_ConnectedAnchor").vector2Value;
        Object connectedRigidBody = serializedObject.FindProperty("m_ConnectedRigidBody").objectReferenceValue;

        Dictionary<HingeJoint2D, Vector2> worldConnectedAnchors = new Dictionary<HingeJoint2D, Vector2>();
        foreach (HingeJoint2D hingeJoint2D in targets) {
            worldConnectedAnchors.Add(hingeJoint2D, GetConnectedAnchorPosition(hingeJoint2D));
        }

        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) {
            Vector2 curAnchor = serializedObject.FindProperty("m_Anchor").vector2Value;
            Vector2 curConnectedAnchor = serializedObject.FindProperty("m_ConnectedAnchor").vector2Value;

            bool mainAnchorChanged = Vector2.Distance(curAnchor, originalAnchor) > JointEditorSettings.AnchorEpsilon;
            bool connectedAnchorChanged = Vector2.Distance(curConnectedAnchor, originalConnectedAnchor) >
                                          JointEditorSettings.AnchorEpsilon;

            if (mainAnchorChanged || connectedAnchorChanged) {
                AnchorBias bias;

                if (mainAnchorChanged) {
                    bias = connectedAnchorChanged ? AnchorBias.Either : AnchorBias.Main;
                }
                else {
                    bias = AnchorBias.Connected;
                }
                foreach (HingeJoint2D hingeJoint2D in targets) {
                    HingeJoint2DSettings hingeSettings = HingeJoint2DSettings.Get(hingeJoint2D);
                    bool wantsLock = hingeSettings != null && hingeSettings.lockAnchors;

                    if (wantsLock) {
                        RecordUndo("Inspector", hingeJoint2D);
                        ReAlignAnchors(hingeJoint2D, bias);
                        EditorUtility.SetDirty(hingeJoint2D);
                    }
                }
            }

            if (connectedRigidBody != serializedObject.FindProperty("m_ConnectedRigidBody").objectReferenceValue) {
                foreach (HingeJoint2D hingeJoint2D in targets) {
                    RecordUndo("Inspector", hingeJoint2D);
                    SetWorldConnectedAnchorPosition(hingeJoint2D, worldConnectedAnchors[hingeJoint2D]);

#if RECURSIVE_EDITING
                    RemoveRecursiveEditors(hingeJoint2D);
                    AddRecursiveEditors(hingeJoint2D);
#endif

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

    private static void ReAlignAnchors(HingeJoint2D hingeJoint2D, AnchorBias bias = AnchorBias.Either) {
        Transform transform = hingeJoint2D.transform;

        Vector2 connectedAnchor = hingeJoint2D.connectedAnchor;
        Vector2 worldAnchor = Transform2DPoint(transform, hingeJoint2D.anchor);

        if (hingeJoint2D.connectedBody) {
            Rigidbody2D connectedBody = hingeJoint2D.connectedBody;
            Transform connectedTransform = connectedBody.transform;

            if (bias != AnchorBias.Main
                && (bias == AnchorBias.Connected
                    || (!transform.rigidbody2D.isKinematic && connectedBody.isKinematic))) {
                //other body is static or there is a bias
                Vector2 worldConnectedAnchor = Transform2DPoint(connectedTransform, connectedAnchor);
                hingeJoint2D.anchor = InverseTransform2DPoint(transform, worldConnectedAnchor);
            }
            else if (bias == AnchorBias.Main
                     || (transform.rigidbody2D.isKinematic && !connectedBody.isKinematic)) {
                //this body is static or there is a bias
                hingeJoint2D.connectedAnchor = InverseTransform2DPoint(connectedTransform, worldAnchor);
            }
            else {
                Vector2 midPoint = (Transform2DPoint(connectedTransform, connectedAnchor) + worldAnchor)*.5f;
                hingeJoint2D.anchor = InverseTransform2DPoint(transform, midPoint);
                hingeJoint2D.connectedAnchor = InverseTransform2DPoint(connectedTransform, midPoint);
            }
        }
        else {
            if (bias == AnchorBias.Main) {
                hingeJoint2D.connectedAnchor = worldAnchor;
            }
            else {
                hingeJoint2D.anchor = InverseTransform2DPoint(transform, connectedAnchor);
            }
        }
    }
}
