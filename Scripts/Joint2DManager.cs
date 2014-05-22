/*#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class Joint2DManager {
    private static readonly Dictionary<Joint2DSettings, Rigidbody2D> JointSettingsMapping =
        new Dictionary<Joint2DSettings, Rigidbody2D>();

    static Joint2DManager() {
        EditorApplication.update += Update;
        SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
//        Debug.Log(JointSettingsMapping.Count);
    }

    private static void OnSceneGUIDelegate(SceneView sceneview) {
        foreach (Editor editor in AnchorEditors.Values) {
            IJoint2DEditor joint2DEditor = editor as IJoint2DEditor;
            if (joint2DEditor != null) {
                joint2DEditor.OnSceneGUI();
            }
        }
    }

    public static void AddJointSettings(Joint2DSettings joint2DSettings) {
        JointSettingsMapping.Add(joint2DSettings,
            joint2DSettings.attachedJoint ? joint2DSettings.attachedJoint.connectedBody : null);
    }

    public static void RemoveJointSettings(Joint2DSettings joint2DSettings) {
        JointSettingsMapping.Remove(joint2DSettings);
    }

    private static int _selectionHash;

    private static readonly Dictionary<Joint2D, Editor> AnchorEditors = new Dictionary<Joint2D, Editor>();
    
    private static void Update() {
        int currentHash = Selection.objects.Aggregate(1, (current, obj) => current ^ obj.GetHashCode());

        if (_selectionHash == currentHash) {
            return;
        }
        Debug.Log("Selection changed!");

        for (int i=0;i< AnchorEditors.Keys.Count; i++) {
            Joint2D joint2D = AnchorEditors.Keys.ElementAt(i);
            if (joint2D.connectedBody==null || !Selection.Contains(joint2D.connectedBody.gameObject) || Selection.Contains(joint2D.gameObject)) {
                Debug.Log("Not relevant anymore: "+joint2D.name);
                Object.DestroyImmediate(AnchorEditors[joint2D]);
                AnchorEditors.Remove(joint2D);
                i--;
            }
        }
        foreach (GameObject gameObject in Selection.gameObjects.Where(o => o.rigidbody2D != null)) {
            GameObject o = gameObject;
            foreach (
                Joint2DSettings joint2DSettings in
                    JointSettingsMapping.Keys.Where(joint2DSettings => joint2DSettings.attachedJoint &&
                                                                        joint2DSettings.attachedJoint.connectedBody ==
                                                                        o.rigidbody2D)) {

                if (!Selection.Contains(joint2DSettings.gameObject) && !AnchorEditors.ContainsKey(joint2DSettings.attachedJoint))
                {
                    Debug.Log("found an attached object! " + gameObject.name + " from " + joint2DSettings.name);

                    Editor editor = Editor.CreateEditor(joint2DSettings.attachedJoint);
                    AnchorEditors[joint2DSettings.attachedJoint] = editor;
                }
            }
        }

//        UnityEditor.TransformInspector;

        
//        foreach(Editor editor in Editors.Except(createdEditors)) {
//            Object.DestroyImmediate(editor);
//        }

        _selectionHash = currentHash;
    }

    public static void UpdateJointSettings(Joint2DSettings joint2DSettings) {
        JointSettingsMapping[joint2DSettings] = joint2DSettings.attachedJoint
            ? joint2DSettings.attachedJoint.connectedBody
            : null;
    }
}
#endif*/