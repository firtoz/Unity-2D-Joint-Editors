using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;

public class Joint2DEditorPostProcessor
{
    [PostProcessScene]
    public static void OnPostprocessScene()
    {
        if (BuildPipeline.isBuildingPlayer) {
            Joint2DSettings[] editorSettings = Object.FindObjectsOfType<Joint2DSettings>();
            foreach (Joint2DSettings jointEditorSettings in editorSettings)
            {
                Object.DestroyImmediate(jointEditorSettings);
            }    
        }
    }
}
