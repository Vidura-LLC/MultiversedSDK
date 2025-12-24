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
                if (_authManager != null)
                {
                    return _authManager.IsInitialized;
                }
                return false;
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
            if (!pauseStatus)
            {
                CheckForDeepLink();
            }
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
            if (IsInitialized)
            {
                SDKLogger.LogWarning("SDK already initialized");
                return;
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
                    SDKLogger.Log("SDK credentials verified");
                    if (OnInitialized != null)
                    {
                        OnInitialized();
                    }
                }
                else
                {
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
                    if (OnWalletConnected != null)
                    {
                        OnWalletConnected(session);
                    }
                },
                onError: (error) =>
                {
                    SDKLogger.LogError("Wallet connection failed: " + error);
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
            if (OnWalletDisconnected != null)
            {
                OnWalletDisconnected();
            }
        }

        public void HandleDeepLink(string url)
        {
            if (_walletManager != null)
            {
                _walletManager.HandleDeepLink(url);
            }
        }

        private void CheckForDeepLink()
        {
            string deepLink = Application.absoluteURL;
            if (!string.IsNullOrEmpty(deepLink))
            {
                HandleDeepLink(deepLink);
            }
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