# Version Matrix

調査基準日: **2026-08-23**

## Initial compatibility freeze

| Component | Initial target | Status | Rationale |
|---|---:|---|---|
| Terraria | 1.4.4.9 | Candidate | tModLoader 1.4.4系のゲーム基盤 |
| tModLoader | `v2026.06.3.6`, 1.4.4 stable | Candidate | 2026-08-13公開のlatest stable |
| tModLoader source | `29bf9785f5f4de8cd305be002c4cc48aa1177b20` | Confirmed | tagのsource commit。Calamity向けmitigation patchを含む |
| tModLoader API docs | `v2026.06` | Confirmed for research | stable API docsの表示バージョン |
| Runtime | .NET 8 | Confirmed from stable targets | `TargetFramework=net8.0` |
| C# | 12.0 | Confirmed from stable targets | `LangVersion=12.0`（個別csprojで`latest`に上書きしない） |
| Calamity Mod | `2.2.2` | Candidate | 公開mirrorの`build.txt` |
| Calamity source reference | `1a8cebd27ec5615316b78f71973446b5528d2b78` | Confirmed | `Merge Update 2.2.2 into release branch` |
| Calamity internal name | `CalamityMod` | Confirmed | `build.txt`とnamespace |
| Addon dependency | `CalamityMod@2.2.2` | Proposed | 最低バージョンを明示し、互換性を固定 |

`build.txt`の初期案:

```text
displayName = Calamity Multiplayer Raid Addon
author = Minamium
version = 0.1.0
modReferences = CalamityMod@2.2.2
side = Both
hideCode = false
hideResources = false
includeSource = true
```

内部Mod名と公開名は未決定のため、このファイルはまだ作成しない。tModLoaderは`ModReference`の`Name@Version`形式を、指定版以上かつ同じmajor versionとして判定する。したがって`CalamityMod@2.2.2`は2.2.3や2.3.0を許可し、3.0.0を拒否する。

企画上の対応範囲を2.2.xへ限定するため、Compatibility層で実行時に`2.2.2 <= version < 2.3.0`を検査する。範囲外ではMod全体をcrashさせず、Raid起動を無効化して必要versionを表示する。

## Build project policy

stable ExampleModと同じく、プロジェクトはtModLoader配下の`../tModLoader.targets`をImportする。

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="..\tModLoader.targets" />
  <PropertyGroup>
    <AssemblyName>__INTERNAL_MOD_NAME__</AssemblyName>
    <LangVersion>12.0</LangVersion>
  </PropertyGroup>
</Project>
```

`tModLoader.targets`の場所に依存するため、リポジトリ単体の一般的な`dotnet build`は標準の成功条件にしない。正式な検証コマンドは、tModLoaderのMod Sources配下または同等の明示的な`tmlPath`設定で実行する。

## Calamity integration boundary

Calamityへのアクセスは`Common/Compatibility/Calamity/`へ隔離する。

- 進行判定は、可能なら公開`Mod.Call`（例: `GetBossDowned`）をラップする。
- Damage Class、Adrenaline、Rogue Stealthなど直接型参照が必要な機能はadapter越しに公開する。
- Calamityのprivate/internal実装、IL patch、reflection依存を初期実装で使用しない。
- コンパイル時にCalamityを直接参照するファイルを限定し、更新時の修正面積を小さくする。
- Calamityソースをコピーしない。参照目的で利用し、コードを移植する場合はライセンス条件とクレジットを個別確認する。
- `Mod.Call`が返す値は必ず型検査する。不正引数時に`Exception` object、不明Call時に`null`が返る可能性を扱う。
- Calamity `2.2.2`本体は`CalamityModMusic`を必須参照する。開発環境へ公式Music Modも導入するが、本Addonから音楽APIを直接使わない限り重複して`modReferences`へ追加しない。

## Freeze gate

候補を確定値へ変更する条件:

1. 対象tModLoaderをfresh installする。
2. Calamity `2.2.2`と必須依存ModをWorkshopから導入する。
3. 最小AddonをBuild + Reloadする。
4. Single PlayerでWorldへ入る。
5. `start-tModLoaderServer`相当でDedicated Serverを起動する。
6. 2クライアントが接続し、Addonのpacket round-tripを確認する。
7. 実行ログからTerraria、tModLoader、Calamity、Addonのversionを記録する。

失敗した場合、latestへ無条件追従せず、動作した組み合わせをこの表へ固定する。

## Upgrade policy

- tModLoader monthly stableまたはCalamity patch releaseへは自動追従しない。
- 更新は専用branchでbuild、Dedicated Server、2人smoke testを通してから行う。
- 互換性修正とEncounter変更を同じcommitへ混在させない。
- 1.4.5 portは別milestone・別branchとし、1.4.4の完成前には開始しない。

## Binary provenance

- Calamityの`.tmod`をrepositoryや公開CI artifactへ含めない。
- 利用者向け依存先は公式Steam Workshop版だけとする。
- Calamity公開mirrorはbranchがrelease時に置換されるため、調査根拠はbranch名ではなく上記commit SHAで記録する。
- 実機freeze時にWorkshop ID、実際のMod version、可能ならlocal `.tmod` SHA-256を非配布のbuild recordへ保存する。
