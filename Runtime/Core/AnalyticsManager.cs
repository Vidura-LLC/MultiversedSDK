// File: Runtime/Core/AnalyticsManager.cs
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Multiversed.Utils;

namespace Multiversed.Core
{
    /// <summary>
    /// Manages analytics event tracking and batching
    /// </summary>
    public class AnalyticsManager
    {
        #region Singleton

        private static AnalyticsManager _instance;
        public static AnalyticsManager Instance => _instance ??= new AnalyticsManager();

        #endregion

        #region Configuration

        private string _gameId;
        private string _apiKey;
        private string _baseUrl;
        private string _sessionId;
        private bool _enabled = true;
        private bool _initialized = false;

        #endregion

        #region Queue Management

        private readonly Queue<AnalyticsEvent> _eventQueue = new Queue<AnalyticsEvent>();
        private readonly object _queueLock = new object();
        private MonoBehaviour _coroutineRunner;
        private Coroutine _flushCoroutine;

        #endregion

        #region Constants

        private const int MAX_QUEUE_SIZE = 100;
        private const int BATCH_SIZE = 20;
        private const float FLUSH_INTERVAL_SECONDS = 30f;
        private const int REQUEST_TIMEOUT_SECONDS = 10;

        #endregion

        #region Context

        private string _unityVersion;
        private string _platform;
        private string _osVersion;
        private string _environment;
        private string _defaultTokenType;
        private string _walletAddress;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize analytics manager
        /// </summary>
        public void Initialize(
            string gameId,
            string apiKey,
            string baseUrl,
            MonoBehaviour coroutineRunner,
            SDKConfig config)
        {
            if (_initialized)
            {
                SDKLogger.LogWarning("[Analytics] Already initialized");
                return;
            }

            _gameId = gameId;
            _apiKey = apiKey;
            _baseUrl = baseUrl;
            _coroutineRunner = coroutineRunner;
            _enabled = config?.EnableAnalytics ?? true;

            // Generate session ID
            _sessionId = GenerateSessionId();

            // Capture context
            _unityVersion = Application.unityVersion;
            _platform = GetPlatform();
            _osVersion = SystemInfo.operatingSystem;
            _environment = config?.Environment.ToString() ?? "Devnet";
            _defaultTokenType = config?.DefaultTokenType.ToString() ?? "SPL";

            _initialized = true;

            // Start periodic flush
            if (_enabled && _coroutineRunner != null)
            {
                _flushCoroutine = _coroutineRunner.StartCoroutine(PeriodicFlush());
            }

            SDKLogger.Log($"[Analytics] Initialized - Enabled: {_enabled}, Session: {_sessionId}");
        }

        #endregion

        #region Event Tracking

        /// <summary>
        /// Track an analytics event
        /// </summary>
        public void Track(string eventName, Dictionary<string, object> eventData = null)
        {
            if (!_enabled || !_initialized)
            {
                return;
            }

            var analyticsEvent = new AnalyticsEvent(eventName)
            {
                eventData = eventData ?? new Dictionary<string, object>(),
                sdkVersion = SDKVersion.Current,
                unityVersion = _unityVersion,
                platform = _platform,
                osVersion = _osVersion,
                environment = _environment,
                tokenType = _defaultTokenType,
                sessionId = _sessionId,
                walletAddress = _walletAddress
            };

            EnqueueEvent(analyticsEvent);

            // Immediate flush for high-priority events
            if (IsHighPriorityEvent(eventName))
            {
                FlushAsync();
            }

            SDKLogger.Log($"[Analytics] Tracked: {eventName}");
        }

        /// <summary>
        /// Track an error event with details
        /// </summary>
        public void TrackError(string eventName, string error, string errorType = null)
        {
            var eventData = new Dictionary<string, object>
            {
                { "error", error }
            };

            if (!string.IsNullOrEmpty(errorType))
            {
                eventData["error_type"] = errorType;
            }

            Track(eventName, eventData);
        }

        /// <summary>
        /// Track event with token type override
        /// </summary>
        public void Track(string eventName, TokenType tokenType, Dictionary<string, object> eventData = null)
        {
            if (!_enabled || !_initialized)
            {
                return;
            }

            var data = eventData ?? new Dictionary<string, object>();

            var analyticsEvent = new AnalyticsEvent(eventName)
            {
                eventData = data,
                sdkVersion = SDKVersion.Current,
                unityVersion = _unityVersion,
                platform = _platform,
                osVersion = _osVersion,
                environment = _environment,
                tokenType = tokenType.ToString(),
                sessionId = _sessionId,
                walletAddress = _walletAddress
            };

            EnqueueEvent(analyticsEvent);

            if (IsHighPriorityEvent(eventName))
            {
                FlushAsync();
            }

            SDKLogger.Log($"[Analytics] Tracked: {eventName} (TokenType: {tokenType})");
        }

        #endregion

        #region Queue Management

        private void EnqueueEvent(AnalyticsEvent evt)
        {
            lock (_queueLock)
            {
                // Drop oldest if queue is full
                if (_eventQueue.Count >= MAX_QUEUE_SIZE)
                {
                    _eventQueue.Dequeue();
                    SDKLogger.LogWarning("[Analytics] Queue full, dropping oldest event");
                }
                _eventQueue.Enqueue(evt);
            }
        }

        private List<AnalyticsEvent> DequeueEvents(int count)
        {
            var events = new List<AnalyticsEvent>();
            lock (_queueLock)
            {
                while (_eventQueue.Count > 0 && events.Count < count)
                {
                    events.Add(_eventQueue.Dequeue());
                }
            }
            return events;
        }

        private void RequeueEvents(List<AnalyticsEvent> events)
        {
            lock (_queueLock)
            {
                // Re-add events to front of queue (reversed to maintain order)
                var tempList = new List<AnalyticsEvent>(_eventQueue);
                _eventQueue.Clear();

                foreach (var evt in events)
                {
                    if (_eventQueue.Count < MAX_QUEUE_SIZE)
                    {
                        _eventQueue.Enqueue(evt);
                    }
                }

                foreach (var evt in tempList)
                {
                    if (_eventQueue.Count < MAX_QUEUE_SIZE)
                    {
                        _eventQueue.Enqueue(evt);
                    }
                }
            }
        }

        #endregion

        #region Flush Logic

        private IEnumerator PeriodicFlush()
        {
            while (_enabled)
            {
                yield return new WaitForSeconds(FLUSH_INTERVAL_SECONDS);
                yield return FlushEvents();
            }
        }

        public void FlushAsync()
        {
            if (_coroutineRunner != null && _enabled)
            {
                _coroutineRunner.StartCoroutine(FlushEvents());
            }
        }

        private IEnumerator FlushEvents()
        {
            if (!_enabled || !_initialized)
            {
                yield break;
            }

            var events = DequeueEvents(BATCH_SIZE);

            if (events.Count == 0)
            {
                yield break;
            }

            string url = $"{_baseUrl}/api/analytics/events/batch";

            // Use custom JSON serialization for Dictionary support
            string json = SerializeBatchRequest(events);

            using (var webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("X-API-Key", _apiKey);
                webRequest.SetRequestHeader("X-Game-Id", _gameId);
                webRequest.timeout = REQUEST_TIMEOUT_SECONDS;

                yield return webRequest.SendWebRequest();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    // Re-queue events on failure
                    RequeueEvents(events);
                    SDKLogger.LogWarning($"[Analytics] Flush failed: {webRequest.error}");
                }
                else
                {
                    SDKLogger.Log($"[Analytics] Flushed {events.Count} events");
                }
            }
        }

        #endregion

        #region Helper Methods

        private bool IsHighPriorityEvent(string eventName)
        {
            return eventName.EndsWith("_failed") ||
                   eventName == "sdk_initialized" ||
                   eventName == "wallet_connected" ||
                   eventName == "tournament_registered" ||
                   eventName == "score_submitted";
        }

        private string GetPlatform()
        {
#if UNITY_ANDROID
            return "Android";
#elif UNITY_IOS
            return "iOS";
#elif UNITY_WEBGL
            return "WebGL";
#elif UNITY_STANDALONE_WIN
            return "Windows";
#elif UNITY_STANDALONE_OSX
            return "macOS";
#elif UNITY_STANDALONE_LINUX
            return "Linux";
#elif UNITY_EDITOR
            return "Editor";
#else
            return "Unknown";
#endif
        }

        private string GenerateSessionId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 16);
        }

        public void SetWalletAddress(string address)
        {
            // Truncate for privacy
            if (!string.IsNullOrEmpty(address) && address.Length > 8)
            {
                _walletAddress = $"{address.Substring(0, 4)}...{address.Substring(address.Length - 4)}";
            }
            else
            {
                _walletAddress = address;
            }
        }

        public void SetTokenType(TokenType tokenType)
        {
            _defaultTokenType = tokenType.ToString();
        }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;

            if (_enabled && _flushCoroutine == null && _coroutineRunner != null)
            {
                _flushCoroutine = _coroutineRunner.StartCoroutine(PeriodicFlush());
            }
            else if (!_enabled && _flushCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_flushCoroutine);
                _flushCoroutine = null;
            }

            SDKLogger.Log($"[Analytics] Enabled: {_enabled}");
        }

        #endregion

        #region JSON Serialization

        private string SerializeBatchRequest(List<AnalyticsEvent> events)
        {
            var sb = new StringBuilder();
            sb.Append("{\"events\":[");

            for (int i = 0; i < events.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(SerializeEvent(events[i]));
            }

            sb.Append("]}");
            return sb.ToString();
        }

        private string SerializeEvent(AnalyticsEvent evt)
        {
            var sb = new StringBuilder();
            sb.Append("{");

            sb.Append($"\"eventName\":\"{EscapeJson(evt.eventName)}\"");
            sb.Append($",\"sdkVersion\":\"{EscapeJson(evt.sdkVersion)}\"");
            sb.Append($",\"unityVersion\":\"{EscapeJson(evt.unityVersion)}\"");
            sb.Append($",\"platform\":\"{EscapeJson(evt.platform)}\"");
            sb.Append($",\"osVersion\":\"{EscapeJson(evt.osVersion)}\"");
            sb.Append($",\"environment\":\"{EscapeJson(evt.environment)}\"");
            sb.Append($",\"tokenType\":\"{EscapeJson(evt.tokenType)}\"");
            sb.Append($",\"sessionId\":\"{EscapeJson(evt.sessionId)}\"");

            if (!string.IsNullOrEmpty(evt.walletAddress))
            {
                sb.Append($",\"walletAddress\":\"{EscapeJson(evt.walletAddress)}\"");
            }

            sb.Append($",\"clientTimestamp\":{evt.clientTimestamp}");

            // Serialize eventData dictionary
            if (evt.eventData != null && evt.eventData.Count > 0)
            {
                sb.Append(",\"eventData\":{");
                bool first = true;
                foreach (var kvp in evt.eventData)
                {
                    if (!first) sb.Append(",");
                    first = false;

                    if (kvp.Value is string strVal)
                    {
                        sb.Append($"\"{EscapeJson(kvp.Key)}\":\"{EscapeJson(strVal)}\"");
                    }
                    else if (kvp.Value is int || kvp.Value is long || kvp.Value is float || kvp.Value is double)
                    {
                        sb.Append($"\"{EscapeJson(kvp.Key)}\":{kvp.Value}");
                    }
                    else if (kvp.Value is bool boolVal)
                    {
                        sb.Append($"\"{EscapeJson(kvp.Key)}\":{(boolVal ? "true" : "false")}");
                    }
                    else
                    {
                        sb.Append($"\"{EscapeJson(kvp.Key)}\":\"{EscapeJson(kvp.Value?.ToString() ?? "")}\"");
                    }
                }
                sb.Append("}");
            }

            sb.Append("}");
            return sb.ToString();
        }

        private string EscapeJson(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        #endregion

        #region Application Lifecycle

        public void OnApplicationQuit()
        {
            // Flush remaining events synchronously
            int queueCount = 0;
            lock (_queueLock)
            {
                queueCount = _eventQueue.Count;
            }

            if (queueCount > 0)
            {
                SDKLogger.Log($"[Analytics] Flushing {queueCount} events on quit");
                FlushAsync();
            }
        }

        public void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                int queueCount = 0;
                lock (_queueLock)
                {
                    queueCount = _eventQueue.Count;
                }

                if (queueCount > 0)
                {
                    SDKLogger.Log($"[Analytics] Flushing {queueCount} events on pause");
                    FlushAsync();
                }
            }
        }

        #endregion
    }
}

