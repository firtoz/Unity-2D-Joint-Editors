using toxicFork.GUIHelpers.DisposableHandles;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public abstract class Joint2DSettings : MonoBehaviour
{
    public bool showJointGizmos = true;
    public bool lockAnchors = false;

    public Joint2D attachedJoint;
    [SerializeField]
    private bool setupComplete = false;

    public void Setup(Joint2D hingeJoint2D)
    {
        setupComplete = true;
        attachedJoint = hingeJoint2D;
    }

    public void OnEnable()
    {
        if (setupComplete && attachedJoint == null)
        {
            Debug.Log("!!!");
            //       DestroyImmediate(this);
        }
    }

    public void Update()
    {
        if (attachedJoint == null) {
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

        using (new HandleColor(Color.green))
        {
            Handles.DrawLine(mainAnchorPosition, transform.position);
        }
        if (joint2D.connectedBody)
        {
            using (new HandleColor(Color.red))
            {
                Handles.DrawLine(mainAnchorPosition, joint2D.connectedBody.transform.position);
            }
        }
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
