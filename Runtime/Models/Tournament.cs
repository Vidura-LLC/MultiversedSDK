// File: Runtime/Models/Tournament.cs
using Multiversed.Core;

namespace Multiversed.Models
{
    /// <summary>
    /// Tournament data model
    /// </summary>
    [System.Serializable]
    public class Tournament
    {
        public string id;
        public string name;
        public string description;
        public string gameId;
        public float entryFee;
        public string startTime;
        public string endTime;
        public string status;
        public int participantsCount;
        public int maxParticipants;
        public int max_participants;
        public TokenType tokenType;
        // Deprecated: prize pool model replaced by reward ladder vault.
        // Kept for backwards compatibility — will be null on new tournaments.
        public string prizePool;
        public string createdBy;
        public string createdAt;
        public bool isRegistered; // Set by server if walletAddress is provided in query
        public string rewardLadderId;
        public string tournamentDistribution;
        public RewardLadderInfo rewardLadder;

        // Helper to get max participants from either field
        public int GetMaxParticipants()
        {
            if (maxParticipants > 0) return maxParticipants;
            return max_participants;
        }

        public bool IsActive
        {
            get { return status == "Active"; }
        }

        public bool IsOpenForRegistration
        {
            get { return status == "Not Started" || status == "Active"; }
        }

        public bool HasEnded
        {
            get
            {
                return status == "Ended"
                    || status == "Settled"
                    || status == "Distributed"
                    || status == "Awarded";
            }
        }
    }

    /// <summary>
    /// Tournament list response - uses "data" field from API
    /// </summary>
    [System.Serializable]
    public class TournamentListResponse
    {
        public bool success;
        public Tournament[] data;
    }

    /// <summary>
    /// Single tournament response - API returns "tournament" field directly
    /// </summary>
    [System.Serializable]
    public class TournamentResponse
    {
        public bool success;
        public Tournament tournament;  // Direct tournament object
    }

    /// <summary>
    /// Tournament registration response
    /// </summary>
    [System.Serializable]
    public class RegistrationResponse
    {
        public bool success;
        public string message;
        public string transaction;
    }

    /// <summary>
    /// Registration confirmation response
    /// </summary>
    [System.Serializable]
    public class RegistrationConfirmation
    {
        public bool success;
        public string message;
        public string tournamentId;
    }

    [System.Serializable]
    public class RewardTierInfo
    {
        public int minParticipants;
        public int maxParticipants;
        public int winnerCount;
        public string[] amounts; // lamports as strings
    }

    [System.Serializable]
    public class RewardLadderInfo
    {
        public string address;
        public string admin;
        public string name;
        public int tierCount;
        public RewardTierInfo[] tiers;
        public int tokenType;
        public bool isActive;
    }

    [System.Serializable]
    public class RewardLadderListResponse
    {
        public bool success;
        public RewardLadderInfo[] data;
        public int count;
    }
}