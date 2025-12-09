using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Multiversed.Utils;
using Multiversed.Models;

namespace Multiversed.Core
{
    /// <summary>
    /// HTTP client for Multiversed API calls
    /// </summary>
    public class ApiClient
    {
        private readonly AuthManager _authManager;
        private readonly SDKConfig _config;
        private string _baseUrl;

        // API Endpoints
        private const string ENDPOINT_VERIFY = "/api/sdk/verify";
        private const string ENDPOINT_TOURNAMENTS = "/api/sdk/tournaments";
        private const string ENDPOINT_PREPARE_REGISTRATION = "/api/sdk/tournaments/prepare-registration";
        private const string ENDPOINT_CONFIRM_REGISTRATION = "/api/sdk/tournaments/confirm-registration";
        private const string ENDPOINT_LEADERBOARD = "/api/sdk/tournaments/{0}/leaderboard";
        private const string ENDPOINT_SUBMIT_SCORE = "/api/sdk/tournaments/{0}/score";

        public ApiClient(AuthManager authManager, SDKConfig config)
        {
            _authManager = authManager;
            _config = config;
            UpdateBaseUrl();
        }

        /// <summary>
        /// Update base URL based on environment or custom URL
        /// </summary>
        public void UpdateBaseUrl()
        {
            // Check for custom URL first
            if (!string.IsNullOrEmpty(_config.CustomApiUrl))
            {
                _baseUrl = _config.CustomApiUrl.TrimEnd('/');
                SDKLogger.Log("API Base URL set to custom: " + _baseUrl);
                return;
            }

            // Otherwise use environment
            if (_config.Environment == SDKEnvironment.Mainnet)
            {
                _baseUrl = "https://api.multiversed.io";
            }
            else
            {
                _baseUrl = "https://devnet-api.multiversed.io";
            }

            SDKLogger.Log("API Base URL set to: " + _baseUrl);
        }

        /// <summary>
        /// Set custom base URL (for local development)
        /// </summary>
        public void SetCustomBaseUrl(string url)
        {
            _baseUrl = url.TrimEnd('/');
            SDKLogger.Log("API Base URL overridden to: " + _baseUrl);
        }

        #region Public API Methods

        /// <summary>
        /// Verify SDK credentials
        /// </summary>
        public IEnumerator VerifyCredentials(Action<bool, string> callback)
        {
            yield return PostRequest(ENDPOINT_VERIFY, null, (success, response) =>
            {
                if (success)
                {
                    var result = JsonHelper.FromJson<ApiResponse>(response);
                    bool isSuccess = result != null && result.success;
                    string message = result != null ? result.message : "Verification failed";
                    callback(isSuccess, message);
                }
                else
                {
                    callback(false, response);
                }
            });
        }

        /// <summary>
        /// Get all tournaments for this game
        /// </summary>
        public IEnumerator GetTournaments(TokenType tokenType, Action<Tournament[], string> callback)
        {
            string url = ENDPOINT_TOURNAMENTS + "?tokenType=" + (int)tokenType;

            yield return GetRequest(url, (success, response) =>
            {
                if (success)
                {
                    var result = JsonHelper.FromJson<TournamentListResponse>(response);
                    if (result != null && result.success)
                    {
                        // Use "data" field instead of "tournaments"
                        Tournament[] tournaments = result.data;
                        if (tournaments == null)
                        {
                            tournaments = new Tournament[0];
                        }
                        callback(tournaments, null);
                    }
                    else
                    {
                        callback(null, "Failed to parse tournaments");
                    }
                }
                else
                {
                    callback(null, response);
                }
            });
        }

        /// <summary>
        /// Get single tournament by ID
        /// </summary>
        public IEnumerator GetTournament(string tournamentId, TokenType tokenType, Action<Tournament, string> callback)
        {
            string url = ENDPOINT_TOURNAMENTS + "/" + tournamentId + "?tokenType=" + (int)tokenType;

            yield return GetRequest(url, (success, response) =>
            {
                if (success)
                {
                    var result = JsonHelper.FromJson<TournamentResponse>(response);
                    if (result != null && result.tournament != null)
                    {
                        callback(result.tournament, null);
                    }
                    else
                    {
                        callback(null, "Failed to parse tournament");
                    }
                }
                else
                {
                    callback(null, response);
                }
            });
        }
        /// <summary>
        /// Prepare tournament registration (get unsigned transaction)
        /// </summary>
        public IEnumerator PrepareRegistration(
            string tournamentId,
            string walletAddress,
            TokenType tokenType,
            Action<string, string> callback)
        {
            var body = new RegistrationRequest
            {
                tournamentId = tournamentId,
                userPublicKey = walletAddress,
                tokenType = (int)tokenType
            };

            string jsonBody = JsonHelper.ToJson(body);

            yield return PostRequest(ENDPOINT_PREPARE_REGISTRATION, jsonBody, (success, response) =>
            {
                if (success)
                {
                    var result = JsonHelper.FromJson<RegistrationResponse>(response);
                    if (result != null && result.success && !string.IsNullOrEmpty(result.transaction))
                    {
                        callback(result.transaction, null);
                    }
                    else
                    {
                        string errorMsg = result != null ? result.message : "Failed to prepare registration";
                        callback(null, errorMsg);
                    }
                }
                else
                {
                    callback(null, response);
                }
            });
        }

        /// <summary>
        /// Confirm tournament registration after signing
        /// </summary>
        public IEnumerator ConfirmRegistration(
            string tournamentId,
            string signature,
            TokenType tokenType,
            Action<bool, string> callback)
        {
            var body = new ConfirmRegistrationRequest
            {
                tournamentId = tournamentId,
                signature = signature,
                tokenType = (int)tokenType
            };

            string jsonBody = JsonHelper.ToJson(body);

            yield return PostRequest(ENDPOINT_CONFIRM_REGISTRATION, jsonBody, (success, response) =>
            {
                if (success)
                {
                    var result = JsonHelper.FromJson<RegistrationConfirmation>(response);
                    bool isSuccess = result != null && result.success;
                    string message = result != null ? result.message : "Confirmation failed";
                    callback(isSuccess, message);
                }
                else
                {
                    callback(false, response);
                }
            });
        }
        /// <summary>
        /// Get tournament leaderboard
        /// </summary>
        public IEnumerator GetLeaderboard(
            string tournamentId,
            TokenType tokenType,
            Action<LeaderboardEntry[], string> callback)
        {
            string url = string.Format(ENDPOINT_LEADERBOARD, tournamentId) + "?tokenType=" + (int)tokenType;

            yield return GetRequest(url, (success, response) =>
            {
                if (success)
                {
                    var result = JsonHelper.FromJson<LeaderboardResponse>(response);
                    if (result != null && result.success)
                    {
                        // Check for nested data structure first
                        if (result.data != null && result.data.leaderboard != null)
                        {
                            callback(result.data.leaderboard, null);
                        }
                        // Fallback to direct leaderboard array
                        else if (result.leaderboard != null)
                        {
                            callback(result.leaderboard, null);
                        }
                        else
                        {
                            // Empty leaderboard
                            callback(new LeaderboardEntry[0], null);
                        }
                    }
                    else
                    {
                        callback(null, "Failed to parse leaderboard");
                    }
                }
                else
                {
                    callback(null, response);
                }
            });
        }

        /// <summary>
        /// Submit player score
        /// </summary>
        public IEnumerator SubmitScore(
            string tournamentId,
            string walletAddress,
            int score,
            TokenType tokenType,
            Action<bool, string> callback)
        {
            string url = string.Format(ENDPOINT_SUBMIT_SCORE, tournamentId);

            var body = new SubmitScoreRequest
            {
                tournamentId = tournamentId,
                userPublicKey = walletAddress,
                score = score,
                tokenType = (int)tokenType
            };

            string jsonBody = JsonHelper.ToJson(body);

            yield return PostRequest(url, jsonBody, (success, response) =>
            {
                if (success)
                {
                    var result = JsonHelper.FromJson<ApiResponse>(response);
                    bool isSuccess = result != null && result.success;
                    string message = result != null ? result.message : "Score submission failed";
                    callback(isSuccess, message);
                }
                else
                {
                    callback(false, response);
                }
            });
        }

        #endregion

        #region HTTP Request Helpers

        /// <summary>
        /// Make GET request
        /// </summary>
        private IEnumerator GetRequest(string endpoint, Action<bool, string> callback)
        {
            string url = _baseUrl + endpoint;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                AddAuthHeaders(request);
                request.timeout = _config.RequestTimeoutSeconds;

                SDKLogger.Log("GET " + endpoint);

                yield return request.SendWebRequest();

                HandleResponse(request, callback);
            }
        }

        /// <summary>
        /// Make POST request
        /// </summary>
        private IEnumerator PostRequest(string endpoint, string jsonBody, Action<bool, string> callback)
        {
            string url = _baseUrl + endpoint;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                if (!string.IsNullOrEmpty(jsonBody))
                {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }

                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                AddAuthHeaders(request);
                request.timeout = _config.RequestTimeoutSeconds;

                SDKLogger.Log("POST " + endpoint);

                yield return request.SendWebRequest();

                HandleResponse(request, callback);
            }
        }

        /// <summary>
        /// Add authentication headers
        /// </summary>
        private void AddAuthHeaders(UnityWebRequest request)
        {
            if (_authManager.IsInitialized)
            {
                request.SetRequestHeader("x-api-key", _authManager.ApiKey);
                request.SetRequestHeader("x-game-id", _authManager.GameId);
            }
        }

        /// <summary>
        /// Handle HTTP response
        /// </summary>
        private void HandleResponse(UnityWebRequest request, Action<bool, string> callback)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                SDKLogger.Log("Response: " + TruncateForLog(response));
                callback(true, response);
            }
            else
            {
                string error = "";
                if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    error = request.downloadHandler.text;
                }
                else
                {
                    error = request.error;
                }

                SDKLogger.LogError("Request failed: " + error);
                callback(false, error);
            }
        }

        /// <summary>
        /// Truncate long strings for logging
        /// </summary>
        private string TruncateForLog(string text, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength) + "...";
        }

        #endregion

        #region Request Models

        [Serializable]
        private class RegistrationRequest
        {
            public string tournamentId;
            public string userPublicKey;
            public int tokenType;
        }

        [Serializable]
        private class ConfirmRegistrationRequest
        {
            public string tournamentId;
            public string signature;
            public int tokenType;
        }

        [Serializable]
        private class SubmitScoreRequest
        {
            public string tournamentId;
            public string userPublicKey;
            public int score;
            public int tokenType;
        }

        #endregion
    }
}