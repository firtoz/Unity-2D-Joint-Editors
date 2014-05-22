using UnityEngine;

public class WheelJoint2DSettings : Joint2DSettings {
    public override bool IsValidType() {
        return attachedJoint is WheelJoint2D;
    }

#if UNITY_EDITOR
//    public void OnDrawGizmos() {
//        if (Selection.Contains(gameObject)) {
//            return;
//        }
//        WheelJoint2D joint2D = attachedJoint as WheelJoint2D;
//        if (joint2D == null) {
//            return;
//        }
//
//        DrawAnchorLines();
//    }
#endif
}