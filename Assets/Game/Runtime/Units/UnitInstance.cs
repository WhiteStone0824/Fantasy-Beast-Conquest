using System;
using System.Collections.Generic;
using UnityEngine;

namespace FusionAnyMonster
{
    [Serializable] public sealed class IndividualValues
    {
        public const int MinValue = 0;
        public const int MaxValue = 100;
        public const int StatCount = 10;
        [Range(MinValue,MaxValue)] public int hp, mp, physicalAttack, magicAttack, physicalDefense, magicDefense, moveSpeed, attackSpeed, searchRange, mpRegen;
        public int Get(int i) { return Clamp(RawValue(i)); }
        private int RawValue(int i) { switch(i){case 0:return hp;case 1:return mp;case 2:return physicalAttack;case 3:return magicAttack;case 4:return physicalDefense;case 5:return magicDefense;case 6:return moveSpeed;case 7:return attackSpeed;case 8:return searchRange;default:return mpRegen;} }
        public static int Clamp(int value) { return Mathf.Clamp(value,MinValue,MaxValue); }
        public void Set(int i,int v) { v=Clamp(v);switch(i){case 0:hp=v;break;case 1:mp=v;break;case 2:physicalAttack=v;break;case 3:magicAttack=v;break;case 4:physicalDefense=v;break;case 5:magicDefense=v;break;case 6:moveSpeed=v;break;case 7:attackSpeed=v;break;case 8:searchRange=v;break;default:mpRegen=v;break;} }
        public static IndividualValues Random(System.Random rng) { var v=new IndividualValues();for(int i=0;i<StatCount;i++)v.Set(i,rng.Next(MinValue,MaxValue+1));return v; }
    }

    [Serializable] public sealed class LineageRecord
    {
        public string fatherId, motherId;
        public int generation;
        public string breedingSummary;
    }

    [Serializable] public sealed class UnitInstance
    {
        public string id;
        public string speciesId;
        [Range(1,99)] public int level = 1;
        public long experience;
        public IndividualValues iv = new IndividualValues();
        public PersonalityType personality;
        public List<MagicGenome> magicGenomes = new List<MagicGenome>();
        public List<MagicGenome> magicGeneSlots = new List<MagicGenome>();
        public LineageRecord lineage = new LineageRecord();
        public List<string> mutationHistory = new List<string>();
        public bool deployed;
        public EquipmentItem equipment;

        public static UnitInstance Create(string speciesId, PersonalityType personality, int seed)
        {
            var rng=new System.Random(seed);
            return new UnitInstance { id=Guid.NewGuid().ToString("N"), speciesId=speciesId, personality=personality, iv=IndividualValues.Random(rng), lineage=new LineageRecord() };
        }
    }

    [Serializable] public sealed class PersonalityDefinition
    {
        public PersonalityType type;
        public string displayName;
        public int boostedStatIndex = -1;
        public int reducedStatIndex = -1;
        [Range(.1f,3f)] public float boostMultiplier = 1.1f;
        [Range(.1f,3f)] public float reductionMultiplier = .9f;
        [Range(0,1)] public float aggression = .5f;
        [Range(0,1)] public float caution = .5f;
        [Range(0,1)] public float teamwork = .5f;
        [Range(0,1)] public float lowHpPriority = .5f;
        [Min(.1f)] public float chargeCooldownMultiplier = 1f;
        [Min(.1f)] public float chargeRetreatDistance = 1.3f;
        public static float DefaultChargeCooldown(PersonalityType type)
        {
            switch(type) { case PersonalityType.Brave:return .7f; case PersonalityType.Timid:return 2.6f; case PersonalityType.Calm:return 1.5f; case PersonalityType.Cautious:return 2.2f; case PersonalityType.Aggressive:return .5f; case PersonalityType.Gentle:return 2f; default:return 1f; }
        }
        public static float DefaultRetreatDistance(PersonalityType type)
        {
            switch(type) { case PersonalityType.Brave:return 1f; case PersonalityType.Timid:return 2f; case PersonalityType.Calm:return 1.5f; case PersonalityType.Cautious:return 1.9f; case PersonalityType.Aggressive:return .8f; case PersonalityType.Gentle:return 1.7f; default:return 1.3f; }
        }
    }

}
