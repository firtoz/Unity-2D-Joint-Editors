using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(DistanceJoint2D))]
[CanEditMultipleObjects]
public class DistanceJoint2DEditor : Joint2DEditor {

    public void OnSceneGUI() {
        DistanceJoint2D hingeJoint2D = target as DistanceJoint2D;
        if (hingeJoint2D == null || !hingeJoint2D.enabled) {
            return;
        }
        DistanceJoint2DSettings settings = SettingsHelper.GetOrCreate<DistanceJoint2DSettings>(hingeJoint2D);
        if (settings && !settings.showJointGizmos) {
            return;
        }
    }
}
