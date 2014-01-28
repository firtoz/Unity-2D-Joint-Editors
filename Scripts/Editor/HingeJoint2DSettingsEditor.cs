using System;
using System.Linq;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof (HingeJoint2DSettings))]
[CanEditMultipleObjects]
public class HingeJoint2DSettingsEditor : Editor {
    private static T Create<T>(Joint2D joint2D) where T : Joint2DSettings {
        T settings = Undo.AddComponent<T>(joint2D.gameObject);

        EditorHelpers.RecordUndo(null, settings);
        settings.Setup(joint2D);
        return settings;
    }

    private static T Get<T>(Joint2D joint2D) where T : Joint2DSettings {
        T[] allSettings = joint2D.GetComponents<T>();

        return allSettings.FirstOrDefault(settings => settings.attachedJoint == joint2D);
    }

    public static T GetOrCreate<T>(Joint2D joint2D) where T : Joint2DSettings
    {
        return Get<T>(joint2D) ?? Create<T>(joint2D) as T;
    }
}