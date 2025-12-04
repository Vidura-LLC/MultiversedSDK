using System;
using System.Collections;
using System.Collections.Generic;
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
        /// Update base URL based on environment
        /// </summary>
        public void UpdateBaseUrl()
        {
            _baseUrl = _config.Environment == SDKEnvironment.Mainnet
                ? "https://api.multiversed.io"
                : "https://devnet-api.multiversed.io";

            Logger.Log($"API Base URL set to: {_baseUrl}");
        }

        /// <summary>
        /// Set custom base URL (for local development)
        /// </summary>
        public void SetCustomBaseUrl(string url)
        {
            _baseUrl = url.TrimEnd('/');
            Logger.Log($"API Base URL overridden to: {_baseUrl}");
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
                    callback?.Invoke(result?.success ?? false, result?.message ?? "Verification failed");
                }
                else
                {
                    callback?.Invoke(false, response);
                }
            });
        }

        /// <summary>
        /// Get all tournaments for this game
        /// </summary>
        public IEnumerator GetTournaments(TokenType tokenType, Action<Tournament[], string> callback)
        {
            string url = $"{ENDPOINT_TOURNAMENTS}?tokenType={(int)tokenType}";

            yield return GetRequest(url, (success, response) =>
            {
                if (success)
                {
                    var result = JsonHelper.FromJson<TournamentListResponse>(response);
                    if (result != null && result.success)
                    {
                        callback?.Invoke(result.tournaments, null);
                    }
                    else
                    {
                        callback?.Invoke(null, "Failed to parse tournaments");
                    }
                }
                else
                {
                    callback?.Invoke(null, response);
                }
            });
        }

        /// <summary>
        /// Get single tournament by ID
        /// </summary>
        public IEnumerator GetTournament(string tournamentId, TokenType tokenType, Action<Tournament, string> callback)
        {
            string url = $"{ENDPOINT_TOURNAMENTS}/{tournamentId}?tokenType={(int)tokenType}";

            yield return GetRequest(url, (success, response) =>
            {
                if (success)
                {
                    var result = JsonHelper.FromJson<TournamentResponse>(response);
                    if (result != null && result.success)
                    {
                        callback?.Invoke(result.tournament, null);
                    }
                    else
                    {
                        callback?.Invoke(null, "Failed to parse tournament");
                    }
                }
                else
                {
                    callback?.Invoke(null, response);
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
                        callback?.Invoke(result.transaction, null);
                    }
                    else
                    {
                        callback?.Invoke(null, result?.message ?? "Failed to prepare registration");
                    }
                }
                else
                {
                    callback?.Invoke(null, response);
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
                    callback?.Invoke(result?.success ?? false, result?.message ?? "Confirmation failed");
                }
                else
                {
                    callback?.Invoke(false, response);
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
            string url = string.Format(ENDPOINT_LEADERBOARD, tournamentId) + $"?tokenType={(int)tokenType}";

            yield return GetRequest(url, (success, response) =>
            {
                if (success)
                {
                    var result = JsonHelper.FromJson<LeaderboardResponse>(response);
                    if (result != null && result.success)
                    {
                        callback?.Invoke(result.leaderboard, null);
                    }
                    else
                    {
                        callback?.Invoke(null, "Failed to parse leaderboard");
                    }
                }
                else
                {
                    callback?.Invoke(null, response);
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
                    callback?.Invoke(result?.success ?? false, result?.message ?? "Score submission failed");
                }
                else
                {
                    callback?.Invoke(false, response);
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

                Logger.Log($"GET {endpoint}");

                yield return request.SendWebRequest();

                HandleResponse(request, callback);
            }
        }

        /// <summary>
        /// Make POST request
        /// </summary>
        private IEnumerator PostRequest(string endpoint, string jsonBody, Action<bool, string> callback)