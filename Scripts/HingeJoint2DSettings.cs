using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class HingeJoint2DSettings : MonoBehaviour {
    public bool lockAnchors = true;
	public HingeJoint2D attachedJoint;

	void Awake () {

    }

    void OnEnable() {
    }
	
	void Update () {
	    if (!gameObject.GetComponent<HingeJoint2D>()) {
	        DestroyImmediate(this);
	    }
	}

	private static HingeJoint2DSettings Create(HingeJoint2D hingeJoint2D) {
        HingeJoint2DSettings settings = Undo.AddComponent<HingeJoint2DSettings>(hingeJoint2D.gameObject);
		settings.attachedJoint = hingeJoint2D;
//		settings.hideFlags = HideFlags.HideInInspector;
		return settings;
	}

	public static HingeJoint2DSettings Get(HingeJoint2D hingeJoint2D) {
		HingeJoint2DSettings[] allSettings = hingeJoint2D.GetComponents<HingeJoint2DSettings>();

		foreach (HingeJoint2DSettings settings in allSettings.Where(settings => settings.attachedJoint == hingeJoint2D))
		{
			return settings;
		}
		return null;
	}

	public static HingeJoint2DSettings GetOrCreate(HingeJoint2D hingeJoint2D) {
		return Get(hingeJoint2D) ?? Create(hingeJoint2D);
	}
}
