using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;

public class Joint2DEditorPostProcessor
{
    [PostProcessScene]
    public static void OnPostprocessScene()
    {
        if (BuildPipeline.isBuildingPlayer) {
            Joint2DSettingsBase[] editorSettings = Object.FindObjectsOfType<Joint2DSettingsBase>();
            foreach (Joint2DSettingsBase jointEditorSettings in editorSettings)
            {
                Object.DestroyImmediate(jointEditorSettings);
            }    
            Joint2DTarget[] joint2DTargets = Object.FindObjectsOfType<Joint2DTarget>();
            foreach (Joint2DTarget target in joint2DTargets)
            {
                Object.DestroyImmediate(target);
            }    
        }
    }
}
