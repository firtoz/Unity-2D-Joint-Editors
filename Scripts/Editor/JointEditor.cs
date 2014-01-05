using System;
using System.Collections.Generic;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;

public class JointEditor : Editor
{
    protected static readonly AssetUtils Utils = new AssetUtils("2DJointEditors/Data");

    private static JointEditorSettings _jointSettings;

    protected static JointEditorSettings jointSettings
    {
        get
        {
            return _jointSettings ?? (_jointSettings = Utils.GetOrCreateAsset<JointEditorSettings>("settings.asset"));
        }
    }

    protected enum AnchorBias
    {
        Main,
        Connected,
        Either
    }

    protected static Vector2 AnchorSlider(Vector2 position, float handleScale, IEnumerable<Vector2> snapPositions,
        AnchorBias bias, Joint2D joint)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Native);
        return AnchorSlider(controlID, position, handleScale, snapPositions, bias, joint);
    }

    protected static Vector2 AnchorSlider(int controlID, Vector2 position, float handleScale,
        IEnumerable<Vector2> snapPositions, AnchorBias bias, Joint2D joint)
    {
        float handleSize = HandleUtility.GetHandleSize(position)*handleScale;
        EditorGUI.BeginChangeCheck();
        Vector2 targetPosition;
        if (bias == AnchorBias.Connected)
        {
            if (joint.connectedBody)
            {
                targetPosition = joint.connectedBody.transform.position;
            }
            else
            {
                targetPosition = position;
            }
        }
        else
        {
            targetPosition = joint.gameObject.transform.position;
        }

        float originalAngle;

        if (Vector3.Distance(targetPosition, position) > JointEditorSettings.AnchorEpsilon)
        {
            Vector3 towardsTarget = (targetPosition - position).normalized;

            originalAngle = JointEditorHelpers.GetAngle(towardsTarget);
        }
        else
        {
            originalAngle = joint.gameObject.transform.rotation.eulerAngles.z;
        }

        if (GUIUtility.hotControl == controlID)
        {
            using (
                DisposableGUITextureDrawer drawer =
                    new DisposableGUITextureDrawer(jointSettings.hotHingeTexture,
                        Quaternion.AngleAxis(originalAngle,
                            Vector3.forward),
                        jointSettings.anchorDisplayScale))
            {
                drawer.DrawSquare(position, Quaternion.identity, handleSize);
            }
        }


        float distanceFromInner = HandleUtility.DistanceToCircle(position, handleSize*.5f);
        bool inZone = distanceFromInner <= 0;
        if ((inZone && GUIUtility.hotControl == 0) || controlID == GUIUtility.hotControl)
        {
            GUIHelpers.SetEditorCursor(MouseCursor.MoveArrow, controlID);
        }

        //Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        Vector2 result;


        Texture2D sliderTexture;

        switch (bias)
        {
            case AnchorBias.Main:
                sliderTexture = jointSettings.mainHingeTexture;
                break;
            case AnchorBias.Connected:
                sliderTexture = jointSettings.connectedHingeTexture;
                break;
            case AnchorBias.Either:
                sliderTexture = jointSettings.lockedHingeTexture;
                break;
            default:
                throw new ArgumentOutOfRangeException("bias");
        }
        using (
            DisposableGUITextureDrawer drawer =
                new DisposableGUITextureDrawer(sliderTexture,
                    Quaternion.AngleAxis(originalAngle,
                        Vector3.forward),
                    jointSettings.anchorDisplayScale))
        {
            result = Handles.Slider2D(controlID, position, Vector3.forward, Vector3.up, Vector3.right, handleSize,
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
    }

    protected static void RadiusHandle(IEnumerable<Transform> transforms, Vector2 midPoint, float innerRadius,
        float radius)
    {
        RadiusHandle(GUIUtility.GetControlID(FocusType.Passive), transforms, midPoint, innerRadius, radius);
    }

    protected static void RadiusHandle(int controlID, IEnumerable<Transform> transforms, Vector2 midPoint,
        float innerRadius,
        float radius)
    {
        RadiusHandleData radiusHandleData = StateObject.Get<RadiusHandleData>(controlID);
        if (GUIUtility.hotControl == controlID)
        {
            Vector2 mousePosition = Event.current.mousePosition;
            Vector2 previousPosition = radiusHandleData.previousPosition;

            Vector2 worldMousePosition = HandleUtility.GUIPointToWorldRay(mousePosition).origin;
            Vector2 worldPreviousPosition = HandleUtility.GUIPointToWorldRay(previousPosition).origin;

            Vector2 towardsMouse = worldMousePosition - midPoint;
            Vector2 towardsPrevious = worldPreviousPosition - midPoint;

            float previousAngle = JointEditorHelpers.GetAngle(towardsPrevious);
            float newAngle = JointEditorHelpers.GetAngle(towardsMouse);

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
                JointEditorHelpers.GetAngle(
                    (Vector2)
                        HandleUtility.GUIPointToWorldRay(radiusHandleData.originalPosition).origin -
                    midPoint);

            foreach (KeyValuePair<Transform, TransformInfo> kvp in radiusHandleData.originalTransformInfos)
            {
                Transform transform = kvp.Key;
                TransformInfo info = kvp.Value;

                Vector2 currentPosition = transform.position;
                if (Vector3.Distance(currentPosition, midPoint) <= JointEditorSettings.AnchorEpsilon)
                {
                    float currentObjectAngle = transform.rotation.eulerAngles.z;
                    float originalObjectAngle = info.rot.eulerAngles.z;

                    float snappedAngle = originalObjectAngle + snappedAccum;

                    if (Mathf.Abs(snappedAngle - currentObjectAngle) > Mathf.Epsilon)
                    {
                        GUI.changed = true;
                        GUIHelpers.RecordUndo("Orbit", transform, transform.gameObject);
                        Quaternion rotationDelta = Quaternion.AngleAxis(snappedAccum,
                            Vector3.forward);

                        transform.rotation = rotationDelta*info.rot;
                    }
                }
                else
                {
                    Vector2 originalPosition = info.pos;

                    Vector2 currentTowardsObject = (currentPosition - midPoint);
                    Vector2 originalTowardsObject = (originalPosition - midPoint);

                    float currentObjectAngle = JointEditorHelpers.GetAngle(currentTowardsObject);
                    float originalObjectAngle = JointEditorHelpers.GetAngle(originalTowardsObject);

                    float snappedAngle = originalObjectAngle + snappedAccum;

                    if (Mathf.Abs(snappedAngle - currentObjectAngle) > Mathf.Epsilon)
                    {
                        GUI.changed = true;
                        GUIHelpers.RecordUndo("Orbit", transform, transform.gameObject);

                        float angleDelta = snappedAngle - currentObjectAngle;

                        Quaternion rotationDelta = Quaternion.AngleAxis(angleDelta, Vector3.forward);

                        transform.position = ((Vector3) midPoint + ((rotationDelta)*currentTowardsObject))
                                             + new Vector3(0, 0, info.pos.z);

                        transform.rotation = rotationDelta*transform.rotation;
//                        transform.rotation = info.rot * rotationDelta;
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
                    if (Event.current.button == 0)
                    {
                        Event.current.Use();
                        GUIUtility.hotControl = 0;
                    }
                    break;
                }
                case EventType.repaint:

                    if (Event.current.type == EventType.repaint)
                    {
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
                                new DisposableHandleColor(spins%2 == 1
                                    ? jointSettings.radiusColor
                                    : jointSettings.alternateRadiusColor))
                            {
                                Handles.DrawSolidArc(midPoint, Vector3.forward,
                                    (Quaternion.AngleAxis(originalAngle, Vector3.forward))*
                                    Vector3.right,
                                    -completion, radius);
                            }
                        }
                        using (
                            new DisposableHandleColor(spins%2 == 0
                                ? jointSettings.radiusColor
                                : jointSettings.alternateRadiusColor))
                        {
                            Handles.DrawSolidArc(midPoint, Vector3.forward,
                                (Quaternion.AngleAxis(originalAngle, Vector3.forward))*
                                Vector3.right,
                                arcAngle, radius);
                        }

                        if (jointSettings.drawRadiusRings)
                        {
                            using (new DisposableHandleColor())
                            {
                                for (int i = 0; i <= spins; i++)
                                {
                                    Handles.color = i%2 == 0
                                        ? jointSettings.radiusColor
                                        : jointSettings.alternateRadiusColor;
                                    Handles.DrawWireDisc(midPoint, Vector3.forward, radius + radius*i*0.125f);
                                }
                                Handles.color = Color.Lerp(Handles.color, spins%2 == 1
                                    ? jointSettings.radiusColor
                                    : jointSettings.alternateRadiusColor,
                                    Mathf.Abs(arcAngle/360));
                                Handles.DrawWireDisc(midPoint, Vector3.forward, radius +
                                                                                radius*(spins + Mathf.Abs(arcAngle/360))*
                                                                                0.125f);
                            }
                        }
                    }
                    break;
            }
        }

        float distanceFromInner = HandleUtility.DistanceToCircle(midPoint, innerRadius);
        float distanceFromOuter = HandleUtility.DistanceToCircle(midPoint, radius);
        bool inZone = distanceFromInner > 0 && distanceFromOuter <= JointEditorSettings.AnchorEpsilon;
        if ((inZone && GUIUtility.hotControl == 0) || controlID == GUIUtility.hotControl)
        {
            GUIHelpers.SetEditorCursor(MouseCursor.RotateArrow, controlID);
            using (new DisposableHandleColor(jointSettings.previewRadiusColor))
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
                    if (Event.current.button == 0 && GUIUtility.hotControl == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        radiusHandleData.originalTransformInfos = new Dictionary<Transform, TransformInfo>();
                        foreach (Transform transform in transforms)
                        {
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
    }

    protected static AnchorBias GetBias(PositionChange change)
    {
        switch (change)
        {
            case PositionChange.MainChanged:
                return AnchorBias.Main;
            case PositionChange.ConnectedChanged:
                return AnchorBias.Connected;
            default:
                return AnchorBias.Either;
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
//            GUIHelpers.RecordUndo(null, settings);
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