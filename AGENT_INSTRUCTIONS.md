# Agent Push Instructions

Whenever the user asks you to "pack everything and push" or "push the changes", you MUST perform the following steps sequentially before making the push.

**CRITICAL RULE 1:** ALWAYS create a new branch before making any changes to the project. Before branching out, ALWAYS ensure you checkout `main` and pull the latest changes (`git checkout main && git pull`). Do not commit or push directly to the `main` branch. Use descriptive names for your branches (e.g., `feature/add-new-ui`, `fix/refactor-logic`).

**CRITICAL RULE 2:** Do NOT push changes to the remote repository automatically or implicitly. ALWAYS wait for explicit permission or a direct request from the user before running `git push`.

1. **Run Tests & Verify Coverage:**
   Run `dotnet test --collect:"XPlat Code Coverage"` (or your custom test command).
   Ensure all tests pass. Read the coverage summary and verify coverage remains high (> 95%).

2. **Generate/Update Coverage Report:**
   Run the coverage report generator tool (e.g., `reportgenerator`) to update the HTML coverage reports.

3. **Update README.md:**
   Review `README.md` and ensure any new features or architectural changes from this session are properly documented.

4. **Update CHANGELOG.md:**
   Add a new entry in `CHANGELOG.md` under `## [Unreleased]` describing the features added, changed, or fixed in this session.
   Follow the format: `### Added`, `### Changed`, `### Fixed`.

5. **Commit & Push:**
   Stage all changes (`git add .`).
   Commit with a clear, descriptive message (`git commit -m "..."`).
   Push to the repository (`git push`).
