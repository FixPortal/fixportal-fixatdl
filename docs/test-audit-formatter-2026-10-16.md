# Direct FIX formatter qualification

The October 11 F9 residual concerned direct `FixValueFormatter.Format` calls;
mapper-only assertions can bypass these branches. This adds 51 direct cases:
two Boolean cases each checked as CLR and parsed JSON input, thirteen temporal
representations, and all six floating FIX types crossed with double/float NaN
and positive/negative infinity. Expected wire strings are literal, including
UTC rollover for offset timestamps and times, and local calendar date semantics.

Seven independent production mutations compiled and failed the new tests:
inverted Boolean literals, missing timestamp milliseconds, changed date/time/
MonthYear formats, and removal of each double/float finite guard. These mutations
were applied only inside an owned disposable Linux SDK guest. An initial malformed
finite-guard mutation did not compile and is excluded from catching evidence.
The corrected two guard mutations each produced eighteen assertion failures.

After restoring production source and forcing a nonincremental build, all 258
Contracts tests passed with zero skips. The solution build had zero warnings and
errors, and repository CSharpier passed. The first new-test compilation required
`nameof(value)` for CA1507; that setup failure is not regression evidence.

The guest had no host mounts, Docker socket, ports, credentials or privilege.
Public package/tool restore used temporary network access; tests ran after network
disconnect. No PowerShell, snapshot generator, coverage-floor script or golden-file
regeneration ran locally. The guest was stopped with evidence retained. Full
hosted CI, including existing coverage floors, remains required before merge.
