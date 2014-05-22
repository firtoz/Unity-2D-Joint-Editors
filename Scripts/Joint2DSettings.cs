using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using toxicFork.GUIHelpers;
using toxicFork.GUIHelpers.DisposableHandles;
#endif

[ExecuteInEditMode]
public abstract class Joint2DSettings : MonoBehaviour
{

#if UNITY_EDITOR
    private static JointEditorSettings _editorSettings;
#endif

    public void OnEnable()
    {
        if (setupComplete && attachedJoint == null)
        {
            Debug.Log("!!!");
            //       DestroyImmediate(this);
        }

#if UNITY_EDITOR
        _editorSettings = JointEditorSettings.Singleton;
//        lastJointHash = attachedJoint != null ? attachedJoint.GetHashCode() : 0;
//        Joint2DManager.AddJointSettings(this);
#endif
    }

    public void OnDisable()
    {
#if UNITY_EDITOR
//        Joint2DManager.RemoveJointSettings(this);
#endif
    }

#if UNITY_EDITOR

    protected void DrawAnchorLines()
    {
        if (_editorSettings == null)
        {
            return;
        }
        AnchoredJoint2D joint2D = attachedJoint as AnchoredJoint2D;
        if (joint2D == null)
        {
            return;
        }

        Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
        Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
        Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);

        using (new HandleColor(_editorSettings.mainDiscColor))
        {
            Vector2 mainPosition = GetTargetPositionWithOffset(joint2D, JointHelpers.AnchorBias.Main);
            Handles.DrawLine(mainAnchorPosition, mainPosition);
        }
        if (joint2D.connectedBody)
        {
            using (new HandleColor(_editorSettings.connectedDiscColor))
            {
                Vector2 connectedPosition = GetTargetPositionWithOffset(joint2D, JointHelpers.AnchorBias.Connected);
                Handles.DrawLine(connectedAnchorPosition, connectedPosition);
            }
        }
    }
#endif

    public bool showJointGizmos = true;
    public bool lockAnchors = false;

    public Joint2D attachedJoint;
    [SerializeField]
    private bool setupComplete;

    public void Setup(Joint2D joint2D)
    {
        setupComplete = true;
        attachedJoint = joint2D;
    }


    public abstract bool IsValidType();

//    private int lastJointHash = 0;

    public void Update() {
        if (attachedJoint == null || !IsValidType()) {
            DestroyImmediate(this);
        }
#if UNITY_EDITOR
//        else {
//            int jointHash = attachedJoint != null ? attachedJoint.GetHashCode() : 0;
//            if (jointHash != lastJointHash) {
//                lastJointHash = jointHash;
//                Joint2DManager.UpdateJointSettings(this);
//            }
//        }
#endif
    }

#if UNITY_EDITOR

    protected Vector2 GetTargetPositionWithOffset(AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias)
    {
        Transform targetTransform = JointHelpers.GetTargetTransform(joint2D, bias);
        Vector2 offset = GetOffset(bias);

        Vector2 worldOffset = offset;
        if (targetTransform != null)
        {
            worldOffset = Helpers2D.TransformVector(targetTransform, worldOffset);
        }

        return JointHelpers.GetTargetPosition(joint2D, bias) + worldOffset;
    }
#endif

    public Vector2 mainBodyOffset = Vector2.zero;
    public Vector2 connectedBodyOffset = Vector2.zero;

    public Vector2 GetOffset(JointHelpers.AnchorBias bias)
    {
        return bias == JointHelpers.AnchorBias.Connected ? connectedBodyOffset : mainBodyOffset;
    }

    public void SetOffset(JointHelpers.AnchorBias bias, Vector2 newOffset)
    {
        if (bias == JointHelpers.AnchorBias.Connected)
        {
            connectedBodyOffset = newOffset;
            return;
        }
        mainBodyOffset = newOffset;
    }
}
