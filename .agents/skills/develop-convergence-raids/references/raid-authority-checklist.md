# Raid Authority Checklist

## Identity and commands

- Scope every command by protocol version and the smallest relevant stable identity.
- Resolve the sender from the trusted transport argument.
- Reject stale Encounter Sequence, Fight ID, participant generation, request nonce, and revision.
- Bound enums, counts, strings, coordinates, and payload sizes before allocation or mutation.
- Parse the complete typed payload before validation and mutation.

## Downed and Revive

- Treat lethal damage interception as a server adapter feeding a pure Raid-domain transition.
- Do not assume another Mod returning `false` from `ModPlayer.PreKill` stops later hooks. tModLoader combines every registered result, so prototype the pinned Calamity revival effects and define precedence before enabling a death-interception adapter.
- Store Downed start/expiry ticks, not client wall-clock duration.
- Validate reviver/target membership, connection generation, alive/Downed state, range, movement, damage interruption, token availability, and one active channel per actor.
- Collect the complete bounded set of revive-start requests for one authority tick and resolve it in stable Participant ID order; packet arrival order must not choose a target or spend the last token.
- Apply every authority command for a tick before one explicit commit evaluates failure. Reject commands for an already committed tick and keep the terminal event last.
- Give each channel a nonce/lease identity; a delayed cancellation must not affect a newer channel from the same reviver.
- Deduct a token only on successful authoritative completion unless the encounter specification explicitly chooses another policy.
- Make duplicate completion and cleanup idempotent.
- Evaluate all-participant Downed and unrecoverable expired Downed states on the authority tick.
- Define disconnect and rejoin behavior before enabling the hook.

## Arena and outsiders

- Resolve the actual server-side Core Tile Entity before accepting arena bounds.
- Validate the complete arena without mutation, then create owned transient resources.
- Keep barriers logical; avoid generating hundreds of wall tiles or projectiles.
- Correct participants server-side with hysteresis to avoid permanent rubber-banding.
- Give outsiders a visible warning and grace interval, then deterministic ejection or encounter-scoped punishment. Avoid copying Dungeon Guardian damage blindly.
- Bind participant and outsider boundary episodes to a server-assigned connection epoch, and re-resolve the current slot/epoch immediately before correction or exclusion.
- Revalidate teleport, recall, hook, mount, dash, knockback, and external-mod movement paths.

## Boss and mechanics

- Keep phase transitions, assignments, DPS windows, part state, weak points, random selection, and victory on the server.
- Represent telegraphs as timed facts; clients render from server ticks.
- Make DPS checks explicit windows with a server-owned target and result.
- Prefer soft failures and escalation stacks before hard enrage unless the specification requires a wipe.
- Use stable owned-entity handles and defensive cleanup scans scoped by Fight ID.

## Replication and recovery

- Publish state only after an atomic authoritative transition.
- Make full snapshots convergent and ordered by Encounter Sequence, revision, and authority tick.
- Do not let a delta bridge an unknown Fight ID or revision gap; request a full snapshot.
- Keep presentation events bounded and disposable. Rejoin correctness comes from snapshots, not replaying every effect.
