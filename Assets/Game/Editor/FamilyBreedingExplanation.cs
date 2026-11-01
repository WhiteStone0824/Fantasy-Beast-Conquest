namespace FusionAnyMonster.Editor
{
    internal static class FamilyBreedingExplanation
    {
        private static readonly string[] Reasons={
            "不定形・流動性と生命力が結び付く", "陸上動物としての体や獣性が強く現れる", "翼や飛翔性が鳥の姿として現れる",
            "鱗や硬い外皮を持つ動物の姿になる", "水辺への適応と陸上の生命力が結び付く", "水中への適応が生物の姿を方向づける",
            "翼・小型性・外殻などが虫の姿へまとまる", "水辺の性質と硬い殻・素材が結び付く", "自然の生命力と生長性が植物として現れる",
            "湿り気や腐朽・増殖性が菌類として現れる", "知性・人型と生命力が亜人の姿へまとまる", "肉体と大地・硬い素材が大型の体を形作る",
            "器物・素材の性質が身体の中心になる", "大地の力や硬質な素材が結晶へまとまる", "器物に動力・知性が加わり機構を形作る",
            "生命と器物・人工的な構造が融合する", "魔力が器物や知性へ定着する", "鱗・外殻・翼と強い生命力や魔力が結び付く",
            "動物の性質と霊性・魔力が結び付く", "自然の魔力と小さな・人型の生命が結び付く", "自然と魔力が形のない精霊へまとまる",
            "光・聖性と人型・翼の性質が結び付く", "闇の魔力が生物や人型の姿へ現れる", "死や腐朽の性質が肉体・生命へ宿る",
            "死の性質と形のない霊力が結び付く", "闇や霊性が器物へ宿り呪物になる", "闇と形のない力が影の存在へまとまる",
            "光・結晶・翼や霊力が天体的な存在へ昇華する", "水や不定形の力と深い闇が結び付く", "相反する力や不定形の魔力が混ざり合う"
        };
        public static string Explain(SpeciesFamily a,SpeciesFamily b,SpeciesFamily result)
        {
            if(a==SpeciesFamily.Truth || b==SpeciesFamily.Truth)return "真理の系統と種族を子へ受け継ぐ";
            if(a==b)return "同じ系統の性質を維持する";
            if(Pair(a,b,SpeciesFamily.Spirit,SpeciesFamily.Material))return "精霊の魔力が物質へ定着する";
            if(Pair(a,b,SpeciesFamily.Beast,SpeciesFamily.Machine))return "獣の生命と機械の構造が融合する";
            if(Pair(a,b,SpeciesFamily.Plant,SpeciesFamily.DemiHuman))return "植物の生命力が人型の妖精として現れる";
            if(Pair(a,b,SpeciesFamily.Beast,SpeciesFamily.Reptile))return "獣の体格と爬虫の鱗が融合する";
            if(Pair(a,b,SpeciesFamily.Ghost,SpeciesFamily.Material))return "霊体が器物へ憑依する";
            if(Pair(a,b,SpeciesFamily.Angel,SpeciesFamily.Demon))return "相反する聖性と魔性が混ざり合う";
            if(Pair(a,b,SpeciesFamily.Astral,SpeciesFamily.Abyssal))return "星界と深淵の相反する力が混ざり合う";
            if(Pair(a,b,SpeciesFamily.Slime,SpeciesFamily.Plant))return "湿潤な身体と植物の生長性から菌類が発現する";
            if(Pair(a,b,SpeciesFamily.Mineral,SpeciesFamily.Spirit))return "結晶に精霊の魔力が定着する";
            if(result==a || result==b)
                return FamilyBreedingCatalog.DisplayName(result)+"の性質を主体に、"+FamilyBreedingCatalog.DisplayName(result==a?b:a)+"の形質を受け継ぐ";
            return Reasons[(int)result];
        }
        private static bool Pair(SpeciesFamily a,SpeciesFamily b,SpeciesFamily x,SpeciesFamily y)
        {return a==x && b==y || a==y && b==x;}
    }
}
