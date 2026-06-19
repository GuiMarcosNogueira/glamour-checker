# Contributing to Glamour Checker

Thank you for your interest in contributing to the Glamour Checker plugin for Final Fantasy XIV! By participating in this project, you agree to abide by our code of conduct and standard open-source conventions.

This document serves as the central hub for developers. It covers environment setup, manual testing, architectural design, and our pull request workflow.

---

## 🛠️ Developer Setup & Installation

To get started with development, you will need to configure your environment to interact with Dalamud's API and load the plugin in-game.

### Prerequisites
1. **Visual Studio 2022** (or Rider) with the `.NET Desktop Development` workload installed.
2. **.NET 10.0 SDK** (required for Dalamud plugins).
3. **XIVLauncher** and **Dalamud** installed and running on your system.

### Enabling Dalamud Developer Mode
Before you can load custom or compiled plugins:
1. Open the game using **XIVLauncher**.
2. Type `/xlsettings` in the chat to open Dalamud Settings.
3. Navigate to the **"Experimental"** tab.
4. Check the **"Enable Developer Mode"** box.

### Compiling and Running
1. Clone this repository to your local machine.
2. Open `GlamourChecker.sln` in your IDE.
3. Upon building the project for the first time, MSBuild will automatically search for your Dalamud installation at `%AppData%\XIVLauncher\addon\Hooks\dev\`.
4. Compile the project in **Debug** or **Release** mode. The output DLLs will be generated in `GlamourChecker/bin/`.
5. Type `/xlplugins` in the in-game chat to open the Plugin Installer.
6. Go to **Settings** -> **Experimental**, and add the absolute path to your `GlamourChecker/bin/Debug/GlamourChecker` folder into the "Dev Plugin Locations" list. Ensure the "Enabled" box is checked.
7. Under the **"Dev Tools"** tab (left menu), click **"Installed Dev Plugins"**. Find "GlamourChecker" and toggle it ON.
   > **Tip:** If you see the green message "No validation issues found in this plugin!", you are good to go! Type `/glamourchecker` to open the interface.

---

## 🏗️ Architecture Overview

If you want to add features or fix bugs, it helps to understand the core architecture. The plugin relies on Dalamud and Lumina (for Excel sheet data) and is built using C#.

### Core Managers
1. **`ModelScanner.cs`**
   - Generates a unique `ulong` signature (`ModelId`) for any item based on its 3D model, variant, and dyeability.
   - Strips dye channel bits for dyeable items, ensuring different colored versions of the same item resolve to the same underlying appearance.
   - Incorporates `EquipSlotCategory` to ensure weapons or gear from different slots don't collide.
2. **`InventoryWatcher.cs`**
   - Monitors the Glamour Dresser memory state (`MirageManager`) and Armoire in real-time.
   - Calculates "New Appearances" by comparing your active inventories against the `ModelId`s cached from the Dresser.
   - Identifies "Duplicates" by grouping items in the Dresser by their `ModelId`.
3. **`TooltipManager.cs`**
   - Uses `AddonLifecycle` to intercept the `ItemDetail` addon (tooltips).
   - Injects custom text payload into the item category node (e.g., "Necklace [Model: Stored]").
4. **`Loc.cs`**
   - Simple localization engine that loads JSON files from the `loc/` directory.

### Memory & State
GlamourChecker relies on FFXIV's active memory for reading the Dresser:
- The Dresser items are stored in the client memory only when you physically open the Glamour Dresser in an inn room.
- To avoid massive performance hits, the plugin reads `MirageManager->PrismBoxItemIds`. This array is monitored continuously while the UI is open. If the total item count changes, the plugin forcibly recalculates all duplicates instantly.

### Adding a New Language
1. Duplicate `loc/en.json` and rename it to your locale code (e.g., `es.json`).
2. Translate the right-hand values.
3. The plugin will automatically detect the new JSON file and add it to the Language dropdown.

---

## 🧪 Testing

We highly value code quality. Please make sure that you write unit tests for any new logic added to `GlamourLogic.cs` or the `ViewModels`.

- Run tests using your IDE's test runner, or run the following command in the root folder:
  ```bash
  dotnet test
  ```
- Make sure that test coverage does not drop significantly.

---

## 💅 Code Style & Formatting

This project enforces **strict code formatting** using `.editorconfig`. If your code is not formatted properly, the build **will fail**.

- Before opening a Pull Request, always run the formatter:
  ```bash
  dotnet format
  ```
- Do not bypass formatting errors. If the build pipeline catches a formatting warning (`IDE0055`), your Pull Request will be blocked.

---

## 🔄 Pull Request Process (GitHub Flow)

We use the GitHub Flow branching strategy combined with `release-please` for automated versioning. Please follow these steps:

1. **Never commit directly to `main`.**
2. Create a new branch from `main` (e.g. `feat/new-ui-button` or `fix/crash-on-open`).
3. Make your changes and write tests.
4. **DO NOT MANUALLY UPDATE `CHANGELOG.md`**. This is handled automatically by the Release Please bot.
5. Commit your changes using [Conventional Commits](https://www.conventionalcommits.org/) (e.g., `feat: added new UI button`, `fix: corrected item sort order`). This is **crucial** because our CI pipeline reads these prefixes to generate automated release notes!
6. **Customizing the Release Notes (Dalamud Integration):**
   When the `release-please` bot decides to cut a new release, it will automatically open a Release Pull Request (e.g., `chore: release 0.4.5`). This PR will contain a raw list of commits.
   To make the release notes friendly for our users (which will be displayed on GitHub **and injected directly into the Dalamud Plugin Installer**):
   - Edit the description (body) of the bot's Release PR before merging it.
   - Append the following block to the end of the description:
     ```markdown
     BEGIN_COMMIT_OVERRIDE
     🎉 **Version 0.4.5 - The Lalafell Update!**
     
     ✨ **New Features:**
     - Added a beautiful new icon.
     - The changelog is now synchronized with Dalamud!
     END_COMMIT_OVERRIDE
     ```
   - When you merge the PR, the bot will parse this block and use it as the official changelog for both `CHANGELOG.md` and the in-game Dalamud update window!
7. Open a normal Pull Request against `main` for your features and fill out the Pull Request Template checklist.
8. A maintainer will review your code. Once approved and CI checks pass, it will be Squash & Merged into `main`.

Thank you for keeping our community awesome!
