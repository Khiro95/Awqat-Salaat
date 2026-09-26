# Tray icon setting checks

Run the settings tests from the repository root:

```powershell
dotnet test tests/AwqatSalaat.Settings.Tests/AwqatSalaat.Settings.Tests.csproj -c Release
```

These tests use the test host's settings store, separate from the application. They cover the visible default, preview and Cancel, Save and reload, and labels and help text in the four supported languages.

## Windows runtime checklist

Use the WinUI Release build for these checks. The existing Debug build uses a fixed date and resets configuration, so it is unsuitable for verifying the running prayer countdown.

1. Open Settings > General. Confirm **Show system tray icon** starts enabled for a new settings profile.
2. Turn it off. Confirm the icon disappears from the notification area and its overflow while the taskbar widget and countdown continue working.
3. Cancel with the cross button. Confirm the icon returns. Turn it off again and Save with the checkmark.
4. Restart the app. Confirm the saved preference is applied. Preview turning it on, then Cancel. Confirm the saved hidden preference returns.
5. With the icon hidden, verify reminder and prayer-time notifications and sound playback.
6. Hide the widget. Confirm the tray icon becomes available so Show and Quit remain reachable. Show the widget again and confirm the saved icon preference returns.
7. Restart Windows Explorer. Confirm the preference is retained when the widget recovers. If the existing recovery leaves an unusable widget, the tray must remain accessible for Quit and relaunch. Repeat with the widget deliberately hidden and confirm its tray icon remains available.
8. If no display is available, confirm the tray icon stays visible and Show remains enabled. After reconnecting a display, use Show to reopen the widget. Display recovery otherwise follows the existing application behavior.
9. Quit with each icon preference and confirm the app exits without leaving a tray icon. Check normal Windows sign-out/shutdown separately.

10. Confirm the tray setting is next to the lock-screen setting in General. Check the Arabic label uses "درج النظام" and the help uses "الأداة".

The icon is hidden using the existing H.NotifyIcon visibility API. Its message window remains alive for Explorer restart and session-ending events. No prayer, notification, or audio services are disabled by the setting.
