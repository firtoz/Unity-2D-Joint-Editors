using UnityEngine;

public class JointEditorHelpers {
	public static Vector2 Transform2DPoint(Transform transform, Vector2 point) {
		Vector2 scaledPoint = Vector2.Scale(point, transform.lossyScale);
		float angle = transform.rotation.eulerAngles.z;
		Vector2 rotatedScaledPoint = Quaternion.AngleAxis(angle, Vector3.forward) * scaledPoint;
		Vector2 translatedRotatedScaledPoint = (Vector2) transform.position + rotatedScaledPoint;
		return translatedRotatedScaledPoint;
	}

	public static Vector2 InverseTransform2DPoint(Transform transform, Vector2 translatedRotatedScaledPoint) {
		Vector2 rotatedScaledPoint = translatedRotatedScaledPoint - (Vector2) transform.position;
		float angle = transform.rotation.eulerAngles.z;
		Vector2 scaledPoint = Quaternion.AngleAxis(-angle, Vector3.forward) * rotatedScaledPoint;
		Vector2 point = Vector2.Scale(scaledPoint, new Vector2(1 / transform.lossyScale.x, 1 / transform.lossyScale.y));
		return point;
	}


	public static Vector2 GetMainAnchorPosition(HingeJoint2D joint2D) {
		return Transform2DPoint(joint2D.transform, joint2D.anchor);
	}

	public static Vector2 GetConnectedAnchorPosition(HingeJoint2D joint2D) {
		if (joint2D.connectedBody) {
			return Transform2DPoint(joint2D.connectedBody.transform, joint2D.connectedAnchor);
		}
		return joint2D.connectedAnchor;
	}

	public enum AnchorBias {
		Main,
		Connected,
		Either
	}

	public static Vector3 GetAnchorPosition(HingeJoint2D hingeJoint2D, AnchorBias bias = AnchorBias.Either) {
		switch (bias) {
			case AnchorBias.Connected:
				return GetConnectedAnchorPosition(hingeJoint2D);
			default:
				return GetMainAnchorPosition(hingeJoint2D);
		}
	}

	public static void SetAnchorPosition(HingeJoint2D hingeJoint2D, Vector3 position, AnchorBias bias) {
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
		hingeJoint2D.anchor = InverseTransform2DPoint(hingeJoint2D.transform, worldAnchor);
	}

	public static void SetWorldConnectedAnchorPosition(HingeJoint2D hingeJoint2D, Vector2 worldConnectedAnchor) {
		if (hingeJoint2D.connectedBody) {
			hingeJoint2D.connectedAnchor = InverseTransform2DPoint(hingeJoint2D.connectedBody.transform,
				worldConnectedAnchor);
		}
		else {
			hingeJoint2D.connectedAnchor = worldConnectedAnchor;
		}
	}

	public static float GetAngle(Vector2 vector) {
		return Mathf.Rad2Deg * Mathf.Atan2(vector.y, vector.x);
	}

	public static Quaternion Rotate2D(float angle) {
		return Quaternion.AngleAxis(angle, Vector3.forward);
	}

	public static Vector3 Rotated2DVector(float angle) {
		return Rotate2D(angle) * Vector3.right;
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

			angle = GetAngle(towardsTarget);
		}
		else
		{
			angle = targetRotation;
		}
		return angle;
	}
}