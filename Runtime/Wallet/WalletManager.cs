using System;
using UnityEngine;
using Multiversed.Core;
using Multiversed.Utils;

namespace Multiversed.Wallet
{
    /// <summary>
    /// Manages wallet connection and transaction signing
    /// </summary>
    public class WalletManager
    {
        private readonly SDKConfig _config;
        private readonly WalletSession _session;
        private PhantomDeepLink _phantomDeepLink;

        // Pending callbacks
        private Action<WalletSession> _onConnectSuccess;
        private Action<string> _onConnectError;
        private Action<string> _onSignSuccess;
        private Action<string> _onSignError;

        // Pending transaction data
        private string _pendingTournamentId;

        public WalletSession Session { get { return _session; } }
        public bool IsConnected { get { return _session.IsConnected; } }
        public string WalletAddress { get { return _session.WalletAddress; } }

        public WalletManager(SDKConfig config, string gameId)
        {
            _config = config;
            _session = new WalletSession();

            // Load any existing session
            _session.LoadFromPrefs();

            // Initialize Phantom deep link handler
            InitializePhantomDeepLink(gameId);
        }

        /// <summary>
        /// Initialize Phantom deep link with proper URL scheme
        /// </summary>
        private void InitializePhantomDeepLink(string gameId)
        {
            string urlScheme;
            if (!string.IsNullOrEmpty(_config.CustomUrlScheme))
            {
                urlScheme = _config.CustomUrlScheme;
            }
            else
            {
                int len = Math.Min(8, gameId.Length);
                urlScheme = "multiversed-" + gameId.Substring(0, len);
            }

            string appUrl = "https://multiversed.io";
            bool isDevnet = _config.Environment == SDKEnvironment.Devnet;

            _phantomDeepLink = new PhantomDeepLink(appUrl, urlScheme, isDevnet);

            SDKLogger.Log("WalletManager initialized with scheme: " + urlScheme);
        }

        /// <summary>
        /// Connect wallet via Phantom
        /// </summary>
        public void Connect(Action<WalletSession> onSuccess, Action<string> onError)
        {
            _onConnectSuccess = onSuccess;
            _onConnectError = onError;

            string connectUrl = _phantomDeepLink.GetConnectUrl();
            _phantomDeepLink.OpenUrl(connectUrl);

            SDKLogger.Log("Opening Phantom for wallet connection...");
        }

        /// <summary>
        /// Disconnect wallet
        /// </summary>
        public void Disconnect()
        {
            _session.Disconnect();
            SDKLogger.Log("Wallet disconnected");
        }

        /// <summary>
        /// Sign and send transaction via Phantom
        /// </summary>
        public void SignAndSendTransaction(
            string base64Transaction,
            string tournamentId,
            Action<string> onSuccess,
            Action<string> onError)
        {
            if (!IsConnected)
            {
                onError("Wallet not connected");
                return;
            }

            _onSignSuccess = onSuccess;
            _onSignError = onError;
            _pendingTournamentId = tournamentId;

            string signUrl = _phantomDeepLink.GetSignAndSendTransactionUrl(base64Transaction);
            _phantomDeepLink.OpenUrl(signUrl);

            SDKLogger.Log("Opening Phantom for transaction signing...");
        }

        /// <summary>
        /// Handle deep link callback from Phantom
        /// </summary>
        public void HandleDeepLink(string url)
        {
            int maxLen = Math.Min(50, url.Length);
            SDKLogger.Log("Handling deep link: " + url.Substring(0, maxLen) + "...");

            if (url.Contains("/callback/connect"))
            {
                HandleConnectCallback(url);
            }
            else if (url.Contains("/callback/signTransaction"))
            {
                HandleSignTransactionCallback(url);
            }
            else
            {
                SDKLogger.LogWarning("Unknown deep link callback: " + url);
            }
        }

        /// <summary>
        /// Handle connect callback from Phantom
        /// </summary>
        private void HandleConnectCallback(string url)
        {
            var result = _phantomDeepLink.ParseConnectCallback(url);

            if (result.Success)
            {
                _session.Connect(result.PublicKey, null, result.PhantomEncryptionPublicKey);
                if (_onConnectSuccess != null)
                {
                    _onConnectSuccess(_session);
                }
            }
            else
            {
                SDKLogger.LogError("Connect failed: " + result.Error);
                if (_onConnectError != null)
                {
                    _onConnectError(result.Error);
                }
            }

            // Clear callbacks
            _onConnectSuccess = null;
            _onConnectError = null;
        }

        /// <summary>
        /// Handle sign transaction callback from Phantom
        /// </summary>
        private void HandleSignTransactionCallback(string url)
        {
            var result = _phantomDeepLink.ParseSignTransactionCallback(url);

            if (result.Success)
            {
                int maxLen = Math.Min(20, result.Signature.Length);
                SDKLogger.Log("Transaction signed: " + result.Signature.Substring(0, maxLen) + "...");
                if (_onSignSuccess != null)
                {
                    _onSignSuccess(result.Signature);
                }
            }
            else
            {
                SDKLogger.LogError("Sign transaction failed: " + result.Error);
                if (_onSignError != null)
                {
                    _onSignError(result.Error);
                }
            }

            // Clear callbacks and pending data
            _onSignSuccess = null;
            _onSignError = null;
            _pendingTournamentId = null;
        }

        /// <summary>
        /// Check if Phantom is installed
        /// </summary>
        public bool IsPhantomInstalled()
        {
            return _phantomDeepLink.IsPhantomInstalled();
        }
    }
}