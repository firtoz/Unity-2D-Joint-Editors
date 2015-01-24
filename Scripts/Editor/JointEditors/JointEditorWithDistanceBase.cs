using System;
using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableEditor;
using toxicFork.GUIHelpers.DisposableGUI;
using toxicFork.GUIHelpers.DisposableHandles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public abstract class JointEditorWithDistanceBase<T> : Joint2DEditorBase where T : AnchoredJoint2D {
    public abstract float GetDistance(T joint);
    public abstract void SetDistance(T joint, float distance);

// ReSharper disable StaticFieldInGenericType
    private static readonly HashSet<string> ControlNames = new HashSet<string> { "distance" };
// ReSharper restore StaticFieldInGenericType

    protected override HashSet<string> GetControlNames()
    {
        return ControlNames;
    }

    protected override bool SingleAnchorGUI(AnchoredJoint2D joint2D, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias)
    {
        T jointWithDistance = joint2D as T;
        if (!jointWithDistance) {
            return false;
        }

        Vector2 mainAnchorPosition = JointHelpers.GetMainAnchorPosition(jointWithDistance);
        Vector2 connectedAnchorPosition = JointHelpers.GetConnectedAnchorPosition(jointWithDistance);
        if (Vector2.Distance(mainAnchorPosition, connectedAnchorPosition) > AnchorEpsilon)
        {
            using (new HandleColor(Color.green))
            {
                Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
            }
        }


        DrawDistance(jointWithDistance, anchorInfo, bias);

        Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);

        return false;
    }

    protected override Vector2 AlterDragResult(int sliderID, Vector2 position, AnchoredJoint2D joint,
        JointHelpers.AnchorBias bias, float snapDistance)
    {
        if (!EditorGUI.actionKey)
        {
            return position;
        }

        JointHelpers.AnchorBias otherBias = bias == JointHelpers.AnchorBias.Main
            ? JointHelpers.AnchorBias.Connected
            : JointHelpers.AnchorBias.Main;

        T jointWithDistance = (T)joint;

        AnchorSliderState anchorSliderState = StateObject.Get<AnchorSliderState>(sliderID);
        Vector2 currentMousePosition = Helpers2D.GUIPointTo2DPosition(Event.current.mousePosition);
        Vector2 currentAnchorPosition = currentMousePosition - anchorSliderState.mouseOffset;

        Vector2 otherAnchorPosition = JointHelpers.GetAnchorPosition(jointWithDistance, otherBias);
        Vector2 diff = otherAnchorPosition - currentAnchorPosition;
        if (diff.magnitude <= Mathf.Epsilon)
        {
            diff = -Vector2.up;
        }

        Vector2 normalizedDiff = diff.normalized;

        Vector2 wantedAnchorPosition = otherAnchorPosition - normalizedDiff * GetDistance(jointWithDistance);

        if (Vector2.Distance(position, wantedAnchorPosition) < snapDistance)
        {
            return wantedAnchorPosition;
        }

        return position;
    }

    public override Bounds OnGetFrameBounds()
    {
        Bounds baseBounds = base.OnGetFrameBounds();

        foreach (T joint2D in targets.Cast<T>())
        {
            Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
            Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
            Vector2 diff = connectedAnchorPosition - mainAnchorPosition;
            if (diff.magnitude <= Mathf.Epsilon)
            {
                diff = -Vector2.up;
            }
            Vector2 normalizedDiff = diff.normalized;
            Vector2 wantedMainAnchorPosition = connectedAnchorPosition - normalizedDiff * GetDistance(joint2D);

            baseBounds.Encapsulate(wantedMainAnchorPosition);
        }

        return baseBounds;
    }

    private void DrawDistance(T jointWithDistance, AnchorInfo anchorInfo, JointHelpers.AnchorBias bias)
    {
        if (jointWithDistance == null)
        {
            return;
        }

        JointHelpers.AnchorBias otherBias = bias == JointHelpers.AnchorBias.Main
            ? JointHelpers.AnchorBias.Connected
            : JointHelpers.AnchorBias.Main;

        Vector2 anchorPosition = JointHelpers.GetAnchorPosition(jointWithDistance, bias);
        Vector2 otherAnchorPosition = JointHelpers.GetAnchorPosition(jointWithDistance, otherBias);
        Vector2 diff = anchorPosition - otherAnchorPosition;
        if (diff.magnitude <= Mathf.Epsilon)
        {
            diff = Vector2.up * (bias == JointHelpers.AnchorBias.Connected ? 1 : -1);
        }
        Vector2 normalizedDiff = diff.normalized;

        JointHelpers.AnchorBias wantedBias;
        switch (GetSettings(jointWithDistance).anchorPriority)
        {
            case JointWithDistanceSettings.AnchorPriority.Main:
                wantedBias = JointHelpers.AnchorBias.Main;
                break;
            case JointWithDistanceSettings.AnchorPriority.Connected:
                wantedBias = JointHelpers.AnchorBias.Connected;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (bias == wantedBias && EditorGUI.actionKey && GUIUtility.hotControl == anchorInfo.GetControlID("slider"))
        {
            Handles.DrawWireDisc(otherAnchorPosition, Vector3.forward, GetDistance(jointWithDistance));
        }

        if (bias != wantedBias)
        {
            int distanceControlID = anchorInfo.GetControlID("distance");

            EditorGUI.BeginChangeCheck();

            float newDistance;
            using (new HandleColor(isCreatedByTarget ? new Color(1, 1, 1, editorSettings.connectedJointTransparency) : Color.white))
            {
                newDistance = EditorHelpers.LineSlider(distanceControlID, otherAnchorPosition, GetDistance(jointWithDistance), Helpers2D.GetAngle(normalizedDiff), 0.125f, true);

                EditorHelpers.DrawThickLine(anchorPosition, otherAnchorPosition + normalizedDiff * newDistance,
                    Vector2.Distance(anchorPosition, otherAnchorPosition) > newDistance ? 2 : 1, true);
            }

            if (Event.current.type == EventType.repaint)
            {
                if (EditorHelpers.IsWarm(distanceControlID) && DragAndDrop.objectReferences.Length == 0)
                {

                    GUIContent labelContent = new GUIContent(string.Format("Distance: {0:0.00}", GetDistance(jointWithDistance)));

                    Vector2 sliderPosition = otherAnchorPosition + normalizedDiff * GetDistance(jointWithDistance);

                    float fontSize = HandleUtility.GetHandleSize(sliderPosition) * (1f / 64f);

                    float labelOffset = fontSize * EditorHelpers.FontWithBackgroundStyle.CalcSize(labelContent).y + fontSize * 20 * Mathf.Abs(Mathf.Cos(Mathf.Deg2Rad * Helpers2D.GetAngle(normalizedDiff)));

                    EditorHelpers.OverlayLabel((Vector3)sliderPosition + (Camera.current.transform.up * labelOffset), labelContent, EditorHelpers.FontWithBackgroundStyle);
                }
            }


            if (EditorGUI.EndChangeCheck())
            {
                using (new Modification("Change Distance", jointWithDistance))
                {
                    if (newDistance < 0)
                    {
                        SetDistance(jointWithDistance, 0f);
                    }
                    else
                    {
                        float distanceBetweenAnchors = Vector2.Distance(otherAnchorPosition, anchorPosition);
                        SetDistance(jointWithDistance, EditorGUI.actionKey && Mathf.Abs(newDistance - distanceBetweenAnchors) <
                                           HandleUtility.GetHandleSize(anchorPosition) * 0.125f
                            ? distanceBetweenAnchors
                            : newDistance);
                    }
                }
            }

            DistanceContext(jointWithDistance, distanceControlID);
        }
    }


    private void DistanceContext(T jointWithDistance, int controlID)
    {
        Vector2 mousePosition = Event.current.mousePosition;

        EditorHelpers.ContextClick(controlID, () =>
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Edit Distance"), false, () =>
                ShowUtility("Edit Distance",
                    new Rect(mousePosition.x - 250, mousePosition.y + 15, 500, EditorGUIUtility.singleLineHeight * 3),
                    delegate(Action close, bool focused)
                    {
                        EditorGUI.BeginChangeCheck();
                        float newDistance = EditorGUILayout.FloatField("Distance", GetDistance(jointWithDistance));
                        if (EditorGUI.EndChangeCheck())
                        {
                            using (new Modification("Change Distance", jointWithDistance))
                            {
                                SetDistance(jointWithDistance, newDistance);
                            }
                        }
                        if (GUILayout.Button("Done") ||
                            (Event.current.isKey &&
                             (Event.current.keyCode == KeyCode.Escape) &&
                             focused))
                        {
                            close();
                        }
                    }));
            menu.ShowAsContext();
        });
    }

// ReSharper disable StaticFieldInGenericType
    private static readonly GUIContent AngleLimitsModeContent =
// ReSharper restore StaticFieldInGenericType
        new GUIContent("Anchor Priority",
            "Which anchor's angle limits would you like to see? If there is no connected body this setting will be ignored.");

    private void SelectAngleLimitsMode(SerializedObject serializedSettings, bool enabled)
    {
        EditorGUI.BeginChangeCheck();
        JointWithDistanceSettings.AnchorPriority value;

        using (new GUIEnabled(enabled))
        {
            SerializedProperty anchorPriority = serializedSettings.FindProperty("anchorPriority");
            EditorGUILayout.PropertyField(anchorPriority, AngleLimitsModeContent);
            value = (JointWithDistanceSettings.AnchorPriority)
                Enum.Parse(typeof(JointWithDistanceSettings.AnchorPriority),
                    anchorPriority.enumNames[anchorPriority.enumValueIndex]);
        }

        if (EditorGUI.EndChangeCheck())
        {
            foreach (Object tar in targets)
            {
                T jointWithDistance = tar as T;
                JointWithDistanceSettings settings = GetSettings(jointWithDistance);

                using (new Modification("toggle angle limits display mode", settings))
                {
                    settings.anchorPriority = value;
                }
            }
        }
    }

    protected abstract JointWithDistanceSettings GetSettings(T jointWithDistance);


    protected override void InspectorDisplayGUI(bool enabled)
    {
        List<Object> allSettings =
            targets.Cast<T>()
                .Select(jointWithDistance => GetSettings(jointWithDistance))
                .Where(distanceSettings => distanceSettings != null).Cast<Object>().ToList();

        SerializedObject serializedSettings = new SerializedObject(allSettings.ToArray());
        SelectAngleLimitsMode(serializedSettings, enabled);
    }

    protected override void OwnershipMoved(AnchoredJoint2D cloneJoint)
    {
        T jointWithDistance = cloneJoint as T;
        if (!jointWithDistance)
        {
            return;
        }

        var settings = GetSettings(jointWithDistance);

        if (settings.anchorPriority == JointWithDistanceSettings.AnchorPriority.Main)
        {
            settings.anchorPriority = JointWithDistanceSettings.AnchorPriority.Connected;
        }
        else if (settings.anchorPriority == JointWithDistanceSettings.AnchorPriority.Connected)
        {
            settings.anchorPriority = JointWithDistanceSettings.AnchorPriority.Main;
        }
    }
}
