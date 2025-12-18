namespace Multiversed.Models
{
    /// <summary>
    /// Player information
    /// </summary>
    [System.Serializable]
    public class Player
    {
        public string walletAddress;
        public string visibleAddress;
        public int score;
        public int rank;

        /// <summary>
        /// Get shortened wallet address for display
        /// </summary>
        public string GetShortAddress(int prefixLength = 4, int suffixLength = 4)
        {
            if (string.IsNullOrEmpty(walletAddress) || walletAddress.Length < prefixLength + suffixLength)
                return walletAddress;

            return $"{walletAddress.Substring(0, prefixLength)}...{walletAddress.Substring(walletAddress.Length - suffixLength)}";
        }
    }

    /// <summary>
    /// Tournament participant data
    /// </summary>
    [System.Serializable]
    public class Participant
    {
        public string userPublicKey;
        public int score;
        public int rank;
    }
}