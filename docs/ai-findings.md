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
| `docs/superpowers/plans/2026-05-30-fixatdl-1.0-roadmap.md` — "Claude Opus 4.8 is not a valid Anthropic model identifier" (AI Findings) | dismissed | false-positive | Claude Opus 4.8 is a real model released after the reviewing model's knowledge cutoff; the doc records the model actually used. Do not "correct" model names in historical planning docs. | 2026-06-10 |
| `docs/superpowers/plans/2026-05-30-fixatdl-1.0-roadmap.md` — "GPT-5.4 is not a recognized OpenAI model name" (AI Findings) | dismissed | false-positive | GPT-5.4 is a real model (the GitHub Copilot CLI reviewer in the adversarial-review panel) released after the reviewing model's knowledge cutoff. Same class of FP as the Opus 4.8 finding; expect recurrences for any post-cutoff model name. | 2026-06-10 |
