# Release package verification

Run `python3 scripts/test/verify-release-package.py` in a fresh disposable checkout with .NET 10, Python 3 and Bash. Public NuGet restore/advisory access is required unless current dependency and advisory data are already cached. Existing `artifacts` is rejected; the verifier never deletes an existing stage.

The verifier reads and executes the actual release workflow Restore, Build (Release), Test (Release) and Pack commands, with `REF_NAME=v9.8.7-audit.1`. It never publishes. Its deliberately narrow parser rejects unsupported workflow execution shapes rather than silently simulating them. Update its accepted shape alongside intentional release-command changes.

The package probe checks both actual package filenames, nuspec versions and the Contracts dependency, packaged assembly versions, and byte-for-byte declared README/LICENSE assets plus Core NOTICE. Thus stamping the nuspec while retaining an old compiled DLL also fails. No generated golden files are involved.

Required build-and-test runs this verifier. The independent required gate-coverage job checks that invocation and both protected HIGH paths via `--check-wiring`.

Both jobs must remain unconditional without job-level execution overrides. Qualification independently added `if: false` to each real job: the earlier wiring check accepted those bypasses, and the corrected check refuses both while accepting the real workflow.
