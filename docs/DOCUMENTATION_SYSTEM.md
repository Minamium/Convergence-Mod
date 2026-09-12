---
doc_id: docs.system
document_type: governance
status: accepted
owners:
  - project
last_reviewed: 2026-09-12
source_of_truth_for:
  - documentation.data_model
aliases:
  - documentation database
  - knowledge base design
related_code:
  - tools/docs_catalog.py
  - tools/repository_checks.py
  - tools/validate_yaml.py
related_docs:
  - docs.entrypoint
  - docs.search-index
---

# Documentation and Knowledge-Base Design

The committed knowledge base uses Git-readable Markdown as the canonical store. YAML front matter supplies structured records, [`INDEX.md`](INDEX.md) supplies a curated human/agent search view, and [`catalog/documents.yml`](catalog/documents.yml) is a generated machine index. A binary database is deliberately not committed.

## Directory model

```text
docs/
├── README.md                 entry point and reading order
├── INDEX.md                  curated topic/search map
├── STATUS.md                 current implementation inventory only
├── GLOSSARY.md               names, IDs, aliases, migration terms
├── DOCUMENTATION_SYSTEM.md   schema and ownership rules
├── encounters/<feature>/     feature-local current specs/plans/backlog
├── runbooks/                 repeatable workstation/operation procedures
├── handoff/                  point-in-time transfer checkpoints
├── evidence/                 sanitized verification templates and records
├── catalog/                  generated machine-readable index
├── adr/                      immutable structural decision history
└── research/                 cited observations; never current authority
```

Stable root documents such as `ARCHITECTURE.md` and `DEVELOPMENT.md` remain in place because repository instructions, checks, and Skills already link to them. Any future bulk move must update all references in the same commit and keep temporary redirect stubs.

## Front-matter record

Active indexed documents begin with:

```yaml
---
doc_id: encounter.first-severance.spec
document_type: spec
status: accepted
owners:
  - gameplay
  - networking
last_reviewed: 2026-09-04
source_of_truth_for:
  - first_severance.encounter_loop
aliases:
  - First Severance
  - 第一断絶
related_code:
  - Content/Encounters/FirstSeverance
related_docs:
  - project.status
---
```

Rules:

- `doc_id` is globally unique and stable across file moves. It is a
  dot-separated lowercase namespace; each namespace segment may contain internal
  hyphens, for example `encounter.first-severance.spec`.
- `document_type` comes from the schema in [`catalog/schema.yml`](catalog/schema.yml).
- `status` is one of `draft`, `provisional`, `accepted`, `superseded`, or `historical`.
- `last_reviewed` is an ISO date, not a claim that runtime verification occurred.
- `source_of_truth_for` topic keys are globally unique. Only `accepted` and
  `provisional` documents may own topics; `draft`, `superseded`, and
  `historical` documents must leave the list empty.
- `related_code` contains repository paths that exist now. Future paths belong in prose until created.
- `related_docs` contains stable document IDs, not filenames. Its machine
  relation type is the fixed value `related`; introduce a separately reviewed
  field before adding richer relation semantics.
- Front matter accepts only the keys listed in `allowed_fields`; unknown and
  duplicate YAML keys fail validation.
- One file has exactly one H1 outside fenced code. Use relative Markdown links
  and never commit workstation-specific absolute paths.

Every `docs/**/*.md` file must either satisfy this record or have an exact
repository-relative entry with a non-empty reason in
`catalog/schema.yml#excluded_paths`. Exclusions are migration inventory, not a
glob mechanism. In particular, `docs/catalog/README.md` is explicit; adding
another Markdown file under `docs/catalog` fails until it is deliberately
classified.

## Authority hierarchy

1. Accepted ADRs govern expensive structural decisions.
2. The active topic owner in `source_of_truth_for` governs its declared subject.
3. `STATUS.md` governs implementation state; plans must not claim code exists.
4. Feature specs govern player-visible behavior; implementation plans govern sequence and gates.
5. Backlog and research preserve ideas/evidence but cannot expand current scope.
6. Changelog records completed changes only.

## Catalog workflow

After an indexed-document edit batch is settled, regenerate and verify once through the shared wrapper:

```bash
python3 .agents/skills/develop-convergence-raids/scripts/verify_repo.py --write-catalog .
```

This also runs repository and YAML checks; repeating the individual commands is unnecessary. During editing, update only the document owning a changed fact and leave generation until the final batch. Use `tools/docs_catalog.py --write` or `--check` directly only for catalog-specific work.

The tool parses all Markdown under `docs/`, rejects undeclared exclusions,
duplicate YAML keys, unknown fields, duplicate IDs and authority topics, checks
related document IDs and current repository paths, and deterministically writes
`docs/catalog/documents.yml`. It emits both indexed `documents` and the exact
reason-bearing `excluded_documents` migration inventory. Each indexed record includes `content_sha256`, calculated
from the complete UTF-8 document after normalizing CRLF/CR to LF and terminal
newlines. CI runs `--check`, so metadata or body edits that leave the catalog
stale fail.

Useful queries on any workstation:

```bash
rg -n "source_of_truth_for|first_severance|PreKill" docs
python3 tools/docs_catalog.py --check
```

## Search tooling

Use rg and the generated catalog. No SQLite, embeddings or additional search service is implemented or required. The former optional database sketch is [archived](history/2026-09-07-pre-consolidation.md#optional-search-design-sketch-not-implemented), outside normal development reading. Any future index must be disposable, ignored cache justified by actual search cost.

## Agent guidance maintenance

When editing AGENTS.md or a Skill, keep durable project boundaries in the agreement and route task-specific procedures through narrow Skill descriptions and references. Shared guidance supports contributors using different models; model preferences belong in personal configuration. Task prompts should identify a concrete result and its applicable verification, with [AGENTS.md](../AGENTS.md#verification) owning the common completion contract.

Check realistic boundary cases after changing a trigger: a wording fix should not load gameplay research, known tuning should not repeat API investigation, and Ghost Samurai presentation should not inherit Doll-specific rules. Structural validation checks syntax and links; it does not prove actual model selection or behavior.

This maintenance approach was reviewed on 2026-09-12 against OpenAI's [skills and prompts article](https://developers.openai.com/blog/rethinking-skills-and-prompts-for-gpt-6-astra), [Build skills](https://learn.chatgpt.com/docs/build-skills), and [Codex best practices](https://learn.chatgpt.com/guides/best-practices). The local agreement and verification matrix define the project's actual requirements.

## Game-state data is separate

The documentation catalog is not a game database. Active Raid state remains ephemeral server memory under the accepted encounter architecture. A database is unnecessary for the first Raid. Future clear flags, settings, or progression should use explicit versioned tModLoader World/Player save schemas and receive their own ADR before implementation.
