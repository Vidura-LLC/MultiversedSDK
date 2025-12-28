// File: Runtime/Models/LeaderboardEntry.cs
namespace Multiversed.Models
{
    /// <summary>
    /// Leaderboard entry data model
    /// </summary>
    [System.Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string walletAddress;
        public string userPublicKey;
        public string playerId;  // Add this - API uses playerId
        public int score;
        public string oddsMultiplier;
        public string oddsPercentage;
        public string oddsAmount;
        public string oddsJackpot;

        /// <summary>
        /// Get the wallet address (checks all possible field names)
        /// </summary>
        public string GetAddress()
        {
            if (!string.IsNullOrEmpty(walletAddress))
                return walletAddress;
            if (!string.IsNullOrEmpty(userPublicKey))
                return userPublicKey;
            if (!string.IsNullOrEmpty(playerId))
                return playerId;
            return "";
        }

        /// <summary>
        /// Get shortened wallet address for display
        /// </summary>
        public string GetShortAddress(int prefixLength = 4, int suffixLength = 4)
        {
            string address = GetAddress();
            if (string.IsNullOrEmpty(address) || address.Length < prefixLength + suffixLength)
                return address;

            return address.Substring(0, prefixLength) + "..." + address.Substring(address.Length - suffixLength);
        }
    }

    /// <summary>
    /// Leaderboard response - API returns nested structure
    /// </summary>
    [System.Serializable]
    public class LeaderboardResponse
    {
        public bool success;
        public LeaderboardData data;
        public LeaderboardEntry[] leaderboard;
    }

    /// <summary>
    /// Leaderboard data wrapper
    /// </summary>
    [System.Serializable]
    public class LeaderboardData
    {
        public string tournamentName;
        public string gameId;
        public string status;
        public LeaderboardEntry[] leaderboard;
    }
}