# Security Policy

## Reporting a vulnerability

Please report suspected vulnerabilities privately via
[GitHub Security Advisories](https://github.com/FixPortal/fixportal-fixatdl/security/advisories/new)
rather than opening a public issue. Include the FIXatdl strategy XML (or a
minimal redacted variant) that reproduces the problem where relevant — this
library parses externally-supplied XML, so parser inputs are the most useful
evidence.

You should receive an acknowledgement within a few days. Please give us a
reasonable window to investigate and ship a fix before any public disclosure.

## Attack surface

`FixPortal.FixAtdl` is a headless parsing library consumed as a NuGet package.
Its security-relevant surface is:

- **Strategy XML parsing and validation** — malformed, unusual, or hostile
  FIXatdl documents (entity expansion, deeply nested structures, unexpected
  control types) fed to `StrategiesReader` / the validation layer.
- **FIX-tag value emission** — values produced by `GetOutputValues()` are
  consumed by host applications' FIX sessions; a value that is silently wrong
  is more dangerous than one that fails loudly.

The library performs no network I/O of its own; sending FIX messages over the
wire is the host application's responsibility and out of this repo's scope.

## Supported versions

| Version | Supported |
|---|---|
| 1.1.x (latest release) | Yes |
| < 1.1 | No |

Fixes land on `main` and ship in the next patch release of the
`FixPortal.FixAtdl` package.
