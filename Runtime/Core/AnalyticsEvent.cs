// File: Runtime/Core/AnalyticsEvent.cs
using System;
using System.Collections.Generic;

namespace Multiversed.Core
{
    /// <summary>
    /// Analytics event data model
    /// </summary>
    [Serializable]
    public class AnalyticsEvent
    {
        public string eventName;
        public Dictionary<string, object> eventData;
        public string sdkVersion;
        public string unityVersion;
        public string platform;
        public string osVersion;
        public string environment;
        public string tokenType;
        public string sessionId;
        public string walletAddress;
        public long clientTimestamp;

        public AnalyticsEvent(string eventName)
        {
            this.eventName = eventName;
            this.eventData = new Dictionary<string, object>();
            // Use DateTime.UtcNow for Unity compatibility
            var epoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            this.clientTimestamp = (long)(System.DateTime.UtcNow - epoch).TotalMilliseconds;
        }

        public AnalyticsEvent WithData(string key, object value)
        {
            eventData[key] = value;
            return this;
        }

        public AnalyticsEvent WithError(string error, string errorType = null)
        {
            eventData["error"] = error;
            if (!string.IsNullOrEmpty(errorType))
            {
                eventData["error_type"] = errorType;
            }
            return this;
        }
    }

    /// <summary>
    /// Batch events request model
    /// </summary>
    [Serializable]
    public class BatchEventsRequest
    {
        public AnalyticsEvent[] events;
    }

    /// <summary>
    /// Analytics response model
    /// </summary>
    [Serializable]
    public class AnalyticsResponse
    {
        public bool success;
        public int tracked;
        public int total;
    }
}

