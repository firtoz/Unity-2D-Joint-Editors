using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class Joint2DTarget : MonoBehaviour {
    public List<Joint2D> attachedJoints = new List<Joint2D>();

    public void UpdateJoint(Joint2D joint) {
        initialized = true;
        if (!attachedJoints.Contains(joint)) {
            attachedJoints.Add(joint);
        }
    }

    [SerializeField]
    private bool initialized;

#if UNITY_EDITOR
    public void OnEnable()
    {
        if (initialized) {
            Update();
        }
    }

    [SerializeField] 
    private bool wantsDestroy;

    public void Update()
    {
        if (wantsDestroy)
        {
            return;
        }

        if (!JointEditorSettings.Singleton.showConnectedJoints) {
            Helpers.DestroyImmediate(this);
            return;
        }

        var jointsToRemove = attachedJoints
            .Where(attachedJoint => !attachedJoint || attachedJoint.connectedBody != rigidbody2D)
            .ToList();

        foreach (Joint2D joint2D in jointsToRemove) {
            attachedJoints.Remove(joint2D);
        }

        if (attachedJoints.Count == 0) {
            wantsDestroy = true;
            EditorApplication.delayCall += () => Helpers.DestroyImmediate(this);
        }
    }
#endif
}