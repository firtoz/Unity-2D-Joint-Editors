using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class JointEditor : Editor {
    protected static readonly AssetUtils Utils = new AssetUtils("2DJointEditors/Data");

    protected static void RecordUndo(String action, params Object[] objects)
    {
        Undo.RecordObjects(objects, action);
    }

    protected static Vector2 Transform2DPoint(Transform transform, Vector2 point)
    {
        Vector2 scaledPoint = Vector2.Scale(point, transform.lossyScale);
        float angle = transform.rotation.eulerAngles.z;
        Vector2 rotatedScaledPoint = Quaternion.AngleAxis(angle, Vector3.forward) * scaledPoint;
        Vector2 translatedRotatedScaledPoint = (Vector2)transform.position + rotatedScaledPoint;
        return translatedRotatedScaledPoint;
    }

    protected static Vector2 InverseTransform2DPoint(Transform transform, Vector2 translatedRotatedScaledPoint)
    {
        Vector2 rotatedScaledPoint = translatedRotatedScaledPoint - (Vector2)transform.position;
        float angle = transform.rotation.eulerAngles.z;
        Vector2 scaledPoint = Quaternion.AngleAxis(-angle, Vector3.forward) * rotatedScaledPoint;
        Vector2 point = Vector2.Scale(scaledPoint, new Vector2(1 / transform.lossyScale.x, 1 / transform.lossyScale.y));
        return point;
    }


    protected static Vector2 GetAnchorPosition(HingeJoint2D joint2D)
    {
        return Transform2DPoint(joint2D.transform, joint2D.anchor);
    }

    protected static Vector2 GetConnectedAnchorPosition(HingeJoint2D joint2D)
    {
        if (joint2D.connectedBody)
        {
            return Transform2DPoint(joint2D.connectedBody.transform, joint2D.connectedAnchor);
        }
        return joint2D.connectedAnchor;
    }

    private static JointEditorSettings _jointSettings;

    protected static JointEditorSettings jointSettings
    {
        get
        {
            return _jointSettings ?? (_jointSettings = Utils.GetOrCreateAsset<JointEditorSettings>("settings.asset"));
        }
    }
}