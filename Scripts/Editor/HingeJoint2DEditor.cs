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

    private const string ANCHOR_TEXTURE = "2djointeditor_anchor";
    private const string LOCKED_ANCHOR_TEXTURE = "2djointeditor_locked_anchor";

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
            material = new Material(Shader.Find("Unlit/Transparent")) {
                hideFlags = HideFlags.HideAndDontSave
            };
            material.SetTexture(0, texture);
        }
        materials[textureName] = material;
        return material;
    }

    public void OnEnable() {
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        foreach (HingeJoint2D hingeJoint2D in Selection.GetFiltered(typeof (HingeJoint2D), SelectionMode.TopLevel)) {
            positionCache.Add(hingeJoint2D, hingeJoint2D.transform.position);
        }
    }

    public void OnDisable() {
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
    }

    private void OnUndoRedoPerformed()
    {
        foreach (HingeJoint2D hingeJoint2D in Selection.GetFiltered(typeof(HingeJoint2D), SelectionMode.TopLevel))
        {
            positionCache[hingeJoint2D] = hingeJoint2D.transform.position;
        }
    }

    private Vector2 AnchorSlider(Vector2 position, float handleScale, out bool changed,
                                 IEnumerable<Vector2> snapPositions, float angle = 0, bool locked = false) {
        float handleSize = HandleUtility.GetHandleSize(position)*handleScale;
        EditorGUI.BeginChangeCheck();
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Vector2 result;
        if (locked) {
            result = Handles.Slider2D(position, Vector3.forward, rotation*Vector3.up, rotation*Vector3.right, handleSize,
                                      DrawLockedAnchor, 0f);
        }
        else {
            result = Handles.Slider2D(position, Vector3.forward, rotation*Vector3.up, rotation*Vector3.right, handleSize,
                                      DrawAnchor, 0f);
        }
        changed = EditorGUI.EndChangeCheck();
        if (changed && snapPositions != null) {
            foreach (Vector2 snapPosition in snapPositions) {
                if (Vector2.Distance(result, snapPosition) < handleSize*0.5f) {
                    result = snapPosition;
                    break;
                }
            }
        }

        return result;
    }


    private void DrawLockedAnchor(int controlID, Vector3 position, Quaternion rotation, float size) {
        Material lockedAnchorMaterial = GetMaterial(LOCKED_ANCHOR_TEXTURE);
        if (lockedAnchorMaterial != null) {
            DrawMaterial(lockedAnchorMaterial, position, rotation, size);
        }
        else {
            Handles.SphereCap(controlID, position, rotation, size);
        }
    }

    private static void DrawMaterial(Material material, Vector3 position, Quaternion rotation, float size) {
        float doubleSize = size*1.5f;
        Vector3 up = rotation*Vector3.up;
        Vector3 right = rotation*Vector3.right;
        Vector3 bottomLeft = position - up*doubleSize*.5f - right*doubleSize*.5f;
        material.SetPass(0);
        GL.Begin(GL.QUADS);
        GL.TexCoord2(0, 0);
        GL.Vertex(bottomLeft);
        GL.TexCoord2(0, 1);
        GL.Vertex(bottomLeft + up*doubleSize);
        GL.TexCoord2(1, 1);
        GL.Vertex(bottomLeft + up*doubleSize + right*doubleSize);
        GL.TexCoord2(1, 0);
        GL.Vertex(bottomLeft + right*doubleSize);
        GL.End();
    }

    private void DrawAnchor(int controlID, Vector3 position, Quaternion rotation, float size) {
        Material anchorMaterial = GetMaterial(ANCHOR_TEXTURE);
        if (anchorMaterial != null) {
            DrawMaterial(anchorMaterial, position, rotation, size);
        }
        else {
            Handles.SphereCap(controlID, position, rotation, size);
        }
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
        Object[] selectedHingeJoints = Selection.GetFiltered(typeof (HingeJoint2D), SelectionMode.Deep);
        if (selectedHingeJoints.Length == 0) {
            return;
        }

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
                using (new DisposableHandleColor(Color.green)) {
                    Handles.RectangleCap(0, bounds.center, Quaternion.identity, bounds.size.magnitude*0.5f);
                }

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
            foreach (HingeJoint2D otherHingeJoint in selectedHingeJoints) {
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
        HingeJoint2DSettings hingeSettings = hingeJoint2D.gameObject.GetComponent<HingeJoint2DSettings>();

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

        if (anchorLock) {
            if (Vector2.Distance(worldConnectedAnchor, worldAnchor) <= ANCHOR_EPSILON) {
                using (new DisposableHandleColor(Color.red)) {
                    bool anchorChanged;

                    List<Vector2> snapPositions =
                        new List<Vector2>(new[] {transformPosition, connectedTransformPosition});
                    snapPositions.AddRange(otherAnchors);

                    Vector2 newWorldConnectedAnchor = AnchorSlider(worldConnectedAnchor, 0.5f, out anchorChanged,
                                                                   snapPositions, 45, true);

                    if (anchorChanged) {
                        worldConnectedAnchor = newWorldConnectedAnchor;
                        RecordUndo("Connected Anchor Move", hingeJoint2D);
                        changed = true;
                        hingeJoint2D.connectedAnchor = InverseTransform2DPoint(connectedTransform, worldConnectedAnchor);
                        hingeJoint2D.anchor = InverseTransform2DPoint(transform, worldAnchor = worldConnectedAnchor);
                    }
                }
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

                Vector2 newWorldAnchor = AnchorSlider(worldAnchor, 0.5f, out anchorChanged, snapPositions);

                if (anchorChanged) {
                    worldAnchor = newWorldAnchor;
                    RecordUndo("Anchor Move", hingeJoint2D);
                    changed = true;
                    hingeJoint2D.anchor = InverseTransform2DPoint(transform, worldAnchor);
                }
            }

            using (new DisposableHandleColor(Color.green)) {
                bool anchorChanged;

                List<Vector2> snapPositions = new List<Vector2>(new[] {transformPosition, connectedTransformPosition});
                if (snapToOtherAnchor) {
                    snapPositions.Add(worldAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                Vector2 newWorldConnectedAnchor = AnchorSlider(worldConnectedAnchor, 0.5f, out anchorChanged,
                                                               snapPositions, 45);
                if (anchorChanged) {
                    worldConnectedAnchor = newWorldConnectedAnchor;
                    RecordUndo("Connected Anchor Move", hingeJoint2D);
                    changed = true;
                    hingeJoint2D.connectedAnchor = InverseTransform2DPoint(connectedTransform, worldConnectedAnchor);
                }
            }
        }


        Vector2 midPoint = (worldConnectedAnchor + worldAnchor)*0.5f;
        Vector2 otherTransformPosition = connectedTransformPosition;
        using (new DisposableHandleColor(Color.green)) {
            if (connectedBody.isKinematic) {
                Handles.DrawLine(otherTransformPosition, worldConnectedAnchor);
            }
            else if (DrawExtraGizmos(connectedTransform, worldConnectedAnchor)) {
                changed = true;
            }
        }

        using (new DisposableHandleColor(Color.red)) {
            if (transform.rigidbody2D.isKinematic) {
                Handles.DrawLine(transformPosition, worldAnchor);
            }
            else if (DrawExtraGizmos(transform, worldAnchor)) {
                changed = true;
            }
        }

        if (Vector2.Distance(worldConnectedAnchor, worldAnchor) > ANCHOR_EPSILON) {
            bool wantsAlignment = changed && anchorLock;
            bool manual = false;
            if (!wantsAlignment && anchorLock)
            {
                using (new DisposableHandleGUI()) {
                    float handleSize = 50;
                    Vector2 screenPoint = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(midPoint);
                    screenPoint.y = Screen.height - screenPoint.y;
                    Rect buttonPosition = new Rect(screenPoint.x - handleSize*1.6f/2f, screenPoint.y - handleSize,
                                                   handleSize*1.6f, handleSize);

                    manual = GUI.Button(buttonPosition, "Re-align");
                }
            }
            if (wantsAlignment || manual) {
                changed = true;
                string undoMessage = manual ? "anchor re-alignment" : null;
                RecordUndo(undoMessage, hingeJoint2D);
                ReAlignAnchors(hingeJoint2D);
            }
            else {
                using (new DisposableHandleColor(Color.green)) {
                    Handles.DrawLine(worldConnectedAnchor, midPoint);
                    Handles.ArrowCap(0, worldConnectedAnchor,
                                     Quaternion.FromToRotation(Vector3.forward, midPoint - worldConnectedAnchor),
                                     Vector2.Distance(midPoint, worldConnectedAnchor)*0.5f);
                }
                using (new DisposableHandleColor(Color.red)) {
                    Handles.DrawLine(worldAnchor, midPoint);
                    Handles.ArrowCap(0, worldAnchor, Quaternion.FromToRotation(Vector3.forward, midPoint - worldAnchor),
                                     Vector2.Distance(midPoint, worldAnchor)*0.5f);
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
                                               Vector2.right, radius * .5f, Handles.ArrowCap, Vector2.zero);        

        if (EditorGUI.EndChangeCheck()) {
            //go along the radius

            Vector2 towardsNewPosition = newPosition - midPoint;
            towardsNewPosition = towardsNewPosition.normalized * radius;

            float originalAngle = Mathf.Rad2Deg * Mathf.Atan2(towardsObject.y, towardsObject.x);
            float newAngle = Mathf.Rad2Deg * Mathf.Atan2(towardsNewPosition.y, towardsNewPosition.x);


            float snappedAngle = Handles.SnapValue(newAngle, 45);
            if (Math.Abs(snappedAngle-originalAngle) > ANCHOR_EPSILON) {
                RecordUndo("Orbit", transform, transform.gameObject);
                var angleDiff = snappedAngle - originalAngle;//finalRotation.eulerAngles.z;
                Quaternion rotationDelta = Quaternion.AngleAxis(angleDiff, Vector3.forward);

                transform.position = ((Vector3)midPoint + ((rotationDelta) * towardsObject) * radius) + new Vector3(0, 0, transform.position.z);
                transform.rotation *= rotationDelta;

                EditorUtility.SetDirty(transform.gameObject);
                EditorUtility.SetDirty(transform);
            }
        }
    }

    private bool DrawWorldAnchorHandles(HingeJoint2D hingeJoint2D, List<Vector2> otherAnchors) {
        bool changed = false;
        HingeJoint2DSettings hingeSettings = hingeJoint2D.gameObject.GetComponent<HingeJoint2DSettings>();

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

        if (anchorLock) {
            using (new DisposableHandleColor(Color.red)) {
                List<Vector2> snapPositions = new List<Vector2> {transformPosition};

                snapPositions.AddRange(otherAnchors);

                bool anchorChanged;
                connectedAnchor = AnchorSlider(connectedAnchor, 0.5f, out anchorChanged, snapPositions, 0, true);
                if (anchorChanged) {
                    RecordUndo("Anchor Move", hingeJoint2D);
                    changed = true;
                    hingeJoint2D.anchor = InverseTransform2DPoint(transform, worldAnchor = connectedAnchor);
                    hingeJoint2D.connectedAnchor = connectedAnchor;
                }
            }
        }
        else {
            using (new DisposableHandleColor(Color.red)) {
                List<Vector2> snapPositions = new List<Vector2> {transformPosition};
                if (snapToOtherAnchor) {
                    snapPositions.Add(connectedAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                bool anchorChanged;
                worldAnchor = AnchorSlider(worldAnchor, 0.5f, out anchorChanged, snapPositions);
                if (anchorChanged) {
                    RecordUndo("Anchor Move", hingeJoint2D);
                    changed = true;
                    hingeJoint2D.anchor = InverseTransform2DPoint(transform, worldAnchor);
                }
            }

            using (new DisposableHandleColor(Color.green)) {
                List<Vector2> snapPositions = new List<Vector2> {transformPosition};

                if (snapToOtherAnchor) {
                    snapPositions.Add(worldAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                bool anchorChanged;
                connectedAnchor = AnchorSlider(connectedAnchor, 0.5f, out anchorChanged, snapPositions, 45);
                if (anchorChanged) {
                    RecordUndo("Connected Anchor Move", hingeJoint2D);
                    changed = true;
                    hingeJoint2D.connectedAnchor = connectedAnchor;
                }
            }
        }

        bool orbited = false;
        using (new DisposableHandleColor(Color.red)) {
            if (DrawExtraGizmos(transform, worldAnchor)) {
                changed = true;
                orbited = true;
            }
        }


        if (Vector2.Distance(connectedAnchor, worldAnchor) > ANCHOR_EPSILON) {
            if (changed && anchorLock) {
                Vector2 lastPosition;
                bool positionCached = positionCache.TryGetValue(hingeJoint2D, out lastPosition);
                if (!positionCached) {
                    positionCache.Add(hingeJoint2D, transformPosition);
                }
                else {
                    RecordUndo(null, hingeJoint2D);
                    ReAlignAnchors(hingeJoint2D, orbited ? AnchorBias.Main : AnchorBias.Connected);
                }
            }
            else {
                using (new DisposableHandleColor(Color.green)) {
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
        foreach (HingeJoint2D hingeJoint2D in Selection.GetFiltered(typeof (HingeJoint2D), SelectionMode.TopLevel)) {
            HingeJoint2DSettings hingeSettings = hingeJoint2D.gameObject.GetComponent<HingeJoint2DSettings>();
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
                    choice = EditorUtility.DisplayDialogComplex("Enable Anchor Lock",
                                                                    "Which anchor would you like to lock to?",
                                                                    "Main",
                                                                    "Connected",
                                                                    "Cancel");

                    if (choice == 2) {
                        wantsContinue = false;
                    }
                }
                if(wantsContinue)
                foreach (
                    HingeJoint2D hingeJoint2D in Selection.GetFiltered(typeof (HingeJoint2D), SelectionMode.TopLevel)) {
                    HingeJoint2DSettings hingeSettings = hingeJoint2D.gameObject.GetComponent<HingeJoint2DSettings>() ??
                                                         Undo.AddComponent<HingeJoint2DSettings>(hingeJoint2D.gameObject);

                    RecordUndo("toggle anchor locking", hingeSettings);
                    hingeSettings.lockAnchors = lockAnchors.Value;
                    EditorUtility.SetDirty(hingeSettings);

                    if (lockAnchors.Value) {
                        AnchorBias bias = choice==0 ? AnchorBias.Main : AnchorBias.Connected;

                        RecordUndo("toggle anchor locking", hingeJoint2D);
                        ReAlignAnchors(hingeJoint2D, bias);
                        EditorUtility.SetDirty(hingeJoint2D);
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
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) {
            Vector2 curAnchor = serializedObject.FindProperty("m_Anchor").vector2Value;
            Vector2 curConnectedAnchor = serializedObject.FindProperty("m_ConnectedAnchor").vector2Value;

            
            bool mainAnchorChanged = Vector2.Distance(curAnchor, originalAnchor) > ANCHOR_EPSILON;
            bool connectedAnchorChanged = Vector2.Distance(curConnectedAnchor, originalConnectedAnchor) > ANCHOR_EPSILON;

            if (mainAnchorChanged || connectedAnchorChanged )
            {
                AnchorBias bias;

                if (mainAnchorChanged) {
                    if (connectedAnchorChanged) {
                        bias = AnchorBias.Either;
                    }
                    else {
                        bias = AnchorBias.Main;
                    }
                }
                else {
                    bias = AnchorBias.Connected;
                }
                foreach ( HingeJoint2D hingeJoint2D
                    in Selection.GetFiltered(typeof(HingeJoint2D), SelectionMode.TopLevel))
                {
                    HingeJoint2DSettings hingeSettings = hingeJoint2D.gameObject.GetComponent<HingeJoint2DSettings>();
                    bool wantsLock = hingeSettings != null && hingeSettings.lockAnchors;

                    if(wantsLock) {
                        RecordUndo(null, hingeJoint2D);
                        ReAlignAnchors(hingeJoint2D, bias);
                    }
                }
            }
        }
        
        if (EditorGUI.EndChangeCheck()) {
            Debug.Log("!!!");
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
                && ( bias == AnchorBias.Connected
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
