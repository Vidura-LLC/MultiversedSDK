// File: Runtime/Wallet/WalletSession.cs
using UnityEngine;
using Multiversed.Utils;

namespace Multiversed.Wallet
{
    /// <summary>
    /// Stores wallet connection session data
    /// </summary>
    [System.Serializable]
    public class WalletSession
    {
        private const string PREFS_KEY_ADDRESS = "multiversed_wallet_address";
        private const string PREFS_KEY_SESSION = "multiversed_wallet_session";
        private const string PREFS_KEY_PHANTOM_KEY = "multiversed_phantom_public_key";

        public string WalletAddress { get; private set; }
        public string SessionToken { get; private set; }
        public string PhantomEncryptionPublicKey { get; private set; }

        public bool IsConnected
        {
            get { return !string.IsNullOrEmpty(WalletAddress); }
        }

        /// <summary>
        /// Set wallet connection data
        /// </summary>
        public void Connect(string walletAddress, string sessionToken = null, string phantomPublicKey = null)
        {
            WalletAddress = walletAddress;
            SessionToken = sessionToken;
            PhantomEncryptionPublicKey = phantomPublicKey;

            SaveToPrefs();

            SDKLogger.Log("Wallet connected: " + GetShortAddress());
        }

        /// <summary>
        /// Clear wallet session
        /// </summary>
        public void Disconnect()
        {
            WalletAddress = null;
            SessionToken = null;
            PhantomEncryptionPublicKey = null;

            ClearPrefs();

            SDKLogger.Log("Wallet disconnected");
        }

        /// <summary>
        /// Load session from PlayerPrefs
        /// </summary>
        public void LoadFromPrefs()
        {
            WalletAddress = PlayerPrefs.GetString(PREFS_KEY_ADDRESS, null);
            SessionToken = PlayerPrefs.GetString(PREFS_KEY_SESSION, null);
            PhantomEncryptionPublicKey = PlayerPrefs.GetString(PREFS_KEY_PHANTOM_KEY, null);

            if (IsConnected)
            {
                SDKLogger.Log("Loaded wallet session: " + GetShortAddress());
            }
        }

        /// <summary>
        /// Save session to PlayerPrefs
        /// </summary>
        private void SaveToPrefs()
        {
            if (!string.IsNullOrEmpty(WalletAddress))
                PlayerPrefs.SetString(PREFS_KEY_ADDRESS, WalletAddress);

            if (!string.IsNullOrEmpty(SessionToken))
                PlayerPrefs.SetString(PREFS_KEY_SESSION, SessionToken);

            if (!string.IsNullOrEmpty(PhantomEncryptionPublicKey))
                PlayerPrefs.SetString(PREFS_KEY_PHANTOM_KEY, PhantomEncryptionPublicKey);

            PlayerPrefs.Save();
        }

        /// <summary>
        /// Clear session from PlayerPrefs
        /// </summary>
        private void ClearPrefs()
        {
            PlayerPrefs.DeleteKey(PREFS_KEY_ADDRESS);
            PlayerPrefs.DeleteKey(PREFS_KEY_SESSION);
            PlayerPrefs.DeleteKey(PREFS_KEY_PHANTOM_KEY);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Get shortened wallet address for display
        /// </summary>
        public string GetShortAddress(int prefixLength = 4, int suffixLength = 4)
        {
            if (string.IsNullOrEmpty(WalletAddress) || WalletAddress.Length < prefixLength + suffixLength)
                return WalletAddress;

            return WalletAddress.Substring(0, prefixLength) + "..." + WalletAddress.Substring(WalletAddress.Length - suffixLength);
        }
    }
}