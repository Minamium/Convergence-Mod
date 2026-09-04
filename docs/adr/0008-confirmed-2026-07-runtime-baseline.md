# ADR-0008: Confirmed 2026.07 Windows Runtime Baseline

- Status: Accepted
- Date: 2026-09-05
- Decider: Minamium

## Context

The bootstrap candidate was Terraria 1.4.4.9, tModLoader v2026.06.3.6, and Calamity 2.2.2. By the first Windows handoff run, the official installed stable environment had advanced to tModLoader v2026.07.3.0 and Calamity 2.2.4 with Calamity Music 2.1.

At clean commit `b34adbc18fc5191280e5681a041686699255068b`, that installed combination passed repository checks, the dependency-free domain harness, command build, Build + Reload, Single Player, Dedicated Server, and a two-client loopback smoke. The sanitized result is committed as [2026-09-05 Windows baseline evidence](../evidence/2026-09-05-windows-baseline.json).

Calamity's public source mirror still exposes a 2.2.2 reference point. The verified 2.2.4 Workshop binary therefore cannot be represented as source-equivalent to that mirror.

## Decision

The Stage A Windows baseline is pinned to:

- Terraria 1.4.4.9;
- tModLoader stable v2026.07.3.0, source commit `666f69962d3bdffde54fc14025f02634965b4e7c`;
- Calamity Mod 2.2.4 and Calamity Music 2.1;
- .NET SDK 8.0.424 with the .NET 8/C# 12 settings supplied by tModLoader targets.

Raise the compile-time dependency floor and runtime minimum from Calamity 2.2.2 to 2.2.4. Keep the runtime upper bound exclusive at 2.3.0 and fail encounter activation closed outside `2.2.4 <= version < 2.3.0`.

This ADR supersedes only the numerical Calamity lower bound in [ADR-0004](0004-calamity-compatibility-boundary.md). The isolated compatibility boundary, defensive API validation, and prohibition on private/internal patching remain accepted. The public 2.2.2 source commit remains a reference-only research point; no 2.2.4 implementation detail may be inferred from it.

Optional quality-of-life or development Mods are excluded from the compatibility baseline. They may be enabled in a separate local development profile, but failures must be reproduced against the three-Mod Calamity Music/Calamity/Convergence baseline before classification.

## Consequences

Positive:

- the repository matches the actual verified Windows runtime instead of an unavailable handoff candidate;
- dependency rejection messages use the minimum version that was actually exercised;
- future drift has an exact binary checksum, runtime source commit, and reproducible gate record.

Costs and risks:

- Calamity 2.2.2 installations no longer satisfy the Addon dependency;
- public-source research trails the verified Workshop binary and cannot justify 2.2.4-specific implementation assumptions;
- every future tModLoader or Calamity update requires a separate compatibility change and the full baseline gate.

## Alternatives

- Keep the 2.2.2 candidate despite running 2.2.4: rejected because it would claim an untested lower bound.
- Track whatever Steam currently installs without a pin: rejected because a moving dependency cannot support reproducible multiplayer evidence.
- Treat the public 2.2.2 mirror as the 2.2.4 binary source: rejected because that equivalence has not been established.
