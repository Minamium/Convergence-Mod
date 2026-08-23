# Art Direction

## Core statement

南極の巨大研究基地が、拘束していた超常存在の覚醒によって「工業設備から儀式装置へ読み替わる」瞬間を描く。機械Bossではなく、観測・封印・分解されていた存在を中心にする。

## Shape language

- Arena: 水平・垂直、巨大な柱、計測grid、反復する構造。
- Boss: 非対称、有機曲線、裂け目、浮遊部位、観測不能な空白。
- Seal: 円環、同心円、放射線、四方向anchor。
- Danger: 鋭い三角、断続線、収束するbeam。
- Safe/assigned: 明確なcircle、diamond、cross、数字・runeの組み合わせ。

## Palette

基調:

- ice white / pale cyan
- charcoal / black metal
- warning red
- oxidized dark gold

assignment markerは背景paletteから独立させる。色だけに依存せず、形状とanimation cadenceも変える。

## Scale hierarchy

1. Arena architecture: 画面外へ続く最大scale。
2. Boss silhouette: playerの20～40倍に見えるが、hit areaは読みやすく分割。
3. Breakable parts: 画面上で常に識別可能。
4. Telegraph: gameplay上の最優先layer。
5. Decoration: telegraphより暗く、低contrast。

## Boss construction proposal

- central body / void
- Crown part
- left/right Wingsまたはmembrane assemblies
- Heart Casing
- exposed Core
- separate emissive masks
- code-driven appendages and primitive trails

部位は別NPC/描画componentとして扱える構造にし、sprite designもhitboxと破壊状態に対応させる。

## UI and typography

- phase titleは短い英語名 + localization。
- countdownは遠距離から読める太い数字。
- Stack、Spread、Baitは固有shapeを固定。
- warning textは装飾より情報階層を優先。
- 大字幕中もplayer、marker、safe areaを隠さない。

## Forbidden references

- EVA機体に似た頭部、顎、肩、拘束具の組合せ
- NERVに似たleaf/logo/seal
- 使徒の顔・core・maskの直接再現
- 映画の構図、字幕文言、固有名詞、camera timingの再現
- Calamity既存Bossのsprite/texture/particleの抽出・加工
- 特定の存命artist名をstyle promptとして指定

## Production checklist

- concept 3～5案
- silhouette-only review
- 100% scale readability review
- parts-separated key art
- grayscale telegraph test
- color-vision test
- 2/3/4-player marker overlap test
- Reduced/Minimal VFX mode
- provenance entry before asset merge
