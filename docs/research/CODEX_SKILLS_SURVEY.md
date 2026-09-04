---
doc_id: research.codex-skills-survey
document_type: research
status: historical
owners:
  - research
last_reviewed: 2026-08-23
source_of_truth_for: []
aliases:
  - Codex Skills survey
  - repository Skills research
related_code:
  - .agents/skills
related_docs:
  - docs.system
  - research.sources
---

# Codex Skills Survey

Accessed: 2026-08-23

## Question

How should Convergence package repeatable AI-assisted workflows for a large tModLoader/Calamity project without duplicating architecture documentation or encouraging unsafe code copying?

## Confirmed platform facts

- OpenAI defines a Skill as a directory containing `SKILL.md`, with optional `scripts/`, `references/`, `assets/`, and `agents/openai.yaml`.
- Codex discovers repository-scoped Skills under `.agents/skills` from the current directory through the repository root.
- `name` and `description` frontmatter drive explicit and implicit invocation. Progressive disclosure keeps detailed references out of the initial context.
- `AGENTS.md` is the right location for short rules that apply to every task. Skills are better for repeatable, task-specific workflows.

Primary sources:

- [OpenAI — Build skills](https://learn.chatgpt.com/docs/build-skills)
- [OpenAI — Codex best practices](https://learn.chatgpt.com/guides/best-practices)
- [OpenAI — AGENTS.md](https://developers.openai.com/codex/agent-configuration/agents-md)

## Public tModLoader repository observations

### tsorcRevamp

The official project repository contains separate `.agents/skills` for a VFX pipeline, shader tips, and dust tips. The useful pattern is responsibility-based decomposition and keeping expensive toolchain details in task-scoped Skills rather than a root instruction file.

- [VFX pipeline Skill at observed commit](https://github.com/timhjersted/tsorcRevamp/blob/f658d7eabd853dcd4c25ecc9374a48a6a754220e/.agents/skills/vfx-pipeline/SKILL.md)
- [Repository license at observed commit](https://github.com/timhjersted/tsorcRevamp/blob/f658d7eabd853dcd4c25ecc9374a48a6a754220e/LICENSE)

The repository is GPL-3.0 licensed. Convergence does not copy its instructions, scripts, shader code, or project-specific paths. Only the general decomposition pattern informed this survey.

### Path of Terraria

Path of Terraria uses a detailed `AGENTS.md` to route agents through a large tModLoader codebase by subsystem, base type, data source, and synchronization path. This supports keeping Convergence's always-on `AGENTS.md` concise while moving specialized Raid and research procedures into Skills.

- [Path of Terraria AGENTS.md at observed commit](https://github.com/Path-of-Terraria/PathOfTerraria/blob/98afde749e6712399b9a89ef6bddf07a7682d454/AGENTS.md)

No code or wording was copied. The observed repository did not expose a root `LICENSE` at the inspected commit, so its text is treated only as a navigation-pattern observation.

## Repository decision

Create two focused repository Skills:

| Skill | Responsibility |
|---|---|
| `develop-convergence-raids` | Implement and review Convergence multiplayer Raid code while preserving authority, module, cleanup, and verification invariants |
| `research-tmodloader-sources` | Investigate official APIs and public Mod source with exact evidence, version, and license boundaries |

Both live under `.agents/skills`, contain only reusable workflow material, and link to repository documents rather than duplicating the entire project brief.

## Rejected approaches

- One all-purpose Terraria Skill: its trigger would be too broad and its body would consume unnecessary context.
- Putting detailed Raid procedures only in `AGENTS.md`: every unrelated edit would pay the context cost.
- Importing another Mod's Skill or source verbatim: license, version, paths, and architecture do not match Convergence.
- Encoding current experimental hook choices as permanent rules: `PreKill`, revive precedence, and transport must be validated against the pinned runtime and Calamity build first.

## Follow-up Skill candidates

Add these only after a real repeated workflow exists:

- deterministic telegraph/VFX preview and accessibility review;
- audio stem export and phase-cue validation;
- Dedicated Server multiplayer soak-test log triage;
- release asset provenance and Workshop packaging audit.
