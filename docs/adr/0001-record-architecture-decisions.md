# 0001. Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-11-15

## Context

ChattyDuck is a learning project. The reasoning behind its design is as valuable as the code itself, and it will evolve through many changes to services, protocols and infrastructure. Without a written record, the "why" behind earlier choices gets lost.

## Decision

We will record every significant architectural decision as an Architecture Decision Record in `docs/adr/`, using the template in `0000-template.md`.

A decision counts as significant when it introduces or replaces a service, a layer boundary, a protocol, a storage engine, a hosting or deployment target, or a cross-cutting library (authentication, messaging, observability).

## Alternatives considered

- **Wiki or external docs:** lives apart from the code and drifts out of date. Rejected.
- **Comments and PR descriptions only:** hard to find later and not reviewable as a whole. Rejected.

## Consequences

- Architecture changes take a little more effort, because each one needs a short write-up.
- The history of the design can be read, linked to and reviewed alongside the code.
