using System;
using System.Linq;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (HingeJoint2DSettings))]
[CanEditMultipleObjects]
public class HingeJoint2DSettingsEditor : Editor {
	public void OnEnable()
	{
		EditorApplication.update += Update;
		foreach (HingeJoint2DSettings hingeJoint2DSettings in targets)
		{
			if (hingeJoint2DSettings.attachedJoint == null)
			{
				DestroyImmediate(hingeJoint2DSettings);
			}
		}
	}

	private void Update() {
		if (!EditorApplication.isPlayingOrWillChangePlaymode) {
			foreach (HingeJoint2DSettings hingeJoint2DSettings in targets) {
				HingeJoint2D hingeJoint2D = hingeJoint2DSettings.attachedJoint;
				if (hingeJoint2D == null) {
					return;
				}
				
				Vector2 mainCenter = JointEditorHelpers.GetAnchorPosition(hingeJoint2D, JointEditorHelpers.AnchorBias.Main);
				Vector2 mainPosition = JointEditorHelpers.GetTargetPosition(hingeJoint2D, JointEditorHelpers.AnchorBias.Main);

				hingeJoint2DSettings.mainAngle = JointEditorHelpers.AngleFromAnchor(mainCenter, mainPosition, JointEditorHelpers.GetTargetRotation(hingeJoint2D, JointEditorHelpers.AnchorBias.Main));

				if (hingeJoint2D.connectedBody) {
					Vector2 connectedCenter = JointEditorHelpers.GetAnchorPosition(hingeJoint2D, JointEditorHelpers.AnchorBias.Main);
					Vector2 connectedPosition = JointEditorHelpers.GetTargetPosition(hingeJoint2D, JointEditorHelpers.AnchorBias.Connected);

					hingeJoint2DSettings.connectedAngle = JointEditorHelpers.AngleFromAnchor(connectedCenter, connectedPosition, JointEditorHelpers.GetTargetRotation(hingeJoint2D, JointEditorHelpers.AnchorBias.Connected));
				}
			}
		}
	}



	public void OnDisable() {
		EditorApplication.update -= Update;
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

        return allSettings.FirstOrDefault(settings => settings.attachedJoint == hingeJoint2D);
    }

    public static HingeJoint2DSettings GetOrCreate(HingeJoint2D hingeJoint2D)
    {
        return Get(hingeJoint2D) ?? Create(hingeJoint2D);
    }
}