using System;
using System.Collections.Generic;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.Disposable;
using toxicFork.GUIHelpers.DisposableGUI;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;

public class JointEditor : Editor
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

    protected static Vector2 AnchorSlider(Vector2 position, float handleScale, IEnumerable<Vector2> snapPositions,
        JointHelpers.AnchorBias bias, Joint2D joint)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Native);
        return AnchorSlider(controlID, position, handleScale, snapPositions, bias, joint);
    }


	protected static Vector2 AnchorSlider(int controlID, float handleScale,
		IEnumerable<Vector2> snapPositions, JointHelpers.AnchorBias bias, HingeJoint2D joint) {
		Vector2 position = JointHelpers.GetAnchorPosition(joint, bias);

		return AnchorSlider(controlID, position, handleScale, snapPositions, bias, joint);
	}

	protected static Vector2 AnchorSlider(int controlID, Vector2 anchorPosition, float handleScale,
        IEnumerable<Vector2> snapPositions, JointHelpers.AnchorBias bias, Joint2D joint)
    {
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

    protected static JointHelpers.AnchorBias GetBias(PositionChange change)
    {
        switch (change)
        {
            case PositionChange.MainChanged:
                return JointHelpers.AnchorBias.Main;
            case PositionChange.ConnectedChanged:
                return JointHelpers.AnchorBias.Connected;
            default:
                return JointHelpers.AnchorBias.Either;
        }
    }

    public enum PositionChange
    {
        NoChange,
        MainChanged,
        ConnectedChanged,
        BothChanged
    }

    protected struct PositionInfo
    {
        public static void Record(HingeJoint2D hingeJoint2D)
        {
//            HingeJoint2DSettings settings = HingeJoint2DSettingsEditor.GetOrCreate(hingeJoint2D);
//            EditorGUIHelpers.RecordUndo(null, settings);
//            settings.worldAnchor = JointEditorHelpers.GetAnchorPosition(hingeJoint2D);
//            settings.worldConnectedAnchor = JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D);
//            EditorUtility.SetDirty(settings);
        }

//        public static PositionChange Changed(HingeJoint2D hingeJoint2D) {
//            HingeJoint2DSettings settings = HingeJoint2DSettingsEditor.Get(hingeJoint2D);
//            if (!settings || !settings.lockAnchors) {
//                return PositionChange.NoChange;
//            }
//            
//            return Changed(JointEditorHelpers.GetAnchorPosition(hingeJoint2D),
//                           JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D), settings);
//        }

//        private static PositionChange Changed(Vector2 main, Vector2 connected, HingeJoint2DSettings settings)
//        {
//            PositionChange result = PositionChange.NoChange;
//
////            bool mainChanged = Vector3.Distance(settings.worldAnchor, main) > JointEditorSettings.AnchorEpsilon;
////            bool connectedChanged = Vector3.Distance(settings.worldConnectedAnchor, connected) > JointEditorSettings.AnchorEpsilon;
////
////            if (mainChanged) {
////                result = connectedChanged ? PositionChange.BothChanged : PositionChange.MainChanged;
////            }
////            else if (connectedChanged) {
////                result = PositionChange.ConnectedChanged;
////            }
//            return result;
//        }
    }
}