// File: Runtime/Wallet/WalletManager.cs
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

            bool isDevnet = _config.Environment == SDKEnvironment.Devnet;
            string appUrl = !string.IsNullOrEmpty(_config.CustomAppUrl) 
                ? _config.CustomAppUrl 
                : null; // Use default in PhantomDeepLink

            // Initialize static PhantomDeepLink
            PhantomDeepLink.Initialize(urlScheme, isDevnet, appUrl);

            SDKLogger.Log("WalletManager initialized with scheme: " + urlScheme);
        }

        /// <summary>
        /// Connect wallet via Phantom
        /// </summary>
/// <summary>
/// Connect wallet via Phantom
/// </summary>
public void Connect(Action<WalletSession> onSuccess, Action<string> onError)
{
    _onConnectSuccess = onSuccess;
    _onConnectError = onError;

    string connectUrl = PhantomDeepLink.GetConnectUrl();  // Use consistent variable name
    
    if (!string.IsNullOrEmpty(connectUrl))
    {
        PhantomDeepLink.OpenUrl(connectUrl);
        SDKLogger.Log("Opening Phantom for wallet connection...");
    }
    else
    {
        if (onError != null)
        {
            onError("Failed to generate connect URL");
        }
    }
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

            // Use session token if available, otherwise use wallet address
            string session = _session.SessionToken ?? _session.WalletAddress ?? string.Empty;

            // Use signTransaction (sign only) instead of signAndSendTransaction,
            // since some Phantom environments don't support the latter.
            string signUrl = PhantomDeepLink.GetSignTransactionUrl(base64Transaction, session);
            if (!string.IsNullOrEmpty(signUrl))
            {
                PhantomDeepLink.OpenUrl(signUrl);
                SDKLogger.Log("Opening Phantom for transaction signing...");
            }
            else
            {
                if (onError != null)
                {
                    onError("Failed to generate sign transaction URL");
                }
            }
        }

        /// <summary>
        /// Handle deep link callback from Phantom
        /// </summary>
        public void HandleDeepLink(string url)
        {
            SDKLogger.Log("Handling deep link: " + url);

            // Phantom returns to: multiversed-<gameId>://onConnect?...
            // or: multiversed-<gameId>://onSignTransaction?...

            if (url.Contains("onConnect"))
            {
                HandleConnectCallback(url);
            }
            else if (url.Contains("onSignAndSendTransaction") || url.Contains("onSignTransaction"))
            {
                HandleSignTransactionCallback(url);
            }
            else if (url.Contains("onSignMessage"))
            {
                // Reuse the same handler for message signing flows
                HandleSignTransactionCallback(url);
            }
            else if (url.Contains("onDisconnect"))
            {
                _session.Disconnect();
                SDKLogger.Log("Wallet disconnected via deep link");
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
            string publicKey;
            string session;
            string error;

            bool success = PhantomDeepLink.ParseConnectResponse(url, out publicKey, out session, out error);

            if (success && !string.IsNullOrEmpty(publicKey))
            {
                _session.Connect(publicKey, session, null);
                if (_onConnectSuccess != null)
                {
                    _onConnectSuccess(_session);
                }
            }
            else
            {
                string errorMsg = error ?? "Connection failed";
                SDKLogger.LogError("Connect failed: " + errorMsg);
                if (_onConnectError != null)
                {
                    _onConnectError(errorMsg);
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
            string signature;
            string error;

            bool success = PhantomDeepLink.ParseSignatureResponse(url, out signature, out error);

            if (success && !string.IsNullOrEmpty(signature))
            {
                int maxLen = Math.Min(20, signature.Length);
                SDKLogger.Log("Transaction signed: " + signature.Substring(0, maxLen) + "...");
                if (_onSignSuccess != null)
                {
                    _onSignSuccess(signature);
                }
            }
            else
            {
                string errorMsg = error ?? "Transaction signing failed";
                SDKLogger.LogError("Sign transaction failed: " + errorMsg);
                if (_onSignError != null)
                {
                    _onSignError(errorMsg);
                }
            }

            // Clear callbacks and pending data
            _onSignSuccess = null;
            _onSignError = null;
            _pendingTournamentId = null;
        }

        /// <summary>
        /// Check if Phantom is installed
        /// Note: This is a placeholder - actual detection would require platform-specific code
        /// </summary>
        public bool IsPhantomInstalled()
        {
            // Phantom detection is platform-specific and complex
            // For now, we assume it's available if the user can open the URL
            // In production, you might want to implement platform-specific checks
            return true;
        }
    }
}