using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
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

    [SerializeField] private bool initialized;

    public Texture2D connectedAnchorTexture;
    public Texture2D mainAnchorTexture;
    public Texture2D lockedAnchorTexture;
    public Texture2D hotAnchorTexture;
    public Texture2D offsetTexture;
    public Texture2D lockButtonTexture;
    public Texture2D unlockButtonTexture;

    public float anchorScale = 0.5f;
    public float anchorDisplayScale = 1.75f;
    public float lockButtonScale = 0.5f;

    //hingejoint2d settings
    public float angleLimitRadius = 1.5f;
    public float angleHandleSize = 5.0f;
    public RingDisplayMode ringDisplayMode = RingDisplayMode.Hover;
    public Color mainDiscColor = Color.green;
    public Color connectedDiscColor = Color.green;
    public Color angleLimitColor = new Color(0, 255f / 255f, 23f / 255f);
    public Color angleAreaColor = Color.gray;

    //sliderjoint2d settings
    public Color minLimitColor = Color.magenta;
    public Color maxLimitColor = Color.cyan;

    public Color anchorHoverColor = new Color(1f, 1f, 0.5f, 0.125f);
    public Color anchorsToMainBodyColor = Color.red;
    public Color anchorsToConnectedBodyColor = Color.green;
    public Color incorrectLimitsColor = Color.red;
    public Color correctLimitsColor = Color.green;

    public Color hoverAngleColor = Color.yellow;
    public Color activeAngleColor = Color.green;
    public Color inactiveAngleColor = Color.white;
    
    public bool showAdvancedOptions = false;

    private static JointEditorSettings _editorSettings;
    private static bool _loading;

    const string Label = "jointeditorssettingspath";
    public const string ConnectedHingeTexturePath = "2d_joint_editor_hinge_connected.png";
    public const string OffsetTexturePath = "2djointeditor_anchor.png";
    public const string MainHingeTexturePath = "2d_joint_editor_hinge_main.png";
    public const string LockedHingeTexturePath = "2d_joint_editor_hinge_locked.png";
    public const string HotHingeTexturePath = "2d_joint_editor_hinge_hot.png";

    public const string LockButtonTexturePath = "2d_joint_editor_lock_button.png";
    public const string UnlockButtonTexturePath = "2d_joint_editor_unlock_button.png";

#if UNITY_EDITOR
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

                AssetUtils utils;

                string[] guids = AssetDatabase.FindAssets("l:" + Label);
                if (guids.Any()) {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    utils = new AssetUtils(path);
                    _editorSettings = utils.GetOrCreateAsset<JointEditorSettings>("settings.asset");
                    if (_editorSettings == null) {
                        Debug.Log("deleted!");
                    }
                }
                else {
                    string path =
                        EditorUtility.OpenFolderPanel("Please pick a path for the JointEditor2D settings to be stored.",
                            Application.dataPath, "");

                    if (path != null) {
                        if (Directory.Exists(path) && AssetUtils.IsAssetPath(path)) {
                            string assetPath = AssetUtils.GetRelativePath(path);

                            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                            if (asset != null) {
                                List<String> labels = new List<string>(AssetDatabase.GetLabels(asset)) {
                                    Label
                                };
                                AssetDatabase.SetLabels(asset, labels.ToArray());
                                AssetDatabase.SaveAssets();
                                utils = new AssetUtils(assetPath);
                                _editorSettings = utils.GetOrCreateAsset<JointEditorSettings>("settings.asset");
                                if (_editorSettings == null) {
                                    Debug.Log("deleted!");
                                }
                            }
                        }
                    }
                    else {
                        Debug.LogError("Why don't you want to save the settings? :(");
                    }
                }
                _loading = false;
            }

            return _editorSettings;
        }
    }

    public void OnEnable() {
        if (!initialized) {
            initialized = true;
            string[] guids = AssetDatabase.FindAssets("l:" + Label);
            if (guids.Any())
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);

                path = Path.GetFullPath(path + "/../Icons");

                string iconPath = AssetUtils.GetRelativePath(path);

                connectedAnchorTexture = LoadIcon(iconPath, ConnectedHingeTexturePath);
                offsetTexture = LoadIcon(iconPath, OffsetTexturePath);
                mainAnchorTexture = LoadIcon(iconPath, MainHingeTexturePath);
                lockedAnchorTexture = LoadIcon(iconPath, LockedHingeTexturePath);
                hotAnchorTexture = LoadIcon(iconPath, HotHingeTexturePath);

                lockButtonTexture = LoadIcon(iconPath, LockButtonTexturePath);
                unlockButtonTexture = LoadIcon(iconPath, UnlockButtonTexturePath);
            }

        }
    }

    private static Texture2D LoadIcon(params string[] path) {
        return Resources.LoadAssetAtPath<Texture2D>(AssetUtils.CreatePath(path));
    }
#endif

    public void Awake() {}

    public void Start() {}

    public void OnDisable() {}
}