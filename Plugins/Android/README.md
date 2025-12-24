# Android Configuration

## AndroidManifest.xml Setup

This folder contains a template `AndroidManifest.xml` for deep link support.

### Setup Instructions

1. **Copy the manifest to your project:**
   - Copy `AndroidManifest.xml` to `Assets/Plugins/Android/AndroidManifest.xml` in your Unity project
   - Unity will merge this with the default manifest during build

2. **Configure your deep link scheme:**
   - Open `AndroidManifest.xml`
   - Find all instances of `multiversed-XXXXXXXX`
   - Replace `XXXXXXXX` with the first 8 characters of your `gameId`
   - Example: If your `gameId` is `"df62e4e4-3b47-4d02-8be8-3cddc9fa12ee"`, use `"multiversed-df62e4e4"`

3. **Verify the scheme matches SDK initialization:**
   - The scheme in the manifest must match what the SDK generates
   - SDK automatically generates: `multiversed-{first8CharsOfGameId}`
   - Or use `SDKConfig.CustomUrlScheme` to set a custom scheme

### Example

If your gameId is `"abc12345-6789-0123-4567-890123456789"`:
- Deep link scheme: `multiversed-abc12345`
- Update both `<data android:scheme="..." />` entries in the manifest

### Important Notes

- The manifest must be in `Assets/Plugins/Android/` folder
- Unity merges this with the default manifest during build
- Deep links are required for Phantom Wallet callbacks
- Without proper manifest configuration, wallet callbacks will not work

