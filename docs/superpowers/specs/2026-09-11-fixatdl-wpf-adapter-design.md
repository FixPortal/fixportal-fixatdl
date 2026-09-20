# FixPortal.FixAtdl.Wpf adapter — design spec

**Date:** 2026-09-11
**Status:** approved for planning
**Author:** brainstorm session, fixportal-fixatdl

## Context

fixportal-fixatdl (this repo) forked the defunct FIXatdl.net project and, in a
prior refactor (commit `23c877a`), stripped WPF, Notification, Providers and
example-app code to isolate the core ATDL parsing/model library from UI. That
core is now a clean, OSS-ready, published package
(`FixPortal.FixAtdl`, 1.0.0 shipped).

Two goals now motivate revisiting the UI split:

1. EMS (our desktop trading app, which we control) needs to leverage the new
   ATDL core library, replacing its dependency on the original defunct
   FIXatdl.net. This requires a WPF UI layer — EMS's algo-parameter editing is
   "full fat": strategy parameters are entirely editable via dynamically
   constructed forms generated from the ATDL Strategy XML, matching what the
   original FIXatdl.net WPF layer provided.
2. We want to showcase this work as an OSS release once both the WPF adapter
   (this spec) and a parallel React extraction (from the simulator's existing
   full-fat ATDL workbench — separate work, handed to Codex, out of scope
   here) are complete. No announcement until both land.

This spec covers only the WPF side: a new `fixportal-fixatdl-wpf` repository
providing `FixPortal.FixAtdl.Wpf`, a dynamic-form-rendering control library
that EMS references directly.

## Goals

- Full dynamic form generation from ATDL `Strategy_t` (rendered from Strategy
  XML via the existing core parser) — control layout, data binding, edit
  triggers, validation — equivalent capability to the original FIXatdl.net
  WPF layer.
- Clean composition: EMS is being upgraded alongside this, so no compatibility
  shims for a legacy EMS DI container/MVVM framework. Adapter assumes and
  requires `Microsoft.Extensions.Hosting` generic host + `CommunityToolkit.Mvvm`
  on the consuming side, per `scaffold-desktop` conventions.
- Package as a reusable control library (`FixPortal.FixAtdl.Wpf`), not a
  bundled app — EMS hosts it in its own window/panel/navigation.
- OSS-clean: no legacy FIXatdl.net licensing/attribution debt carried forward
  beyond what the core repo already resolved.

## Non-goals

- Not rebuilding EMS's own navigation, theming, or app shell — the adapter is
  a control surface, not an application.
- Not a from-scratch WPF rewrite of rendering/layout math — the original
  layer's layout algorithms are being ported, not re-derived.
- Not covering the React/simulator extraction — separate repo, separate
  owner (Codex), tracked independently.
- Not migrating EMS's existing (non-ATDL) UI to CommunityToolkit.Mvvm/generic
  host — only the surface EMS uses to host this adapter needs to speak that
  contract; if EMS overall isn't yet on those conventions, that migration is
  EMS's own scope, a stated dependency of this work, not part of it.

## Source material

The pre-strip WPF tree is fully recoverable from git history at
`23c877a^` (the parent of "refactor: strip WPF, Notification, Providers,
example app, and legacy csproj scaffolding"), under `Atdl4net/Wpf/`:

- `View/Controls/*` — concrete WPF controls (CheckBoxList, RadioButtonList,
  sliders, spinners, TimePicker, StrategyPanelFrame, ClickSelectTextBox).
- `View/DefaultRendering/*` — per-control-type renderers (one class per ATDL
  control type).
- `View/WpfStrategyPanelRenderer.cs`, `View/GridCoordinate.cs`,
  `View/WpfComboBoxSizer.cs` — the layout engine: positions controls into a
  grid per the ATDL layout spec, sizes combo boxes to content. This is the
  expensive-to-derive part.
- `ViewModel/*` — EditViewModel, ControlViewModel, ListControlViewModel,
  ListItemViewModel, InvalidatableControlViewModel,
  IInitialFixValueProvider.

## Approach

**Hybrid port** (chosen over full rewrite or verbatim wholesale import):

- **Port verbatim**, adapted only for compilation against current core APIs:
  the layout engine (`WpfStrategyPanelRenderer`, `GridCoordinate`,
  `WpfComboBoxSizer`), the control renderers, and the XAML/code-behind
  controls themselves. This is hard-won layout math and rendering logic with
  no reason to re-derive it.
- **Rewrite**: the `ViewModel/*` layer, replaced with
  `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`,
  `[RelayCommand]`) wired against the current core's `Strategy_t`/`Control_t`
  model (the core's public surface has changed since the original fork).
  Composition root (`App.xaml.cs` equivalent — here, a DI registration
  extension method since this is a library, not an app) is net-new; the
  original predates modern DI entirely.
- Extension-point interfaces (`IWpfControlRenderer`, `INamespaceProvider`)
  are kept where they still cleanly fit the new core's model; otherwise
  folded into DI-resolved renderer registrations.

Rejected alternatives:
- *Direct port, minimal changes*: would import whatever legacy
  architecture/DI-absence surrounded the renderers, contradicting the
  no-fudges goal.
- *Full rewrite from scratch*: throws away validated layout algorithms for
  no benefit: layout math doesn't change with core API changes.

## Architecture

- New repo `fixportal-fixatdl-wpf`. Scaffolded via `scaffold-dotnet` first
  (`.slnx`, `FixPortal.CodeStyle`, NodaTime, CSharpier, central package
  management), then `scaffold-desktop` for the WPF/MVVM/DI/navigation layer
  (see `~/.agents/skills/scaffold-desktop/SKILL.md` for the full mandate:
  CommunityToolkit.Mvvm, generic host with `StartAsync`/`StopAsync`
  lifecycle, `INavigationService` with per-navigation `IServiceScope`,
  DataTemplate-per-ViewModel registration, default WPF theme resources kept
  as generated).
- Solution structure (per scaffold-desktop's testability rule — a
  `net10.0` test project cannot reference a `net10.0-windows` WPF head
  directly):
  - `FixPortal.FixAtdl.Wpf` — the WPF class library: ported controls,
    renderers, and the layout engine. Targets `net10.0-windows`.
  - `FixPortal.FixAtdl.Wpf.Core` — plain `net10.0` class library holding the
    rewritten ViewModels and layout-coordinate/validation logic. Referenced
    by both the WPF head and the ViewModel test project — the WPF head
    itself cannot be referenced by a plain `net10.0` test project (MSBuild
    rejects the incompatible target framework), so ViewModels live here
    rather than in `FixPortal.FixAtdl.Wpf`.
  - `FixPortal.FixAtdl.Wpf.Core.Tests` — plain `net10.0` xUnit v3 project,
    ViewModel and layout-logic unit tests only (NSubstitute,
    AwesomeAssertions), zero WPF runtime dependency.
  - `FixPortal.FixAtdl.Wpf.Tests.UI` — STA-runtime smoke test project
    covering View/DataTemplate resolution, since WPF has no headless test
    harness (scaffold-desktop's documented gap: no DependencyProperty/
    RoutedEvent analyzer exists either — WpfAnalyzers is stale/rejected;
    known, not an oversight).
- Depends on `FixPortal.FixAtdl` (this repo's published core package) for
  parsing and the `Strategy_t` model — no core logic duplicated.

## Data flow

1. EMS obtains Strategy XML (from a venue/broker FIX Strategy list) and
   parses it via `FixPortal.FixAtdl`'s existing parser into a `Strategy_t`.
2. EMS calls the adapter's entry point (an `AtdlPanelViewModel` or
   equivalent, resolved via DI) with that `Strategy_t`.
3. Adapter builds the ViewModel tree (`EditViewModel` → per-control
   `ControlViewModel`s) and the layout engine renders the control grid.
4. User edits controls; two-way bindings update ViewModel state; validation
   runs per-control as values change.
5. On submit, EMS reads populated FIX tag/value pairs back from the
   ViewModel tree (via the existing core value-provider contract) to build
   the outbound order/IOI message.

## Error handling

- XML parse/model-construction errors (malformed Strategy XML, unresolvable
  control types) are core-library concerns already handled by
  `FixPortal.FixAtdl` — the adapter surfaces them as a construction-time
  exception EMS catches before attempting to render (can't render a panel
  for a Strategy that didn't parse).
- Per-control validation (constraint violations, required-field-empty) is
  ViewModel-level state, not an exception — surfaced via
  `INotifyDataErrorInfo` or CommunityToolkit.Mvvm's built-in validation
  attributes. EMS's submit action is gated on `!HasErrors` across the
  ViewModel tree; no exception crosses the EMS boundary for a user input
  error.

## Testing

- ViewModel and layout-logic unit tests: xUnit v3, NSubstitute,
  AwesomeAssertions, no WPF runtime — covers control-tree construction,
  validation logic, layout coordinate calculation in isolation.
- View/DataTemplate resolution: STA smoke test project (or documented manual
  verification, per scaffold-desktop, if an automated STA harness proves
  impractical — decide at implementation time).
- Verification gate: `CommunityToolkit.Mvvm` source-generator diagnostics +
  `FixPortal.CodeStyle`, both warnings-as-errors, per scaffold-desktop.

## Dependencies / out-of-band work

- EMS must be on (or be migrated to, as EMS-side work) generic host +
  CommunityToolkit.Mvvm to consume this adapter's DI registration cleanly —
  explicitly not this repo's problem to shim around.
- Parallel: `fixportal-fixatdl-react`, extracted from the simulator's
  existing full-fat ATDL workbench (Codex-owned, tracked separately). No OSS
  announcement until both this and the React extraction are complete.

## Open questions for implementation planning

- Whether the STA View-resolution smoke test is worth automating for this
  repo specifically, or whether manual verification is accepted here (per
  scaffold-desktop, this is a per-repo call).
