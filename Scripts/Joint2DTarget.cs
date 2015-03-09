#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using toxicFork.GUIHelpers;
using UnityEditor;
#endif
using UnityEngine;


[ExecuteInEditMode]
public class Joint2DTarget : MonoBehaviour
{
#if UNITY_EDITOR

    public List<Joint2D> attachedJoints = new List<Joint2D>();

    public void UpdateJoint(Joint2D joint) {
        initialized = true;
        if (!attachedJoints.Contains(joint)) {
            attachedJoints.Add(joint);
        }
    }

    [SerializeField]
    private bool initialized;

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

        var jointEditorSettings = JointEditorSettings.Singleton;
        if (jointEditorSettings == null || !jointEditorSettings.showConnectedJoints || jointEditorSettings.disableEverything)
        {
            Helpers.DestroyImmediate(this);
            return;
        }

        var jointsToRemove = attachedJoints
            .Where(attachedJoint => !attachedJoint || attachedJoint.connectedBody != GetComponent<Rigidbody2D>())
            .ToList();

        foreach (var joint2D in jointsToRemove) {
            attachedJoints.Remove(joint2D);
        }

        if (attachedJoints.Count == 0) {
            wantsDestroy = true;
            EditorApplication.delayCall += () => Helpers.DestroyImmediate(this);
        }
    }
#endif
}