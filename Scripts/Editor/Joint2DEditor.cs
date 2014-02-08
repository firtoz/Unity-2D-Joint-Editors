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

public abstract class Joint2DEditor : Editor
{
    protected static readonly AssetUtils Utils = new AssetUtils("2DJointEditors/Data");

    private static JointEditorSettings _editorSettings;

    protected static JointEditorSettings editorSettings
    {
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

    protected static void ReAlignAnchors(AnchoredJoint2D hingeJoint2D,
        JointHelpers.AnchorBias bias = JointHelpers.AnchorBias.Either)
    {
        Transform transform = hingeJoint2D.transform;

        Vector2 connectedAnchor = hingeJoint2D.connectedAnchor;
        Vector2 worldAnchor = Helpers.Transform2DPoint(transform, hingeJoint2D.anchor);

        if (hingeJoint2D.connectedBody)
        {
            Rigidbody2D connectedBody = hingeJoint2D.connectedBody;
            Transform connectedTransform = connectedBody.transform;

            if (bias != JointHelpers.AnchorBias.Main
                && (bias == JointHelpers.AnchorBias.Connected
                    || (!transform.rigidbody2D.isKinematic && connectedBody.isKinematic)))
            {
                //other body is static or there is a bias
                Vector2 worldConnectedAnchor = Helpers.Transform2DPoint(connectedTransform, connectedAnchor);
                hingeJoint2D.anchor = Helpers.InverseTransform2DPoint(transform, worldConnectedAnchor);
            }
            else if (bias == JointHelpers.AnchorBias.Main
                     || (transform.rigidbody2D.isKinematic && !connectedBody.isKinematic))
            {
                //this body is static or there is a bias
                hingeJoint2D.connectedAnchor = Helpers.InverseTransform2DPoint(connectedTransform,
                    worldAnchor);
            }
            else
            {
                Vector2 midPoint = (Helpers.Transform2DPoint(connectedTransform, connectedAnchor) +
                                    worldAnchor) * .5f;
                hingeJoint2D.anchor = Helpers.InverseTransform2DPoint(transform, midPoint);
                hingeJoint2D.connectedAnchor = Helpers.InverseTransform2DPoint(connectedTransform, midPoint);
            }
        }
        else
        {
            if (bias == JointHelpers.AnchorBias.Main)
            {
                hingeJoint2D.connectedAnchor = worldAnchor;
            }
            else
            {
                hingeJoint2D.anchor = Helpers.InverseTransform2DPoint(transform, connectedAnchor);
            }
        }
    }

    protected static Vector2 AnchorSlider(float handleScale, IEnumerable<Vector2> snapPositions,
        JointHelpers.AnchorBias bias, AnchoredJoint2D joint)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Native);
        return AnchorSlider(controlID, handleScale, snapPositions, bias, joint);
    }

    public bool HasFrameBounds()
    {
        AnchoredJoint2D anchoredJoint2D = target as HingeJoint2D;
        if (anchoredJoint2D == null || !anchoredJoint2D.enabled)
        {
            return false;
        }
        return true;
    }

    protected virtual bool WantsLocking()
    {
        return false;
    }

    protected virtual Vector2 GetTargetPosition(AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias) {
        return JointHelpers.GetTargetPosition(joint2D, bias);
    }

    public Bounds OnGetFrameBounds()
    {
        Bounds bounds = Selection.activeGameObject.renderer
            ? Selection.activeGameObject.renderer.bounds
            : new Bounds((Vector2)Selection.activeGameObject.transform.position, Vector2.zero);
        foreach (Transform selectedTransform in Selection.transforms)
        {
            bounds.Encapsulate((Vector2)selectedTransform.position);
        }

        foreach (AnchoredJoint2D hingeJoint2D in targets.Cast<AnchoredJoint2D>())
        {
            Vector2 midPoint = (JointHelpers.GetAnchorPosition(hingeJoint2D) +
                                JointHelpers.GetConnectedAnchorPosition(hingeJoint2D)) * .5f;
            float distance = Vector2.Distance(midPoint,
                GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Main));
            if (hingeJoint2D.connectedBody)
            {
                float connectedDistance = Vector2.Distance(midPoint,
                    GetTargetPosition(hingeJoint2D, JointHelpers.AnchorBias.Connected));
                distance = Mathf.Max(distance, connectedDistance);
            }
            Bounds hingeBounds = new Bounds(midPoint, Vector2.one * distance * 0.5f);
            bounds.Encapsulate(hingeBounds);
        }

        return bounds;
    }

	protected static Vector2 AnchorSlider(int controlID, float handleScale,
        IEnumerable<Vector2> snapPositions, JointHelpers.AnchorBias bias, AnchoredJoint2D joint)
    {
        Vector2 anchorPosition = JointHelpers.GetAnchorPosition(joint, bias);
        float handleSize = HandleUtility.GetHandleSize(anchorPosition)*handleScale;
        EditorGUI.BeginChangeCheck();
        Vector2 targetPosition;
        if (bias == JointHelpers.AnchorBias.Connected)
        {
            if (joint.connectedBody)
            {
                targetPosition = joint.connectedBody.transform.position;
            }
            else
            {
                targetPosition = anchorPosition;
            }
        }
        else
        {
            targetPosition = joint.gameObject.transform.position;
        }

		float originalAngle = JointHelpers.AngleFromAnchor(anchorPosition, targetPosition, joint.gameObject.transform.rotation.eulerAngles.z);

        if (GUIUtility.hotControl == controlID)
        {
            using (
                GUITextureDrawer drawer =
                    new GUITextureDrawer(editorSettings.hotHingeTexture,
                        Helpers.Rotate2D(originalAngle),
                        editorSettings.anchorDisplayScale))
            {
                drawer.DrawSquare(anchorPosition, Quaternion.identity, handleSize);
            }
        }


        float distanceFromInner = HandleUtility.DistanceToCircle(anchorPosition, handleSize*.5f);
        bool inZone = distanceFromInner <= 0;
        if ((inZone && GUIUtility.hotControl == 0) || controlID == GUIUtility.hotControl)
        {
            EditorHelpers.SetEditorCursor(MouseCursor.MoveArrow, controlID);

			using (new HandleColor(editorSettings.previewRadiusColor))
			{
				Handles.DrawSolidDisc(anchorPosition, Vector3.forward, handleSize * .5f);
				Handles.DrawWireDisc(anchorPosition, Vector3.forward, handleSize * .5f);
			}
        }

	    Event current = Event.current;

                    if (HandleUtility.nearestControl == controlID) {
	    switch (current.GetTypeForControl(controlID)) {
	        case EventType.DragPerform:
                foreach (GameObject go in DragAndDrop.objectReferences
                    .Cast<GameObject>()
                    .Where(go => !go.Equals(joint.gameObject) && go.GetComponent<Rigidbody2D>() != null))
                {
                    HingeJoint2DSettings hingeSettings = SettingsHelper.GetOrCreate<HingeJoint2DSettings>(joint);
                    bool wantsLock = hingeSettings.lockAnchors;

                    EditorHelpers.RecordUndo("Drag Onto Anchor", joint);
                    Vector2 connectedBodyPosition = JointHelpers.GetConnectedAnchorPosition(joint);
                    joint.connectedBody = go.GetComponent<Rigidbody2D>();
                    if (wantsLock)
                    {
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
                    .Cast<GameObject>().Any(go => !go.Equals(joint.gameObject) && go.GetComponent<Rigidbody2D>() != null)) {
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

        switch (bias)
        {
            case JointHelpers.AnchorBias.Main:
                sliderTexture = editorSettings.mainHingeTexture;
                break;
            case JointHelpers.AnchorBias.Connected:
                sliderTexture = editorSettings.connectedHingeTexture;
                break;
            case JointHelpers.AnchorBias.Either:
                sliderTexture = editorSettings.lockedHingeTexture;
                break;
            default:
                throw new ArgumentOutOfRangeException("bias");
        }
        using (
            GUITextureDrawer drawer =
                new GUITextureDrawer(sliderTexture,
					Helpers.Rotate2D(originalAngle),
                    editorSettings.anchorDisplayScale))
        {
            result = Handles.Slider2D(controlID, anchorPosition, Vector3.forward, Vector3.up, Vector3.right, handleSize,
                drawer.DrawSquare, Vector2.zero);
        }
        if (EditorGUI.EndChangeCheck() && snapPositions != null)
        {
            foreach (Vector2 snapPosition in snapPositions)
            {
                if (Vector2.Distance(result, snapPosition) < handleSize*0.25f)
                {
                    result = snapPosition;
                    break;
                }
            }
        }

        return result;
    }


	public struct TransformInfo
    {
        public readonly Vector3 pos;
        public readonly Quaternion rot;

        public TransformInfo(Vector3 position, Quaternion rotation)
        {
            pos = position;
            rot = rotation;
        }
    }

    public class RadiusHandleData
    {
        public Vector2 previousPosition, originalPosition;
        public Dictionary<Transform, TransformInfo> originalTransformInfos;
        public float accum;
        public int button;
        public Vector2 midPoint;
    }

    protected static void RadiusHandle(
        IEnumerable<Transform> transforms, 
        IEnumerable<Transform> rightTransforms, 
        Vector2 midPoint, 
        float innerRadius,
        float radius)
    {
        RadiusHandle(
            GUIUtility.GetControlID(FocusType.Passive), 
            transforms,
            rightTransforms,
            midPoint, 
            innerRadius, 
            radius);
    }

    protected static void RadiusHandle(
        IEnumerable<Transform> transforms, 
        Vector2 midPoint, 
        float innerRadius,
        float radius)
    {
        RadiusHandle(
            GUIUtility.GetControlID(FocusType.Passive), 
            transforms,
            null,
            midPoint, 
            innerRadius, 
            radius);
    }

    protected static void RadiusHandle(
        int controlID,
        IEnumerable<Transform> transforms,
        Vector2 midPoint,
        float innerRadius,
        float radius)
    {
        RadiusHandle(
            controlID,
            transforms,
            null,
            midPoint,
            innerRadius,
            radius);
    }

    protected static void RadiusHandle(
        int controlID, 
        IEnumerable<Transform> transforms,
        IEnumerable<Transform> rightTransforms, 
        Vector2 midPoint,
        float innerRadius,
        float radius)
    {
        RadiusHandleData radiusHandleData = StateObject.Get<RadiusHandleData>(controlID);
        if (GUIUtility.hotControl == controlID)
        {
            Vector2 mousePosition = Event.current.mousePosition;
            Vector2 previousPosition = radiusHandleData.previousPosition;

            Vector2 worldMousePosition = EditorHelpers.HandlePointToWorld(mousePosition);
            Vector2 worldPreviousPosition = EditorHelpers.HandlePointToWorld(previousPosition);
            Vector2 center = radiusHandleData.midPoint;

            Vector2 towardsMouse = worldMousePosition - center;
            Vector2 towardsPrevious = worldPreviousPosition - center;

            float previousAngle = Helpers.GetAngle(towardsPrevious);
            float newAngle = Helpers.GetAngle(towardsMouse);

            float mainAngleDiff = newAngle - previousAngle;
            if (mainAngleDiff > 180)
            {
                mainAngleDiff -= 360;
            }
            if (mainAngleDiff < -180)
            {
                mainAngleDiff += 360;
            }

            radiusHandleData.accum += mainAngleDiff;
            radiusHandleData.previousPosition = Event.current.mousePosition;

            var snappedAccum = Handles.SnapValue(radiusHandleData.accum, 45);

            var originalAngle =
                Helpers.GetAngle(
                    (Vector2)
                        EditorHelpers.HandlePointToWorld(radiusHandleData.originalPosition) -
                    center);

            foreach (KeyValuePair<Transform, TransformInfo> kvp in radiusHandleData.originalTransformInfos)
            {
                Transform transform = kvp.Key;
                TransformInfo info = kvp.Value;

                Vector2 currentPosition = transform.position;
                if (Vector3.Distance(currentPosition, center) <= JointHelpers.AnchorEpsilon)
                {
                    float currentObjectAngle = transform.rotation.eulerAngles.z;
                    float originalObjectAngle = info.rot.eulerAngles.z;

                    float snappedAngle = originalObjectAngle + snappedAccum;

                    if (Mathf.Abs(snappedAngle - currentObjectAngle) > Mathf.Epsilon)
                    {
                        GUI.changed = true;
                        EditorHelpers.RecordUndo("Orbit", transform, transform.gameObject);
                        Quaternion rotationDelta = Helpers.Rotate2D(snappedAccum);

                        transform.rotation = rotationDelta*info.rot;
                    }
                }
                else
                {
                    Vector2 originalPosition = info.pos;
                    Quaternion originalRotation = info.rot;

                    if (Mathf.Abs(snappedAccum) > Mathf.Epsilon)
                    {
                        GUI.changed = true;
                        EditorHelpers.RecordUndo("Orbit", transform, transform.gameObject);

                        Quaternion rotationDelta = Helpers.Rotate2D(snappedAccum);
                        Vector2 originalTowardsObject = (originalPosition - center);

                        transform.position = (Vector3)(center + (Vector2)((rotationDelta) * originalTowardsObject))
                                             + new Vector3(0, 0, info.pos.z);

                        transform.rotation = rotationDelta * originalRotation;
                    }
                }
            }

            switch (Event.current.type)
            {
                case EventType.mouseMove:
                case EventType.mouseDrag:
                {
                    Event.current.Use();
                    break;
                }
                case EventType.mouseUp:
                {
                    if (Event.current.button == radiusHandleData.button)
                    {
                        Event.current.Use();
                        GUIUtility.hotControl = 0;
                    }
                    break;
                }
                case EventType.repaint:
                    int spins = Mathf.FloorToInt(Mathf.Abs(snappedAccum/360));
                    float arcAngle = snappedAccum%360;

                    if (spins > 0)
                    {
//                            using (new DisposableHandleColor(Color.yellow))
//                            {
//                                Handles.DrawSolidDisc(midPoint, Vector3.forward, radius * Mathf.Abs(snappedAccum / 360) * 0.125f);
//                            }
                        float completion = 360 - arcAngle;
                        using (
                            new HandleColor(spins%2 == 1
                                ? editorSettings.radiusColor
                                : editorSettings.alternateRadiusColor))
                        {
                            Handles.DrawSolidArc(midPoint, Vector3.forward,
                                Helpers.Rotated2DVector(originalAngle),
                                -completion, radius);
                        }
                    }
                    using (
                        new HandleColor(spins%2 == 0
                            ? editorSettings.radiusColor
                            : editorSettings.alternateRadiusColor))
                    {
                        Handles.DrawSolidArc(midPoint, Vector3.forward,
                            Helpers.Rotated2DVector(originalAngle),
                            arcAngle, radius);
                    }

                    if (editorSettings.drawRadiusRings)
                    {
                        using (new HandleColor())
                        {
                            for (int i = 0; i <= spins; i++)
                            {
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
        if ((inZone && GUIUtility.hotControl == 0) || controlID == GUIUtility.hotControl)
        {
            EditorHelpers.SetEditorCursor(MouseCursor.RotateArrow, controlID);
            using (new HandleColor(editorSettings.previewRadiusColor))
            {
                Handles.DrawSolidDisc(midPoint, Vector3.forward, innerRadius);
                Handles.DrawWireDisc(midPoint, Vector3.forward, radius);
            }
            HandleUtility.Repaint();
        }

        if (inZone)
        {
            HandleUtility.AddControl(controlID, distanceFromOuter);
            switch (Event.current.type)
            {
                case EventType.mouseDown:
                    if (GUIUtility.hotControl == 0 &&
                        (Event.current.button == 0 || (Event.current.button == 1 && (rightTransforms != null))))
                    {

                        GUIUtility.hotControl = controlID;
                        radiusHandleData.originalTransformInfos = new Dictionary<Transform, TransformInfo>();
                        radiusHandleData.button = Event.current.button;
                        radiusHandleData.midPoint = midPoint;
                        if (Event.current.button == 0)
                        {
                            foreach (Transform transform in transforms)
                            {
                                radiusHandleData.originalTransformInfos.Add(transform,
                                    new TransformInfo(transform.position,
                                        transform.rotation));
                            }
                        }
                        else if(rightTransforms != null)
                        {
                            foreach (Transform transform in rightTransforms)
                            {
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

    public sealed override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        bool foldout = EditorGUILayout.Foldout(editorSettings.foldout, "Advanced Options");
        if (EditorGUI.EndChangeCheck())
        {
            //no need to record undo here.
            editorSettings.foldout = foldout;
            EditorUtility.SetDirty(editorSettings);
        }
        if (foldout) {
            using (new Indent())
            {
                List<Object> allSettings =
                        targets.Cast<Joint2D>().Select(joint2D => SettingsHelper.GetOrCreate(joint2D))
                            .Where(hingeSettings => hingeSettings != null).Cast<Object>().ToList();

                SerializedObject serializedSettings = null;
                if (allSettings.Count > 0)
                {
                    serializedSettings = new SerializedObject(allSettings.ToArray());
                }
                EditorGUILayout.LabelField("Display:");
                using (new Indent())
                {
                    ToggleShowGizmos(serializedSettings);

                }
            }
        }
        InspectorGUI(foldout);
    }

    protected virtual void InspectorGUI(bool foldout) {
        DrawDefaultInspector();
    }


    protected static List<Vector2> GetAllAnchorsInSelection(AnchoredJoint2D hingeJoint2D)
    {
        List<Vector2> otherAnchors = new List<Vector2>();
        foreach (AnchoredJoint2D otherHingeObject in Selection.GetFiltered(typeof(AnchoredJoint2D), SelectionMode.Deep))
        {
            foreach (AnchoredJoint2D otherHingeJoint in otherHingeObject.GetComponents<AnchoredJoint2D>())
            {
                if (otherHingeJoint == hingeJoint2D)
                {
                    continue;
                }

                Vector2 otherWorldAnchor = Helpers.Transform2DPoint(otherHingeJoint.transform,
                    otherHingeJoint.anchor);
                Vector2 otherConnectedWorldAnchor = otherHingeJoint.connectedBody
                    ? Helpers.Transform2DPoint(
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
        return otherAnchors;
    }
}