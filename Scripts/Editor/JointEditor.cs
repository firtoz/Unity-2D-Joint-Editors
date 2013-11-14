using System;
using System.Collections.Generic;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class JointEditor : Editor {
    protected static readonly AssetUtils Utils = new AssetUtils("2DJointEditors/Data");

    private static JointEditorSettings _jointSettings;

    protected static JointEditorSettings jointSettings {
        get {
            return _jointSettings ?? (_jointSettings = Utils.GetOrCreateAsset<JointEditorSettings>("settings.asset"));
        }
    }

    protected enum AnchorBias {
        Main,
        Connected,
        Either
    }


    protected static void RecordUndo(String action, params Object[] objects) {
        Undo.RecordObjects(objects, action);
    }

    protected static Vector2 Transform2DPoint(Transform transform, Vector2 point) {
        Vector2 scaledPoint = Vector2.Scale(point, transform.lossyScale);
        float angle = transform.rotation.eulerAngles.z;
        Vector2 rotatedScaledPoint = Quaternion.AngleAxis(angle, Vector3.forward)*scaledPoint;
        Vector2 translatedRotatedScaledPoint = (Vector2) transform.position + rotatedScaledPoint;
        return translatedRotatedScaledPoint;
    }

    protected static Vector2 InverseTransform2DPoint(Transform transform, Vector2 translatedRotatedScaledPoint) {
        Vector2 rotatedScaledPoint = translatedRotatedScaledPoint - (Vector2) transform.position;
        float angle = transform.rotation.eulerAngles.z;
        Vector2 scaledPoint = Quaternion.AngleAxis(-angle, Vector3.forward)*rotatedScaledPoint;
        Vector2 point = Vector2.Scale(scaledPoint, new Vector2(1/transform.lossyScale.x, 1/transform.lossyScale.y));
        return point;
    }


    protected static Vector2 GetAnchorPosition(HingeJoint2D joint2D) {
        return Transform2DPoint(joint2D.transform, joint2D.anchor);
    }

    protected static Vector2 GetConnectedAnchorPosition(HingeJoint2D joint2D) {
        if (joint2D.connectedBody) {
            return Transform2DPoint(joint2D.connectedBody.transform, joint2D.connectedAnchor);
        }
        return joint2D.connectedAnchor;
    }

    protected static void SetWorldAnchorPosition(HingeJoint2D hingeJoint2D, Vector2 worldAnchor) {
        hingeJoint2D.anchor = InverseTransform2DPoint(hingeJoint2D.transform, worldAnchor);
    }

    protected static void SetWorldConnectedAnchorPosition(HingeJoint2D hingeJoint2D, Vector2 worldConnectedAnchor) {
        if (hingeJoint2D.connectedBody) {
            hingeJoint2D.connectedAnchor = InverseTransform2DPoint(hingeJoint2D.connectedBody.transform,
                                                                   worldConnectedAnchor);
        }
        else {
            hingeJoint2D.connectedAnchor = worldConnectedAnchor;
        }
    }

    protected static float GetAngle(Vector2 vector) {
        return Mathf.Rad2Deg*Mathf.Atan2(vector.y, vector.x);
    }

    protected static Vector2 AnchorSlider(Vector2 position, float handleScale, out bool changed,
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
                DisposableGUITextureDrawer drawer =
                    new DisposableGUITextureDrawer(jointSettings.hotHingeTexture,
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
                DisposableGUITextureDrawer drawer =
                    new DisposableGUITextureDrawer(jointSettings.lockedHingeTexture,
                                                   Quaternion.AngleAxis(originalAngle,
                                                                        Vector3.forward),
                                                   jointSettings.anchorDisplayScale)) {
                result = Handles.Slider2D(controlID, position, Vector3.forward, Vector3.up, Vector3.right, handleSize,
                                          drawer.DrawSquare, Vector2.zero);
            }
        }
        else {
            using (
                DisposableGUITextureDrawer drawer = new DisposableGUITextureDrawer(bias == AnchorBias.Main
                                                                                       ? jointSettings.mainHingeTexture
                                                                                       : jointSettings
                                                                                             .connectedHingeTexture,
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

    protected static void RadiusHandle(IEnumerable<Transform> transforms, Vector2 midPoint, float innerRadius,
                                       float radius) {
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        RadiusHandleData radiusHandleData = StateObject.Get<RadiusHandleData>(controlID);
        if (GUIUtility.hotControl == controlID) {
            Vector2 mousePosition = Event.current.mousePosition;
            Vector2 previousPosition = radiusHandleData.previousPosition;

            Vector2 worldMousePosition = HandleUtility.GUIPointToWorldRay(mousePosition).origin;
            Vector2 worldPreviousPosition = HandleUtility.GUIPointToWorldRay(previousPosition).origin;

            Vector2 towardsMouse = worldMousePosition - midPoint;
            Vector2 towardsPrevious = worldPreviousPosition - midPoint;

            float previousAngle = GetAngle(towardsPrevious);
            float newAngle = GetAngle(towardsMouse);

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
                GetAngle((Vector2) HandleUtility.GUIPointToWorldRay(radiusHandleData.originalPosition).origin - midPoint);

            foreach (KeyValuePair<Transform, TransformInfo> kvp in radiusHandleData.originalTransformInfos) {
                Transform transform = kvp.Key;
                TransformInfo info = kvp.Value;

                Vector2 currentPosition = transform.position;
                if (Vector3.Distance(currentPosition, midPoint) <= JointEditorSettings.AnchorEpsilon) {
                    float currentObjectAngle = transform.rotation.eulerAngles.z;
                    float originalObjectAngle = info.rot.eulerAngles.z;

                    float snappedAngle = originalObjectAngle + snappedAccum;

                    if (Mathf.Abs(snappedAngle - currentObjectAngle) > Mathf.Epsilon) {
                        GUI.changed = true;
                        RecordUndo("Orbit", transform, transform.gameObject);
                        Quaternion rotationDelta = Quaternion.AngleAxis(snappedAngle,
                                                                        Vector3.forward);

                        transform.rotation = rotationDelta * info.rot;
                    }
                }
                else {
                    Vector2 originalPosition = info.pos;

                    Vector2 currentTowardsObject = (currentPosition - midPoint);
                    Vector2 originalTowardsObject = (originalPosition - midPoint);

                    float currentObjectAngle = GetAngle(currentTowardsObject);
                    float originalObjectAngle = GetAngle(originalTowardsObject);

                    float snappedAngle = originalObjectAngle + snappedAccum;

                    if (Mathf.Abs(snappedAngle - currentObjectAngle) > Mathf.Epsilon) {
                        GUI.changed = true;
                        RecordUndo("Orbit", transform, transform.gameObject);

                        Quaternion rotationDelta = Quaternion.AngleAxis(snappedAccum, Vector3.forward);

                        transform.position = ((Vector3) midPoint + ((rotationDelta) * originalTowardsObject)) 
                            + new Vector3(0, 0, info.pos.z);

                        transform.rotation = rotationDelta * info.rot;
                    }
                }
            }

            switch (Event.current.type) {
                case EventType.mouseMove:
                case EventType.mouseDrag: {
                    Event.current.Use();
                }
                    break;
                case EventType.mouseUp: {
                    Event.current.Use();
                    GUIUtility.hotControl = 0;
                }
                    break;
                case EventType.repaint:

                    if (Event.current.type == EventType.repaint) {
                        int spins = Mathf.FloorToInt(Mathf.Abs(snappedAccum/360));
                        float arcAngle = snappedAccum%360;

                        if (spins > 0) {
//                            using (new DisposableHandleColor(Color.yellow))
//                            {
//                                Handles.DrawSolidDisc(midPoint, Vector3.forward, radius * Mathf.Abs(snappedAccum / 360) * 0.125f);
//                            }
                            float completion = 360 - arcAngle;
                            using (
                                new DisposableHandleColor(spins%2 == 1
                                                              ? jointSettings.radiusColor
                                                              : jointSettings.alternateRadiusColor)) {
                                Handles.DrawSolidArc(midPoint, Vector3.forward,
                                                     (Quaternion.AngleAxis(originalAngle, Vector3.forward))*
                                                     Vector3.right,
                                                     -completion, radius);
                            }
                        }
                        using (
                            new DisposableHandleColor(spins%2 == 0
                                                          ? jointSettings.radiusColor
                                                          : jointSettings.alternateRadiusColor)) {
                            Handles.DrawSolidArc(midPoint, Vector3.forward,
                                                 (Quaternion.AngleAxis(originalAngle, Vector3.forward))*
                                                 Vector3.right,
                                                 arcAngle, radius);
                        }

                        if (jointSettings.drawRadiusRings) {
                            using (new DisposableHandleColor()) {
                                for (int i = 0; i <= spins; i++) {
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
    }

    protected static AnchorBias GetBias(PositionChange change) {
        switch (change) {
            case PositionChange.MainChanged:
                return AnchorBias.Main;
            case PositionChange.ConnectedChanged:
                return AnchorBias.Connected;
            default:
                return AnchorBias.Either;
        }
    }

    public enum PositionChange {
        NoChange,
        MainChanged,
        ConnectedChanged,
        BothChanged
    }

    protected struct PositionInfo {
        public PositionInfo(HingeJoint2D hingeJoint2D) {
            main = GetAnchorPosition(hingeJoint2D);
            connected = GetConnectedAnchorPosition(hingeJoint2D);
        }

        private readonly Vector2 main;
        private readonly Vector2 connected;

        public PositionChange Changed(HingeJoint2D hingeJoint2D) {
            return Changed(GetAnchorPosition(hingeJoint2D), GetConnectedAnchorPosition(hingeJoint2D));
        }

        private PositionChange Changed(Vector2 main, Vector2 connected) {
            PositionChange result = PositionChange.NoChange;

            bool mainChanged = Vector3.Distance(this.main, main) > JointEditorSettings.AnchorEpsilon;
            bool connectedChanged = Vector3.Distance(this.connected, connected) > JointEditorSettings.AnchorEpsilon;

            if (mainChanged) {
                result = connectedChanged ? PositionChange.BothChanged : PositionChange.MainChanged;
            }
            else if (connectedChanged) {
                result = PositionChange.ConnectedChanged;
            }
            return result;
        }
    }
}
