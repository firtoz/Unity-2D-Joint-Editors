using UnityEngine;

[ExecuteInEditMode]
public class HingeJoint2DSettings : MonoBehaviour {
    public bool lockAnchors = false;
    public bool showJointGizmos = true;
    public HingeJoint2D attachedJoint;
//    public Vector2 worldAnchor;
//    public Vector2 worldConnectedAnchor;

    public void Awake()
    {
    }

    public void OnEnable()
    {
    }

    public void Update() {
        if (!gameObject.GetComponent<HingeJoint2D>()) {
            DestroyImmediate(this);
        }
    }

    public void Setup(HingeJoint2D hingeJoint2D) {
        attachedJoint = hingeJoint2D;
//        worldAnchor = JointEditorHelpers.GetAnchorPosition(hingeJoint2D);
//        worldConnectedAnchor = JointEditorHelpers.GetConnectedAnchorPosition(hingeJoint2D);
    }
}
