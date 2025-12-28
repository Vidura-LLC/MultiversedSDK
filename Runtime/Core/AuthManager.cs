// File: Runtime/Core/AuthManager.cs
using Multiversed.Utils;

namespace Multiversed.Core
{
    /// <summary>
    /// Manages SDK authentication credentials
    /// </summary>
    public class AuthManager
    {
        private string _gameId;
        private string _apiKey;
        private bool _isInitialized;

        public string GameId { get { return _gameId; } }
        public string ApiKey { get { return _apiKey; } }
        public bool IsInitialized { get { return _isInitialized; } }

        /// <summary>
        /// Initialize authentication with game credentials
        /// </summary>
        public void Initialize(string gameId, string apiKey)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                SDKLogger.LogError("Game ID cannot be null or empty");
                return;
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                SDKLogger.LogError("API Key cannot be null or empty");
                return;
            }

            if (!ValidateApiKeyFormat(apiKey))
            {
                SDKLogger.LogError("Invalid API Key format. Expected format: sk_live_* or sk_test_*");
                return;
            }

            _gameId = gameId;
            _apiKey = apiKey;
            _isInitialized = true;

            SDKLogger.Log("AuthManager initialized for game: " + GetMaskedGameId());
        }

        /// <summary>
        /// Clear authentication credentials
        /// </summary>
        public void Clear()
        {
            _gameId = null;
            _apiKey = null;
            _isInitialized = false;

            SDKLogger.Log("AuthManager credentials cleared");
        }

        /// <summary>
        /// Validate API key format
        /// </summary>
        private bool ValidateApiKeyFormat(string apiKey)
        {
            return apiKey.StartsWith("sk_live_") || apiKey.StartsWith("sk_test_");
        }

        /// <summary>
        /// Get masked game ID for logging (show first 8 chars only)
        /// </summary>
        private string GetMaskedGameId()
        {
            if (string.IsNullOrEmpty(_gameId) || _gameId.Length <= 8)
                return _gameId;

            return _gameId.Substring(0, 8) + "...";
        }

        /// <summary>
        /// Get masked API key for logging (show prefix only)
        /// </summary>
        public string GetMaskedApiKey()
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Length <= 16)
                return "***";

            return _apiKey.Substring(0, 16) + "...";
        }
    }
}