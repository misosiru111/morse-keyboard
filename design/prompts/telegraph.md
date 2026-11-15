# 電信局テーマ 画像生成プロンプト集

`design/ASSETS.md` の部品を、画像生成AIで作るためのプロンプトです。
プロンプト本文は英語です（どの画像生成AIも英語のほうが指示が通りやすいため）。説明は日本語です。

## 進め方

1. **最初に雰囲気見本を1枚作る**（下の「0. 雰囲気見本」）。気に入るまで何度か作り直す
2. 以降の部品は、その見本を**参照画像**として渡して作る。質感と色味がそろいます
   - ChatGPT / Gemini: 見本画像を添付して「この画像の質感と照明で」と頼む
   - Midjourney: `--sref <見本のURL>` を付ける
   - Stable Diffusion: IP-Adapter などで見本を参照
3. **点灯版は、消灯版の画像を編集して作る**（「同じ画像のまま電球だけ点けて」）。別々に生成すると位置や大きさがずれるため
4. できた画像は `design/raw/telegraph/` に、表の「保存名」で置く。背景の切り抜き、サイズ合わせ、グレースケール化、タイル化はこちらで処理します

## 画像生成AIでは作らないもの

次の部品は、生成した画像から計算で作るほうが、ずれがなく自然です。

- 影（`*_shadow`）: 部品の形をぼかして右下にずらして作る
- 照り返し（`light`）: 中心から薄れる光の円を計算で作る
- 線の点灯（`edge_*_on`）: 線の形から計算で作る
- 縦向きの線（`edge_v`）: 横向きの線を90°回して作る（棒は長さ方向に一様なので、光の向きも正しく保てる）

## 全部品に共通する決まり

各プロンプトの末尾に、次の共通指定を付けてください。

```text
Photorealistic product photograph, orthographic top-down view with the camera directly above, no perspective distortion. Single soft key light from the upper left at 45 degrees, gentle fill light, consistent with a warm 1900s telegraph office. Object centered and isolated, filling about 80% of the frame. Plain, uniform, seamless light-gray studio background with no texture, no props, and no cast shadow on the background. No text, no watermark, no logo. Ultra-detailed macro photography, sharp focus across the whole object, 8k.
```

除外したい要素（Stable Diffusion の negative prompt、Midjourney の `--no`）:

```text
perspective, tilted angle, isometric, cast shadow, drop shadow, background texture, table, hands, text, letters, watermark, logo, cartoon, illustration, 3d render look, plastic, blur, depth of field, cropped, out of frame
```

- **真上から**撮った写真にすること。斜めだと、ツリーに並べたときに浮いて見えます
- **影は描かせない**こと。影は後からそろえて付けます
- **光は左上から**。全部品でそろえないと立体感がちぐはぐになります

---

## 0. 雰囲気見本（素材としては使わない）

全体の方向性を決めるための1枚です。比率は 2:3（縦長）。

```text
A vertical control panel from a 1900s telegraph office, seen straight on. Dark hand-rubbed walnut board with deep grain and decades of patina. Mounted on it, a branching diagram of small round brass-rimmed glass indicator bulbs and short brass-framed glass tube lamps, connected by thin polished brass rods. A few bulbs glow warm amber like tungsten filaments, casting soft warm light onto the wood around them; the rest are dark amber glass. An engraved brass nameplate in the upper left, slotted brass screws in the corners. Warm, quiet, nostalgic atmosphere, faint dust, museum-quality photograph, soft light from the upper left, rich shadows, 8k.
```

ここで決まった質感（木の色、真鍮の色味、ガラスの色）を、以降の部品の基準にします。

---

## 1. 背景 — 保存名 `background.png`

比率 2:3（縦長）。**正面から**撮った木の板です（真上からと同じ扱い）。

```text
A flat panel of dark aged walnut wood, photographed straight on, filling the entire frame edge to edge. Vertical grain, two subtle plank seams, hand-rubbed oil finish with a soft satin sheen, faint scratches and decades of patina. Edges of the board slightly darker. Warm light from the upper left gently falling off toward the lower right. Nothing mounted on it. Photorealistic texture photograph, even focus, 8k.
```

- 共通指定のうち「Object centered and isolated…」「studio background」の部分は外してください（画面いっぱいの木が欲しいので）
- 木目が明るすぎると光る部品が目立たなくなります。暗めのものを選んでください

## 2. 銘板 — 保存名 `nameplate.png`

比率 3:1 か 16:9（横長）。

```text
A small rectangular engraved brass nameplate with rounded corners and a thin inner border line, a domed brass rivet at each end. The words "MORSE TELEGRAPH" engraved in a classic serif typeface, centered, with dark filled engraving. Aged brass with soft polish wear on the edges and a slightly darker patina in the recesses.
```

- 文字が崩れる場合は、文字の部分を `No text, the plate is blank.` に変えて無地の銘板を作ってください。刻印はこちらで入れます

## 3. ネジ — 保存名 `screw.png`

比率 1:1。

```text
A single slotted brass wood screw head seen from directly above, slightly domed, the slot at a 30 degree angle, fine machining marks, aged brass with a little darkening around the rim.
```

## 4. 短点（電球）消灯 — 保存名 `dit_off.png`

比率 1:1。

```text
A small round panel indicator lamp seen from directly above: a thick polished brass bezel ring with a beveled edge, holding a domed glass lens of dark amber glass. Through the glass, a faint coiled tungsten filament on two thin support wires is visible. A small bright window reflection on the upper left of the glass dome. Aged brass with subtle patina.
```

- 枠と光る部分の比率が大事です。ガラス部分が全体の直径の7割ほどのものを選んでください

## 5. 短点（電球）点灯 — 保存名 `dit_on.png`

**4 の画像を編集して**作ります。比率 1:1。

```text
The exact same image, same framing and position, but the lamp is switched on: the tungsten filament glows white-hot, the amber glass dome glows brightly from within with a warm incandescent light, and a soft halo of light blooms just beyond the brass bezel. Keep everything else identical.
```

- 消灯版との**差分**から光だけを取り出します。位置や大きさが変わっていたら作り直してください
- 色はアプリが状態ごとに付け直すので、電球の色はオレンジのままで構いません

## 6. 長点（ガラス管）横向き 消灯 — 保存名 `dahh_off.png`

比率 2:1（横長）。

```text
A horizontal oblong panel indicator lamp seen from directly above: a long rounded-rectangle polished brass bezel with a beveled edge, holding a cylindrical glass tube of dark amber glass lying horizontally. A thin straight tungsten filament runs along the length of the tube. A long thin highlight along the top edge of the glass. Aged brass with subtle patina.
```

## 7. 長点（ガラス管）横向き 点灯 — 保存名 `dahh_on.png`

**6 の画像を編集して**作ります。

```text
The exact same image, same framing and position, but the lamp is switched on: the filament glows white-hot along its length, the amber glass tube glows brightly from within with warm incandescent light, and a soft halo blooms around the brass bezel. Keep everything else identical.
```

## 8. 長点（ガラス管）縦向き 消灯 / 点灯 — 保存名 `dahv_off.png` / `dahv_on.png`

比率 1:2（縦長）。6 のプロンプトの `horizontal` を `vertical` に、`lying horizontally` を `standing vertically` に、`along the top edge` を `along the left edge` に置き換えて作ります。点灯版は 7 と同じく編集で作ります。

- 横向きの画像を90°回すと、光の当たり方まで回って不自然になるため、縦向きも別に作ります

## 9. 線（真鍮の棒） — 保存名 `edge.png`

比率 16:9（横長）。

```text
A long straight polished brass rod lying horizontally across the entire frame from edge to edge, round cross-section, about as thick as a pencil. Bright specular highlight running along its upper side, darker underside, fine longitudinal polishing marks, slightly aged.
```

- 継ぎ目なく繰り返せるように、まっすぐで太さが一定の部分を切り出して使います

## 10. 根元の飾り — 保存名 `root.png`

比率 3:4（縦長）。今のアンテナ記号（逆三角形の中にT）を真鍮で作る案です。

```text
A small emblem cut from polished brass bar stock, seen from directly above: an inverted triangle outline with a T shape inside it, and a single straight vertical stem extending down from the bottom point of the triangle, twice as long as the triangle is tall. The bars are round and polished with highlights on their upper-left sides. The inside of the triangle is filled with a dark walnut inlay.
```

- 形が崩れやすい部品です。うまくいかなければ、今のコード製のアンテナのままにします
- 別案として、本物の電鍵（ストレートキー）を真上から撮った写真にもできます。その場合は、取っ手の付け根が下端の中央に来ている構図にしてください

## 11. 数字の小札 — 保存名 `tag.png`

比率 2:1（横長）。数字と符号はアプリが刻印するので、**無地**で作ります。

```text
A small blank rectangular brass tag with slightly rounded corners, a small domed brass rivet at the middle of each short end. Aged brass with soft polish wear, faint scratches and a little darker patina at the edges. No engraving, no text, the plate is blank.
```

- 銘板（2）と並ぶので、銘板の画像を参照に渡して同じ真鍮の色味にしてください
- 10枚並ぶので、汚れや傷は控えめなものが向いています

## 12. 紙テープ — 保存名 `tape.png`

比率 16:9 か 21:9（横長）。

```text
A strip of aged cream-colored paper telegraph tape lying flat and straight across the frame, about one quarter of the frame tall, the ends cut cleanly at the left and right edges. Slightly yellowed, very fine paper fibers, faint creases, the edges a little darker. Nothing printed on it.
```

- 印字はアプリが行うので、何も書かれていない紙にします
- 左右いっぱいに伸ばして表示するので、端から端まで同じ調子の紙が向いています

## 13. 候補の文字を出すガラス窓 — 保存名 `window.png`

比率 1:1。

```text
A square indicator window seen from directly above: a thick polished brass bezel with rounded corners and a beveled edge, holding a flat pane of very dark smoky glass. A soft reflection on the upper left of the glass. Aged brass with subtle patina.
```

- ガラスは**暗いほど**中の光る文字が映えます
- 電球（4）と同じ真鍮の色味にしてください

---

## 生成したらチェックすること

- 真上から撮れているか（側面が見えていないか）
- 光が左上から当たっているか（ハイライトが左上、暗い側が右下）
- 背景が無地で、部品の影が背景に落ちていないか
- 点灯版が消灯版と同じ位置・同じ大きさか
- 部品同士の真鍮の色味がそろっているか
