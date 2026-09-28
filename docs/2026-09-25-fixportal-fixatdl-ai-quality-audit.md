---
project: fixportal-fixatdl
review-type: ai-quality-audit
run-id: 20260925T213323Z
date: 2026-09-25
commit: 71b139c184081e0d418d1782782fb61ea5ddb77f
corpus: ai-code-quality v5
corpus-age-days: 7.1
seats-reporting: 3 of 3
findings: 54
unclaimed-findings: 18
anchors-checked: 54
anchors-marked: 5
disposition: reported
tags:
  - audit/ai-quality
  - project/fixportal-fixatdl
---

# fixportal-fixatdl - AI quality audit

3 models audited `fixportal-fixatdl` at commit `71b139c18408` against 15 specific published criticisms of AI-written code, taken from corpus `ai-code-quality` v5. For each criticism the question was simply: does this repository do the thing the criticism describes?

## Verdict

No overall verdict: 4 of 15 published criticisms were not fully examined (4 partial, 0 unexamined). 10 apply outright and 1 are contested.

0 clean, 10 exhibited, 1 contested, 4 partial.

What applies, and what it is:

- **C04** (asserted) - AI assistance increases duplicated code and reduces refactoring and code reuse.
- **C05** (asserted) - AI-generated C# ignores nullable reference type annotations: it omits null checks and assigns possibly-null results to non-nullable targets.
- **C07** (asserted) - AI-generated tests assert general outcomes rather than specific values, and omit coverage for new public methods.
- **C09** (confirmed) - This repository's own tests are too few or too weak to falsify the code they cover: they exercise the happy path, assert loosely, or draw inputs from a pool that cannot reach the failing case.
- **C10** (confirmed) - AI-generated code can appear to work while breaking core functionality, revealed only by thorough testing.
- **C11** (asserted) - AI-generated code solves the immediate task but misses long-term maintainability and architectural fit.
  - Counterpoint (**C25**): A controlled study of subsequent evolution found no significant difference in completion time or code quality between AI-co-developed and human-written code.
- **C13** (confirmed) - LLM-generated code carries recurring bug patterns that differ from human-written defects.
  - Counterpoint (**C26**): Comparing like for like, human-written code showed a greater variety of security problems than GPT-4 code; the generated code differed by containing more severe outliers, not more defects.
- **C14** (asserted) - Generated tests run and pass while asserting weakly: they are executable without meaningfully constraining behaviour, so coverage overstates what they verify.
- **C15** (asserted) - Coverage and mutation adequacy do not catch the faults LLM-generated code actually contains, because the test oracles fail to capture the faulty behaviour.
- **C16** (asserted) - LLM-generated unit tests carry test smells -- design flaws that undermine readability and maintainability -- beyond what compilability or coverage reveals.
- **C06** (contested) - AI-generated .NET code neglects disposal of resources, reaches for generic exception types, and applies null-checking inconsistently.

The panel raised 54 findings in total: 36 matching a published criticism, and 18 matching none of them. The second group is the more interesting one -- the corpus was written about AI-generated code in general, not about this repository. 

## What was found

54 findings, each anchored to a file and line. A finding marked "failed - demonstrated" is covered by a probe that ran and showed the defect. A seat emits ONE probe and names the CLAIM it demonstrates, not a single finding -- so where that seat raised several findings under the same claim, the cell marks each of them, and the probe count in Evidence quality is the number of probes rather than the number of rows. "not probed" means this seat's probe addressed a different claim; one marked "passed - unsubstantiated" is the seat's assertion with no executable evidence behind it. "failed - not attributable" means the probe command exited non-zero in a clean room that could not run this repository's tests in the first place, so the exit says nothing about the finding. The Role column separates an independent issue from a second reviewer's account of the same one: several seats landing on one file and line is CORROBORATION, and a flat table renders it as volume instead. The strongest account of a location is marked independent and names the seats that corroborated it; the rest are marked supporting and point at it. Nothing is merged -- each seat's own wording is evidence about that seat, and folding them would discard the independence a cross-vendor panel exists to buy.


By declared severity: 2 high, 13 medium, 39 low. 54 findings sit at 49 distinct file and line positions, of which 3 were reached independently by more than one seat -- 4 of the rows below are those seats' separate accounts of a location already listed, marked supporting in the Role column rather than merged away.

### Matching a published criticism

| Severity | Finding | Seat | Role | Claim | Anchor | Probe | Summary |
|---|---|---|---|---|---|---|---|
| high | F4 | K | independent | [C10](#c10) | `docs/batch-5-conformance-review.md:36` OK | not probed | The repo's own conformance record shows code that appeared to work while breaking core functionality: with the suite green and coverage/mutation strong, C1 relabeled Berlin local time as UTC without shifting (wrong FIX instant on the wire for a trading library) and H1 made EX/NX always wrong for list controls; only an external audit against real broker specs revealed it. Instances are recorded fixed and pinned, but the green-tests-broken-core gap is the exhibited characteristic. |
| medium | F1 | C | independent | [C10](#c10) | `src/FixPortal.FixAtdl/Model/Controls/Support/NumericControlBase.cs:194` OK | failed - demonstrated | Numeric controls silently truncate fractional values into Int_t parameters (1.5 becomes wire 1 and reports valid), while TextField_t rejects the same input. |
| medium | F2 | C | independent | [C09](#c09) | `tests/FixPortal.FixAtdl.Tests/Model/Types/ValueTypeConversionTests.cs:422` OK | failed - demonstrated | Currency_t wire tests use only valid alpha codes; no case reaches the numeric-string acceptance path, so F1 goes undetected. |
| medium | F6 | K | independent | [C13](#c13) | `docs/superpowers/specs/2026-06-02-batch8-review-report.md:15` RECOVERED (quote at line 14) | failed - demonstrated | One AI-authored fix pass introduced twelve correctness bugs (two High: both boolean states emitting 'Y' on the wire; a parameter silently omitted from FIX output after an empty-string set), on top of the same defect classes (nullability, comparison edge semantics, DateTime Kind/timezone) recurring in earlier passes — documented recurring bug patterns in LLM-generated code, with three of the twelve being pre-existing defects the pass exposed rather than introduced. |
| medium | F1 | K | independent | [C13](#c13) | `src/FixPortal.FixAtdl/Diagnostics/ThrowHelper.cs:240` OK | failed - demonstrated | ThrowHelper.Rethrow's two single-argument convenience overloads still run caller templates through string.Format unguarded, so a brace-bearing template (e.g. containing '{NULL}') throws FormatException from the error-reporting path - the exact defect class the params overload was hardened against (F1a/F1b/F1c). |
| medium | F3 | C | independent | [C05](#c05) | `src/FixPortal.FixAtdl/Model/Elements/Edit_t.cs:44` OK | not probed | The optional XML attribute Field2 is declared as a non-nullable string initialised with null!, so the published API hides a routinely-null value from nullable analysis. |
| medium | F6 | C | independent | [C11](#c11) | `.github/scripts/assert_gate_coverage.py:1095` OK | not probed | The merge-gate checker hand-parses YAML with regexes over 2667 lines and documents repeated fail-open escapes, while the sibling checker in the same directory uses PyYAML. |
| medium | F5 | K | independent | [C15](#c15) | `docs/coverage-baseline.md:101` OK | not probed | Adequacy metrics gave strong false confidence: the recorded Stryker run killed every covered mutant (0 survived of 3,240) at 69.3% line coverage while the same code carried the C1/C2/H1/H2 wire-semantics faults — the test oracles did not encode the faulty behavior, so coverage and mutation adequacy could not catch the faults the code actually contained. |
| medium | F3 | K | independent | [C04](#c04) | `.github/scripts/assert_workflow_hygiene.py:557` OK | failed - the probe did not run: its output reports a setup failure (matched 'is not recognized as') | assert_gate_coverage.py and assert_workflow_hygiene.py duplicate overlapping security-check machinery over the same workflow files — local action.yml/action.yaml resolution, composite runs.using detection, visited-set recursion into local delegations (local_action_paths/check_local_action) and pin checking — so a fix applied to one checker and not the other re-opens a gate hole; the estate's own canonical-assets apparatus exists precisely because such copy-drift merged silently on 2026-09-20. |
| medium | F1 | K | independent | [C11](#c11) | `.github/scripts/assert_gate_coverage.py:2` OK | not probed | assert_gate_coverage.py is a 2,667-line hand-rolled YAML-subset parser (regex key scanners, derived-indentation tracking, a static GitHub-expression evaluator) that re-implements, beside it in the same change, the proper PyYAML-based parsing assert_workflow_hygiene.py performs on the same workflow files; its comments document dozens of escape-driven regex patches accumulating instead of replacing the approach — the immediate task solved, the architectural fit and maintainability missed. |
| medium | F1 | X | independent | [C10](#c10) | `src/FixPortal.FixAtdl/Model/Controls/Support/EnumState.cs:464` OK | failed - the probe exited non-zero but its output contains no failing test; a non-zero exit is not by itself evidence | String_t.ToEnumState rejects a valid single enum wire value A,B because the shared parser splits it into separate values. Resolve single-valued strings by exact wire-value lookup before applying multi-value parsing. |
| medium | F1 | X | independent | [C10](#c10) | `src/FixPortal.FixAtdl/Utility/ModelUtils.cs:39` OK | failed - the probe exited non-zero but its output contains no failing test; a non-zero exit is not by itself evidence | Same-named visitor types from different assemblies share a cache entry, causing the second invocation to throw TargetException. Key the cache by actual Type identities rather than FullName strings. |
| low | F2 | C | independent | [C10](#c10) | `src/FixPortal.FixAtdl/Model/Controls/Support/NumericControlBase.cs:225` OK | failed - demonstrated | Same truncation on the uint path, affecting Length_t, NumInGroup_t, SeqNum_t and TagNum_t parameters fed from a spinner. |
| low | F3 | C | independent | [C10](#c10) | `src/FixPortal.FixAtdl/Model/Controls/Support/TextControlBase.cs:174` OK | failed - demonstrated | The sibling text path rejects non-integers, which is what makes the spinner behaviour in F1 an inconsistency rather than a policy. |
| low | F4 | C | independent | [C06](#c06) | `src/FixPortal.FixAtdl/Diagnostics/ThrowHelper.cs:256` OK | not probed | BuildRethrown returns the original exception unchanged when its type lacks a (string, Exception) constructor, silently dropping the formatted context message and the XML line info. |
| low | F8 | C | independent | [C16](#c16) | `tests/FixPortal.FixAtdl.Tests/Fix/FixMessageTests.cs:298` OK | not probed | Tests store Boolean_t and Percentage_t wire values under tag 35 (MsgType), a misleading fixture unrelated to the parameter under test. |
| low | F6 | C | independent | [C06](#c06) | `src/FixPortal.FixAtdl/Fix/FixDateTime.cs:155` OK | not probed | FixDateTime.Parse reports an unparseable string as InvalidCastException rather than FormatException, so callers catching the conventional parse-failure type miss it. |
| low | F5 | C | independent | [C04](#c04) | `src/FixPortal.FixAtdl/Model/Types/Currency_t.cs:53` OK | not probed | An identical ConvertFromWireValueFormat is copied across Currency_t/Country_t/Language_t, each with a redundant literal "None" pre-check that the post-parse None check already covers. |
| low | F5 | C | independent | [C05](#c05) | `src/FixPortal.FixAtdl/Model/Collections/ReadOnlyControlCollection.cs:347` OK | not probed | The Rethrow params overload is typed object[] rather than object?[] (unlike New<T>), which forces callers to suppress nullable ParameterRef with '!'. |
| low | F6 | C | independent, corroborated by K, X | [C14](#c14) | `tests/FixPortal.FixAtdl.Tests/Model/Collections/SupplementalCollectionTests.cs:157` OK | not probed | The test's only assertion is NotThrow, and its comment admits the pass/fail outcome of EvaluateAll is fixture-dependent and unchecked. |
| low | F7 | C | independent | [C14](#c14) | `tests/FixPortal.FixAtdl.Tests/Conformance/InvertedListConformanceTests.cs:115` OK | not probed | The inverted-list init test skips its wire-value assertion for the 'unknown' row, so that case verifies only enum bits. |
| low | F8 | C | independent | [C16](#c16) | `tests/FixPortal.FixAtdl.Tests/Conformance/AdditionalConformanceTests.cs:35` OK | not probed | Assertion Roulette: one test chains about 10 unlabelled assertions across init, round-trip and reset, so a failure does not say which behaviour broke. |
| low | F5 | C | independent | [C05](#c05) | `src/FixPortal.FixAtdl/Fix/FixDateTime.cs:91` OK | not probed | TryParse's value parameter is declared non-nullable string, yet the method documents and handles a reachable null; callers and tests must use the null-forgiving operator. |
| low | F5 | C | independent | [C05](#c05) | `src/FixPortal.FixAtdl/Model/Collections/ReadOnlyControlCollection.cs:313` OK | not probed | A possibly-null result is assigned to a non-nullable IParameter local with null! suppression. |
| low | F4 | C | independent, corroborated by K | [C05](#c05) | `src/FixPortal.FixAtdl/Model/Controls/Support/BinaryControlBase.cs:41` OK | not probed | CheckedEnumRef is declared non-nullable with null! yet is null-checked in HasEnumeratedState; the annotation misstates the contract. |
| low | F6 | C | independent | [C04](#c04) | `src/FixPortal.FixAtdl/Model/Types/TZTimestamp_t.cs:127` OK | not probed | The original-wire-value round-trip emission logic is duplicated between TZTimestamp_t and TZTimeOnly_t (see TZTimeOnly_t.cs:149). |
| low | F4 | C | independent | [C05](#c05) | `src/FixPortal.FixAtdl/Model/Elements/Control_t.cs:97` OK | not probed | The optional ToolTip attribute on the public Control_t base is non-nullable with null!; adapter consumers get no warning that it is commonly absent. |
| low | F2 | K | independent | [C04](#c04) | `.github/workflows/mutation.yml:31` OK | failed - the probe did not run: its output reports a setup failure (matched 'is not recognized as') | The identical actionlint step block is copy-pasted into all three build workflows (ci.yml, mutation.yml, release.yml), and the checkout-with-persist-credentials-false block is repeated across all four — no shared composite action or reusable workflow, so a pin bump or flag change must be edited N times and can drift. |
| low | F3 | K | independent | [C16](#c16) | `tests/FixPortal.FixAtdl.Tests/Fix/FixMessageTests.cs:288` OK | not probed | Leftover dead section-header banner in FixMessageTests: an empty 'FixFieldValueProvider percentage init-value scaling (H3)' comment block sits directly above the R12 banner, the H3 tests having been moved elsewhere - stale navigational structure in the test file. |
| low | F8 | K | independent | [C11](#c11) | `.coderabbit.yaml:56` OK | not probed | The .coderabbit.yaml comment records a prior configuration that was silently ignored for an extended period (a bare instructions: key that does not exist in CodeRabbit schema v2) and was patched in place rather than caught — the same immediate-task-over-verification shape: the config looked like it was briefing reviews while doing nothing. |
| low | F2 | K | independent | [C04](#c04) | `src/FixPortal.FixAtdl/Model/Types/Support/AtdlValueType.cs:81` OK | failed - the probe did not run: its output reports a setup failure (matched 'is not recognized as') | The identical IControlConvertible rejection block is copy-pasted across 11 type classes, and AtdlValueType.cs:81 / AtdlReferenceType.cs:81 are ~150-line near-identical implementations of the same SetWireValue/GetWireValue/SetValueFromControl logic. |
| low | F3 | X | independent | [C09](#c09) | `tests/FixPortal.FixAtdl.Tests/Model/Types/TypeCoverageGapTests.cs:105` OK | not probed | The String_t enum conversion test uses only delimiter-free wire values, leaving the demonstrated punctuation regression undetected by the passing suite. Add single-value round-trip cases containing commas, semicolons and spaces. |
| low | F1 | X | independent | [C05](#c05) | `src/FixPortal.FixAtdl/Fix/FixTagValuesCollection.cs:122` RECOVERED (quote at line 123) | failed - the probe did not run: its output reports a setup failure (matched 'Build failed') | A stored null makes TryGetValue return true despite its NotNullWhen(true) contract, allowing callers to dereference null after a successful lookup. Return false for null values or revise the nullable contract consistently through FixFieldValueProvider. |
| low | F1 | K | supporting F4/C | [C05](#c05) | `src/FixPortal.FixAtdl/Model/Controls/Support/BinaryControlBase.cs:41` OK | not probed | Non-nullable reference properties throughout the model are initialised with null!, storing null in non-nullable targets (null = 'attribute absent' by design). |
| low | F2 | K | supporting F6/C | [C14](#c14) | `tests/FixPortal.FixAtdl.Tests/Model/Collections/SupplementalCollectionTests.cs:157` OK | not probed | StrategyEditCollection_EvaluateAll_with_resolved_edits_does_not_throw asserts only that the path does not throw; its own comment states the pass/fail outcome is fixture-dependent and the test's value is merely that the code path runs - a runnable test that constrains no behaviour. |
| low | F1 | X | supporting F6/C | [C14](#c14) | `tests/FixPortal.FixAtdl.Tests/Model/Collections/SupplementalCollectionTests.cs:157` OK | failed - the probe exited non-zero but its output contains no failing test; a non-zero exit is not by itself evidence | The resolved-edits test evaluates zero edits because twap.xml contains none, duplicating empty-collection coverage. Add an explicit edit and assert its expected result. |

### Not named by any criticism in the corpus

The corpus was written about AI-generated code in general, not about this repository. These findings map to none of its claims, which is where the corpus stops bounding the audit.

| Severity | Finding | Seat | Role | Claim | Anchor | Probe | Summary |
|---|---|---|---|---|---|---|---|
| high | F1 | C | independent | - | `src/FixPortal.FixAtdl/Utility/StringExtensions.cs:59` OK | not probed | ParseAsEnum lets Enum.Parse accept bare numeric strings, so Currency_t/Country_t/Language_t silently map "3" to IsoCurrencyCode.ALL (and ISO numeric codes like "036" to the wrong currency) instead of rejecting them. |
| medium | F1 | C | independent | - | `src/FixPortal.FixAtdl/Fix/FixMessage.cs:108` OK | not probed | FixMessage rejects any repeated tag, so an order containing a legal repeating group (Parties 448/452, or this library's own StrategyParametersGrp 958-960 from StrategyParametersGrpEmitter) fails to parse as a whole and cannot seed initFixField values. |
| medium | F2 | X | independent | - | `.github/workflows/review-tier.yml:198` OK | not probed | The executed workflow removes review-high from a non-HIGH-path PR even when review-high-manual is present, removing its only configured CodeRabbit trigger. Make the manual override retain or apply review-high. |
| low | F9 | C | independent | - | `src/FixPortal.FixAtdl/Fix/FixMessage.cs:63` OK | not probed | Dead branch: string.Split always returns at least one element, so the Length == 0 guard can never fire. |
| low | F4 | C | independent | - | `src/FixPortal.FixAtdl/Fix/FixDateTime.cs:91` OK | not probed | UNVERIFIED: the exact FIX formats are parsed with the caller's provider, and ':' in a custom format string resolves to that culture's TimeSeparator. A public caller passing a non-invariant culture (for example fi-FI under ICU) would fail valid FIX timestamps. Refuted if: TryParse("20260601-08:00:00", new CultureInfo("fi-FI")) returns true with 2026-06-01T08:00Z. |
| low | F3 | C | independent | - | `src/FixPortal.FixAtdl/Fix/FixDateTime.cs:120` OK | not probed | Time-only FIX values parsed with TryParseExact take the host's current local date (a hidden DateTime.Now read inside the BCL), so the returned DateTime for '12:00:00' varies by day and by host timezone near midnight. |
| low | F7 | C | independent | - | `src/FixPortal.FixAtdl/Fix/FixFieldTypes.cs:794` OK | not probed | FixFieldTypes overrides NoLegSecurityAltID to numeric on the stated rule that every NoXXX group counter is numeric, but leaves NoUsernames (tag 809, NumInGroup) absent, so IsNumeric returns false. The enum has 1452 members and the dictionary 1451; EditFixFieldValueTypeTests pins the gap. |
| low | F3 | C | independent | - | `src/FixPortal.FixAtdl/Xml/Serialization/ElementFactory.cs:142` OK | not probed | Attribute-processing failures are rethrown with GeneralElementProcessingError and string.Empty in the {0} element-name slot, so the message reads 'processing the element: ' with no element named. |
| low | F2 | C | independent | - | `CHANGELOG.md:105` OK | not probed | The 1.1.3 CHANGELOG entry says the project 'is now at version 1.1.4', which contradicts its own section header and the later 1.1.4 entry. |
| low | F2 | C | independent | - | `src/FixPortal.FixAtdl/Fix/FixDateTime.cs:25` OK | not probed | The leap-second normaliser accepts :60 at any minute: 12:00:60 silently becomes 12:01:00, although UTCTimestamp only permits a declared leap second at 23:59:60 UTC. FixDateTimeTests line 88 pins this permissive behaviour. |
| low | F1 | C | independent, corroborated by X | - | `CHANGELOG.md:332` OK | not probed | CHANGELOG link references are stale: [Unreleased] compares from v1.1.4 although 1.1.5 and 1.1.6 are released, and [1.1.5]/[1.1.6] have no link definitions. |
| low | F3 | K | independent | - | `src/FixPortal.FixAtdl/Model/Elements/Control_t.cs:258` OK | not probed | Control_t.TryConvertToInt/TryConvertToUint/TryConvertToDecimal XML doc promises 'false otherwise' but the implementation throws InvalidCastException for any non-empty unparseable value (line 267) - the @returns and @exception clauses contradict each other and the returns clause is wrong. |
| low | F7 | K | independent | - | `README.md:161` OK | not probed | Version drift inside the same change: README.md states 'Source on main is versioned 1.1.5 (pending release)' while src/FixPortal.FixAtdl/FixPortal.FixAtdl.csproj sets <Version>1.1.6</Version>; docs/conformance.md meanwhile records '1261 passing tests against package 1.1.3' — three version figures across three committed files, so at least one consumer-facing doc is stale. |
| low | F1 | K | independent | - | `src/FixPortal.FixAtdl/Fix/FixTag.cs:44` RECOVERED (quote at line 45) | not probed | FixTag's implicit int conversion invokes a throwing constructor, so an innocuous assignment can throw ArgumentOutOfRangeException. |
| low | F2 | K | independent | - | `src/FixPortal.FixAtdl/Fix/NumInGroup.cs:43` RECOVERED (quote at line 44) | not probed | NumInGroup's implicit int conversion has the same defect: it runs through the throwing constructor, violating the rule that implicit operators must not throw. |
| low | F3 | K | independent | - | `src/FixPortal.FixAtdl/Fix/StrategyParametersGrpEmitter.cs:39` RECOVERED (quote at line 40) | not probed | StrategyParametersGrpEmitter filters an empty WireValue but not an empty parameter Name, so (958, "") can be emitted for the host-joins-tuples path the remarks explicitly support, producing a '958=' field this codebase's own parser rejects. |
| low | F2 | X | independent | - | `scripts/assert-coverage-floor.ps1:116` OK | not probed | A NaN MinimumLineRate makes the coverage gate accept a zero-coverage report; reproduced with exit code zero, although current CI supplies 70. Reject non-finite floors and percentages outside 0–100. |
| low | F2 | X | supporting F1/C | - | `CHANGELOG.md:332` OK | not probed | The Unreleased comparison starts at 1.1.4 although 1.1.6 is already released, including released changes in the pending-change view. Advance its base to v1.1.6. |

## Coverage: every criticism, and what each seat said

One row per published criticism. The verdict column means:

- **confirmed** - a seat found it AND a probe demonstrated it
- **asserted** - a seat found it, with no probe evidence
- **contested** - the seats disagreed; the disagreement is preserved, never averaged
- **clean** - every seat checked it and none found it
- **clean (partial)** - everyone who checked said no, but not everyone checked
- **not assessed** - no seat examined it. This is not a weaker "clean".

The **Looked** column counts how many reporting seats actually examined that criticism. A verdict backed by one seat is weaker evidence than the same verdict backed by all of them, and a bare matrix hides the difference.

| # | Criticism | X | C | K | Looked | Verdict |
|---|---|---|---|---|---|---|
| C02 | AI-generated code reproduces exploitable defects because it was trained on unvetted, buggy code. | not assessed | clean (partial) | clean | 2 of 3 | clean (partial) |
| C04 | AI assistance increases duplicated code and reduces refactoring and code reuse. | not assessed | exhibits | exhibits | 2 of 3 | asserted |
| C05 | AI-generated C# ignores nullable reference type annotations: it omits null checks and assigns possibly-null results to non-nullable targets. | exhibits | exhibits | exhibits | 3 of 3 | asserted |
| C06 | AI-generated .NET code neglects disposal of resources, reaches for generic exception types, and applies null-checking inconsistently. | clean (partial) | exhibits | clean | 3 of 3 | contested |
| C07 | AI-generated tests assert general outcomes rather than specific values, and omit coverage for new public methods. | exhibits | clean (partial) | clean (partial) | 3 of 3 | asserted |
| C08 | AI-generated code references packages that do not exist, creating a supply-chain attack surface. | clean (partial) | clean | clean | 3 of 3 | clean (partial) |
| C09 | This repository's own tests are too few or too weak to falsify the code they cover: they exercise the happy path, assert loosely, or draw inputs from a pool that cannot reach the failing case. | exhibits | exhibits | clean (partial) | 3 of 3 | confirmed |
| C10 | AI-generated code can appear to work while breaking core functionality, revealed only by thorough testing. | exhibits | exhibits | exhibits | 3 of 3 | confirmed |
| C11 | AI-generated code solves the immediate task but misses long-term maintainability and architectural fit. | not assessed | exhibits | exhibits | 2 of 3 | asserted |
| C12 | AI-generated code introduces security flaws at a high rate across major languages, C# included. | not assessed | clean | clean | 2 of 3 | clean (partial) |
| C13 | LLM-generated code carries recurring bug patterns that differ from human-written defects. | not assessed | not assessed | exhibits | 1 of 3 | confirmed |
| C14 | Generated tests run and pass while asserting weakly: they are executable without meaningfully constraining behaviour, so coverage overstates what they verify. | exhibits | exhibits | exhibits | 3 of 3 | asserted |
| C15 | Coverage and mutation adequacy do not catch the faults LLM-generated code actually contains, because the test oracles fail to capture the faulty behaviour. | not assessed | not assessed | exhibits | 1 of 3 | asserted |
| C16 | LLM-generated unit tests carry test smells -- design flaws that undermine readability and maintainability -- beyond what compilability or coverage reveals. | not assessed | exhibits | exhibits | 2 of 3 | asserted |
| C17 | Benchmarks reporting only functional correctness hide the trade-off against maintainability, efficiency and style -- the qualities that decide whether .NET code survives contact with a team. | clean (partial) | not assessed | clean | 2 of 3 | clean (partial) |

## Practice: disciplines the corpus argues for

These are not allegations against this repository. The corpus carries them because they say why a control exists, and the question is whether this repository follows the discipline - so they are counted nowhere in the verdict above.

- **follows** - the repository demonstrably does this
- **does not follow** - it does not, which is not by itself a defect
- **not assessed** - no seat could tell from source alone

| # | Practice | X | C | K | Looked | Verdict |
|---|---|---|---|---|---|---|
| C20 | Red-green TDD is the working discipline for agent-written code: the agent is given the test command first and made to drive the change from a failing test. | not assessed | not assessed | follows | 1 of 3 | follows |
| C22 | Static analysis and test feedback fed back into generation measurably improves the code produced, which is the argument for treating analyzers as build-blocking rather than advisory. | follows | follows | follows | 3 of 3 | follows |

### What the matrix does not mean

Every seat audited against the same corpus. That is deliberate - it makes divergence attributable to the model rather than to what each one happened to read - and it means agreement across seats is a **control, not reassurance**. A shared frame produces shared conclusions, so a row of "clean" is evidence about the panel before it is evidence about the code.

## Evidence quality

probe baseline: `dotnet restore "FixPortal.FixAtdl.slnx"; dotnet build "FixPortal.FixAtdl.slnx" -c Release --no-restore; dotnet test --solution "FixPortal.FixAtdl.slnx" -c Release --no-build --timeout 5m` ran green in the clean room, so a probe failure is attributable to its finding

probes: 12 emitted, 12 ran, 4 substantiated, 0 timed out

anchor-validation: 54 checked, 5 marked, 0 not checked (retained, never dropped)

Anchors are checked mechanically: the file must exist at that path, the line must be in range, and any quoted code must match at that line. A finding whose anchor fails is marked in the tables above and kept - deleting it would discard the evidence that settled it.

## How this was produced

### Panel

- **X** (openai) - resolved to `gpt-6-astra` at dispatch
- **C** (anthropic) - resolved to `claude-opus-5-5` at dispatch
- **K** (moonshot) - resolved to `kimi-code/kimi-for-coding` at dispatch

### Clean room

The panel audited an ephemeral git worktree at this commit, not this checkout. The files below were deleted from that worktree before dispatch, so no seat could read a previous audit of this repository and launder it as fresh analysis. They remain in version control here, untouched:

- `docs/ai-findings.md`

## Sources

Every criticism and practice above is quoted from published work, not from the panel's own opinion. These are the sources the claims in this report rest on. The corpus holds 74 accepted sources in total; the other 53 back no claim used here and are listed for provenance in the run record.

#### C02
AI-generated code reproduces exploitable defects because it was trained on unvetted, buggy code.
- Asleep at the Keyboard? Assessing the Security of GitHub Copilot's Code Contributions <https://arxiv.org/abs/2108.09293>
- A Survey of Bugs in AI-Generated Code <https://arxiv.org/abs/2512.05239>

#### C04
AI assistance increases duplicated code and reduces refactoring and code reuse.
- AI Copilot Code Quality: 2025 Data Suggests Growth in Code Clones <https://www.gitclear.com/ai_assistant_code_quality_2025_research>

#### C05
AI-generated C# ignores nullable reference type annotations: it omits null checks and assigns possibly-null results to non-nullable targets.
- GitHub Copilot unaware of nullable types in C#? <https://github.com/orgs/community/discussions/120409>

#### C06
AI-generated .NET code neglects disposal of resources, reaches for generic exception types, and applies null-checking inconsistently.
- Reviewing AI-Generated Code in .NET <https://devblogs.microsoft.com/dotnet/developer-and-ai-code-reviewer-reviewing-ai-generated-code-in-dotnet/>

#### C07
AI-generated tests assert general outcomes rather than specific values, and omit coverage for new public methods.
- Reviewing AI-Generated Code in .NET <https://devblogs.microsoft.com/dotnet/developer-and-ai-code-reviewer-reviewing-ai-generated-code-in-dotnet/>

#### C08
AI-generated code references packages that do not exist, creating a supply-chain attack surface.
- We Have a Package for You! A Comprehensive Analysis of Package Hallucinations by Code Generating LLMs <https://arxiv.org/abs/2406.10279>

#### C09
This repository's own tests are too few or too weak to falsify the code they cover: they exercise the happy path, assert loosely, or draw inputs from a pool that cannot reach the failing case.
- Is Your Code Generated by ChatGPT Really Correct? Rigorous Evaluation of LLMs for Code Generation <https://arxiv.org/abs/2305.01210>

#### C10
AI-generated code can appear to work while breaking core functionality, revealed only by thorough testing.
- Can chatbots craft correct code? <https://blog.trailofbits.com/2025/12/19/can-chatbots-craft-correct-code/>

#### C11
AI-generated code solves the immediate task but misses long-term maintainability and architectural fit.
- Reviewing AI-Generated Code in .NET <https://devblogs.microsoft.com/dotnet/developer-and-ai-code-reviewer-reviewing-ai-generated-code-in-dotnet/>

#### C12
AI-generated code introduces security flaws at a high rate across major languages, C# included.
- Assessing the Quality and Security of AI-Generated Code: A Quantitative Analysis <https://arxiv.org/abs/2508.14727>
- Security Weaknesses of Copilot-Generated Code in GitHub Projects <https://arxiv.org/abs/2310.02059>

#### C13
LLM-generated code carries recurring bug patterns that differ from human-written defects.
- Bugs in Large Language Models Generated Code: An Empirical Study <https://arxiv.org/abs/2403.08937>

#### C14
Generated tests run and pass while asserting weakly: they are executable without meaningfully constraining behaviour, so coverage overstates what they verify.
- VibeCheck: Assessing the Quality of LLM-Generated Unit Tests <https://arxiv.org/abs/2609.05978>

#### C15
Coverage and mutation adequacy do not catch the faults LLM-generated code actually contains, because the test oracles fail to capture the faulty behaviour.
- How effective are traditional test criteria at detecting bugs in LLM-generated code? <https://arxiv.org/abs/2609.09315>

#### C16
LLM-generated unit tests carry test smells -- design flaws that undermine readability and maintainability -- beyond what compilability or coverage reveals.
- On the Diffusion of Test Smells in LLM-Generated Unit Tests <https://arxiv.org/abs/2410.10628>

#### C17
Benchmarks reporting only functional correctness hide the trade-off against maintainability, efficiency and style -- the qualities that decide whether .NET code survives contact with a team.
- Benchmarking the Titans: LLM Code Generation Quality in the .NET Ecosystem <https://arxiv.org/abs/2608.22529>

#### C20
Red-green TDD is the working discipline for agent-written code: the agent is given the test command first and made to drive the change from a failing test.
- Engineering practices that make coding agents work (Simon Willison) <https://simonwillison.net/2026/Mar/14/pragmatic-summit/>

#### C22
Static analysis and test feedback fed back into generation measurably improves the code produced, which is the argument for treating analyzers as build-blocking rather than advisory.
- Helping LLMs Improve Code Generation Using Feedback from Testing and Static Analysis <https://arxiv.org/abs/2412.14841>

#### C23
Measured across public repositories rather than purpose-generated samples, the large majority of AI-generated code carries no identifiable CWE-mapped vulnerability at all.
- Security Vulnerabilities in AI-Generated Code: 7,703 public GitHub files (87.9% carry no CWE) <https://arxiv.org/abs/2510.26103>

#### C24
The studies raising the alarm on AI code security largely analysed code generated for the study itself, which leaves their realism open to question.
- WildCode Revisited: prior alarm studies used purpose-generated code <https://arxiv.org/abs/2512.04259>

#### C25
A controlled study of subsequent evolution found no significant difference in completion time or code quality between AI-co-developed and human-written code.
- Echoes of AI: Downstream Effects of AI Assistants on Software Maintainability <https://arxiv.org/abs/2507.00788>

#### C26
Comparing like for like, human-written code showed a greater variety of security problems than GPT-4 code; the generated code differed by containing more severe outliers, not more defects.
- Comparing Human and LLM Generated Code: The Jury is Still Out! <https://arxiv.org/abs/2501.16857>


Each claim carries a verbatim quote from its source in the corpus manifest, and every source is pinned by sha256 against the bytes that were scraped, so a claim can be checked against what was actually published rather than against a paraphrase of it.
