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

    //general textures
    public Texture2D connectedAnchorTexture;
    public Texture2D mainAnchorTexture;
    public Texture2D lockedAnchorTexture;
    public Texture2D hotAnchorTexture;
    public Texture2D offsetTexture;
    public Texture2D lockButtonTexture;
    public Texture2D unlockButtonTexture;

    //general scales
    public float anchorScale = 0.5f;
    public float anchorDisplayScale = 1.75f;
    public float lockButtonScale = 0.5f;

    //hingejoint2d settings
    public float angleLimitRadius = 1.5f;
    public float angleHandleSize = 5.0f;
    public RingDisplayMode ringDisplayMode = RingDisplayMode.Hover;
    public Color mainDiscColor = Color.green;
    public Color connectedDiscColor = Color.green;
    public Color angleAreaColor = Color.gray;

    //sliderjoint2d settings
    public Color minLimitColor = Color.magenta;
    public Color maxLimitColor = Color.cyan;

    //general colors
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

                JointEditorSettings[] allSettings = Resources.FindObjectsOfTypeAll<JointEditorSettings>();
                Debug.Log("herp derp allSettings "+ allSettings.Length);

                if (allSettings.Length > 0)
                {
                    _editorSettings = allSettings[0];
                    if (_editorSettings == null) {
                        Debug.Log("deleted!!?");
                    }
                } else {
                    string path =
                        EditorUtility.OpenFolderPanel("Please pick a path for the JointEditor2D settings to be stored.",
                            Application.dataPath, "");

                    if (path != null) {
                        if (Directory.Exists(path) && AssetUtils.IsAssetPath(path)) {
                            string assetPath = AssetUtils.GetRelativePath(path);

                            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                            if (asset != null) {
                                AssetDatabase.SaveAssets();
                                AssetUtils utils = new AssetUtils(assetPath);
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
            {
//                connectedAnchorTexture = LoadIcon(iconPath, CONNECTED_HINGE_TEXTURE_PATH);
//                offsetTexture = LoadIcon(iconPath, OFFSET_TEXTURE_PATH);
//                mainAnchorTexture = LoadIcon(iconPath, MAIN_HINGE_TEXTURE_PATH);
//                lockedAnchorTexture = LoadIcon(iconPath, LOCKED_HINGE_TEXTURE_PATH);
//                hotAnchorTexture = LoadIcon(iconPath, HOT_HINGE_TEXTURE_PATH);
//
//                lockButtonTexture = LoadIcon(iconPath, LOCK_BUTTON_TEXTURE_PATH);
//                unlockButtonTexture = LoadIcon(iconPath, UNLOCK_BUTTON_TEXTURE_PATH);
            }
            initialized = true;
        }
        else {
            _editorSettings = this;
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