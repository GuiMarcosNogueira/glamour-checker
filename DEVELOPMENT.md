# Developer Documentation

Welcome to the GlamourChecker developer guide! If you want to contribute, add features, or fix bugs, this guide covers the core architecture.

## Architecture Overview

The plugin relies on Dalamud and Lumina (for Excel sheet data) and is built using C#. The core functionality is divided into several managers:

1. **`ModelScanner.cs`**
   - Responsible for generating a unique `ulong` signature (`ModelId`) for any item based on its 3D model, variant, and dyeability.
   - We strip the dye channel bits for items that are dyeable (`DyeCount > 0`), ensuring that a blue and red version of the same dyeable item resolve to the same underlying appearance.
   - Incorporates `EquipSlotCategory` to ensure weapons or gear from different slots don't accidentally collide.

2. **`InventoryWatcher.cs`**
   - Monitors the Glamour Dresser memory state (`MirageManager`) and Armoire in real-time.
   - Calculates "New Appearances" by comparing your active inventories (retrieved via `InventoryManager`) against the `ModelId`s cached from the Dresser.
   - Identifies "Duplicates" by grouping items in the Dresser by their `ModelId`.

3. **`TooltipManager.cs`**
   - Uses `AddonLifecycle` to intercept the `ItemDetail` addon (tooltips).
   - Injects custom text payload into the item category node (e.g., "Necklace [Model: Stored]").

4. **`Loc.cs`**
   - Simple localization engine. It loads JSON files from the `loc/` directory.
   - `PluginLanguage` in the config dictates which JSON is loaded. Fallbacks to `en.json`.

## Memory & State

GlamourChecker relies on FFXIV's active memory for reading the Dresser:
- The Dresser items are stored in the client memory only when you physically open the Glamour Dresser in an inn room.
- To avoid massive performance hits, the plugin reads `MirageManager->PrismBoxItemIds`. This array is monitored continuously while the UI is open. If the total item count changes (e.g., you restore an item), the plugin forcibly recalculates all duplicates instantly.

## Local Testing & Debugging

1. Ensure you have the XIVLauncher and Dalamud installed with Developer Mode enabled.
2. Clone this repository and open `GlamourChecker.csproj` in your IDE.
3. Run `dotnet build`.
4. Point Dalamud's Dev Plugin paths to your `bin/Debug/GlamourChecker.dll`.
5. Useful debugging command: `/glamourchecker scan` forces a manual attempt to read the Dresser, and will reload the Localization dictionary without needing to restart the plugin.

## Adding a New Language

1. Duplicate `loc/en.json`.
2. Rename it to your locale code (e.g., `es.json`, `fr.json`).
3. Translate the right-hand values.
4. The plugin will automatically detect the new JSON file and add it to the Language dropdown in the `/glamourchecker config` menu.
