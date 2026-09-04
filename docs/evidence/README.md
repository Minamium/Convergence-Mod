---
doc_id: verification.evidence
document_type: evidence
status: accepted
owners:
  - quality
last_reviewed: 2026-09-05
source_of_truth_for:
  - verification.evidence_format
aliases:
  - build record
  - test evidence
related_code:
  - Tests/Convergence.DomainTests
  - tools/repository_checks.py
related_docs:
  - development.windows
  - project.status
---

# Verification Evidence

Use [`build-record.example.json`](build-record.example.json) as the local build/smoke template. Copy it to repository-root `build-record.local.json`; that filename is ignored and may contain local machine details during investigation.

A sanitized record may be committed here only when it:

- names the exact Git commit, OS/architecture, Terraria, tModLoader, Calamity, and Addon versions;
- records the Calamity `.tmod` SHA-256 without copying the binary;
- distinguishes repository checks, domain harness, command build, Build + Reload, Single Player, Host & Play, and Dedicated Server;
- gives participant count and network conditions for multiplayer cases;
- uses `passed`, `failed`, `not_run`, or `blocked`, never ambiguous prose;
- links to a tracked issue/task for failures instead of embedding a huge log;
- contains no usernames, Steam IDs, credentials, public/private IPs, world/player files, full personal paths, raw `.tmod` files, or secrets.

One successful path must not be generalized to another. In particular, the dependency-free harness is not a tModLoader build, command-line build is not Build + Reload, and Host & Play is not Dedicated Server.

The current confirmed compatibility run is [2026-09-05 Windows baseline](2026-09-05-windows-baseline.json). It records Host & Play as `not_run` instead of inferring it from the successful Dedicated Server/two-client topology.

Committed filenames should be stable and sortable, for example `2026-09-04-windows-baseline.json`. Update [Status](../STATUS.md) only after the corresponding evidence exists.
