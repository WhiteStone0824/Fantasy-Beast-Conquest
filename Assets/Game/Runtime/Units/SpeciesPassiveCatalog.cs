using System;
using UnityEngine;

namespace FusionAnyMonster
{
    /// <summary>Authored names and affinities for all 30 families x 7 ranks x 2 species.</summary>
    public static class SpeciesPassiveCatalog
    {
        private static readonly string[] Titles={
            "水の申し子|泡のゆりかご|しずくの循環|沼底の粘り|もちもちの守り|とろける再生|雨の追い風|泡沫の手当て|瑠璃の魔脈|琥珀の捕食|霧隠れの足取り|水鏡の反攻|雲わたの魔律|星雫の祝福",
            "森番の毛皮|小さな逃げ足|跳ね牙の狩り|逆立つ背毛|紅牙の追い込み|苔むす息吹|黒斑の捕食|崖際の踏ん張り|月輪の魔力|火喰いの血|白銀の癒し|雷角の充填|天翔ける魔律|古森の息吹",
            "こだまの共鳴|野原の身軽さ|風切りの追跡|火羽の加護|夜目の魔眼|砂よけの羽衣|夜渡りの吸血|雷鳴の急襲|雪冠の手当て|紅蓮の羽盾|月読みの魔律|嵐待ちの翼|暁の再生|天穹の守り",
            "石肌のぬくもり|草むらの警戒|砂潜りの充填|沼地の追跡者|岩背の魔紋|毒牙の逆襲|青鱗の鎧|火口の再生|鎧首の介抱|黒曜の締めつけ|霜背の底力|雷鱗の疾走|古鱗の秘法|天裂きの加護",
            "雨音の加護|水たまりの用心|苔肌の巡り|水辺の跳躍|沼底の魔声|泡吹きの甲羅|失われた尾の再生|毒沼の踏ん張り|月影の吸命|岩砕きの奮起|霧笛の救い|氷沼の早足|古池の静寂|天雨の恵み",
            "小川の流儀|群泳の警戒|泡ふぐの巡り|珊瑚の狩場|深海の灯火|潮吹きの守り|青鋼の手当て|沼底の命脈|月光のしびれ|火山の吸熱|渦潮の滑走|氷海の粘り|海鳴りの律動|蒼海の庇護",
            "花蜜の蓄え|葉影の見張り|糸繰りの魔脈|夜蛍の案内|鎌角の集中|毒針の甲殻|花粉の手当て|甲冑の自己修復|紅蓮の覚醒|鏡翅の吸光|墓守りの歩法|雷角の不屈|群れを導く羽音|千眼の摂理",
            "潮だまりの子|砂殻の安らぎ|泡圧の循環|石背の追走|赤爪の魔紋|岩砕きの殻|真珠の治癒|深淵殻の再生|火山の逆襲|瑠璃殻の捕食|双鋏の足運び|氷殻の底力|鉄甲の律動|海底城の加護",
            "若葉の芽吹き|つる草の見張り|花冠の蜜脈|眠り草の追根|アザミの魔力|蔦巻きの護り|花歌の手当て|火花の新芽|白樺の怒り|毒蜜の吸生|月光葉の揺らぎ|絡み根の不屈|花冠の詠唱|世界樹の祝福",
            "木漏れ日の胞子|胞子の傘守り|眠りの菌糸|沼地の追菌|毒傘の魔香|笑い傘の守り|青苔の養生|夜光の再生胞子|火吹きの奮起|石茸の吸根|夢見の胞子舞い|腐海の粘り|老茸の菌網|星降る滋養",
            "森番の心得|石笛の安息|川辺の魔詩|野鍛冶の眼力|角笛の魔力|霧隠れの受け身|鉄靴の救護|紅蓮舞の呼吸|月下の反攻|鷹目の吸気|白樺の足運び|雷槌の踏ん張り|近衛の秘儀|黎明の誓い",
            "岩こびとの土魂|丘の見張り|苔岩の魔脈|雪原の追跡|山砕きの魔腕|溶岩の石鎧|霧峰の手当て|鉄骨の修復|雷鳴の奮起|氷壁の吸霜|古森の大股|星喰いの不屈|大地の呼吸|天蓋の庇護",
            "土くれの加護|藁束の身構え|石臼の巡り|泥足の追跡|砂時計の魔砂|磁鉄の護り|青銅の補修|陶器の継ぎ目|黒曜の反攻|琥珀の蓄命|白磁の舞い足|千年石の不屈|遺跡の魔式|星鋼の庇護",
            "小石の土魂|砂金の警戒|水晶の共鳴|瑪瑙の追跡|翡翠の魔核|紅玉の守り|黒曜の手当て|琥珀の自己修復|蒼玉の逆光|銀晶の吸熱|黄玉の身軽さ|星鉄の不屈|虹晶の詠唱|天晶の守護",
            "歯車の放電|ブリキの整備|からくりの充填|蒸気の追走|鋼翼の魔導炉|ゼンマイの護り|真鍮の救急機構|磁気の自己修復|雷管の過負荷|砲身の吸熱|白銀の高速機構|黒鉄の非常装甲|古代機関の演算|星渡りの動力炉",
            "夢殻の加護|つぎはぎの護り|培養液の巡り|うつろな追跡|獣核の魔力|硝子の皮膚|縫い目の手当て|培養槽の再生|蒼白の反攻|獣心の吸命|記憶の身のこなし|不死身の粘り|原初の命式|命を編む光",
            "書虫の加護|火花の封蝋|まじないの循環|巻物の追跡|呪文喰いの魔核|青燐の護り|魔法陣の救護|禁書の自己修復|星読みの逆算|雷文の吸魔|時詠みの歩法|紅蓮器の底力|万象の魔律|創世の余韻",
            "火口の血統|若角の警戒|岩翼の巡り|沼毒の追撃|紅蓮の魔喉|青嵐の鱗盾|鎧鱗の手当て|氷牙の再生|雷角の激昂|深緑の捕食|黄金翼の疾走|黒曜の不屈|龍神の詠唱|世界喰らいの威光",
            "一角の加護|白鹿の警戒|火尾の灯脈|羽獅子の狩り|月影の魔角|霧角の護り|紅翼の手当て|雷爪の再生|夢喰いの覚醒|雪鬣の吸命|虹尾の歩法|天火の不屈|白虎の神律|暁を運ぶ祝福",
            "花びらの祝福|木の実の備え|鈴音の巡り|水玉の追跡|いたずらの魔風|蜜羽の護り|迷い森の手当て|月灯りの再生|花冠の奮起|木漏れ日の吸光|夢紡ぎの舞い|七色羽の底力|時渡りの魔律|常若の贈り物",
            "風声の加護|川霧の抱擁|火の粉の輪舞|石ころの見張り|木霊の魔声|水鏡の護り|つむじの手当て|灯火の再燃|嵐雲の奮起|霜花の吸気|大地の歩調|光輪の不屈|星詠みの精律|根源の祝福",
            "羽灯りの加護|白羽の庇い|祈りの循環|聖鐘の見張り|光輪の魔力|翼盾の護り|慈愛の手当て|暁光の再生|裁きの奮起|聖火の吸光|祝翼の歩法|守護の底力|天啓の詠唱|楽園の祝福",
            "小悪魔の火種|黒角の用心|夜契の循環|悪戯な追跡|業火の魔眼|黒翼の護り|契約の手当て|魔血の再生|暴虐の目覚め|魂喰いの渇き|幻惑の足取り|奈落の不屈|禁忌の魔律|終焉の契約",
            "骨の余熱|墓土の守り|鬼火の巡り|骸の追跡|死香の魔力|骨鎧の護り|葬花の手当て|朽ちない再生|怨念の奮起|死者の吸命|影骨の歩法|墓守りの不屈|冥府の詠唱|永夜の残響",
            "微光の加護|ゆらぎの護り|浮遊の魔脈|残響の追跡|幽灯の魔力|霊衣の守り|魂灯の手当て|消えない灯火|未練の奮起|夢影の吸命|透けゆく足跡|薄明の底力|幽界の魔律|彼岸の祝福",
            "呪札の加護|藁呪の備え|針音の循環|封印の追跡|厄印の魔力|黒縄の護り|身代わりの手当て|祟りの再燃|怨嗟の反攻|凶器の吸命|禍影の歩み|封器の底力|禁呪の魔律|因果の結び目",
            "影溜まりの加護|夜布の護り|黒露の循環|影足の追跡|月蝕の魔眼|闇衣の守り|薄影の手当て|消えない残影|暗躍の奮起|影喰いの渇き|夜走りの歩法|深影の粘り|影縫いの魔律|常闇の帳",
            "星屑の加護|小彗星の護り|軌道の循環|流星の追跡|月海の魔力|隕鉄の守り|星灯りの手当て|新星の再燃|超新星の奮起|虚空の吸光|彗星の歩法|重力の底力|天体の魔律|銀河の祝福",
            "深淵の雫|暗礁の庇い|底流の循環|深海の追跡|黒潮の魔眼|深殻の守り|海溝の手当て|淵底の再生|暗流の逆襲|飢えた深淵|沈黙の泳法|深圧の粘り|深淵の詠唱|底なき呼び声",
            "揺らぎの種|歪みの庇い|混色の循環|不定形の追跡|逆理の魔力|継ぎ目なき守り|変転の手当て|崩れて生まれる|秩序への反攻|万象の吸命|乱舞の足取り|矛盾の粘り|万華の魔律|名もなき摂理"
        };
        private static readonly string[] Affinities={
            "水水水土水土水風水土風水風光","土土風土火土闇土光火水風風土","風風風火闇土闇風水火光風火風",
            "土風土水土闇水火土闇水風闇風","水水土水土水水闇闇土風水水水","水水水水闇水水土光火水水水水",
            "風土闇光風闇風土火水闇風風闇","水土水土火土光闇火水風水土水","土風光土闇土光火光闇光土土光",
            "土土闇水闇土水光火土闇闇土光","土風水火風闇土火闇風光風光光","土土土水土火風土風水土闇土光",
            "土風土水土土風光闇土光土土光","土土光土水火闇土光水土土光光","風土火風風土光土風火光闇土光",
            "闇土水闇火光土水闇闇光土光光","闇火風光闇光光闇光風光火光光","火風土闇火風土水風土光闇光闇",
            "光土火風光風火風闇水光火光光","光土土水風風土光光土闇光光光","風水火土土水風火風水土光光光",
            "光光光風光光光火光火風土光光","火闇闇闇火闇闇火闇闇闇土闇闇","土土火闇闇土闇闇闇闇風土闇闇",
            "光風風闇闇水光火闇闇風光闇光","闇土闇闇闇土光火闇闇風土闇闇","闇闇闇風闇闇光闇闇闇風闇闇闇",
            "光風光風水土光火火闇風土光光","水土水闇闇水水闇闇闇水土闇闇","無闇風闇無土光火闇闇風土無無"
        };
        private static readonly string[] Recipes={"ABCF LHDGEJKIMN","FKDBIHJLEAGCMN","CKDAEBJIGFMLHN","ABCD EIFHGJLK MN","ABCKEFHLJIGDMN"};
        private static readonly DebuffType[] Debuffs={DebuffType.Slow,DebuffType.Slow,DebuffType.Paralysis,DebuffType.Poison,DebuffType.Freeze,DebuffType.Freeze,DebuffType.Poison,DebuffType.Slow,DebuffType.Poison,DebuffType.Sleep,DebuffType.Silence,DebuffType.Paralysis,DebuffType.Curse,DebuffType.Freeze,DebuffType.Paralysis,DebuffType.Poison,DebuffType.Silence,DebuffType.Burn,DebuffType.Curse,DebuffType.Sleep,DebuffType.Silence,DebuffType.Curse,DebuffType.Curse,DebuffType.Poison,DebuffType.Sleep,DebuffType.Curse,DebuffType.Sleep,DebuffType.Silence,DebuffType.Curse,DebuffType.Curse};
        public static SpeciesPassive Create(SpeciesDefinition species)
        {
            if(species.family==SpeciesFamily.Truth)
                return new SpeciesPassive {id="passive_"+species.speciesId,displayName="真理の御魂",effects=new[]{E(PassiveEffectType.AllStatsBonus,.2f),D(PassiveEffectType.OnHitDebuff,1f,DebuffType.Poison),E(PassiveEffectType.ReviveAlliesOnDeath,1f)}};
            int family=(int)species.family,index=(int)species.rank*2+(species.speciesId.EndsWith("_B",StringComparison.Ordinal)?1:0);
            // The passive follows the named species when its rank slot is reassigned.
            if(family==(int)SpeciesFamily.Giant) { if(index==5)index=8;else if(index==8)index=5; }
            if(family==(int)SpeciesFamily.Angel) { if(index==9)index=10;else if(index==10)index=9; }
            string[] names=Titles[family].Split('|');
            string recipe=family<Recipes.Length?Recipes[family].Replace(" ",""):"ABCDEFGHIJKLMN";
            if(names.Length!=14 || Affinities[family].Length!=14 || recipe.Length!=14)throw new InvalidOperationException("Incomplete passive catalog for "+species.family);
            AttributeElement element=Element(Affinities[family][index]);DebuffType debuff=Debuffs[family];
            PassiveEffect[] effects;
            switch(recipe[index])
            {
                case 'A':effects=new[]{E(PassiveEffectType.ElementResistance,.5f,element),E(PassiveEffectType.ElementDamage,.2f,element),E(PassiveEffectType.ElementMpReduction,.2f,element)};break;
                case 'B':effects=new[]{E(PassiveEffectType.ElementResistance,.35f,element),D(PassiveEffectType.DebuffResistance,.6f,debuff),E(PassiveEffectType.HealingReceived,.2f)};break;
                case 'C':effects=new[]{E(PassiveEffectType.ElementDamage,.15f,element),E(PassiveEffectType.ElementMpReduction,.25f,element),S(9,.2f)};break;
                case 'D':effects=new[]{S(2,.2f),S(8,.15f),S(6,.1f)};break;
                case 'E':effects=new[]{S(3,.2f),E(PassiveEffectType.ElementMpReduction,.15f,element),D(PassiveEffectType.DebuffResistance,.6f,DebuffType.Silence)};break;
                case 'F':effects=new[]{S(4,.2f),S(5,.15f),D(PassiveEffectType.DebuffResistance,.5f,debuff)};break;
                case 'G':effects=new[]{E(PassiveEffectType.HealingDone,.25f),S(1,.2f),E(PassiveEffectType.ElementResistance,.25f,element)};break;
                case 'H':effects=new[]{E(PassiveEffectType.HpRegeneration,.01f),E(PassiveEffectType.HealingReceived,.2f),D(PassiveEffectType.DebuffResistance,.5f,DebuffType.Curse)};break;
                case 'I':effects=new[]{E(PassiveEffectType.LowHpDamage,.25f),S(7,.15f),D(PassiveEffectType.OnHitDebuff,.15f,debuff)};break;
                case 'J':effects=new[]{E(PassiveEffectType.LifeSteal,.08f),E(PassiveEffectType.ElementDamage,.15f,element),D(PassiveEffectType.DebuffResistance,.5f,DebuffType.Sleep)};break;
                case 'K':effects=new[]{S(6,.15f),S(7,.15f),D(PassiveEffectType.DebuffResistance,.6f,DebuffType.Slow)};break;
                case 'L':effects=new[]{E(PassiveEffectType.LowHpProtection,.25f),S(0,.15f),E(PassiveEffectType.ElementResistance,.25f,element)};break;
                case 'M':effects=new[]{E(PassiveEffectType.CooldownReduction,.1f),E(PassiveEffectType.DurationBonus,.2f),S(9,.2f)};break;
                default:effects=new[]{E(PassiveEffectType.ElementResistance,.35f,element),E(PassiveEffectType.ElementDamage,.25f,element),E(PassiveEffectType.CooldownReduction,.12f)};break;
            }
            return new SpeciesPassive {id="passive_"+species.speciesId,displayName=names[index],effects=effects};
        }
        private static PassiveEffect E(PassiveEffectType type,float amount,AttributeElement element=AttributeElement.None) { return new PassiveEffect {type=type,amount=amount,element=element}; }
        private static PassiveEffect S(int index,float amount) { var effect=E(PassiveEffectType.StatBonus,amount);effect.statIndex=index;return effect; }
        private static PassiveEffect D(PassiveEffectType type,float amount,DebuffType debuff) { var effect=E(type,amount);effect.debuff=debuff;return effect; }
        private static AttributeElement Element(char value) { switch(value){case '火':return AttributeElement.Fire;case '水':return AttributeElement.Water;case '風':return AttributeElement.Wind;case '土':return AttributeElement.Earth;case '光':return AttributeElement.Light;case '闇':return AttributeElement.Dark;default:return AttributeElement.None;} }
    }
}
