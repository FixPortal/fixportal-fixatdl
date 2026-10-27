# Documentation index

## User documentation

Read these to use the library.

- [Usage guide](usage.md): loading, setting values, validation, FIX output, exceptions.
- [API reference](api.md): the types a host actually calls.
- [Architecture overview](architecture/README.md): the pipeline, layers and load-bearing components.
- [Conformance record](conformance.md): the assessed FIXatdl 1.1 surface and its limits.

`images/` and `diagrams/` hold the figures those pages embed.

## Internal audit and review records

Point-in-time maintainer records. They describe the code as it stood on the
date they were written, not as it stands now, and are not needed to use the
package.

- [batch-3-findings-disposition.md](batch-3-findings-disposition.md): Phase 2 findings and their dispositions.
- [batch-5-conformance-review.md](batch-5-conformance-review.md): conformance review against real broker ATDL.
- [coverage-baseline.md](coverage-baseline.md): Phase 1 coverage starting point.
- [test-audit-formatter-2026-10-16.md](test-audit-formatter-2026-10-16.md): direct FIX formatter qualification.
- [test-audit-optional-wire-2026-10-16.md](test-audit-optional-wire-2026-10-16.md): optional JSON fields and per-type metadata tests.
- [test-audit-strategy-wrappers-2026-10-16.md](test-audit-strategy-wrappers-2026-10-16.md): strategy public wrapper proof.
