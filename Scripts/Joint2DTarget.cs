using System;
using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using UnityEngine;

[ExecuteInEditMode]
public class Joint2DTarget : MonoBehaviour {
    // Use this for initialization
    private void Start() {
    }

    [NonSerialized] 
    public HashSet<Joint2D> attachedJoints = new HashSet<Joint2D>();

    public void UpdateJoint(Joint2D joint) {
        if (!attachedJoints.Contains(joint)) {
            attachedJoints.Add(joint);
        }
    }

#if UNITY_EDITOR
    public void Update()
    {
        if (!JointEditorSettings.Singleton.showConnectedJoints) {
            Helpers.DestroyImmediate(this);
            return;
        }

        var jointsToRemove = attachedJoints
            .Where(attachedJoint => !attachedJoint
                                    || attachedJoint.connectedBody != rigidbody2D)
            .ToList();

        foreach (Joint2D joint2D in jointsToRemove)
        {
            attachedJoints.Remove(joint2D);
        }

        if (attachedJoints.Count == 0) {
            Helpers.DestroyImmediate(this);
        }
    }
#endif
}