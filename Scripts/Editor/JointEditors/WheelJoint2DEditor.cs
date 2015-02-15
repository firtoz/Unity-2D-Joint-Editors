using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableEditor;
using toxicFork.GUIHelpers.DisposableEditorGUI;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (WheelJoint2D))]
[CanEditMultipleObjects]
public class WheelJoint2DEditor : JointEditorWithAngleBase<WheelJoint2D> {
    private static readonly HashSet<string> ControlNames = new HashSet<string> {
        "suspensionAngle"
    };

    protected override HashSet<string> GetControlNames() {
        return ControlNames;
    }

    protected override Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
        JointHelpers.AnchorBias bias, float snapDistance) {
        if (!EditorGUI.actionKey) {
            return position;
        }

        var wheelJoint2D = (WheelJoint2D) joint;

        if (!SettingsHelper.GetOrCreate(joint).lockAnchors &&
            !(Vector2.Distance(JointHelpers.GetMainAnchorPosition(joint),
                JointHelpers.GetConnectedAnchorPosition(joint)) <= AnchorEpsilon)) {
            var wantedAnchorPosition = GetWantedAnchorPosition(wheelJoint2D, bias, position);

            if (Vector2.Distance(position, wantedAnchorPosition) < snapDistance) {
                return wantedAnchorPosition;
            }
        }

        return position;
    }

    protected override bool DragBothAnchorsWhenLocked() {
        return false;
    }

    protected override void ReAlignAnchors(AnchoredJoint2D joint2D, JointHelpers.AnchorBias alignmentBias) {
        var wheelJoint2D = (WheelJoint2D) joint2D;

        //align the angle to the connected anchor
        var direction = JointHelpers.GetConnectedAnchorPosition(joint2D) -
                        JointHelpers.GetMainAnchorPosition(joint2D);

        if (direction.magnitude > AnchorEpsilon) {
            var wantedAngle = Helpers2D.GetAngle(direction);

            EditorHelpers.RecordUndo("Realign angle", wheelJoint2D);
            var susp = wheelJoint2D.suspension;
            susp.angle = wantedAngle - wheelJoint2D.transform.eulerAngles.z;
            wheelJoint2D.suspension = susp;
        }
    }

    protected override Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias) {
        return GetWantedAnchorPosition(anchoredJoint2D, bias, JointHelpers.GetAnchorPosition(anchoredJoint2D, bias));
    }

    private static Vector2 GetWantedAnchorPosition(AnchoredJoint2D anchoredJoint2D, JointHelpers.AnchorBias bias,
        Vector2 position) {
        var wheelJoint2D = (WheelJoint2D) anchoredJoint2D;

        var otherBias = JointHelpers.GetOppositeBias(bias);

        var worldAngle = wheelJoint2D.transform.eulerAngles.z + wheelJoint2D.suspension.angle;

        var slideRay = new Ray(JointHelpers.GetAnchorPosition(wheelJoint2D, otherBias),
            Helpers2D.GetDirection(worldAngle));
        var wantedAnchorPosition = Helpers2D.ClosestPointToRay(slideRay, position);
        return wantedAnchorPosition;
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias) {
        var wheelJoint2D = (WheelJoint2D) joint2D;

        var mainAnchorPosition = JointHelpers.GetMainAnchorPosition(wheelJoint2D);
        var connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(wheelJoint2D);

        var suspensionAngleControlID = anchorInfo.GetControlID("suspensionAngle");


        if (bias != JointHelpers.AnchorBias.Connected &&
            (GUIUtility.hotControl == suspensionAngleControlID || !Event.current.shift)) {
            DrawAngleWidget(wheelJoint2D, suspensionAngleControlID);
        }

        //if the joint anchors are being moved, then show snap lines
        var sliderControlID = anchorInfo.GetControlID("slider");
        if (GUIUtility.hotControl == sliderControlID) {
            var snap = GetWantedAnchorPosition(wheelJoint2D, bias);
            using (new HandleColor(new Color(1, 1, 1, .5f))) {
                Handles.DrawLine(connectedAnchorPosition, snap);
                Handles.DrawLine(mainAnchorPosition, snap);
            }
        }

        using (new HandleColor(new Color(1, 1, 1, 0.5f))) {
            Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
            if (wheelJoint2D.connectedBody && GUIUtility.hotControl == suspensionAngleControlID) {
                Handles.DrawLine(mainAnchorPosition, GetTargetPosition(wheelJoint2D, JointHelpers.AnchorBias.Connected));
            }
        }
        return false;
    }

    protected override void SetAngle(WheelJoint2D joint2D, float wantedAngle) {
        var suspension = joint2D.suspension;
        suspension.angle = wantedAngle;
        joint2D.suspension = suspension;
    }

    protected override float GetAngle(WheelJoint2D joint2D) {
        return joint2D.suspension.angle;
    }

    protected override bool PostAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo info, List<Vector2> otherAnchors,
        JointHelpers.AnchorBias bias) {
        var wheelJoint2D = joint2D as WheelJoint2D;
        if (wheelJoint2D == null) {
            return false;
        }


        var suspensionAngleControlID = info.GetControlID("suspensionAngle");

        if (Event.current.type == EventType.repaint) {
            if (EditorHelpers.IsWarm(suspensionAngleControlID) && DragAndDrop.objectReferences.Length == 0) {
                var suspensionAngle = wheelJoint2D.suspension.angle;

                var labelContent = new GUIContent(String.Format("{0:0.00}", suspensionAngle));
                Vector3 mainAnchorPosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);

                var fontSize = HandleUtility.GetHandleSize(mainAnchorPosition) * (1f / 64f);

                var labelOffset = fontSize * EditorHelpers.FontWithBackgroundStyle.CalcSize(labelContent).y;

                EditorHelpers.OverlayLabel(mainAnchorPosition + (Camera.current.transform.up * labelOffset),
                    labelContent,
                    EditorHelpers.FontWithBackgroundStyle);
            }
        }
        else {
            if (EditorHelpers.IsWarm(suspensionAngleControlID)
                && DragAndDrop.objectReferences.Length == 0) {
                if (SceneView.lastActiveSceneView) {
                    SceneView.lastActiveSceneView.Repaint();
                }
            }
        }

        return false;
    }

    protected override void OwnershipMoved(AnchoredJoint2D cloneJoint) {
        //swap limits
        var wheelJoint2D = cloneJoint as WheelJoint2D;
        if (!wheelJoint2D) {
            return;
        }

        var worldAngle = wheelJoint2D.connectedBody.transform.eulerAngles.z + wheelJoint2D.suspension.angle;

        var suspension = wheelJoint2D.suspension;

        suspension.angle = (180.0f + worldAngle) - wheelJoint2D.transform.eulerAngles.z;

        wheelJoint2D.suspension = suspension;
    }


    protected override GUIContent GetAngleEditinGUIContent() {
        return new GUIContent("Suspension Angle",
            "The world movement angle for the suspension. [ -1000000, 1000000 ].");
    }

    protected override void ExtraMenuItems(GenericMenu menu, AnchoredJoint2D joint) {
        base.ExtraMenuItems(menu, joint);

        var wheelJoint2D = joint as WheelJoint2D;
        if (wheelJoint2D != null) {
            var mousePosition = Event.current.mousePosition;

            menu.AddItem(new GUIContent("Configure Suspension"), false, () => {
                ShowUtility("Configure Suspension",
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 6),
                    delegate(Action close, bool focused) {
                        EditorGUILayout.LabelField(new GUIContent("Suspension", "The joint suspension."));

                        using (new Indent()) {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Suspension.m_DampingRatio"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Suspension.m_Frequency"));
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Suspension.m_Angle"));

                            if (EditorGUI.EndChangeCheck()) {
                                using (new Modification("Configure Suspension", wheelJoint2D)) {
                                    serializedObject.ApplyModifiedProperties();
                                }
                            }
                        }

                        if (GUILayout.Button("Done") ||
                            (Event.current.isKey &&
                             (Event.current.keyCode == KeyCode.Escape) &&
                             focused))
                        {
                            close();
                        }
                    });
            });

            menu.AddItem(new GUIContent("Use Motor"), wheelJoint2D.useMotor, () => {
                EditorHelpers.RecordUndo("Use Motor", wheelJoint2D);
                wheelJoint2D.useMotor = !wheelJoint2D.useMotor;
                EditorUtility.SetDirty(wheelJoint2D);
            });

            menu.AddItem(new GUIContent("Configure Motor"), false, () =>
                ShowUtility(
                    "Configure Motor",
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 6),
                    delegate(Action close, bool focused) {
                        EditorGUILayout.LabelField(new GUIContent("Wheel Joint 2D Motor", "The joint motor."));
                        using (new Indent()) {
                            EditorGUI.BeginChangeCheck();

                            var useMotor =
                                EditorGUILayout.Toggle(
                                    new GUIContent("Use Motor", "Whether to use the joint motor or not."),
                                    wheelJoint2D.useMotor);

                            GUI.SetNextControlName("Motor Config");
                            var motorSpeed = EditorGUILayout.FloatField(
                                new GUIContent("Motor Speed",
                                    "The target motor speed in degrees/second. [-100000, 1000000 ]."),
                                wheelJoint2D.motor.motorSpeed);
                            GUI.SetNextControlName("Motor Config");
                            var maxMotorTorque = EditorGUILayout.FloatField(
                                new GUIContent("Maximum Motor Force",
                                    "The maximum force the motor can use to achieve the desired motor speed. [ 0, 1000000 ]."),
                                wheelJoint2D.motor.maxMotorTorque);

                            if (EditorGUI.EndChangeCheck()) {
                                using (new Modification("Configure Motor", wheelJoint2D)) {
                                    var motor = wheelJoint2D.motor;
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
                             focused)) {
                            close();
                        }
                    }));
        }
    }

    protected override JointSettingsWithBias GetSettings(WheelJoint2D joint2D) {
        return SettingsHelper.GetOrCreate<WheelJoint2DSettings>(joint2D);
    }
}