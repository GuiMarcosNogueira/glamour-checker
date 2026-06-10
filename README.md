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

### The Hybrid Architecture (Best of Both Worlds)

Because of how Final Fantasy XIV recycles 3D models, GlamourChecker uses a **Hybrid Architecture** to balance the needs of hardcore collectors and players trying to free up space in their Glamour Dresser.

1. **New Appearances Tab & Tooltips (Strict Native Mode):**
   - When telling you if an item is "New", the plugin acts as a **perfectionist collector**.
   - It uses the strict internal game IDs to detect minimal texture differences. For example, if two robes have the exact same 3D mesh but one has a silver zipper and the other has a gold zipper, the plugin considers them **completely different items**. 
   - This ensures you never accidentally throw away a unique variant just because the mesh is identical.

2. **Duplicates Tab (Aggressive GarlandTools Cleanup):**
   - When telling you if you have duplicates stored, the plugin acts as a **ruthless organizer**.
   - It uses a custom database powered by GarlandTools to group items that share the exact same 3D base mesh, completely ignoring texture and zipper color differences.
   - This aggressively finds old leveling gear (like 5 pairs of identical shoes with different names) that are wasting your precious 800 Dresser slots.
   - **The Tradeoff:** Because it ignores textures, it might generate "False Positives" for perfectionists (e.g., grouping a one-eyed monocle with two-eyed spectacles because the game engine packed them into the same base mesh).

### Absolute Player Control

To combat these engine limitations and false positives, you have full control via the Right-Click context menu on any item:

- **Try On:** Instantly opens the in-game Fitting Room so you can inspect the subtle differences on your character before deciding if you want to keep or delete the item.
- **Independent Ignore Lists:** 
  - **"Ignore as New Appearance":** If the strict mode tells you a robe with a gold zipper is "New", but you don't care about zipper colors, click this. The plugin will stop telling you to collect it.
  - **"Ignore as Duplicate":** If the aggressive mode tells you your precious Monocle is a duplicate of your Spectacles, click this. The plugin will stop suggesting you delete it.
- **Copy Item Name:** Quickly copies the name to search on the Market Board.

---

## Developer Documentation

If you want to contribute, add features, or fix bugs, please see [DEVELOPMENT.md](DEVELOPMENT.md) for a guide to the core architecture, state management, and localization engine.
