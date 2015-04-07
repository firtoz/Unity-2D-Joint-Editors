using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class JointEditorSettings : ScriptableObject {
    public enum RingDisplayMode {
        Always,
        Hover,
        Never
    }

    public bool disableEverything = false;

    //general textures
    [Tooltip("The texture to display for the connected anchor widget")]
    public Texture2D connectedAnchorTexture;

    [Tooltip("The texture to display for the main anchor widget")]
    public Texture2D mainAnchorTexture;

    [Tooltip("The texture to display when the connected and main anchors are locked.")]
    public Texture2D lockedAnchorTexture;

    [Tooltip("The texture to display when an anchor is being hovered.")]
    public Texture2D hotAnchorTexture;

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

    [Tooltip("The color displayed to highlight anchor snapping positions.")]
    public Color snapHighlightColor = Color.cyan;

    //hingejoint2d settings
    [Tooltip("The distance (in pixels) between the anchors and the angle limits widgets.")]
    public float angleLimitRadius = 30.0f;

    [Tooltip("The size (in pixels) of the angle limits widgets.")]
    public float angleHandleSize = 5.0f;

    [Tooltip("The snapping angle that is used when you hold the control key.")]
    public float snapAngle = 45.0f;

    [Tooltip("The fill color for the area between the angle limits widgets and the anchors.")]
    public Color limitsAreaColor = new Color(0.265f, 0.772f, 0.776f, 0.118f);

    [Tooltip("The fill color displayed when limits are incorrect.")]
    public Color incorrectLimitsArea = new Color(1, 0.5f, 0, 0.118f);

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

    [Tooltip("Whether to show joint gizmos on the targets as well as the owners.")]
    public bool showConnectedJoints = false;

    [Tooltip("Toggle the display of lines from anchors to bodies.")]
    public bool drawLinesToBodies = true;

    [Tooltip("The transparency of the connected joint widgets.")]
    [Range(0f, 1f)]
    public float connectedJointTransparency = 0.25f;

    [Tooltip("Whether or not to highlight snap positions while holding the control key.")]
    public bool highlightSnapPositions = true;

    [Tooltip("The snap distance in pixels.")]
    public float snapDistance = 10.0f;

#if UNITY_EDITOR
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

                var settingsGUIDs = AssetDatabase.FindAssets("t:JointEditorSettings");

                foreach (var guid in settingsGUIDs) {
                    var settingsPath = AssetDatabase.GUIDToAssetPath(guid);
//                    Debug.Log("settings path: "+ settingsPath);
                    var loadedAsset = AssetDatabase.LoadAssetAtPath(settingsPath, typeof (JointEditorSettings));
                    if (loadedAsset is JointEditorSettings) {
                        _editorSettings = (JointEditorSettings) loadedAsset;
                    }
                }

                if (_editorSettings == null) {
                    Debug.LogError("Could not find JointEditorSettings!");
                }

                _loading = false;
            }

            return _editorSettings;
        }
    }

    public void OnEnable() {
        _editorSettings = this;
    }
#endif
}