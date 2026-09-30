@AGENTS.md

<!-- Claude Code adapter only. Canonical rules live in AGENTS.md and canonical Skills in .agents/skills; see docs/DOCUMENTATION_SYSTEM.md#agent-guidance-maintenance. A CLAUDE.md stops Claude Code from reading AGENTS.md natively, so keep the import above. -->

# Claude Code

The imported AGENTS.md is the working agreement for every agent; this file only connects Claude Code to the same Skills.

## Repository Skills

Claude Code does not read `.agents/`. Each `.claude/skills/<name>/SKILL.md` registers the canonical Skill as `/<name>` with the same description and points to it. Edit the canonical Skill; `tools/repository_checks.py` keeps adapter metadata identical.

| Skill | Use for | Canonical Skill and on-demand references |
|---|---|---|
| `/develop-convergence-raids` | Boss/Raid mechanics, authority/replication, NPC/weapon/projectile/VFX/UI presentation, playtest feedback | [SKILL.md](.agents/skills/develop-convergence-raids/SKILL.md) · [architecture map](.agents/skills/develop-convergence-raids/references/architecture-map.md) · [authority checklist](.agents/skills/develop-convergence-raids/references/raid-authority-checklist.md) · [presentation direction](.agents/skills/develop-convergence-raids/references/presentation-direction.md) · [verification matrix](.agents/skills/develop-convergence-raids/references/verification-matrix.md) |
| `/research-tmodloader-sources` | Unresolved tModLoader/Calamity/Luminance API behavior or public Mod prior art | [SKILL.md](.agents/skills/research-tmodloader-sources/SKILL.md) · [evidence template](.agents/skills/research-tmodloader-sources/references/evidence-template.md) |

- Load only the references the task needs, as each SKILL.md routes them.
- Codex-only: `agents/openai.yaml` and the [Sol delegation reference](.agents/skills/develop-convergence-raids/references/sol-implementation-delegation.md), which applies only to a top-level `gpt-6-astra` agent.
- Skill adapters below the launch directory load after Claude first reads a file in this repository; start Claude Code at the repository root to list them at launch.
