# 全種族のデフォルト魔法

配合時、各魔法スロットが独立に1%の確率で子の種族・同スロットのデフォルト魔法に突然変異します。2本の染色体を標準のものに置き換え、変異履歴に記録します。使用不可のスロットにも変異が発生し、遺伝情報として残ります。確率は `SpellSettings` の `speciesDefaultMutationChance` で変更できます。

種族マスターに保存したGP魔法です。内部5枠のうちランクに対応する1～5枠を敵が使用し、捕獲後も引き継ぎます。既存の所持個体・配合結果は変更しません。

| 種族ID | 種族 | 系統 | ランク | 使用する魔法 |
|---|---|---|---|---|
|demo_00|スライム|Slime|F|水流の呪縛弾|
|demo_01|牙ウサギ|Beast|E|風刃の吸命弾|
|demo_02|青羽フクロウ|Bird|D|冥闇の波動弾 / 冥闇の呪縛波動弾|
|demo_03|真珠ヤシガニ|Crustacean|C|天光の祝福陣 / 天光の呪縛弾|
|demo_04|沼ぬしガエル|Amphibian|D|岩石の呪縛波動弾 / 岩石の呪縛波動弾|
|demo_05|真鍮フクロウ|Machine|C|天光弾 / 天光の呪縛弾|
|demo_06|雷雲の子|Spirit|B|風刃の追尾弾 / 風刃の呪縛追尾弾 / 風刃の癒し追尾陣|
|demo_07|歌う花樹|Plant|C|天光の呪縛陣 / 天光の呪縛弾|
|demo_08|黒翼ガーゴイル|Demon|B|冥闇の吸命弾 / 冥闇の呪縛弾 / 冥闇の癒し陣|
|demo_09|黄金のグリフォン竜|Dragon|A|火炎の爆裂呪縛弾 / 火炎の呪縛弾 / 火炎の祝福陣 / 火炎の分裂呪縛弾|
|demo_10|糸吐きクモ|Insect|E|冥闇の呪縛弾|
|demo_11|こもれび茸|Fungi|F|岩石の呪縛陣|
|species_F00_R0_B|アワスライム|Slime|F|水流の呪縛波動弾|
|species_F00_R1_A|しずくゼリー|Slime|E|水流の呪縛弾|
|species_F00_R1_B|ぬまぷる|Slime|E|水流の呪縛波動弾|
|species_F00_R2_A|ミズモチ|Slime|D|水流の呪縛弾 / 水流の呪縛弾|
|species_F00_R2_B|べたべた|Slime|D|水流の呪縛波動弾 / 水流の呪縛波動弾|
|species_F00_R3_A|雨つぶ|Slime|C|水流の呪縛弾 / 水流の呪縛弾|
|species_F00_R3_B|泡まじん|Slime|C|風刃の呪縛波動弾 / 風刃の呪縛波動弾|
|species_F00_R4_A|瑠璃ゼリー|Slime|B|水流の呪縛弾 / 水流の呪縛弾 / 水流の癒し陣|
|species_F00_R4_B|琥珀ゼリー|Slime|B|岩石の呪縛波動弾 / 岩石の呪縛波動弾 / 岩石の癒し波動陣|
|species_F00_R5_A|霧まる|Slime|A|水流の呪縛弾 / 水流の呪縛弾 / 水流の癒し陣 / 水流の分裂呪縛弾|
|species_F00_R5_B|水鏡ゼリー|Slime|A|水流の呪縛波動弾 / 水流の呪縛波動弾 / 水流の癒し波動陣 / 水流の分裂呪縛弾|
|species_F00_R6_A|雲わたゼリー|Slime|S|水流の呪縛弾 / 水流の呪縛弾 / 水流の癒し陣 / 水流の分裂呪縛弾 / 水流の爆裂呪縛弾|
|species_F00_R6_B|星雫ゼリー|Slime|S|天光の呪縛波動弾 / 天光の呪縛波動弾 / 天光の癒し波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F01_R0_A|森犬|Beast|F|風刃の吸命弾|
|species_F01_R0_B|野ねずみ|Beast|F|風刃の吸命波動弾|
|species_F01_R1_B|山荒らし|Beast|E|岩石の吸命波動弾|
|species_F01_R2_A|赤尾オオカミ|Beast|D|風刃の吸命弾 / 風刃の呪縛弾|
|species_F01_R2_B|苔ツノ|Beast|D|風刃の吸命波動弾 / 風刃の呪縛波動弾|
|species_F01_R3_A|黒斑ヒョウ|Beast|C|冥闇の吸命弾 / 冥闇の呪縛弾|
|species_F01_R3_B|岩駆け|Beast|C|岩石の吸命波動弾 / 岩石の呪縛波動弾|
|species_F01_R4_A|月輪グマ|Beast|B|天光の吸命弾 / 天光の呪縛弾 / 天光の祝福陣|
|species_F01_R4_B|火喰いテン|Beast|B|火炎の吸命波動弾 / 火炎の呪縛波動弾 / 火炎の祝福波動陣|
|species_F01_R5_A|白銀ライガー|Beast|A|水流の吸命弾 / 水流の呪縛弾 / 水流の祝福陣 / 水流の分裂呪縛弾|
|species_F01_R5_B|雷角バイソン|Beast|A|風刃の吸命波動弾 / 風刃の呪縛波動弾 / 風刃の祝福波動陣 / 風刃の分裂呪縛弾|
|species_F01_R6_A|天翔ける獅子|Beast|S|風刃の吸命弾 / 風刃の呪縛弾 / 風刃の祝福陣 / 風刃の分裂呪縛弾 / 風刃の爆裂呪縛弾|
|species_F01_R6_B|古森の王獣|Beast|S|岩石の吸命波動弾 / 岩石の呪縛波動弾 / 岩石の祝福波動陣 / 岩石の分裂呪縛弾 / 岩石の爆裂呪縛弾|
|species_F02_R0_A|こだま鳥|Bird|F|風刃の波動弾|
|species_F02_R0_B|野原スズメ|Bird|F|風刃の波動弾|
|species_F02_R1_A|風切りツバメ|Bird|E|風刃の波動弾|
|species_F02_R1_B|火羽ガラス|Bird|E|火炎の波動弾|
|species_F02_R2_B|砂漠ヒバリ|Bird|D|岩石の波動弾 / 岩石の呪縛波動弾|
|species_F02_R3_A|夜渡りコウモリ|Bird|C|冥闇の波動弾 / 冥闇の呪縛波動弾|
|species_F02_R3_B|雷鳴ワシ|Bird|C|風刃の波動弾 / 風刃の呪縛波動弾|
|species_F02_R4_A|雪冠ツル|Bird|B|水流の波動弾 / 水流の呪縛波動弾 / 水流の癒し波動陣|
|species_F02_R4_B|紅蓮クジャク|Bird|B|風刃の波動弾 / 風刃の呪縛波動弾 / 風刃の癒し波動陣|
|species_F02_R5_A|月読ミミズク|Bird|A|風刃の波動弾 / 風刃の呪縛波動弾 / 風刃の癒し波動陣 / 風刃の分裂呪縛弾|
|species_F02_R5_B|嵐喰いグリフ|Bird|A|風刃の波動弾 / 風刃の呪縛波動弾 / 風刃の癒し波動陣 / 風刃の分裂呪縛弾|
|species_F02_R6_A|暁の鳳鳥|Bird|S|風刃の波動弾 / 風刃の呪縛波動弾 / 風刃の癒し波動陣 / 風刃の分裂呪縛弾 / 風刃の爆裂呪縛弾|
|species_F02_R6_B|天穹の大鷲|Bird|S|風刃の波動弾 / 風刃の呪縛波動弾 / 風刃の癒し波動陣 / 風刃の分裂呪縛弾 / 風刃の爆裂呪縛弾|
|species_F03_R0_A|石トカゲ|Reptile|F|岩石の呪縛弾|
|species_F03_R0_B|草尾ヘビ|Reptile|F|風刃の呪縛波動弾|
|species_F03_R1_A|砂潜りヤモリ|Reptile|E|岩石の呪縛弾|
|species_F03_R1_B|沼ワニ|Reptile|E|岩石の呪縛波動弾|
|species_F03_R2_A|岩背トカゲ|Reptile|D|岩石の呪縛弾 / 岩石の呪縛弾|
|species_F03_R2_B|毒牙マムシ|Reptile|D|岩石の呪縛波動弾 / 岩石の呪縛波動弾|
|species_F03_R3_A|青鱗バシリスク|Reptile|C|岩石の呪縛弾 / 岩石の呪縛弾|
|species_F03_R3_B|火口サラマンダー|Reptile|C|岩石の呪縛波動弾 / 岩石の呪縛波動弾|
|species_F03_R4_A|鎧首カメレオン|Reptile|B|岩石の呪縛弾 / 岩石の呪縛弾 / 岩石の祝福陣|
|species_F03_R4_B|黒曜コブラ|Reptile|B|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の祝福波動陣|
|species_F03_R5_A|霜背ワニ|Reptile|A|水流の呪縛弾 / 水流の呪縛弾 / 水流の祝福陣 / 水流の分裂呪縛弾|
|species_F03_R5_B|雷鱗ドレイク|Reptile|A|岩石の呪縛波動弾 / 岩石の呪縛波動弾 / 岩石の祝福波動陣 / 岩石の分裂呪縛弾|
|species_F03_R6_A|古鱗の蛇王|Reptile|S|岩石の呪縛弾 / 岩石の呪縛弾 / 岩石の祝福陣 / 岩石の分裂呪縛弾 / 岩石の爆裂呪縛弾|
|species_F03_R6_B|天裂き大蛇|Reptile|S|風刃の呪縛波動弾 / 風刃の呪縛波動弾 / 風刃の祝福波動陣 / 風刃の分裂呪縛弾 / 風刃の爆裂呪縛弾|
|species_F04_R0_A|雨蛙|Amphibian|F|水流の呪縛波動弾|
|species_F04_R0_B|おたま坊|Amphibian|F|水流の呪縛波動弾|
|species_F04_R1_A|苔ヒキガエル|Amphibian|E|岩石の呪縛波動弾|
|species_F04_R1_B|水辺イモリ|Amphibian|E|水流の呪縛波動弾|
|species_F04_R2_B|泡吹きウシガエル|Amphibian|D|水流の呪縛波動弾 / 水流の呪縛波動弾|
|species_F04_R3_A|青斑アホロートル|Amphibian|C|水流の呪縛波動弾 / 水流の呪縛波動弾|
|species_F04_R3_B|毒沼ヤドクガエル|Amphibian|C|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾|
|species_F04_R4_A|月影オオサンショウ|Amphibian|B|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣|
|species_F04_R4_B|岩砕きガマ|Amphibian|B|水流の呪縛波動弾 / 水流の呪縛波動弾 / 水流の癒し波動陣|
|species_F04_R5_A|霧笛カエル|Amphibian|A|風刃の呪縛波動弾 / 風刃の呪縛波動弾 / 風刃の癒し波動陣 / 風刃の分裂呪縛弾|
|species_F04_R5_B|氷沼イモリ|Amphibian|A|水流の呪縛波動弾 / 水流の呪縛波動弾 / 水流の癒し波動陣 / 水流の分裂呪縛弾|
|species_F04_R6_A|古池の主|Amphibian|S|水流の呪縛波動弾 / 水流の呪縛波動弾 / 水流の癒し波動陣 / 水流の分裂呪縛弾 / 水流の爆裂呪縛弾|
|species_F04_R6_B|天雨の大蛙|Amphibian|S|水流の呪縛波動弾 / 水流の呪縛波動弾 / 水流の癒し波動陣 / 水流の分裂呪縛弾 / 水流の爆裂呪縛弾|
|species_F05_R0_A|小川ハゼ|Aquatic|F|水流の呪縛追尾弾|
|species_F05_R0_B|浜辺イワシ|Aquatic|F|水流の呪縛追尾弾|
|species_F05_R1_A|泡ふぐ|Aquatic|E|水流の呪縛追尾弾|
|species_F05_R1_B|珊瑚タイ|Aquatic|E|水流の呪縛追尾弾|
|species_F05_R2_A|深海アンコウ|Aquatic|D|冥闇の呪縛追尾弾 / 冥闇の呪縛追尾弾|
|species_F05_R2_B|潮吹きクジラ|Aquatic|D|水流の呪縛追尾弾 / 水流の呪縛追尾弾|
|species_F05_R3_A|青鋼サメ|Aquatic|C|水流の呪縛追尾弾 / 水流の呪縛追尾弾|
|species_F05_R3_B|沼底ナマズ|Aquatic|C|水流の呪縛追尾弾 / 水流の呪縛追尾弾|
|species_F05_R4_A|月光クラゲ|Aquatic|B|水流の呪縛追尾弾 / 水流の呪縛追尾弾 / 水流の祝福追尾陣|
|species_F05_R4_B|火山うなぎ|Aquatic|B|火炎の呪縛追尾弾 / 火炎の呪縛追尾弾 / 火炎の祝福追尾陣|
|species_F05_R5_A|渦潮マンタ|Aquatic|A|水流の呪縛追尾弾 / 水流の呪縛追尾弾 / 水流の祝福追尾陣 / 水流の分裂呪縛弾|
|species_F05_R5_B|氷海シーラカンス|Aquatic|A|水流の呪縛追尾弾 / 水流の呪縛追尾弾 / 水流の祝福追尾陣 / 水流の分裂呪縛弾|
|species_F05_R6_A|海鳴りの主|Aquatic|S|水流の呪縛追尾弾 / 水流の呪縛追尾弾 / 水流の祝福追尾陣 / 水流の分裂呪縛弾 / 水流の爆裂呪縛弾|
|species_F05_R6_B|蒼海リヴァイア|Aquatic|S|水流の呪縛追尾弾 / 水流の呪縛追尾弾 / 水流の祝福追尾陣 / 水流の分裂呪縛弾 / 水流の爆裂呪縛弾|
|species_F06_R0_A|こがねバチ|Insect|F|風刃の呪縛弾|
|species_F06_R0_B|葉っぱテントウ|Insect|F|岩石の呪縛波動弾|
|species_F06_R1_B|夜蛍|Insect|E|風刃の呪縛波動弾|
|species_F06_R2_A|鎌角カマキリ|Insect|D|風刃の呪縛弾 / 風刃の呪縛弾|
|species_F06_R2_B|毒針サソリ|Insect|D|風刃の呪縛波動弾 / 風刃の呪縛波動弾|
|species_F06_R3_A|花粉モス|Insect|C|風刃の呪縛弾 / 風刃の呪縛弾|
|species_F06_R3_B|甲冑カブト|Insect|C|風刃の呪縛波動弾 / 風刃の呪縛波動弾|
|species_F06_R4_A|紅蓮アゲハ|Insect|B|風刃の呪縛弾 / 風刃の呪縛弾 / 風刃の癒し陣|
|species_F06_R4_B|鏡翅トンボ|Insect|B|水流の呪縛波動弾 / 水流の呪縛波動弾 / 水流の癒し波動陣|
|species_F06_R5_A|墓守コガネ|Insect|A|風刃の呪縛弾 / 風刃の呪縛弾 / 風刃の癒し陣 / 風刃の分裂呪縛弾|
|species_F06_R5_B|雷角クワガタ|Insect|A|風刃の呪縛波動弾 / 風刃の呪縛波動弾 / 風刃の癒し波動陣 / 風刃の分裂呪縛弾|
|species_F06_R6_A|女王スズメバチ|Insect|S|風刃の呪縛弾 / 風刃の呪縛弾 / 風刃の癒し陣 / 風刃の分裂呪縛弾 / 風刃の爆裂呪縛弾|
|species_F06_R6_B|千眼の古虫|Insect|S|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F07_R0_A|磯ガニ|Crustacean|F|水流の祝福陣|
|species_F07_R0_B|砂浜ヤドカリ|Crustacean|F|岩石の祝福波動陣|
|species_F07_R1_A|泡吹きシャコ|Crustacean|E|水流の祝福陣|
|species_F07_R1_B|石背ザリガニ|Crustacean|E|水流の祝福波動陣|
|species_F07_R2_A|赤爪ガザミ|Crustacean|D|火炎の祝福陣 / 火炎の呪縛弾|
|species_F07_R2_B|岩砕きタラバ|Crustacean|D|水流の祝福波動陣 / 水流の呪縛波動弾|
|species_F07_R3_B|深淵ロブスター|Crustacean|C|水流の祝福波動陣 / 水流の呪縛波動弾|
|species_F07_R4_A|火山ダンゴムシ|Crustacean|B|水流の祝福陣 / 水流の呪縛弾 / 水流の祝福陣|
|species_F07_R4_B|瑠璃ザリガニ|Crustacean|B|水流の祝福波動陣 / 水流の呪縛波動弾 / 水流の祝福波動陣|
|species_F07_R5_A|双鋏シオマネキ|Crustacean|A|水流の祝福陣 / 水流の呪縛弾 / 水流の祝福陣 / 水流の分裂呪縛弾|
|species_F07_R5_B|氷殻キングクラブ|Crustacean|A|水流の祝福波動陣 / 水流の呪縛波動弾 / 水流の祝福波動陣 / 水流の分裂呪縛弾|
|species_F07_R6_A|鉄甲の大蟹|Crustacean|S|水流の祝福陣 / 水流の呪縛弾 / 水流の祝福陣 / 水流の分裂呪縛弾 / 水流の爆裂呪縛弾|
|species_F07_R6_B|海底城の守り手|Crustacean|S|水流の祝福波動陣 / 水流の呪縛波動弾 / 水流の祝福波動陣 / 水流の分裂呪縛弾 / 水流の爆裂呪縛弾|
|species_F08_R0_A|わかばトゲ|Plant|F|岩石の呪縛陣|
|species_F08_R0_B|つる草うさぎ|Plant|F|風刃の呪縛波動陣|
|species_F08_R1_A|花かぶり|Plant|E|天光の癒し陣|
|species_F08_R1_B|眠りキノコ草|Plant|E|岩石の呪縛波動陣|
|species_F08_R2_A|人喰いアザミ|Plant|D|冥闇の呪縛陣 / 冥闇の呪縛弾|
|species_F08_R2_B|蔦巻きローパー|Plant|D|岩石の呪縛波動陣 / 岩石の呪縛波動弾|
|species_F08_R3_B|火花サボテン|Plant|C|岩石の呪縛波動陣 / 岩石の呪縛波動弾|
|species_F08_R4_A|白樺ドライアド|Plant|B|岩石の呪縛陣 / 岩石の呪縛弾 / 岩石の癒し陣|
|species_F08_R4_B|毒蜜ラフレシア|Plant|B|冥闇の呪縛波動陣 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣|
|species_F08_R5_A|月光ヒイラギ|Plant|A|岩石の呪縛陣 / 岩石の呪縛弾 / 岩石の癒し陣 / 岩石の分裂呪縛弾|
|species_F08_R5_B|絡み森の大樹|Plant|A|岩石の呪縛波動陣 / 岩石の呪縛波動弾 / 岩石の癒し波動陣 / 岩石の分裂呪縛弾|
|species_F08_R6_A|花冠マンドレイク|Plant|S|岩石の呪縛陣 / 岩石の呪縛弾 / 岩石の癒し陣 / 岩石の分裂呪縛弾 / 岩石の爆裂呪縛弾|
|species_F08_R6_B|世界樹の若枝|Plant|S|天光の呪縛波動陣 / 天光の呪縛波動弾 / 天光の癒し波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F09_R0_B|胞子ぼうや|Fungi|F|岩石の呪縛波動陣|
|species_F09_R1_A|眠りマッシュ|Fungi|E|冥闇の呪縛陣|
|species_F09_R1_B|沼地シメジ|Fungi|E|岩石の呪縛波動陣|
|species_F09_R2_A|毒傘タマゴタケ|Fungi|D|冥闇の呪縛陣 / 冥闇の呪縛弾|
|species_F09_R2_B|笑いエリンギ|Fungi|D|岩石の呪縛波動陣 / 岩石の呪縛波動弾|
|species_F09_R3_A|青苔ヒラタケ|Fungi|C|水流の呪縛陣 / 水流の呪縛弾|
|species_F09_R3_B|夜光キクラゲ|Fungi|C|岩石の呪縛波動陣 / 岩石の呪縛波動弾|
|species_F09_R4_A|火吹きベニテング|Fungi|B|岩石の呪縛陣 / 岩石の呪縛弾 / 岩石の祝福陣|
|species_F09_R4_B|石茸ガンコ|Fungi|B|岩石の呪縛波動陣 / 岩石の呪縛波動弾 / 岩石の祝福波動陣|
|species_F09_R5_A|夢見ポルチーニ|Fungi|A|岩石の呪縛陣 / 岩石の呪縛弾 / 岩石の祝福陣 / 岩石の分裂呪縛弾|
|species_F09_R5_B|腐海のヌメリタケ|Fungi|A|冥闇の呪縛波動陣 / 冥闇の呪縛波動弾 / 冥闇の祝福波動陣 / 冥闇の分裂呪縛弾|
|species_F09_R6_A|森守りの老茸|Fungi|S|岩石の呪縛陣 / 岩石の呪縛弾 / 岩石の祝福陣 / 岩石の分裂呪縛弾 / 岩石の爆裂呪縛弾|
|species_F09_R6_B|星降る冬虫夏草|Fungi|S|天光の呪縛波動陣 / 天光の呪縛波動弾 / 天光の祝福波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F10_R0_A|森番の子|DemiHuman|F|岩石弾|
|species_F10_R0_B|石笛の民|DemiHuman|F|風刃の波動弾|
|species_F10_R1_A|川辺の狩人|DemiHuman|E|水流弾|
|species_F10_R1_B|野鍛冶|DemiHuman|E|火炎の波動弾|
|species_F10_R2_A|角笛の戦士|DemiHuman|D|風刃弾 / 風刃の呪縛弾|
|species_F10_R2_B|霧隠れの斥候|DemiHuman|D|火炎の波動弾 / 火炎の呪縛波動弾|
|species_F10_R3_A|鉄靴の山兵|DemiHuman|C|岩石弾 / 岩石の呪縛弾|
|species_F10_R3_B|紅蓮の舞い手|DemiHuman|C|火炎の波動弾 / 火炎の呪縛波動弾|
|species_F10_R4_A|月下の剣士|DemiHuman|B|火炎弾 / 火炎の呪縛弾 / 火炎の癒し陣|
|species_F10_R4_B|鷹目の弓手|DemiHuman|B|風刃の波動弾 / 風刃の呪縛波動弾 / 風刃の癒し波動陣|
|species_F10_R5_A|白樺の賢者|DemiHuman|A|火炎弾 / 火炎の呪縛弾 / 火炎の癒し陣 / 火炎の分裂呪縛弾|
|species_F10_R5_B|雷槌の豪傑|DemiHuman|A|風刃の波動弾 / 風刃の呪縛波動弾 / 風刃の癒し波動陣 / 風刃の分裂呪縛弾|
|species_F10_R6_A|古王の近衛|DemiHuman|S|火炎弾 / 火炎の呪縛弾 / 火炎の癒し陣 / 火炎の分裂呪縛弾 / 火炎の爆裂呪縛弾|
|species_F10_R6_B|黎明の勇者|DemiHuman|S|天光の波動弾 / 天光の呪縛波動弾 / 天光の癒し波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F11_R0_A|岩こびと|Giant|F|岩石の陣|
|species_F11_R0_B|丘の巨人|Giant|F|岩石の波動陣|
|species_F11_R1_A|苔岩ゴーレム|Giant|E|岩石の陣|
|species_F11_R1_B|雪原トロル|Giant|E|岩石の波動陣|
|species_F11_R2_A|山砕きオーガ|Giant|D|岩石の陣 / 岩石弾|
|species_F11_R2_B|雷鳴の巨兵|Giant|D|岩石の波動陣 / 岩石の波動弾|
|species_F11_R3_A|霧峰の大男|Giant|C|風刃の陣 / 風刃弾|
|species_F11_R3_B|鉄骨ジャイアント|Giant|C|岩石の波動陣 / 岩石の波動弾|
|species_F11_R4_A|溶岩タイタン|Giant|B|岩石の陣 / 岩石弾 / 岩石の祝福陣|
|species_F11_R4_B|氷壁の守人|Giant|B|水流の波動陣 / 水流の波動弾 / 水流の祝福波動陣|
|species_F11_R5_A|深森の古巨人|Giant|A|岩石の陣 / 岩石弾 / 岩石の祝福陣 / 岩石の分裂呪縛弾|
|species_F11_R5_B|星喰い巨人|Giant|A|冥闇の波動陣 / 冥闇の波動弾 / 冥闇の祝福波動陣 / 冥闇の分裂呪縛弾|
|species_F11_R6_A|大地を担う者|Giant|S|岩石の陣 / 岩石弾 / 岩石の祝福陣 / 岩石の分裂呪縛弾 / 岩石の爆裂呪縛弾|
|species_F11_R6_B|天蓋を支える巨人|Giant|S|天光の波動陣 / 天光の波動弾 / 天光の祝福波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F12_R0_A|土くれ人形|Material|F|岩石の周回弾|
|species_F12_R0_B|藁束ドール|Material|F|風刃の周回弾|
|species_F12_R1_A|石臼ゴーレム|Material|E|岩石の周回弾|
|species_F12_R1_B|泥人形|Material|E|岩石の周回弾|
|species_F12_R2_A|砂時計兵|Material|D|岩石の周回弾 / 岩石弾|
|species_F12_R2_B|磁鉄ゴーレム|Material|D|岩石の周回弾 / 岩石の波動弾|
|species_F12_R3_A|青銅の番兵|Material|C|風刃の周回弾 / 風刃弾|
|species_F12_R3_B|陶器の踊り子|Material|C|岩石の周回弾 / 岩石の波動弾|
|species_F12_R4_A|黒曜の騎士像|Material|B|岩石の周回弾 / 岩石弾 / 岩石の癒し陣|
|species_F12_R4_B|琥珀の守り人|Material|B|岩石の周回弾 / 岩石の波動弾 / 岩石の癒し波動陣|
|species_F12_R5_A|白磁の女神像|Material|A|岩石の周回弾 / 岩石弾 / 岩石の癒し陣 / 岩石の分裂呪縛弾|
|species_F12_R5_B|千年石像|Material|A|岩石の周回弾 / 岩石の波動弾 / 岩石の癒し波動陣 / 岩石の分裂呪縛弾|
|species_F12_R6_A|古代遺跡の番人|Material|S|岩石の周回弾 / 岩石弾 / 岩石の癒し陣 / 岩石の分裂呪縛弾 / 岩石の爆裂呪縛弾|
|species_F12_R6_B|星鋼の巨像|Material|S|天光の周回弾 / 天光の波動弾 / 天光の癒し波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F13_R0_A|小石ころ|Mineral|F|岩石の分裂呪縛弾|
|species_F13_R0_B|砂金モグラ|Mineral|F|岩石の分裂呪縛弾|
|species_F13_R1_A|水晶ヤモリ|Mineral|E|天光群星|
|species_F13_R1_B|瑪瑙トカゲ|Mineral|E|岩石の分裂呪縛弾|
|species_F13_R2_A|翡翠カメ|Mineral|D|水流の分裂呪縛弾 / 水流の呪縛弾|
|species_F13_R2_B|紅玉コウモリ|Mineral|D|岩石の分裂呪縛弾 / 岩石の呪縛波動弾|
|species_F13_R3_A|黒曜オオカミ|Mineral|C|冥闇の分裂呪縛弾 / 冥闇の呪縛弾|
|species_F13_R3_B|琥珀ムカデ|Mineral|C|岩石の分裂呪縛弾 / 岩石の呪縛波動弾|
|species_F13_R4_A|蒼玉フクロウ|Mineral|B|岩石の分裂呪縛弾 / 岩石の呪縛弾 / 岩石の祝福陣|
|species_F13_R4_B|銀晶カニ|Mineral|B|水流の分裂呪縛弾 / 水流の呪縛波動弾 / 水流の祝福波動陣|
|species_F13_R5_A|黄玉ライオン|Mineral|A|岩石の分裂呪縛弾 / 岩石の呪縛弾 / 岩石の祝福陣 / 岩石の分裂呪縛弾|
|species_F13_R5_B|星鉄の獣|Mineral|A|岩石の分裂呪縛弾 / 岩石の呪縛波動弾 / 岩石の祝福波動陣 / 岩石の分裂呪縛弾|
|species_F13_R6_A|虹晶ドラゴン|Mineral|S|岩石の分裂呪縛弾 / 岩石の呪縛弾 / 岩石の祝福陣 / 岩石の分裂呪縛弾 / 岩石の爆裂呪縛弾|
|species_F13_R6_B|天晶の守護獣|Mineral|S|天光群星 / 天光の呪縛波動弾 / 天光の祝福波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F14_R0_A|歯車ネズミ|Machine|F|風刃弾|
|species_F14_R0_B|ブリキ兵|Machine|F|岩石の波動弾|
|species_F14_R1_A|からくり犬|Machine|E|火炎弾|
|species_F14_R1_B|蒸気バッタ|Machine|E|風刃の波動弾|
|species_F14_R2_A|鋼鉄カラス|Machine|D|風刃弾 / 風刃の呪縛弾|
|species_F14_R2_B|ゼンマイ兵|Machine|D|風刃の波動弾 / 風刃の呪縛波動弾|
|species_F14_R3_B|磁気ゴーレム|Machine|C|風刃の波動弾 / 風刃の呪縛波動弾|
|species_F14_R4_A|雷管タンク|Machine|B|風刃弾 / 風刃の呪縛弾 / 風刃の癒し陣|
|species_F14_R4_B|自走大砲|Machine|B|火炎の波動弾 / 火炎の呪縛波動弾 / 火炎の癒し波動陣|
|species_F14_R5_A|白銀オートマタ|Machine|A|風刃弾 / 風刃の呪縛弾 / 風刃の癒し陣 / 風刃の分裂呪縛弾|
|species_F14_R5_B|黒鉄の騎士|Machine|A|冥闇の波動弾 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣 / 冥闇の分裂呪縛弾|
|species_F14_R6_A|古代機関兵|Machine|S|風刃弾 / 風刃の呪縛弾 / 風刃の癒し陣 / 風刃の分裂呪縛弾 / 風刃の爆裂呪縛弾|
|species_F14_R6_B|星渡り機巧竜|Machine|S|天光の波動弾 / 天光の呪縛波動弾 / 天光の癒し波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F15_R0_A|ねむり人形|ArtificialLife|F|冥闇の吸命弾|
|species_F15_R0_B|つぎはぎ君|ArtificialLife|F|岩石の吸命波動弾|
|species_F15_R1_A|培養スライム|ArtificialLife|E|水流の吸命弾|
|species_F15_R1_B|うつろな人形|ArtificialLife|E|冥闇の吸命波動弾|
|species_F15_R2_A|合成獣キマイラ|ArtificialLife|D|火炎の吸命弾 / 火炎の呪縛弾|
|species_F15_R2_B|硝子の子|ArtificialLife|D|冥闇の吸命波動弾 / 冥闇の呪縛波動弾|
|species_F15_R3_A|縫い目の巨人|ArtificialLife|C|岩石の吸命弾 / 岩石の呪縛弾|
|species_F15_R3_B|培養槽の番人|ArtificialLife|C|冥闇の吸命波動弾 / 冥闇の呪縛波動弾|
|species_F15_R4_A|蒼白の人造人|ArtificialLife|B|冥闇の吸命弾 / 冥闇の呪縛弾 / 冥闇の祝福陣|
|species_F15_R4_B|獣心ホムンクルス|ArtificialLife|B|冥闇の吸命波動弾 / 冥闇の呪縛波動弾 / 冥闇の祝福波動陣|
|species_F15_R5_A|記憶を持つ人形|ArtificialLife|A|冥闇の吸命弾 / 冥闇の呪縛弾 / 冥闇の祝福陣 / 冥闇の分裂呪縛弾|
|species_F15_R5_B|不死身の合成体|ArtificialLife|A|岩石の吸命波動弾 / 岩石の呪縛波動弾 / 岩石の祝福波動陣 / 岩石の分裂呪縛弾|
|species_F15_R6_A|原初のホムンクルス|ArtificialLife|S|冥闇の吸命弾 / 冥闇の呪縛弾 / 冥闇の祝福陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F15_R6_B|命を編むもの|ArtificialLife|S|天光の吸命波動弾 / 天光の呪縛波動弾 / 天光の祝福波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F16_R0_A|魔導書の虫|Arcane|F|冥闇の周回弾|
|species_F16_R0_B|火花インク|Arcane|F|火炎の周回弾|
|species_F16_R1_A|まじないフクロウ|Arcane|E|風刃の周回弾|
|species_F16_R1_B|浮遊する巻物|Arcane|E|天光の周回弾|
|species_F16_R2_A|呪文喰らい|Arcane|D|冥闇の周回弾 / 冥闇の呪縛弾|
|species_F16_R2_B|青燐のランタン|Arcane|D|天光の周回弾 / 天光の呪縛波動弾|
|species_F16_R3_A|魔法陣の番人|Arcane|C|天光の周回弾 / 天光の呪縛弾|
|species_F16_R3_B|禁書の番犬|Arcane|C|天光の周回弾 / 天光の呪縛波動弾|
|species_F16_R4_A|星読みの水晶|Arcane|B|天光の周回弾 / 天光の呪縛弾 / 天光の癒し陣|
|species_F16_R4_B|雷文のゴーレム|Arcane|B|風刃の周回弾 / 風刃の呪縛波動弾 / 風刃の癒し波動陣|
|species_F16_R5_A|時詠みの司書|Arcane|A|天光の周回弾 / 天光の呪縛弾 / 天光の癒し陣 / 天光群星|
|species_F16_R5_B|紅蓮の魔導器|Arcane|A|火炎の周回弾 / 火炎の呪縛波動弾 / 火炎の癒し波動陣 / 火炎の分裂呪縛弾|
|species_F16_R6_A|万象の大魔導書|Arcane|S|天光の周回弾 / 天光の呪縛弾 / 天光の癒し陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F16_R6_B|天地創世の書|Arcane|S|天光の周回弾 / 天光の呪縛波動弾 / 天光の癒し波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F17_R0_A|火蜥蜴|Dragon|F|火炎の爆裂弾|
|species_F17_R0_B|若角ワイバーン|Dragon|F|風刃の爆裂波動弾|
|species_F17_R1_A|岩翼竜|Dragon|E|岩石の爆裂弾|
|species_F17_R1_B|沼毒ドラゴン|Dragon|E|火炎の爆裂波動弾|
|species_F17_R2_A|紅蓮の飛竜|Dragon|D|火炎の爆裂弾 / 火炎の呪縛弾|
|species_F17_R2_B|青嵐ワイバーン|Dragon|D|火炎の爆裂波動弾 / 火炎の呪縛波動弾|
|species_F17_R3_A|鎧鱗ドレイク|Dragon|C|岩石の爆裂呪縛弾 / 岩石の呪縛弾|
|species_F17_R3_B|氷牙ドラゴン|Dragon|C|火炎の爆裂呪縛弾 / 火炎の呪縛波動弾|
|species_F17_R4_A|雷角リンドブルム|Dragon|B|火炎の爆裂呪縛弾 / 火炎の呪縛弾 / 火炎の祝福陣|
|species_F17_R4_B|深緑の古竜|Dragon|B|岩石の爆裂呪縛弾 / 岩石の呪縛波動弾 / 岩石の祝福波動陣|
|species_F17_R5_B|黒曜バハムート|Dragon|A|冥闇の爆裂呪縛弾 / 冥闇の呪縛波動弾 / 冥闇の祝福波動陣 / 冥闇の分裂呪縛弾|
|species_F17_R6_A|天翔ける龍神|Dragon|S|火炎の爆裂呪縛弾 / 火炎の呪縛弾 / 火炎の祝福陣 / 火炎の分裂呪縛弾 / 火炎の爆裂呪縛弾|
|species_F17_R6_B|世界喰らいの竜|Dragon|S|冥闇の爆裂呪縛弾 / 冥闇の呪縛波動弾 / 冥闇の祝福波動陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F18_R0_A|一角うま|DivineBeast|F|天光の癒し陣|
|species_F18_R0_B|森の白鹿|DivineBeast|F|岩石の癒し波動陣|
|species_F18_R1_A|火尾きつね|DivineBeast|E|火炎の癒し陣|
|species_F18_R1_B|羽根獅子|DivineBeast|E|天光の癒し波動陣|
|species_F18_R2_A|月影ユニコーン|DivineBeast|D|天光の癒し陣 / 天光の呪縛弾|
|species_F18_R2_B|霧角カモシカ|DivineBeast|D|天光の癒し波動陣 / 天光の呪縛波動弾|
|species_F18_R3_A|紅翼ペガサス|DivineBeast|C|火炎の癒し陣 / 火炎の呪縛弾|
|species_F18_R3_B|雷爪ヒポグリフ|DivineBeast|C|天光の癒し波動陣 / 天光の呪縛波動弾|
|species_F18_R4_A|夢喰いバク|DivineBeast|B|天光の癒し陣 / 天光の呪縛弾 / 天光の癒し陣|
|species_F18_R4_B|雪鬣フェンリル|DivineBeast|B|水流の癒し波動陣 / 水流の呪縛波動弾 / 水流の癒し波動陣|
|species_F18_R5_A|虹尾の麒麟|DivineBeast|A|天光の癒し陣 / 天光の呪縛弾 / 天光の癒し陣 / 天光群星|
|species_F18_R5_B|天火の鳳凰|DivineBeast|A|火炎の癒し波動陣 / 火炎の呪縛波動弾 / 火炎の癒し波動陣 / 火炎の分裂呪縛弾|
|species_F18_R6_A|神域の白虎|DivineBeast|S|天光の癒し陣 / 天光の呪縛弾 / 天光の癒し陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F18_R6_B|暁を運ぶ霊獣|DivineBeast|S|天光の癒し波動陣 / 天光の呪縛波動弾 / 天光の癒し波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F19_R0_A|花びら妖精|Fairy|F|天光の癒し陣|
|species_F19_R0_B|木の実ピクシー|Fairy|F|岩石の癒し波動陣|
|species_F19_R1_A|鈴鳴りコロボックル|Fairy|E|岩石の癒し陣|
|species_F19_R1_B|水玉ニンフ|Fairy|E|天光の癒し波動陣|
|species_F19_R2_A|いたずらシルフ|Fairy|D|風刃の癒し陣 / 風刃の呪縛弾|
|species_F19_R2_B|蜜集めの羽虫|Fairy|D|天光の癒し波動陣 / 天光の呪縛波動弾|
|species_F19_R3_A|迷い森の小人|Fairy|C|岩石の癒し陣 / 岩石の呪縛弾|
|species_F19_R3_B|月灯りウィスプ|Fairy|C|天光の癒し波動陣 / 天光の呪縛波動弾|
|species_F19_R4_A|花冠の踊り子|Fairy|B|天光の癒し陣 / 天光の呪縛弾 / 天光の祝福陣|
|species_F19_R4_B|木漏れ日エルフ|Fairy|B|岩石の癒し波動陣 / 岩石の呪縛波動弾 / 岩石の祝福波動陣|
|species_F19_R5_A|夢紡ぎの妖精|Fairy|A|天光の癒し陣 / 天光の呪縛弾 / 天光の祝福陣 / 天光群星|
|species_F19_R5_B|七色の羽根姫|Fairy|A|天光の癒し波動陣 / 天光の呪縛波動弾 / 天光の祝福波動陣 / 天光群星|
|species_F19_R6_A|時渡りのフェイ|Fairy|S|天光の癒し陣 / 天光の呪縛弾 / 天光の祝福陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F19_R6_B|常若の女王|Fairy|S|天光の癒し波動陣 / 天光の呪縛波動弾 / 天光の祝福波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F20_R0_A|風のこだま|Spirit|F|風刃の追尾弾|
|species_F20_R0_B|川霧の子|Spirit|F|水流の追尾弾|
|species_F20_R1_A|火の粉の精|Spirit|E|火炎の追尾弾|
|species_F20_R1_B|石ころの精|Spirit|E|風刃の追尾弾|
|species_F20_R2_A|木霊|Spirit|D|岩石の追尾弾 / 岩石の呪縛追尾弾|
|species_F20_R2_B|水鏡の精|Spirit|D|風刃の追尾弾 / 風刃の呪縛追尾弾|
|species_F20_R3_A|つむじ風|Spirit|C|風刃の追尾弾 / 風刃の呪縛追尾弾|
|species_F20_R3_B|灯火の精霊|Spirit|C|風刃の追尾弾 / 風刃の呪縛追尾弾|
|species_F20_R4_B|氷河の精|Spirit|B|水流の追尾弾 / 水流の呪縛追尾弾 / 水流の癒し追尾陣|
|species_F20_R5_A|森羅の大精|Spirit|A|風刃の追尾弾 / 風刃の呪縛追尾弾 / 風刃の癒し追尾陣 / 風刃の分裂呪縛弾|
|species_F20_R5_B|朝焼けの精霊|Spirit|A|天光の追尾弾 / 天光の呪縛追尾弾 / 天光の癒し追尾陣 / 天光群星|
|species_F20_R6_A|四季を巡るもの|Spirit|S|風刃の追尾弾 / 風刃の呪縛追尾弾 / 風刃の癒し追尾陣 / 風刃の分裂呪縛弾 / 風刃の爆裂呪縛弾|
|species_F20_R6_B|天地の息吹|Spirit|S|天光の追尾弾 / 天光の呪縛追尾弾 / 天光の癒し追尾陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F21_R0_A|白羽の使い|Angel|F|天光の癒し陣|
|species_F21_R0_B|鈴音の天使|Angel|F|天光の癒し波動陣|
|species_F21_R1_A|道しるべの翼|Angel|E|天光の癒し陣|
|species_F21_R1_B|祈りの守人|Angel|E|天光の癒し波動陣|
|species_F21_R2_A|黎明の騎士天使|Angel|D|天光の癒し陣 / 天光の呪縛弾|
|species_F21_R2_B|青空の大翼|Angel|D|天光の癒し波動陣 / 天光の呪縛波動弾|
|species_F21_R3_A|聖火の御使い|Angel|C|天光の癒し陣 / 天光の呪縛弾|
|species_F21_R3_B|金環の守護者|Angel|C|天光の癒し波動陣 / 天光の呪縛波動弾|
|species_F21_R4_A|神託の翼人|Angel|B|天光の癒し陣 / 天光の呪縛弾 / 天光の祝福陣|
|species_F21_R4_B|暁光の大天使|Angel|B|火炎の癒し波動陣 / 火炎の呪縛波動弾 / 火炎の祝福波動陣|
|species_F21_R5_A|白銀の熾天使|Angel|A|天光の癒し陣 / 天光の呪縛弾 / 天光の祝福陣 / 天光群星|
|species_F21_R5_B|天門の番人|Angel|A|岩石の癒し波動陣 / 岩石の呪縛波動弾 / 岩石の祝福波動陣 / 岩石の分裂呪縛弾|
|species_F21_R6_A|七光のセラフ|Angel|S|天光の癒し陣 / 天光の呪縛弾 / 天光の祝福陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F21_R6_B|星界を照らす御使い|Angel|S|天光の癒し波動陣 / 天光の呪縛波動弾 / 天光の祝福波動陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F22_R0_A|小鬼の子|Demon|F|火炎の吸命弾|
|species_F22_R0_B|いたずらインプ|Demon|F|冥闇の吸命波動弾|
|species_F22_R1_A|針尾ガーゴイル|Demon|E|冥闇の吸命弾|
|species_F22_R1_B|煙幕のゴブリン|Demon|E|冥闇の吸命波動弾|
|species_F22_R2_A|夜盗のバグベア|Demon|D|火炎の吸命弾 / 火炎の呪縛弾|
|species_F22_R2_B|火遊びサラマンダー|Demon|D|冥闇の吸命波動弾 / 冥闇の呪縛波動弾|
|species_F22_R3_A|夢魔インキュバス|Demon|C|冥闇の吸命弾 / 冥闇の呪縛弾|
|species_F22_R3_B|毒角オーガ|Demon|C|冥闇の吸命波動弾 / 冥闇の呪縛波動弾|
|species_F22_R4_B|冥府のケルベロス|Demon|B|冥闇の吸命波動弾 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣|
|species_F22_R5_A|紅蓮のバフォメット|Demon|A|冥闇の吸命弾 / 冥闇の呪縛弾 / 冥闇の癒し陣 / 冥闇の分裂呪縛弾|
|species_F22_R5_B|奈落のベルゼブ|Demon|A|岩石の吸命波動弾 / 岩石の呪縛波動弾 / 岩石の癒し波動陣 / 岩石の分裂呪縛弾|
|species_F22_R6_A|魔界の大公|Demon|S|冥闇の吸命弾 / 冥闇の呪縛弾 / 冥闇の癒し陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F22_R6_B|終焉を告げる魔神|Demon|S|冥闇の吸命波動弾 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F23_R0_A|骨ねずみ|Undead|F|岩石の呪縛弾|
|species_F23_R0_B|墓場のカラス|Undead|F|岩石の呪縛波動弾|
|species_F23_R1_A|泥まみれゾンビ|Undead|E|火炎の呪縛弾|
|species_F23_R1_B|鎧骨兵|Undead|E|冥闇の呪縛波動弾|
|species_F23_R2_A|青火の骸骨|Undead|D|冥闇の呪縛弾 / 冥闇の呪縛弾|
|species_F23_R2_B|首なし騎士|Undead|D|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾|
|species_F23_R3_A|屍喰いグール|Undead|C|冥闇の呪縛弾 / 冥闇の呪縛弾|
|species_F23_R3_B|腐沼のリッチ|Undead|C|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾|
|species_F23_R4_A|墓守デュラハン|Undead|B|冥闇の呪縛弾 / 冥闇の呪縛弾 / 冥闇の祝福陣|
|species_F23_R4_B|白骨ワイバーン|Undead|B|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の祝福波動陣|
|species_F23_R5_A|死者の大行進|Undead|A|冥闇の呪縛弾 / 冥闇の呪縛弾 / 冥闇の祝福陣 / 冥闇の分裂呪縛弾|
|species_F23_R5_B|冥界の将軍|Undead|A|岩石の呪縛波動弾 / 岩石の呪縛波動弾 / 岩石の祝福波動陣 / 岩石の分裂呪縛弾|
|species_F23_R6_A|不滅の古王|Undead|S|冥闇の呪縛弾 / 冥闇の呪縛弾 / 冥闇の祝福陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F23_R6_B|黄泉路を統べる者|Undead|S|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の祝福波動陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F24_R0_A|ふわり火|Ghost|F|天光の追尾弾|
|species_F24_R0_B|窓辺のゆうれい|Ghost|F|風刃の追尾弾|
|species_F24_R1_A|迷子の霊|Ghost|E|風刃の追尾弾|
|species_F24_R1_B|すすり泣き|Ghost|E|冥闇の追尾弾|
|species_F24_R2_A|青白い人魂|Ghost|D|冥闇の追尾弾 / 冥闇の呪縛追尾弾|
|species_F24_R2_B|古井戸の影|Ghost|D|冥闇の追尾弾 / 冥闇の呪縛追尾弾|
|species_F24_R3_A|鈴鳴り幽霊|Ghost|C|天光の追尾弾 / 天光の呪縛追尾弾|
|species_F24_R3_B|夜汽車の亡霊|Ghost|C|冥闇の追尾弾 / 冥闇の呪縛追尾弾|
|species_F24_R4_A|逢魔が時の女|Ghost|B|冥闇の追尾弾 / 冥闇の呪縛追尾弾 / 冥闇の癒し追尾陣|
|species_F24_R4_B|白無垢の花嫁霊|Ghost|B|冥闇の追尾弾 / 冥闇の呪縛追尾弾 / 冥闇の癒し追尾陣|
|species_F24_R5_A|百鬼の長|Ghost|A|冥闇の追尾弾 / 冥闇の呪縛追尾弾 / 冥闇の癒し追尾陣 / 冥闇の分裂呪縛弾|
|species_F24_R5_B|千年の怨霊|Ghost|A|天光の追尾弾 / 天光の呪縛追尾弾 / 天光の癒し追尾陣 / 天光群星|
|species_F24_R6_A|幽世の渡し守|Ghost|S|冥闇の追尾弾 / 冥闇の呪縛追尾弾 / 冥闇の癒し追尾陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F24_R6_B|彼岸を閉ざすもの|Ghost|S|天光の追尾弾 / 天光の呪縛追尾弾 / 天光の癒し追尾陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F25_R0_A|呪いの釘|CursedObject|F|冥闇の呪縛弾|
|species_F25_R0_B|ひとりで動く靴|CursedObject|F|岩石の呪縛波動弾|
|species_F25_R1_A|泣き面の能面|CursedObject|E|冥闇の呪縛弾|
|species_F25_R1_B|喋る古鏡|CursedObject|E|冥闇の呪縛波動弾|
|species_F25_R2_A|血塗れの人形|CursedObject|D|冥闇の呪縛弾 / 冥闇の呪縛弾|
|species_F25_R2_B|笑う提灯|CursedObject|D|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾|
|species_F25_R3_A|割れた魔鏡|CursedObject|C|天光の呪縛弾 / 天光の呪縛弾|
|species_F25_R3_B|からくり生首|CursedObject|C|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾|
|species_F25_R4_A|鬼火の簪|CursedObject|B|冥闇の呪縛弾 / 冥闇の呪縛弾 / 冥闇の祝福陣|
|species_F25_R4_B|百目の壺|CursedObject|B|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の祝福波動陣|
|species_F25_R5_A|封じられた甲冑|CursedObject|A|冥闇の呪縛弾 / 冥闇の呪縛弾 / 冥闇の祝福陣 / 冥闇の分裂呪縛弾|
|species_F25_R5_B|泣き叫ぶ大鐘|CursedObject|A|岩石の呪縛波動弾 / 岩石の呪縛波動弾 / 岩石の祝福波動陣 / 岩石の分裂呪縛弾|
|species_F25_R6_A|千年呪いの面|CursedObject|S|冥闇の呪縛弾 / 冥闇の呪縛弾 / 冥闇の祝福陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F25_R6_B|厄災を宿す宝玉|CursedObject|S|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の祝福波動陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F26_R0_A|足元の影|Shadow|F|冥闇の呪縛波動弾|
|species_F26_R0_B|黒猫の影法師|Shadow|F|冥闇の呪縛波動弾|
|species_F26_R1_A|かげぼうし|Shadow|E|冥闇の呪縛波動弾|
|species_F26_R1_B|影縫いの犬|Shadow|E|冥闇の呪縛波動弾|
|species_F26_R2_A|宵闇のフード|Shadow|D|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾|
|species_F26_R2_B|影踏みの鬼|Shadow|D|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾|
|species_F26_R3_A|黒翼の狩人|Shadow|C|天光の呪縛波動弾 / 天光の呪縛波動弾|
|species_F26_R3_B|影喰いワーグ|Shadow|C|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾|
|species_F26_R4_A|月蝕の獣|Shadow|B|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣|
|species_F26_R4_B|影絵の騎士|Shadow|B|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣|
|species_F26_R5_A|夜帳の大狼|Shadow|A|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣 / 冥闇の分裂呪縛弾|
|species_F26_R5_B|無貌の影武者|Shadow|A|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣 / 冥闇の分裂呪縛弾|
|species_F26_R6_A|常闇の主|Shadow|S|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F26_R6_B|光を呑むもの|Shadow|S|冥闇の呪縛波動弾 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F27_R0_A|星屑の子|Astral|F|天光の爆裂螺旋弾|
|species_F27_R0_B|月見うさぎ|Astral|F|風刃の爆裂螺旋弾|
|species_F27_R1_A|流れ星の魚|Astral|E|天光の爆裂螺旋弾|
|species_F27_R1_B|彗尾の狐|Astral|E|天光の爆裂螺旋弾|
|species_F27_R2_A|月輪フクロウ|Astral|D|水流の爆裂螺旋弾 / 水流の呪縛螺旋弾|
|species_F27_R2_B|星渡りの旅人|Astral|D|天光の爆裂螺旋弾 / 天光の呪縛螺旋弾|
|species_F27_R3_A|宵星の鹿|Astral|C|天光の爆裂呪縛弾 / 天光の呪縛螺旋弾|
|species_F27_R3_B|天球クジラ|Astral|C|天光の爆裂呪縛弾 / 天光の呪縛螺旋弾|
|species_F27_R4_A|銀河ヒツジ|Astral|B|天光の爆裂呪縛弾 / 天光の呪縛螺旋弾 / 天光の祝福螺旋陣|
|species_F27_R4_B|極光の大鳥|Astral|B|冥闇の爆裂呪縛弾 / 冥闇の呪縛螺旋弾 / 冥闇の祝福螺旋陣|
|species_F27_R5_A|星座を結ぶ獣|Astral|A|天光の爆裂呪縛弾 / 天光の呪縛螺旋弾 / 天光の祝福螺旋陣 / 天光群星|
|species_F27_R5_B|月喰いの竜|Astral|A|岩石の爆裂呪縛弾 / 岩石の呪縛螺旋弾 / 岩石の祝福螺旋陣 / 岩石の分裂呪縛弾|
|species_F27_R6_A|天狼シリウス|Astral|S|天光の爆裂呪縛弾 / 天光の呪縛螺旋弾 / 天光の祝福螺旋陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F27_R6_B|銀河を泳ぐもの|Astral|S|天光の爆裂呪縛弾 / 天光の呪縛螺旋弾 / 天光の祝福螺旋陣 / 天光群星 / 天光の爆裂呪縛弾|
|species_F28_R0_A|底なし沼の子|Abyssal|F|水流の呪縛陣|
|species_F28_R0_B|深海の触手|Abyssal|F|岩石の呪縛波動陣|
|species_F28_R1_A|暗礁の牙|Abyssal|E|水流の呪縛陣|
|species_F28_R1_B|虚ろ貝|Abyssal|E|水流の呪縛波動陣|
|species_F28_R2_A|奈落の魚影|Abyssal|D|冥闇の呪縛陣 / 冥闇の呪縛弾|
|species_F28_R2_B|黒潮クラーケン|Abyssal|D|水流の呪縛波動陣 / 水流の呪縛波動弾|
|species_F28_R3_A|深淵ウツボ|Abyssal|C|水流の呪縛陣 / 水流の呪縛弾|
|species_F28_R3_B|底無しの捕食者|Abyssal|C|水流の呪縛波動陣 / 水流の呪縛波動弾|
|species_F28_R4_A|暗黒珊瑚の主|Abyssal|B|水流の呪縛陣 / 水流の呪縛弾 / 水流の癒し陣|
|species_F28_R4_B|無音の深海竜|Abyssal|B|冥闇の呪縛波動陣 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣|
|species_F28_R5_A|忘却を飼うもの|Abyssal|A|水流の呪縛陣 / 水流の呪縛弾 / 水流の癒し陣 / 水流の分裂呪縛弾|
|species_F28_R5_B|黒海の大王|Abyssal|A|岩石の呪縛波動陣 / 岩石の呪縛波動弾 / 岩石の癒し波動陣 / 岩石の分裂呪縛弾|
|species_F28_R6_A|奈落を渡る古鯨|Abyssal|S|水流の呪縛陣 / 水流の呪縛弾 / 水流の癒し陣 / 水流の分裂呪縛弾 / 水流の爆裂呪縛弾|
|species_F28_R6_B|世界の底に棲むもの|Abyssal|S|冥闇の呪縛波動陣 / 冥闇の呪縛波動弾 / 冥闇の癒し波動陣 / 冥闇の分裂呪縛弾 / 冥闇の爆裂呪縛弾|
|species_F29_R0_A|ぐにゃり玉|Chaos|F|魔力の分裂呪縛弾|
|species_F29_R0_B|あべこべ鳥|Chaos|F|冥闇の分裂呪縛弾|
|species_F29_R1_A|まぜこぜ獣|Chaos|E|風刃の分裂呪縛弾|
|species_F29_R1_B|裏返りトカゲ|Chaos|E|魔力の分裂呪縛弾|
|species_F29_R2_A|虹色バケモノ|Chaos|D|魔力の分裂呪縛弾 / 魔力の呪縛弾|
|species_F29_R2_B|歪み角のシカ|Chaos|D|魔力の分裂呪縛弾 / 魔力の呪縛波動弾|
|species_F29_R3_A|夢喰いの猫|Chaos|C|天光群星 / 天光の呪縛弾|
|species_F29_R3_B|反転する人形|Chaos|C|魔力の分裂呪縛弾 / 魔力の呪縛波動弾|
|species_F29_R4_A|七色の大蛇|Chaos|B|魔力の分裂呪縛弾 / 魔力の呪縛弾 / 魔力の祝福陣|
|species_F29_R4_B|姿なき合成獣|Chaos|B|冥闇の分裂呪縛弾 / 冥闇の呪縛波動弾 / 冥闇の祝福波動陣|
|species_F29_R5_A|万華のキマイラ|Chaos|A|魔力の分裂呪縛弾 / 魔力の呪縛弾 / 魔力の祝福陣 / 魔力の分裂呪縛弾|
|species_F29_R5_B|形なき混沌|Chaos|A|岩石の分裂呪縛弾 / 岩石の呪縛波動弾 / 岩石の祝福波動陣 / 岩石の分裂呪縛弾|
|species_F29_R6_A|始まりの異形|Chaos|S|魔力の分裂呪縛弾 / 魔力の呪縛弾 / 魔力の祝福陣 / 魔力の分裂呪縛弾 / 魔力の爆裂呪縛弾|
|species_F29_R6_B|名もなき万象|Chaos|S|魔力の分裂呪縛弾 / 魔力の呪縛波動弾 / 魔力の祝福波動陣 / 魔力の分裂呪縛弾 / 魔力の爆裂呪縛弾|
