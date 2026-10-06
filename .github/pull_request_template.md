## Summary

Describe the user-visible or repository-level change and why it is needed.

## Validation

- [ ] `dotnet csharpier check .`
- [ ] `dotnet build FixPortal.FixAtdl.slnx --configuration Release --no-restore`
- [ ] `dotnet test --solution FixPortal.FixAtdl.slnx --configuration Release --no-build`
- [ ] Applicable package, workflow, or documentation checks passed.

## Review checklist

- [ ] `CHANGELOG.md` has an `[Unreleased]` entry for any consumer-visible change.
- [ ] Breaking API, JSON contract, or FIX wire-format impact is called out explicitly.
- [ ] No Contracts golden under `tests/FixPortal.FixAtdl.Contracts.Tests/Golden/` was regenerated.
- [ ] Tests assert the important values and failure modes, not only that execution completes.
- [ ] License and Atdl4net attribution requirements are preserved.
- [ ] No secrets, proprietary strategy documents, or client data are included.
