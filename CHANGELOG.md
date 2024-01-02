# CHANGELOG.md

All notable changes to this project will be documented in this file.
## 0.5

### Added

- Extended System information (Operating System, cpu, gpu, ram, ...).


## 0.4

### Added

- The application now features an expanded translation coverage, with a greater portion of text available in both Arabic and French languages.

### Removed

- Bug reports (If you want to report a bug, open a new [issue](https://github.com/rayenghanmi/RyTuneX/issues/new) at GitHub). 

### Fixed

- Language selection.
- The welcome notification should be shown once now.

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

## 0.3 - Unreleased

- Initial upload to GitHub

## License

This project is licensed under the [GNU AFFERO GENERAL PUBLIC LICENSE Version 3](https://www.gnu.org/licenses/agpl-3.0.html) - see the [LICENSE.md](LICENSE.md) file for details.