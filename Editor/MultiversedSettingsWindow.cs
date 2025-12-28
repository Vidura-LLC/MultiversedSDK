// File: Editor/MultiversedSettingsWindow.cs
using UnityEngine;
using UnityEditor;
using Multiversed.Core;

namespace Multiversed.Editor
{
    /// <summary>
    /// Editor window for Multiversed SDK setup and configuration
    /// </summary>
    public class MultiversedSettingsWindow : EditorWindow
    {
        private MultiversedSettings _settingsAsset;
        private bool _useScriptableObject = false;
        
        private string _gameId = "";
        private string _apiKey = "";
        private int _environmentIndex = 0;
        private int _tokenTypeIndex = 0;
        private string _customUrlScheme = "";
        private string _customAppUrl = "";
        private string _customApiUrl = "";
        private bool _enableLogging = true;
        private int _requestTimeoutSeconds = 30;

        private readonly string[] _environmentOptions = { "Devnet", "Mainnet" };
        private readonly string[] _tokenTypeOptions = { "SPL (YIP)", "SOL" };

        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private bool _stylesInitialized;

        private const string PREFS_GAME_ID = "Multiversed_GameId";
        private const string PREFS_API_KEY = "Multiversed_ApiKey";
        private const string PREFS_ENVIRONMENT = "Multiversed_Environment";
        private const string PREFS_TOKEN_TYPE = "Multiversed_TokenType";
        private const string PREFS_URL_SCHEME = "Multiversed_UrlScheme";
        private const string PREFS_LOGGING = "Multiversed_Logging";

        [MenuItem("Window/Multiversed/SDK Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<MultiversedSettingsWindow>("Multiversed SDK");
            window.minSize = new Vector2(400, 500);
        }

        [MenuItem("Window/Multiversed/Documentation")]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://docs.multiversed.io/unity-sdk");
        }

        [MenuItem("Window/Multiversed/Dashboard")]
        public static void OpenDashboard()
        {
            Application.OpenURL("https://dashboard.multiversed.io");
        }

        private void OnEnable()
        {
            // Try to find existing settings asset
            string[] guids = AssetDatabase.FindAssets("t:MultiversedSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _settingsAsset = AssetDatabase.LoadAssetAtPath<MultiversedSettings>(path);
                if (_settingsAsset != null)
                {
                    _useScriptableObject = true;
                    LoadFromScriptableObject();
                    return;
                }
            }
            
            LoadSettings();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                margin = new RectOffset(0, 0, 10, 10)
            };

            _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 15, 5)
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Header
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Multiversed SDK Settings", _headerStyle);
            EditorGUILayout.Space(5);

            // Configuration Mode Selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Configuration Mode:", GUILayout.Width(150));
            _useScriptableObject = EditorGUILayout.Toggle("Use ScriptableObject", _useScriptableObject);
            EditorGUILayout.EndHorizontal();

            if (_useScriptableObject)
            {
                EditorGUILayout.BeginHorizontal();
                _settingsAsset = (MultiversedSettings)EditorGUILayout.ObjectField(
                    "Settings Asset", _settingsAsset, typeof(MultiversedSettings), false);
                
                if (GUILayout.Button("Create New", GUILayout.Width(100)))
                {
                    CreateNewSettingsAsset();
                }
                EditorGUILayout.EndHorizontal();

                if (_settingsAsset == null)
                {
                    EditorGUILayout.HelpBox(
                        "No Settings Asset assigned. Create a new one or assign an existing asset.",
                        MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Using EditorPrefs (project-independent settings). Switch to ScriptableObject for project-level configuration.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(5);
            DrawHorizontalLine();

            // Credentials Section
            EditorGUILayout.LabelField("Credentials", _sectionStyle);
            EditorGUILayout.HelpBox(
                "Enter your Game ID and API Key from the Multiversed Developer Dashboard.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Game ID");
            _gameId = EditorGUILayout.TextField(_gameId);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("API Key");
            _apiKey = EditorGUILayout.PasswordField(_apiKey);

            EditorGUILayout.Space(10);

            DrawHorizontalLine();

            // Environment Section
            EditorGUILayout.LabelField("Environment", _sectionStyle);

            _environmentIndex = EditorGUILayout.Popup("Network", _environmentIndex, _environmentOptions);

            if (_environmentIndex == 1) // Mainnet
            {
                EditorGUILayout.HelpBox(
                    "Warning: Mainnet uses real SOL/tokens. Make sure your game is fully tested on Devnet first.",
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space(10);

            DrawHorizontalLine();

            // Token Type Section
            EditorGUILayout.LabelField("Default Token Type", _sectionStyle);

            _tokenTypeIndex = EditorGUILayout.Popup("Token", _tokenTypeIndex, _tokenTypeOptions);

            EditorGUILayout.Space(10);

            DrawHorizontalLine();

            // URL Configuration Section
            EditorGUILayout.LabelField("URL Configuration", _sectionStyle);
            EditorGUILayout.HelpBox(
                "Configure custom URLs for API and Phantom wallet integration. Leave empty to use defaults.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Custom API URL (optional)");
            _customApiUrl = EditorGUILayout.TextField(_customApiUrl);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Custom App URL (optional)");
            EditorGUILayout.HelpBox(
                "App URL for Phantom wallet deep links. Default: https://multiversed.io",
                MessageType.None
            );
            _customAppUrl = EditorGUILayout.TextField(_customAppUrl);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Custom URL Scheme (optional)");
            EditorGUILayout.HelpBox(
                "URL scheme for Phantom wallet callbacks. Leave empty to use auto-generated.",
                MessageType.None
            );
            _customUrlScheme = EditorGUILayout.TextField(_customUrlScheme);

            string effectiveScheme = string.IsNullOrEmpty(_customUrlScheme)
                ? $"multiversed-{(_gameId.Length >= 8 ? _gameId.Substring(0, 8) : _gameId)}"
                : _customUrlScheme;

            EditorGUILayout.LabelField($"Effective Scheme: {effectiveScheme}://", EditorStyles.miniLabel);

            EditorGUILayout.Space(10);

            DrawHorizontalLine();

            // Debug Section
            EditorGUILayout.LabelField("Debug", _sectionStyle);

            _enableLogging = EditorGUILayout.Toggle("Enable Logging", _enableLogging);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Request Timeout (seconds)");
            _requestTimeoutSeconds = EditorGUILayout.IntSlider(_requestTimeoutSeconds, 5, 120);

            EditorGUILayout.Space(20);

            DrawHorizontalLine();

            // Action Buttons
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Save Settings", GUILayout.Height(30)))
            {
                if (_useScriptableObject && _settingsAsset != null)
                {
                    SaveToScriptableObject();
                    EditorUtility.SetDirty(_settingsAsset);
                    AssetDatabase.SaveAssets();
                    EditorUtility.DisplayDialog("Multiversed SDK", "Settings saved to ScriptableObject asset!", "OK");
                }
                else
                {
                    SaveSettings();
                    EditorUtility.DisplayDialog("Multiversed SDK", "Settings saved to EditorPrefs!", "OK");
                }
            }

            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Reset Settings",
                    "Are you sure you want to reset all settings to defaults?", "Yes", "Cancel"))
                {
                    ResetToDefaults();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Generate Initialization Code", GUILayout.Height(25)))
            {
                GenerateInitCode();
            }

            EditorGUILayout.Space(10);

            // Quick Links
            EditorGUILayout.LabelField("Quick Links", _sectionStyle);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Open Dashboard"))
            {
                Application.OpenURL("https://dashboard.multiversed.io");
            }

            if (GUILayout.Button("Documentation"))
            {
                Application.OpenURL("https://docs.multiversed.io/unity-sdk");
            }

            if (GUILayout.Button("Support"))
            {
                Application.OpenURL("https://discord.gg/multiversed");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            EditorGUILayout.EndScrollView();
        }

        private void DrawHorizontalLine()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(5);
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString(PREFS_GAME_ID, _gameId);
            EditorPrefs.SetString(PREFS_API_KEY, _apiKey);
            EditorPrefs.SetInt(PREFS_ENVIRONMENT, _environmentIndex);
            EditorPrefs.SetInt(PREFS_TOKEN_TYPE, _tokenTypeIndex);
            EditorPrefs.SetString(PREFS_URL_SCHEME, _customUrlScheme);
            EditorPrefs.SetString("Multiversed_AppUrl", _customAppUrl);
            EditorPrefs.SetString("Multiversed_ApiUrl", _customApiUrl);
            EditorPrefs.SetBool(PREFS_LOGGING, _enableLogging);
            EditorPrefs.SetInt("Multiversed_RequestTimeout", _requestTimeoutSeconds);
        }

        private void LoadSettings()
        {
            _gameId = EditorPrefs.GetString(PREFS_GAME_ID, "");
            _apiKey = EditorPrefs.GetString(PREFS_API_KEY, "");
            _environmentIndex = EditorPrefs.GetInt(PREFS_ENVIRONMENT, 0);
            _tokenTypeIndex = EditorPrefs.GetInt(PREFS_TOKEN_TYPE, 0);
            _customUrlScheme = EditorPrefs.GetString(PREFS_URL_SCHEME, "");
            _customAppUrl = EditorPrefs.GetString("Multiversed_AppUrl", "");
            _customApiUrl = EditorPrefs.GetString("Multiversed_ApiUrl", "");
            _enableLogging = EditorPrefs.GetBool(PREFS_LOGGING, true);
            _requestTimeoutSeconds = EditorPrefs.GetInt("Multiversed_RequestTimeout", 30);
        }

        private void SaveToScriptableObject()
        {
            if (_settingsAsset == null) return;

            _settingsAsset.gameId = _gameId;
            _settingsAsset.apiKey = _apiKey;
            _settingsAsset.environment = _environmentIndex == 0 ? SDKEnvironment.Devnet : SDKEnvironment.Mainnet;
            _settingsAsset.defaultTokenType = _tokenTypeIndex == 0 ? TokenType.SPL : TokenType.SOL;
            _settingsAsset.customUrlScheme = _customUrlScheme;
            _settingsAsset.customAppUrl = _customAppUrl;
            _settingsAsset.customApiUrl = _customApiUrl;
            _settingsAsset.enableLogging = _enableLogging;
            _settingsAsset.requestTimeoutSeconds = _requestTimeoutSeconds;
        }

        private void LoadFromScriptableObject()
        {
            if (_settingsAsset == null) return;

            _gameId = _settingsAsset.gameId;
            _apiKey = _settingsAsset.apiKey;
            _environmentIndex = _settingsAsset.environment == SDKEnvironment.Devnet ? 0 : 1;
            _tokenTypeIndex = _settingsAsset.defaultTokenType == TokenType.SPL ? 0 : 1;
            _customUrlScheme = _settingsAsset.customUrlScheme;
            _customAppUrl = _settingsAsset.customAppUrl;
            _customApiUrl = _settingsAsset.customApiUrl;
            _enableLogging = _settingsAsset.enableLogging;
            _requestTimeoutSeconds = _settingsAsset.requestTimeoutSeconds;
        }

        private void CreateNewSettingsAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Multiversed Settings",
                "MultiversedSettings",
                "asset",
                "Choose where to save the settings asset");

            if (string.IsNullOrEmpty(path)) return;

            MultiversedSettings newAsset = ScriptableObject.CreateInstance<MultiversedSettings>();
            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();

            _settingsAsset = newAsset;
            Selection.activeObject = newAsset;
            EditorGUIUtility.PingObject(newAsset);

            LoadFromScriptableObject();
        }

        private void ResetToDefaults()
        {
            _gameId = "";
            _apiKey = "";
            _environmentIndex = 0;
            _tokenTypeIndex = 0;
            _customUrlScheme = "";
            _customAppUrl = "";
            _customApiUrl = "";
            _enableLogging = true;
            _requestTimeoutSeconds = 30;

            if (_useScriptableObject && _settingsAsset != null)
            {
                SaveToScriptableObject();
                EditorUtility.SetDirty(_settingsAsset);
                AssetDatabase.SaveAssets();
            }
            else
            {
                SaveSettings();
            }
        }

        private void GenerateInitCode()
        {
            string environment = _environmentIndex == 0 ? "SDKEnvironment.Devnet" : "SDKEnvironment.Mainnet";
            string tokenType = _tokenTypeIndex == 0 ? "TokenType.SPL" : "TokenType.SOL";

            string customApiUrlCode = string.IsNullOrEmpty(_customApiUrl) ? "null" : $"\"{_customApiUrl}\"";
            string customAppUrlCode = string.IsNullOrEmpty(_customAppUrl) ? "null" : $"\"{_customAppUrl}\"";
            string customUrlSchemeCode = string.IsNullOrEmpty(_customUrlScheme) ? "null" : $"\"{_customUrlScheme}\"";

            string code = $@"using Multiversed;
using Multiversed.Core;

public class GameInitializer : MonoBehaviour
{{
    void Start()
    {{
        var config = new SDKConfig
        {{
            Environment = {environment},
            DefaultTokenType = {tokenType},
            EnableLogging = {_enableLogging.ToString().ToLower()},
            CustomApiUrl = {customApiUrlCode},
            CustomAppUrl = {customAppUrlCode},
            CustomUrlScheme = {customUrlSchemeCode},
            RequestTimeoutSeconds = {_requestTimeoutSeconds}
        }};

        MultiversedSDK.Instance.Initialize(
            ""{_gameId}"",
            ""{MaskApiKey(_apiKey)}"",
            config
        );

        // Subscribe to events
        MultiversedSDK.Instance.OnInitialized += OnSDKInitialized;
        MultiversedSDK.Instance.OnError += OnSDKError;
        MultiversedSDK.Instance.OnWalletConnected += OnWalletConnected;
    }}

    void OnSDKInitialized()
    {{
        Debug.Log(""Multiversed SDK initialized!"");
    }}

    void OnSDKError(string error)
    {{
        Debug.LogError($""SDK Error: {{error}}"");
    }}

    void OnWalletConnected(WalletSession session)
    {{
        Debug.Log($""Wallet connected: {{session.GetShortAddress()}}"");
    }}
}}";

            EditorGUIUtility.systemCopyBuffer = code;
            EditorUtility.DisplayDialog("Code Generated",
                "Initialization code has been copied to your clipboard!\n\nPaste it into a new script to get started.",
                "OK");
        }

        private string MaskApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey.Length <= 16)
                return "YOUR_API_KEY";

            return apiKey.Substring(0, 16) + "...";
        }
    }
}