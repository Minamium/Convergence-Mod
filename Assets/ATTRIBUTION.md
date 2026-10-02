# Asset Attribution Register

## Records

### Pale Meridian weapon cues — 2026-10-03

Twenty-three cues of the refreshed Pale Meridian (the music-box siege rifle; [weapon spec](../docs/encounters/first-severance/WEAPONS.md#pale-meridian--refreshed-ranged-2026-10)): twenty-two Vorbis one-shots and one sample-exact PCM16 WAV loop. [`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns the windows, filters, pitches, gains, timings (on the weapon's score ticks), loudness targets and source hashes; [`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py) owns the original synthesis (music-box comb tooth on the F minor pentatonic ladder, brass ratchet, porcelain ring and crack, additive flue organ, shimmer, low thump), with a few weapon-local blocks in the generator (brass ring, coil-spring twang, band-swept air, a soft-mallet gong resonance for the two closing strikes); the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) and [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) are reused unmodified. The three Kenney recordings are CC0 1.0 files already recorded in the Ebon Manor reward audio table of this register; they stay in the local store, are SHA-256 verified before use and are not committed. The nine notes are pure synthesis, one file per ladder step (never transposed at runtime). Loudness follows the Ebon scale: BS.1770 K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after encoding (for the loop, including its wrap). The audition page and report stay in the git-ignored `.local`.

| Key | Store or repository file | Source | Source SHA256 |
|---|---|---|---|
| metal_click | sfx-sources/cc0/pack-OGA-Kenney-RPGsounds.zip!OGG/metalClick.ogg | Kenney RPG Audio metalClick.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0) | `9851a69d0c613e13bceef08060ecc4148f098ef487927cbebe270d642398a3b3` |
| metal_latch | sfx-sources/cc0/pack-OGA-Kenney-RPGsounds.zip!OGG/metalLatch.ogg | Kenney RPG Audio metalLatch.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0) | `ba9ba60b172b3ebc131a940f25793cd2e207aca7af73dc80d637277f060f1708` |
| metal_pot | sfx-sources/cc0/pack-OGA-Kenney-RPGsounds.zip!OGG/metalPot1.ogg | Kenney RPG Audio metalPot1.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0) | `159def979e8e386c2c539f5e99cc30a080eb2dcb6c911fa2e4ccc0785b2522fd` |

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianAssemble.ogg`
- Asset ID: doll-weapon-sfx-meridianassemble-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.50 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: metal_latch in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -17.0 LUFS (played at volume 0.55: -22.2 LUFS effective), true peak -8.2 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `194ff024bbb4066a348b5a7eb02abfd7dd3f3701110247d5ea8d996916b1d626`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianNote0.ogg`
- Asset ID: doll-weapon-sfx-meridiannote0-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.90 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -16.9 LUFS (played at volume 0.5: -23.0 LUFS effective), true peak -10.7 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `5ab7f54645b1674262a1b2ab673b3127cd40e0d967d63b0915bad1652664f30a`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianNote1.ogg`
- Asset ID: doll-weapon-sfx-meridiannote1-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.90 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -16.9 LUFS (played at volume 0.5: -23.0 LUFS effective), true peak -11.1 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `cc73bd2e120dacfe403d4b45b6654f2fbfc74530a1947207ac44f7608b172e5e`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianNote2.ogg`
- Asset ID: doll-weapon-sfx-meridiannote2-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.89 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -17.0 LUFS (played at volume 0.5: -23.0 LUFS effective), true peak -11.3 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `739b0148142facf15ff7f6817e17c1dfaf61c6a7ca12ec5816f848b32f833b21`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianNote3.ogg`
- Asset ID: doll-weapon-sfx-meridiannote3-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.90 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -17.0 LUFS (played at volume 0.5: -23.0 LUFS effective), true peak -11.1 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `822a9a408a25bc20da933959328db7dd5b65c0d73e811a0ca9ca730994159702`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianNote4.ogg`
- Asset ID: doll-weapon-sfx-meridiannote4-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.90 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -16.9 LUFS (played at volume 0.5: -23.0 LUFS effective), true peak -12.0 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `c312b042581788f6fcc73c0de68a82d2d63584301831ef48f3e8eb27a891b503`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianNote5.ogg`
- Asset ID: doll-weapon-sfx-meridiannote5-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.90 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -16.9 LUFS (played at volume 0.5: -23.0 LUFS effective), true peak -12.3 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `29215522ee38cefc4277bd10e7afcb89056819d08e61cd1f99d9ccedfc0b6561`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianNote6.ogg`
- Asset ID: doll-weapon-sfx-meridiannote6-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.88 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -16.9 LUFS (played at volume 0.5: -23.0 LUFS effective), true peak -11.7 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `bc3aad28db1bbeddce18fdb8453183cafc0a25abb9945495405a3d7bab4364ed`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianNote7.ogg`
- Asset ID: doll-weapon-sfx-meridiannote7-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.88 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -17.0 LUFS (played at volume 0.5: -23.0 LUFS effective), true peak -11.9 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `873c467bde8bee18dfe73fadc88318baa91aac86e72d895b0b0197726f06a01b`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianNote8.ogg`
- Asset ID: doll-weapon-sfx-meridiannote8-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.87 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -17.0 LUFS (played at volume 0.5: -23.0 LUFS effective), true peak -11.2 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `a220052ab7f6cd9afdc81bc87bcd4cecead4708d5e5cc13d7335a6aba8f6b353`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianPartWarn.ogg`
- Asset ID: doll-weapon-sfx-meridianpartwarn-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.36 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -17.0 LUFS (played at volume 0.45: -23.9 LUFS effective), true peak -7.8 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `c061a0e6bf6008dc8ae30812dfdfcbec6482e2b33dc92aeb02d68670bdb2f03f`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianPartFire.ogg`
- Asset ID: doll-weapon-sfx-meridianpartfire-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.36 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: metal_latch in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -13.0 LUFS (played at volume 0.5: -19.1 LUFS effective), true peak -3.5 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `50600bef59460242026d710457be41cd55d1ab27d46ff944ec6d1917f6bd7c55`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianIgniteWarn.ogg`
- Asset ID: doll-weapon-sfx-meridianignitewarn-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.86 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: metal_click in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -13.0 LUFS (played at volume 0.6: -17.5 LUFS effective), true peak -4.2 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `7b57dc63e3ced4d280d6f16642e466e437f50240328bc28e8feaf7e36a153fd4`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianIgniteFire.ogg`
- Asset ID: doll-weapon-sfx-meridianignitefire-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (1.01 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: metal_click in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -11.5 LUFS (played at volume 0.75: -14.0 LUFS effective), true peak -5.2 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `f9e6faaa1a065babad00cdfd8178772c4f03a0eb49f6c6eafff7d17c87ab4abc`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianLoop.wav`
- Asset ID: doll-weapon-sfx-meridianloop-20261003
- Asset type: stereo 44.1 kHz PCM16 WAV seamless loop, 105840 samples = 144 game ticks (2.40 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 PCM16 WAV
- Human modifications: original synthesis; short-term loudness -17.0 LUFS (played at volume 0.5: -23.0 LUFS effective), true peak -6.5 dBFS; circular filtering and an equal-power crossfade of the overhang into the head, no trim or fade
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak, loop-seam and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `ddd0a8409346012abf924e9451cf94a0277a12263197f7a170e7be8cf980c02d`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianHeavy.ogg`
- Asset ID: doll-weapon-sfx-meridianheavy-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.55 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: metal_pot in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -13.0 LUFS (played at volume 0.5: -19.0 LUFS effective), true peak -6.2 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `98a0a8804c2c030054294405ddf2407bb7f3c4bde12c5c33a43d281637ee67cd`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianStrikeWarn.ogg`
- Asset ID: doll-weapon-sfx-meridianstrikewarn-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.42 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -13.0 LUFS (played at volume 0.6: -17.4 LUFS effective), true peak -6.0 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `3cf62ac96e2ba1df8313e248b2d32b7867c21a859cd919a89ad8dacb7139be32`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianStrikeFire.ogg`
- Asset ID: doll-weapon-sfx-meridianstrikefire-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (1.95 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: metal_pot in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -11.5 LUFS (played at volume 0.85: -12.9 LUFS effective), true peak -4.8 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `6e2d5b324a24c62d4af04db40555a3338a289ab7e920cf771388ae0f4bb03b1f`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianStrikeMiss.ogg`
- Asset ID: doll-weapon-sfx-meridianstrikemiss-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.60 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -17.0 LUFS (played at volume 0.55: -22.2 LUFS effective), true peak -3.7 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `2ac293c946a969c8cac35f784f1ea0c6c2279af1aadfc52c0132500138461966`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianLatticeWarn.ogg`
- Asset ID: doll-weapon-sfx-meridianlatticewarn-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.42 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -13.0 LUFS (played at volume 0.6: -17.4 LUFS effective), true peak -6.0 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `1faaa99781cd852677f6d2b5e331d6690d4840f063bc5cb038b16cd7ed56ac91`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianLatticeFire.ogg`
- Asset ID: doll-weapon-sfx-meridianlatticefire-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (2.68 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -11.5 LUFS (played at volume 0.9: -12.4 LUFS effective), true peak -5.1 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `36adbab1447815725626b7d71c4f4812df2278795a615b0e22bebe717551f01b`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianHit.ogg`
- Asset ID: doll-weapon-sfx-meridianhit-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.18 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -20.1 LUFS (played at volume 0.35: -29.2 LUFS effective), true peak -5.5 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `19bce4a754a54a6eb88392d32826a4c0890c931f01f20da9a595c994d724fe12`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/MeridianHitHeavy.ogg`
- Asset ID: doll-weapon-sfx-meridianhitheavy-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.42 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -17.0 LUFS (played at volume 0.5: -23.1 LUFS effective), true peak -5.2 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and score-tick alignment checks); owner audition pending; in-game mix not_run
- SHA256: `1f39873232ef5e9278baea603cbf9153fa1f15336dab8ea122f423eb6158b86f`

### Pale Meridian energy material — 2026-10-03

The light of the refreshed Pale Meridian on the shared Doll weapon layer ([weapon spec](../docs/encounters/first-severance/WEAPONS.md#pale-meridian--refreshed-ranged-2026-10)).

- Runtime file: `Assets/AutoloadedEffects/Shaders/DollMeridianEnergy.fxc`
- Asset ID: dollmeridianenergy-20261003
- Asset type: compiled original material
- Creator: project-owner-directed original work with Anthropic Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: original Convergence HLSL source alongside runtime export
- Tool/model/version: FXC fx_2_0 O3; compiler/source/export hashes in compiled.json
- Human modifications: Original repository-owned DollMeridianEnergy.fx, evaluated per art dot in the Doll weapon layer's half-resolution Light target and quantized to the Doll light ramp with a world-stable Bayer dither (its palette block is identical to DollPixel.fx): the meridian/lattice packet (a white-hot spine and head over a pearl-violet body thinning to a plum rim, with drifting sparkles), round wakes, round glows with an optional ring, and a cooling residue that crumbles to plum; flowing noise samples Luminance's own TurbulentNoise and WavyBlotchNoise at runtime (not copied); every choice is branch-free (no uniform-only branch). No texture, Calamity or other third-party art/code/sample imported.
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance and generation disclosure
- Reviewer and review date: Claude offline compiled-material review with `tools/preview-doll-meridian.ps1` (the real presentation, exported PNGs and Luminance noise; pixel checks) 2026-10-03; native playtest not_run

### Lacuna Testament cues — 2026-10-03

The seventeen cues of the refreshed Lacuna Testament (the magic Doll reward weapon): sixteen stereo Vorbis one-shots and one sample-exact stereo PCM16 WAV loop of exactly 176,400 frames (4.0 s, eight of the beam's 30-tick visual pulse periods). [`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns each cue's recipe, its baked beats against the weapon's tick schedule (mirrored from `LacunaTestamentScore` and pinned by `tools/tests/test_doll_weapon_audio.py`), the loudness tiers and the source hashes; [`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py) owns the original synthesis (music-box comb tooth on the F minor pentatonic ladder, brass ratchet, porcelain ring and crack, additive flue organ, shimmer, low thump; the generator adds a gong-like plate tuned into the key, also additive synthesis), and both reuse the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) and [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) unmodified. Every layer is original synthesis except one Kenney recording (`metalLatch`, the CC0 1.0 file already recorded in the Ebon Manor reward audio table of this register) under the great aperture's clank; it stays in the local store, is SHA-256 verified before use and is not committed. The loop is periodic by construction (whole cycles on its 0.25 Hz grid, FFT-synthesised noise on its own bins, circularly placed tings, a high-pass over three periods), so its wrap is as smooth as its inside. Loudness follows the Ebon scale (BS.1770 K-weighted maximum 400 ms short-term LUFS; true peak at most -1 dBTP after the Vorbis round trip, or over the loop played round). Nothing is transposed at runtime except the two single-pitch cues (`LacunaIrisTine`, `LacunaPelletFire`, every pitched layer a C, recorded at C6 and played on the ladder step of each iris); every composite cue and the loop play as rendered. The audition page and report stay in the git-ignored `.local`.

| Key | Store or repository file | Source | Source SHA256 |
|---|---|---|---|
| metal_latch | sfx-sources/cc0/pack-OGA-Kenney-RPGsounds.zip!OGG/metalLatch.ogg | Kenney RPG Audio metalLatch.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0) | `ba9ba60b172b3ebc131a940f25793cd2e207aca7af73dc80d637277f060f1708` |

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaIrisWarn.ogg`
- Asset ID: doll-weapon-sfx-lacunairiswarn-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.42 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -17.1 LUFS (played at volume 0.7: -20.2 LUFS effective), true peak -2.0 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `52bb1e6166664656d2ed8fe7662c1fe74be8588122997d9e232986083bb0b078`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaIrisFire.ogg`
- Asset ID: doll-weapon-sfx-lacunairisfire-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.36 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -20.0 LUFS (played at volume 0.8: -21.9 LUFS effective), true peak -5.0 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `5948e130706efdc3c9283c51cebb718f554e239229563a2a06b3c767acf28399`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaIrisTine.ogg`
- Asset ID: doll-weapon-sfx-lacunairistine-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.70 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -20.0 LUFS (played at volume 0.9: -20.9 LUFS effective), true peak -14.4 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `c22daeb5d8dd0b912e9904a6f5d4f36c6ede3360426b59cdfe3145bcfc2262ff`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaPelletWarn.ogg`
- Asset ID: doll-weapon-sfx-lacunapelletwarn-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.16 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -20.9 LUFS (played at volume 0.55: -26.1 LUFS effective), true peak -1.3 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `1f40b840738368eca55260f568ee094ddeb06bb8aa0c07244ab4bab4600d70a6`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaPelletFire.ogg`
- Asset ID: doll-weapon-sfx-lacunapelletfire-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.36 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -20.0 LUFS (played at volume 0.7: -23.1 LUFS effective), true peak -10.5 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `d4500431428fd9e9f18e11898a0f56340cb808ab09a8e54388d3955ad6c1a3a2`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaPelletHit.ogg`
- Asset ID: doll-weapon-sfx-lacunapellethit-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.22 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -20.0 LUFS (played at volume 0.6: -24.4 LUFS effective), true peak -4.1 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `6b0f021392c23d55c118bb081d7789534688d513a651a523f026356550275d27`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaMergeWarn.ogg`
- Asset ID: doll-weapon-sfx-lacunamergewarn-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.42 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -13.0 LUFS (played at volume 0.8: -15.0 LUFS effective), true peak -1.5 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `239230f8fe8d61d76ffb78c26a2f969203c7cc81301b38006275d24593a341e0`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaMergeFire.ogg`
- Asset ID: doll-weapon-sfx-lacunamergefire-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.76 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: metal_latch in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -12.3 LUFS (played at volume 0.85: -13.7 LUFS effective), true peak -1.3 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings and project-owned masters; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `cafd27aff735bdd0298ccd561496626e67d482262defc9d1587c1131b4166589`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaBeamWarn.ogg`
- Asset ID: doll-weapon-sfx-lacunabeamwarn-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.71 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -13.0 LUFS (played at volume 0.85: -14.4 LUFS effective), true peak -4.9 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `b1c972b4ee29d3b190f4dc6e609c8ded5fb6a8c7fc62ab050782bffd60460e9d`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaBeamFire.ogg`
- Asset ID: doll-weapon-sfx-lacunabeamfire-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (2.57 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -11.5 LUFS (played at volume 0.9: -12.4 LUFS effective), true peak -4.1 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `ebff7a9145a06fca40c9a4116144474e3dc18c93b009cb4cb7e48f517cea3c91`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaBeamLoop.wav`
- Asset ID: doll-weapon-sfx-lacunabeamloop-20261003
- Asset type: stereo 44.1 kHz PCM16 WAV Doll weapon loop (4.00 s, 176400 frames)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 PCM16 WAV (sample-exact loop)
- Human modifications: periodic original synthesis; short-term loudness -14.0 LUFS (played at volume 0.7: -17.1 LUFS effective), true peak -9.9 dBFS; loop wrap step 0.0008 against 0.0471 inside
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `52f058f643642ce4183a41dddf0f5b0eed32fe5f3b0a2032417c85ba2a297c58`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaWiden1.ogg`
- Asset ID: doll-weapon-sfx-lacunawiden1-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.58 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -17.0 LUFS (played at volume 0.75: -19.4 LUFS effective), true peak -7.3 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `391e8aba1e45d22e50131cb9c358a81b13c95d1687ca6140143f18d218eb5828`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaWiden2.ogg`
- Asset ID: doll-weapon-sfx-lacunawiden2-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.58 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -17.0 LUFS (played at volume 0.75: -19.5 LUFS effective), true peak -7.4 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `6f5f07c09c440df4fcfb603b5278365bb7dc2b07aa546d47c7dedbee0656c8ec`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaWiden3.ogg`
- Asset ID: doll-weapon-sfx-lacunawiden3-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.93 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -13.0 LUFS (played at volume 0.75: -15.5 LUFS effective), true peak -4.9 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `e8019060e7080d0fb3582b96f93ff9a83a2eb58897c7e4528e99a8b6f82fa528`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaBeamHit.ogg`
- Asset ID: doll-weapon-sfx-lacunabeamhit-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.26 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -20.0 LUFS (played at volume 0.6: -24.4 LUFS effective), true peak -6.1 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `34eaec84cba0afb3adc23b8dd6514b6493d3196536b610706319a9d06af7feb9`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaBeamEnd.ogg`
- Asset ID: doll-weapon-sfx-lacunabeamend-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.68 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -13.0 LUFS (played at volume 0.8: -15.0 LUFS effective), true peak -3.0 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `9141d5eaa3a9e8ca59e9748ba1df4c9fefca407a9a641591102d90097ef7bdc5`

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/LacunaBeamMiss.ogg`
- Asset ID: doll-weapon-sfx-lacunabeammiss-20261003
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (0.89 s)
- Creator: synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis; short-term loudness -13.0 LUFS (played at volume 0.8: -14.9 LUFS effective), true peak -4.9 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-03 (deterministic regeneration, length, loudness, true-peak and loop-seam checks); owner, 2026-10-03 (approved on the audition page); in-game mix not_run
- SHA256: `27222a3dd45eeb4e32a0b03467a3cbc0f33bd7b56911c4c72c0821c0ab38e30f`

### Lacuna Testament void material — 2026-10-03

The Lacuna Testament's own light material for the shared Doll weapon layer ([WEAPONS.md](../docs/encounters/first-severance/WEAPONS.md#magic--lacuna-testament)).

- Runtime file: `Assets/AutoloadedEffects/Shaders/DollLacunaEnergy.fxc`
- Asset ID: dolllacunaenergy-20261003
- Asset type: compiled original material
- Creator: project-owner-directed original work with Anthropic Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: original
- Source work and URL: original Convergence HLSL source alongside runtime export
- Tool/model/version: FXC fx_2_0 O3; compiler/source/export hashes in compiled.json
- Human modifications: Original repository-owned DollLacunaEnergy.fx, evaluated per art dot in the Doll weapon layer's half-resolution Light target and quantized to the Doll palette (its palette block is identical to DollPixel.fx): a void beam (near-black core with plum streaks drifting inward and rare pearl sparks, a one-dot pearl lip, violet/pearl rims whose folds flow outward, a one-dot pearl silhouette on the collision edge, a dithered halo, travelling pulses, a white-hot opening, a far-end cap and a mask that keeps the great aperture in front of its own light), void holes with octant-exact one-dot pearl lips and inward-spiralling violet arms, and pellet wakes; flowing Luminance WavyBlotchNoise and TurbulentNoise sampled at runtime (not copied). Written without uniform-only branches and compiled without any preshader: the flow time and sparkle thresholds are computed on the CPU and reach the pixel shaders through the vertex shader. The drawn hard edge runs at the collision width to the beam's far end, where only the core closes. No texture, Calamity or other third-party art/code/sample imported.
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance and generation disclosure
- Reviewer and review date: Claude offline compiled-material review with `tools/preview-doll-lacuna.ps1` (real Lacuna sprites and Emit sequence, zoom 1, dark and bright ground, pixel checks, a boss forecast under the beam, the running-dry ending) 2026-10-03; native playtest not_run

### Scarlet Invocation reward weapon audio — 2026-10-03

Thirty-five cues in 38 files (`OrganShot` is one file per pipe) for the Scarlet Score Reliquary and the five Scarlet Invocation reward weapons ([rewards spec](../docs/encounters/crimson-foundry/REWARDS.md#art-and-audio)). On 2026-10-03 the owner auditioned two takes (A and B) of each of 28 cue blocks on the local audition page and chose **B** for Cadence, ReliquaryOpen, ScytheWhip, StaffWindup, StaffBarline and RiverRelease and **A** for every other block (the Toll block is the whole Toll0–Toll7 ladder; the OrganShot block is all four pipe files of take A). That choice is the owner's approval of these takes: 37 files are the auditioned masters byte for byte, and ReliquaryOpen is its master with the show's level lift baked in on 2026-10-03 (loudness revision below). Each cue layers CC0 recordings from the VSCO 2 CE and VCSL sample libraries, Freesound and OpenGameArt; no synthesized tone or noise layer is used. No local recording of fire, pouring liquid, wax, bone or paper exists, so the recipe uses the stand-ins named per file (wind and the organ blower for flame, bubbles for liquid, wood chops and stone grains for bone and wax, book pages for paper). The recordings, libraries, recipe and audition page stay outside the repository; no raw sample is distributed.

Rights, checked 2026-10-03 from local records and license files (no source is unconfirmed, so no cue is held back): the thirteen Freesound and OpenGameArt recordings carry the same SHA-256 as rows already in this register (column "Same file in"), whose records state that each page showed Creative Commons 0 (https://creativecommons.org/publicdomain/zero/1.0/) when surveyed on 2026-10-01; Freesound files are the public HQ preview renders of those CC0 uploads. The eight Kenney files come from the local `pack-OGA-Kenney-RPGsounds.zip` (SHA256 `3ae398ad63e293f9c450bda22d5d81c3af69c74df66fc1400f33c012c0bbc231`), whose bundled `license.txt` states CC0 1.0 with optional credit; all eight are already itemised in the Ebon records. VSCO 2 CE 1.1.0 ships a CC0 1.0 `LICENSE`; every VSCO file used here was compared byte for byte with its member of the local release archive. Its organ folder's `Info.txt` credits Simon Dalzell (Ivy Audio), grants redistribution from Versilian Studios and encourages credit; its `Readme.txt` asks that the samples not be sold directly (none are distributed) and for credit to Versilian Studios / Sam Gossner and/or Ivy Audio / Simon Dalzell with a link to the VSCO: CE homepage (https://versilian-studios.com/vsco-community/), so a public credit for these cues carries both names and that link. VCSL is CC0 1.0 per its repository README ("no credit, no special terms"); every VCSL file used here is tracked and unmodified at the recorded commit.

| Library | Version and URL | SHA256 or commit | Terms |
|---|---|---|---|
| VSCO 2 Community Edition (Versilian Studios / Sam Gossner; organ by Ivy Audio / Simon Dalzell) | 1.1.0 release archive, https://github.com/sgossner/VSCO-2-CE/archive/refs/tags/1.1.0.zip; homepage https://versilian-studios.com/vsco-community/ | `4a4446628df0e1a12aaee58e9f65f8fa7cde51971e961abb1b43083a6d3a8ab7` | CC0 1.0 (bundled `LICENSE`); `Readme.txt` requests credit and the homepage link, and no direct sale of the samples |
| Versilian Community Sample Library (VCSL) | https://github.com/sgossner/VCSL | commit `b6e6ac82d22248edee98a0bde185eb9ef6d439ad` | CC0 1.0 (repository README) |

| Key | Recording / sample | Creator | Source | Local source SHA256 | License | Same file in |
|---|---|---|---|---|---|---|
| air_cut | Swosh / Whoosh / Air Cut | qubodup | https://freesound.org/s/60030/ | `0301adf448c60b80c09b89df57510fd09949d6b15bb457ef7c9e70999b8a2ad0` | CC0 1.0 (page surveyed 2026-10-01) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01; Ghost Samurai recorded attack audio — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |
| anime_shing | Anime_drama_shing_sword_2.wav | Euphrosyyn | https://freesound.org/s/529019/ | `a8278823afb4c25a06d55ec2adfdeb7993bb738b1077310555be1e592063d02f` | CC0 1.0 (page surveyed 2026-10-01) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Ghost Samurai recorded attack audio — 2026-10-01; Soboro recorded blade audio — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |
| bass_drum2 | Bass Drum 2 `bassdrum_hit_ff.wav` | Versilian Studios | VCSL `b6e6ac8` | `d0ce17649553655127ee27dff312ece64c9ac698a9b66a470c3b51f244892c7b` | CC0 1.0 (VCSL README) | — |
| bell_As3 | Tubular Bells 1 `chimes_A#3_ff_rr1.wav` | Versilian Studios | VCSL `b6e6ac8` | `55fcfdb825026629670e3486f47762e6a3c4a89e138990fbd0e65c3f0a1d6a79` | CC0 1.0 (VCSL README) | — |
| bell_As3_pp | Tubular Bells 1 `chimes_A#3_pp_rr1.wav` | Versilian Studios | VCSL `b6e6ac8` | `72b7336f9746949ea586c6f37bd815ceffed0bddee82eb74c3e7c8b358153e99` | CC0 1.0 (VCSL README) | — |
| bell_E3 | Tubular Bells 1 `chimes_E3_ff_rr2.wav` | Versilian Studios | VCSL `b6e6ac8` | `086a46cb560dae926014c0494965034aeecdffade7a1d6f54feda13274775a11` | CC0 1.0 (VCSL README) | — |
| bell_E3_pp | Tubular Bells 1 `chimes_E3_pp_rr1.wav` | Versilian Studios | VCSL `b6e6ac8` | `52cba7a19b646eeeae995cf3d2c1cefb5f32f0c0d4e74bbb570793a292a9db9e` | CC0 1.0 (VCSL README) | — |
| bell_Fs3 | Tubular Bells 1 `chimes_F#3_fff_rr1.wav` | Versilian Studios | VCSL `b6e6ac8` | `4664366c705496c3dc5ba9de75ffcd6739610dc669391cadda34a63cbcfa1251` | CC0 1.0 (VCSL README) | — |
| bloody_blade | Bloody Blade 2.wav | Kreastricon62 | https://freesound.org/s/323526/ | `42f16fc3e230066399282c90187e83d20fecd851f32cdf1553b9a9e1225dcb98` | CC0 1.0 (page surveyed 2026-10-01) | Soboro recorded blade audio — 2026-10-01 |
| blower | Organ `Rode_Blower.wav` (the organ blower) | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `f02ac90e80c862d3710320fc21392532f91496a07de6f5ee37c44344f9546c34` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| book_flip | bookFlip3.ogg, RPG Audio | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `c85db5dceb3f1df073e960630277eaa88a5afda0477c1ddd68dad707621767be` | CC0 1.0 (pack `license.txt`) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01 |
| bubbles | Misc 1 `bubbles.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `f119e6f7be7e1a754ed6d1f2e196cbf78e67aa8ae97efe6fb4252997823408eb` | CC0 1.0 (VSCO 2 CE `LICENSE`) | Cathedral recorded audio (SFX v2) — 2026-10-02 |
| bubbles2 | Misc 1 `bubbles2.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `99fc88599996e388856c4b73c18dc64bd73e3120b1e0b66657d22f49e4c86b4b` | CC0 1.0 (VSCO 2 CE `LICENSE`) | Cathedral recorded audio (SFX v2) — 2026-10-02 |
| chain_grind | Misc 1 `chain_grind.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `dfc9a7ec579f91d080460a4a617f0b5cd28518bc849f05f0a459841c2aececf4` | CC0 1.0 (VSCO 2 CE `LICENSE`) | Cathedral recorded audio (SFX v2) — 2026-10-02 |
| chain_loop | Misc 1 `chain_loop.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `1b5ff4216d0b4a9689446933c84dd3194fc503402c23ca5a1764eea95593395a` | CC0 1.0 (VSCO 2 CE `LICENSE`) | — |
| chime_A4 | Hand Chimes `sus_A4_r01_main.wav` | Versilian Studios | VCSL `b6e6ac8` | `43604b3b049f3d7c60c6e5c8601d7027045ded1f124debcd65c2ccef723daaa1` | CC0 1.0 (VCSL README) | — |
| chime_As3 | Hand Chimes `sus_A#3_r01_main.wav` | Versilian Studios | VCSL `b6e6ac8` | `2eb505e93627247114582d8e103d77ff427ab2e882bb73e30925d265c09210a4` | CC0 1.0 (VCSL README) | — |
| chime_C3 | Hand Chimes `sus_C3_r01_main.wav` | Versilian Studios | VCSL `b6e6ac8` | `47bec5759eb7b435c255dd7df38fbbd41306b75a4000a890b951b3640f0af50a` | CC0 1.0 (VCSL README) | — |
| chime_D3 | Hand Chimes `sus_D3_r01_main.wav` | Versilian Studios | VCSL `b6e6ac8` | `fc9e1ee181909bf8b1d82b31e26af6379c3a84def609befd98db1752e260a561` | CC0 1.0 (VCSL README) | — |
| chime_D4 | Hand Chimes `sus_D4_r01_main.wav` | Versilian Studios | VCSL `b6e6ac8` | `ab6598ca5d49f8d19f14b9e5fdfbdb35353ca7cc83e7d4f78946b6faa9371c67` | CC0 1.0 (VCSL README) | — |
| chime_E3 | Hand Chimes `sus_E3_r01_main.wav` | Versilian Studios | VCSL `b6e6ac8` | `7bfca4820f7eb14582354809ef297622c60c5f2cda1071859b5f6b9a093b239d` | CC0 1.0 (VCSL README) | — |
| chime_E4 | Hand Chimes `sus_E4_r01_main.wav` | Versilian Studios | VCSL `b6e6ac8` | `7196b23009e1acc8e03591c35e54144487991c75cfe22413e84e0bbfeb478caf` | CC0 1.0 (VCSL README) | — |
| chop | chop.ogg, RPG Audio | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `d00c2b3c9fff07e376145c8c8c45c90e5084ec192f6ce0387db233f7b86f1486` | CC0 1.0 (pack `license.txt`) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01; Soboro recorded blade audio — 2026-10-01 |
| concrete_smash | Concrete SMASH 2 | magnuswaker | https://freesound.org/s/522099/ | `b182dec5699903068a509113e09e0b4c02a78f42b7aa73f875b23fc24ce34c6e` | CC0 1.0 (page surveyed 2026-10-01) | Ghost Samurai recorded attack audio — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |
| creak1 | creak1.ogg, RPG Audio | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `8a346186fd297254248cab8e8117060a52a5cf2a84f603153a762108550ea95e` | CC0 1.0 (pack `license.txt`) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01 |
| creak2 | creak2.ogg, RPG Audio | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `8a990afdc03aebb91d528f5385e2f95582dbfa8e2c12c71098ab01be9142294a` | CC0 1.0 (pack `license.txt`) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01 |
| cymbal_cresc | Suspended Cymbal 1 `susCymb1_cresc_2s.wav` | Versilian Studios | VCSL `b6e6ac8` | `a5507bf116b33ee220f4eff54506be56fd455e908c8d53903f217590341e5008` | CC0 1.0 (VCSL README) | — |
| cymbal_roll | Suspended Cymbal 1 `susCymb1_roll_mp1_nloop.wav` | Versilian Studios | VCSL `b6e6ac8` | `6f5850ecaf6a002c59b91c7577e0d76d26fdd485eeb11e116472bf97451483fd` | CC0 1.0 (VCSL README) | — |
| door_close | doorClose_4.ogg, RPG Audio | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `fd21c0e7a9d0317375d2561590f0770dd3380ee35507d064862cb44d6f71595b` | CC0 1.0 (pack `license.txt`) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01 |
| energy_wave | sword slash energy wave | greyfeather | https://freesound.org/s/724716/ | `5b9fbd1c8b78cd2e69c0ebfd178e229308b37058fe71da1cd4311c7f70a94b59` | CC0 1.0 (page surveyed 2026-10-01) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Ghost Samurai recorded attack audio — 2026-10-01; Soboro recorded blade audio — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |
| gong_p | Gong 1 `gong_p.wav` | Versilian Studios | VCSL `b6e6ac8` | `3d103e1a7af12eeb17e0c5488f11933ca92b9fbdefa39b8020b64238da2db0d0` | CC0 1.0 (VCSL README) | Cathedral recorded audio (SFX v2) — 2026-10-02 (as VSCO 2 CE `Percussion/gongHit_p.wav`, the same bytes) |
| knife_slice | knifeSlice2.ogg, RPG Audio | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `6c2064d0ef988d1ec3d56868e823ea8823a5cac00f2742560052633529407def` | CC0 1.0 (pack `license.txt`) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |
| low_impact | Very low frequency impact.wav | AudioPapkin | https://freesound.org/s/541029/ | `73c25c4f49baa34cb9ad42290324fc61340124028dc0161299880b78580e335a` | CC0 1.0 (page surveyed 2026-10-01) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |
| metal_click | metalClick.ogg, RPG Audio | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `9851a69d0c613e13bceef08060ecc4148f098ef487927cbebe270d642398a3b3` | CC0 1.0 (pack `license.txt`) | Doll weapon audio foundation and companion summon — 2026-10-02; Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01 |
| metal_pot | metalPot1.ogg, RPG Audio | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `159def979e8e386c2c539f5e99cc30a080eb2dcb6c911fa2e4ccc0785b2522fd` | CC0 1.0 (pack `license.txt`) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |
| organ_04 | Organ `Loud/Rode_Man3Open_04.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `7ce6ac35c8f951d36a1ebe66043f5a3e1ae3ca6bb3c45c1e74d1a3f64834fa5c` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| organ_16 | Organ `Loud/Rode_Man3Open_16.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `aeb0fb90c0b8f8c34bb71c998b63ef485e43522071aa39085045b7f50179bb33` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| organ_22 | Organ `Loud/Rode_Man3Open_22.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `4b58d8b01067b740b6170e6a3900bd27adb7cf2ace7d48033759826a6e4b2316` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| organ_28 | Organ `Loud/Rode_Man3Open_28.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `c8eb1bff00e0261bb403d6a245f54087808c3cb77c03914e97d0f606205895c7` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| organ_31 | Organ `Loud/Rode_Man3Open_31.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `5b0043d88777245103c73f04a5569b9e4e2cd143e21eeebdffd8b3684eec9df6` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| organ_34 | Organ `Loud/Rode_Man3Open_34.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `dc5ad93f22d343ea345d3964e7c7cf1d62a3229f8a2660c12a035473743470b2` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| organ_40 | Organ `Loud/Rode_Man3Open_40.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `cd5305ad957832c8f87d3676ba3fe47751de3817a793d676362438509a415268` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| organ_46 | Organ `Loud/Rode_Man3Open_46.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `5ac10b40fb5cb026e3e487e05e24a1b0aeda35fa8df956d48f8c9d9fd18bff38` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| organq_137 | Organ `Quiet/NT5_Man3Quiet_137_rr1.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `eb92c50999e9a117939de73e74b263730b66eadbdb868f67b9bd89681ed8f113` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| organq_143 | Organ `Quiet/NT5_Man3Quiet_143_rr1.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `c39ec6c7e26dee6a3661e54a96c4a2a04bce08130864244458a7db1b627b3c93` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| organq_149 | Organ `Quiet/NT5_Man3Quiet_149_rr1.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `fa2784dca7e6ef639be46a2d9f129f480755b678c9cce145d257bb59839a77a9` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| organq_152 | Organ `Quiet/NT5_Man3Quiet_152_rr1.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `7ad4206bebfaffe36d3c506e55670abf4a5bed9c72d52a8eea44fe5a20c62b9a` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| pedalq_067 | Organ `Quiet/NT5_PedalQuiet_067_rr1.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `2a9ea128eb632baa1b221206b2f38f9d7b1f440566dea52fdb0ace1919a04305` | CC0 1.0 (VSCO 2 CE `LICENSE`; organ `Info.txt` grants redistribution) | — |
| rock_tumble | Rock Tumble 2.wav | _stubb | https://freesound.org/s/389618/ | `199521191be552261d6e604c8d34e40cfeac4b3d7f3075906dd27182c73adb4a` | CC0 1.0 (page surveyed 2026-10-01) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01; Ghost Samurai recorded attack audio — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |
| samurai_slash | samurai slash | nekoninja | https://freesound.org/s/370204/ | `283b188b2f04f6676ae23be36e58a536bb78d5e7cf4bf5ab8cc95ca13b0065c2` | CC0 1.0 (page surveyed 2026-10-01) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Ghost Samurai recorded attack audio — 2026-10-01; Soboro recorded blade audio — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |
| slit_hi | Slit Drum `LogDrumHi_MedM_v1_rr1_Sum.wav` | Versilian Studios | VCSL `b6e6ac8` | `6ca70964e7d1dd80daf6c67cff226db6e0cadbff3234944de9055c3ef1d7f4eb` | CC0 1.0 (VCSL README) | — |
| slit_lo | Slit Drum `LogDrumLo_MedM_v2_rr1_Sum.wav` | Versilian Studios | VCSL `b6e6ac8` | `609a9ae5441a007371e0dc3120d7e6d4ad5d23073f5f4fb3f16d0dc08823dfc4` | CC0 1.0 (VCSL README) | — |
| stick_woosh | Woosh (stick swung in the air) | Dalesome | https://freesound.org/s/352719/ | `5dc0966b3f689fde08955ab18a3b8dc636cc3db96d105e90b427af54184c3016` | CC0 1.0 (page surveyed 2026-10-01) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Ghost Samurai recorded attack audio — 2026-10-01; Soboro recorded blade audio — 2026-10-01 |
| swish | swish-4.wav, Swishes Sound Pack | artisticdude | https://opengameart.org/content/swishes-sound-pack | `0060f4a7040edce4cc50d1daa10a9cb76764128a942e4688339e69cd1d5d784c` | CC0 1.0 (page surveyed 2026-10-01) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01 |
| swoosh | swoosh.wav | PorkMuncher | https://freesound.org/s/263595/ | `5d11ca0d7ad2ad4bc3108c0b017cccd9ae3e002277e1550fa78693841ea85058` | CC0 1.0 (page surveyed 2026-10-01) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01; Ghost Samurai recorded attack audio — 2026-10-01; Soboro recorded blade audio — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |
| timpani1 | Timpani 1 `Hit/Timpani1_Hit_v4_rr1_Sum.wav` | Versilian Studios | VCSL `b6e6ac8` | `b51d81444425e1ba0c09d94dbdc384a8cd4b4681c42959cc8676ccaf6d6ea793` | CC0 1.0 (VCSL README) | — |
| wind_whirl | Wind Whirl (Small Air Blow) | DARTEKZ_GAMEZ | https://freesound.org/s/719560/ | `5b41e14eaa752d4715ee7c706b99581f3adf5b02630c1d6c565b445a4e725c75` | CC0 1.0 (page surveyed 2026-10-01) | Ghost Samurai recorded attack audio — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |
| woodblock_ff | Woodblock `wood_click_ff.wav` | Versilian Studios | VCSL `b6e6ac8` | `9a911c66212d50a09494821bf9af8ce6e007612e232b5905f838348cb1d63b75` | CC0 1.0 (VCSL README) | — |
| woosh | woosh | florianreichelt | https://freesound.org/s/683096/ | `3c641d4d6ea0c6b65423d8fe1a7d72bf7bfb08a91c1640f9e9a0ab9d5d23b265` | CC0 1.0 (page surveyed 2026-10-01) | Waltz of the Ebon Manor reward weapon audio — 2026-10-02; Waltz of the Ebon Manor — 2026-10-01; Cathedral recorded audio (SFX v2) — 2026-10-02 |

Recipe and common processing (external `sfx-scarlet-rewards` recipe, run with Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0 and soundfile 0.14.0 / libsndfile 1.2.2; SHA256 `common.py` `a6dcd0d619d9198081e6acdd69f944b08678f66febc3f844eb95c37019ffe4d4`, `kit.py` `239413c681c416cfb0844e984be64b8fd199f2ea28a9049dc748684905e88e15`, `parts.py` `c7133f1937a9a6ad7b06d02a85b9931931c1b509a002dbc437d640215c76367d`, `cues_shared.py` `224136bda754edfe75791f10260e2a1758f75f3a12cba85698a354d96a67114e`, `cues_scythe.py` `8fa903eaeab35cb44d044578603001a5370adce18b3f2c9b3679f6f9e633f7e0`, `cues_organ.py` `6de52aa5424c70685dd1fb759ec31e858d860293d1f28fe545c37b1566f1b035`, `cues_baton.py` `f1e921f6ef3e904ac9381a40868162fa83504511f6d6c43b79a628da3ed9dc4d`, `cues_censer.py` `ddc546a9d1da6dbcd776c17846e1396c6e058ba30df2323d74f8d185658990bf`, `cues_quill.py` `d1e81ed8a94039b386101e0e5c224c50c457e45504e92e0950412e9888f5532f`, `render.py` `b500b4c4b851b56c9351179c6fe5c90a9f364cbe6c9e97db60176f3afbbf6928`, `probe.py` `c2968ecf938aaa7a47020ccea71e55bdb1d746f08fd708286cd9b9e01f5b5357`, `sources.lock.json` `9f0bf56ba8ec5bcc9324e541ad7e56ab19d285dbaaa66ca91c9bdb0e5da540f4`). Every source is read through the lock, which refuses a file whose SHA-256 differs. Sources are decoded and resampled to 48 kHz stereo (polyphase). A tuned layer is resampled sampler-style (speed follows pitch) from its measured pitch (the recipe's reference table, measured 2026-10-02) to an exact E♭, F or B♭ target, and the recipe refuses any tuned layer off those three; drums, gongs and bells are tuned by the shift that puts their strong partials on E♭/F/B♭. Windows get 2 ms / 4 ms fades; filters are second-order Butterworth high/low/band-pass and RBJ shelves; envelopes are dB breakpoints; each layer is level-matched on its K-weighted 100 ms peak and placed on a timeline. Space is short reflections of the cue itself: convolution with a deterministic early-reflection, allpass and comb kernel (a processing kernel; no audio is generated). The master removes DC, fades the head 2 ms and the tail (a quarter of the length, 30–300 ms, unless noted), prepends 3 ms of silence, sets the maximum 400 ms momentary loudness (meant as BS.1770 K-weighting on the Ebon scale; found on 2026-10-03 to run the K-weighting biquads across the two channels instead of along time, so in effect it weights no frequency; [the rewards spec](../docs/encounters/crimson-foundry/REWARDS.md#art-and-audio) lists both measurements) to the role target, applies a level-matched tanh glue, limits peaks only above -1.6 dBTP (at most 4 dB of gain reduction for build, per-shot and windup cues, 6 dB for releases), high-passes at 18 Hz and keeps the true peak at or under -1 dBTP after the Vorbis round trip. Targets follow the Raid's loudest strike when the cues were rendered (-7.6 LUFS on this meter, measured on the Raid's 2026-09-18 CrownRupture strike, since retired with the Raid's own sound set; the Doll beam and chorus cues the Raid then played reached -7.7 to -8.4 LUFS at their call volumes): finale -9.6, cadence -11.0, show -13.0, windup -15.0 (a ceiling), release part -15.5 (CenserPour -16.0, InkBlaze -12.0), per-shot -17.0 and build step -19.0 LUFS (ceilings). Those are the levels on the recipe's meter at render time; the loudness revision below lifts the show file, and in the game each cue plays at its role's offset ([rewards spec](../docs/encounters/crimson-foundry/REWARDS.md#levels-against-the-raid)). Encoding is 48 kHz stereo Vorbis at compression level 0.5 with the Ogg serial pinned from the take's name (the first four bytes of SHA-256 of `scarlet-rewards/<take>`). A fresh render of the 35 picked takes from this recipe and lock reproduced all 38 auditioned files byte for byte on 2026-10-03; the shipped ReliquaryOpen is its master after the lift below, which `lift.py` reproduces byte for byte.

**Loudness revision — 2026-10-03 (0.3.82).** The owner found the effects too quiet in play. The revision ([evidence](../docs/evidence/2026-10-03-scarlet-loudness.json)) stages every role against Graceful Ordeal and the Raid's lifted cues with one offset per role, so the balance inside a role stays the auditioned one, and checks the cues that sound in play in a rendered four-player fight. The tolls, one-shots, windups and cascade parts share one runtime offset (-3.5 dB; 0.3.78 played them at -8.5 dB), and the finales and the Cadence play at -0.45 dB, so those 37 files ship as auditioned. Only the reliquary's opening show had to rise above its file, by +3.83 dB. The show's lift is baked in by the external `sfx-scarlet-loud/lift.py` (SHA256 `63876b78053e5fa6135ad7a3b45904f85cef69ee3c2b5002974a87ef1dea6d5c`, run with the same Python and libraries as the recipe). It rebuilds the picked take's master from this recipe and its sources, refuses to continue unless that master encodes to the auditioned file's exact bytes, multiplies it by one gain and, only where the Vorbis round trip would otherwise exceed -1.0 dBTP, runs a look-ahead limiter (5 ms look-ahead, 80 ms release, true-peak detection by 4x polyphase oversampling, linked channels; its ceiling starts at -1.3 dBTP and is lowered until the decoded true peak is at or under -1.0 dBTP; at most 3 dB of gain reduction). The gain is the smallest that brings the file's maximum 400 ms momentary loudness (ITU-R BS.1770-4, the standard's 48 kHz K-weighting along time, padded 0.2 s before and 0.5 s after, 10 ms steps) within 0.1 dB of its auditioned level plus the lift; no limiting was needed. Encoding is the recipe's own (48 kHz stereo Vorbis at compression level 0.5 with the take's pinned serial, so the serial still names the take). No layer, envelope, filter, length or timing changes. A second run reproduced the file byte for byte, and its entry gives the gain and levels.

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/ReliquaryOpen.ogg`
- Asset ID: scarlet-reward-sfx-reliquaryopen-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, opening show (2.30 s), layered from CC0 recordings
- Creator: recordings by _stubb, DARTEKZ_GAMEZ, Kenney, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_E3, bell_Fs3, blower, chain_grind, chime_A4, chime_As3, chime_C3, chime_D3, chime_D4, chime_E3, chime_E4, chop, door_close, gong_p, organq_137, organq_143, organq_149, organq_152, pedalq_067, rock_tumble, wind_whirl, woodblock_ff in the table above
- Tool/model/version: external recipe cues_shared.py `reliquary_B` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `ReliquaryOpen_B`; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of take B for the reliquary opening show. Timed to the 60-tick opening show. Ticks 0-10: heat-crackle stand-in of 8-25 ms rock_tumble grains high-passed at 2.2 kHz (-14 dB, rising) and chain_grind (0.12-0.29 s) high-passed at 1.5 kHz (-22 dB) for the one-dot tremble. Tick 10 (0.167 s): chop high-passed at 1.2 kHz (-6 dB; wax-crack stand-in), woodblock_ff tuned to E♭6 (80 ms, high-passed at 900 Hz, -14 dB) and door_close low-passed at 3.5 kHz and decayed over 0.32 s (-10 dB) as the lid. Ticks 10-20: a rising run of hand chimes on B♭3 E♭4 F4 B♭4 E♭5 F5 B♭5 (chime_C3, chime_D3, chime_E3, chime_As3, chime_D4, chime_E4, chime_A4 tuned; -8 dB rising 0.6 dB a step, one every 24 ms). Tick 20: a 1.25 s cadence (-1 dB) in the Cadence B voicing: quiet organ E♭3, B♭3, E♭4 and F4 (organq_137, organq_143, organq_149, organq_152, each tuned from its measured pitch; release 0.65 s, levels 0/-2/-3/-2 dB, swelling -3 dB to 0 dB at 0.15 s and back to -12 dB at the end), pedal E♭2 (pedalq_067, low-passed at 900 Hz, -6 dB), tubular bells bell_Fs3 to F4 (-6 dB, +6 ms) and bell_E3 to E♭3 (-8 dB), hand chime chime_As3 to B♭4 (-12 dB, +30 ms) and Gong 1 gong_p tuned so its strongest partial is E♭3 (low-passed at 2 kHz, -4 dB, decaying -4 dB by 0.3 s and -20 dB at the end), reflections wet -4 dB (size 1.3, 0.9 s). Ticks 44-60: burn stand-in, an 18-tick flame stand-in (wind_whirl band-passed 120 Hz-5 kHz plus the organ blower 250 Hz-6 kHz at -6 dB) (-16 dB) and rock_tumble grains (-20 dB). Reflections wet -10 dB (size 1.1, 0.7 s). Stand-in layers as named. Target -13.0 LUFS; measured -13.0 LUFS (400 ms momentary maximum), true peak -7.41 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +3.83 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -16.15 → -12.30 LUFS (+3.84 dB of the show's planned +3.83 dB), true peak -3.29 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `bd59fb61adefbf0628dfb01cba82ae3c06ace89cd62ec1ca66b709dbfd52e69b`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/Toll0.ogg`
- Asset ID: scarlet-reward-sfx-toll0-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, build step (0.97 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_E3_pp, chime_C3, chime_D3, slit_hi in the table above
- Tool/model/version: external recipe cues_shared.py `_toll_cue` (parts.py `toll`, variant A) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `Toll0_A`
- Human modifications: owner selection of take A for the build toll 0 (E♭3). Toll 0 of the E♭ sus2 ladder at E♭3, every layer on E♭ (each layer's octave as given): the slit_hi knock tuned to E♭3, head-trimmed, decayed over 0.3 s and low-passed at 2.5 kHz (-0.0 dB, reflections send -12 dB); tubular bell bell_E3_pp tuned to E♭3 (0.72 s, low-passed at 9 kHz, -3.0 dB, +2 ms); the hand chime chime_C3 tuned to E♭3 (0.72 s, -2.0 dB, +6 ms) and an octave-up chime_D3 at E♭4 (0.43 s, -10 dB); reflections wet -6 dB (size 0.9, 0.6 s); 80 ms tail fade. Target -19.0 LUFS; measured -19.0 LUFS (400 ms momentary maximum), true peak -11.70 dBTP; strike pitch +0.8 cents from E♭3. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `e83b10b0bd10e151cfecc6d3cd9a92c15f2adbbcce66f456dd448535249322a2`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/Toll1.ogg`
- Asset ID: scarlet-reward-sfx-toll1-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, build step (0.97 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_E3_pp, chime_C3, chime_E3, slit_hi in the table above
- Tool/model/version: external recipe cues_shared.py `_toll_cue` (parts.py `toll`, variant A) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `Toll1_A`
- Human modifications: owner selection of take A for the build toll 1 (F3). Toll 1 of the E♭ sus2 ladder at F3, every layer on F (each layer's octave as given): the slit_hi knock tuned to F3, head-trimmed, decayed over 0.3 s and low-passed at 2.5 kHz (-0.4 dB, reflections send -12 dB); tubular bell bell_E3_pp tuned to F3 (0.69 s, low-passed at 9 kHz, -3.3 dB, +2 ms); the hand chime chime_C3 tuned to F3 (0.72 s, -1.7 dB, +6 ms) and an octave-up chime_E3 at F4 (0.43 s, -10 dB); reflections wet -6 dB (size 0.9, 0.6 s); 80 ms tail fade. Target -19.0 LUFS; measured -19.0 LUFS (400 ms momentary maximum), true peak -11.88 dBTP; strike pitch +0.7 cents from F3. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `7cff1ae62d514cd5d83ddfd73ceda3e153cc77626f2206a45ebce858c6c0a1f4`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/Toll2.ogg`
- Asset ID: scarlet-reward-sfx-toll2-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, build step (0.97 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_As3_pp, chime_C3, slit_lo in the table above
- Tool/model/version: external recipe cues_shared.py `_toll_cue` (parts.py `toll`, variant A) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `Toll2_A`
- Human modifications: owner selection of take A for the build toll 2 (B♭3). Toll 2 of the E♭ sus2 ladder at B♭3, every layer on B♭ (each layer's octave as given): the slit_lo knock tuned to B♭2, head-trimmed, decayed over 0.3 s and low-passed at 2.5 kHz (-0.9 dB, reflections send -12 dB); tubular bell bell_As3_pp tuned to B♭3 (0.66 s, low-passed at 9 kHz, -3.6 dB, +2 ms); the hand chime chime_C3 tuned to B♭3 (0.72 s, -1.4 dB, +6 ms); reflections wet -6 dB (size 0.9, 0.6 s); 80 ms tail fade. Target -19.0 LUFS; measured -19.0 LUFS (400 ms momentary maximum), true peak -12.38 dBTP; strike pitch +0.1 cents from B♭3. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `269eb14fb46516128ba657039db8e059f3f63d908950c5acaf730f8254a4f1b0`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/Toll3.ogg`
- Asset ID: scarlet-reward-sfx-toll3-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, build step (0.97 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_E3, chime_D3, slit_hi in the table above
- Tool/model/version: external recipe cues_shared.py `_toll_cue` (parts.py `toll`, variant A) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `Toll3_A`
- Human modifications: owner selection of take A for the build toll 3 (E♭4). Toll 3 of the E♭ sus2 ladder at E♭4, every layer on E♭ (each layer's octave as given): the slit_hi knock tuned to E♭3, head-trimmed, decayed over 0.3 s and low-passed at 2.5 kHz (-1.3 dB, reflections send -12 dB); tubular bell bell_E3 tuned to E♭4 (0.63 s, low-passed at 9 kHz, -3.9 dB, +2 ms); the hand chime chime_D3 tuned to E♭4 (0.72 s, -1.1 dB, +6 ms); reflections wet -6 dB (size 0.9, 0.6 s); 80 ms tail fade. Target -19.0 LUFS; measured -19.0 LUFS (400 ms momentary maximum), true peak -12.50 dBTP; strike pitch +0.8 cents from E♭4. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `16d38edb3c0ca5e674f5923b2aa3251aa54b5f7e318d37745ebd956c00c2786e`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/Toll4.ogg`
- Asset ID: scarlet-reward-sfx-toll4-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, build step (0.97 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_Fs3, chime_E3, slit_hi in the table above
- Tool/model/version: external recipe cues_shared.py `_toll_cue` (parts.py `toll`, variant A) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `Toll4_A`
- Human modifications: owner selection of take A for the build toll 4 (F4). Toll 4 of the E♭ sus2 ladder at F4, every layer on F (each layer's octave as given): the slit_hi knock tuned to F3, head-trimmed, decayed over 0.3 s and low-passed at 2.5 kHz (-1.7 dB, reflections send -12 dB); tubular bell bell_Fs3 tuned to F4 (0.60 s, low-passed at 9 kHz, -4.1 dB, +2 ms); the hand chime chime_E3 tuned to F4 (0.72 s, -0.9 dB, +6 ms); reflections wet -6 dB (size 0.9, 0.6 s); 80 ms tail fade. Target -19.0 LUFS; measured -19.0 LUFS (400 ms momentary maximum), true peak -11.48 dBTP; strike pitch -1.1 cents from F4. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `ff3cb6a1ce5b13cbe4dd7b14b7931c23cd4863becef9157df4ca022f13b3e737`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/Toll5.ogg`
- Asset ID: scarlet-reward-sfx-toll5-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, build step (0.97 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_As3, chime_As3, slit_lo in the table above
- Tool/model/version: external recipe cues_shared.py `_toll_cue` (parts.py `toll`, variant A) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `Toll5_A`
- Human modifications: owner selection of take A for the build toll 5 (B♭4). Toll 5 of the E♭ sus2 ladder at B♭4, every layer on B♭ (each layer's octave as given): the slit_lo knock tuned to B♭2, head-trimmed, decayed over 0.3 s and low-passed at 2.5 kHz (-2.1 dB, reflections send -12 dB); tubular bell bell_As3 tuned to B♭4 (0.57 s, low-passed at 9 kHz, -4.4 dB, +2 ms); the hand chime chime_As3 tuned to B♭4 (0.72 s, -0.6 dB, +6 ms); reflections wet -6 dB (size 0.9, 0.6 s); 80 ms tail fade. Target -19.0 LUFS; measured -18.9 LUFS (400 ms momentary maximum), true peak -13.03 dBTP; strike pitch +0.8 cents from B♭4. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `45686bedf1120009f58aa72293b37a6aecafdaa948fe633bce2c09859ef6cf89`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/Toll6.ogg`
- Asset ID: scarlet-reward-sfx-toll6-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, build step (0.97 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_E3, chime_D4, slit_hi in the table above
- Tool/model/version: external recipe cues_shared.py `_toll_cue` (parts.py `toll`, variant A) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `Toll6_A`
- Human modifications: owner selection of take A for the build toll 6 (E♭5). Toll 6 of the E♭ sus2 ladder at E♭5, every layer on E♭ (each layer's octave as given): the slit_hi knock tuned to E♭3, head-trimmed, decayed over 0.3 s and low-passed at 2.5 kHz (-2.6 dB, reflections send -12 dB); tubular bell bell_E3 tuned to E♭4, an octave under the toll (0.53 s, low-passed at 9 kHz, -4.7 dB, +2 ms); the hand chime chime_D4 tuned to E♭5 (0.72 s, -0.3 dB, +6 ms); reflections wet -6 dB (size 0.9, 0.6 s); 80 ms tail fade. Target -19.0 LUFS; measured -18.9 LUFS (400 ms momentary maximum), true peak -13.36 dBTP; strike pitch +0.3 cents from E♭5. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `8105abc4370d9b0d59724349c46b788fffef1b8cf377f649b63c5cd18fc4aef5`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/Toll7.ogg`
- Asset ID: scarlet-reward-sfx-toll7-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, build step (0.97 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_Fs3, chime_E4, slit_hi in the table above
- Tool/model/version: external recipe cues_shared.py `_toll_cue` (parts.py `toll`, variant A) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `Toll7_A`
- Human modifications: owner selection of take A for the build toll 7 (F5). Toll 7 of the E♭ sus2 ladder at F5, every layer on F (each layer's octave as given): the slit_hi knock tuned to F3, head-trimmed, decayed over 0.3 s and low-passed at 2.5 kHz (-3.0 dB, reflections send -12 dB); tubular bell bell_Fs3 tuned to F4, an octave under the toll (0.50 s, low-passed at 9 kHz, -5.0 dB, +2 ms); the hand chime chime_E4 tuned to F5 (0.72 s, +0.0 dB, +6 ms); reflections wet -6 dB (size 0.9, 0.6 s); 80 ms tail fade. Target -19.0 LUFS; measured -18.9 LUFS (400 ms momentary maximum), true peak -12.62 dBTP; strike pitch -0.2 cents from F5. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `41725cb948a23ab6663d89039486d3a405ab17e923f5cd265fafb49833b38ad3`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/Cadence.ogg`
- Asset ID: scarlet-reward-sfx-cadence-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, cadence (2.30 s), layered from CC0 recordings
- Creator: recordings by Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_E3, bell_Fs3, chime_As3, gong_p, organq_137, organq_143, organq_149, organq_152, pedalq_067 in the table above
- Tool/model/version: external recipe cues_shared.py `cadence_B` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `Cadence_B`
- Human modifications: owner selection of take B for the shared cadence. The shared E♭-F-B♭ cadence, 1.7 s: Cadence B voicing: quiet organ E♭3, B♭3, E♭4 and F4 (organq_137, organq_143, organq_149, organq_152, each tuned from its measured pitch; release 0.65 s, levels 0/-2/-3/-2 dB, swelling -3 dB to 0 dB at 0.15 s and back to -12 dB at the end), pedal E♭2 (pedalq_067, low-passed at 900 Hz, -6 dB), tubular bells bell_Fs3 to F4 (-6 dB, +6 ms) and bell_E3 to E♭3 (-8 dB), hand chime chime_As3 to B♭4 (-12 dB, +30 ms) and Gong 1 gong_p tuned so its strongest partial is E♭3 (low-passed at 2 kHz, -4 dB, decaying -4 dB by 0.3 s and -20 dB at the end), reflections wet -4 dB (size 1.3, 0.9 s). Target -11.0 LUFS; measured -11.0 LUFS (400 ms momentary maximum), true peak -8.08 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `1532d65ec4ad97a2da6c67a70ef7c4ee4ccad7fc39e51dfbed9d9ed3b0d804bd`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/ScytheSwingHigh.ogg`
- Asset ID: scarlet-reward-sfx-scytheswinghigh-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, per-shot (0.42 s), layered from CC0 recordings
- Creator: recordings by PorkMuncher, qubodup; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: air_cut, swoosh in the table above
- Tool/model/version: external recipe cues_scythe.py `_swing` (A, high) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `ScytheSwingHigh_A`
- Human modifications: owner selection of take A for the Sable Scythe Over swing. swoosh (0.02-0.34 s, 200 Hz-9 kHz) read at a falling speed 1.10 to 0.85 over 0.3 s, high shelf +3 dB at 3.5 kHz, low shelf -3 dB at 260 Hz, swept left to right, its peak placed on the first live tick (5 ticks); air_cut (0.08-0.26 s, 300 Hz-7 kHz) reversed as a drawn breath, -30 dB swelling to 0 dB, at -9 dB ending on that tick; reflections wet -14 dB (size 0.7, 0.35 s). Target -17.0 LUFS; measured -17.0 LUFS (400 ms momentary maximum), true peak -6.25 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `fc6e1f940fce33a87765a08c249a2df59a75c7f1bb374b30a790388b3e476b7d`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/ScytheSwingLow.ogg`
- Asset ID: scarlet-reward-sfx-scytheswinglow-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, per-shot (0.42 s), layered from CC0 recordings
- Creator: recordings by PorkMuncher, qubodup; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: air_cut, swoosh in the table above
- Tool/model/version: external recipe cues_scythe.py `_swing` (A, low) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `ScytheSwingLow_A`
- Human modifications: owner selection of take A for the Sable Scythe Under swing. swoosh (0.02-0.34 s, 200 Hz-9 kHz) read at a rising speed 0.86 to 1.11 over 0.3 s, high shelf -4 dB at 3.5 kHz, low shelf +3 dB at 260 Hz, swept right to left, its peak on the first live tick (5 ticks); the reversed air_cut breath as for ScytheSwingHigh (-9 dB); reflections wet -14 dB (size 0.7, 0.35 s). Target -17.0 LUFS; measured -17.0 LUFS (400 ms momentary maximum), true peak -6.58 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `71265c94663156c2dfc42d27d54b4925b21efe57b790c505a2d8388f6940fe20`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/ScytheWhipBrace.ogg`
- Asset ID: scarlet-reward-sfx-scythewhipbrace-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, windup (0.26 s), layered from CC0 recordings
- Creator: recordings by florianreichelt, Kenney; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: creak1, woosh in the table above
- Tool/model/version: external recipe cues_scythe.py `whip_brace_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `ScytheWhipBrace_A`
- Human modifications: owner selection of take A for the Sable Scythe Whip brace. creak1 (0.14-0.32 s, 300 Hz-5 kHz, -4 dB) shaped up to 0.1 s and gone by 0.18 s; woosh (0.55-0.80 s, 150 Hz-7 kHz) reversed and swelling to 0 dB where it ends at 0.125 s (its quieter first half falls before the file starts); reflections wet -16 dB (size 0.6, 0.3 s); 20 ms tail fade. Target -15.0 LUFS; measured -15.1 LUFS (400 ms momentary maximum), true peak -4.89 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `1b9a0652a319a914fc2ffe4bda60b9a8faabc60e538cdd5ce3af7f11cc3d3f0b`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/ScytheWhip.ogg`
- Asset ID: scarlet-reward-sfx-scythewhip-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, per-shot (0.55 s), layered from CC0 recordings
- Creator: recordings by Dalesome, greyfeather, Kenney, Kreastricon62; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bloody_blade, chop, energy_wave, stick_woosh in the table above
- Tool/model/version: external recipe cues_scythe.py `whip_B` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `ScytheWhip_B`
- Human modifications: owner selection of take B for the Sable Scythe Whip lash. energy_wave (0.05-0.42 s) and stick_woosh (0.06-0.40 s, 200 Hz-11 kHz, -3 dB) both peaking at 30 ms; chop high-passed at 1.5 kHz (-9 dB, +22 ms; bone stand-in); bloody_blade (0.08-0.5 s, 300 Hz-4 kHz) decayed over 0.32 s (-14 dB, +60 ms) as the crescent's wet tail; reflections wet -14 dB (size 0.8, 0.4 s). Stand-in layers as named. Target -17.0 LUFS; measured -17.2 LUFS (400 ms momentary maximum), true peak -3.95 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `2b516b308cc70d5ec6dc4f626de3e5f105f35f17eed82d740853d08a6376553a`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/StaffWindup.ogg`
- Asset ID: scarlet-reward-sfx-staffwindup-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, windup (0.62 s), layered from CC0 recordings
- Creator: recordings by artisticdude, Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: cymbal_cresc, swish, timpani1 in the table above
- Tool/model/version: external recipe cues_scythe.py `staff_windup_B` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `StaffWindup_B`
- Human modifications: owner selection of take B for the Staff Reap windup. cymbal_cresc (0.98-1.27 s, ending on its crest, high-passed at 900 Hz, -4 dB) and the first 0.287 s of Timpani 1 tuned to E♭2 (timpani1), reversed (-3 dB, reflections send -10 dB), both swelling into 16 ticks (0.267 s); swish (0-0.15 s, 300 Hz-11 kHz, -4 dB) peaking 12 ms before; reflections wet -12 dB (size 1.0, 0.5 s). Target -15.0 LUFS; measured -15.0 LUFS (400 ms momentary maximum), true peak -3.80 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `26c6a02317eae4f78af02f0445f3f8147d7b0f2f72bfcb6291921c64f8ba10b3`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/StaffCut.ogg`
- Asset ID: scarlet-reward-sfx-staffcut-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, release part (0.32 s), layered from CC0 recordings
- Creator: recordings by DARTEKZ_GAMEZ, Euphrosyyn, greyfeather, Kenney, nekoninja, Simon Dalzell (Ivy Audio); layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: anime_shing, blower, energy_wave, knife_slice, samurai_slash, wind_whirl in the table above
- Tool/model/version: external recipe cues_scythe.py `staff_cut_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `StaffCut_A`
- Human modifications: owner selection of take A for the Staff Reap line cut. energy_wave peaking at 35 ms; knife_slice high-passed at 1.2 kHz and decayed over 0.22 s (-5 dB, peak 30 ms); anime_shing tuned so its ring is E♭8, decayed over 0.3 s (-12 dB, +25 ms, reflections send -8 dB); samurai_slash (0-0.3 s) low-passed at 1.5 kHz (-4 dB, peak 40 ms); an 0.18 s flame stand-in (wind_whirl band-passed 120 Hz-5 kHz plus the organ blower 250 Hz-6 kHz at -6 dB) (-8 dB, +20 ms; ignition stand-in); reflections wet -13 dB (size 0.8, 0.35 s). Stand-in layers as named. Target -15.5 LUFS; measured -15.7 LUFS (400 ms momentary maximum), true peak -2.63 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `245b4b8934d7dbb6ca398dcd42e9df475c76c585208818aa75cc1c65c8e80673`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/StaffBarline.ogg`
- Asset ID: scarlet-reward-sfx-staffbarline-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, finale (2.10 s), layered from CC0 recordings
- Creator: recordings by AudioPapkin, greyfeather, Kenney, nekoninja, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bass_drum2, bell_E3, bell_Fs3, chime_As3, chop, energy_wave, gong_p, low_impact, organq_137, organq_143, organq_149, organq_152, pedalq_067, samurai_slash in the table above
- Tool/model/version: external recipe cues_scythe.py `barline_B` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `StaffBarline_B`
- Human modifications: owner selection of take B for the Final Barline. Two cuts 35 ms apart, each samurai_slash (0-0.5 s, high-passed at 90 Hz; -1 and -3 dB), energy_wave (-6 and -8 dB) and chop high-passed at 600 Hz (-8 dB); low_impact (0-0.9 s) decayed over 0.8 s and low-passed at 900 Hz (-3 dB, +30 ms); Bass Drum 2 (bass_drum2) tuned to B♭1, decayed over 0.9 s and low-passed at 400 Hz (-5 dB, +30 ms); a 1.5 s cadence at 45 ms in the Cadence B voicing: quiet organ E♭3, B♭3, E♭4 and F4 (organq_137, organq_143, organq_149, organq_152, each tuned from its measured pitch; release 0.65 s, levels 0/-2/-3/-2 dB, swelling -3 dB to 0 dB at 0.15 s and back to -12 dB at the end), pedal E♭2 (pedalq_067, low-passed at 900 Hz, -6 dB), tubular bells bell_Fs3 to F4 (-6 dB, +6 ms) and bell_E3 to E♭3 (-8 dB), hand chime chime_As3 to B♭4 (-12 dB, +30 ms) and Gong 1 gong_p tuned so its strongest partial is E♭3 (low-passed at 2 kHz, -4 dB, decaying -4 dB by 0.3 s and -20 dB at the end), reflections wet -4 dB (size 1.3, 0.9 s), with the gong 0.9 dB stronger; reflections wet -10 dB (size 1.2, 0.7 s). Target -9.6 LUFS; measured -9.8 LUFS (400 ms momentary maximum), true peak -3.36 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `a1064ecff60b1c9a6317cfba3d39089d8760a5e41b943ff2c0e5dd874c5680c7`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/OrganShot1.ogg`
- Asset ID: scarlet-reward-sfx-organshot1-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, per-shot (0.26 s), layered from CC0 recordings
- Creator: recordings by Kenney, qubodup, Simon Dalzell (Ivy Audio); layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: air_cut, chop, metal_click, organ_28 in the table above
- Tool/model/version: external recipe cues_organ.py `organ_shot_A` (file 1 of 4) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `OrganShot1_A`
- Human modifications: owner selection of take A for the Canticle Organ pipe 1 shot. Pipe 1: the speech transient (chiff) of organ_28 tuned to E♭4, its first 90 ms after the onset band-passed 1.8-9 kHz (third order) so no pitch remains, falling to -18 dB by 60 ms; metal_click high-passed at 2.5 kHz, decayed over 35 ms (-6 dB, +2 ms); chop high-passed at 1.8 kHz, decayed over 50 ms (-10 dB, +1 ms; bone stand-in); an air_cut puff (0.10-0.24 s, 900 Hz-7 kHz, -11 dB, +6 ms); reflections wet -16 dB (size 0.6, 0.3 s). Stand-in layers as named. Target -17.0 LUFS; measured -17.1 LUFS (400 ms momentary maximum), true peak -2.09 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `740ea327452078680eb7bc1740e49c2c71d98a30841b981c92eb68ef1e1fce4b`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/OrganShot2.ogg`
- Asset ID: scarlet-reward-sfx-organshot2-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, per-shot (0.26 s), layered from CC0 recordings
- Creator: recordings by Kenney, qubodup, Simon Dalzell (Ivy Audio); layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: air_cut, chop, metal_click, organ_34 in the table above
- Tool/model/version: external recipe cues_organ.py `organ_shot_A` (file 2 of 4) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `OrganShot2_A`
- Human modifications: owner selection of take A for the Canticle Organ pipe 2 shot. Pipe 2: the speech transient (chiff) of organ_34 tuned to B♭4, its first 90 ms after the onset band-passed 1.8-9 kHz (third order) so no pitch remains, falling to -18 dB by 60 ms; metal_click high-passed at 2.5 kHz, decayed over 35 ms (-7 dB, +2 ms); chop high-passed at 1.8 kHz, decayed over 50 ms (-9 dB, +1 ms; bone stand-in); an air_cut puff (0.12-0.26 s, 900 Hz-7 kHz, -11 dB, +6 ms); reflections wet -16 dB (size 0.6, 0.3 s). Stand-in layers as named. Target -17.0 LUFS; measured -17.1 LUFS (400 ms momentary maximum), true peak -2.15 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `0f3d2435aa77fe534dc505111c509faf43a2517d251345b6defee489008c60eb`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/OrganShot3.ogg`
- Asset ID: scarlet-reward-sfx-organshot3-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, per-shot (0.26 s), layered from CC0 recordings
- Creator: recordings by Kenney, qubodup, Simon Dalzell (Ivy Audio); layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: air_cut, chop, metal_click, organ_40 in the table above
- Tool/model/version: external recipe cues_organ.py `organ_shot_A` (file 3 of 4) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `OrganShot3_A`
- Human modifications: owner selection of take A for the Canticle Organ pipe 3 shot. Pipe 3: the speech transient (chiff) of organ_40 tuned to E♭5, its first 90 ms after the onset band-passed 1.8-9 kHz (third order) so no pitch remains, falling to -18 dB by 60 ms; metal_click high-passed at 2.5 kHz, decayed over 35 ms (-8 dB, +2 ms); chop high-passed at 1.8 kHz, decayed over 50 ms (-8 dB, +1 ms; bone stand-in); an air_cut puff (0.14-0.28 s, 900 Hz-7 kHz, -11 dB, +6 ms); reflections wet -16 dB (size 0.6, 0.3 s). Stand-in layers as named. Target -17.0 LUFS; measured -17.1 LUFS (400 ms momentary maximum), true peak -1.67 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `b57a977eaa19bcab68e9a534b545bcbcf024670f11ff4a1262b157253f27a5f0`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/OrganShot4.ogg`
- Asset ID: scarlet-reward-sfx-organshot4-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, per-shot (0.26 s), layered from CC0 recordings
- Creator: recordings by Kenney, qubodup, Simon Dalzell (Ivy Audio); layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: air_cut, chop, metal_click, organ_46 in the table above
- Tool/model/version: external recipe cues_organ.py `organ_shot_A` (file 4 of 4) with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `OrganShot4_A`
- Human modifications: owner selection of take A for the Canticle Organ pipe 4 shot. Pipe 4: the speech transient (chiff) of organ_46 tuned to B♭5, its first 90 ms after the onset band-passed 1.8-9 kHz (third order) so no pitch remains, falling to -18 dB by 60 ms; metal_click high-passed at 2.5 kHz, decayed over 35 ms (-9 dB, +2 ms); chop high-passed at 1.8 kHz, decayed over 50 ms (-7 dB, +1 ms; bone stand-in); an air_cut puff (0.16-0.30 s, 900 Hz-7 kHz, -11 dB, +6 ms); reflections wet -16 dB (size 0.6, 0.3 s). Stand-in layers as named. Target -17.0 LUFS; measured -17.1 LUFS (400 ms momentary maximum), true peak -1.51 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `83de85bbb0ada623accc0c1333634e8e2f8857025cd919044edd3859c85dbcf1`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/HymnInhale.ogg`
- Asset ID: scarlet-reward-sfx-hymninhale-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, windup (0.62 s), layered from CC0 recordings
- Creator: recordings by Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: blower, chime_A4, organq_137, organq_143, organq_152 in the table above
- Tool/model/version: external recipe cues_organ.py `inhale_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `HymnInhale_A`
- Human modifications: owner selection of take A for the Hymn of Hands inhale. blower (1.2-1.8 s, 200 Hz-7 kHz) swelling -26 dB to 0 dB at 10 ticks (0.167 s) and gone by 0.6 s; quiet organ E♭3, B♭3 and F4 (organq_137, organq_143, organq_152 tuned, sustain from 0.6 s, levels 0/-2/-3 dB) at -3 dB swelling into the same tick (reflections send -8 dB); chime_A4 tuned to B♭5 and reversed (0.5 s, -10 dB) ending there; reflections wet -10 dB (size 1.0, 0.55 s). Target -15.0 LUFS; measured -15.2 LUFS (400 ms momentary maximum), true peak -6.25 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `f1f04b3efa28baf76ac8f44482777c1bbeaae35790037e03376a9a4bc5178a27`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/HandSlam.ogg`
- Asset ID: scarlet-reward-sfx-handslam-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, release part (0.40 s), layered from CC0 recordings
- Creator: recordings by _stubb, Kenney, Kreastricon62, magnuswaker, Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bloody_blade, bubbles, chop, concrete_smash, rock_tumble, timpani1 in the table above
- Tool/model/version: external recipe cues_organ.py `hand_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `HandSlam_A`
- Human modifications: owner selection of take A for the bone-hand slam. Bone stand-in: chop high-passed at 250 Hz, concrete_smash (0-0.5 s, 180 Hz-7 kHz, -4 dB, +3 ms) and four 30 ms rock_tumble grains high-passed at 900 Hz (-12 to -18 dB), decayed over 0.3 s; Timpani 1 tuned to E♭2 (timpani1, 0.32 s, low-passed at 320 Hz, -4 dB, +2 ms) as the thump; bloody_blade (0.08-0.45 s, 250 Hz-5 kHz) decayed over 0.3 s (-6 dB, +12 ms) and bubbles (1.15-1.37 s, 120 Hz-2.6 kHz, -14 dB, +30 ms) as the wet splash stand-in; reflections wet -12 dB (size 0.9, 0.45 s). Stand-in layers as named. Target -15.5 LUFS; measured -15.7 LUFS (400 ms momentary maximum), true peak -1.37 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `45fda59ebb49104bb18d33c4d6d813b2eccc8c5039af518529e86a5607987119`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/ChoirClasp.ogg`
- Asset ID: scarlet-reward-sfx-choirclasp-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, finale (2.20 s), layered from CC0 recordings
- Creator: recordings by _stubb, AudioPapkin, Kenney, Kreastricon62, magnuswaker, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_As3, bell_E3, bloody_blade, bubbles, chop, concrete_smash, low_impact, organ_04, organ_16, organ_22, organ_31, rock_tumble, timpani1 in the table above
- Tool/model/version: external recipe cues_organ.py `clasp_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `ChoirClasp_A`
- Human modifications: owner selection of take A for the Clasp. Four HandSlam A hands (bone grains reseeded, timpani at -2 dB) at 0, 9, 16 and 24 ms (-2, -3.5, -5 and -6.5 dB); low_impact (0-1.0 s) decayed over 0.9 s and low-passed at 800 Hz (-4 dB, +10 ms); a 1.6 s cadence at 30 ms in the Cadence A voicing: loud organ E♭2, E♭3, B♭3 and F4 (organ_04, organ_16, organ_22, organ_31 tuned; release 0.6 s, levels -3/0/-2/-3 dB, low-passed at 5.2 kHz), tubular bells bell_E3 to E♭4 (-5 dB, +4 ms) and bell_As3 to B♭3 (-9 dB, +18 ms), Timpani 1 tuned between its principal and second mode to E♭2 (-3 dB plus the weight), reflections wet -4 dB (size 1.3, 0.9 s) (+1.2 dB on the timpani); reflections wet -10 dB (size 1.2, 0.7 s). Stand-in layers as named. Target -9.6 LUFS; measured -9.7 LUFS (400 ms momentary maximum), true peak -3.24 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `6c616fafc3d70b758abe2743f01051da177023bcae24c5b0a6499d5608cad82c`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/BatonStroke.ogg`
- Asset ID: scarlet-reward-sfx-batonstroke-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, per-shot (0.50 s), layered from CC0 recordings
- Creator: recordings by artisticdude, Kreastricon62, qubodup, Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: air_cut, bloody_blade, bubbles2, swish in the table above
- Tool/model/version: external recipe cues_baton.py `stroke_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `BatonStroke_A`
- Human modifications: owner selection of take A for the Scarlet Baton stroke. swish (0-0.15 s, 250 Hz-10 kHz) read at 0.86 speed, peaking at 6 ticks (0.1 s); air_cut (0.08-0.3 s, 300 Hz-8 kHz, -8 dB) peaking 10 ms later; bloody_blade (0.1-0.5 s, 400 Hz-5 kHz) decayed over 0.26 s (-9 dB) and bubbles2 (0.35-0.61 s, 120 Hz-2.2 kHz, -14 dB, from 0.16 s) as the ink-tail stand-in; reflections wet -15 dB (size 0.7, 0.35 s). Stand-in layers as named. Target -17.0 LUFS; measured -17.0 LUFS (400 ms momentary maximum), true peak -7.70 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `474532cb67d96129dea5e13f18ecc4c9d38e9fbd7f5b869fd87a6d4cb8517717`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/BatonLift.ogg`
- Asset ID: scarlet-reward-sfx-batonlift-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, windup (0.50 s), layered from CC0 recordings
- Creator: recordings by florianreichelt, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: chime_E4, organq_143, organq_152, woosh in the table above
- Tool/model/version: external recipe cues_baton.py `lift_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `BatonLift_A`
- Human modifications: owner selection of take A for the Tutti lift. woosh (0.6-0.77 s, 150 Hz-7 kHz) reversed, swelling to 8 ticks (0.133 s); chime_E4 tuned to F5 and reversed (0.45 s, -6 dB) ending there; quiet organ B♭3 and F4 (organq_143, organq_152 tuned, sustain from 0.6 s) at -6 dB swelling into the same tick (reflections send -8 dB); reflections wet -10 dB (size 0.9, 0.5 s). Target -15.0 LUFS; measured -15.1 LUFS (400 ms momentary maximum), true peak -4.97 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `e69df9e5e6dbcb727bf5eed1a04adca24abc28c97acb1872044da69facb6b0f8`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/InkIgnite.ogg`
- Asset ID: scarlet-reward-sfx-inkignite-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, release part (0.38 s), layered from CC0 recordings
- Creator: recordings by AudioPapkin, DARTEKZ_GAMEZ, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: blower, bubbles, low_impact, wind_whirl in the table above
- Tool/model/version: external recipe cues_baton.py `ignite_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `InkIgnite_A`
- Human modifications: owner selection of take A for the stroke ignition. wind_whirl (0.5-0.6 s, 150 Hz-6 kHz) reversed into 3 ticks (-2 dB); low_impact (0-0.5 s) decayed over 0.28 s and low-passed at 700 Hz; a 0.24 s flame stand-in (wind_whirl band-passed 120 Hz-5 kHz plus the organ blower 250 Hz-6 kHz at -6 dB) (-6 dB) from 3 ticks; bubbles (1.5-1.7 s) high-passed at 2 kHz (-14 dB; sizzle stand-in); reflections wet -13 dB (size 0.9, 0.4 s). Stand-in layers as named. Target -15.5 LUFS; measured -15.5 LUFS (400 ms momentary maximum), true peak -6.64 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `a9d0408a6a94b40d419b159dd420c9bfc9d711a6680c89673ca1a9851fb8d9b7`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/RiverRelease.ogg`
- Asset ID: scarlet-reward-sfx-riverrelease-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, finale (2.40 s), layered from CC0 recordings
- Creator: recordings by _stubb, AudioPapkin, DARTEKZ_GAMEZ, florianreichelt, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_E3, bell_Fs3, bubbles2, chime_As3, gong_p, low_impact, organq_137, organq_143, organq_149, organq_152, pedalq_067, rock_tumble, wind_whirl, woosh in the table above
- Tool/model/version: external recipe cues_baton.py `river_B` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `RiverRelease_B`
- Human modifications: owner selection of take B for the Black-Blood River. Surge into 0.3 s: woosh (0.25-0.95 s, 100 Hz-8 kHz), wind_whirl (0.1-0.9 s, 100 Hz-6 kHz, -4 dB) and bubbles2 (0.5-1.4 s, 120 Hz-1.8 kHz, -8 dB; flow stand-in); low_impact (0-1.2 s) decayed over 1.1 s and low-passed at 900 Hz (-2 dB, at 0.28 s); rock_tumble grains (-18 dB, at 0.3 s); a 1.6 s cadence at 0.3 s in the Cadence B voicing: quiet organ E♭3, B♭3, E♭4 and F4 (organq_137, organq_143, organq_149, organq_152, each tuned from its measured pitch; release 0.65 s, levels 0/-2/-3/-2 dB, swelling -3 dB to 0 dB at 0.15 s and back to -12 dB at the end), pedal E♭2 (pedalq_067, low-passed at 900 Hz, -6 dB), tubular bells bell_Fs3 to F4 (-6 dB, +6 ms) and bell_E3 to E♭3 (-8 dB), hand chime chime_As3 to B♭4 (-12 dB, +30 ms) and Gong 1 gong_p tuned so its strongest partial is E♭3 (low-passed at 2 kHz, -4 dB, decaying -4 dB by 0.3 s and -20 dB at the end), reflections wet -4 dB (size 1.3, 0.9 s), with the gong 0.9 dB stronger; reflections wet -10 dB (size 1.3, 0.8 s). Stand-in layers as named. Target -9.6 LUFS; measured -9.6 LUFS (400 ms momentary maximum), true peak -6.08 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `5c3bd133fa3837a5fabb718d9f8dbb342abb413f477f43d79634dadaca2991e9`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/CenserSummon.ogg`
- Asset ID: scarlet-reward-sfx-censersummon-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, per-shot (1.00 s), layered from CC0 recordings
- Creator: recordings by DARTEKZ_GAMEZ, Kenney, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: blower, chain_grind, chime_D4, metal_pot, wind_whirl in the table above
- Tool/model/version: external recipe cues_censer.py `summon_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `CenserSummon_A`
- Human modifications: owner selection of take A for the Ember Censer summon. chain_grind (0.08-0.5 s) high-passed at 1.2 kHz (-6 dB); metal_pot tuned to B♭4, decayed over 0.7 s and low-passed at 6 kHz (+20 ms, reflections send -8 dB); chime_D4 tuned to E♭5 (0.8 s, -6 dB, +50 ms); a 0.5 s flame stand-in (wind_whirl band-passed 120 Hz-5 kHz plus the organ blower 250 Hz-6 kHz at -6 dB) (-10 dB, +50 ms); reflections wet -10 dB (size 0.9, 0.5 s). Stand-in layers as named. Target -17.0 LUFS; measured -17.1 LUFS (400 ms momentary maximum), true peak -9.02 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `10453c7af7b467d05463e4a38a720eaa1aab924ca7b7ee9a5ce0698849c61e73`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/CenserSwing.ogg`
- Asset ID: scarlet-reward-sfx-censerswing-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, windup (0.32 s), layered from CC0 recordings
- Creator: recordings by DARTEKZ_GAMEZ, Kenney, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: blower, chain_loop, creak2, wind_whirl in the table above
- Tool/model/version: external recipe cues_censer.py `swing_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `CenserSwing_A`
- Human modifications: owner selection of take A for the censer swing windup. creak2 (0.3-0.5 s, 300 Hz-5 kHz, -4 dB); chain_loop (0.9-1.15 s) high-passed at 1.2 kHz (-10 dB) and a rising flame stand-in (wind_whirl band-passed 120 Hz-5 kHz plus the organ blower 250 Hz-6 kHz at -6 dB), both swelling to 10 ticks (0.167 s), the pour; reflections wet -14 dB (size 0.8, 0.4 s); 50 ms tail fade. Stand-in layers as named. Target -15.0 LUFS; measured -15.1 LUFS (400 ms momentary maximum), true peak -4.10 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `0a5c5228beadc54cdc89c1f59bc1fb99c7f6bf976a77ef758f42bb76aac74cf4`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/CenserPour.ogg`
- Asset ID: scarlet-reward-sfx-censerpour-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, release part (0.50 s), layered from CC0 recordings
- Creator: recordings by AudioPapkin, DARTEKZ_GAMEZ, florianreichelt, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: blower, bubbles, low_impact, wind_whirl, woosh in the table above
- Tool/model/version: external recipe cues_censer.py `pour_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `CenserPour_A`
- Human modifications: owner selection of take A for the censer pour. Live 16 ticks: low_impact low-passed at 1.3 kHz, woosh (0.4-1.02 s, 100 Hz-7 kHz, -3 dB), bubbles (0.8-1.37 s, 120 Hz-2 kHz, -5 dB; pour stand-in) and a flame stand-in (wind_whirl band-passed 120 Hz-5 kHz plus the organ blower 250 Hz-6 kHz at -6 dB) (-6 dB), each held to 0.267 s then fading to -30 dB; reflections wet -11 dB (size 0.9, 0.45 s). Stand-in layers as named. Target -16.0 LUFS; measured -16.1 LUFS (400 ms momentary maximum), true peak -12.15 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `067ec15261bcf9f315ba4e03df6bca7a3586d636681eafad14eadf63c7bba92e`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/CenserBrace.ogg`
- Asset ID: scarlet-reward-sfx-censerbrace-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, windup (0.30 s), layered from CC0 recordings
- Creator: recordings by Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: blower, chain_loop, cymbal_roll in the table above
- Tool/model/version: external recipe cues_censer.py `brace_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `CenserBrace_A`
- Human modifications: owner selection of take A for the Grand Pour brace. chain_loop (1.75-1.95 s) high-passed at 1.5 kHz, cymbal_roll (0.5-0.65 s) high-passed at 3 kHz (-4 dB; heat stand-in) and blower (1.3-1.48 s, 250 Hz-6 kHz, -6 dB), all swelling into 6 ticks (0.1 s), the Grand Pour; reflections wet -14 dB (size 0.7, 0.35 s); 40 ms tail fade. Stand-in layers as named. Target -15.0 LUFS; measured -15.2 LUFS (400 ms momentary maximum), true peak -1.88 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `9fe4042eb40311ad0f3917ed88a14eccded617bf1bb24dea0020dc3738bf12fe`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/CenserGrandPour.ogg`
- Asset ID: scarlet-reward-sfx-censergrandpour-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, finale (1.30 s), layered from CC0 recordings
- Creator: recordings by AudioPapkin, DARTEKZ_GAMEZ, florianreichelt, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: blower, bubbles, chain_loop, gong_p, low_impact, wind_whirl, woosh in the table above
- Tool/model/version: external recipe cues_censer.py `grand_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `CenserGrandPour_A`
- Human modifications: owner selection of take A for the Grand Pour. Live 20 ticks: the CenserPour layers held to 0.333 s (low_impact low-passed at 900 Hz) with Gong 1 gong_p tuned so its strongest partial is E♭3 (1.2 s, low-passed at 2.2 kHz, -3 dB, reflections send -8 dB) and chain_loop (1.2-1.6 s) high-passed at 1.2 kHz (-12 dB); reflections wet -11 dB (size 1.1, 0.6 s). Stand-in layers as named. Target -9.6 LUFS; measured -9.6 LUFS (400 ms momentary maximum), true peak -6.24 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `066cdb9e8b72b12f64ba19d1ade649e8968d67aa5a3eef55392133e2af703ff5`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/QuillThrow.ogg`
- Asset ID: scarlet-reward-sfx-quillthrow-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, per-shot (0.26 s), layered from CC0 recordings
- Creator: recordings by artisticdude, qubodup; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: air_cut, swish in the table above
- Tool/model/version: external recipe cues_quill.py `throw_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `QuillThrow_A`
- Human modifications: owner selection of take A for the Bloodink Quill throw. swish (0-0.15 s, 700 Hz-12 kHz) peaking at 30 ms; air_cut (0.1-0.3 s, 900 Hz-10 kHz, -8 dB) peaking at 35 ms; reflections wet -18 dB (size 0.5, 0.25 s). Target -17.0 LUFS; measured -17.0 LUFS (400 ms momentary maximum), true peak -5.14 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `a0315f1ce273f0424fb7d91fbdbb2fd58bdd698a3711877290ed0257e4ff9ce9`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/QuillStick.ogg`
- Asset ID: scarlet-reward-sfx-quillstick-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, per-shot (0.24 s), layered from CC0 recordings
- Creator: recordings by Kenney, Kreastricon62; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bloody_blade, chop, knife_slice in the table above
- Tool/model/version: external recipe cues_quill.py `stick_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `QuillStick_A`
- Human modifications: owner selection of take A for the quill stick. knife_slice high-passed at 1.5 kHz and decayed over 80 ms; chop high-passed at 1.2 kHz and decayed over 60 ms (-4 dB, +2 ms); bloody_blade (0.1-0.4 s, 500 Hz-5 kHz) decayed over 0.12 s (-12 dB, +6 ms); reflections wet -18 dB (size 0.5, 0.25 s). Target -17.0 LUFS; measured -17.3 LUFS (400 ms momentary maximum), true peak -1.29 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `209798be05211c35d1ab10eaa7e0a2ca2c613a11ba38607493006ca68eef49f6`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/ScoreUnseal.ogg`
- Asset ID: scarlet-reward-sfx-scoreunseal-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, windup (0.40 s), layered from CC0 recordings
- Creator: recordings by Kenney, Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: book_flip, chime_A4, chop in the table above
- Tool/model/version: external recipe cues_quill.py `unseal_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `ScoreUnseal_A`
- Human modifications: owner selection of take A for the Sealed Score unseal. Wax-crack stand-in: chop high-passed at 2 kHz and decayed over 50 ms, then two chop ticks high-passed at 3.5 kHz at 55 and 80 ms (-10 and -12 dB); book_flip (0-0.2 s, high-passed at 500 Hz, -3 dB) from 30 ms, swelling for 0.133 s as the unrolling score; chime_A4 tuned to B♭5 (0.25 s, -12 dB, reflections send -6 dB) at 0.113 s, so the score glints as the ink catches 8 ticks in; reflections wet -14 dB (size 0.7, 0.35 s). Stand-in layers as named. Target -15.0 LUFS; measured -18.5 LUFS (400 ms momentary maximum), true peak -1.31 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `58461f0cef4e4aba695583a048556e940b72d15942ed947d4c29c1c019f3731f`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/InkBlaze.ogg`
- Asset ID: scarlet-reward-sfx-inkblaze-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, release part (0.75 s), layered from CC0 recordings
- Creator: recordings by _stubb, AudioPapkin, DARTEKZ_GAMEZ, florianreichelt, Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bubbles2, low_impact, rock_tumble, wind_whirl, woosh in the table above
- Tool/model/version: external recipe cues_quill.py `blaze_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `InkBlaze_A`
- Human modifications: owner selection of take A for the ink blaze. wind_whirl (0.55-0.62 s, 150 Hz-7 kHz) reversed into 70 ms (-2 dB); woosh (0.7-1.3 s, 120 Hz-8 kHz) from 60 ms; low_impact decayed over 0.4 s and low-passed at 800 Hz (-3 dB, +60 ms); bubbles2 (1.2-1.6 s) high-passed at 2.5 kHz (-14 dB, +80 ms; sizzle stand-in) and rock_tumble grains (-18 dB, +0.15 s; crackle stand-in); reflections wet -12 dB (size 1.0, 0.5 s). Stand-in layers as named. Target -12.0 LUFS; measured -12.1 LUFS (400 ms momentary maximum), true peak -5.90 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `80913ef8335d11c400f318e258c6aef9be40b74fd8e448c364b83364a69dacab`

- Runtime file: `Assets/Sounds/Weapons/ScarletRewards/ScoreChord.ogg`
- Asset ID: scarlet-reward-sfx-scorechord-20261003
- Asset type: stereo 48 kHz Vorbis Scarlet reward cue, finale (2.30 s), layered from CC0 recordings
- Creator: recordings by AudioPapkin, Kenney, magnuswaker, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-03
- Source type: public-domain
- Source work and URL: bell_As3, bell_E3, book_flip, concrete_smash, low_impact, organ_04, organ_16, organ_22, organ_31, timpani1 in the table above
- Tool/model/version: external recipe cues_quill.py `score_chord_A` with common.py, kit.py, parts.py and render.py (hashes above), pinned Ogg serial of take `ScoreChord_A`
- Human modifications: owner selection of take A for the score burst. low_impact (0-1.0 s) decayed over 0.9 s and low-passed at 900 Hz (-2 dB); concrete_smash (0-0.5 s) high-passed at 200 Hz and decayed over 0.35 s (-6 dB); book_flip (0-0.22 s, high-passed at 600 Hz, -8 dB, +30 ms) as the scattering paper; a 1.6 s cadence at 10 ms in the Cadence A voicing: loud organ E♭2, E♭3, B♭3 and F4 (organ_04, organ_16, organ_22, organ_31 tuned; release 0.6 s, levels -3/0/-2/-3 dB, low-passed at 5.2 kHz), tubular bells bell_E3 to E♭4 (-5 dB, +4 ms) and bell_As3 to B♭3 (-9 dB, +18 ms), Timpani 1 tuned between its principal and second mode to E♭2 (-3 dB plus the weight), reflections wet -4 dB (size 1.3, 0.9 s) (+0.9 dB on the timpani); reflections wet -10 dB (size 1.2, 0.7 s). Stand-in layers as named. Target -9.6 LUFS; measured -9.7 LUFS (400 ms momentary maximum), true peak -3.31 dBTP. Recordings stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of the take, 2026-10-03; Claude review 2026-10-03 (source licenses and hashes, loudness, true peak, boundaries and clicks, byte-identical re-render); in-game mix not_run
- SHA256: `184cd662a77f5bc4f0597a77054371e365396dcace5d3acb90b0e1f71ae7a4ba`

### Scarlet Invocation recorded audio — 2026-10-02

Sixteen cues for every Scarlet Invocation sound moment ([Scarlet spec](../docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md#sound-effects)). The owner auditioned 16 scenes x 3 variants on 2026-10-02 and chose variant A for every scene; these files are those auditioned masters with one level lift per group baked in on 2026-10-03 (loudness revision below). They replace the borrowed Doll cues in Scarlet's gesture, chorus and ceremony code and the retired 2026-09-18 Scarlet WAVs (Foretell, CrownRupture, SilkCleave, ThornRend, ScarletRelease), which are removed with their records. Each cue layers CC0 recordings from the VSCO 2 CE and VCSL sample libraries and from Freesound/OpenGameArt; no synthesized tone is used. The recordings, libraries and recipe stay outside the repository.

Every Freesound/OpenGameArt source below showed Creative Commons 0 (https://creativecommons.org/publicdomain/zero/1.0/) on its page when it was surveyed on 2026-10-01 (local survey records; the six sources already recorded for Ghost Samurai, Soboro and Ebon Manor carry the same hashes there). Freesound files are the public HQ preview renders of those CC0 uploads. Kenney's pack carries its CC0 `license.txt`. VSCO 2 CE 1.1.0 ships a CC0 1.0 `LICENSE`; its organ folder credits Simon Dalzell (Ivy Audio) and states that Versilian Studios grants redistribution. Beside CC0, its `Readme.txt` terms ask that the samples not be sold directly (none are distributed), encourage keeping improvements to the sample set open (the set itself is not modified or redistributed), and ask for credit to Versilian Studios / Sam Gossner and/or Ivy Audio / Simon Dalzell where applicable, with a link to the VSCO: CE homepage (https://versilian-studios.com/vsco-community/); a public credit for these cues carries both names and that link. VCSL is CC0 1.0 per its repository README, which asks for no credit. BMacZero's OpenGameArt page offers an optional credit to Brian MacIntosh (page rechecked 2026-10-02).

| Library | Version and URL | SHA256 or commit | Terms |
|---|---|---|---|
| VSCO 2 Community Edition (Versilian Studios / Sam Gossner; organ by Ivy Audio / Simon Dalzell) | 1.1.0 release archive, https://github.com/sgossner/VSCO-2-CE/archive/refs/tags/1.1.0.zip; homepage https://versilian-studios.com/vsco-community/ | `4a4446628df0e1a12aaee58e9f65f8fa7cde51971e961abb1b43083a6d3a8ab7` | CC0 1.0 (bundled `LICENSE`); `Readme.txt` requests credit and the homepage link, and no direct sale of the samples |
| Versilian Community Sample Library (VCSL) | https://github.com/sgossner/VCSL | commit `b6e6ac82d22248edee98a0bde185eb9ef6d439ad` | CC0 1.0 (repository README) |

| Key | Recording / sample | Creator | Source | Local source SHA256 |
|---|---|---|---|---|
| bass_drum | Bass Drum 1 `BDrumNew_hit_v3_rr1_Sum.wav` | Versilian Studios | VCSL `b6e6ac8` | `a2d8829e8b2b59e8afb12fa396b01758827059339c2a84ad066d17b36c82d1ff` |
| gong_f | Gong 1 `gong_2_f.wav` | Versilian Studios | VCSL `b6e6ac8` | `0f7538cccc7673f953fc66db0e4449d4902f2ff2ef01dc4962c748b011813942` |
| gong_p | Gong 1 `gong_p.wav` | Versilian Studios | VCSL `b6e6ac8` | `3d103e1a7af12eeb17e0c5488f11933ca92b9fbdefa39b8020b64238da2db0d0` |
| gong_full | Gong 2 `hit_full2_loudest_ST.mp3` | Versilian Studios | VCSL `b6e6ac8` | `8a65dabf483886d7609136eff6c7b1be7939b0d610824a5625b8bde558f23a6f` |
| chime_As3 | Hand Chimes `sus_A#3_r01_main.wav` | Versilian Studios | VCSL `b6e6ac8` | `2eb505e93627247114582d8e103d77ff427ab2e882bb73e30925d265c09210a4` |
| chime_As5 | Hand Chimes `sus_A#5_r01_main.wav` | Versilian Studios | VCSL `b6e6ac8` | `3713c13b8c17f63d482ec9f997aba7f61175cb9e884afe5a6c14beb93a42fb25` |
| chime_E4 | Hand Chimes `sus_E4_r01_main.wav` | Versilian Studios | VCSL `b6e6ac8` | `7196b23009e1acc8e03591c35e54144487991c75cfe22413e84e0bbfeb478caf` |
| chime_Fs4 | Hand Chimes `sus_F#4_r01_main.wav` | Versilian Studios | VCSL `b6e6ac8` | `858c506fc9c6c35bcb9e0c08850892cab77c1496a83ccff493377df6e6fb1bd4` |
| mark_trees | Mark Trees `Legacy/windchimes_asc1.wav` | Versilian Studios | VCSL `b6e6ac8` | `4da1f0ec4ed2d94902a35995faef9ce2bd747a525761c0db7ace093359939693` |
| slit_drum | Slit Drum `LogDrumHi_MedM_v3_rr1_Sum.wav` | Versilian Studios | VCSL `b6e6ac8` | `e56b10659b821bbf4eeb3cc371f42ea60414adcbd66ad0ead9e7a62cdd11fe3f` |
| cymbal_cresc | Suspended Cymbal 1 `susCymb1_cresc_2s.wav` | Versilian Studios | VCSL `b6e6ac8` | `a5507bf116b33ee220f4eff54506be56fd455e908c8d53903f217590341e5008` |
| cymbal_hit | Suspended Cymbal 1 `susCymb1_hit_fff1.wav` | Versilian Studios | VCSL `b6e6ac8` | `993ceb041adaad19c88042e9d6e66707c30eda6e44a5a263d68bb4a3cdb2ca02` |
| timpani1 | Timpani 1 `Hit/Timpani1_Hit_v4_rr1_Sum.wav` | Versilian Studios | VCSL `b6e6ac8` | `b51d81444425e1ba0c09d94dbdc384a8cd4b4681c42959cc8676ccaf6d6ea793` |
| timpani5 | Timpani 1 `Hit/Timpani5_Hit_v4_rr1_Sum.wav` | Versilian Studios | VCSL `b6e6ac8` | `520bcf3b2f5d974076d0eb0eb3ba5201de0be5098860e9d752c6deaa295ae3ba` |
| bell_As3 | Tubular Bells 1 `chimes_A#3_ff_rr1.wav` | Versilian Studios | VCSL `b6e6ac8` | `55fcfdb825026629670e3486f47762e6a3c4a89e138990fbd0e65c3f0a1d6a79` |
| bell_D3 | Tubular Bells 1 `chimes_D3_ff_rr1.wav` | Versilian Studios | VCSL `b6e6ac8` | `ca1c64961ce8e96f6ea3ed3bf67bde09c0beb723b0fe346881b317695b1ce84f` |
| bell_D4 | Tubular Bells 1 `chimes_D4_ff_rr2.wav` | Versilian Studios | VCSL `b6e6ac8` | `2657add471fc1804a29c1261df45af13c700803cfa29bd11c134400a3fbc8700` |
| woodblock | Woodblock `wood_click_mp.wav` | Versilian Studios | VCSL `b6e6ac8` | `00b76f9fe96c118f29dfc69866c12ec11f2e1264f6cb9c0b36912c2210b015fe` |
| organ_04 | Organ `Loud/Rode_Man3Open_04.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `7ce6ac35c8f951d36a1ebe66043f5a3e1ae3ca6bb3c45c1e74d1a3f64834fa5c` |
| organ_28 | Organ `Loud/Rode_Man3Open_28.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `c8eb1bff00e0261bb403d6a245f54087808c3cb77c03914e97d0f606205895c7` |
| organ_pedal_04 | Organ `Loud/Rode_Pedal_04.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `ac8ed166f7b56056a5d87811ed7a178f6f796ee7e2405fcadaaef990b68f7025` |
| organ_pedal_16 | Organ `Loud/Rode_Pedal_16.wav` | Simon Dalzell (Ivy Audio) for Versilian Studios | VSCO 2 CE 1.1.0 | `415d11b52d0c16d05617c91ab05c55122196e1f9df163f9ea5ce8eefc440e9d0` |
| bubbles | Misc 1 `bubbles4.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `80dbaf61e5eb910674496439f71cfd64c5ae80eaed7ba9505c66a20457f17c5d` |
| chain_loop | Misc 1 `chaingrindLoop.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `9f97699cc87f2589461182ad200658a43cd9e84616f365492d5f254c970db813` |
| glass | Misc 1 `glass_break.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `5cf1b08875add0fa7598b09488c23a1208dccd03523cf7e5aa0b7bc0c1c31f69` |
| glass3 | Misc 1 `glass_break3.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `924a2cb9e993fc7471a3066eed23f4c360a9c1cb6a658e30aa4cf6c145cece07` |
| glass8 | Misc 1 `glass_break8.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `2f52ee0dcd4240bbcb84d135eb40e2be39ddf054744cdc585e3459a4651d32af` |
| siren | Misc 1 `siren3.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `9ed2c04ef4e4081ae4dcbaa8eb4e0302feadffd10a23a47869e39355351c14a0` |
| nepal_bells | Misc 2 `NepaleseBells/fx2_short_r01_main.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `3f4d3f8f115199bdd132f6bd147b9e3aea5c719749dea55313047b6fa3d6ee41` |
| anvil | Percussion `Anvil_Hit1_v3_Sum.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `ac62de051bfb948a022bb2b06e785f0f62dadd8ef39a11a8d5304869a013945a` |
| glock_G4 | Percussion `Glock/glock_medium_G4.wav` | Versilian Studios | VSCO 2 CE 1.1.0 | `6c4649b13e24fa01e4634edad8fe8edf18aef33facc5d49fe53097b376bac4da` |
| zap | ELECTRIC_ZAP_001.wav | JoelAudio | https://freesound.org/s/136542/ | `5efa941ecead3f4d6efe8b034a36554eae3001c27d1cd06c6f6e99d300975737` |
| spark_klein | Spark | elliott.klein | https://freesound.org/s/189630/ | `73081928ac0a508c07d1dfd984ebc9e6418e516b8401f377bdc93174a77c1ac7` |
| spark_oga | spark.wav, Electricity Sound Effects | BMacZero (Brian MacIntosh) | https://opengameart.org/content/electricity-sound-effects-0 | `9a3d987bdf7f405570aa015f6da42596ce84c5073c18c0561be7cd3ae401bc97` |
| slicing_flesh | Slicing through flesh | NeoSpica | https://freesound.org/s/504615/ | `f8e3d47aa24053707ac879fbb1c2760d577ee56ba8b9869ddf09eadafcdc4bed` |
| stone_heavy | impact-stone-heavy.wav | kasparsj | https://freesound.org/s/513694/ | `adcb2d60ec1d36174e3580927c73b0118a73939c0f6883ed33e524ac0ebffb19` |
| concrete_smash | Concrete SMASH 2 | magnuswaker | https://freesound.org/s/522099/ | `b182dec5699903068a509113e09e0b4c02a78f42b7aa73f875b23fc24ce34c6e` |
| low_impact | Very low frequency impact.wav | AudioPapkin | https://freesound.org/s/541029/ | `73c25c4f49baa34cb9ad42290324fc61340124028dc0161299880b78580e335a` |
| knife_slice | knifeSlice.ogg, RPG Audio (RPGsounds_Kenney.zip) | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `4cd96dc630bed9840c15f1dd2306da2cc56a4da26a5d3f1a03c5a7265ac5e54f` |
| sword_hit | Sword Hit | qubodup | https://freesound.org/s/442769/ | `93d72e63bb8d9b8a60d2c0ac665c153171515645fbb85f4ec028e4a253e7b167` |
| draw_sword | Draw sword#1.wav | fielastro | https://freesound.org/s/423935/ | `a703c6e1b8e22e7cd537a72ac1c107bb234ce6cde145886a56825f724976885d` |
| sword_ring | sword-01.wav | audione | https://freesound.org/s/52458/ | `7081d72afa9eaec0367c327b467e679919f1ab19ae1e2a494fef91348ba52378` |
| demon_howl | Demon Giant Howl.wav | Bananaboatman33 | https://freesound.org/s/257635/ | `2a6487786a58fabe2f20ce051408e5caf04b552a83663f0bf377711646ffe58b` |
| swoosh | swoosh.wav | PorkMuncher | https://freesound.org/s/263595/ | `5d11ca0d7ad2ad4bc3108c0b017cccd9ae3e002277e1550fa78693841ea85058` |
| wind_whirl | Wind Whirl (Small Air Blow) | DARTEKZ_GAMEZ | https://freesound.org/s/719560/ | `5b41e14eaa752d4715ee7c706b99581f3adf5b02630c1d6c565b445a4e725c75` |

Common processing (external `sfx-scarlet` recipe; SHA256 `common.py` `49a69026af39748179426af464a4158fec0645ecb2511d81212f5cdf1bfbdf63`, `kit.py` `91fce8bd9cf17b0a4703628a3caf7b88a6070c0f7ceddb212b0fa37dac092e31`, `cues_a.py` `884aaa253de59b59d3aa6a16e79e4f63da1bf5243606762799f40e0081a8512e`, `cues_b.py` `66b0e8ff5b62879c9cbf33b11296fbd23959ebf54cd1cac69e61025c30e731ce`, `cues_c.py` `80b961429f2b642f886988b0a3cee3df119df2f87980282130f3dd4d9a771258`, `cues_d.py` `8fbecbbf079f6eca00a770c16dd1b1f5e10d545d86f5d417a3edae8f602bca4b`, `render.py` `fe75d89668d7214f88b07a623f3069ccc0de369a10d989520f94a785465a03e7`, studio `engine.py` `36de9045f74bff3cf599c097b1ef07143744bffeb905f809d0b6d82536a3b5d3`, and the source catalog `catalog.json` `2faed1e050be83dc2023ebeeddb8694c4ce94c26d987bb49058631bb76d0af60` that `kit.py` reads beside it: the frozen 2026-10-02 source analysis that resolves the VCSL/VSCO short names to files and supplies each sample's measured nominal pitch and the shift table behind every semitone shift named below): sources are decoded and resampled to 48 kHz stereo; pitch shifts are sampler-style polyphase resampling (speed follows pitch) onto the E-flat family of Graceful Ordeal (Eb, Bb, Gb, Db) from each sample's measured nominal pitch; filters are second-order Butterworth high/low/band-pass and RBJ shelves; envelopes are dB breakpoints; each layer is level-matched on its K-weighted 100 ms peak, placed on a timeline and partly sent to a convolution reverb whose impulse response is the studio's synthetic hall (seeded filtered noise with sparse early reflections; a processing kernel, not a recording). The master removes DC, fades the head (1.5 ms) and tail (a quarter of the length, 40-300 ms, unless noted), sets the cue's K-weighted 100 ms peak target, applies a level-matched tanh soft limit and a -1.8 dBTP true-peak ceiling, and encodes 48 kHz stereo Vorbis at compression level 0.5 with a pinned Ogg serial. A fresh render from the hashed recipe and catalog reproduced the sixteen auditioned files byte for byte (rechecked 2026-10-02 after the catalog moved beside the recipe); the shipped files are those masters after the 2026-10-03 lift below, which `lift.py` reproduces byte for byte.

**Loudness revision — 2026-10-03 (0.3.82).** The owner found the effects too quiet in play. Plan A ([evidence](../docs/evidence/2026-10-03-scarlet-loudness.json)) puts Graceful Ordeal on tModLoader's music curve and lifts each group of these cues once, so the balance inside a group stays the auditioned one: the impact +4.99 dB, the foretell +6.37 dB, the big cues (crossflow release, act change, sacrifice, Victory) and the crossflow charge +1.50 dB, the six chorus cues +1.53 dB and Down, revive and Ready +2.14 dB. The lift above a file's own level is baked in by the external `sfx-scarlet-loud/lift.py` (SHA256 `63876b78053e5fa6135ad7a3b45904f85cef69ee3c2b5002974a87ef1dea6d5c`, run with the same Python and libraries as the recipe). For each lifted file it rebuilds the picked take's master from this recipe and its sources, refuses to continue unless that master encodes to the auditioned file's exact bytes, multiplies it by one gain and, only where the Vorbis round trip would otherwise exceed -1.0 dBTP, runs a look-ahead limiter (5 ms look-ahead, 80 ms release, true-peak detection by 4x polyphase oversampling, linked channels; its ceiling starts at -1.3 dBTP and is lowered until the decoded true peak is at or under -1.0 dBTP; at most 3 dB of gain reduction). The gain is the smallest that brings the file's maximum 400 ms momentary loudness (ITU-R BS.1770-4, the standard's 48 kHz K-weighting along time, padded 0.2 s before and 0.5 s after, 10 ms steps) within 0.1 dB of its auditioned level plus the lift; where limiting eats the gain first, the smallest gain reaching the highest loudness under the 3 dB cap. Encoding is the recipe's own (48 kHz stereo Vorbis at compression level 0.5 with the take's pinned serial, so the serial still names the take). No layer, envelope, filter, length or timing changes. A second run reproduced every lifted file byte for byte. Each lifted file's entry gives its gain, limiting and levels.

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletForetell.ogg`
- Asset ID: scarlet-sfx-foretell-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet warning (foretell) cue (0.62 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: chime_As3, chime_As5, woodblock in the table above
- Tool/model/version: external recipe cues_a.py `foretell_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Hand Chimes A#3 (sounding Bb4, unshifted, 0.6 s) and A#5 (Bb, -9 dB, 0.5 s) with fast dB decays, plus the Woodblock click high-passed at 900 Hz (-12 dB) as the beat-head attack; chimes sent to the hall at -6 dB, wet -11 dB (0.8 s IR, RT 0.7/0.35 s). Target K-weighted 100 ms peak -18 dB; measured -18.0 dB, true peak -16.27 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +6.37 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -25.69 → -19.32 LUFS (+6.37 dB of the foretell's planned +6.37 dB), true peak -9.80 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `27a3fdc9a7bdd94be5dec42569187f1a58d52a2173ecd6737b8a9850fbed2678`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletCrossflowCharge.ogg`
- Asset ID: scarlet-sfx-charge-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet crossflow two-beat charge cue (1.25 s), layered from CC0 recordings
- Creator: recordings by Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cymbal_cresc, bell_D3, organ_04, organ_28 in the table above
- Tool/model/version: external recipe cues_b.py `charge_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Organ Man3Open_04 (Eb2), Man3Open_28 (Eb3) and Man3Open_28 pitched +7 semitones (Bb3), low-passed at 2.5 kHz, each swelled from -40 dB to a peak at exactly two beats (0.9375 s) and folded away by 1.2 s; Tubular Bells D3 pitched +1 (Eb3, -14 dB); the Suspended Cymbal 2 s crescendo (0.3-1.55 s) high-passed at 1.2 kHz (-17 dB) rising to the same peak; hall wet -9 dB (1.2 s IR). Target K-weighted 100 ms peak -15 dB; measured -15.0 dB, true peak -12.3 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +1.50 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -21.70 → -20.19 LUFS (+1.50 dB of its release's planned +1.50 dB), true peak -10.93 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `f21365258c9c7fcd41b1678da3c3d8e02a23a950b898381022be4e8b07e81eab`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletCrossflowRelease.ogg`
- Asset ID: scarlet-sfx-release-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet crossflow release cue (1.60 s), layered from CC0 recordings
- Creator: recordings by PorkMuncher, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: bass_drum, timpani1, bell_D3, organ_04, organ_28, organ_pedal_04, glass, anvil, swoosh in the table above
- Tool/model/version: external recipe cues_b.py `release_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Timpani 1 hit (Gb2) faded over 0.5 s; Bass Drum 1 low-passed at 160 Hz (-3 dB); the organ Eb2/Eb3/Bb3 chord (Man3Open_04, Man3Open_28, Man3Open_28 +7) low-passed at 2 kHz, struck within 12 ms and folded by 1.45 s; Tubular Bells D3 +1 (Eb3, -8 dB); Anvil high-passed at 2.2 kHz (-12 dB); glass_break high-passed at 3 kHz and widened 1.6x (-14 dB, +30 ms); the swoosh band-passed 180 Hz-4.2 kHz and panned left to right (-9 dB); organ Pedal_04 (Eb1) low-passed at 140 Hz (-9 dB); hall wet -9 dB (1.3 s IR). Target K-weighted 100 ms peak -8 dB; measured -8.1 dB, true peak -4.2 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +1.50 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -13.96 → -12.45 LUFS (+1.51 dB of the big cues' planned +1.50 dB), true peak -2.48 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `422ec83ab7cc45a049b1e8da28bbcd3cab11d571fe97fed2641f25f8d2f4b9b8`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletImpact.ogg`
- Asset ID: scarlet-sfx-impact-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet thin-beam impact cue (0.50 s), layered from CC0 recordings
- Creator: recordings by Kenney, Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: chime_As5, slit_drum, glass3, knife_slice in the table above
- Tool/model/version: external recipe cues_a.py `impact_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Kenney knifeSlice high-passed at 1.8 kHz (-2 dB); the first 0.14 s of glass_break3 high-passed at 3.5 kHz (-9 dB); Hand Chimes A#5 (Bb) low-passed at 8 kHz (-6 dB); the Slit Drum (Eb3) low-passed at 600 Hz (-8 dB); every layer is silent by 0.45 s; hall wet -10 dB (0.6 s IR). Target K-weighted 100 ms peak -14 dB; measured -14.3 dB, true peak -5.96 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +6.74 dB of gain and the look-ahead limiter at a -1.45 dBTP ceiling (2.58 dB of gain reduction at most); maximum 400 ms momentary loudness (BS.1770) -20.31 → -15.39 LUFS (+4.91 dB of the impact's planned +4.99 dB), true peak -1.17 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `7ffadc5a4c4226c7c7be0907a874be80e0ece9e8a0b9c00d1d765bdcf54225c8`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletStackSummon.ogg`
- Asset ID: scarlet-sfx-stack-summon-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet Stack call (black seal) cue (1.55 s), layered from CC0 recordings
- Creator: recordings by Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: gong_f, timpani1, organ_04, chain_loop in the table above
- Tool/model/version: external recipe cues_c.py `stack_summon_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Gong 1 gong_2_f pitched -0.12 semitone (Eb): its first 1.0 s reversed, low-passed at 3 kHz and swelled to a 1.0 s peak, then the same gong forward (low-passed at 1.5 kHz, -8 dB) with the Timpani 1 hit (Gb2, -7 dB) at 0.988 s; organ Man3Open_04 (Eb2) low-passed at 500 Hz swelling into the hit (-4 dB); chaingrindLoop high-passed at 700 Hz (-16 dB); hall wet -8 dB (1.1 s IR). Target K-weighted 100 ms peak -14 dB; measured -14.0 dB, true peak -12.64 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +1.53 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -19.27 → -17.74 LUFS (+1.53 dB of the chorus cues' planned +1.53 dB), true peak -10.98 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `e8db43813d432733069e60412c5a17ac544fc089b1a1244ce5d19f6cd4cec635`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletStackSuccess.ogg`
- Asset ID: scarlet-sfx-stack-success-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet Stack success (release) cue (1.60 s), layered from CC0 recordings
- Creator: recordings by Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: bass_drum, chime_As5, chime_E4, chime_Fs4, bell_As3, bell_D3, organ_04 in the table above
- Tool/model/version: external recipe cues_c.py `stack_success_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Tubular Bells D3 +1 (Eb3) and A#3 (Bb3, -4 dB, +12 ms); Hand Chimes F#4 (Gb, -9 dB), A#5 (Bb, -11 dB) and E4 pitched -1 then +12 (Eb, -10 dB) entering at 20/45/70 ms; organ Man3Open_04 (Eb2) low-passed at 600 Hz (-9 dB); Bass Drum 1 low-passed at 150 Hz (-9 dB); hall wet -8 dB (1.3 s IR). Target K-weighted 100 ms peak -13 dB; measured -13.1 dB, true peak -8.37 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +1.53 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -18.22 → -16.68 LUFS (+1.54 dB of the chorus cues' planned +1.53 dB), true peak -6.51 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `50eada0980c30caaaec64fa425794721b8c46a90f47c08e9797b8dcde290c60b`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletStackFail.ogg`
- Asset ID: scarlet-sfx-stack-fail-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet Stack failure (black flame) cue (1.60 s), layered from CC0 recordings
- Creator: recordings by AudioPapkin, Bananaboatman33, BMacZero (Brian MacIntosh), DARTEKZ_GAMEZ, elliott.klein, JoelAudio, magnuswaker, Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: organ_pedal_16, bubbles, zap, spark_klein, spark_oga, concrete_smash, low_impact, demon_howl, wind_whirl in the table above
- Tool/model/version: external recipe cues_c.py `stack_fail_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. The very low impact; the demon howl pitched -5 semitones and low-passed at 1.8 kHz (-3 dB); the concrete smash low-passed at 350 Hz (-6 dB); the wind whirl (0.1-1.4 s) low-passed at 700 Hz (-7 dB); a 1.2 s bed of forty 70 ms spark grains cut from the BMacZero spark, the elliott.klein spark and the JoelAudio zap (high-passed at 2.5 kHz, seeded random times, levels -14 to -2 dB and pans; bed at -16 dB); organ Pedal_16 (Eb2) low-passed at 450 Hz (-9 dB); bubbles4 low-passed at 900 Hz (-10 dB); hall wet -9 dB (1.1 s IR); the whole mix decays to silence at 1.58 s. Target K-weighted 100 ms peak -9 dB; measured -9.0 dB, true peak -7.68 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +1.53 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -15.54 → -14.02 LUFS (+1.52 dB of the chorus cues' planned +1.53 dB), true peak -6.26 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `0102d0bd63fc72d550d0db66d99a7002d4d766e516acc96589b560478ab89323`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletSpreadSummon.ogg`
- Asset ID: scarlet-sfx-spread-summon-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet Spread call (red seal) cue (1.50 s), layered from CC0 recordings
- Creator: recordings by fielastro, Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: chime_As3, chime_As5, cymbal_hit, draw_sword in the table above
- Tool/model/version: external recipe cues_c.py `spread_summon_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. The draw-sword ring trimmed, high-passed at 1.8 kHz and widened 1.5x (-2 dB); the Suspended Cymbal fff hit high-passed at 1.5 kHz and reversed into a 0.95 s peak (-6 dB); Hand Chimes A#5 (Bb, -4 dB) and A#3 low-passed at 3 kHz (-12 dB) entering at 0.95 s; hall wet -8 dB (1.0 s IR). Target K-weighted 100 ms peak -14 dB; measured -14.3 dB, true peak -7.56 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +1.53 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -17.49 → -15.96 LUFS (+1.52 dB of the chorus cues' planned +1.53 dB), true peak -6.55 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `b211b911d30620c371a3bd495a67711369478a8af2deb348f846a6d7d6e8718e`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletSpreadSuccess.ogg`
- Asset ID: scarlet-sfx-spread-success-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet Spread success cue (1.60 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: chime_As5, chime_E4, chime_Fs4, mark_trees, timpani5, nepal_bells, glock_G4 in the table above
- Tool/model/version: external recipe cues_c.py `spread_success_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Hand Chimes F#4 (Gb, -2 dB), A#5 (Bb, -5 dB, +25 ms) and E4 pitched -1 then +12 (Eb, -4 dB, +50 ms); Nepalese bells fx2_short low-passed at 8 kHz (-5 dB); Glockenspiel G4 pitched -1 (Gb, -8 dB); Mark Trees ascending (0.05-1.2 s) high-passed at 3 kHz (-14 dB); Timpani 5 hit (Gb3) low-passed at 350 Hz (-12 dB); hall wet -8 dB (1.2 s IR). Target K-weighted 100 ms peak -13 dB; measured -13.1 dB, true peak -6.85 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +1.53 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -18.68 → -17.14 LUFS (+1.55 dB of the chorus cues' planned +1.53 dB), true peak -5.07 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `84b6ffab358b5adc2121ff220a0176af205d02e09f33d4a21abc5a71fe6abffa`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletSpreadFail.ogg`
- Asset ID: scarlet-sfx-spread-fail-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet Spread failure (red cut) cue (0.45 s), layered from CC0 recordings
- Creator: recordings by audione, JoelAudio, NeoSpica, qubodup, Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: timpani5, zap, slicing_flesh, sword_hit, sword_ring in the table above
- Tool/model/version: external recipe cues_c.py `spread_fail_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Slicing through flesh, onset-trimmed and high-passed at 400 Hz; the sword hit high-passed at 900 Hz (-4 dB); the sword ring pitched -0.71 semitone (Eb) and filtered 1.2-9 kHz (-6 dB, +20 ms); Timpani 5 hit (Gb3) low-passed at 380 Hz (-7 dB); the JoelAudio zap high-passed at 2.5 kHz (-14 dB); hall wet -12 dB (0.5 s IR); the mix is shaped down 15 dB over 0.45 s and cut there with a 12 ms fade. Target K-weighted 100 ms peak -10 dB; measured -10.3 dB, true peak -4.19 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +1.53 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -14.27 → -12.65 LUFS (+1.61 dB of the chorus cues' planned +1.53 dB), true peak -3.30 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `81886dafda8e43f915a64ba052312538cf54ce37f99e096ea4c1f04737697a31`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletActChange.ogg`
- Asset ID: scarlet-sfx-act-change-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet act change hit cue (2.40 s), layered from CC0 recordings
- Creator: recordings by kasparsj, Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: bass_drum, gong_full, cymbal_hit, timpani1, bell_D3, stone_heavy in the table above
- Tool/model/version: external recipe cues_d.py `act_change_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. The Gong 2 full hit (unshifted) with an explicit decay to -38 dB at 1.67 s; Tubular Bells D3 pitched -11 semitones (Eb2, -4 dB); Timpani 1 hit (Gb2, -3 dB); Bass Drum 1 low-passed at 170 Hz (-4 dB); the Suspended Cymbal fff hit high-passed at 1.5 kHz (-12 dB); the heavy stone impact low-passed at 1.8 kHz (-12 dB); hall wet -8 dB (1.6 s IR); -46.8 dB re peak at 1.67 s. Target K-weighted 100 ms peak -8 dB; measured -8.1 dB, true peak -4.27 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +1.50 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -13.69 → -12.18 LUFS (+1.52 dB of the big cues' planned +1.50 dB), true peak -2.49 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `70d3664672442484e29d18e8b6208e9424f5856fbd96df0fb97cad897c68abb9`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletSacrifice.ogg`
- Asset ID: scarlet-sfx-sacrifice-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet Final sacrifice swell and flash cue (2.20 s), layered from CC0 recordings
- Creator: recordings by Bananaboatman33, DARTEKZ_GAMEZ, JoelAudio, Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: gong_f, timpani1, bell_D4, glass8, siren, zap, demon_howl, wind_whirl in the table above
- Tool/model/version: external recipe cues_d.py `sacrifice_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Gong 1 gong_2_f (-0.12 semitone, Eb) and the demon howl (-3 semitones, low-passed at 3 kHz) reversed into a flash at 0.95 s, with an accelerating auto-pan (1.5 to 6 Hz) and undulation (2 to 9 Hz); the wind whirl reversed, low-passed at 1.5 kHz and auto-panned (2 to 7 Hz); siren3 (0.6-2.2 s) filtered 300 Hz-3 kHz rising into the flash; at 0.95 s glass_break8 high-passed at 2.5 kHz and widened 1.6x, Tubular Bells D4 +1 (Eb4), the JoelAudio zap high-passed at 2.5 kHz and the Timpani 1 hit (Gb2); hall wet -9 dB (0.9 s IR); -45.1 dB re peak at 1.67 s. Target K-weighted 100 ms peak -9 dB; measured -9.9 dB, true peak -2.05 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +2.38 dB of gain and the look-ahead limiter at a -1.30 dBTP ceiling (1.88 dB of gain reduction at most); maximum 400 ms momentary loudness (BS.1770) -15.88 → -14.45 LUFS (+1.43 dB of the big cues' planned +1.50 dB), true peak -1.34 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `b42ac77295e3e68756b4b17793c5d1661ab08f92b4b978d0693c5f2f5a5018d3`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletVictory.ogg`
- Asset ID: scarlet-sfx-victory-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet Victory bells cue (2.40 s), layered from CC0 recordings
- Creator: recordings by Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: gong_f, timpani1, bell_As3, bell_D3, bell_D4, organ_04, organ_28 in the table above
- Tool/model/version: external recipe cues_d.py `victory_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Tubular Bells D3 +1 (Eb3), A#3 (Bb3, -3 dB) and D4 +1 (Eb4, -5 dB) staggered by 35 ms; Gong 1 gong_2_f (-0.12 semitone, Eb, -6 dB); the organ Eb2/Eb3/Bb3 chord (Man3Open_04, Man3Open_28, Man3Open_28 +7) low-passed at 2.2 kHz swelling to 0.45 s (-3 dB); Timpani 1 hit (Gb2, -5 dB); a shared decay to -36 dB at 1.67 s and silence at 2.4 s; hall wet -8 dB (1.6 s IR). Target K-weighted 100 ms peak -8 dB; measured -8.1 dB, true peak -4.88 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +1.50 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -11.82 → -10.32 LUFS (+1.50 dB of the big cues' planned +1.50 dB), true peak -3.48 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `31ceed9449a84071cac3d4eb780fbb0b388013e7114b334fe4ba0accf7b4a1d0`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletDown.ogg`
- Asset ID: scarlet-sfx-down-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet member Down cue (1.30 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: bass_drum, gong_p, timpani1 in the table above
- Tool/model/version: external recipe cues_d.py `down_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Timpani 1 hit (Gb2) low-passed at 900 Hz; Bass Drum 1 low-passed at 160 Hz (-3 dB); Gong 1 gong_p pitched -0.61 semitone (Db) and low-passed at 1.2 kHz (-6 dB); all decay by 1.2 s; hall wet -9 dB (1.0 s IR). Target K-weighted 100 ms peak -13 dB; measured -12.9 dB, true peak -9.94 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +2.14 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -20.46 → -18.31 LUFS (+2.15 dB of the status cues' planned +2.14 dB), true peak -7.73 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `a1f8acd63a999f56a10e47c271a0e1aafdf862a13385c553ea4bd07d20dbd075`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletRevive.ogg`
- Asset ID: scarlet-sfx-revive-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet member revive cue (1.60 s), layered from CC0 recordings
- Creator: recordings by Simon Dalzell (Ivy Audio), Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: chime_As5, chime_E4, chime_Fs4, timpani1, organ_04 in the table above
- Tool/model/version: external recipe cues_d.py `revive_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Hand Chimes F#4 (Gb, -3 dB), E4 pitched -1 then +12 (Eb, -4 dB) and A#5 (Bb, -5 dB) entering at 0/70/140 ms and held 1.4 s so they blend into a chord; organ Man3Open_04 (Eb2) low-passed at 600 Hz swelling to 0.7 s (-6 dB); Timpani 1 hit low-passed at 500 Hz (-12 dB); hall wet -8 dB (1.2 s IR). Target K-weighted 100 ms peak -15 dB; measured -15.1 dB, true peak -10.4 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +2.14 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -18.93 → -16.77 LUFS (+2.16 dB of the status cues' planned +2.14 dB), true peak -8.95 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `520b56fb3462bfe65c0bcc6e8fa9b3ad87795a1419333e01a88e6014ee893079`

- Runtime file: `Assets/Sounds/CrimsonFoundry/ScarletReady.ogg`
- Asset ID: scarlet-sfx-ready-20261002
- Asset type: stereo 48 kHz Vorbis Scarlet Ready vote cue (1.10 s), layered from CC0 recordings
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: chime_As5, chime_E4 in the table above
- Tool/model/version: external recipe cues_a.py `ready_A` with common.py, kit.py, render.py and the studio hall IR (hashes above); Python 3.12.10, NumPy 2.5.3, SciPy 1.18.1, pyloudnorm 0.2.0, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.5, pinned Ogg serial; level lift by the external `sfx-scarlet-loud/lift.py` (loudness revision above)
- Human modifications: owner selection of variant A. Hand Chimes E4 pitched -1 (Eb) and A#5 (Bb, -6 dB, +30 ms) with 0.9-1.0 s decays; hall wet -6 dB (1.1 s IR). Target K-weighted 100 ms peak -21 dB; measured -21.0 dB, true peak -14.75 dBTP. Recordings stay in the local source store and are excluded from distribution. Loudness revision 2026-10-03 (0.3.82): this take's recipe master, its auditioned bytes reproduced first, re-rendered with +2.14 dB of gain and no limiting; maximum 400 ms momentary loudness (BS.1770) -27.09 → -24.92 LUFS (+2.18 dB of the status cues' planned +2.14 dB), true peak -12.46 dBTP after the Vorbis round trip; same pinned serial. Nothing else changed.
- License and redistribution terms: CC0 1.0 recordings and samples; the layered cue follows the existing project asset terms; no raw sample is distributed
- Required attribution: none required by CC0; retain the tables above as courtesy credit to Versilian Studios / Sam Gossner (VSCO 2 CE, VCSL), Simon Dalzell / Ivy Audio (VSCO organ) and the recording authors; a public credit also links the VSCO: CE homepage as its readme asks
- Reviewer and review date: owner audition and selection of variant A, 2026-10-02; Claude review 2026-10-02 (source licenses, hashes, loudness, true peak, decay, boundaries, byte-identical re-render); loudness revision by Claude 2026-10-03 (auditioned bytes reproduced before the lift, second run byte-identical, loudness and true peak measured after encoding); in-game mix not_run
- SHA256: `ae2d69ddda29664ea653d976c7f3477b8566a18976d54175c4cabb7aeb060008`

### Doll weapon pixel art — 2026-10-02

Original pixel art for the five refreshed Doll weapons (the Null Refrain claw, Pale Meridian, Lacuna Testament, Choir of the Unmade and Last Witness) and the buff, generated by Codex from Claude's owner-directed brief; no artist, work or franchise imitation was requested and no third-party image was used as input (the reference images the brief names were inspected only; every edit's only image input is a candidate Codex had itself generated). The delivery stays outside the repository: `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` SHA256 `ad6d3459a20b35345cfeac5e87ab8bc07e8a43e9dcac6316f7293bc62d83a352` and `asset-deliveries/doll-weapons/2026-10-02/manifest.json` (full prompts per candidate) SHA256 `077fda374114822a095e6aa53398a4584b59e471e011441d95a893af3c3ae375`. The owner approved all 21 delivered picks on 2026-10-03. [`tools/export_doll_weapon_art.py`](../tools/export_doll_weapon_art.py) owns the mechanical export (dot pitch, palette snap, integer reduction, frame strips, gun parts) and the anchors in `Client/Encounters/FirstSeverance/Weapons/DollArtAnchors.g.cs`; it pins each of the 21 inputs by SHA-256. The art map is in [WEAPONS.md](../docs/encounters/first-severance/WEAPONS.md#reward-refresh-2026-10--shared-rules).

- Runtime file: `Assets/Textures/Items/DollWeapons/ClawOpen.png`
- Asset ID: doll-weapon-art-clawopen-20261002
- Asset type: 47x46 Lacrimosa claw right hand, open (fanned) pose; one texel per dot (2 world px, 94x92 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW01 candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW01_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW01_a.png`), SHA256 `6c7e04b11e5cf2e30c150cc2cbaa958880f4374aeebaf74c93e4fd2df035f43b`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW01, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.135 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 4.3, p95 9.79, max 17.94), integer reduction by k = 2 (rule k 2 from a major-axis ratio of 1.684, the review names k 2; a texel is the majority palette colour of its 14.319 x 14.152 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 99.2 % of the outer boundary), 47x46 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW01 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW01_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `3e96ebf70f4a2a3fb847e27b5e49d4d50ed8b227536b183496c7efe1653844f5`

- Runtime file: `Assets/Textures/Items/DollWeapons/ClawOpen_L.png`
- Asset ID: doll-weapon-art-clawopen_l-20261002
- Asset type: 94x91 Lacrimosa claw right hand, open (fanned) pose; one texel per dot (2 world px, 188x182 world px), integer k = 1, larger rung of the pair
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW01 candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW01_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW01_a.png`), SHA256 `6c7e04b11e5cf2e30c150cc2cbaa958880f4374aeebaf74c93e4fd2df035f43b`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW01, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.135 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 4.3, p95 9.79, max 17.94), integer reduction by k = 1 (rule k 2 from a major-axis ratio of 1.684, the review names k 2; a texel is the majority palette colour of its 7.16 x 7.154 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 99.8 % of the outer boundary), 94x91 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW01 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW01_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `abf6d5900ab77b176392f0f902b332725eefbd134b8b1efccea2fb287d4e423e`

- Runtime file: `Assets/Textures/Items/DollWeapons/ClawRake.png`
- Asset ID: doll-weapon-art-clawrake-20261002
- Asset type: 41x35 Lacrimosa claw right hand, raking pose; one texel per dot (2 world px, 82x70 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW01 candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW01_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW01_a.png`), SHA256 `6c7e04b11e5cf2e30c150cc2cbaa958880f4374aeebaf74c93e4fd2df035f43b`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW01, cell [1, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.135 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 4.22, p95 9.52, max 17.94), integer reduction by k = 2 (rule k 2 from a major-axis ratio of 1.582, the review names k 2; a texel is the majority palette colour of its 14.317 x 14.114 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 99.3 % of the outer boundary), 41x35 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW01 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW01_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `0c253ca51d511b08d69c726f20796a2423eb25c07743b49a6e191ed58aeb0393`

- Runtime file: `Assets/Textures/Items/DollWeapons/ClawRake_L.png`
- Asset ID: doll-weapon-art-clawrake_l-20261002
- Asset type: 82x69 Lacrimosa claw right hand, raking pose; one texel per dot (2 world px, 164x138 world px), integer k = 1, larger rung of the pair
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW01 candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW01_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW01_a.png`), SHA256 `6c7e04b11e5cf2e30c150cc2cbaa958880f4374aeebaf74c93e4fd2df035f43b`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW01, cell [1, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.135 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 4.22, p95 9.52, max 17.94), integer reduction by k = 1 (rule k 2 from a major-axis ratio of 1.582, the review names k 2; a texel is the majority palette colour of its 7.159 x 7.159 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 99.7 % of the outer boundary), 82x69 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW01 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW01_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `9b6758239eafa960b456719f4dbad5b60bf66ed0e1fd25520a7b783bb5658abd`

- Runtime file: `Assets/Textures/Items/DollWeapons/ClawClench.png`
- Asset ID: doll-weapon-art-clawclench-20261002
- Asset type: 37x31 Lacrimosa claw right hand, clenched (crushing) pose; one texel per dot (2 world px, 74x62 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW01B candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW01B_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW01B_a.png`), SHA256 `e19fd8362268c07ea9db2e07b865bb96d8ee0ef6c6a14749742949ec85daf101`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW01_a.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW01B, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.135 px (the DW01 family pitch; the sheet's own 8.592 px is recorded but the edit is registered on its base), every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 4.77, p95 11.73, max 17.94), integer reduction by k = 2 (rule k 2 from a major-axis ratio of 1.832, the review names k 2; a texel is the majority palette colour of its 14.135 x 14.129 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 100 % of the outer boundary), 37x31 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW01B of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW01B_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `100ba62af84aba8e9d468f5c116c3254ba017e508528d44ba011d2ea1a7aa159`

- Runtime file: `Assets/Textures/Items/DollWeapons/ClawClench_L.png`
- Asset ID: doll-weapon-art-clawclench_l-20261002
- Asset type: 73x61 Lacrimosa claw right hand, clenched (crushing) pose; one texel per dot (2 world px, 146x122 world px), integer k = 1, larger rung of the pair
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW01B candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW01B_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW01B_a.png`), SHA256 `e19fd8362268c07ea9db2e07b865bb96d8ee0ef6c6a14749742949ec85daf101`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW01_a.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW01B, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.135 px (the DW01 family pitch; the sheet's own 8.592 px is recorded but the edit is registered on its base), every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 4.77, p95 11.73, max 17.94), integer reduction by k = 1 (rule k 2 from a major-axis ratio of 1.832, the review names k 2; a texel is the majority palette colour of its 7.164 x 7.18 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 100 % of the outer boundary), 73x61 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW01B of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW01B_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `15c5d2f4f55770835d5446ab5629d1155d26abbb09743db554ee2974da909184`

- Runtime file: `Assets/Textures/Items/DollWeapons/ClawThrust.png`
- Asset ID: doll-weapon-art-clawthrust-20261002
- Asset type: 50x23 Lacrimosa claw right hand, thrusting pose; one texel per dot (2 world px, 100x46 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW01B candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW01B_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW01B_a.png`), SHA256 `e19fd8362268c07ea9db2e07b865bb96d8ee0ef6c6a14749742949ec85daf101`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW01_a.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW01B, cell [1, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.135 px (the DW01 family pitch; the sheet's own 8.592 px is recorded but the edit is registered on its base), every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 5.11, p95 13.85, max 17.94), integer reduction by k = 2 (rule k 2 from a major-axis ratio of 1.568, the review names k 2; a texel is the majority palette colour of its 14.32 x 14.261 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 100 % of the outer boundary), 50x23 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW01B of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW01B_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `1fd2bfb5e96d2030cfd1065a290dad9356da16feb1428956e52e24a486030761`

- Runtime file: `Assets/Textures/Items/DollWeapons/ClawThrust_L.png`
- Asset ID: doll-weapon-art-clawthrust_l-20261002
- Asset type: 100x46 Lacrimosa claw right hand, thrusting pose; one texel per dot (2 world px, 200x92 world px), integer k = 1, larger rung of the pair
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW01B candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW01B_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW01B_a.png`), SHA256 `e19fd8362268c07ea9db2e07b865bb96d8ee0ef6c6a14749742949ec85daf101`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW01_a.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW01B, cell [1, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.135 px (the DW01 family pitch; the sheet's own 8.592 px is recorded but the edit is registered on its base), every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 5.11, p95 13.85, max 17.94), integer reduction by k = 1 (rule k 2 from a major-axis ratio of 1.568, the review names k 2; a texel is the majority palette colour of its 7.16 x 7.13 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 100 % of the outer boundary), 100x46 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW01B of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW01B_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `f06c4230d094b666316562dd5212a4d2f5e80aa11add2c5ca3a14e66e7fe903b`

- Runtime file: `Assets/Textures/Items/DollWeapons/NullRefrainIcon.png`
- Asset ID: doll-weapon-art-nullrefrainicon-20261002
- Asset type: 62x40 Null Refrain claw weapon inventory icon (two crossed hands) (31x20 logical texels, each stored 2x2 px)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW01I candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW01I_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW01I_a.png`), SHA256 `22b6d558821348b6d6acf8e8202684c247b46c4e76cb9fdcb69361291f11fe49`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW01_a.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW01I, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 8.01 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 5.02, p95 12.5, max 17.94), integer reduction by k = 4 to 31x20 logical texels (fills 96.9 % of the 32x32 limit; the one-dot outline rule leaves ink on 84.2 % of the outer boundary), stored at 2x as 62x40 px. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW01I of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW01I_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `52d621fe54c7086b06279feb3014797fcfb5a0e79f3018f0d82f68217452ca74`

- Runtime file: `Assets/Textures/Items/DollWeapons/MeridianGun.png`
- Asset ID: doll-weapon-art-meridiangun-20261002
- Asset type: 87x17 Pale Meridian assembled siege gun; one texel per dot (2 world px, 174x34 world px), integer k = 3
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW02 candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW02_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW02_a.png`), SHA256 `ceaf1423c2f381242d5557edf4d121a85f5afca896979c4d40c857f4ddc41026`; original built-in image generation from the Doll weapon brief, text-only (no image input); role: pixels; DW02E candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW02E_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW02E_a.png`), SHA256 `86aa6a8aade4fc1d429309393ff27d60c6c16d1600668b3d235469a6f55314ef`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW02_a.png` (the only image input); role: registration only: fixes the shared canvas
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW02, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.199 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 3.22, p95 7.47, max 17.94), integer reduction by k = 3 (a texel is the majority palette colour of its 21.506 x 22.176 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 89.3 % of the outer boundary) on a canvas shared with MeridianBare and the part seats (DW02E registered onto DW02 by the integer shift (6, 6) px (opaque IoU 0.8392)), 87x17 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: DW02: section DW02 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW02_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; DW02E: section DW02E of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW02E_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `31789389a50af07c603cda925de7a5bdaffd86279379da85088ecc7ccb783942`

- Runtime file: `Assets/Textures/Items/DollWeapons/MeridianBare.png`
- Asset ID: doll-weapon-art-meridianbare-20261002
- Asset type: 87x17 Pale Meridian bare gun with its four brass parts removed (same canvas as MeridianGun); one texel per dot (2 world px, 174x34 world px), integer k = 3
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW02E candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW02E_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW02E_a.png`), SHA256 `86aa6a8aade4fc1d429309393ff27d60c6c16d1600668b3d235469a6f55314ef`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW02_a.png` (the only image input); role: pixels; DW02 candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW02_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW02_a.png`), SHA256 `ceaf1423c2f381242d5557edf4d121a85f5afca896979c4d40c857f4ddc41026`; original built-in image generation from the Doll weapon brief, text-only (no image input); role: registration only: DW02E is shifted onto DW02
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW02E, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.199 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 2.7, p95 6.62, max 17.94), integer reduction by k = 3 (a texel is the majority palette colour of its 21.506 x 22.176 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 98.4 % of the outer boundary) on a canvas shared with MeridianGun and the part seats (DW02E registered onto DW02 by the integer shift (6, 6) px (opaque IoU 0.8392)), 87x17 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: DW02E: section DW02E of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW02E_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; DW02: section DW02 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW02_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `a6f7a286d2aeaa215c29e5c9b7b437586de6071ed66d2cf236a9702d0518c40a`

- Runtime file: `Assets/Textures/Items/DollWeapons/MeridianParts.png`
- Asset ID: doll-weapon-art-meridianparts-20261002
- Asset type: 188x6 Pale Meridian four brass parts (cylinder, shroud, sight, spring housing), one per cell, cut from the difference between the assembled and the bare gun, 4 cells of 47x6 texels; one texel per dot (2 world px, 376x12 world px), integer k = 3
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW02 candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW02_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW02_a.png`), SHA256 `ceaf1423c2f381242d5557edf4d121a85f5afca896979c4d40c857f4ddc41026`; original built-in image generation from the Doll weapon brief, text-only (no image input); role: pixels; DW02E candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW02E_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW02E_a.png`), SHA256 `86aa6a8aade4fc1d429309393ff27d60c6c16d1600668b3d235469a6f55314ef`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW02_a.png` (the only image input); role: reference: the cut keeps the DW02 pixels that DW02E does not repeat
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidates a of DW02 and DW02E, generated on transparent backgrounds (byte copies of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.199 px; DW02E registered onto DW02 by the integer shift (6, 6) px (opaque IoU 0.8392); the parts are DW02's non-ivory pixels that DW02E does not repeat within deltaE 10, opened by 2 dots, snapped to the Doll palette as for MeridianGun and reduced by k = 3 on the shared canvas into four equal 47x6-texel cells (cylinder 5x5, shroud 47x2, sight 4x6, spring_housing 3x6 texels; seats recorded as anchors). Labels (sight, shroud, spring housing, cylinder) follow the exporter's geometric rules. The assembled-gun check (opaque IoU 0.957) is preview only and not shipped. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: DW02: section DW02 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW02_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; DW02E: section DW02E of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW02E_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `ee6c91b876e726d7870a7063d61823b0a2fd279c76a0947ac931449f63b1c757`

- Runtime file: `Assets/Textures/Items/DollWeapons/MeridianKey.png`
- Asset ID: doll-weapon-art-meridiankey-20261002
- Asset type: 144x30 Pale Meridian wind-up key, four rotation frames, 4 frames of 36x30 texels; one texel per dot (2 world px, 288x60 world px), integer k = 1
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW02K candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW02K_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW02K_a.png`), SHA256 `f96923dca52da7c57c94c980d3d4bd5fab967a7163a326c508bd898679d91e1f`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW02_a.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW02K, 4 frames from cells [0, 0], [1, 0], [0, 1], [1, 1], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 11.5 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 4.14-4.23 and max deltaE 17.94 over the 4 frames), integer reduction by k = 1 (opaque at 50 % coverage; the one-dot outline rule leaves ink on all of each frame's outer boundary), then the frames are cut into equal 36x30-texel cells registered on their anchor (shaft bottom: centre of the lowest opaque row (bottom edge)), one strip 144x30 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW02K of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW02K_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `a462e9670552d487c3a5df810bd4795150f17134aed9fccfd9882fdceffd11e2`

- Runtime file: `Assets/Textures/Items/DollWeapons/PaleMeridianIcon.png`
- Asset ID: doll-weapon-art-palemeridianicon-20261002
- Asset type: 56x40 Pale Meridian inventory icon (28x20 logical texels, each stored 2x2 px)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW02I candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW02I_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW02I_a.png`), SHA256 `96239792625ad0b7791fc98713bf504c5fc867a6108c86c2d7f89ee73ddc1ed6`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW02_a.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW02I, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 10.265 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 3.24, p95 8.09, max 17.94), integer reduction by k = 4 to 28x20 logical texels (fills 87.5 % of the 32x32 limit; the one-dot outline rule leaves ink on 98.4 % of the outer boundary), stored at 2x as 56x40 px. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW02I of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW02I_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `e07009d8dcc10468ef6877eb83bfd15ab767a753f77ba9461aa25bba1589e1ee`

- Runtime file: `Assets/Textures/Items/DollWeapons/LacunaBook.png`
- Asset ID: doll-weapon-art-lacunabook-20261002
- Asset type: 42x55 Lacuna Testament book with a see-through hole; one texel per dot (2 world px, 84x110 world px), integer k = 1
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW03 candidate b `asset-deliveries/doll-weapons/2026-10-02/alpha/DW03_b.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW03_b.png`), SHA256 `55658616da6acc0b23df076daf00995cbd7ff56c66b052c7f6866bbffa82b316`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate b of DW03, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 11.022 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 2.52, p95 7.02, max 17.94), integer reduction by k = 1 (rule k 1 from a major-axis ratio of 1.452, the review names k 2; a texel is the majority palette colour of its 10.976 x 11.055 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 100 % of the outer boundary), 42x55 texels. Codex marked this candidate accepted_by_codex=false because its mandatory aperture test failed: the largest enclosed aperture contains alpha up to 128 (the test allows 15; 98.2 % of it is at or below 15) and Codex applied no cleanup; the owner approved it on 2026-10-03. The alpha clean and the 50 % coverage rule keep the opening: the exported sprite has a see-through hole of 113 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW03 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW03_b.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `e85cc632d7d4fc7af2f964f0a9a731d99ad1e1d8c02179a8f504e4bc87c56e07`

- Runtime file: `Assets/Textures/Items/DollWeapons/LacunaBook_S.png`
- Asset ID: doll-weapon-art-lacunabook_s-20261002
- Asset type: 21x28 Lacuna Testament book with a see-through hole; one texel per dot (2 world px, 42x56 world px), integer k = 2, smaller rung of the pair
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW03 candidate b `asset-deliveries/doll-weapons/2026-10-02/alpha/DW03_b.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW03_b.png`), SHA256 `55658616da6acc0b23df076daf00995cbd7ff56c66b052c7f6866bbffa82b316`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate b of DW03, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 11.022 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 2.52, p95 7.02, max 17.94), integer reduction by k = 2 (rule k 1 from a major-axis ratio of 1.452, the review names k 2; a texel is the majority palette colour of its 21.952 x 21.714 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 100 % of the outer boundary), 21x28 texels. Codex marked this candidate accepted_by_codex=false because its mandatory aperture test failed: the largest enclosed aperture contains alpha up to 128 (the test allows 15; 98.2 % of it is at or below 15) and Codex applied no cleanup; the owner approved it on 2026-10-03. The alpha clean and the 50 % coverage rule keep the opening: the exported sprite has a see-through hole of 26 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW03 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW03_b.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `5fecc41c18ff15b771f2f57b7f7c76d455ad51b7ae9778335b4c2e2d2125d2c2`

- Runtime file: `Assets/Textures/Items/DollWeapons/LacunaTestamentIcon.png`
- Asset ID: doll-weapon-art-lacunatestamenticon-20261002
- Asset type: 34x46 Lacuna Testament inventory icon (17x23 logical texels, each stored 2x2 px)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW03I candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW03I_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW03I_a.png`), SHA256 `860d363cb63520cc39dd20de7bb1a64d8493c44d108f760001bcb074cc6d54d1`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW03_b.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW03I, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 12.097 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 2.16, p95 6.17, max 17.94), integer reduction by k = 2 to 17x23 logical texels (fills 71.9 % of the 32x32 limit; the one-dot outline rule leaves ink on 100 % of the outer boundary), stored at 2x as 34x46 px. Codex marked this candidate accepted_by_codex=false because its mandatory aperture test failed: the largest enclosed aperture contains alpha up to 128 (the test allows 15; 96.4 % of it is at or below 15) and Codex applied no cleanup; the owner approved it on 2026-10-03. The alpha clean and the 50 % coverage rule keep the opening: the exported sprite has a see-through hole of 15 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW03I of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW03I_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `3c2a7d1ea52daba4bbaa4312ac8fadd2b6ea390b98f1e30afa7b2847167c7607`

- Runtime file: `Assets/Textures/Items/DollWeapons/LacunaIris.png`
- Asset ID: doll-weapon-art-lacunairis-20261002
- Asset type: 100x25 Lacuna porcelain iris, four aperture frames, 4 frames of 25x25 texels; one texel per dot (2 world px, 200x50 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW03A candidate d `asset-deliveries/doll-weapons/2026-10-02/alpha/DW03A_d.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW03A_d.png`), SHA256 `93847c4c11d6cd607aa0c191c4ce2eee644fd53945b4325cbe99d153c45d60d5`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate d of DW03A, 4 frames from cells [0, 0], [1, 0], [0, 1], [1, 1], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 6.826 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 4.0-4.47 and max deltaE 17.94 over the 4 frames), integer reduction by k = 2 (opaque at 50 % coverage; the one-dot outline rule leaves ink on all of each frame's outer boundary), then the frames are cut into equal 25x25-texel cells registered on their anchor (ring centre (least-squares circle through the brass)), one strip 100x25 texels. Codex marked this candidate accepted_by_codex=false because its mandatory aperture test failed: the largest enclosed aperture contains alpha up to 128 (the test allows 15; 97.7 % of it is at or below 15) and Codex applied no cleanup; the owner approved it on 2026-10-03. The alpha clean and the 50 % coverage rule keep the opening: the exported sprite has a see-through hole of 177 texels in its open frame. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW03A of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW03A_d.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `bee3fe9f6743e5b71a5d41a03a3f05c751870814a960ea3b52efcda9e5701f8b`

- Runtime file: `Assets/Textures/Items/DollWeapons/LacunaGreatIris.png`
- Asset ID: doll-weapon-art-lacunagreatiris-20261002
- Asset type: 78x80 Lacuna great porcelain iris (notched ring with folded-back petals); one texel per dot (2 world px, 156x160 world px), integer k = 1
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW03B candidate c `asset-deliveries/doll-weapons/2026-10-02/alpha/DW03B_c.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW03B_c.png`), SHA256 `eeea2e95f4aa9e71765505d0a1d9e78862c3fed326ea094281b6b432123b0056`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW03A_d.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate c of DW03B, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 9.601 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 4.03, p95 8.44, max 17.94), integer reduction by k = 1 (rule k 1 from a major-axis ratio of 1.326, the review names k 1; a texel is the majority palette colour of its 9.641 x 9.55 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 100 % of the outer boundary), 78x80 texels. Codex marked this candidate accepted_by_codex=false because its mandatory aperture test failed: the largest enclosed aperture contains alpha up to 128 (the test allows 15; 99.1 % of it is at or below 15) and Codex applied no cleanup; the owner approved it on 2026-10-03. The alpha clean and the 50 % coverage rule keep the opening: the exported sprite has a see-through hole of 1383 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW03B of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW03B_c.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `6110dfea895f3dc87085ab7a835fdbcb63a709501c4065e1e1e047d27a1d3d5d`

- Runtime file: `Assets/Textures/Items/DollWeapons/Chorister0.png`
- Asset ID: doll-weapon-art-chorister0-20261002
- Asset type: 66x38 Choir of the Unmade faceless chorister, voice 1, three singing frames, 3 frames of 22x38 texels; one texel per dot (2 world px, 132x76 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW04 candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW04_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW04_a.png`), SHA256 `f105dbfe715a4b921e15021ea14717744131a502d2b5c188507df6527ed39f11`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW04, 3 frames from cells [0, 0], [1, 0], [2, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.999 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 2.46-2.51 and max deltaE 17.94 over the 3 frames), integer reduction by k = 2 (opaque at 50 % coverage; the one-dot outline rule leaves ink on all of each frame's outer boundary), then the frames are cut into equal 22x38-texel cells registered on their anchor (stand spike tip: centre of the lowest opaque row (bottom edge); one cell for all voices), one strip 66x38 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW04 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW04_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `bf9474a92e38599679047be57bf39de55f87af4aec13ab5380933fbda2fd5528`

- Runtime file: `Assets/Textures/Items/DollWeapons/Chorister1.png`
- Asset ID: doll-weapon-art-chorister1-20261002
- Asset type: 66x38 Choir of the Unmade faceless chorister, voice 2, three singing frames, 3 frames of 22x38 texels; one texel per dot (2 world px, 132x76 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW04V2 candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW04V2_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW04V2_a.png`), SHA256 `ff71c603eeaae988e3684a2b4bc5bc69ac9a5e6a4167a9c624626008b6b75a32`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW04_a.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW04V2, 3 frames from cells [0, 0], [1, 0], [2, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.999 px (the DW04 family pitch; the sheet's own 9.068 px is recorded but the edit is registered on its base), every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 2.94-2.96 and max deltaE 17.94 over the 3 frames), integer reduction by k = 2 (opaque at 50 % coverage; the one-dot outline rule leaves ink on all of each frame's outer boundary), then the frames are cut into equal 22x38-texel cells registered on their anchor (stand spike tip: centre of the lowest opaque row (bottom edge); one cell for all voices), one strip 66x38 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW04V2 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW04V2_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `981532069fe37d7c7eb93de420d5d2fa5f7111d8526b85fa2b5575cf3acc6b9a`

- Runtime file: `Assets/Textures/Items/DollWeapons/Chorister2.png`
- Asset ID: doll-weapon-art-chorister2-20261002
- Asset type: 66x38 Choir of the Unmade faceless chorister, voice 3, three singing frames, 3 frames of 22x38 texels; one texel per dot (2 world px, 132x76 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW04V3 candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW04V3_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW04V3_a.png`), SHA256 `dc3f054b9568d18cf129d5d774fc12f624e244d1d6c488084758b84c428b298a`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW04_a.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW04V3, 3 frames from cells [0, 0], [1, 0], [2, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 7.999 px (the DW04 family pitch; the sheet's own 7.981 px is recorded but the edit is registered on its base), every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 3.08-3.18 and max deltaE 17.94 over the 3 frames), integer reduction by k = 2 (opaque at 50 % coverage; the one-dot outline rule leaves ink on all of each frame's outer boundary), then the frames are cut into equal 22x38-texel cells registered on their anchor (stand spike tip: centre of the lowest opaque row (bottom edge); one cell for all voices), one strip 66x38 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW04V3 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW04V3_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `a374143950a3a55da7d71b621e91c1547382fe7c6847374aac8d4bf9d050756f`

- Runtime file: `Assets/Textures/Items/DollWeapons/ChoirOrgan.png`
- Asset ID: doll-weapon-art-choirorgan-20261002
- Asset type: 95x96 Choir of the Unmade pipe organ with a see-through mouth; one texel per dot (2 world px, 190x192 world px), integer k = 1
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW04P candidate c `asset-deliveries/doll-weapons/2026-10-02/alpha/DW04P_c.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW04P_c.png`), SHA256 `75c958cdd35ccd65bb141f195ab15e356fa67c6c813d44406023781efe86bee5`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW04_a.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate c of DW04P, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 8.626 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 3.82, p95 8.29, max 17.94), integer reduction by k = 1 (rule k 1 from a major-axis ratio of 1.082, the review names k 1; a texel is the majority palette colour of its 8.642 x 8.615 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 100 % of the outer boundary), 95x96 texels. Codex marked this candidate accepted_by_codex=false because its mandatory aperture test failed: the largest enclosed aperture contains alpha up to 128 (the test allows 15; 98.3 % of it is at or below 15) and Codex applied no cleanup; the owner approved it on 2026-10-03. The alpha clean and the 50 % coverage rule keep the opening: the exported sprite has a see-through hole of 272 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW04P of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW04P_c.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `2c294af62e6be01d0edf05d2c39179c6e1e66e443b1116c731f47cb8ed061e00`

- Runtime file: `Assets/Textures/Items/DollWeapons/ChoirBaton.png`
- Asset ID: doll-weapon-art-choirbaton-20261002
- Asset type: 27x28 Choir of the Unmade baton; one texel per dot (2 world px, 54x56 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW04S candidate c `asset-deliveries/doll-weapons/2026-10-02/alpha/DW04S_c.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW04S_c.png`), SHA256 `fa4fdf2a5bc61c87718e22941544f669017d588c9ea75829465aff49ee878928`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate c of DW04S, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 9.079 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 2.8, p95 7.63, max 17.94), integer reduction by k = 2 (rule k 2 from a major-axis ratio of 1.742, the review names k 2; a texel is the majority palette colour of its 17.926 x 18.071 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 100 % of the outer boundary), 27x28 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW04S of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW04S_c.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `80d2288065b472297919dd7027a76e8c90846d08598a7e5bb3d2ac876b9e8735`

- Runtime file: `Assets/Textures/Items/DollWeapons/ChoirOfTheUnmadeIcon.png`
- Asset ID: doll-weapon-art-choiroftheunmadeicon-20261002
- Asset type: 54x56 Choir of the Unmade inventory icon (27x28 logical texels, each stored 2x2 px)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW04S candidate c `asset-deliveries/doll-weapons/2026-10-02/alpha/DW04S_c.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW04S_c.png`), SHA256 `fa4fdf2a5bc61c87718e22941544f669017d588c9ea75829465aff49ee878928`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate c of DW04S, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 9.079 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 2.8, p95 7.63, max 17.94), integer reduction by k = 2 to 27x28 logical texels (fills 87.5 % of the 32x32 limit; the one-dot outline rule leaves ink on 100 % of the outer boundary), stored at 2x as 54x56 px. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW04S of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW04S_c.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `e409661c95d57afb5fa0cad0aeb90f8feaf9783e6b111457ec656e59fd2c2461`

- Runtime file: `Assets/Textures/Items/DollWeapons/ChoirOfTheUnmadeBuff.png`
- Asset ID: doll-weapon-art-choiroftheunmadebuff-20261002
- Asset type: 32x32 Choir of the Unmade buff icon (16x16 logical texels, each stored 2x2 px)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW04B candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW04B_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW04B_a.png`), SHA256 `8e66aa1d211d298e58f7ad337155eb1e8a250e5f692683a18bde7b362f1d0baa`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW04_a.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW04B, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 16.68 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 3.06, p95 6.22, max 17.94), integer reduction by k = 3 to 16x16 logical texels (fills 81.2 % of the 16x16 limit; the one-dot outline rule leaves ink on 100 % of the outer boundary), stored at 2x as 32x32 px. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW04B of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW04B_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `81c925c6e66bd9788a971bfd8257695ef4558db14212aecd71f598674f595a4e`

- Runtime file: `Assets/Textures/Items/DollWeapons/WitnessBlade.png`
- Asset ID: doll-weapon-art-witnessblade-20261002
- Asset type: 65x12 Last Witness execution blade with the witness hole; one texel per dot (2 world px, 130x24 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW05 candidate b `asset-deliveries/doll-weapons/2026-10-02/alpha/DW05_b.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW05_b.png`), SHA256 `76a4967fee68e5222e691269ca6a1d7f671f468400c9eb42ff1464879d42573b`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate b of DW05, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 11.461 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 3.01, p95 6.11, max 17.94), integer reduction by k = 2 (rule k 2 from a major-axis ratio of 1.81, the review names k 2; a texel is the majority palette colour of its 22.985 x 22.333 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 100 % of the outer boundary), 65x12 texels. Codex marked this candidate accepted_by_codex=false because its mandatory aperture test failed: the largest enclosed aperture contains alpha up to 127 (the test allows 15; 94 % of it is at or below 15) and Codex applied no cleanup; the owner approved it on 2026-10-03. The alpha clean and the 50 % coverage rule keep the opening: the exported sprite has a see-through hole of 4 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW05 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW05_b.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `6fad756181c1590c1cca448037be7f00cc95d083eeebdb8cf041d9646223cf24`

- Runtime file: `Assets/Textures/Items/DollWeapons/WitnessBlade_L.png`
- Asset ID: doll-weapon-art-witnessblade_l-20261002
- Asset type: 130x23 Last Witness execution blade with the witness hole; one texel per dot (2 world px, 260x46 world px), integer k = 1, larger rung of the pair
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW05 candidate b `asset-deliveries/doll-weapons/2026-10-02/alpha/DW05_b.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW05_b.png`), SHA256 `76a4967fee68e5222e691269ca6a1d7f671f468400c9eb42ff1464879d42573b`; original built-in image generation from the Doll weapon brief, text-only (no image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate b of DW05, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 11.461 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 3.01, p95 6.11, max 17.94), integer reduction by k = 1 (rule k 2 from a major-axis ratio of 1.81, the review names k 2; a texel is the majority palette colour of its 11.492 x 11.652 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 100 % of the outer boundary), 130x23 texels. Codex marked this candidate accepted_by_codex=false because its mandatory aperture test failed: the largest enclosed aperture contains alpha up to 127 (the test allows 15; 94 % of it is at or below 15) and Codex applied no cleanup; the owner approved it on 2026-10-03. The alpha clean and the 50 % coverage rule keep the opening: the exported sprite has a see-through hole of 16 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW05 of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW05_b.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `78609fa93d9f88a822a92e79f9fc40f1297fc9166f822a8e02b2d7e00e7b3ce6`

- Runtime file: `Assets/Textures/Items/DollWeapons/WitnessSword.png`
- Asset ID: doll-weapon-art-witnesssword-20261002
- Asset type: 9x53 Last Witness judgement sword; one texel per dot (2 world px, 18x106 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW05V candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW05V_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW05V_a.png`), SHA256 `5c1853e477e4dc97de579ec25c12fde9f101ca5ddc6d7dc47b404308a08eecf4`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW05_b.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW05V, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 11.671 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 2.04, p95 5.53, max 17.94), integer reduction by k = 2 (rule k 2 from a major-axis ratio of 1.641, the review names k 2; a texel is the majority palette colour of its 24.444 x 23.132 px cell, opaque at 50 % coverage; the one-dot outline rule leaves ink on 99.1 % of the outer boundary), 9x53 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW05V of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW05V_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `b68ccf9ac801a711d7ded4960a1380669c02d3f38ee9a65d6b94afddb4ab39ed`

- Runtime file: `Assets/Textures/Items/DollWeapons/WitnessShards.png`
- Asset ID: doll-weapon-art-witnessshards-20261002
- Asset type: 48x10 Last Witness three porcelain shards, one per frame, 3 frames of 16x10 texels; one texel per dot (2 world px, 96x20 world px), integer k = 2
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW05S candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW05S_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW05S_a.png`), SHA256 `9d6b02f0e072a98b9a6d7d51b4d6a6f809d3e6753cacaa3a4847f347ab89dc4a`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW05_b.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW05S, 3 frames from cells [0, 0], [1, 0], [2, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 13.61 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 3.24-3.59 and max deltaE 17.94 over the 3 frames), integer reduction by k = 2 (opaque at 50 % coverage; the one-dot outline rule leaves ink on all of each frame's outer boundary), then the frames are cut into equal 16x10-texel cells registered on their anchor (opaque centroid (pivot)), one strip 48x10 texels. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW05S of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW05S_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `3d498dc4710a821eebb6000dec30754802958a89f4e02ec475dc1b50b65f0ee4`

- Runtime file: `Assets/Textures/Items/DollWeapons/LastWitnessIcon.png`
- Asset ID: doll-weapon-art-lastwitnessicon-20261002
- Asset type: 60x62 Last Witness inventory icon (30x31 logical texels, each stored 2x2 px)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: DW05I candidate a `asset-deliveries/doll-weapons/2026-10-02/alpha/DW05I_a.png` (byte copy of `asset-deliveries/doll-weapons/2026-10-02/raw/DW05I_a.png`), SHA256 `ac2a79946ca2232d1419eb0d7d905ba2b83d9b89bdeb9475c670483f13e905e0`; built-in image edit of the own generated candidate `asset-deliveries/doll-weapons/2026-10-02/raw/DW05_b.png` (the only image input)
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_doll_weapon_art.py` recipe SHA256 `40da38b400ea4ea7474f2032996c5b72a7cea620804ca1e638c45a4c1f75e05d` (shared helpers `tools/export_ebon_art.py` SHA256 `4a0e916a74f114316302b2c4de331845fe2c15f0e12583a4519b599a16be994f`) with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1 on Python 3.12.10
- Human modifications: Codex-recommended candidate a of DW05I, cell [0, 0], generated on a transparent background (byte copy of raw). Mechanical export only (the report records no human modification and no --reink): alpha below 16 to 0 and 240 and above to 255, objects found as 8-connected components (specks under 64 px dropped), measured dot pitch 14.487 px, every opaque pixel snapped to the fixed Doll palette by OKLab nearest (mean deltaE 3.06, p95 6.63, max 17.94), integer reduction by k = 2 to 30x31 logical texels (fills 96.9 % of the 32x32 limit; the one-dot outline rule leaves ink on 98.5 % of the outer boundary), stored at 2x as 60x62 px. No hand painting or repaint.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and offline contact-sheet review 2026-10-02; owner approval of all 21 delivered picks 2026-10-03; in-game acceptance not_run
- Prompt or brief location: section DW05I of `asset-deliveries/doll-weapons/2026-10-02/BRIEF.md` and the `prompt` of `alpha/DW05I_a.png` in `asset-deliveries/doll-weapons/2026-10-02/manifest.json`; delivery files stay outside the repository
- SHA256: `cad624734c4c1e848779fa3a65c242966414a4453e770a42040430479b8bee34`

### Doll weapon audio foundation and companion summon — 2026-10-02

The shared audio basis of the Doll reward weapon refresh and the companion's new summon cue. [`tools/generate_doll_weapon_sfx.py`](../tools/generate_doll_weapon_sfx.py) owns the windows, filters, pitches, gains, loudness targets and source hashes; [`tools/doll_sfx_dsp.py`](../tools/doll_sfx_dsp.py) owns the original synthesis (music-box comb tooth on the F minor pentatonic ladder, brass ratchet, porcelain ring, additive flue organ, shimmer, low thump); both reuse the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) and [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) unmodified. The two Kenney recordings are the CC0 1.0 files already recorded in the Ebon Manor reward audio table of this register; they stay in the local store, are SHA-256 verified before use and are not committed. The project-owned `DollSummon.wav` (recorded in this register as doll-theater-0253-dollsummon; current bytes from the 0.3.7 weapon articulation revision) is layered thinly at the start so the companion keeps its arrival character. Loudness follows the Ebon scale: BS.1770 K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after the Vorbis round trip. The audition page and report stay in the git-ignored `.local`.

| Key | Store or repository file | Source | Source SHA256 |
|---|---|---|---|
| doll_summon | Assets/Sounds/Weapons/DollTheater/DollSummon.wav | Convergence project asset doll-theater-0253-dollsummon (0.3.7 articulation revision) | `f4be1a0733e06ad0262d6703a44fc00a0a01dd9a17a3fb1a3381831d634e8534` |
| metal_click | sfx-sources/cc0/pack-OGA-Kenney-RPGsounds.zip!OGG/metalClick.ogg | Kenney RPG Audio metalClick.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0) | `9851a69d0c613e13bceef08060ecc4148f098ef487927cbebe270d642398a3b3` |
| metal_latch | sfx-sources/cc0/pack-OGA-Kenney-RPGsounds.zip!OGG/metalLatch.ogg | Kenney RPG Audio metalLatch.ogg (https://opengameart.org/content/50-rpg-sound-effects, CC0 1.0) | `ba9ba60b172b3ebc131a940f25793cd2e207aca7af73dc80d637277f060f1708` |

- Runtime file: `Assets/Sounds/Weapons/DollWeapons/CompanionSummon.ogg`
- Asset ID: doll-weapon-sfx-companionsummon-20261002
- Asset type: stereo 44.1 kHz Vorbis Doll weapon cue (1.25 s)
- Creator: recordings by Kenney; project master DollSummon by Convergence; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: doll_summon, metal_click, metal_latch in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_doll_weapon_sfx.py` with `tools/doll_sfx_dsp.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -10.5 LUFS (played at volume 0.9: -11.4 LUFS effective), true peak -5.4 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings and project-owned masters; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02 (deterministic regeneration, length, loudness and true-peak checks); owner, 2026-10-03 (approved on the A/B audition page over the Phase I and Phase III music); in-game mix not_run
- SHA256: `f821d725a0991b879337b1d17b414c0bfe247eece20b9ca50a8824b3733d0b66`

### Scarlet Invocation reward pixel art — 2026-10-02

Original pixel art for the Scarlet Score Reliquary, the five Scarlet reward weapons and the Scarlet Covenant, generated by Codex with its built-in image generation at the owner's request from Claude's brief; no artist, work, game or franchise imitation was requested, and no third-party image was used as input (Claude's style references were viewed only; the only edit inputs were Codex's own SR01_a, SR02_c and SR04_c). Delivery originals, the brief and the manifest with the exact prompts stay outside the repository. Codex marked every candidate `accepted_by_codex=false`: none meets the strict 8 px grid with clean edges, and several have recorded size, layout or shape shortfalls (each record's Reviewer line lists them); its recommended candidates are used. [`tools/export_scarlet_reward_art.py`](../tools/export_scarlet_reward_art.py) owns the hash-pinned inputs, the mechanical export (haze cut, object cut, measured dot pitch, majority-colour resample with no dither at each object's best-agreeing lattice phase, fitting to the brief's limits, the reliquary parts' shared lattice) and the measured anchors; selections are recorded in the [rewards spec](../docs/encounters/crimson-foundry/REWARDS.md#art-and-audio).

- Runtime file: `Assets/Textures/Items/ScarletRewards/CrimsonScoreReliquary.png`
- Asset ID: scarlet-reward-art-crimsonscorereliquary-20261002
- Asset type: 64x50 Scarlet Score Reliquary item icon (32x25 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR01_a top-left (complete casket). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.82 px, 50x39 logical at that pitch exceeds the brief's 32x28, so it was fitted to 32x25 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+7, +10) source px, the one whose cells disagree with the fewest of its px, doubled. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR01_a.png` SHA256 `e3ab89dcce56c8dbc1d20b368422d904b5157c89df1674c231810b89c21b9217`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the complete casket is larger than 32x28 (fitted here), the body cell's margin is under 10%, and its own parts do not reassemble into the casket (SR01P_c's are used)); Claude export, contact-sheet and real-size mock-up review 2026-10-03 (at 32x25 the seal's three bars do not survive, so it reads as a plain round seal); owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `73d0b524f5846b1d006731971bac84aea3ac7a8bad3a48765a864b4a08ce94fb`

- Runtime file: `Assets/Textures/Items/ScarletRewards/ReliquaryBody.png`
- Asset ID: scarlet-reward-art-reliquarybody-20261002
- Asset type: 32x18 reliquary body for the opening show, one texel per logical pixel (drawn at 2x with point sampling)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR01P_c top-left (body). Codex made it by editing its own SR01_a (no other input). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.44 px, resampled with the other reliquary part on one lattice of 10.562 source px per logical pixel (the closed casket as wide as the icon) by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+9, +6) source px, the one whose cells disagree with the fewest of its px (searched for both parts together). No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR01P_c.png` SHA256 `a3624df04da21eca6ec167a8a243e16123181b6778b9275cb89c3e3c411fd1ce`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the parts are not at SR01_a's scale and their reassembly was not checked by Codex (the exporter places them)); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `c364374bc9fd5dc3b8aaf1f60cf1d3327e78243ebc1839a8748e3ba5ce3e37b7`

- Runtime file: `Assets/Textures/Items/ScarletRewards/ReliquaryLid.png`
- Asset ID: scarlet-reward-art-reliquarylid-20261002
- Asset type: 30x13 reliquary lid for the opening show, one texel per logical pixel (drawn at 2x with point sampling)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR01P_c top-right (lid). Codex made it by editing its own SR01_a (no other input). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.44 px, resampled with the other reliquary part on one lattice of 10.562 source px per logical pixel (the closed casket as wide as the icon) by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+9, +6) source px, the one whose cells disagree with the fewest of its px (searched for both parts together). No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR01P_c.png` SHA256 `a3624df04da21eca6ec167a8a243e16123181b6778b9275cb89c3e3c411fd1ce`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the parts are not at SR01_a's scale and their reassembly was not checked by Codex (the exporter places them)); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `aac062deeecaa8c198a5d8c200dbfde6d9c4a08f9c5770a8c50476a9e9ec0bb5`

- Runtime file: `Assets/Textures/Items/ScarletRewards/ReliquarySeal.png`
- Asset ID: scarlet-reward-art-reliquaryseal-20261002
- Asset type: 11x11 reliquary wax seal for the opening show, one texel per logical pixel (drawn at 2x with point sampling)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR01P_c bottom-left (wax seal). Codex made it by editing its own SR01_a (no other input). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.44 px, resampled to 11x11 cells, one more than the reliquary lattice's 10 (the fewest that keep its three bars apart), by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+0, +4) source px, the one whose cells disagree with the fewest of its px. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR01P_c.png` SHA256 `a3624df04da21eca6ec167a8a243e16123181b6778b9275cb89c3e3c411fd1ce`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the parts are not at SR01_a's scale and their reassembly was not checked by Codex (the exporter places them)); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `7152a3dd5bea146132bf85e96c81b3807fac5a1ebddd576a2c49dedb3b8cc83b`

- Runtime file: `Assets/Textures/Items/ScarletRewards/SableScythe.png`
- Asset ID: scarlet-reward-art-sablescythe-20261002
- Asset type: 64x56 held Sable Scythe, one texel per logical pixel (drawn at 2x with point sampling)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR02_c (held). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 5.84 px, 84x73 logical at that pitch exceeds the brief's 64x64, so it was fitted to 64x56 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+7, +0) source px, the one whose cells disagree with the fewest of its px. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR02_c.png` SHA256 `41a498cf43a5a37eddb19c651854c898d2c3a4bb11830db278148d6304803299`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `df8b7de4c301f076778ae3f717b914e36501d57f7f9222ddfebfe2ab59b12555`

- Runtime file: `Assets/Textures/Items/ScarletRewards/CrimsonSableScythe.png`
- Asset ID: scarlet-reward-art-crimsonsablescythe-20261002
- Asset type: 64x56 Sable Scythe item icon (32x28 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR02I_c. Codex made it by editing its own SR02_c (no other input). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 8.92 px, resampled to round(bbox / pitch) = 32x28 logical cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+7, +1) source px, the one whose cells disagree with the fewest of its px, doubled. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR02I_c.png` SHA256 `53908b6ba7db2e3361a399d556aa9ff995d7365d3d6eb6cd14f0aeda877cb79e`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; 3.75 logical px wider than 32 at 8 px (32x28 at its measured pitch)); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `ff719569a2b5116b8c8704a90e7fe692df8bcdf7fad65a976212af69f913bf60`

- Runtime file: `Assets/Textures/Items/ScarletRewards/CanticleOrgan.png`
- Asset ID: scarlet-reward-art-canticleorgan-20261002
- Asset type: 43x22 held Canticle Organ, one texel per logical pixel (drawn at 2x with point sampling)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR03_d top-left (held, facing right). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 5.76 px, 80x41 logical at that pitch exceeds the brief's 44x22, so it was fitted to 43x22 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+10, +9) source px, the one whose cells disagree with the fewest of its px. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR03_d.png` SHA256 `dffc1e82f7de5baf01f0d9be90d9e1fdf623d32e748989b0c985b015836c6aeb`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the gun, hand and icon are larger than their limits (fitted here), and the hand is turned three-quarter front rather than strictly side-on); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `e1d00f1d3c1d3d0e987386baab520ee6d1f854ee114f1b9f7d54b83597cd6893`

- Runtime file: `Assets/Textures/Items/ScarletRewards/CanticleShard.png`
- Asset ID: scarlet-reward-art-canticleshard-20261002
- Asset type: 11x6 Canticle Organ bone shard, one texel per logical pixel (drawn at 2x with point sampling)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR03_d top-right (bone shard). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 5.76 px, 25x13 logical at that pitch exceeds the brief's 12x6, so it was fitted to 11x6 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+6, +9) source px, the one whose cells disagree with the fewest of its px. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR03_d.png` SHA256 `dffc1e82f7de5baf01f0d9be90d9e1fdf623d32e748989b0c985b015836c6aeb`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the gun, hand and icon are larger than their limits (fitted here), and the hand is turned three-quarter front rather than strictly side-on); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `25570656ea33924117af8e1c1781cbcd7584fe3722dd8568d2b89f7b6226f3f2`

- Runtime file: `Assets/Textures/Items/ScarletRewards/BoneHand.png`
- Asset ID: scarlet-reward-art-bonehand-20261002
- Asset type: 20x32 Hymn of Hands bone hand, one texel per logical pixel (drawn at 2x with point sampling)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR03_d bottom-left (bone hand, palm down). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 5.76 px, 36x56 logical at that pitch exceeds the brief's 24x32, so it was fitted to 20x32 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+0, +1) source px, the one whose cells disagree with the fewest of its px. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR03_d.png` SHA256 `dffc1e82f7de5baf01f0d9be90d9e1fdf623d32e748989b0c985b015836c6aeb`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the gun, hand and icon are larger than their limits (fitted here), and the hand is turned three-quarter front rather than strictly side-on); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `8902021a4071ed1888693f13502f751447020ef942c2e40f6b651a6c1f1286cf`

- Runtime file: `Assets/Textures/Items/ScarletRewards/CrimsonCanticleOrgan.png`
- Asset ID: scarlet-reward-art-crimsoncanticleorgan-20261002
- Asset type: 54x56 Canticle Organ item icon (27x28 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR03_d bottom-right (icon). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 5.76 px, 39x40 logical at that pitch exceeds the brief's 28x28, so it was fitted to 27x28 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+6, +3) source px, the one whose cells disagree with the fewest of its px, doubled. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR03_d.png` SHA256 `dffc1e82f7de5baf01f0d9be90d9e1fdf623d32e748989b0c985b015836c6aeb`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the gun, hand and icon are larger than their limits (fitted here), and the hand is turned three-quarter front rather than strictly side-on); Claude export, contact-sheet and real-size mock-up review 2026-10-03 (at 27x28 the icon's four diagonal barrels and rib frame merge into one mottled bone mass at every lattice phase); owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `e0cea4bfa9cbcf5f21a65c48fc9600865869bd8a2abbd7e724c9c7e6fd896a60`

- Runtime file: `Assets/Textures/Items/ScarletRewards/ScarletBaton.png`
- Asset ID: scarlet-reward-art-scarletbaton-20261002
- Asset type: 37x38 held Scarlet Baton, one texel per logical pixel (drawn at 2x with point sampling)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR04_c (held). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.82 px, resampled to round(bbox / pitch) = 37x38 logical cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+5, +2) source px, the one whose cells disagree with the fewest of its px. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR04_c.png` SHA256 `89994551b9db0103d1b4b718ab5e15a8c56219dc6cf023398990f56fdd644966`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `015fb02607e9c02edaa0bfb0a5d51a0e04c2bf2783d5c82efbb10e4c619666f7`

- Runtime file: `Assets/Textures/Items/ScarletRewards/CrimsonBaton.png`
- Asset ID: scarlet-reward-art-crimsonbaton-20261002
- Asset type: 52x52 Scarlet Baton item icon (26x26 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR04I_b. Codex made it by editing its own SR04_c (no other input). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.64 px, resampled to round(bbox / pitch) = 26x26 logical cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+3, +1) source px, the one whose cells disagree with the fewest of its px, doubled. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR04I_b.png` SHA256 `e537bedf532fed04af61cfadaa6e260d9dde36d103d37ab1865bfd466f6fc55a`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `a28a02374b97b11a34b6409b5f2560ae1e266e9223a5fac1c36da119db90c251`

- Runtime file: `Assets/Textures/Items/ScarletRewards/CrimsonEmberCenser.png`
- Asset ID: scarlet-reward-art-crimsonembercenser-20261002
- Asset type: 36x64 Ember Censer item icon (18x32 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR05_d left (icon). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 9.16 px, 23x39 logical at that pitch exceeds the brief's 24x32, so it was fitted to 18x32 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+7, +8) source px, the one whose cells disagree with the fewest of its px, doubled. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR05_d.png` SHA256 `c9c9cd000579613532a262227dd5fb47883870620c968682ae3274b75952e775`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the icon, censer and buff are larger than their limits (fitted here), and the icon shows 3 of the briefed 5 thorns); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `a52bc8f2e4b72c670d907639dd03aa4ec2b2f6536a0898f5ad95416f2573af9d`

- Runtime file: `Assets/Textures/Items/ScarletRewards/EmberCenser.png`
- Asset ID: scarlet-reward-art-embercenser-20261002
- Asset type: 30x36 Ember Censer minion (unlit crown censer), one texel per logical pixel (drawn at 2x with point sampling)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR05_d middle (unlit crown censer). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 9.16 px, 37x45 logical at that pitch exceeds the brief's 32x36, so it was fitted to 30x36 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+7, +11) source px, the one whose cells disagree with the fewest of its px. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR05_d.png` SHA256 `c9c9cd000579613532a262227dd5fb47883870620c968682ae3274b75952e775`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the icon, censer and buff are larger than their limits (fitted here), and the icon shows 3 of the briefed 5 thorns); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `20bdc48760ef769bf3f52778c2ffccd4e5e620547f625974826d9c575528238c`

- Runtime file: `Assets/Textures/Items/ScarletRewards/CrimsonEmberCenserBuff.png`
- Asset ID: scarlet-reward-art-crimsonembercenserbuff-20261002
- Asset type: 32x32 Ember Censer buff icon (14x13 logical centred on 16x16, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR05_d right (buff). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 9.16 px, resampled to round(bbox / pitch) = 14x13 logical cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+9, +9) source px, the one whose cells disagree with the fewest of its px, centred on 16x16 and doubled. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR05_d.png` SHA256 `c9c9cd000579613532a262227dd5fb47883870620c968682ae3274b75952e775`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the icon, censer and buff are larger than their limits (fitted here), and the icon shows 3 of the briefed 5 thorns); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `168bcb122cb6c3f10f352df0bf53ae03f294758566ee57841cb5bbf62f7f61c9`

- Runtime file: `Assets/Textures/Items/ScarletRewards/BloodinkQuill.png`
- Asset ID: scarlet-reward-art-bloodinkquill-20261002
- Asset type: 25x7 thrown Bloodink Quill, one texel per logical pixel (drawn at 2x with point sampling)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR06_d left (quill, nib right). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 6.74 px, 51x13 logical at that pitch exceeds the brief's 28x8, so it was fitted to 28x7 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+4, +9) source px, the one whose cells disagree with the fewest of its px, the 1 texel of the ink bead drawn detached ahead of the nib was cut off (largest piece kept). No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR06_d.png` SHA256 `1dd111845c7e49c8d03fca5d40dbcf3eba7233d4af667bb661d3f41ae1815c71`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the quill, bottle and score are larger than their limits (fitted here), an ink bead is drawn detached ahead of the nib (cut here), and the score's marks are more prominent than the briefed tiny faint ones); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `cbbf17d0558c674a2b12667fdf19b55700b0e7174f65e562a610c1d3de6a786d`

- Runtime file: `Assets/Textures/Items/ScarletRewards/CrimsonBloodinkQuill.png`
- Asset ID: scarlet-reward-art-crimsonbloodinkquill-20261002
- Asset type: 40x64 Bloodink Quill item icon (20x32 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR06_d middle (icon). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 6.74 px, 33x53 logical at that pitch exceeds the brief's 28x32, so it was fitted to 20x32 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+3, +8) source px, the one whose cells disagree with the fewest of its px, doubled. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR06_d.png` SHA256 `1dd111845c7e49c8d03fca5d40dbcf3eba7233d4af667bb661d3f41ae1815c71`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the quill, bottle and score are larger than their limits (fitted here), an ink bead is drawn detached ahead of the nib (cut here), and the score's marks are more prominent than the briefed tiny faint ones); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `bc592358f418148d27a467d6feff2406e2c2836a412adf3de8cd907236662af1`

- Runtime file: `Assets/Textures/Items/ScarletRewards/SealedScore.png`
- Asset ID: scarlet-reward-art-sealedscore-20261002
- Asset type: 24x10 Sealed Score stealth projectile, one texel per logical pixel (drawn at 2x with point sampling)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR06_d right (rolled score). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 6.74 px, 42x20 logical at that pitch exceeds the brief's 24x12, so it was fitted to 24x11 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+7, +8) source px, the one whose cells disagree with the fewest of its px, 24x10 after the empty edge cells. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR06_d.png` SHA256 `1dd111845c7e49c8d03fca5d40dbcf3eba7233d4af667bb661d3f41ae1815c71`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the quill, bottle and score are larger than their limits (fitted here), an ink bead is drawn detached ahead of the nib (cut here), and the score's marks are more prominent than the briefed tiny faint ones); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `68da28d0d403b1b54a07edde13182d3cc4f90e5e178d4e86b1228f4c965199d4`

- Runtime file: `Assets/Textures/Items/ScarletRewards/CrimsonPact.png`
- Asset ID: scarlet-reward-art-crimsonpact-20261002
- Asset type: 58x64 Scarlet Covenant item icon (29x32 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR07_b left (icon). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 10.84 px, 35x38 logical at that pitch exceeds the brief's 32x32, so it was fitted to 29x32 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+2, +6) source px, the one whose cells disagree with the fewest of its px, doubled. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR07_b.png` SHA256 `2a18e8a8f0fd3989c4e6e09ba9d107b4dc86b450f41dfeec27a1e67c822ae14d`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the letter and buff are larger than their limits (fitted here)); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `2a97fc20e2285de807605dbeea74c9a3eee2f1df489963304e30547007cd8a9c`

- Runtime file: `Assets/Textures/Items/ScarletRewards/CrimsonPactBuff.png`
- Asset ID: scarlet-reward-art-crimsonpactbuff-20261002
- Asset type: 32x32 Scarlet Covenant buff icon (14x14 logical centred on 16x16, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Scarlet reward brief at the owner's request; Claude's style references were viewed only, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen), exact model not exposed; `tools/export_scarlet_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.17.1
- Human modifications: Codex-recommended candidate SR07_b right (buff). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 10.84 px, 15x15 logical at that pitch exceeds the brief's 14x14, so it was fitted to 14x14 cells by majority colour (48-colour median cut of the object, opaque at half coverage, no dither), at the lattice phase (+4, +0) source px, the one whose cells disagree with the fewest of its px, centred on 16x16 and doubled. No repaint or recolour. Source `asset-deliveries/scarlet-rewards/2026-10-02/alpha/SR07_b.png` SHA256 `2a18e8a8f0fd3989c4e6e09ba9d107b4dc86b450f41dfeec27a1e67c822ae14d`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02 (accepted_by_codex=false: the strict 8 px pixel grid and clean edges are not met, a semi-transparent fringe and specks remaining; the letter and buff are larger than their limits (fitted here)); Claude export, contact-sheet and real-size mock-up review 2026-10-03; owner selection and in-game acceptance not_run
- Prompt or brief location: `asset-deliveries/scarlet-rewards/2026-10-02/BRIEF.md` and its manifest.json (exact prompts), kept outside the repository; selections in `docs/encounters/crimson-foundry/REWARDS.md#art-and-audio`
- SHA256: `65fdf398af70bb2673819ac7d89399d0a673060103cf80cf50c9e8f7e73f4ba4`

### Waltz of the Ebon Manor reward pixel art — 2026-10-02

Original pixel art for the Ebon Hatbox, the five reward weapons and The Last Waltz, generated by Codex from Claude's owner-directed brief; no artist, work or franchise imitation was requested and no third-party image was used as input. Selections and prompt summary: [asset brief](../docs/encounters/ebon-manor/ASSET_BRIEF.md#reward-set-2026-10-02). Delivery originals stay outside the repository. Codex marked every candidate `accepted_by_codex=false` only for the strict 8 px grid and size limits; [`tools/export_ebon_reward_art.py`](../tools/export_ebon_reward_art.py) owns the mechanical export, the measured dot pitches and the anchors.

- Runtime file: `Assets/Textures/Items/EbonRewards/EbonHatbox.png`
- Asset ID: ebon-reward-art-ebonhatbox-20261002
- Asset type: 64x60 Ebon Hatbox item icon (32x30 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, top-left cell (complete box). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.72 px. 38x36 logical at that pitch exceeds the 32x32 icon limit, so it was fitted to 32x30 by majority colour (48-colour median cut, opaque at half coverage), then doubled. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER01_d.png` SHA256 `e001b0893322a59ead636ac4b3768d784eea758e3ce20a65a8056a83c3b9eff4`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `28195e3ccaed063f53e2a627d0e9a9e942cb1ebda9433edb4b29170fd1897126`

- Runtime file: `Assets/Textures/Items/EbonRewards/HatboxBody.png`
- Asset ID: ebon-reward-art-hatboxbody-20261002
- Asset type: 33x26 hatbox body for the opening show, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, top-right cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.72 px. Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER01_d.png` SHA256 `e001b0893322a59ead636ac4b3768d784eea758e3ce20a65a8056a83c3b9eff4`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `2ca7c56b20004b5d918b363e8206b9208bd6967852db38e4a762fb17beeb0727`

- Runtime file: `Assets/Textures/Items/EbonRewards/HatboxLid.png`
- Asset ID: ebon-reward-art-hatboxlid-20261002
- Asset type: 38x19 hatbox lid for the opening show, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, bottom-left cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.72 px. Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER01_d.png` SHA256 `e001b0893322a59ead636ac4b3768d784eea758e3ce20a65a8056a83c3b9eff4`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `5723d9134fedb3718148742f61d2da289f3138b561c75082433cf2bd237fad5e`

- Runtime file: `Assets/Textures/Items/EbonRewards/HatboxBow.png`
- Asset ID: ebon-reward-art-hatboxbow-20261002
- Asset type: 23x19 hatbox ribbon bow for the opening show, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, bottom-right cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.72 px. Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER01_d.png` SHA256 `e001b0893322a59ead636ac4b3768d784eea758e3ce20a65a8056a83c3b9eff4`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `7d0f4fa9c4a74acb7dfce33bc165658ca517ced9f6adb14760bb80f9c0560fdc`

- Runtime file: `Assets/Textures/Items/EbonRewards/ShearUpper.png`
- Asset ID: ebon-reward-art-shearupper-20261002
- Asset type: 131x25 Moonshear upper blade half, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate c, top half. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.22 px. Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER02_c.png` SHA256 `313653eda52f7e87023bdfd3b670c3b01dcd8e5c262a1915c483e6dd62490475`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `77d4d76eea48e497fae71d0872ffd44467d67be430b4fce0750b21774ce7d6ca`

- Runtime file: `Assets/Textures/Items/EbonRewards/ShearLower.png`
- Asset ID: ebon-reward-art-shearlower-20261002
- Asset type: 132x29 Moonshear lower blade half, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate c, bottom half. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.22 px. Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER02_c.png` SHA256 `313653eda52f7e87023bdfd3b670c3b01dcd8e5c262a1915c483e6dd62490475`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `7b07800519a966891076794600aa579b1a24cd0ac8fb27c7279034994f53775a`

- Runtime file: `Assets/Textures/Items/EbonRewards/EbonMoonshear.png`
- Asset ID: ebon-reward-art-ebonmoonshear-20261002
- Asset type: 64x62 Moonshear item icon (32x31 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate c, a built-in edit of Codex's own ER02 c. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.16 px. 34x33 logical at that pitch exceeds the 32x32 icon limit, so it was fitted to 32x31 by majority colour (48-colour median cut, opaque at half coverage), then doubled. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER02I_c.png` SHA256 `ae0d79e95d7f83fd2646b1d09f4e55aeb6c4930aebb2faa0ac0c20b43b3602b8`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `10eac59fe0cbaf10f42b226ff098eaaa1dbd14e1e85c5c3b8798b373c75b6822`

- Runtime file: `Assets/Textures/Items/EbonRewards/LoomHarp.png`
- Asset ID: ebon-reward-art-loomharp-20261002
- Asset type: 18x56 held lyre bow without strings, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, left cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 3.96 px, 2 dots per logical pixel (7.92 px). Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER03_d.png` SHA256 `7c8331de885451ea236577a9554a79ebd893b462c1116b16eb3b725fcb43b160`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `70f45989aac4244cb00cb898cddd185c9f2d970a6457db9bf1daea15630167d0`

- Runtime file: `Assets/Textures/Items/EbonRewards/NeedleArrow.png`
- Asset ID: ebon-reward-art-needlearrow-20261002
- Asset type: 35x8 needle arrow, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, middle cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 3.96 px, 2 dots per logical pixel (7.92 px). Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER03_d.png` SHA256 `7c8331de885451ea236577a9554a79ebd893b462c1116b16eb3b725fcb43b160`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `3027067abdb6b1141449766aea5e02ba39a69b6cc98843a7c40c08ab2b173e75`

- Runtime file: `Assets/Textures/Items/EbonRewards/EbonLoomHarp.png`
- Asset ID: ebon-reward-art-ebonloomharp-20261002
- Asset type: 24x60 Moonloom Harp item icon (12x30 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, right cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 3.96 px, 2 dots per logical pixel (7.92 px). Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage), then doubled. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER03_d.png` SHA256 `7c8331de885451ea236577a9554a79ebd893b462c1116b16eb3b725fcb43b160`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `d083877bb975a7cd57d45c7628c9fc8cc3b3057725e72e88fb6d62377d415d0c`

- Runtime file: `Assets/Textures/Items/EbonRewards/EbonThimble.png`
- Asset ID: ebon-reward-art-ebonthimble-20261002
- Asset type: 54x64 Ebon Thimble item icon (27x32 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate a. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 5.72 px. 30x36 logical at that pitch exceeds the 32x32 icon limit, so it was fitted to 27x32 by majority colour (48-colour median cut, opaque at half coverage), then doubled. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER04_a.png` SHA256 `8a2bbc42f540c8b88f1705a977ae9dc56277f0d78d2523bac338809fa40a00df`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `2d0d729ea3d22df5dac9f0f4d106b8e23ba4413b46254f48a993ff6cac2a1014`

- Runtime file: `Assets/Textures/Items/EbonRewards/Furniture.png`
- Asset ID: ebon-reward-art-furniture-20261002
- Asset type: 384x64 atlas of eight thrown props (armchair, candelabra, portrait, clock, birdcage, mirror, music box, cello) in 48x64 cells, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, all eight cells. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 4.36 px, 2 dots per logical pixel (8.72 px). Each prop resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage) and centred in its cell; no prop needed fitting. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER05_d.png` SHA256 `3e1fb1c166511f8bd17c1c380d8a114ebbe662185444072efbd8e8eeae8ecba6`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `7e7980f38174b3fc19b3cdc31f31d69c2a326420181ad1941dfe794051ed195a`

- Runtime file: `Assets/Textures/Items/EbonRewards/Piano.png`
- Asset ID: ebon-reward-art-piano-20261002
- Asset type: 76x57 falling grand piano, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, a built-in edit using Codex's own ER05 d as the dot-size reference. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 4.08 px, 2 dots per logical pixel (8.16 px). Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER06_d.png` SHA256 `164586769ae0645e2f25d4bff12c4fed0a36b8cd8f3eedebfa68446226181809`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `07857e1632489d3d02ff47bb0e30c41dc4f6e193464341770e6e9884760f60dc`

- Runtime file: `Assets/Textures/Items/EbonRewards/Chandelier.png`
- Asset ID: ebon-reward-art-chandelier-20261002
- Asset type: 41x68 unlit two-tier chandelier summon, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, top-left cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 6.14 px. Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER07_d.png` SHA256 `b6b2b9f19a5a6009cb08a977a2ae17fd2a14bab40087a842e8c7fc622cc6722c`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `d3dc14e15d96dd9806cc571f18c7ef2790329a10749bea2cca0fa3a46a4904b8`

- Runtime file: `Assets/Textures/Items/EbonRewards/ChandelierSmall.png`
- Asset ID: ebon-reward-art-chandeliersmall-20261002
- Asset type: 34x49 unlit one-tier chandelier summon, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, top-middle cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 6.14 px. Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER07_d.png` SHA256 `b6b2b9f19a5a6009cb08a977a2ae17fd2a14bab40087a842e8c7fc622cc6722c`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `6dc29981165c8e930ce6cef284d47b4b0b71838517fa0a802bc40851e6024de5`

- Runtime file: `Assets/Textures/Items/EbonRewards/EbonChandelierPole.png`
- Asset ID: ebon-reward-art-ebonchandelierpole-20261002
- Asset type: 62x64 Ballroom Chandelier item icon (31x32 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, top-right cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 6.14 px. 59x61 logical at that pitch exceeds the 32x32 icon limit, so it was fitted to 31x32 by majority colour (48-colour median cut, opaque at half coverage), then doubled. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER07_d.png` SHA256 `b6b2b9f19a5a6009cb08a977a2ae17fd2a14bab40087a842e8c7fc622cc6722c`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `042874aa489657990098492d841dceb2ae19dae4b1cfd1026615009322592f6a`

- Runtime file: `Assets/Textures/Items/EbonRewards/ChandelierFlame.png`
- Asset ID: ebon-reward-art-chandelierflame-20261002
- Asset type: 16x14 two-frame candle flame, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, bottom-left cell (both flames). Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 6.14 px. Each flame resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage), bottom-aligned in two equal 8x14 frames. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER07_d.png` SHA256 `b6b2b9f19a5a6009cb08a977a2ae17fd2a14bab40087a842e8c7fc622cc6722c`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `144ad5ea78fcff5cf8d2b2d20e1b937359d26b482920e63b884a1535b368fb00`

- Runtime file: `Assets/Textures/Items/EbonRewards/ChandelierCrystal.png`
- Asset ID: ebon-reward-art-chandeliercrystal-20261002
- Asset type: 12x26 chandelier crystal drop, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, bottom-middle cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 6.14 px. Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER07_d.png` SHA256 `b6b2b9f19a5a6009cb08a977a2ae17fd2a14bab40087a842e8c7fc622cc6722c`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `2ba9c402899b06ed2d616cd40d97a532bbd161e117f45bd10137253228921f3c`

- Runtime file: `Assets/Textures/Items/EbonRewards/EbonChandelierBuff.png`
- Asset ID: ebon-reward-art-ebonchandelierbuff-20261002
- Asset type: 32x32 chandelier summon buff icon (15x16 logical centred in 16x16, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate d, bottom-right cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 6.14 px. 24x25 logical at that pitch exceeds the 16x16 buff limit, so it was fitted to 15x16 by majority colour (48-colour median cut, opaque at half coverage), then doubled. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER07_d.png` SHA256 `b6b2b9f19a5a6009cb08a977a2ae17fd2a14bab40087a842e8c7fc622cc6722c`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `e2dc373c585266f1d82ef2542c0f51b0c309d281fe4d5230d3f7d6a6c2c75be4`

- Runtime file: `Assets/Textures/Items/EbonRewards/Spool.png`
- Asset ID: ebon-reward-art-spool-20261002
- Asset type: 11x18 thrown silk spool, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate b, first cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 3.94 px, 2 dots per logical pixel (7.88 px). Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage). No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER08_b.png` SHA256 `1849a66cbac617791b3543843fabf388da106803121474732d6de2a693c47035`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `43752c4a5ddce16aac8646edfbda9445c7f69988ba24406db4fc921b19c2f141`

- Runtime file: `Assets/Textures/Items/EbonRewards/EbonSeveringSilk.png`
- Asset ID: ebon-reward-art-ebonseveringsilk-20261002
- Asset type: 64x48 Severing Silk item icon (32x24 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate b, second cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 3.94 px, 2 dots per logical pixel (7.88 px). 36x27 logical at that pitch exceeds the 32x32 icon limit, so it was fitted to 32x24 by majority colour (48-colour median cut, opaque at half coverage), then doubled. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER08_b.png` SHA256 `1849a66cbac617791b3543843fabf388da106803121474732d6de2a693c47035`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `250bcce7234e3076b0f76d0ff30c91baf351b8aadedef17987527fd76411ce31`

- Runtime file: `Assets/Textures/Items/EbonRewards/ScissorsClosed.png`
- Asset ID: ebon-reward-art-scissorsclosed-20261002
- Asset type: 35x18 closed bird scissors, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate b, third cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 3.94 px, 2 dots per logical pixel (7.88 px). Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage); both poses share one canvas registered on the finger loops. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER08_b.png` SHA256 `1849a66cbac617791b3543843fabf388da106803121474732d6de2a693c47035`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `2846a3bad9a802b3b6267c118c6d67e5fce2d27707abdc965b1cb069879f2422`

- Runtime file: `Assets/Textures/Items/EbonRewards/ScissorsOpen.png`
- Asset ID: ebon-reward-art-scissorsopen-20261002
- Asset type: 35x18 open bird scissors, one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate b, fourth cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 3.94 px, 2 dots per logical pixel (7.88 px). Resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage); both poses share one canvas registered on the finger loops. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER08_b.png` SHA256 `1849a66cbac617791b3543843fabf388da106803121474732d6de2a693c47035`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `72b5e5f533ef77e9b1e8c15f14c52c372072a544eb2fc1fc947613d1a1570b3f`

- Runtime file: `Assets/Textures/Items/EbonRewards/EbonLastWaltz.png`
- Asset ID: ebon-reward-art-ebonlastwaltz-20261002
- Asset type: 56x64 The Last Waltz item icon (28x32 logical, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate c, left cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.26 px. 29x34 logical at that pitch exceeds the 32x32 icon limit, so it was fitted to 28x32 by majority colour (48-colour median cut, opaque at half coverage), then doubled. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER09_c.png` SHA256 `83f16ba39a186a5a69b0a77580681f09e0c2c3e2f05cac2d975fa1078a094856`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `23bd5d7d9b4069cece71f0383269565513396b4796a5b7386b7fbcd87e34e63e`

- Runtime file: `Assets/Textures/Items/EbonRewards/EbonLastWaltzBuff.png`
- Asset ID: ebon-reward-art-ebonlastwaltzbuff-20261002
- Asset type: 32x32 The Last Waltz buff icon (16x16 logical centred in 16x16, each logical pixel 2x2)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate c, right cell. Generated on a transparent background; the alpha file is a byte copy of raw. Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 7.26 px. 23x24 logical at that pitch exceeds the 16x16 buff limit, so it was fitted to 16x16 by majority colour (48-colour median cut, opaque at half coverage), then doubled. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER09_c.png` SHA256 `83f16ba39a186a5a69b0a77580681f09e0c2c3e2f05cac2d975fa1078a094856`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `011ac12eb29eb7c2d0211eb00fa478aa269183944b21d9880fb6ae46a72efd47`

- Runtime file: `Assets/Textures/Items/EbonRewards/NoiretteWaltz.png`
- Asset ID: ebon-reward-art-noirettewaltz-20261002
- Asset type: 192x64 four companion poses for Noirette (48x64 cells), one texel per logical pixel
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-02
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon reward brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation and background extraction (image_gen.imagegen), exact model not exposed; `tools/export_ebon_reward_art.py` with Pillow 12.1.1, NumPy 2.4.4 and SciPy 1.16.1
- Human modifications: Codex-recommended candidate a, top row, a built-in edit of the Raid's own EM01 b. Built-in background extraction by Codex, which Codex reported as not fully pixel-preserving (outline IoU 96.7-98.5%). Mechanical export: alpha below 16 to 0, object cut out alone, measured dot pitch 4.04 px, 2 dots per logical pixel (8.08 px). Each pose resampled to round(bbox / pitch) logical cells by majority colour (48-colour median cut, opaque at half coverage); cell-relative x kept (poses 1 and 2 moved 2 px left to stay inside 48 px), feet on row 61, nothing clipped. No repaint. Source `asset-deliveries/ebon-rewards/2026-10-02/alpha/ER10_a.png` SHA256 `7db6f9f20464a24cf85b446bfed803c4d19de33e013f7261052910b689cb6fc5`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex recommendation 2026-10-02; Claude export and contact-sheet review 2026-10-02; owner selection and in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `0d8a224e489cda20fa9fe5a6c747815a6586960d505577bc0fcf001d38be2d86`

### Waltz of the Ebon Manor reward pixel layer — 2026-10-02

The Ebon reward weapons share one original half-resolution pixel material ([reward spec](../docs/encounters/ebon-manor/REWARDS.md)); it follows the Soboro technique with its own palette and primitives.

- Runtime file: `Assets/AutoloadedEffects/Shaders/EbonPixel.fxc`
- Asset ID: ebonpixel-20261002
- Asset type: compiled original material
- Creator: project-owner-directed original work with Anthropic Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: original Convergence HLSL source alongside runtime export
- Tool/model/version: FXC fx_2_0 O3; compiler/source/export hashes in compiled.json
- Human modifications: Original repository-owned EbonPixel.fx for the Ebon reward pixel layer: swept crescents, tailor's-chalk and torn bands, and 1-3 dot threads, standing-wave strings and rings evaluated per art dot in a screen-aligned half-resolution target, plus a flat pass for CPU-plotted dot runs, quantized to the Ebon palette (navy outline, charcoal, dusty rose, silver, ivory, moon, gold, candle, internal debris wood); a composite pass point-upscales it with a one-dot navy outline and a small bounded glow (a separate glow-free composite pass serves Reduced Effects). References the existing SlashNoise.png at runtime; no Calamity or other third-party art/code/sample imported.
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance and generation disclosure
- Reviewer and review date: Claude offline compiled-material sheet/sequence review 2026-10-02; native playtest not_run

### Doll weapon pixel layer — 2026-10-02

The refreshed Doll reward weapons share one original half-resolution pixel material for their sprites and light; weapon-specific materials are recorded with their weapons.

- Runtime file: `Assets/AutoloadedEffects/Shaders/DollPixel.fxc`
- Asset ID: dollpixel-20261002
- Asset type: compiled original material
- Creator: project-owner-directed original work with Anthropic Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: original Convergence HLSL source alongside runtime export
- Tool/model/version: FXC fx_2_0 O3; compiler/source/export hashes in compiled.json
- Human modifications: Original repository-owned DollPixel.fx for the shared Doll weapon layer, evaluated per art dot in a world-aligned half-resolution target (1 dot = 2 world px) and quantized to the Doll palette (ink, black iron, porcelain, pearl, dull brass, the plum-to-white light ramp, ruby): point-sampled pixel sprites whose rotated ink rings stay closed, with flash, porcelain-crumble dissolve, row reveal, silhouette and dithered fade; dot-exact 1-3 dot lines, rings and arcs; pearl-violet forecast hairlines with travelling heads; a flat pass for CPU-plotted debris and dots; a built-in ramp/void energy material; composites that point-upscale the Art and Light targets with a one-dot ink outline around light and a bounded glow on lilac-and-brighter tones (a glow-free pass serves Reduced Effects). No texture, Calamity or other third-party art/code/sample imported.
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance and generation disclosure
- Reviewer and review date: Claude offline compiled-material review with `tools/preview-doll-weapons.ps1` (synthetic sprites, pixel checks) 2026-10-02; native playtest not_run

### Waltz of the Ebon Manor reward weapon audio — 2026-10-02

Twenty-seven weapon cues for the five Ebon Hatbox weapons, the hatbox and The Last Waltz ([rewards spec](../docs/encounters/ebon-manor/REWARDS.md#art-and-audio)). [`tools/generate_ebon_reward_sfx.py`](../tools/generate_ebon_reward_sfx.py) owns the windows, filters, pitches, gains, loudness targets and source hashes; it reuses the helpers of [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py). Each cue layers trimmed CC0 recordings with original synthesis (tuned Karplus-Strong silk strings in B minor, a struck-string piano, modal glass and chain partials, fabric rips, band-swept air, thread ratchets, a synthetic hall tail); Note0 to Note7 are pure synthesis. The recordings are the ones already attributed above in the Ebon Manor and Soboro tables (Kenney RPG Audio and the artisticdude Swishes pack on OpenGameArt; Freesound uploads that showed Creative Commons 0 on their pages on 2026-10-01, public HQ preview renders). They stay in the local store, are SHA-256 verified before use and are not committed; the audition WAVs and report stay in the git-ignored `.local`. Loudness follows the Ebon scale: BS.1770 K-weighted maximum 400 ms short-term LUFS, true peak at most -1 dBTP after the Vorbis round trip.

| Key | Store file | Source | Source SHA256 |
|---|---|---|---|
| air_cut | wind-FS60030-qubodup-air_cut.mp3 | https://freesound.org/s/60030/ (qubodup, HQ preview) | `0301adf448c60b80c09b89df57510fd09949d6b15bb457ef7c9e70999b8a2ad0` |
| anime_ring | ring-FS706204-xkeril-nice_anime_sword_hit.mp3 | https://freesound.org/s/706204/ (xkeril, HQ preview) | `2a28c06b3674240e46bbf79516f87e5fbbe9522a1d272b55448f0af2c8599137` |
| anime_shing | ring-FS529019-Euphrosyyn-anime_shing_sword_2.mp3 | https://freesound.org/s/529019/ (Euphrosyyn, HQ preview) | `a8278823afb4c25a06d55ec2adfdeb7993bb738b1077310555be1e592063d02f` |
| armor_strike | metal-FS568170-Merrick079-sword_sound_1.mp3 | https://freesound.org/s/568170/ (Merrick079, HQ preview) | `5f9ab16b7a74a205b1490001d4c913d0d55f561df796cb2d43c1a30b97c351b1` |
| book_flip | pack-OGA-Kenney-RPGsounds.zip!OGG/bookFlip3.ogg | Kenney RPG Audio bookFlip3.ogg | `c85db5dceb3f1df073e960630277eaa88a5afda0477c1ddd68dad707621767be` |
| chop | pack-OGA-Kenney-RPGsounds.zip!OGG/chop.ogg | Kenney RPG Audio chop.ogg | `d00c2b3c9fff07e376145c8c8c45c90e5084ec192f6ce0387db233f7b86f1486` |
| cloth1 | pack-OGA-Kenney-RPGsounds.zip!OGG/cloth1.ogg | Kenney RPG Audio cloth1.ogg | `ddb93a3671233f95da0e0b10367f082f7eb42fa6caaddcf776410aa8833c747d` |
| cloth4 | pack-OGA-Kenney-RPGsounds.zip!OGG/cloth4.ogg | Kenney RPG Audio cloth4.ogg | `e7ab9a6c4466dea874196c61f59bf1da05cfe58748f42fadd695d441a154a99b` |
| creak1 | pack-OGA-Kenney-RPGsounds.zip!OGG/creak1.ogg | Kenney RPG Audio creak1.ogg | `8a346186fd297254248cab8e8117060a52a5cf2a84f603153a762108550ea95e` |
| creak2 | pack-OGA-Kenney-RPGsounds.zip!OGG/creak2.ogg | Kenney RPG Audio creak2.ogg | `8a990afdc03aebb91d528f5385e2f95582dbfa8e2c12c71098ab01be9142294a` |
| door_close | pack-OGA-Kenney-RPGsounds.zip!OGG/doorClose_4.ogg | Kenney RPG Audio doorClose_4.ogg | `fd21c0e7a9d0317375d2561590f0770dd3380ee35507d064862cb44d6f71595b` |
| draw_knife | pack-OGA-Kenney-RPGsounds.zip!OGG/drawKnife3.ogg | Kenney RPG Audio drawKnife3.ogg | `a11ae62fb1a628425769d11a9de394980ad8909c31f4c9a4316f226963e21caf` |
| energy_wave | swing-FS724716-greyfeather-sword_slash_energy_wave.mp3 | https://freesound.org/s/724716/ (greyfeather, HQ preview) | `5b9fbd1c8b78cd2e69c0ebfd178e229308b37058fe71da1cd4311c7f70a94b59` |
| knife_slice | pack-OGA-Kenney-RPGsounds.zip!OGG/knifeSlice2.ogg | Kenney RPG Audio knifeSlice2.ogg | `6c2064d0ef988d1ec3d56868e823ea8823a5cac00f2742560052633529407def` |
| low_impact | impact-FS541029-AudioPapkin-very_low_impact.mp3 | https://freesound.org/s/541029/ (AudioPapkin, HQ preview) | `73c25c4f49baa34cb9ad42290324fc61340124028dc0161299880b78580e335a` |
| metal_click | pack-OGA-Kenney-RPGsounds.zip!OGG/metalClick.ogg | Kenney RPG Audio metalClick.ogg | `9851a69d0c613e13bceef08060ecc4148f098ef487927cbebe270d642398a3b3` |
| metal_latch | pack-OGA-Kenney-RPGsounds.zip!OGG/metalLatch.ogg | Kenney RPG Audio metalLatch.ogg | `ba9ba60b172b3ebc131a940f25793cd2e207aca7af73dc80d637277f060f1708` |
| metal_pot | pack-OGA-Kenney-RPGsounds.zip!OGG/metalPot1.ogg | Kenney RPG Audio metalPot1.ogg | `159def979e8e386c2c539f5e99cc30a080eb2dcb6c911fa2e4ccc0785b2522fd` |
| rock_tumble | impact-FS389618-_stubb-rock_tumble_2.mp3 | https://freesound.org/s/389618/ (_stubb, HQ preview) | `199521191be552261d6e604c8d34e40cfeac4b3d7f3075906dd27182c73adb4a` |
| samurai_slash | swing-FS370204-nekoninja-samurai_slash.mp3 | https://freesound.org/s/370204/ (nekoninja, HQ preview) | `283b188b2f04f6676ae23be36e58a536bb78d5e7cf4bf5ab8cc95ca13b0065c2` |
| stick_woosh | swing-FS352719-Dalesome-woosh_stick.mp3 | https://freesound.org/s/352719/ (Dalesome, HQ preview) | `5dc0966b3f689fde08955ab18a3b8dc636cc3db96d105e90b427af54184c3016` |
| swish | swishes/swish-4.wav | https://opengameart.org/content/swishes-sound-pack (artisticdude, swish-4.wav) | `0060f4a7040edce4cc50d1daa10a9cb76764128a942e4688339e69cd1d5d784c` |
| swoosh | swing-FS263595-PorkMuncher-swoosh.mp3 | https://freesound.org/s/263595/ (PorkMuncher, HQ preview) | `5d11ca0d7ad2ad4bc3108c0b017cccd9ae3e002277e1550fa78693841ea85058` |
| sword_hit | metal-FS442769-qubodup-sword_hit.mp3 | https://freesound.org/s/442769/ (qubodup, HQ preview) | `93d72e63bb8d9b8a60d2c0ac665c153171515645fbb85f4ec028e4a253e7b167` |
| woosh | wind-FS683096-florianreichelt-woosh.mp3 | https://freesound.org/s/683096/ (florianreichelt, HQ preview) | `3c641d4d6ea0c6b65423d8fe1a7d72bf7bfb08a91c1640f9e9a0ab9d5d23b265` |

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/HatboxOpen.ogg`
- Asset ID: ebon-reward-sfx-hatboxopen-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (2.17 s)
- Creator: recordings by Kenney, PorkMuncher and qubodup; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: air_cut, cloth1, cloth4, metal_latch, swoosh in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -17.1 LUFS, true peak -9.8 dBFS; lid pop and chord re-timed to the opening show's release; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `083680c8133df6ab7b7387ede9bcdd1d513f08969200cd28419c132136f5b77a`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/Note0.ogg`
- Asset ID: ebon-reward-sfx-note0-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.36 s)
- Creator: original synthesis by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis only (Karplus-Strong silk string, tuned to B3); short-term loudness -20.0 LUFS, true peak -5.9 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; pitch checked against the tuned note within 3 cents; subjective listening and in-game mix not_run
- SHA256: `b9661da6bfbc8da7b136c4ffc934c30ccd8c8eefe107c5e18b754c410891d28e`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/Note1.ogg`
- Asset ID: ebon-reward-sfx-note1-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.36 s)
- Creator: original synthesis by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis only (Karplus-Strong silk string, tuned to D4); short-term loudness -20.0 LUFS, true peak -5.8 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; pitch checked against the tuned note within 3 cents; subjective listening and in-game mix not_run
- SHA256: `ce1d626229a69c32fb0af7631670168924746e034289c7a8c0a1c92217f9686b`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/Note2.ogg`
- Asset ID: ebon-reward-sfx-note2-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.36 s)
- Creator: original synthesis by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis only (Karplus-Strong silk string, tuned to F#4); short-term loudness -20.0 LUFS, true peak -5.7 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; pitch checked against the tuned note within 3 cents; subjective listening and in-game mix not_run
- SHA256: `24a035d633b3e743bf3c6e17dff4881fc05567959ef4c53febbcf75343631338`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/Note3.ogg`
- Asset ID: ebon-reward-sfx-note3-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.36 s)
- Creator: original synthesis by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis only (Karplus-Strong silk string, tuned to B4); short-term loudness -20.0 LUFS, true peak -4.7 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; pitch checked against the tuned note within 3 cents; subjective listening and in-game mix not_run
- SHA256: `e0092a0df0953742586c21ad3e3f88135f455aaca167732e136215249e518695`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/Note4.ogg`
- Asset ID: ebon-reward-sfx-note4-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.36 s)
- Creator: original synthesis by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis only (Karplus-Strong silk string, tuned to D5); short-term loudness -20.1 LUFS, true peak -4.5 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; pitch checked against the tuned note within 3 cents; subjective listening and in-game mix not_run
- SHA256: `a060568bf9cf1c5ed1ae836efbbc50b80509fd968b3833b805e9110418c6abf7`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/Note5.ogg`
- Asset ID: ebon-reward-sfx-note5-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.36 s)
- Creator: original synthesis by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis only (Karplus-Strong silk string, tuned to F#5); short-term loudness -20.0 LUFS, true peak -4.9 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; pitch checked against the tuned note within 3 cents; subjective listening and in-game mix not_run
- SHA256: `e9a2c98ac66876005707dff7be9a60f7379f4d2a20c35dedbcceb7796e8550bb`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/Note6.ogg`
- Asset ID: ebon-reward-sfx-note6-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.36 s)
- Creator: original synthesis by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis only (Karplus-Strong silk string, tuned to B5); short-term loudness -20.0 LUFS, true peak -4.4 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; pitch checked against the tuned note within 3 cents; subjective listening and in-game mix not_run
- SHA256: `f3c60bbe9424338a8f00d300194551d0e6bafb54e0db49c9347a5facd535f011`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/Note7.ogg`
- Asset ID: ebon-reward-sfx-note7-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.35 s)
- Creator: original synthesis by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: none; original NumPy synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: original synthesis only (Karplus-Strong silk string, tuned to D6); short-term loudness -20.0 LUFS, true peak -1.6 dBFS; pinned Ogg serial
- License and redistribution terms: original project asset under the existing project terms
- Required attribution: none; retain this provenance
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; pitch checked against the tuned note within 3 cents; subjective listening and in-game mix not_run
- SHA256: `775f419549fbdd57017e6a320ef4d0b0acd91568abf5d3207468958c43c9fa36`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/ShearSwing.ogg`
- Asset ID: ebon-reward-sfx-shearswing-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.40 s)
- Creator: recordings by Dalesome, Euphrosyyn, Kenney, nekoninja and PorkMuncher; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: anime_shing, cloth4, samurai_slash, stick_woosh, swoosh in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -17.2 LUFS, true peak -7.4 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `fe974b9de85e667c9b2c0dd36c243294baa0a1074eecf4f32368dcfaa7df9328`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/ShearSwingRise.ogg`
- Asset ID: ebon-reward-sfx-shearswingrise-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.50 s)
- Creator: recordings by Euphrosyyn, greyfeather and PorkMuncher; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: anime_shing, energy_wave, swoosh in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -16.2 LUFS, true peak -8.7 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `be2b41973ad22ce59c52d266d126da81a5b6723ac2d1212406d1b784c35ac500`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/ShearSnip.ogg`
- Asset ID: ebon-reward-sfx-shearsnip-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.48 s)
- Creator: recordings by Euphrosyyn, Kenney and Merrick079; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: anime_shing, armor_strike, knife_slice, metal_latch, metal_pot in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -14.1 LUFS, true peak -2.3 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `6220552ec878b2dab292a2f6c945c76aaa9a9b9bc2bc898631266df347ef86d4`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/ShearCut.ogg`
- Asset ID: ebon-reward-sfx-shearcut-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.97 s)
- Creator: recordings by Kenney, nekoninja and xkeril; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: anime_ring, draw_knife, knife_slice, samurai_slash in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -13.3 LUFS, true peak -6.2 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `6db42a80523f5932b34ff1c6201449557a0a010cfab3062c03441bdc80a25655`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/HarpLoose.ogg`
- Asset ID: ebon-reward-sfx-harploose-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.54 s)
- Creator: recordings by artisticdude, Kenney and qubodup; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: air_cut, chop, swish in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -18.0 LUFS, true peak -5.7 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `0255629bef4096392008c540e99fa7f94e3f06c5a765602059953502d33e532e`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/HarpChord.ogg`
- Asset ID: ebon-reward-sfx-harpchord-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (1.57 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: metal_latch in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -14.0 LUFS, true peak -6.7 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `15cf55fe5cff23b2e112444dcfa2abf03404e4b019bea0a581f553ef025ea156`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/ThimbleLift.ogg`
- Asset ID: ebon-reward-sfx-thimblelift-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.62 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cloth1, cloth4, creak1 in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -19.0 LUFS, true peak -14.1 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `d45372f1b8da0879c734dc1278cfe84de68bccad2133244dade41955595a80ac`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/FurnitureYank.ogg`
- Asset ID: ebon-reward-sfx-furnitureyank-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.50 s)
- Creator: recordings by Dalesome, Kenney and PorkMuncher; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cloth4, stick_woosh, swoosh in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -16.1 LUFS, true peak -5.9 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `2cb546aa01371566b3af3f286a188af8652fa16477b8021aa17bc38423c9cd8c`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/FurnitureCrash.ogg`
- Asset ID: ebon-reward-sfx-furniturecrash-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.72 s)
- Creator: recordings by _stubb and Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: chop, door_close, rock_tumble in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -14.1 LUFS, true peak -3.6 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `26c03066015e1476e9812f23a3ebc49a89d3aff599ff702face895072bb68522`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/PianoCrash.ogg`
- Asset ID: ebon-reward-sfx-pianocrash-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (2.13 s)
- Creator: recordings by _stubb, AudioPapkin and Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: chop, creak2, door_close, low_impact, metal_pot, rock_tumble in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -12.0 LUFS, true peak -5.2 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `a11b1460b472a2c28ae6ac122792f7795640b279ace553aaa4d00ade2bc50fe1`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/ChandelierSnip.ogg`
- Asset ID: ebon-reward-sfx-chandeliersnip-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.44 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: creak1, knife_slice, metal_click, metal_latch in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -16.1 LUFS, true peak -2.9 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `4cdabfc19ad39134d39ed4ee2ebc834268e64c480e954dba78c157fa270b951f`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/ChandelierShatter.ogg`
- Asset ID: ebon-reward-sfx-chandeliershatter-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (1.39 s)
- Creator: recordings by AudioPapkin and Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: low_impact, metal_pot in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -13.1 LUFS, true peak -7.6 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `ef19e6bc7c6987ad65e39dac0ace99184aab5bad19e83d9502e84090c4483477`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/ChandelierReel.ogg`
- Asset ID: ebon-reward-sfx-chandelierreel-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (1.30 s)
- Creator: recordings by Kenney and qubodup; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: air_cut, creak1, metal_click in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -19.1 LUFS, true peak -13.2 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `2b51775414a17b5c1d7c53767af09a95ac8c0e5801a48b08f68a63101ea166d5`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/SpoolThrow.ogg`
- Asset ID: ebon-reward-sfx-spoolthrow-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.45 s)
- Creator: recordings by PorkMuncher and qubodup; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: air_cut, swoosh in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -17.1 LUFS, true peak -3.5 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `969bccfb37174e5386636af8bfd011ed055d4eb4336f40b6cbf1f519ca91f858`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/SilkPin.ogg`
- Asset ID: ebon-reward-sfx-silkpin-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.54 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: chop, metal_click in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -18.0 LUFS, true peak -2.8 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `c830078421b2921f85474258de9f8ce9616d8955582d29489bfb1680b12423e0`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/ScissorsSnip.ogg`
- Asset ID: ebon-reward-sfx-scissorssnip-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (0.50 s)
- Creator: recordings by Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: knife_slice, metal_latch in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -14.0 LUFS, true peak -7.1 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `10142db595bfc44a5e669793feb1774508658170b2829842f20ea9a0b81b6975`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/SeverAll.ogg`
- Asset ID: ebon-reward-sfx-severall-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (1.51 s)
- Creator: recordings by AudioPapkin, Kenney, qubodup and xkeril; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: anime_ring, knife_slice, low_impact, sword_hit in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -12.2 LUFS, true peak -4.1 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `a0198fec92012b19c2fd2bbbbbdeca5dd17aa17da4542dd9f74f72967e7bb23e`

- Runtime file: `Assets/Sounds/Weapons/EbonRewards/WaltzOpen.ogg`
- Asset ID: ebon-reward-sfx-waltzopen-20261002
- Asset type: stereo 44.1 kHz Vorbis Ebon reward cue (1.59 s)
- Creator: recordings by florianreichelt and Kenney; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: book_flip, cloth4, metal_click, woosh in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_reward_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -17.0 LUFS, true peak -9.0 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-02; deterministic regeneration, loudness and true-peak checks; subjective listening and in-game mix not_run
- SHA256: `dee506c9ab9e6f8b0b64022aac723e3377e76c8a69dc5c66f87ca203544d47dc`

### Ghost Samurai original music — 2026-10-02

紫電の亡霊武者 / *Violet Phantom Blade*, an original boss theme composed for Ghost Samurai by Claude at the owner's request. The owner auditioned v1 and v2 on 2026-10-02 and chose v2 for the encounter. No melody, MIDI, arrangement, recording or stem is taken from any other work; the adopted EigHt/kuku tracks were used only as a loudness, spectrum and dynamic-arc reference. The score is code (an external studio recipe: score module, notation helpers, in-house synthesis, sfizz renderer and mixdown), rendered through two CC0 sample libraries plus original numpy synthesis. The libraries, the recipe and all stems stay outside the repository.

| Source | Version and URL | SHA256 or commit | Terms |
|---|---|---|---|
| VSCO 2 Community Edition (Versilian Studios) | 1.1.0 release archive, https://github.com/sgossner/VSCO-2-CE/archive/refs/tags/1.1.0.zip | `4a4446628df0e1a12aaee58e9f65f8fa7cde51971e961abb1b43083a6d3a8ab7` | CC0 1.0 (bundled `LICENSE`) |
| Versilian Community Sample Library (VCSL) | https://github.com/sgossner/VCSL | commit `b6e6ac82d22248edee98a0bde185eb9ef6d439ad` | CC0 1.0 (repository README) |

Sampled instruments: VSCO strings (viola/cello/contrabass spiccato, violin/viola/cello/contrabass sustain, violin/cello tremolo), horn sustain/staccato, trumpet sustain, trombone sustain/staccato, tuba sustain/staccato, non-vibrato and vibrato flute, harp, timpani hits/rolls; VCSL Dan Tranh normal/tremolo (koto role), Bass Drum 1/2, Tom 1/2, Legacy Toms, Slit Drum, Suspended Cymbal 1, Clash Cymbals 1, Gong 1. Original synthesis: shamisen (extended Karplus-Strong with sawari buzz), shakuhachi breath and air, formant chant, taiko body, temple bell (bonshō), riser, sub drop, drone, mist.

Recipe files (external, SHA256): `samurai_theme.py` `781d3e4b072c695909c9f663be79220d4dbf1193d9441b42389cc94e4fb7e63c`, `theory.py` `6bfe442adf3036e38cd867d9cfe8a4ddba2ec40281bde6abee69ad4b599c5f00`, `synths.py` `44aca9da1a206ccd1c144e44852d0f35424c81b8caad2b00b0e48df0584ea794`, `engine.py` `36de9045f74bff3cf599c097b1ef07143744bffeb905f809d0b6d82536a3b5d3`, `render.py` `60a581ee9b2e7eaafc3f076d8dccb3083e6496d2e8f536cae5d8ed164e772454`, `mixdown.py` `cfaebc50bc3f1f61e438a84bd473a29524ee77e8cf5b3ff612b54dba9ddada83`. Every random choice is seeded; a fresh run from these files decodes sample-identical to the auditioned v2.

- Runtime file: `Assets/Music/GhostSamurai/VioletPhantomBlade.ogg`
- Asset ID: ghost-samurai-violet-phantom-blade-20261002
- Asset type: 48 kHz stereo Vorbis boss theme, 131.429 s; 168 BPM; one-shot 8-bar intro, then a 76-bar loop with native `LOOPSTART` 1097143 / `LOOPEND` 6308572 sample tags
- Creator: original composition, orchestration and mix by Claude (Anthropic) at the owner's request; sampled performances by Versilian Studios (CC0)
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: original score; CC0 sample sources in the table above
- Tool/model/version: Claude Opus 5.5; Python 3.12.10, pysfizz 0.1.3 (sfizz bindings, BSD-2-Clause), NumPy 2.5.3, SciPy 1.18.1, soundfile 0.14.0 / libsndfile 1.2.2 Vorbis at compression level 0.3 with a pinned Ogg serial, pyloudnorm 0.2.0, pedalboard 0.9.25 (compressor), mutagen 1.48.1 (tags)
- Human modifications: Owner direction, audition and selection of v2 over v1. Within the recipe: per-stem loudness, EQ and panning, original synthetic hall/room reverb, drum-bus and master compression, a 4x-oversampled true-peak limiter, loudness matched on the loop (-13.1 LUFS-I, true peak -1.1 dBTP decoded). The file repeats the first eight loop bars after the loop so LOOPSTART sits where every intro tail has died; the audio before LOOPEND matches the audio before LOOPSTART (-74 dB difference before encoding).
- License and redistribution terms: CC0 1.0 sample sources impose no conditions; the composition and recording follow the existing project original-asset terms and development publication gate
- Required attribution: none required by CC0; retain the table above as courtesy credit to Versilian Studios
- Reviewer and review date: owner audition and selection 2026-10-02; Claude numeric review 2026-10-02 (loudness, true peak, loop seam, click scan, band balance against the adopted tracks); in-game mix against the nine attack cues not_run
- SHA256: `5b9de70ac5e129715dbd437d015f454e218da055ac25b9110cde956170f79060`

### Waltz of the Ebon Manor — 2026-10-01

Original characters, props and hall for the new Raid, generated by Codex from Claude's owner-directed brief; no artist, work or franchise imitation was requested. Full prompts, candidates and owner selections: [asset brief](../docs/encounters/ebon-manor/ASSET_BRIEF.md). Delivery originals and the owner-supplied recording stay outside the repository. Luminance noise textures are referenced at runtime, never vendored.

- Runtime file: `Assets/Textures/EbonManor/Noirette.png`
- Asset ID: ebon-noirette-20261001
- Asset type: 192x128 eight-pose Terraria-density pixel NPC atlas (48x64 cells)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-01
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon Manor brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation and background extraction (image_gen.imagegen), exact model not exposed; `tools/export_ebon_art.py` with Pillow 12.1.1
- Human modifications: Owner-selected candidate b. Built-in background extraction by Codex. Mechanical nearest-neighbour 1536x1024 to 192x128 (one sample per logical pixel) and binary alpha at 128; no repaint. Source `asset-deliveries/ebon-manor/2026-10-01/alpha/EM01_b.png` SHA256 `e7b5b3ef6b5d926b5bcfd5a97898d600d9ee9f6bc04db25940249544e6c8f3a3`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: owner selection 2026-10-01; Claude alpha/export and offline material review 2026-10-01; in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `c4e107cab83d2b5e145a739b0c4f3920328e0a1ec49e94b96b40915f24d2544b`

- Runtime file: `Assets/Textures/EbonManor/BlackInvitation.png`
- Asset ID: ebon-invitation-20261001
- Asset type: 44x44 tilted invitation item icon
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-01
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon Manor brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation and background extraction (image_gen.imagegen), exact model not exposed; `tools/export_ebon_art.py` with Pillow 12.1.1
- Human modifications: Owner-selected tilted candidate b. Alpha below 16 to 0, content crop, then a mechanical 44x44 logical-cell majority colour (16-colour median-cut palette, opaque at half coverage) because the generated clusters are not on an exact grid; no repaint. Source `asset-deliveries/ebon-manor/2026-10-01/alpha/EM07_b.png` SHA256 `76ab7704b1eb6247ab4153e98a8e870245ee82a63d4b8920577f208a62fd1456`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: owner selection 2026-10-01; Claude alpha/export and offline material review 2026-10-01; in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `05da5e4c4abd0a46691448d40de069f60ed404db9e650977128b40642d0eb921`

- Runtime file: `Assets/Textures/EbonManor/ManorProps.png`
- Asset ID: ebon-props-20261001
- Asset type: 1232x176 atlas of seven thrown furniture pieces (176 px cells)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-01
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon Manor brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation and background extraction (image_gen.imagegen), exact model not exposed; `tools/export_ebon_art.py` with Pillow 12.1.1
- Human modifications: Seven of the eight generated cells (armchair, candelabra, portrait, clock, birdcage, mirror, music box). The porcelain doll cell is excluded at the owner's request. Alpha below 16 to 0 and 240+ to 255, per-cell content crop and Lanczos fit to 160 px; no repaint. Source `asset-deliveries/ebon-manor/2026-10-01/alpha/EM02_a.png` SHA256 `be39b0755f9ded496f7b8fecc245c0a9bfd1b7dfe07c70c9d6888c6f5faa83ed`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: owner selection 2026-10-01; Claude alpha/export and offline material review 2026-10-01; in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `280db05a1d3c68fef94f84a943ecb3c698331413993f24192a7a5126bbdf3bb4`

- Runtime file: `Assets/Textures/EbonManor/ChandelierWide.png`
- Asset ID: ebon-chandelier-wide-20261001
- Asset type: 300x338 transparent falling chandelier
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-01
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon Manor brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation and background extraction (image_gen.imagegen), exact model not exposed; `tools/export_ebon_art.py` with Pillow 12.1.1
- Human modifications: Alpha below 16 to 0 and 240+ to 255, content crop and Lanczos resize to 300 px wide; no repaint. Source `asset-deliveries/ebon-manor/2026-10-01/alpha/EM03_a.png` SHA256 `387bd6a1efdd52f8b8e9aa789297807572aba4828d0a769125278107297ca564`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: owner selection 2026-10-01; Claude alpha/export and offline material review 2026-10-01; in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `a093decfc20d87a19c5f55a8408a047071af7d9e1114d1a56edacf141d4290bc`

- Runtime file: `Assets/Textures/EbonManor/ChandelierTall.png`
- Asset ID: ebon-chandelier-tall-20261001
- Asset type: 240x383 transparent falling chandelier
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-01
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon Manor brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation and background extraction (image_gen.imagegen), exact model not exposed; `tools/export_ebon_art.py` with Pillow 12.1.1
- Human modifications: Alpha below 16 to 0 and 240+ to 255, content crop and Lanczos resize to 240 px wide; no repaint. Source `asset-deliveries/ebon-manor/2026-10-01/alpha/EM03_c.png` SHA256 `01db1673a45c37772dc9dd5345776a51ba35f29e5dc47083301dca03a7f7eaa6`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: owner selection 2026-10-01; Claude alpha/export and offline material review 2026-10-01; in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `729c747fceb7fa0d1b2c105be9b77f9a2a4969b9c01b6165637ef6ffd25e8581`

- Runtime file: `Assets/Textures/EbonManor/Shears.png`
- Asset ID: ebon-shears-20261001
- Asset type: 360x280 two-blade tailor's shears (upper half above, lower half below)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-01
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon Manor brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation and background extraction (image_gen.imagegen), exact model not exposed; `tools/export_ebon_art.py` with Pillow 12.1.1
- Human modifications: Each generated half cropped to content, alpha cleaned (below 16 to 0, 240+ to 255) and Lanczos resized to 360 px; stacked vertically. Pivot holes are measured at runtime; no repaint. Source `asset-deliveries/ebon-manor/2026-10-01/alpha/EM04_b.png` SHA256 `bc6001311fdb6a8719378391d014d54848ad36869c9f7a74cdb876996dae83f1`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: owner selection 2026-10-01; Claude alpha/export and offline material review 2026-10-01; in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `b9a27c5bc78737f825bca54d93894bd552c306ad9bfe6684b7e52aba3a639733`

- Runtime file: `Assets/Textures/EbonManor/ManorHall.png`
- Asset ID: ebon-hall-20261001
- Asset type: 1672x941 RGB painted manor hall backdrop
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-01
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon Manor brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation and background extraction (image_gen.imagegen), exact model not exposed; `tools/export_ebon_art.py` with Pillow 12.1.1
- Human modifications: Generated pixels kept; RGB re-encode only. Source `asset-deliveries/ebon-manor/2026-10-01/raw/EM05_b.png` SHA256 `f6f0f29d53b580846e3ccc758ed9a93ec4f3820d9080a5dd7b899f382072ec0a`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: owner selection 2026-10-01; Claude alpha/export and offline material review 2026-10-01; in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `4629f954de0f31fadb9307563c0530ca80a0afcba9e452ed60f4214241e3aaf7`

- Runtime file: `Assets/Textures/EbonManor/ManorHallFinal.png`
- Asset ID: ebon-hall-final-20261001
- Asset type: 1672x941 RGB collapsed hall backdrop (edit of the hall)
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-01
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon Manor brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation and background extraction (image_gen.imagegen), exact model not exposed; `tools/export_ebon_art.py` with Pillow 12.1.1
- Human modifications: Built-in edit of the owner-selected hall (EM05 b) into its torn Finale state; generated pixels kept, RGB re-encode only. Source `asset-deliveries/ebon-manor/2026-10-01/raw/EM05F_a.png` SHA256 `6b259560563ff6d6f72a0b51af669e9476ac47a22fae90ea87179e0f74a51e78`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: owner selection 2026-10-01; Claude alpha/export and offline material review 2026-10-01; in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `537ac861da43dbcb84016246a79b81db37eb5aa359182fe226c3aa81e76ccfa1`

- Runtime file: `Assets/Textures/EbonManor/ManorFrame.png`
- Asset ID: ebon-frame-20261001
- Asset type: 1672x941 transparent curtain-and-pillar proscenium layer
- Creator: project-owner-directed original artwork generated by Codex (OpenAI) from Claude's brief
- Creation/acquisition date: 2026-10-01
- Source type: generated
- Source work and URL: original built-in image generation from the Ebon Manor brief; repository-only style references, no third-party image input
- Tool/model/version: Codex built-in image generation and background extraction (image_gen.imagegen), exact model not exposed; `tools/export_ebon_art.py` with Pillow 12.1.1
- Human modifications: Built-in background extraction; alpha below 16 to 0 (clears 41 residual alpha-1 pixels in the empty centre) and 240+ to 255; no repaint. Source `asset-deliveries/ebon-manor/2026-10-01/alpha/EM06_b.png` SHA256 `387a49b8b057b12e9607874c9db87353ccdd94bb144a4b8fa6a96ee404141d75`.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: owner selection 2026-10-01; Claude alpha/export and offline material review 2026-10-01; in-game acceptance not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ASSET_BRIEF.md`
- SHA256: `b81b1bdb92410f55f8491480e76bd3e5a26176edf973adb49565d2f7312d6372`

**Music: EigHt — AutoMatador.** [Creator video](https://www.youtube.com/watch?v=twMGsSzV_SQ), [creator's BOOTH entry](https://bgm-cathedral.booth.pm/items/6178144), [governing terms](https://eight-novel.fanbox.cc/posts/7647818). The owner supplied this exact recording (MP3 SHA256 `af0e07f5fa3b7b0846e98cba94281cd507a20982d60c1d179842d79bf01eef43`) and selected it for this Raid. The public FANBOX terms post 7647818 (updated 2026-07-14, read 2026-10-01) permit free personal and commercial use as background music in games, and editing/modification, while EigHt keeps all copyright. They forbid redistributing or selling the material itself, registering it (including edits) with Content ID, and distributing it on music streaming services; music-game inclusion needs contact. This Raid is an action fight with background music, not chart gameplay. Credit is not required but requested: keep 'Music: EigHt'.

- Runtime file: `Assets/Music/EbonManor/ActOne.ogg`
- Asset ID: ebon-music-actone-20261001
- Asset type: stereo 48 kHz Vorbis section edit with long LOOPSTART/LOOPEND loops
- Creator: composition, performance and recording by EigHt; section edit by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: licensed
- Source work and URL: EigHt, AutoMatador, https://www.youtube.com/watch?v=twMGsSzV_SQ; terms https://eight-novel.fanbox.cc/posts/7647818
- Tool/model/version: `tools/edit_ebon_music.py` and `tools/ogg_tools.py`; NumPy 2.4.4, soundfile 0.14.0/libsndfile 1.2.2 Vorbis
- Human modifications: The recording's two-beat pickup and whole 14-bar intro (the protected entrance) to the A drop at bar 14, then the song through A, the breakdown, B and the break; at bar 54, where A' returns, it loops back to the A drop at bar 14 (40 bars, 76.80 s; the bars after and before the jump match the song's own repeat: similarity 0.99/0.96). 125 BPM grid with bar 0 at 1.195 s (the first audible beat at 0.235 s is a two-beat pickup); -1.5 dB gain; a one-beat crossfade landing on the loop start's downbeat with correlation-compensated constant-power gains and the incoming window aligned by cross-correlation; one never-played bar of the continuation after LOOPEND keeps the Vorbis frames continuous; 15 ms fade-in; pinned Ogg serial. No new melody or re-performance.
- License and redistribution terms: EigHt terms (game background use and editing permitted; no standalone redistribution/sale, streaming-service or Content ID registration; copyright retained by EigHt)
- Required attribution: Music: EigHt
- Reviewer and review date: Claude, 2026-10-01 (sections) and 2026-10-02 (long loops at the song's own repeats, owner request); joins checked numerically against the song's ordinary downbeats (spectral flux, sample step, loudness); owner listening and in-game mix not_run
- SHA256: `d02154a9ce6444bdba38833c81d27386f435cf0c2bbc882f65acf43527745950`

- Runtime file: `Assets/Music/EbonManor/ActTwo.ogg`
- Asset ID: ebon-music-acttwo-20261001
- Asset type: stereo 48 kHz Vorbis section edit with long LOOPSTART/LOOPEND loops
- Creator: composition, performance and recording by EigHt; section edit by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: licensed
- Source work and URL: EigHt, AutoMatador, https://www.youtube.com/watch?v=twMGsSzV_SQ; terms https://eight-novel.fanbox.cc/posts/7647818
- Tool/model/version: `tools/edit_ebon_music.py` and `tools/ogg_tools.py`; NumPy 2.4.4, soundfile 0.14.0/libsndfile 1.2.2 Vorbis
- Human modifications: Bars 36-61 (the build into B at bar 38, B, the break and A'), then from the gap before the climax (bar 61) back to the breakdown at bar 32; the file holds the first pass and then the loop bars 32-61 (29 bars, 55.68 s; join similarity 0.81/0.85). 125 BPM grid with bar 0 at 1.195 s; -1.5 dB gain; a one-beat crossfade landing on the loop start's downbeat with correlation-compensated constant-power gains and the incoming window aligned by cross-correlation; one never-played bar of the continuation after LOOPEND keeps the Vorbis frames continuous; 15 ms fade-in; pinned Ogg serial. No new melody or re-performance.
- License and redistribution terms: EigHt terms (game background use and editing permitted; no standalone redistribution/sale, streaming-service or Content ID registration; copyright retained by EigHt)
- Required attribution: Music: EigHt
- Reviewer and review date: Claude, 2026-10-01 (sections) and 2026-10-02 (long loops at the song's own repeats, owner request); joins checked numerically against the song's ordinary downbeats (spectral flux, sample step, loudness); owner listening and in-game mix not_run
- SHA256: `6b068460e487ce02df8f12b254a630c2b433a94bf3c3539b9600dfa5603f7562`

- Runtime file: `Assets/Music/EbonManor/Finale.ogg`
- Asset ID: ebon-music-finale-20261001
- Asset type: stereo 48 kHz Vorbis section edit with long LOOPSTART/LOOPEND loops
- Creator: composition, performance and recording by EigHt; section edit by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: licensed
- Source work and URL: EigHt, AutoMatador, https://www.youtube.com/watch?v=twMGsSzV_SQ; terms https://eight-novel.fanbox.cc/posts/7647818
- Tool/model/version: `tools/edit_ebon_music.py` and `tools/ogg_tools.py`; NumPy 2.4.4, soundfile 0.14.0/libsndfile 1.2.2 Vorbis
- Human modifications: Bars 51-67 (the break, A' at bar 54, the gap and the first climax phrase), then from the climax's second phrase (bar 67) back to A at bar 16; the file holds the first pass and then the loop bars 16-67 (51 bars, 97.92 s; join similarity 0.80/0.80). 125 BPM grid with bar 0 at 1.195 s; -1.5 dB gain; a one-beat crossfade landing on the loop start's downbeat with correlation-compensated constant-power gains and the incoming window aligned by cross-correlation; one never-played bar of the continuation after LOOPEND keeps the Vorbis frames continuous; 15 ms fade-in; pinned Ogg serial. No new melody or re-performance.
- License and redistribution terms: EigHt terms (game background use and editing permitted; no standalone redistribution/sale, streaming-service or Content ID registration; copyright retained by EigHt)
- Required attribution: Music: EigHt
- Reviewer and review date: Claude, 2026-10-01 (sections) and 2026-10-02 (long loops at the song's own repeats, owner request); joins checked numerically against the song's ordinary downbeats (spectral flux, sample step, loudness); owner listening and in-game mix not_run
- SHA256: `6c52c95101852473d27f03b8f9568368afb33ef9917e07d5819d063d6c6baefc`

- Runtime file: `Assets/Music/EbonManor/Curtain.ogg`
- Asset ID: ebon-music-curtain-20261001
- Asset type: stereo 48 kHz Vorbis bar-exact section edit with LOOPSTART/LOOPEND tags
- Creator: composition, performance and recording by EigHt; section edit by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: licensed
- Source work and URL: EigHt, AutoMatador, https://www.youtube.com/watch?v=twMGsSzV_SQ; terms https://eight-novel.fanbox.cc/posts/7647818
- Tool/model/version: `tools/edit_ebon_music.py` and `tools/ogg_tools.py`; NumPy 2.4.4, soundfile 0.14.0/libsndfile 1.2.2 Vorbis
- Human modifications: Bar 71 (the outro) to the end; no loop. 125 BPM grid with bar 0 at 1.195 s (every section entry lands there; the first audible beat at 0.235 s is a two-beat pickup); -1.5 dB gain; 60 ms equal-power seam into the loop start; 15 ms fade-in; pinned Ogg serial. No new melody or re-performance.
- License and redistribution terms: EigHt terms (game background use and editing permitted; no standalone redistribution/sale, streaming-service or Content ID registration; copyright retained by EigHt)
- Required attribution: Music: EigHt
- Reviewer and review date: Claude, 2026-10-01; seams and loop points checked numerically, audition page provided; owner listening and in-game mix not_run
- SHA256: `776d8f31ca68625d8b5deee77a9466f9cd9fe434259e26aab3a41329c3215c1c`

Silk, furniture and hall cues layer trimmed CC0 recordings with original NumPy synthesis (tuned Karplus-Strong silk strings in B minor, modal glass/chain partials, fabric rips, a synthetic hall tail). [`tools/generate_ebon_sfx.py`](../tools/generate_ebon_sfx.py) owns windows, filters, pitches, loudness targets and source hashes; the recordings stay in the local store and are not committed. Kenney RPG Audio and the artisticdude Swishes pack are CC0 on OpenGameArt; the Freesound uploads showed Creative Commons 0 on their pages when collected for Soboro (2026-10-01) and are their public HQ preview renders.

| Key | Store file | Source | Source SHA256 |
|---|---|---|---|
| air_cut | wind-FS60030-qubodup-air_cut.mp3 | https://freesound.org/s/60030/ (qubodup, HQ preview) | `0301adf448c60b80c09b89df57510fd09949d6b15bb457ef7c9e70999b8a2ad0` |
| book_flip | pack-OGA-Kenney-RPGsounds.zip!OGG/bookFlip3.ogg | Kenney RPG Audio bookFlip3.ogg | `c85db5dceb3f1df073e960630277eaa88a5afda0477c1ddd68dad707621767be` |
| chop | pack-OGA-Kenney-RPGsounds.zip!OGG/chop.ogg | Kenney RPG Audio chop.ogg | `d00c2b3c9fff07e376145c8c8c45c90e5084ec192f6ce0387db233f7b86f1486` |
| cloth1 | pack-OGA-Kenney-RPGsounds.zip!OGG/cloth1.ogg | Kenney RPG Audio cloth1.ogg | `ddb93a3671233f95da0e0b10367f082f7eb42fa6caaddcf776410aa8833c747d` |
| cloth4 | pack-OGA-Kenney-RPGsounds.zip!OGG/cloth4.ogg | Kenney RPG Audio cloth4.ogg | `e7ab9a6c4466dea874196c61f59bf1da05cfe58748f42fadd695d441a154a99b` |
| creak1 | pack-OGA-Kenney-RPGsounds.zip!OGG/creak1.ogg | Kenney RPG Audio creak1.ogg | `8a346186fd297254248cab8e8117060a52a5cf2a84f603153a762108550ea95e` |
| creak2 | pack-OGA-Kenney-RPGsounds.zip!OGG/creak2.ogg | Kenney RPG Audio creak2.ogg | `8a990afdc03aebb91d528f5385e2f95582dbfa8e2c12c71098ab01be9142294a` |
| door_close | pack-OGA-Kenney-RPGsounds.zip!OGG/doorClose_4.ogg | Kenney RPG Audio doorClose_4.ogg | `fd21c0e7a9d0317375d2561590f0770dd3380ee35507d064862cb44d6f71595b` |
| draw_knife | pack-OGA-Kenney-RPGsounds.zip!OGG/drawKnife3.ogg | Kenney RPG Audio drawKnife3.ogg | `a11ae62fb1a628425769d11a9de394980ad8909c31f4c9a4316f226963e21caf` |
| kenney_zip | pack-OGA-Kenney-RPGsounds.zip | https://opengameart.org/content/50-rpg-sound-effects (Kenney, RPG Audio) | `3ae398ad63e293f9c450bda22d5d81c3af69c74df66fc1400f33c012c0bbc231` |
| knife_slice | pack-OGA-Kenney-RPGsounds.zip!OGG/knifeSlice2.ogg | Kenney RPG Audio knifeSlice2.ogg | `6c2064d0ef988d1ec3d56868e823ea8823a5cac00f2742560052633529407def` |
| low_impact | impact-FS541029-AudioPapkin-very_low_impact.mp3 | https://freesound.org/s/541029/ (AudioPapkin, HQ preview) | `73c25c4f49baa34cb9ad42290324fc61340124028dc0161299880b78580e335a` |
| metal_click | pack-OGA-Kenney-RPGsounds.zip!OGG/metalClick.ogg | Kenney RPG Audio metalClick.ogg | `9851a69d0c613e13bceef08060ecc4148f098ef487927cbebe270d642398a3b3` |
| metal_latch | pack-OGA-Kenney-RPGsounds.zip!OGG/metalLatch.ogg | Kenney RPG Audio metalLatch.ogg | `ba9ba60b172b3ebc131a940f25793cd2e207aca7af73dc80d637277f060f1708` |
| metal_pot | pack-OGA-Kenney-RPGsounds.zip!OGG/metalPot1.ogg | Kenney RPG Audio metalPot1.ogg | `159def979e8e386c2c539f5e99cc30a080eb2dcb6c911fa2e4ccc0785b2522fd` |
| rock_tumble | impact-FS389618-_stubb-rock_tumble_2.mp3 | https://freesound.org/s/389618/ (_stubb, HQ preview) | `199521191be552261d6e604c8d34e40cfeac4b3d7f3075906dd27182c73adb4a` |
| swish | swishes/swish-4.wav | https://opengameart.org/content/swishes-sound-pack (artisticdude, swish-4.wav) | `0060f4a7040edce4cc50d1daa10a9cb76764128a942e4688339e69cd1d5d784c` |
| swoosh | swing-FS263595-PorkMuncher-swoosh.mp3 | https://freesound.org/s/263595/ (PorkMuncher, HQ preview) | `5d11ca0d7ad2ad4bc3108c0b017cccd9ae3e002277e1550fa78693841ea85058` |
| woosh | wind-FS683096-florianreichelt-woosh.mp3 | https://freesound.org/s/683096/ (florianreichelt, HQ preview) | `3c641d4d6ea0c6b65423d8fe1a7d72bf7bfb08a91c1640f9e9a0ab9d5d23b265` |

- Runtime file: `Assets/Sounds/EbonManor/SilkCast.ogg`
- Asset ID: ebon-sfx-silkcast-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (0.82 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -25.0 LUFS, true peak -6.1 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `0127342f9d98a73285a2580cb8fe789d00321da723f02880a68cc99755a7b821`

- Runtime file: `Assets/Sounds/EbonManor/ThreadYank.ogg`
- Asset ID: ebon-sfx-threadyank-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (1.70 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -21.0 LUFS, true peak -6.8 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `c6d365f7c147223cd492208e8e70dca479b45ed970710838decaf497e565f696`

- Runtime file: `Assets/Sounds/EbonManor/PropCrash.ogg`
- Asset ID: ebon-sfx-propcrash-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (1.37 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -17.0 LUFS, true peak -4.0 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `4a8916457219334d0b428174256fc176be6e34fb5c8ea0458ffebe38ca8c3638`

- Runtime file: `Assets/Sounds/EbonManor/ChandelierCreak.ogg`
- Asset ID: ebon-sfx-chandeliercreak-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (1.92 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -23.0 LUFS, true peak -14.3 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `2be25d935dfd5344acd47b50f32460576e04fd52498cb1cb94ccdc133e8d1f34`

- Runtime file: `Assets/Sounds/EbonManor/ThreadSnap.ogg`
- Asset ID: ebon-sfx-threadsnap-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (0.72 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -24.9 LUFS, true peak -0.9 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `ff8eab0b697ecc15bf7480829a62830e5b53b82fabf28608d0a22dee829e96db`

- Runtime file: `Assets/Sounds/EbonManor/ChandelierShatter.ogg`
- Asset ID: ebon-sfx-chandeliershatter-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (2.08 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -15.1 LUFS, true peak -6.0 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `229d1fae36dd306fcdc954675502ca4aa8ea505bf2840f1721175f6d38edbf80`

- Runtime file: `Assets/Sounds/EbonManor/LoomTighten.ogg`
- Asset ID: ebon-sfx-loomtighten-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (2.05 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -23.1 LUFS, true peak -11.9 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `1b87a3e79db82c432c24c784731bfae643e93b1cd3e60a48e1dd1c232fe9afbd`

- Runtime file: `Assets/Sounds/EbonManor/LoomTwang.ogg`
- Asset ID: ebon-sfx-loomtwang-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (2.53 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -18.1 LUFS, true peak -5.1 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `c39d2b0a63273da916697adaa6bc593fe48d5283961d0fc79757f3c62a898f9f`

- Runtime file: `Assets/Sounds/EbonManor/ShearsOpen.ogg`
- Asset ID: ebon-sfx-shearsopen-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (1.87 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -20.9 LUFS, true peak -12.4 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `2a7014165ade200956b88ce193932233ecce695899d2ebf59b2189408df9945d`

- Runtime file: `Assets/Sounds/EbonManor/ShearsSnip.ogg`
- Asset ID: ebon-sfx-shearssnip-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (1.18 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -16.2 LUFS, true peak -3.1 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `57c721235a3f3b58a65c9f7af93ad312d78eb32fd95e26291861a6982bf95457`

- Runtime file: `Assets/Sounds/EbonManor/WaltzOpen.ogg`
- Asset ID: ebon-sfx-waltzopen-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (2.46 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -20.0 LUFS, true peak -11.9 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `7fa45cbc3ad2099ca7fca27575e6d72c0bba335463a9eee521abb78882976bc4`

- Runtime file: `Assets/Sounds/EbonManor/WaltzRelease.ogg`
- Asset ID: ebon-sfx-waltzrelease-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (2.89 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -19.0 LUFS, true peak -10.5 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `b6aa773c2dd6d4c02020ed68a5b33ad47547b36245ac3f824ba4fbf334b77c0c`

- Runtime file: `Assets/Sounds/EbonManor/StitchCall.ogg`
- Asset ID: ebon-sfx-stitchcall-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (1.87 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -21.0 LUFS, true peak -16.5 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `082c516603ae80259215550b889db9d71475ee5b98c7ff5fb10ad3c5a26dfe4a`

- Runtime file: `Assets/Sounds/EbonManor/StitchBind.ogg`
- Asset ID: ebon-sfx-stitchbind-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (2.61 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -19.0 LUFS, true peak -9.1 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `6f956e194d771938d5402ba63f415295b25294295ebe0962ba0ebbbd769197e8`

- Runtime file: `Assets/Sounds/EbonManor/StitchTear.ogg`
- Asset ID: ebon-sfx-stitchtear-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (1.38 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -17.2 LUFS, true peak -6.1 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `177065369349c589f49acf48f0a52def80f2fff23dcb90f720e7999d11325be5`

- Runtime file: `Assets/Sounds/EbonManor/Weave.ogg`
- Asset ID: ebon-sfx-weave-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (3.01 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -21.0 LUFS, true peak -11.8 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `e9ed55cb2e7f1afe8d9d94b39d45c29042e5a3d8502f30b8a1a0c79b89cda53c`

- Runtime file: `Assets/Sounds/EbonManor/SilkBurst.ogg`
- Asset ID: ebon-sfx-silkburst-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (1.82 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -18.0 LUFS, true peak -2.3 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `b33e0e997f6eaa5b0e3191f45c3c30c1cac562c3e9a1dfd183711afd26be0c82`

- Runtime file: `Assets/Sounds/EbonManor/ActChange.ogg`
- Asset ID: ebon-sfx-actchange-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (2.40 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -17.0 LUFS, true peak -7.6 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `2864d1515a1604810b5162640406939b833303e7f2933f470130db7081f8f105`

- Runtime file: `Assets/Sounds/EbonManor/ManorTear.ogg`
- Asset ID: ebon-sfx-manortear-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (3.33 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -16.1 LUFS, true peak -4.8 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `57f547d9fbcd21091c9941a126e4bec137c70817a67b0b4d2cbcfc9e0be08148`

- Runtime file: `Assets/Sounds/EbonManor/CurtainFall.ogg`
- Asset ID: ebon-sfx-curtainfall-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (2.81 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -17.0 LUFS, true peak -5.7 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `4027d6036174fb9e609f217515031d5922238280cd88c5579dfa60b89a9e46a6`

- Runtime file: `Assets/Sounds/EbonManor/WebWeave.ogg`
- Asset ID: ebon-sfx-webweave-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (2.01 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -22.1 LUFS, true peak -5.2 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `276813fd7e89c84f572ea40cfc1c3906e6a6d73ebff2c1845c683d381a510aa9`

- Runtime file: `Assets/Sounds/EbonManor/WebSever.ogg`
- Asset ID: ebon-sfx-websever-20261001
- Asset type: stereo 44.1 kHz Vorbis Ebon Manor cue (1.22 s)
- Creator: recordings by Kenney, artisticdude, PorkMuncher, qubodup, florianreichelt, AudioPapkin and _stubb where used; synthesis and layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: CC0 recordings in the table above as selected by the cue recipe; remaining layers original synthesis
- Tool/model/version: `tools/generate_ebon_sfx.py`; NumPy 2.4.4, SciPy 1.16.1, soundfile 0.14.0/libsndfile 1.2.2 Vorbis at compression level 0.4
- Human modifications: trimmed, filtered and layered recordings plus original synthesis; short-term loudness -16.2 LUFS, true peak -2.1 dBFS; pinned Ogg serial
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude, 2026-10-01; deterministic regeneration, loudness and spectrogram checks; subjective listening and in-game mix not_run
- SHA256: `3db0f70f9496368bbad5bf58bde62ece733add17340f701a8b545dc26f33ec87`

- Runtime file: `Assets/AutoloadedEffects/Shaders/EbonManor.fxc`
- Asset ID: ebon-shader-ebonmanor-20261001
- Asset type: compiled original HLSL effect (hall, frame, furniture/body weave, chalk lanes, shears tear, stitch hoops, shards and glow passes)
- Creator: project-directed independent implementation by Claude for Minamium
- Creation/acquisition date: 2026-10-01
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/EbonManor.fx`; no third-party shader copied
- Tool/model/version: FXC compiler and options pinned in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: runtime parameters, masks and animation are project code; compilation does not modify approved images
- License and redistribution terms: original project asset under the existing project terms; dependency textures remain external
- Required attribution: retain this provenance, source and compiler/export identity manifest
- Reviewer and review date: automated source/export checks and offline GPU material preview, 2026-10-01; native visual approval not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ENCOUNTER_SPEC.md`, presentation
- SHA256: `4fa244940362c3bcc2704a690bd31943a3ac26b773d2e4dc9b0efed5495b534d`
- Source SHA256: `e659fa5154ec1c23782f267e45b71b546c0f8315439f86cfd344987dc66d54a5`

- Runtime file: `Assets/AutoloadedEffects/Shaders/EbonSilk.fxc`
- Asset ID: ebon-shader-ebonsilk-20261001
- Asset type: compiled original HLSL effect (silk thread material for Luminance primitive ribbons)
- Creator: project-directed independent implementation by Claude for Minamium
- Creation/acquisition date: 2026-10-01
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/EbonSilk.fx`; no third-party shader copied
- Tool/model/version: FXC compiler and options pinned in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: runtime parameters, masks and animation are project code; compilation does not modify approved images
- License and redistribution terms: original project asset under the existing project terms; dependency textures remain external
- Required attribution: retain this provenance, source and compiler/export identity manifest
- Reviewer and review date: automated source/export checks and offline GPU material preview, 2026-10-01; native visual approval not_run
- Prompt or brief location: `docs/encounters/ebon-manor/ENCOUNTER_SPEC.md`, presentation
- SHA256: `29ab697695e19b9cc77ffd04c3cb19ac1141568972bb408eaabd553a75ca58c8`
- Source SHA256: `70adae2a0fa8d892f9b0dc876b5dab375f65dc0f6b8c5938f9101a6c13006fc3`

### Ghost Samurai recorded attack audio — 2026-10-01

Replaces the boss's borrowed vanilla Item1/ScaryScream/Item4/Item14 references; the owner approved CC0 recordings for these cues on2026-10-01. [`tools/remix_samurai_sfx.py`](../tools/remix_samurai_sfx.py) owns exact windows, filters, gains, loudness targets and source hashes, using the shared [`tools/sfx_layers.py`](../tools/sfx_layers.py). The recordings stay in a local source store and are not committed. Every source below showed Creative Commons0 (https://creativecommons.org/publicdomain/zero/1.0/) on its page on2026-10-01. Freesound files are the public HQ preview renders of those CC0 uploads.

| Key | Recording | Author | Source | Source SHA256 |
|---|---|---|---|---|
| air_cut | Swosh / Whoosh / Air Cut | qubodup | https://freesound.org/s/60030/ | `0301adf448c60b80c09b89df57510fd09949d6b15bb457ef7c9e70999b8a2ad0` |
| anime_ring | Nice anime sword hit | xkeril | https://freesound.org/s/706204/ | `2a28c06b3674240e46bbf79516f87e5fbbe9522a1d272b55448f0af2c8599137` |
| anime_shing | Anime_drama_shing_sword_2.wav | Euphrosyyn | https://freesound.org/s/529019/ | `a8278823afb4c25a06d55ec2adfdeb7993bb738b1077310555be1e592063d02f` |
| concrete_smash | Concrete SMASH 2 | magnuswaker | https://freesound.org/s/522099/ | `b182dec5699903068a509113e09e0b4c02a78f42b7aa73f875b23fc24ce34c6e` |
| demon_howl | Demon Giant Howl.wav | Bananaboatman33 | https://freesound.org/s/257635/ | `2a6487786a58fabe2f20ce051408e5caf04b552a83663f0bf377711646ffe58b` |
| energy_wave | sword slash energy wave | greyfeather | https://freesound.org/s/724716/ | `5b9fbd1c8b78cd2e69c0ebfd178e229308b37058fe71da1cd4311c7f70a94b59` |
| metal_bowl | metal bowl - hit - with wooden spoon 01.wav | Anthousai | https://freesound.org/s/405665/ | `d5814c36039e2d231a324468afd12ffd98bec2d5fabf9e0142d6bf83f0357290` |
| rock_tumble | Rock Tumble 2.wav | _stubb | https://freesound.org/s/389618/ | `199521191be552261d6e604c8d34e40cfeac4b3d7f3075906dd27182c73adb4a` |
| samurai_slash | samurai slash | nekoninja | https://freesound.org/s/370204/ | `283b188b2f04f6676ae23be36e58a536bb78d5e7cf4bf5ab8cc95ca13b0065c2` |
| singing_bowl | singing bowl strike sound | inoshirodesign | https://freesound.org/s/271370/ | `5841d9a2a3ad026c69ec9c72f0a604a540e48235a0bc9aaf5c1c900986f780bc` |
| stick_woosh | Woosh (stick swung in the air) | Dalesome | https://freesound.org/s/352719/ | `5dc0966b3f689fde08955ab18a3b8dc636cc3db96d105e90b427af54184c3016` |
| stone_crash | Stone crash | discofield | https://freesound.org/s/711657/ | `91cca28a4c4a9eae31cdad60b8e73414b2b3fe261a9779aa719060a2c57a1503` |
| swish_short | swish-10.wav, Swishes Sound Pack | artisticdude | https://opengameart.org/content/swishes-sound-pack | `4f7381a76f280d3f36f962ac3f44f16f77eec44797f30715d063c81ea3859024` |
| swoosh | swoosh.wav | PorkMuncher | https://freesound.org/s/263595/ | `5d11ca0d7ad2ad4bc3108c0b017cccd9ae3e002277e1550fa78693841ea85058` |
| temple_bell | Bell at Daitokuji temple,kyoto.wav | nahmandub | https://freesound.org/s/131348/ | `c111ac138f867e07ea2a62ce0b75e9bf27ed9cd17fcaa6cfe0803f8252418bf2` |
| war_cry | Middle Ages War Cry.wav | joelcarrsound | https://freesound.org/s/521830/ | `380347fb94625c279ce05e29bab2ec94a2d4df6282b07aba4c075061ec929a84` |
| wind_whirl | Wind Whirl (Small Air Blow) | DARTEKZ_GAMEZ | https://freesound.org/s/719560/ | `5b41e14eaa752d4715ee7c706b99581f3adf5b02630c1d6c565b445a4e725c75` |
| zap | Electric zap.wav | michael_grinnell | https://freesound.org/s/512471/ | `8630ae76d1c6178d0fb0b9192b89326540e7b87cd3619683c2eee1c07dfe6575` |

- Runtime file: `Assets/Sounds/GhostSamurai/SamuraiSlash.ogg`
- Asset ID: ghost-samurai-slash-20261001
- Asset type: stereo44.1kHz Vorbis Ghost Samurai directional, vertical and circle slash cue layered from CC0 recordings
- Creator: recordings by Dalesome, Euphrosyyn, greyfeather and nekoninja; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: anime_shing, energy_wave, samurai_slash, stick_woosh in the table above
- Tool/model/version: `tools/remix_samurai_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, layered on the accepted attack clock and loudness-matched; any low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `ba5547167c95ffdaf0d6bd88ccf01fb8318653105bcd2a4ed810ee12165fb9a5`

- Runtime file: `Assets/Sounds/GhostSamurai/SamuraiGrid.ogg`
- Asset ID: ghost-samurai-grid-20261001
- Asset type: stereo44.1kHz Vorbis Ghost Samurai grid tear cue layered from CC0 recordings
- Creator: recordings by greyfeather, michael_grinnell, nekoninja and xkeril; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: anime_ring, energy_wave, samurai_slash, zap in the table above
- Tool/model/version: `tools/remix_samurai_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, layered on the accepted attack clock and loudness-matched; any low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `aef159e55d62f496e81166f075b270cec4233e984b88e1f3546d80f630ce43a5`

- Runtime file: `Assets/Sounds/GhostSamurai/SamuraiWave.ogg`
- Asset ID: ghost-samurai-wave-20261001
- Asset type: stereo44.1kHz Vorbis Ghost Samurai charged wave release cue layered from CC0 recordings
- Creator: recordings by Dalesome, greyfeather, nekoninja and xkeril; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: anime_ring, energy_wave, samurai_slash, stick_woosh in the table above
- Tool/model/version: `tools/remix_samurai_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, layered on the accepted attack clock and loudness-matched; any low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `146e6e948ebaf33d065a1c4044a34aa3b8ffd9fce708719e2d78eb1bbb2ec8fe`

- Runtime file: `Assets/Sounds/GhostSamurai/SamuraiCleave.ogg`
- Asset ID: ghost-samurai-cleave-20261001
- Asset type: stereo44.1kHz Vorbis Ghost Samurai frontal cleave cue layered from CC0 recordings
- Creator: recordings by Dalesome, Euphrosyyn, greyfeather, michael_grinnell, nekoninja and xkeril; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: anime_ring, anime_shing, energy_wave, samurai_slash, stick_woosh, zap in the table above
- Tool/model/version: `tools/remix_samurai_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, layered on the accepted attack clock and loudness-matched; any low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `78b86550efc4b7493f8063cb71dbaced1a47b57c08b08235952e0d337c30fe76`

- Runtime file: `Assets/Sounds/GhostSamurai/SamuraiRush.ogg`
- Asset ID: ghost-samurai-rush-20261001
- Asset type: stereo44.1kHz Vorbis Ghost Samurai dash cue layered from CC0 recordings
- Creator: recordings by artisticdude, greyfeather, nekoninja and PorkMuncher; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: energy_wave, samurai_slash, swish_short, swoosh in the table above
- Tool/model/version: `tools/remix_samurai_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, layered on the accepted attack clock and loudness-matched; any low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `72b8ca0c2c883f8818fd472a36908aec58400d2357db133c22bade844ebea2a2`

- Runtime file: `Assets/Sounds/GhostSamurai/SamuraiShout.ogg`
- Asset ID: ghost-samurai-shout-20261001
- Asset type: stereo44.1kHz Vorbis Ghost Samurai dash shout cue layered from CC0 recordings
- Creator: recordings by Bananaboatman33 and joelcarrsound; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: demon_howl, war_cry in the table above
- Tool/model/version: `tools/remix_samurai_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, layered on the accepted attack clock and loudness-matched; any low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `ead66d99c34b09b7d4f5a61fd076b4aa8a525975895757826c755db38bd4289e`

- Runtime file: `Assets/Sounds/GhostSamurai/SamuraiChime.ogg`
- Asset ID: ghost-samurai-chime-20261001
- Asset type: stereo44.1kHz Vorbis Ghost Samurai telegraph chime cue layered from CC0 recordings
- Creator: recordings by Anthousai, inoshirodesign and nahmandub; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: metal_bowl, singing_bowl, temple_bell in the table above
- Tool/model/version: `tools/remix_samurai_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, layered on the accepted attack clock and loudness-matched; any low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `b7522eb0aba3f685cd12e9e40b9f3ef89767e90d2983fc327c749c0f4c5e63b0`

- Runtime file: `Assets/Sounds/GhostSamurai/SamuraiWind.ogg`
- Asset ID: ghost-samurai-wind-20261001
- Asset type: stereo44.1kHz Vorbis Ghost Samurai kamaitachi wind cue layered from CC0 recordings
- Creator: recordings by DARTEKZ_GAMEZ, greyfeather and qubodup; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: air_cut, energy_wave, wind_whirl in the table above
- Tool/model/version: `tools/remix_samurai_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, layered on the accepted attack clock and loudness-matched; any low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `f5a9d9b5bcdfc203cfc16d63c11fe6aefd2ff0baf1eeb2ecab950070539528a1`

- Runtime file: `Assets/Sounds/GhostSamurai/SamuraiShock.ogg`
- Asset ID: ghost-samurai-shock-20261001
- Asset type: stereo44.1kHz Vorbis Ghost Samurai ground shockwave cue layered from CC0 recordings
- Creator: recordings by _stubb, discofield and magnuswaker; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: concrete_smash, rock_tumble, stone_crash in the table above
- Tool/model/version: `tools/remix_samurai_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, layered on the accepted attack clock and loudness-matched; any low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `3a629c14947d45c0ad8a30a981426519b3917c04263c5fb67339488bbb90ad02`

### Soboro recorded blade audio — 2026-10-01

Replaces the 2026-09-28 synthesized Soboro cues at the same runtime paths; the owner selected CC0 recordings for these masters on2026-10-01. [`tools/remix_soboro_sfx.py`](../tools/remix_soboro_sfx.py) owns exact windows, filters, gains, loudness targets and source hashes. The recordings stay in a local source store and are not committed. Every source below showed Creative Commons0 (https://creativecommons.org/publicdomain/zero/1.0/) on its page on2026-10-01. Freesound files are the public HQ preview renders of those CC0 uploads.

| Key | Recording | Author | Source | Source SHA256 |
|---|---|---|---|---|
| energy_wave | sword slash energy wave | greyfeather | https://freesound.org/s/724716/ | `5b9fbd1c8b78cd2e69c0ebfd178e229308b37058fe71da1cd4311c7f70a94b59` |
| samurai_slash | samurai slash | nekoninja | https://freesound.org/s/370204/ | `283b188b2f04f6676ae23be36e58a536bb78d5e7cf4bf5ab8cc95ca13b0065c2` |
| swoosh | swoosh.wav | PorkMuncher | https://freesound.org/s/263595/ | `5d11ca0d7ad2ad4bc3108c0b017cccd9ae3e002277e1550fa78693841ea85058` |
| stick_woosh | Woosh (stick swung in the air) | Dalesome | https://freesound.org/s/352719/ | `5dc0966b3f689fde08955ab18a3b8dc636cc3db96d105e90b427af54184c3016` |
| swish_short | swish-10.wav, Swishes Sound Pack | artisticdude | https://opengameart.org/content/swishes-sound-pack | `4f7381a76f280d3f36f962ac3f44f16f77eec44797f30715d063c81ea3859024` |
| anime_shing | Anime_drama_shing_sword_2.wav | Euphrosyyn | https://freesound.org/s/529019/ | `a8278823afb4c25a06d55ec2adfdeb7993bb738b1077310555be1e592063d02f` |
| anime_ring | Nice anime sword hit | xkeril | https://freesound.org/s/706204/ | `2a28c06b3674240e46bbf79516f87e5fbbe9522a1d272b55448f0af2c8599137` |
| sword_hit | Sword Hit | qubodup | https://freesound.org/s/442769/ | `93d72e63bb8d9b8a60d2c0ac665c153171515645fbb85f4ec028e4a253e7b167` |
| armor_strike | Sword sound 1.wav | Merrick079 | https://freesound.org/s/568170/ | `5f9ab16b7a74a205b1490001d4c913d0d55f561df796cb2d43c1a30b97c351b1` |
| bloody_blade | Bloody Blade 2.wav | Kreastricon62 | https://freesound.org/s/323526/ | `42f16fc3e230066399282c90187e83d20fecd851f32cdf1553b9a9e1225dcb98` |
| slashkut | slashkut.wav | Abyssmal | https://freesound.org/s/35213/ | `110aa6c69e8705bc14152f19f55fb53bdaf7e8582c4d95bb175311bdaf592ca8` |
| chop | chop.ogg, RPG Audio (RPGsounds_Kenney.zip) | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `d00c2b3c9fff07e376145c8c8c45c90e5084ec192f6ce0387db233f7b86f1486` |
| zap | Electric zap.wav | michael_grinnell | https://freesound.org/s/512471/ | `8630ae76d1c6178d0fb0b9192b89326540e7b87cd3619683c2eee1c07dfe6575` |

- Runtime file: `Assets/Sounds/Weapons/Soboro/CutDown.ogg`
- Asset ID: soboro-cutdown-20261001
- Asset type: stereo44.1kHz Vorbis Soboro first blade cue layered from CC0 recordings
- Creator: recordings by greyfeather, nekoninja, PorkMuncher, Euphrosyyn and michael_grinnell; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: energy_wave, samurai_slash, swoosh, anime_shing, zap in the table above
- Tool/model/version: `tools/remix_soboro_sfx.py` (score, windows, filters and loudness targets); NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, layered on the accepted cut clock and loudness-matched to the replaced cue; the small low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `015409da709f8c7e6973509542cc79c545e9fd76b0e1b2fba41ad237108dd09e`

- Runtime file: `Assets/Sounds/Weapons/Soboro/CutReverse.ogg`
- Asset ID: soboro-cutreverse-20261001
- Asset type: stereo44.1kHz Vorbis Soboro second blade cue layered from CC0 recordings
- Creator: recordings by greyfeather, PorkMuncher, artisticdude, Euphrosyyn and michael_grinnell; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: energy_wave, swoosh, swish_short, anime_shing, zap in the table above
- Tool/model/version: `tools/remix_soboro_sfx.py` (score, windows, filters and loudness targets); NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, layered on the accepted cut clock and loudness-matched to the replaced cue; the small low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `936b6ae192935baf4047ec03822be0a8f6d425d6da511af698c28dea434ddd81`

- Runtime file: `Assets/Sounds/Weapons/Soboro/CutHeavy.ogg`
- Asset ID: soboro-cutheavy-20261001
- Asset type: stereo44.1kHz Vorbis Soboro third blade cue layered from CC0 recordings
- Creator: recordings by Dalesome, greyfeather, nekoninja, Euphrosyyn, xkeril and michael_grinnell; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: stick_woosh, energy_wave, samurai_slash, anime_shing, anime_ring, zap in the table above
- Tool/model/version: `tools/remix_soboro_sfx.py` (score, windows, filters and loudness targets); NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, layered on the accepted cut clock and loudness-matched to the replaced cue; the small low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `38a222251627fd421e05277e797ff66a62cdddb179ec83cbfd68c37d8ed4b641`

- Runtime file: `Assets/Sounds/Weapons/Soboro/HitMetal.ogg`
- Asset ID: soboro-hitmetal-20261001
- Asset type: stereo44.1kHz Vorbis Soboro metal contact cue layered from CC0 recordings
- Creator: recordings by qubodup, Merrick079 and Kenney; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: sword_hit, armor_strike, chop in the table above
- Tool/model/version: `tools/remix_soboro_sfx.py` (score, windows, filters and loudness targets); NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, layered on the accepted cut clock and loudness-matched to the replaced cue; the small low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `ea4b5f08f76d957aad1a3f2cf4260006e2467f6ce41c1fd93aaa13c3e9738fb3`

- Runtime file: `Assets/Sounds/Weapons/Soboro/HitOrganic.ogg`
- Asset ID: soboro-hitorganic-20261001
- Asset type: stereo44.1kHz Vorbis Soboro organic contact cue layered from CC0 recordings
- Creator: recordings by Kreastricon62, Abyssmal and Kenney; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: public-domain
- Source work and URL: bloody_blade, slashkut, chop in the table above
- Tool/model/version: `tools/remix_soboro_sfx.py` (score, windows, filters and loudness targets); NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, layered on the accepted cut clock and loudness-matched to the replaced cue; the small low body is original sine synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-01; source licenses, timing, loudness, true peak and boundaries checked numerically; subjective listening and in-game mix remain owner-owned
- SHA256: `73a9735f6932b9140e7752a75e92886d52629570f01b18c7ddd87951625a45ca`

### Ghost Samurai sword tears — 2026-09-27

- Runtime file: `Assets/AutoloadedEffects/Shaders/SamuraiCut.fxc`
- Asset ID: samurai-cut-pixel-20261001
- Asset type: compiled original procedural pixel-art slash material
- Creator: Convergence, project-owner-directed work with OpenAI Codex (2026-09-27 material) and Claude (2026-10-01 pixel-art rewrite) assistance
- Creation/acquisition date: 2026-10-01
- Source type: original
- Source work and URL: original SamuraiCut.fx; Soboro's original SoboroPixelSlash palette is the style reference, not a third-party source
- Tool/model/version: FXC fx_2_0 O3; compiler/source/export hashes in compiled.json
- Human modifications: Rewritten to evaluate each accepted hazard per world-aligned 2x2 art pixel with Soboro's six-tone violet palette, lighting only cells wholly inside the footprint: marching-contour/dither forecasts, sweeping white-edged cuts, straight/wind/cleave field cuts, a thick travelling crescent, torn residue and a spirit-fire wisp pass. Installed Luminance noise is referenced, not redistributed. 2026-10-02 (Claude): forecasts gain a two-pixel contour, diagonal hatching from the first tick and a denser fill so they read over the sealed field.
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance
- Reviewer and review date: Claude compiled-material sequence review2026-10-01; Claude forecast-over-field review (84 frames) and cut sequence (1008 frames) 2026-10-02; native gameplay not_run

### Ghost Samurai battlefield — 2026-09-27

- Runtime file: `Assets/AutoloadedEffects/Shaders/SamuraiBattlefield.fxc`
- Asset ID: samurai-battlefield-20260927
- Asset type: compiled original procedural backdrop
- Creator: Convergence, project-owner-directed work with OpenAI Codex assistance
- Creation/acquisition date: 2026-09-27
- Source type: original
- Source work and URL: original SamuraiBattlefield.fx in this repository; no external artwork or shader copied
- Tool/model/version: FXC fx_2_0 O3; compiler/source/export hashes in compiled.json
- Human modifications: Original spears, low arched graves and layered violet mist; flags and floating rectangular stones removed. Opaque field edge with a small masked bleed. Luminance noise is referenced from the dependency, not redistributed. 2026-10-02 rework by Claude (Anthropic) at the owner's request: three passes (parallax night with a phase-linked eclipse, ridges, graves and spears planted on the floor line, distant lightning, a luminance ceiling under the forecast fill; the opaque abyss with a mirrored lake, spirit fire, pasted wall talismans and corner seals; the thin in-field rim light), a deploy trace from the summoner's feet and a Bayer-dithered victory melt. Original math only; no external shader or image. 2026-10-02 second pass by Claude at the owner's request: the graves become planted weapons (katana with tsuba, wrapped grip and a swaying cord, naginata, jumonji and leaf spears, arrow bundles) in a near and a far rank with a moonlit edge; the night is a cool slate so violet belongs to hazards; the ruled edge line is removed in favour of uneven spirit-fire roots, uneven inward light and a mist seam over the lake.
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance
- Reviewer and review date: Codex compiled-material preview 2026-09-27; Claude offline FNA/D3D11 preview of the 2026-10-02 rework (16 frames, luminance gate) 2026-10-02; Claude second-pass preview (16 field frames, 84 forecast-over-field frames, 22 cinema frames, luminance gate) 2026-10-02; native gameplay not_run

### Samurai spectral composite and DXOboro trial — 2026-09-27

Original Convergence images and procedural materials; existing VioletRig, Oboro art and all recordings are retained. The SamuraiSpirit material was revised under its existing provenance; current source/export identities are in `compiled.json`, superseding its historical hashes below. Generated originals remain outside the source tree. No WotG/Calamity sprite, shader or recording is imported.

- Runtime file: `Assets/Textures/Items/GhostSamurai/SealedMask.png`
- Asset ID: ghost-samurai-sealed-mask-20260927
- Asset type: 64×64 RGBA summon icon
- Creator: project-owner-directed original work with OpenAI Codex assistance
- Creation/acquisition date: 2026-09-27
- Source type: generated
- Source work and URL: original Convergence image generation; no third-party source artwork
- Tool/model/version: Built-in OpenAI image generation; exact underlying model undisclosed
- Human modifications: Original ivory oni mask, indigo horns, worn gold, one paper seal and violet ghost flame; transparent game-item silhouette. Newly generated, not traced from external art. System.Drawing bicubic size export; source retained externally.
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance and generation disclosure
- Reviewer and review date: Codex offline asset/material inspection2026-09-27; native playtest not_run
- SHA256: `a11cb0c54c73d16af44a2d7dc1902376c582bb784cec6ec9e119c0f55b843d87`

- Runtime file: `Assets/Textures/Items/DXOboro/Blade.png`
- Asset ID: dx-oboro-blade-20260927
- Asset type: 768×512 RGBA weapon part/icon
- Creator: project-owner-directed original work with OpenAI Codex assistance
- Creation/acquisition date: 2026-09-27
- Source type: generated
- Source work and URL: original Convergence image generation; no third-party source artwork
- Tool/model/version: Built-in OpenAI image generation; exact underlying model undisclosed
- Human modifications: Original curved indigo/ivory blade, violet energy seam, restrained gold guard and black-wrapped hilt. New generation; no prior weapon image input. Bicubic half-size export; grip/pivot and inventory scale calibrated in code; source retained externally.
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance and generation disclosure
- Reviewer and review date: Codex offline asset/material inspection2026-09-27; native playtest not_run
- SHA256: `eaca9440f365ec6982150521cb4654b7bf14ed239ecd13daafe78046585a2080`

- Runtime file: `Assets/AutoloadedEffects/Shaders/SamuraiEnergy.fxc`
- Asset ID: samuraienergy-20260927
- Asset type: compiled original material
- Creator: project-owner-directed original work with OpenAI Codex assistance
- Creation/acquisition date: 2026-09-27
- Source type: original
- Source work and URL: original Convergence HLSL source alongside runtime export
- Tool/model/version: FXC fx_2_0 O3; compiler/source/export hashes in compiled.json
- Human modifications: Original repository-owned SamuraiEnergy.fx; independently authored flow/emission/analytic masks. Luminance API/noise references are runtime dependencies, not vendored textures.
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance and generation disclosure
- Reviewer and review date: Codex offline asset/material inspection2026-09-27; native playtest not_run

- Runtime file: `Assets/AutoloadedEffects/Shaders/SamuraiComposite.fxc`
- Asset ID: samuraicomposite-20260927
- Asset type: compiled original material
- Creator: project-owner-directed original work with OpenAI Codex assistance
- Creation/acquisition date: 2026-09-27
- Source type: original
- Source work and URL: original Convergence HLSL source alongside runtime export
- Tool/model/version: FXC fx_2_0 O3; compiler/source/export hashes in compiled.json
- Human modifications: Original repository-owned SamuraiComposite.fx; independently authored flow/emission/analytic masks. Luminance API/noise references are runtime dependencies, not vendored textures.
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance and generation disclosure
- Reviewer and review date: Codex offline asset/material inspection2026-09-27; native playtest not_run

- Runtime file: `Assets/AutoloadedEffects/Shaders/SoboroPixelSlash.fxc`
- Asset ID: soboropixelslash-20261001
- Asset type: compiled original material
- Creator: project-owner-directed original work with Anthropic Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: original
- Source work and URL: original Convergence HLSL source alongside runtime export
- Tool/model/version: FXC fx_2_0 O3; compiler/source/export hashes in compiled.json
- Human modifications: Original repository-owned SoboroPixelSlash.fx: a swept crescent evaluated per art pixel in a half-resolution target, quantized to a six-tone violet palette with a checker-dithered deep tone, then point-upscaled with a one-pixel outline and restrained glow. Replaces the retired DXOboroVeil material; no Calamity or other third-party art/code/sample imported.
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance and generation disclosure
- Reviewer and review date: Claude offline compiled-material sequence review 2026-10-01; native playtest not_run

- Runtime file: `Assets/Textures/Items/DXOboro/SlashNoise.png`
- Asset ID: soboro-slash-noise-20261001
- Asset type: 128×128 RGB tileable noise texture
- Creator: project-owner-directed original work with Anthropic Claude assistance
- Creation/acquisition date: 2026-10-01
- Source type: generated
- Source work and URL: procedural output of repository script `tools/generate_soboro_noise.py`; no source artwork
- Tool/model/version: tools/generate_soboro_noise.py (NumPy value-noise FBM, seeds 1709/2851/4441, 16/32/64-cell octaves)
- Human modifications: none after generation; the script regenerates the file deterministically
- License and redistribution terms: existing project original code/asset terms; no new third-party redistribution grant
- Required attribution: retain project provenance and generation disclosure
- Reviewer and review date: Claude offline material inspection 2026-10-01; native playtest not_run
- SHA256: `598cfadd25244ad3e582171c2cf1cfc2cbb1a37fbeb79ff7eb8319b7ec7d67f8`


### Doll ruptured energy core — 2026-09-27

`Assets/AutoloadedEffects/Shaders/DollCoreEnergy.fx` and `Assets/AutoloadedEffects/Shaders/DollCoreEnergy.fxc`: original Convergence code-authored violet-volume material, created with OpenAI Codex assistance for the owner-requested claw impact/metal-shell rupture. Existing project code/asset terms apply; no third-party image, sound or shader was copied. Luminance's installed noise textures are referenced at runtime, not redistributed as extracted copies. `tools/compile_shaders.py` records the exact source/export/compiler hashes in `compiled.json`; `tools/preview-doll-core.ps1` renders the export for offline inspection. The existing `ShellBreak` recording is reused unchanged under its original entry.

- Runtime file: `Assets/AutoloadedEffects/Shaders/DollCoreEnergy.fxc`
- Asset ID: doll-ruptured-energy-core-20260927
- Asset type: compiled procedural spherical energy material
- Creator: project-owner-directed original code with OpenAI Codex assistance
- Creation/acquisition date: 2026-09-27
- Source type: original
- Source work and URL: repository-owned `Assets/AutoloadedEffects/Shaders/DollCoreEnergy.fx`; no external source copied
- Tool/model/version: FXC fx_2_0 O3; exact compiler/source/export hashes in `compiled.json`
- Human modifications: requested metal rupture, lasting violet energy and continued directional muzzle; code-authored turbulence, depth and softened silhouette
- License and redistribution terms: existing project original code/asset terms; dependency noise referenced at runtime only
- Required attribution: retain project provenance and generation disclosure
- Reviewer and review date: Codex compiled-material/production-mesh frame inspection2026-09-27; native playtest not_run

### Cathedral recorded audio (SFX v2) — 2026-10-02

Replaces the seven 2026-09-26 NumPy-only Azure cues and the borrowed Doll pressure/beam/chorus layers with one cue per event. The owner auditioned the full set and the lattice candidate on a listening page on2026-10-02 ("効果音全体的にいい"; lattice candidate A chosen). [`tools/remix_azure_sfx.py`](../tools/remix_azure_sfx.py) owns exact windows, filters, gains, loudness targets and the original support synthesis, using the shared [`tools/sfx_layers.py`](../tools/sfx_layers.py); its report lists every source path and hash. The recordings stay in a local source store and are not committed. The Freesound and OpenGameArt files are the same CC0 1.0 (https://creativecommons.org/publicdomain/zero/1.0/) uploads recorded and page-checked for Ghost Samurai and Soboro on2026-10-01; Freesound files are the public HQ preview renders. VSCO 2 Community Edition 1.1.0 is distributed under CC0 1.0 (its bundled LICENSE). The lattice core reuses the project-owned Doll `ChargeRush` beam cue.

| Key | Recording | Author | Source | Source SHA256 |
|---|---|---|---|---|
| cc0:bell-FS131348-nahmandub-daitokuji_bell | Bell at Daitokuji temple,kyoto.wav | nahmandub | https://freesound.org/s/131348/ | `c111ac138f867e07ea2a62ce0b75e9bf27ed9cd17fcaa6cfe0803f8252418bf2` |
| cc0:bell-FS271370-inoshirodesign-singing_bowl | singing bowl strike sound | inoshirodesign | https://freesound.org/s/271370/ | `5841d9a2a3ad026c69ec9c72f0a604a540e48235a0bc9aaf5c1c900986f780bc` |
| cc0:bell-FS405665-Anthousai-metal_bowl_hit | metal bowl - hit - with wooden spoon 01.wav | Anthousai | https://freesound.org/s/405665/ | `d5814c36039e2d231a324468afd12ffd98bec2d5fabf9e0142d6bf83f0357290` |
| cc0:drawKnife2 | drawKnife2.ogg, 50 RPG Sound Effects | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `d5df6a4130cbb016f97b4883769a418917b9629cb4c21717030b186e0c73281a` |
| cc0:drawKnife3 | drawKnife3.ogg, 50 RPG Sound Effects | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `a11ae62fb1a628425769d11a9de394980ad8909c31f4c9a4316f226963e21caf` |
| cc0:impact-FS389618-_stubb-rock_tumble_2 | Rock Tumble 2.wav | _stubb | https://freesound.org/s/389618/ | `199521191be552261d6e604c8d34e40cfeac4b3d7f3075906dd27182c73adb4a` |
| cc0:impact-FS522099-magnuswaker-concrete_smash_2 | Concrete SMASH 2 | magnuswaker | https://freesound.org/s/522099/ | `b182dec5699903068a509113e09e0b4c02a78f42b7aa73f875b23fc24ce34c6e` |
| cc0:impact-FS541029-AudioPapkin-very_low_impact | very low impact | AudioPapkin | https://freesound.org/s/541029/ | `73c25c4f49baa34cb9ad42290324fc61340124028dc0161299880b78580e335a` |
| cc0:impact-FS711657-discofield-stone_crash | Stone crash | discofield | https://freesound.org/s/711657/ | `91cca28a4c4a9eae31cdad60b8e73414b2b3fe261a9779aa719060a2c57a1503` |
| cc0:knifeSlice | knifeSlice.ogg, 50 RPG Sound Effects | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `4cd96dc630bed9840c15f1dd2306da2cc56a4da26a5d3f1a03c5a7265ac5e54f` |
| cc0:knifeSlice2 | knifeSlice2.ogg, 50 RPG Sound Effects | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `6c2064d0ef988d1ec3d56868e823ea8823a5cac00f2742560052633529407def` |
| cc0:metal-FS442769-qubodup-sword_hit | Sword Hit | qubodup | https://freesound.org/s/442769/ | `93d72e63bb8d9b8a60d2c0ac665c153171515645fbb85f4ec028e4a253e7b167` |
| cc0:metalPot1 | metalPot1.ogg, 50 RPG Sound Effects | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `159def979e8e386c2c539f5e99cc30a080eb2dcb6c911fa2e4ccc0785b2522fd` |
| cc0:metalPot3 | metalPot3.ogg, 50 RPG Sound Effects | Kenney | https://opengameart.org/content/50-rpg-sound-effects | `d306e5b848f6843332d0ca19f8f7dfe5796aeb56d8273902b085df463273f1dc` |
| cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2 | Anime_drama_shing_sword_2.wav | Euphrosyyn | https://freesound.org/s/529019/ | `a8278823afb4c25a06d55ec2adfdeb7993bb738b1077310555be1e592063d02f` |
| cc0:ring-FS706204-xkeril-nice_anime_sword_hit | Nice anime sword hit | xkeril | https://freesound.org/s/706204/ | `2a28c06b3674240e46bbf79516f87e5fbbe9522a1d272b55448f0af2c8599137` |
| cc0:roar-FS257635-Bananaboatman33-demon_giant_howl | Demon Giant Howl.wav | Bananaboatman33 | https://freesound.org/s/257635/ | `2a6487786a58fabe2f20ce051408e5caf04b552a83663f0bf377711646ffe58b` |
| cc0:roar-FS521830-joelcarrsound-war_cry | Middle Ages War Cry.wav | joelcarrsound | https://freesound.org/s/521830/ | `380347fb94625c279ce05e29bab2ec94a2d4df6282b07aba4c075061ec929a84` |
| cc0:swing-FS263595-PorkMuncher-swoosh | swoosh.wav | PorkMuncher | https://freesound.org/s/263595/ | `5d11ca0d7ad2ad4bc3108c0b017cccd9ae3e002277e1550fa78693841ea85058` |
| cc0:swing-FS370204-nekoninja-samurai_slash | samurai slash | nekoninja | https://freesound.org/s/370204/ | `283b188b2f04f6676ae23be36e58a536bb78d5e7cf4bf5ab8cc95ca13b0065c2` |
| cc0:swing-FS724716-greyfeather-sword_slash_energy_wave | sword slash energy wave | greyfeather | https://freesound.org/s/724716/ | `5b9fbd1c8b78cd2e69c0ebfd178e229308b37058fe71da1cd4311c7f70a94b59` |
| cc0:swish-10 | swish-10.wav, Swishes Sound Pack | artisticdude | https://opengameart.org/content/swishes-sound-pack | `4f7381a76f280d3f36f962ac3f44f16f77eec44797f30715d063c81ea3859024` |
| cc0:swish-11 | swish-11.wav, Swishes Sound Pack | artisticdude | https://opengameart.org/content/swishes-sound-pack | `9e81d548d8215fbb36f7a41b5771d4b52c2fd02fbb9b5ff9a4c65118fd88ee4d` |
| cc0:swish-12 | swish-12.wav, Swishes Sound Pack | artisticdude | https://opengameart.org/content/swishes-sound-pack | `0513a86d428d9ed8601e93b8554580a5932c6c1a82d39d81a8f48883f1807a67` |
| cc0:swish-13 | swish-13.wav, Swishes Sound Pack | artisticdude | https://opengameart.org/content/swishes-sound-pack | `698230ba3fe05a68c18d68cd6b3a07fb98e6c1198bb1a4238592ee9912c02c77` |
| cc0:wind-FS60030-qubodup-air_cut | Swosh / Whoosh / Air Cut | qubodup | https://freesound.org/s/60030/ | `0301adf448c60b80c09b89df57510fd09949d6b15bb457ef7c9e70999b8a2ad0` |
| cc0:wind-FS683096-florianreichelt-woosh | woosh | florianreichelt | https://freesound.org/s/683096/ | `3c641d4d6ea0c6b65423d8fe1a7d72bf7bfb08a91c1640f9e9a0ab9d5d23b265` |
| cc0:wind-FS719560-DARTEKZ_GAMEZ-wind_whirl | Wind Whirl (Small Air Blow) | DARTEKZ_GAMEZ | https://freesound.org/s/719560/ | `5b41e14eaa752d4715ee7c706b99581f3adf5b02630c1d6c565b445a4e725c75` |
| repo:FirstSeverance/Beams/ChargeRush | `Assets/Sounds/FirstSeverance/Beams/ChargeRush.wav` | Convergence | project asset, recorded above as doll-beam-0273-chargerush | `57adc7042b6ba34074f5c02f455e78471e37bcc13a5b7d404bec08b398378c31` |
| vsco:BDrumNewhit_v7_rr1_Sum | Percussion/BDrumNewhit_v7_rr1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `7024054e68261d2cca0ee58fdf9d6f23b54370792d04aa2c1ccf41acc090bd2b` |
| vsco:BellTree_Stroke1_v1_Sum | Percussion/BellTree_Stroke1_v1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `2597516c86677708c0696ab303e81158ab1d69baa109635ceef204aa68d753e3` |
| vsco:BellTree_Stroke2_v1_Sum | Percussion/BellTree_Stroke2_v1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `a9a25b98a0e055b8fe72275e9ee944d5a16df4bc3f86a4ba542387b3025807d6` |
| vsco:BellTree_Stroke3_v1_Sum | Percussion/BellTree_Stroke3_v1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `2414c9adfdb93e87fc3cb77907667b1b9b291a7ca08579761fb311e7b3612541` |
| vsco:BellTree_Stroke4_v1_Sum | Percussion/BellTree_Stroke4_v1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `1ae8a741782fb466185ab23cb6919e1d13c23e4e41d02f9673290cf569131ef5` |
| vsco:Claves1_Hit_v2_rr1_Sum | Percussion/Claves1_Hit_v2_rr1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `ae0493fa57d0d1dd57693e8639dfe5fd9675b520a1c0c0eb45cf992f2a23ca9c` |
| vsco:Claves1_Hit_v2_rr2_Sum | Percussion/Claves1_Hit_v2_rr2_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `129ef8187b4c06ba995794039d01cddfcd9daad19cee6b7b950c6b7492ff6def` |
| vsco:Claves1_Hit_v3_rr1_Sum | Percussion/Claves1_Hit_v3_rr1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `5561306630b21fd9c0bfaf843c36bff29c90ea1bf84ee03c95b8996af24c0a4b` |
| vsco:Marimba_hit_Outrigger_G4_loud_01 | Percussion/Marimba/Marimba_hit_Outrigger_G4_loud_01.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `36f39f9f24789001add2b4c440defb7641178b0e8449c5cb7f1960bccaf0aaaf` |
| vsco:TB_hit_C4_v4_rr1 | Percussion/TB_hit_C4_v4_rr1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `d11d52910e0fc03c348bec0b0969960d737a6fdd3fa7380804f5751959e422c2` |
| vsco:TB_hit_C5_v4_rr1 | Percussion/TB_hit_C5_v4_rr1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `aba6ebfb36eed049d211ab86c8caa6e8dbaf292db57a2d269306c6aecb2a3ab2` |
| vsco:TB_hit_F5_v3_rr1 | Percussion/TB_hit_F5_v3_rr1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `34736a3a68fad88dde3ff7d04613bdbc1ba7be0804b039fc07312089e5cb1c44` |
| vsco:TB_hit_G4_v4_rr1 | Percussion/TB_hit_G4_v4_rr1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `de3df68f1dd86e71819de9c3a8284d5b5b11650e6f1c37483f505f86aa07cf68` |
| vsco:Triangle3-Hit_v2_rr1_Sum | Percussion/temp/Triangle3-Hit_v2_rr1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `c25a33345123c6fa0d0b7ccf725c62445df7713b0085abd8e016051ae20a09bb` |
| vsco:Triangle3-Hit_v2_rr2_Sum | Percussion/temp/Triangle3-Hit_v2_rr2_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `e5f73f0bceab31eafc85fd3bedb763c0579cbb4377bd573175af797d6833ca62` |
| vsco:Triangle3-Roll_v2_rr1_Sum | Percussion/temp/Triangle3-Roll_v2_rr1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `e9d913e472aaf4319f780e2943e033d25a9cffc2c6239380ef164b03dc6dab17` |
| vsco:Triangle6-HitFM_v1_rr1_Sum | Percussion/temp/Triangle6-HitFM_v1_rr1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `91beeeb7011c030f586022845f8511d86ad512abf8f530def31c84cd6b038886` |
| vsco:Triangle6-HitFM_v2_rr1_Sum | Percussion/temp/Triangle6-HitFM_v2_rr1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `021bdb7d8995b9ad3e5b1d993b49a298c7b0bd0ca1e4572926571b922868db4c` |
| vsco:Triangle6-HitM_v1_rr2_Sum | Percussion/temp/Triangle6-HitM_v1_rr2_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `d5e94804e6cc8a1415274435087f16d15de2f4236109ffb2a6d414d58d4b9ec1` |
| vsco:Triangle6-Hit_v1_rr2_Sum | Percussion/temp/Triangle6-Hit_v1_rr2_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `f41e3914720c1bdea0a846e67de1de943612d785db866fb126868140f7e1c380` |
| vsco:Triangle6-Hit_v2_rr1_Sum | Percussion/temp/Triangle6-Hit_v2_rr1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `6777424db5e9f6955489dbcfb5d91695d40c97764c67051f182aaecd17c61468` |
| vsco:Triangle6-Roll_v2_rr1_Sum | Percussion/temp/Triangle6-Roll_v2_rr1_Sum.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `bba2e233c97d4c6f0841be5cfbf11ae8fc77191313af86c2d855b497dc954423` |
| vsco:Xylo_Medium_C7_ff_01_far | Percussion/Xylo/Xylo_Medium_C7_ff_01_far.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `f922a9ca996341c3cd6d54d3f3c569812e5c4c3325b1bb19de503b113b9a9197` |
| vsco:Xylo_Medium_G4_ff_01_far | Percussion/Xylo/Xylo_Medium_G4_ff_01_far.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `f7e72d629db9b1f7d37b050cc1abf18bbde9629d616c91647d7abe5d7fb249b8` |
| vsco:ambience1 | Miscellania Raw/Misc 1/ambience1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `92cedeb576de685b9788630780a64b40ab13e06a3a8e91747264fe7725ee56aa` |
| vsco:bassdrum_rub1_v1 | Percussion/bassdrum_rub1_v1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `e906d63ab502971dfaefab6e2032332db327ba4b0cfe75101aef6d099fcb7df1` |
| vsco:bassdrum_rub2_v1 | Percussion/bassdrum_rub2_v1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `5dad744e5a88783194e4c410a3e37e797ff3ef1922bf2ecd5f9ec16e84eebb96` |
| vsco:bassdrum_rub3_v1 | Percussion/bassdrum_rub3_v1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `58071336becb66bf5782dea73e9db53ed46de2a537f47964c7df9b758617aa4a` |
| vsco:bassdrum_rub4_v1 | Percussion/bassdrum_rub4_v1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `159d47035e86d76a70a8d3c472fca77289cf01a879a1cd7f7f2316b0035faa4c` |
| vsco:brick_scrape | Miscellania Raw/Misc 1/brick_scrape.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `3cd736cd915a0dd1767022513b3f4074ec63948c4c2f7f6ddddacadd09376356` |
| vsco:brick_scrape2 | Miscellania Raw/Misc 1/brick_scrape2.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `398bf1d53c7bc7544d8b9b0585b78dff536011cdbf7567191957f3f93ebba204` |
| vsco:bubbles | Miscellania Raw/Misc 1/bubbles.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `f119e6f7be7e1a754ed6d1f2e196cbf78e67aa8ae97efe6fb4252997823408eb` |
| vsco:bubbles2 | Miscellania Raw/Misc 1/bubbles2.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `99fc88599996e388856c4b73c18dc64bd73e3120b1e0b66657d22f49e4c86b4b` |
| vsco:bubbles4 | Miscellania Raw/Misc 1/bubbles4.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `80dbaf61e5eb910674496439f71cfd64c5ae80eaed7ba9505c66a20457f17c5d` |
| vsco:chain_grind | Miscellania Raw/Misc 1/chain_grind.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `dfc9a7ec579f91d080460a4a617f0b5cd28518bc849f05f0a459841c2aececf4` |
| vsco:chaingrindLoop | Miscellania Raw/Misc 1/chaingrindLoop.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `9f97699cc87f2589461182ad200658a43cd9e84616f365492d5f254c970db813` |
| vsco:cymb_gong | Miscellania Raw/Misc 1/cymb_gong.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `8f549e5ada5acf07b139762e0c1191062e21777c8a672e732729f0f197259f0d` |
| vsco:cymbal-crash1_mf_rr2 | Percussion/cymbal-crash1_mf_rr2.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `88534fac7b69522738fa6ae39f305c424a26418c274a18ca5a08a1302e4c60a2` |
| vsco:cymbal-crashshort_v1 | Percussion/cymbal-crashshort_v1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `af0d5f0eb7f2226a1c592e5cda8e2dc0588cadbc851999ed39e3f36276acc7c9` |
| vsco:glass_break | Miscellania Raw/Misc 1/glass_break.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `5cf1b08875add0fa7598b09488c23a1208dccd03523cf7e5aa0b7bc0c1c31f69` |
| vsco:glass_break2 | Miscellania Raw/Misc 1/glass_break2.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `aeae215382299c4ae623c3dcbd7213394351d5e5fae138ee6cb9cf1f7d6fd423` |
| vsco:glass_break3 | Miscellania Raw/Misc 1/glass_break3.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `924a2cb9e993fc7471a3066eed23f4c360a9c1cb6a658e30aa4cf6c145cece07` |
| vsco:glass_break4 | Miscellania Raw/Misc 1/glass_break4.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `26ee8c33335bc8f45a9936195be393423d93157bc92b8a4ac06e9027e07613f5` |
| vsco:glass_break5 | Miscellania Raw/Misc 1/glass_break5.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `4bf674412e2e0daab66d024fd506a20839900bf36af57fc26794e4c18de75bc9` |
| vsco:glass_break6 | Miscellania Raw/Misc 1/glass_break6.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `6ce16ff3fcdc65b5e93366ff133d444714b56728bbd99430efaa024d51d9f763` |
| vsco:glass_break7 | Miscellania Raw/Misc 1/glass_break7.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `07871e4cf5bf549fbd439666b2a23e7e916994066f72d3a57f33e939118cca0e` |
| vsco:glass_break8 | Miscellania Raw/Misc 1/glass_break8.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `2f52ee0dcd4240bbcb84d135eb40e2be39ddf054744cdc585e3459a4651d32af` |
| vsco:glock_fx_down_chromatic_fast_01 | Miscellania Raw/Misc 2/glock_glisses/glock_fx_down_chromatic_fast_01.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `5b554b1db3eff1047c4e013e9dc30230a4399eb997436848b069ccca1fcaf156` |
| vsco:glock_fx_down_chromatic_fast_02 | Miscellania Raw/Misc 2/glock_glisses/glock_fx_down_chromatic_fast_02.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `4382b0d3b737f480ada1e1e3fddc80ff97d122f8230add4e692dd2435c50603a` |
| vsco:glock_fx_down_chromatic_fast_03 | Miscellania Raw/Misc 2/glock_glisses/glock_fx_down_chromatic_fast_03.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `0dcca09b69dd63b2e7bafb2ef4e300a69afb14625454ddf2b71491d0064334f8` |
| vsco:glock_fx_down_chromatic_fast_04 | Miscellania Raw/Misc 2/glock_glisses/glock_fx_down_chromatic_fast_04.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `c020adb36f99cf64824bcb251d622b97cc5943029d1fc26aa50220701dc78d6f` |
| vsco:glock_fx_down_pentatonic_med_01 | Miscellania Raw/Misc 2/glock_glisses/glock_fx_down_pentatonic_med_01.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `c7f97a55ee8b4325dc06d7aa025a48781db71e2e848b69b5089842d5c9d2e719` |
| vsco:glock_fx_up_chromatic_fast_01 | Miscellania Raw/Misc 2/glock_glisses/glock_fx_up_chromatic_fast_01.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `6b6cb6ee0fc804fa69f49a848098056ba9b8488b75d890aaa684f46a34f1023d` |
| vsco:glock_fx_up_chromatic_fast_02 | Miscellania Raw/Misc 2/glock_glisses/glock_fx_up_chromatic_fast_02.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `d186612818930426b6fb58c107abb8858b58fa48234fab0ca7104bb2276c11e5` |
| vsco:glock_fx_up_chromatic_med_01 | Miscellania Raw/Misc 2/glock_glisses/glock_fx_up_chromatic_med_01.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `36b7c91663eabc6593222dcc960e7e6bd08e5846001555295181bbbeafd17be4` |
| vsco:glock_fx_up_pentatonic_med_02 | Miscellania Raw/Misc 2/glock_glisses/glock_fx_up_pentatonic_med_02.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `146b16006d6e5665a46b47f864790c1f24596357e898e53db68edfa82aca740d` |
| vsco:glock_medium_C5 | Percussion/Glock/glock_medium_C5.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `635f898e0bcb6975b96b18efd9c896f6f1b97ff12174a339ada4814e49675660` |
| vsco:glock_medium_C6 | Percussion/Glock/glock_medium_C6.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `e942cbf502cf6731df2925945fe3234c876cb63f047d91007b6929887f9e4904` |
| vsco:glock_medium_C7 | Percussion/Glock/glock_medium_C7.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `b2ecbe0e60c983bbfaa49a1f45f8a11559f7dba27b8c8a032eb2cb7366a36910` |
| vsco:glock_medium_G4 | Percussion/Glock/glock_medium_G4.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `6c4649b13e24fa01e4634edad8fe8edf18aef33facc5d49fe53097b376bac4da` |
| vsco:glock_medium_G5 | Percussion/Glock/glock_medium_G5.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `7045c0ef7260e2f1ad11a64406cf7575ed898d1207a91104d33d78caefac63e4` |
| vsco:glock_medium_G6 | Percussion/Glock/glock_medium_G6.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `a2ba94e59119cd5272c1161fc8b8fbfb94c7b44de45a9a6cc5973b1c332103af` |
| vsco:gongHit_p | Percussion/gongHit_p.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `3d103e1a7af12eeb17e0c5488f11933ca92b9fbdefa39b8020b64238da2db0d0` |
| vsco:gongscrape_mf | Percussion/gongscrape_mf.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `3e30f51d3667cde89d3fd3424e878a554ab26ff4ea882686cfbe887aaf2267b0` |
| vsco:gongscrape_pp | Percussion/gongscrape_pp.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `0166526b453fa54a2d978796f2c4e696a213ac947c9cf2ec105f6af16667bd9e` |
| vsco:metal_hit11 | Miscellania Raw/Misc 1/metal_hit11.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `37573de7526c8dfc3893db67eb0d8b7ca746a2c53f507887c293044f43b44481` |
| vsco:metal_hit7 | Miscellania Raw/Misc 1/metal_hit7.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `674358f27b2d3a4dabf742137e039d2299ea28cf9eabdcea62a7b85fdd599b13` |
| vsco:metal_hit9 | Miscellania Raw/Misc 1/metal_hit9.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `896fdcca7cccc913fbf6391b9aca237e6210aa486ca86bf08c2693791a32fa3d` |
| vsco:sleighbell1_hit_3 | Miscellania Raw/Misc 2/sleighbell1_hit_3.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `71047c87f6799d4cc1f7d691c2d8d976dafc0a78568a89d688df4453626c79b1` |
| vsco:sleighbell1_hit_quiet | Miscellania Raw/Misc 2/sleighbell1_hit_quiet.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `1fe715f4eba2701c4391037a8f34bdfd66f2033159864e62217441ff989f7e6f` |
| vsco:susCymb1-bow-1 | Percussion/susCymb1-bow-1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `181f906018e2cc53a82eb73f209894fa07a4b1fb8718aa5b38ad0e15610489d3` |
| vsco:susCymb1-bow-2 | Percussion/susCymb1-bow-2.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `2bf2f6bc8a60eadcdc4a93b52b2f6f56954707063c14c4a8556359ca08e66042` |
| vsco:susCymb1-bow-3 | Percussion/susCymb1-bow-3.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `c0fc09c2a43f6a0a9d6b6616738b999beaf9290c2de5a111fe527f211f6890a2` |
| vsco:susCymb1-cresc-Median_v1 | Percussion/susCymb1-cresc-Median_v1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `bb3b6b251b0b9dac23b7b0fae47aab84a70abbefa4a110b05c9f4ad733e70741` |
| vsco:susCymb1-cresc-Short_v1 | Percussion/susCymb1-cresc-Short_v1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `69be3ba323fffc6d012602274f61dfd40519ca9b908a1bfe0926bbd0dd526983` |
| vsco:susCymb1-hit-bell_fff | Percussion/susCymb1-hit-bell_fff.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `6be90461233ba1900c1c02118cc0d00b50d0d534596ec0ae2fe5bc1e0ce0e715` |
| vsco:susCymb1-scrape1_v1 | Percussion/susCymb1-scrape1_v1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `c75699ffbca27b4d24b7b4a06e13e2d250805b8e5c5efae41114333a6ee9afb7` |
| vsco:susCymb1-scrape2_v1 | Percussion/susCymb1-scrape2_v1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `8e8f2287d479acc0f8620c3a1708ab112c170cdb0d5d009eb0d96255767f7921` |
| vsco:tamb2_rollSlow | Miscellania Raw/Misc 2/tamb2_rollSlow.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `ec1ad587afa44b4485e0eaba585414568abe7c85883a1adb6bc2bc21f68ceefc` |
| vsco:vibraring1 | Miscellania Raw/Misc 1/vibraring1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `ccc78653850d959fb7f5769622331a73a92d4de0d46cf1e1e4cc8cf2b8f3de61` |
| vsco:vibraring3 | Miscellania Raw/Misc 1/vibraring3.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `3f09cad46a5d1e0a5182dae7e654a6261bf9a5c18e71d67086c36005e1cf93e5` |
| vsco:vibraring_v1_rr1 | Percussion/temp/vibraring_v1_rr1.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `61debad8cacf593b09c2d00e44e468b5170975300b80ff89ccfce3327e946205` |
| vsco:vibraring_v1_rr2 | Percussion/temp/vibraring_v1_rr2.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `0b80f5a636adcb1acf5271cc95fadaeae59b24cc8ca7c68738e349c0391e3763` |
| vsco:zap11 | Miscellania Raw/Misc 1/zap11.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `57d259c340e2f9c9319cd39df0f9ebecc8286bee8314a7ff2a7e8a1400c43460` |
| vsco:zap12 | Miscellania Raw/Misc 1/zap12.wav | Versilian Studios | VSCO 2 Community Edition 1.1.0 (https://versilian-studios.com/vsco-community/) | `93a17b3f9d5679eb6b44993f175e9624cfff8f57b6368705f6fa3f51e5af6bd3` |

- Runtime file: `Assets/Sounds/AzureCathedral/PrisonBreak.ogg`
- Asset ID: azure-sfx2-prison-break-20261002
- Asset type: stereo44.1kHz Vorbis 2.21s Cathedral cue: Liora's ice prison cracking and shattering (opening)
- Creator: recordings by discofield, magnuswaker and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:impact-FS522099-magnuswaker-concrete_smash_2, cc0:impact-FS711657-discofield-stone_crash, vsco:brick_scrape2, vsco:chain_grind, vsco:glass_break, vsco:glass_break2, vsco:glass_break3, vsco:glass_break4, vsco:glass_break5, vsco:glass_break6, vsco:glass_break7, vsco:glass_break8, vsco:sleighbell1_hit_3, vsco:sleighbell1_hit_quiet, vsco:vibraring_v1_rr2 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `785ae77edebcba73a7509fb27996782b209796ad4cad123f496ccfa1c3c5708e`

- Runtime file: `Assets/Sounds/AzureCathedral/SwordLight.ogg`
- Asset ID: azure-sfx2-sword-light-20261002
- Asset type: stereo44.1kHz Vorbis 2.51s Cathedral cue: Liora raising the sword and the light pillar blooming (opening)
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:BellTree_Stroke4_v1_Sum, vsco:TB_hit_C4_v4_rr1, vsco:Triangle6-HitM_v1_rr2_Sum, vsco:glock_fx_up_chromatic_fast_01, vsco:susCymb1-cresc-Short_v1 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `6f626464e159bfb856193dd54d15400f0819e68b8ec1668ec22384f4cdc398f3`

- Runtime file: `Assets/Sounds/AzureCathedral/RiftOpen.ogg`
- Asset ID: azure-sfx2-rift-open-20261002
- Asset type: stereo44.1kHz Vorbis 2.01s Cathedral cue: the dimensional rift tearing open (opening)
- Creator: recordings by AudioPapkin, DARTEKZ_GAMEZ, Kenney and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:drawKnife3, cc0:impact-FS541029-AudioPapkin-very_low_impact, cc0:knifeSlice, cc0:wind-FS719560-DARTEKZ_GAMEZ-wind_whirl, vsco:bassdrum_rub3_v1, vsco:chain_grind, vsco:cymbal-crash1_mf_rr2, vsco:glass_break5, vsco:glass_break7, vsco:metal_hit9, vsco:zap11, vsco:zap12 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `46c98be571eae8487267074824347393ffbb0c6193fb69264a8f35544465aa89`

- Runtime file: `Assets/Sounds/AzureCathedral/WormArrival.ogg`
- Asset ID: azure-sfx2-worm-arrival-20261002
- Asset type: stereo44.1kHz Vorbis 3.01s Cathedral cue: Vitrion emerging head first with a low roar and grinding glass plates (opening)
- Creator: recordings by AudioPapkin, Bananaboatman33, discofield, joelcarrsound and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:impact-FS541029-AudioPapkin-very_low_impact, cc0:impact-FS711657-discofield-stone_crash, cc0:roar-FS257635-Bananaboatman33-demon_giant_howl, cc0:roar-FS521830-joelcarrsound-war_cry, vsco:brick_scrape, vsco:chain_grind, vsco:glass_break8, vsco:gongHit_p, vsco:gongscrape_mf in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `da4fe9bd0e804b113ec5676f40ee04978b0b8c9e8f225d2be8505ebcdbfb5301`

- Runtime file: `Assets/Sounds/AzureCathedral/LioraFall.ogg`
- Asset ID: azure-sfx2-liora-fall-20261002
- Asset type: stereo44.1kHz Vorbis 2.01s Cathedral cue: Liora's defeat: cracking glass sword and falling shards
- Creator: recordings by florianreichelt, inoshirodesign, magnuswaker and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:bell-FS271370-inoshirodesign-singing_bowl, cc0:impact-FS522099-magnuswaker-concrete_smash_2, cc0:wind-FS683096-florianreichelt-woosh, vsco:glass_break2, vsco:glass_break4, vsco:glass_break6, vsco:glass_break7, vsco:glock_fx_down_chromatic_fast_01, vsco:metal_hit11, vsco:sleighbell1_hit_quiet in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `a592173a05480f39f99406e1f0ffaa8749be4f6363fbde3654358a26b0bd8325`

- Runtime file: `Assets/Sounds/AzureCathedral/WormRetreat.ogg`
- Asset ID: azure-sfx2-worm-retreat-20261002
- Asset type: stereo44.1kHz Vorbis 2.01s Cathedral cue: Vitrion withdrawing at its 20% floor
- Creator: recordings by AudioPapkin, Bananaboatman33 and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:impact-FS541029-AudioPapkin-very_low_impact, cc0:roar-FS257635-Bananaboatman33-demon_giant_howl, vsco:bassdrum_rub1_v1, vsco:brick_scrape, vsco:chain_grind, vsco:gongscrape_pp in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `9400d50973dcece03c2116de9456b337f3348ffba2226ee4943496870217c41d`

- Runtime file: `Assets/Sounds/AzureCathedral/DevourRush.ogg`
- Asset ID: azure-sfx2-devour-rush-20261002
- Asset type: stereo44.1kHz Vorbis 2.01s Cathedral cue: the devouring rush building toward the bite
- Creator: recordings by DARTEKZ_GAMEZ, florianreichelt and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:wind-FS683096-florianreichelt-woosh, cc0:wind-FS719560-DARTEKZ_GAMEZ-wind_whirl, vsco:ambience1, vsco:bassdrum_rub2_v1, vsco:bassdrum_rub3_v1, vsco:chaingrindLoop, vsco:gongscrape_mf, vsco:susCymb1-bow-1, vsco:susCymb1-bow-3 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `412a8f9f46417d7b7263c31c0aabd43e0b7e4f1f44f08401edd348fe06841802`

- Runtime file: `Assets/Sounds/AzureCathedral/DevourBite.ogg`
- Asset ID: azure-sfx2-devour-bite-20261002
- Asset type: stereo44.1kHz Vorbis 2.51s Cathedral cue: the devouring bite: crushing glass, sub impact and silence
- Creator: recordings by _stubb, AudioPapkin, Bananaboatman33, discofield, magnuswaker and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:impact-FS389618-_stubb-rock_tumble_2, cc0:impact-FS522099-magnuswaker-concrete_smash_2, cc0:impact-FS541029-AudioPapkin-very_low_impact, cc0:impact-FS711657-discofield-stone_crash, cc0:roar-FS257635-Bananaboatman33-demon_giant_howl, vsco:BDrumNewhit_v7_rr1_Sum, vsco:glass_break3, vsco:glass_break4, vsco:glass_break5, vsco:glass_break7, vsco:metal_hit7, vsco:vibraring_v1_rr1 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `812851fddf3ce647917f4e63fd6db4b0d0afb07d3fc561499025a41e4f52b9e6`

- Runtime file: `Assets/Sounds/AzureCathedral/FuryAwaken.ogg`
- Asset ID: azure-sfx2-fury-awaken-20261002
- Asset type: stereo44.1kHz Vorbis 3.01s Cathedral cue: Vitrion's Fury awakening: roar, rising crystal glitter and bass
- Creator: recordings by AudioPapkin, Bananaboatman33, joelcarrsound and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:impact-FS541029-AudioPapkin-very_low_impact, cc0:roar-FS257635-Bananaboatman33-demon_giant_howl, cc0:roar-FS521830-joelcarrsound-war_cry, vsco:BellTree_Stroke1_v1_Sum, vsco:TB_hit_C4_v4_rr1, vsco:glass_break6, vsco:glock_fx_up_chromatic_med_01, vsco:gongHit_p in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `393d6e4222f9bacfa42958b91ae80eeb25803876c55ad9fcea6afe434c48765b`

- Runtime file: `Assets/Sounds/AzureCathedral/FinalBlow.ogg`
- Asset ID: azure-sfx2-final-blow-20261002
- Asset type: stereo44.1kHz Vorbis 2.51s Cathedral cue: the final blow on the enraged worm: impact and chained cracks
- Creator: recordings by AudioPapkin, Bananaboatman33, discofield, inoshirodesign, magnuswaker and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:bell-FS271370-inoshirodesign-singing_bowl, cc0:impact-FS522099-magnuswaker-concrete_smash_2, cc0:impact-FS541029-AudioPapkin-very_low_impact, cc0:impact-FS711657-discofield-stone_crash, cc0:roar-FS257635-Bananaboatman33-demon_giant_howl, vsco:glass_break2, vsco:glass_break3, vsco:glass_break4, vsco:glass_break6, vsco:glass_break7, vsco:glass_break8 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `fe1e32457fee5d4473d98655de75fed8389ebdcdfaa3df9e2994cb9041b0c7c4`

- Runtime file: `Assets/Sounds/AzureCathedral/MeltRush.ogg`
- Asset ID: azure-sfx2-melt-rush-20261002
- Asset type: stereo44.1kHz Vorbis 1.61s Cathedral cue: the harmless melting rush after Victory
- Creator: recordings by DARTEKZ_GAMEZ, PorkMuncher and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:swing-FS263595-PorkMuncher-swoosh, cc0:wind-FS719560-DARTEKZ_GAMEZ-wind_whirl, vsco:Triangle6-HitFM_v1_rr1_Sum, vsco:bassdrum_rub4_v1, vsco:bubbles4, vsco:vibraring_v1_rr1 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `fff42c1dffa3fdeb25bd6734dfa351c5733a3828a275a6a61432c1ed79167570`

- Runtime file: `Assets/Sounds/AzureCathedral/MeltContact.ogg`
- Asset ID: azure-sfx2-melt-contact-20261002
- Asset type: stereo44.1kHz Vorbis 3.01s Cathedral cue: glass softening at melt contact: frost creak and water
- Creator: recordings by DARTEKZ_GAMEZ, inoshirodesign, nahmandub and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:bell-FS131348-nahmandub-daitokuji_bell, cc0:bell-FS271370-inoshirodesign-singing_bowl, cc0:wind-FS719560-DARTEKZ_GAMEZ-wind_whirl, vsco:brick_scrape2, vsco:bubbles2, vsco:chain_grind, vsco:glass_break4, vsco:glass_break7 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `20d9ac760550d90275bb7a210a04ef1afb34f4f299b64f1bbe49aab48c409f99`

- Runtime file: `Assets/Sounds/AzureCathedral/ChainMelt.ogg`
- Asset ID: azure-sfx2-chain-melt-20261002
- Asset type: stereo44.1kHz Vorbis 3.01s Cathedral cue: the whole chain melting: drips and creaks
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:bassdrum_rub4_v1, vsco:brick_scrape2, vsco:bubbles, vsco:bubbles4, vsco:chain_grind, vsco:glass_break7 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `afe8db328d4e91428e36856fb95e26b777ef6cdf936c567ef1d486fb98af6824`

- Runtime file: `Assets/Sounds/AzureCathedral/Victory.ogg`
- Asset ID: azure-sfx2-victory-20261002
- Asset type: stereo44.1kHz Vorbis 5.01s Cathedral cue: victory: descending tuned tubular bells and a long glitter tail
- Creator: recordings by inoshirodesign and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:bell-FS271370-inoshirodesign-singing_bowl, vsco:BellTree_Stroke2_v1_Sum, vsco:TB_hit_C4_v4_rr1, vsco:Triangle6-Hit_v1_rr2_Sum, vsco:glock_fx_down_chromatic_fast_01, vsco:glock_medium_C6, vsco:glock_medium_G5, vsco:sleighbell1_hit_3, vsco:sleighbell1_hit_quiet in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `bb8a36200544d47d2cd7d20a2dbc0ebbe3963d4fbd18fe493db1760baf193553`

- Runtime file: `Assets/Sounds/AzureCathedral/FanCharge.ogg`
- Asset ID: azure-sfx2-fan-charge-20261002
- Asset type: stereo44.1kHz Vorbis 1.01s Cathedral cue: icicle fan forecast swell
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:glock_fx_down_chromatic_fast_03, vsco:susCymb1-bow-2, vsco:susCymb1-cresc-Short_v1, vsco:vibraring3 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `deece4a5b0b89ce80b6eecf14f9d681b668456f566c807f32090680ddd2f3de4`

- Runtime file: `Assets/Sounds/AzureCathedral/FanRelease1.ogg`
- Asset ID: azure-sfx2-fan-release1-20261002
- Asset type: stereo44.1kHz Vorbis 0.61s Cathedral cue: icicle fan release: crystal shing, air cut and small body
- Creator: recordings by Euphrosyyn, Kenney, qubodup and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:drawKnife3, cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2, cc0:wind-FS60030-qubodup-air_cut, vsco:Marimba_hit_Outrigger_G4_loud_01, vsco:glass_break5, vsco:glock_fx_up_chromatic_fast_01 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `226e15b729ecf47944bff80fc684b5927ca0aca3431d77c63c9c39ba9e741977`

- Runtime file: `Assets/Sounds/AzureCathedral/FanRelease2.ogg`
- Asset ID: azure-sfx2-fan-release2-20261002
- Asset type: stereo44.1kHz Vorbis 0.61s Cathedral cue: icicle fan release: crystal shing, air cut and small body
- Creator: recordings by Euphrosyyn, greyfeather, Kenney, Versilian Studios and xkeril; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:drawKnife3, cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2, cc0:ring-FS706204-xkeril-nice_anime_sword_hit, cc0:swing-FS724716-greyfeather-sword_slash_energy_wave, vsco:Xylo_Medium_G4_ff_01_far, vsco:glass_break3, vsco:glock_fx_up_chromatic_fast_02 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `7a66846783dd98f15de1cb2641caa81af6c073245f3330d560fa3e0a0947f3b6`

- Runtime file: `Assets/Sounds/AzureCathedral/FanRelease3.ogg`
- Asset ID: azure-sfx2-fan-release3-20261002
- Asset type: stereo44.1kHz Vorbis 0.61s Cathedral cue: icicle fan release: crystal shing, air cut and small body
- Creator: recordings by Euphrosyyn, Kenney, qubodup and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:drawKnife3, cc0:knifeSlice2, cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2, cc0:wind-FS60030-qubodup-air_cut, vsco:Marimba_hit_Outrigger_G4_loud_01, vsco:glass_break2, vsco:glock_fx_up_pentatonic_med_02 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `3121de946e76a8059f4e3dedb33bf60f5192eda9495751e7593358bf13b753d9`

- Runtime file: `Assets/Sounds/AzureCathedral/RainCharge.ogg`
- Asset ID: azure-sfx2-rain-charge-20261002
- Asset type: stereo44.1kHz Vorbis 1.08s Cathedral cue: glass rain forecast glitter
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:BellTree_Stroke3_v1_Sum, vsco:Triangle6-Roll_v2_rr1_Sum, vsco:Xylo_Medium_C7_ff_01_far, vsco:glock_medium_C7, vsco:glock_medium_G6, vsco:susCymb1-bow-2, vsco:vibraring3 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `ab26e32240f3ee312966e7a2633cf5781129b575869fac598ef118d168894d3c`

- Runtime file: `Assets/Sounds/AzureCathedral/RainRelease1.ogg`
- Asset ID: azure-sfx2-rain-release1-20261002
- Asset type: stereo44.1kHz Vorbis 0.91s Cathedral cue: glass rain release: descending crystal cascade
- Creator: recordings by artisticdude and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:swish-10, vsco:Marimba_hit_Outrigger_G4_loud_01, vsco:TB_hit_C5_v4_rr1, vsco:Triangle3-Hit_v2_rr1_Sum, vsco:glass_break7, vsco:glock_fx_down_chromatic_fast_02 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `0d60fabb030633bf7bcb5751768e54aa6cf23283a62296e2b88a003bd200b911`

- Runtime file: `Assets/Sounds/AzureCathedral/RainRelease2.ogg`
- Asset ID: azure-sfx2-rain-release2-20261002
- Asset type: stereo44.1kHz Vorbis 0.91s Cathedral cue: glass rain release: descending crystal cascade
- Creator: recordings by artisticdude and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:swish-12, vsco:TB_hit_G4_v4_rr1, vsco:Triangle6-Hit_v2_rr1_Sum, vsco:Xylo_Medium_G4_ff_01_far, vsco:glass_break3, vsco:glock_fx_down_pentatonic_med_01 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `eedbb759a0f474fd2bf443ab90e29c06cdd412274bead903ef66ff82ff77ab9f`

- Runtime file: `Assets/Sounds/AzureCathedral/RainRelease3.ogg`
- Asset ID: azure-sfx2-rain-release3-20261002
- Asset type: stereo44.1kHz Vorbis 0.91s Cathedral cue: glass rain release: descending crystal cascade
- Creator: recordings by artisticdude and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:swish-11, vsco:Marimba_hit_Outrigger_G4_loud_01, vsco:TB_hit_F5_v3_rr1, vsco:Triangle3-Hit_v2_rr2_Sum, vsco:glass_break6, vsco:glock_fx_down_chromatic_fast_04 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `60f4020ddd3d4c3ef44115a40b402ab2fee24c5b4adad105f7193e370e70db06`

- Runtime file: `Assets/Sounds/AzureCathedral/BeamCharge.ogg`
- Asset ID: azure-sfx2-beam-charge-20261002
- Asset type: stereo44.1kHz Vorbis 1.61s Cathedral cue: sword beam charge swell
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:glock_fx_down_chromatic_fast_03, vsco:susCymb1-bow-2, vsco:susCymb1-cresc-Median_v1, vsco:vibraring3 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `7a763b4f427432e69059f9802cc5216669514b7bc37298804486963e04632302`

- Runtime file: `Assets/Sounds/AzureCathedral/BeamFire.ogg`
- Asset ID: azure-sfx2-beam-fire-20261002
- Asset type: stereo44.1kHz Vorbis 1.01s Cathedral cue: sword beam ignition
- Creator: recordings by greyfeather, Versilian Studios and xkeril; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:ring-FS706204-xkeril-nice_anime_sword_hit, cc0:swing-FS724716-greyfeather-sword_slash_energy_wave, vsco:TB_hit_C5_v4_rr1, vsco:cymbal-crashshort_v1, vsco:glass_break5, vsco:glock_fx_up_chromatic_fast_02, vsco:susCymb1-hit-bell_fff in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `aefb37b04198482d61adec0b2bf2a0f7237e8d4e3258da7a5e719d526ae76387`

- Runtime file: `Assets/Sounds/AzureCathedral/BeamSweep.ogg`
- Asset ID: azure-sfx2-beam-sweep-20261002
- Asset type: stereo44.1kHz Vorbis 3.01s Cathedral cue: sword beam three-second sweep bed
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:Triangle3-Roll_v2_rr1_Sum, vsco:glock_medium_C6, vsco:glock_medium_G6, vsco:susCymb1-bow-3, vsco:susCymb1-scrape2_v1 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `af53b0251baef348fdd310b5071e3b0493fbd898abccdf5de8af2a3d213b56c8`

- Runtime file: `Assets/Sounds/AzureCathedral/CutCharge.ogg`
- Asset ID: azure-sfx2-cut-charge-20261002
- Asset type: stereo44.1kHz Vorbis 1.01s Cathedral cue: spatial cut forecast tension
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:Triangle6-Roll_v2_rr1_Sum, vsco:glock_fx_down_chromatic_fast_03, vsco:susCymb1-bow-2, vsco:susCymb1-scrape1_v1 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `8dde7eae770c41104a0c0d529fcea51d0668d7d505d5a0bc628e60d0985f91fd`

- Runtime file: `Assets/Sounds/AzureCathedral/CutRelease1.ogg`
- Asset ID: azure-sfx2-cut-release1-20261002
- Asset type: stereo44.1kHz Vorbis 0.51s Cathedral cue: spatial cut: thin tear, blade shing and light body
- Creator: recordings by Euphrosyyn, Kenney, qubodup and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:drawKnife3, cc0:knifeSlice, cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2, cc0:wind-FS60030-qubodup-air_cut, vsco:Xylo_Medium_G4_ff_01_far, vsco:glass_break6 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `f530009c292b8e45de6dec05bb62cf345c07a5de5276df68d0ee10d64af2a66d`

- Runtime file: `Assets/Sounds/AzureCathedral/CutRelease2.ogg`
- Asset ID: azure-sfx2-cut-release2-20261002
- Asset type: stereo44.1kHz Vorbis 0.51s Cathedral cue: spatial cut: thin tear, blade shing and light body
- Creator: recordings by Kenney, qubodup and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:drawKnife2, cc0:drawKnife3, cc0:metal-FS442769-qubodup-sword_hit, cc0:wind-FS60030-qubodup-air_cut, vsco:Marimba_hit_Outrigger_G4_loud_01, vsco:glass_break2 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `ccf3e646d446873dedf7a2a06d5eec16553c80690593abb377bf48377d9ac490`

- Runtime file: `Assets/Sounds/AzureCathedral/LioraHit1.ogg`
- Asset ID: azure-sfx2-liora-hit1-20261002
- Asset type: stereo44.1kHz Vorbis 0.21s Cathedral cue: small crystal tick when Liora is hit
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:Marimba_hit_Outrigger_G4_loud_01, vsco:glass_break6, vsco:glock_medium_G5 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `a172cd80df9212768d1d749e5c4f34f8b73df4a4f4c85aa4abaf784bbb7dd735`

- Runtime file: `Assets/Sounds/AzureCathedral/LioraHit2.ogg`
- Asset ID: azure-sfx2-liora-hit2-20261002
- Asset type: stereo44.1kHz Vorbis 0.21s Cathedral cue: small crystal tick when Liora is hit
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:Xylo_Medium_G4_ff_01_far, vsco:glass_break4, vsco:glock_medium_C6 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `33d814ffe91c4e7cee050932213406340d0a877e6d4ea6808138b9f2412997d6`

- Runtime file: `Assets/Sounds/AzureCathedral/RushWarn.ogg`
- Asset ID: azure-sfx2-rush-warn-20261002
- Asset type: stereo44.1kHz Vorbis 1.68s Cathedral cue: worm rush forecast: rising growl and grinding
- Creator: recordings by AudioPapkin, Bananaboatman33 and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:impact-FS541029-AudioPapkin-very_low_impact, cc0:roar-FS257635-Bananaboatman33-demon_giant_howl, vsco:brick_scrape, vsco:brick_scrape2, vsco:chain_grind, vsco:gongscrape_mf in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `7c70f8b5b0d0b1040ae05d9b2d7ae16449d0391b7d2be936ae7535557bccb64c`

- Runtime file: `Assets/Sounds/AzureCathedral/RushPass.ogg`
- Asset ID: azure-sfx2-rush-pass-20261002
- Asset type: stereo44.1kHz Vorbis 1.41s Cathedral cue: worm rush pass: mass whoosh, glass grind and roar burst
- Creator: recordings by AudioPapkin, Bananaboatman33, florianreichelt, magnuswaker, PorkMuncher and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:impact-FS522099-magnuswaker-concrete_smash_2, cc0:impact-FS541029-AudioPapkin-very_low_impact, cc0:roar-FS257635-Bananaboatman33-demon_giant_howl, cc0:swing-FS263595-PorkMuncher-swoosh, cc0:wind-FS683096-florianreichelt-woosh, vsco:brick_scrape2, vsco:chain_grind, vsco:glass_break5, vsco:gongscrape_mf in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `68f25399bde0c0bf3107a257d4286d35494b6ce58592b6fd60c9c04e3c1f16bc`

- Runtime file: `Assets/Sounds/AzureCathedral/MissileVolley.ogg`
- Asset ID: azure-sfx2-missile-volley-20261002
- Asset type: stereo44.1kHz Vorbis 1.51s Cathedral cue: Fury segment volley launch
- Creator: recordings by artisticdude, DARTEKZ_GAMEZ, florianreichelt, PorkMuncher and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:swing-FS263595-PorkMuncher-swoosh, cc0:swish-10, cc0:swish-11, cc0:swish-12, cc0:swish-13, cc0:wind-FS683096-florianreichelt-woosh, cc0:wind-FS719560-DARTEKZ_GAMEZ-wind_whirl, vsco:Triangle6-HitFM_v2_rr1_Sum, vsco:glass_break7, vsco:glass_break8, vsco:glock_fx_up_chromatic_fast_02, vsco:tamb2_rollSlow in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `b3448d49c3e23ecf2e2034840e5ea4cbbd21434d5336c0c79ac5695e80c9bf1b`

- Runtime file: `Assets/Sounds/AzureCathedral/WormHit1.ogg`
- Asset ID: azure-sfx2-worm-hit1-20261002
- Asset type: stereo44.1kHz Vorbis 0.26s Cathedral cue: glass armor clink when a worm segment is hit
- Creator: recordings by Anthousai, Kenney and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:bell-FS405665-Anthousai-metal_bowl_hit, cc0:metalPot1, vsco:glass_break2 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `b65c81262425d785c81dad8d5996b6e220da7641214fedf4ea13744f35f1ff58`

- Runtime file: `Assets/Sounds/AzureCathedral/WormHit2.ogg`
- Asset ID: azure-sfx2-worm-hit2-20261002
- Asset type: stereo44.1kHz Vorbis 0.26s Cathedral cue: glass armor clink when a worm segment is hit
- Creator: recordings by Anthousai, Kenney and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:bell-FS405665-Anthousai-metal_bowl_hit, cc0:metalPot3, vsco:glass_break4 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `997a49c69caf0798c0f7a9eedaa3f2d0d0b23e8efac9286d2afb71f024539c30`

- Runtime file: `Assets/Sounds/AzureCathedral/WormHit3.ogg`
- Asset ID: azure-sfx2-worm-hit3-20261002
- Asset type: stereo44.1kHz Vorbis 0.26s Cathedral cue: glass armor clink when a worm segment is hit
- Creator: recordings by Anthousai, Kenney and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:bell-FS405665-Anthousai-metal_bowl_hit, cc0:metalPot1, vsco:glass_break6 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `4befb3c0508fdd04b29f61ed434cf54776db7b7ead469df887f8d1d7548b03d0`

- Runtime file: `Assets/Sounds/AzureCathedral/StackCall.ogg`
- Asset ID: azure-sfx2-stack-call-20261002
- Asset type: stereo44.1kHz Vorbis 2.01s Cathedral cue: Stack call: descending figure into a low bell
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:TB_hit_C4_v4_rr1, vsco:cymb_gong, vsco:glock_medium_C5, vsco:glock_medium_G4, vsco:vibraring1, vsco:vibraring3 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `520c5d89c5ae62c000eb158768114302ed16f203128bdc8c726dbb329b5900b3`

- Runtime file: `Assets/Sounds/AzureCathedral/SpreadCall.ogg`
- Asset ID: azure-sfx2-spread-call-20261002
- Asset type: stereo44.1kHz Vorbis 2.01s Cathedral cue: Spread call: rising, dispersing figure
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:BellTree_Stroke3_v1_Sum, vsco:glock_medium_C5, vsco:glock_medium_G4, vsco:glock_medium_G5, vsco:susCymb1-bow-2, vsco:vibraring1, vsco:vibraring3 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `8eae5b686356dd6b5d3c4875b9b5f573e0345478e80e7e3b66abb5d027c595bd`

- Runtime file: `Assets/Sounds/AzureCathedral/ChorusTick1.ogg`
- Asset ID: azure-sfx2-chorus-tick1-20261002
- Asset type: stereo44.1kHz Vorbis 0.51s Cathedral cue: chorus countdown crystal tick
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:Claves1_Hit_v2_rr1_Sum, vsco:glock_medium_G4, vsco:vibraring1 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `2252894450ebf3a01a48e2034a5a3baa7eaeb1766ba5c161ef3f25ac2f097166`

- Runtime file: `Assets/Sounds/AzureCathedral/ChorusTick2.ogg`
- Asset ID: azure-sfx2-chorus-tick2-20261002
- Asset type: stereo44.1kHz Vorbis 0.51s Cathedral cue: chorus countdown crystal tick
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:Claves1_Hit_v2_rr2_Sum, vsco:glock_medium_C5, vsco:vibraring3 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `86626d57ddb2152280af9d69b2eb14b8cbc31c89329fda94dcb6af6d4fafcd3d`

- Runtime file: `Assets/Sounds/AzureCathedral/ChorusTick3.ogg`
- Asset ID: azure-sfx2-chorus-tick3-20261002
- Asset type: stereo44.1kHz Vorbis 0.51s Cathedral cue: chorus countdown crystal tick
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:Claves1_Hit_v3_rr1_Sum, vsco:glock_medium_G5, vsco:vibraring1 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `c1d785a49543f5f18c20adf7769172aee1b7e6b074266ff0d8611da5f3daddf7`

- Runtime file: `Assets/Sounds/AzureCathedral/StackHold.ogg`
- Asset ID: azure-sfx2-stack-hold-20261002
- Asset type: stereo44.1kHz Vorbis 1.51s Cathedral cue: Stack success: resolving chime and released ice
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:TB_hit_G4_v4_rr1, vsco:glass_break5, vsco:glock_medium_C5, vsco:glock_medium_G4, vsco:vibraring1, vsco:vibraring3 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `5097f952893984b318d36dee5d19a637c21647b70bb6bee00f0101fd5198ab7a`

- Runtime file: `Assets/Sounds/AzureCathedral/StackShatter.ogg`
- Asset ID: azure-sfx2-stack-shatter-20261002
- Asset type: stereo44.1kHz Vorbis 1.51s Cathedral cue: Stack failure: crushing ice jaws
- Creator: recordings by AudioPapkin, discofield, magnuswaker and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:impact-FS522099-magnuswaker-concrete_smash_2, cc0:impact-FS541029-AudioPapkin-very_low_impact, cc0:impact-FS711657-discofield-stone_crash, vsco:glass_break3, vsco:glass_break7, vsco:metal_hit9, vsco:vibraring1, vsco:vibraring3 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `97ecac3e226b998b48b7c2dc00918233c4a5c3cec49a1573ae09f53de5b8ffb3`

- Runtime file: `Assets/Sounds/AzureCathedral/SpreadFade.ogg`
- Asset ID: azure-sfx2-spread-fade-20261002
- Asset type: stereo44.1kHz Vorbis 1.21s Cathedral cue: Spread success: fading shimmer
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:BellTree_Stroke3_v1_Sum, vsco:glock_medium_C5, vsco:glock_medium_G5, vsco:susCymb1-bow-2, vsco:vibraring1 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `f7ddf08eafcf408831766a2f82afbb3226e6db8ddb973d50705e5bdf0bcdefd9`

- Runtime file: `Assets/Sounds/AzureCathedral/SpreadPierce.ogg`
- Asset ID: azure-sfx2-spread-pierce-20261002
- Asset type: stereo44.1kHz Vorbis 1.21s Cathedral cue: Spread failure: light sword piercing
- Creator: recordings by Euphrosyyn, greyfeather, magnuswaker, nekoninja and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:impact-FS522099-magnuswaker-concrete_smash_2, cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2, cc0:swing-FS370204-nekoninja-samurai_slash, cc0:swing-FS724716-greyfeather-sword_slash_energy_wave, vsco:glass_break6, vsco:glock_medium_C5, vsco:vibraring1 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `a91bda03c8818e8ad8cf5e66f7b2799151a75a6c65cb1ec699d86daeeea7359b`

- Runtime file: `Assets/Sounds/AzureCathedral/Downed.ogg`
- Asset ID: azure-sfx2-downed-20261002
- Asset type: stereo44.1kHz Vorbis 1.51s Cathedral cue: a member Downed: freezing crackle and low hit
- Creator: recordings by AudioPapkin, magnuswaker and Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:impact-FS522099-magnuswaker-concrete_smash_2, cc0:impact-FS541029-AudioPapkin-very_low_impact, vsco:chain_grind, vsco:glass_break4, vsco:glock_fx_down_chromatic_fast_02, vsco:susCymb1-scrape1_v1, vsco:vibraring1 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `cc84d4758ecfe201bef82adaa3bf769c21741529a95a3e13fcbaf5a845a5c833`

- Runtime file: `Assets/Sounds/AzureCathedral/Revived.ogg`
- Asset ID: azure-sfx2-revived-20261002
- Asset type: stereo44.1kHz Vorbis 2.01s Cathedral cue: a member revived: thaw and rising bells
- Creator: recordings by Versilian Studios; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: vsco:bubbles2, vsco:glass_break2, vsco:glock_medium_C5, vsco:glock_medium_G4, vsco:glock_medium_G5, vsco:susCymb1-bow-2, vsco:vibraring1, vsco:vibraring3 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `6504931ee1fd12f4b9ea84e4e6eb7db4e4317407aba7889d98c087ed4b8fca0c`

- Runtime file: `Assets/Sounds/AzureCathedral/LatticeVolley.ogg`
- Asset ID: azure-sfx2-lattice-volley-20261002
- Asset type: stereo44.1kHz Vorbis 1.11s Cathedral cue: slash lattice head hit (Doll ChargeRush core with glass and shing)
- Creator: recordings by Euphrosyyn and Versilian Studios; the project-owned Doll ChargeRush cue; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2, repo:FirstSeverance/Beams/ChargeRush, vsco:glass_break5, vsco:glass_break7 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `94d59383d48be213c9df6669e52c7216411272bd739f4194a490ddbf26a6292a`

- Runtime file: `Assets/Sounds/AzureCathedral/LatticeSlice1.ogg`
- Asset ID: azure-sfx2-lattice-slice1-20261002
- Asset type: stereo44.1kHz Vorbis 0.21s Cathedral cue: one slash-lattice line (ChargeRush staccato core with a glass crack)
- Creator: recordings by Euphrosyyn and Versilian Studios; the project-owned Doll ChargeRush cue; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2, repo:FirstSeverance/Beams/ChargeRush, vsco:glass_break5 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `8dca81eded9cc4e1a8e987cab3e6c6377ca4c90c6e8c87b766373dc1d166264e`

- Runtime file: `Assets/Sounds/AzureCathedral/LatticeSlice2.ogg`
- Asset ID: azure-sfx2-lattice-slice2-20261002
- Asset type: stereo44.1kHz Vorbis 0.21s Cathedral cue: one slash-lattice line (ChargeRush staccato core with a glass crack)
- Creator: recordings by Euphrosyyn and Versilian Studios; the project-owned Doll ChargeRush cue; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2, repo:FirstSeverance/Beams/ChargeRush, vsco:glass_break3 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `bad48f2b38006a2cc599bee0d2bfdf33fc8b85040c1955046abe1257c31e898c`

- Runtime file: `Assets/Sounds/AzureCathedral/LatticeSlice3.ogg`
- Asset ID: azure-sfx2-lattice-slice3-20261002
- Asset type: stereo44.1kHz Vorbis 0.21s Cathedral cue: one slash-lattice line (ChargeRush staccato core with a glass crack)
- Creator: recordings by Euphrosyyn and Versilian Studios; the project-owned Doll ChargeRush cue; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2, repo:FirstSeverance/Beams/ChargeRush, vsco:glass_break in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `4ec5876126e27ba7182da351e5c0ef9d9c844ad489db9533d889f833575401d6`

- Runtime file: `Assets/Sounds/AzureCathedral/LatticeSlice4.ogg`
- Asset ID: azure-sfx2-lattice-slice4-20261002
- Asset type: stereo44.1kHz Vorbis 0.21s Cathedral cue: one slash-lattice line (ChargeRush staccato core with a glass crack)
- Creator: recordings by Euphrosyyn and Versilian Studios; the project-owned Doll ChargeRush cue; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2, repo:FirstSeverance/Beams/ChargeRush, vsco:glass_break8 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `c87ecb2fc9d051d61ec1047b6205978bf4b22f7ea3fb2597ed749573bd613023`

- Runtime file: `Assets/Sounds/AzureCathedral/LatticeEnd.ogg`
- Asset ID: azure-sfx2-lattice-end-20261002
- Asset type: stereo44.1kHz Vorbis 1.01s Cathedral cue: slash lattice closing hit and falling shards
- Creator: recordings by Euphrosyyn and Versilian Studios; the project-owned Doll ChargeRush cue; layering by Convergence with owner-directed Claude assistance
- Creation/acquisition date: 2026-10-02
- Source type: public-domain
- Source work and URL: cc0:ring-FS529019-Euphrosyyn-anime_shing_sword_2, repo:FirstSeverance/Beams/ChargeRush, vsco:glass_break2, vsco:glass_break7, vsco:glass_break8 in the table above
- Tool/model/version: `tools/remix_azure_sfx.py` with `tools/sfx_layers.py`; NumPy2.4.4, SciPy1.16.1, soundfile0.14.0/libsndfile1.2.2 Vorbis at compression level0.4, pinned Ogg serial
- Human modifications: Recordings resampled to44.1kHz, trimmed, filtered, pitch-shifted by resampling, reversed where noted, layered on the accepted Cathedral event clocks and loudness-matched; risers, shimmer, drips and low bodies are original deterministic synthesis. Recordings and WAV previews stay in the local source store and are excluded from distribution.
- License and redistribution terms: CC0 1.0 recordings and project-owned material; the layered cue follows the existing project asset terms
- Required attribution: none required by CC0; retain the table above as courtesy credit
- Reviewer and review date: Claude,2026-10-02; source licenses, timing, loudness, true peak and boundaries checked numerically; owner approved the set by listening page2026-10-02; in-game mix remains owner-owned
- SHA256: `bcec9f0068065f2379dbe1f5f398e1627ba7c2e5ea1855c3a59f457ec2e0be67`

### Azure material revision — 2026-09-26

Original code-authored materials; no external samples or texture extraction. Original Vitrion/Liora PNGs and music remain unchanged. The latest project-owned Scarlet renderer supplies techniques (masked emission, attached lagging ribbons), not copied red anatomy. Its seven synthesized ice/glass cues were retired by the [recorded audio (SFX v2)](#cathedral-recorded-audio-sfx-v2--2026-10-02) above.

- Runtime file: `Assets/AutoloadedEffects/Shaders/AzureLiora.fxc`
- Asset ID: azure-liora-refraction-20260926
- Asset type: original compiled native-atlas/refraction/veil/sword material
- Creator: project-owner-directed original implementation with OpenAI
- Creation/acquisition date: 2026-09-26
- Source type: original
- Source work and URL: Repository-owned AzureLiora.fx; no external shader code
- Tool/model/version: FXC identity/options in Assets/AutoloadedEffects/Shaders/compiled.json
- Human modifications: Masked cloth caustics, premultiplied native pixel skin, flowing attached strips; original atlas retained
- License and redistribution terms: existing project original-asset terms
- Required attribution: preserve project provenance
- Reviewer and review date: Codex compiled GPU/native-pose inspection2026-09-26; owner native acceptance not_run
- SHA256: `c851c540e0d5897a3ffe711cf39dc795e72d1a36baee4149912e4411add40cda`

### Azure Cathedral — 2026-09-20

New original images use the built-in generator, exact model unknown. Full briefs: [asset brief](../docs/encounters/azure-cathedral/ASSET_BRIEF.md). Existing project-owned Doll sound/beam materials are reused by reference with feature-local gain/pitch/cyan tuning, not replaced. Original image files remain externally archived. Luminance noise textures are referenced at runtime, never vendored.

- Runtime file: `Assets/Textures/AzureCathedral/Liora.png`
- Asset ID: azure-liora-20260920
- Asset type: 192x128 eight-pose native-density pixel NPC atlas
- Creator: project-owner-directed original implementation/artwork with OpenAI
- Creation/acquisition date: 2026-09-20
- Source type: generated
- Source work and URL: Original owner-directed built-in image generation; no third-party input
- Tool/model/version: built-in image generation, exact model not exposed; ffmpeg7.1 mechanical export where noted
- Human modifications: Built-in contrast/dress revision using the prior Liora and our own DollAttendant as references; dark cobalt bodice, shaded ice skirt, retained side ponytail/eight poses. Mechanical nearest-neighbor1536x1024 to192x128 alpha-preserving export. Source SHA256 `715a869c1794879080e8442d1086e6de57716b30c141548229ee30265d875e80`; original and superseded images retained externally.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex asset/material inspection2026-09-20; owner in-game acceptance not_run
- Prompt or brief location: `docs/encounters/azure-cathedral/ASSET_BRIEF.md`
- SHA256: `a940856c1bf12ef5fde5385343740d20d08eee51d3219ed6818fb17a45aa897a`

- Runtime file: `Assets/Textures/AzureCathedral/Vitrion.png`
- Asset ID: azure-vitrion-20260920
- Asset type: 1024x1024 dorsal-view armored worm parts atlas
- Creator: project-owner-directed original implementation/artwork with OpenAI
- Creation/acquisition date: 2026-09-20
- Source type: generated
- Source work and URL: Original owner-directed built-in image generation; no third-party input
- Tool/model/version: built-in image generation, exact model not exposed; ffmpeg7.1 mechanical export where noted
- Human modifications: 2026-09-22 built-in edit of project-owned atlas: intact beetle-like crown and streamlined armor; nearest-neighbor1254x1254 to1024x1024 export. Shader mirrors measured spines. Source SHA256 `97d416ecec5aab8feeb45b7cea2e9e9756a92952c1072ace83787023fe65354c`; previous originals retained externally/Git.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex asset/material inspection2026-09-20; owner in-game acceptance not_run
- Prompt or brief location: `docs/encounters/azure-cathedral/ASSET_BRIEF.md`
- SHA256: `af003eeb78c1dae5005e1b650530c725d733fd577fe1c68409329dbd814c0229`

- Runtime file: `Assets/Textures/AzureCathedral/VitrionFury.png`
- Asset ID: azure-vitrion-fury-20260921
- Asset type: 1024x1024 dedicated second-form worm parts atlas
- Creator: project-owner-directed original implementation/artwork with OpenAI
- Creation/acquisition date: 2026-09-21
- Source type: generated
- Source work and URL: Built-in edit of project-owned Vitrion atlas; no third-party image input
- Tool/model/version: built-in image generation, exact model unavailable; ffmpeg7.1 mechanical export
- Human modifications: 2026-09-22 built-in edit of project-owned Fury atlas: intact crown, pressure-glass armor, separate rather than head-slice jaws; nearest-neighbor1024x1024 transparent export. Source SHA256 `ca76ec96d68f4fa61fdfcccff04602f7edca364e371971adbc4801b2be9d4d0f`; previous original retained externally/Git.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex alpha/material/offline composition inspection2026-09-21; owner game acceptance not_run
- Prompt or brief location: `docs/encounters/azure-cathedral/ASSET_BRIEF.md`
- SHA256: `75df6db33a742033e6e10466a443460b5fb4831e28e4affa82a851a9a19b0306`

- Runtime file: `Assets/Textures/AzureCathedral/VitrionMandible.png`
- Asset ID: azure-mandible-20260922
- Asset type: 384x256 transparent articulated mouth part
- Creator: project-owner-directed original artwork with OpenAI
- Creation/acquisition date: 2026-09-22
- Source type: generated
- Source work and URL: Original project mouth assembly; no third-party art input
- Tool/model/version: built-in image generation, exact model unavailable; ffmpeg7.1 mechanical export
- Human modifications: Built-in alpha extraction; top-left pincer; mirrored about authored hinge at runtime. Source1536x1024 SHA256 `6597646c47765b16e8039f701c26ee1b120fa5145018a864f2a62c2232b5a050`; ffmpeg `crop=768:512:0:0,scale=384:256:flags=neighbor`; no manual repaint. Original retained externally.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex alpha and production-shader preview inspection2026-09-22; owner game acceptance not_run
- Prompt or brief location: `docs/encounters/azure-cathedral/ASSET_BRIEF.md`
- SHA256: `85c576f746329684e2541941e93b8c622ee7372e5fa14319dd3aa4f2ab212aa4`

- Runtime file: `Assets/Textures/AzureCathedral/VitrionMouth.png`
- Asset ID: azure-throat-20260922
- Asset type: 384x256 transparent articulated mouth part
- Creator: project-owner-directed original artwork with OpenAI
- Creation/acquisition date: 2026-09-22
- Source type: generated
- Source work and URL: Original project mouth assembly; no third-party art input
- Tool/model/version: built-in image generation, exact model unavailable; ffmpeg7.1 mechanical export
- Human modifications: Built-in alpha extraction; bottom-left open toothed throat. Source1536x1024 SHA256 `6597646c47765b16e8039f701c26ee1b120fa5145018a864f2a62c2232b5a050`; ffmpeg `crop=768:512:0:512,scale=384:256:flags=neighbor`; no manual repaint. Original retained externally.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex alpha and production-shader preview inspection2026-09-22; owner game acceptance not_run
- Prompt or brief location: `docs/encounters/azure-cathedral/ASSET_BRIEF.md`
- SHA256: `e1b5876b7f9fe14049ace24678812207dbb8b04d17c549c1a433c1394ac50c4a`

- Runtime file: `Assets/Textures/AzureCathedral/VitrionMouthClosed.png`
- Asset ID: azure-throat-closed-20260922
- Asset type: 384x256 transparent articulated mouth part
- Creator: project-owner-directed original artwork with OpenAI
- Creation/acquisition date: 2026-09-22
- Source type: generated
- Source work and URL: Original project mouth assembly; no third-party art input
- Tool/model/version: built-in image generation, exact model unavailable; ffmpeg7.1 mechanical export
- Human modifications: Built-in alpha extraction; bottom-right closed throat lamella. Source1536x1024 SHA256 `6597646c47765b16e8039f701c26ee1b120fa5145018a864f2a62c2232b5a050`; ffmpeg `crop=768:512:768:512,scale=384:256:flags=neighbor`; no manual repaint. Original retained externally.
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex alpha and production-shader preview inspection2026-09-22; owner game acceptance not_run
- Prompt or brief location: `docs/encounters/azure-cathedral/ASSET_BRIEF.md`
- SHA256: `17ce1b7ba041a8eb278f8ffb4ce544b30bf996f0798021fb523f12bbca96e026`

- Runtime file: `Assets/Textures/AzureCathedral/Cathedral.png`
- Asset ID: azure-cathedral-20260920
- Asset type: 1672x941 glacial cathedral background
- Creator: project-owner-directed original implementation/artwork with OpenAI
- Creation/acquisition date: 2026-09-20
- Source type: generated
- Source work and URL: Original owner-directed built-in image generation; no third-party input
- Tool/model/version: built-in image generation, exact model not exposed; ffmpeg7.1 mechanical export where noted
- Human modifications: None; runtime-only water/refraction shading
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex asset/material inspection2026-09-20; owner in-game acceptance not_run
- Prompt or brief location: `docs/encounters/azure-cathedral/ASSET_BRIEF.md`
- SHA256: `ae41d9a8c1f2e66ef1851705f2acfc651b99b1f54cb6dad2dd00c5f8eb57f2a3`

- Runtime file: `Assets/Textures/AzureCathedral/GlacialChime.png`
- Asset ID: azure-chime-20260920
- Asset type: 48x60 item icon
- Creator: project-owner-directed original implementation/artwork with OpenAI
- Creation/acquisition date: 2026-09-20
- Source type: generated
- Source work and URL: Original owner-directed built-in image generation; no third-party input
- Tool/model/version: built-in image generation, exact model not exposed; ffmpeg7.1 mechanical export where noted
- Human modifications: Nearest-neighbor game export; preserve alpha
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex asset/material inspection2026-09-20; owner in-game acceptance not_run
- Prompt or brief location: `docs/encounters/azure-cathedral/ASSET_BRIEF.md`
- SHA256: `f1f5ed0a2271dc0d32a6ada3ab51c1f069661a612171e40e00242852b962a240`

- Runtime file: `Assets/AutoloadedEffects/Shaders/AzureGlass.fxc`
- Asset ID: azure-glass-material-20260920
- Asset type: compiled original ice/glass/background/shard/rift/countdown/frost/dissolution/streaming-energy effect
- Creator: project-owner-directed original implementation/artwork with OpenAI
- Creation/acquisition date: 2026-09-20
- Source type: original
- Source work and URL: Repository-owned AzureGlass.fx; no external shader code. September26 adds anatomy-masked flowing caustics and attached cold-vapor ribbon passes, retaining original opaque armor.
- Tool/model/version: FXC identity/options pinned in Assets/AutoloadedEffects/Shaders/compiled.json
- Human modifications: FXC compilation only; identity/options in compiled.json
- License and redistribution terms: existing project original-asset terms; no third-party art license implied
- Required attribution: preserve project provenance and generation disclosure
- Reviewer and review date: Codex asset/material inspection2026-09-20; owner in-game acceptance not_run
- Prompt or brief location: `docs/encounters/azure-cathedral/ASSET_BRIEF.md`
- SHA256: `18ed6eae5399b555f58872900a0a0ae820cc3856bf218c03c2eb60ca3cfc26f6`

- Runtime file: `Assets/Music/AzureCathedral/WhiteNight.ogg`
- Asset ID: azure-eighth-white-night-20260920
- Asset type: stereo48000Hz Vorbis background music with native loop tags
- Creator: EigHt
- Creation/acquisition date: 2026-09-20
- Source type: licensed
- Source work and URL: 白夜に耀うステンドグラス; https://www.youtube.com/watch?v=k0-SQQkRxis and https://bgm-cathedral.booth.pm/items/6112209 ; exact owner-selected local MP3
- Tool/model/version: ffmpeg7.1 libvorbis quality6
- Human modifications: trim0.323–263.470s,1.5s triangular crossfade of tail into source0.323–1.823s, gain−2.9dB; LOOPSTART72000/LOOPEND12631056; no deleted climax or new composition
- License and redistribution terms: copyright retained by EigHt; official https://eight-novel.fanbox.cc/posts/7647818 permits game background use/editing; read unrestricted creator post via public post.info API on2026-09-20 (updated2026-07-14). Not covered by Convergence code/art license; no standalone soundtrack, streaming-service or Content-ID registration permission. Rhythm-game inclusion requires contacting the creator separately. Contextual Mod/source asset only, not a music-pack download; recheck terms before public release
- Required attribution: Music: EigHt — 白夜に耀うステンドグラス; preserve creator, original and terms links
- Reviewer and review date: source terms and audio statistics checked2026-09-20; subjective seam/mix listening not_run
- Prompt or brief location: `docs/encounters/azure-cathedral/ENCOUNTER_SPEC.md#music`
- Source SHA256: `631325533faa6840880e296664029fbc931b39d65304862378a24bbe46174ad6`
- SHA256: `d73bec8fa67a5b5724b776381b79be6f8f9db17bf6afffbae9b39e2dee04427d`

### Ghost Samurai articulated parts and materials — 2026-09-18

- Runtime file: `Assets/Textures/GhostSamurai/VioletRig.png`
- Asset ID: ghost-samurai-rig-violetrig-20260918
- Asset type: nine-part transparent Boss atlas,1254×1254 RGBA
- Creator: Convergence / Minamium direction; built-in OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-18
- Source type: generated
- Source work and URL: project-authored VioletActions.png reference; exact prompts and selected output in docs/evidence/2026-09-18-ghost-samurai-rig.json
- Tool/model/version: built-in image generation/edit; model and seed not reported
- Human modifications: no pixel edits; final generated result copied byte-for-byte; independent runtime UV/rotation/scale articulation
- License and redistribution terms: owner-requested project use under existing project asset/publication terms; no third-party asset license asserted
- Required attribution: retain this provenance and the owner reference origin; dependency assets remain externally distributed
- Reviewer and review date: Codex,2026-09-19; native compilation and offline layout checked, in-game GPU acceptance remains user-owned
- SHA256: `cda2c0fdf4609c98478dd0d75d3d203c78c52add52174f9acd765c77430faab5`

- Runtime file: `Assets/AutoloadedEffects/Shaders/SamuraiSpirit.fxc`
- Asset ID: ghost-samurai-rig-samuraispirit-20260918
- Asset type: compiled original HLSL effect
- Creator: Convergence / Minamium direction; independently authored Convergence HLSL
- Creation/acquisition date: 2026-09-18
- Source type: original
- Source work and URL: paired .fx source in this directory; Luminance public APIs and runtime-owned noise textures only
- Tool/model/version: tools/compile_shaders.py, pinned Luminance FXC; compiler/options/source/export hashes in compiled.json
- Human modifications: original shader authoring and compilation; no external shader imported
- License and redistribution terms: owner-requested project use under existing project asset/publication terms; no third-party asset license asserted
- Required attribution: retain this provenance and the owner reference origin; dependency assets remain externally distributed
- Reviewer and review date: Codex,2026-09-19; native compilation and offline layout checked, in-game GPU acceptance remains user-owned
- SHA256: `a57a91104b48c8913b270f3baf504d04153db13e46853c98759c452523354b70`

- Runtime file: `Assets/AutoloadedEffects/Shaders/SamuraiRibbon.fxc`
- Asset ID: ghost-samurai-rig-samurairibbon-20260918
- Asset type: compiled original HLSL effect
- Creator: Convergence / Minamium direction; independently authored Convergence HLSL
- Creation/acquisition date: 2026-09-18
- Source type: original
- Source work and URL: paired .fx source in this directory; Luminance public APIs and runtime-owned noise textures only
- Tool/model/version: tools/compile_shaders.py, pinned Luminance FXC; compiler/options/source/export hashes in compiled.json
- Human modifications: original shader authoring and compilation; no external shader imported
- License and redistribution terms: owner-requested project use under existing project asset/publication terms; no third-party asset license asserted
- Required attribution: retain this provenance and the owner reference origin; dependency assets remain externally distributed
- Reviewer and review date: Codex,2026-09-19; native compilation and offline layout checked, in-game GPU acceptance remains user-owned
- SHA256: `474fa47eed26d1ed1ec43d4b851dc6196dfddc9e85cfa76e41d4b06d5f96fc30`

- Runtime file: `Assets/AutoloadedEffects/Shaders/SamuraiMist.fxc`
- Asset ID: ghost-samurai-rig-samuraimist-20260918
- Asset type: compiled original HLSL effect
- Creator: Convergence / Minamium direction; independently authored Convergence HLSL
- Creation/acquisition date: 2026-09-18
- Source type: original
- Source work and URL: paired .fx source in this directory; Luminance public APIs and runtime-owned noise textures only
- Tool/model/version: tools/compile_shaders.py, pinned Luminance FXC; compiler/options/source/export hashes in compiled.json
- Human modifications: original shader authoring and compilation; no external shader imported
- License and redistribution terms: owner-requested project use under existing project asset/publication terms; no third-party asset license asserted
- Required attribution: retain this provenance and the owner reference origin; dependency assets remain externally distributed
- Reviewer and review date: Codex,2026-09-19; native compilation and offline layout checked, in-game GPU acceptance remains user-owned
- SHA256: `fb54d35c3be4b9faf8321d9cba68facb4b7db17f3a05da009b297fc0f22d0065`

### Scarlet Sanctum owner-supplied background — 2026-09-17

- Runtime file: `Assets/Textures/Backgrounds/ScarletSanctum.png`
- Asset ID: scarlet-sanctum-owner-export-20260917
- Asset type: background texture,1672×941 PNG
- Creator: project-owner-directed AI-generated artwork, supplied and approved by Minamium
- Creation/acquisition date: 2026-09-17
- Source type: generated
- Source work and URL: owner-provided attachment in the Scarlet presentation review; no external artwork download
- Tool/model/version: earlier built-in image generation; exact model identifier not supplied
- Human modifications: none to the supplied PNG; copied byte-for-byte. Runtime shaders add only transient scene treatments
- License and redistribution terms: owner-approved original project artwork under existing project asset terms
- Required attribution: retain this provenance; do not represent it as another creator's painting
- Reviewer and review date: owner approval2026-09-17; hash/dimensions and offline FNA composition checked
- Prompt or brief location: `docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md`, approved background
- SHA256: `802d1f6393ae6f0919214e3de535c1b38cc8e740161fcb98e5ba7f71c5a7e1ef`
- Export identity: this is the newly supplied clipboard export, not byte-identical to the previously recorded `94b77c968991bf52b14504bb11095c417dbf4dd2abb3bc378da779da39400a2d` export that could not be transferred

### ScarletBackdrop original shader export — 2026-09-17

- Runtime file: `Assets/AutoloadedEffects/Shaders/ScarletBackdrop.fxc`
- Asset ID: scarlet-v2-scarletbackdrop-20260917
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by OpenAI for Minamium
- Creation/acquisition date: 2026-09-17
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/ScarletBackdrop.fx`; no third-party shader copied
- Tool/model/version: FXC compiler and options pinned in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: runtime material parameters, masks and animation are project code; compilation does not modify approved images
- License and redistribution terms: original project asset under the existing project terms; dependency textures remain external
- Required attribution: retain this provenance, source and compiler/export identity manifest
- Reviewer and review date: automated source/export checks, 2026-09-17; native visual approval not_run
- Prompt or brief location: `docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md`, presentation v2
- SHA256: `8c6a64ddf9ad29cd2e2537aa19013587120c7340c6a1577c908726e7f1eb2323`
- Source SHA256: `ab810c34dda508df2d8f23b3bba37be316937803500811b7778599d87de63f16`

### ScarletResidue original shader export — 2026-09-17

- Runtime file: `Assets/AutoloadedEffects/Shaders/ScarletResidue.fxc`
- Asset ID: scarlet-v2-scarletresidue-20260917
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by OpenAI for Minamium
- Creation/acquisition date: 2026-09-17
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/ScarletResidue.fx`; no third-party shader copied
- Tool/model/version: FXC compiler and options pinned in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: runtime material parameters, masks and animation are project code; compilation does not modify approved images
- License and redistribution terms: original project asset under the existing project terms; dependency textures remain external
- Required attribution: retain this provenance, source and compiler/export identity manifest
- Reviewer and review date: automated source/export checks, 2026-09-17; native visual approval not_run
- Prompt or brief location: `docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md`, presentation v2
- SHA256: `feff3ae4fe628f38ee6e6aa90aba61cefb9568fe9de714e07994cb95e1edbe09`
- Source SHA256: `b0a99ea0bc87086f2e190b678c9a4882e75c68538a6e8d54dc6f686b8a424090`

### ScarletRibbon original shader export — 2026-09-17

- Runtime file: `Assets/AutoloadedEffects/Shaders/ScarletRibbon.fxc`
- Asset ID: scarlet-v2-scarletribbon-20260917
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by OpenAI for Minamium
- Creation/acquisition date: 2026-09-17
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/ScarletRibbon.fx`; no third-party shader copied
- Tool/model/version: FXC compiler and options pinned in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: runtime material parameters, masks and animation are project code; compilation does not modify approved images
- License and redistribution terms: original project asset under the existing project terms; dependency textures remain external
- Required attribution: retain this provenance, source and compiler/export identity manifest
- Reviewer and review date: automated source/export checks, 2026-09-17; native visual approval not_run
- Prompt or brief location: `docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md`, presentation v2
- September19 revision: project-authored feathered travelling energy, smoother forecast boundaries and endpoint UV correction; no external shader copied. Installed Luminance is referenced, not modified or vendored.
- SHA256: `53a2fff40e35b5da56550eec515cacf234d83def0822524d61663d5bac018d4b`
- Source SHA256: `55250e8e4894fb4d12bee34e6d546914606d71cca0e75c1c556d5f45188d83a6`

### ScarletCluster original shader export — 2026-09-22

- Runtime file: `Assets/AutoloadedEffects/Shaders/ScarletCluster.fxc`
- Asset ID: scarlet-cluster-plasma-20260922
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by OpenAI for Minamium
- Creation/acquisition date: 2026-09-22
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/ScarletCluster.fx`; no foreign shader/artwork copied
- Tool/model/version: FXC compiler/options and source/export identities in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: spherical flowing plasma, sparse forecast grains and harmless comet-tail passes; existing project-authored Doll WideCharge/WideFire cues reused without editing recordings
- License and redistribution terms: original project asset under existing project terms; Luminance supplies runtime noise textures, not vendored
- Required attribution: retain this provenance and source/export manifest
- Reviewer and review date: Codex,2026-09-22; actual compiled FNA GPU frames inspected, native game/MP acceptance not_run
- Prompt or brief location: `docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md`, Final cluster orb
- Source SHA256: `4f02e9b86d7bf77fdbf65431ce1d3e1ec949751552b5f067c6688db80edf5bdc`
- Export SHA256: `064f2124d4a40f0a0d2f9ef2725593cf6c64905a10d6009adcd5cba880c8e8d8`

### ScarletSorcery original shader export — 2026-09-20

- Runtime file: `Assets/AutoloadedEffects/Shaders/ScarletSorcery.fxc`
- Asset ID: scarlet-ritual-sorcery-20260920
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by OpenAI for Minamium
- Creation/acquisition date: 2026-09-20
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/ScarletSorcery.fx`; no foreign shader/artwork copied
- Tool/model/version: FXC compiler/options and source/export identities in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: etched red/black seals, white-to-red spatial tears and flowing black flame; September20 crossflow receiver vapor and continuous microflutter/contracting slash residue. September21 exposes tear palette parameters for Liora's cyan variant while preserving Scarlet's red defaults. September22 adds a legible ink core/crimson lip/pale moving filaments to failed Stack flames; existing masked creature regions are recomposed in code for the sacrificial giant, without new image assets. Runtime motion/composition in `ScarletSorcery.cs`; independently adapted behavioral reference recorded in [F18](../docs/research/WOTG_RAID_BENCHMARK.md#f18--orderbringer-cursor-hit-reference-2026-09-20), no external code/art/audio copied.
- License and redistribution terms: original project asset under existing project terms; Luminance supplies its runtime noise, not vendored
- Required attribution: retain this provenance and source/export manifest
- Reviewer and review date: Codex,2026-09-20; actual compiled FNA GPU frames inspected, game/MP acceptance not_run
- Prompt or brief location: `docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md`, orb-to-invocation and chorus sections

September22 transmutation adds original directional red-energy streams in `TransfusionPass`; no sampled audio or foreign material. Current source/export identities remain in `compiled.json`.

### ScarletApparitions original shader export — 2026-09-25

- Runtime file: `Assets/AutoloadedEffects/Shaders/ScarletApparitions.fxc`
- Asset ID: scarlet-apparitions-energy-20260925
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by OpenAI for Minamium
- Creation/acquisition date: 2026-09-25
- Source type: original
- Source work and URL: adjacent `ScarletApparitions.fx`; no third-party code/art copied
- Tool/model/version: FXC/options/source-export hashes in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: flowing emissive organic skin, Crown furnace jets and Mantle hook residues; retained original PNGs
- License and redistribution terms: original project asset under existing project terms; installed Luminance noise borrowed at runtime
- Required attribution: retain provenance and source/export manifest
- Reviewer and review date: Codex,2026-09-25; linked-production GPU frames inspected; native acceptance pending

### ScarletAvatarAnatomy original shader export — 2026-09-25

- Runtime file: `Assets/AutoloadedEffects/Shaders/ScarletAvatarAnatomy.fxc`
- Asset ID: scarlet-avatar-anatomy-20260925
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by OpenAI for Minamium
- Creation/acquisition date: 2026-09-25
- Source type: original
- Source work and URL: adjacent `ScarletAvatarAnatomy.fx`; independent equations, no external code/art
- Tool/model/version: FXC/options/source-export hashes in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: turbulent nucleus, arterial ribs, flowing organic support tendrils, local shadow shroud and sparks
- License and redistribution terms: original project asset under existing project terms; installed Luminance noise borrowed at runtime
- Required attribution: retain provenance and source/export manifest
- Reviewer and review date: Codex,2026-09-25; actual compiled GPU frames inspected; native acceptance pending

### ScarletAvatarComposite original shader export — 2026-09-25

- Runtime file: `Assets/AutoloadedEffects/Shaders/ScarletAvatarComposite.fxc`
- Asset ID: scarlet-avatar-composite-20260925
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by OpenAI for Minamium
- Creation/acquisition date: 2026-09-25
- Source type: original
- Source work and URL: adjacent `ScarletAvatarComposite.fx`; independent implementation informed by separated Avatar rendering in [benchmark](../docs/research/WOTG_RAID_BENCHMARK.md), no copied source/assets
- Tool/model/version: FXC/options/source-export hashes in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: local emission extraction/blur, body composite and restrained material distortion; not a global postprocess
- License and redistribution terms: original project asset under existing project terms; Luminance dependency resources are not redistributed
- Required attribution: retain provenance and source/export manifest
- Reviewer and review date: Codex,2026-09-25; actual body/extraction/composite GPU passes inspected; native acceptance pending

### ScarletChoir original shader export — 2026-09-25

- Runtime file: `Assets/AutoloadedEffects/Shaders/ScarletChoir.fxc`
- Asset ID: scarlet-choir-organic-energy-20260925
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by OpenAI for Minamium
- Creation/acquisition date: 2026-09-25
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/ScarletChoir.fx`; no third-party art or shader source copied
- Tool/model/version: FXC compiler, options and hashes pinned in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: five original passes for retained organic albedo, emissive anatomy, flowing wing/sleeve membranes, vascular heart and sparks; no bitmap modification
- License and redistribution terms: original project asset under existing project terms; Luminance noise borrowed at runtime, never vendored
- Required attribution: retain this provenance and source/export manifest
- Reviewer and review date: Codex, 2026-09-25; linked-production FNA material sequences inspected; native visual acceptance remains owner-tested
- Prompt or brief location: `docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md` Luminance presentation section

### ScarletInk original shader export — 2026-10-02

- Runtime file: `Assets/AutoloadedEffects/Shaders/ScarletInk.fxc`
- Asset ID: scarlet-ink-black-blood-river-20261002
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by Claude (Anthropic) for Minamium
- Creation/acquisition date: 2026-10-02
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/ScarletInk.fx`; no third-party shader or artwork copied
- Tool/model/version: FXC compiler, options and hashes pinned in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: the owner chose this look (black body, burning and melting rim, flowing red threads, twisting core, blaze after impact) from offline prototype frames on 2026-10-02; runtime composition in `Client/Encounters/CrimsonFoundry/Vfx/ScarletInkStroke.cs`, drawn from `CrimsonGestureVisuals`; no bitmap modification. Its `ForecastPass` is kept in the source but not used by the game
- License and redistribution terms: original project asset under existing project terms; Luminance supplies the noise at runtime, not vendored
- Required attribution: retain this provenance and source/export manifest
- Reviewer and review date: Claude, 2026-10-02; compiled-shader FNA/D3D11 frames inspected and pixel-identical to the owner-approved frames; game/MP acceptance not_run
- Prompt or brief location: `docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md`, Luminance presentation v2
- SHA256: `689a5d7bd1136d8104d8a094c3f317be4578498b2b882c60c44c51a87b1020f2`
- Source SHA256: `e801511955804f740b45cb1472f3005f4ab48e12ce19fc2aa1f8e180bc64a227`

### ScarletSurface original shader export — 2026-09-17

- Runtime file: `Assets/AutoloadedEffects/Shaders/ScarletSurface.fxc`
  September22 revision adds original noise-eroded red edges and molten UV flow, paired with client mesh deformation for Act absorption and Victory. Zero ceremony values retain the existing ordinary surface. No new source bitmap or external artwork.
- Asset ID: scarlet-v2-scarletsurface-20260917
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by OpenAI for Minamium
- Creation/acquisition date: 2026-09-17
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/ScarletSurface.fx`; no third-party shader copied
- Tool/model/version: FXC compiler and options pinned in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: runtime material parameters, masks and animation are project code; compilation does not modify approved images
- License and redistribution terms: original project asset under the existing project terms; dependency textures remain external
- Required attribution: retain this provenance, source and compiler/export identity manifest
- Reviewer and review date: automated source/export checks, 2026-09-17; native visual approval not_run
- Prompt or brief location: `docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md`, presentation v2
- SHA256: `768340fbe8468a352d87baad0228dc40f44dd3d127612b6fef6e3b22ddd8ed3e`
- Source SHA256: `46dee4522c642c9adc2e2a4cdbce54319ddcaa32d14d80877571e18d8414a717`

### Crimson Invocation and companion — 2026-09-16 / 0.3.11

- Runtime file: `Assets/Textures/CrimsonFoundry/ScarletConjurer.png`
- Asset ID: crimson-invocation-scarletconjurer-0311
- Asset type: four-pose pixel NPC sprite sheet
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-16
- Source type: generated
- Source work and URL: original project-directed generation; no third-party source image
- Tool/model/version: built-in image generation; actual model not reported
- Human modifications: selected original copied unchanged; runtime UV/size/pose and mesh deformation are code, not baked image edits
- License and redistribution terms: original project asset under the existing project publication/asset terms; no external artwork license asserted
- Required attribution: retain this provenance and generation brief
- Reviewer and review date: Codex, 2026-09-16; alpha/silhouette and representative hidden-FNA frames inspected; gameplay approval remains owner-tested
- Prompt or brief location: the following final generation brief
- Brief: Original red-haired young-adult summoner with very long twin-tails, black/wine-red modest dress, high collar and ivory clasps. Four full-body idle/cast/float/walk poses on true transparency, coarse readable Terraria-like pixel clusters for56px gameplay. No props, UI, text, mechanical costume or reference-image copy. The first high-resolution parts-sheet candidate was rejected.
- SHA256: `65735cb1785e3d6a21f0e76bd28c250edb6eb0d007aa8b5314d8f342d6ca058c`

- Runtime file: `Assets/Textures/CrimsonFoundry/EmberCrown.png`
- Asset ID: crimson-invocation-embercrown-0311
- Asset type: transparent apparition sprite
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-16
- Source type: generated
- Source work and URL: original project-directed generation; no third-party source image
- Tool/model/version: built-in image generation; actual model not reported
- Human modifications: selected original copied unchanged; runtime UV/size/pose and mesh deformation are code, not baked image edits
- License and redistribution terms: original project asset under the existing project publication/asset terms; no external artwork license asserted
- Required attribution: retain this provenance and generation brief
- Reviewer and review date: Codex, 2026-09-16; alpha/silhouette and representative hidden-FNA frames inspected; gameplay approval remains owner-tested
- Prompt or brief location: the following final generation brief
- Brief: Original asymmetrical obsidian/ivory crown, hollow red heart, blood-red veil and three thorn legs. Readable large silhouette, transparent cutout, no machinery, gore, text or HUD.
- SHA256: `daff90ee77fe46adf402a0b696c61823291ac770267433f34e48c5100deade1e`

- Runtime file: `Assets/Textures/CrimsonFoundry/SableMantle.png`
- Asset ID: crimson-invocation-sablemantle-0311
- Asset type: transparent apparition sprite
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-16
- Source type: generated
- Source work and URL: original project-directed generation; no third-party source image
- Tool/model/version: built-in image generation; actual model not reported
- Human modifications: selected original copied unchanged; runtime UV/size/pose and mesh deformation are code, not baked image edits
- License and redistribution terms: original project asset under the existing project publication/asset terms; no external artwork license asserted
- Required attribution: retain this provenance and generation brief
- Reviewer and review date: Codex, 2026-09-16; alpha/silhouette and representative hidden-FNA frames inspected; gameplay approval remains owner-tested
- Prompt or brief location: the following final generation brief
- Brief: Original broad black/blood-red silk apparition, four asymmetric flowing ribbons and ivory spines, tiny hollow mask/red energy knot. True transparent square, no machinery, gore, text or HUD.
- SHA256: `5ffb9323b31a4f1c4c5a0b3dd07b5b6eb134b0c105728dfde048820d485f2086`

- Runtime file: `Assets/Textures/CrimsonFoundry/ThornChoir.png`
- Asset ID: crimson-invocation-thornchoir-0311
- Asset type: transparent apparition sprite
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-16
- Source type: generated
- Source work and URL: original project-directed generation; no third-party source image
- Tool/model/version: built-in image generation; actual model not reported
- Human modifications: selected original copied unchanged; runtime UV/size/pose and mesh deformation are code, not baked image edits
- License and redistribution terms: original project asset under the existing project publication/asset terms; no external artwork license asserted
- Required attribution: retain this provenance and generation brief
- Reviewer and review date: Codex, 2026-09-16; alpha/silhouette and representative hidden-FNA frames inspected; gameplay approval remains owner-tested
- Prompt or brief location: the following final generation brief
- Brief: Original tall faceless black/crimson shroud with uneven branch horns, four ivory claws, hollow torso/red heart and long tendrils. True transparency, no machinery, gore, text or HUD.
- SHA256: `2d417b7c36b9ecc12ec3e6c64a9e8f4dfb3821fef43ae3f959c72bcbb652d282`

September25 presentation revision: the `ThornChoir.png` bytes above remain unchanged. Original `ScarletChoir` shader/mesh code adds independently articulated four-arm anatomy and energy materials; Luminance noise is borrowed at runtime, not redistributed. The rejected nine-cell armor atlas and its provenance are preserved in the ignored local asset archive and are not packaged. No Avatar/WotG artwork or shader code is incorporated.

- Runtime file: `Assets/Textures/CrimsonFoundry/CrimsonConductor.png`
- Asset ID: crimson-invocation-crimsonconductor-0311
- Asset type: summoning item icon
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-16
- Source type: generated
- Source work and URL: original project-directed generation; no third-party source image
- Tool/model/version: built-in image generation; actual model not reported
- Human modifications: selected original copied unchanged; runtime UV/size/pose and mesh deformation are code, not baked image edits
- License and redistribution terms: original project asset under the existing project publication/asset terms; no external artwork license asserted
- Required attribution: retain this provenance and generation brief
- Reviewer and review date: Codex, 2026-09-16; alpha/silhouette and representative hidden-FNA frames inspected; gameplay approval remains owner-tested
- Prompt or brief location: the following final generation brief
- Brief: Compact original black/crimson grimoire with red orb, ivory clasp and two ribbon tails. Terraria-style chunky pixel silhouette, actual transparent background, no labels or reference-image editing.
- SHA256: `18b91f13f30ffef3609d9e85dba2025d8380c828099c9c88f0a6fd1a6384e78c`

- Runtime file: `Assets/Textures/CrimsonFoundry/CrimsonPact.png`
- Asset ID: crimson-invocation-crimsonpact-0311
- Asset type: companion item icon
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-16
- Source type: generated
- Source work and URL: original project-directed generation; no third-party source image
- Tool/model/version: built-in image generation; actual model not reported
- Human modifications: selected original copied unchanged; runtime UV/size/pose and mesh deformation are code, not baked image edits
- License and redistribution terms: original project asset under the existing project publication/asset terms; no external artwork license asserted
- Required attribution: retain this provenance and generation brief
- Reviewer and review date: Codex, 2026-09-16; alpha/silhouette and representative hidden-FNA frames inspected; gameplay approval remains owner-tested
- Prompt or brief location: the following final generation brief
- Brief: Original scarlet twin-tail tassel ornament, ivory clasp and red star gem, distinct from the raid book. Terraria-style chunky pixel silhouette, actual transparent background, no labels.
- SHA256: `a4cb1fb577b21397d29435e5076b56f8a9c4e1f7ca6169e75c7a9d8e8d037bdc`

The six PNGs above are new generations, not image-to-image copies. External originals are preserved; unused high-resolution drafts remain outside the package. Runtime uses the existing original CrimsonReactor/PortalBeam/RaidEnergy shader exports and project Portal charge/fire recordings unchanged. No new third-party recording, shader or texture is vendored.

### Crimson articulated rig and reactor — 2026-09-15 / 0.3.9

- Runtime file: `Assets/Textures/CrimsonFoundry/FoundryRig.png`
- Asset ID: crimson-articulated-rig-039
- Asset type: twelve-part transparent machine/operator atlas
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-15
- Source type: generated
- Source work and URL: original project-directed generation; no third-party source image
- Tool/model/version: built-in image generation; actual model not reported
- Human modifications: original selected output copied unchanged; explicit runtime UV rectangles measured around alpha silhouettes, independently posed in code
- License and redistribution terms: original project asset under the existing project publication/asset terms; no external artwork or license asserted
- Required attribution: retain this provenance and brief
- Reviewer and review date: Codex, 2026-09-15; original/alpha/part boundaries inspected; in-game readability awaits owner acceptance
- Brief: original angular ivory/gunmetal/crimson steel machine, long limbs, nonhuman narrow head, central reactor housing, separate chest/shoulder/thigh armor, engine and small red-haired mechanic in a command chair. Twelve separated parts; no text, HUD, neon panels or third-party designs. Owner's Garde/operator references inform proportions only, not copied costumes or mecha silhouettes.
- Export:1448×1086 RGBA, true alpha0–255; SHA256 `f9a14914699d1dfbc6c85e13e9871c311ccb0e51c01660f00cd6d38e1dc7245e`. Two subsequent background/repacking candidates had opaque painted checkerboards and were rejected; the original is preserved and selected. External originals/generation records remain local, not an off-device backup claim.

- Runtime file: `Assets/AutoloadedEffects/Shaders/CrimsonReactor.fxc`
- Asset ID: crimson-reactor-material-039
- Asset type: compiled original procedural reactor material
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-15
- Source type: original
- Source work and URL: `Assets/AutoloadedEffects/Shaders/CrimsonReactor.fx`, independently written Convergence HLSL
- Tool/model/version: Microsoft FXC, pinned identity/options in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: authored contained plasma/filament/white-core and charge/release envelopes; runtime noise comes from the installed Luminance registry
- License and redistribution terms: original project asset under existing project terms; no copied WoTM/WotG shader, formula, texture or audio
- Required attribution: retain this record; dependency assets are not redistributed here
- Reviewer and review date: Codex, 2026-09-15; source/export compilation checked; actual game composition remains owner-tested
- SHA256: source `e016c33b62286a5781a530d027a7a6b8ae99999bceccd52bbb7cd54933b90607`; export `8df1ac1bdb9dcb1b46eed0490ed4c5ce25272dc7f6a7ef19563880508005180f`.

Older FoundryEngine/FoundryUnbound provenance and originals below remain retained; they no longer drive the active composite. Existing project-authored Portal charge/fire masters and BGM file are unchanged; this revision changes scoped playback gain/envelopes, not their recording licenses.

### Crimson Foundry — 2026-09-15

- Runtime file: `Assets/Textures/CrimsonFoundry/FoundryEngine.png`
- Asset ID: crimson-heavy-engine-038
- Asset type: transparent Boss PNG
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-15
- Source type: generated
- Source work and URL: original generated heavy steel-engine image, no third-party image source
- Tool/model/version: built-in image generation; actual model not reported
- Human modifications: selected original red-haired mechanic/furnace design, background extraction by the built-in tool; runtime export unchanged
- License and redistribution terms: original project asset, subject to existing publication/asset-license gate
- Required attribution: retain this provenance and generation brief
- Reviewer and review date: Codex, 2026-09-15; silhouette/alpha inspected, gameplay readability not_run

- Runtime file: `Assets/Textures/CrimsonFoundry/FoundryUnbound.png`
- Asset ID: crimson-exposed-engine-038
- Asset type: transparent Boss PNG
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-15
- Source type: generated
- Source work and URL: original slim exposed-engine image, no third-party image source
- Tool/model/version: built-in image generation; actual model not reported
- Human modifications: selected original exposed-scythe craft and red-haired pilot; runtime export unchanged
- License and redistribution terms: original project asset, subject to existing publication/asset-license gate
- Required attribution: retain this provenance and generation brief
- Reviewer and review date: Codex, 2026-09-15; silhouette/alpha inspected, gameplay readability not_run
- Creator/source: OpenAI built-in image generation, directed by Codex for Minamium; original designs, no supplied third-party visual reference. Actual generation model name was not reported.
- Brief: red-haired mechanic in an open cockpit; asymmetric heavy gunmetal/crimson furnace weapon engine, then an exposed slim swept-scythe craft after armor purge; crisp textured2D silhouettes, true transparent alpha, no text/UI rings. The heavy image received a background-extraction edit; no other artwork was copied. Full generation records remain external.
- Export: selected PNGs unchanged from the built-in outputs,1536×1024 and1689×931. Runtime code supplies scale, banking, armor fragments and exhaust. External originals remain preserved; not an off-device backup claim.
- SHA256: heavy `7c8703f2d0969bbd1444bb89c57a5d747605cb932e2cf09b298fd0033198078a`; exposed `65912b7856832a0210028eb62e0de7ebee81e26b8d81e9afd667b017c9bbd594`.
- Terms: original generated project assets under the existing publication/asset-license gate. Retain this provenance. Codex inspected the generated silhouettes/alpha on2026-09-15; actual game-distance readability remains owner-tested.
- Effects: existing project-authored `PortalBeam.fxc` / `RaidEnergy.fxc` and existing original Raid cues are referenced unchanged; Luminance supplies its own noise textures at runtime. No external Mod assets or recordings are extracted.

- Runtime file: `Assets/Music/CrimsonFoundry/GracefulOrdeal.ogg`
- Asset ID: crimson-graceful-ordeal-loop-038
- Asset type: third-party game-facing OGG loop edit; not a standalone music release
- Creator: **kuku**, composer/recording owner; Minamium supplied the WAV for this encounter
- Creation/acquisition date: 2026-09-15
- Source type: licensed
- Source work and URL: **Graceful Ordeal** / 「実はとてもお強いお嬢様からの試練BGM」; [author's video and terms](https://www.youtube.com/watch?v=HnBESyUqx_g), with [official WAV](https://drive.google.com/file/d/1bX8QLlttGdZ5ssGm6qtbd-XQAXs09MdA/view) linked from its description
- License and redistribution terms: BGM/personal use permitted, including monetized video use; credit required for secondary creative publication; copyright retained by kuku; uncreative BGM-only/endurance content prohibited. Minamium explicitly approved inclusion of this exact game-facing derivative in the Mod and public source repository on2026-09-15 after reviewing the terms and source-distribution ambiguity. This is project-integrated use, not a claim of a separate standalone-audio redistribution license.
- License exclusion: **Copyright belongs to kuku. This recording and its loop edit are NOT covered by any Convergence source-code or original-asset license.** The author's linked terms continue to govern; no Content ID registration, music-only collection or endurance upload by this project. The raw WAV and audition exports remain external.
- Required attribution: **Music: “Graceful Ordeal” / 「実はとてもお強いお嬢様からの試練BGM」 by kuku — https://www.youtube.com/watch?v=HnBESyUqx_g . Loop edit for Crimson Foundry; original copyright belongs to kuku.**
- Original input SHA256: `42a10539bdc12468d279f9956085ce1c99aae1fa0092fc05912e0225c26076d3`; owner-provided WAV was not modified. Its former external path is unavailable on September20. This revision uses the approved full game OGG `f8f0a566a18dfa7d9ba2fd6203d877160bc6e0ae04123a694a2c9307bbaaffa9`, retained locally and in Git history, not a new recording. Revised OGG SHA256: `1a00366b16c485fd7f921100fe237d19ac59e689471ed6fb966b605d36fa4519`.
- Tool/model/version: original `tools/prepare_crimson_score.py`; current `tools/restore_crimson_full_score.py`, NumPy2.3.5 and FFmpeg7.1/libvorbis quality7; earlier shortening script retained only as history
- Human modifications: restore all original134.5127s including the climax; apply1.15dB encode headroom and append a short return bridge, without cutting/time-stretching/pitch-shifting the original portion. Since 2026-10-02 (0.3.65) the file is unchanged but the game no longer loops it as a whole: clients re-sequence whole 128 BPM bars of it per stage, blend 14 ms of the real continuation at jumps, swell one riser bar, and end Victory on the song's own full-stop bar with a short synthetic hall tail (Defeat: falling low-pass). No other audio is mixed into the recording and the bridge is unused. The retired `Score.json` beat map was project-generated numeric data. See [owning music spec](../docs/encounters/crimson-foundry/ENCOUNTER_SPEC.md#music-graceful-ordeal-on-a-128-bpm-grid), not duplicated tuning here.
- Reviewer and review date: Codex,2026-09-20; numerical timing, peak and loop analysis completed; musical seam/device/MP listening remains owner-owned. Claude,2026-10-02: measured the strict128 BPM grid and bar seams and rendered whole-fight auditions (the owner approved the direction); in-game listening remains owner-owned. Author video terms were checked at original adoption, not newly re-certified here; retain the bundled `Credits.txt` and this provenance. The score analysis JSON is project-generated numeric game data, not a new license for the recording. Public release still requires the repository's release terms check.

### Weapon energy and README NPC export — 2026-09-14

- Runtime file: `Assets/AutoloadedEffects/Shaders/ArmamentEnergy.fxc`
- Asset ID: doll-armament-energy-0277
- Asset type: compiled original weapon trail/beam/seal material
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-14
- Source type: original
- Source work and URL: adjacent `ArmamentEnergy.fx`; project-authored HLSL, runtime Luminance noise dependency; no third-party code/art copied
- Tool/model/version: pinned FXC 10.1/D3DCompiler_47; exact hashes in adjacent `compiled.json`
- Human modifications: flowing dark/light channels, connected hot core, engraved interlaced weapon seals; original geometry and clocks preserved
- License and redistribution terms: project publication/asset-license gate; Luminance textures remain supplied by the separate dependency, not vendored
- Required attribution: retain this provenance and the existing Luminance dependency credit
- Reviewer and review date: Codex, 2026-09-14; compiled FNA/D3D11 frames inspected; gameplay acceptance pending
- Notes: source/export SHA256 and reproduction flags in `Assets/AutoloadedEffects/Shaders/compiled.json`; no client compiler is required

- Runtime file: `docs/media/doll-npc.png`
- Asset ID: readme-doll-native-cel-0277
- Asset type: documentation PNG, excluded from Mod package
- Creator: existing project Doll NPC artwork; mechanical export by Codex
- Creation/acquisition date: 2026-09-14
- Source type: generated
- Source work and URL: `Assets/Textures/NPCs/DollTheater/DollAttendant.png`, first 32x52 cel; inherits the exact existing Doll NPC record's source and terms
- Tool/model/version: Python/Pillow, `tools/export_readme_doll.py`; no image-generation model used for this export
- Human modifications: crop exact native cel and nearest-neighbor 3x enlargement to 96x156; no repainting, compositing or new art
- License and redistribution terms: same project asset/publication gate as the source Doll NPC texture
- Required attribution: retain the source NPC provenance and this derivative record
- Reviewer and review date: Codex, 2026-09-14; native atlas and README export visually inspected
- Notes: original atlas and former generated README banner remain preserved; this export is not promotional artwork or a game screenshot

### Theater Doll activation reliquary — 2026-09-14

- Runtime file: `Assets/Textures/Items/TheaterDoll.png`
- Asset ID: theater-doll-reliquary-0276
- Asset type: inventory item sprite; transparent 44x48 PNG
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-14
- Source type: generated
- Source work and URL: original generation, no reference image or third-party asset; local source identifier `theater-key-20260914/source.png`, preserved outside Git/package
- Tool/model/version: built-in image generation; actual model name not reported; Pillow mechanical alpha crop/resize through [export_theater_key.py](../tools/export_theater_key.py)
- Human modifications: selected a black-iron miniature coffin containing a cracked ivory doll mask, winding key and muted violet ribbon; alpha crop, 40x44 maximum painted envelope centered in 44x48, no redraw or fake transparency
- License and redistribution terms: original generated project asset, subject to the existing project publication/asset-license gate; no third-party character or artwork copied
- Required attribution: retain this record, design brief and export recipe; do not identify an unreported generation model
- Reviewer and review date: Codex, 2026-09-14; full-size source and native export inspected, real inventory acceptance pending
- Notes: source SHA256 `7e621be4164757bc525b61629b07695bea47abc4bddddb8274ca95dc7851e333`; runtime SHA256 `7028322cf5c97e88d097f325c84baa9ee7a59d4a0f678db10651154f1d8453bc`. Abridged generation brief: one readable Terraria-style inventory relic, chunky limited-color clusters, black coffin/ivory half-mask/winding key/purple ribbon, genuine transparent background, no text or girl/minion portrait. Source is retained, never required by the runtime.

### Stack and Spread world-space ring — 2026-09-14

- Runtime file: `Assets/AutoloadedEffects/Shaders/MechanicRing.fxc`
- Asset ID: doll-mechanic-ring-0276
- Asset type: compiled original GPU area marker
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-14
- Source type: original
- Source work and URL: adjacent `MechanicRing.fx`; independent code, using the [Luminance API](../docs/encounters/first-severance/VISUAL_SPEC.md#luminance-raid-presentation) already referenced by the Raid renderer
- Tool/model/version: Microsoft FXC 10.1 / D3DCompiler_47, pinned compiler/source/output hashes in adjacent `compiled.json`
- Human modifications: exact world-radius pearl boundary, inward advected currents, soft shadow and distinct contracting dashed inner countdown; muted teal/plum palette; reduced-detail path without moving the true boundary
- License and redistribution terms: project publication/asset-license gate; separately installed Luminance maps are runtime references, not copied third-party textures
- Required attribution: retain this record; Luminance is [MIT licensed](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/LICENSE)
- Reviewer and review date: Codex, 2026-09-14; compiled FNA/D3D11 frames on dark/light backgrounds; in-game overlap, peer scale and accessibility acceptance pending
- Notes: reproduce with `tools/compile_shaders.py --fxc <local compiler>`. Source/manifest are excluded from the package. The accepted lattice source/export remain byte-identical; current hashes are recorded in the compiled manifest.

### Raid beam pressure audio — 2026-09-13

- Asset ID: doll-beam-pressure-0273; type: original synthesized SFX.
- Runtime files:
  - `Assets/Sounds/FirstSeverance/Beams/PortalCharge.wav`
  - `Assets/Sounds/FirstSeverance/Beams/WideCharge.wav`
  - `Assets/Sounds/FirstSeverance/Beams/GridCharge.wav`
  - `Assets/Sounds/FirstSeverance/Beams/ChargeGather.wav`
  - `Assets/Sounds/FirstSeverance/Beams/ChargeLock.wav`
  - `Assets/Sounds/FirstSeverance/Beams/PortalFire.wav`
  - `Assets/Sounds/FirstSeverance/Beams/CurtainFire.wav`
  - `Assets/Sounds/FirstSeverance/Beams/CoreSalvoFire.wav`
  - `Assets/Sounds/FirstSeverance/Beams/GridFire.wav`
  - `Assets/Sounds/FirstSeverance/Beams/ChargeRush.wav`
  - `Assets/Sounds/FirstSeverance/Beams/WideFire.wav`
  - `Assets/Sounds/FirstSeverance/Beams/BeamSustain.wav`
  - `Assets/Sounds/FirstSeverance/Beams/FloodFire.wav`
  - `Assets/Sounds/FirstSeverance/Beams/SpreadRay.wav`
  - `Assets/Sounds/FirstSeverance/Beams/SpreadScatter.wav`
- Creator: Codex, directed by Minamium. Creation/review date: 2026-09-13.
- Source type: original project synthesis/remix; reproducible master/seed/envelope: [tools/generate_beam_sfx.py](../tools/generate_beam_sfx.py), NumPy FFT synthesis, 44.1-kHz stereo PCM16 export. No third-party samples, reference-recording excerpts, AI audio service or extracted third-party game asset used. SpreadRay additionally layers the project's own unchanged `Assets/Sounds/FirstSeverance/SpreadExecution.wav` (SHA256 `44484f4970caaa194524c6363aa5aeb96ce2b518058f67dd96769ae26efb4220`; [original provenance](#execution-ping-revision--0222--2026-09-07)).
- Reference influence: [F16](../docs/research/WOTG_RAID_BENCHMARK.md#f16--beam-audio-envelope-analysis-2026-09-13), numeric mixed-recording/event analysis and pinned WotG/WoTM cue grouping. This environment cannot hear audio; user approved analysis-based creation with audition delivery. Reference originals/analysis and MP3 previews remain ignored local artifacts.
- Design/editing: aperiodic pressure, inharmonic cavities, short bright rupture, pre-shot dip and bounded release; one periodic rotation sustain. Revision 0.2.75 raises warning/launch presence about 4 dB, adds a brief mid-register resonant fan, restores the owned Spread needle over its heavy body, and separately contains sustain crests for overlap headroom. Separate Raid routes leave existing music, shared weapon and accepted Stack masters untouched.
- Revision 0.2.76 (2026-09-14): preserve the accepted fan/needle, add a 0.24-second dark diffuse release to fired beam masters, and remaster peaks for the revised runtime category gains and bounded overlap. Eleven masters change; the Spread pair, rotation bed and lock master retain their 0.2.75 bytes. The updated exact per-file hashes below and [mix policy](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set) supersede the earlier gain/deadline details. Numeric export/overlap checks pass; no claim of subjective audition.
- License/redistribution: original project asset under the existing project publication/asset-license gate. No WotG/WoTM audio reuse rights are asserted or needed for these independently generated sounds. Retain this record and recipe; preview encoding does not change runtime master identity.
- Review: PCM/header, peak/RMS/DC, one-shot edges, loop wrap and mapping guards; artistic listening and actual game mix remain user-owned. Exact export hashes are recorded in the matching build's source manifest and audio evidence.

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/PortalCharge.wav`
- Asset ID: doll-beam-0273-portalcharge
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `016c41987ae0a730cac443482f38a96ad4174e4d7e36f8ea3ba855870de7c985`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/WideCharge.wav`
- Asset ID: doll-beam-0273-widecharge
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `a14d908e5a5858033f49043ee3ea12ee6f98dc414d878edb87ff621862dacab3`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/GridCharge.wav`
- Asset ID: doll-beam-0273-gridcharge
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `f05950bba0982bb1a646f1a94d6b7cfcb7ef553217104f5ac1a579a026783b8e`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/ChargeGather.wav`
- Asset ID: doll-beam-0273-chargegather
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `e48071a2eb6316e521420850ad570ebf3505da9bc83e6a86f0e4cfbfbc4b1c30`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/ChargeLock.wav`
- Asset ID: doll-beam-0273-chargelock
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `52a0b6e14c8dfb7233b30bce87cd9225d826b7225a19b6a4ac5acacd0011fb7d`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/PortalFire.wav`
- Asset ID: doll-beam-0273-portalfire
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `d7c50341bca300d854a61708e3063549ac47e95c06ea8a651a6ec4bb3ee9b2f9`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/CurtainFire.wav`
- Asset ID: doll-beam-0273-curtainfire
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `2c7074d02f1c0041eef4a723c0e461ee999629fcde643f75acb001b7551f6b33`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/CoreSalvoFire.wav`
- Asset ID: doll-beam-0273-coresalvofire
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `44e87ca0743bcf093d4be9eaed1571a8e9678a496fdeb2ac78e7c8d376982aaf`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/GridFire.wav`
- Asset ID: doll-beam-0273-gridfire
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `d0f53f3ffffdf4f6227ce7d0954e51267ad36356e37ac4c4276ec925b28dc636`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/ChargeRush.wav`
- Asset ID: doll-beam-0273-chargerush
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `57adc7042b6ba34074f5c02f455e78471e37bcc13a5b7d404bec08b398378c31`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/WideFire.wav`
- Asset ID: doll-beam-0273-widefire
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `8cff57a17c01748492e568a12dfccd52b9d19d8355710e50e78e30c99149cb48`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/BeamSustain.wav`
- Asset ID: doll-beam-0273-beamsustain
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Periodic noise/inharmonic rotation bed, owned by the two-turn action; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `965a74c56213bb37c2a498068ffae567f7e2ca30a9e17d23a9cc470c871bc7b6`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/FloodFire.wav`
- Asset ID: doll-beam-0273-floodfire
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `9da55f70db57abbd4fa5579bc159baabad8e432e685ba4cf67ef1b4cd89f8514`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/SpreadRay.wav`
- Asset ID: doll-beam-0273-spreadray
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py) and project-owned [SpreadExecution.wav](Sounds/FirstSeverance/SpreadExecution.wav); original source identity recorded in set provenance above
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Preserve the new pressure body; restore the actual original needle at unchanged pitch/timing with band shaping, resampling and a bounded 0.50-second release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `24683bc4df5f00874cb60065265612cec2bcb95941349ff06569a7aac6082414`

- Runtime file: `Assets/Sounds/FirstSeverance/Beams/SpreadScatter.wav`
- Asset ID: doll-beam-0273-spreadscatter
- Asset type: original beam sound effect
- Creator: Codex, directed by Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: [beam synthesis recipe](../tools/generate_beam_sfx.py)
- Tool/model/version: NumPy FFT synthesis; PCM16 stereo44.1kHz
- Human modifications: Layered pressure/foil envelope, short early reflections and clean one-shot release; see [set design](../docs/AUDIO_CUE_SHEET.md#beam-pressure-set)
- License and redistribution terms: Original project asset; existing project publication/asset-license gate. No sampled third-party work.
- Required attribution: Retain recipe and [set provenance](#raid-beam-pressure-audio--2026-09-13).
- Reviewer and review date: Codex, 2026-09-13; numeric PCM/routing checks, subjective audition pending
- Notes: SHA256 `deea7949fb10cebca5bcb4e7e46964d8ec9c048415c24cc757518d48b1ac3975`


### Portal triplet beam material — 2026-09-13

- Runtime file: `Assets/AutoloadedEffects/Shaders/PortalBeam.fxc`
- Asset ID: doll-portal-jet-0272
- Asset type: compiled original four-pass GPU material
- Creator: Codex, project-directed for Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: adjacent `PortalBeam.fx`; [F15 recorded triplet analysis](../docs/research/WOTG_RAID_BENCHMARK.md#f15--nameless-portal-triplet-2026-09-13) records visual/source influence, not copied equations or textures
- Tool/model/version: Microsoft FXC 10.1 / D3DCompiler_47; exact input/output hashes in adjacent `compiled.json`
- Human modifications: soft forecast veil, pre-shot dim, independent scrolling dark/colored folds and white core, post-damage width contraction, connected source slit. Lattice and prior shader exports preserved unchanged.
- License and redistribution terms: existing project publication/license gate; WotG reuse permission is unestablished and no source/shader/art from it is distributed
- Required attribution: retain this record and F15; Luminance MIT maps are referenced through its public runtime registry, not copied into the package
- Reviewer and review date: Codex, 2026-09-13; native compiled FNA/D3D11 previews, gameplay acceptance pending
- Notes: regenerate with `tools/compile_shaders.py --fxc <local compiler>`. Source/manifest and local user-recording frame analysis are excluded from `.tmod`; no image generation or third-party media conversion into runtime textures.

### Raid Energy material suite — 2026-09-13

- Runtime file: `Assets/AutoloadedEffects/Shaders/RaidEnergy.fxc`
- Asset ID: doll-raid-energy-0266
- Asset type: compiled original eleven-pass GPU material suite
- Creator: Codex, project-directed for Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: adjacent `RaidEnergy.fx`, independent implementation; [pinned research](../docs/research/WOTG_RAID_BENCHMARK.md#f12--極太赤ビームエネルギー弾終幕の連動2026-09-13追補) records observations rather than copied source/assets
- Tool/model/version: Microsoft FXC 10.1 / D3DCompiler_47; input/output hashes in adjacent `compiled.json`
- Human modifications: separate forecast/live/corona/source/orb/wake/pressure/rift/flare passes; exact authority geometry; scene and hit-result adapters, bounded frame-local rendering. The subsequent [thin-axis/ignition revision](../docs/research/WOTG_RAID_BENCHMARK.md#f13--細い予告から射出増幅へ2026-09-13追補) removes area forecasts, stretches live currents longitudinally and couples launch/amplification to shared geometry. The lattice follow-up adds an original finite-ribbon pass with a luminous moving head, long tapered tail and clipping-stable flow coordinates; no third-party material was copied.
- License and redistribution terms: existing project publication/license gate remains; Luminance MIT dependency is separately installed, not vendored
- Recorded-reference revision: [F14](../docs/research/WOTG_RAID_BENCHMARK.md#f14--recorded-beam-motion-2026-09-13) adds an original sparse forecast-glint pass and asymmetric travelling current envelopes to all Raid beam materials. User-supplied gameplay recordings were inspected locally, not packaged or used as textures. Shader code remains independently authored; no WoTM/WotG shader, sprite or recording was copied.
- Required attribution: retain this record; runtime maps use the public Luminance registry (`WavyBlotchNoise`, `TurbulentNoise`, `DendriticNoiseZoomedOut`), whose license is linked below
- Reviewer and review date: Codex, 2026-09-13; compiled FNA/D3D11 material preview, native package; actual game acceptance pending
- Notes: reproduce with `tools/compile_shaders.py --fxc <local compiler>`; no player-side compiler. `.fx` and manifest excluded from `.tmod`. Original shell/rig and previous shader source retained. No WoTM/WotG code/art/shader is distributed. [Luminance MIT](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/LICENSE).

### Pursuit Prism shader — 2026-09-13

- Runtime file: `Assets/AutoloadedEffects/Shaders/PursuitPrismFlow.fxc`
- Asset ID: pursuit-prism-luminance-0265
- Asset type: compiled original forecast/body/bloom/mouth shader
- Creator: Codex, project-directed for Minamium
- Creation/acquisition date: 2026-09-13
- Source type: original
- Source work and URL: adjacent `PursuitPrismFlow.fx`, independently authored; retained historical eight-cast material. Current [visual specification](../docs/encounters/first-severance/VISUAL_SPEC.md#luminance-raid-presentation) supersedes its narrow scope; no WoTM shader/code was copied. Runtime texture references use Luminance's `MiscTexturesRegistry.WavyBlotchNoise` and `TurbulentNoise`; no dependency textures are redistributed in Convergence.
- Tool/model/version: Microsoft FXC 10.1 / D3DCompiler_47; exact compiler and input/output hashes in adjacent `compiled.json`
- Human modifications: replaced the rejected uniform rectangular fill/sine wires with independently advected textured convection, a hot core, diffuse separate bloom and pressure plume; retained the eight-cast-only scope and palette
- License and redistribution terms: project source/asset terms remain subject to the existing development publication gate; Luminance stays a separate MIT-licensed dependency, not bundled code
- Required attribution: retain this record and the API references in the visual specification
- Reviewer and review date: Codex, 2026-09-13; native FNA effect load and offscreen material draws; game acceptance pending
- Notes: `tools/compile_shaders.py --fxc <local compiler>` reproduces the runtime export. Sources/manifest excluded from `.tmod`; no player-side compiler required. Referenced dependency maps are owned by [Luminance, MIT](https://github.com/LucilleKarma/Luminance/blob/b2468dfd2f299597602dc6826af781d436c29a57/LICENSE) and loaded through its public texture registry.

### Convergence Mod README illustration — 2026-09-12

- Runtime file: `docs/media/convergence-banner.png`
- Asset ID: convergence-readme-banner-2026-09-12
- Asset type: documentation illustration
- Creator: OpenAI built-in image_gen, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-12
- Source type: generated
- Source work and URL: the project's existing `icon_workshop.png` emblem, whose original generated-art record is retained below; no external character art or game extraction supplied
- Tool/model/version: OpenAI built-in image_gen; backend model version not exposed
- Human modifications: owner requested an illustrated README; selected 2172×724 PNG copied byte-for-byte, without raster edits, cropping or resampling
- License and redistribution terms: project asset distribution license remains unselected; this task authorizes repository documentation use, not a new third-party license or public game release
- Required attribution: retain this provenance; no external credit requirement specified
- Reviewer and review date: Codex inspected title, emblem, composition and margins on 2026-09-12; README labels it as an illustration, not a screenshot
- Notes: documentation-only export, excluded from the runtime package by the existing `docs` buildIgnore; generated original retained outside source; 2239776 bytes; SHA256 `84e4ffd1fb9ddab1d581542a62a7a0fcf9646f960e926402342c4f72a5e888e3`; [prompt and output evidence](../docs/evidence/2026-09-12-readme-artwork.json)

### Broom companion, treasure box and mechanical cradle — 0.2.57

Built-in prompts: `tools/asset_recipes/doll_presentation_0257.json`; export: `tools/prepare_doll_presentation.ps1`. Project Doll NPC and restraint atlas supplied identity/layout references. Rejected checkerboard background, corrected originals and old runtime atlases are retained; no third-party art imported. Code-native sphere lighting is described in the Doll Theater visual spec.

- Runtime file: `Assets/Textures/NPCs/DollTheater/DollBroom.png`
- Asset ID: doll-theater-0257-dollbroom
- Asset type: image
- Creator: project-directed built-in image generation and Codex mechanical export
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: existing original Convergence NPC/restraint references; no third-party source
- Tool/model/version: built-in ImageGen (backend model unavailable); System.Drawing via tools/prepare_doll_presentation.ps1
- Human modifications: owner requested broom animation, treasure container and smooth mechanical sphere; fixed-pivot registration, crop, keying, palette and nearest resize only
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex alpha/dimensions/pivot and offline pixel inspection, 2026-09-12; actual game readability pending
- Notes: 22801 bytes; SHA256 `2b91a22d51ad936677e3cbad1ef0bf69df18fb8db7e89d1ac8591485db84fb97`.

- Runtime file: `Assets/Textures/Items/RitualArmaments/DollTreasureBox.png`
- Asset ID: doll-theater-0257-dolltreasurebox
- Asset type: image
- Creator: project-directed built-in image generation and Codex mechanical export
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: existing original Convergence NPC/restraint references; no third-party source
- Tool/model/version: built-in ImageGen (backend model unavailable); System.Drawing via tools/prepare_doll_presentation.ps1
- Human modifications: owner requested broom animation, treasure container and smooth mechanical sphere; fixed-pivot registration, crop, keying, palette and nearest resize only
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex alpha/dimensions/pivot and offline pixel inspection, 2026-09-12; actual game readability pending
- Notes: 3485 bytes; SHA256 `ff0d271851e07e9d647ec31c5be73c1428edd368bdb6bc8ca64ddec2207f1c32`.

- Runtime file: `Assets/Textures/NPCs/DollTheater/MechanicalRestraintFrames.png`
- Asset ID: doll-theater-0257-mechanicalrestraintframes
- Asset type: image
- Creator: project-directed built-in image generation and Codex mechanical export
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: existing original Convergence NPC/restraint references; no third-party source
- Tool/model/version: built-in ImageGen (backend model unavailable); System.Drawing via tools/prepare_doll_presentation.ps1
- Human modifications: owner requested broom animation, treasure container and smooth mechanical sphere; fixed-pivot registration, crop, keying, palette and nearest resize only
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex alpha/dimensions/pivot and offline pixel inspection, 2026-09-12; actual game readability pending
- Notes: 1238481 bytes; SHA256 `aaf7480206de13e30824d86e4918f43b860dd529812fbb12887b44eb42264ee2`.

### Ghost Samurai boss rig

- Runtime file: `Assets/Textures/GhostSamurai/GhostSamuraiAtlas.png`
- Asset ID: ghost-samurai-boss-rig-20260912
- Asset type: segmented Boss body and sword-arm atlas
- Creator: project-directed OpenAI image generation from the owner's concept sketch
- Creation/acquisition date: 2026-09-12
- Source type: generated
- Source work and URL: owner-supplied original skull/oni/two-katana sketch; no third-party source; prompts in docs/evidence/2026-09-12-ghost-samurai-visuals.json
- Tool/model/version: built-in image_gen; backend model/seed unavailable; selected PNG copied byte-for-byte
- Human modifications: user requested visual regeneration; agent directed body/arm separation and keyed-background export, then implemented shoulder pivots and client-only runtime transparency
- License and redistribution terms: project license remains undecided; existing public-release gate retained
- Required attribution: retain this provenance; no third-party game asset imported
- Reviewer and review date: Codex 2026-09-12; key/gutter/regions and offline multi-background poses inspected; in-game approval pending
- Notes: 1254x1254 RGB PNG; SHA256 `9e32d45d56d239df31a401cbefbdcc239ebfb81f6dd9cdb28e503adaf961ad48`; detailed production note below

### Doll companion and weapon-only foley — 0.2.53

The new companion uses the existing project-authored NPC as a character reference, not third-party sprite material. Exact briefs and export contract: `tools/doll_companion_recipe.json`. Raw originals and rejected background variants are retained in the external workspace archive. Weapon sounds are independent NumPy synthesis; prior-art event/voice design is recorded in `docs/encounters/first-severance/WEAPONS.md#weapon-sound-and-ten-slot-companion-references`. No foreign audio or implementation is copied. Existing stage-NPC, Boss, Raid SFX and BGM masters remain unchanged.

- Runtime file: `Assets/Textures/NPCs/DollTheater/DollCompanion.png`
- Asset ID: doll-theater-0253-dollcompanion
- Asset type: image
- Creator: project-directed built-in image generation and Codex mechanical export
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: existing Convergence Doll NPC reference; no third-party sprite source
- Tool/model/version: built-in ImageGen (backend name unavailable); System.Drawing export via tools/prepare_doll_companion.ps1
- Human modifications: owner requested NPC-like companion; agent specified separate walk/float/casting poses and Terraria-scale palette/registration
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex atlas/pixel preview inspection, 2026-09-12; actual game readability pending
- Notes: 33416 bytes; SHA256 `585630ae57d530657bd9639ff6b8654b70cb81e2cde07a1ae4ebb5953e257d8c`; 36 frames of 48x64, 12 reused NPC idle / 24 newly drawn movement and cast cels.

- Runtime file: `Assets/Textures/Items/RitualArmaments/DollCovenant.png`
- Asset ID: doll-theater-0253-dollcovenant
- Historical runtime use (2026-09-13): the stage key initially shared this companion inventory texture. It now uses the separate [activation reliquary](#theater-doll-activation-reliquary--2026-09-14); the companion retains this original texture.
- Asset type: image
- Creator: project-directed built-in image generation and Codex mechanical export
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: existing Convergence Doll NPC reference; no third-party sprite source
- Tool/model/version: built-in ImageGen (backend name unavailable); System.Drawing export via tools/prepare_doll_companion.ps1
- Human modifications: owner requested NPC-like companion; agent specified separate walk/float/casting poses and Terraria-scale palette/registration
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex atlas/pixel preview inspection, 2026-09-12; actual game readability pending
- Notes: 2132 bytes; SHA256 `53da713ff5f4810648e09e54edc16b57572b2bf9b37d275fde98105d1c710323`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ChoirCharge.wav`
- Asset ID: doll-theater-0253-choircharge
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; Beams/ChargeRush, Beams/ChargeLock, CoreHit; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.8s; SHA256 `d1f88ea24bf870949d86aedb31ae72cfb47584ca236a876db598578dc7b9d913`; RMS -15.733 dBFS; 4x peak -1.293 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ChoirFire.wav`
- Asset ID: doll-theater-0253-choirfire
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; Beams/WideFire, ShellMassCollapse, Beams/SpreadRay; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.484989s; SHA256 `896921cfdc784c5797fa192da79d7421b3297b57911a135ff31dfbe56e19dad7`; RMS -14.738 dBFS; 4x peak -1.309 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ChoirNote.wav`
- Asset ID: doll-theater-0253-choirnote
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; CoreHit, Beams/SpreadScatter, Beams/PortalFire; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.23s; SHA256 `47f220b864725c85e03a70480002ca6975eb5c540b854c3f5bace2ba96bd1e2c`; RMS -14.898 dBFS; 4x peak -1.31 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ChoirSustain.wav`
- Asset ID: doll-theater-0253-choirsustain
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; ChoirSustain, Beams/WideFire; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 4s; SHA256 `2ef521ede3a62a701c89c2fe750a8179d34495dd09805f573e166b38b8918352`; RMS -10.806 dBFS; 4x peak -1.83 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ClawCrush.wav`
- Asset ID: doll-theater-0253-clawcrush
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; HandCrushImpact, PylonHit, SwordImpale; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.38s; SHA256 `0e827cccbe1ff4c872d3db799011ca5c086ba6f5eb68e077c21e4f120d64d7e1`; RMS -14.135 dBFS; 4x peak -1.308 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ClawGrip.wav`
- Asset ID: doll-theater-0253-clawgrip
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; PylonHit, ShellMassLatch; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.18s; SHA256 `74c93a05b8aabdc7152acbcac68c32d0f2f76f3c3a2aba5a82c03072c70c1545`; RMS -13.055 dBFS; 4x peak -1.31 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ClawHit.wav`
- Asset ID: doll-theater-0253-clawhit
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; PylonHit, CoreHit; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.16s; SHA256 `0bc04192ec4a5c245d4a12338e4a9520e4bf3f95b74f9a4d38b914677dadc081`; RMS -13.452 dBFS; 4x peak -1.309 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ClawSwipe.wav`
- Asset ID: doll-theater-0253-clawswipe
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; BladeUnsheathe, Beams/WideFire, SwordImpale; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.29s; SHA256 `68b896d29f61ce5f2dd12a18380981e3aa29f93efdbd8a13dbfed1b2964ea721`; RMS -13.434 dBFS; 4x peak -1.308 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/DollCharge.wav`
- Asset ID: doll-theater-0253-dollcharge
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; Beams/ChargeRush, Beams/PortalCharge; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.6s; SHA256 `4cbd34461736c4135b1fc49754b45c2e0f24afa1668ef7c3896fb36f905913e4`; RMS -12.396 dBFS; 4x peak -1.31 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/DollSummon.wav`
- Asset ID: doll-theater-0253-dollsummon
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; ShellMassShed, CoreHit, Beams/SpreadScatter; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.55s; SHA256 `626143302b265ce8a8a630bb88478c838ea891e87b48e4cdc39b36ca37ea2916`; RMS -15.231 dBFS; 4x peak -1.31 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/DollThread.wav`
- Asset ID: doll-theater-0253-dollthread
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; Beams/PortalFire, CoreHit; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.224989s; SHA256 `ca84966451c08a1d58f11b77473abf491f789be024d271d4a8df9b03aa35a9a7`; RMS -14.201 dBFS; 4x peak -1.31 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/DollVerdict.wav`
- Asset ID: doll-theater-0253-dollverdict
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; Beams/PortalFire, Beams/CoreSalvoFire, CoreHit; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.404989s; SHA256 `eb726ca2ef03d04cf7bcc2ce28efe62ce2290f9513ae0c96c1db5a37c873ea7b`; RMS -14.074 dBFS; 4x peak -1.309 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/LacunaSustain.wav`
- Asset ID: doll-theater-0253-lacunasustain
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; LacunaSustain, Beams/PortalFire; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 4s; SHA256 `43a305a9985bf82b43cde3909ba4583c85945d4a52addc8ac876007a22434735`; RMS -10.919 dBFS; 4x peak -1.829 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MagicBolt.wav`
- Asset ID: doll-theater-0253-magicbolt
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; Beams/PortalFire, Beams/CoreSalvoFire, CoreHit; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.215011s; SHA256 `e8b8e9f469d882d8fc6746441e5da19d0690ea3999f2de5aa00915a00f7eaacf`; RMS -14.858 dBFS; 4x peak -1.31 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MagicCharge.wav`
- Asset ID: doll-theater-0253-magiccharge
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; Beams/ChargeRush, Beams/PortalCharge; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.8s; SHA256 `74f578f81be0935e89150d472f1d4eda59b0a8c8cd88caad4e90d8674e8e55db`; RMS -12.815 dBFS; 4x peak -1.308 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MagicFire.wav`
- Asset ID: doll-theater-0253-magicfire
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; Beams/PortalFire, Beams/CoreSalvoFire, Beams/SpreadRay; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.404989s; SHA256 `ab85c2fbd76dbcfd6675d6f84026cbe6fcb43f73513fb0b32cff51a2d809de25`; RMS -13.593 dBFS; 4x peak -1.309 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MagicMerge.wav`
- Asset ID: doll-theater-0253-magicmerge
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; ShellMassShed, Beams/ChargeGather, CoreHit; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.366667s; SHA256 `050eafe96fec1f6fbc20ac7886bcab089ebd85108bc4710409a3c50b572c8238`; RMS -13.099 dBFS; 4x peak -1.301 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MagicSigil.wav`
- Asset ID: doll-theater-0253-magicsigil
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; ShellMassLatch, CoreHit; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.22s; SHA256 `fd07a9d835abe36005d622e0fd88b84c7a3b0c3f1b246e69155a4ccc29a2e295`; RMS -14.229 dBFS; 4x peak -1.293 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MeridianSustain.wav`
- Asset ID: doll-theater-0253-meridiansustain
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters and retained loop beds; MeridianSustain, Beams/CoreSalvoFire; no external recording
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_energy.py plus remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: accepted portal-pressure layers, weapon-sized rates/EQ, micro-held intake, transient/body/short attached release; gameplay cue clocks unchanged
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 4s; SHA256 `b3fd1d5f1c2396c777623fc267220776ba54e4cb2f9bc8a03ed7c8bd3be58696`; RMS -11.879 dBFS; 4x peak -1.83 dBFS. Exact inputs and output metrics: docs/evidence/2026-09-14-weapon-energy-assets.json. Prior masters retained externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/RangedCharge.wav`
- Asset ID: doll-theater-0253-rangedcharge
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; Beams/ChargeGather, Beams/ChargeRush, PylonHit; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.8s; SHA256 `f499e542f585fa08697346fcfa60a8768677ab9104d592cb39c1308e815dbb64`; RMS -14.021 dBFS; 4x peak -1.309 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/RangedFire.wav`
- Asset ID: doll-theater-0253-rangedfire
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; HandCrushImpact, Beams/WideFire, PylonHit; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.35s; SHA256 `8d1df5469899a388a6ce51e4945845b5f71b366cf6a1ff0b265aa9ff3e68598c`; RMS -13.812 dBFS; 4x peak -1.309 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/RangedLatch.wav`
- Asset ID: doll-theater-0253-rangedlatch
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; PylonHit, ShellMassLatch; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.17s; SHA256 `c84192e1b289f66aa356c0a48e87b9191bcc7988c430992d2905a806325dd2d3`; RMS -13.367 dBFS; 4x peak -1.308 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/RangedShot.wav`
- Asset ID: doll-theater-0253-rangedshot
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; HandCrushImpact, PylonHit, SwordImpale; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.16s; SHA256 `30231cfd1fe46d6fa47f98dcd20f90ef1eeb8ceadd1e4bf58679c1d5b003fd3e`; RMS -13.577 dBFS; 4x peak -1.3 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/WeaponHit.wav`
- Asset ID: doll-theater-0253-weaponhit
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; PylonHit, CoreHit; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.135011s; SHA256 `0b91af8b76f4abb16037873f6a8cf154d5c207c499ae2248d89877374d21a4f6`; RMS -13.642 dBFS; 4x peak -1.309 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/WitnessDraw.wav`
- Asset ID: doll-theater-0253-witnessdraw
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; BladeUnsheathe, Beams/WideFire, CoreHit; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.26s; SHA256 `7e7e5b8826fc7c2a0193bf8ddb283899ea11342cadea3575bae87d3ab490f14c`; RMS -13.333 dBFS; 4x peak -1.31 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/WitnessFire.wav`
- Asset ID: doll-theater-0253-witnessfire
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; BladeUnsheathe, HandCrushImpact, PylonHit; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.36s; SHA256 `a71243b0a21a11bac35c8b786c77a3259672ef1c6f3c4403339ae5b963b257f7`; RMS -14.091 dBFS; 4x peak -1.31 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/WitnessLock.wav`
- Asset ID: doll-theater-0253-witnesslock
- Asset type: audio
- Creator: project-authored Raid DSP sounds, weapon-duration derivative editing by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: project-owned Raid masters; Beams/ChargeLock, PylonHit; no reference recording samples
- Tool/model/version: Python/NumPy 2.3.5; tools/remix_weapon_impact.py with remix_weapon_foley.py helpers; 44.1kHz PCM16
- Human modifications: one fast physical cut/strike, low/mid pressure, bounded attached air and silence at endpoints; no second singing note
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex numerical decode/peak/endpoint checks, 2026-09-14; user listening pending
- Notes: 0.366667s; SHA256 `fe0b45b04058c54eb66055ca191460fdef3da1879cf7769a22bf3aab34235b04`; RMS -14.125 dBFS; 4x peak -1.304 dBFS. Exact inputs/metrics: docs/evidence/2026-09-14-weapon-impact-assets.json. Previous masters archived externally.


### First Severance expanded authored frames — 0.2.52

- Runtime file: `Assets/Textures/NPCs/DollTheater/RemoteClawFrames.png`
- Asset ID: first-severance-remote-claw-0252
- Asset type: Boss animation atlas
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-12
- Source type: generated
- Source work and URL: previous project-owned originals; tools/asset_recipes/first_severance_doll_frames_0252.json
- Tool/model/version: built-in image_gen; backend model/seed not exposed; PowerShell/System.Drawing mechanical export
- Human modifications: 16 authored poses selected; chroma-key extraction, common-scale fixed-pivot/foot registration, nearest sampling
- License and redistribution terms: project license undecided; existing public-release gate retained
- Required attribution: preserve provenance; no third-party game texture imported
- Reviewer and review date: Codex 2026-09-12; source and packed pose/contact-sheet inspection; in-game acceptance pending
- Notes: runtime SHA256 `d1c8390fce0bcf4e9c772a50bac6dc4a801b56305fc30bb72555fb5698b45ba1`; preceding originals are preserved externally and preceding records remain historical.

- Runtime file: `Assets/Textures/NPCs/DollTheater/RestraintFrames.png`
- Asset ID: first-severance-restraint-0252
- Asset type: Boss animation atlas
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-12
- Source type: generated
- Source work and URL: previous project-owned originals; tools/asset_recipes/first_severance_doll_frames_0252.json
- Tool/model/version: built-in image_gen; backend model/seed not exposed; PowerShell/System.Drawing mechanical export
- Human modifications: 16 authored poses selected; chroma-key extraction, common-scale fixed-pivot/foot registration, nearest sampling
- License and redistribution terms: project license undecided; existing public-release gate retained
- Required attribution: preserve provenance; no third-party game texture imported
- Reviewer and review date: Codex 2026-09-12; source and packed pose/contact-sheet inspection; in-game acceptance pending
- Notes: runtime SHA256 `04c5416a88974c6cf89ebcba2b0dc032c0d9377e53d36d36b3a0954c350fede0`; preceding originals are preserved externally and preceding records remain historical.

- Runtime file: `Assets/Textures/NPCs/DollTheater/DollAttendant.png`
- Asset ID: first-severance-npc-mannerisms-0252
- Asset type: native NPC expression/gesture strip
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-12
- Source type: generated
- Source work and URL: previous project-owned originals; tools/asset_recipes/first_severance_doll_frames_0252.json
- Tool/model/version: built-in image_gen; backend model/seed not exposed; PowerShell/System.Drawing mechanical export
- Human modifications: 12 authored poses selected; chroma-key extraction, common-scale fixed-pivot/foot registration, nearest sampling, 32-colour quantization
- License and redistribution terms: project license undecided; existing public-release gate retained
- Required attribution: preserve provenance; no third-party game texture imported
- Reviewer and review date: Codex 2026-09-12; source and packed pose/contact-sheet inspection; in-game acceptance pending
- Notes: runtime SHA256 `2a5cca62c2f8376533a4f42bfc4ce799d4a304e81c5873e516414063d8ddb839`; preceding originals are preserved externally and preceding records remain historical.

Exact built-in prompts, original hashes, dimensions and re-export arguments: [0.2.52 recipe](../tools/asset_recipes/first_severance_doll_frames_0252.json). The following 0.2.51/0.2.48 entries describe the preceding runtime revisions, not the expanded atlases.

### First Severance remote claw / restraint animation — 0.2.51

- Historical runtime file: `Assets/Textures/NPCs/DollTheater/RemoteClawFrames.png`
- Asset ID: first-severance-remote-claw-frames-20260912
- Asset type: remote hand animation atlas
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-12
- Source type: generated
- Source work and URL: project-owned NullCantorRigAtlas identity reference; tools/asset_recipes/first_severance_doll_frames.json
- Tool/model/version: built-in image_gen; backend model/seed unavailable; PowerShell/System.Drawing mechanical export
- Human modifications: eight-pose selection, matte/alpha cleanup, fixed-pivot registration and common-scale atlas packing
- License and redistribution terms: project license remains undecided; existing public-release gate retained
- Required attribution: retain this provenance; no third-party game asset imported
- Reviewer and review date: Codex 2026-09-12; frame silhouettes/alpha and registration inspected; in-game acceptance pending
- Notes: eight distinct drawings, not eight rotations of one image; source originals archived externally; shared original atlas retained.

- Historical runtime file: `Assets/Textures/NPCs/DollTheater/RestraintFrames.png`
- Asset ID: first-severance-restraint-frames-20260912
- Asset type: Boss torso animation atlas
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-12
- Source type: generated
- Source work and URL: project-owned NullCantorRigAtlas identity reference; tools/asset_recipes/first_severance_doll_frames.json
- Tool/model/version: built-in image_gen; backend model/seed unavailable; PowerShell/System.Drawing mechanical export
- Human modifications: eight-pose selection, matte/alpha cleanup, fixed-pivot registration and common-scale atlas packing
- License and redistribution terms: project license remains undecided; existing public-release gate retained
- Required attribution: retain this provenance; no third-party game asset imported
- Reviewer and review date: Codex 2026-09-12; frame silhouettes/alpha and registration inspected; in-game acceptance pending
- Notes: eight distinct drawings, not eight rotations of one image; source originals archived externally; shared original atlas retained.

Exact generation and matte-correction prompts, export entry and dimensions: [frame recipe](../tools/asset_recipes/first_severance_doll_frames.json). Generated opaque checkerboards were rejected; the selected green-matte originals were keyed mechanically. No animation frame is synthesized by rotating the original art. Rig movement remains a separate client presentation layer.


### First Severance Doll Theater — 0.2.48

- Assets:
  - `Assets/Textures/NPCs/DollTheater/DollAttendant.png`
  - `Assets/Textures/NPCs/DollTheater/DollRigAtlas.png`
  - `Assets/Textures/NPCs/DollTheater/DollCoffin.png`
  - `Assets/Textures/NPCs/DollTheater/DollHead.png`
- New AI-generated designs produced with Codex's built-in image-generation tool. Backend model/seed are not exposed; do not label them as a particular GPT Image version. User-supplied three-panel concept is a private thematic reference, not a redistributed source texture. No Orchis/Avatar or other game's asset/code is imported.
- Prompt direction: sorrowful fully clothed white-haired Gothic ball-jointed girl doll, uneven suspension, porcelain coffin; native NPC silhouettes and separate Boss rig parts. Exact prompts, rejected-alpha handling, input/output hashes and export recipe: [first_severance_doll.json](../tools/asset_recipes/first_severance_doll.json).
- Generated opaque checkerboards were rejected. Final NPC/rig source images use a generated green matte for mechanical keying. The shell source has true exterior alpha and dark interior material. `tools/prepare_doll_assets.ps1` performs key/nearest/32-colour export and portrait crop; it preserves all originals outside the package.
- Runtime output: NPC 32×104 (2 frames), rig 384×384 (9 cells), coffin 256×256, portrait 34×34. Shared-pose offline preview is reproducible through `tools/preview_doll_theater.ps1`; it is not a game screenshot or performance proof.
- Rights/provenance status: original generated production proposals; the project's overall license selection/publication gate is unchanged. User concept and high-resolution originals stay outside the public package. No new third-party license is asserted or inferred.
- Legacy shell/rig/background/weapon originals are retained. This batch does not change the EigHt music attribution, audio files, third-party terms or existing accepted attack textures.

- Historical runtime file: `Assets/Textures/NPCs/DollTheater/DollAttendant.png`
- Asset ID: first-severance-doll-attendant-20260911
- Asset type: conversation NPC texture
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-11
- Source type: generated
- Source work and URL: user-directed original; exact prompts/source hashes in tools/asset_recipes/first_severance_doll.json
- Tool/model/version: built-in image_gen; backend model/seed unavailable; PowerShell/System.Drawing mechanical export
- Human modifications: reference direction, rig pivots, palette/alpha export and native crop; see Doll Theater batch note
- License and redistribution terms: project license remains undecided; existing public-release gate retained
- Required attribution: retain this provenance; no third-party game asset imported
- Reviewer and review date: Codex 2026-09-11; offline cutout/palette/dimensions/shared pose inspected; game acceptance pending
- Notes: runtime SHA256 `82629e1ef9fcb921d030ae14839d4e274cbb25b197cea9146e70c0f8738d36fe`; original retained externally.

- Runtime file: `Assets/Textures/NPCs/DollTheater/DollRigAtlas.png`
- Asset ID: first-severance-doll-rig-20260911
- Asset type: segmented Boss rig atlas
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-11
- Source type: generated
- Source work and URL: user-directed original; exact prompts/source hashes in tools/asset_recipes/first_severance_doll.json
- Tool/model/version: built-in image_gen; backend model/seed unavailable; PowerShell/System.Drawing mechanical export
- Human modifications: reference direction, rig pivots, palette/alpha export and native crop; see Doll Theater batch note
- License and redistribution terms: project license remains undecided; existing public-release gate retained
- Required attribution: retain this provenance; no third-party game asset imported
- Reviewer and review date: Codex 2026-09-11; offline cutout/palette/dimensions/shared pose inspected; game acceptance pending
- Notes: runtime SHA256 `92510d3977e38d4c964a0758db115c117aaae0aa8fc2c2ea222b73b36dd64ae4`; original retained externally.

- Runtime file: `Assets/Textures/NPCs/DollTheater/DollCoffin.png`
- Asset ID: first-severance-doll-coffin-20260911
- Asset type: hinged Boss coffin texture
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-11
- Source type: generated
- Source work and URL: user-directed original; exact prompts/source hashes in tools/asset_recipes/first_severance_doll.json
- Tool/model/version: built-in image_gen; backend model/seed unavailable; PowerShell/System.Drawing mechanical export
- Human modifications: reference direction, rig pivots, palette/alpha export and native crop; see Doll Theater batch note
- License and redistribution terms: project license remains undecided; existing public-release gate retained
- Required attribution: retain this provenance; no third-party game asset imported
- Reviewer and review date: Codex 2026-09-11; offline cutout/palette/dimensions/shared pose inspected; game acceptance pending
- Notes: runtime SHA256 `8ee2ec8e8efead390c1f6c58b5c0d6370e43375ab5fa4381c73a3d62d7c41901`; original retained externally.

- Runtime file: `Assets/Textures/NPCs/DollTheater/DollHead.png`
- Asset ID: first-severance-doll-portrait-20260911
- Asset type: native Boss-bar portrait
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-11
- Source type: generated
- Source work and URL: user-directed original; exact prompts/source hashes in tools/asset_recipes/first_severance_doll.json
- Tool/model/version: built-in image_gen; backend model/seed unavailable; PowerShell/System.Drawing mechanical export
- Human modifications: reference direction, rig pivots, palette/alpha export and native crop; see Doll Theater batch note
- License and redistribution terms: project license remains undecided; existing public-release gate retained
- Required attribution: retain this provenance; no third-party game asset imported
- Reviewer and review date: Codex 2026-09-11; offline cutout/palette/dimensions/shared pose inspected; game acceptance pending
- Notes: runtime SHA256 `5eafc55f85b6b06b488db04597e725e674c54bb2dd1a7529e9b617b3013988b0`; original retained externally.


### Ritual grand apparatus v3 — 2026-09-09

Four separate built-in image generations, text only. No input/reference images; model identifier unavailable and the owner explicitly accepted that limitation. Original PNGs remain in the local generated-image archive. Mechanical export via `tools/export_ritual_icons.py` preserves alpha and fits128px icons/512px apparatus to116px/464px envelopes; no image-to-image step or reuse of older art. Older assets remain untouched. The following is the exact shared prompt, with `{SUBJECT}` replaced by the per-item brief below:

```text
Use case: stylized-concept. Asset type: single high-resolution 2D game inventory weapon icon for an original sinister and solemn cosmic ritual action game. Generate a brand-new design from text only, no reference images and no adaptation of previous images. Subject: {SUBJECT} Style: exquisitely detailed painted hard-surface artifact, sharp bevel highlights, complex but coherent material, strong thick silhouette that remains readable reduced to 40 pixels. Composition: exactly one isolated complete object centered in a square, fills roughly 85% of frame, full object unclipped, slight 3/4 orthographic view. Background: genuinely transparent alpha including gaps in the object. No cast shadow, no floor, no environment, no checkerboard baked into pixels, no border, no words, no labels, no watermarks, no particles or diffuse glow outside the silhouette, no hands or people. Keep all fine detail inside strong large material masses. Original design only, not matching any franchise weapon or palette.
```

- LacunaTestament subject: a levitating forbidden mechanical grimoire. An open asymmetric folio of smoked silver metal leaves frames a vertical black glass slit, with precisely engraved concentric ultraviolet iris mechanisms, chunky pale broken-porcelain corners, two lifted metal pages. Compact broad silhouette, three-quarter view. Restrained violet and frosted silver highlights; no yellow or gold.
- PaleMeridian subject: an ominous folded siege railgun relic. A long diagonal gun silhouette with a dense offset double rail, three interlocking metal ribs, dark ceramic stock and broken ivory casing, narrow cyan luminous induction coils. Mechanical gothic space weapon, not a real firearm replica, no sword. Powerful readable long silhouette, barrel points upper-right.
- ChoirOfTheUnmade subject: a summoning censer shaped like a suspended inhuman pipe-organ reliquary. Seven uneven narrow silver organ towers embrace a milky glass heart inside a fractured porcelain oval cage, short black metal handle beneath. Cold ivory light and very restrained antique bronze hardware, no flame, no background. Distinct tall compact crown silhouette.
- LastWitness subject: an occult throwing weapon: a dense obsidian triangular prism with three asymmetric hooked silver cutting fins, a faceted violet glass chamber and fine mechanical perforations. Compact three-armed bladed relic, not a sword, not a shuriken from any existing series. Heavy irregular silver and black material, tiny electric lavender fissures.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V3/LacunaTestament.png`
- Asset ID: ritual-v3-lacunatestament-20260909
- Asset type: weapon icon
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: new text-only original; shared prompt and LacunaTestament brief above
- Tool/model/version: built-in image_gen; backend model not exposed; Python3/Pillow mechanical export
- Human modifications: user concept/direction; agent prompt and alpha-preserving size integration, no old image input
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: retain this provenance; no third-party work imported
- Reviewer and review date: Codex, 2026-09-09; native PNG alpha/dimensions and40px silhouette preview inspected; game acceptance pending
- Notes: original SHA256 `8be34b90881b51fd8937814513d721612c277571492f94374b619dda81748f8c`; export SHA256 `0c174c91813448d50d7a865e925760c7ab2e9c37c539f5374ce0f35522cdefcc`. Original retained externally.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V3/LacunaTestament_Apparatus.png`
- Asset ID: ritual-v3-lacunatestament_apparatus-20260909
- Asset type: weapon apparatus texture
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: new text-only original; shared prompt and LacunaTestament brief above
- Tool/model/version: built-in image_gen; backend model not exposed; Python3/Pillow mechanical export
- Human modifications: user concept/direction; agent prompt and alpha-preserving size integration, no old image input
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: retain this provenance; no third-party work imported
- Reviewer and review date: Codex, 2026-09-09; native PNG alpha/dimensions and40px silhouette preview inspected; game acceptance pending
- Notes: original SHA256 `8be34b90881b51fd8937814513d721612c277571492f94374b619dda81748f8c`; export SHA256 `486f3fdcc0ded56f784c803a17279ab91f53a627d11bb50e076d4d183ae9b52c`. Original retained externally.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V3/PaleMeridian.png`
- Asset ID: ritual-v3-palemeridian-20260909
- Asset type: weapon icon
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: new text-only original; shared prompt and PaleMeridian brief above
- Tool/model/version: built-in image_gen; backend model not exposed; Python3/Pillow mechanical export
- Human modifications: user concept/direction; agent prompt and alpha-preserving size integration, no old image input
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: retain this provenance; no third-party work imported
- Reviewer and review date: Codex, 2026-09-09; native PNG alpha/dimensions and40px silhouette preview inspected; game acceptance pending
- Notes: original SHA256 `559e6d6d996983baa8238637e285f823ae812577e7e47664f2ac724a131ead95`; export SHA256 `4622f88829533fae9bdc77efdf08be8927a4223e9f8c9fb68f6930323e5cfeaf`. Original retained externally.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V3/PaleMeridian_Apparatus.png`
- Asset ID: ritual-v3-palemeridian_apparatus-20260909
- Asset type: weapon apparatus texture
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: new text-only original; shared prompt and PaleMeridian brief above
- Tool/model/version: built-in image_gen; backend model not exposed; Python3/Pillow mechanical export
- Human modifications: user concept/direction; agent prompt and alpha-preserving size integration, no old image input
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: retain this provenance; no third-party work imported
- Reviewer and review date: Codex, 2026-09-09; native PNG alpha/dimensions and40px silhouette preview inspected; game acceptance pending
- Notes: original SHA256 `559e6d6d996983baa8238637e285f823ae812577e7e47664f2ac724a131ead95`; export SHA256 `4d795916d56846f66e6f75b252639c26284d94b88eaf5d2d4d93d570df44a683`. Original retained externally.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V3/ChoirOfTheUnmade.png`
- Asset ID: ritual-v3-choiroftheunmade-20260909
- Asset type: weapon icon
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: new text-only original; shared prompt and ChoirOfTheUnmade brief above
- Tool/model/version: built-in image_gen; backend model not exposed; Python3/Pillow mechanical export
- Human modifications: user concept/direction; agent prompt and alpha-preserving size integration, no old image input
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: retain this provenance; no third-party work imported
- Reviewer and review date: Codex, 2026-09-09; native PNG alpha/dimensions and40px silhouette preview inspected; game acceptance pending
- Notes: original SHA256 `588cda85f4084cb953af73804eef4253e9f21f1004f812e6c3782d5772c781e9`; export SHA256 `95f03d977dd90ec03b6f705f6d45f3e7be91ecde1a6cc9a4dd39ac05c73153c0`. Original retained externally.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V3/ChoirOfTheUnmade_Apparatus.png`
- Asset ID: ritual-v3-choiroftheunmade_apparatus-20260909
- Asset type: weapon apparatus texture
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: new text-only original; shared prompt and ChoirOfTheUnmade brief above
- Tool/model/version: built-in image_gen; backend model not exposed; Python3/Pillow mechanical export
- Human modifications: user concept/direction; agent prompt and alpha-preserving size integration, no old image input
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: retain this provenance; no third-party work imported
- Reviewer and review date: Codex, 2026-09-09; native PNG alpha/dimensions and40px silhouette preview inspected; game acceptance pending
- Notes: original SHA256 `588cda85f4084cb953af73804eef4253e9f21f1004f812e6c3782d5772c781e9`; export SHA256 `f97db6ad21c24a4a0d0bc78c9d1cda3754097d2ab100b764eeac2c5aa9e9258a`. Original retained externally.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V3/LastWitness.png`
- Asset ID: ritual-v3-lastwitness-20260909
- Asset type: weapon icon
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: new text-only original; shared prompt and LastWitness brief above
- Tool/model/version: built-in image_gen; backend model not exposed; Python3/Pillow mechanical export
- Human modifications: user concept/direction; agent prompt and alpha-preserving size integration, no old image input
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: retain this provenance; no third-party work imported
- Reviewer and review date: Codex, 2026-09-09; native PNG alpha/dimensions and40px silhouette preview inspected; game acceptance pending
- Notes: original SHA256 `b2e22b99f49b64c0ce1fe25037e3224b02315f4ded53c7c94b0e6b4bc7a730ce`; export SHA256 `a8eea9d2f9f67721fc40deeb90a187746a5018f0fce062c92a620a9b17380ab4`. Original retained externally.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V3/LastWitness_Apparatus.png`
- Asset ID: ritual-v3-lastwitness_apparatus-20260909
- Asset type: weapon apparatus texture
- Creator: project-directed OpenAI image generation
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: new text-only original; shared prompt and LastWitness brief above
- Tool/model/version: built-in image_gen; backend model not exposed; Python3/Pillow mechanical export
- Human modifications: user concept/direction; agent prompt and alpha-preserving size integration, no old image input
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: retain this provenance; no third-party work imported
- Reviewer and review date: Codex, 2026-09-09; native PNG alpha/dimensions and40px silhouette preview inspected; game acceptance pending
- Notes: original SHA256 `b2e22b99f49b64c0ce1fe25037e3224b02315f4ded53c7c94b0e6b4bc7a730ce`; export SHA256 `07fae58c1ef765981c3e3fadb5af1183dcd6afa4c6615304fd7093d3085bdffb`. Original retained externally.

### Ritual sustained voices — 2026-09-09

- Runtime file: `Assets/Sounds/FirstSeverance/LacunaSustain.wav`
- Asset ID: ritual-lacunasustain-20260909
- Asset type: weapon sound loop
- Creator: project-authored independent synthesis by Codex
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no samples or third-party recordings
- Tool/model/version: Python3/NumPy; `tools/generate_ritual_sustain.py`
- Human modifications: original periodic harmonic/noise-band design, stereo placement, bounded gain
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex PCM/finite/peak/loop-boundary checks,2026-09-09; human mix review pending
- Notes: four-second stereo44.1kHz PCM16; periodic frequencies; peak0.68. Continuous body, not repeated launch accents.

- Runtime file: `Assets/Sounds/FirstSeverance/MeridianSustain.wav`
- Asset ID: ritual-meridiansustain-20260909
- Asset type: weapon sound loop
- Creator: project-authored independent synthesis by Codex
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no samples or third-party recordings
- Tool/model/version: Python3/NumPy; `tools/generate_ritual_sustain.py`
- Human modifications: original periodic harmonic/noise-band design, stereo placement, bounded gain
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex PCM/finite/peak/loop-boundary checks,2026-09-09; human mix review pending
- Notes: four-second stereo44.1kHz PCM16; periodic frequencies; peak0.68. Continuous body, not repeated launch accents.

- Runtime file: `Assets/Sounds/FirstSeverance/ChoirSustain.wav`
- Asset ID: ritual-choirsustain-20260909
- Asset type: weapon sound loop
- Creator: project-authored independent synthesis by Codex
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no samples or third-party recordings
- Tool/model/version: Python3/NumPy; `tools/generate_ritual_sustain.py`
- Human modifications: original periodic harmonic/noise-band design, stereo placement, bounded gain
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex PCM/finite/peak/loop-boundary checks,2026-09-09; human mix review pending
- Notes: four-second stereo44.1kHz PCM16; periodic frequencies; peak0.68. Continuous body, not repeated launch accents.

### Ritual apparatus v2 — full-color material assemblies / 2026-09-09

- Runtime file: `Assets/Textures/Items/RitualArmaments/V2/ChoirOfTheUnmade.png`
- Asset ID: ritual-v2-choiroftheunmade-2026-09-09
- Asset type: weapon icon
- Creator: project-directed independent geometry/composition by ChatGPT; original P3 material credits retained
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: existing project Assets/Textures/NPCs/NullCantorRigAtlas.png; see its original ImageGen provenance; source SHA256 b3485d1f3a7d66f99f78fed01da34f1e6e52ec32fcd57e9aa46afb6584afe640
- Tool/model/version: Python3 / Pillow11.3.0; no new image-generation model was available or invoked
- Human modifications: minami direction; agent-authored composition, masking, rigid-part layout and full RGBA export; no palette reduction
- License and redistribution terms: existing project rights undecided; development branch only, no public-release approval
- Required attribution: retain original P3 source and this derivative entry; no new third-party requirement
- Reviewer and review date: ChatGPT PNG decode, dimensions/alpha and visual atlas inspection,2026-09-09; game review pending
- Notes: export SHA256 `d4c71e0a83582f063bf17834a4b836d7529ac6067a2884d0b38fe9c8fdc799e4`. External explicit-input/output recipe `make_art.py` SHA256 `673e8f8832eb4bf5221f35ac607d9c1edfbf7069a32483b98c4b8ec55ddc4f65`. All predecessors preserved. Runtime code animates independent atlas regions, never the small icon.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V2/LacunaTestament.png`
- Asset ID: ritual-v2-lacunatestament-2026-09-09
- Asset type: weapon icon
- Creator: project-directed independent geometry/composition by ChatGPT; original P3 material credits retained
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: existing project Assets/Textures/NPCs/NullCantorRigAtlas.png; see its original ImageGen provenance; source SHA256 b3485d1f3a7d66f99f78fed01da34f1e6e52ec32fcd57e9aa46afb6584afe640
- Tool/model/version: Python3 / Pillow11.3.0; no new image-generation model was available or invoked
- Human modifications: minami direction; agent-authored composition, masking, rigid-part layout and full RGBA export; no palette reduction
- License and redistribution terms: existing project rights undecided; development branch only, no public-release approval
- Required attribution: retain original P3 source and this derivative entry; no new third-party requirement
- Reviewer and review date: ChatGPT PNG decode, dimensions/alpha and visual atlas inspection,2026-09-09; game review pending
- Notes: export SHA256 `3a50b74415971088b0abd0e5477156a163cd80917adee731072edef2ee7b8bb5`. External explicit-input/output recipe `make_art.py` SHA256 `673e8f8832eb4bf5221f35ac607d9c1edfbf7069a32483b98c4b8ec55ddc4f65`. All predecessors preserved. Runtime code animates independent atlas regions, never the small icon.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V2/LastWitness.png`
- Asset ID: ritual-v2-lastwitness-2026-09-09
- Asset type: weapon icon
- Creator: project-directed independent geometry/composition by ChatGPT; original P3 material credits retained
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: existing project Assets/Textures/NPCs/NullCantorRigAtlas.png; see its original ImageGen provenance; source SHA256 b3485d1f3a7d66f99f78fed01da34f1e6e52ec32fcd57e9aa46afb6584afe640
- Tool/model/version: Python3 / Pillow11.3.0; no new image-generation model was available or invoked
- Human modifications: minami direction; agent-authored composition, masking, rigid-part layout and full RGBA export; no palette reduction
- License and redistribution terms: existing project rights undecided; development branch only, no public-release approval
- Required attribution: retain original P3 source and this derivative entry; no new third-party requirement
- Reviewer and review date: ChatGPT PNG decode, dimensions/alpha and visual atlas inspection,2026-09-09; game review pending
- Notes: export SHA256 `b015acd90c146e681e9c64ed3cddc4e36f2a19b5fb05546e04dfac8f3d83afcd`. External explicit-input/output recipe `make_art.py` SHA256 `673e8f8832eb4bf5221f35ac607d9c1edfbf7069a32483b98c4b8ec55ddc4f65`. All predecessors preserved. Runtime code animates independent atlas regions, never the small icon.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V2/PaleMeridian.png`
- Asset ID: ritual-v2-palemeridian-2026-09-09
- Asset type: weapon icon
- Creator: project-directed independent geometry/composition by ChatGPT; original P3 material credits retained
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: existing project Assets/Textures/NPCs/NullCantorRigAtlas.png; see its original ImageGen provenance; source SHA256 b3485d1f3a7d66f99f78fed01da34f1e6e52ec32fcd57e9aa46afb6584afe640
- Tool/model/version: Python3 / Pillow11.3.0; no new image-generation model was available or invoked
- Human modifications: minami direction; agent-authored composition, masking, rigid-part layout and full RGBA export; no palette reduction
- License and redistribution terms: existing project rights undecided; development branch only, no public-release approval
- Required attribution: retain original P3 source and this derivative entry; no new third-party requirement
- Reviewer and review date: ChatGPT PNG decode, dimensions/alpha and visual atlas inspection,2026-09-09; game review pending
- Notes: export SHA256 `3b2d26190e28e9629e7fca02291f51f00c9400245bd141dbe04ae005b5101ea5`. External explicit-input/output recipe `make_art.py` SHA256 `673e8f8832eb4bf5221f35ac607d9c1edfbf7069a32483b98c4b8ec55ddc4f65`. All predecessors preserved. Runtime code animates independent atlas regions, never the small icon.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V2/ReliquaryAssemblies.png`
- Asset ID: ritual-v2-reliquaryassemblies-2026-09-09
- Asset type: weapon assembly atlas
- Creator: project-directed independent geometry/composition by ChatGPT; original P3 material credits retained
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: existing project Assets/Textures/NPCs/NullCantorRigAtlas.png; see its original ImageGen provenance; source SHA256 b3485d1f3a7d66f99f78fed01da34f1e6e52ec32fcd57e9aa46afb6584afe640
- Tool/model/version: Python3 / Pillow11.3.0; no new image-generation model was available or invoked
- Human modifications: minami direction; agent-authored composition, masking, rigid-part layout and full RGBA export; no palette reduction
- License and redistribution terms: existing project rights undecided; development branch only, no public-release approval
- Required attribution: retain original P3 source and this derivative entry; no new third-party requirement
- Reviewer and review date: ChatGPT PNG decode, dimensions/alpha and visual atlas inspection,2026-09-09; game review pending
- Notes: export SHA256 `fceaf736393efec231944657e935cd09a060f7c9b1d6d7000d0726721be5efcd`. External explicit-input/output recipe `make_art.py` SHA256 `673e8f8832eb4bf5221f35ac607d9c1edfbf7069a32483b98c4b8ec55ddc4f65`. All predecessors preserved. Runtime code animates independent atlas regions, never the small icon.

- Runtime file: `Assets/Textures/Items/RitualArmaments/V2/RibbonFeather.png`
- Asset ID: ritual-v2-ribbonfeather-2026-09-09
- Asset type: VFX feather texture
- Creator: project-directed independent geometry/composition by ChatGPT; original P3 material credits retained
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: analytic edge feather and deterministic sinusoidal grain, no imported material
- Tool/model/version: Python3 / Pillow11.3.0; no new image-generation model was available or invoked
- Human modifications: minami direction; agent-authored composition, masking, rigid-part layout and full RGBA export; no palette reduction
- License and redistribution terms: existing project rights undecided; development branch only, no public-release approval
- Required attribution: retain original P3 source and this derivative entry; no new third-party requirement
- Reviewer and review date: ChatGPT PNG decode, dimensions/alpha and visual atlas inspection,2026-09-09; game review pending
- Notes: export SHA256 `095d2942363202822d2ffa0a8695e42790950652b3b6f1a544a5b212a23bb4a2`. External explicit-input/output recipe `make_art.py` SHA256 `673e8f8832eb4bf5221f35ac607d9c1edfbf7069a32483b98c4b8ec55ddc4f65`. All predecessors preserved. Runtime code animates independent atlas regions, never the small icon.


### Null Cantor claw inventory icon — 0.2.29

- Runtime file: `Assets/Textures/Items/RitualArmaments/NullCantorClaws.png`
- Asset ID: null-cantor-claws-icon-2026-09-09
- Asset type: weapon texture
- Creator: project original P3 rig with ImageGen assistance; independent project-authored icon composition
- Creation/acquisition date: 2026-09-09
- Source type: generated
- Source work and URL: existing `Assets/Textures/NPCs/NullCantorRigAtlas.png`, original record retained; no third-party input
- Tool/model/version: Pillow11.3.0, RGBA crop, reflection, bicubic rotation and Lanczos128x128 export
- Human modifications: user-directed P3 dual-claw design; agent composed the inventory-only derivative; original atlas unchanged
- License and redistribution terms: existing project asset terms remain undecided; development branch only, no publication approval
- Required attribution: retain original P3 rig provenance and this derivative record
- Reviewer and review date: source alpha/region inspection and output decode,2026-09-09; in-game acceptance pending
- Notes: no palette reduction; SHA256 `634fabd74d575c5126284b1f4c68b8cde76d8f8c3cf773acc600027c037d24bc`. Runtime hand animation samples the original high-resolution atlas, not this inventory icon. Recipe is retained in the feature-branch assembly commit history and the external task working files.


### Publication emblem — 0.2.28 / 2026-09-08

- Runtime file: `icon.png`
- Asset ID: convergence-publication-icon-small
- Asset type: texture
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-08
- Source type: generated
- Source work and URL: no external image; original prompt in docs/evidence/2026-09-08-spacing-publication-materials.json
- Tool/model/version: built-in image_gen tool; model version not exposed; System.Drawing high-quality bicubic size export
- Human modifications: no hand retouching; proportional fit to80×80 RGBA
- License and redistribution terms: project asset license undecided; preparation only, no public release approval
- Required attribution: retain this provenance record; no external credit specified
- Reviewer and review date: Codex master and80px thumbnail visual inspection, 2026-09-08; owner acceptance pending
- Notes: SHA25642feb67ff7ece58378ab77b8141becb26ec89bf68a3baae22c1ce706b1e8b8d4; generated master retained outside source

- Runtime file: `icon_workshop.png`
- Asset ID: convergence-publication-icon-workshop
- Asset type: texture
- Creator: OpenAI built-in image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-08
- Source type: generated
- Source work and URL: same original emblem master; no external image
- Tool/model/version: built-in image_gen tool; model version not exposed; System.Drawing high-quality bicubic size export
- Human modifications: no hand retouching; proportional fit to512×512 RGBA
- License and redistribution terms: project asset license undecided; preparation only, no public release approval
- Required attribution: retain this provenance record; no external credit specified
- Reviewer and review date: Codex master/thumbnail inspection, 2026-09-08; owner acceptance pending
- Notes: SHA256764b07ac79589c81f2963be1f070d171053300aa227197dbf1a61b8c3a62a0cc; promotional art, not a gameplay screenshot

### Five ritual armaments — 0.2.25 / 2026-09-08

- Runtime file: `Assets/Textures/Items/RitualArmaments/NullRefrain.png`
- Asset ID: ritual-nullrefrain-0225-2026-09-08
- Asset type: weapon texture
- Creator: project-directed original concept with built-in OpenAI ImageGen assistance; assistant runtime export
- Creation/acquisition date: 2026-09-08
- Source type: generated
- Source work and URL: user-approved CONVERGENCE / CONCEPT 01 board, generation d492a069-958c-4f23-9747-c26693b26d66; no third-party reference or extracted game asset
- Tool/model/version: built-in ImageGen backend not surfaced; Python/Pillow 12.3.0
- Human modifications: user approved concept; assistant cropped and alpha-masked silhouettes, rotated the staff, made 32-color transparent runtime exports; gun/book use compact exports for their draw sizes
- License and redistribution terms: project asset license undecided; development branch only, no public release approval
- Required attribution: no external requirement specified; preserve provenance
- Reviewer and review date: assistant alpha/silhouette inspection and exact-byte verification, 2026-09-08; in-game review pending
- Notes: source board SHA256 c833eeba1fc06d53951df33bce597efb29c0b52cc0fb221e73acd3586e93c797; export SHA256 2fa5b7fdc4527b2e1de211eb21053a4b1b2a1dd6e93f2419d3da93c088c2f284. Original concept and extraction recipe remain outside the repository. Existing NullRefrain.png and all Boss/audio assets are retained unchanged.

- Runtime file: `Assets/Textures/Items/RitualArmaments/PaleMeridian.png`
- Asset ID: ritual-palemeridian-0225-2026-09-08
- Asset type: weapon texture
- Creator: project-directed original concept with built-in OpenAI ImageGen assistance; assistant runtime export
- Creation/acquisition date: 2026-09-08
- Source type: generated
- Source work and URL: user-approved CONVERGENCE / CONCEPT 01 board, generation d492a069-958c-4f23-9747-c26693b26d66; no third-party reference or extracted game asset
- Tool/model/version: built-in ImageGen backend not surfaced; Python/Pillow 12.3.0
- Human modifications: user approved concept; assistant cropped and alpha-masked silhouettes, rotated the staff, made 32-color transparent runtime exports; gun/book use compact exports for their draw sizes
- License and redistribution terms: project asset license undecided; development branch only, no public release approval
- Required attribution: no external requirement specified; preserve provenance
- Reviewer and review date: assistant alpha/silhouette inspection and exact-byte verification, 2026-09-08; in-game review pending
- Notes: source board SHA256 c833eeba1fc06d53951df33bce597efb29c0b52cc0fb221e73acd3586e93c797; export SHA256 ce1699f9ec033e10ba0bbfafee1b815913fb38a105e8ed3aa3c5f77340562395. Original concept and extraction recipe remain outside the repository. Existing NullRefrain.png and all Boss/audio assets are retained unchanged.

- Runtime file: `Assets/Textures/Items/RitualArmaments/LacunaTestament.png`
- Asset ID: ritual-lacunatestament-0225-2026-09-08
- Asset type: weapon texture
- Creator: project-directed original concept with built-in OpenAI ImageGen assistance; assistant runtime export
- Creation/acquisition date: 2026-09-08
- Source type: generated
- Source work and URL: user-approved CONVERGENCE / CONCEPT 01 board, generation d492a069-958c-4f23-9747-c26693b26d66; no third-party reference or extracted game asset
- Tool/model/version: built-in ImageGen backend not surfaced; Python/Pillow 12.3.0
- Human modifications: user approved concept; assistant cropped and alpha-masked silhouettes, rotated the staff, made 32-color transparent runtime exports; gun/book use compact exports for their draw sizes
- License and redistribution terms: project asset license undecided; development branch only, no public release approval
- Required attribution: no external requirement specified; preserve provenance
- Reviewer and review date: assistant alpha/silhouette inspection and exact-byte verification, 2026-09-08; in-game review pending
- Notes: source board SHA256 c833eeba1fc06d53951df33bce597efb29c0b52cc0fb221e73acd3586e93c797; export SHA256 04291d4f16e10d848d9a222f5ec675a69b06b7f3629e09679919f64ade2fc158. Original concept and extraction recipe remain outside the repository. Existing NullRefrain.png and all Boss/audio assets are retained unchanged.

- Runtime file: `Assets/Textures/Items/RitualArmaments/ChoirOfTheUnmade.png`
- Asset ID: ritual-choiroftheunmade-0225-2026-09-08
- Asset type: weapon texture
- Creator: project-directed original concept with built-in OpenAI ImageGen assistance; assistant runtime export
- Creation/acquisition date: 2026-09-08
- Source type: generated
- Source work and URL: user-approved CONVERGENCE / CONCEPT 01 board, generation d492a069-958c-4f23-9747-c26693b26d66; no third-party reference or extracted game asset
- Tool/model/version: built-in ImageGen backend not surfaced; Python/Pillow 12.3.0
- Human modifications: user approved concept; assistant cropped and alpha-masked silhouettes, rotated the staff, made 32-color transparent runtime exports; gun/book use compact exports for their draw sizes
- License and redistribution terms: project asset license undecided; development branch only, no public release approval
- Required attribution: no external requirement specified; preserve provenance
- Reviewer and review date: assistant alpha/silhouette inspection and exact-byte verification, 2026-09-08; in-game review pending
- Notes: source board SHA256 c833eeba1fc06d53951df33bce597efb29c0b52cc0fb221e73acd3586e93c797; export SHA256 28c3a51232756ed0148884d8e58c4a7600ea6f86412ea7d2d73db98bdb5e1a17. Original concept and extraction recipe remain outside the repository. Existing NullRefrain.png and all Boss/audio assets are retained unchanged.

- Runtime file: `Assets/Textures/Items/RitualArmaments/LastWitness.png`
- Asset ID: ritual-lastwitness-0225-2026-09-08
- Asset type: weapon texture
- Creator: project-directed original concept with built-in OpenAI ImageGen assistance; assistant runtime export
- Creation/acquisition date: 2026-09-08
- Source type: generated
- Source work and URL: user-approved CONVERGENCE / CONCEPT 01 board, generation d492a069-958c-4f23-9747-c26693b26d66; no third-party reference or extracted game asset
- Tool/model/version: built-in ImageGen backend not surfaced; Python/Pillow 12.3.0
- Human modifications: user approved concept; assistant cropped and alpha-masked silhouettes, rotated the staff, made 32-color transparent runtime exports; gun/book use compact exports for their draw sizes
- License and redistribution terms: project asset license undecided; development branch only, no public release approval
- Required attribution: no external requirement specified; preserve provenance
- Reviewer and review date: assistant alpha/silhouette inspection and exact-byte verification, 2026-09-08; in-game review pending
- Notes: source board SHA256 c833eeba1fc06d53951df33bce597efb29c0b52cc0fb221e73acd3586e93c797; export SHA256 2717a20b66cda737a0c0e84fcec1c1ddee46c57852cdad6b2e9552ee697f9f94. Original concept and extraction recipe remain outside the repository. Existing NullRefrain.png and all Boss/audio assets are retained unchanged.

- Runtime file: `Assets/Textures/Items/RitualArmaments/ChoirSentinel.png`
- Asset ID: ritual-choirsentinel-0225-2026-09-08
- Asset type: minion texture
- Creator: project-directed original concept with built-in OpenAI ImageGen assistance; assistant runtime export
- Creation/acquisition date: 2026-09-08
- Source type: generated
- Source work and URL: user-approved CONVERGENCE / CONCEPT 01 board, generation d492a069-958c-4f23-9747-c26693b26d66; no third-party reference or extracted game asset
- Tool/model/version: built-in ImageGen backend not surfaced; Python/Pillow 12.3.0
- Human modifications: user approved concept; assistant cropped and alpha-masked silhouettes, rotated the staff, made 32-color transparent runtime exports; gun/book use compact exports for their draw sizes
- License and redistribution terms: project asset license undecided; development branch only, no public release approval
- Required attribution: no external requirement specified; preserve provenance
- Reviewer and review date: assistant alpha/silhouette inspection and exact-byte verification, 2026-09-08; in-game review pending
- Notes: source board SHA256 c833eeba1fc06d53951df33bce597efb29c0b52cc0fb221e73acd3586e93c797; export SHA256 d8131088aed007fefebff9724f9896e88e23d319d3bfdefc0adfc154e9ea7e47. Original concept and extraction recipe remain outside the repository. Existing NullRefrain.png and all Boss/audio assets are retained unchanged.

### Sword and actor impacts — 0.2.24 / 2026-09-07

- Runtime file: `Assets/Sounds/FirstSeverance/SwordImpale.wav`
- Asset ID: swordimpale-0224-2026-09-07
- Asset type: sound effect
- Creator: project-directed original DSP by Codex
- Creation/acquisition date: 2026-09-07
- Source type: original
- Source work and URL: .34s sliding metal insertion with brittle transient; no imported samples/recordings
- Tool/model/version: external audio0224/render.py; NumPy2.3.5/SciPy1.16.1/SoundFile0.14.0, deterministic seeds22401–22404
- Human modifications: none; human listening pending
- License and redistribution terms: project license undecided; development only, no public release approved
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex finite decode/duration/4x peak checks,2026-09-07
- Notes:48kHz mono PCM16,4x peak below0.781. Recipe SHA256 `4f75c9d12878da04738e4c2aff66746bc01a7c6da6cb7518f0a4a6655affa79b`; PCM24 auditions with actual cue/master gains, manifests and export hashes retained externally in audio0224/export.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellHit.wav`
- Asset ID: shellhit-0224-2026-09-07
- Asset type: sound effect
- Creator: project-directed original DSP by Codex
- Creation/acquisition date: 2026-09-07
- Source type: original
- Source work and URL: .28s close dissonant hard-metal modes; no imported samples/recordings
- Tool/model/version: external audio0224/render.py; NumPy2.3.5/SciPy1.16.1/SoundFile0.14.0, deterministic seeds22401–22404
- Human modifications: none; human listening pending
- License and redistribution terms: project license undecided; development only, no public release approved
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex finite decode/duration/4x peak checks,2026-09-07
- Notes:48kHz mono PCM16,4x peak below0.781. Recipe SHA256 `4f75c9d12878da04738e4c2aff66746bc01a7c6da6cb7518f0a4a6655affa79b`; PCM24 auditions with actual cue/master gains, manifests and export hashes retained externally in audio0224/export.

- Runtime file: `Assets/Sounds/FirstSeverance/CoreHit.wav`
- Asset ID: corehit-0224-2026-09-07
- Asset type: sound effect
- Creator: project-directed original DSP by Codex
- Creation/acquisition date: 2026-09-07
- Source type: original
- Source work and URL: .22s bright glass modes with staggered microcracks; no imported samples/recordings
- Tool/model/version: external audio0224/render.py; NumPy2.3.5/SciPy1.16.1/SoundFile0.14.0, deterministic seeds22401–22404
- Human modifications: none; human listening pending
- License and redistribution terms: project license undecided; development only, no public release approved
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex finite decode/duration/4x peak checks,2026-09-07
- Notes:48kHz mono PCM16,4x peak below0.781. Recipe SHA256 `4f75c9d12878da04738e4c2aff66746bc01a7c6da6cb7518f0a4a6655affa79b`; PCM24 auditions with actual cue/master gains, manifests and export hashes retained externally in audio0224/export.

- Runtime file: `Assets/Sounds/FirstSeverance/PylonHit.wav`
- Asset ID: pylonhit-0224-2026-09-07
- Asset type: sound effect
- Creator: project-directed original DSP by Codex
- Creation/acquisition date: 2026-09-07
- Source type: original
- Source work and URL: .24s low metal-plate knock; no imported samples/recordings
- Tool/model/version: external audio0224/render.py; NumPy2.3.5/SciPy1.16.1/SoundFile0.14.0, deterministic seeds22401–22404
- Human modifications: none; human listening pending
- License and redistribution terms: project license undecided; development only, no public release approved
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex finite decode/duration/4x peak checks,2026-09-07
- Notes:48kHz mono PCM16,4x peak below0.781. Recipe SHA256 `4f75c9d12878da04738e4c2aff66746bc01a7c6da6cb7518f0a4a6655affa79b`; PCM24 auditions with actual cue/master gains, manifests and export hashes retained externally in audio0224/export.

### Shell friction audio — 0.2.23 / 2026-09-07

- Runtime file: `Assets/Sounds/FirstSeverance/ShellArc.wav`
- Asset ID: shellarc-0223-2026-09-07
- Asset type: sound effect
- Creator: project-directed original DSP by Codex
- Creation/acquisition date: 2026-09-07
- Source type: original
- Source work and URL: original filtered-noise sparks, sputtering carrier and frequency-modulated buzz; no third-party samples or recording
- Tool/model/version: external audio0223/render.py; NumPy2.3.5/SciPy1.16.1/SoundFile0.14.0; deterministic seed22301
- Human modifications: none; human listening pending
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex finite decode/duration/4x peak checks,2026-09-07
- Notes:0.38s/48kHz mono PCM16,4x peak0.75999. Recipe SHA256 `1156aa64851576b6b9f5754d7436eee5c868662bdd29a7110edf7729dc076f91`; export SHA256 `bc5a549b2fb42119dc2c743f4c9e4d46740f0ddd4b8580a6b71c6ad443d3b429`. Recipe, manifest and PCM24 audition with actual cue/master gains remain external in audio0223/export. Existing music, sound masters and shell texture are unchanged.

### Execution ping revision — 0.2.22 / 2026-09-07

The existing exact `Assets/Sounds/FirstSeverance/SpreadExecution.wav` record below retains its project-original source and unresolved release-license terms. Its new0.62s/48kHz mono PCM16 master uses only analytic oscillators: a fast focusing pitch scoop,2350Hz settled tone, inharmonic upper partials and a short low onset. No samples, recordings, randomness or external source. Original Codex-authored external audio0222/render.py (`e074a1fbdc40ae201951c4b6095c5c09f832a1693563318bb487dec4959cc205`), NumPy2.3.5/SciPy1.16.1/SoundFile0.14.0. Output SHA256 `44484f4970caaa194524c6363aa5aeb96ce2b518058f67dd96769ae26efb4220`; finite decode and duration passed,4x peak0.82002. Predecessor master, manifest and PCM24 audition with actual cue/master gains remain external in audio0222/export. Human listening pending. Other audio and the original shell texture are unchanged.

### Mechanic verdicts and music-presence revision — 0.2.21 / 2026-09-07

The four existing exact music records (`Assets/Music/ObsidianLiturgy.ogg`, `Assets/Music/UnboundLiturgy.ogg`, `Assets/Music/DistantLiturgy.ogg`, `Assets/Music/TerminalLiturgy.ogg`) retain their original composition, performance, instrument-source/CC0 terms and unresolved project-release license. Original PCM24 mixes from audio0217 and Terminal's extended audio0219 master were lifted1.7× with a stereo-linked soft peak knee; decoded RMS gains4.41–4.57dB, unchanged durations, maximum4× peak0.9783. No new melody, recording, samples or raster edit. Existing runtime files and originals are preserved externally.

The following exact new files are project-directed original DSP by Codex,2026-09-07, with no third-party source/sample/recording. Recipe: external audio0221/render.py (`334aa1a0bea8e37393e8299850dbf505e06dd62de587c76104da0cb672fac98d`), explicit input/output arguments, deterministic seeds2211–2237; NumPy2.3.5/SciPy1.16.1/SoundFile0.14.0. Format48kHz mono PCM16. PCM24 auditions, predecessor files, source/export hashes and machine-specific paths are retained in the external export2 manifest. Codex checked finite decoded samples, duration and4× peaks; human listening pending. No external attribution requirement; project redistribution license undecided/development only, no public release approved.

- Runtime file: `Assets/Sounds/FirstSeverance/SpreadExecution.wav`
- Asset ID: spreadexecution-0221-2026-09-07
- Asset type: sound effect
- Creator: project-directed original DSP by Codex
- Creation/acquisition date: 2026-09-07
- Source type: original
- Source work and URL: Inharmonic high needle chirp/metal impact,0.62s; no external sample/recording
- Tool/model/version: audio0221 recipe and deterministic DSP versions recorded above
- Human modifications: none; human listening pending
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decoded finite/duration/4x peak checks,2026-09-07
- Notes: PCM24 audition, exact source/export hashes and reproducible recipe retained externally as recorded above.

- Runtime file: `Assets/Sounds/FirstSeverance/SpreadDissolve.wav`
- Asset ID: spreaddissolve-0221-2026-09-07
- Asset type: sound effect
- Creator: project-directed original DSP by Codex
- Creation/acquisition date: 2026-09-07
- Source type: original
- Source work and URL: Softer filtered air and falling glass tone,0.70s; no external sample/recording
- Tool/model/version: audio0221 recipe and deterministic DSP versions recorded above
- Human modifications: none; human listening pending
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decoded finite/duration/4x peak checks,2026-09-07
- Notes: PCM24 audition, exact source/export hashes and reproducible recipe retained externally as recorded above.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellLatch.wav`
- Asset ID: shelllatch-0221-2026-09-07
- Asset type: sound effect
- Creator: project-directed original DSP by Codex
- Creation/acquisition date: 2026-09-07
- Source type: original
- Source work and URL: Dry faceted-metal clack,0.23s; no external sample/recording
- Tool/model/version: audio0221 recipe and deterministic DSP versions recorded above
- Human modifications: none; human listening pending
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decoded finite/duration/4x peak checks,2026-09-07
- Notes: PCM24 audition, exact source/export hashes and reproducible recipe retained externally as recorded above.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellCollapse.wav`
- Asset ID: shellcollapse-0221-2026-09-07
- Asset type: sound effect
- Creator: project-directed original DSP by Codex
- Creation/acquisition date: 2026-09-07
- Source type: original
- Source work and URL: Low pressure collapse with staggered metallic fractures,0.85s; no external sample/recording
- Tool/model/version: audio0221 recipe and deterministic DSP versions recorded above
- Human modifications: none; human listening pending
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decoded finite/duration/4x peak checks,2026-09-07
- Notes: PCM24 audition, exact source/export hashes and reproducible recipe retained externally as recorded above.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellShed.wav`
- Asset ID: shellshed-0221-2026-09-07
- Asset type: sound effect
- Creator: project-directed original DSP by Codex
- Creation/acquisition date: 2026-09-07
- Source type: original
- Source work and URL: Irregular diminishing shard impacts without compression,1.10s; no external sample/recording
- Tool/model/version: audio0221 recipe and deterministic DSP versions recorded above
- Human modifications: none; human listening pending
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decoded finite/duration/4x peak checks,2026-09-07
- Notes: PCM24 audition, exact source/export hashes and reproducible recipe retained externally as recorded above.

NullCantorShell's existing texture is unchanged. Runtime faceted fragment masks sample that already-attributed source; no new distributable raster or borrowed texture is introduced.

### Sanctuary build — 0.2.19 / 2026-09-07

- Runtime file: `Assets/Textures/Items/NullRefrain.png`
- Asset ID: null-refrain-0219-2026-09-07
- Asset type: weapon texture
- Creator: project-directed original design with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-07
- Source type: generated
- Source work and URL: original obsidian/platinum weapon brief; no third-party reference/extracted asset
- Tool/model/version: built-in OpenAI ImageGen; backend model not surfaced
- Human modifications: none; unchanged2172x724 RGBA export, client-side scale/pose/filaments only
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: no external requirement specified; retain provenance
- Reviewer and review date: Codex visual selection and alpha inspection,2026-09-07; human in-game review pending
- Notes: original retained externally; exact prompt/original/runtime mapping in ignored playtest weapon-provenance.md. No raster processing.

The existing exact `Assets/Music/TerminalLiturgy.ogg` record retains its source/arrangement/performance/library terms. The external audio0219 parameterized recipe retimes the original DistantLiturgy PCM24 master to the longer Final score, preserving1.00→1.55× acceleration, prior gain/peak knee,4s entry and2.5s tail. Export81.067s,48kHz stereo Vorbis; finite decoded samples, peak0.9068 and4× peak0.9074. Predecessor, PCM24 audition, source/recipe/export hashes and dependency versions remain external. This accommodates timing, not new music or a louder mix; listening remains pending.

### Mix and temporal articulation revision — 0.2.17 / 2026-09-07

The exact records below retain their creator, source/library license and unresolved project-release terms. This revision supersedes their older master/duration notes only; no new third-party source or raster edit is introduced. Agent-authored external audio0217/render.py uses the previously documented NumPy/SciPy/SoundFile DSP toolchain. Codex checked finite decode, stereo/mono layout and four-times oversampled peaks on2026-09-07; human listening/in-game review remains pending. External PCM24 auditions are retained, with the0.80× SFX playback mix applied to the previews only.

| Exact existing runtime file | Current master revision |
|---|---|
| `Assets/Music/ObsidianLiturgy.ogg` | Existing original PCM24 composition/performance +1.25× stereo-linked soft-knee gain,80s; same CC0 instrument source layer |
| `Assets/Music/UnboundLiturgy.ogg` | Existing original PCM24 composition/performance +1.25× stereo-linked soft-knee gain,66.667s; same CC0 instrument source layer |
| `Assets/Music/DistantLiturgy.ogg` | Existing original PCM24 composition/performance +1.25× stereo-linked soft-knee gain,54.545s; same CC0 instrument source layer |
| `Assets/Music/TerminalLiturgy.ogg` | New75.067s edit of that original Distant PCM24 master, continuous1.00→1.55× speed/pitch through4s entry +68.567s active score, plus1.25× soft-knee gain; no new composition/source |
| `Assets/Sounds/FirstSeverance/HandGather.wav` | New0.78s project-DSP metallic pressure rise; no samples/recordings |
| `Assets/Sounds/FirstSeverance/HandClasp.wav` | New1.6s project-DSP needle onset/accelerating flood; no samples/recordings |
| `Assets/Sounds/FirstSeverance/BladeSweep.wav` | Prior original synthetic material condensed to2.6s and re-enveloped; no external source |

Music exports remain48kHz stereo Vorbis, effects48kHz mono PCM16. Source/arrangement/performance/license distinctions in each exact record remain unchanged; original PCM24 masters, current mix recipe and raw CC0 instruments are not packaged. Other runtime SFX files are unchanged; their attenuation is client playback code only.

### Phase scores and terminal survival — 0.2.13

The existing exact RaidVictory.wav record now refers to the original 0.2.13 inward-suction/2.8-second singularity-pop master. Its source remains entirely project DSP; the external current recipe/audition is audio0213. Earlier notes for that file are historical. All new exact runtime files follow; public-release licensing and human game/listening approval remain unresolved.

- Runtime file: `Assets/Textures/NPCs/NullCantorShell.png`
- Asset ID: null-cantor-shell-0213-2026-09-06
- Asset type: texture
- Creator: project-directed original design with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-06
- Source type: generated
- Source work and URL: original brief; no third-party images or extracted assets
- Tool/model/version: built-in OpenAI ImageGen; backend model not surfaced
- Human modifications: no raster edits; selected/visually inspected export, source alpha preserved; independent client-side masks and C# placement
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: no external requirement specified; retain provenance
- Reviewer and review date: Codex visual/alpha inspection, 2026-09-06; in-game review pending
- Notes: original fine-fractured obsidian/titanium cocoon with bronze laminations and pale inner light; external full brief audio0213/ART_PROMPT.md. Client masks partition the supplied texture only for hinged animation; no new exported derivative image.

- Runtime file: `Assets/Music/DistantLiturgy.ogg`
- Asset ID: distantliturgy-0213-2026-09-06
- Asset type: music
- Creator: project-directed original composition, arrangement and rendered performance by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: original music; same local VSCO 2 CE sources as ObsidianLiturgy, revision `440300901dfe9275fd84e0b7763af1f8443ae62e`, [pinned CC0 license](https://raw.githubusercontent.com/sgossner/VSCO-2-CE/440300901dfe9275fd84e0b7763af1f8443ae62e/LICENSE)
- Tool/model/version: project Python sampler/DSP recipe with NumPy/SciPy, SoundFile Vorbis encoder
- Human modifications: none; agent authored orchestration/mix; human listening/live acceptance pending
- License and redistribution terms: CC0 instrumental source license retained; project composition/recording release license undecided, development only
- Required attribution: CC0 library requires none; preserve complete source/provenance record
- Reviewer and review date: Codex decoded finite/peak inspection, 2026-09-06
- Notes: new 176-BPM, 40-bar, 54.545s score with short strings, brass, original choir, high glass responses and asymmetrical accents. Original composition, arrangement, sampler performance and master are distinct from the CC0 raw instrument library; no borrowed melody/MIDI/recording. Only stereo48kHz Vorbis enters Assets; source instrument hashes/licenses remain external in audio025, recipe and PCM24 masters in audio0213.

- Runtime file: `Assets/Music/TerminalLiturgy.ogg`
- Asset ID: terminalliturgy-0213-2026-09-06
- Asset type: music
- Creator: project-directed original composition, arrangement and rendered performance by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: original music; same local VSCO 2 CE sources as ObsidianLiturgy, revision `440300901dfe9275fd84e0b7763af1f8443ae62e`, [pinned CC0 license](https://raw.githubusercontent.com/sgossner/VSCO-2-CE/440300901dfe9275fd84e0b7763af1f8443ae62e/LICENSE)
- Tool/model/version: project Python sampler/DSP recipe with NumPy/SciPy, SoundFile Vorbis encoder
- Human modifications: none; agent authored orchestration/mix; human listening/live acceptance pending
- License and redistribution terms: CC0 instrumental source license retained; project composition/recording release license undecided, development only
- Required attribution: CC0 library requires none; preserve complete source/provenance record
- Reviewer and review date: Codex decoded finite/peak inspection, 2026-09-06
- Notes: 83s continuously resampled version of the new Phase-III master, 1.00x to1.55x playback speed/pitch; not a new borrowed recording. Original composition, arrangement, sampler performance and master are distinct from the CC0 raw instrument library; no borrowed melody/MIDI/recording. Only stereo48kHz Vorbis enters Assets; source instrument hashes/licenses remain external in audio025, recipe and PCM24 masters in audio0213.

- Runtime file: `Assets/Sounds/FirstSeverance/BladeGather.wav`
- Asset ID: bladegather-0213-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording or sample; project DSP
- Tool/model/version: Python, NumPy/SciPy synthesis, SoundFile PCM16 export
- Human modifications: none; agent authored and numerically checked; user listening pending
- License and redistribution terms: project asset license undecided; development only
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decode/finite/peak inspection, 2026-09-06
- Notes: mono 48kHz; external reproducible audio0213/render.py, PCM24 master and reel; no raw tools/master packaged

- Runtime file: `Assets/Sounds/FirstSeverance/BladeSweep.wav`
- Asset ID: bladesweep-0213-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording or sample; project DSP
- Tool/model/version: Python, NumPy/SciPy synthesis, SoundFile PCM16 export
- Human modifications: none; agent authored and numerically checked; user listening pending
- License and redistribution terms: project asset license undecided; development only
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decode/finite/peak inspection, 2026-09-06
- Notes: mono 48kHz; external reproducible audio0213/render.py, PCM24 master and reel; no raw tools/master packaged

- Runtime file: `Assets/Sounds/FirstSeverance/RemoteDeparture.wav`
- Asset ID: remotedeparture-0213-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording or sample; project DSP
- Tool/model/version: Python, NumPy/SciPy synthesis, SoundFile PCM16 export
- Human modifications: none; agent authored and numerically checked; user listening pending
- License and redistribution terms: project asset license undecided; development only
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decode/finite/peak inspection, 2026-09-06
- Notes: mono 48kHz; external reproducible audio0213/render.py, PCM24 master and reel; no raw tools/master packaged

- Runtime file: `Assets/Sounds/FirstSeverance/HandGather.wav`
- Asset ID: handgather-0213-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording or sample; project DSP
- Tool/model/version: Python, NumPy/SciPy synthesis, SoundFile PCM16 export
- Human modifications: none; agent authored and numerically checked; user listening pending
- License and redistribution terms: project asset license undecided; development only
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decode/finite/peak inspection, 2026-09-06
- Notes: mono 48kHz; external reproducible audio0213/render.py, PCM24 master and reel; no raw tools/master packaged

- Runtime file: `Assets/Sounds/FirstSeverance/HandClasp.wav`
- Asset ID: handclasp-0213-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording or sample; project DSP
- Tool/model/version: Python, NumPy/SciPy synthesis, SoundFile PCM16 export
- Human modifications: none; agent authored and numerically checked; user listening pending
- License and redistribution terms: project asset license undecided; development only
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decode/finite/peak inspection, 2026-09-06
- Notes: mono 48kHz; external reproducible audio0213/render.py, PCM24 master and reel; no raw tools/master packaged

- Runtime file: `Assets/Sounds/FirstSeverance/HalfFieldCharge.wav`
- Asset ID: halffieldcharge-0213-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording or sample; project DSP
- Tool/model/version: Python, NumPy/SciPy synthesis, SoundFile PCM16 export
- Human modifications: none; agent authored and numerically checked; user listening pending
- License and redistribution terms: project asset license undecided; development only
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decode/finite/peak inspection, 2026-09-06
- Notes: mono 48kHz; external reproducible audio0213/render.py, PCM24 master and reel; no raw tools/master packaged

- Runtime file: `Assets/Sounds/FirstSeverance/HalfFieldFire.wav`
- Asset ID: halffieldfire-0213-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording or sample; project DSP
- Tool/model/version: Python, NumPy/SciPy synthesis, SoundFile PCM16 export
- Human modifications: none; agent authored and numerically checked; user listening pending
- License and redistribution terms: project asset license undecided; development only
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decode/finite/peak inspection, 2026-09-06
- Notes: mono 48kHz; external reproducible audio0213/render.py, PCM24 master and reel; no raw tools/master packaged

- Runtime file: `Assets/Sounds/FirstSeverance/TerminalEntry.wav`
- Asset ID: terminalentry-0213-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording or sample; project DSP
- Tool/model/version: Python, NumPy/SciPy synthesis, SoundFile PCM16 export
- Human modifications: none; agent authored and numerically checked; user listening pending
- License and redistribution terms: project asset license undecided; development only
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decode/finite/peak inspection, 2026-09-06
- Notes: mono 48kHz; external reproducible audio0213/render.py, PCM24 master and reel; no raw tools/master packaged

- Runtime file: `Assets/Sounds/FirstSeverance/FinalGather.wav`
- Asset ID: finalgather-0213-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording or sample; project DSP
- Tool/model/version: Python, NumPy/SciPy synthesis, SoundFile PCM16 export
- Human modifications: none; agent authored and numerically checked; user listening pending
- License and redistribution terms: project asset license undecided; development only
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decode/finite/peak inspection, 2026-09-06
- Notes: mono 48kHz; external reproducible audio0213/render.py, PCM24 master and reel; no raw tools/master packaged

- Runtime file: `Assets/Sounds/FirstSeverance/FinalSlicerFire.wav`
- Asset ID: finalslicerfire-0213-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording or sample; project DSP
- Tool/model/version: Python, NumPy/SciPy synthesis, SoundFile PCM16 export
- Human modifications: none; agent authored and numerically checked; user listening pending
- License and redistribution terms: project asset license undecided; development only
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decode/finite/peak inspection, 2026-09-06
- Notes: mono 48kHz; external reproducible audio0213/render.py, PCM24 master and reel; no raw tools/master packaged

- Runtime file: `Assets/Sounds/FirstSeverance/FinalBulletRelease.wav`
- Asset ID: finalbulletrelease-0213-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording or sample; project DSP
- Tool/model/version: Python, NumPy/SciPy synthesis, SoundFile PCM16 export
- Human modifications: none; agent authored and numerically checked; user listening pending
- License and redistribution terms: project asset license undecided; development only
- Required attribution: no external requirement; retain provenance
- Reviewer and review date: Codex decode/finite/peak inspection, 2026-09-06
- Notes: mono 48kHz; external reproducible audio0213/render.py, PCM24 master and reel; no raw tools/master packaged

### Revision 0.2.12 — pressure, eclosion and terminal silence

The existing exact records for `GridFire.wav`, `PhaseRupture.wav`, `ShellBreak.wav` and `RaidVictory.wav` under `Assets/Sounds/FirstSeverance` now refer to independent 0.2.12 syntheses. GridFire is denser low/mid pressure; PhaseRupture is a strain swell; ShellBreak is a sequence of tears/creaks; RaidVictory is suction, collapse and synthetic choral residue. Their external recipe/auditions are now audio0212, superseding the prior recipe location for these files. Original source credits and unresolved project release license are unchanged. No external sample, recording or melody is used. Numeric decoding/peak checks on 2026-09-06 are not human listening approval.

- Runtime file: `Assets/Sounds/FirstSeverance/CoreSalvoFire.wav`
- Asset ID: first-severance-core-salvo-0212-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independent frequency-modulated plasma, filtered noise, modal-metal and low-impact synthesis; no external recording, sample or melody
- Tool/model/version: Python 3.12.14, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user requested high-pitched firing and stronger grid pressure; agent authored envelopes, spectrum, layers and composite mastering; human listening pending
- License and redistribution terms: project source/asset license undecided; development only, no public release approval
- Required attribution: none externally required; retain this provenance record
- Reviewer and review date: Codex decode/finite/peak checks, 2026-09-06; live mix and listening pending
- Notes: 48kHz mono PCM16 composite of grid impact and laser; one runtime voice replaces two summed effects. Original DSP recipe, isolated laser stem, PCM24 composite/individual auditions and reel remain external in audio0212. No raw third-party content is packaged.

### Revision 0.2.11 — original Phase-II score and stronger impacts

The existing exact records for `StackSummon.wav`, `StackRelease.wav`, `SpreadSummon.wav`, `SpreadRelease.wav`, `MechanicFailure.wav`, `LanceCharge.wav`, `LanceFire.wav`, `EnergyGather.wav`, `EnergyLock.wav`, `EnergyCharge.wav`, `PylonBreak.wav` and `CoreExposure.wav` (all under `Assets/Sounds/FirstSeverance`) now refer to the 0.2.11 remixes. These retain their original synthetic sources, credits and unresolved project release license. Agent-authored sub impacts, sharper noise cracks, descending plasma and stronger metallic resonances replace the earlier quiet masters. Current recipe, waveform/peak records and PCM24 auditions are retained externally under audio0211; the prior audio025 masters remain historical. No third-party SFX samples or recordings were added. Numeric decode/peak checks passed on 2026-09-06; human listening/live balance remains pending.

- Runtime file: `Assets/Music/UnboundLiturgy.ogg`
- Asset ID: unbound-liturgy-0211-2026-09-06
- Asset type: music
- Creator: project-directed original composition, orchestration and render by Codex; instrumental sample recordings by Sam Gossner and Simon Dalzell / Versilian Studios, sample cutting by Elan Hickler
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: original score, no borrowed melody/MIDI/recording; same VSCO 2 CE instrument sources as the ObsidianLiturgy record below, revision `440300901dfe9275fd84e0b7763af1f8443ae62e`, [CC0 license](https://raw.githubusercontent.com/sgossner/VSCO-2-CE/440300901dfe9275fd84e0b7763af1f8443ae62e/LICENSE)
- Tool/model/version: Python 3.12.14, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user chose ominous high-momentum Phase II; agent authored every note event, 40-bar form, ostinati, orchestration, synthetic choir and mix; no human listening approval yet
- License and redistribution terms: instrumental samples CC0-1.0; new project composition/recording source/asset license undecided, development only, no public release approval
- Required attribution: CC0 imposes none; retain courtesy sample-creator credits and this provenance record
- Reviewer and review date: Codex decode/finite/peak/PCM loop-boundary checks, 2026-09-06; user listening/live-mix review pending
- Notes: new 144-BPM, 40-bar minor/Phrygian orchestral-textural composition; not the old master sped up. Reuses the already attributed local CC0 low strings, spiccato, tremolo/violins, horn/trombone, bass drum, gong/cymbal with independent synthesized choir and metallic percussion. Stereo 48kHz / 3,200,000-frame Vorbis runtime; original release/reverb folded to start with a short seam envelope. Raw instruments, source hashes/license manifest and render dependencies remain external in audio025; new score recipe and PCM24 audition master remain external in audio0211. No third-party source/audio mirror is packaged.

- Runtime file: `Assets/Sounds/FirstSeverance/PhaseRupture.wav`
- Asset ID: first-severance-phaserupture-0211-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independent modal-metal, filtered-noise, sub-impact and descending-plasma synthesis; no external recording or sample library
- Tool/model/version: Python 3.12.14, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user requested stronger impact; agent authored envelopes, layers, filtering and mix; human listening approval pending
- License and redistribution terms: project source/asset license undecided; development only, no public release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak checks, 2026-09-06; no listening/live-mix acceptance claimed
- Notes: 48kHz mono PCM16 runtime; original recipe and PCM24 audition master/reel remain in external audio0211 working output. Dedicated Server never requests this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellBreak.wav`
- Asset ID: first-severance-shellbreak-0211-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independent modal-metal, filtered-noise, sub-impact and descending-plasma synthesis; no external recording or sample library
- Tool/model/version: Python 3.12.14, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user requested stronger impact; agent authored envelopes, layers, filtering and mix; human listening approval pending
- License and redistribution terms: project source/asset license undecided; development only, no public release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak checks, 2026-09-06; no listening/live-mix acceptance claimed
- Notes: 48kHz mono PCM16 runtime; original recipe and PCM24 audition master/reel remain in external audio0211 working output. Dedicated Server never requests this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/GridCharge.wav`
- Asset ID: first-severance-gridcharge-0211-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independent modal-metal, filtered-noise, sub-impact and descending-plasma synthesis; no external recording or sample library
- Tool/model/version: Python 3.12.14, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user requested stronger impact; agent authored envelopes, layers, filtering and mix; human listening approval pending
- License and redistribution terms: project source/asset license undecided; development only, no public release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak checks, 2026-09-06; no listening/live-mix acceptance claimed
- Notes: 48kHz mono PCM16 runtime; original recipe and PCM24 audition master/reel remain in external audio0211 working output. Dedicated Server never requests this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/GridFire.wav`
- Asset ID: first-severance-gridfire-0211-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independent modal-metal, filtered-noise, sub-impact and descending-plasma synthesis; no external recording or sample library
- Tool/model/version: Python 3.12.14, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user requested stronger impact; agent authored envelopes, layers, filtering and mix; human listening approval pending
- License and redistribution terms: project source/asset license undecided; development only, no public release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak checks, 2026-09-06; no listening/live-mix acceptance claimed
- Notes: 48kHz mono PCM16 runtime; original recipe and PCM24 audition master/reel remain in external audio0211 working output. Dedicated Server never requests this asset.

- Runtime file: `Assets/Textures/NPCs/NullCantorRigAtlas.png`
- Asset ID: null-cantor-rig-028-2026-09-06
- Asset type: texture
- Creator: project-directed original design with built-in ImageGen assistance
- Creation/acquisition date: 2026-09-06
- Source type: generated
- Source work and URL: none; original text brief, no external image inputs or extracted game assets
- Tool/model/version: built-in ImageGen; exact backend model/version not exposed
- Human modifications: user art direction; agent selected and inspected the unedited export and authored source-rectangle selection, joints and continuous rendering in C#
- License and redistribution terms: project source/asset license undecided; development only, no public release approval
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex visual inspection, 2026-09-06; user in-game quality/readability acceptance pending
- Notes: 1254x1254 PNG atlas; independent black-iron/ivory/bronze containment torso, articulated arm bones, crown, spine and joints. Generated layout differs from requested regular cells, so measured rectangles are used; no raster editing or third-party shader/code import. Full generation prompts retained in external build028 working output, not packaged.

- Runtime file: `Assets/Textures/Tiles/ContainmentAtlas.png`
- Asset ID: containment-plinth-028-2026-09-06
- Asset type: texture
- Creator: project-directed original design with built-in ImageGen assistance
- Creation/acquisition date: 2026-09-06
- Source type: generated
- Source work and URL: none; original text brief, no external image inputs or extracted game assets
- Tool/model/version: built-in ImageGen; exact backend model/version not exposed
- Human modifications: user art direction; agent selected and inspected the unedited export and authored source-rectangle selection, joints and continuous rendering in C#
- License and redistribution terms: project source/asset license undecided; development only, no public release approval
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex visual inspection, 2026-09-06; user in-game quality/readability acceptance pending
- Notes: 1254x1254 PNG atlas; grounded industrial-gothic plinth, telescopic steel column, iris core and field corner hardware. Generated layout differs from requested regular cells, so measured rectangles are used; no raster editing or third-party shader/code import. Full generation prompts retained in external build028 working output, not packaged.

- Runtime file: `Assets/Textures/VFX/EmissionAtlas.png`
- Asset ID: continuous-emission-028-2026-09-06
- Asset type: texture
- Creator: project-directed original design with built-in ImageGen assistance
- Creation/acquisition date: 2026-09-06
- Source type: generated
- Source work and URL: none; original text brief, no external image inputs or extracted game assets
- Tool/model/version: built-in ImageGen; exact backend model/version not exposed
- Human modifications: user art direction; agent selected and inspected the unedited export and authored source-rectangle selection, joints and continuous rendering in C#
- License and redistribution terms: project source/asset license undecided; development only, no public release approval
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex visual inspection, 2026-09-06; user in-game quality/readability acceptance pending
- Notes: 1254x1254 PNG atlas; original pale fibrous energy plume, apertures and torn electric membranes on transparent background. Generated layout differs from requested regular cells, so measured rectangles are used; no raster editing or third-party shader/code import. Full generation prompts retained in external build028 working output, not packaged.

- Runtime file: `Assets/Music/ObsidianLiturgy.ogg`
- Asset ID: obsidian-liturgy-025-2026-09-06
- Asset type: music
- Creator: project-directed original composition, orchestration and render by Codex; instrumental sample recordings by Sam Gossner and Simon Dalzell / Versilian Studios, sample cutting by Elan Hickler
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: new composition, no existing melody/score/MIDI; instrument library [VSCO 2 CE official distribution](https://versilian-studios.com/vsco-community/), revision `440300901dfe9275fd84e0b7763af1f8443ae62e`, [pinned CC0 license](https://raw.githubusercontent.com/sgossner/VSCO-2-CE/440300901dfe9275fd84e0b7763af1f8443ae62e/LICENSE), verified 2026-09-06
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2; independent sampler/resampler, original formant synthesis and diffuse convolution
- Human modifications: user selected unsettling/solemn direction; agent authored all note events, 32-bar form, dynamics, instrument placement, synthesis and mix; human listening/master approval pending
- License and redistribution terms: instrumental samples CC0-1.0; new project composition/recording source/asset license undecided, development only, no public release approval
- Required attribution: CC0 imposes none; retain courtesy sample-creator credit and this provenance record
- Reviewer and review date: Codex decode/finite/peak/full-loop boundary checks, 2026-09-06; user listening and live mix not yet reviewed
- Notes: Composition/arrangement: original D-pedal/flattened-second tension, 96 BPM/32 bars, not Beethoven or a film/game arrangement. Performance: externally authored orchestration MIDI and sampler recipe; no downloaded MIDI, SoundFont or choir recording. Sample sources: Cello Section susvib A2/B1 v3, spic A2 v2 RR1/RR2; Violin Section susVib/Trem A3 v2; Solo Contrabass SusNV A1 v3; F Horn sus D2 v3; Tenor Trombone sus A#1 v2; Tuba sus A#0 v2 Mid; Timpani2 Roll v3 Sum; VSCO1 bass drum3 fff/mp; cymbal-crash1 mf rr1; Misc1 cymb_gong. Exact source paths, hashes, license and download manifest remain with the external audio025 working files. Master: new stereo 48kHz PCM24; export: 80s/3,840,000-frame Vorbis loop, release/reverb folded to start and bounded writes. Only this newly mixed recording is distributed; raw samples/library/tools/MIDI remain external. The replaced Ninth master remains in preceding external working storage.

- Runtime file: `Assets/Sounds/FirstSeverance/RaidDesignation.wav`
- Asset ID: first-severance-raiddesignation-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/StackSummon.wav`
- Asset ID: first-severance-stacksummon-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/StackRelease.wav`
- Asset ID: first-severance-stackrelease-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/SpreadSummon.wav`
- Asset ID: first-severance-spreadsummon-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/SpreadRelease.wav`
- Asset ID: first-severance-spreadrelease-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/MechanicTick.wav`
- Asset ID: first-severance-mechanictick-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/MechanicFailure.wav`
- Asset ID: first-severance-mechanicfailure-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/LanceCharge.wav`
- Asset ID: first-severance-lancecharge-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/LanceFire.wav`
- Asset ID: first-severance-lancefire-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/EnergyGather.wav`
- Asset ID: first-severance-energygather-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/EnergyLock.wav`
- Asset ID: first-severance-energylock-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/EnergyCharge.wav`
- Asset ID: first-severance-energycharge-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/PylonBreak.wav`
- Asset ID: first-severance-pylonbreak-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/CoreExposure.wav`
- Asset ID: first-severance-coreexposure-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/Downed.wav`
- Asset ID: first-severance-downed-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/Revive.wav`
- Asset ID: first-severance-revive-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/RaidDefeat.wav`
- Asset ID: first-severance-raiddefeat-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Sounds/FirstSeverance/RaidVictory.wav`
- Asset ID: first-severance-raidvictory-025-2026-09-06
- Asset type: audio
- Creator: project-directed original synthesis and mix by Codex
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: independently authored inharmonic modal-metal resonances, low impulses, filtered noise and formant voices; no sample library or external recording
- Tool/model/version: Python 3.12, NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile 1.2.2
- Human modifications: user direction; agent-authored envelopes, layering, spectral filtering and diffuse tail; human listening pending
- License and redistribution terms: project source/asset license undecided; development use only, no release approval
- Required attribution: none externally required; retain this record
- Reviewer and review date: Codex decode/finite/peak/duration checks, 2026-09-06; human listening/live mix pending
- Notes: 48kHz mono PCM16 runtime; individual PCM24 audition master, combined timestamped SFX reel and reproducible recipe remain external under audio025. Dedicated Server does not request this asset.

- Runtime file: `Assets/Textures/Tiles/FoundationCoreMonument.png`
- Asset ID: foundation-core-monument-2026-09-06
- Asset type: texture
- Creator: project-directed original design with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-06
- Source type: generated
- Source work and URL: original polar containment-monument brief; no external reference image or extracted art
- Tool/model/version: OpenAI built-in ImageGen; exact backend model not surfaced
- Prompt or brief location: external working storage, `foundation-core-art-brief.md`; original cyan black-ice prism held by ivory/graphite uprights and gold restraints, orthographic pixel-art sprite, transparent RGBA
- Human modifications: no raster edits; output selected and alpha-inspected, independently aligned/scaled and given a pulsing clickable-base light in C#
- License and redistribution terms: project source/asset license undecided; development use only, no public release approved
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex visual/alpha inspection, 2026-09-06; human in-game review pending
- Notes: 1058x1487 RGBA, transparent corners; rendered on a 176-pixel-high canvas. Decoration does not expand the persistent 2x2 footprint. Existing FoundationCoreItem texture is also reused by the temporary recovery-lockout buff; no additional raster file is exported for it.

- Runtime file: `Content/Encounters/FirstSeverance/FoundationCore/FoundationCoreItem.png`
- Asset ID: foundation-core-item-prototype-2026-09-05
- Asset type: texture
- Creator: project-generated prototype with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-05
- Source type: generated
- Source work and URL: original task concept; no external artwork used
- Tool/model/version: OpenAI built-in ImageGen; exact backend model not surfaced
- Human modifications: local low-resolution pixel cleanup and Terraria item layout export
- License and redistribution terms: project source/asset license remains undecided; development use only, no public release approved
- Required attribution: none externally specified; preserve this provenance record
- Reviewer and review date: Codex task, 2026-09-05
- Notes: Also reused as the experimental Boss/Pylon/kit placeholder.

- Runtime file: `Content/Encounters/FirstSeverance/FoundationCore/FoundationCoreTile.png`
- Asset ID: foundation-core-prototype-2026-09-05
- Asset type: texture
- Creator: project-generated prototype with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-05
- Source type: generated
- Source work and URL: original task concept; no external artwork used
- Tool/model/version: OpenAI built-in ImageGen; exact backend model not surfaced
- Human modifications: local low-resolution pixel cleanup and Terraria item/tile layout export
- License and redistribution terms: project source/asset license remains undecided; development use only, no public release approved
- Required attribution: none externally specified; preserve this provenance record
- Reviewer and review date: Codex task, 2026-09-05
- Notes: Item sprite is also reused as the experimental Boss/Pylon/kit placeholder. Boss 3 music is a Terraria runtime ID reference; no recording/audio asset is copied into this repository.

- Runtime file: `Assets/Textures/NPCs/NullCantorBody.png`
- Asset ID: null-cantor-reliquary-025-2026-09-06
- Asset type: texture
- Creator: project-directed original design with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-06
- Source type: generated
- Source work and URL: original brief, no external reference artwork or extracted assets
- Tool/model/version: built-in OpenAI ImageGen; exact backend model not surfaced
- Prompt or brief location: external audio025/ART_PROMPTS.md; basalt/aged metal/bone-white nonhumanoid suspended reliquary, no face/eye, thin bronze incisions and separated lateral buttresses
- Human modifications: no manual raster edits; agent selected/visually inspected generated export and independently authored C# placement, tint/animation; user in-game review pending
- License and redistribution terms: project source/asset license undecided; development only, no public release approved
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex image/dimension/alpha inspection, 2026-09-06; game readability not_run
- Notes: 1254x1254 RGBA, preserved alpha. Three disconnected assemblies, aperture at (50%,36%); 1020px world canvas, linear sampling, fixed 144x144 target. Replaces the earlier export; old image remains in external baseline storage.

- Runtime file: `Assets/Textures/Backgrounds/HollowCathedral.png`
- Asset ID: hollow-cathedral-025-2026-09-06
- Asset type: texture
- Creator: project-directed original design with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-06
- Source type: generated
- Source work and URL: original brief, no external reference artwork or extracted assets
- Tool/model/version: built-in OpenAI ImageGen; exact backend model not surfaced
- Prompt or brief location: external audio025/ART_PROMPTS.md; impossible eroded cathedral architecture, calm charcoal center, pale distant fog, no characters or symbols
- Human modifications: no manual raster edits; agent selected/visually inspected generated export and independently authored C# placement, tint/animation; user in-game review pending
- License and redistribution terms: project source/asset license undecided; development only, no public release approved
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex image/dimension/alpha inspection, 2026-09-06; game readability not_run
- Notes: 1672x941 opaque RGB; high-detail landscape, covers viewport with 8% overscan. Final ImageGen edit removes an unwanted tiny corner mark; no other raster processing. Client sky only; ReLogic owns the asset.

- Runtime file: `Assets/Textures/VFX/ObsidianLance.png`
- Asset ID: obsidian-lance-025-2026-09-06
- Asset type: texture
- Creator: project-directed original design with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-06
- Source type: generated
- Source work and URL: original brief, no external reference artwork or extracted assets
- Tool/model/version: built-in OpenAI ImageGen; exact backend model not surfaced
- Prompt or brief location: external audio025/ART_PROMPTS.md; obsidian needle enclosed by fine bone-white membranes and fading smoke, restrained inner crimson, no neon or copied film silhouette
- Human modifications: no manual raster edits; agent selected/visually inspected generated export and independently authored C# placement, tint/animation; user in-game review pending
- License and redistribution terms: project source/asset license undecided; development only, no public release approved
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex image/dimension/alpha inspection, 2026-09-06; game readability not_run
- Notes: 2172x724 RGBA, preserved alpha; right-facing dark head and fibrous white wake. Drawn at 410x140 world pixels, wake purely cosmetic; explicit rails retain authority collision. Two separate procedural in-memory glow/filament textures are generated/disposed in client code, not distributable raster files.

### 0.2.16 blade and action cues

- Runtime file: `Assets/Textures/VFX/SeveranceBlade.png`
- Asset ID: severance-blade-0216-2026-09-06
- Asset type: texture
- Creator: project-directed original design with OpenAI ImageGen assistance
- Creation/acquisition date: 2026-09-06
- Source type: generated
- Source work and URL: original inorganic blade brief, no external image/reference artwork
- Tool/model/version: built-in OpenAI ImageGen; exact backend model not surfaced
- Prompt or brief location: external audio0216/ART_PROMPT.md
- Human modifications: none; generated raster and alpha copied unchanged. Independent client code clips the rigid material at the field edge, poses it and draws bounded echoes.
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex visual/alpha/dimension inspection, 2026-09-06; in-game acceptance pending
- Notes: 2172x724 RGBA, alpha0..255. Obsidian/titanium laminae, ivory cutting edge, bronze root and restrained violet seam. Client-only borrowed ReLogic asset, linear sampling.

- Runtime file: `Assets/Sounds/FirstSeverance/ExecutionLock.wav`
- Asset ID: executionlock-0216-2026-09-06
- Asset type: sound effect
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: none; no external recording, sample or composition
- Tool/model/version: Python3.12 / NumPy / SciPy / SoundFile, external audio0216/render.py
- Human modifications: original synthesis, filtering, metallic/air/low-impact layering and soft-knee mastering; listening pending
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex finite/decode/peak checks, 2026-09-06; human mix review pending
- Notes: 0.46-second inharmonic double warning accent; 48kHz mono PCM16 with external PCM24 audition, decoded peak below0.911.

- Runtime file: `Assets/Sounds/FirstSeverance/BladeUnsheathe.wav`
- Asset ID: bladeunsheathe-0216-2026-09-06
- Asset type: sound effect
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: none; no external recording, sample or composition
- Tool/model/version: Python3.12 / NumPy / SciPy / SoundFile, external audio0216/render.py
- Human modifications: original synthesis, filtering, metallic/air/low-impact layering and soft-knee mastering; listening pending
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex finite/decode/peak checks, 2026-09-06; human mix review pending
- Notes: 0.78-second metallic draw transient; 48kHz mono PCM16 with external PCM24 audition, decoded peak below0.911.

- Runtime file: `Assets/Sounds/FirstSeverance/HandCrushGather.wav`
- Asset ID: handcrushgather-0216-2026-09-06
- Asset type: sound effect
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: none; no external recording, sample or composition
- Tool/model/version: Python3.12 / NumPy / SciPy / SoundFile, external audio0216/render.py
- Human modifications: original synthesis, filtering, metallic/air/low-impact layering and soft-knee mastering; listening pending
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex finite/decode/peak checks, 2026-09-06; human mix review pending
- Notes: 2.5-second bracing pressure rise; 48kHz mono PCM16 with external PCM24 audition, decoded peak below0.911.

- Runtime file: `Assets/Sounds/FirstSeverance/HandCrushImpact.wav`
- Asset ID: execution-blade-crush-cues-0216-2026-09-06
- Asset type: sound effects
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-06
- Source type: original
- Source work and URL: no external recording, sample library, speech or composition
- Tool/model/version: Python3.12 / NumPy / SciPy / SoundFile; external audio0216/render.py reuses project-authored DSP helpers
- Human modifications: original synthesis, filtering, inharmonic metal/air/impact layers and bounded soft-knee mastering; user listening pending
- License and redistribution terms: project asset license undecided; development only, no public release approved
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex decode/finite/peak checks, 2026-09-06; human mix review pending
- Notes: 48kHz mono PCM16 runtime, PCM24 auditions retained externally. ExecutionLock0.46s, BladeUnsheathe0.78s, HandCrushGather2.5s, HandCrushImpact1.4s. Maximum absolute decoded sample0.911, no clipped samples.

The same external0.2.16 recipe revises the already-recorded StackSummon, SpreadSummon, LanceCharge, EnergyGather, EnergyLock, GridCharge, BladeGather, HandGather, HalfFieldCharge and FinalGather masters with an early metallic warning and reduced crest factor. MechanicTick is replaced by a0.33s original bell/low-impact warning; BladeSweep becomes a4.1s original accelerating air/metal body. All preserve original provenance above, use48kHz mono PCM16, and have external PCM24 auditions. No music or third-party source layer changes. RMS measurements are technical evidence, not perceived-loudness acceptance.

### Heavy Raid cues — 0.2.37

- Runtime file: `Assets/Sounds/FirstSeverance/IronPressure.wav`
- Asset ID: ironpressure-0237-2026-09-09
- Asset type: sound effect
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no external samples, recordings or compositions
- Tool/model/version: Python3.12 / NumPy; tools/generate_raid_weight_sfx.py, seed2370909,48kHz PCM16 mono
- Human modifications: no human editing; independent synthesis, modal/noise layering, diffuse reflections, spectral DC cut and peak mastering
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex finite/PCM decode/endpoint/peak checks,2026-09-09; human mix acceptance pending
- Notes: 0.65-second restrained blade-loading pressure before each insertion wave. Decoded peak0.780; external auditions retained. Earlier shared weapon/victory cues are not overwritten.

- Runtime file: `Assets/Sounds/FirstSeverance/IronDescent.wav`
- Asset ID: irondescent-0237-2026-09-09
- Asset type: sound effect
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no external samples, recordings or compositions
- Tool/model/version: Python3.12 / NumPy; tools/generate_raid_weight_sfx.py, seed2370909,48kHz PCM16 mono
- Human modifications: no human editing; independent synthesis, modal/noise layering, diffuse reflections, spectral DC cut and peak mastering
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex finite/PCM decode/endpoint/peak checks,2026-09-09; human mix acceptance pending
- Notes: 1.10-second struck inharmonic slab with low pressure and a short edge transient. Decoded peak0.780; external auditions retained. Earlier shared weapon/victory cues are not overwritten.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellMassLatch.wav`
- Asset ID: shellmasslatch-0237-2026-09-09
- Asset type: sound effect
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no external samples, recordings or compositions
- Tool/model/version: Python3.12 / NumPy; tools/generate_raid_weight_sfx.py, seed2370909,48kHz PCM16 mono
- Human modifications: no human editing; independent synthesis, modal/noise layering, diffuse reflections, spectral DC cut and peak mastering
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex finite/PCM decode/endpoint/peak checks,2026-09-09; human mix acceptance pending
- Notes: 0.86-second double-contact heavy fragment latch. Decoded peak0.780; external auditions retained. Earlier shared weapon/victory cues are not overwritten.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellMassArc.wav`
- Asset ID: shellmassarc-0237-2026-09-09
- Asset type: sound effect
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no external samples, recordings or compositions
- Tool/model/version: Python3.12 / NumPy; tools/generate_raid_weight_sfx.py, seed2370909,48kHz PCM16 mono
- Human modifications: no human editing; independent synthesis, modal/noise layering, diffuse reflections, spectral DC cut and peak mastering
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex finite/PCM decode/endpoint/peak checks,2026-09-09; human mix acceptance pending
- Notes: 0.50-second irregular low-mid electrical friction; reduced layer gain. Decoded peak0.780; external auditions retained. Earlier shared weapon/victory cues are not overwritten.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellMassShed.wav`
- Asset ID: shellmassshed-0237-2026-09-09
- Asset type: sound effect
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no external samples, recordings or compositions
- Tool/model/version: Python3.12 / NumPy; tools/generate_raid_weight_sfx.py, seed2370909,48kHz PCM16 mono
- Human modifications: no human editing; independent synthesis, modal/noise layering, diffuse reflections, spectral DC cut and peak mastering
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex finite/PCM decode/endpoint/peak checks,2026-09-09; human mix acceptance pending
- Notes: 1.55-second uneven falling debris and pressure release, without implosion. Decoded peak0.780; external auditions retained. Earlier shared weapon/victory cues are not overwritten.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellMassCollapse.wav`
- Asset ID: shellmasscollapse-0237-2026-09-09
- Asset type: sound effect
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no external samples, recordings or compositions
- Tool/model/version: Python3.12 / NumPy; tools/generate_raid_weight_sfx.py, seed2370909,48kHz PCM16 mono
- Human modifications: no human editing; independent synthesis, modal/noise layering, diffuse reflections, spectral DC cut and peak mastering
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex finite/PCM decode/endpoint/peak checks,2026-09-09; human mix acceptance pending
- Notes: 1.70-second compression and massive fractured-metal impact. Decoded peak0.780; external auditions retained. Earlier shared weapon/victory cues are not overwritten.

- Runtime file: `Assets/Sounds/FirstSeverance/CrushPressure.wav`
- Asset ID: crushpressure-0237-2026-09-09
- Asset type: sound effect
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no external samples, recordings or compositions
- Tool/model/version: Python3.12 / NumPy; tools/generate_raid_weight_sfx.py, seed2370909,48kHz PCM16 mono
- Human modifications: no human editing; independent synthesis, modal/noise layering, diffuse reflections, spectral DC cut and peak mastering
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex finite/PCM decode/endpoint/peak checks,2026-09-09; human mix acceptance pending
- Notes: 2.50-second bracing load and accelerating final pressure rise. Decoded peak0.780; external auditions retained. Earlier shared weapon/victory cues are not overwritten.

- Runtime file: `Assets/Sounds/FirstSeverance/CrushCataclysm.wav`
- Asset ID: crushcataclysm-0237-2026-09-09
- Asset type: sound effect
- Creator: project-authored original DSP
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no external samples, recordings or compositions
- Tool/model/version: Python3.12 / NumPy; tools/generate_raid_weight_sfx.py, seed2370909,48kHz PCM16 mono
- Human modifications: no human editing; independent synthesis, modal/noise layering, diffuse reflections, spectral DC cut and peak mastering
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: none externally specified; retain this record
- Reviewer and review date: Codex finite/PCM decode/endpoint/peak checks,2026-09-09; human mix acceptance pending
- Notes: 2.15-second low-air collapse and dense struck-metal terminal crush. Decoded peak0.780; external auditions retained. Earlier shared weapon/victory cues are not overwritten.

0.2.39 Stack-only remaster: `ShellMassLatch.wav`, `ShellMassArc.wav`, `ShellMassShed.wav` and `ShellMassCollapse.wav` above retain original provenance and durations. `tools/generate_raid_weight_sfx.py --stack-only` adds240–2200Hz presence and controlled saturation; seed/PCM format and0.780peak remain. Decode/finite/endpoint checks passed2026-09-09; human listening pending. External0.2.37 originals and new auditions retained. Only these four masters are modified; other sound assets remain byte-identical.

### Critical impacts and orbit — 0.2.40

IronPressure, IronDescent, ShellMassShed, ShellMassCollapse, CrushPressure and CrushCataclysm retain their exact runtime records above. The same original generator remasters their presence/pressure without changing duration or provenance. Accepted ShellMassLatch and ShellMassArc exports remain byte-identical to0.2.39. New audition masters and previous originals remain externally archived; `--critical-only` regenerates the changed set. Peak0.780PCM, finite/endpoints/decode checked2026-09-09; in-game mix pending.

- Runtime file: `Assets/Sounds/FirstSeverance/BladeOrbitFirst.wav`
- Asset ID: blade-orbit-first-20260909
- Asset type: sound effect
- Creator: project-directed original DSP synthesis
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no external samples or recordings
- Tool/model/version: Python3.12 / NumPy, tools/generate_raid_weight_sfx.py, seed2370909, PCM16 mono48kHz
- Human modifications: no human editing; modal metal, band-limited pressure, reflections and peak mastering
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: none externally specified; retain provenance
- Reviewer and review date: Codex, 2026-09-09; PCM/finite/endpoint/peak checks passed; game listening pending
- Notes: 3.05-second first accelerating orbit; no imported work. External auditions/originals retained.


- Runtime file: `Assets/Sounds/FirstSeverance/BladeOrbitSecond.wav`
- Asset ID: blade-orbit-second-20260909
- Asset type: sound effect
- Creator: project-directed original DSP synthesis
- Creation/acquisition date: 2026-09-09
- Source type: original
- Source work and URL: none; no external samples or recordings
- Tool/model/version: Python3.12 / NumPy, tools/generate_raid_weight_sfx.py, seed2370909, PCM16 mono48kHz
- Human modifications: no human editing; modal metal, band-limited pressure, reflections and peak mastering
- License and redistribution terms: project asset terms not selected; retain existing publication gate
- Required attribution: none externally specified; retain provenance
- Reviewer and review date: Codex, 2026-09-09; PCM/finite/endpoint/peak checks passed; game listening pending
- Notes: 1.95-second second accelerating orbit; no imported work. External auditions/originals retained.


### Oboro and violet Ghost Samurai — 2026-09-16

The September16 boards replace the earlier cyan appearance only. The old Boss atlas and earlier local candidates are retained. Full local generation requests and references remain in the ignored generation record; these are project-directed images, not extracted Calamity/Terraria artwork.

- Runtime file: `Assets/Textures/Items/Oboro/Blade.png`
- Asset ID: oboro-violet-blade
- Asset type: single weapon sprite
- Creator: OpenAI built-in image generation directed for Minamium; dissolve sheet supplied by the owner
- Creation/acquisition date: 2026-09-16
- Source type: generated
- Source work and URL: Generated from the owner September16 Oboro board: indigo curved blade, gold guard, paper talismans, violet fire.
- Tool/model/version: built-in image generation; underlying model and seed not reported; source-sheet generator version not provided
- Human modifications: RGBA transparent original; runtime grip (254,1059), tip-axis angle -0.78 rad, reference length1230; icon/world/held uses explicit scale.
- License and redistribution terms: owner-requested project use and project-directed generated derivative under existing project asset/publication terms; no third-party asset license asserted
- Required attribution: retain this provenance and the owner reference origin
- Reviewer and review date: Codex,2026-09-16; alpha/key removal and dark/light preview checked; game acceptance remains user-owned
- SHA256: `21d61523f86a996c9e413fc4cd90431243df694ce7fc7318a717a398be96bf5a`

- Runtime file: `Assets/Textures/Items/Oboro/Spirit.png`
- Asset ID: oboro-violet-spirit
- Asset type: ghost-fire VFX sprite
- Creator: OpenAI built-in image generation directed for Minamium; dissolve sheet supplied by the owner
- Creation/acquisition date: 2026-09-16
- Source type: generated
- Source work and URL: Generated from the floating orb/flame examples in the owner September16 Oboro board.
- Tool/model/version: built-in image generation; underlying model and seed not reported; source-sheet generator version not provided
- Human modifications: Opaque green-key original; SpectralSpriteCutouts removes green once on the render thread. White/lavender core retained.
- License and redistribution terms: owner-requested project use and project-directed generated derivative under existing project asset/publication terms; no third-party asset license asserted
- Required attribution: retain this provenance and the owner reference origin
- Reviewer and review date: Codex,2026-09-16; alpha/key removal and dark/light preview checked; game acceptance remains user-owned
- SHA256: `9a499a0d179ac0e6a6a9c2f4d3b92d4b4b3b473bcd641386af5643920707ac2a`

- Runtime file: `Assets/Textures/GhostSamurai/VioletActions.png`
- Asset ID: ghost-samurai-violet-actions
- Asset type: twelve-pose Boss atlas
- Creator: OpenAI built-in image generation directed for Minamium; dissolve sheet supplied by the owner
- Creation/acquisition date: 2026-09-16
- Source type: generated
- Source work and URL: Generated from the owner September16 idle, dash, paired cuts, heavy cut and floating-blade boards.
- Tool/model/version: built-in image generation; underlying model and seed not reported; source-sheet generator version not provided
- Human modifications: Opaque green-key original,4 columns x3 rows; runtime key extraction and explicit bounded cells. No per-frame pixel readback.
- License and redistribution terms: owner-requested project use and project-directed generated derivative under existing project asset/publication terms; no third-party asset license asserted
- Required attribution: retain this provenance and the owner reference origin
- Reviewer and review date: Codex,2026-09-16; alpha/key removal and dark/light preview checked; game acceptance remains user-owned
- SHA256: `4f07828fc7ec374aab3fbed64f34e44a9071a56cdd0b1e0e87a6a42fbcacf75d`

- Runtime file: `Assets/Textures/GhostSamurai/VioletDissolve.png`
- Asset ID: ghost-samurai-violet-dissolve
- Asset type: ten-frame Boss dissolve atlas
- Creator: OpenAI built-in image generation directed for Minamium; dissolve sheet supplied by the owner
- Creation/acquisition date: 2026-09-16
- Source type: generated
- Source work and URL: Owner-supplied September16 18_08_03 generated reference image, supplied for this implementation.
- Tool/model/version: built-in image generation; underlying model and seed not reported; source-sheet generator version not provided
- Human modifications: Copied unchanged;5 columns x2 rows; runtime clips cells and suppresses alpha below8; no gameplay death delay.
- License and redistribution terms: owner-requested project use and project-directed generated derivative under existing project asset/publication terms; no third-party asset license asserted
- Required attribution: retain this provenance and the owner reference origin
- Reviewer and review date: Codex,2026-09-16; alpha/key removal and dark/light preview checked; game acceptance remains user-owned
- SHA256: `f90af5b4ad893127ea5926f7604d66c681a53b5c57220ea60a92bd29a47aee72`

### Ghost Samurai spectral slash materials — 2026-09-17

- Runtime file: `Assets/Textures/GhostSamurai/Slashes/NormalSlash.png`
- Asset ID: ghost-samurai-slash-normalslash-0315
- Asset type: original RGBA slash ribbon texture, 2172 x 724
- Creator: OpenAI image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-17
- Source type: generated
- Source work and URL: original text-to-image output for this request; no third-party art/source imported; exact prompt retained in the local generation manifest
- Tool/model/version: built-in image_gen; underlying model/version/seed not exposed
- Human modifications: none to pixels; original alpha preserved. Client-only UV strips fit the texture to existing attack geometry and clocks; no offline resampling.
- License and redistribution terms: project-directed generated runtime art under the repository's existing asset/publication policy; no third-party license claim; release approval remains separate
- Required attribution: retain this record
- Reviewer and review date: Codex, 2026-09-17; source appearance and alpha checked, in-game acceptance pending
- SHA256: `670858dd4ff4ead472520f4df4d1fcceb217f6af826913a3505ffcc541bc37b2`

- Runtime file: `Assets/Textures/GhostSamurai/Slashes/HeavySlash.png`
- Asset ID: ghost-samurai-slash-heavyslash-0315
- Asset type: original RGBA slash ribbon texture, 2172 x 724
- Creator: OpenAI image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-17
- Source type: generated
- Source work and URL: original text-to-image output for this request; no third-party art/source imported; exact prompt retained in the local generation manifest
- Tool/model/version: built-in image_gen; underlying model/version/seed not exposed
- Human modifications: none to pixels; original alpha preserved. Client-only UV strips fit the texture to existing attack geometry and clocks; no offline resampling.
- License and redistribution terms: project-directed generated runtime art under the repository's existing asset/publication policy; no third-party license claim; release approval remains separate
- Required attribution: retain this record
- Reviewer and review date: Codex, 2026-09-17; source appearance and alpha checked, in-game acceptance pending
- SHA256: `f399d13494587204dbe420551a3fddd5cd80abb47f96a0491f223007e70e313a`

- Runtime file: `Assets/Textures/GhostSamurai/Slashes/GridSlash.png`
- Asset ID: ghost-samurai-slash-gridslash-0315
- Asset type: original RGBA slash ribbon texture, 2172 x 724
- Creator: OpenAI image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-17
- Source type: generated
- Source work and URL: original text-to-image output for this request; no third-party art/source imported; exact prompt retained in the local generation manifest
- Tool/model/version: built-in image_gen; underlying model/version/seed not exposed
- Human modifications: none to pixels; original alpha preserved. Client-only UV strips fit the texture to existing attack geometry and clocks; no offline resampling.
- License and redistribution terms: project-directed generated runtime art under the repository's existing asset/publication policy; no third-party license claim; release approval remains separate
- Required attribution: retain this record
- Reviewer and review date: Codex, 2026-09-17; source appearance and alpha checked, in-game acceptance pending
- SHA256: `6698e80758ecbac325b7886e94e853e30726366ba17105a4f64348d99e0ca253`

- Runtime file: `Assets/Textures/GhostSamurai/Slashes/Kamaitachi.png`
- Asset ID: ghost-samurai-slash-kamaitachi-0315
- Asset type: original RGBA slash ribbon texture, 2172 x 724
- Creator: OpenAI image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-17
- Source type: generated
- Source work and URL: original text-to-image output for this request; no third-party art/source imported; exact prompt retained in the local generation manifest
- Tool/model/version: built-in image_gen; underlying model/version/seed not exposed
- Human modifications: none to pixels; original alpha preserved. Client-only UV strips fit the texture to existing attack geometry and clocks; no offline resampling.
- License and redistribution terms: project-directed generated runtime art under the repository's existing asset/publication policy; no third-party license claim; release approval remains separate
- Required attribution: retain this record
- Reviewer and review date: Codex, 2026-09-17; source appearance and alpha checked, in-game acceptance pending
- SHA256: `e540ba728bc79890477d3bea53538d36007d101075994860d629b72cbc95d7f2`

- Runtime file: `Assets/Textures/GhostSamurai/Slashes/DashFlash.png`
- Asset ID: ghost-samurai-slash-dashflash-0315
- Asset type: original RGBA slash ribbon texture, 2172 x 724
- Creator: OpenAI image generation, directed by Codex for Minamium
- Creation/acquisition date: 2026-09-17
- Source type: generated
- Source work and URL: original text-to-image output for this request; no third-party art/source imported; exact prompt retained in the local generation manifest
- Tool/model/version: built-in image_gen; underlying model/version/seed not exposed
- Human modifications: none to pixels; original alpha preserved. Client-only UV strips fit the texture to existing attack geometry and clocks; no offline resampling.
- License and redistribution terms: project-directed generated runtime art under the repository's existing asset/publication policy; no third-party license claim; release approval remains separate
- Required attribution: retain this record
- Reviewer and review date: Codex, 2026-09-17; source appearance and alpha checked, in-game acceptance pending
- SHA256: `729c27ea3b9f1e5c0261b012a091597fba15f7f06a516838b5f6706f57573cf8`

### OboroMoonArc original shader export — 2026-09-21

- Runtime file: `Assets/AutoloadedEffects/Shaders/OboroMoonArc.fxc`
- Asset ID: oboro-moon-arc-20260921
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation by OpenAI for Minamium
- Creation/acquisition date: 2026-09-21
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/OboroMoonArc.fx`; no third-party shader copied
- Tool/model/version: FXC compiler and options pinned in `Assets/AutoloadedEffects/Shaders/compiled.json`
- Human modifications: project-authored spectral crescent mesh, sharp lip, violet flow and lifetime; September27 adds a broader cutting shoulder, connected fractional leading geometry and continuous fading. Approved images unchanged; no source shader copied.
- License and redistribution terms: original project code under existing project terms; no dependency assets bundled
- Required attribution: retain this provenance, source and compiler/export identity manifest
- Reviewer and review date: automated source/export checks and offline FNA inspection, 2026-09-21; owner visual approval not_run
- Prompt or brief location: `docs/encounters/ghost-samurai/ENCOUNTER_SPEC.md`, Oboro metal and spectral blade separation

### Articulated sword lightning — 2026-09-27

- Runtime file: `Assets/AutoloadedEffects/Shaders/SwordLightning.fxc`
- Asset ID: articulated-sword-lightning-20260927
- Asset type: compiled original HLSL effect
- Creator: project-directed independent implementation with OpenAI Codex assistance for Minamium
- Creation/acquisition date: 2026-09-27
- Source type: original
- Source work and URL: repository source `Assets/AutoloadedEffects/Shaders/SwordLightning.fx`; no third-party shader, texture or sound copied
- Tool/model/version: FXC compiler and options pinned in `Assets/AutoloadedEffects/Shaders/compiled.json`; no image-generation model used
- Human modifications: original bounded forked geometry attached to the actual Oboro, Soboro and Ghost Samurai blades; white-violet filament, saturated corona, ion glow and coherent overlapping discharge decay
- License and redistribution terms: original project code under existing project terms; no dependency assets bundled
- Required attribution: retain this provenance, source and compiler/export identity manifest
- Reviewer and review date: automated source/export checks and offline linked-production FNA sequence inspection, 2026-09-27; native owner visual approval not_run
- Prompt or brief location: `docs/encounters/ghost-samurai/ENCOUNTER_SPEC.md`, continuous sword acting and attached violet lightning

## Record template

Copy this section for each asset family. In the Records section above, add one exact Markdown entry in the form `- Runtime file: \`path/from/repository/root\`` for every exported file. The repository check parses only that section and verifies both directions.

```text
- Runtime file: `Assets/example/path.png`
- Asset ID: example-stable-id
- Asset type: texture
- Creator: name or organization
- Creation/acquisition date: YYYY-MM-DD
- Source type: original | generated | commissioned | licensed | public-domain
- Source work and URL: none for original work, otherwise exact source
- Tool/model/version: exact toolchain or none
- Human modifications: concise description or none
- License and redistribution terms: exact project-compatible terms
- Required attribution: exact credit text or none
- Reviewer and review date: reviewer, YYYY-MM-DD
- Notes: optional details
```

For music, record the composition, edition or score, arrangement, performance or MIDI, sample library, recording, and final master separately. A public-domain composition does not make a modern edition, arrangement, performance, recording, or sample library public domain.


### Continuous orchestral A/B/C BGM rebuild — 0.2.42 / 2026-09-10

- Runtime file: `Assets/Music/ObsidianLiturgy.ogg`
- Asset ID: obsidian-liturgy-abc-0242-20260910
- Asset type: music
- Creator: project-directed original composition/orchestration/render by Codex; instrumental sample recordings by Sam Gossner and Simon Dalzell / Versilian Studios, sample cutting by Elan Hickler
- Creation/acquisition date: 2026-09-10
- Source type: original continuation/re-orchestration of the existing project composition
- Source work and URL: existing Convergence A master plus new project-authored B/C note events; instrumental library [VSCO 2 CE official distribution](https://versilian-studios.com/vsco-community/), pinned revision `440300901dfe9275fd84e0b7763af1f8443ae62e`, [CC0 license](https://raw.githubusercontent.com/sgossner/VSCO-2-CE/440300901dfe9275fd84e0b7763af1f8443ae62e/LICENSE)
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile, pyloudnorm 0.1.1, FFmpeg/libvorbis; independent sample player, original formant synthesis and diffuse reflection mix
- Human modifications: owner accepted V13 composition direction and requested A-to-B continuity repair; agent retained A through the B downbeat, authored B/C note events, articulation progression, dynamics and mix
- License and redistribution terms: instrumental samples CC0-1.0; project composition/recording terms remain under the existing development publication gate
- Required attribution: retain this provenance; no WotG/Calamity music or recording imported
- Reviewer and review date: Codex automated decode/finite/48kHz/stereo/duration/peak checks, 2026-09-10; owner in-game final mix/loop review pending
- Notes: Phase 1; 96.000 BPM; 120.000000s; encoded bytes 1850090; decoded peak 0.969381; SHA256 `16750eb81764cb11357dbbd5ec8e9aa7c941a4d5cd64a1e40cf26aaf935ddb64`.

- Runtime file: `Assets/Music/UnboundLiturgy.ogg`
- Asset ID: unbound-liturgy-abc-0242-20260910
- Asset type: music
- Creator: project-directed original composition/orchestration/render by Codex; instrumental sample recordings by Sam Gossner and Simon Dalzell / Versilian Studios, sample cutting by Elan Hickler
- Creation/acquisition date: 2026-09-10
- Source type: original continuation/re-orchestration of the existing project composition
- Source work and URL: existing Convergence A master plus new project-authored B/C note events; instrumental library [VSCO 2 CE official distribution](https://versilian-studios.com/vsco-community/), pinned revision `440300901dfe9275fd84e0b7763af1f8443ae62e`, [CC0 license](https://raw.githubusercontent.com/sgossner/VSCO-2-CE/440300901dfe9275fd84e0b7763af1f8443ae62e/LICENSE)
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile, pyloudnorm 0.1.1, FFmpeg/libvorbis; independent sample player, original formant synthesis and diffuse reflection mix
- Human modifications: owner accepted V13 composition direction and requested A-to-B continuity repair; agent retained A through the B downbeat, authored B/C note events, articulation progression, dynamics and mix
- License and redistribution terms: instrumental samples CC0-1.0; project composition/recording terms remain under the existing development publication gate
- Required attribution: retain this provenance; no WotG/Calamity music or recording imported
- Reviewer and review date: Codex automated decode/finite/48kHz/stereo/duration/peak checks, 2026-09-10; owner in-game final mix/loop review pending
- Notes: Phase 2; 144.000 BPM; 80.000000s; encoded bytes 1285402; decoded peak 0.962677; SHA256 `518ec160eab9b461f17564019bdc37252210e5b96b051594b7120fed62031681`.

- Runtime file: `Assets/Music/DistantLiturgy.ogg`
- Asset ID: distant-liturgy-abc-0242-20260910
- Asset type: music
- Creator: project-directed original composition/orchestration/render by Codex; instrumental sample recordings by Sam Gossner and Simon Dalzell / Versilian Studios, sample cutting by Elan Hickler
- Creation/acquisition date: 2026-09-10
- Source type: original continuation/re-orchestration of the existing project composition
- Source work and URL: existing Convergence A master plus new project-authored B/C note events; instrumental library [VSCO 2 CE official distribution](https://versilian-studios.com/vsco-community/), pinned revision `440300901dfe9275fd84e0b7763af1f8443ae62e`, [CC0 license](https://raw.githubusercontent.com/sgossner/VSCO-2-CE/440300901dfe9275fd84e0b7763af1f8443ae62e/LICENSE)
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile, pyloudnorm 0.1.1, FFmpeg/libvorbis; independent sample player, original formant synthesis and diffuse reflection mix
- Human modifications: owner accepted V13 composition direction and requested A-to-B continuity repair; agent retained A through the B downbeat, authored B/C note events, articulation progression, dynamics and mix
- License and redistribution terms: instrumental samples CC0-1.0; project composition/recording terms remain under the existing development publication gate
- Required attribution: retain this provenance; no WotG/Calamity music or recording imported
- Reviewer and review date: Codex automated decode/finite/48kHz/stereo/duration/peak checks, 2026-09-10; owner in-game final mix/loop review pending
- Notes: Phase 3; 176.000 BPM; 65.454542s; encoded bytes 1052111; decoded peak 0.972132; SHA256 `be09b9847d6986c300bd21087de438a627a3148b872f2d1b18ec0e8a7d0d1ffb`.

- Runtime file: `Assets/Music/TerminalLiturgy.ogg`
- Asset ID: terminal-liturgy-abc-0242-20260910
- Asset type: music
- Creator: project-directed original composition/orchestration/render by Codex; instrumental sample recordings by Sam Gossner and Simon Dalzell / Versilian Studios, sample cutting by Elan Hickler
- Creation/acquisition date: 2026-09-10
- Source type: original continuation/re-orchestration of the existing project composition
- Source work and URL: existing Convergence A master plus new project-authored B/C note events; instrumental library [VSCO 2 CE official distribution](https://versilian-studios.com/vsco-community/), pinned revision `440300901dfe9275fd84e0b7763af1f8443ae62e`, [CC0 license](https://raw.githubusercontent.com/sgossner/VSCO-2-CE/440300901dfe9275fd84e0b7763af1f8443ae62e/LICENSE)
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile, pyloudnorm 0.1.1, FFmpeg/libvorbis; independent sample player, original formant synthesis and diffuse reflection mix
- Human modifications: owner accepted V13 composition direction and requested A-to-B continuity repair; agent retained A through the B downbeat, authored B/C note events, articulation progression, dynamics and mix
- License and redistribution terms: instrumental samples CC0-1.0; project composition/recording terms remain under the existing development publication gate
- Required attribution: retain this provenance; no WotG/Calamity music or recording imported
- Reviewer and review date: Codex automated decode/finite/48kHz/stereo/duration/peak checks, 2026-09-10; owner in-game final mix/loop review pending
- Notes: Phase 4; 178.626 BPM; 69.866667s; encoded bytes 1141710; decoded peak 0.967757; SHA256 `b9e3a7c89e9ab9eb87688f175de94da74b7d604b619a677d3aecb6eff50b94f3`.


### Prismatic / dimensional SFX rebuild — 0.2.42 / 2026-09-10

- Runtime file: `Assets/Sounds/FirstSeverance/EnergyCharge.wav`
- Asset ID: first-severance-energycharge-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Radiant; use Phase-I energy-charge launch; duration 3.200000s; peak 0.699982; RMS 0.174925; bytes 614444; SHA256 `07a454645d307c19beecfa2813b306fe777b7541a78f10066ef49418136995b6`.

- Runtime file: `Assets/Sounds/FirstSeverance/LanceFire.wav`
- Asset ID: first-severance-lancefire-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Radiant; use Observation-lance / shared beam fire; duration 1.150000s; peak 0.700012; RMS 0.085969; bytes 220844; SHA256 `0b6d7fcaa956bdbfe9e103530df297309533ac2bcafce94e94fb6e6af38c2c37`.

- Runtime file: `Assets/Sounds/FirstSeverance/CoreSalvoFire.wav`
- Asset ID: first-severance-coresalvofire-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Radiant; use Grid-plus-Core combined salvo / empowered shared fire; duration 2.450000s; peak 0.700012; RMS 0.067120; bytes 470444; SHA256 `75995b342014e62f5b25c60c5bb1f81d559aeea92172525ce137f79f7e3bfafd`.

- Runtime file: `Assets/Sounds/FirstSeverance/FinalSlicerFire.wav`
- Asset ID: first-severance-finalslicerfire-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Radiant; use Final slicer live-fire edge; duration 0.920000s; peak 0.699982; RMS 0.086021; bytes 176684; SHA256 `1c69c4472463638b08a87c9ed73092747e3ec5707a717ba84e9cbd9bec5386dc`.

- Runtime file: `Assets/Sounds/FirstSeverance/PhaseRupture.wav`
- Asset ID: first-severance-phaserupture-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Void; use phase-transition rupture / eclosion strain; duration 3.700000s; peak 0.700012; RMS 0.128116; bytes 710444; SHA256 `79454e7dc86a0f2dbef01dbee2c05d9dffc23fdf6a39310a55425e6a0c3ae556`.

- Runtime file: `Assets/Sounds/FirstSeverance/HandClasp.wav`
- Asset ID: first-severance-handclasp-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Void; use Phase-III RemoteClaws flood deployment; duration 1.550000s; peak 0.700012; RMS 0.120298; bytes 297644; SHA256 `9ddbad37cb415bd5e8048802436a0566b90660f9b62e8e42bdce90a4c8fc3225`.

- Runtime file: `Assets/Sounds/FirstSeverance/CrushCataclysm.wav`
- Asset ID: first-severance-crushcataclysm-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Void; use RemoteCrush actual collision impact; duration 2.250000s; peak 0.700012; RMS 0.094300; bytes 432044; SHA256 `7574cf9f4b6afbb7e9808164f5888061a78cb40802ac762b4a7c635c6106d98d`.

- Runtime file: `Assets/Sounds/FirstSeverance/StackSummon.wav`
- Asset ID: first-severance-stacksummon-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Mass; use Stack gather / sanctuary formation; duration 2.850000s; peak 0.700012; RMS 0.152377; bytes 547244; SHA256 `edc5fd84b6e4b0096c3ca2532aa0503f2f14b8e7535b30c9cf080b08b16a3171`.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellMassLatch.wav`
- Asset ID: first-severance-shellmasslatch-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Mass; use Stack shell-fragment birth latch; duration 0.720000s; peak 0.699982; RMS 0.104792; bytes 138284; SHA256 `301b9e651700b598c2f71840ee1f3b53ea60b2137e28bd93812f3d96b57879bb`.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellMassShed.wav`
- Asset ID: first-severance-shellmassshed-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Mass; use Stack success shell shedding; duration 1.280000s; peak 0.699982; RMS 0.093898; bytes 245804; SHA256 `2838d915d99934b1fad3cdfa43e53f5982c399f9740b823f11af3ed6f4026b30`.

- Runtime file: `Assets/Sounds/FirstSeverance/ShellMassCollapse.wav`
- Asset ID: first-severance-shellmasscollapse-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Mass; use Stack failure compression/collapse; duration 1.620000s; peak 0.700012; RMS 0.115942; bytes 311084; SHA256 `ee575df82ad8948fab182901fa9e06f5026dfb2fbb8baa8cc266c1b1395f94a1`.

- Runtime file: `Assets/Sounds/FirstSeverance/MeridianSustain.wav`
- Asset ID: first-severance-meridiansustain-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Sustain; use Pale Meridian overdrive continuous voice; duration 4.000000s; peak 0.699982; RMS 0.264451; bytes 768044; SHA256 `e7e73236cbd8e1f5f141a1dbba36800c1c294fafa43e26c80397dae247d540b8`.

- Runtime file: `Assets/Sounds/FirstSeverance/LacunaSustain.wav`
- Asset ID: first-severance-lacunasustain-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Sustain; use Lacuna Testament empowered beam continuous voice; duration 4.000000s; peak 0.699982; RMS 0.255009; bytes 768044; SHA256 `6eccdf0abddebc24a279c287aaf2d7d4375975e59f8d7d32b116b23443f7fc60`.

- Runtime file: `Assets/Sounds/FirstSeverance/ChoirSustain.wav`
- Asset ID: first-severance-choirsustain-prismatic-0242-20260910
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-10
- Source type: original
- Source work and URL: none; no third-party recording/sample imported; WotG/Nameless Deity/Avatar references are high-level sound-design principles only
- Tool/model/version: Python 3.12; NumPy 2.3.5, SciPy 1.16.1, SoundFile 0.14.0/libsndfile; 96kHz synthesis/DSP -> 48kHz stereo PCM16 runtime export
- Human modifications: owner selected the proposed sound direction and requested implementation; agent authored the independent pre-motion/transient/body/resonance/space recipe
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex deterministic export/decode/finite/format/peak checks, 2026-09-10; human in-game mix review pending
- Notes: family Sustain; use Choir of the Unmade / Requiem continuous voice; duration 4.000000s; peak 0.699982; RMS 0.229518; bytes 768044; SHA256 `56d6f90be0058f911dad86621fa3239bd774ccc3f3a96d4c77278321f26aff95`.
# Loudness recovery exports — 2026-09-10 / 0.2.43

These exact runtime exports supersede only the masters below. Source: project 0.2.42 commit `70b990dea58f4430a05a540f17a678189d26d6ef`; original composition, independent SFX synthesis and pinned CC0 VSCO provenance remain as recorded in the corresponding preceding entries. No new third-party source or recording is introduced. Creator: project-directed Codex mastering; user requested recovery of reduced volume. Toolchain: Python, NumPy2.3.5, SciPy1.16.1, SoundFile0.14.0/libsndfile1.2.2. Stereo-linked lookahead gain control, preserved sample count/stereo balance, .86 pre-encode peak ceiling; PCM16 WAV and Vorbis OGG exports. No hard clipping or gameplay gain changes. Existing license/redistribution terms and attribution obligations remain unchanged.

Reviewer: Codex decode/finite/peak/frame-length/hash checks on2026-09-10; user listening review pending. Prior bytes, PCM24 auditions, pinned-input recipe and tool manifest are retained externally under `output/raid-audio-0243` (local workspace inventory; not an off-device-backup claim). Exact input/output identity and RMS measurements: `docs/evidence/2026-09-10-audio-preparation-polish.json`.

| Runtime file | Export SHA256 |
|---|---|
| `Assets/Music/ObsidianLiturgy.ogg` | `9f0e9e4b6434908ed3e7f5401fd1936e326df1dc27776c5ab818b615cd67219f` |
| `Assets/Music/UnboundLiturgy.ogg` | `61745a5f44dda89620e064402a45a0aa0fe094e5404714c7186689ce2adca808` |
| `Assets/Music/DistantLiturgy.ogg` | `eada0cbb9a39ccef20c827a5bd064f408cfc6cccf9bfc2385bb2738928a92f09` |
| `Assets/Music/TerminalLiturgy.ogg` | `e2e644db1f5f171b780c1912a1a856872d38eaa4f328036c5cc9b87d93209981` |
| `Assets/Sounds/FirstSeverance/EnergyCharge.wav` | `00a33a37454cda1714ba2691355b8496b883f2a7d62d65f2e1c812c69b32fad0` |
| `Assets/Sounds/FirstSeverance/LanceFire.wav` | `d66df0063e30faccf41e26fdf2c2d6c8c7c83f495370fec6a50742173c3e2864` |
| `Assets/Sounds/FirstSeverance/CoreSalvoFire.wav` | `acddb121e15752c43210c1d6dd2fafc062d5adc51df2a674d445fda81cb9717d` |
| `Assets/Sounds/FirstSeverance/FinalSlicerFire.wav` | `82e4488143395a1ab98e58239431c4137693946e773af0aa12d1766c236dfa28` |
| `Assets/Sounds/FirstSeverance/PhaseRupture.wav` | `e57055844dfb5b32b734dda766fcbd0bc06ef317e2ba2d006f5e8e6ecaf76691` |
| `Assets/Sounds/FirstSeverance/HandClasp.wav` | `48c536641c16257db90bc1c69b4dd5cf9be4f362c5948d1b8892def86ff6509b` |
| `Assets/Sounds/FirstSeverance/CrushCataclysm.wav` | `f092c7ab49d482b52874c14a8e1c08117fcef2983c9b5caf4a069709f46a1cc0` |
| `Assets/Sounds/FirstSeverance/StackSummon.wav` | `e32d93796b6ab0a8378e5f9158848f92471adafe3d75219c146c189300cefdf2` |
| `Assets/Sounds/FirstSeverance/ShellMassLatch.wav` | `f2f952c2c9c4a733199e720fee7991e3b5cba0a5d13b78091d21bce661de2f7e` |
| `Assets/Sounds/FirstSeverance/ShellMassShed.wav` | `b08a728aa38ba7da425481f89e58c9bde862e986ec71c06761d2d77a5cf84f63` |
| `Assets/Sounds/FirstSeverance/ShellMassCollapse.wav` | `d8c1628c9b28f9d7dabd90c2b479705117e7bba20ec2c9ef8b40bb208f356fbd` |
# Restored wide/core salvo — 2026-09-10 / 0.2.44

Runtime `Assets/Sounds/FirstSeverance/CoreSalvoFire.wav` is restored byte-for-byte from project commit `5fba4d7` (SHA256 `cb9b1ca7a14de74b288e5f229dc84962b6fa76b24e59d135c8774076dd3c1d2d`). The original project synthesis provenance/license remains in the preceding CoreSalvoFire entry; no new external asset is introduced. User requested the old wide-beam sound. Codex checked decode and source hash; actual listening is pending. Both predecessor and restored bytes plus the pinned-input restoration recipe are retained externally in `output/raid-audio-0244`. Raid playback now has a descriptor-owned end/fade; the file also remains shared by weapon fire. All other sound/music masters remain unchanged in this pass.
# Selective non-Stack restoration — 2026-09-11 / 0.2.45

User-directed restoration of these exact project-owned runtime masters from commit `5fba4d7`; their original synthesis/source/license entries remain applicable. No new composition, sample, processing or third-party source is introduced. Codex verified original Git bytes and finite PCM decode (original44.1/48kHz formats retained). Accepted Stack masters and all BGM are unchanged. Before/restored files and the restoration recipe remain in external `output/raid-audio-0245`; metadata is recorded in `docs/evidence/2026-09-11-selective-sfx-rollback.json`. Runtime listening review pending.

| Runtime file | Restored SHA256 |
|---|---|
| `Assets/Sounds/FirstSeverance/ChoirSustain.wav` | `4e2e0bd06665f06839f9e70c4f461a8fa540578100264d65fce49069fd92cb68` |
| `Assets/Sounds/FirstSeverance/CrushCataclysm.wav` | `d15748b793652312ae956528a9057419aeb0bab33eba99bf3cdd4fd36a3bcef6` |
| `Assets/Sounds/FirstSeverance/EnergyCharge.wav` | `8cefc0827dbe8c9efed6abd5466dcafba5ae78a61337b6b3a4480b7a68199157` |
| `Assets/Sounds/FirstSeverance/FinalSlicerFire.wav` | `79715ec6bceb56a799c9fda5ea8918517113aa172165d0ed1c8f594a7ee4f4f1` |
| `Assets/Sounds/FirstSeverance/HandClasp.wav` | `e3e87ccc900fe78cee0c0733fd01c667b29a4144e239ee19e855a0bfdce78582` |
| `Assets/Sounds/FirstSeverance/LacunaSustain.wav` | `c40271d14be754a2021ee2def8407b44f1be8b51fb8e65a6030c4c35d6a1cc98` |
| `Assets/Sounds/FirstSeverance/LanceFire.wav` | `9ac2dd7a976b4afbe9687162bfc0468ffd0be63b494a6d7975e07d7ddf1d71bb` |
| `Assets/Sounds/FirstSeverance/MeridianSustain.wav` | `6753c47585bb7d5447b3b55de7e832803a35c13c91670c21ad6655a9dbcc0274` |
| `Assets/Sounds/FirstSeverance/PhaseRupture.wav` | `b3f0280827ea4c8377c89c1f3b0b3866cb35a9ea41484d3b104fcc3b04576a79` |


## EigHt `不幸な人形劇` phase masters — 0.2.46

- Creator/composer: **EigHt**.
- Source work: **`不幸な人形劇` (Misfortune Puppet Show)**, published BPM 218.
- Official free-BGM item: <https://booth.pm/ja/items/5206457>. The item page states that the MP3 may be used free of charge and directs users to the governing terms.
- Governing terms link supplied by the creator: <https://eight-novel.fanbox.cc/posts/7647818>.
- Reproducible render source: the official BOOTH-hosted public full-preview stream exposed by item 5206457 JSON. The workflow records that exact stream URL/hash below; the owner separately auditioned/provided the free BOOTH MP3 before approving this phase treatment. Official creator video reference: <https://www.youtube.com/watch?v=vTFL5_d_p7o>.
- Owner-provided BOOTH MP3 audition SHA-256: `b4b9a44f2460f89b628673d4725d24e29d09ea76cc29113e005ba2dde7fbd5e8` (not vendored).
- Repository-render input SHA-256: `b4b9a44f2460f89b628673d4725d24e29d09ea76cc29113e005ba2dde7fbd5e8` (`3605056` bytes; official BOOTH public full-preview MP3, decoded to 48kHz stereo before editing).
- Human modifications: one composition is preserved across all phases. P1/P2 progressively restore harmonic bands, percussive/residual content and stereo width while raising tempo and pitch; P3 is the full 218-BPM/original-pitch identity; Final receives only a small tempo/pitch/low-mid/presence/width lift and bounded peak limiting. No new melody, harmony or section reordering is introduced.
- Redistribution note: the raw source master is not committed. Only the game-facing derived OGG phase masters are distributed. Do not extract or redistribute these as a standalone BGM pack; recheck the creator's current governing terms before any public release or external redistribution of the Mod package.
- Required project credit: credit **Music: EigHt — 不幸な人形劇** and retain the official source/terms links above, even where a downstream use might not otherwise require a credit.
- Reviewer/date: owner selected the work and accepted the phase-treatment direction; repository provenance/decode checks 2026-09-11. In-game transition/mix/loop listening remains user-owned.

| Runtime asset | Exact phase treatment / export |
|---|---|
| `Assets/Music/ObsidianLiturgy.ogg` | P1; 168 BPM; -2 semitone; 233.613s; SHA-256 `39b6d164f46e514078cae9dcefa887ae6359dd7d4c6380129f64404517c191c2` |
| `Assets/Music/UnboundLiturgy.ogg` | P2; 194 BPM; -1 semitone; 202.317s; SHA-256 `fac5a5b6b580ddff0615c3c302377b96ba19759904ce5ec561d8e417c9e964f8` |
| `Assets/Music/DistantLiturgy.ogg` | P3; 218 BPM; +0 semitone; 180.037s; SHA-256 `a19fe1f848a777e825b2bc7701aa78858e3e585183dae89656e767826db162ec` |
| `Assets/Music/TerminalLiturgy.ogg` | FP; 222 BPM; +1 semitone; 176.795s; SHA-256 `f18b080345073b765b614ac9310f18a78fd255ff683223de7d318d6bfacbc94d` |

## EigHt `不幸な人形劇` section-loop / mix revision — 0.2.47

Public-test rights recheck, **2026-09-14 / 0.3.1**: the creator's public terms at the governing link above were updated 2026-07-14. They allow personal/commercial background use in games and videos, and editing, while copyright remains with EigHt. Do not present the recording or phase edits as an original Convergence composition, distribute a standalone music pack, register Content ID or upload the music to streaming services. The four derivatives stay contextualized as game background assets in the Mod/source tree, not release soundtrack downloads. The owner-selected work is unchanged. UTF-8 terms-text SHA256: `f3c3888c1f429d87689061cf4d6f3373a4c2fa19241d67466d97f0f759f08bb8`; public `post.info` metadata (post7647818, unrestricted, fee0) was used to read the FANBOX text. README, package description, Workshop description and release notes now carry the composer/work credit and official links. This is a scoped distribution review, not a grant of rights over other music or third-party Mods.

- Creator/composer, work, source, governing terms and redistribution conditions remain exactly the **0.2.46 EigHt `不幸な人形劇`** record; this revision introduces no new third-party recording or composition.
- Edit basis: the four already-approved 0.2.46 runtime phase masters. P1 remains full-form and receives +6.25dB static gain. P2 is cropped to processed-master 40.873–94.068s and receives +0.75dB. P3 is cropped to 92.523–138.485s. Final is cropped to 145.721–170.849s and reduced 0.60dB for encode headroom. P2/P3/Final receive 20ms entry/exit anti-click fades.
- Playback change: during the 6.0s/5.0s/4.0s `PhaseTransition` windows the previous phase music slot is retained; the next file starts only after transformation resolves. This changes no composition, SFX source, user slider, gameplay clock or network state.
- Loop rule: P2/P3/Final contain only their selected section, therefore an ordinary whole-file loop cannot reintroduce the discarded opening material.

| Runtime asset | Duration | Loudness | True peak | SHA-256 |
|---|---:|---:|---:|---|
| `Assets/Music/ObsidianLiturgy.ogg` | 233.613s | -13.5 LUFS-I | -0.6 dBTP | `586c8a0999b3b03badf5a34cded304bb7018c2f6adc9a58053095958c2d41930` |
| `Assets/Music/UnboundLiturgy.ogg` | 53.195s | -12.2 LUFS-I | -1.4 dBTP | `f649dbe264f7e2167aae0805511f9a312bbd24873321dc4b49e8202eab42073c` |
| `Assets/Music/DistantLiturgy.ogg` | 45.962s | -10.5 LUFS-I | -0.5 dBTP | `e9b75cfc8069fb0ae462a39a49fa651d0f8082e6b1f3668bb3ec2b11f264c7b7` |
| `Assets/Music/TerminalLiturgy.ogg` | 25.128s | -10.4 LUFS-I | -1.0 dBTP | `db5ebaa555b95e29c575e10038f952e13c08a267cf07801a7d173bbbab36539f` |

## Ghost Samurai generated boss rig — 0.2.56 / 2026-09-12

- Runtime asset: `Assets/Textures/GhostSamurai/GhostSamuraiAtlas.png`.
- Source type / creator: AI-generated original artwork using the built-in OpenAI image_gen tool, directed by Codex from the repository owner's supplied skull/oni/two-katana sketch. The tool did not expose a specific model version. No third-party character art, game extraction or external source image is imported.
- Concept owner / requested use: the user supplied the original sketch and explicitly requested regeneration of this boss's visual for Convergence. Artistic in-game acceptance is pending.
- Production treatment: generated horned blue-white skeletal oni body and one detached sword-arm assembly; the arm is mirrored and articulated in code. A transparency edit still returned opaque checkerboard pixels, so a final image_gen edit replaced the backdrop with magenta for runtime color-keying. The selected generated PNG is copied byte-for-byte; no external material is composited into it.
- Runtime processing: `GhostSamuraiArt` converts magenta-dominant pixels to transparent once on the client, keeps the existing alpha of remaining pixels, caches the private texture, and disposes it on unload. The source asset is not mutated and the server never loads graphics.
- Exact source/export identity: 1254×1254 RGB PNG; SHA256 `9e32d45d56d239df31a401cbefbdcc239ebfb81f6dd9cdb28e503adaf961ad48`.
- License / redistribution: original project-directed generated asset; project asset licensing and public-release review remain governed by the existing repository policy. Retain this provenance; no third-party license is asserted.
- Prompt set and verification: `docs/evidence/2026-09-12-ghost-samurai-visuals.json`. Runtime integration was compiled; atlas/key/gutter and an offline multi-background articulated preview were inspected. No claim of in-game approval or measured FPS.



## Energy presentation materials — 2026-09-14

Original Convergence HLSL; no external shader code or texture is vendored. Runtime noise is supplied by the required Luminance dependency. Compiled exports use the pinned FXC/flags in `Assets/AutoloadedEffects/Shaders/compiled.json`. Project asset terms and public-release review remain unchanged. Artist/developer: Codex under the repository owner's direction. Offline FNA/D3D11 inspection, not gameplay acceptance.

- Runtime asset: `Assets/AutoloadedEffects/Shaders/ArmamentEnergy.fxc`; SHA256 `9830c065065c36562ec30b5c2ee453e0c5e3ded65fdf4f67fd2388d4a16d82c8`.
- Original source: `Assets/AutoloadedEffects/Shaders/ArmamentEnergy.fx`; SHA256 `7767d55adb4a41719950ffb480e916d09a450990f5bbadb2045513d651fe7dfb`.

- Runtime asset: `Assets/AutoloadedEffects/Shaders/MechanicRing.fxc`; SHA256 `7556fe157bf8afe6c8d394d87b45f4170660de127044620f358b94487881a20e`.
- Original source: `Assets/AutoloadedEffects/Shaders/MechanicRing.fx`; SHA256 `b685e8922f20aa34fad6f86d6da927d5033885dc2b7229be9fc956e6aec80aa3`.

- Runtime asset: `Assets/AutoloadedEffects/Shaders/PortalBeam.fxc`; SHA256 `cf2ffeb0e7441e6b0c0597f2f9cfd87c77a58af87bc51e4f59b333e73a28d699`.
- Original source: `Assets/AutoloadedEffects/Shaders/PortalBeam.fx`; SHA256 `645ffd856e4ee9cdfc7d663eca01f626aab8e3b883a726876559f7d73eaec1d8`.

README `docs/media/doll-npc.png` is the exact first 32×52 cel of `DollAttendant.png`, enlarged 3× with nearest-neighbor by `tools/export_readme_doll.py`; no repainting or image generation. It inherits the recorded Doll NPC asset's provenance/terms. The former banner and its original provenance remain in history/storage, unused by README.


## Weapon articulation revision — 2026-09-15 / 0.3.7

Creator: Codex under the repository owner's direction; original Convergence DSP plus edits of the already-attributed project-authored Raid beam sounds. No third-party audio, reference recording or music sample. Project asset terms remain unchanged. `tools/remix_weapon_articulation.py` defines explicit windows/rate/EQ, deterministic resonant/noise layers, envelopes and attached reflections; `remix_weapon_foley.py` supplies PCM export and safety metering. [Exact per-file hashes, source hashes and metrics](../docs/evidence/2026-09-15-doll-final-check.json) identify these replacements. Prior masters/source copies remain in ignored external audition archives; previous records are historical for the following destinations. Seven long-beam launches/beds are unchanged. Numerical decode/headroom checks are not human listening approval.

- Runtime asset: `Assets/Sounds/Weapons/DollTheater/ClawSwipe.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/ClawGrip.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/ClawCrush.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/ClawHit.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/MagicSigil.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/MagicBolt.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/MagicMerge.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/MagicCharge.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/RangedLatch.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/RangedShot.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/RangedCharge.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/ChoirNote.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/ChoirCharge.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/WitnessDraw.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/WitnessLock.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/WitnessFire.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/WeaponHit.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/DollSummon.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/DollThread.wav`.
- Runtime asset: `Assets/Sounds/Weapons/DollTheater/DollCharge.wav`.

### Scarlet presentation v2 — 2026-09-17 / 0.3.15

- Original project-authored shader sources/exports: `ScarletSurface`, `ScarletRibbon`, `ScarletResidue`, `ScarletBackdrop` under `Assets/AutoloadedEffects/Shaders`. Source/export/compiler identities are in `compiled.json`. No external Mod shader or artwork copied. Existing Luminance assets and APIs are referenced through the dependency, not vendored. Surface masks and runtime articulation preserve existing approved apparition PNGs.
- Historical failed transfer: the first approved1672×941 cathedral export, SHA256 `94b77c968991bf52b14504bb11095c417dbf4dd2abb3bc378da779da39400a2d`, could not be transferred during compute-backend failure. The owner subsequently supplied another explicitly approved PNG export; the current runtime image is identified by the [Scarlet Sanctum record](#scarlet-sanctum-owner-supplied-background--2026-09-17), not this historical hash. No model-version claim or regenerated substitute is made.
- No new third-party music, sound or texture licenses are asserted. Graceful Ordeal remains separately licensed as recorded above.

## Ghost Samurai articulated rig and Luminance materials — 2026-09-18

Creator: Convergence / Minamium direction, with built-in OpenAI ImageGen assistance for the detached parts and independently authored C#/HLSL. Reference: the project's approved `VioletActions.png`, derived from the owner's September16 violet Boss boards. No third-party sprite, source implementation or audio is imported. Existing project rights/release gates continue to apply; this is not a new public redistribution license.

`Assets/Textures/GhostSamurai/VioletRig.png` is the final generated1254×1254 RGBA output, copied byte-for-byte. Its nine cells are reusable parts, not whole-body animation frames. The first output had a duplicate face on the torso and was rejected; one image-tool edit removed it before import. Runtime UVs, transforms and original shaders perform articulation; no generated source images or third-party binaries enter Git. Exact prompts, selected output identifier and all hashes are in [generation/build evidence](../docs/evidence/2026-09-18-ghost-samurai-rig.json).

Three original `.fx` sources and distributable `.fxc` exports use Luminance's public ManagedShader/PrimitiveRenderer/Metaball APIs. Noise and bloom textures are dependency-owned runtime references, not bundled copies. Exports were compiled with Luminance's pinned FXC tool (`tools/compile_shaders.py --fxc <local-fxc>`); `compiled.json` pairs source and export hashes. [Source/API boundary](../docs/research/2026-09-18-ghost-samurai-luminance.md) records versions and independent choices.

| Runtime/source asset | SHA256 |
|---|---|
| `Assets/Textures/GhostSamurai/VioletRig.png` | `cda2c0fdf4609c98478dd0d75d3d203c78c52add52174f9acd765c77430faab5` |
| `Assets/AutoloadedEffects/Shaders/SamuraiSpirit.fx` | `b569ef5eb648aabfb653e8e828ab771dba293ce55b5177eefb08fe336d801970` |
| `Assets/AutoloadedEffects/Shaders/SamuraiSpirit.fxc` | `a57a91104b48c8913b270f3baf504d04153db13e46853c98759c452523354b70` |
| `Assets/AutoloadedEffects/Shaders/SamuraiRibbon.fx` | `4c6bad4044af61a415d37eaddbca2861839ee52959a2d23cc9aace3b44eff95a` |
| `Assets/AutoloadedEffects/Shaders/SamuraiRibbon.fxc` | `474fa47eed26d1ed1ec43d4b851dc6196dfddc9e85cfa76e41d4b06d5f96fc30` |
| `Assets/AutoloadedEffects/Shaders/SamuraiMist.fx` | `aa12892f9724b06b791b7851ba28727b51125f15d28fa0a22866dfc721508bca` |
| `Assets/AutoloadedEffects/Shaders/SamuraiMist.fxc` | `fb54d35c3be4b9faf8321d9cba68facb4b7db17f3a05da009b297fc0f22d0065` |

## Oboro spectral crescent — 2026-09-21

`Assets/AutoloadedEffects/Shaders/OboroMoonArc.fx` and `Assets/AutoloadedEffects/Shaders/OboroMoonArc.fxc`: original Convergence code-authored procedural material, created for this task with OpenAI Codex assistance; project code licensing applies. Sharp moonlit lip and flowing violet interior reference only the motion principles in the owner's Murasama/Vergil clips. No copied shader, image, extracted game asset or video frame is included. Uses the installed Luminance ManagedShader API; the source/export hashes and compiler identity are recorded in `Assets/AutoloadedEffects/Shaders/compiled.json`. Existing approved Oboro Blade/Spirit images are unchanged.
