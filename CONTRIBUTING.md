# Contributing to Paunix Guard

Paunix Guard is a source-available project licensed under the **PolyForm Shield License 1.0.0**. You are welcome to inspect, audit, and suggest improvements to the codebase.

## Security Audits

If you find a security vulnerability, please open a GitHub Issue (do not disclose it publicly until a fix is available). We appreciate responsible disclosure.

## Submitting Changes

Pull requests are welcome under the following conditions:

**Contributor License Agreement (CLA)**

By submitting a pull request, you agree that:

1. You have the right to license your contribution.
2. You grant the Paunix Guard project owner (the licensor) a perpetual, worldwide, non-exclusive, royalty-free, irrevocable license to use, reproduce, modify, distribute, sublicense, and relicense your contribution under **any license**, including but not limited to the PolyForm Shield License, MIT License, or a proprietary/commercial license.
3. You understand that your contribution may become part of a paid or proprietary version of Paunix Guard.

This CLA ensures the project owner can relicense the codebase in the future (e.g., from PolyForm Shield to MIT) without being blocked by unlicensed third-party contributions.

## Code Style

- Follow existing patterns in the codebase.
- Use C# 12 features (primary constructors, collection expressions, etc.).
- Keep methods small and focused.
- No comments unless the logic is non-obvious.
- Write unit tests for new features.

## Pull Request Process

1. Fork the repository.
2. Create a feature branch.
3. Make your changes.
4. Run `dotnet build` and `dotnet test` — all tests must pass.
5. Submit a PR against `master`.
6. Ensure your PR description explains the "why" behind the change.
