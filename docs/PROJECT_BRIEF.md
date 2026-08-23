# Project Brief

## One-line concept

Shadowspec級装備の2～4人パーティーが、南極の終末研究基地に展開された固定Raidフィールドで、世界規模の収束現象を止める最終決戦。

## Product position

本Addonは、Calamity終盤の個人回避と火力最適化を土台に、MMORPG Raid型の協力ギミックをTerrariaの2D移動へ翻訳する。単純に弾幕密度とHPを増やしたSuperbossにはしない。

想定プレイヤーは次の条件を満たす。

- Exo MechsおよびSupreme Calamitas撃破済み
- Shadowspec級装備を利用可能
- 2～4人の固定または準固定パーティー
- 5～12分程度の反復攻略を受け入れられる

Soloは初期リリースの対象外とする。将来対応する場合は、協力ギミックを削るだけでなく独立した個人Superbossとして再設計する。

最初のRaidは基盤の実証対象であり、Addon全体の設計境界ではない。長期的には独立Boss、追加Raid、World content、進行要素、Item、Utility、演出基盤まで拡張し、大型Content Mod級の規模を目指す。共通基盤へThird Severance固有の名称やルールを持ち込まず、逆に実利用が一つしかない機能を早期に過剰共通化しない。

## Design pillars

1. **Execution** — 回避、ダッシュ、位置取り、火力維持。
2. **Coordination** — 頭割り、散開、ペア処理、誘導、同時攻撃。
3. **Optimization** — 複数DPSチェック、Burst Window、装備とクラス構成。
4. **Strategy** — 部位破壊による後続ギミックの選択的弱体化。
5. **Adaptation** — 対象指定、人数別役割、失敗・死亡・離脱後の立て直し。

## Player experience goals

- 死因と改善方法を戦闘中または直後に理解できる。
- 一人の軽微なミスはSoft Failureとなり、回復可能な不利へ変換される。
- 全員生存と正確な連携は、より短い戦闘時間と完成した演出で報われる。
- クラス構成は攻略方法を変えるが、特定クラスを必須にはしない。
- 部位破壊の選択により、同じボスに複数の攻略ルートが生まれる。

## Setting and presentation

舞台は仮称 `Erebus Polar Citadel`。氷床下の巨大工業基地で、観測・拘束されていた超常存在 `The Choir Beneath the Ice` が起動する。

視覚言語は、氷、暗い金属、白、赤、黒、巨大な円環、封印柱、観測装置、無機質な警告表示を中心とする。生物的・宗教的形状と工業設備を融合するが、既存作品の機体、ロゴ、顔、固有用語、構図を再現しない。

## Music direction

クラシックの構造感と終末的な合唱・管弦楽を用い、フェーズごとの音楽的役割を明確にする。ベートーヴェン第九を素材にする場合も、保護期間が満了した原曲そのものから新規に編曲・打ち込み・録音する。既存の録音、映画音源、現代編曲、既存MIDIを流用しない。

初期実装はリアルタイム作曲を行わず、フェーズ別の完成ミックスと短いTransition/Stingerを切り替える。Stem同期は、戦闘同期が安定した後に再評価する。

## Reward direction

Shadowspecを単純に数値で超えるTierは作らない。報酬は特殊挙動、Party Mark、部位破壊補助、攻撃履歴の模倣、支援Utilityなど、最終装備環境へ新しいビルドと遊び方を追加する。

## Initial scope

最初の実装範囲はMilestone 0とMilestone 1のみ。

- 正確な互換バージョンの固定
- 最小Modのクライアント／Dedicated Serverビルド
- Polar Foundation Core
- Arena validation
- 2～4人のJoin／Ready
- Tileを大量生成しない境界Barrier
- 単一Raid制約とFight ID
- 中断、Core破壊、World unload時の完全Cleanup
- modular source boundary、protocol guard、repository policy、asset provenance

頭割り、Spread、Boss Dummy、Downed／Reviveは、この土台が安定した後のVertical Sliceへ含める。

## Non-goals for the first implementation

- 完成版Boss AI、報酬、楽曲、Shader、Sprite
- SubworldまたはDimension
- Soloバランス
- 複数Arenaの同時進行
- リアルタイム音楽合成
- Calamity内部実装のコピーまたは再配布

## Definition of a viable foundation

Milestone 1は、2～4人とDedicated Serverで同一のArena状態が見え、Core起動、Ready、Barrier、キャンセル、Core破壊、切断、World unloadのどの経路でも一時状態が残らないときに完了する。
