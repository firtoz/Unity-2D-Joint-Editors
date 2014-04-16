using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

public class WheelJoint2DSettings : Joint2DSettings {
    public override bool IsValidType() {
        return attachedJoint is WheelJoint2D;
    }

#if UNITY_EDITOR
    public new void OnDrawGizmos() {
        base.OnDrawGizmos();
        if (Selection.Contains(gameObject)) {
            return;
        }
        WheelJoint2D joint2D = attachedJoint as WheelJoint2D;
        if (joint2D == null) {
            return;
        }

        DrawAnchorLines();
    }
#endif
}