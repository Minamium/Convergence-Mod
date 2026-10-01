@AGENTS.md

# Claude Code entry

`.claude/skills/` registers short adapters for the shared Skills in `.agents/skills/`. Load only the Skill and references relevant to the task, reusing guidance already read. Shared workflow changes belong in the canonical `.agents/skills/` files.

The Astra/Sol delegation policy is Codex-only; Claude model selection and delegation remain with the current Claude task configuration. See [agent instructions and Skills](CONTRIBUTING.md#agent-instructions-and-skills) for the layout and setup checks.
