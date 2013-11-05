using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class JointEditorSettings : ScriptableObject {
    public const float AnchorEpsilon = 0.0001f;

    [SerializeField]
    private bool initialized;

    public const string ConnectedHingeTexturePath = "2d_joint_editor_hinge_connected";
    public Texture2D connectedHingeTexture;
    public const string MainHingeTexturePath = "2d_joint_editor_hinge_main";
    public Texture2D mainHingeTexture;
    public const string LockedHingeTexturePath = "2d_joint_editor_hinge_locked";
    public Texture2D lockedHingeTexture;
    public const string HotHingeTexturePath = "2d_joint_editor_hinge_hot";
    public Texture2D hotHingeTexture;

    public const string LockButtonTexturePath = "2d_joint_editor_lock_button";
    public Texture2D lockButtonTexture;
    public const string UnlockButtonTexturePath = "2d_joint_editor_unlock_button";
    public Texture2D unlockButtonTexture;

    public float anchorScale = 0.5f;
    public float anchorDisplayScale = 1.75f;
    public float orbitRangeScale = 1.5f;
    public float lockButtonScale = 0.5f;

    public Color previewRadiusColor = new Color(1f, 1f, 0.5f, 0.125f);
    public Color radiusColor = new Color(1f, 1f, 0f, 0.5f);
    public Color mainDiscColor = Color.red;
    public Color connectedDiscColor = Color.green;

    public void OnEnable() {
        if (!initialized) {
            initialized = true;
            connectedHingeTexture = Resources.Load<Texture2D>(ConnectedHingeTexturePath);
            mainHingeTexture = Resources.Load<Texture2D>(MainHingeTexturePath);
            lockedHingeTexture = Resources.Load<Texture2D>(LockedHingeTexturePath);
            hotHingeTexture = Resources.Load<Texture2D>(HotHingeTexturePath);

            lockButtonTexture = Resources.Load<Texture2D>(LockButtonTexturePath);
            unlockButtonTexture = Resources.Load<Texture2D>(UnlockButtonTexturePath);
            EditorUtility.SetDirty(this);
        }
    }

    public void Awake() {
    }

    public void Start() {
    }

    public void OnDisable() {
    }
}
