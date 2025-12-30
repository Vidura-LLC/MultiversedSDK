using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Multiversed;
using Multiversed.Core;
using Multiversed.Models;
using Multiversed.Wallet;

namespace Multiversed.Samples
{
    /// <summary>
    /// Sample game manager demonstrating Multiversed SDK integration
    /// </summary>
    public class SampleGameManager : MonoBehaviour
    {
        [Header("SDK Configuration")]
        [SerializeField] private string gameId = "YOUR_GAME_ID";
        [SerializeField] private string apiKey = "YOUR_API_KEY";
        [SerializeField] private bool useDevnet = true;

        [Header("UI References (Optional)")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text walletText;
        [SerializeField] private Button connectWalletButton;
        [SerializeField] private Button disconnectWalletButton;
        [SerializeField] private Button getTournamentsButton;
        [SerializeField] private Transform tournamentListParent;

        [Header("Game State")]
        [SerializeField] private int currentScore = 0;

        private List<Tournament> _tournaments = new List<Tournament>();
        private string _currentTournamentId;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeSDK();
            SetupUI();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (MultiversedSDK.Instance != null)
            {
                MultiversedSDK.Instance.OnInitialized -= OnSDKInitialized;
                MultiversedSDK.Instance.OnError -= OnSDKError;
                MultiversedSDK.Instance.OnWalletConnected -= OnWalletConnected;
                MultiversedSDK.Instance.OnWalletDisconnected -= OnWalletDisconnected;
                MultiversedSDK.Instance.OnTournamentRegistered -= OnTournamentRegistered;
                MultiversedSDK.Instance.OnScoreSubmitted -= OnScoreSubmitted;
            }
        }

        #endregion

        #region SDK Initialization

        private void InitializeSDK()
        {
            UpdateStatus("Initializing SDK...");

            // Configure SDK
            var config = new SDKConfig
            {
                Environment = useDevnet ? SDKEnvironment.Devnet : SDKEnvironment.Mainnet,
                DefaultTokenType = TokenType.SPL,
                EnableLogging = true
            };

            // Subscribe to events before initialization
            MultiversedSDK.Instance.OnInitialized += OnSDKInitialized;
            MultiversedSDK.Instance.OnError += OnSDKError;
            MultiversedSDK.Instance.OnWalletConnected += OnWalletConnected;
            MultiversedSDK.Instance.OnWalletDisconnected += OnWalletDisconnected;
            MultiversedSDK.Instance.OnTournamentRegistered += OnTournamentRegistered;
            MultiversedSDK.Instance.OnScoreSubmitted += OnScoreSubmitted;

            // Initialize SDK
            MultiversedSDK.Instance.Initialize(gameId, apiKey, config);

            // For local development, you can override the API URL
            // MultiversedSDK.Instance.SetCustomApiUrl("http://localhost:5000");
        }

        private void SetupUI()
        {
            if (connectWalletButton != null)
                connectWalletButton.onClick.AddListener(OnConnectWalletClicked);

            if (disconnectWalletButton != null)
                disconnectWalletButton.onClick.AddListener(OnDisconnectWalletClicked);

            if (getTournamentsButton != null)
                getTournamentsButton.onClick.AddListener(OnGetTournamentsClicked);

            UpdateWalletUI();
        }

        #endregion

        #region SDK Event Handlers

        private void OnSDKInitialized()
        {
            UpdateStatus("SDK initialized successfully!");
            Debug.Log("[Sample] SDK initialized");

            // Check if wallet was previously connected
            if (MultiversedSDK.Instance.IsWalletConnected)
            {
                UpdateStatus($"Wallet restored: {MultiversedSDK.Instance.WalletAddress}");
            }

            UpdateWalletUI();
        }

        private void OnSDKError(string error)
        {
            UpdateStatus($"Error: {error}");
            Debug.LogError($"[Sample] SDK Error: {error}");
        }

        private void OnWalletConnected(WalletSession session)
        {
            UpdateStatus($"Wallet connected: {session.GetShortAddress()}");
            Debug.Log($"[Sample] Wallet connected: {session.WalletAddress}");
            UpdateWalletUI();
        }

        private void OnWalletDisconnected()
        {
            UpdateStatus("Wallet disconnected");
            Debug.Log("[Sample] Wallet disconnected");
            UpdateWalletUI();
        }

        private void OnTournamentRegistered(string tournamentId)
        {
            UpdateStatus($"Registered for tournament: {tournamentId}");
            Debug.Log($"[Sample] Registered for tournament: {tournamentId}");
            _currentTournamentId = tournamentId;
        }

        private void OnScoreSubmitted()
        {
            UpdateStatus($"Score submitted: {currentScore}");
            Debug.Log($"[Sample] Score submitted: {currentScore}");
        }

        #endregion

        #region UI Button Handlers

        private void OnConnectWalletClicked()
        {
            UpdateStatus("Connecting wallet...");
            MultiversedSDK.Instance.ConnectWallet();
        }

        private void OnDisconnectWalletClicked()
        {
            MultiversedSDK.Instance.DisconnectWallet();
        }

        private void OnGetTournamentsClicked()
        {
            UpdateStatus("Fetching tournaments...");
            GetTournaments();
        }

        #endregion

        #region Public Methods (Call from your game)

        /// <summary>
        /// Connect wallet - call this from a UI button
        /// </summary>
        public void ConnectWallet()
        {
            if (!MultiversedSDK.Instance.IsInitialized)
            {
                Debug.LogWarning("[Sample] SDK not initialized yet");
                return;
            }

            MultiversedSDK.Instance.ConnectWallet();
        }

        /// <summary>
        /// Disconnect wallet
        /// </summary>
        public void DisconnectWallet()
        {
            MultiversedSDK.Instance.DisconnectWallet();
        }

        /// <summary>
        /// Get available tournaments
        /// </summary>
        public void GetTournaments()
        {
            MultiversedSDK.Instance.GetTournaments(
                onSuccess: (tournaments) =>
                {
                    _tournaments = tournaments;
                    UpdateStatus($"Found {tournaments.Count} tournaments");
                    Debug.Log($"[Sample] Loaded {tournaments.Count} tournaments");

                    foreach (var t in tournaments)
                    {
                        Debug.Log($"  - {t.name} ({t.status}) - Entry: {t.entryFee}");
                    }
                },
                onError: (error) =>
                {
                    UpdateStatus($"Failed to get tournaments: {error}");
                }
            );
        }

        /// <summary>
        /// Register for a tournament
        /// </summary>
        public void RegisterForTournament(string tournamentId)
        {
            if (!MultiversedSDK.Instance.IsWalletConnected)
            {
                UpdateStatus("Please connect wallet first");
                return;
            }

            UpdateStatus("Registering for tournament...");

            MultiversedSDK.Instance.RegisterForTournament(
                tournamentId,
                onSuccess: (signature) =>
                {
                    _currentTournamentId = tournamentId;
                    UpdateStatus($"Registration successful!");
                },
                onError: (error) =>
                {
                    UpdateStatus($"Registration failed: {error}");
                }
            );
        }

        /// <summary>
        /// Submit score for current tournament
        /// </summary>
        public void SubmitScore(int score)
        {
            if (string.IsNullOrEmpty(_currentTournamentId))
            {
                UpdateStatus("Not registered for any tournament");
                return;
            }

            currentScore = score;
            UpdateStatus($"Submitting score: {score}...");

            MultiversedSDK.Instance.SubmitScore(
                _currentTournamentId,
                score,
                onSuccess: () =>
                {
                    UpdateStatus($"Score {score} submitted!");
                },
                onError: (error) =>
                {
                    UpdateStatus($"Score submission failed: {error}");
                }
            );
        }

        /// <summary>
        /// Get leaderboard for a tournament
        /// </summary>
        public void GetLeaderboard(string tournamentId)
        {
            MultiversedSDK.Instance.GetLeaderboard(
                tournamentId,
                onSuccess: (entries) =>
                {
                    UpdateStatus($"Leaderboard loaded: {entries.Count} entries");

                    foreach (var entry in entries)
                    {
                        Debug.Log($"  #{entry.rank}: {entry.GetShortAddress()} - {entry.score}");
                    }
                },
                onError: (error) =>
                {
                    UpdateStatus($"Failed to get leaderboard: {error}");
                }
            );
        }

        /// <summary>
        /// Add points to current score (call during gameplay)
        /// </summary>
        public void AddScore(int points)
        {
            currentScore += points;
            Debug.Log($"[Sample] Score: {currentScore}");
        }

        #endregion

        #region Deep Link Handling

        /// <summary>
        /// Call this from your app's deep link handler
        /// </summary>
        public void HandleDeepLink(string url)
        {
            Debug.Log($"[Sample] Received deep link: {url}");
            MultiversedSDK.Instance.HandleDeepLink(url);
        }

        #endregion

        #region Helper Methods

        private void UpdateStatus(string message)
        {
            Debug.Log($"[Sample] {message}");

            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void UpdateWalletUI()
        {
            bool isConnected = MultiversedSDK.Instance?.IsWalletConnected ?? false;

            if (walletText != null)
            {
                walletText.text = isConnected
                    ? MultiversedSDK.Instance.WalletAddress
                    : "Not connected";
            }

            if (connectWalletButton != null)
                connectWalletButton.gameObject.SetActive(!isConnected);

            if (disconnectWalletButton != null)
                disconnectWalletButton.gameObject.SetActive(isConnected);
        }

        #endregion
    }
}