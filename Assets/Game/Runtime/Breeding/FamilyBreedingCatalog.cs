using System;
using F = FusionAnyMonster.SpeciesFamily;

namespace FusionAnyMonster
{
    /// <summary>Explicit unordered family recipes. Row A contains results for B >= A.</summary>
    public static class FamilyBreedingCatalog
    {
        private static readonly string[] Names={"スライム系","獣系","鳥系","爬虫系","両生系","水棲系","虫系","甲殻系","植物系","菌類系","亜人系","巨人系","物質系","鉱物系","機械系","人工生命系","魔導系","竜系","幻獣系","妖精系","精霊系","天使系","悪魔系","死霊系","霊体系","呪物系","影系","星界系","深淵系","混沌系","真理系"};
        private static readonly F[][] Results={
            new[]{F.Slime,F.Amphibian,F.Bird,F.Crustacean,F.Slime,F.Slime,F.Fungi,F.Insect,F.Fungi,F.Aquatic,F.Aquatic,F.Crustacean,F.Giant,F.Mineral,F.Machine,F.ArtificialLife,F.Chaos,F.Aquatic,F.Amphibian,F.Fungi,F.Fungi,F.Amphibian,F.Abyssal,F.Chaos,F.Spirit,F.Ghost,F.Shadow,F.Amphibian,F.Abyssal,F.Slime},
            new[]{F.Beast,F.Beast,F.Dragon,F.Aquatic,F.Amphibian,F.Insect,F.Reptile,F.Beast,F.Plant,F.DemiHuman,F.Reptile,F.Giant,F.Giant,F.ArtificialLife,F.Material,F.DivineBeast,F.Dragon,F.Beast,F.DemiHuman,F.Plant,F.DivineBeast,F.Bird,F.Fungi,F.Undead,F.Undead,F.DivineBeast,F.Mineral,F.Undead,F.ArtificialLife},
            new[]{F.Bird,F.Insect,F.Bird,F.Aquatic,F.Reptile,F.Aquatic,F.Insect,F.Fungi,F.Insect,F.Reptile,F.ArtificialLife,F.Dragon,F.Dragon,F.Slime,F.Astral,F.Reptile,F.Angel,F.DivineBeast,F.Fairy,F.DivineBeast,F.Demon,F.Ghost,F.Slime,F.Dragon,F.Astral,F.Angel,F.Demon,F.Spirit},
            new[]{F.Reptile,F.Crustacean,F.Amphibian,F.Bird,F.Amphibian,F.Reptile,F.Plant,F.Beast,F.Beast,F.Plant,F.Beast,F.Crustacean,F.Slime,F.Machine,F.Insect,F.Reptile,F.Dragon,F.Plant,F.Insect,F.DemiHuman,F.DivineBeast,F.DivineBeast,F.CursedObject,F.Chaos,F.Mineral,F.Aquatic,F.Dragon},
            new[]{F.Amphibian,F.Beast,F.Fungi,F.Reptile,F.Fungi,F.Slime,F.Beast,F.Giant,F.Slime,F.Amphibian,F.Amphibian,F.Crustacean,F.ArtificialLife,F.Reptile,F.Aquatic,F.Fungi,F.Plant,F.DivineBeast,F.Abyssal,F.Beast,F.Chaos,F.Crustacean,F.Amphibian,F.Astral,F.Ghost,F.Aquatic},
            new[]{F.Aquatic,F.Crustacean,F.Reptile,F.Plant,F.Amphibian,F.DemiHuman,F.Beast,F.Material,F.Reptile,F.Crustacean,F.Crustacean,F.Spirit,F.Dragon,F.Aquatic,F.Insect,F.Aquatic,F.Bird,F.Abyssal,F.Slime,F.Chaos,F.ArtificialLife,F.Spirit,F.Spirit,F.Abyssal,F.DivineBeast},
            new[]{F.Insect,F.Plant,F.Reptile,F.Bird,F.Reptile,F.Crustacean,F.Beast,F.Crustacean,F.Insect,F.Bird,F.Spirit,F.Fairy,F.Dragon,F.Plant,F.Astral,F.Fairy,F.Dragon,F.Dragon,F.Fairy,F.DivineBeast,F.Chaos,F.Angel,F.Demon,F.Slime},
            new[]{F.Crustacean,F.Amphibian,F.Insect,F.Aquatic,F.Amphibian,F.Material,F.Aquatic,F.Machine,F.Beast,F.Spirit,F.Crustacean,F.Crustacean,F.Aquatic,F.DivineBeast,F.Dragon,F.Bird,F.Fungi,F.Dragon,F.Abyssal,F.Dragon,F.DivineBeast,F.Crustacean,F.Slime},
            new[]{F.Plant,F.Fungi,F.Fairy,F.Giant,F.Giant,F.Mineral,F.Material,F.Machine,F.ArtificialLife,F.Reptile,F.Plant,F.Insect,F.Spirit,F.Fairy,F.Fairy,F.Undead,F.Spirit,F.Abyssal,F.Plant,F.Astral,F.Amphibian,F.Spirit},
            new[]{F.Fungi,F.DemiHuman,F.DemiHuman,F.Fungi,F.ArtificialLife,F.Plant,F.Machine,F.Fairy,F.Bird,F.Fairy,F.Bird,F.Spirit,F.Insect,F.Insect,F.Fairy,F.Ghost,F.Undead,F.Slime,F.Mineral,F.Undead,F.Chaos},
            new[]{F.DemiHuman,F.Giant,F.Machine,F.Machine,F.Material,F.Material,F.Material,F.Giant,F.Angel,F.Fairy,F.Angel,F.ArtificialLife,F.ArtificialLife,F.Fungi,F.ArtificialLife,F.Undead,F.Chaos,F.Angel,F.Demon,F.Arcane},
            new[]{F.Giant,F.DemiHuman,F.Plant,F.Mineral,F.Mineral,F.Material,F.Dragon,F.Beast,F.Plant,F.Mineral,F.Angel,F.Demon,F.Fungi,F.Undead,F.Mineral,F.Shadow,F.Angel,F.Slime,F.DemiHuman},
            new[]{F.Material,F.Mineral,F.Machine,F.ArtificialLife,F.Mineral,F.DivineBeast,F.Giant,F.Fairy,F.Arcane,F.Arcane,F.CursedObject,F.CursedObject,F.CursedObject,F.Undead,F.Mineral,F.Arcane,F.Material,F.Machine},
            new[]{F.Mineral,F.Mineral,F.Material,F.Machine,F.Arcane,F.Material,F.Giant,F.Arcane,F.Giant,F.Giant,F.Material,F.Astral,F.Astral,F.CursedObject,F.CursedObject,F.Shadow,F.Machine},
            new[]{F.Machine,F.ArtificialLife,F.Arcane,F.Bird,F.DivineBeast,F.DemiHuman,F.Arcane,F.Arcane,F.CursedObject,F.Material,F.CursedObject,F.Abyssal,F.CursedObject,F.Mineral,F.Shadow,F.ArtificialLife},
            new[]{F.ArtificialLife,F.Chaos,F.Giant,F.DivineBeast,F.Slime,F.Chaos,F.CursedObject,F.DemiHuman,F.CursedObject,F.Arcane,F.Shadow,F.Arcane,F.Machine,F.Ghost,F.Arcane},
            new[]{F.Arcane,F.Astral,F.Astral,F.Angel,F.Machine,F.DemiHuman,F.Shadow,F.CursedObject,F.CursedObject,F.Ghost,F.Demon,F.Arcane,F.CursedObject,F.ArtificialLife},
            new[]{F.Dragon,F.Angel,F.Insect,F.Fairy,F.Angel,F.Demon,F.Giant,F.Chaos,F.Demon,F.Demon,F.Spirit,F.Undead,F.Bird},
            new[]{F.DivineBeast,F.DemiHuman,F.Astral,F.DemiHuman,F.Angel,F.Undead,F.Undead,F.Material,F.Shadow,F.Beast,F.Undead,F.Astral},
            new[]{F.Fairy,F.Spirit,F.Bird,F.Fairy,F.Undead,F.Angel,F.Demon,F.Demon,F.Bird,F.Demon,F.Angel},
            new[]{F.Spirit,F.Fairy,F.Shadow,F.Ghost,F.Ghost,F.CursedObject,F.Abyssal,F.Astral,F.Shadow,F.Chaos},
            new[]{F.Angel,F.Chaos,F.DemiHuman,F.Astral,F.Demon,F.Demon,F.Spirit,F.Angel,F.Astral},
            new[]{F.Demon,F.Demon,F.Abyssal,F.Arcane,F.Astral,F.Shadow,F.Ghost,F.Shadow},
            new[]{F.Undead,F.Undead,F.Abyssal,F.Shadow,F.Ghost,F.Abyssal,F.Ghost},
            new[]{F.Ghost,F.Shadow,F.Abyssal,F.Ghost,F.Spirit,F.Ghost},
            new[]{F.CursedObject,F.Ghost,F.Shadow,F.Arcane,F.Machine},
            new[]{F.Shadow,F.Abyssal,F.Ghost,F.Abyssal},
            new[]{F.Astral,F.Chaos,F.Chaos},
            new[]{F.Abyssal,F.Shadow},
            new[]{F.Chaos},
        };
        public static F Resolve(F a,F b)
        {
            int first=(int)a,second=(int)b;
            if(first<0 || first>=Names.Length || !Enum.IsDefined(typeof(F),a))throw new ArgumentOutOfRangeException(nameof(a));
            if(second<0 || second>=Names.Length || !Enum.IsDefined(typeof(F),b))throw new ArgumentOutOfRangeException(nameof(b));
            // The only current Truth species passes its species down when used as a parent.
            if(a==F.Truth || b==F.Truth)return F.Truth;
            int row=Math.Min(first,second);return Results[row][Math.Max(first,second)-row];
        }
        public static string DisplayName(F family)
        {int i=(int)family;return i>=0 && i<Names.Length?Names[i]:family.ToString();}
        public static FamilyPairRule[] CreateRules()
        {
            int count=Enum.GetValues(typeof(F)).Length;
            if(Results.Length!=count-1 || Names.Length!=count)throw new InvalidOperationException("New families require explicit breeding rows and display names.");
            var result=new FamilyPairRule[count*(count+1)/2];int index=0;
            for(int a=0;a<Results.Length;a++)
            {
                if(Results[a].Length!=Results.Length-a)throw new InvalidOperationException("Incomplete breeding row: "+(F)a+" ("+Results[a].Length+" cells)");
                for(int b=a;b<Results.Length;b++)result[index++]=new FamilyPairRule{first=(F)a,second=(F)b,result=Resolve((F)a,(F)b)};
            }
            for(int a=0;a<count;a++)result[index++]=new FamilyPairRule{first=(F)a,second=F.Truth,result=Resolve((F)a,F.Truth)};
            return result;
        }
    }
}
