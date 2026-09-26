# Sol implementation delegation under Astra

Apply only when the current agent is the top-level `gpt-6-astra` agent doing implementation. This project policy authorizes a bounded Sol worker when its work is independent and Astra can make useful progress concurrently. Other lead models and workers do not activate this policy; Luna and log-analysis delegation are outside this trial.

## Choose a useful split

- Give Sol a coherent implementation with a defined outcome, stable inputs and an owned file/module scope. An isolated renderer component or a specified mechanic adapter can qualify. Astra keeps requirements, cross-cutting authority/lifecycle decisions and integration.
- Handle small edits, documentation-only work, unresolved design or tightly coupled changes locally. Do not manufacture subtasks merely to use a cheaper model. Use the fewest workers that remove meaningful work from Astra.
- If Sol or delegation is unavailable, continue locally in Astra; do not silently substitute a different worker model or stop an otherwise feasible task.

## Launch with a compact brief

Explicitly select `gpt-6-sol` and start at `medium` reasoning; raise effort only when the assigned logic warrants it. An unspecified model can inherit Astra. Use a fresh, bounded context (`fork_turns: "none"` where supported), not a full conversation fork.

Pass the outcome, exact checkout/base state and owned files, relevant constraints/spec sections, applicable checks and expected return. Point to the repository agreement and necessary feature guidance rather than copying the whole document set. Supply any uncommitted inputs the worker actually needs; a new worktree does not inherit them.

Assign disjoint edit scopes. Use explicit worktrees when isolation is needed, following [shared development](../../../../CONTRIBUTING.md#shared-development). Also separate build/test output paths, or let Astra run the shared build after integration. Do not replace another worker's package or run competing builds against the same artifacts.

The worker implements its assigned scope and applicable checks, returns changed files, check results and remaining uncertainty, and does not recursively delegate. Return a broken assumption to Astra instead of expanding scope. Astra advances different work while the worker runs; it does not implement the same slice again in parallel.

## Integrate and finish

Review the worker's actual diff and relevant evidence, resolve integration issues, and apply the existing [verification matrix](verification-matrix.md) to the combined result. Reuse valid checks on unchanged inputs rather than repeating every worker check. The worker's completion message alone does not establish correctness or visual acceptance; Astra owns the final outcome and user-facing report.

Astra also owns [retiring worker branches/worktrees](../../../../CONTRIBUTING.md#finish-merged-work) after their work is integrated and no worker or process needs them. Delegation alone does not require a new branch, worktree or PR.

Include one compact delegation note in the existing task result or handoff: worker ID/path, actual model/effort, assigned scope and outcome, including material rework. Do not add a separate report, raw logs or a token benchmark to every implementation.

## Later efficiency review, only when requested

Use the native parent/child session records and the delegation note. Preserve the pre-policy baseline and later aggregates locally under ignored `.local/`; do not commit private session history. Identify the policy commit/date and comparison window.

Sum per-request usage across the parent and its workers, deduplicating response IDs and excluding inherited history/cumulative counters. Separate uncached input, cached input and output; reasoning tokens are part of output, not an extra amount. Include planning, review, repairs and retries. Record missing usage as unknown rather than zero.

Compare tasks with similar scope and acceptance criteria using total weighted cost, elapsed time, successful checks and material rework. Keep raw token count, credit/API cost and subscription allowance distinct; use the applicable model/speed rates at the time. An uncontrolled before/after comparison is an estimate, not proof of a savings percentage.

Model selection and inheritance are documented in [OpenAI Subagents](https://learn.chatgpt.com/docs/agent-configuration/subagents) and [Models](https://learn.chatgpt.com/docs/models) (checked 2026-09-23). The Astra-only scope above is this project's trial policy.
