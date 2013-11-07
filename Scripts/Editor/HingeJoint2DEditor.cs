using System;
using System.Collections.Generic;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof (HingeJoint2D))]
[CanEditMultipleObjects]
public class HingeJoint2DEditor : Editor {
    private struct PositionInfo {
        public PositionInfo(HingeJoint2D hingeJoint2D) {
            main = GetAnchorPosition(hingeJoint2D);
            connected = GetConnectedAnchorPosition(hingeJoint2D);
        }

        private readonly Vector2 main;
        private readonly Vector2 connected;

        public bool Changed(HingeJoint2D hingeJoint2D, out AnchorBias bias) {
            bool result = false;
            bias = AnchorBias.Either;
            if (Vector3.Distance(main, GetAnchorPosition(hingeJoint2D)) > JointEditorSettings.AnchorEpsilon) {
                result = true;
                bias = AnchorBias.Main;
            }
            if (Vector3.Distance(connected, GetConnectedAnchorPosition(hingeJoint2D)) >
                JointEditorSettings.AnchorEpsilon) {
                if (!result) {
                    bias = AnchorBias.Connected;
                    result = true;
                }
                else {
                    bias = AnchorBias.Either;
                }
            }
            return result;
        }
    }

    private readonly Dictionary<HingeJoint2D, PositionInfo> positionCache = new Dictionary<HingeJoint2D, PositionInfo>();

#if RECURSIVE_EDITING
    
    private static readonly Dictionary<HingeJoint2D, HingeJoint2DEditor> Editors =
        new Dictionary<HingeJoint2D, HingeJoint2DEditor>();

    private readonly Dictionary<HingeJoint2D, List<HingeJoint2DEditor>> tempEditors =
        new Dictionary<HingeJoint2D, List<HingeJoint2DEditor>>();
#endif

    private static readonly AssetUtils Utils = new AssetUtils("2DJointEditors/Data");
    private static JointEditorSettings _jointSettings;

    private static JointEditorSettings jointSettings {
        get {
            return _jointSettings ?? (_jointSettings = Utils.GetOrCreateAsset<JointEditorSettings>("settings.asset"));
        }
    }

    private readonly Dictionary<Texture2D, Material> materials = new Dictionary<Texture2D, Material>();

    private static void RecordUndo(String action, params Object[] objects) {
        Undo.RecordObjects(objects, action);
    }

    private Material GetMaterial(Texture2D texture, Texture2D hotTexture = null) {
        if (materials.ContainsKey(texture)) {
            return materials[texture];
        }

        Material material = null;
        if (texture != null) {
            material = new Material(Shader.Find("Joint Editor")) {
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetTexture(0, texture);
            if (hotTexture != null && material.HasProperty("_HotTex")) {
                material.SetTexture("_HotTex", hotTexture);
            }
            materials[texture] = material;
        }
        return material;
    }


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

    private Vector2 AnchorSlider(Vector2 position, float handleScale, out bool changed,
                                 IEnumerable<Vector2> snapPositions, AnchorBias bias, bool locked,
                                 int? givenControlID, HingeJoint2D joint) {
        float handleSize = HandleUtility.GetHandleSize(position)*handleScale;
        int controlID = givenControlID ?? GUIUtility.GetControlID(FocusType.Native);
        EditorGUI.BeginChangeCheck();
        Vector2 targetPosition;
        if (bias == AnchorBias.Connected) {
            if (joint.connectedBody) {
                targetPosition = joint.connectedBody.transform.position;
            }
            else {
                targetPosition = position;
            }
        }
        else {
            targetPosition = joint.gameObject.transform.position;
        }

        float originalAngle;

        if (Vector3.Distance(targetPosition, position) > JointEditorSettings.AnchorEpsilon) {
            Vector3 towardsTarget = (targetPosition - position).normalized;

            originalAngle = GetAngle(towardsTarget);
        }
        else {
            originalAngle = joint.gameObject.transform.rotation.eulerAngles.z;
        }

        if (GUIUtility.hotControl == controlID) {
            using (
                DisposableMaterialDrawer drawer =
                    new DisposableMaterialDrawer(GetMaterial(jointSettings.hotHingeTexture),
                                                 Quaternion.AngleAxis(originalAngle,
                                                                      Vector3.forward),
                                                 jointSettings.anchorDisplayScale)) {
                drawer.DrawSquare(position, Quaternion.identity, handleSize);
            }
        }

        //Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Vector2 result;

        if (locked) {
            using (
                DisposableMaterialDrawer drawer =
                    new DisposableMaterialDrawer(GetMaterial(jointSettings.lockedHingeTexture),
                                                 Quaternion.AngleAxis(originalAngle,
                                                                      Vector3.forward),
                                                 jointSettings.anchorDisplayScale)) {
                result = Handles.Slider2D(controlID, position, Vector3.forward, Vector3.up, Vector3.right, handleSize,
                                          drawer.DrawSquare, Vector2.zero);
            }
        }
        else {
            Material material =
                GetMaterial(bias == AnchorBias.Main
                                ? jointSettings.mainHingeTexture
                                : jointSettings.connectedHingeTexture);


            using (
                DisposableMaterialDrawer drawer = new DisposableMaterialDrawer(material,
                                                                               Quaternion.AngleAxis(originalAngle,
                                                                                                    Vector3.forward),
                                                                               jointSettings.anchorDisplayScale)) {
                result = Handles.Slider2D(controlID, position, Vector3.forward, Vector3.up, Vector3.right, handleSize,
                                          drawer.DrawSquare, Vector2.zero);
            }
        }
        changed = EditorGUI.EndChangeCheck();
        if (changed && snapPositions != null) {
            foreach (Vector2 snapPosition in snapPositions) {
                if (Vector2.Distance(result, snapPosition) < handleSize*0.25f) {
                    result = snapPosition;
                    break;
                }
            }
        }

        return result;
    }

    private static Vector2 Transform2DPoint(Transform transform, Vector2 point) {
        Vector2 scaledPoint = Vector2.Scale(point, transform.lossyScale);
        float angle = transform.rotation.eulerAngles.z;
        Vector2 rotatedScaledPoint = Quaternion.AngleAxis(angle, Vector3.forward)*scaledPoint;
        Vector2 translatedRotatedScaledPoint = (Vector2) transform.position + rotatedScaledPoint;
        return translatedRotatedScaledPoint;
    }

    private static Vector2 InverseTransform2DPoint(Transform transform, Vector2 translatedRotatedScaledPoint) {
        Vector2 rotatedScaledPoint = translatedRotatedScaledPoint - (Vector2) transform.position;
        float angle = transform.rotation.eulerAngles.z;
        Vector2 scaledPoint = Quaternion.AngleAxis(-angle, Vector3.forward)*rotatedScaledPoint;
        Vector2 point = Vector2.Scale(scaledPoint, new Vector2(1/transform.lossyScale.x, 1/transform.lossyScale.y));
        return point;
    }

    private static Vector2 GetAnchorPosition(HingeJoint2D joint2D) {
        return Transform2DPoint(joint2D.transform, joint2D.anchor);
    }

    private static Vector2 GetConnectedAnchorPosition(HingeJoint2D joint2D) {
        if (joint2D.connectedBody) {
            return Transform2DPoint(joint2D.connectedBody.transform, joint2D.connectedAnchor);
        }
        return joint2D.connectedAnchor;
    }

    public void OnSceneGUI() {
        List<HingeJoint2D> selectedHingeJoints = new List<HingeJoint2D> {target as HingeJoint2D};

        if (Event.current.type == EventType.keyDown) {
            if ((Event.current.character + "").ToLower().Equals("f") || Event.current.keyCode == KeyCode.F) { //frame hotkey pressed
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

                foreach (HingeJoint2D hingeJoint2D in selectedHingeJoints) {
                    Vector2 midPoint = (GetAnchorPosition(hingeJoint2D) + GetConnectedAnchorPosition(hingeJoint2D))*.5f;
                    float distance = Vector2.Distance(midPoint, hingeJoint2D.transform.position);
                    Bounds hingeBounds = new Bounds(midPoint, Vector2.one*distance*2);
                    bounds.Encapsulate(hingeBounds);
                }
                using (new DisposableHandleColor(Color.blue)) {
                    Handles.RectangleCap(0, bounds.center, Quaternion.identity, bounds.size.magnitude*0.5f);
                }

                SceneView.lastActiveSceneView.LookAt(bounds.center, Quaternion.identity, bounds.size.magnitude);
                Event.current.Use();
            }
        }
        foreach (HingeJoint2D hingeJoint2D in selectedHingeJoints) {
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

            bool changed = false;
//            if (hingeJoint2D.connectedBody) {
//                if (DrawConnectedBodyAnchorHandles(hingeJoint2D, otherAnchors)) {
//                    changed = true;
//                }
//            }
//            else 
            {
                if (DrawAnchorHandles(hingeJoint2D, otherAnchors)) {
                    changed = true;
                }
            }
            if (changed) {
                EditorUtility.SetDirty(hingeJoint2D);
            }
            
#if RECURSIVE_EDITING
            foreach (HingeJoint2DEditor tempEditor in tempEditors[hingeJoint2D]) {
                tempEditor.OnSceneGUI();
            }
#endif
        }
    }

    private static void DrawExtraGizmos(IEnumerable<Transform> transforms, Vector2 midPoint) {
        RadiusHandle(transforms, midPoint, HandleUtility.GetHandleSize(midPoint)*jointSettings.anchorScale*0.5f,
                     HandleUtility.GetHandleSize(midPoint)*jointSettings.orbitRangeScale*0.5f);
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
    }

    private static void RadiusHandle(IEnumerable<Transform> transforms, Vector2 midPoint, float innerRadius,
                                     float radius) {
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        RadiusHandleData radiusHandleData = StateObject.Get<RadiusHandleData>(controlID);
        if (GUIUtility.hotControl == controlID) {
            switch (Event.current.type) {
                case EventType.mouseMove:
                case EventType.mouseDrag: {
                    Event.current.Use();
                    Vector2 mousePosition = Event.current.mousePosition;
                    Vector2 previousPosition = radiusHandleData.previousPosition;

                    Vector2 worldMousePosition = HandleUtility.GUIPointToWorldRay(mousePosition).origin;
                    Vector2 worldPreviousPosition = HandleUtility.GUIPointToWorldRay(previousPosition).origin;

                    Vector2 towardsMouse = worldMousePosition - midPoint;
                    Vector2 towardsPrevious = worldPreviousPosition - midPoint;

                    float originalAngle = GetAngle(towardsPrevious);
                    float newAngle = GetAngle(towardsMouse);

                    float mainAngleDiff = newAngle - originalAngle;
                    if (mainAngleDiff > 180) {
                        mainAngleDiff -= 360;
                    }
                    if (mainAngleDiff < -180) {
                        mainAngleDiff += 360;
                    }

                    radiusHandleData.accum += mainAngleDiff;
                    radiusHandleData.previousPosition = Event.current.mousePosition;

                    int transformCount = radiusHandleData.originalTransformInfos.Count;
                    foreach (KeyValuePair<Transform, TransformInfo> kvp in radiusHandleData.originalTransformInfos) {
                        Transform transform = kvp.Key;
                        TransformInfo info = kvp.Value;

                        Vector2 currentPosition = transform.position;
                        if (Vector3.Distance(currentPosition, midPoint) <= JointEditorSettings.AnchorEpsilon) {
                            float currentObjectAngle = transform.rotation.eulerAngles.z;
                            float originalObjectAngle = info.rot.eulerAngles.z;
                            float snappedAngle;

                            if (transformCount == 1) {
                                float wantedObjectAngle = originalObjectAngle + radiusHandleData.accum;
                                snappedAngle = Handles.SnapValue(wantedObjectAngle, 45);
                            }
                            else {
                                snappedAngle = originalObjectAngle + Handles.SnapValue(radiusHandleData.accum, 45);
                            }

                            if (Math.Abs(snappedAngle - originalObjectAngle) > Mathf.Epsilon) {
                                RecordUndo("Orbit", transform, transform.gameObject);
                                Quaternion rotationDelta = Quaternion.AngleAxis(snappedAngle - currentObjectAngle,
                                                                                Vector3.forward);

                                transform.rotation *= rotationDelta;
                            }
                        }
                        else {
                            Vector2 originalPosition = info.pos;

                            Vector2 currentTowardsObject = (currentPosition - midPoint);
                            Vector2 originalTowardsObject = (originalPosition - midPoint);

                            float currentObjectAngle = GetAngle(currentTowardsObject);
                            float originalObjectAngle = GetAngle(originalTowardsObject);

                            float snappedAngle;

                            if (transformCount == 1) {
                                float wantedObjectAngle = originalObjectAngle + radiusHandleData.accum;
                                snappedAngle = Handles.SnapValue(wantedObjectAngle, 45);
                            }
                            else {
                                snappedAngle = originalObjectAngle + Handles.SnapValue(radiusHandleData.accum, 45);
                            }

                            if (Math.Abs(snappedAngle - currentObjectAngle) > Mathf.Epsilon) {
                                RecordUndo("Orbit", transform, transform.gameObject);
                                var angleDiff = snappedAngle - currentObjectAngle;
                                Quaternion rotationDelta = Quaternion.AngleAxis(angleDiff, Vector3.forward);

                                transform.position = ((Vector3) midPoint + ((rotationDelta)*currentTowardsObject)) +
                                                     new Vector3(0, 0, transform.position.z);
                                transform.rotation *= rotationDelta;
                            }
                        }
                    }

                    GUI.changed = true;
                }
                    break;
                case EventType.mouseUp: {
                    Event.current.Use();
                    GUIUtility.hotControl = 0;
                }
                    break;
                case EventType.repaint:

                    if (radiusHandleData.originalTransformInfos.Count > 0) {
                        float originalAngle =
                            GetAngle(
                                     (Vector2)
                                     HandleUtility.GUIPointToWorldRay(radiusHandleData.originalPosition).origin -
                                     midPoint);
                        float snappedAngle = Handles.SnapValue(radiusHandleData.accum, 45);

                        using (new DisposableHandleColor(jointSettings.radiusColor)) {
                            Handles.DrawSolidArc(midPoint, Vector3.forward,
                                                 (Quaternion.AngleAxis(originalAngle, Vector3.forward))*Vector3.right,
                                                 snappedAngle, radius);
                        }

                        foreach (KeyValuePair<Transform, TransformInfo> kvp in radiusHandleData.originalTransformInfos) {
                            Transform transform = kvp.Key;
                            TransformInfo info = kvp.Value;

                            Vector2 originalTransformPosition = info.pos;

                            Vector2 startPosition = transform.position;
                            Vector2 towardsObject = (startPosition - midPoint);

                            float firstAngle = GetAngle(originalTransformPosition - midPoint);

                            using (new DisposableHandleColor(jointSettings.radiusColor)) {
                                Handles.DrawWireArc(midPoint, Vector3.forward,
                                                    (Quaternion.AngleAxis(firstAngle, Vector3.forward))*Vector3.right,
                                                    snappedAngle,
                                                    towardsObject.magnitude - HandleUtility.GetHandleSize(midPoint)*0.1f);
                                Handles.DrawWireArc(midPoint, Vector3.forward,
                                                    (Quaternion.AngleAxis(firstAngle, Vector3.forward))*Vector3.right,
                                                    snappedAngle,
                                                    towardsObject.magnitude + HandleUtility.GetHandleSize(midPoint)*0.1f);
                            }
                        }
                    }
                    else {
                        foreach (KeyValuePair<Transform, TransformInfo> kvp in radiusHandleData.originalTransformInfos) {
                            Transform transform = kvp.Key;
                            TransformInfo info = kvp.Value;

                            Vector2 originalTransformPosition = info.pos;

                            Vector2 startPosition = transform.position;
                            Vector2 towardsObject = (startPosition - midPoint);

                            float firstAngle = GetAngle(originalTransformPosition - midPoint);

                            using (new DisposableHandleColor(jointSettings.radiusColor)) {
                                float snappedAngle = Handles.SnapValue(firstAngle + radiusHandleData.accum, 45);

                                Handles.DrawSolidArc(midPoint, Vector3.forward,
                                                     (Quaternion.AngleAxis(firstAngle, Vector3.forward))*Vector3.right,
                                                     snappedAngle - firstAngle, towardsObject.magnitude);
                            }
                        }
                    }
                    break;
            }
        }

        float distanceFromInner = HandleUtility.DistanceToCircle(midPoint, innerRadius);
        float distanceFromOuter = HandleUtility.DistanceToCircle(midPoint, radius);
        bool inZone = distanceFromInner > 0 && distanceFromOuter <= JointEditorSettings.AnchorEpsilon;
        if ((inZone && GUIUtility.hotControl == 0) || controlID == GUIUtility.hotControl) {
            GUIHelpers.SetEditorCursor(MouseCursor.RotateArrow, controlID);
            using (new DisposableHandleColor(jointSettings.previewRadiusColor)) {
                Handles.DrawSolidDisc(midPoint, Vector3.forward, innerRadius);
                Handles.DrawWireDisc(midPoint, Vector3.forward, radius);
            }
            HandleUtility.Repaint();
        }


        if (inZone) {
            HandleUtility.AddControl(controlID, distanceFromOuter);
            switch (Event.current.type) {
                case EventType.mouseDown:
                    if (Event.current.button == 0 && GUIUtility.hotControl == 0) {
                        GUIUtility.hotControl = controlID;
                        radiusHandleData.originalTransformInfos = new Dictionary<Transform, TransformInfo>();
                        foreach (Transform transform in transforms) {
                            radiusHandleData.originalTransformInfos.Add(transform,
                                                                        new TransformInfo(transform.position,
                                                                                          transform.rotation));
                        }
                        radiusHandleData.previousPosition = Event.current.mousePosition;
                        radiusHandleData.originalPosition = Event.current.mousePosition;
                        radiusHandleData.accum = 0;
                        Event.current.Use();
                    }
                    break;
            }
        }
//        EditorGUI.BeginChangeCheck();
//        Vector2 startPosition = transform.position;
//        Vector2 towardsObject = (startPosition - midPoint).normalized;
//
//
//        Vector2 newPosition = Handles.Slider2D(controlID, midPoint + towardsObject*radius*.5f, direction, Vector2.up,
//                                               Vector2.right, radius*.5f, Handles.ArrowCap, Vector2.zero);
//
//        if (EditorGUI.EndChangeCheck()) {
//            //go along the radius
//
//            Vector2 towardsNewPosition = newPosition - midPoint;
//            towardsNewPosition = towardsNewPosition.normalized*radius;
//
//            float originalAngle = Mathf.Rad2Deg*Mathf.Atan2(towardsObject.y, towardsObject.x);
//            float newAngle = Mathf.Rad2Deg*Mathf.Atan2(towardsNewPosition.y, towardsNewPosition.x);
//
//
//            float snappedAngle = Handles.SnapValue(newAngle, 45);
//            if (Math.Abs(snappedAngle - originalAngle) > ANCHOR_EPSILON) {
//                RecordUndo("Orbit", transform, transform.gameObject);
//                var angleDiff = snappedAngle - originalAngle; //finalRotation.eulerAngles.z;
//                Quaternion rotationDelta = Quaternion.AngleAxis(angleDiff, Vector3.forward);
//
//                transform.position = ((Vector3) midPoint + ((rotationDelta)*towardsObject)*radius) +
//                                     new Vector3(0, 0, transform.position.z);
//                transform.rotation *= rotationDelta;
//
//                EditorUtility.SetDirty(transform.gameObject);
//                EditorUtility.SetDirty(transform);
//            }
//        }
    }


    private static float GetAngle(Vector2 vector) {
        return Mathf.Rad2Deg*Mathf.Atan2(vector.y, vector.x);
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
                                                snapPositions, 0, true, connectedControlID, hingeJoint2D);
            if (anchorChanged) {
                RecordUndo("Anchor Move", hingeJoint2D);
                changed = true;
                SetWorldConnectedAnchorPosition(hingeJoint2D, worldConnectedAnchor);
                SetWorldAnchorPosition(hingeJoint2D, worldAnchor = worldConnectedAnchor);
                positionCache[hingeJoint2D] = new PositionInfo(hingeJoint2D);
            }

            if (ToggleLockButton(lockControlID, worldConnectedAnchor,
                                 GetMaterial(jointSettings.lockButtonTexture, jointSettings.unlockButtonTexture))) {
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
                                 GetMaterial(jointSettings.unlockButtonTexture, jointSettings.lockButtonTexture),
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
                                 GetMaterial(jointSettings.unlockButtonTexture, jointSettings.lockButtonTexture),
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


        AnchorBias bias;
        if (anchorLock && positionCache[hingeJoint2D].Changed(hingeJoint2D, out bias)) {
            RecordUndo("...", hingeJoint2D);
            positionCache[hingeJoint2D] = new PositionInfo(hingeJoint2D);

            ReAlignAnchors(hingeJoint2D, bias);
            EditorUtility.SetDirty(hingeJoint2D);
        }
        return changed;
    }

    private static void SetWorldAnchorPosition(HingeJoint2D hingeJoint2D, Vector2 worldAnchor) {
        hingeJoint2D.anchor = InverseTransform2DPoint(hingeJoint2D.transform, worldAnchor);
    }

    private static void SetWorldConnectedAnchorPosition(HingeJoint2D hingeJoint2D, Vector2 worldConnectedAnchor) {
        if (hingeJoint2D.connectedBody) {
            hingeJoint2D.connectedAnchor = InverseTransform2DPoint(hingeJoint2D.connectedBody.transform,
                                                                   worldConnectedAnchor);
        }
        else {
            hingeJoint2D.connectedAnchor = worldConnectedAnchor;
        }
    }

    private static bool ToggleLockButton(int controlID, Vector2 center, Material material, bool force = false) {
        bool result = false;

        Vector2 centerGUIPos = HandleUtility.WorldToGUIPoint(center);

        Vector2 lockPos = HandleUtility.GUIPointToWorldRay(centerGUIPos).origin;
        bool acceptEvents = force || Event.current.shift;

        Color color = Color.white;
        color.a = acceptEvents ? 1f : 0f;

        using (new DisposableMaterialColor(material, color)) {
            if (!acceptEvents && GUIUtility.hotControl == controlID) {
                GUIUtility.hotControl = 0;
                Event.current.Use();
                HandleUtility.Repaint();
            }
            if (acceptEvents && GUIHelpers.CustomHandleButton(controlID, lockPos,
                                                              HandleUtility.GetHandleSize(lockPos)*
                                                              jointSettings.lockButtonScale,
                                                              material)) {
                result = true;
            }
        }
        return result;
    }

    private enum AnchorBias {
        Main,
        Connected,
        Either
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
