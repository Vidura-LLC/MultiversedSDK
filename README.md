# Multiversed SDK for Unity

Integrate Multiversed tournaments, wallet connection, and leaderboards into your Unity game.

## Quick Start

1. **Import the SDK package** into your Unity project
2. **Initialize the SDK** in your game:
   ```csharp
   MultiversedSDK.Instance.Initialize("your-game-id", "your-api-key");
   ```
3. **Connect wallet**:
   ```csharp
   MultiversedSDK.Instance.ConnectWallet(
       onSuccess: (walletAddress) => Debug.Log("Connected: " + walletAddress),
       onError: (error) => Debug.LogError("Connection failed: " + error)
   );
   ```
4. **Use UI Components** (optional):
   - Import the "UI Components" sample from Package Manager
   - Add `TournamentsPage` to your scene
   - Customize to match your game's theme

## Features

- ✅ **Wallet Connection**: Connect with Phantom Wallet via deep links
- ✅ **Tournament Management**: Fetch, display, and register for tournaments
- ✅ **SPL & SOL Support**: Support for both SPL tokens and SOL tournaments
- ✅ **Leaderboards**: Get tournament leaderboards and player scores
- ✅ **Ready-to-Use UI**: Pre-built tournament card and page components
- ✅ **Automatic Deep Links**: Deep link handling is automatic

## Samples

The SDK includes two samples:

1. **Basic Integration**: Simple example showing SDK initialization and basic usage
2. **UI Components**: Ready-to-use `TournamentCard` and `TournamentsPage` components

Import samples via Unity Package Manager → Multiversed SDK → Samples

## Android Setup

For Android builds, you need to configure deep links:

1. Copy `Plugins/Android/AndroidManifest.xml` to `Assets/Plugins/Android/` in your Unity project
2. Replace `multiversed-XXXXXXXX` with your actual deep link scheme (first 8 chars of your gameId)
3. Example: If your gameId is `"df62e4e4-3b47-4d02-8be8-3cddc9fa12ee"`, use `"multiversed-df62e4e4"`

See `Plugins/Android/README.md` for detailed instructions.

## Documentation

- [UI Components Sample](Samples~/UI%20Components/README.md) - Ready-to-use tournament UI
- [Android Configuration](Plugins/Android/README.md) - Deep link setup for Android

## Requirements

- Unity 2020.3 LTS or higher
- Android/iOS build support (for mobile wallet integration)
- Internet connection for API calls

## Support

For issues, questions, or feature requests, contact support@multiversed.io
