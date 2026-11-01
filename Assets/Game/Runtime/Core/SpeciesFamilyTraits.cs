using UnityEngine;

namespace FusionAnyMonster
{
    // primary and weak element, debuff resistance, two favored stats, one trade-off.
    public static class SpeciesFamilyTraits
    {
        public struct Profile
        {
            public int primary,weak,debuff,first,second,tradeoff;
            public Profile(int primary,int weak,int debuff,int first,int second,int tradeoff)
            {this.primary=primary;this.weak=weak;this.debuff=debuff;this.first=first;this.second=second;this.tradeoff=tradeoff;}
        }
        private static readonly Profile[] Profiles={
            new Profile(1,0,0,0,9,2), // slime
            new Profile(2,4,6,2,6,3), // beast
            new Profile(2,3,7,6,8,4), // bird
            new Profile(3,1,0,4,2,6), // reptile
            new Profile(1,0,0,0,9,4), // amphibian
            new Profile(1,3,2,6,7,4), // aquatic
            new Profile(2,0,0,7,6,0), // insect
            new Profile(1,2,0,4,0,6), // crustacean
            new Profile(3,0,0,0,9,6), // plant
            new Profile(3,0,0,5,9,6), // fungi
            new Profile(3,5,6,2,4,3), // demi-human
            new Profile(3,2,3,0,2,6), // giant
            new Profile(3,1,4,4,0,6), // material
            new Profile(3,2,0,4,5,6), // mineral
            new Profile(4,1,0,4,8,9), // machine
            new Profile(5,4,0,0,5,6), // artificial life
            new Profile(4,5,6,3,1,2), // arcane
            new Profile(0,1,1,2,3,7), // dragon
            new Profile(4,5,7,3,6,4), // divine beast
            new Profile(2,3,4,6,1,4), // fairy
            new Profile(2,3,6,3,9,4), // spirit
            new Profile(4,5,7,3,5,2), // angel
            new Profile(5,4,1,3,2,5), // demon
            new Profile(5,4,0,0,4,6), // undead
            new Profile(5,4,4,5,6,2), // ghost
            new Profile(5,4,7,5,3,6), // cursed object
            new Profile(5,4,4,6,7,0), // shadow
            new Profile(4,5,6,1,8,4), // astral
            new Profile(1,4,0,0,5,6), // abyssal
            new Profile(5,4,7,3,9,4), // chaos
            new Profile(6,5,7,3,9,2) // truth
        };
        public static Profile For(SpeciesFamily family) { return Profiles[(int)family]; }
        public static void Apply(SpeciesDefinition species)
        {
            var p=For(species.family);
            foreach(var resistance in species.elementResistances)
            {
                if((int)resistance.element==p.primary)resistance.value.damageReduction=Mathf.Clamp(resistance.value.damageReduction+.35f,.45f,.65f);
                else if((int)resistance.element==p.weak)resistance.value.damageReduction=Mathf.Min(resistance.value.damageReduction,.08f);
            }
            foreach(var resistance in species.debuffResistances)
                if((int)resistance.debuff==p.debuff)resistance.value.applicationResistance=Mathf.Clamp(resistance.value.applicationResistance+.35f,.45f,.65f);
        }
    }
}
