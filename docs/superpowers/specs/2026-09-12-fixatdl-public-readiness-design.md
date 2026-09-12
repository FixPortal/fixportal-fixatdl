# FIXatdl Public Readiness Design

## Goal

Make the public FIXatdl ecosystem easy to evaluate and adopt before an
announcement, without extending the runtime product surface or making claims
that are not evidenced by source and tests.

## Scope

The pass covers `fixportal-fixatdl`, `fixportal-fixatdl-wpf`,
`fixportal-fixatdl-react`, `fixportal-simulator-backend`,
`fixportal-simulator-frontend`, and `ems-win-app`.

### Public library repositories

Each public package README will use one consistent ecosystem narrative:

- `FixPortal.FixAtdl` is the headless .NET FIXatdl v1.1 parser, validator and
  tag-value emitter.
- `FixPortal.FixAtdl.Wpf` renders editable WPF strategy forms over core models.
- `@fix-portal/fixatdl-react` renders browser-side React strategy forms from
  backend-owned DTOs.

Readmes will link to the other packages, public package registries and their
source repositories. They will state the host boundary: order construction,
transport, schema validation and broker-specific behaviour remain host work.
The core README will correct stale version/status wording and use the canonical
repository casing in its issue link. Existing licence and upstream attribution
will remain intact.

### Integration evidence

The simulator repositories will document only the integration already present:
the backend parses and maps FIXatdl XML to its DTO contract and the frontend
renders the workbench through the published React package. EMS will be inspected
for its actual WPF integration and documented only if source evidence supports a
reproducible developer path. No user-facing simulator or EMS behaviour changes
are in scope.

### Launch drafts

Each public package repository may contain a clearly labelled internal
`docs/launch/` draft. Drafts may include release copy, a LinkedIn post and a
technical article outline. They will not be linked from the README, exposed in
application navigation, published, or sent to anyone.

### GitHub discovery metadata

Repository descriptions, homepage URLs and topics will be set from the final
README language. Metadata will use only terms demonstrated by the package:
`fix`, `fixatdl`, `dotnet`, `wpf` or `react`, plus `trading` where appropriate.

## Constraints

- Do not alter public APIs, package versions, licences or attribution.
- Do not add dependencies or a marketing site.
- README content packed into a NuGet package remains NuGet-gallery compatible.
- Claims must be traceable to implementation, tests or an existing release.
- All edits use isolated worktrees and each repository's normal validation gate.
- Announcements remain drafts pending Chris's explicit publication decision.

## Verification

Run the existing documentation/package checks for every edited library. Run the
simulator checks required by the touched repository. Validate every outbound
README link and package-install command against its public source. Review
metadata through the GitHub API before changing it.

## Out of scope

No new control types, conformance claim, full FIXatdl certification, hosted demo,
video production, outreach, customer contact, or public announcement is part of
this pass.
