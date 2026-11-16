# Architecture Decision Records

This folder records the significant architectural decisions for ChattyDuck: what was decided, why, and what it costs.

## Conventions

- File name: `NNNN-kebab-case-title.md`, numbered in order (`0001`, `0002`, …).
- Start from [`0000-template.md`](0000-template.md).
- Status is one of `Proposed`, `Accepted`, `Deprecated` or `Superseded by NNNN`.
- Accepted ADRs are not rewritten to change the decision. Write a new ADR and mark the old one as superseded.

## Index

| # | Title | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-environment-portable-deployment.md) | Environment-portable deployment | Accepted |
| [0003](0003-web-client-authentication-via-bff.md) | Web client authentication via a BFF | Accepted (part 2 superseded by 0005 once accepted) |
| [0004](0004-local-development-environment.md) | Local development environment: Aspire as the single source of every target | Proposed |
| [0005](0005-envoy-as-edge-proxy-and-api-gateway.md) | Envoy as the edge proxy and API gateway | Proposed |
