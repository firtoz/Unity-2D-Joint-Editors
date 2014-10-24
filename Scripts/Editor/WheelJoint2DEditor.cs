using System;
using System.Collections.Generic;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableEditor;
using toxicFork.GUIHelpers.DisposableEditorGUI;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (WheelJoint2D))]
[CanEditMultipleObjects]
public class WheelJoint2DEditor : Joint2DEditor
{
    private static readonly HashSet<string> ControlNames = new HashSet<string> {
        "suspensionAngle"
    };

    protected override HashSet<string> GetControlNames()
    {
        return ControlNames;
    }

    protected override bool WantsLocking()
    {
        return true;
    }

    protected override Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
       JointHelpers.AnchorBias bias, float snapDistance)
    {
        if (!EditorGUI.actionKey)
        {
            return position;
        }

        WheelJoint2D wheelJoint2D = (WheelJoint2D)joint;

        if (!SettingsHelper.GetOrCreate(joint).lockAnchors &&
            !(Vector2.Distance(JointHelpers.GetMainAnchorPosition(joint),
                JointHelpers.GetConnectedAnchorPosition(joint)) <= AnchorEpsilon))
        {
            Vector2 wantedAnchorPosition = GetWantedAnchorPosition(wheelJoint2D, bias, position);

            if (Vector2.Distance(position, wantedAnchorPosition) < snapDistance)
            {
                return wantedAnchorPosition;
            }
        }

        return position;
    }

    protected override bool DragBothAnchorsWhenLocked()
    {
        return false;
    }

    protected override void ReAlignAnchors(AnchoredJoint2D joint2D, JointHelpers.AnchorBias alignmentBias)
    {
        WheelJoint2D wheelJoint2D = (WheelJoint2D)joint2D;

        //align the angle to the connected anchor
        Vector2 direction = JointHelpers.GetConnectedAnchorPosition(joint2D) -
                            JointHelpers.GetMainAnchorPosition(joint2D);

        if (direction.magnitude > AnchorEpsilon) {
            float wantedAngle = Helpers2D.GetAngle(direction);

            EditorHelpers.RecordUndo("Realign angle", wheelJoint2D);
            JointSuspension2D susp = wheelJoint2D.suspension;
            susp.angle = wantedAngle - wheelJoint2D.transform.eulerAngles.z;
            wheelJoint2D.suspension = susp;   
        }
    }

    protected override Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias)
    {
        return GetWantedAnchorPosition(anchoredJoint2D, bias, JointHelpers.GetAnchorPosition(anchoredJoint2D, bias));
    }

    private static Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias,
        Vector2 position)
    {
        WheelJoint2D wheelJoint2D = (WheelJoint2D)anchoredJoint2D;

        JointHelpers.AnchorBias otherBias = JointHelpers.GetOppositeBias(bias);

        float worldAngle = wheelJoint2D.transform.eulerAngles.z + wheelJoint2D.suspension.angle;

        Ray slideRay = new Ray(JointHelpers.GetAnchorPosition(wheelJoint2D, otherBias),
            Helpers2D.GetDirection(worldAngle));
        Vector2 wantedAnchorPosition = Helpers2D.ClosestPointToRay(slideRay, position);
        return wantedAnchorPosition;
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias)
    {
        WheelJoint2D wheelJoint2D = (WheelJoint2D)joint2D;

        Vector2 center = JointHelpers.GetAnchorPosition(wheelJoint2D, bias);
        float scale = editorSettings.anchorScale;
        float handleSize = HandleUtility.GetHandleSize(center) * scale;

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(wheelJoint2D);
        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(wheelJoint2D);

        int suspensionAngleControlID = anchorInfo.GetControlID("suspensionAngle");


        if (bias != JointHelpers.AnchorBias.Connected && (GUIUtility.hotControl == suspensionAngleControlID || !Event.current.shift))
        {
            DrawAngleWidget(wheelJoint2D, suspensionAngleControlID);
        }

        //if the joint anchors are being moved, then show snap lines
        int sliderControlID = anchorInfo.GetControlID("slider");
        if (GUIUtility.hotControl == sliderControlID)
        {
            Vector2 snap = GetWantedAnchorPosition(wheelJoint2D, bias);
            using (new HandleColor(new Color(1, 1, 1, .5f)))
            {
                Handles.DrawLine(connectedAnchorPosition, snap);
                Handles.DrawLine(mainAnchorPosition, snap);
            }
        }

        using (new HandleColor(new Color(1, 1, 1, 0.5f)))
        {
            Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
            if (wheelJoint2D.connectedBody && GUIUtility.hotControl == suspensionAngleControlID)
            {
                Handles.DrawLine(mainAnchorPosition, GetTargetPosition(wheelJoint2D, JointHelpers.AnchorBias.Connected));
            }
        }

        if (bias == JointHelpers.AnchorBias.Main)
        {
            Vector2 mainBodyPosition = GetTargetPosition(wheelJoint2D, JointHelpers.AnchorBias.Main);
            using (new HandleColor(editorSettings.anchorsToMainBodyColor))
            {
                if (Vector2.Distance(mainBodyPosition, center) > AnchorEpsilon)
                {
                    Handles.DrawLine(mainBodyPosition, center);
                }
            }
        }
        else if (bias == JointHelpers.AnchorBias.Connected)
        {
            Vector2 connectedBodyPosition = GetTargetPosition(wheelJoint2D, JointHelpers.AnchorBias.Connected);
            if (wheelJoint2D.connectedBody)
            {
                using (new HandleColor(editorSettings.anchorsToConnectedBodyColor))
                {
                    if (Vector2.Distance(connectedBodyPosition, center) > AnchorEpsilon)
                    {
                        Handles.DrawLine(connectedBodyPosition, center);
                    }
                    else
                    {
                        float rot = JointHelpers.GetTargetRotation(wheelJoint2D, JointHelpers.AnchorBias.Connected);
                        Handles.DrawLine(center, center + Helpers2D.GetDirection(rot) * handleSize);
                    }
                }
            }
        }
        return false;
    }

    private void DrawAngleWidget(WheelJoint2D wheelJoint2D, int controlID)
    {
        float worldAngle = wheelJoint2D.transform.eulerAngles.z + wheelJoint2D.suspension.angle;

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(wheelJoint2D);

        WheelJoint2DSettings joint2DSettings = SettingsHelper.GetOrCreate<WheelJoint2DSettings>(wheelJoint2D);

        HandleDragDrop(controlID, wheelJoint2D, joint2DSettings);

        EditorGUI.BeginChangeCheck();

        float newAngle = LineAngleHandle(controlID, worldAngle, mainAnchorPosition, 0.5f, 2);

        Vector2 mousePosition = Event.current.mousePosition;

        EditorHelpers.ContextClick(controlID, () => {
            GenericMenu menu = new GenericMenu();
            AddSuspensionAngleContextMenuItem(wheelJoint2D, menu, mousePosition);
            menu.ShowAsContext();
        });

        if (EditorGUI.EndChangeCheck())
        {
            Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(wheelJoint2D);
            Vector2 connectedOffset = connectedAnchorPosition - mainAnchorPosition;
            bool snapped = false;

            if (EditorGUI.actionKey)
            {
                float handleSize = HandleUtility.GetHandleSize(mainAnchorPosition);

                Vector2 mousePosition2D = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);

                Ray currentAngleRay = new Ray(mainAnchorPosition, Helpers2D.GetDirection(newAngle));

                Vector2 mousePositionProjectedToAngle = Helpers2D.ClosestPointToRay(currentAngleRay, mousePosition2D);

                List<Vector2> directionsToSnapTo = new List<Vector2> {
                        (GetTargetPosition(wheelJoint2D, JointHelpers.AnchorBias.Main) - mainAnchorPosition)
                            .normalized
                    };

                if (!joint2DSettings.lockAnchors)
                {
                    directionsToSnapTo.Insert(0, connectedOffset.normalized);
                }

                if (wheelJoint2D.connectedBody)
                {
                    directionsToSnapTo.Add(
                        (GetTargetPosition(wheelJoint2D, JointHelpers.AnchorBias.Connected) - mainAnchorPosition)
                            .normalized);
                }


                foreach (Vector2 direction in directionsToSnapTo)
                {
                    Ray rayTowardsConnectedAnchor = new Ray(mainAnchorPosition, direction);

                    Vector2 closestPointTowardsDirection = Helpers2D.ClosestPointToRay(rayTowardsConnectedAnchor,
                        mousePositionProjectedToAngle);

                    if (Vector2.Distance(closestPointTowardsDirection, mousePositionProjectedToAngle) <
                        handleSize * 0.125f)
                    {
                        Vector2 currentDirection = Helpers2D.GetDirection(newAngle);
                        Vector2 closestPositionToDirection =
                            Helpers2D.ClosestPointToRay(rayTowardsConnectedAnchor,
                                mainAnchorPosition + currentDirection);

                        snapped = true;
                        newAngle = Helpers2D.GetAngle(closestPositionToDirection - mainAnchorPosition);

                        break;
                    }
                }
            }

            float wantedAngle = newAngle - wheelJoint2D.transform.eulerAngles.z;

            if (!snapped)
            {
                wantedAngle = Handles.SnapValue(wantedAngle, 45);
            }

            if (joint2DSettings.lockAnchors)
            {
                float angleDelta = Mathf.DeltaAngle(wheelJoint2D.suspension.angle, wantedAngle);
                EditorHelpers.RecordUndo("Alter Wheel Joint 2D Angle", wheelJoint2D);
                JointSuspension2D susp = wheelJoint2D.suspension;
                susp.angle = wantedAngle;
                wheelJoint2D.suspension = susp;

                JointHelpers.SetWorldConnectedAnchorPosition(wheelJoint2D,
                    mainAnchorPosition + (Vector2)(Helpers2D.Rotate(angleDelta) * connectedOffset));
            }
            else
            {
                if (EditorGUI.actionKey) {
                    Ray wantedAngleRay = new Ray(mainAnchorPosition,
                    (JointHelpers.GetConnectedAnchorPosition(wheelJoint2D) - mainAnchorPosition).normalized);
                    float handleSize = HandleUtility.GetHandleSize(mainAnchorPosition);
                    Vector2 mousePosition2D = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
                    Vector2 anglePosition =
                        Helpers2D.ClosestPointToRay(new Ray(mainAnchorPosition, Helpers2D.GetDirection(newAngle)),
                            mousePosition2D);

                    Vector2 closestPosition = Helpers2D.ClosestPointToRay(wantedAngleRay, anglePosition);
                    if (Vector2.Distance(closestPosition, anglePosition) < handleSize*0.125f) {
                        Vector2 currentDirection = Helpers2D.GetDirection(newAngle);
                        Vector2 closestPositionToDirection =
                            Helpers2D.ClosestPointToRay(wantedAngleRay,
                                mainAnchorPosition + currentDirection);

                        newAngle = Helpers2D.GetAngle(closestPositionToDirection - mainAnchorPosition);
                        wantedAngle = newAngle - wheelJoint2D.transform.eulerAngles.z;
                    }
                }

                EditorHelpers.RecordUndo("Change Suspension Angle", wheelJoint2D);
                JointSuspension2D susp = wheelJoint2D.suspension;
                susp.angle = wantedAngle;
                wheelJoint2D.suspension = susp;
            }
        }
    }

    private static void AddSuspensionAngleContextMenuItem(WheelJoint2D wheelJoint2D, GenericMenu menu, Vector2 mousePosition) {
        menu.AddItem(new GUIContent("Edit Suspension Angle"), false,
            delegate {
                Vector2 mainAnchorPosition =
                    JointHelpers.GetMainAnchorPosition(wheelJoint2D);

                WheelJoint2DSettings joint2DSettings = SettingsHelper.GetOrCreate<WheelJoint2DSettings>(wheelJoint2D);

                EditorHelpers.ShowDropDown(
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight*3),
                    delegate(Action close, bool focused) {
                        EditorGUI.BeginChangeCheck();
                        GUI.SetNextControlName("SuspensionAngle");
                        float suspensionAngle =
                            EditorGUILayout.FloatField(
                                new GUIContent("Suspension Angle",
                                    "The world movement angle for the suspension. [ -1000000, 1000000 ]."),
                                wheelJoint2D.suspension.angle);
                        if (EditorGUI.EndChangeCheck()) {
                            using (new Modification("Suspension Angle", wheelJoint2D)) {
                                float angleDelta = Mathf.DeltaAngle(wheelJoint2D.suspension.angle, suspensionAngle);
                                EditorHelpers.RecordUndo("Alter Wheel Joint 2D Angle", wheelJoint2D);
                                JointSuspension2D susp = wheelJoint2D.suspension;
                                susp.angle = suspensionAngle;

                                wheelJoint2D.suspension = susp;


                                if (joint2DSettings.lockAnchors) {
                                    Vector2 connectedAnchorPosition =
                                        JointHelpers.GetConnectedAnchorPosition(wheelJoint2D);
                                    Vector2 connectedOffset = connectedAnchorPosition - mainAnchorPosition;

                                    JointHelpers.SetWorldConnectedAnchorPosition(wheelJoint2D,
                                        mainAnchorPosition +
                                        (Vector2) (Helpers2D.Rotate(angleDelta)*connectedOffset));
                                }

                                JointSuspension2D suspension = wheelJoint2D.suspension;

                                suspension.angle = suspensionAngle;

                                wheelJoint2D.suspension = suspension;
                            }
                        }
                        if (GUILayout.Button("Done") ||
                            (Event.current.isKey &&
                             (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape) &&
                             focused)) {
                            close();
                        }
                    });
            });
    }

    protected override bool PostAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo info, List<Vector2> otherAnchors,
        JointHelpers.AnchorBias bias) {
        WheelJoint2D wheelJoint2D = joint2D as WheelJoint2D;
        if (wheelJoint2D == null) {
            return false;
        }



        if (EditorHelpers.IsWarm(info.GetControlID("suspensionAngle")) && DragAndDrop.objectReferences.Length == 0)
        {
            float suspensionAngle = wheelJoint2D.suspension.angle;

            GUIContent labelContent = new GUIContent(String.Format("{0:0.00}", suspensionAngle));
            Vector3 mainAnchorPosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);

            float fontSize = HandleUtility.GetHandleSize(mainAnchorPosition) * (1f / 64f);

            float labelOffset = fontSize * EditorHelpers.FontWithBackgroundStyle.CalcSize(labelContent).y;
            
                        Handles.Label(mainAnchorPosition + (Camera.current.transform.up * labelOffset), labelContent,
                            EditorHelpers.FontWithBackgroundStyle);
        }

        return false;
    }

    protected override void OwnershipMoved(AnchoredJoint2D cloneJoint) {
        //swap limits
        WheelJoint2D wheelJoint2D = cloneJoint as WheelJoint2D;
        if (!wheelJoint2D) {
            return;
        }

        float worldAngle = wheelJoint2D.connectedBody.transform.eulerAngles.z + wheelJoint2D.suspension.angle;

        JointSuspension2D suspension = wheelJoint2D.suspension;

        suspension.angle = (180.0f+worldAngle) - wheelJoint2D.transform.eulerAngles.z;

        wheelJoint2D.suspension = suspension;
    }


    protected override void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint)
    {
        WheelJoint2D wheelJoint2D = joint as WheelJoint2D;
        if (wheelJoint2D != null)
        {
            Vector2 mousePosition = Event.current.mousePosition;

            AddSuspensionAngleContextMenuItem(wheelJoint2D, menu, mousePosition);

            menu.AddItem(new GUIContent("Use Motor"), wheelJoint2D.useMotor, () =>
            {
                EditorHelpers.RecordUndo("Use Motor", wheelJoint2D);
                wheelJoint2D.useMotor = !wheelJoint2D.useMotor;
                EditorUtility.SetDirty(wheelJoint2D);
            });


            menu.AddItem(new GUIContent("Configure Motor"), false, () =>
                EditorHelpers.ShowDropDown(
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 6),
                    delegate(Action close, bool focused)
                    {
                        EditorGUILayout.LabelField(new GUIContent("Wheel Joint 2D Motor", "The joint motor."));
                        using (new Indent())
                        {
                            EditorGUI.BeginChangeCheck();

                            bool useMotor =
                                EditorGUILayout.Toggle(
                                    new GUIContent("Use Motor", "Whether to use the joint motor or not."),
                                    wheelJoint2D.useMotor);

                            GUI.SetNextControlName("Motor Config");
                            float motorSpeed = EditorGUILayout.FloatField(
                                new GUIContent("Motor Speed",
                                    "The target motor speed in degrees/second. [-100000, 1000000 ]."),
                                wheelJoint2D.motor.motorSpeed);
                            GUI.SetNextControlName("Motor Config");
                            float maxMotorTorque = EditorGUILayout.FloatField(
                                new GUIContent("Maximum Motor Force",
                                    "The maximum force the motor can use to achieve the desired motor speed. [ 0, 1000000 ]."),
                                wheelJoint2D.motor.maxMotorTorque);

                            if (EditorGUI.EndChangeCheck())
                            {
                                using (new Modification("Configure Motor", wheelJoint2D))
                                {
                                    JointMotor2D motor = wheelJoint2D.motor;
                                    motor.motorSpeed = motorSpeed;
                                    motor.maxMotorTorque = maxMotorTorque;
                                    wheelJoint2D.motor = motor;

                                    wheelJoint2D.useMotor = useMotor;
                                }
                            }
                        }


                        if (GUILayout.Button("Done") ||
                            (Event.current.isKey &&
                             (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.Escape) &&
                             focused))
                        {
                            close();
                        }
                    }));
        }
    }
}