# UI Components Sample

Ready-to-use UI components for displaying tournaments in your Unity game.

## Components

### TournamentCard
A reusable UI component that displays tournament information in a card format:
- Tournament title, description, and status
- Entry fee and participant count
- Real-time countdown timer
- Token type indicator (SOL/SPL badge)
- Participate button with state management
- Automatic button state updates (available, registering, registered, ended)

### TournamentsPage
A complete tournaments display page with:
- Horizontally scrollable tournament cards
- Automatic fetching of both SPL and SOL tournaments
- Tournament registration flow integration
- Status messages and error handling
- Back button navigation

## Usage

1. **Add DeepLinkReceiver to your scene:**
   ```csharp
   // In your game manager or initialization script
   if (FindFirstObjectByType<Multiversed.Utils.DeepLinkReceiver>() == null)
   {
       GameObject receiver = new GameObject("DeepLinkReceiver");
       receiver.AddComponent<Multiversed.Utils.DeepLinkReceiver>();
   }
   ```

2. **Create TournamentsPage:**
   ```csharp
   // Create a GameObject and add TournamentsPage component
   GameObject tournamentsPageObj = new GameObject("TournamentsPage");
   TournamentsPage tournamentsPage = tournamentsPageObj.AddComponent<TournamentsPage>();
   
   // Set up panels (create these in your UI)
   tournamentsPage.mainMenuPanel = yourMainMenuPanel;
   tournamentsPage.tournamentsPanel = yourTournamentsPanel;
   
   // Show tournaments
   tournamentsPage.Show();
   ```

3. **Customize:**
   - Modify colors, sizes, and layouts in `CreateCardUI()` and `BuildUI()`
   - Adjust card dimensions via `CARD_WIDTH`, `CARD_HEIGHT`, `CARD_SPACING` constants
   - Customize fonts and styling to match your game's theme

## Requirements

- Unity 2020.3 LTS or higher
- Multiversed SDK initialized
- Wallet connected before registering for tournaments

## Notes

- Components create UI programmatically (no prefabs needed)
- Uses Unity's built-in UI system (uGUI)
- Automatically handles both SPL and SOL tournaments
- Deep link handling is automatic via DeepLinkReceiver

