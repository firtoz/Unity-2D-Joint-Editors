using System;
using System.Collections.Generic;
using GUIHelpers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof (HingeJoint2D))]
[CanEditMultipleObjects]
public class HingeJoint2DEditor : Editor {
    private readonly Dictionary<HingeJoint2D, Vector2> positionCache = new Dictionary<HingeJoint2D, Vector2>();
    private const float ANCHOR_EPSILON = 0.0001f;

    private const string CONNECTED_HINGE_TEXTURE = "2d_joint_editor_hinge_connected";
    private const string MAIN_HINGE_TEXTURE = "2d_joint_editor_hinge_main";
    private const string LOCKED_HINGE_TEXTURE = "2d_joint_editor_hinge_locked";
    private const string HOT_HINGE_TEXTURE = "2d_joint_editor_hinge_hot";

    private const string LOCK_BUTTON_TEXTURE = "2d_joint_editor_lock_button";
    private const string UNLOCK_BUTTON_TEXTURE = "2d_joint_editor_unlock_button";

    private const float ANCHOR_SCALE = 0.75f;
    private const float ANCHOR_GUI_SCALE = 1.25f;
    private const float LOCK_BUTTON_SCALE = 0.25f;

    private const float ANCHOR_LOCK_BUTTON_MINIMUM_DISTANCE = 32f;
    private const float ANCHOR_LOCK_BUTTON_MAXIMUM_DISTANCE = 128f;
    private const float ANCHOR_LOCK_BUTTON_DISTANCE = 64f;

    private readonly Dictionary<String, Material> materials = new Dictionary<string, Material>();

    private static void RecordUndo(String action, params Object[] objects) {
#pragma warning disable 618
        Undo.RegisterUndo(objects, action);
#pragma warning restore 618
    }

    private Material GetMaterial(String textureName) {
        if (materials.ContainsKey(textureName)) {
            return materials[textureName];
        }

        Material material = null;
        Texture2D texture = Resources.Load<Texture2D>(textureName);
        if (texture != null) {
            material = new Material(Shader.Find("Joint Editor")) {
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetTexture(0, texture);
        }
        materials[textureName] = material;
        return material;
    }

    public void OnEnable() {
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        foreach (HingeJoint2D hingeJoint2D in targets) {
            positionCache.Add(hingeJoint2D, hingeJoint2D.transform.position);
        }
    }

    public void OnDisable() {
// ReSharper disable DelegateSubtraction
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
// ReSharper restore DelegateSubtraction
    }

    private void OnUndoRedoPerformed() {
        foreach (HingeJoint2D hingeJoint2D in targets) {
            positionCache[hingeJoint2D] = hingeJoint2D.transform.position;
        }
    }

    private Vector2 AnchorSlider(Vector2 position, float handleScale, out bool changed,
                                 IEnumerable<Vector2> snapPositions, AnchorBias bias, bool locked = false,
                                 int? givenControlID = null) {
        float handleSize = HandleUtility.GetHandleSize(position)*handleScale;
        int controlID = givenControlID ?? GUIUtility.GetControlID(FocusType.Native);
        EditorGUI.BeginChangeCheck();
        //Debug.Log(GUIUtility.hotControl+" "+controlID);
        if (GUIUtility.hotControl == controlID) {
            GetMaterial(HOT_HINGE_TEXTURE).SetPass(0);
            DrawAnchor(controlID, position, Quaternion.identity, handleSize);
        }
        //Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Vector2 result;
        if (locked) {
            result = Handles.Slider2D(controlID, position, Vector3.forward, Vector3.up, Vector3.right, handleSize,
                                      DrawLockedAnchor, Vector2.zero);
        }
        else {
            Material material = GetMaterial(bias == AnchorBias.Main ? MAIN_HINGE_TEXTURE : CONNECTED_HINGE_TEXTURE);
            material.SetPass(0);
            result = Handles.Slider2D(controlID, position, Vector3.forward, Vector3.up, Vector3.right, handleSize,
                                      DrawAnchor, Vector2.zero);
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


    private void DrawLockedAnchor(int controlID, Vector3 position, Quaternion rotation, float size) {
        Material lockedAnchorMaterial = GetMaterial(LOCKED_HINGE_TEXTURE);
        if (lockedAnchorMaterial != null) {
            lockedAnchorMaterial.SetPass(0);
            DrawAnchor(controlID, position, rotation, size);
        }
        else {
            Handles.SphereCap(controlID, position, rotation, size);
        }
    }

    private static void DrawAnchor(int controlID, Vector3 position, Quaternion rotation, float size) {
        GUIHelpers.GUIHelpers.DrawSquare(position, rotation, size*ANCHOR_GUI_SCALE);
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
            if (hingeJoint2D.connectedBody) {
                if (DrawConnectedBodyAnchorHandles(hingeJoint2D, otherAnchors)) {
                    changed = true;
                }
            }
            else {
                if (DrawWorldAnchorHandles(hingeJoint2D, otherAnchors)) {
                    changed = true;
                }
            }
            if (changed) {
                EditorUtility.SetDirty(hingeJoint2D);
            }
        }
    }

    private bool DrawConnectedBodyAnchorHandles(HingeJoint2D hingeJoint2D, List<Vector2> otherAnchors) {
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
        Vector2 worldAnchor = Transform2DPoint(transform, hingeJoint2D.anchor);
        Rigidbody2D connectedBody = hingeJoint2D.connectedBody;
        Transform connectedTransform = connectedBody.transform;
        Vector2 worldConnectedAnchor = Transform2DPoint(connectedTransform, hingeJoint2D.connectedAnchor);

        Vector2 connectedTransformPosition = connectedTransform.position;

        int mainControlID = GUIUtility.GetControlID(FocusType.Native);
        int connectedControlID = GUIUtility.GetControlID(FocusType.Native);

        if (anchorLock && Vector2.Distance(worldConnectedAnchor, worldAnchor) <= ANCHOR_EPSILON) {
            bool anchorChanged;

            List<Vector2> snapPositions =
                new List<Vector2>(new[] {transformPosition, connectedTransformPosition});
            snapPositions.AddRange(otherAnchors);

            Vector2 newWorldConnectedAnchor = AnchorSlider(worldConnectedAnchor, ANCHOR_SCALE, out anchorChanged,
                                                           snapPositions, AnchorBias.Connected, true, connectedControlID);

            if (anchorChanged) {
                worldConnectedAnchor = newWorldConnectedAnchor;
                RecordUndo("Connected Anchor Move", hingeJoint2D);
                changed = true;
                hingeJoint2D.connectedAnchor = InverseTransform2DPoint(connectedTransform, worldConnectedAnchor);
                hingeJoint2D.anchor = InverseTransform2DPoint(transform, worldAnchor = worldConnectedAnchor);
            }
        }
        else {
            using (new DisposableHandleColor(Color.red)) {
                bool anchorChanged;
                List<Vector2> snapPositions = new List<Vector2>(new[] {transformPosition, connectedTransformPosition});
                if (snapToOtherAnchor) {
                    snapPositions.Add(worldConnectedAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                Vector2 newWorldAnchor = AnchorSlider(worldAnchor, ANCHOR_SCALE, out anchorChanged, snapPositions,
                                                      AnchorBias.Main, anchorLock, mainControlID);

                if (anchorChanged || (anchorLock && GUIUtility.hotControl == mainControlID)) {
                    worldAnchor = newWorldAnchor;
                    RecordUndo("Anchor Move", hingeJoint2D);
                    changed = true;
                    hingeJoint2D.anchor = InverseTransform2DPoint(transform, worldAnchor);

                    if (anchorLock) {
                        hingeJoint2D.connectedAnchor = InverseTransform2DPoint(hingeJoint2D.connectedBody.transform,
                                                                               worldConnectedAnchor = worldAnchor);

                        GUIUtility.hotControl = connectedControlID;
                    }
                }
            }

            using (new DisposableHandleColor(Color.green)) {
                bool anchorChanged;

                List<Vector2> snapPositions = new List<Vector2>(new[] {transformPosition, connectedTransformPosition});
                if (snapToOtherAnchor) {
                    snapPositions.Add(worldAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                Vector2 newWorldConnectedAnchor = AnchorSlider(worldConnectedAnchor, ANCHOR_SCALE, out anchorChanged,
                                                               snapPositions, AnchorBias.Connected, anchorLock,
                                                               connectedControlID);
                if (anchorChanged || (anchorLock && GUIUtility.hotControl == connectedControlID)) {
                    worldConnectedAnchor = newWorldConnectedAnchor;
                    RecordUndo("Connected Anchor Move", hingeJoint2D);
                    changed = true;
                    hingeJoint2D.connectedAnchor = InverseTransform2DPoint(connectedTransform, worldConnectedAnchor);

                    if (anchorLock) {
                        hingeJoint2D.anchor = InverseTransform2DPoint(hingeJoint2D.transform,
                                                                      worldAnchor = worldConnectedAnchor);
                    }
                }
            }
        }


        //Vector2 midPoint = (worldConnectedAnchor + worldAnchor) * 0.5f;
        Vector2 otherTransformPosition = connectedTransformPosition;
        using (new DisposableHandleColor(Color.green)) {
            if (connectedBody.isKinematic) {
                Handles.DrawLine(otherTransformPosition, worldConnectedAnchor);
            }
            else if (DrawExtraGizmos(connectedTransform, worldConnectedAnchor)) {
                //changed = true;
            }
        }

        using (new DisposableHandleColor(Color.red)) {
            if (transform.rigidbody2D.isKinematic) {
                Handles.DrawLine(transformPosition, worldAnchor);
            }
            else if (DrawExtraGizmos(transform, worldAnchor)) {
                //changed = true;
            }
        }

        if (Vector2.Distance(worldConnectedAnchor, worldAnchor) > ANCHOR_EPSILON) {
            bool wantsAlignment = changed && anchorLock;
            if (wantsAlignment) {
                RecordUndo(null, hingeJoint2D);
                ReAlignAnchors(hingeJoint2D);
            }
            else {
                using (new DisposableHandleColor(Color.cyan)) {
                    Handles.DrawLine(worldConnectedAnchor, worldAnchor);
                }
            }
        }

        return changed;
    }

    private static bool DrawExtraGizmos(Transform transform, Vector2 midPoint) {
        Vector2 startPosition = transform.position;

        float radius = Vector2.Distance(midPoint, startPosition);

        Vector2 left = (Quaternion.AngleAxis(90, Vector3.forward)*(midPoint - startPosition))*0.5f;

        Handles.DrawWireDisc(midPoint, Vector3.forward, Vector2.Distance(midPoint, startPosition));
        Handles.DrawLine(startPosition, midPoint);

        bool changed = false;

        if (radius > ANCHOR_EPSILON) {
            EditorGUI.BeginChangeCheck();
            RadiusHandle(transform, left, midPoint, radius);
            RadiusHandle(transform, -left, midPoint, radius);
            if (EditorGUI.EndChangeCheck()) {
                changed = true;
            }
        }

        return changed;
    }

    private static void RadiusHandle(Transform transform, Vector3 direction, Vector2 midPoint, float radius) {
        EditorGUI.BeginChangeCheck();
        Vector2 startPosition = transform.position;
        Vector2 towardsObject = (startPosition - midPoint).normalized;

        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        Vector2 newPosition = Handles.Slider2D(controlID, midPoint + towardsObject*radius*.5f, direction, Vector2.up,
                                               Vector2.right, radius*.5f, Handles.ArrowCap, Vector2.zero);

        if (EditorGUI.EndChangeCheck()) {
            //go along the radius

            Vector2 towardsNewPosition = newPosition - midPoint;
            towardsNewPosition = towardsNewPosition.normalized*radius;

            float originalAngle = Mathf.Rad2Deg*Mathf.Atan2(towardsObject.y, towardsObject.x);
            float newAngle = Mathf.Rad2Deg*Mathf.Atan2(towardsNewPosition.y, towardsNewPosition.x);


            float snappedAngle = Handles.SnapValue(newAngle, 45);
            if (Math.Abs(snappedAngle - originalAngle) > ANCHOR_EPSILON) {
                RecordUndo("Orbit", transform, transform.gameObject);
                var angleDiff = snappedAngle - originalAngle; //finalRotation.eulerAngles.z;
                Quaternion rotationDelta = Quaternion.AngleAxis(angleDiff, Vector3.forward);

                transform.position = ((Vector3) midPoint + ((rotationDelta)*towardsObject)*radius) +
                                     new Vector3(0, 0, transform.position.z);
                transform.rotation *= rotationDelta;

                EditorUtility.SetDirty(transform.gameObject);
                EditorUtility.SetDirty(transform);
            }
        }
    }

    private bool DrawWorldAnchorHandles(HingeJoint2D hingeJoint2D, List<Vector2> otherAnchors) {
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
        Vector2 worldAnchor = Transform2DPoint(transform, hingeJoint2D.anchor);

        Vector2 connectedAnchor = hingeJoint2D.connectedAnchor;

        int mainControlID = GUIUtility.GetControlID(FocusType.Native);
        int connectedControlID = GUIUtility.GetControlID(FocusType.Native);
        int lockControlID = GUIUtility.GetControlID(FocusType.Native);

        if (anchorLock && Vector2.Distance(connectedAnchor, worldAnchor) <= ANCHOR_EPSILON) {
            List<Vector2> snapPositions = new List<Vector2> {transformPosition};

            snapPositions.AddRange(otherAnchors);

            bool anchorChanged;
            connectedAnchor = AnchorSlider(connectedAnchor, ANCHOR_SCALE, out anchorChanged,
                                           snapPositions, 0, true, connectedControlID);
            if (anchorChanged) {
                RecordUndo("Anchor Move", hingeJoint2D);
                changed = true;
                hingeJoint2D.anchor = InverseTransform2DPoint(transform, worldAnchor = connectedAnchor);
                hingeJoint2D.connectedAnchor = connectedAnchor;
            }

            Vector2 anchorGUIPos = HandleUtility.WorldToGUIPoint(connectedAnchor);

            float distanceFromCenter = Vector2.Distance(anchorGUIPos, Event.current.mousePosition);

            if (distanceFromCenter < ANCHOR_LOCK_BUTTON_MAXIMUM_DISTANCE
                && distanceFromCenter > ANCHOR_LOCK_BUTTON_MINIMUM_DISTANCE) {
                Vector2 worldMousePos = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
                Vector2 towardsMouse = worldMousePos - connectedAnchor;
                float angle = Mathf.Rad2Deg*Mathf.Atan2(towardsMouse.y, towardsMouse.x);

                Vector2 lockPos = HandleUtility.GUIPointToWorldRay(
                                                                   anchorGUIPos
                                                                   + (Vector2) (
                                                                                   Quaternion.AngleAxis(-angle,
                                                                                                        Vector3.forward)
                                                                                   *Vector2.right
                                                                                   * ANCHOR_LOCK_BUTTON_DISTANCE
                                                                                   )
                                                                                   ).origin;

                if (GUIHelpers.GUIHelpers.CustomButton(lockControlID, lockPos,
                                                       HandleUtility.GetHandleSize(lockPos)*LOCK_BUTTON_SCALE,
                                                       GetMaterial(UNLOCK_BUTTON_TEXTURE))) {
                    Debug.Log("unlock!");
                }

//                Handles.Label(connectedAnchor,
//                              "" + distanceFromCenter + " | " + (HandleUtility.GetHandleSize(connectedAnchor)*100));
                HandleUtility.Repaint();
            }
//            if (HandleUtility.DistanceToCircle(connectedAnchor, HandleUtility.GetHandleSize(connectedAnchor)*ANCHOR_SCALE) <= 5) {
//                Vector3 worldMousePos = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).origin;
//                Handles.Button(worldMousePos, Quaternion.identity,
//                               HandleUtility.GetHandleSize(connectedAnchor)*0.25f,
//                               HandleUtility.GetHandleSize(connectedAnchor)*0.25f, Handles.CircleCap);
//            }
        }
        else {
            using (new DisposableHandleColor(Color.red)) {
                List<Vector2> snapPositions = new List<Vector2> {transformPosition};
                if (snapToOtherAnchor) {
                    snapPositions.Add(connectedAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                bool anchorChanged;
                worldAnchor = AnchorSlider(worldAnchor, ANCHOR_SCALE, out anchorChanged, snapPositions, AnchorBias.Main,
                                           anchorLock, mainControlID);
                if (anchorChanged || (anchorLock && GUIUtility.hotControl == mainControlID)) {
                    RecordUndo("Anchor Move", hingeJoint2D);
                    changed = true;
                    hingeJoint2D.anchor = InverseTransform2DPoint(transform, worldAnchor);
                    if (anchorLock) {
                        hingeJoint2D.connectedAnchor = connectedAnchor = worldAnchor;
                        GUIUtility.hotControl = connectedControlID;
                    }
                }
            }

            using (new DisposableHandleColor(Color.green)) {
                List<Vector2> snapPositions = new List<Vector2> {transformPosition};

                if (snapToOtherAnchor) {
                    snapPositions.Add(worldAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                bool anchorChanged;
                connectedAnchor = AnchorSlider(connectedAnchor, ANCHOR_SCALE, out anchorChanged, snapPositions,
                                               AnchorBias.Connected, anchorLock, connectedControlID);
                if (anchorChanged || (anchorLock && GUIUtility.hotControl == connectedControlID)) {
                    RecordUndo("Connected Anchor Move", hingeJoint2D);
                    changed = true;
                    hingeJoint2D.connectedAnchor = connectedAnchor;
                    if (anchorLock) {
                        hingeJoint2D.anchor = InverseTransform2DPoint(hingeJoint2D.transform,
                                                                      worldAnchor = connectedAnchor);
                    }
                }
            }
        }

        using (new DisposableHandleColor(Color.red)) {
            DrawExtraGizmos(transform, worldAnchor);
        }


        if (Vector2.Distance(connectedAnchor, worldAnchor) > ANCHOR_EPSILON) {
            if (changed && anchorLock) {
                RecordUndo(null, hingeJoint2D);
                ReAlignAnchors(hingeJoint2D, AnchorBias.Main);
            }
            else {
                using (new DisposableHandleColor(Color.cyan)) {
                    Handles.DrawLine(worldAnchor, connectedAnchor);
                }
            }
        }
        return changed;
    }

    private enum AnchorBias {
        Main,
        Connected,
        Either
    }

    public override void OnInspectorGUI() {
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
                            ANCHOR_EPSILON) {
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


            bool mainAnchorChanged = Vector2.Distance(curAnchor, originalAnchor) > ANCHOR_EPSILON;
            bool connectedAnchorChanged = Vector2.Distance(curConnectedAnchor, originalConnectedAnchor) > ANCHOR_EPSILON;

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
                    }
                }
            }

            if (connectedRigidBody != serializedObject.FindProperty("m_ConnectedRigidBody").objectReferenceValue) {
                Object newBody = serializedObject.FindProperty("m_ConnectedRigidBody").objectReferenceValue;
                if (newBody == null) { //we had a body but now we don't
                    foreach (HingeJoint2D hingeJoint2D in targets) {
                        hingeJoint2D.connectedAnchor = worldConnectedAnchors[hingeJoint2D];
                    }
                }
                else { //we changed the body
                    foreach (HingeJoint2D hingeJoint2D in targets) {
                        hingeJoint2D.connectedAnchor = InverseTransform2DPoint(hingeJoint2D.connectedBody.transform,
                                                                               worldConnectedAnchors[hingeJoint2D]);
                    }
                }
            }
        }

        if (EditorGUI.EndChangeCheck()) {
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
