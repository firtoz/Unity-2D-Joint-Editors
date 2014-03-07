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
    protected static readonly AssetUtils Utils = new AssetUtils("2DJointEditors/Data");

    private static JointEditorSettings _editorSettings;

    protected static JointEditorSettings editorSettings
    {
        get
        {
            if (_editorSettings != null)
            {
                return _editorSettings;
            }
            _editorSettings = Utils.GetOrCreateAsset<JointEditorSettings>("settings.asset");
            if (_editorSettings == null)
            {
                Debug.Log("deleted!");
            }
            return _editorSettings;
        }
    }

    protected void DrawAnchorLines()
    {
        DistanceJoint2D joint2D = attachedJoint as DistanceJoint2D;
        if (joint2D == null)
        {
            return;
        }

        Vector2 mainAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Main);
        Vector2 connectedAnchorPosition = JointHelpers.GetAnchorPosition(joint2D, JointHelpers.AnchorBias.Connected);
        Handles.DrawLine(mainAnchorPosition, connectedAnchorPosition);

        using (new HandleColor(editorSettings.mainDiscColor))
        {
            Vector2 mainPosition = GetTargetPositionWithOffset(joint2D, JointHelpers.AnchorBias.Main);
            Handles.DrawLine(mainAnchorPosition, mainPosition);
        }
        if (joint2D.connectedBody)
        {
            using (new HandleColor(editorSettings.connectedDiscColor))
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

    public void OnEnable()
    {
        if (setupComplete && attachedJoint == null)
        {
            Debug.Log("!!!");
            //       DestroyImmediate(this);
        }
    }

    public abstract bool IsValidType();

    public void Update() {
        if (attachedJoint == null || !IsValidType())
        {
            DestroyImmediate(this);
        }
    }

#if UNITY_EDITOR
    public void OnDrawGizmos() {
        if (Selection.Contains(gameObject))
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


        DrawSphereOnScreen(mainAnchorPosition, (1f/8));
        DrawSphereOnScreen(connectedAnchorPosition, (1f / 8));
    }

    protected Vector2 GetTargetPositionWithOffset(AnchoredJoint2D joint2D, JointHelpers.AnchorBias bias)
    {
        Transform targetTransform = JointHelpers.GetTargetTransform(joint2D, bias);
        Vector2 offset = GetOffset(bias);

        Vector2 worldOffset = offset;
        if (targetTransform != null)
        {
            worldOffset = Helpers2D.Transform2DVector(targetTransform, worldOffset);
        }

        return JointHelpers.GetTargetPosition(joint2D, bias) + worldOffset;
    }

    private static void DrawSphereOnScreen(Vector2 position, float radius) {
        Ray ray = HandleUtility.GUIPointToWorldRay(HandleUtility.WorldToGUIPoint(position));

        float drawRadius = HandleUtility.GetHandleSize(ray.origin) * radius;

        Vector3 drawPosition = ray.origin + ray.direction * drawRadius * 2;

        Quaternion rotation = Quaternion.LookRotation(ray.direction, Vector3.up);
        Handles.CircleCap(0, drawPosition, rotation, drawRadius + HandleUtility.GetHandleSize(drawPosition) * (1f/64));

        Gizmos.DrawSphere(drawPosition, drawRadius);
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
