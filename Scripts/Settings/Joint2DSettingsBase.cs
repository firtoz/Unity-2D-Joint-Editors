using toxicFork.GUIHelpers;
using UnityEngine;

[ExecuteInEditMode]
public abstract class Joint2DSettingsBase : MonoBehaviour {
    public void OnEnable() {
        if (setupComplete && attachedJoint == null) {
            Debug.Log("!!!");
        }

        Update();
    }

    public bool showCustomGizmos = true;
    public bool showDefaultgizmos = true;
    public bool lockAnchors = false;

    public Joint2D attachedJoint;
    [SerializeField] private bool setupComplete;

    public virtual void Setup(Joint2D joint2D) {
        setupComplete = true;
        attachedJoint = joint2D;
    }

    public abstract bool IsValidType();

    public void Update() {
        if (!setupComplete) {
            return;
        }
        if ((attachedJoint == null || !IsValidType())) {
            Helpers.DestroyImmediate(this);
            return;
        }
#if UNITY_EDITOR
        var jointEditorSettings = JointEditorSettings.Singleton;


        if (jointEditorSettings != null) {
            if (jointEditorSettings.disableEverything)
            {
                DestroyImmediate(this);
                return;
            }

            if (jointEditorSettings.showConnectedJoints) {
                if (!attachedJoint.connectedBody) {
                    return;
                }
                var connectedObject = attachedJoint.connectedBody.gameObject;
                var joint2DTarget = connectedObject.GetComponent<Joint2DTarget>();
                if (joint2DTarget == null) {
                    joint2DTarget = connectedObject.AddComponent<Joint2DTarget>();
                    joint2DTarget.hideFlags = HideFlags.NotEditable; //need to show it in inspector because it will be used to show the widgets
                }

                joint2DTarget.UpdateJoint(attachedJoint);
            }
        }
#endif
    }

    public Vector2 mainBodyOffset = Vector2.zero;
    public Vector2 connectedBodyOffset = Vector2.zero;

    public Vector2 GetOffset(JointHelpers.AnchorBias bias) {
        return bias == JointHelpers.AnchorBias.Connected ? connectedBodyOffset : mainBodyOffset;
    }

    public void SetOffset(JointHelpers.AnchorBias bias, Vector2 newOffset) {
        if (bias == JointHelpers.AnchorBias.Connected) {
            connectedBodyOffset = newOffset;
            return;
        }
        mainBodyOffset = newOffset;
    }
}