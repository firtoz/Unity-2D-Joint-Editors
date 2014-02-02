﻿using toxicFork.GUIHelpers;
using UnityEngine;

public class JointHelpers {
    public const float AnchorEpsilon = 0.001f;

    public static Vector2 GetMainAnchorPosition(AnchoredJoint2D joint2D) {
		return Helpers.Transform2DPoint(joint2D.transform, joint2D.anchor);
	}

    public static Vector2 GetConnectedAnchorPosition(AnchoredJoint2D joint2D)
    {
		if (joint2D.connectedBody) {
			return Helpers.Transform2DPoint(joint2D.connectedBody.transform, joint2D.connectedAnchor);
		}
		return joint2D.connectedAnchor;
	}

	public enum AnchorBias {
		Main,
		Connected,
		Either
	}

    public static AnchorBias GetBias(PositionInfo.Change change)
    {
        switch (change)
        {
            case PositionInfo.Change.MainChanged:
                return AnchorBias.Main;
            case PositionInfo.Change.ConnectedChanged:
                return AnchorBias.Connected;
            default:
                return AnchorBias.Either;
        }
    }

    public static Vector2 GetAnchorPosition(AnchoredJoint2D joint2D, AnchorBias bias = AnchorBias.Either)
    {
		switch (bias) {
			case AnchorBias.Connected:
				return GetConnectedAnchorPosition(joint2D);
			default:
				return GetMainAnchorPosition(joint2D);
		}
	}

    public static void SetAnchorPosition(AnchoredJoint2D joint2D, Vector2 position, AnchorBias bias)
    {
		switch (bias) {
			case AnchorBias.Connected:
				SetWorldConnectedAnchorPosition(joint2D, position);
				break;
			case AnchorBias.Main:
				SetWorldAnchorPosition(joint2D, position);
				break;
			case AnchorBias.Either:
				SetWorldAnchorPosition(joint2D, position);
				SetWorldConnectedAnchorPosition(joint2D, position);
				break;
		}
	}

    public static void SetWorldAnchorPosition(AnchoredJoint2D joint2D, Vector2 worldAnchor)
    {
		joint2D.anchor = Helpers.InverseTransform2DPoint(joint2D.transform, worldAnchor);
	}

    public static void SetWorldConnectedAnchorPosition(AnchoredJoint2D joint2D, Vector2 worldConnectedAnchor)
    {
		if (joint2D.connectedBody) {
			joint2D.connectedAnchor = Helpers.InverseTransform2DPoint(joint2D.connectedBody.transform,
				worldConnectedAnchor);
		}
		else {
			joint2D.connectedAnchor = worldConnectedAnchor;
		}
	}


	public static Vector2 GetTargetPosition(AnchoredJoint2D joint2D, AnchorBias bias = AnchorBias.Either) {
		Transform transform = GetTargetTransform(joint2D, bias);

	    if (transform == null) {
	        return Vector2.zero;
	    }
		return transform.position;
	}


    public static float GetTargetRotation(AnchoredJoint2D joint2D, AnchorBias bias = AnchorBias.Either)
	{
		Transform transform = GetTargetTransform(joint2D, bias);

        if (transform == null) {
            return 0;
        }

		return transform.rotation.eulerAngles.z;
	}

    public static Transform GetTargetTransform(AnchoredJoint2D joint2D, AnchorBias bias = AnchorBias.Either)
    {
		Transform transform;
		if (bias == AnchorBias.Connected) {
			transform = joint2D.connectedBody ? joint2D.connectedBody.transform : null;
		}
		else {
			transform = joint2D.transform;
		}
		return transform;
	}

	public static float AngleFromAnchor(Vector2 anchorPosition, Vector2 targetPosition, float targetRotation)
	{
		float angle;
		if (Vector3.Distance(targetPosition, anchorPosition) > AnchorEpsilon)
		{
			Vector3 towardsTarget = (targetPosition - anchorPosition).normalized;

			angle = Helpers.GetAngle(towardsTarget);
		}
		else
		{
			angle = targetRotation;
		}
		return angle;
	}
}