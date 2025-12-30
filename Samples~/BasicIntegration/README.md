# Multiversed SDK - Basic Integration Sample

This sample demonstrates how to integrate the Multiversed SDK into your Unity game.

## Setup

1. Open the sample scene or add `SampleGameManager` to your scene
2. Enter your **Game ID** and **API Key** from the Multiversed Developer Dashboard
3. Configure the environment (Devnet for testing, Mainnet for production)

## Getting Your Credentials

1. Go to [Multiversed Developer Dashboard](https://dashboard.multiversed.io)
2. Create a new game or select an existing one
3. Navigate to **SDK Settings**
4. Copy your **Game ID** and **API Key**

## Quick Start

### 1. Initialize the SDK
```csharp
using Multiversed;
using Multiversed.Core;

void Start()
{
    var config = new SDKConfig
    {
        Environment = SDKEnvironment.Devnet,
        DefaultTokenType = TokenType.SPL,
        EnableLogging = true
    };

    MultiversedSDK.Instance.Initialize("YOUR_GAME_ID", "YOUR_API_KEY", config);
    MultiversedSDK.Instance.OnInitialized += () => Debug.Log("SDK Ready!");
}
```

### 2. Connect Wallet
```csharp
public void OnConnectWalletButtonClicked()
{
    MultiversedSDK.Instance.ConnectWallet();
}

// Handle the callback
MultiversedSDK.Instance.OnWalletConnected += (session) =>
{
    Debug.Log($"Connected: {session.WalletAddress}");
};
```

### 3. Get Tournaments
```csharp
MultiversedSDK.Instance.GetTournaments(
    onSuccess: (tournaments) =>
    {
        foreach (var t in tournaments)
        {
            Debug.Log($"{t.name} - Entry: {t.entryFee}");
        }
    },
    onError: (error) => Debug.LogError(error)
);
```

### 4. Register for Tournament
```csharp
MultiversedSDK.Instance.RegisterForTournament(
    tournamentId,
    onSuccess: (signature) => Debug.Log("Registered!"),
    onError: (error) => Debug.LogError(error)
);
```

### 5. Submit Score
```csharp
MultiversedSDK.Instance.SubmitScore(
    tournamentId,
    score: 1000,
    onSuccess: () => Debug.Log("Score submitted!"),
    onError: (error) => Debug.LogError(error)
);
```

### 6. Get Leaderboard
```csharp
MultiversedSDK.Instance.GetLeaderboard(
    tournamentId,
    onSuccess: (entries) =>
    {
        foreach (var entry in entries)
        {
            Debug.Log($"#{entry.rank}: {entry.score}");
        }
    },
    onError: (error) => Debug.LogError(error)
);
```

## Deep Link Setup

For wallet callbacks to work, you need to configure deep links in your app.

### Android

Add to your `AndroidManifest.xml`:
```xml
<activity android:name="com.unity3d.player.UnityPlayerActivity">
    <intent-filter>
        <action android:name="android.intent.action.VIEW" />
        <category android:name="android.intent.category.DEFAULT" />
        <category android:name="android.intent.category.BROWSABLE" />
        <data android:scheme="multiversed-YOUR_GAME_ID" />
    </intent-filter>
</activity>
```

### iOS

Add to your `Info.plist`:
```xml
<key>CFBundleURLTypes</key>
<array>
    <dict>
        <key>CFBundleURLSchemes</key>
        <array>
            <string>multiversed-YOUR_GAME_ID</string>
        </array>
    </dict>
</array>
```

## Testing

1. Use **Devnet** environment for testing
2. Get test SOL from [Solana Faucet](https://faucet.solana.com)
3. Install Phantom wallet on your test device
4. Create test tournaments in the Multiversed Dashboard

## Troubleshooting

### SDK not initializing
- Check your Game ID and API Key
- Ensure you're using the correct environment
- Check the console for error messages

### Wallet not connecting
- Verify Phantom is installed on the device
- Check your deep link configuration
- Test on a real device (not simulator)

### Transactions failing
- Ensure you have enough SOL/tokens for fees
- Check if the tournament is still open
- Verify you're on the correct network (devnet/mainnet)

## Support

- [Documentation](https://docs.multiversed.io/unity-sdk)
- [Discord](https://discord.gg/multiversed)
- [GitHub Issues](https://github.com/multiversed/unity-sdk/issues)