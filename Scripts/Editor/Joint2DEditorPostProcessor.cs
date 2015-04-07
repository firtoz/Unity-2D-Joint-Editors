using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class Joint2DEditorPostProcessor
{
    [PostProcessScene]
    public static void OnPostprocessScene()
    {
        if (!BuildPipeline.isBuildingPlayer) {
            return;
        }

        var editorSettings = Object.FindObjectsOfType<Joint2DSettingsBase>();
        foreach (var jointEditorSettings in editorSettings)
        {
            Object.DestroyImmediate(jointEditorSettings);
        }    
        var joint2DTargets = Object.FindObjectsOfType<Joint2DTarget>();
        foreach (var target in joint2DTargets)
        {
            Object.DestroyImmediate(target);
        }
    }
}
