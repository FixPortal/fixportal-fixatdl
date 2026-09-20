# AI Findings Ledger

GitHub's Copilot **AI Findings** set has no dismiss API. Its only GitHub-side
disposition is the PR comment's **Resolve** action, which carries no reason and
no comment — so the verdict itself has nowhere durable to live. This ledger is
that record; Resolve is still performed on the thread during triage. Before
investigating any AI Finding, match it against this table by `file:line` + rule
— if it is already `fixed` or `dismissed`, do not re-investigate.

Scope is the AI Findings set only. **Code Quality** findings and **code-scanning
security alerts** are both dismissable on GitHub, which records the verdict
durably — do not add rows for those.

Rows first seen before **2026-08-01** are **legacy**: they were recorded under a
wider scope and include Code Quality, CodeQL and analyzer findings that would not
be ledgered today. They are kept as the durable record of a verdict already
reached, not as precedent — do not cite them to justify a new non-AI-Findings
row.

| Finding | Status | Reason | Rationale | First seen |
|---|---|---|---|---|
| `src/FixPortal.FixAtdl/Validation/EditEvaluator.cs:31` — Sources getter checks EditRef before Edit while CurrentState checks Edit first (AI Findings) | fixed | | Aligned `Sources` and the explicit `Resolve` to Edit-first, matching `CurrentState`/`Evaluate`. Order was semantically immaterial — the setters enforce Edit/EditRef mutual exclusivity — so this was consistency only. | 2026-06-10 |
| `src/FixPortal.FixAtdl/Model/Types/Data_t.cs:58` — resource key `MinLengthExceeded` misleading for a below-minimum failure (AI Findings) | fixed | | Renamed `MinLengthExceeded` to `MinLengthNotMet`, plus the sibling `MinValueExceeded` to `MinValueNotMet` (same quirk, 7 call sites). Internal resource keys only; the message text was already correct. | 2026-06-10 |
| `docs/superpowers/plans/2026-05-30-fixatdl-1.0-roadmap.md` — "Claude Opus 4.8 is not a valid Anthropic model identifier" (AI Findings) | dismissed | false-positive | Claude Opus 4.8 is a real model released after the reviewing model's knowledge cutoff; the doc records the model actually used. Do not "correct" model names in historical planning docs. | 2026-06-10 |
| `docs/superpowers/plans/2026-05-30-fixatdl-1.0-roadmap.md` — "GPT-5.4 is not a recognized OpenAI model name" (AI Findings) | dismissed | false-positive | GPT-5.4 is a real model (the GitHub Copilot CLI reviewer in the adversarial-review panel) released after the reviewing model's knowledge cutoff. Same class of FP as the Opus 4.8 finding; expect recurrences for any post-cutoff model name. | 2026-06-10 |
