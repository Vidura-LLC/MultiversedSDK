// File: Runtime/Auth/YIPAuth.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Multiversed.Core;
using Multiversed.Utils;

namespace Multiversed.Core.Auth
{
    /// <summary>
    /// YIP account authentication: browser login + deep link session exchange.
    /// </summary>
    [Serializable]
    public class SessionExchangeResponse
    {
        public bool success;
        public string userId;
        public string walletType;
        public string publicKey;
        public string message;
    }

    public class YIPAuth
    {
        private const string YIP_USER_ID_KEY = "YIPUserId";
        private const string YIP_WALLET_TYPE_KEY = "YIPWalletType";
        private const string YIP_PUBLIC_KEY_KEY = "YIPPublicKey";

        private const string DefaultWebAppBase = "https://app.yip.fun";

        private readonly ApiClient _apiClient;
        private readonly SDKConfig _config;

        public YIPAuth(ApiClient apiClient, SDKConfig config)
        {
            _apiClient = apiClient;
            _config = config;
        }

        public string UserId
        {
            get
            {
                if (!PlayerPrefs.HasKey(YIP_USER_ID_KEY))
                {
                    return null;
                }
                string v = PlayerPrefs.GetString(YIP_USER_ID_KEY);
                return string.IsNullOrEmpty(v) ? null : v;
            }
        }

        public string WalletType
        {
            get
            {
                if (!PlayerPrefs.HasKey(YIP_WALLET_TYPE_KEY))
                {
                    return null;
                }
                string v = PlayerPrefs.GetString(YIP_WALLET_TYPE_KEY);
                return string.IsNullOrEmpty(v) ? null : v;
            }
        }

        public string PublicKey
        {
            get
            {
                if (!PlayerPrefs.HasKey(YIP_PUBLIC_KEY_KEY))
                {
                    return null;
                }
                string v = PlayerPrefs.GetString(YIP_PUBLIC_KEY_KEY);
                return string.IsNullOrEmpty(v) ? null : v;
            }
        }

        public bool IsConnected
        {
            get { return !string.IsNullOrEmpty(UserId); }
        }

        public void Login(string gameId, string returnScheme = "yip")
        {
            if (string.IsNullOrEmpty(gameId))
            {
                SDKLogger.LogWarning("[YIPAuth] Login called with empty gameId");
                return;
            }

            if (string.IsNullOrEmpty(returnScheme))
            {
                returnScheme = "yip";
            }

            string webAppBase;
            if (_config != null && !string.IsNullOrEmpty(_config.CustomApiUrl))
            {
                webAppBase = _config.GetApiUrl().TrimEnd('/');
            }
            else
            {
                webAppBase = DefaultWebAppBase;
            }

            string url = webAppBase
                + "/sdk/auth?gameId="
                + UnityWebRequest.EscapeURL(gameId)
                + "&returnScheme="
                + UnityWebRequest.EscapeURL(returnScheme);

            SDKLogger.Log("[YIPAuth] Opening browser for login: " + url);
            Application.OpenURL(url);
        }

        public IEnumerator HandleDeepLink(string url, Action<bool, string> callback)
        {
            string token = ParseQueryParam(url, "token");
            if (string.IsNullOrEmpty(token))
            {
                callback(false, "No token in deep link");
                yield break;
            }

            SessionExchangeResponse result = null;
            string exchangeError = null;

            yield return _apiClient.ExchangeSessionToken(token, (r, err) =>
            {
                result = r;
                exchangeError = err;
            });

            if (result != null && string.IsNullOrEmpty(exchangeError))
            {
                PlayerPrefs.SetString(YIP_USER_ID_KEY, result.userId);
                PlayerPrefs.SetString(YIP_WALLET_TYPE_KEY, result.walletType);
                if (!string.IsNullOrEmpty(result.publicKey))
                {
                    PlayerPrefs.SetString(YIP_PUBLIC_KEY_KEY, result.publicKey);
                }
                else
                {
                    PlayerPrefs.DeleteKey(YIP_PUBLIC_KEY_KEY);
                }
                PlayerPrefs.Save();
                SDKLogger.Log(
                    "[YIPAuth] Connected: "
                    + result.userId
                    + " ("
                    + result.walletType
                    + ")"
                );
                callback(true, null);
            }
            else
            {
                string err = !string.IsNullOrEmpty(exchangeError)
                    ? exchangeError
                    : (result != null ? result.message : null) ?? "Exchange failed";
                SDKLogger.LogError("[YIPAuth] Exchange failed: " + err);
                callback(false, err);
            }
        }

        public void Logout()
        {
            PlayerPrefs.DeleteKey(YIP_USER_ID_KEY);
            PlayerPrefs.DeleteKey(YIP_WALLET_TYPE_KEY);
            PlayerPrefs.DeleteKey(YIP_PUBLIC_KEY_KEY);
            PlayerPrefs.Save();
            SDKLogger.Log("[YIPAuth] Logged out");
        }

        /// <summary>
        /// Simple query parsing without System.Uri.
        /// </summary>
        public static string ParseQueryParam(string url, string key)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
            {
                return null;
            }

            string needle = key + "=";
            int idx = url.IndexOf(needle, StringComparison.Ordinal);
            if (idx < 0)
            {
                return null;
            }

            int start = idx + needle.Length;
            int end = url.IndexOf('&', start);
            if (end < 0)
            {
                end = url.Length;
            }

            if (start >= end)
            {
                return null;
            }

            string raw = url.Substring(start, end - start);
            if (string.IsNullOrEmpty(raw))
            {
                return null;
            }

            return UnityWebRequest.UnEscapeURL(raw);
        }
    }
}
