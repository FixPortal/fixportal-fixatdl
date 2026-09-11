# FixPortal.FixAtdl.Wpf Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `FixPortal.FixAtdl.Wpf`, a new repo/package providing a full-fat
dynamic-form WPF control library for ATDL strategy editing, for EMS to consume.

**Architecture:** Port the pre-strip WPF layout/rendering engine verbatim from
`fixportal-fixatdl` commit `23c877a^` into a new repo scaffolded via
`scaffold-dotnet` + `scaffold-desktop`. Rewrite the ViewModel and
extension-point (renderer registration) layers against
`CommunityToolkit.Mvvm` and a generic-host DI container, replacing the
original's MEF-based composition (`System.ComponentModel.Composition`)
entirely — no legacy composition model carried forward.

**Tech Stack:** .NET 10, WPF (`net10.0-windows`), `CommunityToolkit.Mvvm`,
`Microsoft.Extensions.Hosting`, `FixPortal.FixAtdl` (core package), xUnit v3,
NSubstitute, AwesomeAssertions, `FixPortal.CodeStyle`, NodaTime, CSharpier.

**Spec:** `docs/superpowers/specs/2026-09-11-fixatdl-wpf-adapter-design.md`

## Global Constraints

- Central package management; pin `CommunityToolkit.Mvvm` and
  `Microsoft.Extensions.Hosting` to current stable versions (query at
  scaffold time, do not guess a version number).
- `dotnet csharpier check .` and build must pass warnings-as-errors
  (`FixPortal.CodeStyle` + `CommunityToolkit.Mvvm` generator diagnostics)
  before any task is considered done.
- ViewModels and layout/coordinate logic live in the plain `net10.0`
  `FixPortal.FixAtdl.Wpf` project only where they have zero WPF-runtime
  dependency; anything requiring `System.Windows.*` types stays in the
  `net10.0-windows` head. (Decided per-file in Task 3/4 — see notes there.)
- No MEF (`System.ComponentModel.Composition`) anywhere in the new code —
  all extension points resolved via `Microsoft.Extensions.DependencyInjection`.
- Source repo for porting: `fixportal-fixatdl`, commit `23c877a^`, path
  `Atdl4net/Wpf/`.

---

### Task 1: Scaffold the new repo

**Files:**
- Create: new repo `fixportal-fixatdl-wpf` (location: sibling to
  `fixportal-fixatdl`, i.e. `D:\fix-portal\fixportal-fixatdl-wpf`)

**Interfaces:**
- Produces: a buildable, empty `.slnx` solution with CodeStyle/CPM/NodaTime/
  CSharpier wired, plus a WPF project head and a plain class library, per
  scaffold-desktop's two-project split.

- [ ] **Step 1: Create the GitHub repo and clone it**

Follow `scaffold-repo` for repo creation (org settings, branch ruleset, root
files). Repo name: `fixportal-fixatdl-wpf`. Clone to
`D:\fix-portal\fixportal-fixatdl-wpf`.

- [ ] **Step 2: Apply scaffold-dotnet for the non-UI baseline**

Follow `~/.agents/skills/scaffold-dotnet/SKILL.md` to produce `.slnx`,
`Directory.Packages.props` (CPM), `FixPortal.CodeStyle` reference,
`.editorconfig`, CSharpier local tool.

- [ ] **Step 3: Apply scaffold-desktop for the WPF layer**

Follow `~/.agents/skills/scaffold-desktop/SKILL.md`. Produces:
- `src/FixPortal.FixAtdl.Wpf/` — `net10.0-windows` WPF class library
  (App.xaml stripped down to a resource dictionary host, no app shell — this
  is a library, not an app; confirm with scaffold-desktop's guidance on
  library vs. app template, adjust `OutputType` accordingly).
- `src/FixPortal.FixAtdl.Wpf.Core/` — plain `net10.0` class library for
  ViewModels and non-WPF logic (layout coordinate math, validation).
- `tests/FixPortal.FixAtdl.Wpf.Core.Tests/` — xUnit v3 project referencing
  `FixPortal.FixAtdl.Wpf.Core`.
- `tests/FixPortal.FixAtdl.Wpf.Tests.UI/` — STA smoke test project (per
  scaffold-desktop's WPF testability guidance).

- [ ] **Step 4: Verify empty solution builds and formats clean**

Run: `dotnet tool restore`
Run: `dotnet csharpier check .`
Expected: PASS (no files to format yet, or scaffold templates already
compliant)
Run: `dotnet restore FixPortal.FixAtdl.Wpf.slnx`
Run: `dotnet build FixPortal.FixAtdl.Wpf.slnx --configuration Release`
Expected: build succeeds, 0 warnings

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: scaffold fixportal-fixatdl-wpf via scaffold-dotnet + scaffold-desktop"
```

---

### Task 2: Reference the core ATDL package

**Files:**
- Modify: `src/FixPortal.FixAtdl.Wpf.Core/FixPortal.FixAtdl.Wpf.Core.csproj`
- Modify: `Directory.Packages.props`

**Interfaces:**
- Consumes: `FixPortal.FixAtdl` package (published from `fixportal-fixatdl`,
  current stable version — query the feed, do not guess) — specifically its
  public `Strategy_t`, `Control_t`, and value-provider types used to
  construct and read back a strategy's control tree.
- Produces: `FixPortal.FixAtdl.Wpf.Core` project has a working reference to
  `FixPortal.FixAtdl`, confirmed by a passing smoke test that constructs a
  `Strategy_t` from a minimal ATDL XML fixture.

- [ ] **Step 1: Add the package reference**

Add `<PackageVersion Include="FixPortal.FixAtdl" Version="X.Y.Z" />` to
`Directory.Packages.props` (query current published version first via
`gh api` or the package feed — do not assume 1.0.0 is still latest at
implementation time). Add `<PackageReference Include="FixPortal.FixAtdl" />`
to `FixPortal.FixAtdl.Wpf.Core.csproj`.

- [ ] **Step 2: Write the failing smoke test**

Create `tests/FixPortal.FixAtdl.Wpf.Core.Tests/CoreReferenceTests.cs`:

```csharp
using AwesomeAssertions;
using FixPortal.FixAtdl.Model.Elements;
using Xunit;

namespace FixPortal.FixAtdl.Wpf.Core.Tests;

public class CoreReferenceTests
{
    [Fact]
    public void CanReferenceStrategyType()
    {
        var strategyType = typeof(Strategy_t);

        strategyType.Should().NotBeNull();
    }
}
```

(Adjust the namespace/type name `Strategy_t` to match the actual public type
name exposed by `FixPortal.FixAtdl` — confirm via
`dotnet-tool` or by inspecting the package's public API before writing this
test; this repo's own core types are the source of truth, not this plan.)

- [ ] **Step 2b: Run test to verify it fails**

Run: `dotnet test tests/FixPortal.FixAtdl.Wpf.Core.Tests --filter CanReferenceStrategyType`
Expected: FAIL (compile error — package not yet referenced) until Step 1 is
also in place; once Step 1 and Step 2 are both done, run once to confirm
green (this is a reference-wiring check, not new behavior, so red-then-green
is a single restore/build cycle here rather than a logic change).

- [ ] **Step 3: Run test to verify it passes**

Run: `dotnet test tests/FixPortal.FixAtdl.Wpf.Core.Tests --filter CanReferenceStrategyType`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add Directory.Packages.props src/FixPortal.FixAtdl.Wpf.Core tests/FixPortal.FixAtdl.Wpf.Core.Tests
git commit -m "feat: reference FixPortal.FixAtdl core package"
```

---

### Task 3: Port the layout engine and control renderers verbatim

**Files:**
- Create: `src/FixPortal.FixAtdl.Wpf/Rendering/GridCoordinate.cs` (ported
  from `Atdl4net/Wpf/View/GridCoordinate.cs` at `23c877a^`)
- Create: `src/FixPortal.FixAtdl.Wpf/Rendering/WpfComboBoxSizer.cs` (ported
  from `Atdl4net/Wpf/View/WpfComboBoxSizer.cs`)
- Create: `src/FixPortal.FixAtdl.Wpf/Rendering/StrategyPanelRenderer.cs`
  (ported from `Atdl4net/Wpf/View/WpfStrategyPanelRenderer.cs`, MEF import
  attributes and `CompositionContainer` usage stripped — replaced by
  constructor-injected `IEnumerable<IControlRenderer>` in Task 5)
- Create: `src/FixPortal.FixAtdl.Wpf/Rendering/DefaultRendering/*.cs`
  (ported from `Atdl4net/Wpf/View/DefaultRendering/*.cs`, one file per
  control-type renderer, `[Export]` MEF attributes removed)
- Create: `src/FixPortal.FixAtdl.Wpf/Controls/*.cs` and `*.xaml`(+`.xaml.cs`)
  (ported from `Atdl4net/Wpf/View/Controls/*`)

**Interfaces:**
- Consumes: `FixPortal.FixAtdl` model types (from Task 2) in place of the
  original's `Atdl4net.Model.Elements` namespace — every ported file's
  `using Atdl4net.Model.*` becomes `using FixPortal.FixAtdl.Model.*` (verify
  exact namespace from the referenced package, adjust to match).
- Produces: `IControlRenderer` interface (renamed from the original
  `IWpfControlRenderer` for clarity — confirm no external consumer depends
  on the old name, none exists yet) with signature
  `FrameworkElement Render(Control_t control, IServiceProvider services)`;
  `StrategyPanelRenderer.Render(Strategy_t strategy, IServiceProvider
  services) : FrameworkElement` as the single public entry point later tasks
  call.

- [ ] **Step 1: Extract the source files**

For each file listed above, run:
`git -C D:\fix-portal\fixportal-fixatdl show 23c877a^:Atdl4net/Wpf/View/<relative-path>`
and write the output to the corresponding new path under
`src/FixPortal.FixAtdl.Wpf/`. Do this for every file under `View/Controls`,
`View/DefaultRendering`, `View/GridCoordinate.cs`,
`View/WpfComboBoxSizer.cs`, `View/WpfStrategyPanelRenderer.cs`.

- [ ] **Step 2: Strip MEF, fix namespaces**

In every extracted file:
- Remove `using System.ComponentModel.Composition;` and
  `using System.ComponentModel.Composition.Hosting;`.
- Remove `[Export]`, `[ImportMany]`, `[ImportingConstructor]` attributes.
- Replace `using Atdl4net.*` with the equivalent `using FixPortal.FixAtdl.*`
  namespace (confirm exact mapping against the core package's actual
  namespaces — do not assume a 1:1 rename without checking).
- Rename the namespace declaration `Atdl4net.Wpf.View` →
  `FixPortal.FixAtdl.Wpf.Rendering` (and `Atdl4net.Wpf.View.Controls` →
  `FixPortal.FixAtdl.Wpf.Controls`, etc.) throughout.

- [ ] **Step 3: Replace MEF composition in StrategyPanelRenderer with constructor injection**

Change `WpfStrategyPanelRenderer` from a `static class` with an internal
`CompositionContainer` to an instance class:

```csharp
namespace FixPortal.FixAtdl.Wpf.Rendering;

public sealed class StrategyPanelRenderer(IEnumerable<IControlRenderer> renderers)
{
    private readonly IReadOnlyDictionary<Type, IControlRenderer> _renderersByControlType =
        renderers.ToDictionary(r => r.ControlType);

    public FrameworkElement Render(Strategy_t strategy, IServiceProvider services)
    {
        // body ported from the original Render/BuildPanel logic (Step 1
        // extraction), with the old `_container.GetExportedValue<T>()`
        // calls replaced by `_renderersByControlType[controlType]` lookups.
    }
}
```

(Full method bodies come from the Step 1 extraction — port the actual logic,
only the composition-resolution calls change shape.)

- [ ] **Step 4: Register renderers for DI and build**

Add `[ControlType]`-equivalent metadata to each renderer class (a simple
`ControlType` property implementing `IControlRenderer`, replacing the MEF
metadata attribute) so `StrategyPanelRenderer`'s constructor dictionary
build (Step 3) works.

Run: `dotnet build FixPortal.FixAtdl.Wpf.slnx --configuration Release`
Expected: 0 errors (namespace/type mismatches from Step 2 surface here —
fix each compile error against the actual core package API).

- [ ] **Step 5: Commit**

```bash
git add src/FixPortal.FixAtdl.Wpf
git commit -m "feat: port WPF layout engine and control renderers from pre-strip history"
```

---

### Task 4: Rewrite the ViewModel layer with CommunityToolkit.Mvvm

**Files:**
- Create: `src/FixPortal.FixAtdl.Wpf.Core/ViewModels/ControlViewModel.cs`
- Create: `src/FixPortal.FixAtdl.Wpf.Core/ViewModels/EditViewModel.cs`
- Create: `src/FixPortal.FixAtdl.Wpf.Core/ViewModels/ListControlViewModel.cs`
- Create: `src/FixPortal.FixAtdl.Wpf.Core/ViewModels/ListItemViewModel.cs`
- Test: `tests/FixPortal.FixAtdl.Wpf.Core.Tests/ViewModels/ControlViewModelTests.cs`
- Test: `tests/FixPortal.FixAtdl.Wpf.Core.Tests/ViewModels/EditViewModelTests.cs`

**Interfaces:**
- Consumes: `Control_t`/`Strategy_t` from `FixPortal.FixAtdl` (Task 2).
- Produces: `ControlViewModel : ObservableValidator` with
  `[ObservableProperty] object? Value` and `INotifyDataErrorInfo` validation
  driven by the control's ATDL constraints; `EditViewModel` exposing
  `ObservableCollection<ControlViewModel> Controls` and
  `bool HasErrors => Controls.Any(c => c.HasErrors)` — this is what Task 6's
  submit-gating and Task 7's error handling both read.

- [ ] **Step 1: Write the failing test for ControlViewModel validation**

```csharp
using AwesomeAssertions;
using FixPortal.FixAtdl.Wpf.Core.ViewModels;
using Xunit;

namespace FixPortal.FixAtdl.Wpf.Core.Tests.ViewModels;

public class ControlViewModelTests
{
    [Fact]
    public void SettingValueOutsideConstraint_SetsHasErrorsTrue()
    {
        var control = TestControls.RequiredNumericControl(); // helper building
                                                               // a Control_t with
                                                               // a required, min=0
                                                               // constraint
        var viewModel = new ControlViewModel(control);

        viewModel.Value = -1;

        viewModel.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void SettingValidValue_ClearsErrors()
    {
        var control = TestControls.RequiredNumericControl();
        var viewModel = new ControlViewModel(control) { Value = -1 };

        viewModel.Value = 5;

        viewModel.HasErrors.Should().BeFalse();
    }
}
```

Add a `TestControls` fixture helper in
`tests/FixPortal.FixAtdl.Wpf.Core.Tests/TestControls.cs` that builds a
minimal `Control_t` via the core package's actual construction API (inspect
`FixPortal.FixAtdl`'s public constructors/builders — this plan cannot
predict their exact shape without reading that package's current public
surface at implementation time).

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FixPortal.FixAtdl.Wpf.Core.Tests --filter ControlViewModelTests`
Expected: FAIL (`ControlViewModel` does not exist)

- [ ] **Step 3: Implement ControlViewModel**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using FixPortal.FixAtdl.Model.Elements;

namespace FixPortal.FixAtdl.Wpf.Core.ViewModels;

public partial class ControlViewModel : ObservableValidator
{
    private readonly Control_t _control;

    public ControlViewModel(Control_t control)
    {
        _control = control;
        _value = control.InitialValue;
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    private object? _value;

    partial void OnValueChanged(object? value)
    {
        ValidateProperty(value, nameof(Value));
    }
}
```

(The actual constraint-to-validation-attribute mapping — required, min/max,
enum membership — is ported logic from the original `InvalidatableControlViewModel`
at `23c877a^:Atdl4net/Wpf/ViewModel/InvalidatableControlViewModel.cs`; read
that file's validation rules and translate them into
`ValidationAttribute`-derived checks or explicit `AddError`/`ClearErrors`
calls here rather than re-deriving the rules from the ATDL spec from
scratch.)

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FixPortal.FixAtdl.Wpf.Core.Tests --filter ControlViewModelTests`
Expected: PASS

- [ ] **Step 5: Write the failing test for EditViewModel aggregate HasErrors**

```csharp
[Fact]
public void HasErrors_TrueWhenAnyChildControlInvalid()
{
    var strategy = TestControls.MinimalStrategyWithOneRequiredControl();
    var viewModel = new EditViewModel(strategy);

    viewModel.Controls[0].Value = null; // required control left empty

    viewModel.HasErrors.Should().BeTrue();
}
```

- [ ] **Step 6: Run test to verify it fails, then implement EditViewModel**

Run: `dotnet test tests/FixPortal.FixAtdl.Wpf.Core.Tests --filter EditViewModelTests`
Expected: FAIL (`EditViewModel` does not exist)

```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FixPortal.FixAtdl.Model.Elements;

namespace FixPortal.FixAtdl.Wpf.Core.ViewModels;

public partial class EditViewModel : ObservableObject
{
    public EditViewModel(Strategy_t strategy)
    {
        Controls = new ObservableCollection<ControlViewModel>(
            strategy.Controls.Select(c => new ControlViewModel(c)));

        foreach (var control in Controls)
        {
            control.ErrorsChanged += (_, _) => OnPropertyChanged(nameof(HasErrors));
        }
    }

    public ObservableCollection<ControlViewModel> Controls { get; }

    public bool HasErrors => Controls.Any(c => c.HasErrors);
}
```

- [ ] **Step 7: Run test to verify it passes**

Run: `dotnet test tests/FixPortal.FixAtdl.Wpf.Core.Tests --filter EditViewModelTests`
Expected: PASS

- [ ] **Step 8: Port ListControlViewModel and ListItemViewModel**

Extract logic from `23c877a^:Atdl4net/Wpf/ViewModel/ListControlViewModel.cs`
and `ListItemViewModel.cs`, translate to `ObservableObject` +
`[ObservableProperty]`/`[RelayCommand]` following the same pattern as Steps
1-7 (write failing test for list-item selection/multi-select behavior first,
per the same control-constraint source as the original).

- [ ] **Step 9: Commit**

```bash
git add src/FixPortal.FixAtdl.Wpf.Core tests/FixPortal.FixAtdl.Wpf.Core.Tests
git commit -m "feat: rewrite ViewModel layer on CommunityToolkit.Mvvm"
```

---

### Task 5: DI composition and renderer registration

**Files:**
- Create: `src/FixPortal.FixAtdl.Wpf/ServiceCollectionExtensions.cs`
- Test: `tests/FixPortal.FixAtdl.Wpf.Tests.UI/ServiceCollectionExtensionsTests.cs`

**Interfaces:**
- Consumes: `IControlRenderer` implementations from Task 3,
  `StrategyPanelRenderer` from Task 3.
- Produces: `IServiceCollection AddFixAtdlWpf(this IServiceCollection
  services)` — the single call EMS's composition root (`App.xaml.cs`) makes
  to register everything this library needs. Later EMS integration depends
  on this exact method name and signature.

- [ ] **Step 1: Write the failing test**

```csharp
using FixPortal.FixAtdl.Wpf.Rendering;
using Microsoft.Extensions.DependencyInjection;
using AwesomeAssertions;
using Xunit;

namespace FixPortal.FixAtdl.Wpf.Tests.UI;

[Collection("STA")]
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFixAtdlWpf_RegistersStrategyPanelRenderer()
    {
        var services = new ServiceCollection();

        services.AddFixAtdlWpf();
        var provider = services.BuildServiceProvider();

        provider.GetService<StrategyPanelRenderer>().Should().NotBeNull();
    }

    [Fact]
    public void AddFixAtdlWpf_RegistersAllDefaultControlRenderers()
    {
        var services = new ServiceCollection();

        services.AddFixAtdlWpf();
        var provider = services.BuildServiceProvider();

        provider.GetServices<IControlRenderer>().Should().NotBeEmpty();
    }
}
```

(`[Collection("STA")]` and its STA-thread test collection fixture are set up
by scaffold-desktop's WPF test-project template from Task 1, Step 3 — reuse
it, don't redefine it here.)

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FixPortal.FixAtdl.Wpf.Tests.UI --filter ServiceCollectionExtensionsTests`
Expected: FAIL (`AddFixAtdlWpf` does not exist)

- [ ] **Step 3: Implement the extension method**

```csharp
using FixPortal.FixAtdl.Wpf.Rendering;
using FixPortal.FixAtdl.Wpf.Rendering.DefaultRendering;
using Microsoft.Extensions.DependencyInjection;

namespace FixPortal.FixAtdl.Wpf;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFixAtdlWpf(this IServiceCollection services)
    {
        services.AddTransient<StrategyPanelRenderer>();

        services.AddTransient<IControlRenderer, CheckBoxRenderer>();
        services.AddTransient<IControlRenderer, CheckBoxListRenderer>();
        services.AddTransient<IControlRenderer, ClockRenderer>();
        services.AddTransient<IControlRenderer, DoubleSpinnerRenderer>();
        services.AddTransient<IControlRenderer, DropDownListRenderer>();
        services.AddTransient<IControlRenderer, EditableDropDownListRenderer>();
        services.AddTransient<IControlRenderer, HiddenFieldRenderer>();
        services.AddTransient<IControlRenderer, LabelRenderer>();
        services.AddTransient<IControlRenderer, MultiSelectListRenderer>();
        services.AddTransient<IControlRenderer, RadioButtonRenderer>();
        services.AddTransient<IControlRenderer, RadioButtonListRenderer>();
        services.AddTransient<IControlRenderer, SingleSelectListRenderer>();
        services.AddTransient<IControlRenderer, SingleSpinnerRenderer>();
        services.AddTransient<IControlRenderer, SliderRenderer>();
        services.AddTransient<IControlRenderer, TextFieldRenderer>();

        return services;
    }
}
```

(Renderer class list comes from the `View/DefaultRendering/*.cs` files
ported in Task 3 — confirm this list matches exactly what Task 3 actually
produced, including any renamed classes.)

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FixPortal.FixAtdl.Wpf.Tests.UI --filter ServiceCollectionExtensionsTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/FixPortal.FixAtdl.Wpf/ServiceCollectionExtensions.cs tests/FixPortal.FixAtdl.Wpf.Tests.UI
git commit -m "feat: add AddFixAtdlWpf DI registration entry point"
```

---

### Task 6: Public entry point — build panel from Strategy, read back values

**Files:**
- Create: `src/FixPortal.FixAtdl.Wpf/AtdlPanel.cs`
- Test: `tests/FixPortal.FixAtdl.Wpf.Tests.UI/AtdlPanelTests.cs`

**Interfaces:**
- Consumes: `StrategyPanelRenderer` (Task 3), `EditViewModel` (Task 4).
- Produces: `AtdlPanel.Create(Strategy_t strategy, IServiceProvider
  services) : (FrameworkElement View, EditViewModel ViewModel)` — this is
  what EMS calls to get both the renderable panel and the ViewModel it reads
  submit-time values and `HasErrors` from. `EditViewModel.ReadBackFixValues()
  : IReadOnlyDictionary<int, string>` — tag-number-keyed FIX values for the
  outbound message, per the spec's data-flow step 5.

- [ ] **Step 1: Write the failing test**

```csharp
[Collection("STA")]
public class AtdlPanelTests
{
    [Fact]
    public void Create_ReturnsViewAndViewModelForStrategy()
    {
        var strategy = TestStrategies.MinimalOneControlStrategy();
        var services = new ServiceCollection().AddFixAtdlWpf().BuildServiceProvider();

        var (view, viewModel) = AtdlPanel.Create(strategy, services);

        view.Should().NotBeNull();
        viewModel.Controls.Should().HaveCount(1);
    }

    [Fact]
    public void ReadBackFixValues_ReturnsEditedValueKeyedByTag()
    {
        var strategy = TestStrategies.MinimalOneControlStrategy(); // control's FIX tag = 6218, say
        var services = new ServiceCollection().AddFixAtdlWpf().BuildServiceProvider();
        var (_, viewModel) = AtdlPanel.Create(strategy, services);

        viewModel.Controls[0].Value = 12.5m;
        var values = viewModel.ReadBackFixValues();

        values.Should().ContainKey(6218).WhoseValue.Should().Be("12.5");
    }
}
```

(`TestStrategies.MinimalOneControlStrategy()` is a shared fixture — add it
alongside `TestControls` in the test project; its exact FIX tag number is
whatever a trivial single-`DoubleField_t`-control ATDL XML fixture produces
via the core package's parser.)

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/FixPortal.FixAtdl.Wpf.Tests.UI --filter AtdlPanelTests`
Expected: FAIL (`AtdlPanel` does not exist)

- [ ] **Step 3: Implement AtdlPanel**

```csharp
using System.Windows;
using FixPortal.FixAtdl.Model.Elements;
using FixPortal.FixAtdl.Wpf.Core.ViewModels;
using FixPortal.FixAtdl.Wpf.Rendering;

namespace FixPortal.FixAtdl.Wpf;

public static class AtdlPanel
{
    public static (FrameworkElement View, EditViewModel ViewModel) Create(
        Strategy_t strategy, IServiceProvider services)
    {
        var viewModel = new EditViewModel(strategy);
        var renderer = services.GetRequiredService<StrategyPanelRenderer>();
        var view = renderer.Render(strategy, services);
        view.DataContext = viewModel;

        return (view, viewModel);
    }
}
```

Add `ReadBackFixValues()` to `EditViewModel` (Task 4's class), reading each
`ControlViewModel`'s `Value` and its underlying `Control_t`'s FIX tag
number — port this mapping logic from the original
`IInitialFixValueProvider`/value-write-back code at
`23c877a^:Atdl4net/Wpf/ViewModel/IInitialFixValueProvider.cs` rather than
re-deriving the tag-to-string formatting rules.

```csharp
public IReadOnlyDictionary<int, string> ReadBackFixValues() =>
    Controls
        .Where(c => c.Value is not null)
        .ToDictionary(c => c.FixTag, c => c.Value!.ToString()!);
```

(`ControlViewModel.FixTag` needs adding as a property sourced from
`Control_t` in Task 4 if not already present — add it there, re-run Task 4's
tests to confirm no regression, before wiring it here.)

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/FixPortal.FixAtdl.Wpf.Tests.UI --filter AtdlPanelTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/FixPortal.FixAtdl.Wpf/AtdlPanel.cs src/FixPortal.FixAtdl.Wpf.Core/ViewModels/EditViewModel.cs tests/FixPortal.FixAtdl.Wpf.Tests.UI
git commit -m "feat: add AtdlPanel entry point with FIX value read-back"
```

---

### Task 7: Full-suite verification gate

**Files:**
- None created — this task runs the whole repo's gate and fixes anything
  red.

**Interfaces:**
- Consumes: everything from Tasks 1-6.
- Produces: a green repo ready for EMS to start consuming.

- [ ] **Step 1: Run CSharpier**

Run: `dotnet csharpier check .`
Expected: PASS. If not, run `dotnet csharpier format .`, review the diff,
re-run check.

- [ ] **Step 2: Full restore and build**

Run: `dotnet restore FixPortal.FixAtdl.Wpf.slnx`
Run: `dotnet build FixPortal.FixAtdl.Wpf.slnx --configuration Release --no-restore`
Expected: 0 warnings (CodeStyle + CommunityToolkit.Mvvm generator
diagnostics both warnings-as-errors per Global Constraints)

- [ ] **Step 3: Full test run**

Run: `dotnet test FixPortal.FixAtdl.Wpf.slnx --configuration Release --no-build`
Expected: all tests pass across
`FixPortal.FixAtdl.Wpf.Core.Tests` and `FixPortal.FixAtdl.Wpf.Tests.UI`

- [ ] **Step 4: Commit any formatting/warning fixes**

```bash
git add -A
git commit -m "chore: clean CSharpier and analyzer gate"
```

(Skip this commit if Steps 1-3 were already clean — nothing to commit.)

---

## Self-review notes

- Spec coverage: architecture (Tasks 1, 3, 5), port strategy (Task 3),
  ViewModel rewrite (Task 4), testability split (Task 1 scaffold, enforced
  throughout), EMS integration surface (Task 5's `AddFixAtdlWpf`, Task 6's
  `AtdlPanel`), data flow (Task 6), error handling (Task 4's
  `ObservableValidator`/`HasErrors`, read by Task 6's caller contract) are
  each covered by a task above.
- Not covered by this plan (explicitly out of scope per spec): the EMS-side
  consuming code itself, and the parallel React extraction — both spec
  non-goals.
- Several steps (Task 4 Step 3, Task 6 Step 3) depend on reading the actual
  public API shape of `FixPortal.FixAtdl` and the exact original
  `23c877a^` ViewModel logic at implementation time — flagged inline rather
  than guessed, since guessing a wrong signature here would silently diverge
  from the real core package.
