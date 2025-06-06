### v4.0.1

- Cancel updates checking when closing Settings panel.
- Fix crashing due to negative prayer time adjustment.
- Fix volume decrease after playing an audio the first time. (Deskband only)
- Enable caching the content of the pages in **More info** window. (WinUI only)
- Implement workarounds to the issues caused by Start11 v2. (WinUI only)

### v4.0

- Add an offline service to calculate prayer times.
- Add adhan.
- Add per-time settings.
- Add prayers calendar which can be printed or exported as a PNG image.
- Add an informational page to learn how prayers times are determined.
- Add logging. (Disabled by default)
- Rename **Islamic Finder** service to **Salah Hour**.
- Adapt to taskbar color when it uses accent color.
- Avoid infecting current settings while making changes in Settings panel.
- Allow the user to retry injecting the widget if that failed. (WinUI only)
- Improve highlighted text color for the light theme.
- Make the widget close itself during update/uninstall and restart after update. (WinUI only)
- Improve interaction with `UIAutomation`. (WinUI only)
- Enable animations in Settings panel. (WinUI only)
- Improve UX of radio button. (Deskband only)
- Fix translation issue in dropdowns. (WinUI only)
- Fix updates detection from Microsoft Store. (WinUI only)

### v3.3.1

- Fix crashing after failing to inject the widget into the taskbar. (WinUI only)
- Fix bug of wrong detection of Windows display language. (WinUI only)

### v3.3

- Make the widget available on Microsoft Store. (WinUI only)
- Allow dragging the widget to custom position. (WinUI only)
- Add a setting to control Hijri date.
- Add updates checking in About page.
- Enable Acrylic background. (WinUI only)
- Improve widget positioning. (WinUI only)
- Fix navigation using Tab key. (Deskband only)
- Make MessageBox dialogs respect current language direction.
- Show a hint when the current service is down to suggest a potential solution.
- Improve location search using Nominatim.
- Various bug-fixes.

### v3.2

- Add **Compact mode**.
- Show Shuruq time.
- Add option for showing elapsed time.
- Add option for playing sound during notification period.
- Make widget's pane on the taskbar display the texts correctly.
- Improve widget positioning. (WinUI only)
- Fix the issue of time jumps when Windows wake up from sleep/hibernation.
- Minor bug-fixes.

### v3.1

- Add the option of selecting juristic school for Asr prayer time.
- Display seconds in countdown.
- Make time format respect system configuration (12h or 24h).
- Unify calculation methods.
- Redesign the Settings panel.
- Add About tab in Settings panel.
- Hide Gregorian date in main panel.
- Avoid overlapping other widgets injected into the taskbar. (WinUI only)
- Make context-menu and system tray icon menu respect language direction. (WinUI only)
- Make widget's pane on the taskbar respects DPI scale. (WinUI only)
- Fix widget's vertical position for 2-in-1 devices. (WinUI only)
- Minor bug-fixes.

### v3.0

- Add Awqat Salaat WinUI app to support Windows 11.
- Fix a critical error in date serialization on systems that use different calendars other than Gregorian.
- Fix settings loss after the installation of Windows Updates (such as cumulative updates).
- Show Hijri date in English when Display language is not set to Arabic.

### v2.0.1

- Fix a typo in **Fajr** prayer English name.
- Stabilize version numbers calculation

### v2.0

- Add Al-Adhan's Prayer Times API.

### v1.1.3

- Show clearer error message when Islamic Finder's Prayer Times API is down.

### v1.1

- Fix compatibility issues with Windows 7.

### v1.0

- Initial release.