using System;
using System.Text;
using UnityEngine;
using Multiversed.Utils;

namespace Multiversed.Wallet
{
    /// <summary>
    /// Handles Phantom wallet deep link generation and parsing
    /// </summary>
    public class PhantomDeepLink
    {
        // Phantom deep link base URLs
        private const string PHANTOM_CONNECT_URL = "https://phantom.app/ul/v1/connect";
        private const string PHANTOM_SIGN_TRANSACTION_URL = "https://phantom.app/ul/v1/signAndSendTransaction";
        private const string PHANTOM_SIGN_MESSAGE_URL = "https://phantom.app/ul/v1/signMessage";

        // Phantom app scheme (for checking if installed)
        private const string PHANTOM_APP_SCHEME = "phantom://";

        private readonly string _appUrl;
        private readonly string _redirectScheme;
        private readonly string _cluster;

        /// <summary>
        /// Create PhantomDeepLink handler
        /// </summary>
        /// <param name="appUrl">Your app's URL (for display in Phantom)</param>
        /// <param name="redirectScheme">URL scheme for callback (e.g., "mygame://")</param>
        /// <param name="isDevnet">Use devnet cluster</param>
        public PhantomDeepLink(string appUrl, string redirectScheme, bool isDevnet = true)
        {
            _appUrl = appUrl;
            _redirectScheme = redirectScheme.TrimEnd('/');
            _cluster = isDevnet ? "devnet" : "mainnet-beta";
        }

        /// <summary>
        /// Generate connect wallet deep link URL
        /// </summary>
        public string GetConnectUrl()
        {
            string redirectLink = $"{_redirectScheme}/callback/connect";

            var url = $"{PHANTOM_CONNECT_URL}" +
                      $"?app_url={Uri.EscapeDataString(_appUrl)}" +
                      $"&dapp_encryption_public_key=" +
                      $"&redirect_link={Uri.EscapeDataString(redirectLink)}" +
                      $"&cluster={_cluster}";

            Logger.Log($"Generated connect URL: {url}");
            return url;
        }

        /// <summary>
        /// Generate sign and send transaction deep link URL
        /// </summary>
        /// <param name="base64Transaction">Base64 encoded transaction</param>
        public string GetSignAndSendTransactionUrl(string base64Transaction)
        {
            string redirectLink = $"{_redirectScheme}/callback/signTransaction";

            var url = $"{PHANTOM_SIGN_TRANSACTION_URL}" +
                      $"?transaction={Uri.EscapeDataString(base64Transaction)}" +
                      $"&redirect_link={Uri.EscapeDataString(redirectLink)}" +
                      $"&cluster={_cluster}";

            Logger.Log($"Generated sign transaction URL");
            return url;
        }

        /// <summary>
        /// Generate sign message deep link URL
        /// </summary>
        /// <param name="message">Message to sign</param>
        public string GetSignMessageUrl(string message)
        {
            string base64Message = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));
            string redirectLink = $"{_redirectScheme}/callback/signMessage";

            var url = $"{PHANTOM_SIGN_MESSAGE_URL}" +
                      $"?message={Uri.EscapeDataString(base64Message)}" +
                      $"&redirect_link={Uri.EscapeDataString(redirectLink)}" +
                      $"&cluster={_cluster}";

            Logger.Log($"Generated sign message URL");
            return url;
        }

        /// <summary>
        /// Parse connect callback URL
        /// </summary>
        /// <param name="callbackUrl">Full callback URL from Phantom</param>
        /// <returns>Wallet public key or null if error</returns>
        public ConnectResult ParseConnectCallback(string callbackUrl)
        {
            try
            {
                var uri = new Uri(callbackUrl);
                var query = ParseQueryString(uri.Query);

                // Check for error
                if (query.TryGetValue("errorCode", out string errorCode))
                {
                    query.TryGetValue("errorMessage", out string errorMessage);
                    return new ConnectResult
                    {
                        Success = false,
                        Error = $"{errorCode}: {errorMessage}"
                    };
                }

                // Get public key
                if (query.TryGetValue("phantom_encryption_public_key", out string phantomKey) &&
                    query.TryGetValue("public_key", out string publicKey))
                {
                    return new ConnectResult
                    {
                        Success = true,
                        PublicKey = publicKey,
                        PhantomEncryptionPublicKey = phantomKey
                    };
                }

                return new ConnectResult
                {
                    Success = false,
                    Error = "Missing public key in callback"
                };
            }
            catch (Exception e)
            {
                Logger.LogError($"Error parsing connect callback: {e.Message}");
                return new ConnectResult
                {
                    Success = false,
                    Error = e.Message
                };
            }
        }

        /// <summary>
        /// Parse sign transaction callback URL
        /// </summary>
        public SignTransactionResult ParseSignTransactionCallback(string callbackUrl)
        {
            try
            {
                var uri = new Uri(callbackUrl);
                var query = ParseQueryString(uri.Query);

                // Check for error
                if (query.TryGetValue("errorCode", out string errorCode))
                {
                    query.TryGetValue("errorMessage", out string errorMessage);
                    return new SignTransactionResult
                    {
                        Success = false,
                        Error = $"{errorCode}: {errorMessage}"
                    };
                }

                // Get signature
                if (query.TryGetValue("signature", out string signature))
                {
                    return new SignTransactionResult
                    {
                        Success = true,
                        Signature = signature
                    };
                }

                return new SignTransactionResult
                {
                    Success = false,
                    Error = "Missing signature in callback"
                };
            }
            catch (Exception e)
            {
                Logger.LogError($"Error parsing sign transaction callback: {e.Message}");
                return new SignTransactionResult
                {
                    Success = false,
                    Error = e.Message
                };
            }
        }

        /// <summary>
        /// Check if Phantom app is installed (mobile only)
        /// </summary>
        public bool IsPhantomInstalled()
        {
#if UNITY_ANDROID
            try
            {
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", PHANTOM_APP_SCHEME))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW", uri))
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
                {
                    var activities = packageManager.Call<AndroidJavaObject>("queryIntentActivities", intent, 0);
                    int count = activities.Call<int>("size");
                    return count > 0;
                }
            }
            catch
            {
                return false;
            }
#elif UNITY_IOS
            // iOS implementation would use canOpenURL
            return true; // Assume installed, will fail gracefully
#else
            return false;
#endif
        }

        /// <summary>
        /// Open URL in browser or app
        /// </summary>
        public void OpenUrl(string url)
        {
            Logger.Log($"Opening URL: {url.Substring(0, Math.Min(100, url.Length))}...");
            Application.OpenURL(url);
        }

        /// <summary>
        /// Parse query string into dictionary
        /// </summary>
        private System.Collections.Generic.Dictionary<string, string> ParseQueryString(string query)
        {
            var dict = new System.Collections.Generic.Dictionary<string, string>();

            if (string.IsNullOrEmpty(query))
                return dict;

            query = query.TrimStart('?');

            foreach (var param in query.Split('&'))
            {
                var parts = param.Split('=');
                if (parts.Length == 2)
                {
                    dict[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                }
            }

            return dict;
        }

        #region Result Types

        public class ConnectResult
        {
            public bool Success;
            public string PublicKey;
            public string PhantomEncryptionPublicKey;
            public string Error;
        }

        public class SignTransactionResult
        {
            public bool Success;
            public string Signature;
            public string Error;
        }

        #endregion
    }
}