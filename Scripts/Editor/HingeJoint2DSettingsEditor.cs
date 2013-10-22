using UnityEditor;

[CustomEditor(typeof (HingeJoint2DSettings))]
[CanEditMultipleObjects]
public class HingeJoint2DSettingsEditor : Editor {
	public void OnEnable()
	{
		foreach (HingeJoint2DSettings hingeJoint2DSettings in targets)
		{
			if (hingeJoint2DSettings.attachedJoint == null)
			{
				DestroyImmediate(hingeJoint2DSettings);
			}
		}
	}

	public void OnDisable() {
		foreach (HingeJoint2DSettings hingeJoint2DSettings in targets) {
			if (hingeJoint2DSettings.attachedJoint == null) {
				DestroyImmediate(hingeJoint2DSettings);
			}
		}
	}
}