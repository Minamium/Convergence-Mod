---
doc_id: compatibility.version-matrix
document_type: policy
status: accepted
owners:
  - engineering
last_reviewed: 2026-09-05
source_of_truth_for:
  - compatibility.version_matrix
aliases:
  - version matrix
  - dependency pin
related_code:
  - build.txt
  - global.json
  - ConvergenceMod.csproj
  - Common/Compatibility/Calamity
related_docs:
  - development.windows
  - policy.release-process
  - project.status
---

# Version Matrix

調査・実機確定日: **2026-09-05**

## Confirmed Windows compatibility baseline

| Component | Pinned target | Status | Rationale |
|---|---:|---|---|
| Terraria | `1.4.4.9` | Confirmed | tModLoader実行ログとSingle Player/Dedicated Serverで確認 |
| tModLoader | `v2026.07.3.0`, stable | Confirmed | 2026-09-01公開releaseを実機Build + Reload/Serverで確認 |
| tModLoader source | `666f69962d3bdffde54fc14025f02634965b4e7c` | Confirmed | release/runtimeが報告したsource commit |
| tModLoader API docs | `v2026.07` | Confirmed for research | pinned stable release family |
| Runtime | .NET 8 | Confirmed from stable targets | `TargetFramework=net8.0` |
| C# | 12.0 | Confirmed from stable targets | `LangVersion=12.0`（個別csprojで`latest`に上書きしない） |
| Calamity Mod | `2.2.4` | Confirmed | 公式Workshop binaryをBuild + Reload/Server/2-clientで確認 |
| Calamity Music | `2.1` | Confirmed | 公式必須依存を同じ実機runで確認 |
| Calamity source reference | `1a8cebd27ec5615316b78f71973446b5528d2b78` (`2.2.2`) | Reference only | 公開mirrorに2.2.4相当sourceが無いため、binaryとの同一性は主張しない |
| Calamity internal name | `CalamityMod` | Confirmed | `build.txt`とnamespace |
| Addon dependency | `CalamityMod@2.2.4` | Confirmed | `build.txt`の下限とruntime gateを実機確認 |

This compatibility freeze describes Stage A of [ADR-0006](adr/0006-staged-calamity-independence.md); it is not a permanent commitment to a hard Calamity dependency.

現在の`build.txt`:

```text
displayName = Convergence (Development Build)
author = Minamium
version = 0.1.0
modReferences = CalamityMod@2.2.4
side = Both
playableOnPreview = false
hideCode = false
hideResources = false
includeSource = false
```

Source/asset licenseが未決定のため、accidental `.tmod` source distributionを避ける目的で`includeSource = false`に固定する。ライセンス決定後に配布方針と合わせて再審査する。

内部Mod/assembly名とroot namespaceは開発コードネーム`Convergence`とした。entry class/project filenameは`ConvergenceMod`である。公開名は未決定であり、`displayName`はdevelopment用である。tModLoaderは`ModReference`の`Name@Version`形式を、指定版以上かつ同じmajor versionとして判定する。したがって`CalamityMod@2.2.4`はloaderの依存解決では後続2.xを許可し、3.0.0を拒否する。

企画上の対応範囲を2.2.xへ限定するため、Compatibility層で実行時に`2.2.4 <= version < 2.3.0`を検査する。範囲外ではMod全体をcrashさせず、Raid起動を無効化して必要versionを表示する。この数値下限だけは[ADR-0008](adr/0008-confirmed-2026-07-runtime-baseline.md)が[ADR-0004](adr/0004-calamity-compatibility-boundary.md)の旧下限を更新し、adapter境界そのものは維持する。

## Build project policy

stable ExampleModと同じく、プロジェクトはtModLoader配下の`../tModLoader.targets`をImportする。

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="..\tModLoader.targets" />
  <PropertyGroup>
    <AssemblyName>Convergence</AssemblyName>
    <RootNamespace>Convergence</RootNamespace>
  </PropertyGroup>
</Project>
```

Target FrameworkとC# versionは固定tModLoader targetsが提供するためAddon側で上書きしない。nullable、implicit usings、analysis levelだけを`Directory.Build.props`で指定する。

`tModLoader.targets`の場所に依存するため、リポジトリ単体の一般的な`dotnet build`は標準の成功条件にしない。正式な検証は`ModSources/Convergence`へcheckoutし、固定tModLoader環境で実行する。folder、assembly、namespace先頭を一致させる。

## Calamity integration boundary

Calamityへのアクセスは`Common/Compatibility/Calamity/`へ隔離する。

- 進行判定は、可能なら公開`Mod.Call`（例: `GetBossDowned`）をラップする。
- Damage Class、Adrenaline、Rogue Stealthなど直接型参照が必要な機能はadapter越しに公開する。
- Calamityのprivate/internal実装、IL patch、reflection依存を初期実装で使用しない。
- コンパイル時にCalamityを直接参照するファイルを限定し、更新時の修正面積を小さくする。
- Calamityソースをコピーしない。参照目的で利用し、コードを移植する場合はライセンス条件とクレジットを個別確認する。
- `Mod.Call`が返す値は必ず型検査する。不正引数時に`Exception` object、不明Call時に`null`が返る可能性を扱う。
- Calamity `2.2.4`本体は`CalamityModMusic`を必須参照する。開発環境へ公式Music Mod `2.1`も導入するが、本Addonから音楽APIを直接使わない限り重複して`modReferences`へ追加しない。

## Confirmation evidence

2026-09-05にcommit `b34adbc18fc5191280e5681a041686699255068b`のclean checkoutで次を完了した:

1. repository/catalog/YAML checksとdependency-free domain harness（24 tests）。
2. `dotnet build ConvergenceMod.csproj`（0 warnings/errors）。
3. tModLoader Build + Reload。
4. Single PlayerでWorldへ入って退出。
5. Dedicated Serverを起動し、同一Windows host上の2クライアントで接続・退出・保存終了。
6. 実行ログからTerraria、tModLoader、Calamity、Music、Addonのversionを記録。
7. Calamity `.tmod`のSHA-256を記録し、binary/log/player/worldはrepositoryへ保存しない。

結果は[2026-09-05 Windows baseline](evidence/2026-09-05-windows-baseline.json)に保存した。Host & Playは`not_run`であり、Dedicated Server結果から推定しない。baselineのenabled ModはCalamity Music、Calamity、Convergenceだけで、任意の開発補助Modは互換性確定根拠へ含めない。

公開source参照は2.2.2時点で、確認したCalamity 2.2.4 Workshop binaryと同一sourceであるとは証明できない。2.2.4固有APIへ依存する変更は、公開・許諾された対応sourceまたは公式API根拠が得られるまで行わない。

## Upgrade policy

- tModLoader monthly stableまたはCalamity patch releaseへは自動追従しない。この表の「latest」は確定日現在の意味であり、moving targetではない。
- 更新は専用branchでbuild、Dedicated Server、2人smoke testを通してから行う。
- 互換性修正とEncounter変更を同じcommitへ混在させない。
- 1.4.5 portは別milestone・別branchとし、1.4.4の完成前には開始しない。

## Binary provenance

- Calamityの`.tmod`をrepositoryや公開CI artifactへ含めない。
- 利用者向け依存先は公式Steam Workshop版だけとする。
- Calamity公開mirrorはbranchがrelease時に置換されるため、調査根拠はbranch名ではなく上記commit SHAで記録する。
- 実機freeze時にWorkshop ID、実際のMod version、可能ならlocal `.tmod` SHA-256を非配布のbuild recordへ保存する。
