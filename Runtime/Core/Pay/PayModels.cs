using System;

namespace Multiversed.Models
{
    [Serializable]
    public class PayRequest
    {
        public string userId;
        public string contextType;
        public string contextId;
        public int tokenType;
    }

    [Serializable]
    public class PayResponse
    {
        public bool success;
        public string status;
        public string txSignature;
        public string serializedTx;
        public string intentId;
        public string topUpUrl;
        public string shortfall;
        public string message;
    }

    [Serializable]
    public class PayStatusResponse
    {
        public bool success;
        public string status;
        public string txSignature;
        public bool canRetry;
        public string failureReason;
        public string error;
    }

    public class PayResult
    {
        public bool IsSuccess;
        public string TxSignature;
        public string Status;
        public string ErrorMessage;
    }
}

