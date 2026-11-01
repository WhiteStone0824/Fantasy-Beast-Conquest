using System;
using UnityEngine;

namespace FusionAnyMonster
{
    [Serializable]
    public sealed class UnitStats
    {
        [Min(1)] public float hp = 50;
        [Min(0)] public float mp = 20;
        [Min(0)] public float physicalAttack = 8;
        [Min(0)] public float magicAttack = 8;
        [Min(0)] public float physicalDefense = 4;
        [Min(0)] public float magicDefense = 4;
        [Min(0)] public float moveSpeed = 2.5f;
        [Min(0.1f)] public float attackSpeed = 1;
        [Min(0)] public float searchRange = 7;
        [Min(0)] public float mpRegen = 1;
        public UnitStats Copy() { return (UnitStats)MemberwiseClone(); }
        public float Get(int index) { switch(index){case 0:return hp;case 1:return mp;case 2:return physicalAttack;case 3:return magicAttack;case 4:return physicalDefense;case 5:return magicDefense;case 6:return moveSpeed;case 7:return attackSpeed;case 8:return searchRange;default:return mpRegen;} }
        public void Set(int index,float value) { switch(index){case 0:hp=value;break;case 1:mp=value;break;case 2:physicalAttack=value;break;case 3:magicAttack=value;break;case 4:physicalDefense=value;break;case 5:magicDefense=value;break;case 6:moveSpeed=value;break;case 7:attackSpeed=value;break;case 8:searchRange=value;break;default:mpRegen=value;break;} }
    }

    [Serializable] public sealed class ResistanceValue { [Range(0,1)] public float damageReduction; [Range(0,1)] public float applicationResistance; }
    [Serializable] public sealed class ElementResistance { public AttributeElement element; public ResistanceValue value = new ResistanceValue(); }
    [Serializable] public sealed class DebuffResistance { public DebuffType debuff; public ResistanceValue value = new ResistanceValue(); }

    [CreateAssetMenu(menuName="Fusion Any Monster/Species Definition")]
    public sealed class SpeciesDefinition : ScriptableObject
    {
        public string speciesId;
        public string displayName;
        [TextArea] public string description;
        public SpeciesFamily family;
        public SpeciesRank rank;
        public UnitStats baseStats = new UnitStats();
        public ElementResistance[] elementResistances = new ElementResistance[0];
        public DebuffResistance[] debuffResistances = new DebuffResistance[0];
        public Sprite sprite;
        public PersonalityType defaultTendency;
        public SpeciesPassive passive;
        [Tooltip("種族の初期GP魔法。5枠を保持し、ランクに対応する枠を使用します。個体へは複製して渡します。")]
        public MagicGenome[] defaultMagicGenomes = new MagicGenome[0];

        public ResistanceValue GetResistance(AttributeElement element)
        {
            if (elementResistances != null)
                for (int i = 0; i < elementResistances.Length; i++)
                    if (elementResistances[i] != null && elementResistances[i].element == element && elementResistances[i].value != null) return elementResistances[i].value;
            return new ResistanceValue();
        }
        public ResistanceValue GetResistance(DebuffType debuff)
        {
            if (debuffResistances != null)
                for (int i = 0; i < debuffResistances.Length; i++)
                    if (debuffResistances[i] != null && debuffResistances[i].debuff == debuff && debuffResistances[i].value != null) return debuffResistances[i].value;
            return new ResistanceValue();
        }
    }

}
