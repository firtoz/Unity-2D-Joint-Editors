using System;
using System.Linq;
using toxicFork.GUIHelpers;
using UnityEditor;
using UnityEngine;

public class SettingsHelper
{
    private static T Create<T>(Joint2D joint2D) where T : Joint2DSettingsBase
    {
        var settings = Undo.AddComponent<T>(joint2D.gameObject);
        settings.hideFlags = HideFlags.HideInInspector;

        EditorHelpers.RecordUndo(null, settings);
        settings.Setup(joint2D);
        EditorUtility.SetDirty(settings);
        return settings;
    }

    private static T Get<T>(Joint2D joint2D) where T : Joint2DSettingsBase
    {
        var allSettings = joint2D.GetComponents<T>();

        return allSettings.FirstOrDefault(settings => settings.attachedJoint == joint2D);
    }

    public static T GetOrCreate<T>(Joint2D joint2D) where T : Joint2DSettingsBase {
        //because ?? doesn't work with webplayer!?
        if (Get<T>(joint2D) != null) {
            return Get<T>(joint2D);
        } else {
            return Create<T>(joint2D);
        }
    }

    public static Joint2DSettingsBase GetOrCreate(Joint2D joint2D)
    {
        if (!joint2D) {
            throw new ArgumentException("The joint is null!?");
        }
        if (joint2D is HingeJoint2D)
        {
            return GetOrCreate<HingeJoint2DSettings>(joint2D);
        }
        if (joint2D is DistanceJoint2D)
        {
            return GetOrCreate<DistanceJoint2DSettings>(joint2D);
        }
        if (joint2D is SliderJoint2D)
        {
            return GetOrCreate<SliderJoint2DSettings>(joint2D);
        }
        if (joint2D is WheelJoint2D)
        {
            return GetOrCreate<WheelJoint2DSettings>(joint2D);
        }
        if (joint2D is SpringJoint2D)
        {
            return GetOrCreate<SpringJoint2DSettings>(joint2D);
        }
        throw new ArgumentException("There are no editors defined for the joint2D: " + joint2D.GetType());
    }
}