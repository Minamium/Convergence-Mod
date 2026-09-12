# Repository Working Agreement

This file applies to the entire repository.

## Start with the smallest useful context

1. Read this agreement once per task. For code/behavior work, read only `docs/STATUS.md`'s Current build, Verification state, and Next change sections, then inspect the affected code.
2. Use the task table in `docs/README.md` to select additional sections. No blanket full-document, architecture, ADR, handoff, or research reading is required. Text-only corrections need only the affected passage and its conventions.
3. Reuse context already read in this task; reread only when relevant files change, scope expands, or a concrete uncertainty remains. Search headings/symbols with `rg` before opening long files.
4. Briefly state the change and applicable verification. Identify authority, replication, and cleanup owners when those responsibilities change; omit unrelated checklist fields.
5. Follow the current user-requested scope and active specification. Historical slices and backlog do not add work or reinstate superseded gates.

Current feature naming is `FirstSeverance` / `first_severance`. The isolated source/key/failure-prefix rename is complete; `ThirdSeverance` forms may remain only in deliberately historical records or completed-rename instructions, and no compatibility alias is required for the unpublished identifier.

## Repository Skills

- Use `.agents/skills/develop-convergence-raids` for Boss/Raid code or behavior implementation/review. Documentation wording and repository housekeeping do not need the gameplay workflow.
- Use `.agents/skills/research-tmodloader-sources` when API behavior or another public Mod implementation must be investigated. Record exact versions, source paths, licenses, observations, and independent design decisions.
- Keep always-on rules here and task-specific repeatable procedures in Skills. Repository Skills are excluded from `.tmod` packaging.

## Non-negotiable boundaries

- Gameplay state and outcomes are server/Single Player authoritative.
- Clients send bounded requests and consume read-only snapshots/events.
- `Common` never depends on `Content` or presentation-only `Client` code.
- Encounter-specific behavior enters through definition-scoped policies and runtime factories; do not add feature switches to global policy, coordinator, or packet-router code.
- Calamity access stays in `Common/Compatibility/Calamity`.
- Every transient world resource has one exact-Fight owning runtime and an idempotent cleanup path.
- Active encounters are ephemeral and at most one may exist per World initially.
- Dedicated Server paths must not initialize graphics or audio.
- Explicit packet IDs are never renumbered; parse bounded DTOs completely before authority validation/mutation.

## Documentation contract

- `docs/STATUS.md` is the canonical implementation-state record. Other documents may include a short context summary only when they link back to `docs/STATUS.md`; conflicting or detailed status belongs there.
- Feature spec owns player-visible behavior; feature plan owns work order; backlog cannot expand current scope.
- Accepted ADRs own structural decisions and are superseded, not silently rewritten.
- Indexed docs use the front-matter model in `docs/DOCUMENTATION_SYSTEM.md`.
- Update only the document owning the changed fact; add links rather than repeating status, tuning, or procedures elsewhere. Small tuning/text changes do not require a new ADR or research report.
- Regenerate the catalog once after the indexed-doc edits are settled; include it with the change. Do not regenerate after every individual edit.
- Never commit personal absolute paths, credentials, raw logs, worlds/players, `.tmod` binaries, or local semantic-search databases.

## Verification

- The command entry point and change-based requirements live in [.agents/skills/develop-convergence-raids/references/verification-matrix.md](.agents/skills/develop-convergence-raids/references/verification-matrix.md). Run its static wrapper once at completion, adding domain/build/runtime checks for the affected behavior.
- Do not repeat passing checks for unchanged inputs/environment without a failure or unresolved concern. Full release and compatibility matrices belong to their declared gates, not every edit.
- Record relevant runtime checks as passed, failed, or `not_run` with the remaining action. A build is not a playtest; missing evidence cannot satisfy an activation, compatibility, or release gate that depends on it.

## Assets and external material

- Do not vendor Calamity binaries, source mirrors, or extracted assets. Third-party recordings may be vendored only when the repository owner explicitly approves the exact work for this project, the source terms permit game use of the committed form, and `Assets/ATTRIBUTION.md` records the creator, source, terms, and exact modifications. Never treat game-use permission as permission for standalone redistribution; recheck the governing source terms before public release.
- Do not commit concept/raw asset directories or generated build output.
- Add an exact `Assets/ATTRIBUTION.md` record for every distributable image, audio, music, or font asset.
- No release or external contribution acceptance occurs until source and asset licenses are selected.

## Change discipline

- Prefer small, concern-focused commits.
- Update docs or an ADR with changes to authority, protocol, persistence, dependencies, module direction, or rights policy.
- Preserve terminal snapshots, stable packet IDs, machine-readable failure codes, and cleanup invariants.

## Parallel development and Ghost Samurai handoff

- 次のGhost Samurai作業は、`git fetch origin`後の最新`origin/main`を基点とする。`feature/ghost-samurai-phases-1-2`（実装最終`d4ad7d2`）は統合用の旧ブランチとして残し、ここから次の実装を継続しない。mainに`d4ad7d2`と再召喚修正`9464e9c`の両方が含まれることを確認する。未統合なら旧版で作業を進めず、統合待ちであることを報告する。
- 未コミット・未プッシュの作業を保全してから、最新mainから目的別の新しいfeatureブランチ／worktreeで再開する。既存worktreeの強制切替、hard reset、force push、旧ファイル一式での上書きはしない。
- Ghost SamuraiとDollは各機能のContent／Client／テスト／仕様内で並行開発する。Commonの通信・終了・cleanupを変更した場合は、統合済みmainを通じて共有し、他機能の修正を取り落とさない。
- 統合担当は既存の高いModバージョンを巻き戻さず、通信変更に必要なprotocol更新を保持する。STATUSは最新mainの他機能情報を残して統合し、カタログは最後に生成する。
- 共有プレイ用の`Convergence.tmod`へのインストールは統合mainから行う。feature側の検証は明示した別出力／プロファイルで行い、正本ModSourcesのjunctionや共有ビルドを勝手に差し替えない。コンパイル成功と実機確認は区別する。
