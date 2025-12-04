using UnityEngine;

namespace Multiversed.Utils
{
    /// <summary>
    /// JSON serialization helper using Unity's JsonUtility
    /// </summary>
    public static class JsonHelper
    {
        /// <summary>
        /// Deserialize JSON string to object
        /// </summary>
        public static T FromJson<T>(string json)
        {
            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (System.Exception e)
            {
                Logger.LogError($"JSON parse error: {e.Message}");
                return default;
            }
        }

        /// <summary>
        /// Serialize object to JSON string
        /// </summary>
        public static string ToJson<T>(T obj, bool prettyPrint = false)
        {
            try
            {
                return JsonUtility.ToJson(obj, prettyPrint);
            }
            catch (System.Exception e)
            {
                Logger.LogError($"JSON serialize error: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserialize JSON array (Unity's JsonUtility doesn't support arrays directly)
        /// </summary>
        public static T[] FromJsonArray<T>(string json)
        {
            string wrappedJson = $"{{\"items\":{json}}}";
            var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrappedJson);
            return wrapper?.items;
        }

        [System.Serializable]
        private class JsonArrayWrapper<T>
        {
            public T[] items;
        }
    }
}