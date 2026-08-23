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

Release candidateごとに次を確認する。

- untracked asset 0
- provenance未記入asset 0
- forbidden source 0
- required creditsがREADME/Workshop pageへ反映済み
- source-only/non-redistributable fileがpackageへ混入していない
- license選択とcontributor agreement方針が確定済み
