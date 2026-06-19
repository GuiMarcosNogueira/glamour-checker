# AI Assistant Guidelines (Antigravity)

## Git & Branching
- **Always Branch Out:** NEVER make direct commits to the `main` branch. Always create a new branch before making any changes (`git checkout main && git pull`, then `git checkout -b feature/feature-name`).
- **Push Only With Approval:** NEVER execute a `git push` command without first asking for and receiving explicit approval from the user. Make your changes locally and notify the user when they are ready for testing.

## Pull Requests & Changelog
- **Pull Request Template:** When creating a Pull Request, you MUST ALWAYS read and follow the template provided in `.github/PULL_REQUEST_TEMPLATE.md`.
- **Automated Changelog:** DO NOT update `CHANGELOG.md` manually in your commits! This is handled automatically by the Release Please bot based on Conventional Commits. Focus on writing good `feat:`, `fix:`, or `refactor:` prefixes.

## Code Quality & Documentation
- **Write Tests & Verify Coverage:** ALWAYS write new unit tests for any code updates to ensure that Line, Branch, and Method coverage do NOT drop. Run `dotnet test` and ensure all tests pass.
- **Update README.md:** Review `README.md` and ensure any new features or architectural changes from the session are properly documented.
