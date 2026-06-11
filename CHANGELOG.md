# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Item Sorting:** The missing items list now precisely reflects the native sort order used by the game's Glamour Dresser and Armoury Chest (Grouped by Slot -> Item Level -> ID).
- **Auto-Open:** The plugin window now opens automatically when interacting with the Glamour Dresser or Armoire.

### Fixed
- **Missing Icon:** Fixed an issue where the plugin icon would not load in version `0.1.2.0` in the Dalamud custom repository.
- **CI/CD Formatting:** The CI pipeline was improved to strictly enforce file formatting rules and fail if formatting is broken.

### Security
- (Reserved for future security updates)

---

## [0.1.2.0] - 2026-06-10

### Added
- **Tooltip Integration:** Added visual indicators `[Model: Stored]` and `[Model: Not Stored]` directly on the item tooltips in-game.
- **Gearset Filter:** Added an option to hide items that are already part of one of your Gearsets.

### Changed
- **Architectural Refactor:** `GlamourLogic.cs` was completely refactored to use delegate dependency injection, removing static `DataManager` dependencies and enabling pure unit testing.
- **Code Coverage:** Expanded test coverage to 98.4% of lines and 93% of branches.

---

## [0.1.1.0] - 2026-06-08

### Added
- **Continuous Deployment (CI/CD):** Full integration with GitHub Actions to automatically generate the `pluginmaster.json` on every new tag, as well as smoothly build and publish releases.
- **Coverage Report:** Configured Coverlet with integrated visualizations directly on Pull Requests and Actions.

---

## [0.1.0.0] - 2026-06-01

### Added
- Initial release of the **Glamour Checker** plugin.
- Inventory scanning tools (Bags, Saddlebags, and Retainers) to cross-reference with the contents of the Glamour Dresser and Armoire.
- Configuration window to manage item exclusions.
- Support for checking duplicates to easily free up storage slots.
