# ADR 0001: Local Identity During Product Bootstrap

## Status

Accepted.

## Context

CodexCommonAgents classifies login, users, roles and permissions as PortalCorporativo reusable capabilities. AppColoreando is also required to run as a complete local product with user-owned progress, admin roles, JWT access tokens and refresh-token rotation.

## Decision

Implement local identity and authorization behind Application ports for this standalone product bootstrap. Keep controllers thin and keep token/password implementation in Infrastructure. Do not couple domain logic to this implementation.

## Consequence

Classification is `ADAPT/CREATE`: local runtime is created now, and the port boundary allows a later PortalCorporativo Security API adapter without domain changes.

