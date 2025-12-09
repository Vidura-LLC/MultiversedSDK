🎮 MULTIVERSED UNITY SDK - INITIAL RELEASE

A comprehensive Unity SDK for integrating blockchain-powered tournaments,
wallet connections, and leaderboards into Unity games.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✨ FEATURES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔐 Authentication

Game ID + API Key authentication
Automatic credential validation on initialization
Secure header injection for all API calls
🌐 Environment Support

Devnet environment for testing
Mainnet environment for production
Custom API URL support for local development
💰 Dual Token Support

SPL token (YIP) tournaments
SOL token tournaments
Configurable default token type
👛 Wallet Integration

Phantom wallet connection via deep links
Automatic session persistence (PlayerPrefs)
Transaction signing flow
Wallet disconnect functionality
🏆 Tournament Features

List all tournaments for a game
Get tournament details by ID
Register for tournaments (with wallet signing)
Tournament status checking (Active, Ended, etc.)
📊 Leaderboard

Fetch tournament leaderboards
Player rankings with scores
Wallet address display helpers
🎯 Score Submission

Submit player scores to tournaments
Automatic wallet authentication
🛠️ Developer Tools

Unity Editor settings window (Window > Multiversed > SDK Settings)
Code generation for initialization
Quick links to dashboard and documentation
Debug logging toggle
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📖 USAGE EXAMPLE
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// Initialize SDK
var config = new SDKConfig
{
Environment = SDKEnvironment.Devnet,
DefaultTokenType = TokenType.SPL,
EnableLogging = true,
CustomApiUrl = "http://localhost:5000\" // For local dev
};

MultiversedSDK.Instance.Initialize(gameId, apiKey, config);

// Subscribe to events
MultiversedSDK.Instance.OnInitialized += () => Debug.Log("Ready!");
MultiversedSDK.Instance.OnWalletConnected += (session) =>
Debug.Log("Wallet: " + session.WalletAddress);

// Connect wallet
MultiversedSDK.Instance.ConnectWallet();

// Get tournaments
MultiversedSDK.Instance.GetTournaments(
onSuccess: (tournaments) => { /* handle / },
onError: (error) => { / handle */ }
);

// Register for tournament
MultiversedSDK.Instance.RegisterForTournament(tournamentId,
onSuccess: (signature) => { /* handle / },
onError: (error) => { / handle */ }
);

// Submit score
MultiversedSDK.Instance.SubmitScore(tournamentId, score,
onSuccess: () => { /* handle / },
onError: (error) => { / handle */ }
);

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔧 CONFIGURATION OPTIONS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

SDKConfig:

Environment: Devnet | Mainnet
DefaultTokenType: SPL | SOL
CustomApiUrl: string (for local development)
CustomUrlScheme: string (for deep link callbacks)
EnableLogging: bool
RequestTimeoutSeconds: int (default: 30)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 API ENDPOINTS SUPPORTED
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

POST /api/sdk/verify - Verify SDK credentials
GET /api/sdk/tournaments - List tournaments
GET /api/sdk/tournaments/:id - Get tournament details
POST /api/sdk/tournaments/prepare-registration - Prepare registration tx
POST /api/sdk/tournaments/confirm-registration - Confirm registration
GET /api/sdk/tournaments/:id/leaderboard - Get leaderboard
POST /api/sdk/tournaments/:id/score - Submit score

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📱 PLATFORM SUPPORT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Unity 2020.3 LTS and higher
Android (with Phantom wallet deep links)
iOS (with Phantom wallet deep links)
Standalone (limited wallet support)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🧪 TESTED
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ SDK initialization with credentials
✅ Custom API URL for local development
✅ Credential verification via API
✅ Tournament listing (empty and populated)
✅ Unity 6.0 compatibility
✅ Assembly definition compilation
✅ Package Manager import

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📝 NOTES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Renamed Logger to SDKLogger to avoid Unity namespace conflict
Tournament response uses 'data' field (not 'tournaments')
Deep link scheme format: multiversed-{first8CharsOfGameId}
Wallet sessions persist across app restarts via PlayerPrefs
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🔜 NEXT STEPS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Test wallet connection on Android device
Test full tournament registration flow
Add Android deep link manifest configuration
Add iOS URL scheme configuration
Create documentation website
Publish to Unity Asset Store (optional)
