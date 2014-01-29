using toxicFork.GUIHelpers.DisposableHandles;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[ExecuteInEditMode]
public class HingeJoint2DSettings : Joint2DSettings {
    public bool lockAnchors = false;
    public bool showRadiusHandles = false;
    public bool showAngleLimits = true;
    public float mainAngle;
    public float connectedAngle;

    public enum AngleLimitsDisplayMode {
        Main,
        Connected,
        Both
    }

    public AngleLimitsDisplayMode angleLimitsDisplayMode = AngleLimitsDisplayMode.Main;

    public new void Update() {
        base.Update();

        if (attachedJoint == null) {
            return;
        }

#if UNITY_EDITOR
        if (!(EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)) {
            HingeJoint2D hingeJoint2D = attachedJoint as HingeJoint2D;
            if (hingeJoint2D == null) {
                return;
            }

            Vector2 mainCenter = JointHelpers.GetAnchorPosition(hingeJoint2D,
                JointHelpers.AnchorBias.Main);
            Vector2 mainPosition = JointHelpers.GetTargetPosition(hingeJoint2D,
                JointHelpers.AnchorBias.Main);

            mainAngle = JointHelpers.AngleFromAnchor(mainCenter, mainPosition,
                JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Main));

            if (hingeJoint2D.connectedBody) {
                Vector2 connectedCenter = JointHelpers.GetAnchorPosition(hingeJoint2D,
                    JointHelpers.AnchorBias.Main);
                Vector2 connectedPosition = JointHelpers.GetTargetPosition(hingeJoint2D,
                    JointHelpers.AnchorBias.Connected);

                connectedAngle = JointHelpers.AngleFromAnchor(connectedCenter,
                    connectedPosition,
                    JointHelpers.GetTargetRotation(hingeJoint2D, JointHelpers.AnchorBias.Connected));
            }
        }
#endif
    }


#if UNITY_EDITOR
    public void OnDrawGizmos() {
        if (Selection.Contains(gameObject)) {
            return;
        }

        HingeJoint2D hingeJoint2D = attachedJoint as HingeJoint2D;
        if (hingeJoint2D == null) {
            return;
        }

        Vector2 anchorPosition = JointHelpers.GetAnchorPosition(hingeJoint2D);

        Ray ray = HandleUtility.GUIPointToWorldRay(HandleUtility.WorldToGUIPoint(anchorPosition));

        float radius = HandleUtility.GetHandleSize(ray.origin)*0.125f;

        Vector3 screenAnchorPosition = ray.origin + ray.direction*radius*2;

        Handles.CircleCap(0, screenAnchorPosition, Quaternion.LookRotation(ray.direction), radius*1.1f);
        Gizmos.DrawSphere(screenAnchorPosition, radius);
        using (new HandleColor(Color.green)) {
            Handles.DrawLine(screenAnchorPosition, transform.position);
        }
        if (hingeJoint2D.connectedBody) {
            using (new HandleColor(Color.red))
            {
                Handles.DrawLine(screenAnchorPosition, hingeJoint2D.connectedBody.transform.position);
            }
        }
    }
#endif
}