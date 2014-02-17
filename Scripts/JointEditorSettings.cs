using UnityEngine;

[ExecuteInEditMode]
public class JointEditorSettings : ScriptableObject {
    public enum RingDisplayMode
    {
        Always,
        Hover,
        Never
    }


    [SerializeField]
    private bool initialized;

    public const string ConnectedHingeTexturePath = "2d_joint_editor_hinge_connected";
    public Texture2D connectedAnchorTexture;
    public const string MainHingeTexturePath = "2d_joint_editor_hinge_main";
    public Texture2D mainAnchorTexture;
    public const string LockedHingeTexturePath = "2d_joint_editor_hinge_locked";
    public Texture2D lockedAnchorTexture;
    public const string HotHingeTexturePath = "2d_joint_editor_hinge_hot";
    public Texture2D hotAnchorTexture;

    public const string LockButtonTexturePath = "2d_joint_editor_lock_button";
    public Texture2D lockButtonTexture;
    public const string UnlockButtonTexturePath = "2d_joint_editor_unlock_button";
    public Texture2D unlockButtonTexture;

    public float anchorScale = 0.5f;
    public float anchorDisplayScale = 1.75f;
    public float orbitRangeScale = 1.5f;
    public float lockButtonScale = 0.5f;

    public float angleHandleSize = 5.0f;

    public Color previewRadiusColor = new Color(1f, 1f, 0.5f, 0.125f);
    public Color radiusColor = new Color(1f, 1f, 0f, 0.5f);
    public Color alternateRadiusColor = new Color(0f, 1f, 1f, 0.5f);
    public Color mainDiscColor = Color.red;
    public Color connectedDiscColor = Color.green;
    public Color angleLimitColor = Color.red;
    public Color angleAreaColor = Color.gray;
    public bool drawRadiusRings = true;

    public RingDisplayMode ringDisplayMode = RingDisplayMode.Hover;
    public bool foldout = false;

    public void OnEnable() {
        if (!initialized) {
            initialized = true;
            connectedAnchorTexture = Resources.Load<Texture2D>(ConnectedHingeTexturePath);
            mainAnchorTexture = Resources.Load<Texture2D>(MainHingeTexturePath);
            lockedAnchorTexture = Resources.Load<Texture2D>(LockedHingeTexturePath);
            hotAnchorTexture = Resources.Load<Texture2D>(HotHingeTexturePath);

            lockButtonTexture = Resources.Load<Texture2D>(LockButtonTexturePath);
            unlockButtonTexture = Resources.Load<Texture2D>(UnlockButtonTexturePath);
        }
    }

    public void Awake() {
    }

    public void Start() {
    }

    public void OnDisable() {
    }
}
