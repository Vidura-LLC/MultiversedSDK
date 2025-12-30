// File: Runtime/Core/MultiversedSettings.cs
using UnityEngine;

namespace Multiversed.Core
{
    /// <summary>
    /// ScriptableObject for Multiversed SDK configuration
    /// Create an asset instance via: Assets > Create > Multiversed > SDK Settings
    /// </summary>
    [CreateAssetMenu(fileName = "MultiversedSettings", menuName = "Multiversed/SDK Settings", order = 1)]
    public class MultiversedSettings : ScriptableObject
    {
        [Header("Credentials")]
        [Tooltip("Your Game ID from the Multiversed Developer Dashboard")]
        public string gameId = "";

        [Tooltip("Your API Key from the Multiversed Developer Dashboard")]
        public string apiKey = "";

        [Header("Environment")]
        [Tooltip("Network environment: Devnet (testing) or Mainnet (production)")]
        public SDKEnvironment environment = SDKEnvironment.Devnet;

        [Header("Token Configuration")]
        [Tooltip("Default token type for tournaments")]
        public TokenType defaultTokenType = TokenType.SPL;

        [Header("URL Configuration")]
        [Tooltip("Custom API base URL (leave empty to use environment defaults)")]
        public string customApiUrl = "";

        [Tooltip("Custom app URL for Phantom wallet deep links (leave empty for default)")]
        public string customAppUrl = "";

        [Tooltip("Custom URL scheme for deep link callbacks (leave empty for auto-generated)")]
        public string customUrlScheme = "";

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableLogging = true;

        [Tooltip("Request timeout in seconds")]
        [Range(5, 120)]
        public int requestTimeoutSeconds = 30;

        /// <summary>
        /// Convert this ScriptableObject to SDKConfig for runtime use
        /// </summary>
        public SDKConfig ToSDKConfig()
        {
            return new SDKConfig
            {
                Environment = environment,
                CustomApiUrl = string.IsNullOrEmpty(customApiUrl) ? null : customApiUrl,
                CustomAppUrl = string.IsNullOrEmpty(customAppUrl) ? null : customAppUrl,
                CustomUrlScheme = string.IsNullOrEmpty(customUrlScheme) ? null : customUrlScheme,
                DefaultTokenType = defaultTokenType,
                EnableLogging = enableLogging,
                RequestTimeoutSeconds = requestTimeoutSeconds
            };
        }

        /// <summary>
        /// Validate settings and return any errors
        /// </summary>
        public string Validate()
        {
            if (string.IsNullOrEmpty(gameId))
                return "Game ID is required";

            if (string.IsNullOrEmpty(apiKey))
                return "API Key is required";

            if (!string.IsNullOrEmpty(customApiUrl) && !IsValidUrl(customApiUrl))
                return "Custom API URL is not a valid URL";

            if (!string.IsNullOrEmpty(customAppUrl) && !IsValidUrl(customAppUrl))
                return "Custom App URL is not a valid URL";

            return null; // No errors
        }

        private bool IsValidUrl(string url)
        {
            return !string.IsNullOrEmpty(url) && 
                   (url.StartsWith("http://") || url.StartsWith("https://"));
        }
    }
}

