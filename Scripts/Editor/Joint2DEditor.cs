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

public abstract class Joint2DEditor : Editor {
    protected const float AnchorEpsilon = JointHelpers.AnchorEpsilon;

    protected static readonly AssetUtils Utils = new AssetUtils("2DJointEditors/Data");

    private static JointEditorSettings _editorSettings;

    protected static JointEditorSettings editorSettings {
        get {
            if (_editorSettings != null) {
                return _editorSettings;
            }
            _editorSettings = Utils.GetOrCreateAsset<JointEditorSettings>("settings.asset");
            if (_editorSettings == null) {
                Debug.Log("deleted!");
            }
            return _editorSettings;
        }
    }

    private static readonly HashSet<String> Empty = new HashSet<string>();

    protected virtual HashSet<String> GetControlNames() {
        return Empty;
    }

    private HashSet<string> controlNames;

    protected virtual Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias) {
        JointHelpers.AnchorBias oppositeBias = JointHelpers.GetOppositeBias(bias);
        return JointHelpers.GetAnchorPosition(anchoredJoint2D, oppositeBias);
    }

    protected virtual void ReAlignAnchors(AnchoredJoint2D joint2D, JointHelpers.AnchorBias alignmentBias) {
        JointHelpers.AnchorBias oppositeBias = JointHelpers.GetOppositeBias(alignmentBias);
        JointHelpers.SetAnchorPosition(joint2D, GetWantedAnchorPosition(joint2D, oppositeBias), oppositeBias);
    }

    protected Vector2 AnchorSlider(float handleScale, IEnumerable<Vector2> snapPositions,
        JointHelpers.AnchorBias bias, AnchoredJoint2D joint) {
        int controlID = GUIUtility.GetControlID(FocusType.Native);
        return AnchorSlider(controlID, handleScale, snapPositions, bias, joint);
    }

    public bool HasFrameBounds() {
        AnchoredJoint2D anchoredJoint2D = target as AnchoredJoint2D;
        if (anchoredJoint2D == null || !anchoredJoint2D.enabled) {
            return false;
        }
        return true;
    }

    protected virtual bool WantsLocking() {
        return false;
    }

    protected virtual bool WantsOffset() {
        return false;
    }

    protected virtual Vector2 GetTargetPosition(AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias) {
        if (WantsOffset()) {
            return GetTargetPositionWithOffset(joint2D, bias);
        }
        return JointHelpers.GetTargetPosition(joint2D, bias);
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

        //float distanceFromInner = HandleUtility.DistanceToCircle(anchorPosition, handleSize*.5f);
        //bool inZone = distanceFromInner <= 0;
        bool hovering = HandleUtility.nearestControl == controlID;

        bool showCursor = (hovering && GUIUtility.hotControl == 0) || controlID == GUIUtility.hotControl;

        SetCursor(showCursor, MouseCursor.MoveArrow, controlID);

        if (showCursor && Event.current.type == EventType.repaint) {
            using (new HandleColor(editorSettings.previewRadiusColor)) {
                Handles.DrawSolidDisc(anchorPosition, Vector3.forward, handleSize*.5f);
                Handles.DrawWireDisc(anchorPosition, Vector3.forward, handleSize*.5f);
            }
        }

        Event current = Event.current;

        switch (current.GetTypeForControl(controlID)) {
            case EventType.mouseDown:
                if (current.button == 0 && HandleUtility.nearestControl == controlID) {
                    AnchorSliderState state = StateObject.Get<AnchorSliderState>(controlID);
                    state.mouseOffset = Helpers2D.GUIPointTo2DPosition(current.mousePosition) - anchorPosition;
                }
                break;
            case EventType.mouseUp:
                if (current.button == 0 && GUIUtility.hotControl == controlID) {}
                break;
        }
        if (HandleUtility.nearestControl == controlID) {
            switch (current.GetTypeForControl(controlID)) {
                case EventType.DragPerform:
                    foreach (GameObject go in DragAndDrop.objectReferences
                        .Cast<GameObject>()
                        .Where(go => !go.Equals(joint.gameObject) && go.GetComponent<Rigidbody2D>() != null)) {
                        Joint2DSettings jointSettings = SettingsHelper.GetOrCreate(joint);
                        bool wantsLock = jointSettings.lockAnchors;

                        EditorHelpers.RecordUndo("Drag Onto Anchor", joint);
                        Vector2 connectedBodyPosition = JointHelpers.GetConnectedAnchorPosition(joint);
                        joint.connectedBody = go.GetComponent<Rigidbody2D>();
                        if (wantsLock) {
                            ReAlignAnchors(joint, JointHelpers.AnchorBias.Main);
                        }
                        else {
                            JointHelpers.SetWorldConnectedAnchorPosition(joint, connectedBodyPosition);
                        }
                        EditorUtility.SetDirty(joint);
                        DragAndDrop.AcceptDrag();
                        break;
                    }
                    break;
                case EventType.DragUpdated:
                    if (DragAndDrop.objectReferences
                        .Cast<GameObject>()
                        .Any(go => !go.Equals(joint.gameObject) && go.GetComponent<Rigidbody2D>() != null)) {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        Event.current.Use();
                    }
//                Debug.Log(DragAndDrop.objectReferences.Cast<Rigidbody2D>().Count());
                    break;
                case EventType.DragExited:
//                Debug.Log(DragAndDrop.objectReferences.Cast<Rigidbody2D>().Count());
                    break;
            }
        }

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
        if (EditorGUI.EndChangeCheck() && snapPositions != null) {
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

    protected virtual Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
        JointHelpers.AnchorBias bias, float snapDistance) {
        return position;
    }

    private static MouseCursor? _lastCursor;
    private static int _cursorControlID;

    private static void SetCursor(bool showCursor, MouseCursor wantedCursor, int controlID) {
        if (showCursor) {
            if (_lastCursor != wantedCursor) {
                HandleUtility.Repaint();
                _lastCursor = wantedCursor;
                _cursorControlID = controlID;
            }
        }
        else {
            if (_lastCursor == wantedCursor && _cursorControlID == controlID) {
                HandleUtility.Repaint();
                _lastCursor = null;
                _cursorControlID = 0;
            }
        }
    }


    public struct TransformInfo {
        public readonly Vector3 pos;
        public readonly Quaternion rot;

        public TransformInfo(Vector3 position, Quaternion rotation) {
            pos = position;
            rot = rotation;
        }
    }

    public class RadiusHandleData {
        public Vector2 previousPosition, originalPosition;
        public Dictionary<Transform, TransformInfo> originalTransformInfos;
        public float accum;
        public int button;
        public Vector2 midPoint;
    }

    protected static void OrbitHandle(
        IEnumerable<Transform> transforms,
        IEnumerable<Transform> rightTransforms,
        Vector2 midPoint,
        float innerRadius,
        float radius) {
        OrbitHandle(
            GUIUtility.GetControlID(FocusType.Passive),
            transforms,
            rightTransforms,
            midPoint,
            innerRadius,
            radius);
    }

    protected static void OrbitHandle(
        IEnumerable<Transform> transforms,
        Vector2 midPoint,
        float innerRadius,
        float radius) {
        OrbitHandle(
            GUIUtility.GetControlID(FocusType.Passive),
            transforms,
            null,
            midPoint,
            innerRadius,
            radius);
    }

    protected static void OrbitHandle(
        int controlID,
        IEnumerable<Transform> transforms,
        Vector2 midPoint,
        float innerRadius,
        float radius) {
        OrbitHandle(
            controlID,
            transforms,
            null,
            midPoint,
            innerRadius,
            radius);
    }

    protected static void OrbitHandle(
        int controlID,
        IEnumerable<Transform> transforms,
        IEnumerable<Transform> rightTransforms,
        Vector2 midPoint,
        float innerRadius,
        float radius) {
        RadiusHandleData radiusHandleData = StateObject.Get<RadiusHandleData>(controlID);
        if (GUIUtility.hotControl == controlID) {
            Vector2 mousePosition = Event.current.mousePosition;
            Vector2 previousPosition = radiusHandleData.previousPosition;

            Vector2 worldMousePosition = EditorHelpers.HandlePointToWorld(mousePosition);
            Vector2 worldPreviousPosition = EditorHelpers.HandlePointToWorld(previousPosition);
            Vector2 center = radiusHandleData.midPoint;

            Vector2 towardsMouse = worldMousePosition - center;
            Vector2 towardsPrevious = worldPreviousPosition - center;

            float previousAngle = Helpers2D.GetAngle(towardsPrevious);
            float newAngle = Helpers2D.GetAngle(towardsMouse);

            float mainAngleDiff = newAngle - previousAngle;
            if (mainAngleDiff > 180) {
                mainAngleDiff -= 360;
            }
            if (mainAngleDiff < -180) {
                mainAngleDiff += 360;
            }

            radiusHandleData.accum += mainAngleDiff;
            radiusHandleData.previousPosition = Event.current.mousePosition;

            var snappedAccum = Handles.SnapValue(radiusHandleData.accum, 45);

            var originalAngle =
                Helpers2D.GetAngle(
                    (Vector2)
                        EditorHelpers.HandlePointToWorld(radiusHandleData.originalPosition) -
                    center);

            foreach (KeyValuePair<Transform, TransformInfo> kvp in radiusHandleData.originalTransformInfos) {
                Transform transform = kvp.Key;
                TransformInfo info = kvp.Value;

                Vector2 currentPosition = transform.position;
                if (Vector3.Distance(currentPosition, center) <= JointHelpers.AnchorEpsilon) {
                    float currentObjectAngle = transform.rotation.eulerAngles.z;
                    float originalObjectAngle = info.rot.eulerAngles.z;

                    float snappedAngle = originalObjectAngle + snappedAccum;

                    if (Mathf.Abs(snappedAngle - currentObjectAngle) > Mathf.Epsilon) {
                        GUI.changed = true;
                        EditorHelpers.RecordUndo("Orbit", transform, transform.gameObject);
                        Quaternion rotationDelta = Helpers2D.Rotate(snappedAccum);

                        transform.rotation = rotationDelta*info.rot;
                    }
                }
                else {
                    Vector2 originalPosition = info.pos;
                    Quaternion originalRotation = info.rot;

                    if (Mathf.Abs(snappedAccum) > Mathf.Epsilon) {
                        GUI.changed = true;
                        EditorHelpers.RecordUndo("Orbit", transform, transform.gameObject);

                        Quaternion rotationDelta = Helpers2D.Rotate(snappedAccum);
                        Vector2 originalTowardsObject = (originalPosition - center);

                        transform.position = (Vector3) (center + (Vector2) ((rotationDelta)*originalTowardsObject))
                                             + new Vector3(0, 0, info.pos.z);

                        transform.rotation = rotationDelta*originalRotation;
                    }
                }
            }

            switch (Event.current.type) {
                case EventType.mouseMove:
                case EventType.mouseDrag: {
                    Event.current.Use();
                    break;
                }
                case EventType.mouseUp: {
                    if (Event.current.button == radiusHandleData.button) {
                        Event.current.Use();
                        GUIUtility.hotControl = 0;
                    }
                    break;
                }
                case EventType.repaint:
                    int spins = Mathf.FloorToInt(Mathf.Abs(snappedAccum/360));
                    float arcAngle = snappedAccum%360;

                    if (spins > 0) {
//                            using (new DisposableHandleColor(Color.yellow))
//                            {
//                                Handles.DrawSolidDisc(midPoint, Vector3.forward, radius * Mathf.Abs(snappedAccum / 360) * 0.125f);
//                            }
                        float completion = 360 - arcAngle;
                        using (
                            new HandleColor(spins%2 == 1
                                ? editorSettings.radiusColor
                                : editorSettings.alternateRadiusColor)) {
                            Handles.DrawSolidArc(midPoint, Vector3.forward,
                                Helpers2D.GetDirection(originalAngle),
                                -completion, radius);
                        }
                    }
                    using (
                        new HandleColor(spins%2 == 0
                            ? editorSettings.radiusColor
                            : editorSettings.alternateRadiusColor)) {
                        Handles.DrawSolidArc(midPoint, Vector3.forward,
                            Helpers2D.GetDirection(originalAngle),
                            arcAngle, radius);
                    }

                    if (editorSettings.drawRadiusRings) {
                        using (new HandleColor()) {
                            for (int i = 0; i <= spins; i++) {
                                Handles.color = i%2 == 0
                                    ? editorSettings.radiusColor
                                    : editorSettings.alternateRadiusColor;
                                Handles.DrawWireDisc(midPoint, Vector3.forward, radius + radius*i*0.125f);
                            }
                            Handles.color = Color.Lerp(Handles.color, spins%2 == 1
                                ? editorSettings.radiusColor
                                : editorSettings.alternateRadiusColor,
                                Mathf.Abs(arcAngle/360));
                            Handles.DrawWireDisc(midPoint, Vector3.forward, radius +
                                                                            radius*(spins + Mathf.Abs(arcAngle/360))*
                                                                            0.125f);
                        }
                    }
                    break;
            }
        }

        float distanceFromInner = HandleUtility.DistanceToCircle(midPoint, innerRadius);
        float distanceFromOuter = HandleUtility.DistanceToCircle(midPoint, radius);
        bool inZone = distanceFromInner > 0 && distanceFromOuter <= JointHelpers.AnchorEpsilon;

        bool showCursor = (inZone && GUIUtility.hotControl == 0) || controlID == GUIUtility.hotControl;


//        if (showCursor != _showCursor && Event.current.type == EventType.mouseMove) {
//            _showCursor = showCursor;
//            Event.current.Use();
//        }

        SetCursor(showCursor, MouseCursor.RotateArrow, controlID);

        if (showCursor && Event.current.type == EventType.repaint) {
            using (new HandleColor(editorSettings.previewRadiusColor)) {
                Handles.DrawSolidDisc(midPoint, Vector3.forward, innerRadius);
                Handles.DrawWireDisc(midPoint, Vector3.forward, radius);
            }
        }

        if (inZone) {
            HandleUtility.AddControl(controlID, distanceFromOuter);
            switch (Event.current.type) {
                case EventType.mouseDown:
                    if (GUIUtility.hotControl == 0 &&
                        (Event.current.button == 0 || (Event.current.button == 1 && (rightTransforms != null)))) {
                        GUIUtility.hotControl = controlID;
                        radiusHandleData.originalTransformInfos = new Dictionary<Transform, TransformInfo>();
                        radiusHandleData.button = Event.current.button;
                        radiusHandleData.midPoint = midPoint;
                        if (Event.current.button == 0) {
                            foreach (Transform transform in transforms) {
                                radiusHandleData.originalTransformInfos.Add(transform,
                                    new TransformInfo(transform.position,
                                        transform.rotation));
                            }
                        }
                        else if (rightTransforms != null) {
                            foreach (Transform transform in rightTransforms) {
                                radiusHandleData.originalTransformInfos.Add(transform,
                                    new TransformInfo(transform.position,
                                        transform.rotation));
                            }
                        }
                        radiusHandleData.previousPosition = Event.current.mousePosition;
                        radiusHandleData.originalPosition = Event.current.mousePosition;
                        radiusHandleData.accum = 0;
                        Event.current.Use();
                    }
                    break;
            }
        }
    }

    private static readonly GUIContent JointGizmosContent =
        new GUIContent("Joint Gizmos", "Toggles the display of advanced joint gizmos on the scene GUI.");


    protected void ToggleShowGizmos(SerializedObject serializedSettings) {
        EditorGUI.BeginChangeCheck();
        SerializedProperty showJointGizmos = serializedSettings.FindProperty("showJointGizmos");
        EditorGUILayout.PropertyField(showJointGizmos, JointGizmosContent);
        if (EditorGUI.EndChangeCheck()) {
            serializedSettings.ApplyModifiedProperties();
        }
    }

    public override sealed void OnInspectorGUI() {
        EditorGUI.BeginChangeCheck();
        bool foldout = EditorGUILayout.Foldout(editorSettings.foldout, "Advanced Options");
        if (EditorGUI.EndChangeCheck()) {
            //no need to record undo here.
            editorSettings.foldout = foldout;
            EditorUtility.SetDirty(editorSettings);
        }
        if (foldout) {
            using (new Indent()) {
                List<Object> allSettings =
                    targets.Cast<Joint2D>().Select(joint2D => SettingsHelper.GetOrCreate(joint2D))
                        .Where(jointSettings => jointSettings != null).Cast<Object>().ToList();

                SerializedObject serializedSettings = new SerializedObject(allSettings.ToArray());

                SerializedProperty showJointGizmos = serializedSettings.FindProperty("showJointGizmos");
                bool enabled = GUI.enabled &&
                               (showJointGizmos.boolValue || showJointGizmos.hasMultipleDifferentValues);
                EditorGUILayout.LabelField("Display:");
                using (new Indent()) {
                    ToggleShowGizmos(serializedSettings);
                    InspectorDisplayGUI(enabled);
                }
                if (WantsLocking() || WantsOffset()) {
                    EditorGUILayout.LabelField("Features:");
                    using (new Indent()) {
                        if (WantsLocking()) {
                            ToggleAnchorLock(serializedSettings);
                        }
                        if (WantsOffset()) {
                            AlterOffsets(serializedSettings, enabled);
                        }
                    }
                }
            }
        }
        InspectorGUI(foldout);
    }

    private static readonly GUIContent MainOffsetContent = new GUIContent("Main Offset",
        "This offset is used to display the current angle of the object that owns the joint.");

    private static readonly GUIContent ConnectedOffsetContent = new GUIContent("Connected Offset",
        "This offset is used to display the current angle of the object that is connected by joint.");

    private void AlterOffsets(SerializedObject serializedSettings, bool enabled) {
        EditorGUI.BeginChangeCheck();

        using (new GUIEnabled(enabled)) {
            SerializedProperty mainBodyOffset = serializedSettings.FindProperty("mainBodyOffset");
            EditorGUILayout.PropertyField(mainBodyOffset, MainOffsetContent);

            SerializedProperty connectedBodyOffset = serializedSettings.FindProperty("connectedBodyOffset");
            EditorGUILayout.PropertyField(connectedBodyOffset, ConnectedOffsetContent);
        }

        if (EditorGUI.EndChangeCheck()) {
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
            foreach (AnchoredJoint2D joint2D in targets) {
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

        EditorHelpers.RecordUndo("toggle anchor locking", jointSettings);
        jointSettings.lockAnchors = wantsLock;
        EditorUtility.SetDirty(jointSettings);

        if (wantsLock) {
            EditorHelpers.RecordUndo("toggle anchor locking", joint2D);
            ReAlignAnchors(joint2D, alignmentBias);
            EditorUtility.SetDirty(joint2D);
        }
    }

    protected virtual void InspectorDisplayGUI(bool enabled) {}

    protected virtual void InspectorGUI(bool foldout) {
        DrawDefaultInspector();
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

        private readonly Dictionary<String, int> controlIDs = new Dictionary<string, int>();

        public AnchorInfo(IEnumerable<String> controlNames) {
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
        List<Vector2> snapPositions = new List<Vector2> {
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

        EditorGUI.BeginChangeCheck();
        Vector2 position = AnchorSlider(sliderID, editorSettings.anchorScale, snapPositions, bias, joint2D);

        bool changed = false;
        if (EditorGUI.EndChangeCheck()) {
            EditorHelpers.RecordUndo("Anchor Move", joint2D);
            changed = true;

            position = AlterDragResult(sliderID, position, joint2D, bias,
                HandleUtility.GetHandleSize(position)*editorSettings.anchorScale*0.25f);

            JointHelpers.SetAnchorPosition(joint2D, position, bias);
        }
        return changed;
    }

    protected bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo,
        IEnumerable<Vector2> otherAnchors, JointHelpers.AnchorBias bias) {
        int lockID = anchorInfo.GetControlID("lock");

        bool changed = false;
        if (WantsLocking() && Event.current.shift) {
            bool farAway = 
                Vector2.Distance(
                    JointHelpers.GetMainAnchorPosition(joint2D),
                    GetWantedAnchorPosition(joint2D, JointHelpers.AnchorBias.Main)
                    ) > AnchorEpsilon || Vector2.Distance(
                        JointHelpers.GetConnectedAnchorPosition(joint2D),
                        GetWantedAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected)
                        ) > AnchorEpsilon;


            if (SettingsHelper.GetOrCreate(joint2D).lockAnchors && (bias == JointHelpers.AnchorBias.Either || !farAway) )
            {
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
        worldOffset = Handles.Slider2D(anchorInfo.GetControlID("offset"),
            worldOffset, Vector3.forward, Vector3.up, Vector3.right,
            handleSize,
            Handles.SphereCap, Vector2.zero);
        if (EditorGUI.EndChangeCheck()) {
            if (Vector2.Distance(worldOffset, transform.position) < handleSize*0.25f) {
                worldOffset = transform.position;
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
        if (playing) {
            //            anchorLock = false;
        }

        Vector2 worldAnchor = JointHelpers.GetMainAnchorPosition(joint2D);
        Vector2 worldConnectedAnchor = JointHelpers.GetConnectedAnchorPosition(joint2D);

        bool overlapping = Vector2.Distance(worldConnectedAnchor, worldAnchor) <= AnchorEpsilon;

        bool changed = false;

        AnchorInfo main = new AnchorInfo(controlNames),
            connected = new AnchorInfo(controlNames),
            locked = new AnchorInfo(controlNames);

        if (WantsOffset()) {
            DrawOffset(joint2D, main, JointHelpers.AnchorBias.Main);
            DrawOffset(joint2D, connected, JointHelpers.AnchorBias.Connected);
        }
        List<Vector2> otherAnchors = GetAllAnchorsInSelection(joint2D);

        if (anchorLock && DragBothAnchorsWhenLocked()) {
            if (playing || overlapping) {
                if (SingleAnchorGUI(joint2D, locked, otherAnchors, JointHelpers.AnchorBias.Either)) {
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
        }
        else {
            if (SingleAnchorGUI(joint2D, connected, otherAnchors, JointHelpers.AnchorBias.Connected)) {
                changed = true;
                if (anchorLock) {
                    ReAlignAnchors(joint2D, JointHelpers.AnchorBias.Connected);
                }
            }

            float handleSize = HandleUtility.GetHandleSize(worldConnectedAnchor)*editorSettings.orbitRangeScale;
            float distance = HandleUtility.DistanceToCircle(worldConnectedAnchor, handleSize*.5f);
            bool hovering = distance <= AnchorEpsilon;
            if (hovering) {
                connected.ignoreHover = true;
            }

            if (SingleAnchorGUI(joint2D, main, otherAnchors, JointHelpers.AnchorBias.Main)) {
                changed = true;
                if (anchorLock) {
                    ReAlignAnchors(joint2D, JointHelpers.AnchorBias.Main);
                }
            }
        }

        if (changed) {
            EditorUtility.SetDirty(joint2D);
        }
    }

    protected virtual bool DragBothAnchorsWhenLocked() {
        return true;
    }

    public void OnSceneGUI() {
        AnchoredJoint2D joint2D = target as AnchoredJoint2D;
        if (joint2D == null || !joint2D.enabled) {
            return;
        }
        Joint2DSettings settings = SettingsHelper.GetOrCreate(joint2D);
        if (settings && !settings.showJointGizmos) {
            return;
        }

        AnchorGUI(joint2D);

        if (_lastCursor != null && Event.current.type == EventType.repaint) {
            EditorHelpers.SetEditorCursor(_lastCursor.Value);
        }
    }

    public void OnEnable() {
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
    }

    public void OnDisable() {
        if (WantsLocking()) {
            // ReSharper disable DelegateSubtraction
            SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
            // ReSharper restore DelegateSubtraction
        }
    }


    private static Vector2 GetTargetPositionWithOffset(AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias) {
        Transform transform = JointHelpers.GetTargetTransform(joint2D, bias);
        Vector2 offset = SettingsHelper.GetOrCreate(joint2D).GetOffset(bias);

        Vector2 worldOffset = offset;
        if (transform != null) {
            worldOffset = Helpers2D.TransformVector(transform, worldOffset);
        }

        return JointHelpers.GetTargetPosition(joint2D, bias) + worldOffset;
    }


    private readonly Dictionary<AnchoredJoint2D, PositionInfo> positions =
        new Dictionary<AnchoredJoint2D, PositionInfo>();

    public void OnPreSceneGUI() {
        if (WantsLocking()) {
            //gets called before gizmos!
            AnchoredJoint2D joint2D = target as AnchoredJoint2D;
            if (joint2D) {
                positions[joint2D] = new PositionInfo(joint2D);
            }
        }
    }

    public void OnSceneGUIDelegate(SceneView sceneView) {
        //gets called after gizmos!


        foreach (AnchoredJoint2D joint2D in targets.Cast<AnchoredJoint2D>()) {
            if (joint2D == null || !joint2D.enabled) {
                continue;
            }
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

public class AnchorSliderState {
    public Vector2 mouseOffset;
}