using UnityEngine;

//#if UNITY_EDITOR

//#endif

[ExecuteInEditMode]
public abstract class Joint2DSettings : MonoBehaviour {
//#if UNITY_EDITOR
//    private static JointEditorSettings _editorSettings;
//#endif

    public void OnEnable() {
        if (setupComplete && attachedJoint == null) {
            Debug.Log("!!!");
            //       DestroyImmediate(this);
        }
    }

//#if UNITY_EDITOR
//    protected void DrawAnchorLines()
//    {
////        return;
////        if (_editorSettings == null)
////        {
////            _editorSettings = JointEditorSettings.Singleton;
////            if (_editorSettings == null) {
////                return;
////            }
////        }
////        AnchoredJoint2D joint2D = attachedJoint as AnchoredJoint2D;
////        if (joint2D == null)
////        {
////            return;
////        }
////
////        Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
////        Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
////        Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);
////
////        using (new HandleColor(_editorSettings.anchorsToMainBodyColor))
////        {
////            Vector2 mainPosition = GetTargetPositionWithOffset(joint2D, JointHelpers.AnchorBias.Main);
////            Handles.DrawLine(mainAnchorPosition, mainPosition);
////        }
////        if (joint2D.connectedBody)
////        {
////            using (new HandleColor(_editorSettings.anchorsToConnectedBodyColor))
////            {
////                Vector2 connectedPosition = GetTargetPositionWithOffset(joint2D, JointHelpers.AnchorBias.Connected);
////                Handles.DrawLine(connectedAnchorPosition, connectedPosition);
////            }
////        }
//    }
//#endif

    public bool showCustomGizmos = true;
    public bool showDefaultgizmos = true;
    public bool lockAnchors = false;

    [Tooltip("Whether to show the offset widgets or not.")] public bool useOffsets = false;

    public Joint2D attachedJoint;
    [SerializeField] private bool setupComplete;

    public void Setup(Joint2D joint2D) {
        setupComplete = true;
        attachedJoint = joint2D;
    }


    public abstract bool IsValidType();

    public void Update() {
        if (!setupComplete) {
            return;
        }
        if ((attachedJoint == null || !IsValidType())) {
            DestroyImmediate(this);
        }
        else {
            JointEditorSettings jointEditorSettings = JointEditorSettings.Singleton;
            if (jointEditorSettings != null && jointEditorSettings.showConnectedJoints)
            {
                if (!attachedJoint.connectedBody) {
                    return;
                }
                var connectedObject = attachedJoint.connectedBody.gameObject;
                var joint2DTarget = connectedObject.GetComponent<Joint2DTarget>();
                if (joint2DTarget == null) {
                    joint2DTarget = connectedObject.AddComponent<Joint2DTarget>();
                    joint2DTarget.hideFlags = HideFlags.NotEditable;
                }

                joint2DTarget.UpdateJoint(attachedJoint);
            }
        }
    }

//#if UNITY_EDITOR

//    public Vector2 GetTargetPositionWithOffset(AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias)
//    {
//        Vector2 targetPosition = JointHelpers.GetTargetPosition(joint2D, bias);
//
//        if (!useOffsets)
//        {
//            return targetPosition;
//        }
//
//        Transform targetTransform = JointHelpers.GetTargetTransform(joint2D, bias);
//
//        Vector2 offset = GetOffset(bias);
//
//        Vector2 worldOffset = offset;
//        if (targetTransform != null)
//        {
//            worldOffset = Helpers2D.TransformVector(targetTransform, worldOffset);
//        }
//
//        return targetPosition + worldOffset;
//    }
//#endif

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