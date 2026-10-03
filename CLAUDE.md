@AGENTS.md

# Claude Code entry

`.claude/skills/` registers short adapters for the shared Skills in `.agents/skills/`. Load only the Skill and references relevant to the task, reusing guidance already read. Shared workflow changes belong in the canonical `.agents/skills/` files.

The Astra/Sol delegation policy is Codex-only. For Claude, every subagent (Agent tool calls and workflow `agent()` calls) passes an explicit `model`:

- Sonnet 5.5 by default.
- Opus only for the final pre-merge review or verdict, hunting multiplayer sync or netcode defects, and comparing and choosing a design.
- Ultracode follows the same rule; wide fan-out stages run on Sonnet. See [agent instructions and Skills](CONTRIBUTING.md#agent-instructions-and-skills) for the layout and setup checks.
