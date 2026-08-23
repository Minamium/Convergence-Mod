# Audio Cue Sheet

初期の作曲・実装用working sheet。秒数と小節数はprototype後に固定する。

## Global rules

- gameplay clockはserver tick。音声再生位置を判定へ使わない。
- cueは`CueId`、`CueStartTick`、`TransitionType`でclientへ通知する。
- telegraphには必ず視覚情報を併用する。
- masterは48 kHz / 24-bit WAV、game assetはloop metadata付きOGGを第一候補とする。
- loudness targetは実機でCalamity music/SFXと比較して決定する。

## Proposed cues

| Cue ID | Phase | Tempo concept | Loop | Transition |
|---|---|---:|---|---|
| `ACTIVATION_01` | Base Activation | free -> 60 BPM | no | silenceからfade in |
| `SEAL_01` | Seal Release | 120 BPM, scherzo | yes | downbeatで開始 |
| `PARTS_01` | Part Break | 120 BPM | yes | short stinger |
| `COORD_01` | Coordination | 120 BPM | modular | mechanic phrase境界 |
| `EFFIGY_01` | Personal Effigies | 90/120 BPM | yes | player motifsを統合 |
| `WEAK_01` | Weak Point | 60 BPM chorale | no/short | exposed eventでhit |
| `ENRAGE_01` | Hard Enrage | 150 BPM | yes | hard cut + impact |
| `LAST_01` | Last Stand | 120 BPM fixed form | no | server sequence start |
| `VICTORY_01` | Clear | free | no | final core hit |
| `WIPE_01` | Wipe | free | no | harmony collapse |

## Motif plan

- Foundation motif: open fifth + rising semitone cluster。
- Identity motif: playerごとにinterval/orderを変えられる4音cell。
- Convergence motif: 複数cellが同じcadenceへ集まる。
- Overload motif: bass noteを半音ずつ上げ、安定和音を侵食。
- Weak Point: chorale textureへ一時的に完全なthirdを導入。
- Last Stand: public-domain原曲から採譜した素材を、新規harmony/rhythm/orchestrationで変奏。

## Mechanic accents

- Stack marker lock: low percussion + unified choir consonant。
- Spread marker lock: four spatially distinct high attacks。ただしstereo定位だけに依存しない。
- Bait target: short identifiable pulse。
- Pylon timeout: remaining barsを明確にするostinato reduction。
- Revive complete: restrained consonant cue。連続再生を制限。

## Deliverables per cue

- MIDI source
- MusicXMLまたはscore PDF
- DAW project and version
- tempo map
- full mix WAV
- game OGG
- optional stems
- loop sample positions
- composer/performer/library attribution
- license record
- in-game test notes
