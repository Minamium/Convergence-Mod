# Asset Attribution Register

## Records

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
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 70604 bytes; SHA256 `e09a2dc86681a8acbb6a4a266065a17c075b7ba084edbfca0335c046a24cbdb6`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ChoirFire.wav`
- Asset ID: doll-theater-0253-choirfire
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 35324 bytes; SHA256 `85519d50ae2f89b3b6f66afe7cfe30562013fe2e1ccea6f33c258e16d2e5e7c3`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ChoirNote.wav`
- Asset ID: doll-theater-0253-choirnote
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 16802 bytes; SHA256 `3d52557aff6a128f1362491eb443b316a161a639bdede358844af91e0303ebba`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ChoirSustain.wav`
- Asset ID: doll-theater-0253-choirsustain
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 705644 bytes; SHA256 `76bc96314a6bcaca63771948930c7cdb37cb8e53d65b5fb38965949b05d30579`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ClawCrush.wav`
- Asset ID: doll-theater-0253-clawcrush
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 26504 bytes; SHA256 `ffaaf4e77af1608cc297d23e4c819fa1e76c22d8942452548fd03579967d3b96`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ClawGrip.wav`
- Asset ID: doll-theater-0253-clawgrip
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 17684 bytes; SHA256 `3ab3c240da43cbd3eefba0b0a36ad408d5c72f160a2441e94761a4de430bddcf`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ClawHit.wav`
- Asset ID: doll-theater-0253-clawhit
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 9746 bytes; SHA256 `e833c0a49cf4c9c6d92f8bedb41501faf74f6052b34e24fabd87eae75eaa46ab`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/ClawSwipe.wav`
- Asset ID: doll-theater-0253-clawswipe
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 12392 bytes; SHA256 `0319fe16afc13da781f805bda0c3b27ea84cb1e74d2fc6aa0bc4bd227d81015f`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/DollCharge.wav`
- Asset ID: doll-theater-0253-dollcharge
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 52964 bytes; SHA256 `deb633b11c43d3e59436a77bfe8a906d7660433680ad977441489f835cea0366`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/DollSummon.wav`
- Asset ID: doll-theater-0253-dollsummon
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 44144 bytes; SHA256 `27ffdbe5e771e00e36f6d162e0b3cec455afac1a7392bd258daa2dde47d751a9`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/DollThread.wav`
- Asset ID: doll-theater-0253-dollthread
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 14156 bytes; SHA256 `0c42e1df804d00e3da199ac49786b7b17cc735de0cbb1d48dea7bd09dba62d34`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/DollVerdict.wav`
- Asset ID: doll-theater-0253-dollverdict
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 28268 bytes; SHA256 `d1bab2ef806460dde10f4aa51d61fc8ea1945d0a6e7cf629a1efca93d50fb730`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/LacunaSustain.wav`
- Asset ID: doll-theater-0253-lacunasustain
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 705644 bytes; SHA256 `f3a86de2f999b661c6e78a25b75fb8b89705d5f4a86df4aecd7e92d9d182c325`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MagicBolt.wav`
- Asset ID: doll-theater-0253-magicbolt
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 10628 bytes; SHA256 `7310665e33da0d792c36c2b2e90985c6ba53d9974bbda9b8c696046509554d13`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MagicCharge.wav`
- Asset ID: doll-theater-0253-magiccharge
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 70604 bytes; SHA256 `b6e42ac9588e4376147b48d61ad01977e175155f7c1eebb918418dbbd20e0f01`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MagicFire.wav`
- Asset ID: doll-theater-0253-magicfire
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 37088 bytes; SHA256 `0ce6e41bd9fe1853016d7215980fe1b36e22952dd3cc62169719fcc1fceed42c`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MagicMerge.wav`
- Asset ID: doll-theater-0253-magicmerge
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 30032 bytes; SHA256 `19419ecd8f6e015504edde3e0427142ff0ddfe08a7520903a5b83e87f0bc5d85`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MagicSigil.wav`
- Asset ID: doll-theater-0253-magicsigil
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 12392 bytes; SHA256 `3257470d6e902f11ec7bcf3540a10f61e9ace0be461d85f1641bb0306426b4a2`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/MeridianSustain.wav`
- Asset ID: doll-theater-0253-meridiansustain
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 705644 bytes; SHA256 `108c5fc5e0b21bd51bcb2fbec0b6474f4284fa444b04c5dd6b135084544e7e62`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/RangedCharge.wav`
- Asset ID: doll-theater-0253-rangedcharge
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 70604 bytes; SHA256 `f0d8bda3def8fff6d9cee76a23c4437a270741fad1d3bf97bde66bc8ec5382cd`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/RangedFire.wav`
- Asset ID: doll-theater-0253-rangedfire
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 30032 bytes; SHA256 `1a5f49abc3fb6e44041cd4a133889ef682e682f961fcf0b7cd4f175e4f055c92`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/RangedLatch.wav`
- Asset ID: doll-theater-0253-rangedlatch
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 9746 bytes; SHA256 `e33d64785ae21c0f2ae64ee1d711bc5f12226f431015461bfb264ea12f56edd4`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/RangedShot.wav`
- Asset ID: doll-theater-0253-rangedshot
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 9304 bytes; SHA256 `8357069202570f1b6d07d0e3f37f261bf9da8a95b96a7d05d3f1071794bffb13`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/WeaponHit.wav`
- Asset ID: doll-theater-0253-weaponhit
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 8864 bytes; SHA256 `fbb4db8e6376a2faa3bdb3a2efe88915ba530f33b2d27296a8d2c236c4e37123`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/WitnessDraw.wav`
- Asset ID: doll-theater-0253-witnessdraw
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 14156 bytes; SHA256 `16ca8028f9e8324b901e2ea28cdb279364edc1679d3cee4a4b540e1c3a8210ae`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/WitnessFire.wav`
- Asset ID: doll-theater-0253-witnessfire
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 33560 bytes; SHA256 `93a0239565a7d80a9d5d1b35b4e11a6b40e36bca2977b843715bc9a2299cf53c`.

- Runtime file: `Assets/Sounds/Weapons/DollTheater/WitnessLock.wav`
- Asset ID: doll-theater-0253-witnesslock
- Asset type: audio
- Creator: project-directed original deterministic DSP synthesis by Codex
- Creation/acquisition date: 2026-09-12
- Source type: original
- Source work and URL: none; no third-party recording or sample imported
- Tool/model/version: Python/NumPy; tools/generate_weapon_foley.py; 44.1kHz PCM16
- Human modifications: owner requested endgame weapon sound redesign; agent authored transient/body/charge/loop recipes
- License and redistribution terms: project asset terms remain under the existing development publication gate
- Required attribution: retain this provenance
- Reviewer and review date: Codex decode/finite/level/cadence checks, 2026-09-12; actual game listening pending
- Notes: 31796 bytes; SHA256 `2c5bba9a19786ac6d93350451170c8e6cc08d48b3b99188691347587fa7cafa9`.


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
