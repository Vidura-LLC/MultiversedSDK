// File: Runtime/Utils/DeepLinkReceiver.cs
using UnityEngine;
using System.Collections;
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

        void OnApplicationPause(bool paused)
        {
            if (!paused)
            {
                // App resumed - check for deep link immediately and multiple times
                SDKLogger.Log("[DeepLinkReceiver] App resumed from pause - checking for deep link");
                StartCoroutine(CheckDeepLinkOnResume());
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                // App gained focus - check for deep link immediately and multiple times
                SDKLogger.Log("[DeepLinkReceiver] App gained focus - checking for deep link");
                StartCoroutine(CheckDeepLinkOnResume());
            }
        }

        System.Collections.IEnumerator CheckDeepLinkOnResume()
        {
            // Check immediately first (intent might already be ready)
            CheckForDeepLink();
            
            // Then check again after a short delay (in case intent takes time)
            yield return new WaitForSeconds(0.3f);
            CheckForDeepLink();
            
            // Final check after a bit more delay (some devices are slower)
            yield return new WaitForSeconds(0.5f);
            CheckForDeepLink();
        }

        void CheckInitialDeepLink()
        {
            CheckForDeepLink();
        }

        void CheckForDeepLink()
        {
            SDKLogger.Log("[DeepLinkReceiver] Checking for deep link...");
            
            // Check Unity's built-in URL
            string absoluteUrl = Application.absoluteURL;
            if (!string.IsNullOrEmpty(absoluteUrl))
            {
                SDKLogger.Log("[DeepLinkReceiver] Application.absoluteURL: " + absoluteUrl);
                if (absoluteUrl.Contains("multiversed-"))
                {
                    SDKLogger.Log("[DeepLinkReceiver] Found deep link in absoluteURL!");
                    ProcessUrl(absoluteUrl);
                    return;
                }
            }
            else
            {
                SDKLogger.Log("[DeepLinkReceiver] Application.absoluteURL is empty");
            }

            // Check Android intent
            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity != null)
                    {
                        using (AndroidJavaObject intent = activity.Call<AndroidJavaObject>("getIntent"))
                        {
                            if (intent != null)
                            {
                                string action = intent.Call<string>("getAction");
                                SDKLogger.Log("[DeepLinkReceiver] Intent action: " + (action ?? "null"));
                                
                                if (action == "android.intent.action.VIEW" || action == "android.intent.action.MAIN")
                                {
                                    using (AndroidJavaObject uri = intent.Call<AndroidJavaObject>("getData"))
                                    {
                                        if (uri != null)
                                        {
                                            string url = uri.Call<string>("toString");
                                            SDKLogger.Log("[DeepLinkReceiver] Intent data URI: " + url);
                                            if (!string.IsNullOrEmpty(url) && url.Contains("multiversed-"))
                                            {
                                                SDKLogger.Log("[DeepLinkReceiver] Found deep link in intent!");
                                                ProcessUrl(url);
                                                return;
                                            }
                                        }
                                        else
                                        {
                                            SDKLogger.Log("[DeepLinkReceiver] Intent data URI is null");
                                        }
                                    }
                                }
                                
                                // Also try getStringExtra for deep link data
                                try
                                {
                                    string extraData = intent.Call<string>("getStringExtra", "android.intent.extra.TEXT");
                                    if (!string.IsNullOrEmpty(extraData))
                                    {
                                        SDKLogger.Log("[DeepLinkReceiver] Intent extra data: " + extraData);
                                        if (extraData.Contains("multiversed-"))
                                        {
                                            SDKLogger.Log("[DeepLinkReceiver] Found deep link in intent extra!");
                                            ProcessUrl(extraData);
                                            return;
                                        }
                                    }
                                }
                                catch (System.Exception e)
                                {
                                    SDKLogger.Log("[DeepLinkReceiver] No extra data in intent: " + e.Message);
                                }
                            }
                            else
                            {
                                SDKLogger.Log("[DeepLinkReceiver] Intent is null");
                            }
                        }
                    }
                    else
                    {
                        SDKLogger.Log("[DeepLinkReceiver] Activity is null");
                    }
                }
            }
            catch (System.Exception e)
            {
                SDKLogger.LogError("[DeepLinkReceiver] Error checking intent: " + e.Message);
            }
            #endif
            
            SDKLogger.Log("[DeepLinkReceiver] No deep link found");
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

        /// <summary>
        /// Manually trigger a deep link check (useful for testing or manual retry)
        /// </summary>
        public static void CheckNow()
        {
            if (_instance != null)
            {
                _instance.CheckForDeepLink();
            }
            else
            {
                SDKLogger.LogWarning("[DeepLinkReceiver] Instance not available for manual check");
            }
        }
    }
}

