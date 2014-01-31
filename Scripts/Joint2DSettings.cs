using UnityEngine;

[ExecuteInEditMode]
public abstract class Joint2DSettings : MonoBehaviour
{
    public bool showJointGizmos = true;
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
}
