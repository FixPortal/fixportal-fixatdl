# Optional JSON fields and per-type metadata

This addresses original October 11 F2 and F3 through the actual mapper and
AtdlContractJson serializer. Literal JSON-key assertions cover populated optional
control/parameter fields, false and zero values, and absent optional members.
DefaultValue remains omitted because the mapper deliberately supplies no parameter
default; control defaults are asserted on their actual wire path. No goldens change.

Thirteen cases also cover populated/unset Int, MonthYear and Tenor bounds, Data
lengths, and MultipleChar invert as null/false/true. Assertions inspect both mapped
properties and independently named JSON keys.

Eight independent production mutations compiled and failed: Tooltip omission,
emitting null members, omitting false/zero, and each of the five model-specific
reflection lookups. These are compiling lookup mistakes, not model-property
renames that fail compilation. Restored source hashes match the originals.

After restoration, all 271 Contracts tests and 1,388 core tests passed: 1,659 total,
zero skipped. The five-project Release rebuild had zero warnings/errors, and
repository CSharpier passed. Initial new-test analyzer diagnostics were corrected
without suppression and are excluded from regression evidence. The prior F9 test
file remains byte-identical.

Execution used an owned unmounted SDK guest without network, credentials, ports
or Docker socket. Qualified core cleanup only deletes files allocated by its
current Path.GetTempFileName call. No host PowerShell, snapshot generator or golden
regeneration ran. The guest is stopped with evidence retained. Full hosted CI
remains required before merge.
