# Asset and Audio Pipeline

## What Codex can produce here

### Strong fit

- original concept art、mood board、boss silhouette、Arena background案
- UI mockup、warning typography、phase title、marker案
- placeholder icon、tile texture、projectile/VFX texture
- sprite-sheetのラフ案とanimation key pose案
- 既存の添付画像を基にした編集・variation
- Shader、primitive trail、custom draw、particle systemのコード
- asset naming、atlas、frame metadata、export script
- 楽曲のmotif、和声、form、tempo map、orchestration、cue sheet
- MIDI / MusicXMLなど編集可能な作曲データの生成
- phase transitionとserver tickを結ぶ実装設計
- tModLoader用`.ogg`変換、loop tag、音量検査の自動化

OpenAI公式のCodex image generation guidanceは、UI asset、background、illustration、sprite sheet、placeholderの生成・編集を用途として明示している。

### Requires human/tool refinement

- 1 px単位で完全に整ったproduction pixel art
- 多数frame間で輪郭と装飾が完全一致するanimation
- Terraria/Calamityの既存art directionへ自然に溶け込む最終調整
- hitboxと視認性を考慮した実機playtest
- release-readyな管弦楽sample libraryによるmix/master
- 合唱の発音、voice leading、音域、自然な奏法の最終監修

AI生成画像は完成品と決め打ちせず、concept -> silhouette -> key art -> pixel cleanup -> in-game testの順で利用する。

## Visual workflow

1. **Visual bible** — palette、shape language、禁止要素、scale chartを固定。
2. **Concept batch** — 独自subjectとして複数案を生成。特定作品・artistの模倣promptを使わない。
3. **Selection** — readability、2D hitbox、silhouetteで選ぶ。
4. **Key poses** — idle、attack、break、deathの少数frameを決める。
5. **Pixel production** — Aseprite/Krita等でpaletteとedgeを手修正。
6. **Export** — transparent PNG、固定frame size、nearest-neighbor前提。
7. **In-game QA** — 100% UI scale、各resolution、色覚差、4人分marker重複を確認。

### Repository and working-source separation

```text
external working storage/     # repository外、制作台帳とbackupを別管理
  Concepts/
  Editable/
  DAW/

repository/Assets/            # review済みruntime exportのみ
  Textures/
    NPCs/
    Projectiles/
    Tiles/
    UI/
    VFX/
  Effects/
  Music/
  Sounds/
```

Concept batch、editable source、DAW project、raw recordingはこのrepositoryへcommitしない。外部制作台帳でprompt、tool/model、creator、生成日、license、human editsを保持し、選別したruntime exportをcommitするときに必要なprovenance要約を`Assets/ATTRIBUTION.md`へ転記する。ATTRIBUTIONは配布fileだけを厳密に追跡する。

大型Bossは一枚の巨大animationへせず、本体、Crown、Wings、Heart/Core、glow maskを別spriteにし、code transformとVFXで組み合わせる。これにより部位破壊、network state、animation差分を一致させやすい。

## VFX policy

- gameplay hitboxとtelegraphは低密度で明確なprimitiveとして描く。
- 装飾particle、afterimage、background弾幕はclient-only。
- 太いlaserは1 Projectile + primitive trailを基本とする。
- markerは色だけで区別せず、shape、icon、animationも変える。
- screen shake、flash、chromatic effectsは設定で軽減可能にする。

## What can be done for music

クラシックを基にしたBGMは作曲・編曲できます。具体的には、motif選定、独自変奏、和声、tempo map、管弦楽法、choir voicing、phase別formを設計し、MIDI/MusicXMLとcue sheetを生成できます。また、単純なsynthによるrough WAV/OGGはコードで作れます。

ただし、この作業環境には現時点でrelease-readyなorchestral sample libraryや専用music generation toolが確認できないため、完成音源の品質は別問題です。最終的な管弦楽・合唱音源はDAW + 正規ライセンスのVST、またはcomposer/engineerによるrender・mix・masterを推奨します。こちらは作曲データ、制作仕様、iteration、実装、技術QAまで担当できます。

## Beethoven Ninth policy

ベートーヴェンの原曲は著作者死亡から十分な期間が経過しており、保護期間満了作品を新たに編曲・演奏する方針は成立する。一方で、**作曲物、現代の編曲、特定の演奏、録音物は別の権利対象**として扱う。

許可するsource:

- 原典または明確にpublic-domainと表示されたscore
- 自分たちが新規作成したMIDI、orchestration、performance、recording
- 商用ゲーム同梱を明示的に許すsample/VST license

禁止するsource:

- 映画、CD、配信、YouTube等から抽出した録音
- 現代編曲者のscore/MIDIを無許可で流用
- 既存作品特有のtempo、orchestration、edit、sound designを再現
- license不明のSoundFont、choir sample、SFX

これは法的助言ではない。公開前に配布地域と利用素材について権利確認を行う。

## Initial score architecture

初期案は、完全に同期したdynamic stemsではなく、次のphase-specific full mixesを使用する。

| Cue | Function | Musical direction |
|---|---|---|
| `BaseActivation` | field deploy | 無から形成、低音、遠い合唱、機械pulse |
| `SealRelease` | pylon DPS | scherzo、明確な打楽器grid、countdown |
| `PartBreak` | route choice | ostinato、partごとのinstrument color |
| `Coordination` | stack/spread/line | telegraphと同期するaccent |
| `PersonalEffigies` | individual tests | classごとのmotif断片 |
| `WeakPoint` | burst | 遅いchorale上の強いharmonic arrival |
| `LastStand` | fixed sequence | 独自の歓喜主題変奏、失敗でharmony崩壊 |

serverは`PhaseStartTick`とcue IDを送る。clientはlocal audioを開始し、gameplay判定はaudio playback positionへ依存させない。

120 BPMなら1拍=30 game ticks、4/4の1小節=120 ticksとなる。prototypeでは整数tickへaccentを置きやすいtempoを選ぶが、audio device latencyやframe timingを理由に判定窓を狭めない。

## tModLoader audio facts

tModLoader stableはMusic folder内の`.mp3`、`.ogg`、`.wav`をautoloadできる。配布版は容量、loop、decoder挙動を考慮し`.ogg`を第一候補とする。OGGの`LOOPSTART`/`LOOPEND` sample tagを利用できるが、実機でseamを検証する。

SFXは`SoundStyle`でvolume、pitch、variants、loop、max instancesを制御する。専用serverでは音声assetを前提にしたロジックを実行しない。

## Audio production workflow

1. 60～90秒のmotif and instrumentation prototype。
2. tempo mapをgame ticksへ対応付ける。
3. MIDI/MusicXMLをDAWへimport。
4. 正規ライセンス音源でorchestration。
5. phaseごとにfull mixとtransition tailを書き出す。
6. 48 kHz/24-bit WAV masterを保存。
7. game assetはOGGへencodeしloop metadataを付与。
8. loudness、clipping、loop seam、phase switchをtest。
9. project file、sample license、composer/performer creditを保存。

リアルタイムStemは、固定mixで戦闘が完成し、音ズレ・復帰・途中参加の要件が明確になってから実装する。
