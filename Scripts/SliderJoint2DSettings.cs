using UnityEngine;

public class SliderJoint2DSettings : Joint2DSettings
{
    public override bool IsValidType() {
        return attachedJoint is SliderJoint2D;
    }

#if UNITY_EDITOR
//    public void OnDrawGizmos() {
//        if (Selection.Contains(gameObject))
//            return;
//        SliderJoint2D joint2D = attachedJoint as SliderJoint2D;
//        if (joint2D == null) {
//            return;
//        }
//
//        DrawAnchorLines();
//    }
#endif

}
