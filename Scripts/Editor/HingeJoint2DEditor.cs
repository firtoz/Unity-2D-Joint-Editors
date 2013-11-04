using System;
using System.Collections.Generic;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof (HingeJoint2D))]
[CanEditMultipleObjects]
public class HingeJoint2DEditor : Editor {
    private readonly Dictionary<HingeJoint2D, Vector2> positionCache = new Dictionary<HingeJoint2D, Vector2>();

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
            positionCache.Add(hingeJoint2D, GetAnchorPosition(hingeJoint2D));
        }
    }

    public void OnDisable() {
// ReSharper disable DelegateSubtraction
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
// ReSharper restore DelegateSubtraction
    }

    private void OnUndoRedoPerformed() {
        foreach (HingeJoint2D hingeJoint2D in targets) {
            positionCache[hingeJoint2D] = GetAnchorPosition(hingeJoint2D);
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
        Vector3 towardsTarget = (targetPosition - position).normalized;

        float originalAngle = Mathf.Rad2Deg*Mathf.Atan2(towardsTarget.y, towardsTarget.x);

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
        //if (selectedHingeJoints.Count == 0) {
        //	return;
        //}

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
        }
    }

    private static void DrawExtraGizmos(Transform transform, Vector2 midPoint) {
        Vector2 startPosition = transform.position;

        //Vector2 left = (Quaternion.AngleAxis(90, Vector3.forward)*(midPoint - startPosition))*0.5f;

        Handles.DrawWireDisc(midPoint, Vector3.forward, Vector2.Distance(midPoint, startPosition));
        Handles.DrawLine(startPosition, midPoint);

//        EditorGUI.BeginChangeCheck();

        RadiusHandle(transform, midPoint, HandleUtility.GetHandleSize(midPoint)*jointSettings.anchorScale*0.5f,
                     HandleUtility.GetHandleSize(midPoint)*jointSettings.orbitRangeScale*0.5f);
//            RadiusHandle(transform, -left, midPoint, radius);
//        if (EditorGUI.EndChangeCheck()) {
////            Debug.Log("Radius handled!");
//        }
    }

    private class RadiusHandleData {
        public Vector2 previousPosition;
        public Vector2 originalPosition;
    }

    private static void RadiusHandle(Transform transform, Vector2 midPoint, float innerRadius, float radius) {
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        RadiusHandleData radiusHandleData = StateObject.Get<RadiusHandleData>(controlID);
        if (GUIUtility.hotControl == controlID) {
            Vector2 startPosition = transform.position;
            Vector2 towardsObject = (startPosition - midPoint);

            float originalObjectAngle = GetAngle(towardsObject);

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

                    float originalAngle = Mathf.Rad2Deg*Mathf.Atan2(towardsPrevious.y, towardsPrevious.x);
                    float newAngle = GetAngle(towardsMouse);

                    float objectAngle = originalObjectAngle + newAngle - originalAngle;

                    float snappedAngle = Handles.SnapValue(objectAngle, 45);
                    if (Math.Abs(snappedAngle - originalObjectAngle) > JointEditorSettings.AnchorEpsilon) {
                        RecordUndo("Orbit", transform, transform.gameObject);
                        var angleDiff = snappedAngle - originalObjectAngle;
                        Quaternion rotationDelta = Quaternion.AngleAxis(angleDiff, Vector3.forward);


                        transform.position = ((Vector3) midPoint + ((rotationDelta)*towardsObject)) +
                                             new Vector3(0, 0, transform.position.z);
                        transform.rotation *= rotationDelta;

                        radiusHandleData.previousPosition = Event.current.mousePosition;

                        GUI.changed = true;
                    }
                }
                    break;
                case EventType.mouseUp: {
                    Event.current.Use();
                    GUIUtility.hotControl = 0;
                }
                    break;
                case EventType.repaint:

                    Vector2 firstMousePosition = radiusHandleData.originalPosition;
                    Vector2 currentPosition = transform.position;

                    float firstAngle = GetAngle(firstMousePosition - midPoint);
                    float currentAngle = GetAngle(currentPosition - midPoint);

                    using (new DisposableHandleColor(jointSettings.radiusColor)) {
                        Handles.DrawLine(midPoint,
                                         (Vector3) midPoint +
                                         (Quaternion.AngleAxis(firstAngle, Vector3.forward))*Vector3.right*radius);
                        Handles.DrawLine(midPoint,
                                         (Vector3) midPoint +
                                         (Quaternion.AngleAxis(currentAngle, Vector3.forward))*Vector3.right*radius);
                        Handles.DrawSolidArc(midPoint, Vector3.forward,
                                             (Quaternion.AngleAxis(firstAngle, Vector3.forward))*Vector3.right,
                                             (360 + currentAngle - firstAngle)%360, innerRadius);
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
                        radiusHandleData.originalPosition = transform.position;
                        radiusHandleData.previousPosition = Event.current.mousePosition;
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
                SetWorldAnchorPosition(hingeJoint2D, positionCache[hingeJoint2D] = worldAnchor = worldConnectedAnchor);
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
                    SetWorldAnchorPosition(hingeJoint2D, positionCache[hingeJoint2D] = worldAnchor);
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
                SetWorldConnectedAnchorPosition(hingeJoint2D,
                                                positionCache[hingeJoint2D] = worldConnectedAnchor = worldAnchor);
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

                SetWorldAnchorPosition(hingeJoint2D, positionCache[hingeJoint2D] = worldAnchor = worldConnectedAnchor);
                EditorUtility.SetDirty(hingeSettings);
            }
        }

        EditorGUI.BeginChangeCheck();
        using (new DisposableHandleColor(Color.red)) {
            DrawExtraGizmos(transform, worldAnchor);
        }
        if (EditorGUI.EndChangeCheck()) {
//            positionCache[hingeJoint2D] = GetAnchorPosition(hingeJoint2D);
        }

        if (hingeJoint2D.connectedBody) {
            using (new DisposableHandleColor(Color.green)) {
                DrawExtraGizmos(hingeJoint2D.connectedBody.transform, worldConnectedAnchor);
            }
        }


        if (Vector2.Distance(worldConnectedAnchor, worldAnchor) > JointEditorSettings.AnchorEpsilon) {
            using (new DisposableHandleColor(Color.cyan)) {
                Handles.DrawLine(worldAnchor, worldConnectedAnchor);
            }
        }

        Vector3 position = GetAnchorPosition(hingeJoint2D);
        if (anchorLock && Vector3.Distance(positionCache[hingeJoint2D], position) > JointEditorSettings.AnchorEpsilon) {
//            Debug.Log("movement!");
            positionCache[hingeJoint2D] = position;
            RecordUndo(null, hingeJoint2D);
            SetWorldConnectedAnchorPosition(hingeJoint2D, position);
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
                        RecordUndo(null, hingeJoint2D);
                        ReAlignAnchors(hingeJoint2D, bias);
                        EditorUtility.SetDirty(hingeJoint2D);
                    }
                }
            }

            if (connectedRigidBody != serializedObject.FindProperty("m_ConnectedRigidBody").objectReferenceValue) {
                foreach (HingeJoint2D hingeJoint2D in targets) {
                    RecordUndo(null, hingeJoint2D);
                    SetWorldConnectedAnchorPosition(hingeJoint2D, worldConnectedAnchors[hingeJoint2D]);
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
