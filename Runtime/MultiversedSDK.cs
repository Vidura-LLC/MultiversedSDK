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

        /// <summary>
        /// Fired when SDK is initialized successfully
        /// </summary>
        public event Action OnInitialized;

        /// <summary>
        /// Fired when an error occurs
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// Fired when wallet is connected
        /// </summary>
        public event Action<WalletSession> OnWalletConnected;

        /// <summary>
        /// Fired when wallet is disconnected
        /// </summary>
        public event Action OnWalletDisconnected;

        /// <summary>
        /// Fired when tournament registration is successful
        /// </summary>
        public event Action<string> OnTournamentRegistered;

        /// <summary>
        /// Fired when score is submitted successfully
        /// </summary>
        public event Action OnScoreSubmitted;

        #endregion

        #region Properties

        /// <summary>
        /// Whether SDK is initialized
        /// </summary>
        public bool IsInitialized => _authManager?.IsInitialized ?? false;

        /// <summary>
        /// Whether wallet is connected
        /// </summary>
        public bool IsWalletConnected => _walletManager?.IsConnected ?? false;

        /// <summary>
        /// Connected wallet address
        /// </summary>
        public string WalletAddress => _walletManager?.WalletAddress;

        /// <summary>
        /// Current SDK configuration
        /// </summary>
        public SDKConfig Config => _config;

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

        private void OnApplicationPause(bool pauseStatus)
        {
            // Handle deep link when app resumes (for mobile)
            if (!pauseStatus)
            {
                CheckForDeepLink();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize SDK with default configuration
        /// </summary>
        /// <param name="gameId">Game ID from Multiversed dashboard</param>
        /// <param name="apiKey">API Key from Multiversed dashboard</param>
        public void Initialize(string gameId, string apiKey)
        {
            Initialize(gameId, apiKey, new SDKConfig());
        }

        /// <summary>
        /// Initialize SDK with custom configuration
        /// </summary>
        /// <param name="gameId">Game ID from Multiversed dashboard</param>
        /// <param name="apiKey">API Key from Multiversed dashboard</param>
        /// <param name="config">SDK configuration</param>
        public void Initialize(string gameId, string apiKey, SDKConfig config)
        {
            if (IsInitialized)
            {
                Logger.LogWarning("SDK already initialized");
                return;
            }

            _config = config ?? new SDKConfig();
            Logger.EnableLogging = _config.EnableLogging;

            // Initialize auth manager
            _authManager = new AuthManager();
            _authManager.Initialize(gameId, apiKey);

            if (!_authManager.IsInitialized)
            {
                OnError?.Invoke("Failed to initialize authentication");
                return;
            }

            // Initialize API client
            _apiClient = new ApiClient(_authManager, _config);

            // Initialize wallet manager
            _walletManager = new WalletManager(_config, gameId);

            Logger.Log($"SDK initialized - Environment: {_config.Environment}");

            // Verify credentials
            StartCoroutine(VerifyCredentialsCoroutine());
        }

        /// <summary>
        /// Verify SDK credentials with server
        /// </summary>
        private IEnumerator VerifyCredentialsCoroutine()
        {
            yield return _apiClient.VerifyCredentials((success, message) =>
            {
                if (success)
                {
                    Logger.Log("SDK credentials verified");
                    OnInitialized?.Invoke();
                }
                else
                {
                    Logger.LogError($"Credential verification failed: {message}");
                    OnError?.Invoke($"Credential verification failed: {message}");
                }
            });
        }

        /// <summary>
        /// Set custom API base URL (for development)
        /// </summary>
        public void SetCustomApiUrl(string url)
        {
            _apiClient?.SetCustomBaseUrl(url);
        }

        #endregion

        #region Wallet Methods

        /// <summary>
        /// Connect wallet via Phantom
        /// </summary>
        public void ConnectWallet()
        {
            if (!IsInitialized)
            {
                OnError?.Invoke("SDK not initialized");
                return;
            }

            _walletManager.Connect(
                onSuccess: (session) =>
                {
                    Logger.Log($"Wallet connected: {session.GetShortAddress()}");
                    OnWalletConnected?.Invoke(session);
                },
                onError: (error) =>
                {
                    Logger.LogError($"Wallet connection failed: {error}");
                    OnError?.Invoke(error);
                }
            );
        }

        /// <summary>
        /// Disconnect wallet
        /// </summary>
        public void DisconnectWallet()
        {
            _walletManager?.Disconnect();
            OnWalletDisconnected?.Invoke();
        }

        /// <summary>
        /// Handle deep link callback (call from your app's deep link handler)
        /// </summary>
        public void HandleDeepLink(string url)
        {
            _walletManager?.HandleDeepLink(url);
        }

        /// <summary>
        /// Check for pending deep links
        /// </summary>
        private void CheckForDeepLink()
        {
            // Unity's absoluteURL contains the deep link on some platforms
            string deepLink = Application.absoluteURL;
            if (!string.IsNullOrEmpty(deepLink))
            {
                HandleDeepLink(deepLink);
            }
        }

        #endregion

        #region Tournament Methods

        /// <summary>
        /// Get all tournaments for this game
        /// </summary>
        public void GetTournaments(Action<List<Tournament>> onSuccess, Action<string> onError)
        {
            GetTournaments(_config.DefaultTokenType, onSuccess, onError);
        }

        /// <summary>
        /// Get all tournaments for this game with specific token type
        /// </summary>
        public void GetTournaments(TokenType tokenType, Action<List<Tournament>> onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("SDK not initialized");
                return;
            }

            StartCoroutine(_apiClient.GetTournaments(tokenType, (tournaments, error) =>
            {
                if (tournaments != null)
                {
                    onSuccess?.Invoke(new List<Tournament>(tournaments));
                }
                else
                {
                    onError?.Invoke(error);
                    OnError?.Invoke(error);
                }
            }));
        }

        /// <summary>
        /// Get single tournament by ID
        /// </summary>
        public void GetTournament(string tournamentId, Action<Tournament> onSuccess, Action<string> onError)
        {
            GetTournament(tournamentId, _config.DefaultTokenType, onSuccess, onError);
        }

        /// <summary>
        /// Get single tournament by ID with specific token type
        /// </summary>
        public void GetTournament(string tournamentId, TokenType tokenType, Action<Tournament> onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("SDK not initialized");
                return;
            }

            StartCoroutine(_apiClient.GetTournament(tournamentId, tokenType, (tournament, error) =>
            {
                if (tournament != null)
                {
                    onSuccess?.Invoke(tournament);
                }
                else
                {
                    onError?.Invoke(error);
                    OnError?.Invoke(error);
                }
            }));
        }

        /// <summary>
        /// Register for a tournament
        /// </summary>
        public void RegisterForTournament(string tournamentId, Action<string> onSuccess, Action<string> onError)
        {
            RegisterForTournament(tournamentId, _config.DefaultTokenType, onSuccess, onError);
        }

        /// <summary>
        /// Register for a tournament with specific token type
        /// </summary>
        public void RegisterForTournament(string tournamentId, TokenType tokenType, Action<string> onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("SDK not initialized");
                return;
            }

            if (!IsWalletConnected)
            {
                onError?.Invoke("Wallet not connected");
                return;
            }

            StartCoroutine(RegisterForTournamentCoroutine(tournamentId, tokenType, onSuccess, onError));
        }

        /// <summary>
        /// Tournament registration coroutine
        /// </summary>
        private IEnumerator RegisterForTournamentCoroutine(
            string tournamentId,
            TokenType tokenType,
            Action<string> onSuccess,
            Action<string> onError)
        {
            string unsignedTransaction = null;
            string prepareError = null;

            // Step 1: Get unsigned transaction
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
                onError?.Invoke(prepareError ?? "Failed to prepare registration");
                yield break;
            }

            // Step 2: Sign transaction via Phantom
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

            // Wait for signing to complete
            yield return new WaitUntil(() => signCompleted);

            if (string.IsNullOrEmpty(signature))
            {
                onError?.Invoke(signError ?? "Transaction signing failed");
                yield break;
            }

            // Step 3: Confirm registration
            yield return _apiClient.ConfirmRegistration(
                tournamentId,
                signature,
                tokenType,
                (success, message) =>
                {
                    if (success)
                    {
                        Logger.Log($"Registered for tournament: {tournamentId}");
                        onSuccess?.Invoke(signature);
                        OnTournamentRegistered?.Invoke(tournamentId);
                    }
                    else
                    {
                        onError?.Invoke(message);
                        OnError?.Invoke(message);
                    }
                }
            );
        }

        /// <summary>
        /// Get tournament leaderboard
        /// </summary>
        public void GetLeaderboard(string tournamentId, Action<List<LeaderboardEntry>> onSuccess, Action<string> onError)
        {
            GetLeaderboard(tournamentId, _config.DefaultTokenType, onSuccess, onError);
        }

        /// <summary>
        /// Get tournament leaderboard with specific token type
        /// </summary>
        public void GetLeaderboard(string tournamentId, TokenType tokenType, Action<List<LeaderboardEntry>> onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("SDK not initialized");
                return;
            }

            StartCoroutine(_apiClient.GetLeaderboard(tournamentId, tokenType, (entries, error) =>
            {
                if (entries != null)
                {
                    onSuccess?.Invoke(new List<LeaderboardEntry>(entries));
                }
                else
                {
                    onError?.Invoke(error);
                    OnError?.Invoke(error);
                }
            }));
        }

        #endregion

        #region Score Methods

        /// <summary>
        /// Submit score for a tournament
        /// </summary>
        public void SubmitScore(string tournamentId, int score, Action onSuccess, Action<string> onError)
        {
            SubmitScore(tournamentId, score, _config.DefaultTokenType, onSuccess, onError);
        }

        /// <summary>
        /// Submit score for a tournament with specific token type
        /// </summary>
        public void SubmitScore(string tournamentId, int score, TokenType tokenType, Action onSuccess, Action<string> onError)
        {
            if (!IsInitialized)
            {
                onError?.Invoke("SDK not initialized");
                return;
            }

            if (!IsWalletConnected)
            {
                onError?.Invoke("Wallet not connected");
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
                        Logger.Log($"Score submitted: {score}");
                        onSuccess?.Invoke();
                        OnScoreSubmitted?.Invoke();
                    }
                    else
                    {
                        onError?.Invoke(message);
                        OnError?.Invoke(message);
                    }
                }
            ));
        }

        #endregion
    }
}