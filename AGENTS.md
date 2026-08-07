# Sharp Forge Development Rules

- Write all source code, comments, XML documentation, user-facing technical documentation, and commit-ready text in English.
- Document every type and method with XML documentation. Include `param`, `returns`, and `exception` elements whenever applicable.
- Keep the application version in `SharpForge/SharpForge.csproj` compliant with Semantic Versioning (`MAJOR.MINOR.PATCH`). Increment it for every user-facing change: `PATCH` for fixes, `MINOR` for backward-compatible features, and `MAJOR` for breaking changes.
- Display the exact three-part project version in the right side of the application footer.
- Preserve `#202020` as the window background and `#252525` as the card background unless the product requirements explicitly change.
- Use embedded SVG assets through `SvgIconProvider`; do not use Unicode glyphs or symbol fonts as interface icons.
- Keep the Real-ESRGAN runtime and model files included in build and publish output through the project file.
- Run a Release build, `dotnet format --verify-no-changes`, and `git diff --check` before considering a change complete.
