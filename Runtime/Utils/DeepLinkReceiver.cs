using UnityEngine;
using Multiversed;

namespace Multiversed.Utils
{
    /// <summary>
    /// Handles deep link callbacks from external apps (like Phantom wallet)
    /// Automatically forwards deep links to MultiversedSDK
    /// </summary>
    public class DeepLinkReceiver : MonoBehaviour
    {
        private static DeepLinkReceiver _instance;
        
        public static string LastDeepLink { get; private set; }
        
        public delegate void DeepLinkHandler(string url);
        public static event DeepLinkHandler DeepLinkReceived;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.name = "DeepLinkReceiver";
            
            SDKLogger.Log("[DeepLinkReceiver] Initialized");
        }

        void Start()
        {
            CheckInitialDeepLink();
        }

        void CheckInitialDeepLink()
        {
            // Check Unity's built-in URL
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                SDKLogger.Log("[DeepLinkReceiver] Found launch URL: " + Application.absoluteURL);
                if (Application.absoluteURL.Contains("multiversed-"))
                {
                    ProcessUrl(Application.absoluteURL);
                }
            }

            // Check Android intent
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject intent = activity.Call<AndroidJavaObject>("getIntent"))
                {
                    if (intent != null)
                    {
                        using (AndroidJavaObject uri = intent.Call<AndroidJavaObject>("getData"))
                        {
                            if (uri != null)
                            {
                                string url = uri.Call<string>("toString");
                                SDKLogger.Log("[DeepLinkReceiver] Found intent URL: " + url);
                                if (!string.IsNullOrEmpty(url) && url.Contains("multiversed-"))
                                {
                                    ProcessUrl(url);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                SDKLogger.LogError("[DeepLinkReceiver] Error checking intent: " + e.Message);
            }
            #endif
        }

        // Called from Android native code via UnitySendMessage
        public void OnDeepLinkReceived(string url)
        {
            SDKLogger.Log("[DeepLinkReceiver] Received from native: " + url);
            ProcessUrl(url);
        }

        void ProcessUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            
            LastDeepLink = url;
            SDKLogger.Log("[DeepLinkReceiver] Processing: " + url);
            
            // Notify listeners
            DeepLinkReceived?.Invoke(url);
            
            // Also forward to SDK directly
            if (MultiversedSDK.Instance != null)
            {
                MultiversedSDK.Instance.HandleDeepLink(url);
            }
        }
    }
}

