namespace Multiversed.Utils
{
    /// <summary>
    /// Internal logger for SDK debugging
    /// </summary>
    public static class Logger
    {
        private const string TAG = "[Multiversed]";

        public static bool EnableLogging { get; set; } = true;

        public static void Log(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (EnableLogging)
            {
                UnityEngine.Debug.Log($"{TAG} {message}");
            }
#endif
        }

        public static void LogWarning(string message)
        {
            if (EnableLogging)
            {
                UnityEngine.Debug.LogWarning($"{TAG} {message}");
            }
        }

        public static void LogError(string message)
        {
            UnityEngine.Debug.LogError($"{TAG} {message}");
        }

        public static void LogException(System.Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
        }
    }
}