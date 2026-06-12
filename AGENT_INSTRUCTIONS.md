# Agent Push Instructions

Whenever the user asks you to "pack everything and push" or "push the changes", you MUST perform the following steps sequentially before making the push:

1. **Run Tests & Verify Coverage:**
   Run `dotnet test --collect:"XPlat Code Coverage"`
   Ensure all tests pass. Read the coverage summary and verify coverage remains high (> 90%).

2. **Update README.md:**
   Review `README.md` and ensure any new features or architectural changes from this session are properly documented.

3. **Update CHANGELOG.md:**
   Add a new entry in `CHANGELOG.md` under `## [Unreleased]` describing the features added, changed, or fixed in this session.
   Follow the format: `### Added`, `### Changed`, `### Fixed`.

4. **Commit & Push:**
   Stage all changes (`git add .`).
   Commit with a clear, descriptive message (`git commit -m "..."`).
   Push to the repository (`git push`).
