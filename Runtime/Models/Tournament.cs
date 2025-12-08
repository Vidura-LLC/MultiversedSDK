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
        public TokenType tokenType;
        public string prizePool;
        public string createdBy;
        public string createdAt;

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
            get { return status == "Ended" || status == "Distributed" || status == "Awarded"; }
        }
    }

    /// <summary>
    /// Tournament list response - uses "data" field from API
    /// </summary>
    [System.Serializable]
    public class TournamentListResponse
    {
        public bool success;
        public Tournament[] data;  // Changed from "tournaments" to "data"
    }

    /// <summary>
    /// Single tournament response
    /// </summary>
    [System.Serializable]
    public class TournamentResponse
    {
        public bool success;
        public Tournament tournament;
        public Tournament data;  // Some endpoints might use "data"
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
}