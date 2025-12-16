using System;
using System.Text;
using UnityEngine;

namespace Multiversed.Wallet
{
    public static class PhantomDeepLink
    {
        // Use Phantom's browse URL for simpler connection (no encryption needed)
        private const string PHANTOM_BROWSE_URL = "https://phantom.app/ul/browse/";
        private const string PHANTOM_CONNECT_URL = "phantom://";
        
        private static string _appScheme;
        private static string _cluster = "devnet";
        private static string _appUrl = "https://multiversed.io";

        public static void Initialize(string appScheme, bool isDevnet = true)
        {
            _appScheme = appScheme;
            _cluster = isDevnet ? "devnet" : "mainnet-beta";
            Debug.Log("[Phantom] Initialized with scheme: " + _appScheme + ", cluster: " + _cluster);
        }

        /// <summary>
        /// Generate the connect URL for Phantom wallet using the simpler approach
        /// </summary>
        public static string GetConnectUrl()
        {
            if (string.IsNullOrEmpty(_appScheme))
            {
                Debug.LogError("[Phantom] App scheme not initialized!");
                return null;
            }

            // Method 1: Direct Phantom connect URL (works on mobile)
            // Format: phantom://connect?app_url=<URL>&redirect_url=<URL>&cluster=<CLUSTER>
            
            string redirectUrl = _appScheme + "://onConnect";
            
            StringBuilder urlBuilder = new StringBuilder();
            urlBuilder.Append("phantom://v1/connect");
            urlBuilder.Append("?app_url=").Append(Uri.EscapeDataString(_appUrl));
            urlBuilder.Append("&dapp_encryption_public_key=").Append(GenerateBase58Key());
            urlBuilder.Append("&redirect_link=").Append(Uri.EscapeDataString(redirectUrl));
            urlBuilder.Append("&cluster=").Append(_cluster);

            string connectUrl = urlBuilder.ToString();
            Debug.Log("[Phantom] Connect URL: " + connectUrl);
            return connectUrl;
        }

        /// <summary>
        /// Alternative: Get connect URL using HTTPS universal link
        /// </summary>
        public static string GetConnectUrlHttps()
        {
            if (string.IsNullOrEmpty(_appScheme))
            {
                Debug.LogError("[Phantom] App scheme not initialized!");
                return null;
            }

            string redirectUrl = _appScheme + "://onConnect";
            
            StringBuilder urlBuilder = new StringBuilder();
            urlBuilder.Append("https://phantom.app/ul/v1/connect");
            urlBuilder.Append("?app_url=").Append(Uri.EscapeDataString(_appUrl));
            urlBuilder.Append("&dapp_encryption_public_key=").Append(GenerateBase58Key());
            urlBuilder.Append("&redirect_link=").Append(Uri.EscapeDataString(redirectUrl));
            urlBuilder.Append("&cluster=").Append(_cluster);

            string connectUrl = urlBuilder.ToString();
            Debug.Log("[Phantom] Connect URL (HTTPS): " + connectUrl);
            return connectUrl;
        }

        /// <summary>
        /// Generate URL to sign and send a transaction
        /// </summary>
        public static string GetSignAndSendTransactionUrl(string base64Transaction, string session)
        {
            if (string.IsNullOrEmpty(_appScheme))
            {
                Debug.LogError("[Phantom] App scheme not initialized!");
                return null;
            }

            string redirectUrl = _appScheme + "://onSignAndSendTransaction";
            
            StringBuilder urlBuilder = new StringBuilder();
            urlBuilder.Append("phantom://v1/signAndSendTransaction");
            urlBuilder.Append("?transaction=").Append(Uri.EscapeDataString(base64Transaction));
            urlBuilder.Append("&redirect_link=").Append(Uri.EscapeDataString(redirectUrl));
            
            if (!string.IsNullOrEmpty(session))
            {
                urlBuilder.Append("&session=").Append(Uri.EscapeDataString(session));
            }

            string url = urlBuilder.ToString();
            Debug.Log("[Phantom] Sign Transaction URL: " + url);
            return url;
        }

        /// <summary>
        /// Generate disconnect URL
        /// </summary>
        public static string GetDisconnectUrl(string session)
        {
            if (string.IsNullOrEmpty(_appScheme))
            {
                return null;
            }

            string redirectUrl = _appScheme + "://onDisconnect";
            
            StringBuilder urlBuilder = new StringBuilder();
            urlBuilder.Append("phantom://v1/disconnect");
            urlBuilder.Append("?redirect_link=").Append(Uri.EscapeDataString(redirectUrl));
            
            if (!string.IsNullOrEmpty(session))
            {
                urlBuilder.Append("&session=").Append(Uri.EscapeDataString(session));
            }

            return urlBuilder.ToString();
        }

        /// <summary>
        /// Parse the response from Phantom after connect
        /// </summary>
        public static bool ParseConnectResponse(string url, out string publicKey, out string session, out string error)
        {
            publicKey = null;
            session = null;
            error = null;

            try
            {
                Debug.Log("[Phantom] Parsing connect response: " + url);

                // Check for error
                string errorCode = GetQueryParameter(url, "errorCode");
                if (!string.IsNullOrEmpty(errorCode))
                {
                    error = GetQueryParameter(url, "errorMessage") ?? "Connection rejected (code: " + errorCode + ")";
                    Debug.LogError("[Phantom] Connection error: " + error);
                    return false;
                }

                // Try to get public key from various parameter names
                publicKey = GetQueryParameter(url, "phantom_encryption_public_key");
                
                if (string.IsNullOrEmpty(publicKey))
                {
                    publicKey = GetQueryParameter(url, "public_key");
                }

                // Get session/nonce data
                session = GetQueryParameter(url, "nonce");
                if (string.IsNullOrEmpty(session))
                {
                    session = GetQueryParameter(url, "data");
                }

                // For the encrypted response, we need to decrypt it
                // But first, let's check if we got a direct public key
                string data = GetQueryParameter(url, "data");
                if (!string.IsNullOrEmpty(data) && string.IsNullOrEmpty(publicKey))
                {
                    // The data might contain the encrypted response
                    // For now, we'll try to use phantom_encryption_public_key as the wallet address
                    // This is a simplification - full implementation would require decryption
                    Debug.Log("[Phantom] Got encrypted data, checking phantom_encryption_public_key");
                }

                if (!string.IsNullOrEmpty(publicKey))
                {
                    Debug.Log("[Phantom] Connected! Public Key: " + publicKey);
                    return true;
                }

                // If we still don't have a public key, the connection might have succeeded
                // but we need to handle the encrypted response
                error = "Could not extract wallet address from response. URL: " + url;
                Debug.LogWarning("[Phantom] " + error);
                return false;
            }
            catch (Exception e)
            {
                error = e.Message;
                Debug.LogError("[Phantom] Parse error: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Parse transaction signature from response
        /// </summary>
        public static bool ParseSignatureResponse(string url, out string signature, out string error)
        {
            signature = null;
            error = null;

            try
            {
                string errorCode = GetQueryParameter(url, "errorCode");
                if (!string.IsNullOrEmpty(errorCode))
                {
                    error = GetQueryParameter(url, "errorMessage") ?? "Transaction rejected";
                    return false;
                }

                signature = GetQueryParameter(url, "signature");
                
                if (!string.IsNullOrEmpty(signature))
                {
                    Debug.Log("[Phantom] Signature: " + signature);
                    return true;
                }

                error = "Failed to parse signature";
                return false;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        /// <summary>
        /// Open Phantom app with the given URL
        /// </summary>
        public static void OpenUrl(string url)
        {
            Debug.Log("[Phantom] Opening URL: " + url);
            Application.OpenURL(url);
        }

        /// <summary>
        /// Get query parameter from URL
        /// </summary>
        private static string GetQueryParameter(string url, string key)
        {
            try
            {
                // Handle both ? and # as query separators (some deep links use fragments)
                int queryStart = url.IndexOf('?');
                int fragmentStart = url.IndexOf('#');
                
                int start = queryStart >= 0 ? queryStart : fragmentStart;
                if (start < 0) return null;

                string query = url.Substring(start + 1);
                
                // Also check fragment if exists
                if (queryStart >= 0 && fragmentStart > queryStart)
                {
                    query = url.Substring(queryStart + 1);
                }

                string[] pairs = query.Split('&');

                foreach (string pair in pairs)
                {
                    int eqIndex = pair.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        string pairKey = pair.Substring(0, eqIndex);
                        if (pairKey == key)
                        {
                            return Uri.UnescapeDataString(pair.Substring(eqIndex + 1));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Phantom] Error parsing query param '" + key + "': " + e.Message);
            }

            return null;
        }

        /// <summary>
        /// Generate a Base58-encoded 32-byte key for Phantom
        /// </summary>
        private static string GenerateBase58Key()
        {
            byte[] key = new byte[32];
            
            // Use a deterministic key for testing (in production, use proper key generation)
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            
            return Base58Encode(key);
        }

        /// <summary>
        /// Base58 encoding (used by Solana)
        /// </summary>
        private static string Base58Encode(byte[] data)
        {
            const string ALPHABET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
            
            // Count leading zeros
            int leadingZeros = 0;
            foreach (byte b in data)
            {
                if (b == 0) leadingZeros++;
                else break;
            }

            // Convert to base58
            var result = new System.Collections.Generic.List<char>();
            
            // Work with a copy as BigInteger-like processing
            byte[] input = new byte[data.Length];
            Array.Copy(data, input, data.Length);

            while (!IsZero(input))
            {
                int remainder = DivideBy58(input);
                result.Insert(0, ALPHABET[remainder]);
            }

            // Add leading '1's for each leading zero byte
            for (int i = 0; i < leadingZeros; i++)
            {
                result.Insert(0, '1');
            }

            return new string(result.ToArray());
        }

        private static bool IsZero(byte[] data)
        {
            foreach (byte b in data)
            {
                if (b != 0) return false;
            }
            return true;
        }

        private static int DivideBy58(byte[] data)
        {
            int remainder = 0;
            for (int i = 0; i < data.Length; i++)
            {
                int value = (remainder << 8) + data[i];
                data[i] = (byte)(value / 58);
                remainder = value % 58;
            }
            return remainder;
        }
    }
}