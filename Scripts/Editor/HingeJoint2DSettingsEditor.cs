using System.Linq;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;

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


    private static HingeJoint2DSettings Create(HingeJoint2D hingeJoint2D)
    {
        HingeJoint2DSettings settings = Undo.AddComponent<HingeJoint2DSettings>(hingeJoint2D.gameObject);

        GUIHelpers.RecordUndo(null, settings);
        settings.Setup(hingeJoint2D);
        //        worldAnchor = 
        //		settings.hideFlags = HideFlags.HideInInspector;
        return settings;
    }

    public static HingeJoint2DSettings Get(HingeJoint2D hingeJoint2D)
    {
        HingeJoint2DSettings[] allSettings = hingeJoint2D.GetComponents<HingeJoint2DSettings>();

        foreach (HingeJoint2DSettings settings in allSettings.Where(settings => settings.attachedJoint == hingeJoint2D))
        {
            return settings;
        }
        return null;
    }

    public static HingeJoint2DSettings GetOrCreate(HingeJoint2D hingeJoint2D)
    {
        return Get(hingeJoint2D) ?? Create(hingeJoint2D);
    }
}