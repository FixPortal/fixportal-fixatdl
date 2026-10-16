# Strategy public wrapper proof

Original October 11 A15 is exercised through nine actual loaded-XML strategy cases.
They call EvaluateAllStrategyEdits with supplied and null FIX providers, verify
short-circuit state, and call LoadInitialControlValues and LoadParameterValues
with actual custom Boolean translation, authored fallback and reset behavior.
No product or immutable golden changes are needed.

Five independent compiling production mutations failed the expected assertions:
ignoring supplied FIX values (four failures), removing the supported null fallback
(one), flipping short-circuit forwarding (two), dropping control initialization
FIX values (one), and inverting the parameter reset flag (two).

The audit's suggested mutation that drops only the Parameters argument remained
green across all nine cases. That argument is not consumed by the current edit
evaluation path; the separate provider overload that consumes it is used for
control initialization. This equivalent control is recorded explicitly and is
not counted as a detected regression. No observer-only assertion was invented.

After restoring production bytes, the full core and Contracts suites passed
1,397 and 271 cases respectively: 1,668 passed with zero skips. The five-project
Release rebuild had zero warnings/errors; repository CSharpier passed. Two initial
mutant forms rejected by analyzers were replaced with compiling variants and
excluded from regression evidence. Earlier F9 and F2/F3 tests are unchanged.

Execution used an owned unmounted SDK guest without network, ports, socket or
credentials. The new fixture is entirely in-memory; qualified existing core
cleanup only deletes currently allocated temporary files. The guest is stopped
and evidence retained. Full hosted CI remains required before merge.
