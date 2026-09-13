# Convergence Mod

![Convergence promotional artwork: dark blades and pale light in a cathedral](docs/media/convergence-banner.png)

Convergence is a playable development mod for Terraria / tModLoader. It adds a cooperative raid and an independent boss, with combat tuned around Calamity's endgame equipment. The raid combines bullet dodging with group mechanics, a shared arena, and teammate revival.

[Current status](docs/STATUS.md) · [Contributing](CONTRIBUTING.md) · [Development setup](docs/DEVELOPMENT.md) · [Documentation](docs/README.md)

The banner is promotional artwork, not a gameplay screenshot. [Artwork provenance](docs/evidence/2026-09-12-readme-artwork.json).

## Content

- **The Unfortunate Doll Play** — a raid against **Lacrimosa — The Bound Heart**, recommended for 2–4 players. Fight through multiple phases using coordinated Stack and Spread mechanics, damage windows, and instant teammate revival. Survive the final sequence to earn weapon reward boxes. [Raid guide](docs/encounters/first-severance/README.md) · [Combat specification](docs/encounters/first-severance/ENCOUNTER_SPEC.md).
- **Ghost Samurai** — an independent dual-wielding boss with travelling slash waves, dash attacks, lattice patterns, and wisps. It uses its own summon and normal player death, not the raid's Ready or revival system. [Boss specification](docs/encounters/ghost-samurai/ENCOUNTER_SPEC.md).

Content, balance, visuals, and compatibility are still being revised. The [status page](docs/STATUS.md) identifies the latest installed development build, verification evidence, and outstanding issues. Development-only solo raid admission is a testing aid, not a balanced solo mode; companions do not replace raid participants.

## Requirements and starting a raid

Use the supported tModLoader version and dependency versions in the [version matrix](docs/VERSION_MATRIX.md). **Calamity Mod and Luminance are required**, together with the dependencies requested by tModLoader. Recommended equipment is Calamity endgame gear. Client and server must run matching Convergence builds and protocols.

1. Place a **Foundation Core** pedestal with enough unobstructed arena space.
2. Hold the **Theater Doll** activation item and click the pedestal to deploy preparation. This item is separate from the summon weapon and is not consumed.
3. Every admitted player must mark **Ready**. The raid starts after everyone is ready.

See [arena setup](docs/ARENA_INFRASTRUCTURE.md) for placement/admission rules and [recovery](docs/encounters/first-severance/REVIVE_SPEC.md) for Down and revival behavior. Ordinary gameplay and Reload checks are distinct from successful compilation.

The raid's former name, `First Severance`, remains in stable code, asset, packet, and document identifiers. Its public rename does not migrate saved items or network IDs.

## Development

Clone into a source directory named `Convergence`:

```sh
git clone https://github.com/Minamium/Convergence-Mod.git Convergence
cd Convergence
python -m pip install -r tools/requirements-ci.txt
python .agents/skills/develop-convergence-raids/scripts/verify_repo.py .
```

This runs repository checks, not a Mod build. Follow [Development](docs/DEVELOPMENT.md) and the [Windows runbook](docs/runbooks/WINDOWS_DEVELOPMENT.md#diagnose-or-build) to configure local dependencies and produce a recorded package. Choose checks by change type using the [verification matrix](.agents/skills/develop-convergence-raids/references/verification-matrix.md).

```text
Common/              Shared infrastructure, networking, authority, domain, compatibility
Content/Encounters/  Encounter-specific gameplay
Client/              Rendering, audio, UI, accessibility
Tests/               Terraria-independent domain and codec checks
docs/                Specifications, workflow, status, evidence
.agents/skills/      Task-specific development and research guidance
```

Work from integrated main in a scoped branch or worktree. Shared play packages follow the [integration workflow](CONTRIBUTING.md#shared-development). Game outcomes remain server/Single Player authoritative, with only the explicitly documented [Ghost Samurai native-wave exception](docs/adr/0023-ghost-samurai-native-wave-damage.md).

## Reports and release planning

Report reproducible issues through [GitHub Issues](https://github.com/Minamium/Convergence-Mod/issues). Include the Mod/tModLoader versions, single-player or multiplayer mode, relevant enabled Mods, reproduction steps, and a short relevant log excerpt. Remove personal paths, names, chat, and credentials; do not upload saves or full raw logs by default.

The proposed 0.3.x release track is in [Release Process](docs/RELEASE_PROCESS.md#03x-github-prerelease-plan). Planning is not a published release or a claim that all compatibility checks passed.

## License and credits

Maintained by [Minamium](https://github.com/Minamium), with contributions including Ghost Samurai by [mac10101010](https://github.com/mac10101010). See [contributors](https://github.com/Minamium/Convergence-Mod/graphs/contributors) for the project history.

Source and asset distribution licenses have not yet been selected. Repository access does not grant reuse or redistribution rights. Agreed collaboration and public distribution follow [Contributing](CONTRIBUTING.md#contribution-scope-and-licensing) and [Release Process](docs/RELEASE_PROCESS.md). Asset, music, and generated-art sources and permissions are recorded in [Attribution](Assets/ATTRIBUTION.md).
