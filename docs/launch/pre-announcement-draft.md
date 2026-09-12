# FixPortal.FixAtdl pre-announcement draft

**Not approved for publication.** This unlinked draft is publicly visible in
repository source and awaits Chris's explicit approval.

## Release copy — Not for publication

FixPortal.FixAtdl 1.1.2 is a headless .NET 10 library for parsing, validating
and emitting FIX tag values from FIXatdl v1.1 strategy XML. It is the
modernised MIT-licensed fork of Atdl4net, with companion WPF and React adapters
for hosts that need editable strategy forms. Hosts retain responsibility for
order construction, transport, schema validation and broker-specific behaviour.

## LinkedIn draft — Not for publication

We have published FixPortal.FixAtdl 1.1.2, a headless .NET 10 FIXatdl v1.1
parser, validator and FIX-tag emitter. It modernises the MIT-licensed Atdl4net
reference implementation and sits alongside WPF and React adapters for editable
strategy forms. It is intended for hosts that own the final order workflow.

## Technical article outline — Not for publication

1. Why FIXatdl strategy definitions need a maintained .NET model.
2. The headless boundary: XML parsing, validation and FIX tag values.
3. Choosing a form adapter: WPF for desktop and React for browser hosts.
4. What remains host work: order construction, transport, schema validation and
   broker-specific behaviour.
5. Conformance evidence, stated scope and known limits.
