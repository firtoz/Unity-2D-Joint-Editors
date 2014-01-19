using toxicFork.GUIHelpers;
using UnityEngine;

public class JointEditorHelpers {


	public static Vector2 GetMainAnchorPosition(HingeJoint2D joint2D) {
		return GUIHelpers.Transform2DPoint(joint2D.transform, joint2D.anchor);
	}

	public static Vector2 GetConnectedAnchorPosition(HingeJoint2D joint2D) {
		if (joint2D.connectedBody) {
			return GUIHelpers.Transform2DPoint(joint2D.connectedBody.transform, joint2D.connectedAnchor);
		}
		return joint2D.connectedAnchor;
	}

	public enum AnchorBias {
		Main,
		Connected,
		Either
	}

	public static Vector2 GetAnchorPosition(HingeJoint2D hingeJoint2D, AnchorBias bias = AnchorBias.Either) {
		switch (bias) {
			case AnchorBias.Connected:
				return GetConnectedAnchorPosition(hingeJoint2D);
			default:
				return GetMainAnchorPosition(hingeJoint2D);
		}
	}

	public static void SetAnchorPosition(HingeJoint2D hingeJoint2D, Vector2 position, AnchorBias bias) {
		switch (bias) {
			case AnchorBias.Connected:
				SetWorldConnectedAnchorPosition(hingeJoint2D, position);
				break;
			case AnchorBias.Main:
				SetWorldAnchorPosition(hingeJoint2D, position);
				break;
			case AnchorBias.Either:
				SetWorldAnchorPosition(hingeJoint2D, position);
				SetWorldConnectedAnchorPosition(hingeJoint2D, position);
				break;
		}
	}

	public static void SetWorldAnchorPosition(HingeJoint2D hingeJoint2D, Vector2 worldAnchor) {
		hingeJoint2D.anchor = GUIHelpers.InverseTransform2DPoint(hingeJoint2D.transform, worldAnchor);
	}

	public static void SetWorldConnectedAnchorPosition(HingeJoint2D hingeJoint2D, Vector2 worldConnectedAnchor) {
		if (hingeJoint2D.connectedBody) {
			hingeJoint2D.connectedAnchor = GUIHelpers.InverseTransform2DPoint(hingeJoint2D.connectedBody.transform,
				worldConnectedAnchor);
		}
		else {
			hingeJoint2D.connectedAnchor = worldConnectedAnchor;
		}
	}


	public static Vector2 GetTargetPosition(HingeJoint2D hingeJoint2D, AnchorBias bias = AnchorBias.Either) {
		Transform transform = GetTargetTransform(hingeJoint2D, bias);

		return transform.position;
	}


	public static float GetTargetRotation(HingeJoint2D hingeJoint2D, AnchorBias bias = AnchorBias.Either)
	{
		Transform transform = GetTargetTransform(hingeJoint2D, bias);

		return transform.rotation.eulerAngles.z;
	}

	private static Transform GetTargetTransform(HingeJoint2D hingeJoint2D, AnchorBias bias = AnchorBias.Either) {
		Transform transform;
		if (bias == AnchorBias.Connected) {
			transform = hingeJoint2D.connectedBody ? hingeJoint2D.connectedBody.transform : hingeJoint2D.transform;
		}
		else {
			transform = hingeJoint2D.transform;
		}
		return transform;
	}

	public static float AngleFromAnchor(Vector2 anchorPosition, Vector2 targetPosition, float targetRotation)
	{
		float angle;
		if (Vector3.Distance(targetPosition, anchorPosition) > JointEditorSettings.AnchorEpsilon)
		{
			Vector3 towardsTarget = (targetPosition - anchorPosition).normalized;

			angle = GUIHelpers.GetAngle(towardsTarget);
		}
		else
		{
			angle = targetRotation;
		}
		return angle;
	}
}