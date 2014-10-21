using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class JointEditorSettings : ScriptableObject {
    public enum RingDisplayMode {
        Always,
        Hover,
        Never
    }

    //general textures
    [Tooltip("The texture to display for the connected anchor widget")]
    public Texture2D connectedAnchorTexture;
    [Tooltip("The texture to display for the main anchor widget")]
    public Texture2D mainAnchorTexture;
    [Tooltip("The texture to display when the connected and main anchors are locked.")]
    public Texture2D lockedAnchorTexture;
    [Tooltip("The texture to display when an anchor is being hovered.")]
    public Texture2D hotAnchorTexture;
    [Tooltip("The texture for the offset gizmo. Is visible when the control key is held.")]
    public Texture2D offsetTexture;
    [Tooltip("The texture for the lock button. Is visible when the shift key is held.")]
    public Texture2D lockButtonTexture;
    [Tooltip("The texture for the unlock button. Is visible when the shift key is held.")]
    public Texture2D unlockButtonTexture;

    //general scales
    [Tooltip("The anchor textures are resized by this scaling factor.")]
    public float anchorScale = 0.5f;
    [Tooltip("This scale only affects the graphical part of the anchors, and does not affect hover or click area.")]
    public float anchorDisplayScale = 1.0f;
    [Tooltip("The lock button textures are resized by this scaling factor.")]
    public float lockButtonScale = 0.5f;

    //general colors
    [Tooltip("The highlight color to highlight an anchor when the mouse is over it.")]
    public Color anchorHoverColor = new Color(1f, 1f, 0.5f, 0.125f);
    [Tooltip("The color of the line between anchors and the main body.")]
    public Color anchorsToMainBodyColor = Color.red;
    [Tooltip("The color of the line between anchors and the connected body.")]
    public Color anchorsToConnectedBodyColor = Color.green;
    [Tooltip("The color displayed when limits are incorrect (used by slider and hinge joints).")]
    public Color incorrectLimitsColor = new Color(1, 0.5f, 0, 1);
    [Tooltip("The color displayed when limits are correct (used by slider and hinge joints).")]
    public Color correctLimitsColor = new Color(0.098f, 0.956f, 0, 0.737f);
    [Tooltip("The color used by angle widgets.")]
    public Color angleWidgetColor = Color.white;
    [Tooltip("The color displayed to highlight when the mouse is over angle widgets.")]
    public Color hoverAngleColor = Color.yellow;
    [Tooltip("The color displayed to highlight when angle widgets are active.")]
    public Color activeAngleColor = Color.green;

    //general toggles
    [Tooltip("Whether locked anchors should actively try to realign themselves.")]
    public bool automaticRealign = true;

    //hingejoint2d settings
    [Tooltip("The distance (in pixels) between the anchors and the angle limits widgets.")]
    public float angleLimitRadius = 30.0f;
    [Tooltip("The size (in pixels) of the angle limits widgets.")]
    public float angleHandleSize = 5.0f;
    [Tooltip("The fill color for the area between the angle limits widgets and the anchors.")]
    public Color limitsAreaColor = new Color(0.265f, 0.772f, 0.776f, 0.118f);
    [Tooltip("The fill color displayed when limits are incorrect.")]
    [Header("test")]
    public Color incorrectLimitsArea = new Color(1, 0.5f, 0, 0.118f);
    [Header("test")]
    [Tooltip("How should the rings be displayed?.")]
    public RingDisplayMode ringDisplayMode = RingDisplayMode.Hover;
    [Tooltip("The ring that highlights the path of the main body.")]
    public Color mainRingColor = Color.green;
    [Tooltip("The ring that highlights the path of the connected body.")]
    public Color connectedRingColor = new Color(0.568f, 0.514f, 1, 1);

    //sliderjoint2d settings
    [Tooltip("The color for the minimum distance limit.")]
    public Color minLimitColor = new Color(0, .470588237f, 1, 1);
    [Tooltip("The color for the maximum distance limit.")]
    public Color maxLimitColor = Color.cyan;
    
    public bool showAdvancedOptions = false;

    private static JointEditorSettings _editorSettings;
    private static bool _loading;

    public static JointEditorSettings Singleton {
        get {
            {
                if (_editorSettings != null) {
                    return _editorSettings;
                }
                if (_loading) {
                    return null;
                }

                _loading = true;

                JointEditorSettings[] allSettings = Resources.FindObjectsOfTypeAll<JointEditorSettings>();

                if (allSettings.Length > 0) {
                    _editorSettings = allSettings[0];
                    if (_editorSettings == null) {
                        Debug.Log("deleted!!?");
                    }
                }
                else {
                    AssetDatabase.Refresh();
                    string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
                    foreach (JointEditorSettings settings in 
                        allAssetPaths.Select(
                            assetPath => AssetDatabase.LoadAssetAtPath(assetPath, typeof (JointEditorSettings)))
                            .OfType<JointEditorSettings>()) {
                        Debug.LogWarning("Just Loaded JointEditorSettings!");
                        _editorSettings = settings;
                        break;
                    }

                    if (_editorSettings == null) {
                        Debug.LogWarning("Could not find JointEditorSettings!");
                    }
                }
                _loading = false;
            }

            return _editorSettings;
        }
    }

    public void OnEnable() {
        _editorSettings = this;
    }

    public void Awake() {}

    public void Start() {}

    public void OnDisable() {}
}