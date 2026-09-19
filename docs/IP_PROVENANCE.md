---
doc_id: policy.ip-provenance
document_type: policy
status: accepted
owners:
  - project
  - art
last_reviewed: 2026-09-19
source_of_truth_for:
  - policy.ip_provenance
aliases:
  - IP provenance
  - asset rights policy
related_code:
  - Assets
related_docs:
  - project.asset-pipeline
  - policy.release-process
---

# IP and Asset Provenance Policy

この文書は制作管理方針であり、法的助言ではない。

## Core rule

repositoryへ追加する画像、音声、音楽、font、code、reference dataは、出所と利用根拠を説明できなければmergeしない。

## Required record

各assetについて次を記録する。

```text
Asset path
Creator / contributor
Creation date
Source type (original / generated / commissioned / licensed)
Source links
Tool and model, if generated
Prompt or brief location
Human modifications
License / agreement
Attribution text
Distribution restrictions
Reviewer and review date
```

## Generated images

- promptは独自のsubject、shape、material、compositionで記述する。
- 特定作品、brand、artistの再現を要求しない。
- 生成結果をそのまま完成品にせず、選択、描き直し、分割、animation、pixel cleanupの履歴を残す。
- reference imageを使う場合、利用権と目的を記録する。
- 最終assetとconceptを区別し、不要なconcept sourceは配布buildから除外する。

## Calamity dependency

The0.2.34 weapon score design uses behavior-only study of Yharim's Crystal, Drataliornus and Midnight Sun UFO. [Pinned findings and independent decisions](encounters/first-severance/WEAPONS.md#prior-art-findings-and-engine-seams) record official authorship, version mismatch, verified paths and license. No third-party code or media was imported. Four new text-only built-in image generations and three independently synthesized sustain beds have separate [asset records](../Assets/ATTRIBUTION.md#ritual-grand-apparatus-v3--2026-09-09); the built-in model version was not exposed.

- Calamityのsource、`.tmod`、texture、audioをvendorまたは再配布しない。
- 公開sourceはAPI調査と挙動理解のreferenceとして使う。
- codeを持ち込む必要が生じた場合は、Calamity licenseのcredit/link条件を満たすか確認し、可能なら独立実装を選ぶ。
- userは公式Steam Workshop版を別途導入する。

## Licensed recordings

現在のEigHt BGMはユーザーが曲とフェーズ編集方針を選択したもの。出所・変更・credit・配布制限は [Attribution](../Assets/ATTRIBUTION.md#eight-不幸な人形劇-phase-masters--0246)、制作上の扱いは [Asset Pipeline](ASSET_PIPELINE.md#music-rights-policy) を正本とする。ゲーム同梱と音源単体再配布を混同しない。過去の「全曲project-original」という記述から現在の権利を判断しない。

## Classical music

- 使用する原曲のcomposer、work、edition、source、保護期間を記録する。
- public-domain compositionと、現代編曲・演奏・録音を分ける。
- score/MIDIを自分たちで採譜または利用許諾済みsourceから作る。
- recording、sample library、choir libraryのEULAを保存する。
- 現代訳詞や映画版のedit/orchestrationを流用しない。

## Third-party tools and libraries

- Shader/code package、font、SoundFont、sample packはlicense fileを保存する。
- 商用game内へのrendered output同梱が許可されるか確認する。
- source file再配布が禁止される場合、repositoryへ入れず再現手順だけを記録する。
- CI secretや有償assetを公開artifactへ含めない。

## WotG research influence — 2026-09-06

[WotG benchmark research](research/WOTG_RAID_BENCHMARK.md) records the two user-shared YouTube references and public source revision `7cb5b86c770e73d6853749b2b688d478ba3326a7` (declared 1.2.24). Video metadata/chapters were read, not the audiovisual content itself. Composite body/depth, staged anticipation, off-body attacks, sky/audio choreography and accessibility informed independent proposals for Convergence; none of those proposals was implemented in this documentation pass.

No license permission was established in the inspected repository tree/metadata. Do not treat public visibility, `includeSource` or a filename in `buildIgnore` as permission. No WotG code, shaders, textures, rigs, recordings, videos, or downloaded worlds entered the repository. The report cites behavior and symbols only, preserves version limits, and rejects adopting its single-player assumptions as multiplayer Raid authority. This is research influence, not an asset attribution or release approval.

On 2026-09-13, two separately supplied local gameplay MP4s were decoded and visually examined through overview, dense and native consecutive frames. [F14](research/WOTG_RAID_BENCHMARK.md#f14--recorded-beam-motion-2026-09-13) separates observed motion from candidate source identities; [F15](research/WOTG_RAID_BENCHMARK.md#f15--nameless-portal-triplet-2026-09-13) adds detailed native-frame inspection of the user's selected Nameless V/H/V triplet and independently authored portal-jet choreography. This does not retroactively mark the earlier YouTube references as watched. Originals and analysis frames remain local; no audio assessment, third-party art extraction or recording-to-texture conversion was performed. No WotG equations, shader source or assets were copied; [Attribution](../Assets/ATTRIBUTION.md#portal-triplet-beam-material--2026-09-13) owns the new export.

## Release audit

The `0.2.2` attack choreography follows two user-supplied diagrams: a four-color out-and-back predicted beam sequence and mirrored dash/stillness steps. Their raw images are not distributed; all geometry, timing and animation are independently implemented in C#. No third-party film or Mod assets/code were imported. The original Boss texture and existing vanilla sound references are reused. See [encounter specification](encounters/first-severance/ENCOUNTER_SPEC.md) and [focused public-API evidence](research/INSTANT_REVIVAL_CORE_APIS.md) for design and integration boundaries.

The `0.2.1` Foundation Core monument is an original generated polar containment prism, selected unedited as a provisional runtime asset and independently integrated with a lit control base/placement preview. Its exact output, external prompt path, tool disclosure and release limits are in [Attribution](../Assets/ATTRIBUTION.md). The faster VFX use original geometry and existing vanilla runtime sound IDs, not WotG/film code, images or recordings. Revival/debuff/tile integration follows official pinned API contracts with independent code; [source evidence](research/INSTANT_REVIVAL_CORE_APIS.md) distinguishes observations from unverified multiplayer ordering inferences. This remains development use, not release approval.

The `0.2.0` giant Null Cantor pass is an original polar black-ice/ceramic containment design produced with built-in ImageGen assistance, then independently animated/layered in C#. WotG's named Bosses are a user-provided scale/spectacle benchmark; no source, textures, rigs or shaders were reused. The beam's high-energy contrast is a broad user reference, not a film frame/silhouette/timing/audio reproduction. Official tModLoader hook names/behavior inform independent implementation. See [visual research](research/FIRST_SEVERANCE_GIANT_VISUALS.md) and [exact asset record](../Assets/ATTRIBUTION.md). Source and asset licenses remain undecided; this pass does not authorize release.

The `0.1.2` recovery transport fix follows the official pinned `ModNet.HandleModPacket` reader contract without copying its implementation. The marker correction uses the existing runtime MagicPixel texture with a one-texel source rectangle; no texture or other external asset was extracted or added. See [API evidence](research/FIRST_SEVERANCE_SLICE3_APIS.md).

The 2026-09-05 First Severance experiment uses independently written adapters informed by official tModLoader API behavior; no external source implementation was copied. Foundation Core prototype texture provenance is recorded in [Asset Attribution](../Assets/ATTRIBUTION.md). Boss 3 is a Terraria runtime music reference, not a bundled audio recording.

The 0.2.5 solemn pass replaces the rejected Ninth arrangement with an independently authored composition/render. VSCO 2 CE instrumental samples are CC0 at revision `440300901dfe9275fd84e0b7763af1f8443ae62e`, verified via the official Versilian distribution page and pinned license. Only the new mixed music recording enters the Mod; the raw library stays external. All 18 effects are original synthesis without samples. Boss, cathedral and lance textures were generated from original basalt/ivory/bronze briefs with built-in ImageGen, without external visual inputs. Exact creator/source/tool records and the music rights layers are in [Attribution](../Assets/ATTRIBUTION.md). This changes development assets, not the unresolved public-release license policy.

Release candidateごとに次を確認する。

- untracked asset 0
- provenance未記入asset 0
- forbidden source 0
- required creditsがREADME/Workshop pageへ反映済み
- source-only/non-redistributable fileがpackageへ混入していない
- license選択とcontributor agreement方針が確定済み

The 0.2.8 continuous-emission/ground-containment pass is materially informed by WotG's timed portal-laser layers and articulated rendering, and Calamity's logical arena-wall pattern. [The pinned F11 evidence](research/WOTG_RAID_BENCHMARK.md) records versions, authorship/license limits, observed behavior and independent decisions. No external implementation, shader, atlas, recording or source mirror is copied. Three original generated atlases are used unedited through measured C# source rectangles; exact assets and briefs are in [Attribution](../Assets/ATTRIBUTION.md). Development use does not resolve the project's release-license gate.

## Doll companion and weapon-only sounds — 0.2.53

The 2026-09-13 Doll Raid material suite uses Luminance's public loading/parameter/texture APIs with independently authored HLSL. No Wrath of the Machines/Gods shader or art is copied. Three dependency-owned noise maps are referenced at runtime, not vendored. [API boundary](encounters/first-severance/VISUAL_SPEC.md#luminance-raid-presentation) links pinned observations, adoption/rejection and the source/binary distinction; [Attribution](../Assets/ATTRIBUTION.md#raid-energy-material-suite--2026-09-13) owns export provenance and regeneration. Luminance remains a separately distributed dependency.

[Weapon findings](encounters/first-severance/WEAPONS.md#weapon-sound-and-ten-slot-companion-references) record native summon-slot/lifetime and endgame sound event/voice design influences, with pinned versions and existing license caveats. New weapon recordings are independent NumPy synthesis without samples; the Doll motion sheets are generated from the existing project-authored NPC reference. [Attribution](../Assets/ATTRIBUTION.md#doll-companion-and-weapon-only-foley--0253) owns exact assets, prompts/export recipe, hashes and release limits. No Calamity/Terraria source, extracted sound or external sprite enters the package. The Raid naming is a Convergence character decision, not an assertion that EigHt authored or endorsed the character.

## Oboro and violet Ghost Samurai — 2026-09-16

The owner supplied the weapon sketch and eight violet Boss/weapon boards and requested their appearance in Convergence. The selected weapon, spirit and twelve-pose atlas were newly generated from those references with the built-in image tool; the supplied dissolve sheet is used as a runtime atlas. No Calamity/Murasama code or extracted artwork was copied. [API/reference boundary](research/2026-09-16-oboro.md) and [exact exports](../Assets/ATTRIBUTION.md) distinguish visual references, gameplay choices and publication provenance.

The September17 [Murasama motion survey](research/2026-09-16-oboro.md#murasama-motion-reference--2026-09-17) informs the three-beat presentation and heavier final-cut emphasis. Oboro's easing, pose history, violet ribbons and sprite echoes are independently written; existing project-authored textures are reused. No new third-party asset or implementation is imported.

## Ghost Samurai articulated presentation — 2026-09-18

The owner requested extensive use of the existing Luminance dependency. [Research](research/2026-09-18-ghost-samurai-luminance.md) separates public API inventory, inspected implementations and installed-binary checks. Calamity Catastrophe/Murasama informs motion emphasis only. The detached purple rig was generated from the existing project-authored atlas; all three materials and articulation code are original. Luminance retains ownership of runtime noise/bloom textures and render targets. No extracted asset, code mirror or dependency binary is imported; [attribution](../Assets/ATTRIBUTION.md#ghost-samurai-articulated-rig-and-luminance-materials--2026-09-18) owns exact exports/prompts/hashes.
