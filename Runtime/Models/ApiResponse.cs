// File: Runtime/Models/ApiResponse.cs
namespace Multiversed.Models
{
    /// <summary>
    /// Generic API response wrapper
    /// </summary>
    [System.Serializable]
    public class ApiResponse<T>
    {
        public bool success;
        public string message;
        public T data;
        public string error;
    }

    /// <summary>
    /// Simple API response without data
    /// </summary>
    [System.Serializable]
    public class ApiResponse
    {
        public bool success;
        public string message;
        public string error;
    }

    /// <summary>
    /// Error response from API
    /// </summary>
    [System.Serializable]
    public class ApiError
    {
        public bool success;
        public string error;
        public string message;
    }
}