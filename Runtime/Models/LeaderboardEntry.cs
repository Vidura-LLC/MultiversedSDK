namespace Multiversed.Models
{
    /// <summary>
    /// Leaderboard entry data
    /// </summary>
    [System.Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string walletAddress;
        public string userPublicKey;
        public int score;
        public string displayName;

        /// <summary>
        /// Get shortened wallet address for display
        /// </summary>
        public string GetShortAddress(int prefixLength = 4, int suffixLength = 4)
        {
            string address = !string.IsNullOrEmpty(walletAddress) ? walletAddress : userPublicKey;

            if (string.IsNullOrEmpty(address) || address.Length < prefixLength + suffixLength)
                return address;

            return $"{address.Substring(0, prefixLength)}...{address.Substring(address.Length - suffixLength)}";
        }
    }

    /// <summary>
    /// Leaderboard response from API
    /// </summary>
    [System.Serializable]
    public class LeaderboardResponse
    {
        public bool success;
        public LeaderboardEntry[] leaderboard;
        public string tournamentId;
    }
}