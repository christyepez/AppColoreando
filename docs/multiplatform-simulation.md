# Multi-Platform Simulation and Web

AppColoreando uses one Flutter codebase for Android, iOS, Web and optional Windows Desktop.

## Recommended local simulation

### Web / PC simulation

This is the default simulation mode on Windows because it requires only Flutter and Chrome.

```powershell
.\tools\simulation\run-app.ps1 -Target web
```

The app connects to `http://localhost:8080` by default. Override it with:

```powershell
.\tools\simulation\run-app.ps1 -Target web -ApiBaseUrl http://localhost:8080
```

For a browser-hosted server on a fixed port:

```powershell
.\tools\simulation\run-web.ps1 -Port 8083
```
## Android Emulator

Android Studio is the supported local emulator path on Windows.

Requirements:

- Android Studio + Android SDK.
- Android Emulator.
- One AVD, preferably a recent Pixel profile.
- Hardware virtualization enabled.

After an AVD exists:

```powershell
flutter emulators
flutter emulators --launch <emulator-id>
flutter devices
flutter run -d <device-id> --dart-define=API_BASE_URL=http://10.0.2.2:8080
```

`10.0.2.2` maps the Android emulator back to the Windows host, where the local API listens on port 8080.
## iOS Simulator

The iOS Simulator is available only on macOS through Xcode. It cannot run natively on Windows.

For current iOS releases, use a supported Mac capable of running a current Xcode version. A MacBook Air 2013 is not a supported release machine for current Xcode/iOS SDK versions.

Recommended paths:

- Apple Silicon Mac mini/MacBook Air for local iOS development.
- A managed macOS CI runner for archive/sign/store builds.

## Windows Desktop

The Windows target is generated and shares the same Flutter codebase. Building the native `.exe` additionally requires Developer Mode and Visual Studio C++ desktop build tools.

```powershell
.\tools\simulation\run-app.ps1 -Target windows
```

## Web Release

```powershell
cd apps\mobile
flutter build web --release --dart-define=API_BASE_URL=https://api.example.com
```

Output: `apps/mobile/build/web`.
