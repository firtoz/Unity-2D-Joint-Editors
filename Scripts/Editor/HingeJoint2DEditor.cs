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
        foreach (HingeJoint2D hingeJoint2D in Selection.GetFiltered(typeof (HingeJoint2D), SelectionMode.TopLevel)) {
            positionCache.Add(hingeJoint2D, hingeJoint2D.transform.position);
        }
    }

    private Vector2 AnchorSlider(Vector2 position, float handleScale, out bool changed, IEnumerable<Vector2> snapPositions, float angle = 0, bool locked = false) {

        float handleSize = HandleUtility.GetHandleSize(position) * handleScale;
        EditorGUI.BeginChangeCheck();
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Vector2 result;
        if (locked) {
            result = Handles.Slider2D(position, Vector3.forward, rotation*Vector3.up, rotation*Vector3.right, handleSize,
                DrawLockedAnchor, 0f);
        }
        else
        {
            result = Handles.Slider2D(position, Vector3.forward, rotation * Vector3.up, rotation * Vector3.right, handleSize,
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
        if (lockedAnchorMaterial != null)
        {
            DrawMaterial(lockedAnchorMaterial, position, rotation, size);
        }
        else
        {
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

    private static Vector2 GetAnchorPosition(HingeJoint2D joint2D) {
        return joint2D.transform.TransformPoint(joint2D.anchor);
    }

    private static Vector2 GetConnectedAnchorPosition(HingeJoint2D joint2D) {
        if (joint2D.connectedBody) {
            return joint2D.connectedBody.transform.TransformPoint(joint2D.connectedAnchor);
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

                Vector2 otherWorldAnchor = otherHingeJoint.transform.TransformPoint(otherHingeJoint.anchor);
                Vector2 otherConnectedWorldAnchor = otherHingeJoint.connectedBody
                    ? (Vector2) otherHingeJoint.connectedBody.transform.TransformPoint(otherHingeJoint.connectedAnchor)
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
        Vector2 worldAnchor = transform.TransformPoint(hingeJoint2D.anchor);
        Rigidbody2D connectedBody = hingeJoint2D.connectedBody;
        Transform connectedTransform = connectedBody.transform;
        Vector2 worldConnectedAnchor = connectedTransform.TransformPoint(hingeJoint2D.connectedAnchor);

        Vector2 connectedTransformPosition = connectedTransform.position;

        if (anchorLock) {
            using (new DisposableHandleColor(Color.red)) {
                bool anchorChanged;

                List<Vector2> snapPositions = new List<Vector2>(new[] { transformPosition, connectedTransformPosition });
                snapPositions.AddRange(otherAnchors);

                Vector2 newWorldConnectedAnchor = AnchorSlider(worldConnectedAnchor, 0.5f, out anchorChanged, snapPositions, 45, true);

                if (anchorChanged)
                {
                    worldConnectedAnchor = newWorldConnectedAnchor;
                    Undo.RecordObject(hingeJoint2D, "Connected Anchor Move");
                    changed = true;
                    hingeJoint2D.connectedAnchor = connectedTransform.InverseTransformPoint(worldConnectedAnchor);
                    hingeJoint2D.anchor = transform.InverseTransformPoint(worldAnchor = worldConnectedAnchor);
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
                    Undo.RecordObject(hingeJoint2D, "Anchor Move");
                    changed = true;
                    hingeJoint2D.anchor = transform.InverseTransformPoint(worldAnchor);
                }
            }

            using (new DisposableHandleColor(Color.green))
            {
                bool anchorChanged;

                List<Vector2> snapPositions = new List<Vector2>(new[] { transformPosition, connectedTransformPosition });
                if (snapToOtherAnchor)
                {
                    snapPositions.Add(worldAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                Vector2 newWorldConnectedAnchor = AnchorSlider(worldConnectedAnchor, 0.5f, out anchorChanged, snapPositions, 45);
                if (anchorChanged)
                {
                    worldConnectedAnchor = newWorldConnectedAnchor;
                    Undo.RecordObject(hingeJoint2D, "Connected Anchor Move");
                    changed = true;
                    hingeJoint2D.connectedAnchor = connectedTransform.InverseTransformPoint(worldConnectedAnchor);
                }
            }
        }


        Vector2 midPoint = (worldConnectedAnchor + worldAnchor)*0.5f;
        Vector2 otherTransformPosition = connectedTransformPosition;
        using (new DisposableHandleColor(Color.green)) {
            if (connectedBody.isKinematic) {
                Handles.DrawLine(otherTransformPosition, midPoint);
            }
            else {
                DrawExtraGizmos(connectedTransform, midPoint);
            }
        }

        using (new DisposableHandleColor(Color.red)) {
            if (transform.rigidbody2D.isKinematic) {
                Handles.DrawLine(transformPosition, midPoint);
            }
            else {
                DrawExtraGizmos(transform, midPoint);
            }
        }

        if (Vector2.Distance(worldConnectedAnchor, worldAnchor) > ANCHOR_EPSILON) {
            if (anchorLock) {
                if (!transform.rigidbody2D.isKinematic && connectedBody.isKinematic) { //other body is static
                    Undo.RecordObject(hingeJoint2D, "Automated Anchor Move");
                    changed = true;
                    hingeJoint2D.anchor = transform.InverseTransformPoint(worldConnectedAnchor);
                }
                else if (transform.rigidbody2D.isKinematic && !connectedBody.isKinematic) //this body is static
                {
                    Undo.RecordObject(hingeJoint2D, "Automated Anchor Move");
                    changed = true;
                    hingeJoint2D.connectedAnchor = connectedTransform.InverseTransformPoint(worldAnchor);
                }
                else {
                    Vector2 lastPosition;
                    bool positionCached = positionCache.TryGetValue(hingeJoint2D, out lastPosition);
                    if (!positionCached) {
                        positionCache.Add(hingeJoint2D, transformPosition);
                    }
                    else if (Vector2.Distance(lastPosition, transformPosition) > ANCHOR_EPSILON) { //our body is moved
                        Undo.RecordObject(hingeJoint2D, "Automated Anchor Move");
                        changed = true;
                        hingeJoint2D.anchor = transform.InverseTransformPoint(worldConnectedAnchor);
                    }
                    else {
                        Undo.RecordObject(hingeJoint2D, "Automated Anchor Move");
                        changed = true;
                        hingeJoint2D.anchor = transform.InverseTransformPoint(midPoint);
                        hingeJoint2D.connectedAnchor = connectedTransform.InverseTransformPoint(midPoint);
                    }
                }
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

    private static void DrawExtraGizmos(Transform transform, Vector2 midPoint) {
        Vector2 startPosition = transform.position;

        float radius = Vector2.Distance(midPoint, startPosition);
        float handleSize = radius*0.5f;

        Vector2 left = (Quaternion.AngleAxis(90, Vector3.forward)*(midPoint - startPosition))*0.5f;

        Handles.DrawWireDisc(midPoint, Vector3.forward, Vector2.Distance(midPoint, startPosition));
        Handles.DrawLine(startPosition, midPoint);

        if (radius > ANCHOR_EPSILON) {
            RadiusHandle(transform, left, handleSize, midPoint, radius);
            RadiusHandle(transform, -left, handleSize, midPoint, radius);
        }
    }

    private static void RadiusHandle(Transform transform, Vector3 direction, float handleSize, Vector2 midPoint, float radius) {
        EditorGUI.BeginChangeCheck();
        Vector2 startPosition = transform.position;
        Vector2 towardsCenter = startPosition - midPoint;

        Vector2 newPosition = Handles.Slider2D(startPosition, direction, Vector2.up, Vector2.right, handleSize, Handles.ArrowCap, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            //go along the radius
            Vector2 offset = newPosition - midPoint;
            offset = offset.normalized * radius;
            startPosition = midPoint + offset;

            Undo.RecordObject(transform.gameObject, "Moved Along Hinge Radius");

            transform.position = new Vector3(startPosition.x, startPosition.y, transform.position.z);
            transform.rotation *= Quaternion.FromToRotation(towardsCenter, startPosition - midPoint);

            EditorUtility.SetDirty(transform.gameObject);
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
        Vector2 worldAnchor = transform.TransformPoint(hingeJoint2D.anchor);

        Vector2 connectedAnchor = hingeJoint2D.connectedAnchor;

        if (anchorLock)
        {
            using (new DisposableHandleColor(Color.red))
            {
                List<Vector2> snapPositions = new List<Vector2> { transformPosition };

                snapPositions.AddRange(otherAnchors);

                bool anchorChanged;
                worldAnchor = AnchorSlider(worldAnchor, 0.5f, out anchorChanged, snapPositions);
                if (anchorChanged)
                {
                    Undo.RecordObject(hingeJoint2D, "Anchor Move");
                    changed = true;
                    hingeJoint2D.anchor = transform.InverseTransformPoint(worldAnchor);
                    hingeJoint2D.connectedAnchor = worldAnchor;
                }
            }
        }
        else {
            using (new DisposableHandleColor(Color.red))
            {
                List<Vector2> snapPositions = new List<Vector2> { transformPosition };
                if (snapToOtherAnchor)
                {
                    snapPositions.Add(connectedAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                bool anchorChanged;
                worldAnchor = AnchorSlider(worldAnchor, 0.5f, out anchorChanged, snapPositions);
                if (anchorChanged)
                {
                    Undo.RecordObject(hingeJoint2D, "Anchor Move");
                    changed = true;
                    hingeJoint2D.anchor = transform.InverseTransformPoint(worldAnchor);
                }
            }

            using (new DisposableHandleColor(Color.green))
            {
                List<Vector2> snapPositions = new List<Vector2> { transformPosition };

                if (snapToOtherAnchor)
                {
                    snapPositions.Add(worldAnchor);
                }

                snapPositions.AddRange(otherAnchors);

                bool anchorChanged;
                connectedAnchor = AnchorSlider(connectedAnchor, 0.5f, out anchorChanged, snapPositions, 45);
                if (anchorChanged)
                {
                    Undo.RecordObject(hingeJoint2D, "Connected Anchor Move");
                    changed = true;
                    hingeJoint2D.connectedAnchor = connectedAnchor;
                }
            }
        }

        using (new DisposableHandleColor(Color.red)) {
            DrawExtraGizmos(transform, connectedAnchor);
        }


        if (Vector2.Distance(connectedAnchor, worldAnchor) > ANCHOR_EPSILON) {
            if (anchorLock) {
                Vector2 lastPosition;
                bool positionCached = positionCache.TryGetValue(hingeJoint2D, out lastPosition);
                if (!positionCached) {
                    positionCache.Add(hingeJoint2D, transformPosition);
                }
                else if (Vector2.Distance(lastPosition, transformPosition) > ANCHOR_EPSILON) { //our body is moved
                    Undo.RecordObject(hingeJoint2D, "Automated Anchor Move");
                    changed = true;
                    hingeJoint2D.anchor = transform.InverseTransformPoint(hingeJoint2D.connectedAnchor);
                }
                else {
                    Undo.RecordObject(hingeJoint2D, "Automated Anchor Move");
                    changed = true;
                    hingeJoint2D.connectedAnchor = worldAnchor;
                }
            }
            else {
                using (new DisposableHandleColor(Color.red)) {
                    Handles.DrawLine(worldAnchor, connectedAnchor);
                }
            }
        }
        return changed;
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
                foreach (
                    HingeJoint2D hingeJoint2D in Selection.GetFiltered(typeof (HingeJoint2D), SelectionMode.TopLevel)) {
                    HingeJoint2DSettings hingeSettings = hingeJoint2D.gameObject.GetComponent<HingeJoint2DSettings>() ??
                                                         Undo.AddComponent<HingeJoint2DSettings>(hingeJoint2D.gameObject);

                    Undo.RecordObject(hingeSettings, "toggle anchor locking");
                    hingeSettings.lockAnchors = lockAnchors.Value;
                    EditorUtility.SetDirty(hingeSettings);

                    if (lockAnchors.Value) {
                        Transform transform = hingeJoint2D.transform;

                        Vector2 worldConnectedAnchor, midPoint;
                        Vector2 connectedAnchor = hingeJoint2D.connectedAnchor;
                        Vector2 worldAnchor = transform.TransformPoint(hingeJoint2D.anchor);

                        if (hingeJoint2D.connectedBody) {
                            Transform connectedTransform = hingeJoint2D.connectedBody.transform;

                            worldConnectedAnchor = connectedTransform.TransformPoint(hingeJoint2D.connectedAnchor);

                            midPoint = (worldConnectedAnchor + worldAnchor)*0.5f;
                        }
                        else {
                            midPoint = worldConnectedAnchor = connectedAnchor;
                        }

                        if (Vector2.Distance(worldConnectedAnchor, worldAnchor) > ANCHOR_EPSILON) {
                            hingeJoint2D.anchor = transform.InverseTransformPoint(midPoint);
                            if (hingeJoint2D.connectedBody) {
                                hingeJoint2D.connectedAnchor =
                                    hingeJoint2D.connectedBody.transform.InverseTransformPoint(midPoint);
                            }
                        }
                    }
                }
            }
        }


        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) {
            //hinge angle changed...
        }
    }
}