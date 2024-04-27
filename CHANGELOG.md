# CHANGELOG.md

All notable changes to this branch will be documented in this file.

## 0.8.2 - Unreleased

## TODO

- Startup apps
- Edge removal (partially done) (not tested)

## Changes

- Enhanced translation consistency across Arabic, French, and Simplified Chinese.
- Updated Simplified Chinese translation thanks to @wcxu21.

## Added

- Implemented German language translation.
- Introduced new Networking section with the bility to change DNS server.
- Ability to enable Endtask option in Windows 11.
- Option to add background blur effect to explorer made by [Maplespe's ExplorerBlurMica](https://github.com/Maplespe/ExplorerBlurMica).

## Fixes

- Fixed minor `debloat` problems.

> [!NOTE]
> Interested in contributing to RyTuneX translations? [Learn more here](https://github.com/rayenghanmi/RyTuneX?tab=readme-ov-file#-translation).

## 0.8.1 - Released

> This is a hotfix for version 0.8.0

## Added

- Add Simplified Chinese translation

## Fixes

- [x] Resolved an issue stopping users on Windows 10 from using `Debloat`.

## Known Issues

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