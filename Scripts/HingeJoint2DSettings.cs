using UnityEngine;

[ExecuteInEditMode]
public class HingeJoint2DSettings : MonoBehaviour {
    public bool lockAnchors = true;

    void Awake () {

    }

    void OnEnable() {
    }
	
	void Update () {
	    if (!gameObject.GetComponent<HingeJoint2D>()) {
	        DestroyImmediate(this);
	    }
	}
}
