using UnityEngine;

public abstract class Joint2DSettings : MonoBehaviour
{
    public bool showJointGizmos = true;
    public Joint2D attachedJoint;
    [SerializeField]
    private bool isSetup = false;

    public void Setup(Joint2D hingeJoint2D)
    {
        isSetup = true;
        attachedJoint = hingeJoint2D;
    }

    public void OnEnable()
    {
        if (isSetup && attachedJoint == null)
        {
            Debug.Log("!!!");
            //       DestroyImmediate(this);
        }
    }

    public void Update()
    {
        if (isSetup && attachedJoint == null) {
            DestroyImmediate(this);
        }
    }
}
