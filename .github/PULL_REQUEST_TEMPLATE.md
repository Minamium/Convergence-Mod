## Summary

Describe the player-facing or architectural outcome.

## Authority and lifecycle

- Authoritative owner:
- Client request/replica changes:
- Snapshot or rejoin behavior:
- Cleanup path:

## Verification

- [ ] `python3 tools/repository_checks.py`
- [ ] `python3 tools/validate_yaml.py`, or no YAML changed
- [ ] `dotnet run --project Tests/Convergence.DomainTests/Convergence.DomainTests.csproj`, or no Raid-domain code changed
- [ ] `dotnet build ConvergenceMod.csproj`, or no C# changed
- [ ] tModLoader build + reload
- [ ] Single Player smoke test, or not applicable
- [ ] Host & Play smoke test, or not applicable
- [ ] Dedicated Server smoke test, or not applicable
- [ ] 2/3/4-player behavior considered
- [ ] High-latency/disconnect behavior considered
- [ ] Documentation or ADR updated
- [ ] Asset attribution updated, or no assets changed

## Test environment

- tModLoader:
- Calamity Mod:
- Other content mods:
- Player count / latency:
