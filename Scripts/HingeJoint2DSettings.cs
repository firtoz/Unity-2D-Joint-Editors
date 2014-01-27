using toxicFork.GUIHelpers.Disposable;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[ExecuteInEditMode]
public class HingeJoint2DSettings : MonoBehaviour {
    public bool lockAnchors = false;
    public bool showJointGizmos = true;
    public bool showAngleLimits = true;
    public HingeJoint2D attachedJoint;
	public float mainAngle;
	public float connectedAngle;

    public enum AngleLimitsDisplayMode {
        Main,
        Connected,
        Both
    }

    public AngleLimitsDisplayMode angleLimitsDisplayMode = AngleLimitsDisplayMode.Main;

    public void OnEnable()
    {
    }

    public void Update() {
        if (attachedJoint == null)
        {
            DestroyImmediate(this);
        }

#if UNITY_EDITOR
        if (!(EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying)) {
            HingeJoint2D hingeJoint2D = attachedJoint;
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

    public void Setup(HingeJoint2D hingeJoint2D) {
        attachedJoint = hingeJoint2D;
    }
    
#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        if (Selection.Contains(gameObject)) {
            return;
        }

	    Vector2 anchorPosition = JointHelpers.GetAnchorPosition(attachedJoint);

        Ray ray = HandleUtility.GUIPointToWorldRay(HandleUtility.WorldToGUIPoint(anchorPosition));

        float radius = HandleUtility.GetHandleSize(ray.origin) * 0.125f;

        Vector3 screenAnchorPosition = ray.origin + ray.direction * radius*2;

        Handles.CircleCap(0, screenAnchorPosition, Quaternion.LookRotation(ray.direction), radius * 1.1f);
        Gizmos.DrawSphere(screenAnchorPosition, radius);
	}
#endif
}
