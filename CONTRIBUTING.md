# Contributing to Glamour Checker

Thank you for your interest in contributing to the Glamour Checker plugin for Final Fantasy XIV! 
By participating in this project, you agree to abide by our code of conduct and standard open-source conventions.

## 🛠️ Developer Setup Guide

To get started with development, you will need to clone the repository and configure your environment to interact with Dalamud's API.

### Prerequisites
1. **Visual Studio 2022** (or Rider) with the `.NET Desktop Development` workload installed.
2. **.NET 10.0 SDK** (required for Dalamud plugins).
3. **XIVLauncher** and **Dalamud** installed and running on your system.

### Compiling and Running
1. Clone this repository to your local machine.
2. Open `GlamourChecker.sln` in your IDE.
3. Upon building the project for the first time, MSBuild will automatically search for your Dalamud installation at `%AppData%\XIVLauncher\addon\Hooks\dev\`.
4. Compile the project in **Debug** or **Release** mode. The output DLLs will be generated in `GlamourChecker/bin/`.
5. Open Final Fantasy XIV via XIVLauncher.
6. Type `/xlplugins` to open the Plugin Installer, go to **Settings** -> **Experimental**, and add the path to the `GlamourChecker/bin/Debug/GlamourChecker` folder in the "Dev Plugin Locations" list.
7. You can now hot-reload the plugin using the Dalamud UI.

## 🧪 Testing

We highly value code quality. Please make sure that you write unit tests for any new logic added to `GlamourLogic.cs` or the `ViewModels`.

- Run tests using your IDE's test runner, or run the following command in the root folder:
  ```bash
  dotnet test
  ```
- Make sure that test coverage does not drop significantly.

## 📋 Code Style & Formatting

This project enforces **strict code formatting** using `.editorconfig`. If your code is not formatted properly, the build **will fail**.

- Before opening a Pull Request, always run the formatter:
  ```bash
  dotnet format
  ```
- Do not bypass formatting errors. If the build pipeline catches a formatting warning (`IDE0055`), your Pull Request will be blocked.

## 🔀 Pull Request Process (GitHub Flow)

We use the GitHub Flow branching strategy. Please follow these steps:

1. **Never commit directly to `main`.**
2. Create a new branch from `main` (e.g. `feat/new-ui-button` or `fix/crash-on-open`).
3. Make your changes and write tests.
4. **Update `CHANGELOG.md`**: Add a note about your changes under the `## [Unreleased]` section.
5. Commit your changes using [Conventional Commits](https://www.conventionalcommits.org/) (e.g., `feat: added new UI button`, `fix: corrected item sort order`). This is crucial because our CI pipeline reads these prefixes to generate automated release notes!
6. Open a Pull Request against `main`. 
7. Fill out the Pull Request Template checklist.
8. A maintainer will review your code. Once approved and CI checks pass, it will be Squash & Merged into `main`.

Thank you for keeping our community awesome!
