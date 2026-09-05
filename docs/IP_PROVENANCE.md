---
doc_id: policy.ip-provenance
document_type: policy
status: accepted
owners:
  - project
  - art
last_reviewed: 2026-09-06
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

- Calamityのsource、`.tmod`、texture、audioをvendorまたは再配布しない。
- 公開sourceはAPI調査と挙動理解のreferenceとして使う。
- codeを持ち込む必要が生じた場合は、Calamity licenseのcredit/link条件を満たすか確認し、可能なら独立実装を選ぶ。
- userは公式Steam Workshop版を別途導入する。

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

## Release audit

The `0.2.1` Foundation Core monument is an original generated polar containment prism, selected unedited as a provisional runtime asset and independently integrated with a lit control base/placement preview. Its exact output, external prompt path, tool disclosure and release limits are in [Attribution](../Assets/ATTRIBUTION.md). The faster VFX use original geometry and existing vanilla runtime sound IDs, not WotG/film code, images or recordings. Revival/debuff/tile integration follows official pinned API contracts with independent code; [source evidence](research/INSTANT_REVIVAL_CORE_APIS.md) distinguishes observations from unverified multiplayer ordering inferences. This remains development use, not release approval.

The `0.2.0` giant Null Cantor pass is an original polar black-ice/ceramic containment design produced with built-in ImageGen assistance, then independently animated/layered in C#. WotG's named Bosses are a user-provided scale/spectacle benchmark; no source, textures, rigs or shaders were reused. The beam's high-energy contrast is a broad user reference, not a film frame/silhouette/timing/audio reproduction. Official tModLoader hook names/behavior inform independent implementation. See [visual research](research/FIRST_SEVERANCE_GIANT_VISUALS.md) and [exact asset record](../Assets/ATTRIBUTION.md). Source and asset licenses remain undecided; this pass does not authorize release.

The `0.1.2` recovery transport fix follows the official pinned `ModNet.HandleModPacket` reader contract without copying its implementation. The marker correction uses the existing runtime MagicPixel texture with a one-texel source rectangle; no texture or other external asset was extracted or added. See [API evidence](research/FIRST_SEVERANCE_SLICE3_APIS.md).

The 2026-09-05 First Severance experiment uses independently written adapters informed by official tModLoader API behavior; no external source implementation was copied. Foundation Core prototype texture provenance is recorded in [Asset Attribution](../Assets/ATTRIBUTION.md). Boss 3 is a Terraria runtime music reference, not a bundled audio recording.

Release candidateごとに次を確認する。

- untracked asset 0
- provenance未記入asset 0
- forbidden source 0
- required creditsがREADME/Workshop pageへ反映済み
- source-only/non-redistributable fileがpackageへ混入していない
- license選択とcontributor agreement方針が確定済み
