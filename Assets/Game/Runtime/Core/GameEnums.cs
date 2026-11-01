using System;
using UnityEngine;

namespace FusionAnyMonster
{
    public enum SpeciesFamily { Slime, Beast, Bird, Reptile, Amphibian, Aquatic, Insect, Crustacean, Plant, Fungi, DemiHuman, Giant, Material, Mineral, Machine, ArtificialLife, Arcane, Dragon, DivineBeast, Fairy, Spirit, Angel, Demon, Undead, Ghost, CursedObject, Shadow, Astral, Abyssal, Chaos, Truth }
    public enum SpeciesRank { F, E, D, C, B, A, S }
    public enum AttributeElement { Fire, Water, Wind, Earth, Light, Dark, None }
    public enum DebuffType { Poison, Burn, Freeze, Paralysis, Sleep, Slow, Silence, Curse }
    public enum PersonalityType { Brave, Timid, Calm, Cautious, Aggressive, Gentle }

    public enum BuffType { AttackBoost=12, DefenseBoost=13, Haste=14 }

    public static class RankRules
    {
        private static readonly int[] DefaultMagicSlots = { 1, 1, 2, 2, 3, 4, 5 };
        private static RankMagicSlotDatabase database;
        public static int MagicSlots(SpeciesRank rank) { if (database == null) database = Resources.Load<RankMagicSlotDatabase>("RankMagicSlotDatabase"); return database == null ? DefaultMagicSlots[Math.Max(0, Math.Min(6, (int)rank))] : database.GetSlots(rank); }
    }
}
