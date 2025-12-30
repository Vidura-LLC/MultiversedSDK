// File: Runtime/MultiversedSDK.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Multiversed.Core;
using Multiversed.Models;
using Multiversed.Wallet;
using Multiversed.Utils;

namespace Multiversed
{
    /// <summary>
    /// Main entry point for Multiversed SDK
    /// </summary>
    public class MultiversedSDK : MonoBehaviour
    {
        #region Singleton

        private static MultiversedSDK _instance;

        /// <summary>
        /// SDK singleton instance
        /// </summary>
        public static MultiversedSDK Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MultiversedSDK");
                    _instance = go.AddComponent<MultiversedSDK>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        #endregion

        #region Events

        public event Action OnInitialized;
        public event Action<string> OnError;
        public event Action<WalletSession> OnWalletConnected;
        public event Action OnWalletDisconnected;
        public event Action<string> OnTournamentRegistered;
        public event Action OnScoreSubmitted;

        #endregion

        #region Properties

        public bool IsInitialized
        {
            get
            {
                // SDK is only truly initialized if credentials are verified
                return _credentialsVerified && _authManager != null && _authManager.IsInitialized;
            }
        }

        public bool IsWalletConnected
        {
            get
            {
                if (_walletManager != null)
                {
                    return _walletManager.IsConnected;
                }
                return false;
            }
        }

        public string WalletAddress
        {
            get
            {
                if (_walletManager != null)
                {
                    return _walletManager.WalletAddress;
                }
                return null;
            }
        }

        public SDKConfig Config
        {
            get { return _config; }
        }

        #endregion

        #region Private Fields

        private SDKConfig _config;
        private AuthManager _authManager;
        private ApiClient _apiClient;
        private WalletManager _walletManager;
        private bool _credentialsVerified = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Handle analytics pause
            AnalyticsManager.Instance.OnApplicationPause(pauseStatus);
            
            if (!pauseStatus)
            {
                // App resumed - check for deep link after a short delay to ensure intent is ready
                StartCoroutine(CheckForDeepLinkDelayed());
            }
        }

        private void OnApplicationQuit()
        {
            // Flush analytics on quit
            AnalyticsManager.Instance.OnApplicationQuit();
        }

        private IEnumerator CheckForDeepLinkDelayed()
        {
            // Wait a bit for Android intent to be ready
            yield return new WaitForSeconds(0.3f);
            CheckForDeepLink();
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }
#endif

        #endregion

        #region Initialization

        public void Initialize(string gameId, string apiKey)
        {
            Initialize(gameId, apiKey, new SDKConfig());
        }

        public void Initialize(string gameId, string apiKey, SDKConfig config)
        {
            // If already fully initialized (credentials verified), warn and return
            if (IsInitialized)
            {
                SDKLogger.LogWarning("SDK already initialized");
                return;
            }

            // If partially initialized (auth manager exists but verification failed), clear it
            if (_authManager != null && !_credentialsVerified)
            {
                SDKLogger.Log("Previous initialization failed. Clearing state and reinitializing...");
                _authManager.Clear();
                _authManager = null;
                _apiClient = null;
            }

            if (config != null)
            {
                _config = config;
            }
            else
            {
                _config = new SDKConfig();
            }

            SDKLogger.EnableLogging = _config.EnableLogging;

            // Initialize analytics FIRST
            AnalyticsManager.Instance.Initialize(
                gameId,
                apiKey,
                _config.GetApiUrl(),
                this,  // MonoBehaviour for coroutines
                _config
            );

            // Reset verification flag
            _credentialsVerified = false;

            // Initialize auth manager
            _authManager = new AuthManager();
            _authManager.Initialize(gameId, apiKey);

            if (!_authManager.IsInitialized)
            {
                if (OnError != null)
                {
                    OnError("Failed to initialize authentication");
                }
                return;
            }

            // Initialize API client
            _apiClient = new ApiClient(_authManager, _config);

            // Initialize wallet manager
            _walletManager = new WalletManager(_config, gameId);

            // Initialize DeepLinkReceiver for automatic deep link handling
            InitializeDeepLinkReceiver();

            SDKLogger.Log("SDK initialized - Environment: " + _config.Environment);

            // Verify credentials
            StartCoroutine(VerifyCredentialsCoroutine());
        }

        /// <summary>
        /// Initialize DeepLinkReceiver for automatic deep link handling
        /// </summary>
        private void InitializeDeepLinkReceiver()
        {
            // Check if DeepLinkReceiver already exists
            var existingReceiver = FindFirstObjectByType<Utils.DeepLinkReceiver>();
            if (existingReceiver == null)
            {
                GameObject receiverObj = new GameObject("DeepLinkReceiver");
                receiverObj.AddComponent<Utils.DeepLinkReceiver>();
                SDKLogger.Log("DeepLinkReceiver initialized automatically");
            }
        }

        private IEnumerator VerifyCredentialsCoroutine()
        {
            yield return _apiClient.VerifyCredentials((success, message) =>
            {
                if (success)
                {
                    _credentialsVerified = true;
                    SDKLogger.Log("SDK credentials verified");
                    
                    // Track successful init
                    AnalyticsManager.Instance.Track("sdk_initialized");
                    
                    if (OnInitialized != null)
                    {
                        OnInitialized();
                    }
                }
                else
                {
                    // Clear auth state on verification failure to allow re-initialization
                    _credentialsVerified = false;
                    if (_authManager != null)
                    {
                        _authManager.Clear();
                    }
                    
                    // Track failed init
                    AnalyticsManager.Instance.TrackError("sdk_init_failed", message, "initialization_error");
                    
                    SDKLogger.LogError("Credential verification failed: " + message);
                    if (OnError != null)
                    {
                        OnError("Credential verification failed: " + message);
                    }
                }
            });
        }

        public void SetCustomApiUrl(string url)
        {
            if (_apiClient != null)
            {
                _apiClient.SetCustomBaseUrl(url);
            }
        }

        /// <summary>
        /// Reset SDK state to allow re-initialization
        /// </summary>
        public void Reset()
        {
            _credentialsVerified = false;
            if (_authManager != null)
            {
                _authManager.Clear();
                _authManager = null;
            }
            _apiClient = null;
            _walletManager = null;
            SDKLogger.Log("SDK state reset");
        }

        #endregion

        #region Wallet Methods

        public void ConnectWallet()
        {
            if (!IsInitialized)
            {
                if (OnError != null)
                {
                    OnError("SDK not initialized");
                }
                return;
            }

            _walletManager.Connect(
                onSuccess: (session) =>
                {
                    SDKLogger.Log("Wallet connected: " + session.GetShortAddress());
                    
                    // Update analytics with wallet address
                    AnalyticsManager.Instance.SetWalletAddress(session.WalletAddress);
                    
                    // Track wallet connected
                    AnalyticsManager.Instance.Track("wallet_connected");
                    
                    if (OnWalletConnected != null)
                    {
                        OnWalletConnected(session);
                    }
                },
                onError: (error) =>
                {
                    SDKLogger.LogError("Wallet connection failed: " + error);
                    
                    // Track wallet connection failure
                    AnalyticsManager.Instance.TrackError("wallet_connect_failed", error, "wallet_error");
                    
                    if (OnError != null)
                    {
                        OnError(error);
                    }
                }
            );
        }

        public void DisconnectWallet()
        {
            if (_walletManager != null)
            {
                _walletManager.Disconnect();
            }
            
            // Track disconnection
            AnalyticsManager.Instance.Track("wallet_disconnected");
            
            if (OnWalletDisconnected != null)
            {
                OnWalletDisconnected();
            }
        }

        public void HandleDeepLink(string url)
        {
            SDKLogger.Log("MultiversedSDK.HandleDeepLink called with: " + url);
            
            // Bring app to foreground on Android
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity != null)
                    {
                        // Get window and bring to front
                        using (AndroidJavaObject window = activity.Call<AndroidJavaObject>("getWindow"))
                        {
                            if (window != null)
                            {
                                window.Call("addFlags", 0x00000020); // FLAG_SHOW_WHEN_LOCKED
                                window.Call("addFlags", 0x00000008); // FLAG_DISMISS_KEYGUARD
                            }
                        }
                        // Request focus
                        activity.Call("onResume");
                    }
                }
            }
            catch (System.Exception e)
            {
                SDKLogger.LogWarning("Error bringing app to foreground: " + e.Message);
            }
            #endif

            if (_walletManager != null)
            {
                _walletManager.HandleDeepLink(url);
            }
            else
            {
                SDKLogger.LogWarning("WalletManager is null, cannot handle deep link");
            }
        }

        private void CheckForDeepLink()
        {
            SDKLogger.Log("[MultiversedSDK] CheckForDeepLink called");
            
            // Check Unity's absoluteURL first
            string deepLink = Application.absoluteURL;
            SDKLogger.Log("[MultiversedSDK] Application.absoluteURL: " + (deepLink ?? "null"));
            if (!string.IsNullOrEmpty(deepLink) && deepLink.Contains("multiversed-"))
            {
                SDKLogger.Log("[MultiversedSDK] Found deep link from Application.absoluteURL: " + deepLink);
                HandleDeepLink(deepLink);
                return;
            }

            // On Android, also check the intent data
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity != null)
                    {
                        using (AndroidJavaObject intent = activity.Call<AndroidJavaObject>("getIntent"))
                        {
                            if (intent != null)
                            {
                                string action = intent.Call<string>("getAction");
                                SDKLogger.Log("[MultiversedSDK] Intent action: " + (action ?? "null"));
                                
                                if (action == "android.intent.action.VIEW" || action == "android.intent.action.MAIN")
                                {
                                    using (AndroidJavaObject uri = intent.Call<AndroidJavaObject>("getData"))
                                    {
                                        if (uri != null)
                                        {
                                            string url = uri.Call<string>("toString");
                                            SDKLogger.Log("[MultiversedSDK] Intent data URI: " + url);
                                            if (!string.IsNullOrEmpty(url) && url.Contains("multiversed-"))
                                            {
                                                SDKLogger.Log("[MultiversedSDK] Found deep link from Android intent: " + url);
                                                HandleDeepLink(url);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            SDKLogger.Log("[MultiversedSDK] Intent data URI is null");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                SDKLogger.Log("[MultiversedSDK] Intent is null");
                            }
                        }
                    }
                    else
                    {
                        SDKLogger.Log("[MultiversedSDK] Activity is null");
                    }
                }
            }
            catch (System.Exception e)
            {
                SDKLogger.LogWarning("[MultiversedSDK] Error checking Android intent for deep link: " + e.Message);
            }
            #endif
            
            SDKLogger.Log("[MultiversedSDK] No deep link found in CheckForDeepLink");
        }

        #endregion

        #region Tournament Methods

        public void GetTournaments(Action<List<Tournament>> onSuccess, Action<string> onError)
        {
            GetTournaments(_config.DefaultTokenType, onSuccess, onError);
        }

        public void GetTournaments(TokenType tokenType, Action<List<Tournament>> onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                if (onError != null)
                {
                    onError("SDK not initialized");
                }
                return;
            }

            StartCoroutine(_apiClient.GetTournaments(tokenType, (tournaments, error) =>
            {
                if (tournaments != null)
                {
                    // Track tournaments fetched (lower priority)
                    AnalyticsManager.Instance.Track("tournaments_fetched", tokenType, new Dictionary<string, object>
                    {
                        { "count", tournaments.Length }
                    });
                    
                    if (onSuccess != null)
                    {
                        onSuccess(new List<Tournament>(tournaments));
                    }
                }
                else
                {
                    if (onError != null)
                    {
                        onError(error);
                    }
                    if (OnError != null)
                    {
                        OnError(error);
                    }
                }
            }));
        }

        public void GetTournament(string tournamentId, Action<Tournament> onSuccess, Action<string> onError)
        {
            GetTournament(tournamentId, _config.DefaultTokenType, onSuccess, onError);
        }

        public void GetTournament(string tournamentId, TokenType tokenType, Action<Tournament> onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                if (onError != null)
                {
                    onError("SDK not initialized");
                }
                return;
            }

            StartCoroutine(_apiClient.GetTournament(tournamentId, tokenType, (tournament, error) =>
            {
                if (tournament != null)
                {
                    if (onSuccess != null)
                    {
                        onSuccess(tournament);
                    }
                }
                else
                {
                    if (onError != null)
                    {
                        onError(error);
                    }
                    if (OnError != null)
                    {
                        OnError(error);
                    }
                }
            }));
        }

        public void RegisterForTournament(string tournamentId, Action<string> onSuccess, Action<string> onError)
        {
            RegisterForTournament(tournamentId, _config.DefaultTokenType, onSuccess, onError);
        }

        public void RegisterForTournament(string tournamentId, TokenType tokenType, Action<string> onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                if (onError != null)
                {
                    onError("SDK not initialized");
                }
                return;
            }

            if (!IsWalletConnected)
            {
                if (onError != null)
                {
                    onError("Wallet not connected");
                }
                return;
            }

            StartCoroutine(RegisterForTournamentCoroutine(tournamentId, tokenType, onSuccess, onError));
        }

        private IEnumerator RegisterForTournamentCoroutine(
            string tournamentId,
            TokenType tokenType,
            Action<string> onSuccess,
            Action<string> onError)
        {
            string unsignedTransaction = null;
            string prepareError = null;

            yield return _apiClient.PrepareRegistration(
                tournamentId,
                WalletAddress,
                tokenType,
                (tx, error) =>
                {
                    unsignedTransaction = tx;
                    prepareError = error;
                }
            );

            if (string.IsNullOrEmpty(unsignedTransaction))
            {
                string errorMsg = prepareError != null ? prepareError : "Failed to prepare registration";
                if (onError != null)
                {
                    onError(errorMsg);
                }
                yield break;
            }

            bool signCompleted = false;
            string signature = null;
            string signError = null;

            _walletManager.SignAndSendTransaction(
                unsignedTransaction,
                tournamentId,
                (sig) =>
                {
                    signature = sig;
                    signCompleted = true;
                },
                (error) =>
                {
                    signError = error;
                    signCompleted = true;
                }
            );

            yield return new WaitUntil(() => signCompleted);

            if (string.IsNullOrEmpty(signature))
            {
                string errorMsg = signError != null ? signError : "Transaction signing failed";
                if (onError != null)
                {
                    onError(errorMsg);
                }
                yield break;
            }

            yield return _apiClient.ConfirmRegistration(
                tournamentId,
                WalletAddress,
                signature,
                tokenType,
                (success, message) =>
                {
                    if (success)
                    {
                        SDKLogger.Log("Registered for tournament: " + tournamentId);
                        
                        // Track registration with token type
                        AnalyticsManager.Instance.Track("tournament_registered", tokenType, new Dictionary<string, object>
                        {
                            { "tournament_id", tournamentId }
                        });
                        
                        if (onSuccess != null)
                        {
                            onSuccess(signature);
                        }
                        if (OnTournamentRegistered != null)
                        {
                            OnTournamentRegistered(tournamentId);
                        }
                    }
                    else
                    {
                        // Track registration failure
                        AnalyticsManager.Instance.Track("registration_failed", tokenType, new Dictionary<string, object>
                        {
                            { "tournament_id", tournamentId },
                            { "error", message },
                            { "error_type", "registration_error" }
                        });
                        
                        if (onError != null)
                        {
                            onError(message);
                        }
                        if (OnError != null)
                        {
                            OnError(message);
                        }
                    }
                }
            );
        }

        public void GetLeaderboard(string tournamentId, Action<List<LeaderboardEntry>> onSuccess, Action<string> onError)
        {
            GetLeaderboard(tournamentId, _config.DefaultTokenType, onSuccess, onError);
        }

        public void GetLeaderboard(string tournamentId, TokenType tokenType, Action<List<LeaderboardEntry>> onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                if (onError != null)
                {
                    onError("SDK not initialized");
                }
                return;
            }

            StartCoroutine(_apiClient.GetLeaderboard(tournamentId, tokenType, (entries, error) =>
            {
                if (entries != null)
                {
                    // Track leaderboard fetched (lower priority)
                    AnalyticsManager.Instance.Track("leaderboard_fetched", tokenType, new Dictionary<string, object>
                    {
                        { "tournament_id", tournamentId },
                        { "count", entries.Length }
                    });
                    
                    if (onSuccess != null)
                    {
                        onSuccess(new List<LeaderboardEntry>(entries));
                    }
                }
                else
                {
                    if (onError != null)
                    {
                        onError(error);
                    }
                    if (OnError != null)
                    {
                        OnError(error);
                    }
                }
            }));
        }

        #endregion

        #region Score Methods

        public void SubmitScore(string tournamentId, int score, Action onSuccess, Action<string> onError)
        {
            SubmitScore(tournamentId, score, _config.DefaultTokenType, onSuccess, onError);
        }

        public void SubmitScore(string tournamentId, int score, TokenType tokenType, Action onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                if (onError != null)
                {
                    onError("SDK not initialized");
                }
                return;
            }

            if (!IsWalletConnected)
            {
                if (onError != null)
                {
                    onError("Wallet not connected");
                }
                return;
            }

            StartCoroutine(_apiClient.SubmitScore(
                tournamentId,
                WalletAddress,
                score,
                tokenType,
                (success, message) =>
                {
                    if (success)
                    {
                        SDKLogger.Log("Score submitted: " + score);
                        
                        // Track score submission
                        AnalyticsManager.Instance.Track("score_submitted", tokenType, new Dictionary<string, object>
                        {
                            { "tournament_id", tournamentId }
                        });
                        
                        if (onSuccess != null)
                        {
                            onSuccess();
                        }
                        if (OnScoreSubmitted != null)
                        {
                            OnScoreSubmitted();
                        }
                    }
                    else
                    {
                        // Track score submission failure
                        AnalyticsManager.Instance.Track("score_submit_failed", tokenType, new Dictionary<string, object>
                        {
                            { "tournament_id", tournamentId },
                            { "error", message },
                            { "error_type", "score_error" }
                        });
                        
                        if (onError != null)
                        {
                            onError(message);
                        }
                        if (OnError != null)
                        {
                            OnError(message);
                        }
                    }
                }
            ));
        }

        #endregion
    }
}