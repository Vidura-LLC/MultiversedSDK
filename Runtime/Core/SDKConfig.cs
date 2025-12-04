namespace Multiversed.Core
{
    /// <summary>
    /// SDK Configuration settings
    /// </summary>
    [System.Serializable]
    public class SDKConfig
    {
        /// <summary>
        /// Environment for API calls
        /// </summary>
        public SDKEnvironment Environment { get; set; } = SDKEnvironment.Devnet;

        /// <summary>
        /// Custom URL scheme for deep link callbacks
        /// If null, defaults to "multiversed-{gameId}"
        /// </summary>
        public string CustomUrlScheme { get; set; } = null;

        /// <summary>
        /// Token type for tournaments (SOL or SPL)
        /// </summary>
        public TokenType DefaultTokenType { get; set; } = TokenType.SPL;

        /// <summary>
        /// Enable debug logging
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int RequestTimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// SDK Environment (Devnet or Mainnet)
    /// </summary>
    public enum SDKEnvironment
    {
        Devnet = 0,
        Mainnet = 1
    }

    /// <summary>
    /// Token type for transactions
    /// </summary>
    public enum TokenType
    {
        SPL = 0,
        SOL = 1
    }
}