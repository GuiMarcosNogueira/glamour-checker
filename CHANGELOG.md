# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed
- **Context Menu Duplication:** Fixed an issue in the main window UI where right-clicking an item that had exact duplicates in the same list (e.g., identical pieces in different Armoury Chest slots) would cause the context menu options to duplicate themselves infinitely. The menu generation now strictly enforces uniqueness per row.
- **Tooltip Layout & Width:** Fixed a pervasive UI bug where expanding the tooltip width to fit long item replacement names would permanently stretch the tooltip for all subsequent items and break the native "ITEM LEVEL" bar alignment. The custom tooltip injection now flawlessly leverages FFXIV's native layout engine to dynamically expand and shrink per-item without overlapping or clipping.

---

## [0.3.1.5] - 2026-06-13

### Added
- **In-Game Tutorial:** Added a new welcome window that automatically opens on the first launch to teach new users how to use the plugin and scan their Dresser/Armoire. Can be re-opened with `/glamourchecker tutorial`.

---

## [0.3.1.0] - 2026-06-12

### Added
- **Ignored Items Tab:** Added a dedicated tab in the main window (can be enabled via settings) to manage ignored new appearances and ignored duplicates. It features item icons, tooltips, and correct grouping logic.

### Changed
- **Tooltip "Stored" Text:** Streamlined the tooltip status from `[Item: Stored]` to `[Stored]` (and similar in PT-BR) for a cleaner UI.
- **Code Coverage:** Expanded test coverage to 98.9% of lines and 90.5% of branches.

### Fixed
- **Infinite Tooltip Growth:** Fixed a critical bug in the tooltip module where the FFXIV native UI engine would infinitely expand the tooltip's background vertically when inspecting items with specific flags (e.g. "Market Prohibited").
- **Unified Duplicate Grouping:** Fixed an issue where items in the Ignored Duplicates list were not grouped correctly. They now strictly use the same Visual Group ID fallback logic as the main Duplicates tab.

---

## [0.2.1.0] - 2026-06-11

### Added
- **Exact Item Tooltips:** Tooltips now distinguish between having the exact item stored versus having a shared appearance stored. If you only own a shared appearance, the tooltip dynamically fetches and displays the name of the identical item you already own (e.g., `[Appearance: Brand-new Trousers]`).
- **Ignored Status Tooltip:** Items that have been manually marked to be ignored from the New Appearances list will now show an explicit `[Ignored]` status on their tooltips, giving clearer feedback.

### Changed
- **Code Coverage:** Expanded test coverage to 98.5% of lines and 92.4% of branches.

### Fixed
- **Outfit Inspection Tooltips:** Fixed an issue where items inspected inside of Outfits wouldn't correctly report as "Stored" due to the way FFXIV handles Outfit sub-items.
- **Accessory Masking:** Fixed a bug where FFXIV's 16-bit accessories mask wasn't being read completely, causing some accessories to falsely appear as not stored.
- **Compiler Warnings:** Resolved nullable and obsolete warnings in the test layer, allowing the strict CI pipeline to pass flawlessly.

---

## [0.1.3.0] - 2026-06-10

### Added
- **Item Sorting:** The missing items list now precisely reflects the native sort order used by the game's Glamour Dresser and Armoury Chest (Grouped by Slot -> Item Level -> ID).
- **Auto-Open:** The plugin window now opens automatically when interacting with the Glamour Dresser or Armoire.

### Changed
- **Architectural Refactor:** `GlamourLogic.cs` was completely refactored to use delegate dependency injection, removing static `DataManager` dependencies and enabling pure unit testing.
- **Release Automation:** The CI pipeline now automatically parses `CHANGELOG.md` to cleanly format GitHub Releases, replacing the raw commit logs.
- **Plugin Icon:** Replaced the plugin icon with a new, higher quality image.

### Fixed
- **Missing Icon:** Fixed an issue where the plugin icon would not load in version `0.1.2.0` in the Dalamud custom repository.
- **CI/CD Formatting:** The CI pipeline was improved to strictly enforce file formatting rules and fail if formatting is broken.

---

## [0.1.2.0] - 2026-06-10

### Added
- **Tooltip Integration:** Added visual indicators `[Model: Stored]` and `[Model: Not Stored]` directly on the item tooltips in-game.
- **Gearset Filter:** Added an option to hide items that are already part of one of your Gearsets.

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
