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

        /// <summary>
        /// Check if tournament is currently active
        /// </summary>
        public bool IsActive => status == "Active";

        /// <summary>
        /// Check if tournament is open for registration
        /// </summary>
        public bool IsOpenForRegistration => status == "Not Started" || status == "Active";

        /// <summary>
        /// Check if tournament has ended
        /// </summary>
        public bool HasEnded => status == "Ended" || status == "Distributed" || status == "Awarded";
    }

    /// <summary>
    /// Tournament list response
    /// </summary>
    [System.Serializable]
    public class TournamentListResponse
    {
        public bool success;
        public Tournament[] tournaments;
    }

    /// <summary>
    /// Single tournament response
    /// </summary>
    [System.Serializable]
    public class TournamentResponse
    {
        public bool success;
        public Tournament tournament;
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