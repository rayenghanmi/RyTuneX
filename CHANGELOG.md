# CHANGELOG.md

All notable changes to this branch will be documented in this file.


## 1.6.1 - Unreleased

### Added

- Added more Win32 apps to the debloat list by expanding the detection methods.
- Added user profile header to the navigation view with account type display and quick access to account settings.

### Fixes

- Fixed an issue where navigating to any page other than `Settings` would cause the navigation item to stuck for a few seconds till the page finishes loading.

### Changes

- Reduced the msix package size by ~45% by removing unnecessary dependencies and enabling trimming.
- Upgraded the app's target framework to .NET 10.
- Enhanced the update process to improve reliability and performance.
- Improved the system info fetching process for better accuracy.
- Optimized the icon extraction process for more app icons to be displayed correctly.
- Replaced the old driver extraction method with dism.exe for better compatibility.
- Redesigned the logo and app icon for a refreshed look.

## 1.6.0 - Released

### Added

- Added Battery & Power section in the `Optimize` page for better power management (suggested by @abiabi0707 in #79):
  - `Power Mode`
  - `Add Ultimate Performance Plan`
- Added a new `Policies` page for detecting and managing Local Group Policy overrides (suggested by @abiabi0707 in #79):
  - Scans for configured Group Policy overrides across multiple categories (Windows Update, Privacy & Telemetry, Cortana & Search, Windows Store, OneDrive, Security, etc.).
  - Displays a summary of configured policies by category.
  - Allows removal of individual policy overrides, by category, or all at once.
  - Returns removed policies to their default "Not Configured" state.
  - Supports Windows version-specific policies (e.g., Windows Copilot, Windows Recall).
- Added a new Services section in the `Home` page for managing Windows services (suggested by @abiabi0707 in #79):
  - Displays a list of Windows services with their current status (Running/Stopped).
  - Allows users to start, stop, or restart individual services.
  - Provides options to set the startup type of services (Automatic, Manual, Disabled).
  - Includes a search bar for quickly finding specific services.
- Added a new Processes section in the `Home` page for managing running processes (suggested by @abiabi0707 in #79):
  - Displays a list of currently running processes with details (CPU, Memory usage).
  - Allows users to end individual processes.
  - Includes a search bar for quickly finding specific processes.

### Fixes

- Fixed an issue where the export settings feature did not work #80.
- Resolved an issue where the sfc repairing method was failing to start.
- Fixed an issue where notification progress would glitch on hover and notifications failed to auto-hide.
- Resolved an issue where the temp files cleanup was taking longer than expected.
- Fixed an issue where the stop button in the `Repair` page did not work as expected.
- Fixed an issue where edge was not being uninstalled correctly.

### Changes

- Changed the Disable Windows Update toggle to a 4 options dropdown menu:
  - Default
  - Security Only
  - Manually
  - Disabled
- Reworked the repairing process to make it more reliable and efficient.

## 1.5.2 - Released

> [!IMPORTANT]
> The update process has been modified to download the installer directly instead of a ZIP archive. This change will cause the automatic update feature to fail on versions prior to 1.5.2. Users on older versions must manually download and install version 1.5.2 or later from the [releases page](https://github.com/rayenghanmi/rytunex/releases) to benefit from future automatic updates.

### Added

- Introduced a new search bar to the title bar for quick navigation between toggles.
- Redesigned the `Security` page with comprehensive security monitoring:
  - Added real-time security status checks with auto-refresh every 30 seconds.
  - Added detection for Antivirus, Firewall, Windows Update, SmartScreen, Real-Time Protection, UAC, Tamper Protection, Controlled Folder Access, BitLocker, and Windows Defender Service.
  - Added Quick Scan functionality to run Windows Defender quick scans directly from the app.
  - Added Defender signature update functionality.
  - Added direct links to Windows Security settings pages for disabled features.
  - Added antivirus product name display and signature update date.
  - Improved performance by running security checks in parallel on background threads.
- Added the ability to install the app using Powershell script `irm "https://raw.githubusercontent.com/rayenghanmi/rytunex/main/install.ps1" | iex`.

### Fixes

- Fixed the Disable News and Interests toggle to set the correct registry values.
- Enhanced `Repair` page encoding handling for console output (Possible fix for #73).

### Changes

- Redesigned the setup for higher scaling quality on high DPI displays #71.
- Improved file/folder picker logic to use Microsoft.Windows.Storage.Pickers and removed DevWinUI.
- Pinned primary action buttons in the `Debloat` page to keep them visible while scrolling (suggested in #74).
- Updated the app's target framework to .NET 9 and Windows SDK 10.0.26100.0.
- Made navigation smoother by preloading search data, toggles, and system info.
- Prevented navigation stack crashes when clearing history.

## 1.5.1 - Released

### Fixes

- Removed duplicate toggle switches state saving in Registry.

### Changes

- Removed the newly added toggle switches state management as it was causing more issues than it was solving. The app will revert to using the old method for saving and retrieving toggle states.

## 1.5.0 - Released

### Added

- Added a new toggle switches state management that reflect the current system state rather than a user defined keys #50.
- Added a "Keep app data" checkbox to the `Revert All Changes` dialog, allowing users to preserve their application settings (language preferences, theme selection, and first-run dialog preferences) when reverting system optimizations.
- Added a new elevated (Admin) shield icon in the title bar to clearly indicate when the app is running with administrator privileges.

### Fixes

- Fixed an issue where Windows auto updates were not being disabled correctly when the `Disable Automatic Updates` option was toggled in the `Optimiza` page.
- Replaced the Upload/Download icons in the `Home` page with new icons that are visible in Windows 10.
- Fixed the `Legacy Volume Slider` toggle to set the correct registry values.
- Toggle state detection now reflects correct values.
- Fixed incorrect Edge SmartScreen registry keys when disabling Edge telemetry.
- Resolved an issue where the setup would throw an error when trying to install the app on some systems #61.

### Changes

- Enhanced the app's internal architecture for better performance and reliability.
- Improved the `Revert All Changes` functionality to work with the new registry state management.
- All extracted icons are now stored exclusively in a dedicated temp folder `%temp%\RyTuneX_AppIcons` instead of individual program folders. This prevents permission issues, keeps the system cleaner.
- Removed the following deprecated options from the `Optimize` page: `GPU and Priority Settings`, `Frame Server Mode`, `Low Latency GPU Settings`, `Non-Best Effort Limit`, and `Disable NTFS Timestamp`.
- Improved `DisableTelemetryServices` by adding safe Microsoft telemetry host blocking while keeping updates, Store, and activation fully functional.
- Refactored system monitoring to use native Windows APIs.
- Improved the app's startup time by optimizing the loading process (79.6% faster) #59.

## 1.4.1 - Released

### Added

- Added a new `Disable Windows Recall` option in the `Features` page.

### Fixes

- Fixed the `Debloat` page uninstallation and error notifications.

### Changes

- Removed the `Disable Unnecessary Services` option from the `Optimize` page due to its inconvenience.
- Removed the `Loading Window` at app startup to improve speed.
- Changed the text of the `Remove Temp Files` to `Clear System Temp` in the `Debloat` page for a better clarity.

## 1.4.0 - Released

### Added

- Added RTL (Right-to-Left) support for Arabic like languages, allowing the app to display correctly in these languages.
- Introduced a `ContentDialog` to display scan results on the `Repair` page #44.
- Added support for Hebrew language (`he-il`), contributed by @Y-PLONI #36.

### Fixes

- Fixed some wrong translations in the `zh-Hans` language, contributed by @wumingshiali #37.

### Changes

- Changed the restore point creation process to use the default System Restore `SystemPropertiesProtection` #41.
- Enhance telemetry disabling and temp file cleanup.

## 1.3.2 - Released

### Added

- Added a new `Disable Recommended Section In Start Menu` option in the `Features` page, suggested by @XMontech1337X #31.

### Fixes

- Fixed an error showing up when loading installed apps on for the first time #30.

### Changes

- Changed Edge removal to use @he3als EdgeRemover instead of the current one for better performance and reliability #32.

## 1.3.1 - Released

### Added

- Implemented a new way to export and import settings, allowing users to save their settings in a `.reg` file and import them later #28. This feature is available in the `Settings` page.
- Added vi-vn (Vietnamese) language support, contributed by @kleqing #29.

### Fixes

- Resolved an issue where the app would crash upon attempting to close it while the `Home` page was actively updating system usage statistics.
- Fixed an issue where some failed apps were being displayed as successfully uninstalled.

### Changes

- Updated the toggle switch state management in `FeaturesPage`, `OptimizeSystemPage`, and `PrivacyPage` to use Windows Registry entries instead of `LocalSettings` for saving and retrieving states.
- Improved Error Logging by displaying the error message in a notification before logging it.

## 1.3.0 - Released

### Added

- Added translations for Russian, Spanish, Korean, Portuguese, Italian, Turkish (thanks to @Vzxor) and Traditional Chinese (thanks to @OrStudio) languages.
- Introduced new optimization options:
  - `WPBT Execution Settings`
  - `Foreground Applications Prioritization`
  - `Paging Settings`
  - `NTFS Optimization`
  - `Legacy Boot Menu`
  - `Disable Unnecessary Services`

### Fixes

- Fixed an issue that was causing the restore point creation process to load indefinitely without creating any restore points.
- Resolved an issue where the `Revert All Changes` in the `Settings` page did not work as expected.
- Better `VBS` and `Widgets` disabling.
- Fixed an issue where the disc and network usage were not displayed correctly in the `Home` page.

### Changes

- Improved the `Home` page by spacing the usage tiles for better readability.
- Better UX for the `Device` page.
- Enhanced Win32 apps fetching and better icon generation for most apps.

## 1.2.0 - Released

### Added

- Introduced a toggle in the `Optimize` page to disable Service Host splitting for improved performance.
- Added options in the `Features` page to enable or disable Dark Mode and transparency effects without the need to activate Windows.
- Implemented an option in the `Optimize` page to reduce system binary sizes for freeing up disk space.
- Added a switch to disable background apps in the `Optimize`.
- Integrated lazy loading to improve performance when displaying app icons in the `appsTreeView`.

### Fixes

- Resolved issues where certain apps could not be uninstalled.
- Fixed minor translation issues to enhance language support.
- Corrected an issue where Snap Assist wasn't disabled when toggling the corresponding option.
- Fixed a bug causing the taskbar search to be hidden when toggling `Disable Quick History Access`.
- Addressed missing icons on Windows 10 for better visual consistency.
- Resolved an issue where the repair progress bar did not reset after operation completion.

### Changes

- Updated the UI to fallback to Acrylic when Mica material is not supported (Windows 10).
- Optimized the process for retrieving installed apps and improved app removal performance.
- Increased the speed of fetching installed apps to reduce load times.
- Expanded and improved commands for clearing temporary files.
- Enhanced the user experience on the `Debloat` page for better usability.


## 1.0.1 - Released

### Added

- Introduced a new `Security` page.
- Added support for Japanese language (`ja-jp`), contributed by @coolvitto.
- Integrated Realtime Performance Monitoring on the `Home` page.
- Included a Search Box on the `Debloat` page for improved navigation.
- Added a `Restart` button to the navigation menu for easy application restarts.
- Implemented Win32 app detection and removal on the `Debloat` page.
- Introduced a new `Repair` page to address system issues.
- Enabled the creation of a restore point on the first launch for added safety.
- Added functionality to restore the system to the most recent restore point and undo changes made by the app.
- Added a new loading animation at app startup, optimizing loading times for improved user experience.

### Fixes

- Fixed an issue where the uninstallation state text did not reflect the current app being uninstalled.
- Resolved a bug causing the UI under the apps list in the `Debloat` page to flicker when the scroll bar appeared or disappeared.
- Fixed an issue where GPU information was not displayed correctly on the `Home` page.
- Resolved an issue where the optimization functions whould run when the app was loading the toggle switches on initial load.

### Changes

- General UI enhancements for better user experience and consistency.
- Introduced a smooth page transition effect for seamless navigation.
- Improved the installed apps list on the `Debloat` page, adding logos for easier app identification.
- Moved the `Report` and `Support` buttons to the footer of the navigation menu for a cleaner layout.
- Improved error logging to prevent log corruption and enhance troubleshooting.
- Enhanced the visibility of the `Optimize`, `Privacy`, and `Features` pages by adding icons for better access.
- Expanded the `Optimize System` page with new settings for basic and advanced system optimizations, breaking down Performance Tweaks into smaller, more manageable controls:
  - `Menu Show Delay`, `Mouse Hover Time`, `Auto Complete`, `Crash Dump`, `Remote Assistance`, `Window Shake`, `Copy Move Context Menu`, `Task Timeouts`, `Low Disk Space Checks`, `Link Resolve`, `Service Timeouts`, `Remote Registry`, `File Extensions and Hidden Files`, `System Profile`, `GPU and Priority Settings`, `Frame Server Mode`, `Low Latency GPU Settings`, `Non-Best Effort Limit`.
- Introduced new privacy settings on the `Privacy` page, breaking down `Enhance Privacy` into smaller, focused controls:
  - `Advertising ID`, `Bluetooth Advertising`, `News and Interests`, `Spotlight Features`, `Tailored Experiences`, `Cloud Optimized Content`, `Feedback Notifications`, `Activity Feed`, `Cdp`, `Diagnostics Toast`, `Online Speech Privacy`, `Location Features`, `Automatic Restart Sign-On`, `Handwriting Data Sharing`, `Text Input Data Collection`, `Input Personalization`, `Safe Search Mode`, `Activity Uploads`, `Clipboard Sync`, `Message Sync`, `Setting Sync`, `Voice Activation`, `Find My Device`.


## 0.9.1 - Released

### Added

- Added detailed sections for Network and Battery information in `System Info` page.
- Enhanced `System Info` page to include more detailed information and options.
- Replaced the old Win32 folder picker with the modern `FolderPicker` from the `Windows.Storage.Pickers` namespace for selecting the folder path in the `System Info` page.
- Added caching to some pages to retain their data when navigating away and returning.

### Changes

- Optimized the loading of installed apps by parallelizing tasks and minimizing UI updates.
- Improved the efficiency of the uninstallation process by running tasks in parallel.
- Enhanced the responsiveness of the `Debloat` page.
- Introduced an enhanced way to gather system information in `System Info` page.
- Optimized the `Networking` page by caching network interfaces, enhancing DNS setting process and improving the UI responsiveness.
- Enhanced the UI by initializing toggle switches asynchronously and minimizing UI updates.
- Improved logging and error handling.

## 0.9.0 - Released

### Added

- Introduced the ability to update the app without the need to install it from the website.

### Fixes

- Resolved an issue where temporary files were not being removed.

### Changes

- Enhanced the failed uninstallation notification to ensure that the debloat process continues even if some apps fail to uninstall.

## 0.8.3 - Released

### Added

- Introduced a `RAM cleaner` toggle that utilizes the new `rytunexsvc` service, which periodically checks RAM usage and reduces its usage when it exceeds 80% by clearing the working set.

### Changes

- Introduced a `Biometrics` toggle as a separate option on the `Privacy` page.

### Fixes

- Fixed some minor translation mistakes.
- Made several other minor enhancements.
- Solved an issue where Microsoft Edge does not appear in the debloat list even though it was installed.

> [!NOTE]
> After removing Microsoft Edge, File Explorer may not restart immediately. You can simply open Task Manager and start a new task for explorer.exe.

## 0.8.2 - Released

### Changes

- Enhanced translation consistency across Arabic, French, and Simplified Chinese.
- Updated Simplified Chinese translation thanks to @wcxu21.

### Added

- Implemented German language translation.
- Introduced new Networking section with the bility to change DNS server.
- Ability to enable Endtask option in Windows 11.
- Ability to uninstall `Microsoft Edge` (Tested on Windows 11 22H2).
 
### Fixes

- Fixed minor `Debloat` problems.

> [!NOTE]
> Interested in contributing to RyTuneX translations? [Learn more here](https://github.com/rayenghanmi/RyTuneX?tab=readme-ov-file#-translation).

## 0.8.1 - Released

> This is a hotfix for version 0.8.0

### Added

- Add Simplified Chinese translation

### Fixes

- [x] Resolved an issue stopping users on Windows 10 from using `Debloat`.

### Known Issues

- This hotfix broke the ability to remove `MicrosoftEdge` (will be fixed in the next version).

## 0.8.0 - Released

### Fixes

- [x] Addressed a bug where the `Show All` checkbox would not retain its state after refreshing the list of installed apps upon uninstallation completion.
- [x] Resolved an issue where certain apps were incorrectly displaying `Uninstalled successfully` despite not being removed.
- [x] Fixed a recurring exception error that could arise after successfully removing certain packages.
- [x] Rectified an issue where reapplying tweaks would fail to initialize toggle switches' previous states.
- [x] Corrected the inaccurate count of uninstalled apps displayed on the `Debloat` page.
- [x] Mitigated a crash occurring on specific Windows versions upon successful installation startup. Refer to issue #8 for more details.

### Added

- Introduced an option to remove temporary files within the `Debloat` page.
- Included the capability to uninstall `Microsoft.MicrosoftEdge`.
- Included an option to extract and import drivers within the `System Info` page.
- Enhanced text and navigation animations.

## Known Issues

- Inconsistencies in language translation for Arabic and French.

## 0.7.2 - Released

### Added

- Verbose Logon Messages option in `System Features Page`.
- Restoring Classic Start Menu option in `System Features Page` under `Windows 11 Exclusive`.

### Changes

- Upgraded minimum requirements to `Windows 10 20H1`.
- An Overhauled UX & Redesigned UI :heart_eyes:.
- Rearraged Optimization options.

### Removed

- Irrelevent Optimization options as well as non-working ones.

### Fixes

- [x] Setup not opening for some users.
- [ ] Some optimization issues.
- [x] Some logs that are not being logged.

## 0.7.1 - Released

### Added

- Version number is now displayed on the titlbar.

### Fixes

- [x] Crashing when exiting the app while fetching installed apps.

## 0.7.0 - Unreleased

### Added

- Error Logging for better debugging.
- New `View Logs` HyperLink in Settings Page.

### Changes

- Minor UI improvements and fixes.
- Better exception handling.
- Package Name has been changed to `Rayen.RyTuneX`.

### Fixes

- [x] Selecting and unselecting a package, then selecting it again, should now attempt to remove it once.
- [x] Crashing when exiting the app while fetching installed apps.

## 0.6.0 - Released

> [!TIP]
> **This version adressed the following issue:**
>
> Windows Defender detecting the setup as a trojan:
>
> This problem occurs because `pyinstaller` compiling is similar to a trojan, and Windows Defender detects the executable as a malicious file due to the way it was compiled.

### Added

- An enhanced setup file.

### Changes

- Code clean up.

> [!NOTE]
> If you have any design ideas for the setup, feel free to share them.

## 0.5.0 - Unreleased

### Added

- Extended System information (Operating System, cpu, gpu, ram, ...).


## 0.4.0 - Unreleased

### Added

- The application now features an expanded translation coverage, with a greater portion of text available in both Arabic and French languages.

### Removed

- Bug reports (If you want to report a bug, open a new [issue](https://github.com/rayenghanmi/RyTuneX/issues/new) at GitHub). 

### Fixes

- [x] Language selection.
- [x] The welcome notification should be shown once now.

## Usage Guidance

To make sure that the app forces the user to run it as admin, follow these steps:

1. Open the `app.manifest` file in your project.

2. Uncomment the following lines:

```xml
  <security>
	  <requestedPrivileges>
		  <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
	  </requestedPrivileges>
  </security>
```

## 0.3.0 - Unreleased

- Initial upload to GitHub

## License

This project is licensed under the [GNU AFFERO GENERAL PUBLIC LICENSE Version 3](https://www.gnu.org/licenses/agpl-3.0.html) - see the [LICENSE.md](LICENSE.md) file for details.
