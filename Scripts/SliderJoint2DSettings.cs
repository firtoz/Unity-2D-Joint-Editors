using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SliderJoint2DSettings : Joint2DSettings
{
    public override bool IsValidType() {
        return attachedJoint is SliderJoint2D;
    }

#if UNITY_EDITOR
    public new void OnDrawGizmos() {
        base.OnDrawGizmos();
        if (Selection.Contains(gameObject))
            return;
        SliderJoint2D joint2D = attachedJoint as SliderJoint2D;
        if (joint2D == null) {
            return;
        }

        DrawAnchorLines();
    }
#endif

}
