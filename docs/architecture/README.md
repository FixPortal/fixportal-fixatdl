# FixPortal.FixAtdl — Architecture

A navigable map of the library, derived from two whole-repo graph passes:

- **graphify** (`graphify-out/`) — agent-facing dependency graph: degree-based god
  nodes, betweenness bridges, community detection. Open `graphify-out/graph.html`
  or read `graphify-out/GRAPH_REPORT.md`.
- **understand-anything** (`src/.understand-anything/knowledge-graph.json`) —
  human-facing knowledge graph: 382 nodes, 536 edges, 9 layers, a 13-step guided
  tour. Explore it with `/understand-dashboard` (interactive) or
  `/understand-explain <file>`.

This document is the prose synthesis of both. For "where is X / what calls Y",
query the graphs rather than grepping.

## What this library is

A **headless** .NET 10 library that turns a FIXatdl v1.1 strategy XML document
into a navigable object model, validates user input against the document's edit
rules, and emits the resulting **FIX tag values**. It is *not* a FIX engine (the
host sends the tags) and *not* a UI library (consumers wire their own UI onto the
parsed model). Modernised fork of [Atdl4net](https://github.com/atdl4net/atdl4net).

## The pipeline (data flow)

The whole library is one pipeline from an XML stream to FIX tag values. The
guided tour walks it; this is the short form:

```
File/Stream
   │  StrategiesReader.Load(stream)            ← public entry point (XXE-hardened)
   ▼
ElementFactory (reflective XML→object engine)  ← god node, 22 methods
   │  driven by SchemaDefinitions + the ElementDefinition family
   ▼
Strategies_t → Strategy_t                       ← domain aggregate root
   ├── Parameters  : Parameter_t  (Type system: AtdlValueType / AtdlReferenceType)
   ├── StrategyPanel_t → Controls  (Control model: Control_t hierarchy)
   ├── StateRules  : StateRule_t   ┐
   └── StrategyEdits: StrategyEdit_t┘ → Edit_t  (Validation: EditEvaluator)
   │  Strategy_t.ResolveAll()  resolves EditRef→Edit, control↔parameter
   ▼
Parameter_t.WireValue set by host → GetOutputValues()
   ▼
FixTagValuesCollection → FixTag / FixField      ← FIX emission (the output)
```

Two facts make this design what it is:

1. **The parser is data-driven, not hand-written.** `ElementFactory` knows
   nothing about FIXatdl specifically — it reflects over `SchemaDefinitions`
   (a static table of `ElementDefinition`s) to map any XML element to a C# type,
   set constructor parameters, and recurse into children. Adding a new element
   type is a table edit, not a parser change. This is the single most
   load-bearing seam in the codebase (see *Load-bearing components*).
2. **Construction is two-phase.** Deserialization builds the object tree; a
   second `ResolveAll()` pass wires cross-references (`EditRef_t` → `Edit_t`,
   controls ↔ parameters) once every node exists. `StrategiesReader.StrategyLoaded`
   fires *after* deserialization but *before* full validation — treat it as
   "deserialized", not "valid".

## Layers

The understand-anything pass assigned all 180 file-level nodes to 9 layers:

| Layer | Files | What lives here |
|---|---:|---|
| **XML Ingestion** | 20 | `StrategiesReader` (entry point), `ElementFactory`, the `ElementDefinition` family, `SchemaDefinitions`, `ValueConverter`, `AtdlNamespaces` — the data-driven deserialization engine. |
| **Domain Model** | 52 | `Strategies_t`/`Strategy_t`/`Parameter_t`/`StrategyPanel_t`/`StateRule_t`, the typed **Collections**, **Enumerations**, and ISO **Reference** data. The parsed strategy as objects. |
| **Control Model** | 26 | `Control_t` → `InitializableControl` → {`Binary`/`List`/`Numeric`/`Text`ControlBase} and the concrete `*_t` controls; `EnumState`, `IControlVisitor`. The UI-control abstraction (UI-agnostic). |
| **Parameter Type System** | 41 | `AtdlValueType` / `AtdlReferenceType` roots, `EnumTypeBase`, `DateTimeTypeBase`, integer bases, and every concrete `*_t` value type. Each XML `xsi:type` → a C# class that parses/formats its FIX wire value. |
| **Validation** | 5 | `EditEvaluator`, `EditValueConverter`, `ValidationResult`, `ControlValidationState`, `IValueProvider` — the edit-rule engine. |
| **FIX Emission** | 9 | `FixTagValuesCollection`, `FixTag`, `FixField` (generated tag constants), `FixFieldValueProvider`, `FixMessage` — the library's output. |
| **Diagnostics** | 19 | `FixAtdlException` hierarchy (9 subclasses), `ThrowHelper` (reflective exception factory), `ExceptionInfo`, the localized `Resources`. |
| **Utility** | 6 | `IParentable`, `IResolvable`, `ModelUtils`, `StringExtensions`, `ProcessExtensions`, `DataEntryMode`. |
| **Configuration** | 2 | `FixAtdlOptions` POCO (replaces upstream `System.Configuration`). |

`Domain Model` and `Control Model` are kept separate deliberately: the
`Model/Controls` subtree is its own abstract base-class hierarchy, architecturally
independent of the `Elements`/`Collections` subtree.

## Two parallel type hierarchies

The library has **two** independent inheritance trees. Confusing them is the most
common mistake, so they are documented side by side.

### Parameter value types — "what FIX wire value does this carry?"

```
IParameterType / IControlConvertible
   └── AtdlValueType<T>            AtdlReferenceType<T>
         ├── EnumTypeBase                 ├── String_t ── Exchange_t, MultipleChar/StringValue_t
         │     └── Country_t/Currency_t/Language_t      └── Data_t
         ├── DateTimeTypeBase ── UTCDateTimeTypeBase ── UTC* / TZ* / LocalMktDate_t
         ├── NonNegativeIntegerTypeBase ── NonZeroPositiveIntegerTypeBase ── TagNum_t
         ├── Int_t ── Length_t, NumInGroup_t, SeqNum_t
         ├── Float_t ── Amt_t, Price_t, PriceOffset_t, Qty_t, Percentage_t
         └── Boolean_t, Char_t, MonthYear_t, Tenor_t
```

god nodes here: `AtdlValueType` (root, degree 24), `DateTimeTypeBase` (23),
`Float_t` (21). Thin leaf types (`Amt_t`, `Price_t`…) are one-liners that only fix
the base type's generic parameter.

### UI controls — "how does a user enter this?"

```
Control_t  (carries Id/Label/ParameterRef/InitPolicy/StateRules)
   └── InitializableControl  (adds InitValue / LoadInitValue)
         ├── BinaryControlBase   ── CheckBox_t, RadioButton_t …
         ├── ListControlBase     ── DropDownList_t, MultiSelectList_t, Slider_t …  (uses EnumState)
         ├── NumericControlBase  ── SingleSpinner_t, DoubleSpinner_t
         └── TextControlBase     ── TextField_t, HiddenField_t, Label_t
```

god nodes here: `ListControlBase` (degree 24), `Control_t` (23),
`BinaryControlBase` (21), `EnumState` (19, a `BitArray`-backed multi-select
tracker). `IParameterConvertible` / `IControlConvertible` are the bridge between
a control's entered value and the parameter's wire value.

## Load-bearing components

Verified against the graph (degree from graphify, role confirmed via
`graphify explain` / the analyzer pass):

- **`ElementFactory`** (`Xml/Serialization/ElementFactory.cs`) — the reflective
  deserialization engine, 22 methods. Every parsed object passes through it. The
  one place to understand before changing how XML maps to the model.
- **`IParentable`** (`Utility/IParentable.cs`) — **the top betweenness bridge**
  (0.106). The parent/child tree contract implemented by `Control_t`,
  `Strategy_t`, `StrategyPanel_t`, `StateRule_t`, and `ReadOnlyControlCollection`.
  It is what lets any node walk up to its owning strategy; it ties the otherwise
  separate model subtrees into one tree. Paired with `IResolvable` for
  cross-reference resolution in `ResolveAll()`.
- **`ReadOnlyControlCollection`** (degree 23) — the runtime coordinator for a
  strategy's controls (implements 3 interfaces, 18 methods).
- **`Edit_t`** (degree 20, 568 lines) — dual-purpose: a parsed data record *and*
  a runtime rule evaluator. The validation layer's core. AND/OR/XOR/NOT grammar
  lives in `EditEvaluatingCollection`, shared by both `StateRule_t` (UI state) and
  `StrategyEdit_t` (business rules).
- **`ThrowHelper`** (`Diagnostics/`) — the sole exception factory. Callers never
  `new` an exception directly; `ThrowHelper` constructs the right `FixAtdlException`
  subclass reflectively with a localized message from `ErrorMessages.resx`.

## The NodaTime boundary

Date/time is split deliberately, matching the project's NodaTime-at-the-edge
policy:

- **Domain side uses NodaTime.** `Clock_t` injects `IClock` /
  `IDateTimeZoneProvider` (testable — no static `now`); `MonthYear` and `Tenor`
  are NodaTime-backed value structs; `DateTimeTypeBase` and `InitValueClock` parse
  via NodaTime patterns.
- **FIX wire side uses BCL.** `FixDateTime` / `FixDateTimeFormat` parse raw FIX
  timestamp strings with BCL `DateTime` at the I/O boundary, then convert inward.

## Security note

`StrategiesReader` hardens against XXE: the shared `XmlReaderSettings` sets
`DtdProcessing = Prohibit` and `XmlResolver = null`. Both `Load` overloads use it.
Preserve this when touching XML ingestion.

## How to navigate further

| Question | Tool |
|---|---|
| "Where is X / what calls Y / what inherits Z" | `graphify query "..."` or `graphify explain "X"` |
| Visual walk of the architecture | `/understand-dashboard` then the 13-step tour |
| Deep-dive one file | `/understand-explain <path>` |
| God nodes / bridges / cohesion | `graphify-out/GRAPH_REPORT.md` |

Both graphs are whole-repo and rebuildable: `graphify --update` (agent graph)
and `/understand` (human graph) after code changes. Keep this doc in sync when a
layer boundary or load-bearing seam actually moves — not on every commit.
