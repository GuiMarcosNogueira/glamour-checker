# GlamourChecker

GlamourChecker is a Dalamud plugin for Final Fantasy XIV that helps you manage your Glamour Dresser and Armoire. It automatically tracks the appearances (models) you have stored and identifies duplicate items or unstored appearances across your inventories.

## Features

- **New Appearances (Aparências Novas):** Scans your Inventory, Armoury Chest, Chocobo Saddlebag, and Retainers to find items whose visual appearance (model + dye channels) is **not yet stored** in your Glamour Dresser or Armoire.
- **Duplicates (Duplicatas):** Identifies items stored in your Glamour Dresser or Armoire that share the exact same visual appearance, helping you free up valuable storage space.
- **Tooltip Integration:** Displays a visual indicator `[Model: Stored]` or `[Model: Not Stored]` directly on in-game item tooltips, so you know at a glance if you can safely discard or sell an item.
- **Multi-Language Support:** Fully translatable UI via lightweight JSON files. Supports English, Portuguese, and easy community contributions.

## Usage

1. Open your **Glamour Dresser** in an inn room. The plugin will automatically scan your stored items and update its database in real-time.
2. Open the **Armoire** to scan its contents as well.
3. Type `/glamourchecker` in the chat to open the main window.
4. Use the dropdowns to filter by inventory type (e.g., Retainer Inventory, Armoury Chest).
5. Type `/glamourchecker config` to access the settings (Tooltip integration and Language selection).

---

## Developer Documentation

If you want to contribute, add features, or fix bugs, please see [DEVELOPMENT.md](DEVELOPMENT.md) for a guide to the core architecture, state management, and localization engine.
