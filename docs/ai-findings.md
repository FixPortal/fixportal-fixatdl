# AI Findings Ledger

Durable record of un-dismissable static-analysis findings (GitHub Code Quality,
in-editor CodeQL advisories, Copilot "AI Findings"). These surfaces have no
dismiss API or UI, so the same finding resurfaces on every scan; this ledger
substitutes for the missing dismiss button. Before investigating any finding
from those sources, match it against this table by `file:line` + rule — if it
is already `fixed` or `dismissed`, do not re-investigate.

| Finding | Status | Reason | Rationale | First seen |
|---|---|---|---|---|
| `src/FixPortal.FixAtdl/Validation/EditEvaluator.cs:31` — Sources getter checks EditRef before Edit while CurrentState checks Edit first (AI Findings) | fixed | | Aligned `Sources` and the explicit `Resolve` to Edit-first, matching `CurrentState`/`Evaluate`. Order was semantically immaterial — the setters enforce Edit/EditRef mutual exclusivity — so this was consistency only. | 2026-06-10 |
| `src/FixPortal.FixAtdl/Model/Types/Data_t.cs:58` — resource key `MinLengthExceeded` misleading for a below-minimum failure (AI Findings) | fixed | | Renamed `MinLengthExceeded` to `MinLengthNotMet`, plus the sibling `MinValueExceeded` to `MinValueNotMet` (same quirk, 7 call sites). Internal resource keys only; the message text was already correct. | 2026-06-10 |
